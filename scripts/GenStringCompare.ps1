. $PSScriptRoot/Defines.ps1

& $RunCsx @"

var package = Load("psv/sn.bin", false);
var packageSteam = Load("steam/sn.bin", true);
GenStringCompare(package, "string_list_modifiers.sexp", packageSteam, null, "$DataRoot/../reference/");

"@
