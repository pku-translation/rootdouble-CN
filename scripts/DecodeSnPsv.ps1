. $PSScriptRoot/Defines.ps1

& $Run decode-sn `
    --input=$DataRoot/psv/sn.bin `
    --outputdir=$DataRoot/sn_psv `
    --dump-binary --dump-script
