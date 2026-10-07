$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

dotnet test --nologo
$testsExitCode = $LASTEXITCODE

Write-Host ""
Write-Host "=== Working tree ===" -ForegroundColor Cyan
git status --short

Write-Host ""
if ($testsExitCode -eq 0) {
    Write-Host "ALL TESTS GREEN" -ForegroundColor Green
}
else {
    Write-Host "TESTS FAILED - look for [FAIL] above" -ForegroundColor Red
    exit $testsExitCode
}
