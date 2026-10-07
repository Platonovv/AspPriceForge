$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

if (Test-Path openapi.json) {
    git add openapi.json
}

$env:UPDATE_OPENAPI = '1'
try {
    dotnet test tests/PriceForge.IntegrationTests --filter "FullyQualifiedName~OpenApiContractTests" --nologo
    if ($LASTEXITCODE -ne 0) {
        throw "Snapshot update failed (is Docker running?)"
    }
}
finally {
    Remove-Item Env:UPDATE_OPENAPI -ErrorAction SilentlyContinue
}

Write-Host ""
Write-Host "=== Contract changes (openapi.json) ===" -ForegroundColor Cyan
git --no-pager diff --stat openapi.json
git --no-pager diff openapi.json

$changed = git diff --name-only openapi.json
if (-not $changed) {
    Write-Host "No contract changes." -ForegroundColor Green
}
