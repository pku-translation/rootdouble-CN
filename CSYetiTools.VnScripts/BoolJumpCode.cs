using System.Collections.Generic;
using System.IO;
using CSYetiTools.Base.IO;

namespace CSYetiTools.VnScripts;

public abstract class BoolJumpCode : OpCode, IHasAddress
{
    public ScriptArgument Arg { get; set; } = new();

    public LabelReference TargetAddress { get; set; } = new();

    public override int GetArgLength(IBinaryStream stream)
        => 6;

    protected override void ReadArgs(IBinaryStream reader)
    {
        Arg = ReadArgument(reader);
        TargetAddress = ReadAddress(reader);
    }

    protected override void WriteArgs(IBinaryStream writer)
    {
        WriteArgument(writer, Arg);
        WriteAddress(writer, TargetAddress);
    }

    protected override void DumpArgs(TextWriter writer)
    {
        writer.Write(' '); writer.Write(Arg.ToString());
        writer.Write(' '); writer.Write(TargetAddress);
    }

    public IEnumerable<LabelReference> GetAddresses()
    {
        yield return TargetAddress;
    }
}

public class JumpIfCode : BoolJumpCode
{
    protected override string CodeName => "jump-if";
}

public class JumpIfNotCode : BoolJumpCode
{
    protected override string CodeName => "jump-if-not";
}
