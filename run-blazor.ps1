# Packaging Tender Decision Engine (PTD-E)
# Blazor Cockpit — Launch Script
#
# Starts the Blazor frontend (Decision Cockpit).
# Access the cockpit at: https://localhost:5001 or http://localhost:5000
#
# For WinForms verification shell, use: run-winforms.ps1

Set-Location $PSScriptRoot

Get-Process -Name "PackagingTenderTool.Blazor" -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Milliseconds 500

dotnet build .\PackagingTenderTool.sln
if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed. Fix the errors above and run again." -ForegroundColor Red
    return
}

Write-Host ""
Write-Host "Starting Packaging Tender Decision Engine (PTD-E)..." -ForegroundColor Green
Write-Host "Open your browser at: https://localhost:5001" -ForegroundColor Cyan
Write-Host ""

dotnet run --project .\src\PackagingTenderTool.Blazor\PackagingTenderTool.Blazor.csproj
