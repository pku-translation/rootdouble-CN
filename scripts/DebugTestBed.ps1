. $PSScriptRoot/Defines.ps1

Push-Location $DataRoot

try {
    & dotnet run --project $PSScriptRoot/../CSYetiTools.Commandlet -c Debug -- testbed
}
finally {
    Pop-Location
}