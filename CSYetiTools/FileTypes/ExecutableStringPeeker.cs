using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Untitled.Sexp;
using Untitled.Sexp.Attributes;

namespace CsYetiTools.FileTypes
{
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
            if (escapeLinefeed)
            {
                Content = content.Replace("%N", "\n");
            }
            else
            {
                Content = content;
            }

            if (Length % 4 != 0) throw new ArgumentException($"0x{Offset:X08}: {Length} is not multiple of 4");
        }

        public static StringSegment FromStream(Stream stream, int maxOffset, Encoding encoding, bool escapeLinefeed)
        {
            var offset = (int)stream.Position;
            var bytes = new List<byte>();
            int b;
            while ((b = stream.ReadByte()) > 0)
            {
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
            var bytes =  encoding.GetBytes(content);
            if (bytes.Length + 1 > Length)
            {
                if (throwIfLengthError)
                {
                    throw new InvalidOperationException($"{new SValue(content)} length > {Length - 1}");
                }
                else
                {
                    if (content.Length == bytes.Length || Length <= 5) // all single-byte means maybe english, or too short.
                    {
                        var color = Console.ForegroundColor;
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"{Offset:X08}: {new SValue(content)} length > {new SValue(Content)} (limit: {Length}), use source");
                        Console.ForegroundColor = color;
                        content = Content;
                        bytes = encoding.GetBytes(content);
                    }
                    else
                    {
                        var byteList = new List<byte>();
                        var chars1 = new char[1];
                        var chars2 = new char[2];
                        foreach (var c in content)
                        {
                            byte[] newBytes;
                            if (char.IsHighSurrogate(c))
                            {
                                chars2[0] = c;
                                continue;
                            }
                            else if (char.IsLowSurrogate(c))
                            {
                                chars2[1] = c;
                                newBytes = encoding.GetBytes(chars2);
                            }
                            else
                            {
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
                        var color = Console.ForegroundColor;
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"{Offset:X08}: trunked (limit: {Length}) {new SValue(content)}({new SValue(Content)}) to {new SValue(newString)}");
                        Console.ForegroundColor = color;
                    }
                }
            }
            stream.Position = Offset;
            stream.Write(bytes);
            for (int i = bytes.Length; i < Length; ++i)
            {
                stream.WriteByte(0);
            }
        }
    }

    public class ExecutableStringPeeker
    {
#nullable disable
        [SexpAsList]
        private class StringSegmentRange
        {
            public Symbol Id { get; set; }

            public bool EscapeLinefeed { get; set; }

            public List<Pair> Ranges { get; set; }
        }
#nullable enable

        private class SegmentGroup
        {
            public string Name { get; set; }
            public bool EscapeLinefeed { get; set; }
            public List<StringSegment> Segments { get; set; }

            public SegmentGroup(string name, bool escapeLinefeed, IEnumerable<StringSegment> segments)
            {
                Name = name;
                EscapeLinefeed = escapeLinefeed;
                Segments = segments.ToList();
            }
        }

        private List<SegmentGroup> _rangeGroups { get; } = new List<SegmentGroup>();

        public ExecutableStringPeeker(Stream stream, SValue rangesExpr, Encoding encoding)
        {
            var ranges = rangesExpr.AsEnumerable()
                .Select(expr => SexpConvert.ToObject<StringSegmentRange>(expr));
            foreach (var range in ranges)
            {
                var segments = new List<StringSegment>();
                foreach (var (begin, end) in range.Ranges)
                {
                    stream.Position = begin.AsInt();
                    while (stream.Position < end.AsInt())
                    {
                        segments.Add(StringSegment.FromStream(stream, end.AsInt(), encoding, range.EscapeLinefeed));
                    }
                }
                _rangeGroups.Add(new SegmentGroup(range.Id.Name, range.EscapeLinefeed, segments));
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
            => _rangeGroups.Select(group => group.Name);

        public List<StringSegment> Segments(string name)
            => _rangeGroups.First(group => group.Name == name).Segments;

        public void Modify(Stream stream, Encoding encoding, FilePath stringPoolDirPath)
        {
            foreach (var group in _rangeGroups)
            {
                var name = group.Name;
                var segs = group.Segments;
                var references = LoadReferences(stringPoolDirPath, name);
                if (segs.Count != references.Count) throw new ArgumentException($"{name}: references({references.Count}) doesnt match segs({segs.Count})");
                foreach (var (i, seg) in segs.Reverse<StringSegment>().WithIndex())
                {
                    seg.Modify(stream, encoding, references[i], group.EscapeLinefeed, false);
                }
            }
        }

        public void Modify(Stream stream, Encoding encoding)
        {
            foreach (var group in _rangeGroups)
            {
                var name = group.Name;
                foreach (var (i, seg) in group.Segments.Reverse<StringSegment>().WithIndex())
                {
                    seg.Modify(stream, encoding, null, group.EscapeLinefeed, false);
                }
            }
        }

        public void DumpTranslateSource(string name, FilePath path, IList<string> references)
        {
            var segs = Segments(name);
            if (segs.Count != references.Count) throw new ArgumentException($"{name}: references({references.Count}) doesnt match segs({segs.Count})");

            var dict = new SortedDictionary<string, Transifex.TranslationInfo>();
            foreach (var (i, seg) in segs.Reverse<StringSegment>().WithIndex())
            {
                dict.Add(i.ToString("0000"), new Transifex.TranslationInfo
                {
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
            foreach (var name in Names)
            {
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

        private void ApplyTranslations(string name, IList<string> references, IList<string> translations)
        {
            var segs = Segments(name);
            if (segs.Count != references.Count) throw new ArgumentException($"{name}: references({references.Count}) doesnt match segs({segs.Count})");
            if (segs.Count != translations.Count) throw new ArgumentException($"{name}: translations({references.Count}) doesnt match segs({segs.Count})");

            foreach (var (i, seg) in segs.Reverse<StringSegment>().WithIndex())
            {
                var trans = translations[i];
                var trimmed = trans.Trim();
                if (trimmed == "@en") { trans = seg.Content; }
                else if (trimmed == "@cp" || trimmed == "@jp") { trans = references[i]; }

                seg.Content = trans;
            }
        }

        public void ApplyTranslations(FilePath translationDirPath, FilePath referenceStringPoolPath)
        {
            foreach (var name in Names)
            {
                var references = LoadReferences(referenceStringPoolPath, name);
                var path = translationDirPath / (name.Replace('-', '_') + ".json");
                if (File.Exists(path))
                {
                    try
                    {
                        var dict = Utils.DeserializeJsonFromFile<SortedDictionary<string, Transifex.TranslationInfo>>(path);
                        var translations = dict.Values.Select(info => info.String).ToList();

                        ApplyTranslations(name, references, translations);
                    }
                    catch (Exception exc)
                    {
                        throw new InvalidDataException($"Error loading {path}", exc);
                    }
                }
            }
        }

        public IEnumerable<char> EnumerateChars()
        {
            foreach (var group in _rangeGroups)
            {
                foreach (var seg in group.Segments)
                {
                    foreach (var c in seg.Content) yield return c;
                }
            }
        }
    }
}