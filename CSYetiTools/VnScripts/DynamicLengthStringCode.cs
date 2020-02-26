using System.IO;

namespace CsYetiTools.VnScripts
{
    public class DynamicLengthStringCode : StringCode
    {
        public short Short1;
        public short Short2;

        private int _extralength;

        public DynamicLengthStringCode(byte code) : base(code) { }

        public override int ArgLength
            => _extralength + ContentLength;

        protected override void ReadArgs(BinaryReader reader)
        {
            Short1 = reader.ReadInt16();
            Short2 = reader.ReadInt16();
            if (Short1 == -1 || Short2 == -1)
            {
                _extralength = 4;
            }
            else
            {
                reader.BaseStream.Seek(-2, SeekOrigin.Current);
                _extralength = 2;
            }
            ReadString(reader);
        }

        protected override void WriteArgs(BinaryWriter writer)
        {
            writer.Write(Short1);
            if (_extralength == 4) writer.Write(Short2);
            WriteString(writer);
        }

        protected override void DumpArgs(TextWriter writer)
        {
            writer.Write(' '); writer.Write(Short1);
            if (_extralength == 4)
            {
                writer.Write(' '); writer.Write(Short2);
            }
            writer.Write(' ');
            writer.Write(ContentToString());
        }
    }
}
