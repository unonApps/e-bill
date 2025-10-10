-- Simplified normalization script for telecom tables
USE [TABDB];
GO

PRINT '================================================================';
PRINT 'TELECOM TABLES NORMALIZATION - SIMPLIFIED VERSION';
PRINT '================================================================';
PRINT '';

-- Add foreign key columns to Safaricom
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Safaricom') AND name = 'OrganizationId')
BEGIN
    ALTER TABLE Safaricom ADD OrganizationId INT NULL;
    PRINT 'Added OrganizationId to Safaricom';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Safaricom') AND name = 'OfficeId')
BEGIN
    ALTER TABLE Safaricom ADD OfficeId INT NULL;
    PRINT 'Added OfficeId to Safaricom';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Safaricom') AND name = 'SubOfficeId')
BEGIN
    ALTER TABLE Safaricom ADD SubOfficeId INT NULL;
    PRINT 'Added SubOfficeId to Safaricom';
END
GO

-- Add foreign key columns to Airtel
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Airtel') AND name = 'OrganizationId')
BEGIN
    ALTER TABLE Airtel ADD OrganizationId INT NULL;
    PRINT 'Added OrganizationId to Airtel';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Airtel') AND name = 'OfficeId')
BEGIN
    ALTER TABLE Airtel ADD OfficeId INT NULL;
    PRINT 'Added OfficeId to Airtel';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Airtel') AND name = 'SubOfficeId')
BEGIN
    ALTER TABLE Airtel ADD SubOfficeId INT NULL;
    PRINT 'Added SubOfficeId to Airtel';
END
GO

-- Add foreign key columns to PSTNs
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('PSTNs') AND name = 'OrganizationId')
BEGIN
    ALTER TABLE PSTNs ADD OrganizationId INT NULL;
    PRINT 'Added OrganizationId to PSTNs';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('PSTNs') AND name = 'OfficeId')
BEGIN
    ALTER TABLE PSTNs ADD OfficeId INT NULL;
    PRINT 'Added OfficeId to PSTNs';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('PSTNs') AND name = 'SubOfficeId')
BEGIN
    ALTER TABLE PSTNs ADD SubOfficeId INT NULL;
    PRINT 'Added SubOfficeId to PSTNs';
END
GO

-- Add foreign key columns to PrivateWires
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('PrivateWires') AND name = 'OrganizationId')
BEGIN
    ALTER TABLE PrivateWires ADD OrganizationId INT NULL;
    PRINT 'Added OrganizationId to PrivateWires';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('PrivateWires') AND name = 'OfficeId')
BEGIN
    ALTER TABLE PrivateWires ADD OfficeId INT NULL;
    PRINT 'Added OfficeId to PrivateWires';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('PrivateWires') AND name = 'SubOfficeId')
BEGIN
    ALTER TABLE PrivateWires ADD SubOfficeId INT NULL;
    PRINT 'Added SubOfficeId to PrivateWires';
END
GO

PRINT 'Foreign key columns added successfully';
PRINT '';

-- Now update the foreign keys based on string matches
PRINT 'Mapping string values to foreign keys...';

-- Update PSTNs
UPDATE p
SET p.OrganizationId = o.Id
FROM PSTNs p
INNER JOIN Organizations o ON (p.Organization = o.Code OR p.Organization = o.Name)
WHERE p.OrganizationId IS NULL AND p.Organization IS NOT NULL;

PRINT 'Updated PSTNs OrganizationId';

UPDATE p
SET p.OfficeId = ofc.Id
FROM PSTNs p
INNER JOIN Offices ofc ON (p.Office = ofc.Code OR p.Office = ofc.Name)
WHERE p.OfficeId IS NULL AND p.Office IS NOT NULL;

PRINT 'Updated PSTNs OfficeId';

-- Update PrivateWires
UPDATE pw
SET pw.OrganizationId = o.Id
FROM PrivateWires pw
INNER JOIN Organizations o ON (pw.Organization = o.Code OR pw.Organization = o.Name)
WHERE pw.OrganizationId IS NULL AND pw.Organization IS NOT NULL;

PRINT 'Updated PrivateWires OrganizationId';

UPDATE pw
SET pw.OfficeId = ofc.Id
FROM PrivateWires pw
INNER JOIN Offices ofc ON (pw.Office = ofc.Code OR pw.Office = ofc.Name)
WHERE pw.OfficeId IS NULL AND pw.Office IS NOT NULL;

PRINT 'Updated PrivateWires OfficeId';
GO

-- Create foreign key constraints
PRINT '';
PRINT 'Creating foreign key constraints...';

-- Safaricom FKs
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Safaricom_Organizations')
BEGIN
    ALTER TABLE Safaricom ADD CONSTRAINT FK_Safaricom_Organizations
    FOREIGN KEY (OrganizationId) REFERENCES Organizations(Id) ON DELETE SET NULL;
    PRINT 'Created FK_Safaricom_Organizations';
END

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Safaricom_Offices')
BEGIN
    ALTER TABLE Safaricom ADD CONSTRAINT FK_Safaricom_Offices
    FOREIGN KEY (OfficeId) REFERENCES Offices(Id) ON DELETE SET NULL;
    PRINT 'Created FK_Safaricom_Offices';
