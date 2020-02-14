using System.IO;
using System.Linq;

namespace CSYetiTools.OpCodes
{
    public class DynamicLengthStringCode : StringCode
    {
        public short Short1;
        public short Short2;

        private int _extralength;

        public DynamicLengthStringCode(byte code) : base(code) { }

        public override int ArgLength
            => _extralength + ContentLength;

        public override byte[] ArgsToBytes()
        {
            if (_extralength == 4)
            {
                return GetBytes(Short1).Concat(GetBytes(Short2)).Concat(ContentToBytes()).ToArray();
            }
            else
            {
                return GetBytes(Short1).Concat(ContentToBytes()).ToArray();
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
                    return Utils.BytesToHex(GetBytes(Short1)) + " " + Utils.BytesToHex(GetBytes(Short2));
                }
                else
                {
                    return Utils.BytesToHex(GetBytes(Short1));
                }
            }
            else
            {
                if (_extralength == 4)
                {
                    return Utils.BytesToHex(GetBytes(Short1)) + " " + Utils.BytesToHex(GetBytes(Short2)) + " " + ContentToString();
                }
                else
                {
                    return Utils.BytesToHex(GetBytes(Short1)) + " " + ContentToString();
                }
            }
        }

        protected override void Read(BinaryReader reader)
        {
            Short1 = reader.ReadInt16();
            Short2 = reader.ReadInt16();
            if (Short1 == -1 || Short2 == -1)
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
