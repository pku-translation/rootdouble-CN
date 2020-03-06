using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using CsYetiTools.IO;

namespace CsYetiTools.VnScripts
{
    /********************************************************
    
        For steam version, all the strings is moved to the end of the script file before footer.
        This makes it possible to replace the strings without modify any offsets in the opcodes
        except which contain strings.
    
     */
    public sealed class Script
    {
        private const byte FooterSeparator = 0x05;

        private readonly bool _isStringPooled;

        private ScriptHeader _header;

        private readonly List<OpCode> _codes = new List<OpCode>();

        private readonly SortedDictionary<int, string> _labelForward = new SortedDictionary<int, string>();

        private readonly Dictionary<string, int> _labelBackward = new Dictionary<string, int>();

        private int _index;

        public Script(byte[] bytes, ScriptFooter footer, bool isStringPooled, Encoding? encoding = null, bool allowError = false)
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
            var stringTableEnd = codeEnd;

            using var stream = new BinaryStream(bytes, encoding ?? Utils.Cp932);

            // header
            var maybeEntries = new List<CodeAddressData>();
            var firstCodeOffset = stream.ReadInt32LE();
            int currentOffset = 0;
            maybeEntries.Add(new CodeAddressData(currentOffset, firstCodeOffset));
            while (stream.Position <= firstCodeOffset - 4)
            {
                var offset = stream.ReadInt32LE();
                currentOffset += 4;
                if (offset < stream.Position)
                {
                    stream.Seek(-4);
                    break;
                }
                if (offset < firstCodeOffset)
                {
                    firstCodeOffset = offset;
                }
                maybeEntries.Add(new CodeAddressData(currentOffset, offset));
            }
            var maybeRemainBytes = stream.ReadBytesExact(firstCodeOffset - (int)(stream.Position));

            // codes
            try
            {
                while (stream.Position < codeEnd)
                {
                    var opCode = OpCode.GetNextCode(stream, _codes, isStringPooled);
                    if (isStringPooled && opCode is StringCode strCode)
                    {
                        var refOffset = strCode.ContentOffset.AbsoluteOffset;
                        if (refOffset > strCode.Offset && codeEnd > refOffset)
                        {
                            codeEnd = refOffset;
                        }

                        var pos = stream.Position;
                        stream.Position = refOffset;
                        strCode.Content = stream.ReadStringZ();

                        if (refOffset > strCode.Offset && stringTableEnd < stream.Position)
                        {
                            Console.WriteLine($"strange code: ");
                            Console.Write($"    {strCode.Index}: {strCode}");
                            stringTableEnd = (int)stream.Position;
                        }

                        stream.Position = pos;
                    }
                    _codes.Add(opCode);
                }

                if (isStringPooled)
                {
                    var stringStart = (int)stream.Position;
                    if (stringStart != codeEnd) throw new InvalidDataException($"string start ({stringStart}) != code end ({codeEnd})");
                    if (stringTableEnd != footerStart - 1)
                    {
                        Console.WriteLine($"[{footer}]: {stringTableEnd} != {footerStart - 1}");
                    }
                    while (stream.Position < stringTableEnd)
                    {
                        var pos = (int)stream.Position;
                        var content = stream.ReadStringZ();
                    }
                }
                if (stream.Position < stringTableEnd)
                {
                    throw new InvalidDataException($"Read string table causes pos({stream.Position} != {stringTableEnd})");
                }

                var separator = stream.ReadByte();
                if (separator != FooterSeparator) throw new InvalidDataException($"Separator != {separator}");

                var footerBytes = ScriptFooter.ReadFrom(stream);
                if (!footerBytes.Equals(footer)) throw new InvalidDataException($"Footer [{footerBytes}] != [{footer}]");

                var fillBytes = stream.ReadToEnd();
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
                    UnparsedBytes = stream.ReadToEnd();
                }
                else
                {
                    ExceptionDispatchInfo.Capture(exc).Throw();
                }
            }

