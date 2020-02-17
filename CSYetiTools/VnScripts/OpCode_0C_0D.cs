using System.Collections.Generic;
using System.IO;

namespace CsYetiTools.VnScripts
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

        protected override void ReadArgs(BinaryReader reader)
        {
            _unknown1 = reader.ReadInt16();
            _targetOffset = ReadAddress(reader);
        }

        protected override void WriteArgs(BinaryWriter writer)
        {
            writer.Write(_unknown1);
            WriteAddress(writer, _targetOffset);
        }

        protected override void DumpArgs(TextWriter writer)
        {
            writer.Write(' '); writer.Write(_unknown1.ToHex());
            writer.Write(' '); writer.Write(_targetOffset);
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
