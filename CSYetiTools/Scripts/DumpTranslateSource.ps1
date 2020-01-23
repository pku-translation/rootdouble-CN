. $PSScriptRoot/Defines.ps1

& $Run dump-trans-source `
    --input=$DataRoot/sn_jp_psv.bin `
    --outputdir=$PSScriptRoot/../../source_json