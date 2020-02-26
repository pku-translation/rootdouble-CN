using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CsYetiTools.VnScripts
{
    public class CodeAddressData
    {
        /*
            seems if-else: 

            756 | [0A] CE 80 07 00 ${code 760}
            757 | [0C] 15 80 ${code 759}
            758 | [89] 2 1 [ 0 0 1 0 0 0 0 0 0]
            759 | [01] ${code 762}
            760 | [0C] 15 80 ${code 762}
            761 | [89] 3 0 [ 0 0 0 0 0 0 0 0 0]
            762 | ...
        */

        public CodeAddressData(int baseOffset = default, int absoluteOffset = default)
        {
            BaseOffset = baseOffset;
            AbsoluteOffset = absoluteOffset;
        }

        public int? TargetCodeIndex { get; set; }

        public int? TargetCodeRelativeIndex { get; set; }

        public string? TargetLabel { get; set; }

        public int AbsoluteOffset { get; set; }

        public int BaseOffset { get; set; }

        public int RelativeOffset
            => AbsoluteOffset - BaseOffset;

        public override string ToString()
        {
            if (TargetLabel != null)
            {
                return $"${TargetLabel}";
            }
            if (TargetCodeIndex != null)
            {
                return $"$0x{AbsoluteOffset:X8}:{{code {TargetCodeIndex}}}";
            }
            return $"$0x{AbsoluteOffset:X8}";
        }
    }

    public abstract class OpCode
    {
        private static OpCode CreateOpCode(byte op)
        {
            return op switch
            {
                0x00 => new ZeroCode(),  // empty block

                0x01 => new JumpCode(op),            // jump to address?
                0x02 => new ScriptJumpCode(0x02),       // script jump?
                0x03 => new JumpCode(op),            // jump to address?
                0x04 => new ScriptJumpCode(0x04),       // script jump or else?
                0x05 => new FixedLengthCode(op, 0),     // return? end-block?
                0x06 => new PrefixedAddressCode(op, 4), // invoke?
                0x07 => new PrefixedAddressCode(op, 4),
                0x08 => new PrefixedAddressCode(op, 4),
                0x09 => new PrefixedAddressCode(op, 4),
                0x0A => new PrefixedAddressCode(op, 4), // scope?
                0x0B => new PrefixedAddressCode(op, 4), // scope?

                0x0C => new OpCode_0C_0D(op),           // scope with sub codes?
                0x0D => new OpCode_0C_0D(op),           // scope with sub codes?

                0x0E => new OpCode_0E(),                // scoped by 0C/0D when count == 0x80CB ?
                                                        // all these scopes are: [0D] 15 80 0x${code +2} and [0C] 5B 80 0x${code +2}
                                                        // and [0E]s are always [0E] 80 80 CB 80

                0x32 => new DebugMenuCode(op),          // debug menu?

                0x45 => new DialogCode(),               // dialog
                0x47 => new ExtraDialogCode(),          // character name

                0x55 => new OpCode_55(),                // title

                0x68 => new TextAreaCode(),

                0x85 => new DynamicLengthStringCode(op), // directive message?
                                                         // [85] 0A 00 FF FF is message-box?
                0x86 => new NovelCode(),                 // novel

                0x87 => new SssInputCode(),              // センシズ受付開始？
                0x88 => new FixedLengthCode(op, 2),      // センシズ受付終了？
                0x89 => new SssFlagCode(),               // センシズフラッグ？

                _ => new FixedLengthCode(op, op switch
                {
                    0x10 => 4,
                    0x11 => 4,
                    0x12 => 4,
                    0x13 => 4,
                    0x14 => 4,
                    0x15 => 4,
                    0x16 => 4,
                    0x17 => 4,
                    0x18 => 4,
                    0x19 => 8,
                    0x1A => 4,
                    0x1B => 0,
                    0x1C => 0,
                    0x1D => 6,
                    0x1E => 10,
                    0x1F => 12,

                    0x20 => 6,
                    0x21 => 6,
                    0x22 => 4,
                    0x23 => 8,
                    0x24 => 6,
                    0x25 => 4,
                    0x27 => 0,
                    0x28 => 2,
                    0x2B => 0,
                    0x2C => 2,
                    0x2D => 4,
                    0x2E => 0,
                    0x2F => 2,

                    0x30 => 10,

                    0x33 => 0,
                    0x34 => 10,
                    0x35 => 4,
                    0x36 => 2,
                    0x37 => 0,
                    0x38 => 2,
                    0x39 => 4,
                    0x3A => 4,
                    0x3B => 2,
                    0x3C => 2,
                    0x3D => 0,
                    0x3F => 0,

                    0x42 => 8,
                    0x43 => 4,
                    0x44 => 4,  // dialog box related?

                    0x48 => 2,  // dialog box related? (area index when BC, FF FF when novel)
                    0x49 => 4,  // always FF FF FF FF
                    0x4A => 2,
                    0x4B => 4,
                    0x4C => 6,
                    0x4E => 4,
                    0x4F => 4,

                    0x51 => 6,

                    0x59 => 0,
                    0x5A => 0,
                    0x5E => 2,
                    0x5F => 0,

                    0x60 => 6,
                    0x61 => 6,
                    0x62 => 4,
                    0x63 => 10,
                    0x64 => 6,
                    0x65 => 4,
                    0x66 => 2,
                    //0x68 => 10,
                    0x69 => 2,
                    0x6A => 4,  // always FF FF FF FF
                    0x6B => 2,
                    0x6C => 16,
                    0x6D => 2,
                    0x6E => 4,
                    0x6F => 6,

                    0x70 => 0,
                    0x71 => 6,
                    0x72 => 4,
                    0x74 => 6,
                    0x75 => 4,
                    0x7A => 10,
                    0x7B => 4,
                    0x7C => 4,
                    0x7D => 2,
                    0x7E => 2,
                    0x7F => 4,

                    0x80 => 4,
                    0x81 => 2,
                    0x82 => 2,
                    0x83 => 4,

                    0x8A => 2,
                    0x8B => 4,
                    0x8C => 4,
                    0x8D => 0,
                    0x8E => 10,
                    0x8F => 6,

                    0x91 => 8,
                    _ => throw new InvalidDataException($"Unknown opcode {op:X02}"),
                }),
            };
        }

        public static OpCode GetNextCode(BinaryReader reader, IReadOnlyList<OpCode> prevCodes, bool isStringPooled)
        {
            var offset = (int)reader.BaseStream.Position;
            if (reader.BaseStream.Position == reader.BaseStream.Length) throw new OpCodeParseException("Stream is empty", "");

            var op = reader.ReadByte();

            try
            {
                var opCode = CreateOpCode(op);

                opCode._offset = offset;
                opCode._index = prevCodes.Count;

                if (opCode is OpCode_0E scopedCode)
                {
                    if (prevCodes.Count > 0
                        && prevCodes.Last() is OpCode_0C_0D scopeCode
                        && scopeCode.TargetOffset.AbsoluteOffset != 0)
                    {
                        scopedCode.TargetEndOffset = scopeCode.TargetOffset.AbsoluteOffset;
                    }
                }
                else if (opCode is StringCode strCode)
                {
                    strCode.IsOffset = isStringPooled;
                }
                opCode.ReadArgs(reader);

                #if DEBUG
                // check parse result
                reader.BaseStream.Seek(-opCode.TotalLength, SeekOrigin.Current);
                var opBytes = reader.ReadBytes(opCode.TotalLength);
                System.Diagnostics.Debug.Assert(opBytes.SequenceEqual(opCode.ToBytes()), $"OpCode 0x{op:X02} parsing is invalid");
                #endif
                
                return opCode;
            }
            catch (Exception exc)
            {
                reader.BaseStream.Seek(offset, SeekOrigin.Begin);
                var buffer = reader.ReadBytes(64);
                var context = string.Join(Environment.NewLine, Utils.BytesToTextLines(buffer, offset));
                reader.BaseStream.Seek(offset, SeekOrigin.Begin);
                throw new OpCodeParseException($"Error parsing code {op:X02}", context, exc);
            }
        }

        protected byte _code;

        protected int _offset;

        protected int _index;

        public byte Code
            => _code;

        public int Offset
            => _offset;

        public int Index
            => _index;

        public abstract int ArgLength { get; }

        public int TotalLength
            => ArgLength + 1;

        public byte[] ToBytes()
        {
            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);
            WriteTo(writer);
            return ms.ToArray();
        }

        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(_code);
            WriteArgs(writer);
        }

        public void Dump(TextWriter writer)
        {
            writer.Write('[');
            writer.Write(Code.ToHex());
            writer.Write(']');
            DumpArgs(writer);
        }

        protected OpCode(byte code)
        {
            _code = code;
        }

        public override string ToString()
        {
            using var writer = new StringWriter();
            Dump(writer);
            return writer.ToString();
        }

        protected CodeAddressData ReadAddress(BinaryReader reader)
        {
            return new CodeAddressData(_offset, reader.ReadInt32());
        }

        protected void WriteAddress(BinaryWriter writer, CodeAddressData address)
        {
            if (address.BaseOffset != _offset)
            {
                throw new InvalidOperationException($"Writing address data with {nameof(address.BaseOffset)}(0x{address.BaseOffset:X08}) != {_offset}");
            }
            writer.Write(address.AbsoluteOffset);
        }

        protected abstract void ReadArgs(BinaryReader reader);

        protected abstract void WriteArgs(BinaryWriter writer);

        protected abstract void DumpArgs(TextWriter writer);
    }
}