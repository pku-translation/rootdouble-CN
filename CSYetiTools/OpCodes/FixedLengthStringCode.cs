using System.IO;
using System.Linq;

namespace CSYetiTools.OpCodes
{
    public class FixedLengthStringCode : StringCode
    {
        private byte[] _extra = new byte[4];

        public FixedLengthStringCode(byte code) : base(code) { }

        public override int ArgLength
            => 4 + ContentLength;

        public override byte[] ArgsToBytes()
            => _extra.Concat(ContentToBytes()).ToArray();

        protected override string ArgsToString()
            => ArgsToString(false);

        protected override string ArgsToString(bool noString)
        {
            if (noString) return Utils.BytesToHex(_extra);
            else return Utils.BytesToHex(_extra) + " " + ContentToString();
        }

        protected override void Read(BinaryReader reader)
        {
            reader.Read(_extra);
            ReadString(reader);
        }
    }
}
