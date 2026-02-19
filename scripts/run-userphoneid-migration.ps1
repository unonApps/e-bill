# Azure SQL Database connection details
$ServerInstance = "ebiling.database.windows.net"
$Database = "tabdb"
$Username = "ebiling"
$SecurePassword = Read-Host "Enter Azure SQL password" -AsSecureString
$BSTR = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($SecurePassword)
$Password = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto($BSTR)
$SqlFile = "add-userphoneid-azure.sql"

Write-Host "Connecting to Azure SQL Database..." -ForegroundColor Cyan
Write-Host "Server: $ServerInstance" -ForegroundColor Cyan
Write-Host "Database: $Database" -ForegroundColor Cyan
Write-Host ""

try {
    # Execute the SQL script
    Invoke-Sqlcmd -ServerInstance $ServerInstance `
                  -Database $Database `
                  -Username $Username `
                  -Password $Password `
                  -InputFile $SqlFile `
                  -Verbose `
                  -ErrorAction Stop

    Write-Host ""
    Write-Host "Migration completed successfully!" -ForegroundColor Green
}
catch {
    Write-Host ""
    Write-Host "Error executing migration:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
}
