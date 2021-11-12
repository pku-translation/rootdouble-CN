using System.Collections.Generic;
using System.IO;
using CSYetiTools.Base.IO;

namespace CSYetiTools.VnScripts;

public abstract class CmpJumpCode : OpCode, IHasAddress
{
    public ScriptArgument Arg1 { get; set; } = new();

    public ScriptArgument Arg2 { get; set; } = new();

    public LabelReference TargetAddress { get; set; } = new();

    public override int GetArgLength(IBinaryStream stream)
        => 4 + 4;

    protected override void ReadArgs(IBinaryStream reader)
    {
        Arg1 = ReadArgument(reader);
        Arg2 = ReadArgument(reader);
        TargetAddress = ReadAddress(reader);
    }

    protected override void WriteArgs(IBinaryStream writer)
    {
        WriteArgument(writer, Arg1);
        WriteArgument(writer, Arg2);
        WriteAddress(writer, TargetAddress);
    }

    protected override void DumpArgs(TextWriter writer)
    {
        writer.Write(' '); writer.Write(Arg1);
        writer.Write(' '); writer.Write(Arg2);
        writer.Write(' '); writer.Write(TargetAddress);
    }

    public IEnumerable<LabelReference> GetAddresses()
    {
        yield return TargetAddress;
    }
}

public class JumpIfEqCode : CmpJumpCode
{
    protected override string CodeName => "jump-if:==";
}
public class JumpIfNotEqCode : CmpJumpCode
{
    protected override string CodeName => "jump-if:!=";
}
public class JumpIfGtCode : CmpJumpCode
{
    protected override string CodeName => "jump-if:>";
}
public class JumpIfGteCode : CmpJumpCode
{
    protected override string CodeName => "jump-if:>=";
}
public class JumpIfLtCode : CmpJumpCode
{
    protected override string CodeName => "jump-if:<";
}
public class JumpIfLteCode : CmpJumpCode
{
    protected override string CodeName => "jump-if:<=";
}
