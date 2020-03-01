. $PSScriptRoot/Defines.ps1

& $RunCsx @"

Load("steam/sn.bin", true).Dump("steam_sn", isDumpBinary: true, isDumpScript: true);

"@
