using System.IO;
using CSYetiTools.Base;
using CSYetiTools.Base.IO;

namespace CSYetiTools.VnScripts;

public class FixedLengthCode : OpCode
{
    protected byte[] Args;

    public FixedLengthCode(int argLength)
    {
        Args = new byte[argLength];
    }

    protected override string CodeName => "code-" + Code.ToString("X02");

    public override int GetArgLength(IBinaryStream stream)
        => Args.Length;

    protected override void ReadArgs(IBinaryStream reader)
    {
        Args = reader.ReadBytesExact(Args.Length);
    }

    protected override void WriteArgs(IBinaryStream writer)
    {
        writer.Write(Args);
    }

    protected override void DumpArgs(TextWriter writer)
    {
        foreach (var arg in Args) {
            writer.Write(' ');
            writer.Write(arg.ToHex());
        }
    }
}
