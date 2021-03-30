using System;
using CSYetiTools.Base.IO;

namespace CSYetiTools.VnScripts
{
    /********************************************************
    
        For steam version, all the strings is moved to the end of the script file before footer.
        This makes it possible to replace the strings without modify any offsets in the opcodes
        except which contain strings.
    
     */

    public sealed record ScriptFooter(int IndexedDialogCount, int Unknown, int FlagCodeCount, int ScriptIndex)
    {
        public static readonly ScriptFooter Zero = new ScriptFooter(0, 0, 0, 0);
        public static readonly ScriptFooter End = new ScriptFooter(-1, 0, 0, 0);

        public byte[] ToBytes()
        {
            var result = new byte[16];
            BitConverter.TryWriteBytes(new Span<byte>(result, 0, 4), IndexedDialogCount);
            BitConverter.TryWriteBytes(new Span<byte>(result, 4, 4), Unknown);
            BitConverter.TryWriteBytes(new Span<byte>(result, 8, 4), FlagCodeCount);
            BitConverter.TryWriteBytes(new Span<byte>(result, 12, 4), ScriptIndex);
            return result;
        }

        public static ScriptFooter ReadFrom(IBinaryStream stream)
        {
            return new ScriptFooter(stream.ReadInt32LE(), stream.ReadInt32LE(), stream.ReadInt32LE(), stream.ReadInt32LE());
        }

        public void WriteTo(IBinaryStream writer)
        {
            writer.WriteLE(IndexedDialogCount);
            writer.WriteLE(Unknown);
            writer.WriteLE(FlagCodeCount);
            writer.WriteLE(ScriptIndex);
        }

        public override string ToString()
        {
            return $"{IndexedDialogCount,6} {Unknown,6} {FlagCodeCount,6} {ScriptIndex,6}";
        }

        public static ScriptFooter operator +(ScriptFooter lhs, ScriptFooter rhs)
        {
            return new ScriptFooter(
                lhs.IndexedDialogCount + rhs.IndexedDialogCount,
                lhs.Unknown + rhs.Unknown,
                lhs.FlagCodeCount + rhs.FlagCodeCount,
                lhs.ScriptIndex + rhs.ScriptIndex
            );
        }
    }
}
