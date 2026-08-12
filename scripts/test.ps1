$ErrorActionPreference = 'Stop'
dotnet test "$PSScriptRoot/../MarkdownEditor.sln" --configuration Release `
  -p:BuildWeb=false
$webDirectory = Join-Path $PSScriptRoot '../src/MarkdownEditor.Web'
Push-Location $webDirectory
try {
  corepack pnpm typecheck
  corepack pnpm test
  corepack pnpm test:browser
}
finally {
  Pop-Location
}
