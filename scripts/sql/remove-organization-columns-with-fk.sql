-- =====================================================
-- Remove redundant OrganizationId, OfficeId, SubOfficeId columns
-- First drop foreign key constraints, then remove columns
-- =====================================================

PRINT 'Step 1: Dropping foreign key constraints...';
PRINT '';

-- Drop foreign key constraints from PrivateWires
IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_PrivateWires_Organizations')
BEGIN
    ALTER TABLE PrivateWires DROP CONSTRAINT FK_PrivateWires_Organizations;
    PRINT 'Dropped FK_PrivateWires_Organizations';
END

IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_PrivateWires_Offices')
BEGIN
    ALTER TABLE PrivateWires DROP CONSTRAINT FK_PrivateWires_Offices;
    PRINT 'Dropped FK_PrivateWires_Offices';
END

IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_PrivateWires_SubOffices')
BEGIN
    ALTER TABLE PrivateWires DROP CONSTRAINT FK_PrivateWires_SubOffices;
    PRINT 'Dropped FK_PrivateWires_SubOffices';
END

-- Drop foreign key constraints from Safaricom
IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Safaricom_Organizations')
BEGIN
    ALTER TABLE Safaricom DROP CONSTRAINT FK_Safaricom_Organizations;
    PRINT 'Dropped FK_Safaricom_Organizations';
END

IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Safaricom_Offices')
BEGIN
    ALTER TABLE Safaricom DROP CONSTRAINT FK_Safaricom_Offices;
    PRINT 'Dropped FK_Safaricom_Offices';
END

IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Safaricom_SubOffices')
BEGIN
    ALTER TABLE Safaricom DROP CONSTRAINT FK_Safaricom_SubOffices;
    PRINT 'Dropped FK_Safaricom_SubOffices';
END

-- Drop foreign key constraints from Airtel
IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Airtel_Organizations')
BEGIN
    ALTER TABLE Airtel DROP CONSTRAINT FK_Airtel_Organizations;
    PRINT 'Dropped FK_Airtel_Organizations';
END

IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Airtel_Offices')
BEGIN
    ALTER TABLE Airtel DROP CONSTRAINT FK_Airtel_Offices;
    PRINT 'Dropped FK_Airtel_Offices';
END

IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Airtel_SubOffices')
BEGIN
    ALTER TABLE Airtel DROP CONSTRAINT FK_Airtel_SubOffices;
    PRINT 'Dropped FK_Airtel_SubOffices';
END

-- Drop foreign key constraints from PSTNs
IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_PSTNs_Organizations')
BEGIN
    ALTER TABLE PSTNs DROP CONSTRAINT FK_PSTNs_Organizations;
    PRINT 'Dropped FK_PSTNs_Organizations';
END

IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_PSTNs_Offices')
BEGIN
    ALTER TABLE PSTNs DROP CONSTRAINT FK_PSTNs_Offices;
    PRINT 'Dropped FK_PSTNs_Offices';
END

IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_PSTNs_SubOffices')
BEGIN
    ALTER TABLE PSTNs DROP CONSTRAINT FK_PSTNs_SubOffices;
    PRINT 'Dropped FK_PSTNs_SubOffices';
END

PRINT '';
PRINT 'Step 2: Removing organization hierarchy columns...';
PRINT '';

-- Remove columns from PrivateWires
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

-- Remove columns from Safaricom
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

-- Remove columns from Airtel
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

-- Remove columns from PSTNs
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
PRINT 'Use EbillUserId -> EbillUsers to get OrganizationId, OfficeId, and SubOfficeId.';
PRINT '';

-- Show sample query to get organization info
PRINT 'Example query to get organization info:';
PRINT 'SELECT a.*, eu.FirstName, eu.LastName, org.Name AS Organization, ofc.Name AS Office';
PRINT 'FROM Airtel a';
PRINT 'LEFT JOIN EbillUsers eu ON a.EbillUserId = eu.Id';
PRINT 'LEFT JOIN Organizations org ON eu.OrganizationId = org.Id';
PRINT 'LEFT JOIN Offices ofc ON eu.OfficeId = ofc.Id';