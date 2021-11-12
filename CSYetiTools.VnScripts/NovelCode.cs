using System.IO;

namespace CSYetiTools.VnScripts;

public class NovelCode : FixedLengthStringCode
{
    // Short1: may be flags
    // Short2: may be voice index, -1 indicates no voice

    protected override string CodeName => "novel";

    protected override void DumpArgs(TextWriter writer)
    {
        writer.Write(" <Novel>");
        base.DumpArgs(writer);
    }
}
