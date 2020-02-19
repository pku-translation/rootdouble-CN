. $PSScriptRoot/Defines.ps1

& $Run replace-string-list `
    --modifiers=$DataRoot/string_list_modifiers.ss `
    --input-ref=$DataRoot/psv/sn.bin `
    --input-steam=$DataRoot/steam/sn.bin `
    --output=$DataRoot/sn_steam_modified.bin `
    #--dump-result-text-path=$DataRoot/steam_sn_modified/
