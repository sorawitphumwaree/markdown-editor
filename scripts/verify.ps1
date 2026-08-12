$ErrorActionPreference = 'Stop'

& "$PSScriptRoot/test.ps1"
& "$PSScriptRoot/package.ps1"
& "$PSScriptRoot/smoke-package.ps1"