            var codeDict = new SortedDictionary<int, OpCode>();
            foreach (var code in _codes)
            {
                codeDict.Add(code.Offset, code);
            }
            foreach (var code in _codes)
            {
                if (code is IHasAddress addressCode)
                {
                    foreach (var address in addressCode.GetAddresses())
                    {
                        if (codeDict.TryGetValue(address.AbsoluteOffset, out var targetCode))
                        {
                            address.TargetCodeIndex = targetCode.Index;
                            address.TargetCodeRelativeIndex = targetCode.Index - code.Index;
                        }
                    }
                }
            }

            // header
            var entryCount = maybeEntries.FindIndex(e => !codeDict.ContainsKey(e.AbsoluteOffset));

            if (entryCount >= 0)
            {
                using var ms = new MemoryStream();
                using var headerRemainWriter = new BinaryWriter(ms);
                for (int i = entryCount; i < maybeEntries.Count; ++i)
                {
                    headerRemainWriter.Write(maybeEntries[i].AbsoluteOffset);
                }
                headerRemainWriter.Write(maybeRemainBytes);
                _header = new ScriptHeader(maybeEntries.Take(entryCount), ms.ToArray());
            }
            else
            {
                _header = new ScriptHeader(maybeEntries, maybeRemainBytes);
            }

            // labels
            var entryIndex = 0;
            foreach (var entry in _header.Entries)
            {
                if (_labelForward.TryGetValue(entry.AbsoluteOffset, out var entryName))
                {
                    entry.TargetLabel = entryName;
                }
                else
                {
                    var entryLabelName = $"Entry-{entryIndex++:00}";
                    entry.TargetLabel = entryLabelName;
                    _labelForward.Add(entry.AbsoluteOffset, entryLabelName);
                    _labelBackward.Add(entryLabelName, entry.AbsoluteOffset);
                }
            }
            foreach (var address in _codes.OfType<IHasAddress>().SelectMany(c => c.GetAddresses()))
            {
                if (_labelForward.TryGetValue(address.AbsoluteOffset, out var label))
                {
                    address.TargetLabel = label;
                }
                else
                {
                    _labelForward.Add(address.AbsoluteOffset, "");
                }
            }
            var labelOffsets = _labelForward.Keys.ToList();
            if (labelOffsets.Count >= 10000) throw new InvalidDataException($"Too many labels: {labelOffsets.Count}");
            var labelIndex = 1;
            foreach (var offset in labelOffsets)
            {
                if (string.IsNullOrWhiteSpace(_labelForward[offset]))
                {
                    var labelName = $"Label-{labelIndex++:0000}";
                    _labelForward[offset] = labelName;
                    _labelBackward.Add(labelName, offset);
                }
            }
            foreach (var address in _codes.OfType<IHasAddress>().SelectMany(c => c.GetAddresses()))
            {
                address.TargetLabel = _labelForward[address.AbsoluteOffset];
            }

