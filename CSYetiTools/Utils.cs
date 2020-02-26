using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace CsYetiTools
{
    public static class Utils
    {
        public static readonly Encoding Cp932 = CodePagesEncodingProvider.Instance.GetEncoding(932) ?? throw new InvalidOperationException("Cannot get encoding of code page 932");

        public static readonly Encoding Utf8 = new UTF8Encoding(/*encoderShouldEmitUTF8Identifier: */false);
        
        public static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new SnakeCaseNamingStrategy(false, false)
            },
            Formatting = Formatting.Indented,
            NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore,
        };

        public static T SafeCastEnum<T>(object value) where T : Enum
        {
            if (Enum.IsDefined(typeof(T), value)) return (T)value;
            throw new ArgumentException($"Cannot cast {value} to {typeof(T).Name}");
        }

        public static string ReadStringZ(BinaryReader reader)
        {
            var bytes = new List<byte>();
            byte b;
            while ((b = reader.ReadByte()) != 0) bytes.Add(b);
            return Cp932.GetString(bytes.ToArray());
        }

        public static void WriteStringZ(BinaryWriter writer, string content)
        {
            writer.Write(Cp932.GetBytes(content));
            writer.Write((byte)0x00);
        }

        public static int GetStringZByteCount(string str)
        {
            return Cp932.GetByteCount(str) + 1;
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

        public static string BytesToHex(IEnumerable<byte> bytes)
        {
            using var enumerator = bytes.GetEnumerator();
            return BytesToHex(enumerator);
        }

        public static string BytesToHex(IEnumerator<byte> enumerator)
        {
            var builder = new StringBuilder();
            if (enumerator.MoveNext())
            {
                builder.Append(enumerator.Current.ToHex());
            }
            while (enumerator.MoveNext())
            {
                builder.Append(' ').Append(enumerator.Current.ToHex());
            }
            return builder.ToString();
        }

        public static string BytesToHex(byte[] bytes)
        {
            return BytesToHex((IEnumerable<byte>)bytes);
        }

        public static string BytesToHex(Span<byte> span)
        {
            var builder = new StringBuilder();
            if (span.Length > 0)
            {
                builder.Append(span[0].ToHex());
            }
            for (int i = 1; i < span.Length; ++i)
            {
                builder.Append(' ').Append(span[i].ToHex());
            }
            return builder.ToString();
        }

        public static IEnumerable<string> BytesToTextLines(IEnumerable<byte> bytes, int extraStart = 0, bool withHeader = true, bool withOffset = true)
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
                buffer[position++] = b.ToHex();
                if (position == rowSize)
                {
                    yield return CurrentLine();
                    lineOffset += rowSize;
                    position = 0;
                }
            }
            if (position != 0) yield return CurrentLine();
        }

        public static void CreateOrClearDirectory(string dirPath)
        {
            var dirInfo = new DirectoryInfo(dirPath);
            if (dirInfo.Exists)
            {
                foreach (var file in dirInfo.GetFiles()) file.Delete();
                foreach (var subDir in dirInfo.GetDirectories()) subDir.Delete(true);
            }
            else
            {
                dirInfo.Create();
            }
        }

        public static void Time(Action action, string format = "{0}")
        {
            var stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();
            action();
            stopwatch.Stop();
            Console.WriteLine(format, stopwatch.ElapsedMilliseconds + " ms");
        }

        public static T Time<T>(Func<T> action, string format = "{0}")
        {
            var stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();
            var result = action();
            stopwatch.Stop();
            Console.WriteLine(format, stopwatch.ElapsedMilliseconds + " ms");
            return result;
        }

        public static bool CompareStream(Stream stream1, Stream stream2)
        {
            var buffer1 = new byte[4096];
            var buffer2 = new byte[4096];
            while (true)
            {
                int read1 = stream1.Read(buffer1, 0, buffer1.Length);
                int read2 = stream2.Read(buffer2, 0, buffer2.Length);
                if (!buffer1.SequenceEqual(buffer2)) return false;
                if (read1 != buffer1.Length) break;
            }
            return true;
        }

        public static bool CompareFile(string path1, string path2)
        {
            using var file1 = File.OpenRead(path1);
            using var file2 = File.OpenRead(path2);
            if (file1.Length != file2.Length) return false;
            return CompareStream(file1, file2);
        }
    }
}