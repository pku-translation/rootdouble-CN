using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;

namespace CsYetiTools.FileTypes
{
    public sealed class Cpk : IDisposable
    {
        // file format reference:
        //     https://gist.github.com/unknownbrackets/78c4631a4091044d381432ffb7f1bae4
        //     https://github.com/vn-tools/arc_unpacker
        //     https://github.com/kamikat/cpktools

        private enum ColumnCategory
        {
            Empty = 1,
            Constant = 3,
            Row = 5,
        }

        private enum ColumnType
        {
            Byte = 0,
            SByte = 1,
            UInt16 = 2,
            Int16 = 3,
            UInt32 = 4,
            Int32 = 5,
            UInt64 = 6,
            Int64 = 7,
            Single = 8,
            String = 10,
            Bytes = 11,
        }

        private class Column
        {
            public ColumnCategory Category { get; set; }
            public ColumnType Type { get; set; }
            public string Name { get; set; }
            public object? Data { get; set; }

            public Column(ColumnCategory category, ColumnType type, string name, object? data = null)
            {
                Category = category;
                Type = type;
                Name = name;
                Data = data;
            }

            public override string ToString()
                => $"{Name}: {Type}({Category})";
        }

        private class UtfTable
        {
            public string Name { get; set; }
            public Column[] Columns { get; set; }
            public dynamic[] Rows { get; set; }
            public UtfTable(string name, Column[] columns, dynamic[] rows)
            {
                Name = name;
                Columns = columns;
                Rows = rows;
            }
        }

        public class ItocEntry
        {
            public int Id { get; set; }
            public long Offset { get; set; }
            public long FileSize { get; set; }
            public long ExtractSize { get; set; }
            public bool Low { get; set; }

            public override string ToString()
                => $"{Id}: {Offset} {FileSize}({ExtractSize})" + (Low ? "(L)" : "(H)");
        }

        private static byte[] ToBytes(string input)
            => Encoding.UTF8.GetBytes(input);
        private static byte[] FileTag = ToBytes("CPK ");
        private static byte[] UtfTag = ToBytes("@UTF");
        private static byte[] ItocTag = ToBytes("ITOC");
        private static byte[] LaylaTag = ToBytes("CRILAYLA");

        private static string PeekString(BinaryReader reader, long pos)
        {
            var oldPos = reader.BaseStream.Position;
            reader.BaseStream.Position = pos;
            var bytes = new List<byte>();
            byte c;
            while ((c = reader.ReadByte()) != 0) bytes.Add(c);
            var str = Encoding.ASCII.GetString(bytes.ToArray());
            reader.BaseStream.Position = oldPos;
            return str;
        }

        private static byte[] PeekBytes(BinaryReader reader, long pos, int size)
        {
            var oldPos = reader.BaseStream.Position;
            reader.BaseStream.Position = pos;
            var bytes = reader.ReadBytesExact(size);
            reader.BaseStream.Position = oldPos;
            return bytes;
        }

        private static void WriteBytesTo(Stream source, Stream dest, long count)
        {
            var buffer = new byte[4096];
            while (count > 0)
            {
                var shouldRead = count < buffer.Length ? (int)count : buffer.Length;
                var read = source.Read(buffer, 0, shouldRead);
                dest.Write(buffer, 0, read);
                if (read != shouldRead) return;
                count -= read;
            }
        }

        private static void CheckBytes(IEnumerable<byte> data, byte[] target, string message)
        {
            var arr = data.ToList();
            if (!arr.SequenceEqual(target))
            {
                throw new InvalidDataException(
                    $"{message}: [{Utils.BytesToHex(arr)}] != [{Utils.BytesToHex(target)}]");
            }
        }

        private static void DecryptInPlace(byte[] data)
        {
            Console.WriteLine("Decrypt");
            int m = 0x655f;
            for (int i = 0; i < data.Length; ++i)
            {
                data[i] ^= (byte)(m & 0xFF);
                m *= 0x4115;
            }
        }

