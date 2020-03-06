. $PSScriptRoot/Defines.ps1

Push-Location $DataRoot

try {
    & dotnet run -p $PSScriptRoot/../CsYetiTools -c Release -- testbed
}
finally {
    Pop-Location
}