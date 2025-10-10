# Master script to import EbillUsers from CSV with code mapping
# Runs all necessary steps in sequence

Write-Host "================================================================" -ForegroundColor Green
Write-Host "EBILLUSERS CSV IMPORT WITH CODE MAPPING" -ForegroundColor Green
Write-Host "================================================================" -ForegroundColor Green
Write-Host ""

$serverName = ".\SQLEXPRESS"
$databaseName = "TABDB"
$csvPath = "C:\Users\dxmic\Downloads\ebill user.csv"

# Step 1: Prepare codes mapping
Write-Host "Step 1: Preparing organization and office codes..." -ForegroundColor Cyan
Write-Host "This ensures all codes from the CSV are mapped in the database" -ForegroundColor Yellow
Write-Host ""

$result = sqlcmd -S $serverName -d $databaseName -E -i "prepare-codes-mapping.sql" 2>&1

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Codes preparation completed successfully!" -ForegroundColor Green
} else {
    Write-Host "❌ Error preparing codes" -ForegroundColor Red
    Write-Host $result -ForegroundColor Red
    Write-Host "Please fix the errors and try again." -ForegroundColor Yellow
    exit 1
}

Write-Host ""
Write-Host "Step 2: Importing EbillUsers from CSV..." -ForegroundColor Cyan
Write-Host "CSV Path: $csvPath" -ForegroundColor Yellow
Write-Host ""

# Check if CSV exists
if (-not (Test-Path $csvPath)) {
    Write-Host "❌ CSV file not found at: $csvPath" -ForegroundColor Red
    Write-Host "Please ensure the CSV file exists at the specified location." -ForegroundColor Yellow
    exit 1
}

# Run the import script
& ".\import-ebillusers-csv.ps1" -CsvPath $csvPath -ServerName $serverName -DatabaseName $databaseName

Write-Host ""
Write-Host "================================================================" -ForegroundColor Green
Write-Host "IMPORT PROCESS COMPLETED" -ForegroundColor Green
Write-Host "================================================================" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "1. Review the import log for any unmapped organizations or offices" -ForegroundColor White
Write-Host "2. Add any missing organizations/offices with their codes" -ForegroundColor White
Write-Host "3. Re-run the import for any skipped records" -ForegroundColor White
Write-Host "4. Verify the imported data in the EbillUsers table" -ForegroundColor White
Write-Host ""

# Show current counts
Write-Host "Checking current database status..." -ForegroundColor Cyan

$checkScript = @"
USE [$databaseName]
SELECT 'Total EbillUsers' as Metric, COUNT(*) as Count FROM EbillUsers
UNION ALL
SELECT 'Active Users', COUNT(*) FROM EbillUsers WHERE IsActive = 1
UNION ALL
SELECT 'Users with Organizations', COUNT(*) FROM EbillUsers WHERE OrganizationId IS NOT NULL
UNION ALL
SELECT 'Users with Offices', COUNT(*) FROM EbillUsers WHERE OfficeId IS NOT NULL
UNION ALL
SELECT 'Users with Locations', COUNT(*) FROM EbillUsers WHERE Location IS NOT NULL;
"@

$checkScript | sqlcmd -S $serverName -d $databaseName -E

Write-Host ""
Write-Host "Press any key to exit..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")