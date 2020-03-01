using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CsYetiTools.FileTypes
{
    public enum CpkFileType
    {
        Xtx,
        Hca,
        Shader,
        Unknown,
    }

    public class SysCpkEntry
    {
        public CpkFileType Type { get; }
        public object Content { get; }
        public FilePath Path { get; }

        public SysCpkEntry(CpkFileType type, FilePath path, object content)
        {
            Type = type;
            Path = path;
            Content = content;
        }
    }

    public static class CpkHelper
    {
        // file-id  lzss    content
        //
        //    0      1       Main UI & SSS
        //    1      0       HCAs 
        //    2      1       Intro UI
        //    3      1       Main menu
        //    4      1       Options
        //    5      1       Help
        //    6      1       Album
        //    7      1       Music
        //    8      1       Playing log
        //    9      1       shaders
        //   10      1       "Phantom", not cleaned
        //   11      1       Xpisode
        //   12      1       RAM
        //   13      1       Map
        //   14      1       Tips
        //   15      1       RAM tutorial

        private static SysCpkEntry HandleXTX(FilePath path, byte[] data)
        {
            try
            {
                return new SysCpkEntry(
                    CpkFileType.Xtx,
                    path + ".png",
                    new Xtx(data)
                );
            }
            catch (NotSupportedException exc)
            {
                Console.WriteLine(exc.Message);
                return new SysCpkEntry(
                    CpkFileType.Unknown,
                    path + ".xtx",
                    data
                );
            }
        }

        private static SysCpkEntry HandleHCA(FilePath path, byte[] data)
        {
            // not implemented
            return new SysCpkEntry(CpkFileType.Unknown, path + ".hca", data);
        }

        private static SysCpkEntry HandleSingleFile(FilePath path, byte[] data)
        {
            var flag = data.Take(4).ToArray();
            if (flag.SequenceEqual(Xtx.FileTag)) return HandleXTX(path, data);
            if (flag.SequenceEqual(Hca.FileTag)) return HandleHCA(path, data);

            return new SysCpkEntry(CpkFileType.Unknown, path + ".dump", data);
        }

        private static IEnumerable<SysCpkEntry> HandleNested(
            FilePath path,
            byte[] data)
        {
            using var ms = new MemoryStream(data);
            using var reader = new BinaryReader(ms);

            var firstEntry = (
                offset: reader.ReadInt32(),
                size: reader.ReadInt32(),
                unknown1: reader.ReadInt32(),
                unknown2: reader.ReadInt32()
            );
            var headers = new List<(int offset, int size, int unknown1, int unknown2)> { firstEntry };

            // guess content
            if (firstEntry.unknown1 == 0 && firstEntry.unknown2 == 0)
            {
                while (ms.Position < firstEntry.offset)
                {
                    headers.Add((reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32()));
                }
                foreach (var (i, header) in headers.WithIndex())
                {
                    var bytes = reader.ReadBytesExact(header.size);
                    foreach (var subFile in HandleNested(path / i.ToString("0000"), bytes))
                        yield return subFile;
                }
            }
            else
            {
                // single file
                SysCpkEntry entry;
                try
                {
                    entry = HandleSingleFile(path, data);
                }
                catch (Exception exc)
                {
                    File.WriteAllBytes(path + ".dump", data);
                    throw new InvalidDataException($"{path} failed to load.", exc);
                }
                yield return entry;
            }
        }

        public static IEnumerable<SysCpkEntry> EnumerateSysFiles(Cpk cpk, int index)
        {
            if (cpk.ItocEntries.Count != 16)
            {
                throw new InvalidDataException($"{cpk.ItocEntries.Count} != 16");
            }

            byte[] data;
            using (var ms = new MemoryStream())
            {
                cpk.ExtractItoc(ms, cpk.ItocEntries[index]);
                if (index != 1) // lzss
                {
                    Span<byte> span = stackalloc byte[4];
                    ms.Position = 0;
                    ms.Read(span);
                    int size = BitConverter.ToInt32(span);
                    data = LZSS.Decode(ms.StreamAsIEnumerable()).ToArray();
                    if (data.Length != size)
                    {
                        throw new InvalidDataException($"itoc {index} decoded size {data.Length} != {size}");
                    }
                }
                else
                {
                    data = new byte[ms.Length];
                    ms.Position = 0;
                    ms.Read(data);
                }
            }
            return HandleNested("", data);
        }

        public static void DumpSys(Cpk cpk, int index, FilePath dirPath)
        {
            Parallel.ForEach(EnumerateSysFiles(cpk, index), entry =>
            {
                var path = dirPath / entry.Path;
                var parent = path.Parent;
                if (!Directory.Exists(parent)) Directory.CreateDirectory(parent);
                switch (entry.Content)
                {
                    case Xtx xtx:
                        xtx.SaveTo(path);
                        break;
                    case byte[] bytes:
                        File.WriteAllBytes(path, bytes);
                        break;
                    default:
                        throw new InvalidDataException($"Unknonw content type: {entry.Content.GetType().FullName}");
                }
            });
        }

        public static void DumpSys(Cpk cpk, FilePath dirPath)
        {
            Utils.CreateOrClearDirectory(dirPath);
            Parallel.For(0, 16, i => 
            {
                Console.WriteLine($"Dumping: {i:00} ... ");
                DumpSys(cpk, i, dirPath / $"{i:00}");
            });
        }

        public static void DumpCpk(Cpk cpk, FilePath dirPath)
        {
            void HandleContent(FilePath path, object content)
            {
                Console.WriteLine($"Dumping: {path} ... ");
                try
                {
                    switch (content)
                    {
                        case Xtx xtx:
                            xtx.SaveTo(path);
                            break;
                        case byte[] bytes:
                            File.WriteAllBytes(path, bytes);
                            break;
                        default:
                            throw new InvalidDataException($"Unknonw content type: {content.GetType().FullName}");
                    }
                }
                catch (NotSupportedException exc)
                {
                    Console.WriteLine(exc.Message);
                }
            }

            Utils.CreateOrClearDirectory(dirPath);
            Parallel.ForEach(cpk.ItocEntries, itoc =>
            {
                var itocStr = "Itoc" + itoc.Id.ToString(cpk.ItocEntries.Count > 10000 ? "000000" : "0000");

                using var ms = new MemoryStream();
                cpk.ExtractItoc(ms, itoc);
                var data = ms.ToArray();

                var files = HandleNested("", data).ToList();
                if (files.Count > 1)
                {
                    var itocDirPath = dirPath / itocStr;
                    Utils.CreateOrClearDirectory(itocDirPath);
                    foreach (var entry in files)
                    {
                        var path = itocDirPath / entry.Path;
                        HandleContent(path, entry.Content);
                    }
                }
                else
                {
                    var entry = files.First();
                    var path = dirPath / (itocStr + "_" + entry.Path);
                    HandleContent(path, entry.Content);
                }
            });
        }
    }
}