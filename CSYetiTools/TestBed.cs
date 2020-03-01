using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CsYetiTools.FileTypes;
using CsYetiTools.VnScripts;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.Primitives;
using static CsYetiTools.Utils;

namespace CsYetiTools
{
    class TestBed
    {
        private static SnPackage Load(FilePath path, bool isStringPooled)
        {
            var rpath = path.ToRelative();
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
            await Task.Run(() => { });

            FilePath root = Environment.GetEnvironmentVariable("RW_ROOT")!;

            FilePath ps3_root = Environment.GetEnvironmentVariable("RW_PS3_ROOT")!;

            // CreateOrClearDirectory("test");

            // ------------- font ------------------------------

            // var fontMapping = new FontMapping(Range(0x6110, 0x8000).Select(i => (char)i));
            // using var img = fontMapping.RendererToTexture();
            // img.Save("fonts/test2.png");

            // ----------- font texture test -----------------------
//             var fontMapping = new FontMapping(@"正式名称：International Nuclear Event Scale(国際原子力事象評価尺度)。INESでイネスとも読む。
// 　国際原子力機関(IAEA)と経済協力開発機構原子力機関(OECD／NEA)が1990年に策定し、1992年から各国で採用されている原子力事故の深刻さを評価する尺度のこと。
// 　レベル1(逸脱)から、レベル7(深刻な事故)までの7段階で評価される。
// 　チェルノブイリ原子力発電所事故がレベル7だとされている。
// 　ただし全段階の最高位であることを考えると、レベル7は『レベル6以上のすべての事故』という定義だともいえるので、レベル7の事故が他にあっても、2つの事故を比べてどちらがどうであるかを論ずるには慎重さを必要とする。
// 　それを踏まえ、一部の専門家からはレベル8を新設するべきではとの声も上がっている。
// 　メルトダウンにより原子炉の炉心が溶融・損傷をした場合、その時点で最低でもレベル4の評価となる。

// 　全称：International Nuclear Event Scale（国际核事件分级表）。
// 　由国际原子能机构（IAEA）和经济合作与发展组织核能署（OECD-NEA）于 1990 年制定，并于 1992 年开始被各国采用的核事件严重性分级表。
// 　事件从 1 级（异常情况）到 7 级（特大事故）分为七级。
// 　切尔诺贝利核电站事故属于 7 级事故。
// 　然而考虑到最高等级的位置，也可以说 7 级的定义是“所有比 6 级更严重的事故”。因此即便有其它的 7 级事故，将二者的严重性进行对比时也应谨慎。
// 　考虑到这一点，一些专家提议应当设立 8 级。
// 　若核反应堆发生堆芯熔毁，事件至少会被认定为 4 级。  ");

//             using var img = fontMapping.RendererToTexture();
//             img.Save("fonts/test2.png");
            

            // -------------------
            var exePeeker = ExecutableStringPeeker.FromFile("steam/executable", Utils.Cp932);
            exePeeker.ApplyTranslations("../zh_CN/sys/", "string_pool/");

            var package = new SnPackage("steam/sn.bin", true);
            var jpPackage = new SnPackage("psv/sn.bin", false);


            package.ReplaceStringTable(jpPackage, StringListModifier.LoadFile("string_list_modifiers.ss"));
            package.ApplyTranslations("../source_json/", "../zh_CN/");

            var chars = package.EnumerateChars().Concat(exePeeker.EnumerateChars()).Where(c => c >= 0x80).Distinct().Ordered().ToArray();
            Console.WriteLine(chars.Length);

            foreach (var (i, c) in chars.WithIndex())
            {
                Console.Write(c);
                if ((i + 1) % 128 == 0) Console.WriteLine();
            }

            // var fontMapping = new FontMapping(chars);
            // using var texture = fontMapping.GenerateTexture();
            // using var xtx = new Xtx(texture, 1, true);
            
            // using var exeStream = new MemoryStream(File.ReadAllBytes("steam/executable"));
            // exePeeker.Modify(exeStream, fontMapping);

            // Utils.CreateOrClearDirectory("release");
            // Utils.CreateOrClearDirectory("release/data");

            // File.WriteAllBytes("release/rwxe.exe", exeStream.ToArray());
            // package.WriteTo("release/data/sn.bin", fontMapping);
            // File.WriteAllBytes("release/data/font48.xtx", xtx.ToBytes());

        }
    }
}

