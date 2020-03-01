using System;
using System.Collections.Generic;
using System.IO;

namespace CsYetiTools.VnScripts
{
    public class SssInputCode : OpCode
    {
        private static readonly string[] TypeNames = { "Blue", "Yellow", "Red" };

        private static readonly string[] Names = { "渡瀬", "風見", "洵", "恵那", "宇喜多", "悠里", "夏彦", "ましろ", "サリュ" };

        private short _type; // 1 = yellow, 2 = red

        // 9 shorts format an Ennegram.
        private short[][] _ennegrams = Array.Empty<short[]>();

        public SssInputCode() : base(0x87) { }

        public override int ArgLength
            => 2 + _ennegrams.Length * 18 + 2; // 0xFFFF as end.

        protected override void ReadArgs(BinaryReader reader)
        {
            _type = reader.ReadInt16();
            if (_type < 0 || _type >= TypeNames.Length) throw new InvalidDataException("$Invalid SssInputCode type {_type}");
            var ennegrams = new List<short[]>();
            while (true)
            {
                var s = reader.ReadInt16();
                if (s == -1) break;
                var ennegram = new short[9];
                ennegram[0] = s;
                for (int i = 1; i < 9; ++i)
                {
                    ennegram[i] = reader.ReadInt16();
                }
                ennegrams.Add(ennegram);
            }
            _ennegrams = ennegrams.ToArray();
        }

        protected override void WriteArgs(BinaryWriter writer)
        {
            writer.Write(_type);
            foreach (var ennegram in _ennegrams)
            {
                foreach (var elem in ennegram)
                {
                    writer.Write(elem);
                }
            }
            writer.Write((short)-1);
        }

        protected override void DumpArgs(TextWriter writer)
        {
            writer.Write(' '); writer.WriteLine(TypeNames[_type]);
            writer.Write("               Active:   [ ");
            foreach (var elem in _ennegrams[0])
            {
                writer.Write(elem); writer.Write(' ');
            }
            writer.Write("]");
            for (int i = 1; i < _ennegrams.Length; ++i)
            {
                writer.WriteLine();
                writer.Write("               Answer ");
                writer.Write(i);
                writer.Write(": [ ");
                foreach (var elem in _ennegrams[i])
                {
                    writer.Write(elem); writer.Write(' ');
                }
                writer.Write("]");
            }
        }

        public string TypeName
            => TypeNames[_type];

        public IEnumerable<string> EnumerateNames()
        {
            for (int i = 0; i < 9; ++i)
            {
                if (_ennegrams[0][i] != 0) yield return Names[i];
            }
        }
    }
}
