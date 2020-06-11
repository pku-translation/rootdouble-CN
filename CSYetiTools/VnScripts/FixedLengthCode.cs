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
        protected byte[] _args = System.Array.Empty<byte>();

        public FixedLengthCode(byte code, int argLength) : base(code)
        {
            _args = new byte[argLength];
        }

        public FixedLengthCode()
        { }

        public override int GetArgLength(IBinaryStream stream)
            => _args.Length;

        protected override void ReadArgs(IBinaryStream reader)
        {
            _args = reader.ReadBytesExact(_args.Length);
        }

        protected override void WriteArgs(IBinaryStream writer)
        {
            writer.Write(_args);
        }

        // protected override void DumpArgs(TextWriter writer)
        // {
        //     foreach (var arg in _args)
        //     {
        //         writer.Write(' ');
        //         writer.Write(arg.ToHex());
        //     }
        // }
    }
}
