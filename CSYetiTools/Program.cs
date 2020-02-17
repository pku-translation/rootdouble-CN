using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CommandLine;
using Newtonsoft.Json.Linq;
using CsYetiTools.VnScripts;
using Flurl;
using Flurl.Util;
using Flurl.Http;

namespace CsYetiTools
{
    class Program
    {
        [Verb("test-bed", HelpText = "Test a sn.bin")]
        class TestBedOptions
        {
            [Option("data-path")]
            public string? DataPath { get; set; }
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

        [Verb("encode-sn", HelpText = "Encode sn.bin with files in specified folder")]
        class EncodeSnOptions
        {
            [Option("inputdir")]
            public string InputDir { get; set; } = "";

            [Option("output", Default = "sn.bin")]
            public string Output { get; set; } = "";

            [Option("string-pooled")]
            public bool IsStringPooled { get; set; }
        }

        [Verb("decode-sn", HelpText = "Decode sn.bin to specified folder")]
        class DecodeSnOptions
        {
            [Option("input", Default = "sn.bin")]
            public string Input { get; set; } = "";

            [Option("outputdir", Default = "./")]
            public string OutputDir { get; set; } = "";

            [Option("string-pooled")]
            public bool IsStringPooled { get; set; }

            [Option("dump-binary")]
            public bool IsDumpBinary { get; set; }

            [Option("dump-script")]
            public bool IsDumpScript { get; set; }
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
            public string? DumpResultTextPath { get; set; }
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
            var stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();

            await Parser.Default.ParseArguments<
                TestBedOptions,
                EncodeSnOptions,
                DecodeSnOptions,
                GenStringCompareOptions,
                ReplaceStringListOptions,
                DumpTranslateSourceOptions,
                FillTransifexDuplicatedStringsOptions
            >(args).MapResult(
                (TestBedOptions o) => Task.Run(() => TestBed(o)),
                (EncodeSnOptions o) => Task.Run(() => EncodeSn(o)),
                (DecodeSnOptions o) => Task.Run(() => DecodeSn(o)),
                (GenStringCompareOptions o) => Task.Run(() => GenStringCompare(o)),
                (ReplaceStringListOptions o) => Task.Run(() => ReplaceStringList(o)),
                (DumpTranslateSourceOptions o) => Task.Run(() => DumpTranslateSource(o)),
                (FillTransifexDuplicatedStringsOptions o) => FillTransifexDuplicatedStrings(o),
                errs => Task.Run(() => errs.ForEach(HandleError))
            );

            stopwatch.Stop();

            Console.WriteLine($"Done in {stopwatch.ElapsedMilliseconds} ms");
        }

        private static void TestBed(TestBedOptions options)
        {
            new TestBed(options.DataPath!).Run();
        }

        private static void EncodeSn(EncodeSnOptions options)
        {
            var package = SnPackage.CreateFrom(options.InputDir, options.IsStringPooled);
            package.WriteTo(options.Output);
        }

        private static void DecodeSn(DecodeSnOptions options)
        {
            var input = Path.GetRelativePath(Directory.GetCurrentDirectory(), options.Input);
            var outputDir = Path.GetRelativePath(Directory.GetCurrentDirectory(), options.OutputDir);
            Console.WriteLine($"Decoding {input} --> {outputDir} ...");
            var package = new SnPackage(input, options.IsStringPooled);
            package.Dump(outputDir, Path.GetFileNameWithoutExtension(input), options.IsDumpBinary, options.IsDumpScript);
        }

        private static void GenStringCompare(GenStringCompareOptions options)
        {
            var outputDir = Path.GetRelativePath(Directory.GetCurrentDirectory(), options.OutputDir);
            Console.WriteLine($"Gen string compare --> {outputDir}");

            void DumpText(string path, IEnumerable<CodeScript.StringReferenceEntry> entries)
            {
                using var writer = new StreamWriter(path);
                foreach (var (i, entry) in entries.WithIndex())
                {
                    //writer.WriteLine($"{i, 3}: {entry.Index, 3}| [{entry.Code:X02}] [{entry.Content}]");
                    writer.WriteLine($"{i,3}: {entry.Index,4}| [{entry.Code:X02}] [{entry.Content}]");
                }
            }

            var package = new SnPackage(options.Input, isStringPooled: false);
            var packageSteam = new SnPackage(options.InputSteam, isStringPooled: true);

            var modifierTable = StringListModifier.Load(options.ModifierFile);

            var dumpDir = new DirectoryInfo(outputDir);
            if (dumpDir.Exists)
            {
                foreach (var file in dumpDir.EnumerateFiles()) file.Delete();
            }
            else
            {
                dumpDir.Create();
            }
            using var writer = new StreamWriter(Path.Combine(outputDir, "compare-result.txt"));
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
                DumpText(Path.Combine(outputDir, $"chunk_{i:0000}_ref.txt"), c1s);
                DumpText(Path.Combine(outputDir, $"chunk_{i:0000}_steam.txt"), c2s);
            }
            writer.WriteLine($"{n} different files");
        }

        private static SnPackage GenerateStringReplacedPackage(string input, string inputRef, string modifiersFile)
        {
            var package = new SnPackage(input, isStringPooled: true);
            var refPackage = new SnPackage(inputRef, isStringPooled: false);

            var modifierDict = StringListModifier.Load(modifiersFile);
            package.ReplaceStringTable(refPackage, modifierDict);

            return package;
        }

        private static void ReplaceStringList(ReplaceStringListOptions options)
        {
            var output = Path.GetRelativePath(Directory.GetCurrentDirectory(), options.Output);
            Console.WriteLine("Replace string table, write to " + output);

            var package = GenerateStringReplacedPackage(options.InputSteam, options.InputRef, options.ModifiersFile);

            package.WriteTo(output);

            var dumpPath = options.DumpResultTextPath;
            if (dumpPath != null)
            {
                Utils.CreateOrClearDirectory(dumpPath);
                package.Dump(dumpPath, Path.GetFileNameWithoutExtension(output), isDumpBinary: false, isDumpScript: true);
            }
        }

        private static void DumpTranslateSource(DumpTranslateSourceOptions options)
        {
            var outputDir = Path.GetRelativePath(Directory.GetCurrentDirectory(), options.OutputDir);
            Console.WriteLine($"Dump translate source --> {outputDir}");

            var package = GenerateStringReplacedPackage(options.Input, options.InputRef, options.ModifiersFile);

            package.DumpTranslateSource(outputDir);
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
                foreach (var code in script.Codes.OfType<StringCode>())
                {
                    if (code is ExtraDialogCode) continue;

                    var content = code.Content;
                    if (regex.IsMatch(code.Content))
                    {
                        if (dict.TryGetValue(content, out var entry))
                        {
                            ++entry.DupCounter;
                            list.Add(JObject.FromObject(new
                            {
                                source_entity_hash = GetMd5Hash($"{code.Index:000000}:{code.Index:000000}"),
                                translation = $"@import {entry.Chunk:0000} {entry.Index:000000}"
                            }));
                        }
                        else
                        {
                            dict.Add(content, new DupEntry { Chunk = chunkIndex, Index = code.Index, DupCounter = 1 });
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
        }

        private static void HandleError(Error error)
        {
            Console.WriteLine(error.ToString());
        }

    }
}
