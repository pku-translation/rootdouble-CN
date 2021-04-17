. $PSScriptRoot/Defines.ps1

Write-Output "Building BranchViewer..."
Remove-Item -Recurse -Force -ErrorAction Ignore $DataRoot/BranchViewer
mkdir $DataRoot/BranchViewer/ > $null
dotnet publish $PSScriptRoot/../CSYetiTools.BranchViewer/ `
    --force `
    -c Release -r win-x64 -o $DataRoot/BranchViewer/ `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:PublishReadyToRun=true `
    -p:PublishProfileFullPath=$DataRoot/BranchViewer/BranchViewer.exe `
    -p:DebugType=None `
    -p:DebugSymbols=false

Write-Output "Exporting graph..."
. $PSScriptRoot/ExportGraph.ps1

$p = Resolve-Path $DataRoot/BranchViewer/
Write-Output "Build -> $p"