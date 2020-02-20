. $PSScriptRoot/Defines.ps1

& $Run @"

var package = Load("psv/sn.bin", false);
var packageSteam = Load("steam/sn.bin", true);
ReplaceStringList(packageSteam, package, "string_list_modifiers.ss");
packageSteam.WriteTo("$DataRoot/sn_modified.bin");

"@
