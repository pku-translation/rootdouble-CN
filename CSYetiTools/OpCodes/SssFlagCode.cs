using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CSYetiTools.OpCodes
{
    public class SssFlagCode : OpCode
    {
        private short _flagIndex;

        private short _sound;

        // 9 shorts format an Ennegram.
        private short[] _changes = Array.Empty<short>();

        public override int ArgLength
            => 2 + 2 + 9 * 2;

        public SssFlagCode() : base(0x89) { }
        
        public override byte[] ArgsToBytes()
        {
            using var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms);
            bw.Write(_flagIndex);
            bw.Write(_sound);
            foreach (var change in _changes) 
            {
                bw.Write(change);
            }
            bw.Flush();
            return ms.ToArray();
        }

        protected override string ArgsToString()
        {
            var builder = new StringBuilder()
                .Append(_flagIndex)
                .Append(' ')
                .Append(_sound)
                .Append(" [");
            foreach (var change in _changes)
            {
                builder.Append(' ').Append(change.ToString().PadLeft(2));
            }
            builder.Append(" ]");
            return builder.ToString();
        }

        protected override void Read(BinaryReader reader)
        {
            _flagIndex = reader.ReadInt16();
            _sound = reader.ReadInt16();
            _changes = new short[9];
            for (int i = 0; i < 9; ++i)
            {
                _changes[i] = reader.ReadInt16();
            }
        }
    }
}
