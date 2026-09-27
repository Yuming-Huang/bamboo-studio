$ErrorActionPreference = 'Stop'
Push-Location $PSScriptRoot
try {
    $exe = Join-Path $PSScriptRoot 'artifacts/desktop/BambooStudio.exe'
    if (-not (Test-Path -LiteralPath $exe)) { throw 'Run Build.ps1 first.' }
    & npm.cmd run prepare:ui-test
    if ($LASTEXITCODE -ne 0) { throw 'Test preparation failed' }
    $folder = Join-Path $PSScriptRoot 'artifacts/ui-test'
    $report = Join-Path $folder 'visual-check.json'
    if (Test-Path -LiteralPath $report) { Remove-Item -LiteralPath $report }
    $process = Start-Process -FilePath $exe -ArgumentList @('--ui-test', ('"' + $folder + '"')) -WindowStyle Hidden -PassThru
    if (-not $process.WaitForExit(180000)) { Stop-Process -Id $process.Id; throw 'UI smoke test timed out' }
    if (-not (Test-Path -LiteralPath $report)) { throw 'UI test produced no report' }
    $result = Get-Content -LiteralPath $report -Raw | ConvertFrom-Json
    if (-not $result.pass) { throw $result.error }
    Write-Host 'Desktop UI replay passed. No new Karamba solve was performed.'
} finally { Pop-Location }
