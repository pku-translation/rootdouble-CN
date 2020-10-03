﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CsYetiTools.VnScripts;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.CSharp;
using System.Text.RegularExpressions;
using CsYetiTools.FileTypes;
using SixLabors.ImageSharp;

namespace CsYetiTools
{
    public static class Program
    {
        private static string GetAllInput()
        {
            var builder = new StringBuilder();
            string? line;
            while ((line = Console.ReadLine()) != null)
            {
                builder.AppendLine(line);
            }
            return builder.ToString();
        }

        static async Task Main(string[] args)
        {
            if (args.Length == 1)
            {
                if (args[0] == "testbed")
                {
                    var stopwatch = new System.Diagnostics.Stopwatch();
                    stopwatch.Start();
                    await TestBed.Run();
                    Console.WriteLine($"Done in {stopwatch.ElapsedMilliseconds} ms");
                }
                else if (args[0] == "benchmark")
                {
                    Benchmarks.Run();
                }
                else
                {
                    Console.WriteLine($"Unknown command \"{args[0]}\"");
                }
            }
            else
            {
                var scriptText = args.Length switch
                {
                    0 => GetAllInput(),
                    2 => args[0] switch
                    {
                        "file" => File.ReadAllText(args[1], Encoding.UTF8),
                        "command" => args[1],
                        _ => throw new ArgumentException($"Unknown type {args[0]}, expecting (file|command)")
                    },
                    _ => throw new ArgumentException($"Expecting zero or two args (file <filename> | command <command>)")
                };

                var script = CSharpScript.Create(scriptText,
                    ScriptOptions.Default
                        .AddReferences(typeof(Program).Assembly)
                        .AddImports("CsYetiTools"
                                  , "CsYetiTools.IO"
                                  , "CsYetiTools.FileTypes"
                                  , "CsYetiTools.VnScripts"
                                  , typeof(Utils).FullName
                                  , typeof(Program).FullName
                        )
                        .WithCheckOverflow(true)
                        .WithFileEncoding(Encoding.UTF8)
                        .WithLanguageVersion(LanguageVersion.CSharp8));
                try
                {
                    var stopwatch = new System.Diagnostics.Stopwatch();
                    stopwatch.Start();
                    script.Compile();
                    stopwatch.Stop();
                    Console.WriteLine($"Compiled in {stopwatch.ElapsedMilliseconds} ms");

                    stopwatch.Restart();
                    await script.RunAsync();
                    stopwatch.Stop();
                    Console.WriteLine($"Done in {stopwatch.ElapsedMilliseconds} ms");

                }
                catch (CompilationErrorException exc)
                {
                    Console.WriteLine("Compile error in input: ");
                    Utils.PrintColored(ConsoleColor.Blue, scriptText);
                    Utils.PrintError(exc.ToString());
                    return;
                }

            }
        }

        public static SnPackage Load(FilePath path, bool isStringPooled)
        {
            path = path.ToRelative();
            Console.Write("Loading package " + path + " ... ");
            Console.Out.Flush();

            var stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();
            var package = new SnPackage(path, isStringPooled);
            stopwatch.Stop();

            Console.WriteLine($"{stopwatch.Elapsed.TotalMilliseconds} ms");
            return package;
        }

        public static void GenStringCompare(SnPackage package1, string? modifierPath1, SnPackage package2, string? modifierPath2, FilePath outputDir)
        {
            outputDir = outputDir.ToRelative();
            Console.WriteLine($"Gen string compare --> {outputDir}");

            void DumpText(string path, IEnumerable<VnScripts.Script.StringReferenceEntry> entries)
            {
                using var writer = Utils.CreateStreamWriter(path);
                foreach (var (i, entry) in entries.WithIndex())
                {
                    writer.WriteLine($"{i,3}: {entry.Index,4}| [{entry.Code:X02}] [{entry.Content}]");
                }
            }
            var modifierTable1 = modifierPath1 != null ? StringListModifier.LoadFile(modifierPath1) : null;
            var modifierTable2 = modifierPath2 != null ? StringListModifier.LoadFile(modifierPath2) : null;

            Utils.CreateOrClearDirectory(outputDir);

            using var writer = Utils.CreateStreamWriter(outputDir / "compare-result.txt");
            int n = 0;
            foreach (var (i, (s1, s2)) in package1.Scripts.Zip(package2.Scripts).WithIndex())
            {
                if (s1.Footer.ScriptIndex < 0) continue;

                var c1s = s1.GenerateStringReferenceList(
                    modifierTable1 != null ? modifierTable1.TryGetValue(i, out var modifier1) ? modifier1 : null : null);
                var c2s = s2.GenerateStringReferenceList(
                    modifierTable2 != null ? modifierTable2.TryGetValue(i, out var modifier2) ? modifier2 : null : null);

                bool same = true;
                foreach (var (c1, c2) in c1s.Zip(c2s))
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
                DumpText(outputDir / $"chunk_{i:0000}_ref.txt", c1s);
                DumpText(outputDir / $"chunk_{i:0000}_steam.txt", c2s);
            }
            writer.WriteLine($"{n} different files");
        }

