$global:DataRoot = (Join-Path $PSScriptRoot /../data).replace('\', '/')
$global:ScriptRoot = (Join-Path $PSScriptRoot /../scripts).replace('\', '/')
$global:RunCsx = {
    Push-Location $DataRoot
    try {
        $args | dotnet run -p $PSScriptRoot/../CsYetiTools -c Release
    }
    finally {
        Pop-Location
    }
}
