. $PSScriptRoot/Defines.ps1

& $Run decode-sn `
    --input=$DataRoot/steam/sn.bin `
    --outputdir=$DataRoot/sn_steam/ `
    --string-pooled --dump-binary --dump-script