        public static void ReplaceStringList(SnPackage package, SnPackage refPackage, string modifiersFile)
        {
            var modifierDict = StringListModifier.LoadFile(modifiersFile);
            package.ReplaceStringTable(refPackage, modifierDict);
        }


        class DupEntry
        {
            public int Chunk { get; set; }
            public int Index { get; set; }
            public int DupCounter { get; set; }
        }

        private static async Task FillTransifexDuplicatedStrings(
            SnPackage package, string filterPattern,
            string projectSlug, string chunkFormatter, string? token = null)
        {
            var client = new Transifex.TransifexClient(token);
            var project = client.Project(projectSlug);

            var filterReg = new Regex(filterPattern, RegexOptions.Compiled | RegexOptions.Singleline);

            var dupDict = new Dictionary<string, DupEntry>();

            foreach (var (chunkIndex, script) in package.Scripts.WithIndex())
            {
                if (script.Footer.ScriptIndex < 0) continue;

                var dict = new SortedDictionary<int, string>();
                foreach (var code in script.Codes.OfType<StringCode>())
                {
                    if (code is ExtraDialogCode exDialog && !exDialog.IsDialog) continue;

                    var content = code.Content;
                    if (filterReg.IsMatch(code.Content))
                    {
                        if (dupDict.TryGetValue(content, out var entry))
                        {

                            ++entry.DupCounter;
                            dict.Add(code.Index, $"@auto-import {entry.Chunk:0000} {entry.Index:000000}");
                        }
                        else
                        {
                            dupDict.Add(content, new DupEntry { Chunk = chunkIndex, Index = code.Index, DupCounter = 1 });
                        }
                    }
                }
                if (dict.Count > 0)
                {
                    var resource = project.Resource(string.Format(chunkFormatter, chunkIndex));
                    try
                    {
                        var currentTranslations = new SortedDictionary<int, Transifex.TranslationStringInfo>();
                        foreach (var trans in await resource.GetTranslationStrings("zh_CN"))
                        {
                            currentTranslations.Add(int.Parse(trans.Key), trans);
                        }
                        Console.WriteLine($"chunk {chunkIndex}");
                        var imports = new List<Transifex.TranslationStringsPutInfo>();
                        foreach (var (k, t) in dict)
                        {
                            if (currentTranslations.TryGetValue(k, out var currentTranslation)
                                && !string.IsNullOrWhiteSpace(currentTranslation.Translation)
                                && !currentTranslation.Translation.StartsWith("@auto-import"))
                            {
                                Console.WriteLine($"    skip: [{currentTranslation.Key}] {currentTranslation.User} => {currentTranslation.Translation}");
                                continue;
                            }
                            var keyStr = k.ToString("000000");
                            imports.Add(new Transifex.TranslationStringsPutInfo(keyStr, keyStr, dict[k], ""));
                        }
                        if (imports.Count == 0)
                        {
                            Console.WriteLine("Empty");
                            continue;
                        }
                        Console.Write($"Filling ...");
                        Console.Out.Flush();

                        var result = await resource.PutTranslationStrings("zh_CN", imports.ToArray());
                        Console.WriteLine(result);
                    }
                    catch (Exception exc)
                    {
                        Console.WriteLine(exc.Message);
                        return;
                    }
                }
            }
            dupDict = dupDict.Where(entry => entry.Value.DupCounter > 1).ToDictionary(entry => entry.Key, entry => entry.Value);
            Console.WriteLine($"{dupDict.Sum(entry => entry.Value.DupCounter)} dups of {dupDict.Count} strings");
        }

        public static async Task FillTransifexIgnores(
            SnPackage package,
            string projectSlug, string chunkFormatter, string? token = null)
        {
            var client = new Transifex.TransifexClient(token);
            var project = client.Project(projectSlug);

            foreach (var (chunkIndex, script) in package.Scripts.WithIndex())
            {
                if (script.Footer.ScriptIndex < 0) continue;

                var ignores = new List<Transifex.TranslationStringsPutInfo>();

                foreach (var code in script.Codes)
                {
                    switch (code)
                    {
                        case SssInputCode si:
                        case ScriptJumpCode c when c.IsJump:
                            {
                                var keyStr = code.Index.ToString("000000");
                                ignores.Add(new Transifex.TranslationStringsPutInfo(keyStr, keyStr, "@ignore", ""));
                            }
                            break;
                    }
                }
                if (ignores.Count > 0)
                {
                    var resource = project.Resource(string.Format(chunkFormatter, chunkIndex));
                    try
                    {
                        Console.WriteLine($"chunk {chunkIndex}");
                        //Console.WriteLine(Utils.SerializeJson(ignores));

                        Console.Write($"Filling ...");
                        Console.Out.Flush();

                        var result = await resource.PutTranslationStrings("zh_CN", ignores.ToArray());
                        Console.WriteLine(result);
                    }
                    catch (Exception exc)
                    {
                        Console.WriteLine(exc.Message);
                        return;
                    }
                }
            }
        }

