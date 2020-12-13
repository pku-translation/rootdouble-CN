using System;
using System.Collections.Generic;
using System.IO;
using CsYetiTools.IO;
using Untitled.Sexp.Attributes;

namespace CsYetiTools.VnScripts
{
    [SexpAsList]
    public class SssInputCode : OpCode
    {
        private static readonly string[] TypeNames = { "Blue", "Yellow", "Red" };

        private static readonly string[] Names = { "渡瀬", "風見", "洵", "恵那", "宇喜多", "悠里", "夏彦", "ましろ", "サリュ" };

        [SexpSymbolEnum]
        public enum SssType
        {
            Blue,
            Yellow,
            Red,
        }

        public SssType Type { get; set; } // 1 = yellow, 2 = red

        // 9 shorts format an Ennegram.
        public short[][] Ennegrams { get; set; } = Array.Empty<short[]>();

        public SssInputCode() : base(0x87) { }

        public override int GetArgLength(IBinaryStream stream)
            => 2 + Ennegrams.Length * 18 + 2; // 0xFFFF as end.

        protected override void ReadArgs(IBinaryStream reader)
        {
            var type = reader.ReadInt16LE();
            if (type < 0 || type >= TypeNames.Length) throw new InvalidDataException("$Invalid SssInputCode type {_type}");
            Type = (SssType)type;
            var ennegrams = new List<short[]>();
            while (true) {
                var s = reader.ReadInt16LE();
                if (s == -1) break;
                var ennegram = new short[9];
                ennegram[0] = s;
                foreach (var i in 1..9) {
                    ennegram[i] = reader.ReadInt16LE();
                }
                ennegrams.Add(ennegram);
            }
            Ennegrams = ennegrams.ToArray();
        }

        protected override void WriteArgs(IBinaryStream writer)
        {
            writer.WriteLE((short)Type);
            foreach (var ennegram in Ennegrams) {
                foreach (var elem in ennegram) {
                    writer.WriteLE(elem);
                }
            }
            writer.WriteLE((short)-1);
        }

        protected override void DumpArgs(TextWriter writer)
        {
            writer.Write(' '); writer.WriteLine(TypeNames[(int)Type]);
            writer.Write("               Active:   [ ");
            foreach (var elem in Ennegrams[0]) {
                writer.Write(elem); writer.Write(' ');
            }
            writer.Write("]");
            foreach (var i in 1..Ennegrams.Length) {
                writer.WriteLine();
                writer.Write("               Answer ");
                writer.Write(i);
                writer.Write(": [ ");
                foreach (var elem in Ennegrams[i]) {
                    writer.Write(elem); writer.Write(' ');
                }
                writer.Write("]");
            }
        }

        [SexpIgnore]
        public string TypeName
            => TypeNames[(short)Type];

        public IEnumerable<string> EnumerateNames()
        {
            for (var i = 0; i < 9; ++i) {
                if (Ennegrams[0][i] != 0) yield return Names[i];
            }
        }
    }
}
