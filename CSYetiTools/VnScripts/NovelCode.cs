using System.IO;
using Untitled.Sexp.Attributes;

namespace CsYetiTools.VnScripts
{
    [SexpCustomConverter(typeof(NovelCodeConverter))]
    public class NovelCode : FixedLengthStringCode
    {
        protected class NovelCodeConverter : Converter
        {
            protected override FixedLengthStringCode CreateInstance()
                => new NovelCode();
        }

        // Short1: may be flags
        // Short2: may be voice index, -1 indicates no voice
        public NovelCode() : base(0x86) { }

        // protected override void DumpArgs(TextWriter writer)
        // {
        //     writer.Write(" <Novel>");
        //     base.DumpArgs(writer);
        // }
    }
}
