using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CommandLine;
using Newtonsoft.Json.Linq;
using Flurl;
using Flurl.Util;
using Flurl.Http;

namespace CSYetiTools
{
    class Program
    {
        [Verb("test-bed", HelpText = "Test a sn.bin")]
        class TestBedOptions
        {
            [Option("input", Default = null)]
            public string Input { get; set; } = "";

            [Option("input-steam", Default = null)]
            public string InputSteam { get; set; } = "";
        }

        [Verb("gen-string-compare", HelpText = "Generate string compare file")]
        class GenStringCompareOptions
        {
            [Option("input")]
            public string Input { get; set; } = "";

            [Option("input-steam")]
            public string InputSteam { get; set; } = "";

            [Option("outputdir")]
            public string OutputDir { get; set; } = "";

            [Option("modifier-file")]
            public string ModifierFile { get; set; } = "string_list_modifiers.sexpr";
        }

        [Verb("gen-code-compare", HelpText = "Generate code compare file")]
        class GenCodeCompareOptions
        {
            [Option("input")]
            public string Input { get; set; } = "";

            [Option("input-steam")]
            public string InputSteam { get; set; } = "";

            [Option("outputdir")]
            public string OutputDir { get; set; } = "";

            [Option("modifier-file")]
            public string ModifierFile { get; set; } = "string_list_modifiers.sexpr";
        }

        [Verb("encode-sn", HelpText = "Encode sn.bin with files in specified folder")]
        class EncodeSnOptions
        {
            [Option("inputdir")]
            public string InputDir { get; set; } = "";

            [Option("output", Default = "sn.bin")]
            public string Output { get; set; } = "";

            [Option("steam")]
            public bool IsSteam { get; set; }
        }

        [Verb("decode-sn", HelpText = "Decode sn.bin to specified folder")]
        class DecodeSnOptions
        {
            [Option("input", Default = "sn.bin")]
            public string Input { get; set; } = "";

            [Option("outputdir", Default = "./")]
            public string OutputDir { get; set; } = "";

            [Option("steam")]
            public bool IsSteam { get; set; }

            [Option("dump-binary")]
            public bool IsDumpBinary { get; set; }

            [Option("dump-script")]
            public bool IsDumpScript { get; set; }
        }

        [Verb("decode-script", HelpText = "Decode script file")]
        class DecodeScriptOptions
        {
            [Option("input")]
            public string Input { get; set; } = "";

            [Option("output", Default = "")]
            public string Output { get; set; } = "";

            [Option("steam", Default = false)]
            public bool IsSteam { get; set; }
        }

        [Verb("replace-string-list", HelpText = "Replace string list of steam version")]
        class ReplaceStringListOptions
        {
            [Option("input-steam")]
            public string InputSteam { get; set; } = "";

            [Option("input-ref")]
            public string InputRef { get; set; } = "";

            [Option("modifiers")]
            public string ModifiersFile { get; set; } = "";

            [Option("output")]
            public string Output { get; set; } = "";

            [Option("dump-result-text-path", Default = null)]
            public string DumpResultTextPath { get; set; } = "";
        }

        [Verb("dump-trans-source", HelpText = "DumpTranslateSource")]
        class DumpTranslateSourceOptions
        {
            [Option("input", Default = "sn.bin")]
            public string Input { get; set; } = "";

            [Option("input-ref")]
            public string InputRef { get; set; } = "";

            [Option("outputdir", Default = "./")]
            public string OutputDir { get; set; } = "";

            [Option("modifier-file")]
            public string ModifiersFile { get; set; } = "string_list_modifiers.sexpr";
        }

        [Verb("fill-fx-dups", HelpText = "FillTransifexDuplicatedStrings")]
        class FillTransifexDuplicatedStringsOptions
        {

            [Option("input", Default = "sn.bin")]
            public string Input { get; set; } = "";

            [Option("input-ref")]
            public string InputRef { get; set; } = "";

            [Option("modifier-file")]
            public string ModifiersFile { get; set; } = "string_list_modifiers.sexpr";

            [Option("url")]
            public string Url { get; set; } = "";

            [Option("token", Default = "")]
            public string Token { get; set; } = "";

            [Option("pattern", Default = ".*")]
            public string Pattern { get; set; } = ".*";
        }

