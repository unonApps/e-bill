# Verify Test Data Script
Write-Host "`n===== VERIFYING TEST DATA =====" -ForegroundColor Cyan

$serverName = "MICHUKI\SQLEXPRESS"
$databaseName = "TABDB"

# Run verification query
sqlcmd -S $serverName -d $databaseName -E -i "create-test-interim-simple.sql"

Write-Host "`n===== NEXT STEPS =====" -ForegroundColor Yellow
Write-Host "1. Start your application" -ForegroundColor White
Write-Host "2. Navigate to: http://localhost:5041/Admin/CallLogStaging" -ForegroundColor White
Write-Host "3. Click 'Consolidate New Batch' button" -ForegroundColor White
Write-Host "4. You should see:" -ForegroundColor White
Write-Host "   - Pending verification summary at the top" -ForegroundColor Gray
Write-Host "   - Two tabs: 'Monthly Consolidation' and 'Staff Separation (Interim)'" -ForegroundColor Gray
Write-Host "5. Click 'Staff Separation (Interim)' tab" -ForegroundColor White
Write-Host "6. Enter Staff Index Number: TEST001" -ForegroundColor Green
Write-Host "7. The system should validate and show 'John Doe'" -ForegroundColor White
Write-Host "8. Fill in:" -ForegroundColor White
Write-Host "   - Separation Date: Today's date" -ForegroundColor Gray
Write-Host "   - Separation Reason: Any option" -ForegroundColor Gray
Write-Host "   - Billing Start Date: First day of current month" -ForegroundColor Gray
Write-Host "   - Billing End Date: Today" -ForegroundColor Gray
Write-Host "9. Click 'Consolidate'" -ForegroundColor Green

Write-Host "`n===== EXPECTED RESULT =====" -ForegroundColor Cyan
Write-Host "The system will create a batch named:" -ForegroundColor White
Write-Host "'INTERIM - John Doe - [Today's Date]'" -ForegroundColor Yellow
Write-Host "And import all call records for phone 254722123456" -ForegroundColor White

Read-Host "`nPress Enter to exit"