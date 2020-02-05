. $PSScriptRoot/Defines.ps1

& $Run dump-trans-source `
    --input=$DataRoot/sn_en_steam.bin `
    --input-ref=$DataRoot/sn_jp_psv.bin `
    --outputdir=$PSScriptRoot/../../source_json `
    --modifier-file=$PSScriptRoot/../string_list_modifiers.sexpr
