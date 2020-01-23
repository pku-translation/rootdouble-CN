using System.IO;
using System.Linq;

namespace CSYetiTools.OpCodes
{
    public class OpCode_44 : OpCode
    {
        private short _arg1;
        private short _arg2;

        private byte _optionalByte; // a op-code?

        public OpCode_44() : base(0x44) { }

        public override int ArgLength
            => _arg2 != -1 ? 4 : 5;

        public override byte[] ArgsToBytes()
        {
            if (_arg2 != -1)
            {
                return GetBytes(_arg1).Concat(GetBytes(_arg2)).ToArray();
            }
            else
            {
                return GetBytes(_arg1).Concat(GetBytes(_arg2)).ConcatOne(_optionalByte).ToArray();
            }
        }

        protected override string ArgsToString()
            => Utils.BytesToHex(ArgsToBytes());

        protected override void Read(BinaryReader reader)
        {
            _arg1 = reader.ReadInt16();
            _arg2 = reader.ReadInt16();
            if (_arg2 == -1) _optionalByte = reader.ReadByte();
        }
    }
}
