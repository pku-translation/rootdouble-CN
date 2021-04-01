using System;
using System.Collections.Generic;
using System.IO;
using CSYetiTools.Base;
using CSYetiTools.Base.IO;

namespace CSYetiTools.VnScripts
{
    public sealed class SwitchCode : OpCode, IHasAddress
    {
        // if _count is runtime then the size of _remain will be determined at runtime (in this game always [CB 80])

        public ScriptArgument Arg { get; set; } = new();

        public ScriptArgument Count { get; set; } = new();

        private class Branch
        {
            public short Prefix { get; }
            public LabelReference Offset { get; }

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

        public int TargetEndOffset { get; set; }

        protected override string CodeName => "switch";

        public override int GetArgLength(IBinaryStream stream)
            => 4 + _branches.Length * 6;

        protected override void ReadArgs(IBinaryStream reader)
        {
            Arg = ReadArgument(reader);
            Count = ReadArgument(reader);
            if (Count.IsConst) {
                _branches = new Branch[Count.ConstValue];
            }
            else {
                var size = TargetEndOffset - (int)reader.Position;
                if (size < 0 || size % 6 != 0)
                    throw new ArgumentException($"Scoped op-code 0E with invalid range [{(int)reader.Position:X08}, {TargetEndOffset:X08}) (size={size})");
                _branches = new Branch[size / 6];
            }
            foreach (var i in .._branches.Length) {
                var prefix = reader.ReadInt16LE();
                var offset = ReadAddress(reader);
                _branches[i] = new Branch(prefix, offset);
            }
        }

        protected override void WriteArgs(IBinaryStream writer)
        {
            WriteArgument(writer, Arg);
            WriteArgument(writer, Count);
            foreach (var (prefix, offset) in _branches) {
                writer.WriteLE(prefix);
                WriteAddress(writer, offset);
            }
        }

        protected override void DumpArgs(TextWriter writer)
        {
            writer.Write(' '); writer.Write(Arg);
            writer.Write(' '); writer.Write(Count);
            writer.Write(" [");
            foreach (var (i, (prefix, offset)) in _branches.WithIndex()) {
                writer.WriteLine();
                writer.Write("                ");
                writer.Write(i.ToString().PadLeft(3));
                writer.Write(": ");
                writer.Write(prefix.ToString("X04"));
                writer.Write(": ");
                writer.Write(offset.ToString());
            }
            writer.Write(" ]");
        }

        public IEnumerable<LabelReference> GetAddresses()
        {
            foreach (var (_, offset) in _branches) {
                yield return offset;
            }
        }
    }
}
