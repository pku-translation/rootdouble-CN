using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CsYetiTools.FileTypes;
using CsYetiTools.Transifex;
using CsYetiTools.VnScripts;
using Newtonsoft.Json;

namespace CsYetiTools
{
    class TestBed
    {
        private static SnPackage Load(FilePath path, bool isStringPooled)
        {
            var rpath = path.ToRelative();
            Console.Write("Loading package " + rpath + " ... ");
            Console.Out.Flush();

            var stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();
            var package = new SnPackage(path, isStringPooled);
            stopwatch.Stop();

            Console.WriteLine($"{stopwatch.Elapsed.TotalMilliseconds} ms");
            return package;
        }

        private static void SaveModifiers(string file, IDictionary<int, StringListModifier[]> modifiers)
        {
            using (var writer = new StreamWriter(file))
            {
                foreach (var (k, v) in modifiers)
                {
                    writer.WriteLine();
                    writer.WriteLine($"(script {k}");
                    foreach (var m in v.SkipLast(1))
                    {
                        writer.Write("    ");
                        writer.WriteLine(m.ToSExpr().ToString("    "));
                    }
                    writer.Write("    "); writer.Write(v.Last().ToSExpr().ToString("    "));
                    writer.WriteLine(")");
                }
            }
        }

        private static long GetX(int i, long width, byte level)
        {
            int v1 = (level >> 2) + (level >> 1 >> (level >> 2));
            int v2 = i << v1;
            int v3 = (v2 & 0x3F) + ((v2 >> 2) & 0x1C0) + ((v2 >> 3) & 0x1FFFFE00);
            return ((((level << 3) - 1) & ((v3 >> 1) ^ ((v3 ^ (v3 >> 1)) & 0xF))) >> v1)
                + ((((((v2 >> 6) & 0xFF) + ((v3 >> (v1 + 5)) & 0xFE)) & 3)
                    + (((v3 >> (v1 + 7)) % (((width + 31)) >> 5)) << 2)) << 3);
        }
        private static long GetY(int i, long width, byte level)
        {
            int v1 = (level >> 2) + (level >> 1 >> (level >> 2));
            int v2 = i << v1;
            int v3 = (v2 & 0x3F) + ((v2 >> 2) & 0x1C0) + ((v2 >> 3) & 0x1FFFFE00);
            return ((v3 >> 4) & 1)
                + ((((v3 & ((level << 6) - 1) & -0x20)
                    + ((((v2 & 0x3F)
                        + ((v2 >> 2) & 0xC0)) & 0xF) << 1)) >> (v1 + 3)) & -2)
                + ((((v2 >> 10) & 2) + ((v3 >> (v1 + 6)) & 1)
                    + (((v3 >> (v1 + 7)) / ((width + 31) >> 5)) << 2)) << 3);
        }

        public static async Task Run()
        {
            await Task.Run(() => { });

            // var peeker = ExecutableStringPeeker.FromFile("steam/executable", Utils.Cp932);
            // using var stream = new MemoryStream(File.ReadAllBytes("steam/executable"));
            // peeker.Modify(stream, Utils.Cp932, "string_pool");
            // stream.Position = 0;
            // using var file = File.Create("jpexe");
            // stream.WriteTo(file);

            var buffer = new MsbBitBuffer(new byte[]{ 0b10101111, 0b00001011, 0b00100101, 0b11111111 }, 31);
            Console.WriteLine(Utils.BytesToHex(buffer.ToBytes()));
        }
    }
}