END

-- Airtel FKs
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Airtel_Organizations')
BEGIN
    ALTER TABLE Airtel ADD CONSTRAINT FK_Airtel_Organizations
    FOREIGN KEY (OrganizationId) REFERENCES Organizations(Id) ON DELETE SET NULL;
    PRINT 'Created FK_Airtel_Organizations';
END

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Airtel_Offices')
BEGIN
    ALTER TABLE Airtel ADD CONSTRAINT FK_Airtel_Offices
    FOREIGN KEY (OfficeId) REFERENCES Offices(Id) ON DELETE SET NULL;
    PRINT 'Created FK_Airtel_Offices';
END

-- PSTNs FKs
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_PSTNs_Organizations')
BEGIN
    ALTER TABLE PSTNs ADD CONSTRAINT FK_PSTNs_Organizations
    FOREIGN KEY (OrganizationId) REFERENCES Organizations(Id) ON DELETE SET NULL;
    PRINT 'Created FK_PSTNs_Organizations';
END

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_PSTNs_Offices')
BEGIN
    ALTER TABLE PSTNs ADD CONSTRAINT FK_PSTNs_Offices
    FOREIGN KEY (OfficeId) REFERENCES Offices(Id) ON DELETE SET NULL;
    PRINT 'Created FK_PSTNs_Offices';
END

-- PrivateWires FKs
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_PrivateWires_Organizations')
BEGIN
    ALTER TABLE PrivateWires ADD CONSTRAINT FK_PrivateWires_Organizations
    FOREIGN KEY (OrganizationId) REFERENCES Organizations(Id) ON DELETE SET NULL;
    PRINT 'Created FK_PrivateWires_Organizations';
END

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_PrivateWires_Offices')
BEGIN
    ALTER TABLE PrivateWires ADD CONSTRAINT FK_PrivateWires_Offices
    FOREIGN KEY (OfficeId) REFERENCES Offices(Id) ON DELETE SET NULL;
    PRINT 'Created FK_PrivateWires_Offices';
END
GO

-- Create indexes for performance
PRINT '';
PRINT 'Creating performance indexes...';

-- EbillUserId indexes
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Safaricom_EbillUserId')
BEGIN
    CREATE INDEX IX_Safaricom_EbillUserId ON Safaricom(EbillUserId);
    PRINT 'Created IX_Safaricom_EbillUserId';
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Airtel_EbillUserId')
BEGIN
    CREATE INDEX IX_Airtel_EbillUserId ON Airtel(EbillUserId);
    PRINT 'Created IX_Airtel_EbillUserId';
END

-- OrganizationId indexes
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Safaricom_OrganizationId')
BEGIN
    CREATE INDEX IX_Safaricom_OrganizationId ON Safaricom(OrganizationId);
    PRINT 'Created IX_Safaricom_OrganizationId';
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Airtel_OrganizationId')
BEGIN
    CREATE INDEX IX_Airtel_OrganizationId ON Airtel(OrganizationId);
    PRINT 'Created IX_Airtel_OrganizationId';
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_PSTNs_OrganizationId')
BEGIN
    CREATE INDEX IX_PSTNs_OrganizationId ON PSTNs(OrganizationId);
    PRINT 'Created IX_PSTNs_OrganizationId';
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_PrivateWires_OrganizationId')
BEGIN
    CREATE INDEX IX_PrivateWires_OrganizationId ON PrivateWires(OrganizationId);
    PRINT 'Created IX_PrivateWires_OrganizationId';
END
GO

-- Final verification
PRINT '';
PRINT '================================================================';
PRINT 'NORMALIZATION STATUS';
PRINT '================================================================';

SELECT 'Safaricom' as TableName,
    COUNT(*) as TotalRecords,
    COUNT(OrganizationId) as WithOrganizationId,
    COUNT(OfficeId) as WithOfficeId,
    COUNT(EbillUserId) as WithEbillUserId
FROM Safaricom
UNION ALL
SELECT 'Airtel',
    COUNT(*),
    COUNT(OrganizationId),
    COUNT(OfficeId),
    COUNT(EbillUserId)
FROM Airtel
UNION ALL
SELECT 'PSTNs',
    COUNT(*),
    COUNT(OrganizationId),
    COUNT(OfficeId),
    COUNT(EbillUserId)
FROM PSTNs
UNION ALL
SELECT 'PrivateWires',
    COUNT(*),
    COUNT(OrganizationId),
    COUNT(OfficeId),
    COUNT(EbillUserId)
FROM PrivateWires;

PRINT '';
PRINT 'NORMALIZATION COMPLETE!';
PRINT '';
PRINT 'To remove deprecated columns later, use:';
PRINT 'EXEC sp_rename ''[TableName].Organization'', ''Organization_DEPRECATED'', ''COLUMN'';';
PRINT 'EXEC sp_rename ''[TableName].Office'', ''Office_DEPRECATED'', ''COLUMN'';';
PRINT 'Then after verification: ALTER TABLE [TableName] DROP COLUMN [Column_DEPRECATED];';
GO