. $PSScriptRoot/Defines.ps1

pushd $DataRoot

& dotnet run -p $PSScriptRoot/../CsYetiTools -c Release -- benchmark

popd