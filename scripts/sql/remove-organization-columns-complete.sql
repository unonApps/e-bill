-- =====================================================
-- Complete removal of OrganizationId, OfficeId, SubOfficeId
-- Drops indexes, foreign keys, then columns
-- =====================================================

PRINT 'Starting complete removal of organization hierarchy columns...';
PRINT '';

-- Step 1: Drop indexes
PRINT 'Step 1: Dropping indexes...';

-- Drop indexes from all tables
DECLARE @sql NVARCHAR(MAX) = '';

SELECT @sql = @sql + 'DROP INDEX ' + i.name + ' ON ' + t.name + '; PRINT ''Dropped index ' + i.name + ''';' + CHAR(13)
FROM sys.indexes i
INNER JOIN sys.tables t ON i.object_id = t.object_id
WHERE t.name IN ('PrivateWires', 'Safaricom', 'Airtel', 'PSTNs')
  AND i.name LIKE '%OrganizationId%' OR i.name LIKE '%OfficeId%' OR i.name LIKE '%SubOfficeId%';

IF @sql != ''
BEGIN
    EXEC sp_executesql @sql;
END

-- Step 2: Drop foreign key constraints
PRINT '';
PRINT 'Step 2: Dropping foreign key constraints...';

SET @sql = '';
SELECT @sql = @sql + 'ALTER TABLE ' + t.name + ' DROP CONSTRAINT ' + f.name + '; PRINT ''Dropped constraint ' + f.name + ''';' + CHAR(13)
FROM sys.foreign_keys f
INNER JOIN sys.tables t ON f.parent_object_id = t.object_id
WHERE t.name IN ('PrivateWires', 'Safaricom', 'Airtel', 'PSTNs')
  AND f.name LIKE '%Organization%' OR f.name LIKE '%Office%' OR f.name LIKE '%SubOffice%';

IF @sql != ''
BEGIN
    EXEC sp_executesql @sql;
END

-- Step 3: Now drop the columns
PRINT '';
PRINT 'Step 3: Removing columns...';

-- Remove from PrivateWires
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('PrivateWires') AND name = 'OrganizationId')
BEGIN
    ALTER TABLE PrivateWires DROP COLUMN OrganizationId;
    PRINT 'Dropped PrivateWires.OrganizationId';
END

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('PrivateWires') AND name = 'OfficeId')
BEGIN
    ALTER TABLE PrivateWires DROP COLUMN OfficeId;
    PRINT 'Dropped PrivateWires.OfficeId';
END

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('PrivateWires') AND name = 'SubOfficeId')
BEGIN
    ALTER TABLE PrivateWires DROP COLUMN SubOfficeId;
    PRINT 'Dropped PrivateWires.SubOfficeId';
END

-- Remove from Safaricom
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Safaricom') AND name = 'OrganizationId')
BEGIN
    ALTER TABLE Safaricom DROP COLUMN OrganizationId;
    PRINT 'Dropped Safaricom.OrganizationId';
END

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Safaricom') AND name = 'OfficeId')
BEGIN
    ALTER TABLE Safaricom DROP COLUMN OfficeId;
    PRINT 'Dropped Safaricom.OfficeId';
END

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Safaricom') AND name = 'SubOfficeId')
BEGIN
    ALTER TABLE Safaricom DROP COLUMN SubOfficeId;
    PRINT 'Dropped Safaricom.SubOfficeId';
END

-- Remove from Airtel
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Airtel') AND name = 'OrganizationId')
BEGIN
    ALTER TABLE Airtel DROP COLUMN OrganizationId;
    PRINT 'Dropped Airtel.OrganizationId';
END

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Airtel') AND name = 'OfficeId')
BEGIN
    ALTER TABLE Airtel DROP COLUMN OfficeId;
    PRINT 'Dropped Airtel.OfficeId';
END

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Airtel') AND name = 'SubOfficeId')
BEGIN
    ALTER TABLE Airtel DROP COLUMN SubOfficeId;
    PRINT 'Dropped Airtel.SubOfficeId';
END

-- Remove from PSTNs
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('PSTNs') AND name = 'OrganizationId')
BEGIN
    ALTER TABLE PSTNs DROP COLUMN OrganizationId;
    PRINT 'Dropped PSTNs.OrganizationId';
END

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('PSTNs') AND name = 'OfficeId')
BEGIN
    ALTER TABLE PSTNs DROP COLUMN OfficeId;
    PRINT 'Dropped PSTNs.OfficeId';
END

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('PSTNs') AND name = 'SubOfficeId')
BEGIN
    ALTER TABLE PSTNs DROP COLUMN SubOfficeId;
    PRINT 'Dropped PSTNs.SubOfficeId';
END

PRINT '';
PRINT '=== COMPLETE ===';
PRINT 'All organization hierarchy columns have been removed.';
PRINT 'Use EbillUserId -> EbillUsers table for organization information.';
PRINT '';
PRINT 'Remaining columns in Airtel:';

SELECT COLUMN_NAME
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Airtel'
ORDER BY ORDINAL_POSITION;