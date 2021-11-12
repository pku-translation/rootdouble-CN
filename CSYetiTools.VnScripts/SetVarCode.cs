using System.IO;
using CSYetiTools.Base.IO;

namespace CSYetiTools.VnScripts;

public abstract class SetVarCode : OpCode
{
    public ScriptArgument Arg { get; set; } = new();

    public ScriptArgument Value { get; set; } = new();

    protected override string CodeName => "set";

    protected abstract string OpName { get; }

    public override int GetArgLength(IBinaryStream stream)
        => 4;

    protected override void ReadArgs(IBinaryStream reader)
    {
        Arg = ReadArgument(reader);
        Value = ReadArgument(reader);
    }

    protected override void WriteArgs(IBinaryStream writer)
    {
        WriteArgument(writer, Arg);
        WriteArgument(writer, Value);
    }

    protected override void DumpArgs(TextWriter writer)
    {
        writer.Write(' '); writer.Write(Arg);
        writer.Write(' '); writer.Write(OpName);
        writer.Write(' '); writer.Write(Value);
    }
}

public class SetCode : SetVarCode
{
    protected override string OpName => "=";
}

public class AddCode : SetVarCode
{
    protected override string OpName => "+=";
}
public class SubCode : SetVarCode
{
    protected override string OpName => "-=";
}
public class MulCode : SetVarCode
{
    protected override string OpName => "*=";
}

public class DivCode : SetVarCode
{
    protected override string OpName => "/=";
}
public class ModCode : SetVarCode
{
    protected override string OpName => "%=";
}
public class AndCode : SetVarCode
{
    protected override string OpName => "&=";
}
public class OrCode : SetVarCode
{
    protected override string OpName => "|=";
}
public class RandomRangeCode : SetVarCode
{
    protected override string OpName => "= rand-range";
}
