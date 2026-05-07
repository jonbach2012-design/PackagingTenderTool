# Packaging Tender Decision Engine (PTD-E)
# Blazor Cockpit — Launch Script
#
# Starts the Blazor frontend (Decision Cockpit).
# Access the cockpit at: https://localhost:7144
#
# For WinForms verification shell, use: run-winforms.ps1

Set-Location $PSScriptRoot

Get-Process -Name "PackagingTenderTool.Blazor" -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Milliseconds 500

Write-Host ""
Write-Host "Starting Packaging Tender Decision Engine (PTD-E)..." -ForegroundColor Green
Write-Host "Open your browser at: https://localhost:7144" -ForegroundColor Cyan
Write-Host ""

Start-Process "https://localhost:7144"
dotnet run --project .\src\PackagingTenderTool.Blazor\PackagingTenderTool.Blazor.csproj
