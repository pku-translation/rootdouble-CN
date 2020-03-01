using System.IO;
using CsYetiTools.IO;

namespace CsYetiTools.VnScripts
{
    class OpCode_55 : StringCode
    {
        private CodeAddressData _startOffset = new CodeAddressData();
        private byte _unknown = 0x01; // always 0x01 ?
        private CodeAddressData _endOffset = new CodeAddressData();

        public CodeAddressData StartOffset => _startOffset;

        public CodeAddressData EndOffset => _endOffset;

        public OpCode_55() : base(0x55) { }

        public override int ArgLength
            => IsOffset ? 4 : 9 + ContentLength;

        protected override void ReadArgs(IBinaryStream reader)
        {
            if (IsOffset)
            {
                ReadString(reader);
            }
            else
            {
                _startOffset = ReadAddress(reader);
                if (_startOffset.RelativeOffset != 10) throw new InvalidDataException($"code [55] start {_startOffset.RelativeOffset} != 10");
                _unknown = reader.ReadByte();
                if (_unknown != 0x01) throw new InvalidDataException("code [55] separator != 0x01");
                _endOffset = ReadAddress(reader);
                ReadString(reader);
                if (_endOffset.RelativeOffset != 10 + Utils.GetStringZByteCount(Content)) throw new InvalidDataException($"code [55] end {_endOffset.RelativeOffset} != 10 + strlen {Utils.GetStringZByteCount(Content)}");
            }
        }

        protected override void WriteArgs(IBinaryStream writer)
        {
            if (IsOffset)
            {
                WriteString(writer);
            }
            else
            {
                WriteAddress(writer, _startOffset);
                writer.Write(_unknown);
                WriteAddress(writer, _endOffset);
                WriteString(writer);
            }
        }

        protected override void DumpArgs(TextWriter writer)
        {
            // if (IsOffset)
            // {
            //     writer.Write(' '); writer.Write(ContentToString());
            // }
            // else
            // {
            //     writer.Write($" {_startOffset:X04} {Utils.BytesToHex(_unknown1)} {_endOffset:X04} {Utils.BytesToHex(_unknown2)} {ContentToString()}");
            // }
            writer.Write(' '); writer.Write(ContentToString());
        }
    }
}
