using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace CsYetiTools.FileTypes
{
    public class Xtx : IDisposable
    {
        public static byte[] FileTag = { (byte)'x', (byte)'t', (byte)'x', 0x00 };

        private static void CheckBytes(IEnumerable<byte> data, byte[] target, string message)
        {
            var arr = data.ToList();
            if (!arr.SequenceEqual(target))
            {
                throw new InvalidDataException(
                    $"{message}: [{Utils.BytesToHex(arr)}] != [{Utils.BytesToHex(target)}]");
            }
        }

        public int Width { get; private set; }

        public int Height { get; private set; }

        private int _alignedWidth;

        private int _alignedHeight;

        public int OffsetX { get; private set; }

        public int OffsetY { get; private set; }

        public int Format { get; private set; }

        public object Content { get; private set; }

        public void Dispose()
        {
            if (Format == 0)
                ((Image<Bgra32>)Content).Dispose();
            else if (Format == 1)
                ((Image<Bgr565>)Content).Dispose();
            else if (Format == 2)
                ((Image<Bgra32>)Content).Dispose();
            else
                throw new InvalidDataException($"invalid format-type {Format}");
        }

        public void SaveTo(string path)
        {
            if (Format == 0)
                ((Image<Bgra32>)Content).Save(path);
            else if (Format == 1)
                ((Image<Bgr565>)Content).Save(path);
            else if (Format == 2)
                ((Image<Bgra32>)Content).Save(path);
            else
                throw new InvalidDataException($"invalid format-type {Format}");
        }

        public Xtx(byte[] bytes)
        {
            using var ms = new MemoryStream(bytes);
            using var reader = new BinaryReader(ms);

            CheckBytes(reader.ReadBytes(4), FileTag, "XTX tag mismatch");
            Format = reader.ReadInt32();

            _alignedWidth = reader.ReadBEInt32();
            _alignedHeight = reader.ReadBEInt32();
            Width = reader.ReadBEInt32();
            Height = reader.ReadBEInt32();
            OffsetX = reader.ReadBEInt32();
            OffsetY = reader.ReadBEInt32();

            Content = Format switch
            {
                0 => DecodeFormat0(reader),
                1 => DecodeFormat1(reader),
                2 => DecodeFormat2(reader),
                _ => throw new NotSupportedException($"Xtx format {Format} not supported"),
            };
        }

        private static long GetX(int i, long width, byte level)
        {
            int v1 = (level >> 2) + (level >> 1 >> (level >> 2));
            int v2 = i << v1;
            int v3 = (v2 & 0x3F) + ((v2 >> 2) & 0x1C0) + ((v2 >> 3) & 0x1FFFFE00);
            return ((((level << 3) - 1) & ((v3 >> 1) ^ ((v3 ^ (v3 >> 1)) & 0xF))) >> v1)
                + ((((((v2 >> 6) & 0xFF) + ((v3 >> (v1 + 5)) & 0xFE)) & 3)
                    + (((v3 >> (v1 + 7)) % (((width + 31)) >> 5)) << 2)) << 3);
        }
        private static long GetY(int i, long width, byte level)
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

        private Image<Bgra32> DecodeFormat0(BinaryReader reader)
        {
            var data = reader.ReadBytes(_alignedWidth * _alignedHeight * 4);
            var pixels = new Bgra32[Width * Height];

            foreach (var i in Utils.Range(_alignedWidth * _alignedHeight))
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

        private Image<Bgr565> DecodeFormat1(BinaryReader reader)
        {
            var data = reader.ReadBytes(_alignedWidth * _alignedHeight * 2);
            var pixels = new Bgr565[Width * Height];
            
            foreach (var i in Utils.Range(_alignedWidth * _alignedHeight))
            {
                var x = GetX(i, _alignedWidth, 2);
                var y = GetY(i, _alignedWidth, 2);
                if (x >= Width || y >= Height) continue;
                
                var src = i * 2;

                pixels[x + y * Width].PackedValue = checked((ushort)(data[src] | (data[src + 1] << 8)));
            }
            return Image.LoadPixelData(pixels, Width, Height);
        }

        private Image<Bgra32> DecodeFormat2(BinaryReader reader)
        {
            var textureWidth = _alignedWidth / 4;
            var textureHeight = _alignedHeight / 4;
            var data = reader.ReadBytesExact(_alignedWidth * _alignedHeight);
            var decrypted = new byte[data.Length];
            int src = 0;
            foreach (var i in Utils.Range(textureWidth * textureHeight))
            {
                var x = GetX(i, textureWidth, 16);
                var y = GetY(i, textureWidth, 16);
                var dst = (x + y * textureWidth) * 16;
                foreach (var j in Utils.Range(8))
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

    }
}