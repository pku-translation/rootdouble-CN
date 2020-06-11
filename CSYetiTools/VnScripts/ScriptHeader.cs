using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CsYetiTools.IO;
using Untitled.Sexp.Attributes;
using Untitled.Sexp.Formatting;

namespace CsYetiTools.VnScripts
{
    [SexpAsList]
    public sealed class ScriptHeader
    {
        [SexpListFormatting()]
        public LabelReference[] Entries { get; set; }

        [SexpBytesFormatting(Radix = NumberRadix.Hexadecimal, LineLimit = 16)]
        public byte[] RemainBytes { get; set; }

        public ScriptHeader(IEnumerable<LabelReference> entries, IEnumerable<byte> bytes)
        {
            Entries = entries.ToArray();
            RemainBytes = bytes.ToArray();
        }

        public void WriteTo(IBinaryStream stream)
        {
            foreach (var entry in Entries)
            {
                stream.WriteLE(entry.AbsoluteOffset);
            }
            stream.Write(RemainBytes);
        }

        internal void Dump(TextWriter writer)
        {
            foreach (var entry in Entries)
            {
                writer.WriteLine(entry);
            }
            writer.WriteLine();
            if (RemainBytes.Length > 0)
            {
                Utils.BytesToTextLines(RemainBytes, 4 * Entries.Length).ForEach(writer.WriteLine);
                writer.WriteLine();
            }
        }
    }
}