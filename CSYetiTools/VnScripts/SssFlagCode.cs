using System;
using System.IO;

namespace CsYetiTools.VnScripts
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

        protected override void ReadArgs(BinaryReader reader)
        {
            _flagIndex = reader.ReadInt16();
            _sound = reader.ReadInt16();
            _changes = new short[9];
            for (int i = 0; i < 9; ++i)
            {
                _changes[i] = reader.ReadInt16();
            }
        }

        protected override void WriteArgs(BinaryWriter writer)
        {
            writer.Write(_flagIndex);
            writer.Write(_sound);
            foreach (var change in _changes)
            {
                writer.Write(change);
            }
        }

        protected override void DumpArgs(TextWriter writer)
        {
            writer.Write(' '); writer.Write(_flagIndex);
            writer.Write(' '); writer.Write(_sound);
            writer.Write(" [");
            foreach (var change in _changes)
            {
                writer.Write(' '); writer.Write(change.ToString().PadLeft(2));
            }
            writer.Write(" ]");
        }
    }
}
