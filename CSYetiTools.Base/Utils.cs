using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Threading.Tasks;
using YamlDotNet.Core.Events;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace CSYetiTools.Base;

public static class Utils
{
    public static readonly Encoding Cp932 = CodePagesEncodingProvider.Instance.GetEncoding(932, new EncoderExceptionFallback(), new DecoderExceptionFallback())
        ?? throw new InvalidOperationException("Cannot get encoding of code page 932");
    public static readonly Encoding Cp936 = CodePagesEncodingProvider.Instance.GetEncoding(936, new EncoderExceptionFallback(), new DecoderExceptionFallback())
        ?? throw new InvalidOperationException("Cannot get encoding of code page 936");
    public static readonly Encoding Utf8 = new UTF8Encoding(/*encoderShouldEmitUTF8Identifier: */false, /*throwOnInvalidBytes: */ true);

    public static void PrintColored(ConsoleColor color, string msg)
    {
        var oldColor = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.WriteLine(msg);
        Console.ForegroundColor = oldColor;
    }

    public static void PrintWarning(string error)
    {
        PrintColored(ConsoleColor.Yellow, error);
    }

    public static void PrintError(string error)
    {
        PrintColored(ConsoleColor.Red, error);
    }

    public static readonly JsonSerializerSettings JsonSettings = new() {
        ContractResolver = new DefaultContractResolver {
            NamingStrategy = new SnakeCaseNamingStrategy(false, false)
        },
        Formatting = Formatting.Indented,
        NullValueHandling = NullValueHandling.Ignore
    };

    private static readonly JsonSerializer DefaultJsonSerializer = JsonSerializer.CreateDefault(JsonSettings);

    public static string SerializeJson(object obj)
    {
        using var writer = new StringWriter(new StringBuilder(256));
        DefaultJsonSerializer.Serialize(writer, obj);
        return writer.ToString();
    }

    public static void SerializeJsonToFile(object obj, FilePath path)
    {
        using var writer = Utils.CreateStreamWriter(path);
        DefaultJsonSerializer.Serialize(writer, obj);
        writer.WriteLine();
    }

    public static T DeserializeJson<T>(string value)
        => JsonConvert.DeserializeObject<T>(value, JsonSettings) ?? throw new JsonSerializationException("Cannot deserialize object");

    public static T DeserializeJsonFromFile<T>(FilePath path)
        => DeserializeJson<T>(File.ReadAllText(path, Utf8));

    private class YamlDocEmitter : IEmitter
    {
        private readonly TextWriter _writer;
        private readonly Emitter _inner;
        private readonly string? _startComment;
        private readonly bool _mappingEndNewLine;

        public YamlDocEmitter(TextWriter writer, string? startComment = null, bool mappingEndNewLine = false)
        {
            _writer = writer;
            _inner = new Emitter(writer);
            _startComment = startComment;
            _mappingEndNewLine = mappingEndNewLine;
        }
        public void Emit(ParsingEvent @event)
        {
            if (@event is DocumentStart) {
                if (_startComment != null) {
                    _inner.Emit(new Comment(_startComment, false));
                    _writer.WriteLine();
                }
                _inner.Emit(new DocumentStart());
            }
            else if (@event is MappingEnd) {
                _inner.Emit(@event);
                if (_mappingEndNewLine) {
                    _writer.WriteLine();
                }
            }
            else if (@event is DocumentEnd) {
                _inner.Emit(new DocumentEnd(true));
            }
            else {
                _inner.Emit(@event);
            }
        }
    }

    public static void WriteYamlDocument(string path, YamlDocument doc, string? startComment, bool mappingEndNewLine)
    {
        using var writer = new StreamWriter(path, false, Utf8) { NewLine = "\n" };
        var yamlStream = new YamlStream(doc);
        yamlStream.Save(new YamlDocEmitter(writer, startComment, mappingEndNewLine), false);
    }

    public static T DeserializeYamlFromFile<T>(FilePath path)
        => new YamlDotNet.Serialization.Deserializer().Deserialize<T>(File.ReadAllText(path, Utf8));

