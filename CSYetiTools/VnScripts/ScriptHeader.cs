using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CsYetiTools.IO;

namespace CsYetiTools.VnScripts
{
    public sealed class ScriptHeader
    {
        public CodeAddressData[] Entries { get; set; }

        public byte[] RemainBytes { get; set; }

        public ScriptHeader(IEnumerable<CodeAddressData> entries, IEnumerable<byte> bytes)
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