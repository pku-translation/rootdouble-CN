. $PSScriptRoot/Defines.ps1

& $Run @"

var peeker = CsYetiTools.FileTypes.ExecutableStringPeeker.FromFile("$DataRoot/steam/executable", Utils.Cp932);
peeker.DumpTranslateSource("$DataRoot/../source_json/sys", "$DataRoot/string_pool/");

"@
