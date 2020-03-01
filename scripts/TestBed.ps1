. $PSScriptRoot/Defines.ps1

pushd $DataRoot

try {
    & dotnet run -p $PSScriptRoot/../CsYetiTools -c Release -- testbed
}
finally {
    popd
}