. $PSScriptRoot/Defines.ps1

& $Run decode-sn `
    --input=$DataRoot/sn_jp_psv.bin `
    --outputdir=$DataRoot/sn_jp_psv `
    --dump-binary --dump-script
