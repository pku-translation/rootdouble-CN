using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace CsYetiTools
{
    public static class Extensions
    {
        private static char[] Hex1Table = "0123456789ABCDEF".ToArray();

        private static string[] Hex2Table = Enumerable.Range(0, 256).Select(i =>
        {
            Span<char> span = stackalloc char[2] { Hex1Table[i / 16], Hex1Table[i % 16] };
            return new string(span);
        }).ToArray();

        public static string ToHex(this byte b)
        {
            return Hex2Table[b];
            //return b.ToString("X02");
        }

        public static string ToHex(this short s)
        {
            // Span<char> chars = stackalloc char[4];
            // chars[0] = Hex1Table[(s & 0xF000) >> 12];
            // chars[1] = Hex1Table[(s & 0x0F00) >> 8];
            // chars[2] = Hex1Table[(s & 0x00F0) >> 4];
            // chars[3] = Hex1Table[s & 0x000F];
            // var str = new string(chars);
            // System.Diagnostics.Debug.Assert(str == s.ToString("X04"));
            // return str;
            return s.ToString("X04");
        }

        public static string ToHex(this int i)
        {
            // Span<byte> span = stackalloc byte[sizeof(int)];
            // BitConverter.TryWriteBytes(span, i);
            // return BytesToHex(span);
            return i.ToString("X08");
        }

        public static string ToBin(this byte b)
        {
            Span<char> chrs = stackalloc char[8];
            var mask = 0b10000000;
            for (int i = 0; i < 8; ++i)
            {
                chrs[i] = (b & mask) != 0 ? '1' : '0';
                mask >>=1;
            }
            return new string(chrs);
        }

        public static void Times(this int times, Action action)
        {
            for (int i = 0; i < times; ++i) action();
        }

        public static int Peek(this Stream stream)
        {
            if (!stream.CanSeek) return -1;

            Span<byte> oneByte = stackalloc byte[1];
            var pos = stream.Position;
            int num = stream.Read(oneByte);
            stream.Position = pos;
            return num != 0 ? oneByte[0] : -1;
        }
        
        public static IEnumerable<(int index, T element)> WithIndex<T>(this IEnumerable<T> source)
        {
            int i = 0;
            foreach (var elem in source) yield return (i++, elem);
        }

        public static void ForEach<T>(this IEnumerable<T> elems, Action<T> action)
        {
            foreach (var elem in elems) action(elem);
        }

        public static IEnumerable<T> ConcatOne<T>(this IEnumerable<T> elems, T elem)
        {
            foreach (var e in elems) yield return e;
            yield return elem;
        }

        public static IEnumerable<T> Ordered<T>(this IEnumerable<T> elems)
            => elems.OrderBy(e => e);

        public static string Join(this string separator, IEnumerable<string> values)
            => string.Join(separator, values);

        public static string Join(this char separator, IEnumerable<string> values)
            => string.Join(separator, values);

        public static IEnumerable<byte> StreamAsIEnumerable(this Stream stream)
        {
            if (stream != null)
            {
                int c;
                while ((c = stream.ReadByte()) != -1) yield return (byte)c;
            }
        }

        public static void RemoveAt<T>(this List<T> src, params int[] indices)
        {
            // straight-forward
            for (int i = 0; i < indices.Length; ++i)
            {
                src.RemoveAt(indices[i] - i);
            }
        }

        // public static IEnumerable<(T1, T2)> ZipTuple<T1, T2>(this IEnumerable<T1> seq1, IEnumerable<T2> seq2)
        // {
        //     using var e1 = seq1.GetEnumerator();
        //     using var e2 = seq2.GetEnumerator();
        //     while (e1.MoveNext() && e2.MoveNext())
        //         yield return (e1.Current, e2.Current);
        // }
        // public static IEnumerable<(T1, T2, T3)> ZipTuple<T1, T2, T3>(this IEnumerable<T1> seq1, IEnumerable<T2> seq2, IEnumerable<T3> seq3)
        // {
        //     using var e1 = seq1.GetEnumerator();
        //     using var e2 = seq2.GetEnumerator();
        //     using var e3 = seq3.GetEnumerator();
        //     while (e1.MoveNext() && e2.MoveNext() && e3.MoveNext())
        //         yield return (e1.Current, e2.Current, e3.Current);
        // }

        public static Match? TryMatch(this Regex regex, string content)
        {
            try
            {
                return regex.Match(content);
            }
            catch
            {
                return null;
            }
        }
        
        public static void WriteBE(this BinaryWriter writer, Int16 i)
        {
            writer.Write((byte)(i >> 8));
            writer.Write((byte)(i & 0xFF));
        }

        public static void WriteBE(this BinaryWriter writer, UInt16 i)
        {
            writer.Write((byte)(i >> 8));
            writer.Write((byte)(i & 0xFF));
        }

        public static void WriteBE(this BinaryWriter writer, Int32 i)
        {
            writer.Write((byte)((i >> 0x18) & 0xFF));
            writer.Write((byte)((i >> 0x10) & 0xFF));
            writer.Write((byte)((i >> 0x08) & 0xFF));
            writer.Write((byte)(i & 0xFF));
        }
        
        public static void WriteBE(this BinaryWriter writer, UInt32 i)
        {
            writer.Write((byte)((i >> 0x18) & 0xFF));
            writer.Write((byte)((i >> 0x10) & 0xFF));
            writer.Write((byte)((i >> 0x08) & 0xFF));
            writer.Write((byte)(i & 0xFF));
        }

        public static void WriteBE(this BinaryWriter writer, Int64 i)
        {
            writer.Write((byte)((i >> 0x38) & 0xFF));
            writer.Write((byte)((i >> 0x30) & 0xFF));
            writer.Write((byte)((i >> 0x28) & 0xFF));
            writer.Write((byte)((i >> 0x20) & 0xFF));
            writer.Write((byte)((i >> 0x18) & 0xFF));
            writer.Write((byte)((i >> 0x10) & 0xFF));
            writer.Write((byte)((i >> 0x08) & 0xFF));
            writer.Write((byte)(i & 0xFF));
        }
        
        public static void WriteBE(this BinaryWriter writer, UInt64 i)
        {
            writer.Write((byte)((i >> 0x38) & 0xFF));
            writer.Write((byte)((i >> 0x30) & 0xFF));
            writer.Write((byte)((i >> 0x28) & 0xFF));
            writer.Write((byte)((i >> 0x20) & 0xFF));
            writer.Write((byte)((i >> 0x18) & 0xFF));
            writer.Write((byte)((i >> 0x10) & 0xFF));
            writer.Write((byte)((i >> 0x08) & 0xFF));
            writer.Write((byte)(i & 0xFF));
        }

        public static void Seek(this BinaryReader reader, long offset, SeekOrigin origin = SeekOrigin.Begin)
        {
            reader.BaseStream.Seek(offset, origin);
        }
        public static void Seek(this BinaryReader reader, ulong offset, SeekOrigin origin = SeekOrigin.Begin)
        {
            reader.BaseStream.Seek(checked((long)offset), origin);
        }
    }
}