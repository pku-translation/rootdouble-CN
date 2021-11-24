. $PSScriptRoot/Defines.ps1

& $RunCsx @"

var package = Load("steam/sn.bin", true);
var peeker = ExecutableStringPeeker.FromFile("$DataRoot/steam/executable", Utils.Cp932);

await DownloadTranslationsToYaml(package, peeker
                 , translationDir: "../translated/"
                 , projectSlug: "rootdouble_steam_cn"
                 , chunkFormatter: "source-json-chunk-{0:0000}-json--master"
                 , sysFormatter: "source-json-sys-{0}-json--master"
);

"@
