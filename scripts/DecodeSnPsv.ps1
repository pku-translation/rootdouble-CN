. $PSScriptRoot/Defines.ps1

& $Run @"

Load("psv/sn.bin", false).Dump("psv_sn", isDumpBinary: true, isDumpScript: true);

"@
