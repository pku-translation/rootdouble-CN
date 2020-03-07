param (
    [Parameter(Mandatory=$true)][string] $outputDir
)

. $PSScriptRoot/Defines.ps1

if (Test-Path env:RW_ROOT) {

   $path = Join-Path $env:RW_ROOT "data/sys.cpk" 
   $path = $path.replace("\", "/")
   
   & $RunCsx @"
   var cpk = Cpk.FromFile("$path");
   CpkHelper.DumpSys(cpk, "$outputDir");

"@

}
else {
    Write-Output "please set env ""RW_ROOT"" to game folder."
}



