using System;
using System.Collections.Generic;
using System.Linq;
using Untitled.Sexp;
using Untitled.Sexp.Attributes;
using Untitled.Sexp.Conversion;
using Untitled.Sexp.Formatting;

namespace CsYetiTools.VnScripts
{
    [SexpCustomTypeResolver(typeof(StringListModifierResolver))]
    public abstract class StringListModifier
    {
        // { (script <index> {<instruction>}) }
        // recode: (-> <index> <code>)
        // concat: (<- <index> {<srcindex>})
        // drop:   (-- {<index>})
        // insert: (++ <index> <code> <content>)
        // copy:   (<+ <index> <index> [<code>])

        public class StringListModifierResolver : LookupTypeResolver
        {
            public StringListModifierResolver()
            {
                AddGeneral(Symbol.FromString("->"), typeof(Recode));
                AddGeneral(Symbol.FromString("<-"), typeof(ConcatCodes));
                AddGeneral(Symbol.FromString("--"), typeof(DropCodes));
                AddGeneral(Symbol.FromString("++"), typeof(InsertCode));
                AddGeneral(Symbol.FromString("<+"), typeof(CopyCode));
            }
        }

        public static IDictionary<int, StringListModifier[]> LoadFile(string path)
        {
            var sexp = Sexp.ParseFile(path);
            return SexpConvert.ToObject<Dictionary<int, StringListModifier[]>>(sexp);
        }

        public abstract void Modify(IDictionary<int, Script.StringReferenceEntry> table);
    }

    [SexpAsList]
    public class Recode : StringListModifier
    {
        public int Index;

        [SexpNumberFormatting(Radix = NumberRadix.Hexadecimal)]
        public byte Code;

        public override void Modify(IDictionary<int, Script.StringReferenceEntry> table)
        {
            table[Index].Code = Code;
        }
    }

    [SexpAsList]
    public class ConcatCodes : StringListModifier
    {
        public int Index;
        public List<int> Sources = new List<int>();

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
    }

    [SexpAsList]
    public class DropCodes : StringListModifier
    {
        public List<int> Indices = new List<int>();

        public override void Modify(IDictionary<int, Script.StringReferenceEntry> table)
        {
            foreach (var idx in Indices) table.Remove(idx);
        }
    }

    [SexpAsList]
    public class InsertCode : StringListModifier
    {
        public int Index;

        [SexpNumberFormatting(Radix = NumberRadix.Hexadecimal)]
        public byte Code;

        public string Content = "";

        public override void Modify(IDictionary<int, Script.StringReferenceEntry> table)
        {
            table.Add(Index, new Script.StringReferenceEntry(Index, Code, Content));
        }
    }
    
    [SexpAsList]
    public class CopyCode : StringListModifier
    {
        public int Index;
        public int SourceIndex;

        [SexpNumberFormatting(Radix = NumberRadix.Hexadecimal)]
        public byte Code;
        public override void Modify(IDictionary<int, Script.StringReferenceEntry> table)
        {
            var source = table[SourceIndex];
            table.Add(Index, new Script.StringReferenceEntry(Index, Code, source.Content));
        }
    }
}