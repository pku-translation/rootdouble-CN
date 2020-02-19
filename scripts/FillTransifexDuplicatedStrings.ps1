. $PSScriptRoot/Defines.ps1

& $Run fill-fx-dups `
    --input=$DataRoot/steam/sn.bin `
    --input-ref=$DataRoot/psv/sn.bin `
    --modifier-file=$DataRoot/string_list_modifiers.ss `
    --url="https://www.transifex.com/api/2/project/rootdouble_steam_cn/resource/source-json-chunk-<chunk>-json--master/translation/zh_CN/strings/" `
    --pattern="^.{5}.*$"
    #--token=
