using System.IO;
using System.Linq;
using System.Text;

namespace CSYetiTools.OpCodes
{
    public class OpCode_31_32 : OpCode
    {
        private const int PrefixLength = 10;
        
        private short _arg1;

        private short _arg2;

        private (byte[] prefix, string title)[] _choices = System.Array.Empty<(byte[] prefix, string title)>();

        public OpCode_31_32(byte code) : base(code) { }

        public override int ArgLength
            => 2 + 1 + 2 + _choices.Sum(c => PrefixLength + Utils.GetStringZByteCount(c.title));

        public override byte[] ArgsToBytes()
        {
            using var ms = new MemoryStream();
            ms.Write(GetBytes(_arg1));
            ms.Write(GetBytes((short)_choices.Length));
            ms.Write(GetBytes(_arg2));
            foreach (var (prefix, title) in _choices)
            {
                ms.Write(prefix);
                ms.Write(Utils.GetStringZBytes(title).ToArray());
            }
            return ms.ToArray();
        }

        protected override string ArgsToString()
        {
            var builder = new StringBuilder()
                .Append(Utils.BytesToHex(GetBytes(_arg1)))
                .Append(" (short)")
                .Append(_choices.Length)
                .Append(" choices ")
                .Append(Utils.BytesToHex(GetBytes(_arg2)))
                .Append(" [");
            int index = 0;
            foreach (var (prefix, title) in _choices)
            {
                builder.AppendLine()
                    .Append("                ")
                    .Append(index++.ToString().PadLeft(3))
                    .Append(": ")
                    .Append(Utils.BytesToHex(prefix))
                    .Append(" \"")
                    .Append(title)
                    .Append("\"");
                
            }
            builder.Append(" ]");
            return builder.ToString();
        }

        protected override void Read(BinaryReader reader)
        {
            _arg1 = reader.ReadInt16();
            int count = reader.ReadInt16();
            _arg2 = reader.ReadInt16();
            _choices = new (byte[], string)[count];
            for (int i = 0; i < count; ++i)
            {
                _choices[i].prefix = reader.ReadBytes(PrefixLength);
                _choices[i].title = Utils.ReadStringZ(reader);
            }
        }
    }
}
