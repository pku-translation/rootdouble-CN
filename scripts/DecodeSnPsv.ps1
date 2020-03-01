. $PSScriptRoot/Defines.ps1

& $RunCsx @"

Load("psv/sn.bin", false).Dump("psv_sn", isDumpBinary: true, isDumpScript: true);

"@
