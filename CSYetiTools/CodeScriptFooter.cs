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
        
        public int Unknown { get; set; }

        public int FlagCodeCount { get; set; }

        public int ScriptIndex { get; set; }

        public byte[] ToBytes()
        {
            var result = new byte[16];
            BitConverter.TryWriteBytes(new Span<byte>(result, 0, 4), Int1);
            BitConverter.TryWriteBytes(new Span<byte>(result, 4, 4), Unknown);
            BitConverter.TryWriteBytes(new Span<byte>(result, 8, 4), FlagCodeCount);
            BitConverter.TryWriteBytes(new Span<byte>(result, 12, 4), ScriptIndex);
            return result;
        }

        public static CodeScriptFooter ReadFrom(BinaryReader reader)
        {
            return new CodeScriptFooter {
                Int1 = reader.ReadInt32(),
                Unknown = reader.ReadInt32(),
                FlagCodeCount = reader.ReadInt32(),
                ScriptIndex = reader.ReadInt32(),
            };
        }

        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(Int1);
            writer.Write(Unknown);
            writer.Write(FlagCodeCount);
            writer.Write(ScriptIndex);
        }
        
        public override string ToString()
        {
            return $"{Int1,6} {Unknown,6} {FlagCodeCount,6} {ScriptIndex,6}";
        }

        public CodeScriptFooter Clone()
        {
            return new CodeScriptFooter
            {
                Int1 = Int1,
                Unknown = Unknown,
                FlagCodeCount = FlagCodeCount,
                ScriptIndex = ScriptIndex,
            };
        }

    }
}