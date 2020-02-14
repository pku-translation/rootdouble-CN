using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CSYetiTools
{
    class TestBed
    {
        private SnPackage? package;

        private SnPackage? packageSteam;

        private string? textOutputPath;

        public TestBed(SnPackage? package, SnPackage? packageSteam, string? textOutputPath)
        {
            this.package = package;
            this.packageSteam = packageSteam;
            this.textOutputPath = textOutputPath;
        }

        public void Run()
        {
            using var writer = new StreamWriter(textOutputPath!, false, Encoding.UTF8);

            // foreach (var s in package!.Scripts)
            // {
            //     writer.WriteLine(Utils.BytesToHex(s.Footer));
            // }

            SortedDictionary<byte, int> CountCodes(CodeScript script)
            {
                var dict = new SortedDictionary<byte, int>();
                foreach (var code in script.Codes)
                {
                    if (dict.ContainsKey(code.Code)) ++dict[code.Code];
                    else dict.Add(code.Code, 1);
                }
                return dict;
            }

            var d137 = CountCodes(package!.Scripts[137]);

            foreach (var (i, s) in package!.Scripts.WithIndex())
            {
                //if (i != 268) continue;

                var int1 = s.Footer.Int1;


                var dialogs = s.Codes.Count(c => c is OpCodes.DialogCode dialogCode && dialogCode.IsIndexed);
                var dialogs2 = s.Codes.Count(c => c is OpCodes.ExtraDialogCode exDialogCode && exDialogCode.IsDialog);

                var counter = dialogs + dialogs2;

                if (int1 == counter) {
                    continue;
                }
                Console.WriteLine($"{i} mismatch");

                writer.WriteLine($"Script {i}");
                writer.WriteLine($"{int1}");

                writer.Write($"{counter}");
                if (counter > int1) writer.Write($" +{counter - int1}");
                else if (counter < int1) writer.Write($" -{int1 - counter}");

                writer.WriteLine();
                writer.WriteLine("-----------------------------------------------------------------------------");
            }

        }
    }
}
