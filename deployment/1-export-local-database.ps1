# ========================================
# Export Local Database (Schema + Data)
# Run this from DEVELOPMENT MACHINE
# ========================================

param(
    [string]$LocalServer = "(localdb)\mssqllocaldb",
    [string]$DatabaseName = "tabdb",
    [string]$OutputPath = "deployment\tabdb_full_export.bacpac"
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "EXPORT LOCAL DATABASE" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Source: $LocalServer" -ForegroundColor White
Write-Host "Database: $DatabaseName" -ForegroundColor White
Write-Host "Export to: $OutputPath" -ForegroundColor White
Write-Host ""

# Check if SqlPackage is available
$sqlPackagePath = "C:\Program Files\Microsoft SQL Server\160\DAC\bin\SqlPackage.exe"
if (-not (Test-Path $sqlPackagePath)) {
    $sqlPackagePath = "C:\Program Files\Microsoft SQL Server\150\DAC\bin\SqlPackage.exe"
}

if (-not (Test-Path $sqlPackagePath)) {
    Write-Host "ERROR: SqlPackage.exe not found!" -ForegroundColor Red
    Write-Host ""
    Write-Host "Install it from:" -ForegroundColor Yellow
    Write-Host "https://aka.ms/sqlpackage-windows" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Alternative: Use SSMS to export manually:" -ForegroundColor Yellow
    Write-Host "  1. Open SSMS" -ForegroundColor White
    Write-Host "  2. Right-click database '$DatabaseName'" -ForegroundColor White
    Write-Host "  3. Tasks -> Export Data-tier Application..." -ForegroundColor White
    Write-Host "  4. Save as: $OutputPath" -ForegroundColor White
    Write-Host ""
    exit 1
}

Write-Host "Using SqlPackage: $sqlPackagePath" -ForegroundColor Green
Write-Host ""
Write-Host "Exporting database (this may take several minutes)..." -ForegroundColor Yellow
Write-Host ""

# Export database to BACPAC (includes schema + data)
$connectionString = "Server=$LocalServer;Database=$DatabaseName;Integrated Security=True;TrustServerCertificate=True;"

try {
    & $sqlPackagePath `
        /Action:Export `
        /SourceConnectionString:$connectionString `
        /TargetFile:$OutputPath `
        /p:VerifyExtraction=True `
        /DiagnosticsFile:"deployment\export_diagnostics.log"

    if ($LASTEXITCODE -eq 0) {
        Write-Host ""
        Write-Host "========================================" -ForegroundColor Green
        Write-Host "EXPORT SUCCESSFUL!" -ForegroundColor Green
        Write-Host "========================================" -ForegroundColor Green
        Write-Host ""

        $fileInfo = Get-Item $OutputPath
        $fileSizeMB = [math]::Round($fileInfo.Length / 1MB, 2)

        Write-Host "File: $OutputPath" -ForegroundColor White
        Write-Host "Size: $fileSizeMB MB" -ForegroundColor White
        Write-Host ""
        Write-Host "Next step: Copy this file to the server and run 2-import-to-server.ps1" -ForegroundColor Cyan
        Write-Host ""
    } else {
        Write-Host ""
        Write-Host "========================================" -ForegroundColor Red
        Write-Host "EXPORT FAILED!" -ForegroundColor Red
        Write-Host "========================================" -ForegroundColor Red
        Write-Host ""
        Write-Host "Check diagnostics: deployment\export_diagnostics.log" -ForegroundColor Yellow
        Write-Host ""
        exit 1
    }
} catch {
    Write-Host ""
    Write-Host "ERROR: $_" -ForegroundColor Red
    Write-Host ""
    exit 1
}
