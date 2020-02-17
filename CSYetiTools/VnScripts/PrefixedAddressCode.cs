using System.Collections.Generic;
using System.IO;

namespace CsYetiTools.VnScripts
{
    public class PrefixedAddressCode : OpCode, IHasAddress
    {
        private byte[] _prefix;

        private CodeAddressData _address = new CodeAddressData();

        public PrefixedAddressCode(byte op, int prefixLength) : base(op) { 
            _prefix = new byte[prefixLength];
        }

        public override int ArgLength
            => _prefix.Length + 4;

        protected override void ReadArgs(BinaryReader reader)
        {
            reader.Read(_prefix, 0, _prefix.Length);
            _address = ReadAddress(reader);
        }

        protected override void WriteArgs(BinaryWriter writer)
        {
            writer.Write(_prefix);
            WriteAddress(writer, _address);
        }

        protected override void DumpArgs(TextWriter writer)
        {
            writer.Write(' '); writer.Write(Utils.BytesToHex(_prefix));
            writer.Write(' '); writer.Write(_address);
        }
        
        public void SetCodeIndices(IReadOnlyDictionary<int, OpCode> codeTable)
        {
            if (codeTable.TryGetValue(_address.AbsoluteOffset, out var code))
            {
                _address.TargetCodeIndex = code.Index;
                _address.TargetCodeRelativeIndex = code.Index - _index;
            }
        }
    }
}
