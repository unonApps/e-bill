-- Fast Bulk Import for EbillUsers CSV
-- This uses SQL Server's BULK INSERT for speed

USE [TABDB]
GO

-- Create staging table for raw CSV data
IF OBJECT_ID('dbo.EbillUsers_Staging') IS NOT NULL
    DROP TABLE dbo.EbillUsers_Staging;

CREATE TABLE dbo.EbillUsers_Staging (
    OfficialMobileNumber NVARCHAR(20),
    FirstName NVARCHAR(100),
    LastName NVARCHAR(100),
    IndexNumber NVARCHAR(50),
    Location NVARCHAR(200),
    Org NVARCHAR(50),
    Office NVARCHAR(50),
    SubOffice NVARCHAR(50),
    ClassOfService NVARCHAR(100)
);

-- Bulk insert from CSV
PRINT 'Starting bulk insert from CSV...';
PRINT 'Time: ' + CONVERT(VARCHAR, GETDATE(), 120);

BULK INSERT dbo.EbillUsers_Staging
FROM 'C:\Users\dxmic\Downloads\ebill user.csv'
WITH (
    FIRSTROW = 2,  -- Skip header row
    FIELDTERMINATOR = ',',
    ROWTERMINATOR = '\n',
    TABLOCK,
    BATCHSIZE = 10000,
    MAXERRORS = 1000
);

PRINT 'Bulk insert completed.';
PRINT 'Rows inserted: ' + CAST(@@ROWCOUNT AS VARCHAR(10));
PRINT '';

-- Clean up the data
PRINT 'Cleaning data...';

-- Remove extra spaces and special characters
UPDATE dbo.EbillUsers_Staging
SET
    FirstName = LTRIM(RTRIM(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(FirstName, CHAR(9), ''), CHAR(10), ''), CHAR(13), ''), '"', ''), '﻿', ''))),
    LastName = LTRIM(RTRIM(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(LastName, CHAR(9), ''), CHAR(10), ''), CHAR(13), ''), '"', ''), '﻿', ''))),
    IndexNumber = LTRIM(RTRIM(IndexNumber)),
    Location = LTRIM(RTRIM(Location)),
    Org = LTRIM(RTRIM(Org)),
    Office = LTRIM(RTRIM(Office)),
    SubOffice = LTRIM(RTRIM(SubOffice)),
    OfficialMobileNumber = LTRIM(RTRIM(OfficialMobileNumber));

-- Remove invalid records
DELETE FROM dbo.EbillUsers_Staging
WHERE
    FirstName LIKE '%Service%'
    OR FirstName LIKE '%-Reception%'
    OR FirstName LIKE '%-Library%'
    OR FirstName LIKE '%-Fax%'
    OR FirstName LIKE '%-Office%'
    OR FirstName LIKE '%Consultant(%'
    OR FirstName LIKE '%-Intern%'
    OR FirstName LIKE '%CONF%'
    OR FirstName LIKE '$%'
    OR FirstName LIKE '&%'
    OR FirstName LIKE '-%'
    OR IndexNumber = '0'
    OR IndexNumber IS NULL
    OR LEN(LTRIM(RTRIM(FirstName))) < 2
    OR LEN(LTRIM(RTRIM(LastName))) < 2;

PRINT 'Invalid records removed: ' + CAST(@@ROWCOUNT AS VARCHAR(10));
PRINT '';

-- Create email addresses
UPDATE dbo.EbillUsers_Staging
SET FirstName = REPLACE(FirstName, ' ', ''),
    LastName = REPLACE(LastName, ' ', '');

-- Add email column and generate emails
ALTER TABLE dbo.EbillUsers_Staging ADD Email NVARCHAR(256);

UPDATE dbo.EbillUsers_Staging
SET Email = LOWER(FirstName) + '.' + LOWER(LastName) +
    CASE
        WHEN Org = 'WHO' THEN '@who.int'
        WHEN Org = 'FAO' THEN '@fao.org'
        WHEN Org = 'WFP' THEN '@wfp.org'
        WHEN Org = 'UNDP' THEN '@undp.org'
        WHEN Org = 'UNICEF' THEN '@unicef.org'
        ELSE '@un.org'
    END;

-- Clean up email addresses
UPDATE dbo.EbillUsers_Staging
SET Email = REPLACE(REPLACE(REPLACE(Email, '..', '.'), ',.', '.'), '.,', '.');

PRINT 'Data cleaning completed.';
PRINT '';

