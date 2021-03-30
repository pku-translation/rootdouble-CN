using System.IO;
using CSYetiTools.Base.IO;

namespace CSYetiTools.VnScripts
{
    public class TitleCode : StringCode
    {
        protected override string CodeName => "title";

        public override int GetArgLength(IBinaryStream stream)
            => IsOffset ? 4 : 9 + GetContentLength(stream);

        protected override void ReadArgs(IBinaryStream reader)
        {
            if (IsOffset) {
                ReadString(reader);
            }
            else {
                var startOffset = reader.ReadInt32LE() - Offset;
                if (startOffset != 10) throw new InvalidDataException($"code [55] start {startOffset} != 10");
                var unknown = reader.ReadByte();
                if (unknown != 0x01) throw new InvalidDataException("code [55] separator != 0x01");
                var endOffset = reader.ReadInt32LE() - Offset;
                ReadString(reader);
                if (endOffset != 10 + reader.GetStringZByteCount(Content)) {
                    throw new InvalidDataException($"code [55] end {endOffset} != 10 + strlen {reader.GetStringZByteCount(Content)}");
                }
            }
        }

        protected override void WriteArgs(IBinaryStream writer)
        {
            if (IsOffset) {
                WriteString(writer);
            }
            else {
                var startOffset = (int)writer.Position + 9;
                var endOffset = (int)writer.Position + 9 + writer.GetStringZByteCount(Content);
                writer.WriteLE(startOffset);
                writer.Write((byte)0x01);
                writer.WriteLE(endOffset);
                WriteString(writer);
            }
        }

        protected override void DumpArgs(TextWriter writer)
        {
            writer.Write(' '); writer.Write(ContentToString());
        }
    }
}
