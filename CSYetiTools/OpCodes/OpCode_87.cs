using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CSYetiTools.OpCodes
{
    public class OpCode_87 : OpCode
    {
        private short _unknown1;

        private (short prefix, byte[] content)[] _pairs = Array.Empty<(short prefix, byte[] content)>();

        public OpCode_87() : base(0x87) { }

        public override int ArgLength
            => 2 + _pairs.Length * 18 + 2;

        public override byte[] ArgsToBytes()
        {
            var bytes = new byte[ArgLength];
            GetBytes(_unknown1).CopyTo(bytes, 0);
            for (int i = 0; i < _pairs.Length; ++i)
            {
                GetBytes(_pairs[i].prefix).CopyTo(bytes, 2 + i * 18);
                _pairs[i].content.CopyTo(bytes, 2 + i * 18 + 2);
            }
            GetBytes((short)-1).CopyTo(bytes, 2 + _pairs.Length * 18);
            return bytes;
        }

        protected override string ArgsToString()
        {
            var builder = new StringBuilder()
                .Append(Utils.BytesToHex(GetBytes(_unknown1)))
                .Append(" (")
                .Append(_pairs.Length)
                .Append(") [");
            for (int i = 0; i < _pairs.Length; ++i)
            {
                builder.AppendLine()
                    .Append("                ")
                    .Append(i.ToString().PadLeft(3))
                    .Append(": ")
                    .Append(_pairs[i].prefix.ToString("X04"))
                    .Append(" ")
                    .Append(Utils.BytesToHex(_pairs[i].content));
            }
            builder.Append(" ]");
            return builder.ToString();
        }

        protected override void Read(BinaryReader reader)
        {
            _unknown1 = reader.ReadInt16();
            var pairs = new List<(short prefix, byte[] content)>();
            while (true)
            {
                var s = reader.ReadInt16();
                if (s == -1) break;
                var c = reader.ReadBytes(16);
                pairs.Add((s, c));
            }
            _pairs = pairs.ToArray();
        }
    }
}
