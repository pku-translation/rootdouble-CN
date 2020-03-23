param (
    [Parameter(Mandatory=$true)][string] $file,
    [Parameter(Mandatory=$true)][string] $outputDir
)

. $PSScriptRoot/Defines.ps1

$path = $file.replace("\", "/")

& $RunCsx @"
var cpk = Cpk.FromFile("$path");
CpkHelper.DumpCpk(cpk, "$outputDir")

"@

# ./scripts/DumpCpk.ps1 -file bg.cpk -outputDir steam_dump/bg
