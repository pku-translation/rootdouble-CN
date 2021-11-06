. $PSScriptRoot/Defines.ps1

& $RunCsx @"

var package = Load("steam/sn.bin", true);
var jpPackage = Load("psv/sn.bin", false);
package.ReplaceStringTable(jpPackage, StringListModifier.LoadFile("string_list_modifiers.sexp"));

ReleaseTranslation(executable: "steam/executable"
                 , executableStringPool: "string_pool/"
                 , snPackage: package
                 , translationSourceDir: "../source_json/"
                 , translationDir: "../translated/"
                 , releaseDir: "release/"
                 // , dumpFontTexture: true
);

"@