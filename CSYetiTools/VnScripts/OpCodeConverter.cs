using System;
using System.Collections.Generic;
using System.Globalization;
using Untitled.Sexp;
using Untitled.Sexp.Conversion;

namespace CsYetiTools.VnScripts
{
    public sealed class OpCodeConverter : SexpConverter
    {
        private static readonly Dictionary<Symbol, Type> ForwardTable = new Dictionary<Symbol, Type>();

        private static readonly Dictionary<Type, Symbol> BackwardTable = new Dictionary<Type, Symbol>();

        private static readonly SortedDictionary<byte, (string name, Type type)> GeneralTypeTable
            = new SortedDictionary<byte, (string name, Type type)>
            {
                [0x01] = ("jump", typeof(JumpCode)),
                [0x02] = ("script-jump", typeof(ScriptJumpCode)),
                [0x03] = ("jump", typeof(JumpCode)),
                [0x04] = ("script-jump", typeof(ScriptJumpCode)),
                [0x06] = ("addressed", typeof(PrefixedAddressCode)),
                [0x07] = ("addressed", typeof(PrefixedAddressCode)),
                [0x08] = ("addressed", typeof(PrefixedAddressCode)),
                [0x09] = ("addressed", typeof(PrefixedAddressCode)),
                [0x0A] = ("addressed", typeof(PrefixedAddressCode)),
                [0x0B] = ("addressed", typeof(PrefixedAddressCode)),
                [0x0C] = ("code-0c", typeof(OpCode_0C_0D)),
                [0x0D] = ("code-0d", typeof(OpCode_0C_0D)),
            };

        static OpCodeConverter()
        {
            Add("zero", typeof(ZeroCode));
            Add("code-0e", typeof(OpCode_0E));
            Add("debug-menu", typeof(DebugMenuCode));
            Add("dialog", typeof(DialogCode));
            Add("exdialog", typeof(ExtraDialogCode));
            Add("title", typeof(TitleCode));
            Add("text-area", typeof(TextAreaCode));
            Add("directive", typeof(DirectiveMessageCode));
            Add("novel", typeof(NovelCode));
            Add("sss-input", typeof(SssInputCode));
            Add("sss-hide", typeof(SssHideCode));
            Add("sss-flag", typeof(SssFlagCode));
        }

        private static void Add(string id, Type type)
        {
            var symbol = Symbol.FromString(id);
            ForwardTable.Add(symbol, type);
            BackwardTable.Add(type, symbol);
        }

        public override bool CanConvert(Type type)
            => type == typeof(OpCode);

        public override object? ToObject(SValue value)
        {
            throw new InvalidOperationException();
        }

        public override object? ToObjectWithTypeCheck(SValue value)
        {
            if (value.IsSymbol)
            {
                return new OpCodeLabel(value.AsSymbol().Name);
            }
            if (!value.IsPair) throw new ArgumentException($"Cannot convert {value} to OpCode");
            var (car, cdr) = value.AsPair();
            var id = car.AsSymbol();

            if (ForwardTable.TryGetValue(id, out var type))
            {
                return SexpConvert.GetConverter(type).ToObject(cdr);
            }

            var name = id.Name;
            var i = name.LastIndexOf('-');
            if (i < 0) throw new ArgumentException($"Cannot convert {value} to OpCode");
            var prefix = name[0..i];
            var code = Convert.ToByte(name[(i + 1)..^0], 16);
            if (GeneralTypeTable.TryGetValue(code, out var pair))
            {
                if (pair.name != prefix) throw new ArgumentException($"Cannot convert {value} to OpCode");
                var opCode = (OpCode)SexpConvert.GetConverter(pair.type).ToObject(cdr)!;
                opCode.Code = code;
                return opCode;
            }

            if (OpCode.FixedLengthCodeTable.TryGetValue(code, out var length))
            {
                var opCode = (OpCode)SexpConvert.GetConverter(typeof(FixedLengthCode)).ToObject(cdr)!;
                opCode.Code = code;
                return opCode;
            }

            throw new ArgumentException($"Cannot convert {value} to OpCode");
        }

        public override SValue ToValue(object obj)
        {
            throw new InvalidOperationException();
        }

        public override SValue ToValueWithTypeCheck(Type type, object? obj)
        {
            if (obj == null) return SValue.Null;

            if (obj is OpCodeLabel label)
            {
                return SValue.Symbol(label.Name);
            }
            if (obj is OpCode opCode)
            {
                try
                {
                    if (OpCode.FixedLengthCodeTable.TryGetValue(opCode.Code, out var length))
                    {
                        return SValue.Cons(
                            SValue.Symbol("code-" + opCode.Code.ToString("x02")),
                            SexpConvert.GetConverter(typeof(FixedLengthCode)).ToValue(opCode)
                        );
                    }
                    if (GeneralTypeTable.TryGetValue(opCode.Code, out var pair))
                    {
                        return SValue.Cons(
                            SValue.Symbol(pair.name + "-" + opCode.Code.ToString("x02")),
                            SexpConvert.GetConverter(pair.type).ToValue(opCode)
                        );
                    }
                    if (BackwardTable.TryGetValue(opCode.GetType(), out var name))
                    {
                        return SValue.Cons(
                            name,
                            SexpConvert.GetConverter(opCode.GetType()).ToValue(opCode)
                        );
                    }
                }
                catch (Exception exc)
                {
                    throw new InvalidCastException($"Cannot convert {obj.GetType()}", exc);
                }
            }

            throw new ArgumentException($"Cannot convert {obj.GetType()} to SValue");
        }
    }
}