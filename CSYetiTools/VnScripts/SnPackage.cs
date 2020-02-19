using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CsYetiTools.VnScripts
{
    public sealed class SnPackage
    {
        public class FootersChunk
        {
            private ScriptFooter[] _footers;

            public ScriptFooter this[int index]
            {
                get => _footers[index];
            }

            public FootersChunk(byte[] source)
            {
                using var ms = new MemoryStream(source);
                using var reader = new BinaryReader(ms);
                var footers = new List<ScriptFooter>();
                while (true)
                {
                    var footer = ScriptFooter.ReadFrom(reader);
                    footers.Add(footer);
                    if (footer.IndexedDialogCount == -1) break;
                }
                if (footers.Count < 2)
                {
                    throw new InvalidDataException($"Footer chunk length({footers.Count}) < 2");
                }

                var checkFooter = footers[^2];
                var accFooter = new ScriptFooter();
                foreach (var footer in footers.SkipLast(2))
                {
                    accFooter.IndexedDialogCount += footer.IndexedDialogCount;
                    accFooter.Unknown += footer.Unknown;
                    accFooter.FlagCodeCount += footer.FlagCodeCount;
                    accFooter.ScriptIndex += footer.ScriptIndex;
                }
                if (!accFooter.Equals(checkFooter))
                {
                    throw new InvalidDataException($"Footer sum check failed: [{accFooter}] != [{checkFooter}]");
                }

                _footers = footers.ToArray();
            }

            public FootersChunk(IEnumerable<ScriptFooter> footers)
            {
                var list = footers.ToList();
                var accFooter = new ScriptFooter();
                foreach (var footer in footers)
                {
                    accFooter.IndexedDialogCount += footer.IndexedDialogCount;
                    accFooter.Unknown += footer.Unknown;
                    accFooter.FlagCodeCount += footer.FlagCodeCount;
                    accFooter.ScriptIndex += footer.ScriptIndex;
                }
                list.Add(accFooter);
                list.Add(new ScriptFooter { IndexedDialogCount = -1 });
                _footers = list.ToArray();
            }

            public byte[] ToBytes()
            {
                using var ms = new MemoryStream();
                using var writer = new BinaryWriter(ms);
                foreach (var footer in _footers)
                {
                    footer.WriteTo(writer);
                }
                return ms.ToArray();
            }

            public void Dump(TextWriter writer)
            {
                foreach (var footer in _footers)
                {
                    writer.WriteLine(footer);
                }
            }
        }

        private Script[] _scripts;

        public FootersChunk Footers
            => new FootersChunk(_scripts.Select(s => s.Footer));

        private SnPackage(Script[] scripts, FootersChunk footers)
        {
            _scripts = scripts;
        }

        public SnPackage(string filename, bool isStringPooled)
        {
            var data = File.ReadAllBytes(filename);
            var decodedSize = BitConverter.ToInt32(data);
            var bytes = LZSS.Decode(data.Skip(4)).ToArray();

            if (decodedSize != bytes.Length)
            {
                throw new InvalidDataException($"Invalid size (header = {decodedSize}, bytes = {bytes.Length}).");
            }
            try
            {
                int maxOffset = BitConverter.ToInt32(bytes, 0);
                var chunks = new List<byte[]>();

                for (int offset = 0; offset < maxOffset; offset += 16)
                {
                    int chunkOffset = BitConverter.ToInt32(bytes, offset);
                    int chunkSize = BitConverter.ToInt32(bytes, offset + 4);

                    var chunk = new byte[chunkSize];
                    Array.Copy(bytes, chunkOffset, chunk, 0, chunkSize);
                    chunks.Add(chunk);
                }

                var footers = new FootersChunk(chunks.Last());

                _scripts = new Script[chunks.Count - 1];

                Parallel.For(0, _scripts.Length, i =>
                {
                    _scripts[i] = new Script(chunks[i], footers[i], isStringPooled);
                });
            }
            catch (IndexOutOfRangeException)
            {
                throw new ArgumentException("Incomplete file");
            }

            System.Diagnostics.Debug.Assert(bytes.SequenceEqual(ToRawBytes()));
        }

        public static SnPackage CreateFrom(string dirPath, bool isStringPooled)
        {
            if (!Directory.Exists(dirPath)) throw new ArgumentException($"Directory {dirPath} not exists!");

            var footersPath = Path.Combine(dirPath, "footers");
            if (!File.Exists(footersPath)) throw new ArgumentException("File footers not exists!");
            var footers = new FootersChunk(File.ReadAllBytes(footersPath));

            var scripts = Directory.GetFiles(dirPath, "*.script")
                .OrderBy(o => o)
                .Select((file, i) => new Script(File.ReadAllBytes(file), footers[i], isStringPooled))
                .ToArray();

            return new SnPackage(scripts, footers);
        }

        public void Dump(string dirPath, string postfix, bool isDumpBinary, bool isDumpScript)
        {
            Utils.CreateOrClearDirectory(dirPath);

            if (isDumpBinary)
            {
                Parallel.ForEach(_scripts.WithIndex(), entry =>
                {
                    var (i, script) = entry;
                    var path = Path.Combine(dirPath, $"chunk_{i:0000}_{postfix}.script");
                    using var writer = new BinaryWriter(File.Create(path));
                    script.WriteTo(writer);
                });
                File.WriteAllBytes(Path.Combine(dirPath, $"footers_{postfix}"), Footers.ToBytes());
            }

            if (isDumpScript)
            {
                var errors = new List<string>();
                Parallel.ForEach(_scripts.WithIndex(), entry =>
                {
                    var (i, script) = entry;
                    script.DumpText(Path.Combine(dirPath, $"chunk_{i:0000}_{postfix}.script-dump.txt"));
                    if (script.ParseError != null)
                    {
                        var errorInfo = $"Error parsing chunk_{i:0000}_{postfix}: \r\n" + script.ParseError + "\r\n-------------------------------------------------------\r\n";
                        lock (errors) errors.Add(errorInfo);
                    }
                });
                if (errors.Count > 0)
                {
                    File.WriteAllLines(Path.Combine(dirPath, "parse_error.log"), errors);
                }
            }

            using (var writer = new StreamWriter(Path.Combine(dirPath, $"footers_{postfix}.txt")))
            {
                Footers.Dump(writer);
            }
        }

        public void DumpTranslateSource(string dirPath)
        {
            Utils.CreateOrClearDirectory(dirPath);

            var errors = new List<string>();

            var names = new HashSet<string>();

            foreach (var (i, script) in _scripts.WithIndex())
            {
                if (script.Footer.ScriptIndex < 0) continue; // skip non-text scripts

                var dict = new SortedDictionary<string, Transifex.TranslationInfo>();
                foreach (var info in script.EnumerateTranslateSources())
                {
                    dict.Add(info.Context, info);
                }
                var content = JsonConvert.SerializeObject(dict, Transifex.TransifexClient.JsonSettings);
                names.UnionWith(script.EnumerateCharacterNames());

                using (var writer = new StreamWriter(Path.Combine(dirPath, $"chunk_{i:0000}.json"), false, Utils.Utf8))
                {
                    writer.NewLine = "\n";
                    writer.WriteLine(content);
                }
                if (script.ParseError != null)
                {
                    errors.Add($"Error parsing chunk_{i:0000}: \r\n" + script.ParseError + "\r\n-------------------------------------------------------\r\n");
                }
            }

            using (var writer = new StreamWriter(Path.Combine(dirPath, "names.json"), false, Utils.Utf8))
            {
                writer.NewLine = "\n";
                using (var jsonWriter = new JsonTextWriter(writer))
                {
                    jsonWriter.Formatting = Formatting.Indented;
                    var namesObj = new JObject();
                    foreach (var (i, name) in names.WithIndex())
                    {
                        namesObj.Add($"{i:000}", JObject.FromObject(new { @string = name }));
                    }
                    namesObj.WriteTo(jsonWriter);
                }
            }

            if (errors.Count > 0)
            {
                using var errorWriter = new StreamWriter(Path.Combine(dirPath, "parse_error.log"));
                foreach (var error in errors)
                {
                    errorWriter.WriteLine(error);
                }
            }
        }

        public void WriteTo(Stream stream)
        {
            var rawBytes = ToRawBytes();
            var bytes = LZSS.Encode(rawBytes).ToArray();

            stream.Write(BitConverter.GetBytes(rawBytes.Length));
            stream.Write(bytes);
            stream.Flush();
        }

        public void WriteTo(string path)
        {
            using (var stream = File.Create(path))
            {
                WriteTo(stream);
            }
        }

        public IReadOnlyList<Script> Scripts
            => Array.AsReadOnly(_scripts);

        public byte[] ToRawBytes()
        {
            var lengths = new int[_scripts.Length + 1];
            var chunks = new byte[_scripts.Length + 1][];

            Parallel.ForEach(_scripts.WithIndex(), entry =>
            {
                var (i, script) = entry;
                var rawBytes = script.ToRawBytes();
                chunks[i] = rawBytes;
                lengths[i] = rawBytes.Length;
            });

            var footersChunk = Footers.ToBytes();
            lengths[_scripts.Length] = footersChunk.Length;
            chunks[_scripts.Length] = footersChunk;

            using (var ms = new MemoryStream())
            {
                var offset = 16 * (_scripts.Length + 1); // (offset, size, 0, 0) for each chunk
                foreach (var length in lengths)
                {
                    ms.Write(BitConverter.GetBytes(offset));
                    ms.Write(BitConverter.GetBytes(length));
                    ms.Write(BitConverter.GetBytes(0));
                    ms.Write(BitConverter.GetBytes(0));
                    offset += length;
                }

                foreach (var chunk in chunks)
                {
                    ms.Write(chunk);
                }
                return ms.ToArray();
            }
        }

        public void ReplaceStringTable(SnPackage refPackage, IDictionary<int, StringListModifier[]> modifierDict)
        {
            foreach (var (i, (script, refScript)) in Scripts.ZipTuple(refPackage.Scripts).WithIndex())
            {
                var refList = modifierDict.TryGetValue(i, out var modifiers)
                    ? refScript.GenerateStringReferenceList(modifiers)
                    : refScript.GenerateStringReferenceList();

                script.ReplaceStringTable(refList);
            }
        }

    }
}