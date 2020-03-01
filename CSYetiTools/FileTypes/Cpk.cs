using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using CsYetiTools.IO;

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

        private static string PeekString(IBinaryStream reader, long pos)
        {
            var oldPos = reader.Position;
            reader.Position = pos;
            var bytes = new List<byte>();
            byte c;
            while ((c = reader.ReadByte()) != 0) bytes.Add(c);
            var str = Encoding.ASCII.GetString(bytes.ToArray());
            reader.Position = oldPos;
            return str;
        }

        private static byte[] PeekBytes(IBinaryStream reader, long pos, int size)
        {
            var oldPos = reader.Position;
            reader.Position = pos;
            var bytes = reader.ReadBytesExact(size);
            reader.Position = oldPos;
            return bytes;
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

        private static Byte[] ReadUtfPacket(IBinaryStream reader)
        {
            var unknown = reader.ReadUInt32LE(); // 0x000000FF
            if (unknown != 0x00000FFU) Console.WriteLine($"Unknown u32 {unknown} != 0x000000FF");
            var size = reader.ReadInt64LE();
            if (size > int.MaxValue) throw new InvalidDataException($"Too large UTF size {size}");
            var utf = reader.ReadBytesExact((int)size);
            if (!utf.Take(4).SequenceEqual(UtfTag)) DecryptInPlace(utf);
            return utf;
        }

        private static UtfTable ParseUtf(byte[] utf)
        {
            using var reader = new BinaryStream(utf);
            CheckBytes(reader.ReadBytesMax(4), UtfTag, "UTF tag mismatch");
            var tableSize = reader.ReadInt32BE();
            if (tableSize + 8 != utf.Length) Console.WriteLine($"tableSize {tableSize} != {utf.Length - 8}?");
            var rowsStart = reader.ReadInt32BE() + 8;
            var textStart = reader.ReadInt32BE() + 8;
            var firstString = PeekString(reader, textStart);
            if (firstString != "<NULL>") Console.WriteLine("first string: " + firstString);
            var dataStart = reader.ReadInt32BE() + 8;
            var tableNameOffset = reader.ReadInt32BE();
            var columnCount = reader.ReadInt16BE();
            var rowSize = reader.ReadInt16BE();
            var rowCount = reader.ReadInt32BE();

            object ReadCell(ColumnType type)
            {
                return type switch
                {
                    ColumnType.Byte => reader.ReadByte(),
                    ColumnType.SByte => reader.ReadSByte(),
                    ColumnType.UInt16 => reader.ReadUInt16BE(),
                    ColumnType.Int16 => reader.ReadInt16BE(),
                    ColumnType.UInt32 => reader.ReadUInt32BE(),
                    ColumnType.Int32 => reader.ReadInt32BE(),
                    ColumnType.UInt64 => reader.ReadUInt64BE(),
                    ColumnType.Int64 => reader.ReadInt64BE(),
                    ColumnType.Single => throw new NotSupportedException("float not supported"),
                    ColumnType.String => PeekString(reader, reader.ReadInt32BE()),
                    ColumnType.Bytes => PeekBytes(reader, dataStart + reader.ReadInt32BE(), reader.ReadInt32BE()),
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
                    flag = reader.ReadInt32BE();
                }
                var category = Utils.SafeCastEnum<ColumnCategory>((flag & 0xF0) >> 4);
                var type = Utils.SafeCastEnum<ColumnType>(flag & 0x0F);
                var nameOffset = reader.ReadInt32BE();
                var name = PeekString(reader, textStart + nameOffset);
                object? data = null;
                if (category == ColumnCategory.Constant)
                {
                    data = ReadCell(type);
                    Console.WriteLine($"Constant column data: {name} = {data}");
                }
                columns.Add(new Column(category, type, name, data));
            }
            if (reader.Position != rowsStart)
            {
                throw new InvalidDataException(
                    $"Column not followed by row data: {reader.Position} => {rowsStart}");
            }

            var rows = new List<dynamic>();
            for (int i = 0; i < rowCount; ++i)
            {
                var row = new ExpandoObject();
                reader.Position = rowsStart + rowSize * i;
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
            if (reader.Position != textStart)
            {
                throw new InvalidDataException(
                    $"Rows not followed by text data: {reader.Position} => {textStart}");
            }

            return new UtfTable(tableName, columns.ToArray(), rows.ToArray());
        }

        private static IEnumerable<(int id, ItocEntry entry)> ReadItoc(
            IBinaryStream reader,
            long contentOffset,
            long itocOffset)
        {
            reader.Position = itocOffset;
            CheckBytes(reader.ReadBytesMax(4), ItocTag, "Itoc tag mismatch");

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
                yield return (id, new ItocEntry { Id = id, FileSize = fileSize, ExtractSize = extractSize, Low = true });
            }
            foreach (var row in dataH.Rows)
            {
                int id = row.ID;
                long fileSize = row.FileSize;
                long extractSize = row.ExtractSize ?? fileSize;
                yield return (id, new ItocEntry { Id = id, FileSize = fileSize, ExtractSize = extractSize, Low = false });
            }

            yield break;
        }

        private BinaryStream _stream;

        private List<ItocEntry> _itocEntries;

        public IReadOnlyList<ItocEntry> ItocEntries
            => _itocEntries.AsReadOnly();

        public Cpk(Stream stream)
        {
            _stream = new BinaryStream(stream);

            CheckBytes(_stream.ReadBytesMax(4), FileTag, "File tag mismatch");
            var headerTable = ParseUtf(ReadUtfPacket(_stream));
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
                foreach (var (id, entry) in ReadItoc(_stream, contentOffset, (long)header.ItocOffset))
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

        private static IEnumerable<int> RepeatCounts()
        {
            yield return 2;
            yield return 3;
            yield return 5;
            while (true) yield return 8;
        }

        private static byte[] DecodeLayla(byte[] bytes)
        {
            using var reader = new BinaryStream(bytes);
            var rawSize = reader.ReadInt32LE();
            var newSize = reader.ReadInt32LE();
            var data = reader.ReadBytesExact(newSize);
            var prefix = reader.ReadToEnd();

            var bits = new MsbBitBuffer(data.Reverse());
            var result = new List<byte>();
            while (result.Count < rawSize)
            {
                if (bits.Get())
                {
                    var offset = (int)bits.Gets(13) + 3;
                    var count = 3;
                    foreach (var next in RepeatCounts())
                    {
                        var extra = bits.Gets(next);
                        count += (int)extra;
                        if (extra != (1uL << next) - 1)
                            break;
                    }
                    while (count-- > 0)
                        result.Add(result[result.Count - offset]);
                }
                else
                {
                    result.Add((byte)bits.Gets(8));
                }
            }
            if (result.Count != rawSize) throw new InvalidDataException($"result({result.Count}) != {rawSize}");

            return prefix.Concat(result.Reverse<byte>()).ToArray();
        }

        public void ExtractItoc(Stream dest, ItocEntry entry)
        {
            Span<byte> head = stackalloc byte[8];
            byte[] data;
            lock (_stream)
            {
                _stream.Position = entry.Offset;
                _stream.Read(head);
                data = _stream.ReadBytesExact((int)entry.FileSize - 8);
            }
            if (head.SequenceEqual(LaylaTag))
            {
                dest.Write(DecodeLayla(data));
            }
            else
            {
                dest.Write(head);
                dest.Write(data);
            }
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