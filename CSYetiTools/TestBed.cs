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

        }
    }
}

