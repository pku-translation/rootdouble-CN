. $PSScriptRoot/Defines.ps1

& $Run dump-trans-source `
    --input=$DataRoot/steam/sn.bin `
    --input-ref=$DataRoot/psv/sn.bin `
    --outputdir=$PSScriptRoot/../source_json `
    --modifier-file=$DataRoot/string_list_modifiers.ss
