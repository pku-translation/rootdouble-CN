using System.IO;
using System.Linq;

namespace CSYetiTools.OpCodes
{
    public class FixedLengthCode : OpCode
    {
        protected int _argLength;
        protected byte[] _args = System.Array.Empty<byte>();

        public FixedLengthCode(byte code, int length) : base(code)
        {
            _argLength = length - 1;
        }

        public override int ArgLength
            => _argLength;

        public override byte[] ArgsToBytes()
            => _args.ToArray();

        protected override string ArgsToString()
            => " ".Join(_args.Select(b => b.ToString("X02")));

        protected override void Read(BinaryReader reader)
        {
            _args = reader.ReadBytes(_argLength);
        }
    }
}