        static async Task Main(string[] args)
        {
            await Parser.Default.ParseArguments<
                TestBedOptions,
                EncodeSnOptions,
                DecodeSnOptions,
                DecodeScriptOptions,
                GenStringCompareOptions,
                GenCodeCompareOptions,
                ReplaceStringListOptions,
                DumpTranslateSourceOptions,
                FillTransifexDuplicatedStringsOptions
            >(args).MapResult(
                (TestBedOptions o) => Task.Run(() => TestBed(o)),
                (EncodeSnOptions o) => Task.Run(() => EncodeSn(o)),
                (DecodeSnOptions o) => Task.Run(() => DecodeSn(o)),
                (DecodeScriptOptions o) => Task.Run(() => DecodeScript(o)),
                (GenStringCompareOptions o) => Task.Run(() => GenStringCompare(o)),
                (GenCodeCompareOptions o) => Task.Run(() => GenCodeCompare(o)),
                (ReplaceStringListOptions o) => Task.Run(() => ReplaceStringList(o)),
                (DumpTranslateSourceOptions o) => Task.Run(() => DumpTranslateSource(o)),
                (FillTransifexDuplicatedStringsOptions o) => FillTransifexDuplicatedStrings(o),
                errs => Task.Run(() => errs.ForEach(HandleError))
            );

            Console.WriteLine("Done.");
        }

        private static void TestBed(TestBedOptions options)
        {
            SnPackage? package = null;
            SnPackage? packageSteam = null;

            var stopwatch = new System.Diagnostics.Stopwatch();
            if (!string.IsNullOrWhiteSpace(options.Input))
            {
                Console.WriteLine("Loading package " + options.Input + " ... ");
                stopwatch.Start();
                package = new SnPackage(options.Input, isSteam: false);
                stopwatch.Stop();
                Console.WriteLine($"{stopwatch.Elapsed.TotalMilliseconds} ms");
            }
            if (!string.IsNullOrWhiteSpace(options.InputSteam))
            {
                stopwatch.Restart();
                Console.WriteLine("Loading steam package " + options.InputSteam + " ... ");
                Console.Out.Flush();
                packageSteam = new SnPackage(options.InputSteam, isSteam: true);
                stopwatch.Stop();
                Console.WriteLine($"{stopwatch.Elapsed.TotalMilliseconds} ms");
            }
            new TestBed(package, packageSteam).Run();
        }

        private static void EncodeSn(EncodeSnOptions options)
        {
            var package = SnPackage.CreateFrom(options.InputDir, options.IsSteam);
            package.WriteTo(options.Output);
        }

        private static void DecodeSn(DecodeSnOptions options)
        {
            Console.WriteLine($"Decoding {options.Input} --> {options.OutputDir} ...");
            var package = new SnPackage(options.Input, options.IsSteam);

            package.Dump(options.OutputDir, Path.GetFileNameWithoutExtension(options.Input), options.IsDumpBinary, options.IsDumpScript);
        }

        private static void DecodeScript(DecodeScriptOptions options)
        {
            var script = new CodeScript(File.ReadAllBytes(options.Input), options.IsSteam);
            if (script.ParserError != null) Console.WriteLine(script.ParserError);
            script.DumpText(options.Output);
        }

