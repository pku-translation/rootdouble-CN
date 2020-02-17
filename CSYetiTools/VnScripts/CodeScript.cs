using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;

namespace CsYetiTools.VnScripts
{
    /********************************************************
    
        For steam version, all the strings is moved to the end of the script file before footer.
        This makes it possible to replace the strings without modify any offsets in the opcodes
        except which contain strings.
    
     */
    public sealed class CodeScript
    {
        private const byte FooterSeparator = 0x05;

        private readonly bool _isStringPooled;

        private byte[] _header = Array.Empty<byte>();

        private int _opCodeStart;

        private readonly List<OpCode> _codes = new List<OpCode>();

        private readonly SortedDictionary<int, OpCode> _codeTable = new SortedDictionary<int, OpCode>();

        private int _stringStart;

        private List<(int offset, string content)> _stringList = new List<(int, string)>();

        private int _index;

        public CodeScript(byte[] bytes, CodeScriptFooter footer, bool isStringPooled, bool allowError = false)
        {
            if (bytes.Length % 16 != 0) throw new InvalidDataException($"{nameof(bytes)} length is not times of 16");

            _isStringPooled = isStringPooled;
            _index = footer.ScriptIndex;

            // find footer
            int FindFooterStart()
            {
                var fBytes = footer.ToBytes();
                for (int i = 0; i < 16; ++i) // max fill 0x00 bytes
                {
                    if (bytes[(bytes.Length - i - 16)..(bytes.Length - i)].SequenceEqual(fBytes))
                        return bytes.Length - i - 16;
                }
                for (int i = 0; i < 16; ++i) // max fill 0x00 bytes
                {
                    Console.WriteLine($"compare [{Utils.BytesToHex(bytes[(bytes.Length - i - 16)..(bytes.Length - i)])}] with [{Utils.BytesToHex(fBytes)}]");
                }
                throw new InvalidDataException($"Cannot locate footer [{footer}] in script");
            }

            var footerStart = FindFooterStart();

            var codeEnd = footerStart - 1; // FooterSeparator

            using var reader = new BinaryReader(new MemoryStream(bytes),
                Encoding.Default,
                leaveOpen: false);

            _opCodeStart = reader.ReadInt32();
            _header = new byte[_opCodeStart - 4];
            reader.Read(_header);

            var stringTableEnd = codeEnd;

            try
            {
                while (reader.BaseStream.Position < codeEnd)
                {
                    var opCode = OpCode.GetNextCode(reader, _codes, isStringPooled);
                    if (isStringPooled && opCode is StringCode strCode)
                    {
                        // there are two {[55] ""} referring [0x00] at the end of header.
                        var refOffset = strCode.ContentOffset.AbsoluteOffset;
                        if (refOffset > strCode.Offset && codeEnd > refOffset)
                        {
                            codeEnd = refOffset;
                        }

                        var pos = reader.BaseStream.Position;
                        reader.BaseStream.Position = refOffset;
                        strCode.Content = Utils.ReadStringZ(reader);

                        if (refOffset > strCode.Offset && stringTableEnd < reader.BaseStream.Position)
                        {
                            Console.WriteLine($"strange code: ");
                            Console.Write($"    {strCode.Index}: {strCode}");
                            stringTableEnd = (int)reader.BaseStream.Position;
                        }

                        reader.BaseStream.Position = pos;
                    }
                    _codes.Add(opCode);
                }

                if (isStringPooled)
                {
                    _stringStart = (int)reader.BaseStream.Position;
                    if (_stringStart != codeEnd) throw new InvalidDataException($"string start ({_stringStart}) != code end ({codeEnd})");
                    if (stringTableEnd != footerStart - 1)
                    {
                        Console.WriteLine($"[{footer}]: {stringTableEnd} != {footerStart - 1}");
                    }
                    while (reader.BaseStream.Position < stringTableEnd)
                    {
                        var pos = (int)reader.BaseStream.Position;
                        var content = Utils.ReadStringZ(reader);
                        _stringList.Add((pos, content));
                    }
                }
                if (reader.BaseStream.Position < stringTableEnd)
                {
                    throw new InvalidDataException($"Read string table causes pos({reader.BaseStream.Position} != {stringTableEnd})");
                }

                var separator = reader.ReadByte();
                if (separator != FooterSeparator) throw new InvalidDataException($"Separator != {separator}");

                var footerBytes = CodeScriptFooter.ReadFrom(reader);
                if (!footerBytes.Equals(footer)) throw new InvalidDataException($"Footer [{footerBytes}] != [{footer}]");

                var fillBytes = reader.ReadBytes((int)(reader.BaseStream.Length - reader.BaseStream.Position));
                if (fillBytes.Length >= 16 || fillBytes.Any(b => b != 0x00))
                {
                    throw new InvalidDataException("Invalid fill bytes: " + Utils.BytesToTextLines(fillBytes));
                }
            }
            catch (Exception exc)
            {
                if (allowError)
                {
                    ParseError = allowError.ToString();
                    UnparsedBytes = reader.ReadBytes((int)(reader.BaseStream.Length - reader.BaseStream.Position));
                }
                else
                {
                    ExceptionDispatchInfo.Capture(exc).Throw();
                }
            }

            foreach (var code in _codes)
            {
                _codeTable.Add(code.Offset, code);
            }
            foreach (var code in _codes.OfType<IHasAddress>())
            {
                code.SetCodeIndices(_codeTable);
            }

            System.Diagnostics.Debug.Assert(ToRawBytes().SequenceEqual(bytes), "Rawbytes not sequence-equal to original.");
        }

