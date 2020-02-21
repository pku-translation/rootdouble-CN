using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            await Task.Run(() => {});
            

            // ------------------------------------------------------------------------

            // var package = Load("psv/sn.bin", false);
            // using var client = new TransifexClient();
            // foreach (var (i, script) in package.Scripts.WithIndex())
            // {
            //     if (i == 251) continue;

            //     if (script.Footer.ScriptIndex < 0) continue;

            //     var mainCodeIndex = script.Codes.First(c => c.Offset == script.Header.Entries[0].AbsoluteOffset).Index;
            //     if (mainCodeIndex == 0) continue;

            //     Console.WriteLine($"script {i}");

            //     var translations = JsonConvert.DeserializeObject<TranslationStringsPutInfo[]>(
            //         File.ReadAllText($"translations/chunk_{i}.json"),
            //         TransifexClient.JsonSettings)!;
                
            //     var resource = client.Project("rootdouble_steam_cn").Resource($"source-json-chunk-{i:0000}-json--master");

            //     Console.WriteLine(await resource.PutTranslationStrings("zh_CN", translations));
            // }
        }
    }
}