        private static void GenStringCompare(GenStringCompareOptions options)
        {
            Console.WriteLine($"Gen string compare --> {options.OutputDir}");

            void DumpText(string path, IEnumerable<CodeScript.StringReferenceEntry> entries)
            {
                using var writer = new StreamWriter(path);
                foreach (var (i, entry) in entries.WithIndex())
                {
                    //writer.WriteLine($"{i, 3}: {entry.Index, 3}| [{entry.Code:X02}] [{entry.Content}]");
                    writer.WriteLine($"{i,3}: {entry.Index,4}| [{entry.Code:X02}] [{entry.Content}]");
                }
            }

            var package = new SnPackage(options.Input, isSteam: false);
            var packageSteam = new SnPackage(options.InputSteam, isSteam: true);

            var modifierTable = StringListModifier.Load(options.ModifierFile);

            var dumpDir = new DirectoryInfo(options.OutputDir);
            if (dumpDir.Exists)
            {
                foreach (var file in dumpDir.EnumerateFiles()) file.Delete();
            }
            else
            {
                dumpDir.Create();
            }
            using var writer = new StreamWriter(Path.Combine(options.OutputDir, "compare-result.txt"));
            int n = 0;
            foreach (var (i, (s1, s2)) in package.Scripts.ZipTuple(packageSteam.Scripts).WithIndex())
            {
                //var c1s = s1.GenerateStringReferenceList(raplaceExcepts.TryGetValue(i, out var except) ? except : null);
                var c1s = s1.GenerateStringReferenceList(modifierTable.TryGetValue(i, out var modifier) ? modifier : null);
                var c2s = s2.GenerateStringReferenceList();

                bool same = true;
                foreach (var (c1, c2) in c1s.ZipTuple(c2s))
                {
                    if (c1.Code != c2.Code)
                    {
                        same = false;
                        writer.WriteLine($"script {i}: {c1.Index,3}[{c1.Code:X02}] != {c2.Index,3}[{c2.Code:X02}], {c1.Content} & {c2.Content}");
                    }
                }
                if (same)
                {
                    if (c1s.Count != c2s.Count)
                    {
                        ++n;
                        writer.WriteLine($"script {i}: codes not equal {c1s.Count} != {c2s.Count}");
                        writer.WriteLine("========================================================");
                    }
                }
                else
                {
                    ++n;
                    writer.WriteLine("========================================================");
                }
                DumpText(Path.Combine(options.OutputDir, $"chunk_{i:0000}_ref.txt"), c1s);
                DumpText(Path.Combine(options.OutputDir, $"chunk_{i:0000}_steam.txt"), c2s);
            }
            writer.WriteLine($"{n} different files");
        }

        private static void GenCodeCompare(GenCodeCompareOptions options)
        {
            Console.WriteLine($"Gen code compare --> {options.OutputDir}");

            var package = new SnPackage(options.Input, isSteam: false);
            var packageSteam = new SnPackage(options.InputSteam, isSteam: true);

            var dumpDir = new DirectoryInfo(options.OutputDir);
            if (dumpDir.Exists)
            {
                foreach (var file in dumpDir.EnumerateFiles()) file.Delete();
            }
            else
            {
                dumpDir.Create();
            }
            foreach (var (i, (s1, s2)) in package.Scripts.ZipTuple(packageSteam.Scripts).WithIndex())
            {
                var writer1 = new StringWriter();
                var writer2 = new StringWriter();
                s1.DumpText(writer1, "{nostrcode}", footer: false);
                s2.DumpText(writer2, "{nostrcode}", footer: false);

                var str1 = writer1.ToString();
                var str2 = writer2.ToString();
                if (str1 != str2)
                {
                    File.WriteAllText(Path.Combine(options.OutputDir, $"chunk_{i:0000}_code_ref.txt"), str1, Encoding.UTF8);
                    File.WriteAllText(Path.Combine(options.OutputDir, $"chunk_{i:0000}_code_steam.txt"), str2, Encoding.UTF8);
                }
            }
        }

        private static SnPackage GenerateStringReplacedPackage(string input, string inputRef, string modifiersFile)
        {
            var package = new SnPackage(input, isSteam: true);
            var refPackage = new SnPackage(inputRef, isSteam: false);

            var modifierDict = StringListModifier.Load(modifiersFile);

            foreach (var (i, (script, refScript)) in package.Scripts.ZipTuple(refPackage.Scripts).WithIndex())
            {
                var refList = modifierDict.TryGetValue(i, out var modifiers)
                    ? refScript.GenerateStringReferenceList(modifiers)
                    : refScript.GenerateStringReferenceList();

                script.ReplaceStringTable(refList);
            }

            return package;
        }

        private static void ReplaceStringList(ReplaceStringListOptions options)
        {
            var package = GenerateStringReplacedPackage(options.InputSteam, options.InputRef, options.ModifiersFile);

            package.WriteTo(options.Output);

            var dumpPath = options.DumpResultTextPath;
            if (dumpPath != null)
            {
                if (Directory.Exists(dumpPath)) File.Delete(Path.Combine(dumpPath, "*"));
                else Directory.CreateDirectory(dumpPath);
                package.Dump(dumpPath, Path.GetFileNameWithoutExtension(options.Output), isDumpBinary: false, isDumpScript: true);
            }
        }

