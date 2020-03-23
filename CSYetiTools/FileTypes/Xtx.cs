using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CsYetiTools.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using static CsYetiTools.Utils;

namespace CsYetiTools.FileTypes
{
    public sealed class Xtx : IDisposable
    {
        public static byte[] FileTag = { (byte)'x', (byte)'t', (byte)'x', 0x00 };

        private static void CheckBytes(IEnumerable<byte> data, byte[] target, string message)
        {
            var arr = data.ToList();
            if (!arr.SequenceEqual(target))
            {
                throw new InvalidDataException(
                    $"{message}: [{BytesToHex(arr)}] != [{BytesToHex(target)}]");
            }
        }

        public int Width { get; private set; }

        public int Height { get; private set; }

        public bool IsFontAtlas { get; private set; }

        private int _alignedWidth;

        private int _alignedHeight;

        public int OffsetX { get; private set; }

        public int OffsetY { get; private set; }

        public int Format { get; private set; }

        public Image Content { get; private set; }

        public void Dispose()
        {
            Content.Dispose();
        }

        public void SaveTextureTo(FilePath path)
        {
            Content.Save(path);
        }

        public void SaveBinaryTo(FilePath path)
        {
            File.WriteAllBytes(path, ToBytes());
        }

        private static int GetX(int i, int width, byte level)
        {
            int v1 = (level >> 2) + (level >> 1 >> (level >> 2));
            int v2 = i << v1;
            int v3 = (v2 & 0x3F) + ((v2 >> 2) & 0x1C0) + ((v2 >> 3) & 0x1FFFFE00);
            return ((((level << 3) - 1) & ((v3 >> 1) ^ ((v3 ^ (v3 >> 1)) & 0xF))) >> v1)
                + ((((((v2 >> 6) & 0xFF) + ((v3 >> (v1 + 5)) & 0xFE)) & 3)
                    + (((v3 >> (v1 + 7)) % (((width + 31)) >> 5)) << 2)) << 3);
        }
        private static int GetY(int i, int width, byte level)
        {
            int v1 = (level >> 2) + (level >> 1 >> (level >> 2));
            int v2 = i << v1;
            int v3 = (v2 & 0x3F) + ((v2 >> 2) & 0x1C0) + ((v2 >> 3) & 0x1FFFFE00);
            return ((v3 >> 4) & 1)
                + ((((v3 & ((level << 6) - 1) & -0x20)
                    + ((((v2 & 0x3F)
                        + ((v2 >> 2) & 0xC0)) & 0xF) << 1)) >> (v1 + 3)) & -2)
                + ((((v2 >> 10) & 2) + ((v3 >> (v1 + 6)) & 1)
                    + (((v3 >> (v1 + 7)) / ((width + 31) >> 5)) << 2)) << 3);
        }

        private static int GetAlignedSize(int size, int format)
        {
            // var trunked = 1 << (Msb(size) - 1);
            // var aligned = trunked == size ? size : trunked << 1;
            // return aligned < 0x80 ? 0x80 : aligned;

            var cell = format switch
            {
                0 => 0x20,
                1 => 0x10,
                2 => 0x80,
                _ => throw new InvalidDataException(),
            };

            var remainder = size % cell;
            if (remainder == 0) return size;

            return size + (cell - remainder);
        }

        private Image<Bgra32> DecodeFormat0(IBinaryStream reader)
        {
            var data = reader.ReadBytesExact(_alignedWidth * _alignedHeight * 4);
            var pixels = new Bgra32[Width * Height];

            foreach (var i in Range(_alignedWidth * _alignedHeight))
            {
                var x = GetX(i, _alignedWidth, 4);
                var y = GetY(i, _alignedWidth, 4);
                if (x >= Width || y >= Height) continue;

                var src = i * 4;
                pixels[x + y * Width] = new Bgra32(
                    b: data[src + 3],
                    g: data[src + 2],
                    r: data[src + 1],
                    a: data[src]
                );
            }
            return Image.LoadPixelData(pixels, Width, Height);
        }

        private byte[] EncodeFormat0()
        {
            throw new NotImplementedException();
        }

