using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CSYetiTools.OpCodes;

namespace CSYetiTools
{
    public abstract class OpCode
    {
        public static readonly byte EndBlock = 0x05;

        protected static byte[] GetBytes(int n)
            => BitConverter.GetBytes(n);
        protected static byte[] GetBytes(short n)
            => BitConverter.GetBytes(n);

        private static string GetContext(Stream stream)
        {
            const int length = 64;
            var buffer = new byte[length];
            var offset = (int)stream.Position;
            var read = stream.Read(buffer);
            return string.Join(Environment.NewLine, Utils.BytesToTextLines(buffer.Take(read).ToArray(), offset));
        }

        public static OpCode GetNextCode(BinaryReader reader, IReadOnlyList<OpCode> prevCodes, bool isSteam)
        {
            var offset = (int)reader.BaseStream.Position;
            if (reader.BaseStream.Position == reader.BaseStream.Length) throw new OpCodeParseException("Stream is empty", "");

            var op = reader.ReadByte();
            if (op == 0x00)
            {
                // if it's in 0C/0D scope, tailing zero is allowed?
                // assume there is no long-ranged scope?
                foreach (var code in prevCodes.Reverse())
                {
                    if (code is OpCode_0C_0D scopeCode && scopeCode.TargetOffset >= reader.BaseStream.Position)
                    {
                        var opCode = new ZeroCode();
                        opCode.Read(reader);
                        return opCode;
                    }
                }

                // else throw exception
                reader.BaseStream.Seek(offset, SeekOrigin.Begin);
                var context = GetContext(reader.BaseStream);
                reader.BaseStream.Seek(offset, SeekOrigin.Begin);
                throw new ZeroCodeException("Code is zero", context);
            }
            try
            {
                if (s_opCodeTable.TryGetValue(op, out var obj))
                {
                    OpCode opCode;
                    if (obj is int n)
                    {
                        opCode = new FixedLengthCode(op, n);
                    }
                    else if (obj is Func<OpCode> func)
                    {
                        opCode = func();
                    }
                    else
                    {
                        reader.BaseStream.Seek(offset, SeekOrigin.Begin);
                        var context = GetContext(reader.BaseStream);
                        reader.BaseStream.Seek(offset, SeekOrigin.Begin);
                        throw new OpCodeParseException($"Unknown opcode {op:X02}", context);
                    }

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
                        strCode.IsOffset = isSteam;
                    }
                    opCode.Read(reader);

                    return opCode;
                }
                else
                {
                    reader.BaseStream.Seek(offset, SeekOrigin.Begin);
                    var context = GetContext(reader.BaseStream);
                    reader.BaseStream.Seek(offset, SeekOrigin.Begin);
                    throw new OpCodeParseException($"Unknown opcode {op:X02}", context);
                }
            }
            catch (Exception exc)
            {
                reader.BaseStream.Seek(offset, SeekOrigin.Begin);
                var context = GetContext(reader.BaseStream);
                reader.BaseStream.Seek(offset, SeekOrigin.Begin);
                throw new OpCodeParseException($"Error parsing code {op:X02}", context, exc);
            }
        }

        private static Func<OpCode> F(Func<OpCode> lambda)
            => lambda;

        protected static Dictionary<byte, object> s_opCodeTable = new Dictionary<byte, object>
        {
            [0x01] = 5,    // with offset
            [0x02] = 5,
            [0x03] = 5,
            [0x04] = 5,
            [0x05] = F(() => new OpCode_05()),
            [0x06] = 9,    // with offset
            [0x07] = 9,
            [0x08] = 9,
            [0x09] = 9,
            [0x0A] = 9,
            [0x0B] = 9,
            [0x0C] = F(() => new OpCode_0C_0D(0x0C)),  // seems scope with sub codes
            [0x0D] = F(() => new OpCode_0C_0D(0x0D)),  // seems scope with sub codes
            [0x0E] = F(() => new OpCode_0E()), //  seems scoped by 0C/0D when count == 0x80CB

            [0x0F] = 9,

            [0x10] = 5,
            [0x11] = 5,
            [0x12] = 5,
            [0x13] = 5,
            [0x14] = 5,
            [0x15] = 5,
            [0x16] = 5,
            [0x17] = 5,
            [0x18] = 5,
            [0x19] = 9,
            [0x1A] = 5,
            [0x1B] = 1,
            [0x1C] = 1,
            [0x1D] = 7,
            [0x1E] = 11,
            [0x1F] = 13,

            [0x20] = 7,
            [0x21] = 7,
            [0x22] = 5,
            [0x23] = 9,
            [0x24] = 7,
            [0x25] = 5,
            [0x27] = 1,
            [0x28] = 3,
            [0x2B] = 1,
            [0x2C] = 3,
            [0x2D] = 5,
            [0x2E] = 1,
            [0x2F] = 3,

            [0x30] = 11,
            [0x31] = F(() => new OpCode_31_32(0x31)), // choice
            [0x32] = F(() => new OpCode_31_32(0x32)), // switch
            [0x33] = 1,
            [0x34] = 11,
            [0x35] = 5,
            [0x36] = 3,
            [0x37] = 1,
            [0x38] = 3,
            [0x39] = 5,
            [0x3A] = 5,
            [0x3B] = 3,
            [0x3C] = 3,
            [0x3D] = 1,
            [0x3F] = 1,

            [0x42] = 9,
            [0x43] = 5,
            [0x44] = F(() => new OpCode_44()),
            [0x45] = F(() => new DialogCode()), // dialog
            [0x47] = F(() => new CharacterCode()), // character name
            [0x48] = 3,
            [0x49] = 5,
            [0x4A] = 3,
            [0x4B] = 5,
            [0x4C] = 7,
            [0x4E] = 5,
            [0x4F] = 5,

            [0x51] = 7,
            [0x55] = F(() => new OpCode_55()), // title
            [0x59] = 1,
            [0x5A] = 1,
            [0x5E] = 3,
            [0x5F] = 1,

            [0x60] = 7,
            [0x61] = 7,
            [0x62] = 5,
            [0x63] = 11,
            [0x64] = 7,
            [0x65] = 5,
            [0x66] = 3,
            [0x68] = 11,
            [0x69] = 3,
            [0x6A] = 5,
            [0x6B] = 3,
            [0x6C] = 17,
            [0x6D] = 3,
            [0x6E] = 5,
            [0x6F] = 7,

            [0x70] = 1,
            [0x71] = 7,
            [0x72] = 5,
            [0x74] = 7,
            [0x75] = 5,
            [0x7A] = 11,
            //[0x7B] = f(() => new OpCode_7B()),
            [0x7B] = 5,
            [0x7C] = 5,
            [0x7D] = 3,
            [0x7E] = 3,
            [0x7F] = 5,

            [0x80] = 5,
            [0x81] = 3,
            [0x82] = 3,
            [0x83] = 5,
            [0x85] = F(() => new DynamicLengthStringCode(0x85)), // directive message?
            [0x86] = F(() => new NovelCode()),  // novel
            [0x87] = F(() => new OpCode_87()),  // SSS?
            [0x88] = 3, // ?
            [0x89] = 23, // ?

            [0x8A] = 3,
            [0x8B] = 5,
            [0x8C] = 5,
            [0x8D] = 1,
            [0x8E] = 11,
            [0x8F] = 7,

            [0x91] = 9,
        };

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

        public override string ToString()
        {
            var args = ArgsToString();
            if (string.IsNullOrWhiteSpace(args)) return $"[{_code:X02}]";
            else return $"[{_code:X02}] " + args;
        }

        protected abstract void Read(BinaryReader reader);
    }
}