        private static Byte[] ReadUtfPacket(BinaryReader reader)
        {
            var unknown = reader.ReadUInt32(); // 0x000000FF
            if (unknown != 0x00000FFU) Console.WriteLine($"Unknown u32 {unknown} != 0x000000FF");
            var size = reader.ReadInt64();
            if (size > int.MaxValue) throw new InvalidDataException($"Too large UTF size {size}");
            var utf = reader.ReadBytesExact((int)size);
            if (!utf.Take(4).SequenceEqual(UtfTag)) DecryptInPlace(utf);
            return utf;
        }

        private static UtfTable ParseUtf(byte[] utf)
        {
            using var reader = new BinaryReader(new MemoryStream(utf));
            CheckBytes(reader.ReadBytes(4), UtfTag, "UTF tag mismatch");
            var tableSize = reader.ReadBEInt32();
            if (tableSize + 8 != utf.Length) Console.WriteLine($"tableSize {tableSize} != {utf.Length - 8}?");
            var rowsStart = reader.ReadBEInt32() + 8;
            var textStart = reader.ReadBEInt32() + 8;
            var firstString = PeekString(reader, textStart);
            if (firstString != "<NULL>") Console.WriteLine("first string: " + firstString);
            var dataStart = reader.ReadBEInt32() + 8;
            var tableNameOffset = reader.ReadBEInt32();
            var columnCount = reader.ReadBEInt16();
            var rowSize = reader.ReadBEInt16();
            var rowCount = reader.ReadBEInt32();

            object ReadCell(ColumnType type)
            {
                return type switch
                {
                    ColumnType.Byte => reader.ReadByte(),
                    ColumnType.SByte => reader.ReadSByte(),
                    ColumnType.UInt16 => reader.ReadBEUInt16(),
                    ColumnType.Int16 => reader.ReadBEInt16(),
                    ColumnType.UInt32 => reader.ReadBEUInt32(),
                    ColumnType.Int32 => reader.ReadBEInt32(),
                    ColumnType.UInt64 => reader.ReadBEUInt64(),
                    ColumnType.Int64 => reader.ReadBEInt64(),
                    ColumnType.Single => reader.ReadBESingle(),
                    ColumnType.String => PeekString(reader, reader.ReadBEInt32()),
                    ColumnType.Bytes => PeekBytes(reader, dataStart + reader.ReadBEInt32(), reader.ReadBEInt32()),
                    _ => throw new InvalidDataException($"Unknown cell type: {type}")
                };
            }

            var tableName = PeekString(reader, textStart + tableNameOffset);

            var columns = new List<Column>();
            for (int i = 0; i < columnCount; ++i)
            {
                int flag = reader.ReadByte();
                if (flag == 0)
                {
                    Console.WriteLine("flag == 0");
                    flag = reader.ReadBEInt32();
                }
                var category = Utils.SafeCastEnum<ColumnCategory>((flag & 0xF0) >> 4);
                var type = Utils.SafeCastEnum<ColumnType>(flag & 0x0F);
                var nameOffset = reader.ReadBEInt32();
                var name = PeekString(reader, textStart + nameOffset);
                object? data = null;
                if (category == ColumnCategory.Constant)
                {
                    data = ReadCell(type);
                    Console.WriteLine($"Constant column data: {name} = {data}");
                }
                columns.Add(new Column(category, type, name, data));
            }
            if (reader.BaseStream.Position != rowsStart)
            {
                throw new InvalidDataException(
                    $"Column not followed by row data: {reader.BaseStream.Position} => {rowsStart}");
            }

            var rows = new List<dynamic>();
            for (int i = 0; i < rowCount; ++i)
            {
                var row = new ExpandoObject();
                reader.BaseStream.Position = rowsStart + rowSize * i;
                foreach (var column in columns)
                {
                    object? cell = column.Category switch
                    {
                        ColumnCategory.Empty => null,
                        ColumnCategory.Constant => column.Data,
                        ColumnCategory.Row => ReadCell(column.Type),
                        _ => throw new InvalidDataException($"Invalid column category {column.Category}")
                    };
                    if (!row.TryAdd(column.Name, cell))
                    {
                        throw new InvalidDataException($"Cannot add cell {cell} to {row}");
                    }
                }
                rows.Add(row);
            }
            if (reader.BaseStream.Position != textStart)
            {
                throw new InvalidDataException(
                    $"Rows not followed by text data: {reader.BaseStream.Position} => {textStart}");
            }

            return new UtfTable(tableName, columns.ToArray(), rows.ToArray());
        }

