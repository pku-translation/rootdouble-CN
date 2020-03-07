param (
    [Parameter(Mandatory=$true)][string] $file,
    [Parameter(Mandatory=$true)][string] $outputDir
)

. $PSScriptRoot/Defines.ps1

if (Test-Path env:RW_ROOT) {

    $path = Join-Path $env:RW_ROOT "data" $file
    $path = $path.replace("\", "/")
    
    & $RunCsx @"

    var cpk = Cpk.FromFile("$path");
    CpkHelper.DumpCpk(cpk, "$outputDir")
    
"@

}
else {
    Write-Output "please set env ""RW_ROOT"" to game folder."
}

# ./scripts/DumpCpk.ps1 -file bg.cpk -outputDir steam_dump/bg