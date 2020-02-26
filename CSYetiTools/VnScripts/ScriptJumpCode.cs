using System.IO;

namespace CsYetiTools.VnScripts
{
    public class ScriptJumpCode : OpCode
    {
        public short TargetScript { get; set; }

        public short Unknown { get; set; }

        public ScriptJumpCode(byte op) : base(op) { }

        public override int ArgLength
            => 4;

        public bool IsJump
            => Unknown == 0;

        protected override void ReadArgs(BinaryReader reader)
        {
            TargetScript = reader.ReadInt16();
            Unknown = reader.ReadInt16();
        }

        protected override void WriteArgs(BinaryWriter writer)
        {
            writer.Write(TargetScript);
            writer.Write(Unknown);
        }

        protected override void DumpArgs(TextWriter writer)
        {
            writer.Write(' '); writer.Write(TargetScript);
            writer.Write(' '); writer.Write(Unknown);
        }
    }
}