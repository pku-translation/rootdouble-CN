using System;
using System.Collections.Generic;
using System.IO;

namespace CSYetiTools
{
    /********************************************************
    
        For steam version, all the strings is moved to the end of the script file before footer.
        This makes it possible to replace the strings without modify any offsets in the opcodes
        except which contain strings.
    
     */
    public sealed class CodeScriptFooter
    {
        public int Int1 { get; set; }
        
        public int Int2 { get; set; }

        public int Int3 { get; set; }

        public int Int4 { get; set; }

        public byte[] ToBytes()
        {
            var result = new byte[16];
            BitConverter.TryWriteBytes(result[0..3], Int1);
            BitConverter.TryWriteBytes(result[4..7], Int2);
            BitConverter.TryWriteBytes(result[8..11], Int3);
            BitConverter.TryWriteBytes(result[12..15], Int4);
            return result;
        }

        public static CodeScriptFooter ReadFrom(BinaryReader reader)
        {
            return new CodeScriptFooter {
                Int1 = reader.ReadInt32(),
                Int2 = reader.ReadInt32(),
                Int3 = reader.ReadInt32(),
                Int4 = reader.ReadInt32(),
            };
        }

        public static CodeScriptFooter[] ReadAllFrom(BinaryReader reader)
        {
            var footers = new List<CodeScriptFooter>();
            while (true)
            {
                var footer = ReadFrom(reader);
                footers.Add(footer);
                if (footer.Int1 == -1) break;
            }
            return footers.ToArray();
        }

        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(Int1);
            writer.Write(Int2);
            writer.Write(Int3);
            writer.Write(Int4);
        }

        public static void WriteAllTo(IEnumerable<CodeScriptFooter> footers, BinaryWriter writer)
        {
            foreach (var footer in footers)
            {
                footer.WriteTo(writer);
            }
        }

        public override string ToString()
        {
            return $"{Int1,4} {Int2,4} {Int3,4} {Int4,4}";
        }

    }
}