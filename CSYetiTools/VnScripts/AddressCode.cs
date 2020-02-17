using System.Collections.Generic;
using System.IO;

namespace CsYetiTools.VnScripts
{
    public class AddressCode : OpCode, IHasAddress
    {
        private CodeAddressData _address = new CodeAddressData();

        public AddressCode(byte op) : base(op) { }

        public override int ArgLength
            => 4;

        protected override void ReadArgs(BinaryReader reader)
        {
            _address = ReadAddress(reader);
        }

        protected override void WriteArgs(BinaryWriter writer)
        {
            WriteAddress(writer, _address);
        }

        protected override void DumpArgs(TextWriter writer)
        {
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
