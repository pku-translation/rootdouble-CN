using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CSYetiTools
{
    class TestBed
    {
        private SnPackage? package;

        private SnPackage? packageSteam;

        public TestBed(SnPackage? package, SnPackage? packageSteam)
        {
            this.package = package;
            this.packageSteam = packageSteam;
        }

        public void Run()
        {
        }
    }
}