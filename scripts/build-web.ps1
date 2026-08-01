$ErrorActionPreference = 'Stop'
$webDirectory = Join-Path $PSScriptRoot '../src/MarkdownEditor.Web'
Push-Location $webDirectory
try {
  corepack pnpm install --frozen-lockfile
  corepack pnpm build
}
finally {
  Pop-Location
}
