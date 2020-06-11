using System.Collections.Generic;
using System.IO;
using System.Linq;
using CsYetiTools.IO;
using Untitled.Sexp;
using Untitled.Sexp.Attributes;
using Untitled.Sexp.Formatting;

namespace CsYetiTools.VnScripts
{
    [SexpAsList]
    public class DebugMenuCode : OpCode, IHasAddress
    {
        [SexpAsList]
        private class Choice
        {
            public const int PrefixLength = 6;

            [SexpBytesFormatting(Radix = NumberRadix.Hexadecimal)]
            public byte[] Prefix = new byte[PrefixLength];

            public LabelReference Offset = new LabelReference();

            public string Title = "";

            public int GetLength(IBinaryStream stream)
                => PrefixLength + 4 + stream.GetStringZByteCount(Title);
        }

        [SexpNumberFormatting(Radix = NumberRadix.Hexadecimal)]
        private short _arg1;

        [SexpNumberFormatting(Radix = NumberRadix.Hexadecimal)]
        private short _arg2;

        private Choice[] _choices = System.Array.Empty<Choice>();

        public DebugMenuCode() : base(0x32) { }

        public override int GetArgLength(IBinaryStream stream)
            => 2 + 2 + 2 + _choices.Sum(c => c.GetLength(stream));

        protected override void ReadArgs(IBinaryStream reader)
        {
            _arg1 = reader.ReadInt16LE();
            int count = reader.ReadInt16LE();
            _arg2 = reader.ReadInt16LE();
            _choices = new Choice[count];
            for (int i = 0; i < count; ++i)
            {
                var choice = new Choice();
                choice.Prefix = reader.ReadBytesExact(Choice.PrefixLength);
                choice.Offset = ReadAddress(reader);
                choice.Title = reader.ReadStringZ();

                _choices[i] = choice;
            }
        }

        protected override void WriteArgs(IBinaryStream writer)
        {
            writer.WriteLE(_arg1);
            writer.WriteLE((short)_choices.Length);
            writer.WriteLE(_arg2);
            foreach (var choice in _choices)
            {
                writer.Write(choice.Prefix);
                WriteAddress(writer, choice.Offset);
                writer.WriteStringZ(choice.Title);
            }
        }

        // protected override void DumpArgs(TextWriter writer)
        // {
        //     writer.Write(' '); writer.Write(_arg1.ToHex());
        //     writer.Write(" choices:(short)"); writer.Write(_choices.Length);
        //     writer.Write(' '); writer.Write(_arg2.ToHex());
        //     writer.Write(" [");
        //     int index = 0;
        //     foreach (var choice in _choices)
        //     {
        //         writer.WriteLine();
        //         writer.Write("                "); writer.Write(index++.ToString().PadLeft(3));
        //         writer.Write(": "); writer.Write(Utils.BytesToHex(choice.Prefix));
        //         writer.Write(' '); writer.Write(choice.Offset.ToString());
        //         writer.Write(" \""); writer.Write(choice.Title); writer.Write("\"");
        //     }
        //     writer.Write(" ]");
        // }

        public IEnumerable<LabelReference> GetAddresses()
        {
            foreach (var choice in _choices)
            {
                yield return choice.Offset;
            }
        }

        public IEnumerable<char> EnumerateChars()
        {
            foreach (var choice in _choices)
            {
                foreach (var c in choice.Title) yield return c;
            }
        }
    }
}
