using System.Collections.Generic;
using System.IO;
using CsYetiTools.IO;

namespace CsYetiTools.VnScripts
{
    public class PrefixedAddressCode : OpCode, IHasAddress
    {
        private byte[] _prefix;

        public CodeAddressData TargetAddress { get; set; } = new CodeAddressData();

        public PrefixedAddressCode(byte op, int prefixLength) : base(op) { 
            _prefix = new byte[prefixLength];
        }

        public override int ArgLength
            => _prefix.Length + 4;

        protected override void ReadArgs(IBinaryStream reader)
        {
            reader.Read(_prefix, 0, _prefix.Length);
            TargetAddress = ReadAddress(reader);
        }

        protected override void WriteArgs(IBinaryStream writer)
        {
            writer.Write(_prefix);
            WriteAddress(writer, TargetAddress);
        }

        protected override void DumpArgs(TextWriter writer)
        {
            writer.Write(' '); writer.Write(Utils.BytesToHex(_prefix));
            writer.Write(' '); writer.Write(TargetAddress);
        }
        
        public IEnumerable<CodeAddressData> GetAddresses()
        {
            yield return TargetAddress;
        }
    }
}
