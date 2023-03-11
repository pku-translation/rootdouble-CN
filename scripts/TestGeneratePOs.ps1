. $PSScriptRoot/Defines.ps1

& $RunCsx @"

var enPackage = Load("$DataRoot/steam/sn.bin", true);
var jpPackage = Load("$DataRoot/steam/sn.bin", true);

{
    
    var jpRefPackage = Load("psv/sn.bin", false);
    jpPackage.ReplaceStringTable(jpRefPackage, StringListModifier.LoadFile("string_list_modifiers.sexp"));
}

await GeneratePOs("pku_translation", "POs", jpPackage, enPackage, projectSlug: "rootdouble_steam_cn", chunkFormatter: "source-json-chunk-{0:0000}-json--master");

var peeker = ExecutableStringPeeker.FromFile("$DataRoot/steam/executable", Utils.Cp932);
await GenerateSysPOs("pku_translation", "POs", peeker, projectSlug: "rootdouble_steam_cn", sysFormatter: "source-json-sys-{0}-json--master");


"@
