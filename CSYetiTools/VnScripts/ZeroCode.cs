using System.IO;

namespace CsYetiTools.VnScripts
{
    // 0x00 when allowed (confusing :( )
    public sealed class ZeroCode : OpCode
    {
        public ZeroCode() : base(0x00) { }

        public override int ArgLength
            => 0;

        protected override void ReadArgs(BinaryReader reader) { }

        protected override void WriteArgs(BinaryWriter writer) { }

        protected override void DumpArgs(TextWriter writer) { }

    }
}
