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
            // using var writer = new StreamWriter(textOutputPath!, false, Encoding.UTF8);

            // foreach (var s in package!.Scripts)
            // {
            //     writer.WriteLine(Utils.BytesToHex(s.Footer));
            // }
        }
    }
}
