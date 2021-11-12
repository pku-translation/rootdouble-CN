using System.IO;
using CSYetiTools.Base.IO;

namespace CSYetiTools.VnScripts;

// 0x00 when allowed (confusing :( )
public sealed class ReturnCode : OpCode
{
    protected override string CodeName => "return";

    public override int GetArgLength(IBinaryStream stream)
        => 0;

    protected override void ReadArgs(IBinaryStream reader) { }

    protected override void WriteArgs(IBinaryStream writer) { }

    protected override void DumpArgs(TextWriter writer) { }

}
