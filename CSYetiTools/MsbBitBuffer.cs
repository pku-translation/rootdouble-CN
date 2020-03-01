

using System;
using System.Collections.Generic;
using System.Linq;

namespace CsYetiTools
{
    public class MsbBitBuffer
    {
        private static byte[] Masks = {
            0x80, 0x40, 0x20, 0x10, 0x08, 0x04, 0x02, 0x01
        };

        private List<bool> _bools = new List<bool>();

        private int _position;

        public int Position
        {
            get => _position;
            set
            {
                if (value < 0 || value >= Size) throw new InvalidOperationException("Invalid pos");
                _position = value;
            }
        }

        public int Size
            => _bools.Count;

        public MsbBitBuffer(byte[] bytes, int size)
        {
            if (size < 0 || size > bytes.Length * 8) throw new ArgumentException("Invalid size");
            var fulls = size / 8;
            var tail = size % 8;
            for (int i = 0; i < fulls; ++i)
            {
                var b = bytes[i];
                foreach (var mask in Masks) _bools.Add((b & mask) != 0);
            }
            var lb = bytes[fulls];
            for (int j = 0; j < tail; ++j)
            {
                _bools.Add((lb & Masks[j]) != 0);
            }
        }

        public MsbBitBuffer(IEnumerable<byte> bytes)
        {
            foreach (var b in bytes)
            {
                foreach (var mask in Masks) _bools.Add((b & mask) != 0);
            }
        }

        public MsbBitBuffer()
        {
        }

        public bool Get()
        {
            var result = _bools[_position];
            ++_position;
            return result;
        }

        public ulong Gets(int count)
        {
            if (count > 64) throw new ArgumentException("Count must <= 64");
            if (_position + count > Size) throw new InvalidOperationException("No enough bits");
            var result = 0uL;
            for (int i = 0; i < count; ++i)
            {
                result = (result << 1) | (_bools[_position + i] ? 1u : 0u);
            }
            _position += count;
            return result;
        }

        public void Append(ulong bits, int count)
        {
            var mask = 1uL << (count - 1);
            while (mask > 0)
            {
                _bools.Add((bits & mask) > 0);
                mask >>= 1;
            }
        }

        public byte[] ToBytes()
        {
            var remain = Size % 8;
            var fulls = Size / 8;
            var result = new byte[remain != 0 ? fulls + 1 : fulls];
            for (int i = 0; i < fulls; ++i)
            {
                for (int j = 0; j < 8; ++j)
                {
                    if (_bools[i * 8 + j]) result[i] |= Masks[j];
                }
            }
            if (remain != 0)
            {
                for (int j = 0; j < remain; ++j)
                {
                    if (_bools[fulls * 8 + j]) result[fulls] |= Masks[j];
                }
            }
            return result;
        }
    }

    // var buffer = new MsbBitBuffer(new byte[]{ 0b10101111, 0b00001011, 0b00100101, 0b11111111 });
}