        private static IEnumerable<(int id, ItocEntry entry)> ReadItoc(
            BinaryReader reader,
            long contentOffset,
            long itocOffset)
        {
            reader.BaseStream.Position = itocOffset;
            CheckBytes(reader.ReadBytes(4), ItocTag, "Itoc tag mismatch");

            var table = ParseUtf(ReadUtfPacket(reader));

            if (table.Rows.Length == 0) yield break;
            
            if (table.Name != "CpkItocInfo") throw new InvalidDataException($"Expact CpkItocInfo, find {table.Name}");

            if (table.Rows.Length > 1) Console.WriteLine("Multi-entry itoc?");

            var info = table.Rows.First();

            var filesL = checked((int)info.FilesL);
            var filesH = checked((int)info.FilesL);
            var dataL = ParseUtf((byte[])info.DataL);
            var dataH = ParseUtf((byte[])info.DataH);

            foreach (var row in dataL.Rows)
            {
                int id = row.ID;
                int fileSize = row.FileSize;
                int extractSize = row.ExtractSize ?? fileSize;
                yield return (id, new ItocEntry{ Id = id, FileSize = fileSize, ExtractSize = extractSize, Low = true });
            }
            foreach (var row in dataH.Rows)
            {
                int id = row.ID;
                long fileSize = row.FileSize;
                long extractSize = row.ExtractSize ?? fileSize;
                yield return (id, new ItocEntry{ Id = id, FileSize = fileSize, ExtractSize = extractSize, Low = false });
            }

            yield break;
        }
        
        private Stream _stream;

        private List<ItocEntry> _itocEntries;

        public IReadOnlyList<ItocEntry> ItocEntries
            => _itocEntries.AsReadOnly();

        public Cpk(Stream stream)
        {
            _stream = stream;

            using var reader = new BinaryReader(stream, Encoding.Default, leaveOpen: true);
            CheckBytes(reader.ReadBytes(4), FileTag, "File tag mismatch");
            var headerTable = ParseUtf(ReadUtfPacket(reader));
            var header = headerTable.Rows[0];
            var contentOffset = checked((long)header.ContentOffset);
            var align = checked((int)header.Align);

            //Console.WriteLine(headerTable.Columns.First(c => c.Name == "ContentSize").Type);
            //Console.WriteLine(header.ContentSize);

            if (header.TocOffset != null)
            {
                throw new InvalidOperationException("Toc not support");
            }

            if (header.ItocOffset != null)
            {
                var itocs = new SortedDictionary<int, ItocEntry>();
                foreach (var (id, entry) in ReadItoc(reader, contentOffset, (long)header.ItocOffset))
                {
                    itocs.Add(id, entry);
                }
                long offset = contentOffset;
                foreach (var (id, entry) in itocs)
                {
                    var size = entry.FileSize;
                    entry.Offset = offset;
                    offset += size;
                    if (offset % align != 0) offset += align - (offset % align);
                }
                _itocEntries = new List<ItocEntry>(itocs.Values);
            }
            else
            {
                _itocEntries = new List<ItocEntry>();
            }

            if (header.EtocOffset != null)
            {
                throw new InvalidOperationException("Etoc not support");
            }
        }

        public void ExtractItoc(Stream dest, ItocEntry entry)
        {
            Span<byte> buffer = stackalloc byte[8];
            _stream.Position = entry.Offset;
            _stream.Read(buffer);
            if (buffer.SequenceEqual(LaylaTag)) throw new NotSupportedException("Layla decrypt not supported");
            dest.Write(buffer);
            WriteBytesTo(_stream, dest, entry.FileSize - 8);
        }

        public void ExtractItoc(string filePath, ItocEntry entry)
        {
            using var file = File.Create(filePath);
            ExtractItoc(file, entry);
        }

        public static Cpk FromFile(string path)
        {
            return new Cpk(File.OpenRead(path));
        }

        public void Dispose()
        {
            _stream.Dispose();
        }

    }
}