using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CSYetiTools.Base;
using CSYetiTools.Commandlet.FileTypes;
using CSYetiTools.VnScripts;
using static CSYetiTools.Base.Utils;

namespace CSYetiTools.Commandlet;

public class FontMapping : Encoding
{
    private const string InvalidCharMessage = "Only BMP scalar values supported";

    public const int CellSize = 48;
    public const int DbXCount = 128;
    public const int DbYCount = 64;
    public const int MbXCount = 64;
    public const int MbYCount = 4;
    public const int DbWidth = DbXCount * CellSize;
    public const int DbHeight = DbYCount * CellSize;
    public const int MbWidth = MbXCount * CellSize;
    public const int MbHeight = MbYCount * CellSize;

    public static readonly IReadOnlyList<int> RawSjisTable = (
        from leading in Range(0x81, 0xa1).Concat(Range(0xe0, 0xec))
        from tail in Range(0x40, 0x7f).Concat(Range(0x80, 0xfd))
        select (leading << 8) | tail
    ).Take(DbXCount * DbYCount).ToList().AsReadOnly();

    // 84FC~85A0 (index: 751~847) for single-byte, banned
    private const int MbRangeStart = 751;

    private const int MbRangeEnd = 847;

    // private static int MbCodeStart = 0x84FC;
    // private static int MbCodeEnd = 0x85A0;

    private static readonly IReadOnlyList<int?> RawSjisReverseTable;

    static FontMapping()
    {
        var list = Enumerable.Repeat<int?>(null, ReverseMax - ReverseMin).ToList();
        foreach (var i in ..RawSjisTable.Count) {
            if (i >= MbRangeStart && i < MbRangeEnd) continue;
            list[RawSjisTable[i] - ReverseMin] = i;
        }
        RawSjisReverseTable = list.AsReadOnly();
    }

    private readonly char[] _chars;

    private readonly int?[] _reverseTable;

    private readonly int _minChar;

    private readonly int _maxChar;

    private const int ReverseMin = 0x8000;

    private const int ReverseMax = 0xED00;

    private const int SingleByteMax = 0x80;

    public IEnumerable<char> Chars
        => _chars.AsEnumerable();

    public FontMapping(IEnumerable<char> chars)
    {
        var charSet = chars.Where(c => c >= SingleByteMax).Distinct().OrderBy(c => c).ToArray();
        if (charSet.Length <= MbRangeStart) {
            _chars = charSet;
        }
        else {
            _chars = charSet.Take(MbRangeStart).Concat(Repeat('·', MbRangeEnd - MbRangeStart)).Concat(charSet.Skip(MbRangeStart)).ToArray();
        }

        _minChar = _chars[0];
        _maxChar = _chars[^1] + 1;

        if (_chars.Length > RawSjisTable.Count) {
            throw new ArgumentException($"Too many chars: {_chars.Length} > {RawSjisTable.Count}");
        }

        _reverseTable = new int?[_maxChar - _minChar];
        foreach (var (i, c) in _chars.WithIndex()) {
            if (char.IsSurrogate(c)) throw new ArgumentException(InvalidCharMessage);

            if (i >= MbRangeStart && i < MbRangeEnd) continue;

            _reverseTable[c - _minChar] = i;
        }

    }

    private static void ForeachChars(Image img, char[] chars, Action<char, PointF, IImageProcessingContext> operation)
    {
        var i = 0;
        foreach (var y in Range(0, DbYCount)) {
            foreach (var x in Range(0, DbXCount)) {
                var chr = chars[i];

                if (chr == '\u3000') {
                    ++i;
                    continue; // why ImageSharp handle this as unknown symbol?
                }

                img.Mutate(ctx => operation(chr, new PointF(CellSize * x, CellSize * y), ctx));
                if (++i >= chars.Length) return;
            }
        }
    }

    public Image<Bgra32> GenerateTexture(bool drawGlyphBorder = false)
    {
        var img = new Image<Bgra32>(DbWidth, DbHeight);
        try {
            var fonts = new FontCollection();
            var fontFamily = fonts.Add("fonts/SourceHanSansSC-Regular.ttf");
            var font = new Font(fontFamily, 40);

            var drawingOptions = new DrawingOptions {
                GraphicsOptions = { Antialias = true },
            };

            var textOptions = new TextOptions(font) {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };

            var outlinePen = new Pen(Color.FromRgba(255, 255, 255, 115), 3.6f);

            ForeachChars(img, _chars, (chr, point, ctx) => ctx.DrawText(drawingOptions, new TextOptions(textOptions) { Origin = point + new PointF(22, 20) }, chr.ToString(), brush: null, pen: outlinePen));

            img.Mutate(ctx => ctx.GaussianBlur(0.5f));

            var whiteBrush = new SolidBrush(Color.White);
            ForeachChars(img, _chars, (chr, point, ctx) => ctx.DrawText(drawingOptions, new TextOptions(textOptions) { Origin = point + new PointF(22, 20) }, chr.ToString(), brush: whiteBrush, pen: null));

            if (drawGlyphBorder) {
                ForeachChars(img, _chars, (_, point, ctx) => {
                    ctx.DrawPolygon(drawingOptions, Color.White, 1.0f
                        , point + new PointF(0, 1)
                        , point + new PointF(44, 1)
                        , point + new PointF(44, 45)
                        , point + new PointF(0, 45)
                    );
                });
            }

            return img;
        }
        catch {
            img.Dispose();
            throw;
        }
    }

