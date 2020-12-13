using System;
using System.IO;
using System.Linq;
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
            Changes = Utils.Generate(() => reader.ReadInt16LE(), 9).ToArray();
        }

        protected override void WriteArgs(IBinaryStream writer)
        {
            writer.WriteLE(FlagIndex);
            writer.WriteLE(Sound);
            foreach (var change in Changes) {
                writer.WriteLE(change);
            }
        }

        protected override void DumpArgs(TextWriter writer)
        {
            writer.Write(' '); writer.Write(FlagIndex);
            writer.Write(' '); writer.Write(Sound);
            writer.Write(" [");
            foreach (var change in Changes) {
                writer.Write(' '); writer.Write(change.ToString().PadLeft(2));
            }
            writer.Write(" ]");
        }
    }
}
