using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CSYetiTools
{
    public sealed class SnPackage
    {
        private class FootersChunk
        {
            private CodeScriptFooter[] _footers;

            public CodeScriptFooter this[int index]
            {
                get => _footers[index];
            }

            public FootersChunk(byte[] source)
            {
                using var ms = new MemoryStream(source);
                using var reader = new BinaryReader(ms);
                var footers = new List<CodeScriptFooter>();
                while (true)
                {
                    var footer = CodeScriptFooter.ReadFrom(reader);
                    footers.Add(footer);
                    if (footer.Int1 == -1) break;
                }
                if (footers.Count < 2) {
                    throw new InvalidDataException($"Footer chunk length({footers.Count}) < 2");
                }
                
                var checkFooter = footers[^2];
                var accFooter = new CodeScriptFooter();
                for (int i = 0; i < footers.Count - 2; ++i)
                {
                    var f = footers[i];
                    accFooter.Int1 += f.Int1;
                    accFooter.Int2 += f.Int2;
                    accFooter.Int3 += f.Int3;
                    accFooter.ScriptIndex += f.ScriptIndex;
                }
                if (!accFooter.ToBytes().SequenceEqual(checkFooter.ToBytes()))
                {
                    throw new InvalidDataException($"Footer sum check failed: [{accFooter}] != [{checkFooter}]");
                }

                _footers = footers.ToArray();
            }

            public byte[] ToBytes()
            {
                using var ms = new MemoryStream();
                using (var writer = new BinaryWriter(ms))
                {
                    foreach (var footer in _footers)
                    {
                        footer.WriteTo(writer);
                    }
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


        #region Members

        private CodeScript[] _codeScripts;

        private FootersChunk _footers;

        #endregion

        private SnPackage(CodeScript[] scripts, FootersChunk footers)
        {
            _codeScripts = scripts;
            _footers = footers;
        }

        public SnPackage(string filename, bool isSteam)
        {
            using var stream = File.OpenRead(filename);
            Span<byte> header = stackalloc byte[4];
            stream.Read(header);
            var decodedSize = BitConverter.ToInt32(header);
            var bytes = LZSS.Decode(stream.StreamAsIEnumerable()).ToArray();

            if (decodedSize != bytes.Length)
            {
                throw new ArgumentException($"Invalid size (header = {decodedSize}, bytes = {bytes.Length}).");
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

                _footers = new FootersChunk(chunks.Last());

                var codeScripts = new List<CodeScript>();
                for (int i = 0; i < chunks.Count - 1; ++i)
                {
                    codeScripts.Add(new CodeScript(chunks[i], _footers[i], isSteam));
                }
                _codeScripts = codeScripts.ToArray();

            }
            catch (IndexOutOfRangeException)
            {
                throw new ArgumentException("Incomplete file");
            }
        }

        public static SnPackage CreateFrom(string dirPath, bool isSteam)
        {
            if (!Directory.Exists(dirPath)) throw new ArgumentException($"Directory {dirPath} not exists!");

            var footersPath = Path.Combine(dirPath, "footers");
            if (!File.Exists(footersPath)) throw new ArgumentException("File footers not exists!");
            var footers = new FootersChunk(File.ReadAllBytes(footersPath));

            var scripts = Directory.GetFiles(dirPath, "*.codescript")
                .OrderBy(o => o)
                .Select((file, i) => new CodeScript(File.ReadAllBytes(file), footers[i], isSteam))
                .ToArray();
            
            return new SnPackage(scripts, footers);
        }

        public void Dump(string dirPath, string postfix, bool isDumpBinary, bool isDumpScript)
        {
            if (!Directory.Exists(dirPath)) Directory.CreateDirectory(dirPath);

            if (isDumpBinary)
            {
                foreach (var (i, script) in _codeScripts.WithIndex())
                {
                    File.WriteAllBytes(Path.Combine(dirPath, $"chunk_{i:0000}_{postfix}.codescript"), script.GetRawBytes());
                }
                File.WriteAllBytes(Path.Combine(dirPath, $"footers_{postfix}"), _footers.ToBytes());
            }

            if (isDumpScript)
            {
                using var errorWriter = new StreamWriter(Path.Combine(dirPath, "parse_error.log"));
                Parallel.ForEach(_codeScripts.WithIndex(), entry =>
                {
                    var (i, script) = entry;
                    script.DumpText(Path.Combine(dirPath, $"chunk_{i:0000}_{postfix}.codescript-dump.txt"));
                    if (script.ParserError != null)
                    {
                        var errorInfo = $"Error parsing chunk_{i:0000}_{postfix}: \r\n" + script.ParserError + "\r\n-------------------------------------------------------\r\n";
                        errorWriter.WriteLine(errorInfo);
                    }
                });
            }

            using (var writer = new StreamWriter(Path.Combine(dirPath, $"footers_{postfix}.txt")))
            {
                _footers.Dump(writer);
            }
        }

        public void DumpTranslateSource(string dirPath)
        {
            if (!Directory.Exists(dirPath)) Directory.CreateDirectory(dirPath);

            var errors = new List<string>();

            var names = new HashSet<string>();

            foreach (var (i, script) in _codeScripts.WithIndex())
            {
                if (i == 0) continue; // skip first
                var contents = new JObject();
                string? currentName = null;

                foreach (var code in script.Codes.OfType<OpCodes.StringCode>())
                {
                    var index = $"{code.Index:000000}";
                    if (code is OpCodes.CharacterCode)
                    {
                        currentName = code.Content;
                        names.Add(code.Content);
                    }
                    else if (code is OpCodes.DialogCode)
                    {
                        var content = new
                        {
                            context = index,
                            code = $"0x{code.Code:X2}",
                            developer_comment = currentName ?? "",
                            @string = code.Content
                        };
                        currentName = null;
                        contents.Add(index, JObject.FromObject(content));
                    }
                    else
                    {
                        var content = new
                        {
                            context = index,
                            code = $"0x{code.Code:X2}",
                            @string = code.Content
                        };
                        contents.Add(index, JObject.FromObject(content));
                    }
                }

                using (var writer = new StreamWriter(Path.Combine(dirPath, $"chunk_{i:0000}.json"), false, new UTF8Encoding(/*encoderShouldEmitUTF8Identifier: */false)))
                {
                    writer.NewLine = "\n";
                    using var jsonWriter = new JsonTextWriter(writer);
                    jsonWriter.Formatting = Formatting.Indented;
                    contents.WriteTo(jsonWriter);
                }
                if (script.ParserError != null)
                {
                    errors.Add($"Error parsing chunk_{i:0000}: \r\n" + script.ParserError + "\r\n-------------------------------------------------------\r\n");
                }
            }

            using (var writer = new StreamWriter(Path.Combine(dirPath, "names.json"), false, new UTF8Encoding(/*encoderShouldEmitUTF8Identifier: */false)))
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
            var rawBytes = GetRawBytes();
            var bytes = LZSS.Encode(rawBytes).ToArray();
            stream.Write(BitConverter.GetBytes(rawBytes.Length));
            stream.Write(bytes);
            stream.Flush();
        }

        public void WriteTo(string path)
        {
            using (var stream = File.OpenWrite(path))
            {
                WriteTo(stream);
            }
        }

        public IReadOnlyList<CodeScript> Scripts
            => Array.AsReadOnly(_codeScripts);

        public byte[] GetRawBytes()
        {
            var lengths = new int[_codeScripts.Length + 1];
            var chunks = new byte[_codeScripts.Length + 1][];

            Parallel.ForEach(_codeScripts.WithIndex(), entry =>
            {
                var (i, script) = entry;
                var rawBytes = script.GetRawBytes();
                chunks[i] = rawBytes;
                lengths[i] = rawBytes.Length;
            });

            var footersChunk = _footers.ToBytes();
            lengths[_codeScripts.Length] = footersChunk.Length;
            chunks[_codeScripts.Length] = footersChunk;

            using (var ms = new MemoryStream())
            {
                var offset = 16 * (_codeScripts.Length + 1); // (offset, size, 0, 0) for each chunk
                foreach (var length in lengths)
                {
                    ms.Write(BitConverter.GetBytes(offset));
                    ms.Write(BitConverter.GetBytes(length));
                    ms.Write(BitConverter.GetBytes(0));
                    ms.Write(BitConverter.GetBytes(0));
                    offset += length;
                }

                foreach (var ch in chunks)
                {
                    ms.Write(ch);
                }
                return ms.ToArray();
            }
        }

    }
}