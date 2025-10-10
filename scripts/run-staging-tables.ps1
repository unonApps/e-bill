# PowerShell script to create staging tables in SQL Server
# Run this script in PowerShell as Administrator

$connectionString = "Server=MICHUKI\SQLEXPRESS;Database=TABDB;Integrated Security=True;MultipleActiveResultSets=true;TrustServerCertificate=true"
$sqlFile = "create-staging-tables.sql"

Write-Host "Creating Call Log Staging tables..." -ForegroundColor Green

try {
    # Read SQL file content
    $sqlContent = Get-Content $sqlFile -Raw

    # Split by GO statements
    $sqlStatements = $sqlContent -split '\bGO\b'

    # Create connection
    $connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $connection.Open()

    foreach ($statement in $sqlStatements) {
        if ($statement.Trim() -ne "") {
            $command = New-Object System.Data.SqlClient.SqlCommand($statement, $connection)
            $command.ExecuteNonQuery() | Out-Null
            Write-Host "." -NoNewline
        }
    }

    $connection.Close()

    Write-Host ""
    Write-Host "Staging tables created successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "You can now access the Call Log Staging page at:" -ForegroundColor Cyan
    Write-Host "http://localhost:5041/Admin/CallLogStaging" -ForegroundColor Yellow

} catch {
    Write-Host "Error creating tables: $_" -ForegroundColor Red
    Write-Host ""
    Write-Host "Alternative: Please run the following SQL script manually in SQL Server Management Studio:" -ForegroundColor Yellow
    Write-Host "create-staging-tables.sql" -ForegroundColor Cyan
}

Write-Host ""
Write-Host "Press any key to exit..."
$host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")