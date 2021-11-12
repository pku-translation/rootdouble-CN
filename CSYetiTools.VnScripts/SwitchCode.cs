using System;
using System.Collections.Generic;
using System.IO;
using CSYetiTools.Base;
using CSYetiTools.Base.IO;

namespace CSYetiTools.VnScripts;

public sealed class SwitchCode : OpCode, IHasAddress
{
    // if _count is runtime then the size of _remain will be determined at runtime (in this game always [CB 80])

    public ScriptArgument Arg { get; set; } = new();

    public ScriptArgument Count { get; set; } = new();

    public class Branch
    {
        public ScriptArgument Arg { get; }
        public LabelReference Offset { get; }

        public Branch(ScriptArgument arg, LabelReference offset)
        {
            Arg = arg;
            Offset = offset;
        }

        public void Deconstruct(out ScriptArgument arg, out LabelReference offset)
        {
            arg = Arg;
            offset = Offset;
        }
    }

    public Branch[] Branches = Array.Empty<Branch>();

    public int TargetEndOffset { get; set; }

    protected override string CodeName => "switch";

    public override int GetArgLength(IBinaryStream stream)
        => 4 + Branches.Length * 6;

    protected override void ReadArgs(IBinaryStream reader)
    {
        Arg = ReadArgument(reader);
        Count = ReadArgument(reader);
        if (Count.IsConst) {
            Branches = new Branch[Count.ConstValue];
        }
        else {
            var size = TargetEndOffset - (int)reader.Position;
            if (size < 0 || size % 6 != 0)
                throw new ArgumentException($"Scoped op-code 0E with invalid range [{(int)reader.Position:X08}, {TargetEndOffset:X08}) (size={size})");
            Branches = new Branch[size / 6];
        }
        foreach (var i in ..Branches.Length) {
            var arg = ReadArgument(reader);
            var offset = ReadAddress(reader);
            Branches[i] = new Branch(arg, offset);
        }
    }

    protected override void WriteArgs(IBinaryStream writer)
    {
        WriteArgument(writer, Arg);
        WriteArgument(writer, Count);
        foreach (var (arg, offset) in Branches) {
            WriteArgument(writer, arg);
            WriteAddress(writer, offset);
        }
    }

    protected override void DumpArgs(TextWriter writer)
    {
        writer.Write(' '); writer.Write(Arg);
        writer.Write(' '); writer.Write(Count);
        writer.Write(" [");
        foreach (var (i, (arg, offset)) in Branches.WithIndex()) {
            writer.WriteLine();
            writer.Write("                ");
            writer.Write(i.ToString().PadLeft(3));
            writer.Write(": ");
            writer.Write(arg);
            writer.Write(": ");
            writer.Write(offset.ToString());
        }
        writer.Write(" ]");
    }

    public IEnumerable<LabelReference> GetAddresses()
    {
        foreach (var (_, offset) in Branches) {
            yield return offset;
        }
    }
}
