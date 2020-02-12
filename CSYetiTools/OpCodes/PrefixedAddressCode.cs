using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CSYetiTools.OpCodes
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

        public override byte[] ArgsToBytes()
            => _prefix.Concat(GetBytes(_address.AbsoluteOffset)).ToArray();

        protected override string ArgsToString()
            => Utils.BytesToHex(_prefix) + " " + _address.ToString();

        protected override void Read(BinaryReader reader) {
            reader.Read(_prefix, 0, _prefix.Length);
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
