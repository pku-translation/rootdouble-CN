. $PSScriptRoot/Defines.ps1

& dotnet run -p $PSScriptRoot/../CsYetiTools -c Debug -- test-bed `
    --data-path=$DataRoot/ `
    