        private Image<Bgr565> DecodeFormat1(IBinaryStream reader)
        {
            var data = reader.ReadBytesExact(_alignedWidth * _alignedHeight * 2);
            var pixels = new Bgr565[Width * Height];

            foreach (var i in Range(_alignedWidth * _alignedHeight))
            {
                var x = GetX(i, _alignedWidth, 2);
                var y = GetY(i, _alignedWidth, 2);
                if (x >= Width || y >= Height) continue;

                var src = i * 2;

                pixels[x + y * Width].PackedValue = checked((ushort)(data[src] | (data[src + 1] << 8)));
            }
            return Image.LoadPixelData(pixels, Width, Height);
        }

        private byte[] EncodeFormat1()
        {
            throw new NotImplementedException();
        }

        private Image<Bgra32> DecodeFormat2(IBinaryStream reader)
        {
            var textureWidth = _alignedWidth / 4;
            var textureHeight = _alignedHeight / 4;
            var data = reader.ReadBytesExact(_alignedWidth * _alignedHeight);
            var decrypted = new byte[data.Length];
            int src = 0;
            foreach (var i in Range(textureWidth * textureHeight))
            {
                var x = GetX(i, textureWidth, 16);
                var y = GetY(i, textureWidth, 16);
                var dst = (x + y * textureWidth) * 16;
                foreach (var j in Range(8))
                {
                    decrypted[dst + 1] = data[src];
                    decrypted[dst] = data[src + 1];
                    dst += 2;
                    src += 2;
                }
            }
            var pixels = Dxt5Codec.Decode(decrypted, _alignedWidth, _alignedHeight);

            var image = Image.LoadPixelData<Bgra32>(pixels, _alignedWidth, _alignedHeight);
            image.Mutate(x => x.Crop(Width, Height));

            return image;
        }

        private byte[] EncodeFormat2()
        {
            throw new NotImplementedException();
        }

        private const int BlockSize = 48;

        private Image<Bgra4444> DecodeFont(IBinaryStream reader)
        {
            // from https://github.com/vn-tools/arc_unpacker/issues/54
            IsFontAtlas = true;

            var data = reader.ReadBytesExact(_alignedWidth * _alignedHeight * 2);
            var encodedWidth = Width;
            Width *= 4;
            var encodedHeight = Height;
            var pixels = new Bgra4444[Width * Height];

            foreach (var i in Range(_alignedWidth * _alignedHeight))
            {
                var absX = GetX(i, _alignedWidth, 2);
                var absY = GetY(i, _alignedWidth, 2);
                if (absX >= encodedWidth || absY >= encodedHeight) continue;

                var src = i * 2;

                var blockX = (absX / BlockSize) * BlockSize;
                var blockY = (absY / BlockSize) * BlockSize;
                var x = absX % BlockSize;
                var y = absY % BlockSize;
                var targetY = blockY + y;
                var targetBase = blockX * 4 + x + targetY * Width;
                var target1 = targetBase;
                var target2 = targetBase + BlockSize;
                var target3 = targetBase + BlockSize * 2;
                var target4 = targetBase + BlockSize * 3;

                pixels[target1].PackedValue = (ushort)(0x0FFFu | ((data[src] >> 4) << 12));
                pixels[target2].PackedValue = (ushort)(0x0FFFu | ((data[src] & 0xF) << 12));
                pixels[target3].PackedValue = (ushort)(0x0FFFu | ((data[src + 1] >> 4) << 12));
                pixels[target4].PackedValue = (ushort)(0x0FFFu | ((data[src + 1] & 0xF) << 12));
            }
            return Image.LoadPixelData(pixels, Width, Height);
        }

        private byte[] EncodeFont()
        {
            var data = new byte[_alignedWidth * _alignedHeight * 2];
            var img = (Image<Bgra4444>)Content;
            var pixels = img.GetPixelSpan();
            var encodedWidth = Width / 4;
            var encodedHeight = Height;
            
            foreach (var i in Range(_alignedWidth * _alignedHeight))
            {
                var absX = GetX(i, _alignedWidth, 2);
                var absY = GetY(i, _alignedWidth, 2);
                if (absX >= encodedWidth || absY >= encodedHeight) continue;

                var src = i * 2;

                var blockX = (absX / BlockSize) * BlockSize;
                var blockY = (absY / BlockSize) * BlockSize;
                var x = absX % BlockSize;
                var y = absY % BlockSize;
                var targetY = blockY + y;
                var targetBase = blockX * 4 + x + targetY * Width;
                var target1 = targetBase;
                var target2 = targetBase + BlockSize;
                var target3 = targetBase + BlockSize * 2;
                var target4 = targetBase + BlockSize * 3;

                data[src] = (byte)(
                    ((0xF000 & pixels[target1].PackedValue) >> 8)
                  | ((0xF000 & pixels[target2].PackedValue) >> 12)
                );
                data[src + 1] = (byte)(
                    ((0xF000 & pixels[target3].PackedValue) >> 8)
                  | ((0xF000 & pixels[target4].PackedValue) >> 12)
                );
            }
            return data;
        }

