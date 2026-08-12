$ErrorActionPreference = 'Stop'
$publishDirectory = Join-Path $PSScriptRoot '../artifacts/publish'
$archivePath = Join-Path $PSScriptRoot '../artifacts/markdown-editor-win-x64.zip'
$executablePath = Join-Path $publishDirectory 'MarkdownEditor.App.exe'

if (!(Test-Path -LiteralPath $archivePath)) { throw "Package not found: $archivePath" }
if (!(Test-Path -LiteralPath $executablePath)) { throw "Published executable not found: $executablePath" }

$requiredEntries = @(
  'MarkdownEditor.App.exe',
  'MarkdownEditor.App.dll',
  'Web/index.html',
  'mermaid-rendering-test-cases.md'
)
$archive = [System.IO.Compression.ZipFile]::OpenRead($archivePath)
try {
  $entryNames = $archive.Entries.FullName.Replace('\', '/')
  foreach ($requiredEntry in $requiredEntries) {
    if ($entryNames -notcontains $requiredEntry) {
      throw "Package is missing required entry: $requiredEntry"
    }
  }
  if (!($entryNames | Where-Object { $_ -like 'Web/assets/*.js' })) {
    throw 'Package is missing the compiled Web JavaScript asset.'
  }
}
finally { $archive.Dispose() }

$process = Start-Process -FilePath $executablePath -PassThru -WindowStyle Hidden
try {
  Start-Sleep -Seconds 5
  if ($process.HasExited) {
    throw "Packaged application exited during startup with code $($process.ExitCode)."
  }
}
finally {
  if (!$process.HasExited) {
    Stop-Process -Id $process.Id
    $process.WaitForExit(5000)
  }
}

Write-Output 'Packaged application startup and archive integrity smoke checks passed.'
