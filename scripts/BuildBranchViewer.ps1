. $PSScriptRoot/Defines.ps1

Remove-Item -R -Force $DataRoot/BranchViewer/
mkdir $DataRoot/BranchViewer/

dotnet publish $PSScriptRoot/../CSYetiTools.BranchViewer/ `
    --force `
    -c Release -r win-x64 -o $DataRoot/BranchViewer/ `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:PublishReadyToRun=true `
    -p:PublishProfileFullPath=$DataRoot/BranchViewer/BranchViewer.exe

. $PSScriptRoot/ExportGraph.ps1