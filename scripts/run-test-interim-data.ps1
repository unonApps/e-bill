# PowerShell script to create test data for interim billing feature
Write-Host "Creating test data for Interim Billing feature..." -ForegroundColor Green

# Database connection parameters
$serverName = "MICHUKI\SQLEXPRESS"
$databaseName = "TABDB"

# Run the SQL script
try {
    Write-Host "Connecting to database..." -ForegroundColor Yellow
    sqlcmd -S $serverName -d $databaseName -E -i "create-test-interim-data.sql"

    Write-Host "`nTest data created successfully!" -ForegroundColor Green
    Write-Host "`nTest Details:" -ForegroundColor Cyan
    Write-Host "  Staff Index Number: TEST001" -ForegroundColor White
    Write-Host "  Staff Name: John Doe (Test Separation)" -ForegroundColor White
    Write-Host "  Phone Number: 254722123456" -ForegroundColor White
    Write-Host "  Email: john.doe.test@example.org" -ForegroundColor White
    Write-Host "`nCall Logs Created:" -ForegroundColor Cyan
    Write-Host "  - 20 Safaricom calls (current month)" -ForegroundColor White
    Write-Host "  - 15 Safaricom calls (previous month)" -ForegroundColor White
    Write-Host "  - 10 Airtel calls (current month)" -ForegroundColor White

    Write-Host "`nHow to test the feature:" -ForegroundColor Yellow
    Write-Host "1. Navigate to /Admin/CallLogStaging" -ForegroundColor White
    Write-Host "2. Click 'Consolidate New Batch'" -ForegroundColor White
    Write-Host "3. Select 'Staff Separation (Interim)' tab" -ForegroundColor White
    Write-Host "4. Enter Index Number: TEST001" -ForegroundColor White
    Write-Host "5. The system will auto-validate and fill the name" -ForegroundColor White
    Write-Host "6. Select separation date and reason" -ForegroundColor White
    Write-Host "7. Choose billing period (current month dates)" -ForegroundColor White
    Write-Host "8. Click 'Consolidate' to import interim bills" -ForegroundColor White

} catch {
    Write-Host "Error creating test data: $_" -ForegroundColor Red
}

Read-Host "`nPress Enter to exit"