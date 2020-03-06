param (
    [Parameter(Mandatory=$true)][string] $in,
    [Parameter(Mandatory=$true)][bool] $font,
    [Parameter(Mandatory=$true)][string] $out
)

. $PSScriptRoot/Defines.ps1

$in = $in.replace('\', '/')
$out = $out.replace('\', '/')
$isFont = if ($font) { "true" } else { "false" }

& $RunCsx @"

var xtx = new CsYetiTools.FileTypes.Xtx(System.IO.File.ReadAllBytes("$in"), $isFont);
xtx.SaveTextureTo("$out");

"@