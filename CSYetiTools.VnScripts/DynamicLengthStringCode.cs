using System.IO;
using CSYetiTools.Base.IO;

namespace CSYetiTools.VnScripts;

public abstract class DynamicLengthStringCode : StringCode
{
    public short Short1;
    public short Short2;

    private int _extralength;

    public override int GetArgLength(IBinaryStream stream)
        => _extralength + GetContentLength(stream);

    protected override void ReadArgs(IBinaryStream reader)
    {
        Short1 = reader.ReadInt16LE();
        Short2 = reader.ReadInt16LE();
        if (Short1 == -1 || Short2 == -1) {
            _extralength = 4;
        }
        else {
            reader.Seek(-2);
            _extralength = 2;
        }
        ReadString(reader);
    }

    protected override void WriteArgs(IBinaryStream writer)
    {
        writer.WriteLE(Short1);
        if (_extralength == 4) writer.WriteLE(Short2);
        WriteString(writer);
    }

    protected override void DumpArgs(TextWriter writer)
    {
        writer.Write(' '); writer.Write(Short1);
        if (_extralength == 4) {
            writer.Write(' '); writer.Write(Short2);
        }
        writer.Write(' ');
        writer.Write(ContentToString());
    }
}
