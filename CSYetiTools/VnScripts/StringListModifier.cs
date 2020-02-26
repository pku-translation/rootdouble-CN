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
            var rootList = SExpr.ParseFile(path);
            var result = new Dictionary<int, StringListModifier[]>();
            foreach (var root in rootList.AsEnumerable())
            {
                var rootConsumer = new SExprConsumer(root);
                if (rootConsumer.TakeSymbol() != "script") throw new ArgumentException($"Invalid script value {root}");
                int script = rootConsumer.TakeInt();

                var modifiers = new List<StringListModifier>();
                foreach (var expr in rootConsumer.TakeRest())
                {
                    var consumer = new SExprConsumer(expr);

                    var op = consumer.TakeSymbol();

                    modifiers.Add(op switch
                    {
                        "->" => new Recode(consumer),
                        "<-" => new ConcatCodes(consumer),
                        "--" => new DropCodes(consumer),
                        "++" => new InsertCode(consumer),
                        "<+" => new CopyCode(consumer),
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
        public Recode(SExprConsumer consumer)
        {
            Index = consumer.TakeInt();
            Code = (byte)consumer.TakeInt();
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
        public ConcatCodes(SExprConsumer consumer)
        {
            Index = consumer.TakeInt();
            Sources = consumer.TakeRest().Select(s => s.AsInt()).ToList();
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
        public DropCodes(SExprConsumer consumer)
        {
            Indices = consumer.TakeRest().Select(s => s.AsInt()).ToList();
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
        public InsertCode(SExprConsumer consumer)
        {
            Index = consumer.TakeInt();
            Code = (byte)consumer.TakeInt();
            Content = consumer.TakeString();
        }

        public override void Modify(IDictionary<int, Script.StringReferenceEntry> table)
        {
            table.Add(Index, new Script.StringReferenceEntry(Index, Code, Content));
        }

        public override SExpr ToSExpr()
            => SExpr.List(SExpr.Symbol("++"), Index, SExpr.Int(Code, SExprIntFormat.Hex), Content);
    }

    public class CopyCode : StringListModifier
    {
        public int Index;
        public int SourceIndex;
        public byte? Code;
        public CopyCode(SExprConsumer consumer)
        {
            Index = consumer.TakeInt();
            SourceIndex = consumer.TakeInt();
            if (!consumer.IsEmpty)
                Code = (byte)consumer.TakeInt();
        }

        public override void Modify(IDictionary<int, Script.StringReferenceEntry> table)
        {
            var source = table[SourceIndex];
            table.Add(Index, new Script.StringReferenceEntry(Index, Code ?? source.Code, source.Content));
        }

        public override SExpr ToSExpr()
            => Code != null ? SExpr.List(SExpr.Symbol("<+"), Index, SourceIndex, SExpr.Int(Code.Value, SExprIntFormat.Hex))
                             : SExpr.List(SExpr.Symbol("<+"), Index, SourceIndex);
    }
}