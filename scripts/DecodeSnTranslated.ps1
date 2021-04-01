. $PSScriptRoot/Defines.ps1

& $RunCsx @"
var package = Load("steam/sn.bin", true);
var jpPackage = Load("psv/sn.bin", false);
package.ReplaceStringTable(jpPackage, StringListModifier.LoadFile("string_list_modifiers.sexp"));
package.ApplyTranslations("../source_json/", "../zh_CN/", true);
package.Dump("translated_sn", isDumpBinary: false, isDumpScript: true);

"@
