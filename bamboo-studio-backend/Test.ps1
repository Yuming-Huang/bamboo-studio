param([string]$RhinoRoot = (Join-Path $env:ProgramFiles 'Rhino 8'), [string]$KarambaRoot, [switch]$CoreOnly, [switch]$CheckGrasshopper)
$ErrorActionPreference = 'Stop'
if (-not $KarambaRoot) { $KarambaRoot = Join-Path $RhinoRoot 'Plug-ins/Karamba' }
$reports = Join-Path $PSScriptRoot 'artifacts/test-results'
New-Item -ItemType Directory -Path $reports -Force | Out-Null
Push-Location $reports
try {
    & dotnet run --project (Join-Path $PSScriptRoot 'tests/Search/Search.Tests.csproj') -c Release
    if ($LASTEXITCODE -ne 0) { throw 'Search tests failed' }
    if (-not $CoreOnly) {
        & (Join-Path $PSScriptRoot 'Build.ps1') -RhinoRoot $RhinoRoot -KarambaRoot $KarambaRoot
        & dotnet run --project (Join-Path $PSScriptRoot 'tests/Adapter/Adapter.Tests.csproj') -c Release "-p:RhinoRoot=$RhinoRoot" "-p:KarambaRoot=$KarambaRoot" -- (Join-Path $PSScriptRoot 'tests/Adapter/input.json') (Join-Path $PSScriptRoot 'artifacts/plugin/BambooKarambaBridge.rhp') $RhinoRoot $KarambaRoot
        if ($LASTEXITCODE -ne 0) { throw 'Adapter tests failed' }
        if ($CheckGrasshopper) {
            foreach ($project in Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'grasshopper/components') -Filter '*.csproj') {
                & dotnet build $project.FullName -c Release "-p:RhinoRoot=$RhinoRoot" "-p:KarambaRoot=$KarambaRoot"
                if ($LASTEXITCODE -ne 0) { throw "GH component compile failed: $($project.Name)" }
            }
        }
    }
    Write-Host 'Software tests passed. No Rhino UI or live Karamba solve was started.'
} finally { Pop-Location }
