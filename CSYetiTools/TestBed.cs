using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CsYetiTools.Transifex;
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

        public async Task Run()
        {
            var jpPath = FilePath("psv/sn.bin");
            var enPath = FilePath("steam/sn.bin");
            var modifierFilePath = FilePath("string_list_modifiers.sexpr");
            using var writer = new StreamWriter(FilePath("test_bed.txt"), false, Encoding.UTF8);

            var jpPackage = Load(jpPath, false);
            var enPackage = Load(enPath, true);
            await Task.Run(() => { });

            

            // ----------------------------------------------------------------------

            // foreach (var (i, script) in jpPackage.Scripts.WithIndex())
            // {
            //     var header = script.Header;
            //     if (header.Entries.Length <= 1) continue;

            //     var start = header.Entries[0].AbsoluteOffset;
            //     var exStart = header.Entries.Min(e => e.AbsoluteOffset);

            //     if (exStart == start) continue;

            //     foreach (var c in script.Codes)
            //     {
            //         if (c.Offset < start) {
            //             if (c is StringCode) Console.WriteLine(c);
            //             if (c is IHasAddress addressCode)
            //             {
            //                 foreach (var addr in addressCode.GetAddresses())
            //                 {
            //                     if (addr.AbsoluteOffset >= start)
            //                     {
            //                         Console.WriteLine($"{i,4}: {c.Index,4}|0x{c.Offset:X08} {c}");
            //                     }
            //                 }
            //             }
            //         }
            //     }
            // }

            // -------------------------------------------------------------------------------------------

            // var client = new TransifexClient();
            // var resource = client.Resource("rootdouble_steam_cn", $"source-json-chunk-{251:0000}-json--master");

            // var trans = await resource.GetTranslations("zh_CN", TranslationMode.OnlyTranslated);

            // // var json = await resource.Test("translation/zh_CN/", new { mode = "translator" });
            // // var content = Newtonsoft.Json.JsonConvert.DeserializeAnonymousType(json, new{ Content = "" }).Content;

            // // Console.WriteLine(content);

            // var source = jpPackage.Scripts[251].EnumerateTranslateSources().ToList();
            // Console.WriteLine($"{trans.Count} vs {source.Count}");

            // string ToStr(Transifex.TranslationInfo v)
            // {
            //     if (string.IsNullOrWhiteSpace(v.DeveloperComment))
            //         return $"[{v.Code}] {v.Context} | {(string.IsNullOrWhiteSpace(v.String) ? "<empty>" : v.String)}";
            //     else
            //         return $"[{v.Code}] {v.Context} | {v.DeveloperComment}: {(string.IsNullOrWhiteSpace(v.String) ? "<empty>" : v.String)}";
            // }

            // foreach (var (i, (src, (k, v))) in source.ZipTuple(trans).WithIndex())
            // {
            //     Console.WriteLine(" ---- " + ToStr(src));
            //     Console.WriteLine("      " + ToStr(v));
            // }

            // ----------------------------------------------------------------------------------

            // var pinfo = await client.GetProject("rootdouble_steam_cn");
            // Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(pinfo));

            // var info = await client.GetResource("rootdouble_steam_cn", $"source-json-chunk-{251:0000}-json--master");
            // Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(info));

            // enPackage.ReplaceStringTable(jpPackage, StringListModifier.Load(modifierFilePath));

            // foreach (var script in enPackage.Scripts)
            // {
            //     foreach (var code in script.GetCodes<TextAreaCode>()) 
            //     {
            //         if (code.IsEnArea)
            //         {
            //             code.Y = 24;
            //         }
            //     }
            // }

            // enPackage.WriteTo(FilePath("sn.bin"));

        }
    }
}
