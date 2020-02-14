. $PSScriptRoot/Defines.ps1

& dotnet run -p $PSScriptRoot/../CsYetiTools -c Debug -- test-bed `
    --input=$DataRoot/sn_jp_psv.bin `
    --input-steam=$DataRoot/sn_en_steam.bin `
    --text-output=$DataRoot/test_bed.txt
    