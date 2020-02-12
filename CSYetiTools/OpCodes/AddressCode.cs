using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CSYetiTools.OpCodes
{
    public class AddressCode : OpCode, IHasAddress
    {
        private CodeAddressData _address = new CodeAddressData();

        public AddressCode(byte op) : base(op) { }

        public override int ArgLength
            => 4;

        public override byte[] ArgsToBytes()
            => GetBytes(_address.AbsoluteOffset);



        protected override string ArgsToString()
            => _address.ToString();

        protected override void Read(BinaryReader reader) {
            _address.BaseOffset = _offset;
            _address.AbsoluteOffset = reader.ReadInt32();
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
