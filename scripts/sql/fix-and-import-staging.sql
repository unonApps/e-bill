-- Fix staging data and import to EbillUsers
USE [TABDB]
GO

-- Check what's in staging
PRINT 'Checking staging data...';
SELECT TOP 10 * FROM dbo.EbillUsers_Staging WHERE LastName IS NULL OR LastName = '';

-- Fix NULL or empty names
UPDATE dbo.EbillUsers_Staging
SET LastName = FirstName
WHERE (LastName IS NULL OR LastName = '') AND FirstName IS NOT NULL;

UPDATE dbo.EbillUsers_Staging
SET FirstName = 'Unknown'
WHERE FirstName IS NULL OR FirstName = '';

UPDATE dbo.EbillUsers_Staging
SET LastName = 'User'
WHERE LastName IS NULL OR LastName = '';

-- Remove records with invalid names
DELETE FROM dbo.EbillUsers_Staging
WHERE LEN(LTRIM(RTRIM(ISNULL(FirstName, '')))) < 2
   OR LEN(LTRIM(RTRIM(ISNULL(LastName, '')))) < 2;

PRINT 'Fixed NULL names. Removed: ' + CAST(@@ROWCOUNT AS VARCHAR(10));

-- Now import valid records
PRINT 'Starting import...';

DECLARE @ImportedCount INT = 0;

-- Insert records that don't exist
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
SELECT DISTINCT
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
  AND s.FirstName IS NOT NULL
  AND s.LastName IS NOT NULL
  AND NOT EXISTS (
    SELECT 1 FROM EbillUsers e
    WHERE e.IndexNumber = s.IndexNumber
  );

SET @ImportedCount = @@ROWCOUNT;
PRINT 'Imported ' + CAST(@ImportedCount AS VARCHAR(10)) + ' new records.';

-- Show results
PRINT '';
PRINT 'Import Summary:';
PRINT '===============';
SELECT
    'Total EbillUsers' as Metric, COUNT(*) as Count FROM EbillUsers
UNION ALL
SELECT 'New Records Added', @ImportedCount
UNION ALL
SELECT 'Valid Records in Staging', COUNT(*)
FROM dbo.EbillUsers_Staging
WHERE IndexNumber IS NOT NULL
  AND IndexNumber != ''
  AND IndexNumber != '0'
  AND FirstName IS NOT NULL
  AND LastName IS NOT NULL
UNION ALL
SELECT 'Users with Organizations', COUNT(*) FROM EbillUsers WHERE OrganizationId IS NOT NULL
UNION ALL
SELECT 'Users with Locations', COUNT(*) FROM EbillUsers WHERE Location IS NOT NULL AND Location != '';

-- Show sample imported records
PRINT '';
PRINT 'Sample imported records:';
SELECT TOP 10
    FirstName,
    LastName,
    IndexNumber,
    Location,
    OfficialMobileNumber,
    Email
FROM EbillUsers
ORDER BY CreatedDate DESC;

GO