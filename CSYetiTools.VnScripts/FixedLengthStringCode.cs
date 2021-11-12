using System.IO;
using CSYetiTools.Base.IO;

namespace CSYetiTools.VnScripts;

public abstract class FixedLengthStringCode : StringCode
{
    public short Short1 { get; set; }

    public short Short2 { get; set; }

    public override int GetArgLength(IBinaryStream stream)
        => 4 + GetContentLength(stream);

    protected override void ReadArgs(IBinaryStream reader)
    {
        Short1 = reader.ReadInt16LE();
        Short2 = reader.ReadInt16LE();
        ReadString(reader);
    }

    protected override void WriteArgs(IBinaryStream writer)
    {
        writer.WriteLE(Short1);
        writer.WriteLE(Short2);
        WriteString(writer);
    }

    protected override void DumpArgs(TextWriter writer)
    {
        writer.Write(' '); writer.Write(Short1);
        writer.Write(' '); writer.Write(Short2);
        writer.Write(' '); writer.Write(ContentToString());
    }
}
