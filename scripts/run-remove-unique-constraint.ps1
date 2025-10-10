# PowerShell script to remove the unique constraint on IndexNo in SimRequests table
# This allows multiple SIM requests for the same staff member

Write-Host "Removing unique constraint on SimRequests.IndexNo..." -ForegroundColor Yellow

# Get connection string from appsettings.json
$appSettings = Get-Content "appsettings.json" | ConvertFrom-Json
$connectionString = $appSettings.ConnectionStrings.DefaultConnection

if (-not $connectionString) {
    Write-Host "Error: Could not find connection string in appsettings.json" -ForegroundColor Red
    exit 1
}

# Extract server and database from connection string
if ($connectionString -match "Server=([^;]+).*Database=([^;]+)") {
    $server = $matches[1]
    $database = $matches[2]
    
    Write-Host "Server: $server" -ForegroundColor Cyan
    Write-Host "Database: $database" -ForegroundColor Cyan
    
    # Run the SQL script
    try {
        sqlcmd -S $server -d $database -i "SQL\RemoveIndexNoUniqueConstraint.sql"
        Write-Host "Successfully removed unique constraint!" -ForegroundColor Green
    }
    catch {
        Write-Host "Error running SQL script: $_" -ForegroundColor Red
        Write-Host ""
        Write-Host "Alternative: Run this SQL manually in SQL Server Management Studio:" -ForegroundColor Yellow
        Write-Host "DROP INDEX IX_SimRequests_IndexNo ON dbo.SimRequests;" -ForegroundColor White
        Write-Host "CREATE NONCLUSTERED INDEX IX_SimRequests_IndexNo ON dbo.SimRequests (IndexNo);" -ForegroundColor White
    }
}
else {
    Write-Host "Could not parse connection string" -ForegroundColor Red
    Write-Host "Please run the following SQL manually in your database:" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "DROP INDEX IX_SimRequests_IndexNo ON dbo.SimRequests;" -ForegroundColor White
    Write-Host "CREATE NONCLUSTERED INDEX IX_SimRequests_IndexNo ON dbo.SimRequests (IndexNo);" -ForegroundColor White
}