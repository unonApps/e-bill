-- Fix NULL emails and complete the import
USE [TABDB]
GO

-- Check records with NULL emails
PRINT 'Checking records with NULL emails...';
SELECT COUNT(*) as NullEmailCount
FROM dbo.EbillUsers_Staging
WHERE Email IS NULL OR Email = '';

-- Generate emails for records that don't have them
UPDATE dbo.EbillUsers_Staging
SET Email = LOWER(REPLACE(FirstName, ' ', '')) + '.' + LOWER(REPLACE(LastName, ' ', '')) +
    CASE
        WHEN Org = 'WHO' THEN '@who.int'
        WHEN Org = 'FAO' THEN '@fao.org'
        WHEN Org = 'WFP' THEN '@wfp.org'
        WHEN Org = 'UNDP' THEN '@undp.org'
        WHEN Org = 'UNICEF' THEN '@unicef.org'
        WHEN Org = 'IFAD' THEN '@ifad.org'
        WHEN Org = 'ICAO' THEN '@icao.int'
        WHEN Org LIKE 'UN%' THEN '@un.org'
        WHEN Org = 'CON' THEN '@contractor.un.org'
        ELSE '@un.org'
    END
WHERE Email IS NULL OR Email = '';

PRINT 'Generated emails for ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' records.';

-- Clean up any remaining email issues
UPDATE dbo.EbillUsers_Staging
SET Email = REPLACE(REPLACE(REPLACE(REPLACE(Email, '..', '.'), ',.', '.'), '.,', '.'), 'null.', 'user.')
WHERE Email LIKE '%..%' OR Email LIKE '%null%';

-- For records that still have email issues, generate a unique email
UPDATE dbo.EbillUsers_Staging
SET Email = 'user.' + IndexNumber + '@un.org'
WHERE (Email IS NULL OR Email = '' OR Email = '@un.org' OR Email LIKE '%null%')
  AND IndexNumber IS NOT NULL AND IndexNumber != '';

-- Remove records that still don't have valid data
DELETE FROM dbo.EbillUsers_Staging
WHERE Email IS NULL
   OR Email = ''
   OR FirstName IS NULL
   OR LastName IS NULL
   OR IndexNumber IS NULL
   OR IndexNumber = ''
   OR IndexNumber = '0';

PRINT 'Removed invalid records: ' + CAST(@@ROWCOUNT AS VARCHAR(10));

-- Now perform the import
PRINT '';
PRINT 'Starting final import...';

DECLARE @ImportedCount INT = 0;
DECLARE @SkippedCount INT = 0;

-- Count existing records that would be skipped
SELECT @SkippedCount = COUNT(*)
FROM dbo.EbillUsers_Staging s
WHERE EXISTS (
    SELECT 1 FROM EbillUsers e
    WHERE e.IndexNumber = s.IndexNumber
);

-- Insert new records only
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
WHERE NOT EXISTS (
    SELECT 1 FROM EbillUsers e
    WHERE e.IndexNumber = s.IndexNumber
);

SET @ImportedCount = @@ROWCOUNT;

PRINT 'Import completed!';
PRINT '================';
PRINT 'Records imported: ' + CAST(@ImportedCount AS VARCHAR(10));
PRINT 'Duplicates skipped: ' + CAST(@SkippedCount AS VARCHAR(10));
PRINT '';

-- Final summary
PRINT 'Database Summary:';
PRINT '=================';
SELECT
    'Total EbillUsers' as Metric, COUNT(*) as Count FROM EbillUsers
UNION ALL
SELECT 'New Records Added Today', COUNT(*)
FROM EbillUsers
WHERE CAST(CreatedDate as DATE) = CAST(GETUTCDATE() as DATE)
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
PRINT 'Users by Organization (Top 10):';
SELECT TOP 10
    ISNULL(o.Name, 'No Organization') as Organization,
    COUNT(*) as UserCount
FROM EbillUsers e
LEFT JOIN Organizations o ON e.OrganizationId = o.Id
GROUP BY o.Name
ORDER BY COUNT(*) DESC;

-- Show sample of imported records
PRINT '';
PRINT 'Sample of newly imported records:';
SELECT TOP 10
    FirstName,
    LastName,
    IndexNumber,
    OfficialMobileNumber,
    Location,
    Email
FROM EbillUsers
WHERE CAST(CreatedDate as DATE) = CAST(GETUTCDATE() as DATE)
ORDER BY Id DESC;

PRINT '';
PRINT 'Import process fully completed!';
GO