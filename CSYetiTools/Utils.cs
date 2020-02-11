using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CSYetiTools
{
    public static class Utils
    {
        private static readonly Encoding Cp932 = CodePagesEncodingProvider.Instance.GetEncoding(932) ?? throw new InvalidOperationException("Cannot get encoding of code page 932");

        public static string ReadStringZ(BinaryReader reader)
        {
            var bytes = new List<byte>();
            byte b;
            while ((b = reader.ReadByte()) != 0) bytes.Add(b);
            return Cp932.GetString(bytes.ToArray());
        }

        public static string ReadStringZ(byte[] bytes, int index)
        {
            var strBytes = new List<byte>();
            while (bytes[index] != 0x00)
            {
                strBytes.Add(bytes[index++]);
            }
            return Cp932.GetString(strBytes.ToArray());
        }

        public static int GetStringZByteCount(string str)
        {
            return Cp932.GetByteCount(str) + 1;
        }

        public static IEnumerable<byte> GetStringZBytes(string str)
        {
            foreach (var b in Cp932.GetBytes(str)) yield return b;
            yield return 0x00;
        }

        public static byte ParseByte(string input)
        {
            input = input.Trim();
            if (input.StartsWith("0x") || input.StartsWith(@"\x"))
            {
                input = input.Substring(2);
            }

            if (input.Length != 2)
            {
                throw new ArgumentException($"{input} is not a valid byte."); 
            }
            return byte.Parse(input, System.Globalization.NumberStyles.HexNumber);
        }

        private static readonly char[] WhitespaceSplitter = { ' ' };
        public static byte[] ParseBytes(string input)
        {
            return input.Split(WhitespaceSplitter, StringSplitOptions.RemoveEmptyEntries)
                .Select(ParseByte)
                .ToArray();
        }

        public static string ByteToHex(byte b)
        {
            return b.ToString("X02");
        }

        public static string BytesToHex(IEnumerable<byte> bytes)
        {
            return string.Join(' ', bytes.Select(ByteToHex));
        }

        public static IEnumerable<string> BytesToTextLines(byte[] bytes, int extraStart, bool withHeader = true, bool withOffset = true)
        {
            const int rowSize = 16;
            var header = "           " + "  ".Join(Enumerable.Range(0, rowSize).Select(n => n.ToString("X1")));
            
            if (withHeader) yield return header;
            
            var buffer = new string[rowSize];
            var lineOffset = extraStart / rowSize * rowSize;
            var position = 0;
            string CurrentLine()
            {
                if (withOffset) return lineOffset.ToString("X08") + ": " + string.Join(' ', buffer.Take(position));
                else return string.Join(' ', buffer.Take(position));
            }
            
            if (lineOffset != extraStart)
            {
                for (; position < extraStart - lineOffset; ++position)
                {
                    buffer[position] = "  ";
                }
            }
            foreach (var b in bytes)
            {
                buffer[position++] = ByteToHex(b);
                if (position == rowSize)
                {
                    yield return CurrentLine();
                    lineOffset += rowSize;
                    position = 0;
                }
            }
            if (position != 0) yield return CurrentLine();
        }

    }
}