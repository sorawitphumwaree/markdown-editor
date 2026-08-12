$ErrorActionPreference = 'Stop'
& "$PSScriptRoot/build-web.ps1"
$publishDirectory = "$PSScriptRoot/../artifacts/publish"
if (Test-Path -LiteralPath $publishDirectory) {
  Remove-Item -LiteralPath $publishDirectory -Recurse -Force
}
dotnet publish "$PSScriptRoot/../src/MarkdownEditor.App/MarkdownEditor.App.csproj" `
  --configuration Release --runtime win-x64 --self-contained false `
  --output $publishDirectory -p:BuildWeb=false
Copy-Item -LiteralPath "$PSScriptRoot/../docs/test-cases/mermaid-rendering.md" `
  -Destination "$publishDirectory/mermaid-rendering-test-cases.md"
Compress-Archive -Path "$publishDirectory/*" `
  -DestinationPath "$PSScriptRoot/../artifacts/markdown-editor-win-x64.zip" -Force
