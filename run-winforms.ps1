Set-Location $PSScriptRoot

Get-Process PackagingTenderTool.App -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Milliseconds 500

dotnet build .\PackagingTenderTool.sln
if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed. Fix the errors above and run again." -ForegroundColor Red
    return
}

& .\src\PackagingTenderTool.App\bin\Debug\net10.0-windows\PackagingTenderTool.App.exe
