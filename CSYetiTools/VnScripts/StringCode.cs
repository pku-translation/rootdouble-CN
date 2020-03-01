using System.IO;
using CsYetiTools.IO;

namespace CsYetiTools.VnScripts
{
    // Content/Offset is public for en->jp replacement
    //    poor design :(
    public abstract class StringCode : OpCode
    {
        protected StringCode(byte code) : base(code) { }

        public string Content { get; set; } = "";

        public CodeAddressData ContentOffset { get; set; } = new CodeAddressData();

        public bool IsOffset { get; set; }

        protected int ContentLength
            => IsOffset ? 4 : Utils.GetStringZByteCount(Content);
        
        protected void ReadString(IBinaryStream reader)
        {
            if (IsOffset)
            {
                ContentOffset = new CodeAddressData(Offset, reader.ReadInt32LE());
            }
            else
            {
                Content = reader.ReadStringZ();
            }
        }

        protected void WriteString(IBinaryStream writer)
        {
            if (IsOffset)
            {
                WriteAddress(writer, ContentOffset);
            }
            else
            {
                writer.WriteStringZ(Content);
            }
        }

        protected string ContentToString()
            => "\"" + Content.Replace("\"", "\\\"").Replace("\n", "\\n") + "\"";
            //=> IsOffset ? $"0x{ContentOffset:X08} \"{Content}\"" : ("\"" + Content + "\"");
    }
}