using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace CSYetiTools
{
    /********************************************************
    
        For steam version, all the strings is moved to the end of the script file before footer.
        This makes it possible to replace the strings without modify any offsets in the opcodes
        except which contain strings.
    
     */


    public sealed class CodeScriptFooter : IEquatable<CodeScriptFooter>
    {
        public int IndexedDialogCount { get; set; }
        
        public int Unknown { get; set; }

        public int FlagCodeCount { get; set; }

        public int ScriptIndex { get; set; }

        public byte[] ToBytes()
        {
            var result = new byte[16];
            BitConverter.TryWriteBytes(new Span<byte>(result, 0, 4), IndexedDialogCount);
            BitConverter.TryWriteBytes(new Span<byte>(result, 4, 4), Unknown);
            BitConverter.TryWriteBytes(new Span<byte>(result, 8, 4), FlagCodeCount);
            BitConverter.TryWriteBytes(new Span<byte>(result, 12, 4), ScriptIndex);
            return result;
        }

        public static CodeScriptFooter ReadFrom(BinaryReader reader)
        {
            return new CodeScriptFooter {
                IndexedDialogCount = reader.ReadInt32(),
                Unknown = reader.ReadInt32(),
                FlagCodeCount = reader.ReadInt32(),
                ScriptIndex = reader.ReadInt32(),
            };
        }

        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(IndexedDialogCount);
            writer.Write(Unknown);
            writer.Write(FlagCodeCount);
            writer.Write(ScriptIndex);
        }
        
        public override string ToString()
        {
            return $"{IndexedDialogCount,6} {Unknown,6} {FlagCodeCount,6} {ScriptIndex,6}";
        }

        public CodeScriptFooter Clone()
        {
            return new CodeScriptFooter
            {
                IndexedDialogCount = IndexedDialogCount,
                Unknown = Unknown,
                FlagCodeCount = FlagCodeCount,
                ScriptIndex = ScriptIndex,
            };
        }

        public bool Equals(CodeScriptFooter? other)
        {
            if (!(other is CodeScriptFooter footer)) return false;

            return IndexedDialogCount == footer.IndexedDialogCount
                && Unknown == footer.Unknown
                && FlagCodeCount == footer.FlagCodeCount
                && ScriptIndex == footer.ScriptIndex;
        }
    }
}