using System.IO;
using System.Linq;

namespace CSYetiTools.OpCodes
{
    public class DynamicLengthStringCode : StringCode
    {
        private short _extra1;
        private short _extra2;

        private int _extralength;

        public DynamicLengthStringCode(byte code) : base(code) { }

        public override int ArgLength
            => _extralength + ContentLength;

        public override byte[] ArgsToBytes()
        {
            if (_extralength == 4)
            {
                return GetBytes(_extra1).Concat(GetBytes(_extra2)).Concat(ContentToBytes()).ToArray();
            }
            else
            {
                return GetBytes(_extra1).Concat(ContentToBytes()).ToArray();
            }
        }

        protected override string ArgsToString()
        {
            return ArgsToString(false);
        }

        protected override string ArgsToString(bool noString = false)
        {
            if (noString)
            {
                if (_extralength == 4)
                {
                    return Utils.BytesToHex(GetBytes(_extra1)) + " " + Utils.BytesToHex(GetBytes(_extra2));
                }
                else
                {
                    return Utils.BytesToHex(GetBytes(_extra1));
                }
            }
            else
            {
                if (_extralength == 4)
                {
                    return Utils.BytesToHex(GetBytes(_extra1)) + " " + Utils.BytesToHex(GetBytes(_extra2)) + " " + ContentToString();
                }
                else
                {
                    return Utils.BytesToHex(GetBytes(_extra1)) + " " + ContentToString();
                }
            }
        }

        protected override void Read(BinaryReader reader)
        {
            _extra1 = reader.ReadInt16();
            _extra2 = reader.ReadInt16();
            if (_extra1 == -1 || _extra2 == -1)
            {
                _extralength = 4;
            }
            else
            {
                reader.BaseStream.Seek(-2, SeekOrigin.Current);
                _extralength = 2;
            }
            ReadString(reader);
        }
    }
}
