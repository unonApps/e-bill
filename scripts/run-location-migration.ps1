# Script to add Location column to EbillUsers table
# Update the connection parameters if needed

# SQL Server connection settings
$serverName = ".\SQLEXPRESS"  # SQL Express instance
$databaseName = "TABDB"

# Use Windows Authentication
$useWindowsAuth = $true

Write-Host "================================================================" -ForegroundColor Green
Write-Host "ADDING LOCATION COLUMN TO EBILLUSERS TABLE" -ForegroundColor Green
Write-Host "================================================================" -ForegroundColor Green
Write-Host ""

# Execute the SQL script
Write-Host "Attempting to connect to SQL Server..." -ForegroundColor Cyan

if ($useWindowsAuth) {
    Write-Host "Using Windows Authentication..." -ForegroundColor Yellow
    $result = sqlcmd -S $serverName -d $databaseName -E -i "add-location-to-ebillusers.sql" 2>&1
} else {
    Write-Host "Using SQL Authentication..." -ForegroundColor Yellow
    $result = sqlcmd -S $serverName -d $databaseName -U $username -P $password -i "add-location-to-ebillusers.sql" 2>&1
}

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "✅ Location column added successfully to EbillUsers table!" -ForegroundColor Green
    Write-Host $result
} else {
    Write-Host ""
    Write-Host "❌ Error executing migration script" -ForegroundColor Red
    Write-Host $result -ForegroundColor Red
    Write-Host ""
    Write-Host "Please ensure:" -ForegroundColor Yellow
    Write-Host "1. SQL Server is running" -ForegroundColor Yellow
    Write-Host "2. The connection parameters are correct" -ForegroundColor Yellow
    Write-Host "3. The TABDB database exists" -ForegroundColor Yellow
    Write-Host "4. The EbillUsers table exists" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Press any key to continue..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")