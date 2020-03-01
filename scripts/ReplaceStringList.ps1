. $PSScriptRoot/Defines.ps1

& $RunCsx @"

var packageSteam = Load("steam/sn.bin", true);
var package = Load("psv/sn.bin", false);
ReplaceStringList(packageSteam, package, "string_list_modifiers.ss");
packageSteam.WriteTo("$DataRoot/sn_modified.bin");

"@
