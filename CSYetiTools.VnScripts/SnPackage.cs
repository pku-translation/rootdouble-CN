using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSYetiTools.Base;
using CSYetiTools.Base.IO;
using CSYetiTools.VnScripts.Transifex;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CSYetiTools.VnScripts
{
    public sealed class SnPackage
    {
        public class FootersChunk
        {
            private readonly ScriptFooter[] _footers;

            public ScriptFooter this[int index]
                => _footers[index];

            public FootersChunk(byte[] source)
            {
                using var reader = new BinaryStream(source);
                var footers = new List<ScriptFooter>();
                while (true) {
                    var footer = ScriptFooter.ReadFrom(reader);
                    footers.Add(footer);
                    if (footer.IndexedDialogCount == -1) break;
                }
                if (footers.Count < 2) {
                    throw new InvalidDataException($"Footer chunk length({footers.Count}) < 2");
                }

                var checkFooter = footers[^2];
                var accFooter = footers.SkipLast(2).Aggregate(ScriptFooter.Zero, (current, footer) => current + footer);
                if (!accFooter.Equals(checkFooter)) {
                    throw new InvalidDataException($"Footer sum check failed: [{accFooter}] != [{checkFooter}]");
                }

                _footers = footers.ToArray();
            }

            public FootersChunk(IEnumerable<ScriptFooter> footers)
            {
                var list = footers.ToList();
                var accFooter = list.Aggregate(ScriptFooter.Zero, (current, footer) => current + footer);
                list.Add(accFooter);
                list.Add(ScriptFooter.End);
                _footers = list.ToArray();
            }

            public byte[] ToBytes()
            {
                using var writer = new BinaryStream();
                foreach (var footer in _footers) {
                    footer.WriteTo(writer);
                }
                return writer.ToBytes();
            }

            public void Dump(TextWriter writer)
            {
                foreach (var footer in _footers) {
                    writer.WriteLine(footer);
                }
            }
        }

        private readonly Script[] _scripts;

        public FootersChunk Footers
            => new(_scripts.Select(s => s.Footer));

        private SnPackage(Script[] scripts)
        {
            _scripts = scripts;
        }

        public SnPackage(string filename, bool isStringPooled, Encoding? encoding = null)
            : this(File.ReadAllBytes(filename), isStringPooled, encoding)
        { }

        public SnPackage(byte[] data, bool isStringPooled, Encoding? encoding = null)
        {
            var decodedSize = BitConverter.ToInt32(data);
            var bytes = LZSS.Decode(data.Skip(4)).ToArray();

            if (decodedSize != bytes.Length) {
                throw new InvalidDataException($"Invalid size (header = {decodedSize}, bytes = {bytes.Length}).");
            }
            try {
                var maxOffset = BitConverter.ToInt32(bytes, 0);
                var chunks = new List<byte[]>();

                for (var offset = 0; offset < maxOffset; offset += 16) {
                    var chunkOffset = BitConverter.ToInt32(bytes, offset);
                    var chunkSize = BitConverter.ToInt32(bytes, offset + 4);

                    var chunk = new byte[chunkSize];
                    Array.Copy(bytes, chunkOffset, chunk, 0, chunkSize);
                    chunks.Add(chunk);
                }

                var footers = new FootersChunk(chunks.Last());

                _scripts = new Script[chunks.Count - 1];

                Parallel.For(0, _scripts.Length, i => {
                    _scripts[i] = Script.ParseBytes(chunks[i], footers[i], isStringPooled, encoding);
                });
            }
            catch (IndexOutOfRangeException) {
                throw new ArgumentException("Incomplete file");
            }
        }

        public static SnPackage CreateFrom(FilePath dirPath, bool isStringPooled)
        {
            if (!Directory.Exists(dirPath)) throw new ArgumentException($"Directory {dirPath} not exists!");

            var footersPath = dirPath / "footers";
            if (!File.Exists(footersPath)) throw new ArgumentException("File footers not exists!");
            var footers = new FootersChunk(File.ReadAllBytes(footersPath));

            var scripts = Directory.GetFiles(dirPath, "*.script")
                .OrderBy(o => o)
                .Select((file, i) => Script.ParseBytes(File.ReadAllBytes(file), footers[i], isStringPooled))
                .ToArray();

            return new SnPackage(scripts);
        }

        public void Dump(FilePath dirPath, bool isDumpBinary, bool isDumpScript)
        {
            Utils.CreateOrClearDirectory(dirPath);

            if (isDumpBinary) {
                Parallel.ForEach(_scripts.WithIndex(), entry => {
                    var (i, script) = entry;
                    var path = dirPath / $"chunk_{i:0000}.script";
                    script.WriteToFile(path);
                });
                File.WriteAllBytes(dirPath / "footers", Footers.ToBytes());
            }

            if (isDumpScript) {
                var errors = new List<string>();
                Parallel.ForEach(_scripts.WithIndex(), entry => {
                    var (i, script) = entry;
                    script.DumpText(dirPath / $"chunk_{i:0000}.script-dump.txt");
                    if (script.ParseError != null) {
                        var errorInfo = $"Error parsing chunk_{i:0000}: \r\n" + script.ParseError + "\r\n-------------------------------------------------------\r\n";
                        lock (errors) errors.Add(errorInfo);
                    }
                });
                if (errors.Count > 0) {
                    Utils.WriteAllLines(dirPath / "parse_error.log", errors);
                }
            }

            using var writer = Utils.CreateStreamWriter(dirPath / $"footers.txt");
            Footers.Dump(writer);
        }

        public void WriteTo(Stream stream, Encoding? encoding = null)
        {
            var rawBytes = ToRawBytes(encoding);
            var bytes = LZSS.Encode(rawBytes).ToArray();

            stream.Write(BitConverter.GetBytes(rawBytes.Length));
            stream.Write(bytes);
            stream.Flush();
        }

        public void WriteTo(string path, Encoding? encoding = null)
        {
            using var stream = File.Create(path);
            WriteTo(stream, encoding);
        }

        public void WriteChunk(string path, int chunkIndex, Encoding? encoding = null)
        {
            Scripts[chunkIndex].WriteToFile(path, encoding);
        }

        public IReadOnlyList<Script> Scripts
            => Array.AsReadOnly(_scripts);

        public byte[] ToRawBytes(Encoding? encoding = null)
        {
            var lengths = new int[_scripts.Length + 1];
            var chunks = new byte[_scripts.Length + 1][];

            Parallel.ForEach(_scripts.WithIndex(), entry => {
                var (i, script) = entry;
                var rawBytes = script.ToRawBytes(encoding);
                chunks[i] = rawBytes;
                lengths[i] = rawBytes.Length;
            });

            var footersChunk = Footers.ToBytes();
            lengths[_scripts.Length] = footersChunk.Length;
            chunks[_scripts.Length] = footersChunk;

            using var writer = new BinaryStream();
            var offset = 16 * (_scripts.Length + 1); // (offset, size, 0, 0) for each chunk
            foreach (var length in lengths) {
                writer.WriteLE(offset);
                writer.WriteLE(length);
                writer.WriteLE(0);
                writer.WriteLE(0);
                offset += length;
            }

            foreach (var chunk in chunks) {
                writer.Write(chunk);
            }
            return writer.ToBytes();
        }

        public void ReplaceStringTable(SnPackage refPackage, IDictionary<int, StringListModifier[]> modifierDict)
        {
            foreach (var (i, (script, refScript)) in Scripts.Zip(refPackage.Scripts).WithIndex()) {
                var refList = modifierDict.TryGetValue(i, out var modifiers)
                    ? refScript.GenerateStringReferenceList(modifiers)
                    : refScript.GenerateStringReferenceList();

                script.ReplaceStringTable(refList);
            }
        }

        public void DumpTranslateSource(FilePath dirPath)
        {
            Utils.CreateOrClearDirectory(dirPath);

            var errors = new List<string>();

            var names = new HashSet<string>();

            foreach (var (i, script) in _scripts.WithIndex()) {
                if (script.Footer.ScriptIndex < 0) continue; // skip non-text scripts

                names.UnionWith(script.GetCharacterNames());

                Utils.SerializeJsonToFile(script.GetTranslateSources(), dirPath / $"chunk_{i:0000}.json");

                if (script.ParseError != null) {
                    errors.Add($"Error parsing chunk_{i:0000}: \r\n" + script.ParseError + "\r\n-------------------------------------------------------\r\n");
                }
            }

            using (var writer = Utils.CreateStreamWriter(dirPath / "names.json")) {
                using (var jsonWriter = new JsonTextWriter(writer)) {
                    jsonWriter.Formatting = Formatting.Indented;
                    var namesObj = new JObject();
                    foreach (var (i, name) in names.WithIndex()) {
                        namesObj.Add($"{i:000}", JObject.FromObject(new { @string = name }));
                    }
                    namesObj.WriteTo(jsonWriter);
                }
            }

            if (errors.Count > 0) {
                using var errorWriter = Utils.CreateStreamWriter(dirPath / "parse_error.log");
                foreach (var error in errors) {
                    errorWriter.WriteLine(error);
                }
            }
        }

        public void ApplyTranslations(FilePath sourceDir, FilePath translationDir,
            bool debugChunkNum = false, bool debugSource = false)
        {
            var sourceNamesPath = sourceDir / "names.json";
            var sourceNameDict = Utils.DeserializeJsonFromFile<SortedDictionary<string, JObject>>(sourceNamesPath);
            var transNamesPath = translationDir / "names.json";
            var transNameDict = File.Exists(transNamesPath)
                ? Utils.DeserializeJsonFromFile<SortedDictionary<string, JObject>>(transNamesPath)
                : new SortedDictionary<string, JObject>();

            var nameTable = new Dictionary<string, string>();
            foreach (var (k, src) in sourceNameDict) {
                var srcName = (string)src["string"]!;
                if (transNameDict.TryGetValue(k, out var trans)) {
                    nameTable.Add(srcName, (string)trans["string"]!);
                }
                else {
                    nameTable.Add(srcName, srcName);
                }
            }

            var translationTables = Utils.Range(_scripts.Length).Select(i => new Dictionary<int, string>()).ToArray();

            foreach (var (i, script) in _scripts.WithIndex()) {
                if (script.Footer.ScriptIndex < 0) continue; // skip non-text scripts

                var translationPath = translationDir / $"chunk_{i:0000}.json";
                if (File.Exists(translationPath)) {
                    try {
                        var translationTable = translationTables[i];
                        var translations = Utils.DeserializeJsonFromFile<SortedDictionary<string, TranslationInfo>>(translationPath);
                        foreach (var (k, v) in translations) {
                            var index = int.Parse(k);
                            var translation = v.String;
                            var trimmed = translation.Trim();
                            if (trimmed == "@ignore") continue;
                            if (trimmed.StartsWith("@import") || trimmed.StartsWith("@auto-import")) {
                                var importingInfo = trimmed.StartsWith("@import") ? "importing" : "auto-importing";
                                var segs = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                                var targetChunk = int.Parse(segs[1]);
                                var targetIndex = int.Parse(segs[2]);
                                if (targetChunk < 0 || targetChunk >= translationTables.Length) {
                                    Utils.PrintError($"[{i:0000}:{k}] {trimmed}, target chunk index out of range");
                                }
                                else if (translationTables[targetChunk].TryGetValue(targetIndex, out var targetContent)) {
                                    translationTable.Add(index, targetContent);
                                    continue;
                                }
                                else if (targetChunk == i && targetIndex == index) {
                                    Utils.PrintError($"[{i:0000}:{k}] {importingInfo} self");
                                }
                                else if (targetChunk > i || (targetChunk == i && targetIndex > index)) {
                                    Utils.PrintError($"[{i:0000}:{k}] forward {importingInfo}");
                                }
                                else {
                                    Utils.PrintError($"[{i:0000}:{k}] {importingInfo} unknown source {targetChunk:0000}:{targetIndex:000000}");
                                }
                            }
                            translationTable.Add(index, translation);
                        }
                    }
                    catch (Exception exc) {
                        throw new InvalidDataException($"Invalid translation data for chunk_{i:0000}", exc);
                    }
                }
            }

            foreach (var (i, script) in _scripts.WithIndex()) {
                try {
                    script.ApplyTranslations(translationTables[i], nameTable,
                        debugChunkNum ? $"[{i:0000}] ": null, debugSource);
                }
                catch (Exception exc) {
                    throw new InvalidDataException($"Error translating chunk_{i:0000}", exc);
                }
            }
        }

        public IEnumerable<char> EnumerateChars()
        {
            return _scripts.SelectMany(script => script.EnumerateChars());
        }
    }
}
