using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace CSYetiTools.Base
{
    public static class Extensions
    {
        private static readonly char[] Hex1Table = "0123456789ABCDEF".ToArray();

        private static readonly string[] Hex2Table = Enumerable.Range(0, 256).Select(i => {
            Span<char> span = stackalloc char[2] { Hex1Table[i / 16], Hex1Table[i % 16] };
            return new string(span);
        }).ToArray();

        public static string ToHex(this byte b)
        {
            return Hex2Table[b];
        }

        public static string ToHex(this short s)
        {
            return s.ToString("X04");
        }

        public static string ToHex(this ushort s)
        {
            return s.ToString("X04");
        }

        public static string ToHex(this int i)
        {
            return i.ToString("X08");
        }

        public static string ToHex(this uint i)
        {
            return i.ToString("X08");
        }

        public static string ToBin(this byte b)
        {
            Span<char> chrs = stackalloc char[8];
            var mask = 0b10000000;
            for (var i = 0; i < 8; ++i) {
                chrs[i] = (b & mask) != 0 ? '1' : '0';
                mask >>= 1;
            }
            return new string(chrs);
        }

        public static void Times(this int times, Action action)
        {
            for (var i = 0; i < times; ++i) action();
        }

        public static int Peek(this Stream stream)
        {
            if (!stream.CanSeek) return -1;

            Span<byte> oneByte = stackalloc byte[1];
            var pos = stream.Position;
            var num = stream.Read(oneByte);
            stream.Position = pos;
            return num != 0 ? oneByte[0] : -1;
        }

        public static IEnumerable<(int index, T element)> WithIndex<T>(this IEnumerable<T> source)
        {
            var i = 0;
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

        public static SortedDictionary<TKey, TValue> ToSortedDictionary<TKey, TValue>(this IEnumerable<TValue> elems,
            Func<TValue, TKey> keySelector) where TKey : notnull
        {
            var result = new SortedDictionary<TKey, TValue>();
            foreach (var elem in elems) result.Add(keySelector(elem), elem);
            return result;
        }

        public static string Join(this string separator, IEnumerable<string> values)
            => string.Join(separator, values);

        public static string Join(this char separator, IEnumerable<string> values)
            => string.Join(separator, values);

        public static IEnumerable<byte> StreamAsIEnumerable(this Stream stream)
        {
            int c;
            while ((c = stream.ReadByte()) != -1) yield return (byte)c;
        }

        public static void RemoveAt<T>(this List<T> src, params int[] indices)
        {
            // straight-forward
            for (var i = 0; i < indices.Length; ++i) {
                src.RemoveAt(indices[i] - i);
            }
        }

        public static Match? TryMatch(this Regex regex, string content)
        {
            try {
                return regex.Match(content);
            }
            catch {
                return null;
            }
        }

        public static IEnumerator<int> GetEnumerator(this Range range)
        {
            if (range.Start.IsFromEnd || range.End.IsFromEnd) {
                throw new ArgumentException($"Cannot enumerate range {range}");
            }

            return Enumerable.Range(range.Start.Value, range.End.Value - range.Start.Value).GetEnumerator();
        }
    }
}
