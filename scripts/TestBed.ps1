. $PSScriptRoot/Defines.ps1

Push-Location $DataRoot

try {
    & dotnet run -p $PSScriptRoot/../CSYetiTools.Commandlet -c Debug -- testbed
}
finally {
    Pop-Location
}