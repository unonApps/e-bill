# PowerShell script to import EbillUsers from CSV file
# This script reads the CSV, processes it, and imports to SQL Server

param(
    [string]$CsvPath = "C:\Users\dxmic\Downloads\ebill user.csv",
    [string]$ServerName = ".\SQLEXPRESS",
    [string]$DatabaseName = "TABDB"
)

Write-Host "================================================================" -ForegroundColor Green
Write-Host "IMPORTING EBILLUSERS FROM CSV FILE" -ForegroundColor Green
Write-Host "================================================================" -ForegroundColor Green
Write-Host ""

# Check if CSV file exists
if (-not (Test-Path $CsvPath)) {
    Write-Host "Error: CSV file not found at $CsvPath" -ForegroundColor Red
    exit 1
}

Write-Host "Reading CSV file from: $CsvPath" -ForegroundColor Cyan

# Read CSV with proper encoding
try {
    $csvData = Import-Csv $CsvPath -Encoding UTF8
    Write-Host "Successfully read $($csvData.Count) records from CSV" -ForegroundColor Green
} catch {
    Write-Host "Error reading CSV file: $_" -ForegroundColor Red
    exit 1
}

# Create SQL script with bulk insert statements
$sqlScript = @"
-- Import EbillUsers from CSV data
USE [$DatabaseName]
GO

-- Create temporary table
IF OBJECT_ID('tempdb..#TempEbillUsers') IS NOT NULL
    DROP TABLE #TempEbillUsers;

CREATE TABLE #TempEbillUsers (
    OfficialMobileNumber NVARCHAR(20),
    FirstName NVARCHAR(100),
    LastName NVARCHAR(100),
    IndexNumber NVARCHAR(50),
    Location NVARCHAR(200),
    OrgCode NVARCHAR(50),
    OfficeCode NVARCHAR(50),
    SubOfficeCode NVARCHAR(50),
    ClassOfService NVARCHAR(100),
    Email NVARCHAR(256),
    ProcessingStatus NVARCHAR(50) DEFAULT 'Pending'
);

-- Insert CSV data
"@

# Process each CSV row
$validCount = 0
$skipCount = 0

foreach ($row in $csvData) {
    # Clean up the data
    $firstName = ($row.FirstName -replace '[^\w\s-]', '').Trim()
    $lastName = ($row.LastName -replace '[^\w\s-]', '').Trim()
    $indexNumber = $row.IndexNumber.Trim()
    $location = $row.Location.Trim()
    $orgCode = $row.Org.Trim()
    $officeCode = $row.Office.Trim()
    $subOfficeCode = $row.'Sub-Office'.Trim()
    $mobileNumber = $row.OfficialMobileNumber.Trim()
    $classOfService = $row.ClassOfService.Trim()

    # Skip invalid records
    if ($firstName -match '^[-&$#]' -or
        $firstName -match '(Service|Reception|Library|Fax|Office|Consultant|Intern|CONF)' -or
        $indexNumber -eq '0' -or
        [string]::IsNullOrEmpty($indexNumber)) {
        $skipCount++
        continue
    }

    # Generate email if not provided
    $email = "$($firstName.ToLower()).$($lastName.ToLower())@un.org"
    $email = $email -replace '\s+', '.' -replace '\.+', '.'

    # Handle special organization email domains
    if ($orgCode -eq 'WHO') {
        $email = "$($firstName.ToLower()).$($lastName.ToLower())@who.int"
    } elseif ($orgCode -eq 'FAO') {
        $email = "$($firstName.ToLower()).$($lastName.ToLower())@fao.org"
    } elseif ($orgCode -eq 'WFP') {
        $email = "$($firstName.ToLower()).$($lastName.ToLower())@wfp.org"
    } elseif ($orgCode -eq 'UNDP') {
        $email = "$($firstName.ToLower()).$($lastName.ToLower())@undp.org"
    } elseif ($orgCode -eq 'UNICEF') {
        $email = "$($firstName.ToLower()).$($lastName.ToLower())@unicef.org"
    }

    # Escape single quotes for SQL
    $firstName = $firstName -replace "'", "''"
    $lastName = $lastName -replace "'", "''"
    $location = $location -replace "'", "''"
    $email = $email -replace "'", "''"

    # Add insert statement
    $sqlScript += @"
INSERT INTO #TempEbillUsers (OfficialMobileNumber, FirstName, LastName, IndexNumber, Location, OrgCode, OfficeCode, SubOfficeCode, ClassOfService, Email)
VALUES ('$mobileNumber', '$firstName', '$lastName', '$indexNumber', '$location', '$orgCode', '$officeCode', '$subOfficeCode', '$classOfService', '$email');

"@
    $validCount++
}

