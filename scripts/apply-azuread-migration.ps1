# Apply Azure AD columns to both local and Azure databases

Write-Host "==== Applying Azure AD Migration ====" -ForegroundColor Cyan
Write-Host ""

# Apply to Local Database
Write-Host "1. Applying to Local Database (MICHUKI\SQLEXPRESS)..." -ForegroundColor Yellow
try {
    $sqlFile = "add-azuread-columns.sql"
    Invoke-Sqlcmd -ServerInstance "MICHUKI\SQLEXPRESS" -Database "TABDB" -InputFile $sqlFile -Verbose
    Write-Host "   Success: Local database updated!" -ForegroundColor Green
}
catch {
    Write-Host "   Error updating local database:" -ForegroundColor Red
    Write-Host "   $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""

# Apply to Azure Database
Write-Host "2. Applying to Azure Database (ebiling.database.windows.net)..." -ForegroundColor Yellow
$connStr = "Server=tcp:ebiling.database.windows.net,1433;Initial Catalog=tabdb;Persist Security Info=False;User ID=ebiling;Password=KamitiF5%254;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"

try {
    Add-Type -AssemblyName System.Data
    $sqlFile = "add-azuread-columns.sql"
    $sqlScript = Get-Content -Path $sqlFile -Raw
    $connection = New-Object System.Data.SqlClient.SqlConnection($connStr)
    $connection.Open()
    $batches = $sqlScript -split '\bGO\b'
    foreach ($batch in $batches) {
        if ($batch.Trim() -ne '') {
            $command = $connection.CreateCommand()
            $command.CommandText = $batch
            $command.CommandTimeout = 120
            $null = $command.ExecuteNonQuery()
        }
    }
    $connection.Close()
    Write-Host "   Success: Azure database updated!" -ForegroundColor Green
}
catch {
    Write-Host "   Error updating Azure database:" -ForegroundColor Red
    Write-Host "   $($_.Exception.Message)" -ForegroundColor Red
    if ($connection -and $connection.State -eq 'Open') {
        $connection.Close()
    }
}

Write-Host ""
Write-Host "==== Migration Complete ====" -ForegroundColor Cyan
