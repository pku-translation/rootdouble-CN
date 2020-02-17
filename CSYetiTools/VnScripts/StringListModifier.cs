using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace CsYetiTools.VnScripts
{
    public class StringListModifier
    {
        #region a simple data s-expression parser (not support pairs)

        // public static void TestSExpr(string expr)
        // {
        //     var sexpr = SExpr.Parse(new StringReader(expr));
        //     Console.WriteLine(sexpr.ToString());
        // }

        private enum SExprType
        {
            String,
            Integer,
            Symbol,
            List,
        }

        private class SExpr
        {
            public SExprType Type { get; private set; }

            private object _value = 0;

            public static implicit operator SExpr(int n)
            {
                return new SExpr { _value = n, Type = SExprType.Integer };
            }
            public static implicit operator SExpr(string s)
            {
                return new SExpr { _value = s, Type = SExprType.String };
            }
            public static SExpr Symbol(string s)
            {
                return new SExpr { _value = s, Type = SExprType.Symbol };
            }
            public static SExpr List(IEnumerable<SExpr> exps)
            {
                return new SExpr { _value = new List<SExpr>(exps), Type = SExprType.List };
            }
            public static SExpr List()
            {
                return new SExpr { _value = new List<SExpr>(), Type = SExprType.List };
            }

            public bool IsInt
                => Type == SExprType.Integer;

            public bool IsSymbol
                => Type == SExprType.Symbol;

            public bool IsString
                => Type == SExprType.String;

            public bool IsList
                => Type == SExprType.List;

            public int AsInt()
            {
                if (Type != SExprType.Integer) throw new ArgumentException($"{Enum.GetName(typeof(SExprType), Type)} is not int");
                return (int)_value;
            }

            public string AsSymbol()
            {
                if (Type != SExprType.Symbol) throw new ArgumentException($"{Enum.GetName(typeof(SExprType), Type)} is not symbol");
                return (string)_value;
            }
            public string AsString()
            {
                if (Type != SExprType.String) throw new ArgumentException($"{Enum.GetName(typeof(SExprType), Type)} is not string");
                return (string)_value;
            }
            public IList<SExpr> AsList()
            {
                if (Type != SExprType.List) throw new ArgumentException($"{Enum.GetName(typeof(SExprType), Type)} is not list");
                return (IList<SExpr>)_value;
            }

            public override string ToString()
            {
                return ToString(string.Empty);
            }

            public string ToString(string prefix)
            {
                switch (Type)
                {
                    case SExprType.Integer: return ((int)_value).ToString();
                    case SExprType.Symbol: return (string)_value;
                    case SExprType.String: return "\"" + Regex.Escape((string)_value) + "\"";
                    case SExprType.List:
                        {
                            var list = (List<SExpr>)_value;
                            if (list.All(e => e.Type != SExprType.List))
                            {
                                return "(" + " ".Join(list.Select(e => e.ToString(prefix))) + ")";
                            }
                            else
                            {
                                return "(" + ("\r\n" + prefix).Join(list.Select(e => e.ToString(prefix + "  "))) + ")";
                            }
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
                                yield return (TokenType.OpenParenthesis, '(');
                                break;
                            case ')':
                                yield return (TokenType.CloseParenthesis, ')');
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
                                                if (Regex.IsMatch(str, @"^0x[0-9a-fA-F]+$")) yield return (TokenType.Integer, Convert.ToInt32(str.Substring(2), 16));
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
                var stack = new Stack<SExpr>();
                stack.Push(List());

                foreach (var (type, token) in tokens)
                {
                    var list = stack.Peek().AsList();
                    switch (type)
                    {
                        case TokenType.String: list.Add((string)token); break;
                        case TokenType.Integer: list.Add((int)token); break;
                        case TokenType.Symbol: list.Add(Symbol((string)token)); break;
                        case TokenType.OpenParenthesis: stack.Push(List()); break;
                        case TokenType.CloseParenthesis:
                            {
                                var top = stack.Pop();
                                stack.Peek().AsList().Add(top);
                                break;
                            }
                    }
                }
                if (stack.Count != 1) throw new ArgumentException("Incomplete sexpr");
                return stack.Pop();
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
        #endregion


        private List<Action<Dictionary<int, CodeScript.StringReferenceEntry>>> _actions = new List<Action<Dictionary<int, CodeScript.StringReferenceEntry>>>();

        public static IDictionary<int, StringListModifier> Load(string path)
        {
            var rootList = SExpr.ParseFile(path).AsList();
            var result = new Dictionary<int, StringListModifier>();
            foreach (var root in rootList)
            {
                var rootArgs = root.AsList();
                if (rootArgs[0].AsSymbol() != "script") throw new ArgumentException($"Invalid script value {root}");
                int script = rootArgs[1].AsInt();

                // { (script <index> {<instruction>}) }
                // recode: (<index> -> <code>)
                // concat: (<index> <- {<srcindex>})
                // drop:   (- {<index>})
                // insert: (+ <index> <code> <content>)
                // copy:   (+ <index> <index> [<code>])
                var actions = new List<Action<Dictionary<int, CodeScript.StringReferenceEntry>>>();
                for (int i = 2; i < rootArgs.Count; ++i)
                {
                    var actionArgs = rootArgs[i].AsList();

                    if (actionArgs[0].IsSymbol)
                    {
                        var op = actionArgs[0].AsSymbol();
                        if (op == "->")
                        {
                            // recode
                            var index = actionArgs[1].AsInt();
                            var code = (byte)actionArgs[2].AsInt();
                            actions.Add(table => table[index].Code = code);
                        }
                        else if (op == "<-")
                        {
                            // concat
                            var index = actionArgs[1].AsInt();
                            var indexes = new List<int>();
                            for (int r = 2; r < actionArgs.Count; ++r)
                                indexes.Add(actionArgs[r].AsInt());
                            actions.Add(table =>
                            {
                                var target = table[index];
                                foreach (var source in indexes)
                                {
                                    table.Remove(source, out var entry);
                                    if (entry != null)
                                    {
                                        target.Content += entry.Content.Trim();
                                    }
                                }
                            });
                        }
                        else if (op == "+")
                        {
                            var index = actionArgs[1].AsInt();
                            if (actionArgs.Count == 4 && actionArgs[3].IsString)
                            {
                                // insert
                                var code = (byte)actionArgs[2].AsInt();
                                var content = actionArgs[3].AsString();
                                actions.Add(table => table.Add(index, new CodeScript.StringReferenceEntry(index, code, -1, content)));
                            }
                            else
                            {
                                // copy
                                var index2 = actionArgs[2].AsInt();
                                if (actionArgs.Count == 4)
                                {
                                    actions.Add(table =>
                                    {
                                        var source = table[index2];
                                        var code = (byte)actionArgs[3].AsInt();
                                        table.Add(index, new CodeScript.StringReferenceEntry(index, code, -1, source.Content));
                                    });
                                }
                                else
                                {
                                    actions.Add(table =>
                                    {
                                        var source = table[index2];
                                        table.Add(index, new CodeScript.StringReferenceEntry(index, source.Code, -1, source.Content));
                                    });
                                }
                            }
                        }
                        else if (op == "-")
                        {
                            // drop
                            var indexes = new List<int>();
                            for (int r = 1; r < actionArgs.Count; ++r)
                                indexes.Add(actionArgs[r].AsInt());
                            actions.Add(table =>
                            {
                                foreach (var idx in indexes)
                                    table.Remove(idx);
                            });
                        }
                        else
                        {
                            throw new ArgumentException($"Unknown operation: {rootArgs[i]}");
                        }
                    }
                    else
                    {
                        throw new ArgumentException($"Unknown operation: {rootArgs[i]}");
                    }
                }
                result.Add(script, new StringListModifier { _actions = actions });
            }
            return result;
        }

        public void Modify(Dictionary<int, CodeScript.StringReferenceEntry> table)
        {
            foreach (var action in _actions)
            {
                action(table);
            }
        }
    }
}