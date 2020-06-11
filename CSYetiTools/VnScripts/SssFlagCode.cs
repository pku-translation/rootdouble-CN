using System;
using System.IO;
using CsYetiTools.IO;
using Untitled.Sexp.Attributes;

namespace CsYetiTools.VnScripts
{
    [SexpAsList]
    public class SssFlagCode : OpCode
    {
        public short FlagIndex;

        public short Sound;

        // 9 shorts format an Ennegram.
        public short[] Changes = Array.Empty<short>();

        public override int GetArgLength(IBinaryStream stream)
            => 2 + 2 + 9 * 2;

        public SssFlagCode() : base(0x89) { }

        protected override void ReadArgs(IBinaryStream reader)
        {
            FlagIndex = reader.ReadInt16LE();
            Sound = reader.ReadInt16LE();
            Changes = new short[9];
            for (int i = 0; i < 9; ++i)
            {
                Changes[i] = reader.ReadInt16LE();
            }
        }

        protected override void WriteArgs(IBinaryStream writer)
        {
            writer.WriteLE(FlagIndex);
            writer.WriteLE(Sound);
            foreach (var change in Changes)
            {
                writer.WriteLE(change);
            }
        }

        // protected override void DumpArgs(TextWriter writer)
        // {
        //     writer.Write(' '); writer.Write(_flagIndex);
        //     writer.Write(' '); writer.Write(_sound);
        //     writer.Write(" [");
        //     foreach (var change in _changes)
        //     {
        //         writer.Write(' '); writer.Write(change.ToString().PadLeft(2));
        //     }
        //     writer.Write(" ]");
        // }
    }
}