            System.Diagnostics.Debug.Assert(ToRawBytes().SequenceEqual(bytes), "Rawbytes not sequence-equal to original.");
        }

        public string? ParseError;

        public byte[]? UnparsedBytes;

        public ScriptHeader Header
            => _header;

        public ScriptFooter Footer
        {
            get
            {
                int dialogCounter = 0;
                foreach (var code in Codes)
                {
                    if (code is DialogCode dialogCode && dialogCode.IsIndexed) ++dialogCounter;
                    else if (code is ExtraDialogCode exDialogCode && exDialogCode.IsDialog) ++dialogCounter;
                }
                return new ScriptFooter
                {
                    IndexedDialogCount = dialogCounter,
                    FlagCodeCount = GetCodes<SssFlagCode>().Count(),
                    ScriptIndex = _index,
                };
            }
        }

        public IEnumerable<OpCode> Codes
            => _codes.ToArray();

        public OpCode GetCodeAt(int index)
            => _codes[index];

        public IEnumerable<T> GetCodes<T>() where T : OpCode
        {
            return Codes.OfType<T>();
        }

        public IEnumerable<T> GetCodes<T>(byte code) where T : OpCode
        {
            return Codes.Where(c => c.Code == code).OfType<T>();
        }

        public class StringReferenceEntry
        {
            public StringReferenceEntry(int index, byte code, string content)
            {
                Index = index;
                Code = code;
                Content = content;
            }
            public int Index { get; set; }
            public byte Code { get; set; }
            public string Content { get; set; }
        }

        public List<StringReferenceEntry> GenerateStringReferenceList()
        {
            return _codes.OfType<StringCode>()
                .Select(code => new StringReferenceEntry(code.Index, code.Code, code.Content))
                .ToList();
        }

        public List<StringReferenceEntry> GenerateStringReferenceList(IEnumerable<StringListModifier>? modifiers)
        {
            if (modifiers == null) return GenerateStringReferenceList();

            var list = GenerateStringReferenceList();
            var dict = new SortedDictionary<int, StringReferenceEntry>();
            foreach (var entry in list) dict.Add(entry.Index, entry);
            foreach (var modifier in modifiers) modifier.Modify(dict);

            list = dict.Values.OrderBy(entry => entry.Index).ToList();
            return dict.Values.ToList();
        }

        public void ReplaceStringTable(IReadOnlyList<StringReferenceEntry> referenceList)
        {
            if (!_isStringPooled) throw new InvalidOperationException("Cannot replace string table of a non-steam version script");

            var strCodeList = _codes.OfType<StringCode>().ToList();
            if (strCodeList.Count == 0)
                return;

            if (strCodeList.Count != referenceList.Count)
                throw new ArgumentException($"string-list length {strCodeList.Count} != reference-list length {referenceList.Count}");

            foreach (var (code, refEntry) in strCodeList.Zip(referenceList))
            {
                if (code.Code != refEntry.Code)
                    throw new ArgumentException(
                        $"string-list code {code.Code:X02} != reference-list code {refEntry.Code}");
                code.Content = refEntry.Content;
            }

#if DEBUG
            var bytes = ToRawBytes();
            var script = new Script(bytes, Footer, isStringPooled: true);
            if (!script.ToRawBytes().SequenceEqual(bytes))
            {
                throw new ArgumentException("error");
            }
#endif
        }

        public byte[] ToRawBytes(Encoding? encoding = null)
        {
            using var stream = new BinaryStream(encoding ?? Utils.Cp932);

            WriteTo(stream);

            System.Diagnostics.Debug.Assert(stream.Length % 16 == 0);
            return stream.ToBytes();
        }

        public void WriteTo(IBinaryStream writer)
        {
            _header.WriteTo(writer);
            var stringPool = new List<string>();
            var stringOffsetTable = new Dictionary<string, int>();
            foreach (var code in _codes)
            {
                code.Offset = (int)writer.Position;
                code.WriteTo(writer);
                if (_isStringPooled && code is StringCode strCode)
                {
                    var content = strCode.Content;
                    if (stringPool.Count == 0 || stringPool.Last() != content)
                    {
                        stringPool.Add(content);
                    }
                }
            }
            if (_isStringPooled)
            {
                foreach (var s in stringPool)
                {
                    if (stringOffsetTable.TryAdd(s, (int)writer.Position))
                    {
                        writer.WriteStringZ(s);
                    }
                }
                var pos = writer.Position;
                foreach (var code in _codes.OfType<StringCode>())
                {
                    code.ContentOffset.AbsoluteOffset = stringOffsetTable[code.Content];
                    writer.Position = code.Offset;
                    code.WriteTo(writer);
                }
                writer.Position = pos;
            }
            writer.Write(FooterSeparator);
            writer.Write(Footer.ToBytes());
            var alignFillCount = 16 - writer.Position % 16;
            if (alignFillCount != 16)
            {
                for (var i = 0; i < alignFillCount; ++i)
                {
                    writer.Write((byte)0x00);
                }
            }
        }

        public void WriteToFile(string path, Encoding? encoding = null)
        {
            using var stream = BinaryStream.WriteFile(path, encoding ?? Utils.Cp932);
            WriteTo(stream);
        }

        public void DumpText(TextWriter writer, bool header = true, bool footer = true)
        {
            if (header)
            {
                writer.WriteLine("* * * Header * * *");
                _header.Dump(writer);
                writer.WriteLine();
            }

            writer.WriteLine("* * * Scripts * * *");

            foreach (var (i, code) in _codes.WithIndex())
            {
                if (_labelForward.TryGetValue(code.Offset, out var label))
                {
                    writer.Write("#");
                    writer.WriteLine(label);
                }
                //writer.Write($"{i,4} | 0x{code.Offset:X08}: ");
                writer.Write("  ");
                code.Dump(writer);
                writer.WriteLine();
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
            if (encoding == null) encoding = Utils.Utf8;
            using var writer = new StreamWriter(path, false, encoding);
            DumpText(writer, header, footer);
        }

        public HashSet<string> GetCharacterNames()
        {
            var names = new HashSet<string>();
            foreach (var code in Codes.OfType<StringCode>())
            {
                var index = $"{code.Index:000000}";
                if (code is ExtraDialogCode dialogCode && dialogCode.IsCharacter)
                {
                    names.Add(code.Content);
                }
            }
            return names;
        }

        public SortedDictionary<string, Transifex.TranslationInfo> GetTranslateSources()
        {
            string? currentName = null;

            var dict = new SortedDictionary<string, Transifex.TranslationInfo>();

            foreach (var code in Codes)
            {
                var index = $"{code.Index:000000}";
                if (code is ScriptJumpCode scriptJumpCode)
                {
                    if (!scriptJumpCode.IsJump) continue;

                    var prefix = code.Code switch
                    {
                        0x02 => @"jump-script ",
                        0x04 => @"call-script ",
                        _ => throw new InvalidDataException($"Is [{code.Code:X02}] script-jump-code???"),
                    };
                    dict.Add(index, new Transifex.TranslationInfo
                    {
                        Context = index,
                        Code = $"0x{code.Code:X2}",
                        String = prefix + scriptJumpCode.TargetScript,
                    });
                }
                else if (code is StringCode strCode)
                {
                    if (strCode is ExtraDialogCode dialogCode && dialogCode.IsCharacter)
                    {
                        currentName = strCode.Content;
                    }
                    else if (strCode is DialogCode || strCode is ExtraDialogCode)
                    {
                        dict.Add(index, new Transifex.TranslationInfo
                        {
                            Context = index,
                            Code = $"0x{strCode.Code:X2}",
                            DeveloperComment = currentName ?? "",
                            String = strCode.Content
                        });
                        currentName = null;
                    }
                    else
                    {
                        dict.Add(index, new Transifex.TranslationInfo
                        {
                            Context = index,
                            Code = $"0x{strCode.Code:X2}",
                            String = strCode.Content
                        });
                    }
                }
                else if (code is SssInputCode sssInputCode)
                {
                    dict.Add(index, new Transifex.TranslationInfo
                    {
                        Context = index,
                        Code = $"0x{sssInputCode.Code:X2}",
                        String = $"@sss-active {sssInputCode.TypeName.ToLower()} [{" ".Join(sssInputCode.EnumerateNames())}]"
                    });
                }
            }

            return dict;
        }

        public void ApplyTranslations(IDictionary<int, string> translations, IDictionary<string, string> nameTable)
        {
            foreach (var (index, translation) in translations)
            {
                var code = GetCodeAt(index);
                if (!(code is StringCode strCode))
                {
                    throw new InvalidOperationException($"Corrupt translation: attempt to apply [{code}] at index {index} to {translation}");
                }

                if (strCode is ExtraDialogCode exDialog && exDialog.IsCharacter)
                {
                    throw new InvalidOperationException($"Corrupt translation: attempt to apply [{code}] at index {index} to {translation}");
                }

                strCode.Content = translation;
            }

            foreach (var code in _codes.OfType<ExtraDialogCode>().Where(c => c.IsCharacter))
            {
                if (nameTable.TryGetValue(code.Content, out var translated))
                {
                    code.Content = translated;
                }
            }
        }

        public IEnumerable<char> EnumerateChars()
        {
            foreach (var code in GetCodes<StringCode>())
            {
                foreach (var c in code.Content) yield return c;
            }
            foreach (var code in GetCodes<DebugMenuCode>())
            {
                foreach (var c in code.EnumerateChars()) yield return c;
            }
        }
    }
}