# Add the rest of the SQL processing logic
$sqlScript += @"

-- Update processing status for invalid records
UPDATE #TempEbillUsers
SET ProcessingStatus = 'Invalid'
WHERE FirstName LIKE '%Service%'
   OR FirstName LIKE '%Reception%'
   OR FirstName LIKE '%Library%'
   OR FirstName LIKE '%Fax%'
   OR FirstName LIKE '%Office%'
   OR FirstName LIKE '%Consultant%'
   OR FirstName LIKE '%Intern%'
   OR FirstName LIKE '%CONF%'
   OR IndexNumber = '0'
   OR IndexNumber IS NULL
   OR LEN(FirstName) < 2
   OR LEN(LastName) < 2;

-- Insert valid records into EbillUsers
DECLARE @InsertedCount INT = 0;
DECLARE @SkippedCount INT = 0;
DECLARE @ErrorCount INT = 0;

-- Process records in batches
DECLARE @BatchSize INT = 100;
DECLARE @CurrentBatch INT = 0;

WHILE EXISTS (SELECT 1 FROM #TempEbillUsers WHERE ProcessingStatus = 'Pending')
BEGIN
    BEGIN TRY
        BEGIN TRANSACTION;

        -- Insert batch of records
        INSERT INTO EbillUsers (
            FirstName,
            LastName,
            IndexNumber,
            Email,
            OfficialMobileNumber,
            Location,
            ClassOfService,
            OrganizationId,
            OfficeId,
            SubOfficeId,
            IsActive,
            CreatedDate
        )
        SELECT TOP (@BatchSize)
            t.FirstName,
            t.LastName,
            t.IndexNumber,
            t.Email,
            t.OfficialMobileNumber,
            t.Location,
            CASE
                WHEN t.ClassOfService = '0' OR t.ClassOfService = '' THEN NULL
                ELSE t.ClassOfService
            END,
            org.Id as OrganizationId,
            off.Id as OfficeId,
            sub.Id as SubOfficeId,
            1 as IsActive,
            GETUTCDATE() as CreatedDate
        FROM #TempEbillUsers t
        LEFT JOIN Organizations org ON org.Code = t.OrgCode
        LEFT JOIN Offices off ON off.Code = t.OfficeCode AND off.OrganizationId = org.Id
        LEFT JOIN SubOffices sub ON sub.Code = t.SubOfficeCode AND sub.OfficeId = off.Id
        WHERE t.ProcessingStatus = 'Pending'
          AND NOT EXISTS (
            SELECT 1 FROM EbillUsers e
            WHERE e.IndexNumber = t.IndexNumber
          );

        SET @InsertedCount = @InsertedCount + @@ROWCOUNT;

        -- Mark processed records
        UPDATE TOP (@BatchSize) #TempEbillUsers
        SET ProcessingStatus = 'Processed'
        WHERE ProcessingStatus = 'Pending';

        COMMIT TRANSACTION;

        SET @CurrentBatch = @CurrentBatch + 1;
        PRINT 'Processed batch ' + CAST(@CurrentBatch AS VARCHAR(10)) + ' - Total imported: ' + CAST(@InsertedCount AS VARCHAR(10));

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        SET @ErrorCount = @ErrorCount + 1;

        -- Mark failed records
        UPDATE TOP (@BatchSize) #TempEbillUsers
        SET ProcessingStatus = 'Error: ' + ERROR_MESSAGE()
        WHERE ProcessingStatus = 'Pending';

        PRINT 'Error in batch ' + CAST(@CurrentBatch AS VARCHAR(10)) + ': ' + ERROR_MESSAGE();
    END CATCH;
END

-- Final report
PRINT '';
PRINT '================================================================';
PRINT 'IMPORT SUMMARY';
PRINT '================================================================';
PRINT 'Total records in CSV: ' + CAST((SELECT COUNT(*) FROM #TempEbillUsers) AS VARCHAR(10));
PRINT 'Valid records processed: ' + CAST((SELECT COUNT(*) FROM #TempEbillUsers WHERE ProcessingStatus = 'Processed') AS VARCHAR(10));
PRINT 'Invalid records skipped: ' + CAST((SELECT COUNT(*) FROM #TempEbillUsers WHERE ProcessingStatus = 'Invalid') AS VARCHAR(10));
PRINT 'Records with errors: ' + CAST((SELECT COUNT(*) FROM #TempEbillUsers WHERE ProcessingStatus LIKE 'Error:%') AS VARCHAR(10));
PRINT 'Successfully imported to EbillUsers: ' + CAST(@InsertedCount AS VARCHAR(10));

-- Show unmapped organizations
PRINT '';
PRINT 'Organizations that need to be added:';
SELECT DISTINCT t.OrgCode, COUNT(*) as RecordCount
FROM #TempEbillUsers t
LEFT JOIN Organizations org ON org.Code = t.OrgCode
WHERE org.Id IS NULL AND t.OrgCode IS NOT NULL AND t.OrgCode != ''
  AND t.ProcessingStatus != 'Invalid'
GROUP BY t.OrgCode
ORDER BY RecordCount DESC;

-- Show unmapped offices
PRINT '';
PRINT 'Offices that need to be added:';
SELECT DISTINCT t.OrgCode, t.OfficeCode, COUNT(*) as RecordCount
FROM #TempEbillUsers t
LEFT JOIN Organizations org ON org.Code = t.OrgCode
LEFT JOIN Offices off ON off.Code = t.OfficeCode AND off.OrganizationId = org.Id
WHERE off.Id IS NULL AND t.OfficeCode IS NOT NULL AND t.OfficeCode != ''
  AND org.Id IS NOT NULL
  AND t.ProcessingStatus != 'Invalid'
GROUP BY t.OrgCode, t.OfficeCode
ORDER BY t.OrgCode, RecordCount DESC;

-- Clean up
DROP TABLE #TempEbillUsers;

PRINT '';
PRINT 'Import process completed.';
GO
"@

Write-Host ""
Write-Host "Processed CSV data:" -ForegroundColor Yellow
Write-Host "- Valid records to import: $validCount" -ForegroundColor Green
Write-Host "- Invalid records skipped: $skipCount" -ForegroundColor Yellow
Write-Host ""

# Save SQL script to file
$scriptPath = Join-Path (Split-Path $CsvPath -Parent) "ebillusers_import.sql"
$sqlScript | Out-File -FilePath $scriptPath -Encoding UTF8

Write-Host "SQL script saved to: $scriptPath" -ForegroundColor Cyan
Write-Host ""

# Execute the SQL script
Write-Host "Executing SQL import script..." -ForegroundColor Cyan

try {
    sqlcmd -S $ServerName -d $DatabaseName -E -i $scriptPath -o import_log.txt

    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Import completed successfully!" -ForegroundColor Green
        Write-Host ""
        Write-Host "Check import_log.txt for detailed results" -ForegroundColor Yellow

        # Display the log
        Get-Content import_log.txt | Select-Object -Last 30
    } else {
        Write-Host "❌ Error during import" -ForegroundColor Red
        Get-Content import_log.txt | Select-Object -Last 20
    }
} catch {
    Write-Host "❌ Error executing SQL script: $_" -ForegroundColor Red
}

Write-Host ""
Write-Host "Press any key to continue..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")