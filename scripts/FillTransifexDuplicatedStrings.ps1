. $PSScriptRoot/Defines.ps1

& $Run fill-fx-dups `
    --input=$DataRoot/sn_en_steam.bin `
    --input-ref=$DataRoot/sn_jp_psv.bin `
    --modifier-file=$DataRoot/string_list_modifiers.sexpr `
    --url="https://www.transifex.com/api/2/project/rootdouble_steam_cn/resource/source-json-chunk-<chunk>-json--master/translation/zh_CN/strings/" `
    --pattern="^.{5}.*$" `
    #--token=
