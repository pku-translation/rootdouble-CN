using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CSYetiTools.OpCodes
{
    public class OpCode_31_32 : OpCode, IHasAddress
    {
        private class Choice
        {
            public const int PrefixLength = 6;

            public byte[] Prefix = new byte[PrefixLength];

            public CodeAddressData Offset = new CodeAddressData();

            public string Title = "";

            public int Length
                => PrefixLength + 4 + Utils.GetStringZByteCount(Title);
        }

        
        private short _arg1;

        private short _arg2;

        private Choice[] _choices = System.Array.Empty<Choice>();

        public OpCode_31_32(byte code) : base(code) { }

        public override int ArgLength
            => 2 + 1 + 2 + _choices.Sum(c => c.Length);

        public override byte[] ArgsToBytes()
        {
            using var ms = new MemoryStream();
            ms.Write(GetBytes(_arg1));
            ms.Write(GetBytes((short)_choices.Length));
            ms.Write(GetBytes(_arg2));
            foreach (var choice in _choices)
            {
                ms.Write(choice.Prefix);
                ms.Write(GetBytes(choice.Offset));
                ms.Write(Utils.GetStringZBytes(choice.Title).ToArray());
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
            foreach (var choice in _choices)
            {
                builder.AppendLine()
                    .Append("                ")
                    .Append(index++.ToString().PadLeft(3))
                    .Append(": ")
                    .Append(Utils.BytesToHex(choice.Prefix))
                    .Append(" ")
                    .Append(choice.Offset.ToString())
                    .Append(" \"")
                    .Append(choice.Title)
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
            _choices = new Choice[count];
            for (int i = 0; i < count; ++i)
            {
                var choice = new Choice();
                choice.Prefix = reader.ReadBytes(Choice.PrefixLength);
                choice.Offset.BaseOffset = _offset;
                choice.Offset.AbsoluteOffset = reader.ReadInt32();
                choice.Title = Utils.ReadStringZ(reader);

                _choices[i] = choice;
            }
        }

        public void SetCodeIndices(IReadOnlyDictionary<int, OpCode> codeTable)
        {
            foreach (var choice in _choices)
            {
                if (codeTable.TryGetValue(choice.Offset.AbsoluteOffset, out var code))
                {
                    choice.Offset.TargetCodeIndex = code.Index;
                    choice.Offset.TargetCodeRelativeIndex = code.Index - _index;
                }
            }
        }
    }
}
