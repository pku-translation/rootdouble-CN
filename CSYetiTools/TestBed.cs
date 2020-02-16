using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CSYetiTools.OpCodes;

namespace CSYetiTools
{
    class TestBed
    {
        private string dataPath;

        private SnPackage Load(string path, bool isStringPooled)
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

        public TestBed(string dataPath)
        {
            this.dataPath = dataPath;
        }

        private string FilePath(string file)
            => Path.Combine(dataPath, file);

        public void Run()
        {
            var jpPath = FilePath("sn_jp_psv.bin");
            var enPath = FilePath("sn_en_steam.bin");
            var modifierFilePath = FilePath("string_list_modifiers.sexpr");
            using var writer = new StreamWriter(FilePath("test_bed.txt"), false, Encoding.UTF8);

            var jpPackage = Load(jpPath, false);
            var enPackage = Load(enPath, true);

            enPackage.ReplaceStringTable(jpPackage, StringListModifier.Load(modifierFilePath));
            
            foreach (var script in enPackage.Scripts)
            {
                foreach (var code in script.Codes) 
                {
                    if (code is TextAreaCode c && c.IsTestTarget)
                    {
                        c.ChangeArgs(0x0A, 0x00, 0x64, 0x00, 0x00, 0x00, 0x38, 0x04, 0x60, 0x00);
                    }
                }
            }

            enPackage.WriteTo(FilePath("sn.bin"));
            enPackage.Dump(FilePath("test_dump/"), "e", false, true);

        }
    }
}
