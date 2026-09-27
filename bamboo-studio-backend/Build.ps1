param([string]$RhinoRoot, [string]$KarambaRoot)
$ErrorActionPreference = 'Stop'
Push-Location $PSScriptRoot
try {
    $properties = @()
    if ($RhinoRoot) { $properties += "-p:RhinoRoot=$RhinoRoot" }
    if ($KarambaRoot) { $properties += "-p:KarambaRoot=$KarambaRoot" }
    & dotnet build src/BambooKarambaBridge/BambooKarambaBridge.csproj -c Release -o artifacts/plugin @properties
    if ($LASTEXITCODE -ne 0) { throw 'Bridge build failed' }
    Write-Host 'Built: artifacts/plugin/BambooKarambaBridge.rhp. No installed plugin has been replaced.'
} finally { Pop-Location }
