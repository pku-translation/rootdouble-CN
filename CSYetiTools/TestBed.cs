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
        private static SnPackage Load(string path, bool isStringPooled)
        {
            var rpath = Path.GetRelativePath(Directory.GetCurrentDirectory(), path);
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

        public static async Task Run()
        {
            await Task.Run(() => { });

            // var peeker = ExecutableStringPeeker.FromFile("steam/executable", Utils.Cp932);
            // using var stream = new MemoryStream(File.ReadAllBytes("steam/executable"));
            // peeker.Modify(stream, Utils.Cp932, "string_pool");
            // stream.Position = 0;
            // using var file = File.Create("jpexe");
            // stream.WriteTo(file);

            // ----------------- sys --------------------------
            // using var cpk = Cpk.FromFile("steam/sys.cpk");
            // for (int i = 0; i < 16; ++i)
            // {
            //     Console.WriteLine($"# decoding sys.cpk {i}...");
            //     SysCpkHelper.DumpContent(cpk, i, $"steam_sys/{i:0000}");
            // }
            //SysCpkHelper.DumpContent(cpk, 0, "steam_sys/test0");

            // ----------------- bgs ---------------------------
            using var cpk = Cpk.FromFile("steam/bgs.cpk");
            Utils.CreateOrClearDirectory("steam_bgs");
            foreach (var itoc in cpk.ItocEntries)
            {
                using var ms = new MemoryStream();
                cpk.ExtractItoc(ms, itoc);
                var data = ms.ToArray();
                
                if (data.Take(4).SequenceEqual(Xtx.FileTag))
                {
                    try {
                        new Xtx(data).SaveTo($"steam_bgs/{itoc.Id:00000}.png");
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

