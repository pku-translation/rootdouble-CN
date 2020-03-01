using System;
using System.Collections.Generic;
using System.IO;
using CsYetiTools.IO;

namespace CsYetiTools.VnScripts
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

        protected override void ReadArgs(IBinaryStream reader)
        {
            _unknown1 = reader.ReadInt16LE();
            _count = reader.ReadInt16LE();
            if (_count == InvalidCount)
            {
                int size = TargetEndOffset - (int)reader.Position;
                if (size < 0 || size % 6 != 0)
                    throw new ArgumentException($"Scoped op-code 0E with invalid range [{(int)reader.Position:X08}, {TargetEndOffset:X08}) (size={size})");
                _branches = new (short prefix, CodeAddressData offset)[size / 6];
            }
            else
            {
                _branches = new (short prefix, CodeAddressData offset)[_count];
            }
            for (int i = 0; i < _branches.Length; ++i)
            {
                _branches[i].prefix = reader.ReadInt16LE();
                _branches[i].offset = new CodeAddressData(Offset, reader.ReadInt32LE());
            }
        }

        protected override void WriteArgs(IBinaryStream writer)
        {
            writer.WriteLE(_unknown1);
            writer.WriteLE(_count);
            for (int i = 0; i < _branches.Length; ++i)
            {
                writer.WriteLE(_branches[i].prefix);
                WriteAddress(writer, _branches[i].offset);
            }
        }

        protected override void DumpArgs(TextWriter writer)
        {
            writer.Write(' '); writer.Write(_unknown1.ToHex());
            if (_count == InvalidCount)
            {
                writer.Write(' '); writer.Write(InvalidCount.ToHex());
            }
            else
            {
                writer.Write(" (short)"); writer.Write(_count);
            }
            writer.Write(" [");
            foreach (var (i, branch) in _branches.WithIndex())
            {
                writer.WriteLine();
                    writer.Write("                ");
                    writer.Write(i.ToString().PadLeft(3));
                    writer.Write(": ");
                    writer.Write(branch.prefix.ToString("X04"));
                    writer.Write(": ");
                    writer.Write(branch.offset.ToString());
            }
            writer.Write(" ]");
        }

        public IEnumerable<CodeAddressData> GetAddresses()
        {
            foreach (var (prefix, offset) in _branches)
            {
                yield return offset;
            }
        }
    }
}
