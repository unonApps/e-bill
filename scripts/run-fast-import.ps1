# Fast import script using SQL BULK INSERT
Write-Host "================================================================" -ForegroundColor Green
Write-Host "FAST BULK IMPORT FOR EBILLUSERS (200K RECORDS)" -ForegroundColor Green
Write-Host "================================================================" -ForegroundColor Green
Write-Host ""

$serverName = ".\SQLEXPRESS"
$databaseName = "TABDB"

Write-Host "This will use SQL Server's BULK INSERT for maximum speed." -ForegroundColor Cyan
Write-Host "Expected time: 1-2 minutes for 200k records" -ForegroundColor Yellow
Write-Host ""

# First ensure organizations and offices have codes
Write-Host "Step 1: Preparing organization codes..." -ForegroundColor Cyan
sqlcmd -S $serverName -d $databaseName -E -i "prepare-codes-mapping.sql" -o prepare_log.txt

Write-Host "✅ Codes prepared" -ForegroundColor Green
Write-Host ""

# Run the fast bulk import
Write-Host "Step 2: Running fast bulk import..." -ForegroundColor Cyan
Write-Host "This will:" -ForegroundColor Yellow
Write-Host "  1. Bulk load CSV data" -ForegroundColor White
Write-Host "  2. Clean invalid records" -ForegroundColor White
Write-Host "  3. Map codes to IDs" -ForegroundColor White
Write-Host "  4. Import to EbillUsers table" -ForegroundColor White
Write-Host ""

$startTime = Get-Date
Write-Host "Start time: $startTime" -ForegroundColor Gray

# Execute with extended timeout
sqlcmd -S $serverName -d $databaseName -E -i "fast-bulk-import.sql" -t 600

$endTime = Get-Date
$duration = $endTime - $startTime

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "✅ IMPORT COMPLETED SUCCESSFULLY!" -ForegroundColor Green
    Write-Host "Duration: $($duration.TotalMinutes.ToString('F2')) minutes" -ForegroundColor Cyan
} else {
    Write-Host ""
    Write-Host "❌ Import encountered errors. Check the output above." -ForegroundColor Red
}

Write-Host ""
Write-Host "Press any key to exit..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")