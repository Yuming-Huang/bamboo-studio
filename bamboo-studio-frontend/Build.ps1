param([switch]$SkipTests)
$ErrorActionPreference = 'Stop'
Push-Location $PSScriptRoot
try {
    & npm.cmd ci
    if ($LASTEXITCODE -ne 0) { throw 'npm ci failed' }
    if (-not $SkipTests) {
        & npm.cmd test
        if ($LASTEXITCODE -ne 0) { throw 'Frontend tests failed' }
    }
    & npm.cmd run build:ui
    if ($LASTEXITCODE -ne 0) { throw 'UI build failed' }
    & dotnet restore BambooStudio.Frontend.csproj --locked-mode
    if ($LASTEXITCODE -ne 0) { throw 'NuGet restore failed' }
    & dotnet publish BambooStudio.Frontend.csproj -c Release --no-restore -o artifacts/desktop
    if ($LASTEXITCODE -ne 0) { throw 'Desktop publish failed' }
    Write-Host 'Built: artifacts/desktop/BambooStudio.exe (distribute the whole desktop folder).'
} finally { Pop-Location }
