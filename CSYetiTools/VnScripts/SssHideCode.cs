using System;
using System.IO;
using CsYetiTools.IO;
using Untitled.Sexp;
using Untitled.Sexp.Attributes;
using Untitled.Sexp.Conversion;

namespace CsYetiTools.VnScripts
{
    [SexpAsList]
    public sealed class SssHideCode : FixedLengthCode
    {
        public SssHideCode() : base(0x88, 2) { }

    }
}
