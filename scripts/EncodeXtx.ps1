param (
    [Parameter(Mandatory=$true)][string] $in,
    [Parameter(Mandatory=$true)][int] $format,
    [Parameter(Mandatory=$true)][bool] $font,
    [Parameter(Mandatory=$true)][string] $out
)

. $PSScriptRoot/Defines.ps1

$in = $in.replace('\', '/')
$out = $out.replace('\', '/')
$isFont = if ($font) { "true" } else { "false" }

& $RunCsx @"

var img = SixLabors.ImageSharp.Image.Load(System.IO.File.ReadAllBytes("$in"));
var xtx = new CsYetiTools.FileTypes.Xtx(img, $format, $isFont);
System.IO.File.WriteAllBytes("$out", xtx.ToBytes());

"@