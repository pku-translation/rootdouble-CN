. $PSScriptRoot/Defines.ps1

& $Run @"

Load("ps3/sn.bin", false).Dump("ps3_sn", isDumpBinary: true, isDumpScript: true);

"@