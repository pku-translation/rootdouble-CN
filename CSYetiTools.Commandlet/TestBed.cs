using System;
using System.Threading.Tasks;
using CSYetiTools.Base;
using CSYetiTools.VnScripts;
using System.Collections.Generic;

namespace CSYetiTools.Commandlet
{
    internal class TestBed
    {
        private static SnPackage Load(FilePath path, bool isStringPooled)
        {
            var rpath = path.ToRelative();
            Console.Write("Loading package " + rpath + " ... ");
            Console.Out.Flush();

            return Utils.Time(() => new SnPackage(path, isStringPooled));
        }

        class DupEntry
        {
            public int Chunk { get; set; }
            public int Index { get; set; }
            public int DupCounter { get; set; }
        }

        public static async Task Run()
        {
            await Task.Run(() => { });

            //Load("ps3/sn.bin", false).Dump("ps3_sn", false, true);
            //Load("psv/sn.bin", false).Dump("psv_sn", true, true);
            //Load("steam/sn.bin", true).Dump("steam_sn", true, true);

            var package = Load("steam/sn.bin", true);
            var jpPackage = Load("psv/sn.bin", false);
            package.ReplaceStringTable(jpPackage, StringListModifier.LoadFile("string_list_modifiers.sexp"));
            package.ApplyTranslations("../source_json/", "../zh_CN/", true);

            //var graphs = new List<RawGraph>();
            //foreach (var (i, script) in package.Scripts.WithIndex()) {
            //    graphs.Add(new RawGraph(i, script));
            //}

            //new RawGraph(337, package.Scripts[337]).ToGraph().Save("test337.yaml");
            RawGraph.LoadPackage(package, false).WithIndex().ForEach(p => p.element?.Save($"graphs/{p.index:0000}.yaml"));
        }
    }
}
