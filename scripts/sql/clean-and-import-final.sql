-- Clean duplicates and perform final import
USE [TABDB]
GO

-- Check what's already in EbillUsers
PRINT 'Current EbillUsers:';
SELECT COUNT(*) as ExistingUsers FROM EbillUsers;
SELECT TOP 5 IndexNumber, FirstName, LastName FROM EbillUsers;

-- Check for duplicate IndexNumbers in staging
PRINT '';
PRINT 'Checking duplicate IndexNumbers in staging...';
WITH DuplicateIndexNumbers AS (
    SELECT IndexNumber, COUNT(*) as DupeCount
    FROM dbo.EbillUsers_Staging
    WHERE IndexNumber IS NOT NULL AND IndexNumber != '' AND IndexNumber != '0'
    GROUP BY IndexNumber
    HAVING COUNT(*) > 1
)
SELECT COUNT(*) as TotalDuplicateIndexNumbers FROM DuplicateIndexNumbers;

-- Show sample of duplicates
PRINT 'Sample duplicate IndexNumbers:';
SELECT TOP 10 IndexNumber, COUNT(*) as Count
FROM dbo.EbillUsers_Staging
WHERE IndexNumber IS NOT NULL AND IndexNumber != '' AND IndexNumber != '0'
GROUP BY IndexNumber
HAVING COUNT(*) > 1
ORDER BY COUNT(*) DESC;

-- Remove duplicates, keeping only the first occurrence of each IndexNumber
PRINT '';
PRINT 'Removing duplicate IndexNumbers (keeping first occurrence)...';
WITH CTE AS (
    SELECT *,
           ROW_NUMBER() OVER (PARTITION BY IndexNumber ORDER BY OfficialMobileNumber, FirstName) as RowNum
    FROM dbo.EbillUsers_Staging
    WHERE IndexNumber IS NOT NULL AND IndexNumber != '' AND IndexNumber != '0'
)
DELETE FROM CTE WHERE RowNum > 1;

PRINT 'Removed ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' duplicate records.';

-- Check how many unique records we have now
PRINT '';
PRINT 'Unique records ready for import:';
SELECT COUNT(DISTINCT IndexNumber) as UniqueRecords
FROM dbo.EbillUsers_Staging
WHERE IndexNumber IS NOT NULL AND IndexNumber != '' AND IndexNumber != '0';

-- Remove any IndexNumbers that already exist in EbillUsers
DELETE FROM dbo.EbillUsers_Staging
WHERE IndexNumber IN (SELECT IndexNumber FROM EbillUsers);

PRINT 'Removed ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' records that already exist in EbillUsers.';

-- Now import all remaining records
PRINT '';
PRINT 'Starting final import...';

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
  AND s.IndexNumber != '0';

SET @ImportedCount = @@ROWCOUNT;

PRINT '';
PRINT '============================================';
PRINT 'IMPORT COMPLETED SUCCESSFULLY!';
PRINT '============================================';
PRINT 'Records imported: ' + CAST(@ImportedCount AS VARCHAR(10));
PRINT '';

-- Final statistics
PRINT 'Final Database Statistics:';
PRINT '==========================';
SELECT
    'Total EbillUsers' as Metric, COUNT(*) as Count FROM EbillUsers
UNION ALL
SELECT 'Users with Valid IndexNumbers', COUNT(*)
FROM EbillUsers
WHERE IndexNumber IS NOT NULL AND IndexNumber != '' AND IndexNumber != '0'
UNION ALL
SELECT 'Users with Organizations', COUNT(*) FROM EbillUsers WHERE OrganizationId IS NOT NULL
UNION ALL
SELECT 'Users with Offices', COUNT(*) FROM EbillUsers WHERE OfficeId IS NOT NULL
UNION ALL
SELECT 'Users with Locations', COUNT(*) FROM EbillUsers WHERE Location IS NOT NULL AND Location != ''
UNION ALL
SELECT 'Active Users', COUNT(*) FROM EbillUsers WHERE IsActive = 1;

-- Show organizations with most users
PRINT '';
PRINT 'Top 20 Organizations by User Count:';
SELECT TOP 20
    ISNULL(o.Code, 'NO-ORG') as OrgCode,
    ISNULL(o.Name, 'No Organization') as Organization,
    COUNT(*) as UserCount
FROM EbillUsers e
LEFT JOIN Organizations o ON e.OrganizationId = o.Id
GROUP BY o.Code, o.Name
ORDER BY COUNT(*) DESC;

-- Show sample of newly imported records
PRINT '';
PRINT 'Sample of imported users (showing variety):';
SELECT TOP 30
    e.IndexNumber,
    e.FirstName,
    e.LastName,
    e.Location,
    e.OfficialMobileNumber,
    ISNULL(o.Code, 'N/A') as Org,
    ISNULL(ofc.Code, 'N/A') as Office
FROM EbillUsers e
LEFT JOIN Organizations o ON e.OrganizationId = o.Id
LEFT JOIN Offices ofc ON e.OfficeId = ofc.Id
WHERE e.Id > 7  -- Skip the test records
ORDER BY e.Id DESC;

-- Show locations distribution
PRINT '';
PRINT 'Top 10 Locations:';
SELECT TOP 10
    Location,
    COUNT(*) as UserCount
FROM EbillUsers
WHERE Location IS NOT NULL AND Location != ''
GROUP BY Location
ORDER BY COUNT(*) DESC;

-- Clean up staging table
DROP TABLE dbo.EbillUsers_Staging;
PRINT '';
PRINT 'Staging table dropped.';
PRINT 'Import process fully completed!';
GO