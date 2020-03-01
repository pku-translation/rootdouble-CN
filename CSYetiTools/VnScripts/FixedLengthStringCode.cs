using System.IO;
using CsYetiTools.IO;

namespace CsYetiTools.VnScripts
{
    public class FixedLengthStringCode : StringCode
    {
        public short Short1 { get; set; }

        public short Short2 { get; set; }

        public FixedLengthStringCode(byte code) : base(code) { }

        public override int ArgLength
            => 4 + ContentLength;

        protected override void ReadArgs(IBinaryStream reader)
        {
            Short1 = reader.ReadInt16LE();
            Short2 = reader.ReadInt16LE();
            ReadString(reader);
        }

        protected override void WriteArgs(IBinaryStream writer)
        {
            writer.WriteLE(Short1);
            writer.WriteLE(Short2);
            WriteString(writer);
        }

        protected override void DumpArgs(TextWriter writer)
        {
            writer.Write(' '); writer.Write(Short1);
            writer.Write(' '); writer.Write(Short2);
            writer.Write(' '); writer.Write(ContentToString());
        }
    }
}
