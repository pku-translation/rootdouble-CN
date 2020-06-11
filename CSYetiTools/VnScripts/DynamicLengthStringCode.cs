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
    public abstract class DynamicLengthStringCode : StringCode
    {
        protected abstract class Converter : SexpConverter
        {
            public override bool CanConvert(Type type)
                => typeof(DynamicLengthStringCode).IsAssignableFrom(type);

            protected abstract DynamicLengthStringCode CreateInstance();

            public override object? ToObject(SValue value)
            {
                var opCode = CreateInstance();
                var (car, cdr) = value.AsPair();
                opCode.Short1 = (short)car;
                (car, cdr) = cdr.AsPair();
                if (cdr.IsNull)
                {
                    opCode.Content = car.AsString();
                }
                else
                {
                    opCode.Short2 = (short)car;
                    opCode.Content = cdr.AsPair().Cdr.AsString();
                }
                return opCode;
            }

            public override SValue ToValue(object obj)
            {
                var opCode = (DynamicLengthStringCode)obj;
                var builder = new ListBuilder();
                builder.Add(new SValue(opCode.Short1, new NumberFormatting { Radix = NumberRadix.Hexadecimal }));
                if (opCode._extralength == 4)
                {
                    builder.Add(new SValue(opCode.Short2, new NumberFormatting { Radix = NumberRadix.Hexadecimal }));
                }
                builder.Add(opCode.Content);
                return builder.ToValue();
            }
        }

        public short Short1;
        public short Short2;
        
        private int _extralength;

        public DynamicLengthStringCode(byte code) : base(code) { }

        public override int GetArgLength(IBinaryStream stream)
            => _extralength + GetContentLength(stream);

        protected override void ReadArgs(IBinaryStream reader)
        {
            Short1 = reader.ReadInt16LE();
            Short2 = reader.ReadInt16LE();
            if (Short1 == -1 || Short2 == -1)
            {
                _extralength = 4;
            }
            else
            {
                reader.Seek(-2);
                _extralength = 2;
            }
            ReadString(reader);
        }

        protected override void WriteArgs(IBinaryStream writer)
        {
            writer.WriteLE(Short1);
            if (_extralength == 4) writer.WriteLE(Short2);
            WriteString(writer);
        }

        // protected override void DumpArgs(TextWriter writer)
        // {
        //     writer.Write(' '); writer.Write(Short1);
        //     if (_extralength == 4)
        //     {
        //         writer.Write(' '); writer.Write(Short2);
        //     }
        //     writer.Write(' ');
        //     writer.Write(ContentToString());
        // }
    }
}
