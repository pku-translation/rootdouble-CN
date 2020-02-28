using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CsYetiTools.FileTypes
{
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
        private delegate void FileHandler(string path, byte[] data);

        private static void HandleXTX(string path, byte[] data)
        {
            //File.WriteAllBytes(path, data);
            try
            {
                new Xtx(data).SaveTo(path + ".png");
            }
            catch (NotSupportedException exc)
            {
                Console.WriteLine(exc);
            }
        }

        private static void HandleHCA(string path, byte[] data)
        {
            //File.WriteAllBytes(path, data);
        }

        private static void HandleShader(string path, byte[] data)
        {
            File.WriteAllBytes(path + ".shader", data);
        }

        private static void HandleNested(FileHandler innerHandler, string path, byte[] data)
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
                // nested
                while (ms.Position < firstEntry.offset)
                {
                    headers.Add((reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32()));
                }
                Console.WriteLine($"header: {headers.Count}");

                Utils.CreateOrClearDirectory(path);
                foreach (var (i, header) in headers.WithIndex())
                {
                    var bytes = reader.ReadBytesExact(header.size);
                    HandleNested(innerHandler, Path.Combine(path, i.ToString("0000")), bytes);
                }
            }
            else
            {
                // single file
                try
                {
                    innerHandler(path, data);
                }
                catch (Exception exc)
                {
                    File.WriteAllBytes(path + ".dump", data);
                    throw new InvalidDataException($"{path.Replace("\\", "/")} failed to load.", exc);
                }
            }
        }

        private static FileHandler[] FileHandlers =
        {
            HandleXTX,
            HandleHCA,
            HandleXTX,
            HandleXTX,
            HandleXTX,
            HandleXTX,
            HandleXTX,
            HandleXTX,
            HandleXTX,
            HandleShader,
            HandleXTX,
            HandleXTX,
            HandleXTX,
            HandleXTX,
            HandleXTX,
            HandleXTX,
        };

        public static void DumpSys(Cpk cpk, int index, string dirPath, int? countLimit = null)
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
            HandleNested(FileHandlers[index], Path.Combine(dirPath), data);
        }

        public static void DumpXTXs(Cpk cpk, string dirPath)
        {
            Utils.CreateOrClearDirectory(dirPath);
            foreach (var itoc in cpk.ItocEntries)
            {
                using var ms = new MemoryStream();
                cpk.ExtractItoc(ms, itoc);
                var data = ms.ToArray();

                if (data.Take(4).SequenceEqual(Xtx.FileTag))
                {
                    var path = Path.Combine(dirPath, $"{itoc.Id:00000}.png");
                    Console.WriteLine("Dumping: " + path + "...");
                    try
                    {
                        new Xtx(data).SaveTo(path);
                        Console.WriteLine(" done.");
                    }
                    catch (NotSupportedException exc)
                    {
                        Console.WriteLine(exc.Message);
                    }
                }
                else
                {
                    Console.WriteLine($"Unknown itoc: [{Utils.BytesToHex(data.Take(16))}] ...");
                }
            }
        }
    }
}