        public static void ReleaseTranslation(
            FilePath executable,
            FilePath executableStringPool,
            SnPackage snPackage,
            FilePath translationSourceDir,
            FilePath translationDir,
            FilePath releaseDir,
            bool dumpFontTexture = false,
            bool debugChunkNum = false)
        {
            Console.Write("Translating executable... "); Console.Out.Flush();
            var exePeeker = Utils.Time(() =>
            {
                var peeker = ExecutableStringPeeker.FromFile(executable, Utils.Cp932);
                peeker.ApplyTranslations(translationDir / "sys", executableStringPool);
                return peeker;
            });

            Console.WriteLine("Translating sn-package... ");
            Utils.Time(() =>
            {

                snPackage.ApplyTranslations(translationSourceDir, translationDir, debugChunkNum);
                return snPackage;
            }, "Translated sn-package in {0}");

            var dbChars = snPackage.EnumerateChars().Concat(exePeeker.EnumerateChars()).Where(c => c >= 0x80).Distinct().Ordered().ToArray();
            Console.WriteLine($"Double-byte code used: {dbChars.Length}");

            Console.Write("Generating font... "); Console.Out.Flush();
            var fontMapping = new FontMapping(dbChars);

            var pair = Utils.Time(() =>
            {
                var texture = fontMapping.GenerateTexture();
                var xtx = Xtx.CreateFont(texture);
                return (texture, xtx);
            });
            using var xtx = pair.xtx;
            using var texture = pair.texture;

            using var exeStream = new MemoryStream(File.ReadAllBytes(executable));
            exePeeker.Modify(exeStream, fontMapping);

            Utils.CreateOrClearDirectory(releaseDir);
            Utils.CreateOrClearDirectory(releaseDir / "data");

            File.WriteAllBytes(releaseDir / "rwxe.exe", exeStream.ToArray());
            snPackage.WriteTo(releaseDir / "data/sn.bin", fontMapping);
            xtx.SaveBinaryTo(releaseDir / "data/font48.xtx");
            if (dumpFontTexture) texture.Save(releaseDir / "font.png");
        }

        public static async Task DownloadTranslations(SnPackage package, ExecutableStringPeeker peeker, FilePath translationDir, string projectSlug, string chunkFormatter, string sysFormatter, string? token = null)
        {
            var client = new Transifex.TransifexClient(token);
            var project = client.Project(projectSlug);

            foreach (var (chunkIndex, script) in package.Scripts.WithIndex())
            {
                if (script.Footer.ScriptIndex < 0) continue;
                var resource = project.Resource(string.Format(chunkFormatter, chunkIndex));
                string raw;
                while (true)
                {
                    try
                    {
                        raw = await resource.GetRawTranslations("zh_CN");
                        break;
                    }
                    catch (Flurl.Http.FlurlHttpTimeoutException)
                    {
                        Console.WriteLine($"chunk {chunkIndex:0000} timeout, retrying");
                    }
                }
                using (var writer = new StreamWriter(translationDir / $"chunk_{chunkIndex:0000}.json", false, Utils.Utf8))
                {
                    writer.NewLine = "\n";
                    writer.Write(raw);
                }
                Console.WriteLine($"chunk {chunkIndex:0000} downloaded");
            }

            foreach (var name in peeker.Names)
            {
                var resource = project.Resource(string.Format(sysFormatter, name.Replace('_', '-')));
                string raw;
                while (true)
                {
                    try
                    {
                        raw = await resource.GetRawTranslations("zh_CN");
                        break;
                    }
                    catch (Flurl.Http.FlurlHttpTimeoutException)
                    {
                        Console.WriteLine($"sys/{name} timeout, retrying");
                    }
                }
                using (var writer = new StreamWriter(translationDir / "sys" / $"{name.Replace('-', '_')}.json", false, Utils.Utf8))
                {
                    writer.NewLine = "\n";
                    writer.Write(raw);
                }
                Console.WriteLine($"sys/{name} downloaded");
            }
        }

