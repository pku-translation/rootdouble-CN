using System.Collections.Generic;
using System.IO;
using CsYetiTools.IO;
using Untitled.Sexp.Attributes;

namespace CsYetiTools.VnScripts
{
    [SexpAsList]
    public class CmpJumpCode : OpCode, IHasAddress
    {
        public byte[] Prefix { get; set; } = new byte[4];

        public LabelReference TargetAddress { get; set; } = new LabelReference();

        public CmpJumpCode(byte op) : base(op)
        { }

        public override int GetArgLength(IBinaryStream stream)
            => 4 + 4;

        protected override void ReadArgs(IBinaryStream reader)
        {
            reader.Read(Prefix, 0, Prefix.Length);
            TargetAddress = ReadAddress(reader);
        }

        protected override void WriteArgs(IBinaryStream writer)
        {
            writer.Write(Prefix);
            WriteAddress(writer, TargetAddress);
        }

        protected override void DumpArgs(TextWriter writer)
        {
            writer.Write(' '); writer.Write(Utils.BytesToHex(Prefix));
            writer.Write(' '); writer.Write(TargetAddress);
        }

        public IEnumerable<LabelReference> GetAddresses()
        {
            yield return TargetAddress;
        }
    }
}
