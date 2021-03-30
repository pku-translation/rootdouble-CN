using CSYetiTools.Base.IO;

namespace CSYetiTools.VnScripts
{
    // Content/Offset is public for en->jp replacement
    //    poor design :(
    public abstract class StringCode : OpCode
    {
        public string Content { get; set; } = "";

        public LabelReference ContentOffset { get; set; } = new LabelReference();

        public bool IsOffset { get; set; }

        protected int GetContentLength(IBinaryStream stream)
            => IsOffset ? 4 : stream.GetStringZByteCount(Content);

        protected void ReadString(IBinaryStream reader)
        {
            if (IsOffset) {
                ContentOffset = new LabelReference(Offset, reader.ReadInt32LE());
            }
            else {
                Content = reader.ReadStringZ();
            }
        }

        protected void WriteString(IBinaryStream writer)
        {
            if (IsOffset) {
                WriteAddress(writer, ContentOffset);
            }
            else {
                writer.WriteStringZ(Content);
            }
        }

        protected string ContentToString()
            => "\"" + Content.Replace("\"", "\\\"").Replace("\n", "\\n") + "\"";
        //=> IsOffset ? $"0x{ContentOffset:X08} \"{Content}\"" : ("\"" + Content + "\"");
    }
}
