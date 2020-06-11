using System.Collections.Generic;
using System.IO;
using CsYetiTools.IO;
using Untitled.Sexp.Attributes;
using Untitled.Sexp.Formatting;

namespace CsYetiTools.VnScripts
{
    [SexpAsList]
    public class OpCode_0C_0D : OpCode, IHasAddress
    {
        [SexpNumberFormatting(Radix = NumberRadix.Hexadecimal)]
        private byte Unknown1 { get; set; }

        [SexpNumberFormatting(Radix = NumberRadix.Hexadecimal)]
        private byte Unknown2 { get; set; }

        public LabelReference TargetOffset { get; set; } = new LabelReference();

        public OpCode_0C_0D() { }
        public OpCode_0C_0D(byte code) : base(code) { }

        public override int GetArgLength(IBinaryStream stream)
            => 6;

        protected override void ReadArgs(IBinaryStream reader)
        {
            Unknown1 = reader.ReadByte();
            Unknown2 = reader.ReadByte();
            TargetOffset = ReadAddress(reader);
        }

        protected override void WriteArgs(IBinaryStream writer)
        {
            writer.Write(Unknown1);
            writer.Write(Unknown2);
            WriteAddress(writer, TargetOffset);
        }

        // protected override void DumpArgs(TextWriter writer)
        // {
        //     writer.Write(' '); writer.Write(_unknown1.ToHex());
        //     writer.Write(' '); writer.Write(_unknown2.ToHex());
        //     writer.Write(' '); writer.Write(TargetOffset);
        // }
        
        public IEnumerable<LabelReference> GetAddresses()
        {
            yield return TargetOffset;
        }
    }
}
