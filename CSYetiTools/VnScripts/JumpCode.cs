using System.Collections.Generic;
using System.IO;
using CsYetiTools.IO;
using Untitled.Sexp.Attributes;

namespace CsYetiTools.VnScripts
{
    [SexpAsList]
    public class JumpCode : OpCode, IHasAddress
    {
        public LabelReference TargetAddress { get; set; } = new LabelReference();

        public JumpCode() { }

        public JumpCode(byte op) : base(op) { }

        public override int GetArgLength(IBinaryStream stream)
            => 4;

        protected override void ReadArgs(IBinaryStream reader)
        {
            TargetAddress = ReadAddress(reader);
        }

        protected override void WriteArgs(IBinaryStream writer)
        {
            WriteAddress(writer, TargetAddress);
        }

        // protected override void DumpArgs(TextWriter writer)
        // {
        //     writer.Write(' '); writer.Write(TargetAddress);
        // }

        public IEnumerable<LabelReference> GetAddresses()
        {
            yield return TargetAddress;
        }
    }
}
