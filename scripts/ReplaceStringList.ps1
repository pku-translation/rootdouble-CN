. $PSScriptRoot/Defines.ps1

& $Run replace-string-list `
    --modifiers=$DataRoot/string_list_modifiers.sexpr `
    --input-ref=$DataRoot/sn_jp_psv.bin `
    --input-steam=$DataRoot/sn_en_steam.bin `
    --output=$DataRoot/sn_en_steam_modified.bin `
    --dump-result-text-path=$DataRoot/sn_en_steam_modified/