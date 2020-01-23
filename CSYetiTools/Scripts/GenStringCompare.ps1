. $PSScriptRoot/Defines.ps1

& $Run gen-string-compare `
    --input=$DataRoot/sn_jp_psv.bin `
    --input-steam=$DataRoot/sn_en_steam.bin `
    --outputdir=$DataRoot/string_compare 