. $PSScriptRoot/Defines.ps1

& $Run gen-string-compare `
    --input=$DataRoot/psv/sn.bin `
    --input-steam=$DataRoot/steam/sn.bin `
    --outputdir=$DataRoot/../reference/ `
    --modifier-file=$DataRoot/string_list_modifiers.ss
