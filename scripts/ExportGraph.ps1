. $PSScriptRoot/Defines.ps1

& $RunCsx @"

using System.IO;
var package = Load("steam/sn.bin", true);
var jpPackage = Load("psv/sn.bin", false);
package.ReplaceStringTable(jpPackage, StringListModifier.LoadFile("string_list_modifiers.sexp"));
var mem = new MemoryStream();
package.WriteTo(mem);
var cnPackage = new SnPackage(mem.ToArray(), true);
cnPackage.ApplyTranslations("../source_json/", "../zh_CN/", new TranslationSettings{ DebugChunkNum = true });
var sceneTitles = await File.ReadAllLinesAsync("scene_titles");
RawGraph.LoadPackage(cnPackage, package, sceneTitles).WithIndex().ForEach(p => p.element?.Save($"BranchViewer/data/{p.index:0000}.yaml"));

"@
