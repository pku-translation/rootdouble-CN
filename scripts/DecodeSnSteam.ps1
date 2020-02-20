. $PSScriptRoot/Defines.ps1

& $Run @"

Load("steam/sn.bin", true).Dump("steam_sn", isDumpBinary: true, isDumpScript: true);

"@
