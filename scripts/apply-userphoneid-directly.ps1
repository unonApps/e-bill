# Direct SQL execution using .NET SqlClient
$connectionString = "Server=tcp:ebiling.database.windows.net,1433;Initial Catalog=tabdb;Persist Security Info=False;User ID=ebiling;Password=KamitiF5%254;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"

$sqlScript = @"
-- Add UserPhoneId column to PrivateWires if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PrivateWires]') AND name = 'UserPhoneId')
BEGIN
    ALTER TABLE [dbo].[PrivateWires] ADD [UserPhoneId] int NULL;
    PRINT 'Added UserPhoneId to PrivateWires';
END

-- Add UserPhoneId column to Airtel if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Airtel]') AND name = 'UserPhoneId')
BEGIN
    ALTER TABLE [dbo].[Airtel] ADD [UserPhoneId] int NULL;
    PRINT 'Added UserPhoneId to Airtel';
END

-- Add UserPhoneId column to Safaricom if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Safaricom]') AND name = 'UserPhoneId')
BEGIN
    ALTER TABLE [dbo].[Safaricom] ADD [UserPhoneId] int NULL;
    PRINT 'Added UserPhoneId to Safaricom';
END

-- Add UserPhoneId column to PSTNs if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PSTNs]') AND name = 'UserPhoneId')
BEGIN
    ALTER TABLE [dbo].[PSTNs] ADD [UserPhoneId] int NULL;
    PRINT 'Added UserPhoneId to PSTNs';
END
"@

Write-Host "Connecting to Azure SQL Database..." -ForegroundColor Cyan

try {
    # Load System.Data assembly
    Add-Type -AssemblyName System.Data

    # Create connection
    $connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $connection.Open()

    Write-Host "Connected successfully!" -ForegroundColor Green

    # Execute SQL
    $command = $connection.CreateCommand()
    $command.CommandText = $sqlScript
    $command.CommandTimeout = 120

    Write-Host "Executing SQL script..." -ForegroundColor Cyan
    $result = $command.ExecuteNonQuery()

    Write-Host "Script executed successfully!" -ForegroundColor Green
    Write-Host "Rows affected: $result" -ForegroundColor Yellow

    $connection.Close()
}
catch {
    Write-Host "Error:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    if ($connection -and $connection.State -eq 'Open') {
        $connection.Close()
    }
    exit 1
}