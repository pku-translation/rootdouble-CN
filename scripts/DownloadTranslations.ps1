. $PSScriptRoot/Defines.ps1

& $RunCsx @"

var package = Load("steam/sn.bin", true);

await DownloadTranslations(package
                 , translationDir: "../zh_CN/"
                 , projectSlug: "rootdouble_steam_cn"
                 , chunkFormatter: "source-json-chunk-{0:0000}-json--master"
);

"@
