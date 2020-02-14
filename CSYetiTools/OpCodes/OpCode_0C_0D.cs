using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CSYetiTools.OpCodes
{
    public class OpCode_0C_0D : OpCode, IHasAddress
    {
        private short _unknown1;

        private CodeAddressData _targetOffset = new CodeAddressData();

        public OpCode_0C_0D(byte code) : base(code) { }

        public int TargetOffset
            => _targetOffset.AbsoluteOffset;

        public override int ArgLength
            => 6;

        public override byte[] ArgsToBytes()
        {
            return GetBytes(_unknown1)
                .Concat(GetBytes(_targetOffset))
                .ToArray();
        }

        protected override string ArgsToString()
            => Utils.BytesToHex(GetBytes(_unknown1)) + " " + _targetOffset.ToString();

        protected override void Read(BinaryReader reader)
        {
            _unknown1 = reader.ReadInt16();
            _targetOffset.BaseOffset = _offset;
            _targetOffset.AbsoluteOffset = reader.ReadInt32();
        }
        
        public void SetCodeIndices(IReadOnlyDictionary<int, OpCode> codeTable)
        {
            if (codeTable.TryGetValue(_targetOffset.AbsoluteOffset, out var code))
            {
                _targetOffset.TargetCodeIndex = code.Index;
                _targetOffset.TargetCodeRelativeIndex = code.Index - _index;
            }
        }
    }
}
