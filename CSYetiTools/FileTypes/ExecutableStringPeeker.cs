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

        public void Modify(Stream stream, Encoding encoding)
        {
            var bytes = encoding.GetBytes(Content.Replace("\n", "%N"));
            if (bytes.Length + 1 >= Length)
            {
                throw new InvalidOperationException($"\"{Content.Replace("\"", "\\\"")}\" length > {Length - 1}");
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

        public void Modify(Stream stream, Encoding encoding)
        {
            foreach (var (name, segs) in _ranges)
            {
                foreach (var seg in segs) seg.Modify(stream, encoding);
            }
        }

        public void DumpTranslateSource(string name, string path, IList<string> reference)
        {
            var segs = Segments(name);
            if (segs.Count != reference.Count) throw new ArgumentException($"{name}: reference({reference.Count}) doesnt match segs({segs.Count})");

            using var writer = new StreamWriter(path, false, Utils.Utf8);
            writer.NewLine = "\n";

            var jobj = new JObject();
            foreach (var (i, seg) in segs.Reverse<StringSegment>().WithIndex())
            {
                var offset = seg.Offset.ToString("X08");
                jobj.Add(offset, JObject.FromObject(new Transifex.TranslationInfo
                {
                    String = reference[i],
                    Context = offset,
                    Offset = offset,
                    DeveloperComment = seg.Content,
                    CharacterLimit = seg.Length / 2 - 1,
                }));
            }

            writer.WriteLine(JsonConvert.SerializeObject(jobj, Utils.JsonSettings));
        }

        public void DumpTranslateSource(string dirPath, string stringPoolDirPath)
        {
            Utils.CreateOrClearDirectory(dirPath);
            foreach (var name in Names)
            {
                var sexprs = SExpr.ParseFile(Path.Combine(stringPoolDirPath, name + ".ss"));
                var list = sexprs.AsEnumerable().Select(exp => "\n".Join(exp.AsEnumerable().Select(e => e.AsString()))).ToList();
                DumpTranslateSource(name, Path.Combine(dirPath, name.Replace('-', '_') + ".json"), list);
            }
        }
    }
}