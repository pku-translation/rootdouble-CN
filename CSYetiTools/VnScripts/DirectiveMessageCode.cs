using Untitled.Sexp;
using Untitled.Sexp.Attributes;

namespace CsYetiTools.VnScripts
{
    [SexpCustomConverter(typeof(DirectiveMessageConverter))]
    public class DirectiveMessageCode : DynamicLengthStringCode
    {
        protected class DirectiveMessageConverter : Converter
        {
            protected override DynamicLengthStringCode CreateInstance()
                => new ExtraDialogCode();
        }
        
        public DirectiveMessageCode() : base(0x85) { }
    }
}