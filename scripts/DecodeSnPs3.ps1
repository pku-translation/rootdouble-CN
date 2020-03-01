. $PSScriptRoot/Defines.ps1

& $RunCsx @"

Load("ps3/sn.bin", false).Dump("ps3_sn", isDumpBinary: true, isDumpScript: true);

"@