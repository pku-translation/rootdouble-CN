param (
    [Parameter(Mandatory=$true)][string] $file,
    [Parameter(Mandatory=$true)][string] $outputDir
)

. $PSScriptRoot/Defines.ps1


$path = $file.replace("\", "/")

& $RunCsx @"
var cpk = Cpk.FromFile("$path");
CpkHelper.DumpSys(cpk, "$outputDir");

"@
