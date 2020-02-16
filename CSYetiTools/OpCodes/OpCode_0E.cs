using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CSYetiTools.OpCodes
{
    public class OpCode_0E : OpCode, IHasAddress
    {
        // if _count == InvalidCount then the size of _remain will be determined by previous 0C/0D?
        private const short InvalidCount = unchecked((short)0x80CB);

        private short _unknown1;

        private short _count;

        private (short prefix, CodeAddressData offset)[] _branches = Array.Empty<(short prefix, CodeAddressData offset)>();

        public int TargetEndOffset { get; set; }

        public OpCode_0E() : base(0x0E) { }

        public override int ArgLength
            => 4 + _branches.Length * 6;

        public override byte[] ArgsToBytes()
        {
            var bytes = new byte[ArgLength];
            GetBytes(_unknown1).CopyTo(bytes, 0);
            GetBytes(_count).CopyTo(bytes, 2);
            for (int i = 0; i < _branches.Length; ++i)
            {
                GetBytes(_branches[i].prefix).CopyTo(bytes, 4 + i * 6);
                GetBytes(_branches[i].offset).CopyTo(bytes, 4 + i * 6 + 2);
            }
            return bytes;
        }

        protected override string ArgsToString()
        {
            var builder = new StringBuilder()
                .Append(Utils.BytesToHex(GetBytes(_unknown1)));
            if (_count == InvalidCount)
            {
                builder.Append(" ").Append(Utils.BytesToHex(GetBytes(InvalidCount)));
            }
            else
            {
                builder.Append(" (short)").Append(_count);
            }
            builder.Append(" [");
            foreach (var (i, branch) in _branches.WithIndex())
            {
                builder.AppendLine()
                    .Append("                ")
                    .Append(i.ToString().PadLeft(3))
                    .Append(": ")
                    .Append(branch.prefix.ToString("X04"))
                    .Append(": ")
                    .Append(branch.offset.ToString());
            }
            builder.Append(" ]");
            return builder.ToString();
        }

        protected override void Read(BinaryReader reader)
        {
            _unknown1 = reader.ReadInt16();
            _count = reader.ReadInt16();
            if (_count == InvalidCount)
            {
                int size = TargetEndOffset - (int)reader.BaseStream.Position;
                if (size < 0 || size % 6 != 0)
                    throw new ArgumentException($"Scoped op-code 0E with invalid range [{(int)reader.BaseStream.Position:X08}, {TargetEndOffset:X08}) (size={size})");
                _branches = new (short prefix, CodeAddressData offset)[size / 6];
            }
            else
            {
                _branches = new (short prefix, CodeAddressData offset)[_count];
            }
            for (int i = 0; i < _branches.Length; ++i)
            {
                _branches[i].prefix = reader.ReadInt16();
                _branches[i].offset = new CodeAddressData(_offset, reader.ReadInt32());
            }
        }

        public void SetCodeIndices(IReadOnlyDictionary<int, OpCode> codeTable)
        {
            foreach (var (prefix, offset) in _branches)
            {
                if (codeTable.TryGetValue(offset.AbsoluteOffset, out var code))
                {
                    offset.TargetCodeIndex = code.Index;
                    offset.TargetCodeRelativeIndex = code.Index - _index;
                }
            }
        }
    }
}
