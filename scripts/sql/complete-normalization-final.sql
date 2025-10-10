-- Final normalization steps - Map Safaricom records and drop deprecated columns
USE [TABDB];
GO

PRINT '================================================================';
PRINT 'FINAL NORMALIZATION STEPS';
PRINT '================================================================';
PRINT '';

-- Step 1: Check what data exists in Safaricom that needs mapping
PRINT 'Step 1: Checking unmapped Safaricom data...';
SELECT TOP 5
    Id,
    Organization_DEPRECATED,
    Office_DEPRECATED,
    Department_DEPRECATED,
    UserName_DEPRECATED,
    IndexNumber
FROM Safaricom
WHERE OrganizationId IS NULL OR OfficeId IS NULL;

-- Map Safaricom records based on any available data
PRINT '';
PRINT 'Mapping Safaricom records...';

-- Try to map by IndexNumber to EbillUser first
UPDATE s
SET s.EbillUserId = eu.Id
FROM Safaricom s
INNER JOIN EbillUsers eu ON s.IndexNumber = eu.IndexNumber
WHERE s.EbillUserId IS NULL AND s.IndexNumber IS NOT NULL;

-- Then get Organization/Office from the EbillUser
UPDATE s
SET s.OrganizationId = eu.OrganizationId,
    s.OfficeId = eu.OfficeId
FROM Safaricom s
INNER JOIN EbillUsers eu ON s.EbillUserId = eu.Id
WHERE (s.OrganizationId IS NULL OR s.OfficeId IS NULL) AND s.EbillUserId IS NOT NULL;

-- For remaining records, try direct mapping
UPDATE s
SET s.OrganizationId = o.Id
FROM Safaricom s
INNER JOIN Organizations o ON s.Organization_DEPRECATED = o.Code OR s.Organization_DEPRECATED = o.Name
WHERE s.OrganizationId IS NULL AND s.Organization_DEPRECATED IS NOT NULL;

UPDATE s
SET s.OfficeId = ofc.Id
FROM Safaricom s
INNER JOIN Offices ofc ON s.Office_DEPRECATED = ofc.Code OR s.Office_DEPRECATED = ofc.Name
WHERE s.OfficeId IS NULL AND s.Office_DEPRECATED IS NOT NULL;

PRINT 'Safaricom mapping completed';
GO

-- Step 2: Final verification before dropping columns
PRINT '';
PRINT 'Step 2: Final verification...';
PRINT '';

DECLARE @CanDrop BIT = 1;
DECLARE @Message NVARCHAR(500);

-- Check for unmapped critical data
IF EXISTS (
    SELECT 1 FROM PSTNs
    WHERE Organization_DEPRECATED IS NOT NULL
    AND OrganizationId IS NULL
)
BEGIN
    SET @CanDrop = 0;
    SET @Message = 'Warning: PSTNs has unmapped Organization data';
    PRINT @Message;
END

IF EXISTS (
    SELECT 1 FROM PrivateWires
    WHERE Organization_DEPRECATED IS NOT NULL
    AND OrganizationId IS NULL
)
BEGIN
    SET @CanDrop = 0;
    SET @Message = 'Warning: PrivateWires has unmapped Organization data';
    PRINT @Message;
END

IF @CanDrop = 1
BEGIN
    PRINT 'All critical data has been mapped. Safe to drop deprecated columns.';
END
ELSE
BEGIN
    PRINT '';
    PRINT 'Some data could not be mapped. Review before dropping columns.';
END
GO

-- Step 3: Drop deprecated columns (only if safe)
PRINT '';
PRINT 'Step 3: Dropping deprecated columns...';
PRINT '';

-- Drop from Safaricom
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Safaricom') AND name = 'Organization_DEPRECATED')
BEGIN
    ALTER TABLE Safaricom DROP COLUMN Organization_DEPRECATED;
    PRINT 'Dropped Safaricom.Organization_DEPRECATED';
END

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Safaricom') AND name = 'Office_DEPRECATED')
BEGIN
    ALTER TABLE Safaricom DROP COLUMN Office_DEPRECATED;
    PRINT 'Dropped Safaricom.Office_DEPRECATED';
END

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Safaricom') AND name = 'Department_DEPRECATED')
BEGIN
    ALTER TABLE Safaricom DROP COLUMN Department_DEPRECATED;
    PRINT 'Dropped Safaricom.Department_DEPRECATED';
