using System.IO;
using CsYetiTools.IO;
using Untitled.Sexp.Attributes;

namespace CsYetiTools.VnScripts
{
    [SexpAsList]
    class TitleCode : StringCode
    {
        public TitleCode() : base(0x55) { }

        public override int GetArgLength(IBinaryStream stream)
            => IsOffset ? 4 : 9 + GetContentLength(stream);

        protected override void ReadArgs(IBinaryStream reader)
        {
            if (IsOffset)
            {
                ReadString(reader);
            }
            else
            {
                var startOffset = reader.ReadInt32LE() - Offset;
                if (startOffset != 10) throw new InvalidDataException($"code [55] start {startOffset} != 10");
                var unknown = reader.ReadByte();
                if (unknown != 0x01) throw new InvalidDataException("code [55] separator != 0x01");
                var endOffset = reader.ReadInt32LE() - Offset;
                ReadString(reader);
                if (endOffset != 10 + reader.GetStringZByteCount(Content)) throw new InvalidDataException($"code [55] end {endOffset} != 10 + strlen {reader.GetStringZByteCount(Content)}");
            }
        }

        protected override void WriteArgs(IBinaryStream writer)
        {
            if (IsOffset)
            {
                WriteString(writer);
            }
            else
            {
                var startOffset = (int)writer.Position + 9;
                var endOffset = (int)writer.Position + 9 + writer.GetStringZByteCount(Content);
                writer.WriteLE(startOffset);
                writer.Write((byte)0x01);
                writer.WriteLE(endOffset);
                WriteString(writer);
            }
        }

        // protected override void DumpArgs(TextWriter writer)
        // {
        //     writer.Write(' '); writer.Write(ContentToString());
        // }
    }
}
