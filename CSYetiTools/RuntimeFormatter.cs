using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace CSYetiTools
{
    /// <summary>
    /// A simple runtime string formatter.
    /// </summary>
    /// <example>
    /// <code>
    /// var format = "key = {Key,3}, value = {Value:X2}";
    /// RuntimeFormatter.Format(format, new { Key = "x", Value = 42 }) // "key =   x, value = 2A"
    /// 
    /// var formatter = new RuntimeFormatter(format);
    /// formatter.Format(new { Key = "y", Value = 31 })                // "key =   y, value = 1F"
    /// 
    /// format.RuntimeFormat(new { Key = "z", Value = 62 })            // "key =   z, value = 3E"
    /// </code>
    /// </example>
    public class RuntimeFormatter
    {
        private abstract class Segment
        {
            public abstract void Shoot(StringBuilder builder, Func<string, object?> getter);
        }

        private class StringSegment : Segment
        {
            public string content;
            public StringSegment(string content)
            {
                this.content = content;
            }

            public override void Shoot(StringBuilder builder, Func<string, object?> getter)
            {
                builder.Append(content);
            }
        }

        private class ObjectSegment : Segment
        {
            public string key;

            public int width;

            public bool leftJustify;

            public string? format;

            public ObjectSegment(string key, int width, bool leftJustify, string? format)
            {
                this.key = key;
                this.width = width;
                this.leftJustify = leftJustify;
                this.format = format;
            }

            public override void Shoot(StringBuilder builder, Func<string, object?> getter)
            {
                var arg = getter(key);

                var s = arg is IFormattable formattableArg
                    ? formattableArg.ToString(format, null)
                    : arg?.ToString()
                    ?? string.Empty;

                int pad = width - s.Length;

                if (!leftJustify && pad > 0) builder.Append(' ', pad);
                builder.Append(s);
                if (leftJustify && pad > 0) builder.Append(' ', pad);
            }
        }

        private List<Segment> _segments = new List<Segment>();

        public RuntimeFormatter(string format)
        {
            int pos = 0;
            int len = format.Length;
            char ch = '\x0';

            var builder = new StringBuilder();

            while (true)
            {
                int p = pos;
                int i = pos;
                while (pos < len)
                {
                    ch = format[pos];

                    pos++;
                    if (ch == '}')
                    {
                        if (pos < len && format[pos] == '}') // Treat as escape character for }}
                            pos++;
                        else
                            throw FormatError();
                    }

                    if (ch == '{')
                    {
                        if (pos < len && format[pos] == '{') // Treat as escape character for {{
                            pos++;
                        else
                        {
                            // enter {} scope
                            _segments.Add(new StringSegment(builder.ToString()));
                            builder = new StringBuilder();
                            pos--;
                            break;
                        }
                    }

                    builder.Append(ch);
                }

                if (pos == len) break;
                pos++;
                if (pos == len) throw FormatError();
                ch = format[pos];
                if (ch == ',' || ch == ':' || ch == '}') throw FormatError();
                
                var keyBuilder = new StringBuilder();
                do
                {
                    keyBuilder.Append(ch);
                    pos++;
                    if (pos == len) throw FormatError();
                    ch = format[pos];
                } while (ch != ',' && ch != ':' && ch != '}');

                while (pos < len && (ch = format[pos]) == ' ') pos++;
                bool leftJustify = false;
                int width = 0;
                if (ch == ',')
                {
                    pos++;
                    while (pos < len && format[pos] == ' ') pos++;

                    if (pos == len) throw FormatError();
                    ch = format[pos];
                    if (ch == '-')
                    {
                        leftJustify = true;
                        pos++;
                        if (pos == len) throw FormatError();
                        ch = format[pos];
                    }
                    if (ch < '0' || ch > '9') throw FormatError();
                    do
                    {
                        width = width * 10 + ch - '0';
                        pos++;
                        if (pos == len) throw FormatError();
                        ch = format[pos];
                    } while (ch >= '0' && ch <= '9' && width < 1000000);
                }

                while (pos < len && (ch = format[pos]) == ' ') pos++;
                
                StringBuilder? fmtBuilder = null;
                if (ch == ':')
                {
                    pos++;
                    p = pos;
                    i = pos;
                    while (true)
                    {
                        if (pos == len) throw FormatError();
                        ch = format[pos];
                        pos++;
                        if (ch == '{')
                        {
                            if (pos < len && format[pos] == '{')  // Treat as escape character for {{
                                pos++;
                            else
                                throw FormatError();
                        }
                        else if (ch == '}')
                        {
                            if (pos < len && format[pos] == '}')  // Treat as escape character for }}
                                pos++;
                            else
                            {
                                pos--;
                                break;
                            }
                        }

                        if (fmtBuilder == null)
                        {
                            fmtBuilder = new StringBuilder();
                        }
                        fmtBuilder.Append(ch);
                    }
                }
                if (ch != '}') throw FormatError();
                pos++;

                _segments.Add(new ObjectSegment(keyBuilder.ToString(), width, leftJustify, fmtBuilder?.ToString()));

            }
        }

        private FormatException FormatError()
        {
            return new FormatException("Invalid format string");
        }

        public string Format<T>(IDictionary<string, T> dict)
        {
            return Format(key => (object?)dict[key]);
        }

        public string Format(Func<string, object?> valueGetter)
        {
            var builder = new StringBuilder();
            foreach (var seg in _segments)
            {
                seg.Shoot(builder, valueGetter);
            }
            return builder.ToString();
        }

        public string Format<T>(T obj)
        {
            var type = obj!.GetType();
            return Format(key => {
                var prop = type.GetProperty(key);
                if (prop != null) return prop.GetValue(obj);

                var field = type.GetField(key);
                if (field != null) return field.GetValue(obj);

                throw FormatError();
            });
        }

        public static string Format<T>(string format, IDictionary<string, T> dict)
            => new RuntimeFormatter(format).Format(dict);
        
        public static string Format(string format, Func<string, object?> valueGetter)
            => new RuntimeFormatter(format).Format(valueGetter);

        public static string Format<T>(string format, T obj)
            => new RuntimeFormatter(format).Format(obj);
    }

    public static class RuntimeFormatterExtensions
    {
        public static string RuntimeFormat<T>(this string format, IDictionary<string, T> dict)
            => new RuntimeFormatter(format).Format(dict);
        
        public static string RuntimeFormat(this string format, Func<string, object?> valueGetter)
            => new RuntimeFormatter(format).Format(valueGetter);

        public static string RuntimeFormat<T>(this string format, T obj)
            => new RuntimeFormatter(format).Format(obj);
    }
}
