using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Untitled.Sexp;
using Untitled.Sexp.Attributes;
using CSYetiTools.Base;
using CSYetiTools.VnScripts.Transifex;

namespace CSYetiTools.Commandlet.FileTypes;

public class StringSegment
{
    public int Offset { get; set; }
    public int Length { get; set; }
    public string Content { get; set; }
    public bool EscapeLinefeed { get; set; }

    public StringSegment(int offset, int length, string content, bool escapeLinefeed)
    {
        Offset = offset;
        Length = length;
        EscapeLinefeed = escapeLinefeed;
        Content = escapeLinefeed ? content.Replace("%N", "\n") : content;

        if (Length % 4 != 0) throw new ArgumentException($"0x{Offset:X08}: {Length} is not multiple of 4");
    }

    public static StringSegment FromStream(Stream stream, int maxOffset, Encoding encoding, bool escapeLinefeed)
    {
        var offset = (int)stream.Position;
        var bytes = new List<byte>();
        int b;
        while ((b = stream.ReadByte()) > 0) {
            bytes.Add((byte)b);
        }
        while (stream.Position < maxOffset && stream.Peek() == 0) stream.ReadByte();
        var length = (int)(stream.Position - offset);
        return new StringSegment(offset, length, encoding.GetString(bytes.ToArray()), escapeLinefeed);
    }

    public void Modify(Stream stream, Encoding encoding, string? replacement, bool escapeLinefeed, bool throwIfLengthError)
    {
        var content = replacement ?? Content;
        if (escapeLinefeed) content = content.Replace("\n", "%N");
        var bytes = encoding.GetBytes(content);
        if (bytes.Length + 1 > Length) {
            if (throwIfLengthError) {
                throw new InvalidOperationException($"{new SValue(content)} length > {Length - 1}");
            }
            else {
                if (content.Length == bytes.Length || Length <= 5) { // too short.
                    Utils.PrintError($"{Offset:X08}: {new SValue(content)} length > {new SValue(Content)} (limit: {Length}), use source");
                    content = Content;
                    bytes = encoding.GetBytes(content);
                }
                else {
                    var byteList = new List<byte>();
                    var chars1 = new char[1];
                    var chars2 = new char[2];
                    foreach (var c in content) {
                        byte[] newBytes;
                        if (char.IsHighSurrogate(c)) {
                            chars2[0] = c;
                            continue;
                        }
                        else if (char.IsLowSurrogate(c)) {
                            chars2[1] = c;
                            newBytes = encoding.GetBytes(chars2);
                        }
                        else {
                            chars1[0] = c;
                            newBytes = encoding.GetBytes(chars1);
                        }
                        if (byteList.Count + newBytes.Length + 1 > Length) break;
                        byteList.AddRange(newBytes);
                    }
                    bytes = byteList.ToArray();
                    if (bytes.Length + 1 > Length)
                        throw new InvalidProgramException("???");
                    var newString = encoding.GetString(bytes);
                    Utils.PrintError($"{Offset:X08}: truncated (limit: {Length}) {new SValue(content)}({new SValue(Content)}) to {new SValue(newString)}");
                }
            }
        }
        stream.Position = Offset;
        stream.Write(bytes);
        foreach (var _ in bytes.Length..Length) {
            stream.WriteByte(0);
        }
    }
}

public class ExecutableStringPeeker
{
#nullable disable
    [UsedImplicitly]
    [SexpAsList]
    private class StringSegmentRange
    {
        [UsedImplicitly] public Symbol Id { get; set; }

        [UsedImplicitly] public bool EscapeLinefeed { get; set; }

        [UsedImplicitly] public List<Pair> Ranges { get; set; }
    }
#nullable enable

    private class SegmentGroup
    {
        public string Name { get; }
        public bool EscapeLinefeed { get; }
        public List<StringSegment> Segments { get; }

        public SegmentGroup(string name, bool escapeLinefeed, IEnumerable<StringSegment> segments)
        {
            Name = name;
            EscapeLinefeed = escapeLinefeed;
            Segments = segments.ToList();
        }
    }

    private List<SegmentGroup> RangeGroups { get; } = new();

    public ExecutableStringPeeker(Stream stream, SValue rangesExpr, Encoding encoding)
    {
        var ranges = rangesExpr.AsEnumerable()
            .Select(SexpConvert.ToObject<StringSegmentRange>);
        foreach (var range in ranges) {
            var segments = new List<StringSegment>();
            foreach (var (begin, end) in range.Ranges) {
                stream.Position = begin.AsInt();
                while (stream.Position < end.AsInt()) {
                    segments.Add(StringSegment.FromStream(stream, end.AsInt(), encoding, range.EscapeLinefeed));
                }
            }
            RangeGroups.Add(new SegmentGroup(range.Id.Name, range.EscapeLinefeed, segments));
        }
    }

    public static ExecutableStringPeeker FromFile(FilePath path, SValue rangesExpr, Encoding encoding)
    {
        using var file = File.OpenRead(path);
        return new ExecutableStringPeeker(file, rangesExpr, encoding);
    }

    public static ExecutableStringPeeker FromFile(FilePath path, Encoding encoding)
    {
        return FromFile(path, Sexp.ParseFile(path.Parent / "exe_string_pool.sexp"), encoding);
    }

