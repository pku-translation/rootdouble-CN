using System;
using System.Linq;
using System.Threading.Tasks;
using CSYetiTools.Base;
using CSYetiTools.VnScripts;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using CSYetiTools.VnScripts.Transifex;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Core;
using Flurl.Http;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Http.Headers;

namespace CSYetiTools.Commandlet;

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
        await Task.FromResult(0);

        //Load("ps3/sn.bin", false).Dump("ps3_sn", false, true);
        //Load("psv/sn.bin", false).Dump("psv_sn", true, true);
        //Load("steam/sn.bin", true).Dump("steam_sn", true, true);

        // var package = Load("steam/sn.bin", true);
        // var jpPackage = Load("psv/sn.bin", false);
        // package.ReplaceStringTable(jpPackage, StringListModifier.LoadFile("string_list_modifiers.sexp"));
        // var mem = new MemoryStream();
        // package.WriteTo(mem);
        // var cnPackage = new SnPackage(mem.ToArray(), true);
        // cnPackage.ApplyTranslations("../source_json/", "../zh_CN/", true);

        // var sceneTitles = await File.ReadAllLinesAsync("scene_titles");
        // RawGraph.LoadPackage(cnPackage, package, sceneTitles).WithIndex().ForEach(p => p.element?.Save($"graphs/{p.index:0000}.yaml"));

        // foreach (var file in Directory.GetFiles("../zh_CN", "*.json", new EnumerationOptions { RecurseSubdirectories = true })) {
        //     var filename = file[("../zh_CN".Length + 1)..^(".json".Length)];
        //     var infoTable = JsonConvert.DeserializeObject<Dictionary<string, TranslationInfo>>(File.ReadAllText(file))!;
        //     var yaml = new YamlMappingNode();
        //     foreach (var (key, value) in infoTable) {
        //         var keyNode = new YamlScalarNode(key) { Style = ScalarStyle.SingleQuoted };
        //         var node = new YamlScalarNode(value.String) { Style = value.String.Contains('\n') ? ScalarStyle.Literal : ScalarStyle.Plain };
        //         yaml.Add(keyNode, node);
        //     }
        //     var yamlDoc = new YamlDocument(yaml);
        //     var destFile = "../translated/" + filename + ".yaml";
        //     var dir = Path.GetDirectoryName(destFile)!;
        //     if (!Directory.Exists(dir)) {
        //         Directory.CreateDirectory(dir);
        //     }
        //     Utils.WriteYamlDocument(destFile, yamlDoc, null, false);
        // }

        var apiToken = Environment.GetEnvironmentVariable("TX_TOKEN");

        var flurlClient = new FlurlClient()
            //.WithBasicAuth("Bearer", apiToken)
            .WithOAuthBearerToken(apiToken)
            .WithTimeout(30);

            // var baseUrl = "https://rest.api.transifex.com";
            // var response = await flurlClient.Request(baseUrl, "resources")
            //     .SetQueryParam("filter[project]", "o:pku_translation:p:rootdouble_steam_cn")
            //     .SetQueryParam("filter[slug]", "source-json-chunk-0005-json--master")
            //     .GetStringAsync();
            // File.WriteAllText("D:\\test_proj.json", response);

            // var url = "https://rest.api.transifex.com/resources/o:pku_translation:p:rootdouble_steam_cn:r:source-json-chunk-0005-json--master";
            // var response = await flurlClient.Request(url).GetStringAsync();
            // File.WriteAllText("D:\\test_proj.json", response);

            // var url = "https://rest.api.transifex.com/resource_translations?filter[resource]=o:pku_translation:p:rootdouble_steam_cn:r:source-json-chunk-0005-json--master&filter[language]=l:zh_CN"
            // var response = await flurlClient.Request(url).GetStringAsync();
            // File.WriteAllText("D:\\test_proj.json", response);

            // var baseUrl = "https://rest.api.transifex.com";
            // var response = await flurlClient.Request(baseUrl, "resource_translations")
            //     .SetQueryParam("filter[resource]", "o:pku_translation:p:rootdouble_steam_cn:r:source-json-chunk-0005-json--master")
            //     .SetQueryParam("filter[language]", "l:zh_CN")
            //     .GetStringAsync();
            // File.WriteAllText("D:\\test_proj.json", response);

    }
}
