-- =====================================================
-- Remove redundant OrganizationId, OfficeId, SubOfficeId columns
-- These can be obtained through EbillUserId relationship
-- =====================================================

PRINT 'Removing redundant organization hierarchy columns from telecom tables...';
PRINT '';

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
PRINT 'Organization hierarchy columns removed successfully!';
PRINT 'Use EbillUserId to get OrganizationId, OfficeId, and SubOfficeId from EbillUsers table.';
PRINT '';

-- Show remaining columns for verification
PRINT 'Remaining columns in telecom tables:';
PRINT '';
PRINT 'Airtel columns:';
SELECT COLUMN_NAME
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Airtel'
ORDER BY ORDINAL_POSITION;