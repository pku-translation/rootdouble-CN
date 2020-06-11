using System;
using System.IO;
using CsYetiTools.IO;
using Untitled.Sexp;
using Untitled.Sexp.Attributes;
using Untitled.Sexp.Conversion;

namespace CsYetiTools.VnScripts
{
    // 0x00 when allowed (confusing :( )
    [SexpCustomConverter(typeof(Converter))]
    public sealed class ZeroCode : OpCode
    {
        private class Converter : SexpConverter
        {
            public override bool CanConvert(Type type)
                => type == typeof(ZeroCode);

            public override object? ToObject(SValue value)
                => new ZeroCode();

            public override SValue ToValue(object obj)
                => SValue.Null;
        }
        public ZeroCode() : base(0x00) { }

        public override int GetArgLength(IBinaryStream stream)
            => 0;

        protected override void ReadArgs(IBinaryStream reader) { }

        protected override void WriteArgs(IBinaryStream writer) { }

        //protected override void DumpArgs(TextWriter writer) { }

    }
}
