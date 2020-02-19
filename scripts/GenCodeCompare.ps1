. $PSScriptRoot/Defines.ps1

& $Run gen-code-compare `
    --input=$DataRoot/psv/sn.bin `
    --input-steam=$DataRoot/steam/sn.bin `
    --outputdir=$DataRoot/code_compare `
    --modifier-file=$DataRoot/string_list_modifiers.ss