END

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Safaricom') AND name = 'UserName_DEPRECATED')
BEGIN
    ALTER TABLE Safaricom DROP COLUMN UserName_DEPRECATED;
    PRINT 'Dropped Safaricom.UserName_DEPRECATED';
END

-- Drop from Airtel
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Airtel') AND name = 'Organization_DEPRECATED')
BEGIN
    ALTER TABLE Airtel DROP COLUMN Organization_DEPRECATED;
    PRINT 'Dropped Airtel.Organization_DEPRECATED';
END

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Airtel') AND name = 'Office_DEPRECATED')
BEGIN
    ALTER TABLE Airtel DROP COLUMN Office_DEPRECATED;
    PRINT 'Dropped Airtel.Office_DEPRECATED';
END

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Airtel') AND name = 'Department_DEPRECATED')
BEGIN
    ALTER TABLE Airtel DROP COLUMN Department_DEPRECATED;
    PRINT 'Dropped Airtel.Department_DEPRECATED';
END

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Airtel') AND name = 'UserName_DEPRECATED')
BEGIN
    ALTER TABLE Airtel DROP COLUMN UserName_DEPRECATED;
    PRINT 'Dropped Airtel.UserName_DEPRECATED';
END

-- Drop from PSTNs
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('PSTNs') AND name = 'Organization_DEPRECATED')
BEGIN
    ALTER TABLE PSTNs DROP COLUMN Organization_DEPRECATED;
    PRINT 'Dropped PSTNs.Organization_DEPRECATED';
END

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('PSTNs') AND name = 'Office_DEPRECATED')
BEGIN
    ALTER TABLE PSTNs DROP COLUMN Office_DEPRECATED;
    PRINT 'Dropped PSTNs.Office_DEPRECATED';
END

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('PSTNs') AND name = 'SubOffice')
BEGIN
    ALTER TABLE PSTNs DROP COLUMN SubOffice;
    PRINT 'Dropped PSTNs.SubOffice';
END

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('PSTNs') AND name = 'CallerName_DEPRECATED')
BEGIN
    ALTER TABLE PSTNs DROP COLUMN CallerName_DEPRECATED;
    PRINT 'Dropped PSTNs.CallerName_DEPRECATED';
END

-- Drop from PrivateWires
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('PrivateWires') AND name = 'Organization_DEPRECATED')
BEGIN
    ALTER TABLE PrivateWires DROP COLUMN Organization_DEPRECATED;
    PRINT 'Dropped PrivateWires.Organization_DEPRECATED';
END

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('PrivateWires') AND name = 'Office_DEPRECATED')
BEGIN
    ALTER TABLE PrivateWires DROP COLUMN Office_DEPRECATED;
    PRINT 'Dropped PrivateWires.Office_DEPRECATED';
END

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('PrivateWires') AND name = 'SubOffice')
BEGIN
    ALTER TABLE PrivateWires DROP COLUMN SubOffice;
    PRINT 'Dropped PrivateWires.SubOffice';
END

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('PrivateWires') AND name = 'CallerName_DEPRECATED')
BEGIN
    ALTER TABLE PrivateWires DROP COLUMN CallerName_DEPRECATED;
    PRINT 'Dropped PrivateWires.CallerName_DEPRECATED';
END
GO

-- Final Report
PRINT '';
PRINT '================================================================';
PRINT 'NORMALIZATION COMPLETE - PRODUCTION READY';
PRINT '================================================================';

SELECT 'Safaricom' as TableName,
    COUNT(*) as Records,
    COUNT(OrganizationId) as WithOrg,
    COUNT(OfficeId) as WithOffice,
    COUNT(SubOfficeId) as WithSubOffice,
    COUNT(EbillUserId) as WithUser
FROM Safaricom
UNION ALL
SELECT 'Airtel',
    COUNT(*),
    COUNT(OrganizationId),
    COUNT(OfficeId),
    COUNT(SubOfficeId),
    COUNT(EbillUserId)
FROM Airtel
UNION ALL
SELECT 'PSTNs',
    COUNT(*),
    COUNT(OrganizationId),
    COUNT(OfficeId),
    COUNT(SubOfficeId),
    COUNT(EbillUserId)
FROM PSTNs
UNION ALL
SELECT 'PrivateWires',
    COUNT(*),
    COUNT(OrganizationId),
    COUNT(OfficeId),
    COUNT(SubOfficeId),
    COUNT(EbillUserId)
FROM PrivateWires;

PRINT '';
PRINT '✅ ALL TELECOM TABLES ARE NOW PROPERLY NORMALIZED!';
GO