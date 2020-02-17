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

        private static string[] Hex2Table = Enumerable.Range(0, 256).Select(i => {
            Span<char> span = stackalloc char[2] { Hex1Table[i / 16], Hex1Table[i % 16] };
            return new string(span);
        }).ToArray();

        public static string ToHex(this byte b)
        {
            var s = b.ToString("X02");
            System.Diagnostics.Debug.Assert(s == Hex2Table[b]);
            return s;
        }
        
        public static string ToHex(this int i)
        {
            // Span<byte> span = stackalloc byte[sizeof(int)];
            // BitConverter.TryWriteBytes(span, i);
            // return BytesToHex(span);
            return i.ToString("X08");
        }

        public static string ToHex(this short s)
        {
            // Span<byte> span = stackalloc byte[sizeof(short)];
            // BitConverter.TryWriteBytes(span, s);
            // return BytesToHex(span);
            return s.ToString("X04");
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

        public static int PeekByte(this BinaryReader reader)
        {
            return reader.BaseStream.Peek();
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

        public static IEnumerable<(T1, T2)> ZipTuple<T1, T2>(this IEnumerable<T1> seq1, IEnumerable<T2> seq2)
        {
            using var e1 = seq1.GetEnumerator();
            using var e2 = seq2.GetEnumerator();
            while (e1.MoveNext() && e2.MoveNext())
                yield return (e1.Current, e2.Current);
        }
        public static IEnumerable<(T1, T2, T3)> ZipTuple<T1, T2, T3>(this IEnumerable<T1> seq1, IEnumerable<T2> seq2, IEnumerable<T3> seq3)
        {
            using var e1 = seq1.GetEnumerator();
            using var e2 = seq2.GetEnumerator();
            using var e3 = seq3.GetEnumerator();
            while (e1.MoveNext() && e2.MoveNext() && e3.MoveNext())
                yield return (e1.Current, e2.Current, e3.Current);
        }

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
    }
}