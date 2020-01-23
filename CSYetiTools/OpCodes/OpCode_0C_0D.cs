using System;
using System.IO;
using System.Linq;

namespace CSYetiTools.OpCodes
{
    public class OpCode_0C_0D : OpCode
    {
        private short _unknown1;

        private int _targetOffset;

        public OpCode_0C_0D(byte code) : base(code) { }

        public int TargetOffset
            => _targetOffset;

        public override int ArgLength
            => 6;

        public override byte[] ArgsToBytes()
        {
            return GetBytes(_unknown1)
                .Concat(GetBytes(_targetOffset))
                .ToArray();
        }

        protected override string ArgsToString()
            => Utils.BytesToHex(GetBytes(_unknown1)) + " 0x" + _targetOffset.ToString("X08");

        protected override void Read(BinaryReader reader)
        {
            _unknown1 = reader.ReadInt16();
            _targetOffset = reader.ReadInt32();
        }
    }
}
