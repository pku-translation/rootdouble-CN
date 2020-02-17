using System.IO;

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
        
        protected void ReadString(BinaryReader reader)
        {
            if (IsOffset)
            {
                ContentOffset = new CodeAddressData(_offset, reader.ReadInt32());
            }
            else
            {
                Content = Utils.ReadStringZ(reader);
            }
        }

        protected void WriteString(BinaryWriter writer)
        {
            if (IsOffset)
            {
                WriteAddress(writer, ContentOffset);
            }
            else
            {
                Utils.WriteStringZ(writer, Content);
            }
        }

        protected string ContentToString()
            => "\"" + Content.Replace("\"", "\\\"").Replace("\n", "\\n") + "\"";
            //=> IsOffset ? $"0x{ContentOffset:X08} \"{Content}\"" : ("\"" + Content + "\"");
    }
}