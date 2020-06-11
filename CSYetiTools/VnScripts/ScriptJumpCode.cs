using System.IO;
using CsYetiTools.IO;
using Untitled.Sexp.Attributes;

namespace CsYetiTools.VnScripts
{
    [SexpAsList]
    public class ScriptJumpCode : OpCode
    {
        public short TargetScript { get; set; }

        public short Unknown { get; set; }

        public ScriptJumpCode(byte op) : base(op) { }

        public override int GetArgLength(IBinaryStream stream)
            => 4;

        [SexpIgnore]
        public bool IsJump
            => Unknown == 0;

        protected override void ReadArgs(IBinaryStream reader)
        {
            TargetScript = reader.ReadInt16LE();
            Unknown = reader.ReadInt16LE();
        }

        protected override void WriteArgs(IBinaryStream writer)
        {
            writer.WriteLE(TargetScript);
            writer.WriteLE(Unknown);
        }

        // protected override void DumpArgs(TextWriter writer)
        // {
        //     writer.Write(' '); writer.Write(TargetScript);
        //     writer.Write(' '); writer.Write(Unknown);
        // }
    }
}