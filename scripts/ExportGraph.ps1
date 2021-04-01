. $PSScriptRoot/Defines.ps1

& $RunCsx @"

var package = Load("steam/sn.bin", true);
var jpPackage = Load("psv/sn.bin", false);
package.ReplaceStringTable(jpPackage, StringListModifier.LoadFile("string_list_modifiers.sexp"));
package.ApplyTranslations("../source_json/", "../zh_CN/", true);
RawGraph.LoadPackage(package, false).WithIndex().ForEach(p => p.element?.Save($"BranchViewer/data/{p.index:0000}.yaml"));

"@
