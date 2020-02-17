using System.IO;

namespace CsYetiTools.VnScripts
{
    public class OpCode_05 : OpCode
    {
        public OpCode_05() : base(0x05) { }

        public override int ArgLength
            => 0;

        protected override void ReadArgs(BinaryReader reader) { }
        
        protected override void WriteArgs(BinaryWriter writer) { }

        protected override void DumpArgs(TextWriter writer) { }
    }
}
