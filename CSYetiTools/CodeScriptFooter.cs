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

        public int ScriptIndex { get; set; }

        public byte[] ToBytes()
        {
            var result = new byte[16];
            BitConverter.TryWriteBytes(new Span<byte>(result, 0, 4), Int1);
            BitConverter.TryWriteBytes(new Span<byte>(result, 4, 4), Int2);
            BitConverter.TryWriteBytes(new Span<byte>(result, 8, 4), Int3);
            BitConverter.TryWriteBytes(new Span<byte>(result, 12, 4), ScriptIndex);
            return result;
        }

        public static CodeScriptFooter ReadFrom(BinaryReader reader)
        {
            return new CodeScriptFooter {
                Int1 = reader.ReadInt32(),
                Int2 = reader.ReadInt32(),
                Int3 = reader.ReadInt32(),
                ScriptIndex = reader.ReadInt32(),
            };
        }

        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(Int1);
            writer.Write(Int2);
            writer.Write(Int3);
            writer.Write(ScriptIndex);
        }
        
        public override string ToString()
        {
            return $"{Int1,6} {Int2,6} {Int3,6} {ScriptIndex,6}";
        }

        public CodeScriptFooter Clone()
        {
            return new CodeScriptFooter
            {
                Int1 = Int1,
                Int2 = Int2,
                Int3 = Int3,
                ScriptIndex = ScriptIndex,
            };
        }

    }
}