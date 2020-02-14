. $PSScriptRoot/Defines.ps1

& $Run test-bed `
    --input=$DataRoot/sn_jp_psv.bin `
    --input-steam=$DataRoot/sn_en_steam.bin `
    --text-output=$DataRoot/test_bed.txt
    