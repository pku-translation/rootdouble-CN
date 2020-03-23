. $PSScriptRoot/Defines.ps1

Push-Location $DataRoot

& dotnet run -p $PSScriptRoot/../CsYetiTools -c Release -- benchmark

Pop-Location
