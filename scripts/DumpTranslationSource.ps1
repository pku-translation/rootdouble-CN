. $PSScriptRoot/Defines.ps1

& $Run @"

var packageSteam = Load("steam/sn.bin", true);
var package = Load("psv/sn.bin", false);
ReplaceStringList(packageSteam, package, "string_list_modifiers.ss");
packageSteam.DumpTranslateSource("$DataRoot/../source_json")

"@
