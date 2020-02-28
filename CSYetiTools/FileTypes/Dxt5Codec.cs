using System;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace CsYetiTools.FileTypes
{
    public static class Dxt5Codec
    {
        // Straight-forward implementation

        private static byte Lerp(byte a1, byte a2, double t1, double t2)
            => checked((byte)((a1 * t1 + a2 * t2) / (t1 + t2)));

        private static Bgra32 Lerp(Bgra32 c1, Bgra32 c2, double t1, double t2)
            => new Bgra32(
                r: Lerp(c1.R, c2.R, t1, t2),
                g: Lerp(c1.G, c2.G, t1, t2),
                b: Lerp(c1.B, c2.B, t1, t2),
                a: Lerp(c1.A, c2.A, t1, t2)
            );

        private static void DecodeU8Alpha(BinaryReader reader, Span<byte> span)
        {
            Span<byte> alpha = stackalloc byte[8];
            alpha[0] = reader.ReadByte();
            alpha[1] = reader.ReadByte();
            if (alpha[0] > alpha[1])
            {
                for (int i = 1; i < 7; ++i) alpha[i + 1] = Lerp(alpha[0], alpha[1], 7 - i, i);
            }
            else
            {
                for (int i = 1; i < 5; ++i) alpha[i + 1] = Lerp(alpha[0], alpha[1], 5 - i, i);
                alpha[6] = 0x00;
                alpha[7] = 0xFF;
            }
            int index = 0;
            for (int i = 0; i < 2; ++i)
            {
                int b24 = reader.ReadByte();
                b24 |= reader.ReadByte() << 8;
                b24 |= reader.ReadByte() << 16;

                for (int j = 0; j < 8; ++j)
                {
                    span[index++] = alpha[b24 & 0b111];
                    b24 >>= 3;
                }
            }
        }

        private static void DecodeBgr565(BinaryReader reader, Span<Bgra32> span)
        {
            var c565 = new Bgr565();
            Span<Bgra32> c = stackalloc Bgra32[4];
            c565.PackedValue = reader.ReadUInt16();
            c[0].FromVector4(c565.ToVector4());
            c565.PackedValue = reader.ReadUInt16();
            c[1].FromVector4(c565.ToVector4());
            c[2] = Lerp(c[0], c[1], 2, 1);
            c[3] = Lerp(c[0], c[1], 1, 2);

            var b32 = reader.ReadUInt32();
            for (int i = 0; i < 16; ++i)
            {
                span[i] = c[(int)(b32 & 0b11)];
                b32 >>= 2;
            }
        }

        public static Bgra32[] Decode(byte[] bytes, int width, int height)
        {
            var pixels = new Bgra32[width * height];
            using var reader = new BinaryReader(new MemoryStream(bytes));
            Span<byte> alphas = stackalloc byte[16];
            Span<Bgra32> colors = stackalloc Bgra32[16];

            foreach (var blockY in Utils.Range(0, height, 4))
            {
                foreach (var blockX in Utils.Range(0, width, 4))
                {
                    DecodeU8Alpha(reader, alphas);
                    DecodeBgr565(reader, colors);

                    for (int i = 0; i < 16; ++i)
                    {
                        colors[i].A = alphas[i];
                        var x = i % 4 + blockX;
                        var y = i / 4 + blockY;
                        pixels[x + y * width] = colors[i];
                    }
                }
            }

            return pixels;
        }
    }
}