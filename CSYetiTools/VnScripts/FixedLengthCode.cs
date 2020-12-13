using System.IO;
using CsYetiTools.IO;
using Untitled.Sexp.Attributes;
using Untitled.Sexp.Formatting;

namespace CsYetiTools.VnScripts
{
    [SexpAsList]
    public class FixedLengthCode : OpCode
    {
        [SexpBytesFormatting(Radix = NumberRadix.Hexadecimal)]
        protected byte[] Args;

        public FixedLengthCode(byte code, int argLength) : base(code)
        {
            Args = new byte[argLength];
        }

        public override int GetArgLength(IBinaryStream stream)
            => Args.Length;

        protected override void ReadArgs(IBinaryStream reader)
        {
            Args = reader.ReadBytesExact(Args.Length);
        }

        protected override void WriteArgs(IBinaryStream writer)
        {
            writer.Write(Args);
        }

        protected override void DumpArgs(TextWriter writer)
        {
            foreach (var arg in Args) {
                writer.Write(' ');
                writer.Write(arg.ToHex());
            }
        }
    }
}
