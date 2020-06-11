using System;
using System.IO;
using CsYetiTools.IO;
using Untitled.Sexp;
using Untitled.Sexp.Attributes;
using Untitled.Sexp.Conversion;
using Untitled.Sexp.Formatting;
using Untitled.Sexp.Utilities;

namespace CsYetiTools.VnScripts
{
    public class FixedLengthStringCode : StringCode
    {
        protected abstract class Converter : SexpConverter
        {
            public override bool CanConvert(Type type)
                => typeof(FixedLengthStringCode).IsAssignableFrom(type);

            protected abstract FixedLengthStringCode CreateInstance();

            public override object? ToObject(SValue value)
            {
                var opCode = CreateInstance();
                var (car, cdr) = value.AsPair();
                opCode.Short1 = (short)car;
                (car, cdr) = cdr.AsPair();
                opCode.Short2 = (short)car;
                (car, cdr) = cdr.AsPair();
                opCode.Content = cdr.AsString();
                return opCode;
            }

            public override SValue ToValue(object obj)
            {
                var opCode = (FixedLengthStringCode)obj;
                var builder  =new ListBuilder();
                builder.Add(new SValue(opCode.Short1, new NumberFormatting{ Radix = NumberRadix.Hexadecimal}));
                builder.Add(new SValue(opCode.Short2, new NumberFormatting{ Radix = NumberRadix.Hexadecimal}));
                builder.Add(opCode.Content);
                return builder.ToValue();
            }
        }

        public short Short1 { get; set; }

        public short Short2 { get; set; }

        public FixedLengthStringCode(byte code) : base(code) { }

        public override int GetArgLength(IBinaryStream stream)
            => 4 + GetContentLength(stream);

        protected override void ReadArgs(IBinaryStream reader)
        {
            Short1 = reader.ReadInt16LE();
            Short2 = reader.ReadInt16LE();
            ReadString(reader);
        }

        protected override void WriteArgs(IBinaryStream writer)
        {
            writer.WriteLE(Short1);
            writer.WriteLE(Short2);
            WriteString(writer);
        }

        // protected override void DumpArgs(TextWriter writer)
        // {
        //     writer.Write(' '); writer.Write(Short1);
        //     writer.Write(' '); writer.Write(Short2);
        //     writer.Write(' '); writer.Write(ContentToString());
        // }
    }
}