        public string? ParseError;

        public byte[]? UnparsedBytes;

        public byte[] Header
            => _header.ToArray();

        public CodeScriptFooter Footer
        {
            get
            {
                int dialogCounter = 0;
                foreach (var code in Codes)
                {
                    if (code is DialogCode dialogCode && dialogCode.IsIndexed) ++dialogCounter;
                    else if (code is ExtraDialogCode exDialogCode && exDialogCode.IsDialog) ++dialogCounter;
                }
                return new CodeScriptFooter
                {
                    IndexedDialogCount = dialogCounter,
                    FlagCodeCount = GetCodes<SssFlagCode>().Count(),
                    ScriptIndex = _index,
                };
            }
        }

        public IEnumerable<OpCode> Codes
            => _codes.ToArray();

        public IEnumerable<T> GetCodes<T>() where T : OpCode
        {
            return Codes.OfType<T>();
        }

        public IEnumerable<T> GetCodes<T>(byte code) where T : OpCode
        {
            return Codes.Where(c => c.Code == code).OfType<T>();
        }

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
            return _codes.OfType<StringCode>()
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
            if (!_isStringPooled) throw new InvalidOperationException("Cannot replace string table of a non-steam version script");

            var strCodeList = _codes.OfType<StringCode>().ToList();
            if (strCodeList.Count == 0)
                return;

            if (strCodeList.Count != referenceList.Count)
                throw new ArgumentException($"string-list length {strCodeList.Count} != reference-list length {referenceList.Count}");

            string? prevString = null; // some adjacent string ([86] [85]) uses the same entry? (maybe debug)
            int? prevOffset = null;
            var currentOffset = _stringStart;
            var newStringList = new List<(int offset, string content)>();
            foreach (var (code, refEntry) in strCodeList.ZipTuple(referenceList))
            {
                if (code.Code != refEntry.Code)
                    throw new ArgumentException(
                        $"string-list code {code.Code:X02} != reference-list code {refEntry.Code}");
                code.Content = refEntry.Content;
                if (refEntry.Content != prevString)
                {
                    code.ContentOffset.AbsoluteOffset = currentOffset;
                    prevString = refEntry.Content;
                    prevOffset = currentOffset;
                    newStringList.Add((currentOffset, prevString));
                    currentOffset += Utils.GetStringZByteCount(prevString);
                }
                else
                {
                    code.ContentOffset.AbsoluteOffset = prevOffset!.Value;
                }
            }
            _stringList = newStringList;

#if DEBUG
            var bytes = ToRawBytes();
            var script = new CodeScript(bytes, Footer, isStringPooled: true);
            if (!script.ToRawBytes().SequenceEqual(bytes))
            {
                throw new ArgumentException("error");
            }
#endif
        }

        public byte[] ToRawBytes()
        {
            using var ms = new MemoryStream();
            using (var writer = new BinaryWriter(ms))
            {
                WriteTo(writer);
            }

            System.Diagnostics.Debug.Assert(ms.Length % 16 == 0);
            return ms.ToArray();
        }

        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(_opCodeStart);
            writer.Write(_header);
            foreach (var code in _codes)
            {
                code.WriteTo(writer);
            }
            if (_isStringPooled)
            {
                foreach (var (_, content) in _stringList)
                {
                    Utils.WriteStringZ(writer, content);
                }
            }
            writer.Write(FooterSeparator);
            writer.Write(Footer.ToBytes());
            var alignFillCount = 16 - writer.BaseStream.Position % 16;
            if (alignFillCount != 16)
            {
                for (var i = 0; i < alignFillCount; ++i)
                {
                    writer.Write((byte)0x00);
                }
            }
        }

        public void DumpText(TextWriter writer, bool header = true, bool footer = true)
        {
            if (header)
            {
                writer.WriteLine("* * * Header * * *");
                Utils.BytesToTextLines(_header, 4).ForEach(writer.WriteLine);
                writer.WriteLine();
            }

            writer.WriteLine("* * * Scripts * * *");

            foreach (var (i, code) in _codes.WithIndex())
            {
                writer.WriteLine($"{i,4} | 0x{code.Offset:X08}: ");
                code.Dump(writer);
            }
            writer.WriteLine();


            if (footer)
            {
                writer.WriteLine("* * * Footer * * *");
                writer.WriteLine(Footer);
            }
        }

        public void DumpText(string path, Encoding? encoding = null, bool header = true, bool footer = true)
        {
            if (encoding == null) encoding = new UTF8Encoding(/*encoderShouldEmitUTF8Identifier: */false);
            using var writer = new StreamWriter(path, false, encoding);
            DumpText(writer, header, footer);
        }
    }
}