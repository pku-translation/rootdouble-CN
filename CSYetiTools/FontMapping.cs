using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CsYetiTools.FileTypes;
using CsYetiTools.VnScripts;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.Primitives;
using static CsYetiTools.Utils;

namespace CsYetiTools
{
    public class FontMapping : System.Text.Encoding
    {
        private static string InvalidCharMessage = "Only BMP scalar values supported";

        public const int CellSize = 48;
        public const int DbXCount = 128;
        public const int DbYCount = 64;
        public const int MbXCount = 64;
        public const int MbYCount = 4;
        public const int DbWidth = DbXCount * CellSize;
        public const int DbHeight = DbYCount * CellSize;
        public const int MbWidth = MbXCount * CellSize;
        public const int MbHeight = MbYCount * CellSize;

        public static IReadOnlyList<int> RawSjisTable = (
            from leading in Range(0x81, 0xa0).Concat(Range(0xe0, 0xec))
            from tail in Range(0x40, 0x7f).Concat(Range(0x80, 0xfd))
            select ((leading << 8) | tail)
        ).Take(128 * 64).ToList().AsReadOnly();

        public static IReadOnlyList<int?> RawSjisReverseTable;

        static FontMapping()
        {
            var list = Enumerable.Repeat<int?>(null, ReverseMax - ReverseMin).ToList();
            for (int i = 0; i < RawSjisTable.Count; ++i)
            {
                if (RawSjisTable[i] < ReverseMin)
                {
                    throw new ArgumentException($"{RawSjisTable[i]} < {ReverseMin}");
                }
                if (RawSjisTable[i] >= ReverseMax)
                {
                    throw new ArgumentException($"{RawSjisTable[i]} >= {ReverseMax}");
                }
                list[RawSjisTable[i] - ReverseMin] = i;
            }
            RawSjisReverseTable = list.AsReadOnly();
        }

        private char[] _chars;

        private int?[] _reverseTable;

        private int _minChar;

        private int _maxChar;


        private const int ReverseMin = 0x8000;
        private const int ReverseMax = 0xED00;

        private const int SingleByteMax = 0x80;

        public FontMapping(IEnumerable<char> chars)
        {
            _chars = chars.Where(c => c >= SingleByteMax).Distinct().OrderBy(c => c).ToArray();
            _minChar = _chars[0];
            _maxChar = _chars[^1] + 1;

            if (_chars.Length > RawSjisTable.Count)
            {
                throw new ArgumentException($"Too many chars: {_chars.Length} > {RawSjisTable.Count}");
            }

            _reverseTable = new int?[_maxChar - _minChar];
            foreach (var (i, c) in _chars.WithIndex())
            {
                if (char.IsSurrogate(c)) throw new ArgumentException(InvalidCharMessage);

                _reverseTable[c - _minChar] = i;
            }
        }

        private void ForeachChars(Image img, Action<char, PointF, IImageProcessingContext> operation)
        {
            var i = 0;
            foreach (var y in Range(0, DbYCount))
            {
                foreach (var x in Range(0, DbXCount))
                {
                    var chr = _chars[i];

                    if (chr == '\u3000')
                    {
                        ++i;
                        continue; // why ImageSharp handle this as unknown symbol?
                    }

                    img.Mutate(ctx => operation(chr, new PointF(48 * x + 24, 48 * y + 24), ctx));
                    if (++i >= _chars.Length) return;
                }
            }
        }

        public Image<Bgra32> GenerateTexture()
        {
            var img = new Image<Bgra32>(DbWidth, DbHeight);
            try
            {
                var fonts = new FontCollection();
                var fontFamily = fonts.Install("fonts/SourceHanSansSC-Regular.ttf");
                var font = new Font(fontFamily, 42);
                var renderOptions = new RendererOptions(font);
                var textGraphicsOptions = new TextGraphicsOptions();
                textGraphicsOptions.HorizontalAlignment = HorizontalAlignment.Center;
                textGraphicsOptions.VerticalAlignment = VerticalAlignment.Center;

                var outlinePen = new Pen(Color.FromRgba(255, 255, 255, 115), 3.6f);
                
                ForeachChars(img, (chr, point, ctx) => ctx.DrawText(textGraphicsOptions, chr.ToString(), font, outlinePen, point));

                img.Mutate(ctx => ctx.GaussianBlur(0.5f));

                ForeachChars(img, (chr, point, ctx) => ctx.DrawText(textGraphicsOptions, chr.ToString(), font, Color.White, point));

                return img;
            }
            catch
            {
                img.Dispose();
                throw;
            }
        }

