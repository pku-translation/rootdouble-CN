. $PSScriptRoot/Defines.ps1

& $RunCsx @"

var package = Load("steam/sn.bin", true);
var jpPackage = Load("psv/sn.bin", false);
package.ReplaceStringTable(jpPackage, StringListModifier.LoadFile("string_list_modifiers.ss"));

ReleaseTranslation(
    executable: "steam/executable",
    executableStringPool: "string_pool/",
    snPackage: package,
    translationSourceDir: "../source_json/",
    translationDir: "../zh_CN/",
    releaseDir: "release/",
    true
);

"@
