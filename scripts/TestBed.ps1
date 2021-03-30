. $PSScriptRoot/Defines.ps1

Push-Location $DataRoot

try {
    & dotnet run -p $PSScriptRoot/../CSYetiTools -c Release -- testbed
}
finally {
    Pop-Location
}