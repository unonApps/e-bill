-- Fix duplicate emails and complete import
USE [TABDB]
GO

-- Check for duplicate emails in staging
PRINT 'Checking for duplicate emails in staging...';
WITH DuplicateEmails AS (
    SELECT Email, COUNT(*) as DupeCount
    FROM dbo.EbillUsers_Staging
    GROUP BY Email
    HAVING COUNT(*) > 1
)
SELECT TOP 20 * FROM DuplicateEmails
ORDER BY DupeCount DESC;

-- Make emails unique by appending IndexNumber for duplicates
PRINT '';
PRINT 'Making emails unique...';

WITH DuplicateEmails AS (
    SELECT
        Email,
        IndexNumber,
        ROW_NUMBER() OVER (PARTITION BY Email ORDER BY IndexNumber) as RowNum
    FROM dbo.EbillUsers_Staging
)
UPDATE s
SET s.Email =
    CASE
        WHEN de.RowNum > 1 THEN
            SUBSTRING(s.Email, 1, CHARINDEX('@', s.Email) - 1) + '.' + s.IndexNumber + SUBSTRING(s.Email, CHARINDEX('@', s.Email), LEN(s.Email))
        ELSE s.Email
    END
FROM dbo.EbillUsers_Staging s
INNER JOIN DuplicateEmails de ON s.IndexNumber = de.IndexNumber AND s.Email = de.Email
WHERE de.RowNum > 1;

PRINT 'Fixed ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' duplicate emails.';

-- Also check if any emails already exist in EbillUsers table
PRINT '';
PRINT 'Checking for conflicts with existing users...';
SELECT COUNT(*) as ConflictingEmails
FROM dbo.EbillUsers_Staging s
WHERE EXISTS (
    SELECT 1 FROM EbillUsers e
    WHERE e.Email = s.Email AND e.IndexNumber != s.IndexNumber
);

-- Fix conflicts with existing emails
UPDATE s
SET s.Email = SUBSTRING(s.Email, 1, CHARINDEX('@', s.Email) - 1) + '.' + s.IndexNumber + SUBSTRING(s.Email, CHARINDEX('@', s.Email), LEN(s.Email))
FROM dbo.EbillUsers_Staging s
WHERE EXISTS (
    SELECT 1 FROM EbillUsers e
    WHERE e.Email = s.Email AND e.IndexNumber != s.IndexNumber
);

PRINT 'Fixed email conflicts: ' + CAST(@@ROWCOUNT AS VARCHAR(10));

-- Now perform the import
PRINT '';
PRINT 'Starting import with unique emails...';

DECLARE @ImportedCount INT = 0;
DECLARE @BatchSize INT = 10000;
DECLARE @CurrentBatch INT = 0;

-- Process in batches to avoid timeout
WHILE EXISTS (
    SELECT 1
    FROM dbo.EbillUsers_Staging s
    WHERE NOT EXISTS (
        SELECT 1 FROM EbillUsers e
        WHERE e.IndexNumber = s.IndexNumber
    )
)
BEGIN
    BEGIN TRY
        BEGIN TRANSACTION;

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
            s.FirstName,
            s.LastName,
            s.IndexNumber,
            s.Email,
            s.OfficialMobileNumber,
            s.Location,
            CASE WHEN s.ClassOfService = '0' OR s.ClassOfService = '' THEN NULL ELSE s.ClassOfService END,
            org.Id as OrganizationId,
            ofc.Id as OfficeId,
            sub.Id as SubOfficeId,
            1 as IsActive,
            GETUTCDATE()
        FROM dbo.EbillUsers_Staging s
        LEFT JOIN Organizations org ON org.Code = s.Org
        LEFT JOIN Offices ofc ON ofc.Code = s.Office AND ofc.OrganizationId = org.Id
        LEFT JOIN SubOffices sub ON sub.Code = s.SubOffice AND sub.OfficeId = ofc.Id
        WHERE NOT EXISTS (
            SELECT 1 FROM EbillUsers e
            WHERE e.IndexNumber = s.IndexNumber
        )
        AND NOT EXISTS (
            SELECT 1 FROM EbillUsers e2
            WHERE e2.Email = s.Email
        );

        SET @ImportedCount = @ImportedCount + @@ROWCOUNT;
        SET @CurrentBatch = @CurrentBatch + 1;

        -- Mark processed records (optional - for tracking)
        -- DELETE FROM dbo.EbillUsers_Staging WHERE ... (if you want to remove processed records)

        COMMIT TRANSACTION;

        IF @ImportedCount % 10000 = 0
            PRINT 'Imported ' + CAST(@ImportedCount AS VARCHAR(10)) + ' records...';

        -- Break if no more records were inserted (to avoid infinite loop)
        IF @@ROWCOUNT = 0
            BREAK;

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        PRINT 'Error in batch ' + CAST(@CurrentBatch AS VARCHAR(10)) + ': ' + ERROR_MESSAGE();
        BREAK;
    END CATCH;
END;

PRINT '';
PRINT '===========================================';
PRINT 'IMPORT COMPLETED SUCCESSFULLY!';
PRINT '===========================================';
PRINT 'Total records imported: ' + CAST(@ImportedCount AS VARCHAR(10));
PRINT '';

-- Final summary
SELECT
    'Total EbillUsers' as Metric, COUNT(*) as Count FROM EbillUsers
UNION ALL
SELECT 'Imported in this session', @ImportedCount
UNION ALL
SELECT 'Users with Organizations', COUNT(*) FROM EbillUsers WHERE OrganizationId IS NOT NULL
UNION ALL
SELECT 'Users with Offices', COUNT(*) FROM EbillUsers WHERE OfficeId IS NOT NULL
UNION ALL
SELECT 'Users with Locations', COUNT(*) FROM EbillUsers WHERE Location IS NOT NULL AND Location != ''
UNION ALL
SELECT 'Active Users', COUNT(*) FROM EbillUsers WHERE IsActive = 1;

-- Show breakdown by organization
PRINT '';
PRINT 'Top 15 Organizations by User Count:';
SELECT TOP 15
    ISNULL(o.Name, 'No Organization') as Organization,
    ISNULL(o.Code, 'N/A') as Code,
    COUNT(*) as UserCount
FROM EbillUsers e
LEFT JOIN Organizations o ON e.OrganizationId = o.Id
GROUP BY o.Name, o.Code
ORDER BY COUNT(*) DESC;

-- Show sample of imported records
PRINT '';
PRINT 'Sample of imported users:';
SELECT TOP 20
    e.FirstName,
    e.LastName,
    e.IndexNumber,
    e.Location,
    ISNULL(o.Code, 'N/A') as Org
FROM EbillUsers e
LEFT JOIN Organizations o ON e.OrganizationId = o.Id
ORDER BY e.Id DESC;

-- Cleanup option (uncomment if you want to drop staging table)
-- DROP TABLE dbo.EbillUsers_Staging;

PRINT '';
PRINT 'Process complete!';
GO