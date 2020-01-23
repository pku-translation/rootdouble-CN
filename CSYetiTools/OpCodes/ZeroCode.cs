using System;
using System.IO;
using System.Linq;

namespace CSYetiTools.OpCodes
{
    // 0x00 when allowed (confusing :( )
    public class ZeroCode : OpCode
    {
        public ZeroCode() : base(0x00) { }

        public override int ArgLength
            => 0;

        public override byte[] ArgsToBytes()
            => Array.Empty<byte>();

        protected override string ArgsToString()
            => string.Empty;

        protected override void Read(BinaryReader reader) { }
    }
}