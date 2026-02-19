# Test Azure SQL Database Connection
Write-Host "Testing Azure SQL Database Connection..." -ForegroundColor Yellow

$Password = Read-Host "Enter Azure SQL password" -AsSecureString
$BSTR = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($Password)
$PlainPassword = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto($BSTR)
$connectionString = "Server=tcp:ebiling.database.windows.net,1433;Initial Catalog=tabdb;User ID=ebiling;Password=$PlainPassword;Encrypt=True;TrustServerCertificate=False;"

try {
    $connection = New-Object System.Data.SqlClient.SqlConnection
    $connection.ConnectionString = $connectionString

    Write-Host "Attempting to connect to Azure SQL Database..." -ForegroundColor Cyan
    $connection.Open()

    Write-Host "✅ Connection SUCCESSFUL!" -ForegroundColor Green
    Write-Host "Server Version: $($connection.ServerVersion)" -ForegroundColor Gray

    # Test a simple query
    $command = $connection.CreateCommand()
    $command.CommandText = "SELECT COUNT(*) as TableCount FROM sys.tables"
    $result = $command.ExecuteScalar()
    Write-Host "Number of tables in database: $result" -ForegroundColor Gray

    $connection.Close()
}
catch {
    Write-Host "❌ Connection FAILED!" -ForegroundColor Red
    Write-Host "Error: $_" -ForegroundColor Red

    Write-Host "`nTroubleshooting suggestions:" -ForegroundColor Yellow
    Write-Host "1. Check if password needs URL encoding (% -> %25)" -ForegroundColor White
    Write-Host "2. Verify firewall rules allow your IP in Azure SQL" -ForegroundColor White
    Write-Host "3. Ensure 'Allow Azure services' is enabled" -ForegroundColor White
    Write-Host "4. Check username and password are correct" -ForegroundColor White
}