    public static T SafeCastEnum<T>(object value) where T : Enum
    {
        if (Enum.IsDefined(typeof(T), value)) return (T)value;
        throw new ArgumentException($"Cannot cast {value} to {typeof(T).Name}");
    }

    public static int GetStringZByteCount(string str)
    {
        return Cp932.GetByteCount(str) + 1;
    }

    public static byte ParseByte(string input)
    {
        input = input.Trim(' ');
        if (input.StartsWith("0x") || input.StartsWith(@"\x")) {
            input = input[2..];
        }

        if (input.Length != 2) {
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
        if (enumerator.MoveNext()) {
            builder.Append(enumerator.Current.ToHex());
        }
        while (enumerator.MoveNext()) {
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
        if (span.Length > 0) {
            builder.Append(span[0].ToHex());
        }
        for (var i = 1; i < span.Length; ++i) {
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

        if (lineOffset != extraStart) {
            for (; position < extraStart - lineOffset; ++position) {
                buffer[position] = "  ";
            }
        }
        foreach (var b in bytes) {
            buffer[position++] = b.ToHex();
            if (position == rowSize) {
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
        if (dirInfo.Exists) {
            foreach (var file in dirInfo.GetFiles()) file.Delete();
            foreach (var subDir in dirInfo.GetDirectories()) subDir.Delete(true);
        }
        else {
            dirInfo.Create();
        }
    }

    public static StreamWriter CreateStreamWriter(string path)
    {
        return new(path, false, Utf8) { NewLine = "\n" };
    }

    public static void WriteAllText(string path, string text)
    {
        using var writer = CreateStreamWriter(path);
        writer.Write(text);
    }

    public static void WriteAllLines(string path, IEnumerable<string> lines)
    {
        using var writer = CreateStreamWriter(path);
        foreach (var line in lines) writer.WriteLine(line);
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
        while (true) {
            var read1 = stream1.Read(buffer1, 0, buffer1.Length);
            stream2.Read(buffer2, 0, buffer2.Length);
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

    public static IEnumerable<int> Range(int count)
        => Enumerable.Range(0, count);

    public static IEnumerable<int> Range(int start, int end)
        => Enumerable.Range(start, end - start);

    public static IEnumerable<int> Range(int start, int end, int step)
    {
        if (start == end) {

        }
        else if (start < end && step > 0) {
            while (start < end) {
                yield return start;
                start += step;
            }
        }
        else if (start > end && step < 0) {
            while (start > end) {
                yield return start;
                start += step;
            }
        }
        else {
            throw new ArgumentException($"Invalid range ({start}, {end}, {step})");
        }
    }

    public static IEnumerable<T> Repeat<T>(T elem, int count)
    {
        for (var i = 0; i < count; ++i) yield return elem;
    }

    public static IEnumerable<T> Generate<T>(Func<T> func, int count)
    {
        for (var i = 0; i < count; ++i) yield return func();
    }

    public static IEnumerable<T> Generate<T>(Func<int, T> func, int count)
    {
        for (var i = 0; i < count; ++i) yield return func(i);
    }

    public static List<T> ParallelGenerateList<T>(Func<int, T> func, int count)
    {
        var list = new List<T>(count);
        for (var i = 0; i < count; ++i) { list.Add(default!); }
        Parallel.For(0, count, i => {
            list[i] = func(i);
        });
        return list;
    }

    public static T[] ParallelGenerateArray<T>(Func<int, T> func, int count)
    {
        var array = new T[count];
        Parallel.For(0, count, i => {
            array[i] = func(i);
        });
        return array;
    }

    private static readonly byte[] MsbTable =
    {
            0,1,2,2,3,3,3,3,4,4,4,4,4,4,4,4,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,
            6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,
            7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,
            7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,
            8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,
            8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,
            8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,
            8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8
        };

    public static int Msb(int x)
    {
        var a = x <= 0xffff ? (x <= 0xff ? 0 : 8) : (x <= 0xffffff ? 16 : 24);
        return MsbTable[x >> a] + a;
    }

}
