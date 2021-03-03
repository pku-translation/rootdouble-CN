using System.IO;
using CsYetiTools.IO;
using Untitled.Sexp.Attributes;

namespace CsYetiTools.VnScripts
{
    [SexpAsList]
    public class ScriptJumpCode : OpCode
    {
        public short TargetScript { get; set; }

        public short TargetEntryIndex { get; set; }

        public ScriptJumpCode(byte op) : base(op) { }

        public override int GetArgLength(IBinaryStream stream)
            => 4;

        protected override void ReadArgs(IBinaryStream reader)
        {
            TargetScript = reader.ReadInt16LE();
            TargetEntryIndex = reader.ReadInt16LE();
        }

        protected override void WriteArgs(IBinaryStream writer)
        {
            writer.WriteLE(TargetScript);
            writer.WriteLE(TargetEntryIndex);
        }

        protected override void DumpArgs(TextWriter writer)
        {
            writer.Write(' '); writer.Write(TargetScript);
            writer.Write(' '); writer.Write(TargetEntryIndex);
        }
    }
}
