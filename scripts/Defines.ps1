$DataRoot = Join-Path $PSScriptRoot /../data/
$Run = { dotnet run -p $PSScriptRoot/../CsYetiTools -c Release -- $args }