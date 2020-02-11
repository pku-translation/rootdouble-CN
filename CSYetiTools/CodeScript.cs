using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace CSYetiTools
{
    /********************************************************
    
        For steam version, all the strings is moved to the end of the script file before footer.
        This makes it possible to replace the strings without modify any offsets in the opcodes
        except which contain strings.
    
     */
    public sealed class CodeScript
    {
        private readonly bool _isSteam;
        private byte[] _header = Array.Empty<byte>();

        private int _opCodeStart;

        private readonly List<OpCode> _codes = new List<OpCode>();

        private int _stringStart;

        private List<(int offset, string content)> _stringList = new List<(int, string)>();

        private byte[] _footer = Array.Empty<byte>();

        public CodeScript(byte[] bytes, bool isSteam)
        {
            //_rawBytes = bytes.ToArray();
            _isSteam = isSteam;

            var errorBuilder = new StringBuilder();

            using (var reader = new BinaryReader(new MemoryStream(bytes),
                Encoding.Default,
                leaveOpen: false))
            {
                if (isSteam)
                    ParseSteam(reader, errorBuilder);
                else
                    Parse(reader, errorBuilder);
            }
            if (errorBuilder.Length != 0) ParserError = errorBuilder.ToString();

#if DEBUG
            var rawBytes = GetRawBytes();
            if (!rawBytes.SequenceEqual(bytes))
            {
                throw new InvalidOperationException("Rawbytes not sequence-equal to original.");
            }
#endif
        }

        private void Parse(BinaryReader reader, StringBuilder errorBuilder)
        {
            _opCodeStart = reader.ReadInt32();
            HeaderStart = 4;
            _header = new byte[_opCodeStart - 4];
            reader.Read(_header);

            try
            {
                while (reader.BaseStream.Position < reader.BaseStream.Length)
                {
                    try
                    {
                        var opCode = OpCode.GetNextCode(reader, _codes, isSteam: false);
                        _codes.Add(opCode);
                        //if (opCode.Code == OpCode.EndBlock && _codes.Count > 0 && _codes.Last().Code == OpCode.EndBlock)
                        if (opCode.Code == OpCode.EndBlock && reader.BaseStream.Length - reader.BaseStream.Position < 40)
                        {
                            //reader.BaseStream.Seek(-1, SeekOrigin.Current);
                            break;
                        }
                    }
                    catch (ZeroCodeException)
                    {
                        if (reader.BaseStream.Length - reader.BaseStream.Position >= 40) throw;
                        reader.BaseStream.Seek(1, SeekOrigin.Current);
                        _codes.Add(new OpCodes.ZeroCode());
                        break;
                    }
                }
            }
            catch (OpCodeParseException exc)
            {
                errorBuilder.AppendLine(exc.Message);
                if (!string.IsNullOrWhiteSpace(exc.ScriptContext))
                {
                    errorBuilder.AppendLine("------ context --------");
                    errorBuilder.AppendLine(exc.ScriptContext);
                    errorBuilder.AppendLine("-----------------------");
                }
                if (exc.InnerException != null)
                {
                    errorBuilder.AppendLine(exc.InnerException.ToString());
                }
            }

            foreach (var code in _codes)
            {
                if (code is OpCodes.StringCode strCode)
                {
                    _stringList.Add((offset: 0, content: strCode.Content));
                }
            }

            FooterStart = (int)reader.BaseStream.Position;
            _footer = reader.ReadBytes((int)(reader.BaseStream.Length - reader.BaseStream.Position));
        }

        private void ParseSteam(BinaryReader reader, StringBuilder errorBuilder)
        {
            _opCodeStart = reader.ReadInt32();
            HeaderStart = 4;
            _header = new byte[_opCodeStart - 4];
            reader.Read(_header);

            var stringTableStart = (int)reader.BaseStream.Length;
            var stringTableEnd = 0;
            try
            {
                while (reader.BaseStream.Position < stringTableStart)
                {
                    try
                    {
                        var opCode = OpCode.GetNextCode(reader, _codes, isSteam: true);
                        if (opCode is OpCodes.StringCode strCode)
                        {
                            if (strCode.ContentOffset > strCode.Offset && stringTableStart > strCode.ContentOffset) stringTableStart = strCode.ContentOffset;

                            var pos = reader.BaseStream.Position;
                            reader.BaseStream.Position = strCode.ContentOffset;
                            strCode.Content = Utils.ReadStringZ(reader);

#if DEBUG
                            if (strCode.ContentOffset < strCode.Offset && strCode.Content.Length != 0)
                            {
                                throw new ArgumentException("Test if all empty referenced string are pointed to 0x00 in header: failed!");
                            }
#endif

                            if (strCode.ContentOffset > strCode.Offset && stringTableEnd < reader.BaseStream.Position) stringTableEnd = (int)reader.BaseStream.Position;

                            reader.BaseStream.Position = pos;
                        }
                        _codes.Add(opCode);
                        //if (opCode.Code == OpCode.EndBlock && _codes.Count > 0 && _codes.Last().Code == OpCode.EndBlock)
                        if (opCode.Code == OpCode.EndBlock
                            && (stringTableStart - reader.BaseStream.Position < 4 || reader.BaseStream.Length - reader.BaseStream.Position < 40))
                        {
                            //reader.BaseStream.Seek(-1, SeekOrigin.Current);
                            break;
                        }
                    }
                    catch (ZeroCodeException)
                    {
                        if (stringTableStart - reader.BaseStream.Position < 4 || reader.BaseStream.Length - reader.BaseStream.Position < 40)
                        {
                            reader.BaseStream.Seek(1, SeekOrigin.Current);
                            _codes.Add(new OpCodes.ZeroCode());
                            break;
                        }
                        throw;
                    }
                }
            }
            catch (OpCodeParseException exc)
            {
                errorBuilder.AppendLine(exc.Message);
                if (!string.IsNullOrWhiteSpace(exc.ScriptContext))
                {
                    errorBuilder.AppendLine("------ context --------");
                    errorBuilder.AppendLine(exc.ScriptContext);
                    errorBuilder.AppendLine("-----------------------");
                }
                if (exc.InnerException != null)
                {
                    errorBuilder.AppendLine(exc.InnerException.ToString());
                }
                FooterStart = (int)reader.BaseStream.Position;
                _footer = reader.ReadBytes((int)(reader.BaseStream.Length - reader.BaseStream.Position));
                return;
            }

            _stringStart = (int)reader.BaseStream.Position;
            while (reader.BaseStream.Position < stringTableEnd)
            {
                var pos = (int)reader.BaseStream.Position;
                var content = Utils.ReadStringZ(reader);
                _stringList.Add((pos, content));
            }

#if DEBUG
            if (stringTableEnd > reader.BaseStream.Position)
            {
                int pos = (int)reader.BaseStream.Position;
                Console.WriteLine($"Unknown data in string table: {stringTableEnd - pos}");
                Utils.BytesToTextLines(reader.ReadBytes(stringTableEnd - pos), pos).ForEach(Console.WriteLine);
                reader.BaseStream.Position = pos;

                //reader.BaseStream.Position = stringTableEnd;
            }
#endif
            FooterStart = (int)reader.BaseStream.Position;
            _footer = reader.ReadBytes((int)(reader.BaseStream.Length - reader.BaseStream.Position));
        }

        public string? ParserError { get; }

        public byte[] Header
            => _header.ToArray();

        public int HeaderStart { get; private set; }

        public byte[] Footer
            => _footer.ToArray();

        public int FooterStart { get; private set; }

        public OpCode[] Codes
            => _codes.ToArray();

        public IReadOnlyList<(int offset, string content)> StringList
            => _stringList.AsReadOnly();

        public class StringReferenceEntry
        {
            public StringReferenceEntry(int index, byte code, int offset, string content)
            {
                Index = index;
                Code = code;
                Offset = offset;
                Content = content;
            }
            public int Index { get; set; }
            public byte Code { get; set; }
            public int Offset { get; set; }
            public string Content { get; set; }
        }

        public List<StringReferenceEntry> GenerateStringReferenceList()
        {
            return _codes.OfType<OpCodes.StringCode>()
                .Select(code => new StringReferenceEntry(code.Index, code.Code, code.Offset, code.Content))
                .ToList();
        }

        public List<StringReferenceEntry> GenerateStringReferenceList(StringListModifier? modifier)
        {
            var list = GenerateStringReferenceList();
            if (modifier != null)
            {
                var dict = list.ToDictionary(entry => entry.Index);
                modifier.Modify(dict);

                list = dict.Values.OrderBy(entry => entry.Index).ToList();
            }
            return list;
        }

        public void ReplaceStringTable(IReadOnlyList<StringReferenceEntry> referenceList)
        {
            if (!_isSteam) throw new InvalidOperationException("Cannot replace string table of a non-steam version script");

            var strCodeList = _codes.OfType<OpCodes.StringCode>().ToList();
            if (strCodeList.Count == 0)
                return;

            if (strCodeList.Count != referenceList.Count)
                throw new ArgumentException($"string-list length {strCodeList.Count} != reference-list length {referenceList.Count}");

            string? prevString = null; // some adjacent string ([86] [85]) uses the same entry? (maybe debug)
            var currentOffset = _stringStart;
            var newStringList = new List<(int offset, string content)>();
            foreach (var (code, refEntry) in strCodeList.ZipTuple(referenceList))
            {
                if (code.Code != refEntry.Code)
                    throw new ArgumentException(
                        $"string-list code {code.Code:X02} != reference-list code {refEntry.Code}");
                code.Content = refEntry.Content;
                code.ContentOffset = currentOffset;
                if (refEntry.Content != prevString)
                {
                    prevString = refEntry.Content;
                    newStringList.Add((currentOffset, prevString));
                    currentOffset += Utils.GetStringZByteCount(prevString);
                }
            }
            _stringList = newStringList;
            FooterStart = currentOffset;
            if ((FooterStart + _footer.Length) % 16 != 0)
            {
                var extraBytes = Enumerable.Repeat<byte>(0x00, 16 - ((FooterStart + _footer.Length) % 16));
                _footer = _footer.Concat(extraBytes).ToArray();
            }

#if DEBUG
            var bytes = GetRawBytes();
            var script = new CodeScript(bytes, isSteam: true);
            if (!script.GetRawBytes().SequenceEqual(bytes))
            {
                throw new ArgumentException("error");
            }
#endif
        }

        public byte[] GetRawBytes()
        {
            byte[] result;
            if (_isSteam)
            {
                result = BitConverter.GetBytes(_opCodeStart)
                    .Concat(_header)
                    .Concat(_codes.SelectMany(code => code.ToBytes()))
                    .Concat(_stringList.SelectMany(entry => (Utils.GetStringZBytes(entry.content))))
                    .Concat(_footer)
                    .ToArray();
            }
            else
            {
                result = BitConverter.GetBytes(_opCodeStart)
                    .Concat(_header)
                    .Concat(_codes.SelectMany(code => code.ToBytes()))
                    .Concat(_footer)
                    .ToArray();
            }
#if DEBUG
            if (result.Length % 16 != 0)
            {
                Console.WriteLine($"CodeScript size {result.Length} % 16 != 0");
            }
#endif
            return result;
        }

        /// <summary>
        /// Dump all content text.
        /// </summary>
        /// <remarks>
        /// {s}
        /// </remarks>
        /// <param name="writer"></param>
        /// <param name="codeFormat"></param>
        public void DumpText(TextWriter writer, string codeFormat = "{index,4} | 0x{offset:X08}: {code}")
        {
            writer.WriteLine("* * * Header * * *");
            Utils.BytesToTextLines(_header, HeaderStart).ForEach(writer.WriteLine);
            writer.WriteLine();

            writer.WriteLine("* * * Scripts * * *");

            var formatter = new RuntimeFormatter(codeFormat);

            foreach (var (i, code) in _codes.WithIndex())
            {
                
                writer.WriteLine(formatter.Format(key => key switch {
                    "index" => i,
                    "offset" => code.Offset,
                    "code" => code,
                    _ => throw new ArgumentException($"Invalid key {key} for code dump"),
                }));
                //writer.WriteLine($"{i,4} | 0x{code.Offset:X08}: {code}");
                if (code.Code == OpCode.EndBlock)
                {
                    writer.WriteLine("-----------------------------------------------------");
                }
            }
            writer.WriteLine();

            writer.WriteLine("* * * Footer * * *");
            Utils.BytesToTextLines(_footer, FooterStart).ForEach(writer.WriteLine);
            writer.WriteLine();
        }

        public void DumpText(string path, string codeFormat = "{index,4} | 0x{offset:X08}: {code}", Encoding? encoding = null)
        {
            if (encoding == null) encoding = new UTF8Encoding(/*encoderShouldEmitUTF8Identifier: */false);
            using var writer = new StreamWriter(path, false, encoding);
            DumpText(writer, codeFormat);
        }
    }
}