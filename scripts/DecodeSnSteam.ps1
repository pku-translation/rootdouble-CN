. $PSScriptRoot/Defines.ps1

& $Run decode-sn `
    --input=$DataRoot/sn_en_steam.bin `
    --outputdir=$DataRoot/sn_en_steam `
    --string-pooled --dump-binary --dump-script