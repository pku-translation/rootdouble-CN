. $PSScriptRoot/Defines.ps1

& $Run @"

var packageSteam = Load("steam/sn.bin", true);
var package = Load("psv/sn.bin", false);
ReplaceStringList(packageSteam, package, "string_list_modifiers.ss");
packageSteam.DumpTranslateSource("$DataRoot/../source_json");

var peeker = CsYetiTools.FileTypes.ExecutableStringPeeker.FromFile("$DataRoot/steam/executable", Utils.Cp932);
peeker.DumpTranslateSource("$DataRoot/../source_json/sys", "$DataRoot/string_pool/");

"@
