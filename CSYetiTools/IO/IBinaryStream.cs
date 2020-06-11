using System;
using System.Text;

namespace CsYetiTools.IO
{
    public interface IBinaryStream : IDisposable
    {
        long Position { get; set; }
        long Length { get; }
        bool CanSeek { get; }
        bool CanRead { get; }
        bool CanWrite { get; }
        int Peek();
        void Seek(int offset);
        int Read(byte[] buffer, int offset, int count);
        int Read(Span<byte> bytes);
        byte[] ReadBytesExact(int count);
        byte[] ReadBytesMax(int count);
        byte[] ReadToEnd();
        void ReadBytesExact(Span<byte> span);
        byte ReadByte();
        sbyte ReadSByte();
        short ReadInt16LE();
        ushort ReadUInt16LE();
        int ReadInt32LE();
        uint ReadUInt32LE();
        long ReadInt64LE();
        ulong ReadUInt64LE();
        short ReadInt16BE();
        ushort ReadUInt16BE();
        int ReadInt32BE();
        uint ReadUInt32BE();
        long ReadInt64BE();
        ulong ReadUInt64BE();
        string ReadStringZ();
        void Write(byte[] buffer, int offset, int count);
        void Write(byte[] bytes);
        void Write(ReadOnlySpan<byte> bytes);
        void Write(byte b);
        void Write(sbyte b);
        void WriteLE(short s);
        void WriteLE(ushort s);
        void WriteLE(int i);
        void WriteLE(uint i);
        void WriteLE(long l);
        void WriteLE(ulong l);
        void WriteBE(short s);
        void WriteBE(ushort s);
        void WriteBE(int i);
        void WriteBE(uint i);
        void WriteBE(long l);
        void WriteBE(ulong l);
        void WriteStringZ(string s);
        int GetStringZByteCount(string s);
        
    }
}