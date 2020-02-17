using System.IO;

namespace CsYetiTools.VnScripts
{
    public class FixedLengthStringCode : StringCode
    {
        public short Short1 { get; set; }

        public short Short2 { get; set; }

        public FixedLengthStringCode(byte code) : base(code) { }

        public override int ArgLength
            => 4 + ContentLength;

        protected override void ReadArgs(BinaryReader reader)
        {
            Short1 = reader.ReadInt16();
            Short2 = reader.ReadInt16();
            ReadString(reader);
        }

        protected override void WriteArgs(BinaryWriter writer)
        {
            writer.Write(Short1);
            writer.Write(Short2);
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