        public override int GetByteCount(char[] chars, int index, int count)
        {
            var result = 0;
            for (int i = 0; i < count; ++i)
            {
                var c = chars[index];
                if (char.IsSurrogate(c)) throw new EncoderFallbackException(InvalidCharMessage);
                if (c < SingleByteMax)
                {
                    ++result;
                }
                else if (c < _minChar || c >= _maxChar || _reverseTable[c - _minChar] == null)
                {
                    throw new EncoderFallbackException($"Char [{c}] is not in the mapping");
                }
                else
                {
                    result += 2;
                }
                ++index;
            }
            return result;
        }

        public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
        {
            var index = byteIndex;

            for (int i = 0; i < charCount; ++i)
            {
                var c = chars[charIndex];
                if (char.IsSurrogate(c)) throw new EncoderFallbackException(InvalidCharMessage);
                if (c < SingleByteMax)
                {
                    bytes[index++] = (byte)c;
                }
                else if (c < _minChar || c >= _maxChar || !(_reverseTable[c - _minChar] is int revIndex))
                {
                    throw new EncoderFallbackException($"Char [{c}] is not in the mapping");
                }
                else
                {
                    var code = RawSjisTable[revIndex];
                    bytes[index++] = (byte)(code >> 8);
                    bytes[index++] = (byte)(code & 0xFF);
                }
                ++charIndex;
            }
            return index - byteIndex;
        }

        public override int GetCharCount(byte[] bytes, int index, int count)
        {
            var result = 0;
            int? leading = null;
            for (int i = 0; i < count; ++i)
            {
                var b = bytes[index];
                if (leading != null)
                {
                    var composed = (leading.Value << 8) | b;
                    if (composed < ReverseMin || composed >= ReverseMax || RawSjisReverseTable[composed - ReverseMin] == null)
                    {
                        throw new DecoderFallbackException($"Cannot decode bytes {composed.ToHex()} at {index - 1}, out of range", bytes[(i - 1)..(i + 1)], index - 1);
                    }
                    ++result;
                    leading = null;
                }
                else if (b < SingleByteMax)
                {
                    ++result;
                }
                else
                {
                    leading = b;
                }
                ++index;
            }
            if (leading != null) throw new DecoderFallbackException($"Unexpected ending", new[] { (byte)leading.Value }, index - 1);
            return result;
        }

        public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
        {
            var index = charIndex;
            int? leading = null;
            for (int i = 0; i < byteCount; ++i)
            {
                var b = bytes[byteIndex];
                if (leading != null)
                {
                    var composed = (leading.Value << 8) | b;
                    if (composed < ReverseMin || composed >= ReverseMax || !(RawSjisReverseTable[composed - ReverseMin] is int reverseIndex))
                    {
                        throw new DecoderFallbackException($"Cannot decode bytes {composed.ToHex()}, out of range", bytes[(i - 1)..(i + 1)], byteIndex - 1);
                    }
                    chars[index++] = _chars[reverseIndex];
                    leading = null;
                }
                else if (b < SingleByteMax)
                {
                    chars[index++] = (char)b;
                }
                else
                {
                    leading = b;
                }
                ++byteIndex;
            }
            if (leading != null) throw new DecoderFallbackException($"Unexpected ending", new[] { (byte)leading.Value }, index - 1);
            return index - charIndex;
        }

        public override int GetMaxByteCount(int charCount)
        {
            return charCount * 2;
        }

        public override int GetMaxCharCount(int byteCount)
        {
            return byteCount;
        }

        public IEnumerable<char> GetAllCharacters()
        {
            return _chars;
        }

        public static FontMapping CreateFrom(SnPackage package, ExecutableStringPeeker peeker)
        {
            throw new NotImplementedException();
        }
    }
}