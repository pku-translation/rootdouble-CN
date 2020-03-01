$DataRoot = (Join-Path $PSScriptRoot /../data).replace('\', '/')
$ScriptRoot = (Join-Path $PSScriptRoot /../scripts).replace('\', '/')
$Run = {
    pushd $DataRoot
    try {
        $args | dotnet run -p $PSScriptRoot/../CsYetiTools -c Release
    }
    finally {
        popd
    }
}
