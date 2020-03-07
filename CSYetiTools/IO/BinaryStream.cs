using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CsYetiTools.IO
{
    public class BinaryStream : IBinaryStream
    {
        private Stream? _stream;
        private byte[] _buffer = new byte[16];
        private char[] _largeCharBuffer = new char[256];
        private byte[] _largeByteBuffer = new byte[256];

        private Encoding _encoding;

        public long Position
        {
            get => _stream == null ? throw DisposedException() : _stream.Position;
            set
            {
                if (_stream == null) throw DisposedException();
                _stream.Position = value;
            }
        }

        public long Length
            => _stream == null ? throw DisposedException() : _stream.Length;

        public bool CanSeek => throw new NotImplementedException();

        public bool CanRead => throw new NotImplementedException();

        public bool CanWrite => throw new NotImplementedException();

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                var copyOfStream = _stream;
                _stream = null;
                copyOfStream?.Close();
            }
            _stream = null;
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected ObjectDisposedException DisposedException()
        {
            return new ObjectDisposedException("Stream is disposed");
        }

        private void FillBuffer(int count)
        {
            if (_stream == null) throw DisposedException();
            if (count != _stream.Read(_buffer, 0, count)) throw new EndOfStreamException();
        }

        public int Peek()
        {
            if (_stream == null) throw DisposedException();
            return _stream.Peek();
        }

        public void Seek(int offset)
        {
            if (_stream == null) throw DisposedException();
            _stream.Seek(offset, SeekOrigin.Current);
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            if (_stream == null) throw DisposedException();
            return _stream.Read(buffer, offset, count);
        }

        public int Read(Span<byte> bytes)
        {
            if (_stream == null) throw DisposedException();
            return _stream.Read(bytes);
        }

        public byte[] ReadBytesExact(int count)
        {
            if (_stream == null) throw DisposedException();
            if (count < 0) throw new ArgumentException($"Negative read count {count} is invalid");
            var bytes = new byte[count];
            var read = _stream.Read(bytes);
            if (read != count) throw new EndOfStreamException();
            return bytes;
        }

        public byte[] ReadBytesMax(int count)
        {
            if (_stream == null) throw DisposedException();
            if (count < 0) throw new ArgumentException($"Negative read count {count} is invalid");
            var bytes = new byte[count];
            var read = _stream.Read(bytes);
            return read == count ? bytes : bytes[0..read];
        }

        public byte[] ReadToEnd()
        {
            if (_stream == null) throw DisposedException();
            var count = _stream.Length - _stream.Position;
            if (count > int.MaxValue) throw new InvalidOperationException($"Remain too many bytes ({count}), can not read to end.");
            return ReadBytesExact((int)count);
        }

        public void ReadBytesExact(Span<byte> span)
        {
            if (_stream == null) throw DisposedException();
            if (span.Length != _stream.Read(span)) throw new EndOfStreamException();
        }

        public byte ReadByte()
        {
            if (_stream == null) throw DisposedException();

            int b = _stream.ReadByte();
            if (b == -1)
                throw new EndOfStreamException();
            return (byte)b;
        }

        public sbyte ReadSByte()
        {
            return (sbyte)ReadByte();
        }

        public short ReadInt16LE()
        {
            FillBuffer(2);
            return (short)(_buffer[0] | _buffer[1] << 8);
        }

        public ushort ReadUInt16LE()
        {
            FillBuffer(2);
            return (ushort)(_buffer[0] | _buffer[1] << 8);
        }

        public int ReadInt32LE()
        {
            FillBuffer(4);
            return (int)(_buffer[0] | _buffer[1] << 8 | _buffer[2] << 16 | _buffer[3] << 24);
        }

        public virtual uint ReadUInt32LE()
        {
            FillBuffer(4);
            return (uint)(_buffer[0] | _buffer[1] << 8 | _buffer[2] << 16 | _buffer[3] << 24);
        }

        public long ReadInt64LE()
        {
            FillBuffer(8);
            uint lo = (uint)(_buffer[0] | _buffer[1] << 8 |
                             _buffer[2] << 16 | _buffer[3] << 24);
            uint hi = (uint)(_buffer[4] | _buffer[5] << 8 |
                             _buffer[6] << 16 | _buffer[7] << 24);
            return (long)((ulong)hi) << 32 | lo;
        }

        public ulong ReadUInt64LE()
        {
            FillBuffer(8);
            uint lo = (uint)(_buffer[0] | _buffer[1] << 8 |
                             _buffer[2] << 16 | _buffer[3] << 24);
            uint hi = (uint)(_buffer[4] | _buffer[5] << 8 |
                             _buffer[6] << 16 | _buffer[7] << 24);
            return ((ulong)hi) << 32 | lo;
        }

        public short ReadInt16BE()
        {
            FillBuffer(2);
            return (short)(_buffer[1] | _buffer[0] << 8);
        }

        public ushort ReadUInt16BE()
        {
            FillBuffer(2);
            return (ushort)(_buffer[1] | _buffer[0] << 8);
        }

        public int ReadInt32BE()
        {
            FillBuffer(4);
            return (int)(_buffer[3] | _buffer[2] << 8 | _buffer[1] << 16 | _buffer[0] << 24);
        }

        public uint ReadUInt32BE()
        {
            FillBuffer(4);
            return (uint)(_buffer[3] | _buffer[2] << 8 | _buffer[1] << 16 | _buffer[0] << 24);
        }

        public long ReadInt64BE()
        {
            FillBuffer(8);
            uint lo = (uint)(_buffer[7] | _buffer[6] << 8 |
                             _buffer[5] << 16 | _buffer[4] << 24);
            uint hi = (uint)(_buffer[3] | _buffer[2] << 8 |
                             _buffer[1] << 16 | _buffer[0] << 24);
            return (long)((ulong)hi) << 32 | lo;
        }

        public ulong ReadUInt64BE()
        {
            FillBuffer(8);
            uint lo = (uint)(_buffer[7] | _buffer[6] << 8 |
                             _buffer[5] << 16 | _buffer[4] << 24);
            uint hi = (uint)(_buffer[3] | _buffer[2] << 8 |
                             _buffer[1] << 16 | _buffer[0] << 24);
            return (ulong)((ulong)hi) << 32 | lo;
        }

        public string ReadStringZ()
        {
            if (_stream == null) throw DisposedException();
            var buffer = _largeByteBuffer;
            int count = 0;
            int b;
            while ((b = _stream.ReadByte()) != 0)
            {
                if (b < 0) throw new EndOfStreamException();
                if (count == buffer.Length)
                {
                    var newBuffer = new byte[buffer.Length * 4];
                    Array.Copy(buffer, newBuffer, buffer.Length);
                    buffer = newBuffer;
                }
                buffer[count++] = (byte)b;
            }
            try
            {
                return _encoding.GetString(buffer, 0, count);
            }
            catch (DecoderFallbackException exc)
            {
                throw new InvalidDataException($"Cannot decode using {_encoding.EncodingName}, data=[{Utils.BytesToHex(buffer)}]", exc);
            }
        }

        public void Write(byte[] buffer, int offset, int count)
        {
            if (_stream == null) throw DisposedException();
            _stream.Write(buffer, offset, count);
        }

        public void Write(byte[] bytes)
        {
            if (_stream == null) throw DisposedException();
            _stream.Write(bytes);
        }

        public void Write(ReadOnlySpan<byte> bytes)
        {
            if (_stream == null) throw DisposedException();
            _stream.Write(bytes);
        }

        public void Write(byte b)
        {
            if (_stream == null) throw DisposedException();
            _stream.WriteByte(b);
        }

        public void Write(sbyte b)
        {
            if (_stream == null) throw DisposedException();
            _stream.WriteByte((byte)b);
        }

        public void WriteLE(short s)
        {
            if (_stream == null) throw DisposedException();
            _buffer[0] = (byte)s;
            _buffer[1] = (byte)(s >> 8);
            _stream.Write(_buffer, 0, 2);
        }

        public void WriteLE(ushort s)
        {
            if (_stream == null) throw DisposedException();
            _buffer[0] = (byte)s;
            _buffer[1] = (byte)(s >> 8);
            _stream.Write(_buffer, 0, 2);
        }

        public void WriteLE(int i)
        {
            if (_stream == null) throw DisposedException();
            _buffer[0] = (byte)i;
            _buffer[1] = (byte)(i >> 8);
            _buffer[2] = (byte)(i >> 16);
            _buffer[3] = (byte)(i >> 24);
            _stream.Write(_buffer, 0, 4);
        }

        public void WriteLE(uint i)
        {
            if (_stream == null) throw DisposedException();
            _buffer[0] = (byte)i;
            _buffer[1] = (byte)(i >> 8);
            _buffer[2] = (byte)(i >> 16);
            _buffer[3] = (byte)(i >> 24);
            _stream.Write(_buffer, 0, 4);
        }

        public void WriteLE(long l)
        {
            if (_stream == null) throw DisposedException();
            _buffer[0] = (byte)l;
            _buffer[1] = (byte)(l >> 8);
            _buffer[2] = (byte)(l >> 16);
            _buffer[3] = (byte)(l >> 24);
            _buffer[4] = (byte)(l >> 32);
            _buffer[5] = (byte)(l >> 40);
            _buffer[6] = (byte)(l >> 48);
            _buffer[7] = (byte)(l >> 56);
            _stream.Write(_buffer, 0, 8);
        }

        public void WriteLE(ulong l)
        {
            if (_stream == null) throw DisposedException();
            _buffer[0] = (byte)l;
            _buffer[1] = (byte)(l >> 8);
            _buffer[2] = (byte)(l >> 16);
            _buffer[3] = (byte)(l >> 24);
            _buffer[4] = (byte)(l >> 32);
            _buffer[5] = (byte)(l >> 40);
            _buffer[6] = (byte)(l >> 48);
            _buffer[7] = (byte)(l >> 56);
            _stream.Write(_buffer, 0, 8);
        }

        public void WriteBE(short s)
        {
            if (_stream == null) throw DisposedException();
            _buffer[1] = (byte)s;
            _buffer[0] = (byte)(s >> 8);
            _stream.Write(_buffer, 0, 2);
        }

        public void WriteBE(ushort s)
        {
            if (_stream == null) throw DisposedException();
            _buffer[1] = (byte)s;
            _buffer[0] = (byte)(s >> 8);
            _stream.Write(_buffer, 0, 2);
        }

        public void WriteBE(int i)
        {
            if (_stream == null) throw DisposedException();
            _buffer[3] = (byte)i;
            _buffer[2] = (byte)(i >> 8);
            _buffer[1] = (byte)(i >> 16);
            _buffer[0] = (byte)(i >> 24);
            _stream.Write(_buffer, 0, 4);
        }

        public void WriteBE(uint i)
        {
            if (_stream == null) throw DisposedException();
            _buffer[3] = (byte)i;
            _buffer[2] = (byte)(i >> 8);
            _buffer[1] = (byte)(i >> 16);
            _buffer[0] = (byte)(i >> 24);
            _stream.Write(_buffer, 0, 4);
        }

        public void WriteBE(long l)
        {
            if (_stream == null) throw DisposedException();
            _buffer[7] = (byte)l;
            _buffer[6] = (byte)(l >> 8);
            _buffer[5] = (byte)(l >> 16);
            _buffer[4] = (byte)(l >> 24);
            _buffer[3] = (byte)(l >> 32);
            _buffer[2] = (byte)(l >> 40);
            _buffer[1] = (byte)(l >> 48);
            _buffer[0] = (byte)(l >> 56);
            _stream.Write(_buffer, 0, 8);
        }

        public void WriteBE(ulong l)
        {
            if (_stream == null) throw DisposedException();
            _buffer[7] = (byte)l;
            _buffer[6] = (byte)(l >> 8);
            _buffer[5] = (byte)(l >> 16);
            _buffer[4] = (byte)(l >> 24);
            _buffer[3] = (byte)(l >> 32);
            _buffer[2] = (byte)(l >> 40);
            _buffer[1] = (byte)(l >> 48);
            _buffer[0] = (byte)(l >> 56);
            _stream.Write(_buffer, 0, 8);
        }

        public void WriteStringZ(string s)
        {
            if (_stream == null) throw DisposedException();
            var maxCount = _encoding.GetMaxByteCount(s.Length);
            var buffer = maxCount > _largeByteBuffer.Length ? new byte[maxCount] : _largeByteBuffer;
            var count = _encoding.GetBytes(s, 0, s.Length, buffer, 0);
            _stream.Write(buffer, 0, count);
            _stream.WriteByte(0x00);
        }

        public int GetStringZByteCount(string s)
        {
            return _encoding.GetByteCount(s) + 1;
        }

        public byte[] ToBytes()
        {
            if (_stream is MemoryStream ms) return ms.ToArray();

            throw new InvalidOperationException("Cannot get bytes from non-memory stream");
        }

        public BinaryStream(Stream stream, Encoding? encoding = null)
        {
            _stream = stream;
            _encoding = encoding ?? Utils.Utf8;
        }

        public BinaryStream(byte[] bytes, Encoding? encoding = null)
            : this(new MemoryStream(bytes), encoding)
        { }

        public BinaryStream(Encoding? encoding = null)
            : this(new MemoryStream(), encoding)
        { }

        public static BinaryStream ReadFile(FilePath path, Encoding? encoding = null)
        {
            return new BinaryStream(File.OpenRead(path), encoding);
        }

        public static BinaryStream WriteFile(FilePath path, Encoding? encoding = null)
        {
            return new BinaryStream(File.Create(path), encoding);
        }
        
    }
}