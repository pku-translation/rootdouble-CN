using System.IO;
using System.Linq;

namespace CSYetiTools.OpCodes
{
    class OpCode_55 : StringCode
    {
        private CodeAddressData _startOffset = new CodeAddressData();
        private byte _unknown = 0x01; // always 0x01 ?
        private CodeAddressData _endOffset = new CodeAddressData();

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
                    .ConcatOne(_unknown)
                    .Concat(GetBytes(_endOffset))
                    .Concat(ContentToBytes())
                    .ToArray();
            }
        }

        protected override string ArgsToString()
        {
            // if (IsOffset)
            // {
            //     return ContentToString();
            // }
            // else
            // {
            //     return $"{_startOffset:X04} {Utils.BytesToHex(_unknown1)} {_endOffset:X04} {Utils.BytesToHex(_unknown2)} {ContentToString()}";
            // }
            return ContentToString();
        }

        protected override string ArgsToString(bool noString = false)
        {
            if (noString) return string.Empty;
            else return ContentToString();
        }

        protected override void Read(BinaryReader reader)
        {
            if (IsOffset)
            {
                ReadString(reader);
            }
            else
            {
                _startOffset.BaseOffset = _offset;
                _startOffset.AbsoluteOffset = reader.ReadInt32();
                if (_startOffset.RelativeOffset != 10) throw new InvalidDataException("code [55] start != 10");
                _unknown = reader.ReadByte();
                if (_unknown != 0x01) throw new InvalidDataException("code [55] separator != 0x01");
                _endOffset.BaseOffset = _offset;
                _endOffset.AbsoluteOffset = reader.ReadInt32();
                ReadString(reader);
                if (_endOffset.RelativeOffset != 10 + Utils.GetStringZByteCount(Content)) throw new InvalidDataException("code [55] end != 10 + strlen");
            }
        }
    }
}
