using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CsYetiTools.IO;
using Untitled.Sexp;
using Untitled.Sexp.Attributes;
using Untitled.Sexp.Conversion;

namespace CsYetiTools.VnScripts
{
    [SexpCustomConverter(typeof(LabelReferenceConverter))]
    public class LabelReference
    {
        public class LabelReferenceConverter : SexpConverter
        {
            public override bool CanConvert(Type type)
                => type == typeof(LabelReference);

            public override object? ToObject(SValue value)
            {
                return new LabelReference{ TargetLabel = value.AsSymbol().Name };
            }

            public override SValue ToValue(object obj)
            {
                return Symbol.FromString(((LabelReference)obj).TargetLabel ?? "<null-label>");
            }
        }

        public LabelReference(int baseOffset = default, int absoluteOffset = default)
        {
            BaseOffset = baseOffset;
            AbsoluteOffset = absoluteOffset;
        }

        public string? TargetLabel { get; set; }

        [SexpIgnore]
        public int AbsoluteOffset { get; set; }

        [SexpIgnore]
        public int BaseOffset { get; set; }

        [SexpIgnore]
        public int RelativeOffset
            => AbsoluteOffset - BaseOffset;

        public override string ToString()
        {
            if (TargetLabel != null)
            {
                return $"${TargetLabel}";
            }
            return $"$0x{AbsoluteOffset:X8}";
        }
    }

    [SexpCustomConverter(typeof(OpCodeLabelConverter))]
    public sealed class OpCodeLabel : OpCode
    {
        public class OpCodeLabelConverter : SexpConverter
        {
            public override bool CanConvert(Type type)
                => type == typeof(OpCodeLabel);

            public override object? ToObject(SValue value)
            {
                return new OpCodeLabel(value.AsSymbol().Name);
            }

            public override SValue ToValue(object obj)
            {
                return Symbol.FromString(((OpCodeLabel)obj).Name);
            }
        }

        public string Name { get; set; }

        public override int GetArgLength(IBinaryStream stream) => 0;

        public OpCodeLabel(string name, int offset = 0)
        {
            Name = name;
            Offset = offset;
        }

        public override void WriteTo(IBinaryStream writer)
        { }

        protected override void ReadArgs(IBinaryStream reader)
        { }

        protected override void WriteArgs(IBinaryStream writer)
        { }
    }

    [SexpCustomConverter(typeof(OpCodeConverter))]
    public abstract class OpCode
    {
        internal static SortedDictionary<byte, int> FixedLengthCodeTable = new SortedDictionary<byte, int>
        {
            [0x05] = 0, // return? end-block?

            [0x10] = 4,
            [0x11] = 4,
            [0x12] = 4,
            [0x13] = 4,
            [0x14] = 4,
            [0x15] = 4,
            [0x16] = 4,
            [0x17] = 4,
            [0x18] = 4,
            [0x19] = 8,
            [0x1A] = 4,  // BG
            [0x1B] = 0,
            [0x1C] = 0,
            [0x1D] = 6,
            [0x1E] = 10,
            [0x1F] = 12,

            [0x20] = 6,
            [0x21] = 6,
            [0x22] = 4,
            [0x23] = 8,
            [0x24] = 6,
            [0x25] = 4,
            [0x27] = 0,
            [0x28] = 2,
            [0x2B] = 0,
            [0x2C] = 2,
            [0x2D] = 4,
            [0x2E] = 0,
            [0x2F] = 2,

            [0x30] = 10,

            [0x33] = 0,
            [0x34] = 10,
            [0x35] = 4,
            [0x36] = 2,
            [0x37] = 0,
            [0x38] = 2,
            [0x39] = 4,
            [0x3A] = 4,
            [0x3B] = 2,
            [0x3C] = 2,
            [0x3D] = 0,
            [0x3F] = 0,

            [0x42] = 8,
            [0x43] = 4,
            [0x44] = 4,  // dialog box related?

            [0x48] = 2,  // dialog box related? (area index when BC, FF FF when novel)
            [0x49] = 4,  // always FF FF FF FF
            [0x4A] = 2,
            [0x4B] = 4,
            [0x4C] = 6,
            [0x4E] = 4,
            [0x4F] = 4,

            [0x51] = 6,

            [0x59] = 0,
            [0x5A] = 0,
            [0x5E] = 2,
            [0x5F] = 0,

            [0x60] = 6,
            [0x61] = 6,
            [0x62] = 4,
            [0x63] = 10,
            [0x64] = 6,
            [0x65] = 4,
            [0x66] = 2,
            //[0x68] = 10,
            [0x69] = 2,
            [0x6A] = 4,  // always FF FF FF FF
            [0x6B] = 2,
            [0x6C] = 16,
            [0x6D] = 2,
            [0x6E] = 4,
            [0x6F] = 6,

            [0x70] = 0,
            [0x71] = 6,
            [0x72] = 4,
            [0x74] = 6,
            [0x75] = 4,
            [0x7A] = 10,
            [0x7B] = 4,
            [0x7C] = 4,
            [0x7D] = 2,
            [0x7E] = 2,
            [0x7F] = 4,

            [0x80] = 4,
            [0x81] = 2,
            [0x82] = 2,
            [0x83] = 4,

            [0x8A] = 2,
            [0x8B] = 4,
            [0x8C] = 4,
            [0x8D] = 0,
            [0x8E] = 10,
            [0x8F] = 6,

            [0x91] = 8,
        };

