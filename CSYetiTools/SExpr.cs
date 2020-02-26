using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace CsYetiTools
{

    public enum SExprType
    {
        Null,
        String,
        Integer,
        Symbol,
        Pair,
    }

    public enum SExprIntFormat
    {
        Dec,
        Hex,
    }

    /// <summary>
    /// A simple data s-expression parser.<br />
    /// Not support '.', ''', ','
    /// </summary>
    public class SExpr
    {
        // public static void TestSExpr(string expr)
        // {
        //     var sexpr = SExpr.Parse(new StringReader(expr));
        //     Console.WriteLine(sexpr.ToString());
        // }

        private static UnicodeCategory[] NonRenderingCategories = new UnicodeCategory[] {
            UnicodeCategory.Control,
            UnicodeCategory.OtherNotAssigned,
            UnicodeCategory.Surrogate
        };

        public static string EscapeString(string input)
        {
            var builder = new StringBuilder();
            foreach (var c in input)
            {
                var escape = c switch
                {
                    '\a' => "\\a",
                    '\b' => "\\b",
                    '\t' => "\\t",
                    '\n' => "\\n",
                    '\v' => "\\v",
                    '\f' => "\\f",
                    '\r' => "\\r",
                    '\x1B' => "\\e",
                    '\"' => "\\\"",
                    '\\' => "\\\\",
                    _ => null
                };
                if (escape != null)
                {
                    builder.Append(escape);
                }
                else if (NonRenderingCategories.Contains(char.GetUnicodeCategory(c)))
                {
                    if (c <= 0xFF) builder.Append("\\x").Append(((int)c).ToString("X02"));
                    else builder.Append("\\u").Append(((int)c).ToString("X04"));
                    // ignoring surrogate-pairs
                }
                else
                {
                    builder.Append(c);
                }
            }
            return builder.ToString();
        }

        public static SExpr Null = new SExpr { Type = SExprType.Null };

        public SExprType Type { get; private set; }

        private object _value = 0;

        private SExprIntFormat _intFormat = SExprIntFormat.Dec;

        public static implicit operator SExpr(int n)
            => new SExpr { _value = n, Type = SExprType.Integer };
        public static SExpr Int(int n, SExprIntFormat format = SExprIntFormat.Dec)
            => new SExpr { _value = n, Type = SExprType.Integer, _intFormat = format };
        public static implicit operator SExpr(string s)
            => new SExpr { _value = s, Type = SExprType.String };
        public static SExpr Symbol(string s)
            => new SExpr { _value = s, Type = SExprType.Symbol };
        public static SExpr Pair(SExpr car, SExpr cdr)
            => new SExpr { _value = Tuple.Create(car, cdr), Type = SExprType.Pair };
        public static SExpr List(IEnumerable<SExpr> exprs)
        {
            var current = Null;
            foreach (var expr in exprs.Reverse())
            {
                current = Pair(expr, current);
            }
            return current;
        }
        public static SExpr List(params SExpr[] exprs)
            => List((IEnumerable<SExpr>)exprs);

        public bool IsNull
            => Type == SExprType.Null;

        public bool IsInt
            => Type == SExprType.Integer;

        public bool IsSymbol
            => Type == SExprType.Symbol;

        public bool IsString
            => Type == SExprType.String;

        public bool IsPair
            => Type == SExprType.Pair;

        public bool IsList
            => IsNull || (Type == SExprType.Pair && AsPair().cdr.IsList);

        public int AsInt()
        {
            if (!IsInt) throw new ArgumentException($"{Enum.GetName(typeof(SExprType), Type)} is not int");
            return (int)_value;
        }
        public string AsSymbol()
        {
            if (!IsSymbol) throw new ArgumentException($"{Enum.GetName(typeof(SExprType), Type)} is not symbol");
            return (string)_value;
        }
        public string AsString()
        {
            if (!IsString) throw new ArgumentException($"{Enum.GetName(typeof(SExprType), Type)} is not string");
            return (string)_value;
        }
        public (SExpr car, SExpr cdr) AsPair()
        {
            if (!IsPair) throw new ArgumentException($"{Enum.GetName(typeof(SExprType), Type)} is not pair");
            var tuple = ((Tuple<SExpr, SExpr>)_value);
            return (tuple.Item1, tuple.Item2);
        }
        public IEnumerable<SExpr> AsEnumerable()
        {
            if (!IsList) throw new InvalidOperationException("Non-list sexpr cannot as-enumerable");
            var cur = this;
            while (!cur.IsNull)
            {
                var (car, cdr) = cur.AsPair();
                yield return car;
                cur = cdr;
            }
        }
        public SExpr Car
            => AsPair().car;
        public SExpr Cdr
            => AsPair().cdr;
        public SExpr Concat(SExpr other)
            => Pair(this, other);
        public IList<SExpr> ToList()
        {
            if (!IsList) throw new ArgumentException($"{Enum.GetName(typeof(SExprType), Type)} is not list");
            var list = new List<SExpr>();
            var current = this;
            while (current != Null)
            {
                var (car, cdr) = current.AsPair();
                list.Add(car);
                current = cdr;
            }
            return list;
        }

        public override string ToString()
        {
            return ToString(string.Empty);
        }

        public string ToString(string indent)
        {
            var writer = new StringWriter();
            Dump(writer, indent);
            return writer.ToString();
        }

        public void Dump(TextWriter writer, string indent = "")
        {
            switch (Type)
            {
                case SExprType.Integer:
                    {
                        writer.Write(_intFormat switch
                        {
                            SExprIntFormat.Dec => ((int)_value).ToString(),
                            SExprIntFormat.Hex => "#x" + ((int)_value).ToString("X"),
                            _ => throw new InvalidDataException("Invalid int format type")
                        });
                        return;
                    }
                case SExprType.Symbol:
                    {
                        writer.Write((string)_value); return;
                    }
                case SExprType.String:
                    {
                        writer.Write('\"');
                        writer.Write(EscapeString((string)_value));
                        writer.Write('\"');
                        return;
                    }
                case SExprType.Pair:
                    {
                        if (IsList)
                        {
                            var list = ToList();
                            if (list.All(e => !e.IsList))
                            {
                                writer.Write('(');
                                if (list.Count > 0)
                                {
                                    list[0].Dump(writer, indent);
                                    foreach (var e in list.Skip(1))
                                    {
                                        writer.Write(' ');
                                        e.Dump(writer, indent);
                                    }
                                }
                                writer.Write(')');
                            }
                            else
                            {
                                var newPrefix = indent + "    ";
                                var sep = "\n" + newPrefix;
                                writer.Write('(');
                                if (list.Count > 0)
                                {
                                    list[0].Dump(writer, newPrefix);
                                    foreach (var e in list.Skip(1))
                                    {
                                        writer.Write(sep);
                                        e.Dump(writer, newPrefix);
                                    }
                                }
                                writer.Write(')');
                            }
                        }
                        else
                        {
                            writer.Write(indent);
                            writer.Write('(');
                            var (car, cdr) = AsPair();
                            car.Dump(writer, string.Empty);
                            writer.Write(" . ");
                            cdr.Dump(writer, indent);
                            writer.Write(')');
                        }

                        return;
                    }
                default: throw new ArgumentException($"Unknown expr type {Type}");
            }
        }

        public enum TokenType
        {
            String,
            Symbol,
            Integer,
            OpenParenthesis,
            CloseParenthesis,
        }
        public static IEnumerable<(TokenType Type, object Value)> Tokenize(TextReader reader)
        {
            char? putBackChar = null;
            IEnumerable<char> IterChars1() // ensure new line == '\n'
            {
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    foreach (var ch in line) yield return ch;
                    yield return '\n';
                }
                yield return '\0'; // boundary
            }
            IEnumerable<char> IterChars2() // can put char back
            {
                using var iter = IterChars1().GetEnumerator();
                while (true)
                {
                    if (putBackChar != null)
                    {
                        yield return putBackChar.Value;
                        putBackChar = null;
                    }
                    else
                    {
                        if (!iter.MoveNext()) yield break;
                        yield return iter.Current;
                    }
                }
            }
            using (var iter = IterChars2().GetEnumerator())
            {
                while (true)
                {
                    iter.MoveNext();
                    while (char.IsWhiteSpace(iter.Current)) iter.MoveNext();

                    char c = iter.Current;
                    switch (c)
                    {
                        case '\0':
                            yield break;
                        case '(':
                        case '[':
                        case '{':
                            yield return (TokenType.OpenParenthesis, c);
                            break;
                        case ')':
                        case ']':
                        case '}':
                            yield return (TokenType.CloseParenthesis, c);
                            break;
                        case ';':
                            {
                                iter.MoveNext();
                                while (iter.Current != '\n' && iter.Current != '\0') iter.MoveNext();
                            }
                            break;
                        default:
                            {
                                var builder = new StringBuilder();
                                if (c == '"')
                                {
                                    // string
                                    while (true)
                                    {
                                        iter.MoveNext();
                                        if (iter.Current == '\0') throw new ArgumentException("Incomplete sexpr");

                                        if (iter.Current == '"')
                                        {
                                            yield return (TokenType.String, Regex.Unescape(builder.ToString()));
                                            break;
                                        }
                                        else if (iter.Current == '\\')
                                        {
                                            iter.MoveNext();
                                            if (iter.Current == '\0') throw new ArgumentException("Incomplete sexpr");
                                            if (iter.Current == '\n') builder.Append('\n');
                                            else builder.Append('\\').Append(iter.Current);
                                        }
                                        else
                                        {
                                            builder.Append(iter.Current);
                                        }
                                    }
                                }
                                else
                                {
                                    builder.Append(c);
                                    while (true)
                                    {
                                        iter.MoveNext();
                                        bool end = false;
                                        switch (iter.Current)
                                        {
                                            case '"':
                                            case '(':
                                            case ')':
                                            case ';':
                                            case '\0':
                                                end = true;
                                                break;
                                            default:
                                                if (char.IsWhiteSpace(iter.Current)) end = true;
                                                else builder.Append(iter.Current);
                                                break;
                                        }
                                        if (end)
                                        {
                                            putBackChar = iter.Current;
                                            var str = builder.ToString();
                                            if (Regex.IsMatch(str, @"^#x[0-9a-fA-F]+$")) yield return (TokenType.Integer, Convert.ToInt32(str.Substring(2), 16));
                                            else if (Regex.IsMatch(str, @"^[0-9]+$")) yield return (TokenType.Integer, Convert.ToInt32(str));
                                            else yield return (TokenType.Symbol, str);
                                            break;
                                        }
                                    }
                                }
                            }
                            break;
                    }
                }
            }
        }

        public static SExpr Parse(IEnumerable<(TokenType type, object token)> tokens)
        {
            var stack = new Stack<List<SExpr>>();
            var parentheisStack = new Stack<char>();
            stack.Push(new List<SExpr>());

            foreach (var (type, token) in tokens)
            {
                var list = stack.Peek();
                switch (type)
                {
                    case TokenType.String: list.Add((string)token); break;
                    case TokenType.Integer: list.Add((int)token); break;
                    case TokenType.Symbol: list.Add(Symbol((string)token)); break;
                    case TokenType.OpenParenthesis:
                        {
                            parentheisStack.Push((char)token);
                            stack.Push(new List<SExpr>());
                            break;
                        }
                    case TokenType.CloseParenthesis:
                        {
                            var open = parentheisStack.Pop();
                            var expectClose = open switch
                            {
                                '(' => ')',
                                '[' => ']',
                                '{' => '}',
                                _ => throw new InvalidProgramException()
                            };
                            var close = (char)token;
                            if (expectClose != close)
                            {
                                throw new InvalidDataException($"Closing parenthsis '{close}' not match '{open}'");
                            }
                            var top = stack.Pop();
                            stack.Peek().Add(SExpr.List(top));
                            break;
                        }
                }
            }
            if (stack.Count != 1) throw new InvalidDataException("Incomplete sexpr");
            return SExpr.List(stack.Pop());
        }

        public static SExpr Parse(TextReader reader)
            => Parse(Tokenize(reader));

        public static SExpr ParseFile(string path)
        {
            using var reader = new StreamReader(path, Encoding.UTF8);
            return Parse(reader);
        }

        public static SExpr ParseString(string str)
            => Parse(new StringReader(str));
    }

    public class SExprConsumer
    {
        private SExpr _sexpr;
        public SExprConsumer(SExpr sexpr)
        {
            if (!sexpr.IsList)
            {
                throw new ArgumentException($"Cannot consume non-list sexpr {sexpr}");
            }
            _sexpr = sexpr;
        }
        public SExpr Take()
        {
            if (_sexpr.IsNull)
            {
                throw new ArgumentNullException("Cannot take more element from sexpr");
            }
            var (car, cdr) = _sexpr.AsPair();
            _sexpr = cdr;
            return car;
        }
        public int TakeInt()
            => Take().AsInt();
        public string TakeString()
            => Take().AsString();
        public string TakeSymbol()
            => Take().AsSymbol();
        public (SExpr car, SExpr cdr) TakePair()
            => Take().AsPair();
        public IList<SExpr> TakeList()
            => Take().ToList();
        public IList<SExpr> TakeRest()
        {
            var rest = _sexpr.ToList();
            _sexpr = SExpr.Null;
            return rest;
        }
        public bool IsEmpty
            => _sexpr.IsNull;
    }
}