using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;

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

            [Option("outputdir", Default = "./")]
            public string OutputDir { get; set; } = "";

            [Option("steam")]
            public bool IsSteam { get; set; }
        }

        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<
                TestBedOptions,
                EncodeSnOptions,
                DecodeSnOptions,
                DecodeScriptOptions,
                GenStringCompareOptions,
                ReplaceStringListOptions,
                DumpTranslateSourceOptions
            >(args)
                .WithParsed<TestBedOptions>(TestBed)
                .WithParsed<EncodeSnOptions>(EncodeSn)
                .WithParsed<DecodeSnOptions>(DecodeSn)
                .WithParsed<DecodeScriptOptions>(DecodeScript)
                .WithParsed<GenStringCompareOptions>(GenStringCompare)
                .WithParsed<ReplaceStringListOptions>(ReplaceStringList)
                .WithParsed<DumpTranslateSourceOptions>(DumpTranslateSource)
                .WithNotParsed(errs => errs.ForEach(HandleError));

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
            void DumpText(string path, IEnumerable<CodeScript.StringReferenceEntry> entries)
            {
                using var writer = new StreamWriter(path);
                foreach (var (i, entry) in entries.WithIndex())
                {
                    //writer.WriteLine($"{i, 3}: {entry.Index, 3}| [{entry.Code:X02}] [{entry.Content}]");
                    writer.WriteLine($"{i, 3}: {entry.Index, 4}| [{entry.Code:X02}] [{entry.Content}]");
                }
            }

            var package = new SnPackage(options.Input, isSteam: false);
            var packageSteam = new SnPackage(options.InputSteam, isSteam: true);

            //var raplaceExcepts = ScriptReplaceExceptions.Load("replace_exceptions.toml");
            var modifierTable = StringListModifier.Load("string_list_modifiers.sexpr");

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

        private static void ReplaceStringList(ReplaceStringListOptions options)
        {
            var package = new SnPackage(options.InputSteam, isSteam: true);
            var refPackage = new SnPackage(options.InputRef, isSteam: false);

            var modifierDict = StringListModifier.Load(options.ModifiersFile);

            foreach (var (i, (script, refScript)) in package.Scripts.ZipTuple(refPackage.Scripts).WithIndex())
            {
                var refList = modifierDict.TryGetValue(i, out var modifiers)
                    ? refScript.GenerateStringReferenceList(modifiers)
                    : refScript.GenerateStringReferenceList();

                script.ReplaceStringTable(refList);
            }

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
            Console.WriteLine($"DumpTranslateSource {options.Input} --> {options.OutputDir} ...");
            var package = new SnPackage(options.Input, options.IsSteam);

            package.DumpTranslateSource(options.OutputDir);
        }

        private static void HandleError(Error error)
        {
            Console.WriteLine(error.ToString());
        }

    }
}
