. $PSScriptRoot/Defines.ps1

& $Run gen-code-compare `
    --input=$DataRoot/sn_jp_psv.bin `
    --input-steam=$DataRoot/sn_en_steam.bin `
    --outputdir=$DataRoot/code_compare `
    --modifier-file=$DataRoot/string_list_modifiers.sexpr
