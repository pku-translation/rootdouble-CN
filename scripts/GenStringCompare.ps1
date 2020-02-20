. $PSScriptRoot/Defines.ps1

& $Run @"

var package = Load("psv/sn.bin", false);
var packageSteam = Load("steam/sn.bin", true);
GenStringCompare(package, "string_list_modifiers.ss", packageSteam, null, "$DataRoot/../reference/");

"@
