using System.IO;
using System.Linq;

namespace CSYetiTools.OpCodes
{
    class OpCode_55 : StringCode
    {
        private short _startOffset; // 0 1
        private byte[] _unknown1 = System.Array.Empty<byte>(); // 2 3 4
        private short _endOffset; // 5 6
        private byte[] _unknown2 = System.Array.Empty<byte>(); // 7 8

        public OpCode_55() : base(0x55) { }

        public override int ArgLength
            => 10 + ContentLength;

        public override byte[] ArgsToBytes()
        {
            if (IsOffset)
            {
                return ContentToBytes();
            }
            else
            {
                return GetBytes(_startOffset)
                    .Concat(_unknown1)
                    .Concat(GetBytes(_endOffset))
                    .Concat(_unknown2)
                    .Concat(ContentToBytes())
                    .ToArray();

            }
        }

        protected override string ArgsToString()
        {
            if (IsOffset)
            {
                return ContentToString();
            }
            else
            {
                return $"{_startOffset:X04} {Utils.BytesToHex(_unknown1)} {_endOffset:X04} {Utils.BytesToHex(_unknown2)} {ContentToString()}";
            }
        }

        protected override void Read(BinaryReader reader)
        {
            if (IsOffset)
            {
                ReadString(reader);
            }
            else
            {
                _startOffset = reader.ReadInt16();
                _unknown1 = reader.ReadBytes(3);
                _endOffset = reader.ReadInt16();
                _unknown2 = reader.ReadBytes(2);
                ReadString(reader);
            }
        }
    }
}