        private static void DumpTranslateSource(DumpTranslateSourceOptions options)
        {
            var package = GenerateStringReplacedPackage(options.Input, options.InputRef, options.ModifiersFile);

            package.DumpTranslateSource(options.OutputDir);
        }

        class DupEntry
        {
            public int Chunk { get; set; }
            public int Index { get; set; }
            public int DupCounter { get; set; }
        }

        private static async Task FillTransifexDuplicatedStrings(FillTransifexDuplicatedStringsOptions options)
        {
            string? token = options.Token;
            if (string.IsNullOrWhiteSpace(token))
            {
                token = Environment.GetEnvironmentVariable("TX_TOKEN");
            }
            if (string.IsNullOrWhiteSpace(token))
            {
                Console.WriteLine("No API token found, please use env TX_TOKEN or args --token to specify API token.");
                return;
            }

            var package = GenerateStringReplacedPackage(options.Input, options.InputRef, options.ModifiersFile);

            var regex = new Regex(options.Pattern, RegexOptions.Compiled | RegexOptions.Singleline);

            var md5 = System.Security.Cryptography.MD5.Create();
            string GetMd5Hash(string input)
            {
                var data = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
                var sBuilder = new StringBuilder();
                for (int i = 0; i < data.Length; i++)
                {
                    sBuilder.Append(data[i].ToString("x2"));
                }
                return sBuilder.ToString();
            }

            var dict = new Dictionary<string, DupEntry>();

            foreach (var (chunkIndex, script) in package.Scripts.WithIndex())
            {
                if (chunkIndex == 0) continue;

                var list = new JArray();
                foreach (var code in script.Codes.OfType<OpCodes.StringCode>())
                {
                    if (code is OpCodes.CharacterCode) continue;

                    var content = code.Content;
                    if (regex.IsMatch(code.Content))
                    {
                        if (dict.TryGetValue(content, out var entry))
                        {
                            ++entry.DupCounter;
                            list.Add(JObject.FromObject(new {
                                source_entity_hash = GetMd5Hash($"{code.Index:000000}:{code.Index:000000}"),
                                translation = $"@import {entry.Chunk:0000} {entry.Index:000000}"
                            }));
                        }
                        else
                        {
                            dict.Add(content, new DupEntry{ Chunk = chunkIndex, Index = code.Index, DupCounter = 1 });
                        }
                    }
                }
                if (list.Count > 0)
                {
                    //GET "https://www.transifex.com/api/2/project/rootdouble_steam_cn/resource/source-json-chunk-0521-json--master/translation/zh_CN/strings/?key=1028"

                    //PUT  "https://www.transifex.com/api/2/project/rootdouble_steam_cn/resource/source-json-chunk-0521-json--master/translation/zh_CN/strings/"
                    //    Content-Type: application/json
                    //    [{"source_entity_hash": "52eb48c64d136a9356bd2fcf03ab4bc2", "translation": "@import 0521 000554"}]

                    var url = Regex.Replace(options.Url, "<chunk>", $"{chunkIndex:0000}");

                    try
                    {
                        Console.Write($"Filling chunk_{chunkIndex:0000} ({list.Count} imports)...");
                        Console.Out.Flush();

                        var result = await url
                            .WithBasicAuth("api", token)
                            .WithTimeout(30)
                            .WithHeader("Content-Type", "application/json")
                            .PutStringAsync(list.ToString())
                            .ReceiveString();
                        
                        Console.WriteLine(result);
                    }
                    catch (FlurlHttpException exc)
                    {
                        Console.WriteLine(exc.Message);
                        return;
                    }
                }
            }
            dict = dict.Where(entry => entry.Value.DupCounter > 1).ToDictionary(entry => entry.Key, entry => entry.Value);
            Console.WriteLine($"{dict.Sum(entry => entry.Value.DupCounter)} dups of {dict.Count} strings");
            
            // foreach (var (k, v) in dict.OrderByDescending(e => e.Value.DupCounter).Take(100))
            // {
            //     Console.WriteLine($"    {k:10}: {v.DupCounter}");
            // }
        }

        private static void HandleError(Error error)
        {
            Console.WriteLine(error.ToString());
        }

    }
}
