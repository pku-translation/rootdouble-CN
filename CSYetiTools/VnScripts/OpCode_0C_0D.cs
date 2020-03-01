using System.Collections.Generic;
using System.IO;
using CsYetiTools.IO;

namespace CsYetiTools.VnScripts
{
    public class OpCode_0C_0D : OpCode, IHasAddress
    {
        private byte _unknown1;
        private byte _unknown2;

        public CodeAddressData TargetOffset { get; set; } = new CodeAddressData();

        public OpCode_0C_0D(byte code) : base(code) { }

        public override int ArgLength
            => 6;

        protected override void ReadArgs(IBinaryStream reader)
        {
            _unknown1 = reader.ReadByte();
            _unknown2 = reader.ReadByte();
            TargetOffset = ReadAddress(reader);
        }

        protected override void WriteArgs(IBinaryStream writer)
        {
            writer.Write(_unknown1);
            writer.Write(_unknown2);
            WriteAddress(writer, TargetOffset);
        }

        protected override void DumpArgs(TextWriter writer)
        {
            writer.Write(' '); writer.Write(_unknown1.ToHex());
            writer.Write(' '); writer.Write(_unknown2.ToHex());
            writer.Write(' '); writer.Write(TargetOffset);
        }
        
        public IEnumerable<CodeAddressData> GetAddresses()
        {
            yield return TargetOffset;
        }
    }
}