        public Xtx(byte[] bytes, bool fontAtlas = false)
        {
            using var reader = new BinaryStream(bytes);

            CheckBytes(reader.ReadBytesMax(4), FileTag, "XTX tag mismatch");
            Format = reader.ReadInt32LE();

            _alignedWidth = reader.ReadInt32BE();
            _alignedHeight = reader.ReadInt32BE();
            Width = reader.ReadInt32BE();
            Height = reader.ReadInt32BE();
            OffsetX = reader.ReadInt32BE();
            OffsetY = reader.ReadInt32BE();

            if (GetAlignedSize(Width, Format) != _alignedWidth || GetAlignedSize(Height, Format) != _alignedHeight)
            {
                throw new NotSupportedException($"{Format} ({Width:X}x{Height:X}) -> ({_alignedWidth:X}x{_alignedHeight:X}), not ({GetAlignedSize(Width, Format):X}x{GetAlignedSize(Height, Format):X})");
            }

            Content = Format switch
            {
                0 => DecodeFormat0(reader),
                1 => fontAtlas ? (Image)DecodeFont(reader) : (Image)DecodeFormat1(reader),
                2 => DecodeFormat2(reader),
                _ => throw new NotSupportedException($"Xtx format {Format} not supported"),
            };
        }

        private Xtx(Image image, int format, bool fontAtlas = false, int offsetX = 0, int offsetY = 0)
        {
            Format = format;
            IsFontAtlas = fontAtlas;
            if (fontAtlas)
            {
                if (Width % BlockSize != 0 && Height % BlockSize != 0)
                {
                    throw new InvalidDataException("Font width/height is not multiple of 48");
                }
            }
            switch (format)
            {
                case 0:
                    if (fontAtlas) throw new ArgumentException("Font atlas format must be 1");
                    Content = image is Image<Bgra32> ? image : image.CloneAs<Bgra32>();
                    break;
                case 1:
                    if (fontAtlas)
                    {
                        Content = image is Image<Bgra4444> ? image : image.CloneAs<Bgra4444>();
                    }
                    else
                    {
                        Content = image is Image<Bgr565> ? image : image.CloneAs<Bgr565>();
                    }
                    break;
                case 2:
                    if (fontAtlas) throw new ArgumentException("Font atlas format must be 1");
                    Content = image is Image<Bgra32> ? image : image.CloneAs<Bgra32>();
                    break;
                default:
                    throw new NotSupportedException($"Xtx format {Format} not supported");
            }
            Width = image.Width;
            Height = image.Height;
            _alignedWidth = fontAtlas ? Width / 4 : GetAlignedSize(Width, format);
            _alignedHeight = fontAtlas ? Height : GetAlignedSize(Height, format);
            OffsetX = offsetX;
            OffsetY = offsetY;
        }

        public Xtx(Image image, int format, int offsetX = 0, int offsetY = 0)
            : this(image, format, fontAtlas: false, offsetX, offsetY)
        { }

        public static Xtx CreateFont(Image image, int offsetX = 0, int offsetY = 0)
            => new Xtx(image, format: 1, fontAtlas: true, offsetX, offsetY);

        public byte[] ToBytes()
        {
            using var writer = new BinaryStream();

            writer.Write(FileTag);
            writer.WriteLE(Format);

            writer.WriteBE(_alignedWidth);
            writer.WriteBE(_alignedHeight);
            writer.WriteBE(IsFontAtlas ? Width / 4 : Width);
            writer.WriteBE(Height);
            writer.WriteBE(OffsetX);
            writer.WriteBE(OffsetY);

            writer.Write(Format switch
            {
                0 => EncodeFormat0(),
                1 => IsFontAtlas ? EncodeFont() : EncodeFormat1(),
                2 => EncodeFormat2(),
                _ => throw new NotSupportedException($"Xtx format {Format} not supported"),
            });

            return writer.ToBytes();
        }

    }
}