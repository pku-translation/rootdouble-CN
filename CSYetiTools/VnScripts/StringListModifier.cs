using System;
using System.Collections.Generic;
using System.Linq;

namespace CsYetiTools.VnScripts
{
    public abstract class StringListModifier
    {
        // { (script <index> {<instruction>}) }
        // recode: (-> <index> <code>)
        // concat: (<- <index> {<srcindex>})
        // drop:   (-- {<index>})
        // insert: (++ <index> <code> <content>)
        // copy:   (<+ <index> <index> [<code>])



        public static IDictionary<int, StringListModifier[]> LoadFile(string path)
        {
            var rootList = SExpr.ParseFile(path).ToList();
            var result = new Dictionary<int, StringListModifier[]>();
            foreach (var root in rootList)
            {
                if (root.Car.AsSymbol() != "script") throw new ArgumentException($"Invalid script value {root}");
                int script = root.Cdr.Car.AsInt();

                var modifiers = new List<StringListModifier>();
                foreach (var expr in root.Cdr.Cdr.ToList())
                {
                    if (!expr.Car.IsSymbol)
                        throw new ArgumentException($"Unknown operation: {expr.Car}");

                    var op = expr.Car.AsSymbol();
                    var args = expr.Cdr;

                    modifiers.Add(op switch
                    {
                        "->" => new Recode(args),
                        "<-" => new ConcatCodes(args),
                        "--" => new DropCodes(args),
                        "++" => new InsertCode(args),
                        "<+" => new CopyCode(args),
                        _ => throw new ArgumentException($"Unknown operation: {op}")
                    });
                }
                result.Add(script, modifiers.ToArray());
            }
            return result;
        }

        public abstract void Modify(IDictionary<int, Script.StringReferenceEntry> table);

        public abstract SExpr ToSExpr();
    }

    public class Recode : StringListModifier
    {
        public int Index;
        public byte Code;
        public Recode(SExpr args)
        {
            Index = args.Car.AsInt();
            Code = (byte)args.Cdr.Car.AsInt();
        }

        public override void Modify(IDictionary<int, Script.StringReferenceEntry> table)
        {
            table[Index].Code = Code;
        }

        public override SExpr ToSExpr()
            => SExpr.List(SExpr.Symbol("->"), Index, SExpr.Int(Code, SExprIntFormat.Hex));
    }

    public class ConcatCodes : StringListModifier
    {
        public int Index;
        public List<int> Sources;
        public ConcatCodes(SExpr args)
        {
            Index = args.Car.AsInt();
            Sources = args.Cdr.ToList().Select(s => s.AsInt()).ToList();
        }

        public override void Modify(IDictionary<int, Script.StringReferenceEntry> table)
        {
            var target = table[Index];
            foreach (var source in Sources)
            {
                table.Remove(source, out var entry);
                if (entry != null)
                {
                    target.Content += entry.Content.Trim();
                }
            }
        }

        public override SExpr ToSExpr()
            => SExpr.Symbol("<-").Concat(SExpr.Int(Index).Concat(SExpr.List(Sources.Select(i => SExpr.Int(i)))));
    }

    public class DropCodes : StringListModifier
    {
        public List<int> Indices;
        public DropCodes(SExpr args)
        {
            Indices = args.ToList().Select(s => s.AsInt()).ToList();
        }

        public override void Modify(IDictionary<int, Script.StringReferenceEntry> table)
        {
            foreach (var idx in Indices) table.Remove(idx);
        }

        public override SExpr ToSExpr()
            => SExpr.Symbol("--").Concat(SExpr.List(Indices.Select(i => SExpr.Int(i))));
    }

    public class InsertCode : StringListModifier
    {
        public int Index;
        public byte Code;
        public string Content;
        public InsertCode(SExpr args)
        {
            Index = args.Car.AsInt();
            Code = (byte)args.Cdr.Car.AsInt();
            Content = args.Cdr.Cdr.Car.AsString();
        }

        public override void Modify(IDictionary<int, Script.StringReferenceEntry> table)
        {
            table.Add(Index, new Script.StringReferenceEntry(Index, Code, -1, Content));
        }

        public override SExpr ToSExpr()
            => SExpr.List(SExpr.Symbol("++"), Index, SExpr.Int(Code, SExprIntFormat.Hex), Content);
    }

    public class CopyCode : StringListModifier
    {
        public int Index;
        public int SourceIndex;
        public byte? Code;
        public CopyCode(SExpr args)
        {
            Index = args.Car.AsInt();
            SourceIndex = args.Cdr.Car.AsInt();
            var last = args.Cdr.Cdr;
            if (!last.IsNull)
                Code = (byte)last.Car.AsInt();
        }

        public override void Modify(IDictionary<int, Script.StringReferenceEntry> table)
        {
            var source = table[SourceIndex];
            table.Add(Index, new Script.StringReferenceEntry(Index, Code ?? source.Code, -1, source.Content));
        }

        public override SExpr ToSExpr()
            => Code != null ? SExpr.List(SExpr.Symbol("<+"), Index, SourceIndex, SExpr.Int(Code.Value, SExprIntFormat.Hex))
                             : SExpr.List(SExpr.Symbol("<+"), Index, SourceIndex);
    }
}