    public Image<Bgra32> GenerateCodeTestTexture()
    {
        var img = new Image<Bgra32>(DbWidth, DbHeight);
        try {
            var fonts = new FontCollection();
            var fontFamily = fonts.Add("fonts/SourceHanSansSC-Regular.ttf");
            var font = new Font(fontFamily, 25);

            var drawingOptions = new DrawingOptions {
                GraphicsOptions = { Antialias = true },  
            };

            var textOptions = new TextOptions(font) {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };

            var outlinePen = new Pen(Color.FromRgba(255, 255, 255, 115), 1f);

            var whiteBrush = new SolidBrush(Color.White);

            ForeachChars(img, _chars, (chr, point, ctx) => {
                ctx.DrawPolygon(drawingOptions, outlinePen
                    , point + new PointF(0, 1)
                    , point + new PointF(45, 1)
                    , point + new PointF(45, 46)
                    , point + new PointF(0, 46)
                );
                var s = ((short)chr).ToHex();

                var mid = point + new PointF(23, 23);

                ctx.DrawText(drawingOptions, new TextOptions(textOptions) { Origin = mid + new PointF(-10, -10) }, s[0].ToString(), brush: whiteBrush, pen: null);
                ctx.DrawText(drawingOptions, new TextOptions(textOptions) { Origin = mid + new PointF(8, -10) }, s[1].ToString(), brush: whiteBrush, pen: null);
                ctx.DrawText(drawingOptions, new TextOptions(textOptions) { Origin = mid + new PointF(-10, 10) }, s[2].ToString(), brush: whiteBrush, pen: null);
                ctx.DrawText(drawingOptions, new TextOptions(textOptions) { Origin = mid + new PointF(8, 10) }, s[3].ToString(), brush: whiteBrush, pen: null);
            });

            return img;
        }
        catch {
            img.Dispose();
            throw;
        }
    }

    public override int GetByteCount(char[] chars, int index, int count)
    {
        var result = 0;
        foreach (var _ in ..count) {
            var c = chars[index];
            if (char.IsSurrogate(c)) throw new EncoderFallbackException(InvalidCharMessage);
            if (c < SingleByteMax) {
                ++result;
            }
            else if (c < _minChar || c >= _maxChar || _reverseTable[c - _minChar] == null) {
                throw new EncoderFallbackException($"Char [{c}] is not in the mapping");
            }
            else {
                result += 2;
            }
            ++index;
        }
        return result;
    }

    public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
    {
        var index = byteIndex;

        foreach (var _ in ..charCount) {
            var c = chars[charIndex];
            if (char.IsSurrogate(c)) throw new EncoderFallbackException(InvalidCharMessage);
            if (c < SingleByteMax) {
                bytes[index++] = (byte)c;
            }
            else if (c < _minChar || c >= _maxChar || !(_reverseTable[c - _minChar] is { } revIndex)) {
                throw new EncoderFallbackException($"Char [{c}] is not in the mapping");
            }
            else {
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
        foreach (var i in ..count) {
            var b = bytes[index];
            if (leading != null) {
                var composed = (leading.Value << 8) | b;
                if (composed < ReverseMin || composed >= ReverseMax || RawSjisReverseTable[composed - ReverseMin] == null) {
                    throw new DecoderFallbackException($"Cannot decode bytes {composed.ToHex()} at {index - 1}, out of range", bytes[(i - 1)..(i + 1)], index - 1);
                }
                ++result;
                leading = null;
            }
            else if (b < SingleByteMax) {
                ++result;
            }
            else {
                leading = b;
            }
            ++index;
        }
        if (leading != null) throw new DecoderFallbackException("Unexpected ending", new[] { (byte)leading.Value }, index - 1);
        return result;
    }

    public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
    {
        var index = charIndex;
        int? leading = null;
        foreach (var i in ..byteCount) {
            var b = bytes[byteIndex];
            if (leading != null) {
                var composed = (leading.Value << 8) | b;
                if (composed < ReverseMin || composed >= ReverseMax || !(RawSjisReverseTable[composed - ReverseMin] is { } reverseIndex)) {
                    throw new DecoderFallbackException($"Cannot decode bytes {composed.ToHex()}, out of range", bytes[(i - 1)..(i + 1)], byteIndex - 1);
                }
                chars[index++] = _chars[reverseIndex];
                leading = null;
            }
            else if (b < SingleByteMax) {
                chars[index++] = (char)b;
            }
            else {
                leading = b;
            }
            ++byteIndex;
        }
        if (leading != null) throw new DecoderFallbackException("Unexpected ending", new[] { (byte)leading.Value }, index - 1);
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
