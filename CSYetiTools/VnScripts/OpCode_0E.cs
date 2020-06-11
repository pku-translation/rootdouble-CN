using System;
using System.Collections.Generic;
using System.IO;
using CsYetiTools.IO;
using Untitled.Sexp.Attributes;
using Untitled.Sexp.Formatting;

namespace CsYetiTools.VnScripts
{
    [SexpAsList]
    public sealed class OpCode_0E : OpCode, IHasAddress
    {
        // if _count == InvalidCount then the size of _remain will be determined by previous 0C/0D?
        private const short InvalidCount = unchecked((short)0x80CB);
        
        [SexpNumberFormatting(Radix = NumberRadix.Hexadecimal)]
        private ushort _unknown1;

        private short _count;

        [SexpAsList]
        private class Branch
        {
            public short Prefix;
            public LabelReference Offset = new LabelReference();

            public Branch(short prefix, LabelReference offset)
            {
                Prefix = prefix;
                Offset = offset;
            }

            public void Deconstruct(out short prefix, out LabelReference offset)
            {
                prefix = Prefix;
                offset = Offset;
            }
        }

        private Branch[] _branches = Array.Empty<Branch>();

        [SexpIgnore]
        public int TargetEndOffset { get; set; }

        public OpCode_0E() : base(0x0E) { }

        public override int GetArgLength(IBinaryStream stream)
            => 4 + _branches.Length * 6;

        protected override void ReadArgs(IBinaryStream reader)
        {
            _unknown1 = reader.ReadUInt16LE();
            _count = reader.ReadInt16LE();
            if (_count == InvalidCount)
            {
                int size = TargetEndOffset - (int)reader.Position;
                if (size < 0 || size % 6 != 0)
                    throw new ArgumentException($"Scoped op-code 0E with invalid range [{(int)reader.Position:X08}, {TargetEndOffset:X08}) (size={size})");
                _branches = new Branch[size / 6];
            }
            else
            {
                _branches = new Branch[_count];
            }
            for (int i = 0; i < _branches.Length; ++i)
            {
                var prefix = reader.ReadInt16LE();
                var offset = ReadAddress(reader);
                _branches[i] = new Branch(prefix, offset);
            }
        }

        protected override void WriteArgs(IBinaryStream writer)
        {
            writer.WriteLE(_unknown1);
            writer.WriteLE(_count);
            for (int i = 0; i < _branches.Length; ++i)
            {
                writer.WriteLE(_branches[i].Prefix);
                WriteAddress(writer, _branches[i].Offset);
            }
        }

        // protected override void DumpArgs(TextWriter writer)
        // {
        //     writer.Write(' '); writer.Write(_unknown1.ToHex());
        //     if (_count == InvalidCount)
        //     {
        //         writer.Write(' '); writer.Write(InvalidCount.ToHex());
        //     }
        //     else
        //     {
        //         writer.Write(" (short)"); writer.Write(_count);
        //     }
        //     writer.Write(" [");
        //     foreach (var (i, branch) in _branches.WithIndex())
        //     {
        //         writer.WriteLine();
        //             writer.Write("                ");
        //             writer.Write(i.ToString().PadLeft(3));
        //             writer.Write(": ");
        //             writer.Write(branch.prefix.ToString("X04"));
        //             writer.Write(": ");
        //             writer.Write(branch.offset.ToString());
        //     }
        //     writer.Write(" ]");
        // }

        public IEnumerable<LabelReference> GetAddresses()
        {
            foreach (var (prefix, offset) in _branches)
            {
                yield return offset;
            }
        }
    }
}
