using System.IO;
using CsYetiTools.IO;

namespace CsYetiTools.VnScripts
{
    // 0x00 when allowed (confusing :( )
    public sealed class ZeroCode : OpCode
    {
        public ZeroCode() : base(0x00) { }

        public override int ArgLength
            => 0;

        protected override void ReadArgs(IBinaryStream reader) { }

        protected override void WriteArgs(IBinaryStream writer) { }

        protected override void DumpArgs(TextWriter writer) { }

    }
}
