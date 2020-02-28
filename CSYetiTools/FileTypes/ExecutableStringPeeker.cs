using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CsYetiTools.FileTypes
{
    public class StringSegment
    {
        public int Offset { get; set; }
        public int Length { get; set; }
        public string Content { get; set; }

        public StringSegment(int offset, int length, string content)
        {
            Offset = offset;
            Length = length;
            Content = content.Replace("%N", "\n");

            if (Length % 4 != 0) throw new ArgumentException($"0x{Offset:X08}: {Length} is not multiple of 4");
        }

        public static StringSegment FromStream(Stream stream, int maxOffset, Encoding encoding)
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
            return new StringSegment(offset, length, encoding.GetString(bytes.ToArray()));
        }

        public void Modify(Stream stream, Encoding encoding, string? replacement, bool throwIfLengthError)
        {
            var content = replacement ?? Content;
            var bytes = encoding.GetBytes(content.Replace("\n", "%N"));
            if (bytes.Length + 1 > Length)
            {
                if (throwIfLengthError)
                {
                    throw new InvalidOperationException($"\"{content.Replace("\"", "\\\"")}\" length > {Length - 1}");
                }
                else
                {
                    if (content.Length == bytes.Length || Length <= 5) // all single-byte means maybe english, or too short.
                    {
                        var color = Console.ForegroundColor;
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"\"{content.Replace("\"", "\\\"")}\" length > \"{Content.Replace("\"", "\\\"")}\", use source");
                        Console.ForegroundColor = color;
                        content = Content;
                        bytes = encoding.GetBytes(content.Replace("\n", "%N"));
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
                        Console.WriteLine($"trunked \"{content.Replace("\"", "\\\"")}\"(\"{Content.Replace("\"", "\\\"")}\") to {newString.Replace("\"", "\\\"")}");
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
        private class StringSegmentRange
        {
            public string Name { get; set; }

            public List<(int begin, int end)> Ranges { get; set; }

            public StringSegmentRange(string name, IEnumerable<(int begin, int end)> ranges)
            {
                Name = name;
                Ranges = ranges.ToList();
            }
        }

        private List<(string name, List<StringSegment> segments)> _ranges { get; } = new List<(string, List<StringSegment>)>();

        public ExecutableStringPeeker(Stream stream, SExpr rangesExpr, Encoding encoding)
        {
            var ranges = rangesExpr.AsEnumerable()
                .Select(expr => new StringSegmentRange(
                    expr.Car.AsSymbol(),
                    expr.Cdr.AsEnumerable().Select(arg => (arg.Car.AsInt(), arg.Cdr.Car.AsInt()))
                ));
            foreach (var range in ranges)
            {
                var segments = new List<StringSegment>();
                foreach (var (begin, end) in range.Ranges)
                {
                    stream.Position = begin;
                    while (stream.Position < end)
                    {
                        segments.Add(StringSegment.FromStream(stream, end, encoding));
                    }
                }
                _ranges.Add((range.Name, segments));
            }
        }

        public static ExecutableStringPeeker FromFile(string path, SExpr rangesExpr, Encoding encoding)
        {
            using var file = File.OpenRead(path);
            return new ExecutableStringPeeker(file, rangesExpr, encoding);
        }

        public static ExecutableStringPeeker FromFile(string path, Encoding encoding)
        {
            return FromFile(path, SExpr.ParseFile(Path.Combine(Path.GetDirectoryName(path)!, "exe_string_pool.ss")), encoding);
        }

        public IEnumerable<string> Names
            => _ranges.Select(kv => kv.name);

        public List<StringSegment> Segments(string name)
            => _ranges.First(kv => kv.name == name).segments;

        public void Modify(Stream stream, Encoding encoding, string stringPoolDirPath)
        {
            foreach (var (name, segs) in _ranges)
            {
                var sexprs = SExpr.ParseFile(Path.Combine(stringPoolDirPath, name + ".ss"));
                var references = sexprs.AsEnumerable().Select(exp => "\n".Join(exp.AsEnumerable().Select(e => e.AsString()))).ToList();
                if (segs.Count != references.Count) throw new ArgumentException($"{name}: references({references.Count}) doesnt match segs({segs.Count})");
                foreach (var (i, seg) in segs.Reverse<StringSegment>().WithIndex())
                {
                    seg.Modify(stream, encoding, references[i], false);
                }
            }
        }

        public void DumpTranslateSource(string name, string path, IList<string> references)
        {
            var segs = Segments(name);
            if (segs.Count != references.Count) throw new ArgumentException($"{name}: references({references.Count}) doesnt match segs({segs.Count})");

            using var writer = new StreamWriter(path, false, Utils.Utf8);
            writer.NewLine = "\n";

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

            writer.WriteLine(JsonConvert.SerializeObject(dict, Utils.JsonSettings));
        }

        public void DumpTranslateSource(string dirPath, string stringPoolDirPath)
        {
            Utils.CreateOrClearDirectory(dirPath);
            foreach (var name in Names)
            {
                var sexprs = SExpr.ParseFile(Path.Combine(stringPoolDirPath, name + ".ss"));
                var references = sexprs.AsEnumerable().Select(exp => "\n".Join(exp.AsEnumerable().Select(e => e.AsString()))).ToList();
                DumpTranslateSource(name, Path.Combine(dirPath, name.Replace('-', '_') + ".json"), references);
            }
        }
    }
}