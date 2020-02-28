using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CsYetiTools.VnScripts
{
    public class DebugMenuCode : OpCode, IHasAddress
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

        public DebugMenuCode(byte code) : base(code) { }

        public override int ArgLength
            => 2 + 2 + 2 + _choices.Sum(c => c.Length);

        protected override void ReadArgs(BinaryReader reader)
        {
            _arg1 = reader.ReadInt16();
            int count = reader.ReadInt16();
            _arg2 = reader.ReadInt16();
            _choices = new Choice[count];
            for (int i = 0; i < count; ++i)
            {
                var choice = new Choice();
                choice.Prefix = reader.ReadBytesExact(Choice.PrefixLength);
                choice.Offset = ReadAddress(reader);
                choice.Title = Utils.ReadStringZ(reader);

                _choices[i] = choice;
            }
        }

        protected override void WriteArgs(BinaryWriter writer)
        {
            writer.Write(_arg1);
            writer.Write((short)_choices.Length);
            writer.Write(_arg2);
            foreach (var choice in _choices)
            {
                writer.Write(choice.Prefix);
                WriteAddress(writer, choice.Offset);
                Utils.WriteStringZ(writer, choice.Title);
            }
        }

        protected override void DumpArgs(TextWriter writer)
        {
            writer.Write(' '); writer.Write(_arg1.ToHex());
            writer.Write(" choices:(short)"); writer.Write(_choices.Length);
            writer.Write(' '); writer.Write(_arg2.ToHex());
            writer.Write(" [");
            int index = 0;
            foreach (var choice in _choices)
            {
                writer.WriteLine();
                writer.Write("                "); writer.Write(index++.ToString().PadLeft(3));
                writer.Write(": "); writer.Write(Utils.BytesToHex(choice.Prefix));
                writer.Write(' '); writer.Write(choice.Offset.ToString());
                writer.Write(" \""); writer.Write(choice.Title); writer.Write("\"");
            }
            writer.Write(" ]");
        }

        public IEnumerable<CodeAddressData> GetAddresses()
        {
            foreach (var choice in _choices)
            {
                yield return choice.Offset;
            }
        }
    }
}
