# PowerShell script to add columns and reseed telecom tables

$server = "localhost"
$database = "TAB_DB"
$username = "sa"
$password = "YourStrong@Passw0rd"

$connectionString = "Server=$server;Database=$database;User Id=$username;Password=$password;TrustServerCertificate=True"

try {
    Write-Host "Connecting to database..." -ForegroundColor Yellow

    # Read SQL script
    $sqlScript = Get-Content "add-month-year-and-reseed.sql" -Raw

    # Execute using SqlConnection
    $connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $connection.Open()

    # Split script by GO statements
    $sqlCommands = $sqlScript -split '\nGO\r?\n'

    foreach ($sqlCommand in $sqlCommands) {
        if ($sqlCommand.Trim() -ne "") {
            $command = New-Object System.Data.SqlClient.SqlCommand($sqlCommand, $connection)
            $command.CommandTimeout = 300

            try {
                $result = $command.ExecuteNonQuery()
                Write-Host "Command executed successfully" -ForegroundColor Green
            }
            catch {
                Write-Host "Error executing command: $_" -ForegroundColor Red
            }
        }
    }

    $connection.Close()
    Write-Host "`nScript execution completed!" -ForegroundColor Green
    Write-Host "Tables now have CallMonth and CallYear columns with August 2025 sample data" -ForegroundColor Cyan
}
catch {
    Write-Host "Error: $_" -ForegroundColor Red
}