-- Show statistics before import
PRINT 'Pre-import Statistics:';
PRINT '=====================';
SELECT
    COUNT(*) as TotalStagingRecords,
    COUNT(DISTINCT IndexNumber) as UniqueIndexNumbers,
    COUNT(DISTINCT Org) as UniqueOrganizations,
    COUNT(DISTINCT Office) as UniqueOffices
FROM dbo.EbillUsers_Staging;

-- Import to EbillUsers table with code mapping
PRINT '';
PRINT 'Starting import to EbillUsers table...';
PRINT 'Time: ' + CONVERT(VARCHAR, GETDATE(), 120);

DECLARE @ImportedCount INT = 0;
DECLARE @SkippedCount INT = 0;

-- Use MERGE for efficient upsert
MERGE EbillUsers AS target
USING (
    SELECT DISTINCT
        s.FirstName,
        s.LastName,
        s.IndexNumber,
        s.Email,
        s.OfficialMobileNumber,
        s.Location,
        CASE WHEN s.ClassOfService = '0' OR s.ClassOfService = '' THEN NULL ELSE s.ClassOfService END as ClassOfService,
        org.Id as OrganizationId,
        ofc.Id as OfficeId,
        sub.Id as SubOfficeId
    FROM dbo.EbillUsers_Staging s
    LEFT JOIN Organizations org ON org.Code = s.Org
    LEFT JOIN Offices ofc ON ofc.Code = s.Office AND ofc.OrganizationId = org.Id
    LEFT JOIN SubOffices sub ON sub.Code = s.SubOffice AND sub.OfficeId = ofc.Id
    WHERE s.IndexNumber IS NOT NULL
      AND s.IndexNumber != ''
      AND s.IndexNumber != '0'
) AS source ON target.IndexNumber = source.IndexNumber
WHEN NOT MATCHED THEN
    INSERT (FirstName, LastName, IndexNumber, Email, OfficialMobileNumber, Location,
            ClassOfService, OrganizationId, OfficeId, SubOfficeId, IsActive, CreatedDate)
    VALUES (source.FirstName, source.LastName, source.IndexNumber, source.Email,
            source.OfficialMobileNumber, source.Location, source.ClassOfService,
            source.OrganizationId, source.OfficeId, source.SubOfficeId, 1, GETUTCDATE());

SET @ImportedCount = @@ROWCOUNT;

PRINT 'Import completed.';
PRINT 'Records imported: ' + CAST(@ImportedCount AS VARCHAR(10));
PRINT '';

-- Show unmapped organizations
PRINT 'Unmapped Organizations (need to be added):';
PRINT '==========================================';
SELECT DISTINCT s.Org, COUNT(*) as RecordCount
FROM dbo.EbillUsers_Staging s
LEFT JOIN Organizations org ON org.Code = s.Org
WHERE org.Id IS NULL
  AND s.Org IS NOT NULL
  AND s.Org != ''
GROUP BY s.Org
ORDER BY RecordCount DESC;

-- Show unmapped offices
PRINT '';
PRINT 'Unmapped Offices (need to be added):';
PRINT '====================================';
SELECT DISTINCT s.Org, s.Office, COUNT(*) as RecordCount
FROM dbo.EbillUsers_Staging s
LEFT JOIN Organizations org ON org.Code = s.Org
LEFT JOIN Offices ofc ON ofc.Code = s.Office AND ofc.OrganizationId = org.Id
WHERE ofc.Id IS NULL
  AND s.Office IS NOT NULL
  AND s.Office != ''
  AND org.Id IS NOT NULL
GROUP BY s.Org, s.Office
ORDER BY s.Org, RecordCount DESC;

-- Final statistics
PRINT '';
PRINT 'Final Import Summary:';
PRINT '====================';
SELECT
    'Total EbillUsers' as Metric, COUNT(*) as Count FROM EbillUsers
UNION ALL
SELECT 'New Records Added', @ImportedCount
UNION ALL
SELECT 'Records in Staging', COUNT(*) FROM dbo.EbillUsers_Staging
UNION ALL
SELECT 'Active Users', COUNT(*) FROM EbillUsers WHERE IsActive = 1
UNION ALL
SELECT 'Users with Organizations', COUNT(*) FROM EbillUsers WHERE OrganizationId IS NOT NULL
UNION ALL
SELECT 'Users with Offices', COUNT(*) FROM EbillUsers WHERE OfficeId IS NOT NULL
UNION ALL
SELECT 'Users with Locations', COUNT(*) FROM EbillUsers WHERE Location IS NOT NULL;

PRINT '';
PRINT 'Import process completed successfully!';
PRINT 'Time: ' + CONVERT(VARCHAR, GETDATE(), 120);
GO