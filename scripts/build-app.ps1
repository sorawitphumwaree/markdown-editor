$ErrorActionPreference = 'Stop'
& "$PSScriptRoot/build-web.ps1"
dotnet build "$PSScriptRoot/../MarkdownEditor.sln" --configuration Release `
  -p:BuildWeb=false
