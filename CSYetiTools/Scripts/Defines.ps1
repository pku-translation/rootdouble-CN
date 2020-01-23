$DataRoot = Join-Path $PSScriptRoot /../data/
$Run = { dotnet run -p $PSScriptRoot/../ -c Release -- $args }