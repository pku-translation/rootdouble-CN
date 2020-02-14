using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CSYetiTools.OpCodes;

namespace CSYetiTools
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

        public int AbsoluteOffset { get; set;}

        public int BaseOffset { get; set; }

        public int RelativeOffset
            => AbsoluteOffset - BaseOffset;

        public override string ToString()
        {
            if (TargetCodeRelativeIndex != null)
            {
                return $"$0x{AbsoluteOffset:X8}:{{code {TargetCodeIndex}}}";
                //if (TargetCodeRelativeIndex >= 0) return $"$0x{AbsoluteOffset:X8}{{code +{TargetCodeRelativeIndex}}}";
                //else return $"$0x{AbsoluteOffset:X8}{{code {TargetCodeRelativeIndex}}}";
            }
            return $"$0x{AbsoluteOffset:X8}";
            // if (RelativeOffset >= 0)
            // {
            //     if (RelativeOffset > 0xFFFF) return $"+{RelativeOffset:X8}";
            //     else return $"+{RelativeOffset:X4}";
            // }
            // else
            // {
            //     if (-RelativeOffset > 0xFFFF) return $"-{-RelativeOffset:X8}";
            //     else return $"-{-RelativeOffset:X4}";
            // }
        }
    }
    public class TestCode68 : FixedLengthCode
    {
        private static byte[] test1 = { 0x0A,0x00,0x64,0x00,0x18,0x00,0x38,0x04,0x60,0x00 };

        private static byte[] test2 = { 0x0A,0x00,0x64,0x00,0x10,0x00,0x38,0x04,0x60,0x00 };

        public TestCode68() : base(0x68, 11) { }

        protected override string ArgsToString(bool noString)
        {
            if (_args.SequenceEqual(test1) || _args.SequenceEqual(test2)) {
                return "";
            }
            else {
                return base.ArgsToString(noString);
            }
        }
    }

    public abstract class OpCode
    {
        private static OpCode CreateOpCode(byte op)
        {
            return op switch
            {
                0x00 => new ZeroCode(),  // empty block

                0x01 => new AddressCode(op),            // seems jump to address
                0x02 => new FixedLengthCode(op, 5),     // seems script jump
                0x03 => new AddressCode(op),            // seems jump to address
                0x04 => new FixedLengthCode(op, 5),     // seems script jump or else?
                0x05 => new OpCode_05(),                // seems return? end-block?
                0x06 => new PrefixedAddressCode(op, 4), // seems invoke
                0x07 => new PrefixedAddressCode(op, 4),
                0x08 => new PrefixedAddressCode(op, 4),
                0x09 => new PrefixedAddressCode(op, 4),
                0x0A => new PrefixedAddressCode(op, 4),
                0x0B => new PrefixedAddressCode(op, 4),

                0x0C => new OpCode_0C_0D(op),           // seems scope with sub codes
                0x0D => new OpCode_0C_0D(op),           // seems scope with sub codes

                0x0E => new OpCode_0E(),                // seems scoped by 0C/0D when count == 0x80CB
                                                        // all scopes are: [0D] 15 80 0x${code +2} and [0C] 5B 80 0x${code +2}
                                                        // and [0E]s are always [0E] 80 80 CB 80

                0x32 => new DebugMenuCode(op),          // debug menu?

                0x44 => new OpCode_44(),            
                0x45 => new DialogCode(),               // dialog
                0x47 => new ExtraDialogCode(),            // character name

                0x55 => new OpCode_55(),                // title

                //0x68 => new TestCode68(),

                0x85 => new DynamicLengthStringCode(op), // directive message?
                                                         // [85] 0A 00 FF FF is message-box?
                0x86 => new NovelCode(),             // novel

                0x87 => new SssInputCode(),          // センシズ受付開始？
                0x88 => new FixedLengthCode(op, 3),  // センシズ受付終了？
                0x89 => new SssFlagCode(),           // センシズフラッグ？

                _ => new FixedLengthCode(op, op switch
                {

                    0x0F => 9, // not found

                    0x10 => 5,
                    0x11 => 5,
                    0x12 => 5,
                    0x13 => 5,
                    0x14 => 5,
                    0x15 => 5,
                    0x16 => 5,
                    0x17 => 5,
                    0x18 => 5,
                    0x19 => 9,
                    0x1A => 5,
                    0x1B => 1,
                    0x1C => 1,
                    0x1D => 7,
                    0x1E => 11,
                    0x1F => 13,

                    0x20 => 7,
                    0x21 => 7,
                    0x22 => 5,
                    0x23 => 9,
                    0x24 => 7,
                    0x25 => 5,
                    0x27 => 1,
                    0x28 => 3,
                    0x2B => 1,
                    0x2C => 3,
                    0x2D => 5,
                    0x2E => 1,
                    0x2F => 3,

                    0x30 => 11,

                    0x33 => 1,
                    0x34 => 11,
                    0x35 => 5,
                    0x36 => 3,
                    0x37 => 1,
                    0x38 => 3,
                    0x39 => 5,
                    0x3A => 5,
                    0x3B => 3,
                    0x3C => 3,
                    0x3D => 1,
                    0x3F => 1,

                    0x42 => 9,
                    0x43 => 5,

                    0x48 => 3,
                    0x49 => 5,
                    0x4A => 3,
                    0x4B => 5,
                    0x4C => 7,
                    0x4E => 5,
                    0x4F => 5,

                    0x51 => 7,

                    0x59 => 1,
                    0x5A => 1,
                    0x5E => 3,
                    0x5F => 1,

                    0x60 => 7,
                    0x61 => 7,
                    0x62 => 5,
                    0x63 => 11,
                    0x64 => 7,
                    0x65 => 5,
                    0x66 => 3,
                    0x68 => 11, // {[68] 0A 00 64 00 18 (->10) 00 38 04 60 00} 24 changed to 16, maybe font-size?
                    0x69 => 3,
                    0x6A => 5,
                    0x6B => 3,
                    0x6C => 17,
                    0x6D => 3,
                    0x6E => 5,
                    0x6F => 7,

                    0x70 => 1,
                    0x71 => 7,
                    0x72 => 5,
                    0x74 => 7,
                    0x75 => 5,
                    0x7A => 11,
                    0x7B => 5,
                    0x7C => 5,
                    0x7D => 3,
                    0x7E => 3,
                    0x7F => 5,

                    0x80 => 5,
                    0x81 => 3,
                    0x82 => 3,
                    0x83 => 5,

                    0x8A => 3,
                    0x8B => 5,
                    0x8C => 5,
                    0x8D => 1,
                    0x8E => 11,
                    0x8F => 7,

                    0x91 => 9,
                    _ => throw new InvalidDataException($"Unknown opcode {op:X02}"),
                }),
            };
        }

        protected static byte[] GetBytes(int n)
            => BitConverter.GetBytes(n);
        protected static byte[] GetBytes(short n)
            => BitConverter.GetBytes(n);

        protected byte[] GetBytes(CodeAddressData address)
            => BitConverter.GetBytes(address.AbsoluteOffset);

        private static string GetContext(Stream stream)
        {
            const int length = 64;
            var buffer = new byte[length];
            var offset = (int)stream.Position;
            var read = stream.Read(buffer);
            return string.Join(Environment.NewLine, Utils.BytesToTextLines(buffer.Take(read).ToArray(), offset));
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
                        && scopeCode.TargetOffset != 0)
                    {
                        scopedCode.TargetEndOffset = scopeCode.TargetOffset;
                    }
                }
                else if (opCode is StringCode strCode)
                {
                    strCode.IsOffset = isStringPooled;
                }
                opCode.Read(reader);

                return opCode;
            }
            catch (Exception exc)
            {
                reader.BaseStream.Seek(offset, SeekOrigin.Begin);
                var context = GetContext(reader.BaseStream);
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
            var argsBytes = ArgsToBytes();
            var bytes = new byte[argsBytes.Length + 1];
            bytes[0] = _code;
            argsBytes.CopyTo(bytes, 1);
            return bytes;
        }

        public abstract byte[] ArgsToBytes();

        protected OpCode(byte code)
        {
            _code = code;
        }

        protected abstract string ArgsToString();

        protected virtual string ArgsToString(bool noString = false)
        {
            return ArgsToString();
        }

        public override string ToString()
        {
            return ToString(false);
        }

        public string ToString(bool noString = false)
        {
            var args = ArgsToString(noString);
            if (string.IsNullOrWhiteSpace(args)) return $"[{_code:X02}]";
            else return $"[{_code:X02}] " + args;
        }

        protected abstract void Read(BinaryReader reader);
    }
}