using System;
using System.IO;

namespace CSYetiTools.OpCodes
{
    public class OpCode_05 : OpCode
    {
        public OpCode_05() : base(0x05) { }

        public override int ArgLength
            => 0;

        public override byte[] ArgsToBytes()
            => Array.Empty<byte>();

        protected override string ArgsToString()
            => string.Empty;

        protected override void Read(BinaryReader reader) { }
    }
}