        private static void WritePOSeg(TextWriter writer, string key, string value)
        {
            var lines = value.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            if (lines.Length > 1)
            {
                writer.WriteLine($"{key} \"\"");
                foreach (var line in lines)
                {
                    writer.WriteLine($"\"{line.Replace("\"", "\\\"")}\\n\"");
                }
            }
            else
            {
                writer.WriteLine($"{key} \"{lines[0].Replace("\"", "\\\"")}\"");
            }
        }

        private static void WritePO(
            FilePath path,
            string prefix,
            IEnumerable<Transifex.TranslationStringInfo> stringInfos,
            Func<Transifex.TranslationStringInfo, string> textSelector,
            Func<Transifex.TranslationStringInfo, string?>? contextSelector = null)
        {
            using var writer = new StreamWriter(path, false, Utils.Utf8);
            writer.NewLine = "\n";
            WritePO(writer, prefix, stringInfos, textSelector, contextSelector);
        }

        private static void WritePO(
            TextWriter writer,
            string prefix,
            IEnumerable<Transifex.TranslationStringInfo> stringInfos,
            Func<Transifex.TranslationStringInfo, string> textSelector,
            Func<Transifex.TranslationStringInfo, string?>? commentSelector = null)
        {
            foreach (var info in stringInfos)
            {
                var key = info.Key;

                var text = textSelector(info);
                var comment = commentSelector?.Invoke(info);

                writer.WriteLine();
                if (!string.IsNullOrWhiteSpace(comment)) writer.WriteLine($"# {comment}");
                WritePOSeg(writer, "msgid", prefix + ":" + key);
                WritePOSeg(writer, "msgstr", text);
            }
        }

        public static async Task GeneratePOs(FilePath outputDir, SnPackage jpPackage, SnPackage enPackage, string projectSlug, string chunkFormatter)
        {
            var client = new Transifex.TransifexClient();
            var project = client.Project(projectSlug);

            Utils.CreateOrClearDirectory(outputDir / "ja/chunk");
            Utils.CreateOrClearDirectory(outputDir / "zh-Hans/chunk");
            Utils.CreateOrClearDirectory(outputDir / "en/chunk");
            foreach (var (chunkIndex, script) in jpPackage.Scripts.WithIndex())
            {
                if (script.Footer.ScriptIndex < 0) continue;
                var resource = project.Resource(string.Format(chunkFormatter, chunkIndex));

                var jaSources = script.GetTranslateSources();
                var enSources = enPackage.Scripts[chunkIndex].GetTranslateSources();

                var indexStr = $"{chunkIndex:0000}";

                Console.WriteLine($"Generate chunk {indexStr} ...");

                Transifex.TranslationStringInfo[] infos;
                while (true)
                {
                    try
                    {
                        infos = await resource.GetTranslationStrings("zh_CN");
                        break;
                    }
                    catch (Flurl.Http.FlurlHttpTimeoutException)
                    {
                        Console.WriteLine($"chunk {indexStr} timeout, retrying");
                    }
                }

                foreach (var info in infos)
                {
                    WritePO(outputDir / "ja/chunk" / $"{indexStr}.po", indexStr, infos, info => info.SourceString, info => info.Comment);
                    WritePO(outputDir / "zh-Hans/chunk" / $"{indexStr}.po", indexStr, infos, info => info.Translation, info => info.Comment);
                    WritePO(outputDir / "en/chunk" / $"{indexStr}.po", indexStr, infos, info => enSources[info.Key].String, info => info.Comment);
                }
            }

        }

        public static async Task GenerateSysPOs(FilePath outputDir, ExecutableStringPeeker peeker, string projectSlug, string sysFormatter)
        {
            var client = new Transifex.TransifexClient();
            var project = client.Project(projectSlug);

            Utils.CreateOrClearDirectory(outputDir / "ja/sys");
            Utils.CreateOrClearDirectory(outputDir / "zh-Hans/sys");
            Utils.CreateOrClearDirectory(outputDir / "en/sys");
            foreach (var name in peeker.Names)
            {
                var resource = project.Resource(string.Format(sysFormatter, name.Replace('_', '-')));

                Console.WriteLine($"Generate {name} ...");

                Transifex.TranslationStringInfo[] infos;
                while (true)
                {
                    try
                    {
                        infos = await resource.GetTranslationStrings("zh_CN");
                        break;
                    }
                    catch (Flurl.Http.FlurlHttpTimeoutException)
                    {
                        Console.WriteLine($"sys/{name} timeout, retrying");
                    }
                }

                foreach (var info in infos)
                {
                    WritePO(outputDir / "ja/sys" / (name + ".po"), name, infos, info => info.SourceString);
                    WritePO(outputDir / "zh-Hans/sys" / (name + ".po"), name, infos, info => info.Translation);
                    WritePO(outputDir / "en/sys" / (name + ".po"), name, infos, info => info.Comment);
                }
            }
        }
    }
}
