using System.IO;

namespace CsYetiTools.VnScripts
{
    public class TextAreaCode : OpCode
    {

        // 0x0A, 100, 24, 1080, 96 (JP)

        // 0x0A, 100, 16, 1080, 96 (EN)

        // 000A: dialog
        // 000D: character name
        // 0046: BC1
        // 0047: BC2

        public short AreaIndex { get; set; }

        public short X { get; set; }

        public short Y { get; set; }

        public short Width { get; set; }

        public short Height { get; set; }

        public TextAreaCode() : base(0x68) { }

        public bool IsEnArea
            => AreaIndex == 0x0A && X == 100 && Y == 16 && Width == 1080 && Height == 96;

        public bool IsJpArea
            => AreaIndex == 0x0A && X == 100 && Y == 24 && Width == 1080 && Height == 96;

        public override int ArgLength
            => 10;
        
        protected override void ReadArgs(BinaryReader reader)
        {
            AreaIndex = reader.ReadInt16();
            X = reader.ReadInt16();
            Y = reader.ReadInt16();
            Width = reader.ReadInt16();
            Height = reader.ReadInt16();
        }

        protected override void WriteArgs(BinaryWriter writer)
        {
            writer.Write(AreaIndex);
            writer.Write(X);
            writer.Write(Y);
            writer.Write(Width);
            writer.Write(Height);
        }

        protected override void DumpArgs(TextWriter writer)
        {
            writer.Write(' '); writer.Write(AreaIndex.ToHex());
            writer.Write(' '); writer.Write(X);
            writer.Write(' '); writer.Write(Y);
            writer.Write(' '); writer.Write(Width);
            writer.Write(' '); writer.Write(Height);
        }
    }
}