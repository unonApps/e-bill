-- =====================================================
-- Remove Redundant Columns from Telecom Tables
-- And drop the unified view
-- =====================================================

-- Step 1: Drop the unified view if it exists
IF EXISTS (SELECT * FROM sys.views WHERE name = 'vw_AllCallRecords')
BEGIN
    DROP VIEW vw_AllCallRecords;
    PRINT 'Dropped view vw_AllCallRecords';
END

-- Step 2: Drop other normalization views if they exist
IF EXISTS (SELECT * FROM sys.views WHERE name = 'vw_PrivateWires_Normalized')
BEGIN
    DROP VIEW vw_PrivateWires_Normalized;
    PRINT 'Dropped view vw_PrivateWires_Normalized';
END

-- Step 3: Remove redundant columns from PrivateWires
-- These columns are redundant because we can get them from EbillUsers relationship
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('PrivateWires') AND name = 'CallerName_DEPRECATED')
BEGIN
    ALTER TABLE PrivateWires DROP COLUMN CallerName_DEPRECATED;
    PRINT 'Dropped PrivateWires.CallerName_DEPRECATED';
END

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('PrivateWires') AND name = 'SubOffice')
BEGIN
    ALTER TABLE PrivateWires DROP COLUMN SubOffice;
    PRINT 'Dropped PrivateWires.SubOffice';
END

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('PrivateWires') AND name = 'Level4Unit')
BEGIN
    ALTER TABLE PrivateWires DROP COLUMN Level4Unit;
    PRINT 'Dropped PrivateWires.Level4Unit';
END

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('PrivateWires') AND name = 'OrganizationalUnit')
BEGIN
    ALTER TABLE PrivateWires DROP COLUMN OrganizationalUnit;
    PRINT 'Dropped PrivateWires.OrganizationalUnit';
END

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('PrivateWires') AND name = 'Location')
BEGIN
    ALTER TABLE PrivateWires DROP COLUMN Location;
    PRINT 'Dropped PrivateWires.Location';
END

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('PrivateWires') AND name = 'OCACode')
BEGIN
    ALTER TABLE PrivateWires DROP COLUMN OCACode;
    PRINT 'Dropped PrivateWires.OCACode';
END

-- Step 4: Remove redundant columns from PSTNs
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('PSTNs') AND name = 'OrganizationalUnit')
BEGIN
    ALTER TABLE PSTNs DROP COLUMN OrganizationalUnit;
    PRINT 'Dropped PSTNs.OrganizationalUnit';
END

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('PSTNs') AND name = 'Location')
BEGIN
    ALTER TABLE PSTNs DROP COLUMN Location;
    PRINT 'Dropped PSTNs.Location';
END

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('PSTNs') AND name = 'OCACode')
BEGIN
    ALTER TABLE PSTNs DROP COLUMN OCACode;
    PRINT 'Dropped PSTNs.OCACode';
END

-- Step 5: Verify the columns have been removed
PRINT '';
PRINT 'Verification - Remaining columns in PrivateWires:';
SELECT COLUMN_NAME
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'PrivateWires'
ORDER BY ORDINAL_POSITION;

PRINT '';
PRINT 'Verification - Remaining columns in PSTNs:';
SELECT COLUMN_NAME
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'PSTNs'
ORDER BY ORDINAL_POSITION;

PRINT '';
PRINT 'Column removal complete!';
PRINT 'The redundant columns have been removed.';
PRINT 'Use the EbillUserId foreign key to get user and organization information.';