        private static OpCode CreateOpCode(byte op)
        {
            if (FixedLengthCodeTable.TryGetValue(op, out var length))
            {
                return new FixedLengthCode(op, length);
            }
            return op switch
            {
                0x00 => new ZeroCode(),  // empty block

                0x01 => new JumpCode(op),            // jump to address?
                0x02 => new ScriptJumpCode(0x02),       // script jump?
                0x03 => new JumpCode(op),            // jump to address?
                0x04 => new ScriptJumpCode(0x04),       // script jump or else?
                // 0x05 => new FixedLengthCode(op, 0),     // return? end-block?
                0x06 => new PrefixedAddressCode(op), // invoke?
                0x07 => new PrefixedAddressCode(op),
                0x08 => new PrefixedAddressCode(op),
                0x09 => new PrefixedAddressCode(op),
                0x0A => new PrefixedAddressCode(op), // scope?
                0x0B => new PrefixedAddressCode(op), // scope?

                0x0C => new OpCode_0C_0D(op),           // scope with sub codes?
                0x0D => new OpCode_0C_0D(op),           // scope with sub codes?

                0x0E => new OpCode_0E(),                // scoped by 0C/0D when count == 0x80CB ?
                                                        // all these scopes are: [0D] 15 80 0x${code +2} and [0C] 5B 80 0x${code +2}
                                                        // and [0E]s are always [0E] 80 80 CB 80

                0x32 => new DebugMenuCode(),            // debug menu?

                0x45 => new DialogCode(),               // dialog
                0x47 => new ExtraDialogCode(),          // character name

                0x55 => new TitleCode(),                // title

                0x68 => new TextAreaCode(),

                0x85 => new DirectiveMessageCode(),      // directive message?
                                                         // [85] 0A 00 FF FF is message-box?
                0x86 => new NovelCode(),                 // novel

                0x87 => new SssInputCode(),              // センシズ受付開始？
                0x88 => new SssHideCode(),               // センシズ受付終了？
                0x89 => new SssFlagCode(),               // センシズフラッグ？

                _ => throw new InvalidDataException($"Unknown opcode {op:X02}")
            };
        }

        public static OpCode GetNextCode(IBinaryStream reader, IReadOnlyList<OpCode> prevCodes, bool isStringPooled)
        {
            var offset = (int)reader.Position;
            if (reader.Position == reader.Length) throw new OpCodeParseException("Stream is empty", "");

            var op = reader.ReadByte();

            try
            {
                var opCode = CreateOpCode(op);

                opCode.Offset = offset;
                opCode.Index = prevCodes.Count;

                if (opCode is OpCode_0E scopedCode)
                {
                    if (prevCodes.Count > 0
                        && prevCodes.Last() is OpCode_0C_0D scopeCode
                        && scopeCode.TargetOffset.AbsoluteOffset != 0)
                    {
                        scopedCode.TargetEndOffset = scopeCode.TargetOffset.AbsoluteOffset;
                    }
                }
                else if (opCode is StringCode strCode)
                {
                    strCode.IsOffset = isStringPooled;
                }
                opCode.ReadArgs(reader);

#if DEBUG
                // check parse result
                if (!(opCode is TitleCode))
                {
                    reader.Seek(-opCode.GetTotalLength(reader));
                    var opBytes = reader.ReadBytesMax(opCode.GetTotalLength(reader));
                    System.Diagnostics.Debug.Assert(opBytes.SequenceEqual(opCode.ToBytes()), $"OpCode 0x{op:X02} parsing is invalid, raw=[{Utils.BytesToHex(opBytes)}, parsed=[{Utils.BytesToHex(opCode.ToBytes())}]");
                }
#endif

                return opCode;
            }
            catch (Exception exc)
            {
                reader.Position = offset;
                var buffer = reader.ReadBytesMax(64);
                var context = string.Join(Environment.NewLine, Utils.BytesToTextLines(buffer, offset));
                reader.Position = offset;
                throw new OpCodeParseException($"Error parsing code {op:X02}", context, exc);
            }
        }

        [SexpIgnore]
        public byte Code { get; set; }

        [SexpIgnore]
        public int Offset { get; set; }

        [SexpIgnore]
        public int Index { get; set; }

        public abstract int GetArgLength(IBinaryStream stream);

        public int GetTotalLength(IBinaryStream stream)
            => GetArgLength(stream) + 1;

        public byte[] ToBytes(Encoding? encoding = null)
        {
            using var stream = new BinaryStream(encoding ?? Utils.Cp932);
            WriteTo(stream);
            return stream.ToBytes();
        }

        public virtual void WriteTo(IBinaryStream writer)
        {
            writer.Write(Code);
            WriteArgs(writer);
        }

        protected OpCode(byte code = 0)
        {
            Code = code;
        }

        public override string ToString()
            => SexpConvert.Serialize<OpCode>(this);

        protected LabelReference ReadAddress(IBinaryStream reader)
        {
            return new LabelReference(Offset, reader.ReadInt32LE());
        }

        protected void WriteAddress(IBinaryStream writer, LabelReference address)
        {
            if (address.BaseOffset != Offset)
            {
                throw new InvalidOperationException($"Writing address data with {nameof(address.BaseOffset)}(0x{address.BaseOffset:X08}) != {Offset}");
            }
            writer.WriteLE(address.AbsoluteOffset);
        }

        protected abstract void ReadArgs(IBinaryStream reader);

        protected abstract void WriteArgs(IBinaryStream writer);

    }
}