using CsYetiTools.IO;
using Untitled.Sexp.Attributes;

namespace CsYetiTools.VnScripts
{
    // Content/Offset is public for en->jp replacement
    //    poor design :(
    public abstract class StringCode : OpCode
    {
        protected StringCode(byte code) : base(code) { }

        public string Content { get; set; } = "";

        [SexpIgnore]
        public LabelReference ContentOffset { get; set; } = new LabelReference();

        [SexpIgnore]
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
