-- Check current state and recreate staging if needed
USE [TABDB]
GO

-- Check if EbillUsers table is truly empty
PRINT 'Checking EbillUsers table...';
SELECT COUNT(*) as TotalRecords FROM EbillUsers;

-- Check if there are any constraints causing issues
PRINT '';
PRINT 'Checking unique constraints on EbillUsers:';
SELECT
    i.name AS IndexName,
    c.name AS ColumnName,
    i.is_unique AS IsUnique
FROM sys.indexes i
INNER JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
INNER JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
WHERE i.object_id = OBJECT_ID('EbillUsers')
  AND i.is_unique = 1;

-- Check if staging table was dropped
IF OBJECT_ID('dbo.EbillUsers_Staging') IS NULL
BEGIN
    PRINT '';
    PRINT 'Staging table was dropped. Need to recreate and reload from CSV.';
    PRINT 'Run the fast-bulk-import.sql script again to reload the CSV data.';
END
ELSE
BEGIN
    PRINT '';
    PRINT 'Staging table exists with records:';
    SELECT COUNT(*) as StagingRecords FROM dbo.EbillUsers_Staging;

    -- Check for duplicate emails in staging
    PRINT '';
    PRINT 'Checking for duplicate emails in staging:';
    SELECT TOP 10 Email, COUNT(*) as Count
    FROM dbo.EbillUsers_Staging
    GROUP BY Email
    HAVING COUNT(*) > 1
    ORDER BY COUNT(*) DESC;

    -- Make ALL emails unique using IndexNumber
    PRINT '';
    PRINT 'Making all emails unique by using IndexNumber...';

    UPDATE dbo.EbillUsers_Staging
    SET Email = LOWER(REPLACE(FirstName, ' ', '')) + '.' +
                LOWER(REPLACE(LastName, ' ', '')) + '.' +
                IndexNumber +
                CASE
                    WHEN Org = 'WHO' THEN '@who.int'
                    WHEN Org = 'FAO' THEN '@fao.org'
                    WHEN Org = 'WFP' THEN '@wfp.org'
                    WHEN Org = 'UNDP' THEN '@undp.org'
                    WHEN Org = 'UNICEF' THEN '@unicef.org'
                    WHEN Org = 'IFAD' THEN '@ifad.org'
                    ELSE '@un.org'
                END
    WHERE IndexNumber IS NOT NULL AND IndexNumber != '' AND IndexNumber != '0';

    PRINT 'Updated ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' email addresses to be unique.';

    -- Verify no duplicates remain
    PRINT '';
    PRINT 'Verifying uniqueness:';
    SELECT
        COUNT(*) as TotalRecords,
        COUNT(DISTINCT IndexNumber) as UniqueIndexNumbers,
        COUNT(DISTINCT Email) as UniqueEmails
    FROM dbo.EbillUsers_Staging
    WHERE IndexNumber IS NOT NULL AND IndexNumber != '' AND IndexNumber != '0';

    -- Now import
    PRINT '';
    PRINT 'Importing unique records...';

    DECLARE @ImportedCount INT = 0;

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
    SELECT
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
    WHERE s.IndexNumber IS NOT NULL
      AND s.IndexNumber != ''
      AND s.IndexNumber != '0'
      AND NOT EXISTS (
        SELECT 1 FROM EbillUsers e
        WHERE e.IndexNumber = s.IndexNumber OR e.Email = s.Email
      );

    SET @ImportedCount = @@ROWCOUNT;

    PRINT '';
    PRINT '============================================';
    PRINT 'IMPORT COMPLETED!';
    PRINT '============================================';
    PRINT 'Records imported: ' + CAST(@ImportedCount AS VARCHAR(10));

    -- Show results
    SELECT
        'Total EbillUsers' as Metric, COUNT(*) as Count FROM EbillUsers
    UNION ALL
        SELECT 'Records Imported', @ImportedCount
    UNION ALL
        SELECT 'Users with Organizations', COUNT(*) FROM EbillUsers WHERE OrganizationId IS NOT NULL
    UNION ALL
        SELECT 'Users with Offices', COUNT(*) FROM EbillUsers WHERE OfficeId IS NOT NULL
    UNION ALL
        SELECT 'Users with Locations', COUNT(*) FROM EbillUsers WHERE Location IS NOT NULL AND Location != '';

    -- Show sample
    PRINT '';
    PRINT 'Sample imported records:';
    SELECT TOP 10
        IndexNumber,
        FirstName + ' ' + LastName as FullName,
        Email,
        Location
    FROM EbillUsers
    ORDER BY Id DESC;
END

GO