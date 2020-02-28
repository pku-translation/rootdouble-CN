using System.IO;

namespace CsYetiTools.VnScripts
{
    public class FixedLengthCode : OpCode
    {
        protected int _argLength;
        protected byte[] _args = System.Array.Empty<byte>();

        public FixedLengthCode(byte code, int argLength) : base(code)
        {
            _argLength = argLength;
        }

        public override int ArgLength
            => _argLength;

        protected override void ReadArgs(BinaryReader reader)
        {
            _args = reader.ReadBytesExact(_argLength);
        }

        protected override void WriteArgs(BinaryWriter writer)
        {
            writer.Write(_args);
        }

        protected override void DumpArgs(TextWriter writer)
        {
            foreach (var arg in _args)
            {
                writer.Write(' ');
                writer.Write(arg.ToHex());
            }
        }
    }
}
