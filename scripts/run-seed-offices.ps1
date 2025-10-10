# Script to seed offices and suboffices data
# Update the connection parameters if needed

# SQL Server connection settings
$serverName = ".\SQLEXPRESS"  # SQL Express instance
$databaseName = "TABDB"

# Use Windows Authentication
$useWindowsAuth = $true

Write-Host "================================================================" -ForegroundColor Green
Write-Host "SEEDING OFFICES AND SUBOFFICES DATA" -ForegroundColor Green
Write-Host "================================================================" -ForegroundColor Green
Write-Host ""

# Execute the SQL script
Write-Host "Attempting to connect to SQL Server..." -ForegroundColor Cyan

if ($useWindowsAuth) {
    Write-Host "Using Windows Authentication..." -ForegroundColor Yellow
    $result = sqlcmd -S $serverName -d $databaseName -E -i "seed-offices-suboffices.sql" 2>&1
} else {
    Write-Host "Using SQL Authentication..." -ForegroundColor Yellow
    $result = sqlcmd -S $serverName -d $databaseName -U $username -P $password -i "seed-offices-suboffices.sql" 2>&1
}

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "✅ Offices and SubOffices seeding completed successfully!" -ForegroundColor Green
    Write-Host $result
} else {
    Write-Host ""
    Write-Host "❌ Error executing seed script" -ForegroundColor Red
    Write-Host $result -ForegroundColor Red
    Write-Host ""
    Write-Host "Please ensure:" -ForegroundColor Yellow
    Write-Host "1. SQL Server is running" -ForegroundColor Yellow
    Write-Host "2. The connection parameters are correct" -ForegroundColor Yellow
    Write-Host "3. The TABDB database exists" -ForegroundColor Yellow
    Write-Host "4. Organizations have been seeded first" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Press any key to continue..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")