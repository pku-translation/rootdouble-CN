using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CsYetiTools.FileTypes;
using CsYetiTools.VnScripts;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.Primitives;
using static CsYetiTools.Utils;

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

        public static async Task Run()
        {
            await Task.Run(() => { });

            Load("ps3/sn.bin", false).Dump("ps3_sn", false, true);
            //Load("psv/sn.bin", false).Dump("psv_sn", true, true);
            //Load("steam/sn.bin", true).Dump("steam_sn", true, true);
        }
    }
}

