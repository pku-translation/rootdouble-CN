using Untitled.Sexp;
using Untitled.Sexp.Attributes;

namespace CsYetiTools.VnScripts
{
    [SexpCustomConverter(typeof(ExtraDialogConverter))]
    public class ExtraDialogCode : DynamicLengthStringCode
    {
        protected class ExtraDialogConverter : Converter
        {
            protected override DynamicLengthStringCode CreateInstance()
                => new ExtraDialogCode();

            public override object? ToObject(SValue value)
            {
                return base.ToObject(value.AsPair().Cdr);
            }

            public override SValue ToValue(object obj)
            {
                var code = (ExtraDialogCode)obj;
                return SValue.Cons(SValue.Symbol(code.IsCharacter ? "character" : "dialog"), base.ToValue(obj));
            }
        }

        // if Short1 == -1, it's used as normal dialog
        // else if Short1 == 0x0D, it's character name
        // else it's special string
        public ExtraDialogCode() : base(0x47) { }

        public bool IsDialog
            => Short1 == -1;

        public bool IsCharacter
            => Short1 == 0x0D;

        // protected override void DumpArgs(System.IO.TextWriter writer)
        // {
        //     writer.Write(IsCharacter ? " <Character>" : " <ExDialog>");
        //     base.DumpArgs(writer);
        // }
    }
}