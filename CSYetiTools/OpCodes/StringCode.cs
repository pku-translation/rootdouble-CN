using System.IO;
using System.Linq;

namespace CSYetiTools.OpCodes
{
    // Content/Offset is public for en->jp replacement
    //    poor design :(
    public abstract class StringCode : OpCode
    {
        protected StringCode(byte code) : base(code) { }

        public string Content { get; set; } = "";

        public CodeAddressData ContentOffset { get; set; } = new CodeAddressData();

        public bool IsOffset { get; set; }

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

        protected int ContentLength
            => IsOffset ? 4 : Utils.GetStringZByteCount(Content);

        protected byte[] ContentToBytes()
            => IsOffset ? GetBytes(ContentOffset) : Utils.GetStringZBytes(Content).ToArray();

        protected string ContentToString()
            => "\"" + Content.Replace("\"", "\\\"").Replace("\n", "\\n") + "\"";
            //=> IsOffset ? $"0x{ContentOffset:X08} \"{Content}\"" : ("\"" + Content + "\"");
    }
}