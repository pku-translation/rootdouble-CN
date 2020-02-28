. $PSScriptRoot/Defines.ps1

if (Test-Path env:RW_ROOT) {

    function DumpXTXs {
        param (
            [Parameter(Mandatory=$true)][string] $file,
            [Parameter(Mandatory=$true)][string] $outputDir
        )
        $path = Join-Path $env:RW_ROOT "data" $file
        $path = $path.replace("\", "/")
        
        & $Run @"

var cpk = CsYetiTools.FileTypes.Cpk.FromFile("$path");
CsYetiTools.FileTypes.CpkHelper.DumpXTXs(cpk, "$outputDir")
    
"@
    }

    DumpXTXs

}
else {
    Write-Output "please set env ""RW_ROOT"" to game folder."
}