    public IEnumerable<string> Names
        => RangeGroups.Select(group => group.Name);

    public List<StringSegment> Segments(string name)
        => RangeGroups.First(group => group.Name == name).Segments;

    public void Modify(Stream stream, Encoding encoding, FilePath stringPoolDirPath)
    {
        foreach (var group in RangeGroups) {
            var name = group.Name;
            var segs = group.Segments;
            var references = LoadReferences(stringPoolDirPath, name);
            if (segs.Count != references.Count) throw new ArgumentException($"{name}: references({references.Count}) doesnt match segs({segs.Count})");
            foreach (var (i, seg) in segs.Reverse<StringSegment>().WithIndex()) {
                seg.Modify(stream, encoding, references[i], group.EscapeLinefeed, false);
            }
        }
    }

    public void Modify(Stream stream, Encoding encoding)
    {
        foreach (var group in RangeGroups) {
            foreach (var (_, seg) in group.Segments.Reverse<StringSegment>().WithIndex()) {
                seg.Modify(stream, encoding, null, group.EscapeLinefeed, false);
            }
        }
    }

    public void DumpTranslateSource(string name, FilePath path, IList<string> references)
    {
        var segs = Segments(name);
        if (segs.Count != references.Count) throw new ArgumentException($"{name}: references({references.Count}) doesnt match segs({segs.Count})");

        var dict = new SortedDictionary<string, TranslationInfo>();
        foreach (var (i, seg) in segs.Reverse<StringSegment>().WithIndex()) {
            dict.Add(i.ToString("0000"), new TranslationInfo {
                String = references[i],
                Context = seg.Offset.ToString("X08"),
                DeveloperComment = seg.Content,
                CharacterLimit = seg.Length / 2 - 1,
            });
        }

        Utils.SerializeJsonToFile(dict, path);
    }

    public void DumpTranslateSource(FilePath dirPath, FilePath stringPoolDirPath)
    {
        Utils.CreateOrClearDirectory(dirPath);
        foreach (var name in Names) {
            var references = LoadReferences(stringPoolDirPath, name);
            DumpTranslateSource(name, dirPath / (name.Replace('-', '_') + ".json"), references);
        }
    }

    private static List<string> LoadReferences(FilePath stringPoolDirPath, string name)
    {
        using var reader = new StreamReader(stringPoolDirPath / (name + ".sexp"), Encoding.UTF8, true);
        var sexpReader = new SexpTextReader(reader);
        return sexpReader.ReadAll().Select(exp => "\n".Join(exp.AsEnumerable<string>())).ToList();
    }

    private void ApplyTranslations(string name, IList<string> references, IDictionary<int, string> translations)
    {
        var segs = Segments(name);
        if (segs.Count != references.Count) throw new ArgumentException($"{name}: references({references.Count}) doesnt match segs({segs.Count})");
        if (segs.Count != translations.Count) throw new ArgumentException($"{name}: translations({references.Count}) doesnt match segs({segs.Count})");

        foreach (var (i, seg) in segs.Reverse<StringSegment>().WithIndex()) {
            var trans = translations[i];
            var trimmed = trans.Trim();
            if (trimmed == "@en") { trans = seg.Content; }
            else if (trimmed == "@cp" || trimmed == "@jp") { trans = references[i]; }

            seg.Content = trans;
        }
    }

    public void ApplyTranslations(FilePath translationDirPath, FilePath referenceStringPoolPath)
    {
        foreach (var name in Names) {
            var references = LoadReferences(referenceStringPoolPath, name);
            var yamlPath = translationDirPath / (name.Replace('-', '_') + ".yaml");
            var jsonPath = translationDirPath / (name.Replace('-', '_') + ".json");
            if (File.Exists(yamlPath)) {
                try {
                    using var reader = new StreamReader(yamlPath);
                    var dict = new YamlDotNet.Serialization.Deserializer().Deserialize<Dictionary<string, string>>(reader);
                    var translations = dict.ToDictionary(kv => int.Parse(kv.Key), kv => kv.Value);
                    ApplyTranslations(name, references, translations);
                }
                catch (Exception exc) {
                    throw new InvalidDataException($"Error loading {yamlPath}", exc);
                }
            }
            else if (File.Exists(jsonPath)) {
                try {
                    var dict = Utils.DeserializeJsonFromFile<SortedDictionary<string, TranslationInfo>>(jsonPath);
                    var translations = dict.ToDictionary(kv => int.Parse(kv.Key), kv => kv.Value.String);
                    // var offsets = new HashSet<int>(RangeGroups.SelectMany(g => g.Segments.Select(s => s.Offset)));
                    // var translations = dict.Where(kv=> offsets.Contains(Convert.ToInt32(kv.Value.Context, 16)))
                    //     .Select(kv => kv.Value.String).ToList();

                    ApplyTranslations(name, references, translations);
                }
                catch (Exception exc) {
                    throw new InvalidDataException($"Error loading {jsonPath}", exc);
                }
            }
        }
    }

    public IEnumerable<char> EnumerateChars()
    {
        foreach (var group in RangeGroups) {
            foreach (var seg in group.Segments) {
                foreach (var c in seg.Content) yield return c;
            }
        }
    }
}
