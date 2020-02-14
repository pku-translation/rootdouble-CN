using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CSYetiTools.OpCodes
{
    /*********************************************************************************
        Route-A:    [87] 01 00 [ 1 0 1 0 0 0 0 0 0 ] = 渡瀬 洵 yellow
                    | [88] CC 80
                    | [0B] CE 80 CC 80 ${code 402} => [01] ${code 680}
                
                    branch 1 => [89] 00 00 01 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
                    branch 2 => [89] 01 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00

                    [87] 02 00 [ 1 0 1 0 0 0 0 0 0 ] = 渡瀬 洵 yellow
                    | [88] CC 80
                    | [08] CE 80 CC 80 ${code 627}
                    | [01] ${code 675} =>  [02] 66 00 00 00 ${chunk 102}
                    | [0C] 15 80 ${code 629}

                    branch 1 => [89] 02 00 01 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
                    branch 2 => [89] 03 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00

        Route-A:    0 1 0 0 0 0 0 0 0 = 風見 yellow
                    | [82] 01 00
                    | [3C] 20 00
                    | [82] 00 00
                    =
                    | [88] CC 80
                    | [0A] CD 80 07 00 ${code 4372}
                    | [01] ${code 4348}

        渡瀬  風見  洵  恵那  宇喜多  悠里  夏彦  ましろ  サリュ

    *********************************************************************************/

    public class SssInputCode : OpCode
    {
        private static readonly string[] TypeName = { "Blue", "Yellow", "Red" };

        private short _type; // 1 = yellow, 2 = red

        // 9 shorts format an Ennegram.
        private short[][] _ennegrams = Array.Empty<short[]>();

        public SssInputCode() : base(0x87) { }

        public override int ArgLength
            => 2 + _ennegrams.Length * 18 + 2; // 0xFFFF as end.

        public override byte[] ArgsToBytes()
        {
            using var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms);
            bw.Write(_type);
            foreach (var ennegram in _ennegrams)
            {
                foreach (var elem in ennegram)
                {
                    bw.Write(elem);
                }
            }
            bw.Write((short)-1);
            bw.Flush();
            return ms.ToArray();
        }

        protected override string ArgsToString()
        {
            var builder = new StringBuilder()
                .AppendLine(TypeName[_type])
                .Append("               Active:   [ ");
            foreach (var elem in _ennegrams[0]) 
            {
                builder.Append(elem).Append(' ');
            }
            builder.Append("]");
            for (int i = 1; i < _ennegrams.Length; ++i)
            {
                builder.AppendLine()
                    .Append("               Answer ")
                    .Append(i.ToString())
                    .Append(": [ ");
                foreach (var elem in _ennegrams[i])
                {
                    builder.Append(elem).Append(' ');
                }
                builder.Append("]");
            }
            return builder.ToString();
        }

        protected override void Read(BinaryReader reader)
        {
            _type = reader.ReadInt16();
            if (_type < 0 || _type >= TypeName.Length) throw new InvalidDataException("$Invalid SssInputCode type {_type}");
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
    }
}
