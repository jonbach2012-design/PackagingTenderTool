# Calls development-only JSON smoke endpoints (see Program.cs: /api/tco-smoke, /api/tco-engine-smoke).
# Requires: app running in Development, e.g. https://localhost:7144
param(
    [string] $BaseUrl = "https://localhost:7144"
)

$ErrorActionPreference = "Stop"

function Get-Json($Path) {
    $uri = "$BaseUrl$Path"
    Write-Host "GET $uri" -ForegroundColor Cyan
    if (Get-Command curl.exe -ErrorAction SilentlyContinue) {
        & curl.exe -sS -k $uri
        return
    }
    if ($PSVersionTable.PSVersion.Major -ge 7) {
        (Invoke-RestMethod -Uri $uri -Method Get -SkipCertificateCheck) | ConvertTo-Json -Depth 6
        return
    }
    throw "Install PowerShell 7+ or use: curl.exe -k `"$uri`""
}

Write-Host "--- ITcoCalculator (regulatory multiplier path) ---" -ForegroundColor Yellow
Get-Json "/api/tco-smoke"

Write-Host "`n--- ITcoEngineService (label dashboard / primary tender engine) ---" -ForegroundColor Yellow
Get-Json "/api/tco-engine-smoke"
