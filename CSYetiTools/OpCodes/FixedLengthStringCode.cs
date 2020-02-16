using System.IO;
using System.Linq;

namespace CSYetiTools.OpCodes
{
    public class FixedLengthStringCode : StringCode
    {
        public short Short1 { get; set; }

        public short Short2 { get; set; }

        public FixedLengthStringCode(byte code) : base(code) { }

        public override int ArgLength
            => 4 + ContentLength;

        public override byte[] ArgsToBytes()
            => GetBytes(Short1).Concat(GetBytes(Short2)).Concat(ContentToBytes()).ToArray();

        protected override string ArgsToString()
        {
            return $"{Short1} {Short2} " + ContentToString();
        }

        protected override void Read(BinaryReader reader)
        {
            Short1 = reader.ReadInt16();
            Short2 = reader.ReadInt16();
            ReadString(reader);
        }
    }
}
