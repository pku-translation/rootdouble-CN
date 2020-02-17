using System;
using System.IO;
using System.Text;
using CsYetiTools.VnScripts;

namespace CsYetiTools
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
                foreach (var code in script.GetCodes<TextAreaCode>()) 
                {
                    if (code.IsEnArea)
                    {
                        code.Y = 24;
                    }
                }
            }

            enPackage.WriteTo(FilePath("sn.bin"));

        }
    }
}
