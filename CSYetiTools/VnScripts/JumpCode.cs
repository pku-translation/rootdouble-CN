using System.Collections.Generic;
using System.IO;
using CsYetiTools.IO;

namespace CsYetiTools.VnScripts
{
    public class JumpCode : OpCode, IHasAddress
    {
        public CodeAddressData TargetAddress { get; set; } = new CodeAddressData();

        public JumpCode(byte op) : base(op) { }

        public override int ArgLength
            => 4;

        protected override void ReadArgs(IBinaryStream reader)
        {
            TargetAddress = ReadAddress(reader);
        }

        protected override void WriteArgs(IBinaryStream writer)
        {
            WriteAddress(writer, TargetAddress);
        }

        protected override void DumpArgs(TextWriter writer)
        {
            writer.Write(' '); writer.Write(TargetAddress);
        }

        public IEnumerable<CodeAddressData> GetAddresses()
        {
            yield return TargetAddress;
        }
    }
}
