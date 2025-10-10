-- =====================================================
-- Simple script to add PublicId columns to existing tables
-- Run this directly in SQL Server Management Studio
-- =====================================================

-- Check if columns exist before adding them

-- 1. EbillUsers table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('EbillUsers') AND name = 'PublicId')
BEGIN
    ALTER TABLE EbillUsers ADD PublicId UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID();
    CREATE UNIQUE INDEX IX_EbillUsers_PublicId ON EbillUsers(PublicId);
END

-- 2. Organizations table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Organizations') AND name = 'PublicId')
BEGIN
    ALTER TABLE Organizations ADD PublicId UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID();
    CREATE UNIQUE INDEX IX_Organizations_PublicId ON Organizations(PublicId);
END

-- 3. Offices table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Offices') AND name = 'PublicId')
BEGIN
    ALTER TABLE Offices ADD PublicId UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID();
    CREATE UNIQUE INDEX IX_Offices_PublicId ON Offices(PublicId);
END

-- 4. SubOffices table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('SubOffices') AND name = 'PublicId')
BEGIN
    ALTER TABLE SubOffices ADD PublicId UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID();
    CREATE UNIQUE INDEX IX_SubOffices_PublicId ON SubOffices(PublicId);
END

-- 5. ClassOfServices table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('ClassOfServices') AND name = 'PublicId')
BEGIN
    ALTER TABLE ClassOfServices ADD PublicId UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID();
    CREATE UNIQUE INDEX IX_ClassOfServices_PublicId ON ClassOfServices(PublicId);
END

-- 6. UserPhones table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('UserPhones') AND name = 'PublicId')
BEGIN
    ALTER TABLE UserPhones ADD PublicId UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID();
    CREATE UNIQUE INDEX IX_UserPhones_PublicId ON UserPhones(PublicId);
END

-- 7. Verify all tables have the column
SELECT
    'EbillUsers' AS TableName,
    CASE WHEN EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('EbillUsers') AND name = 'PublicId')
        THEN 'Added' ELSE 'Missing' END AS Status
UNION ALL
SELECT 'Organizations',
    CASE WHEN EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Organizations') AND name = 'PublicId')
        THEN 'Added' ELSE 'Missing' END
UNION ALL
SELECT 'Offices',
    CASE WHEN EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Offices') AND name = 'PublicId')
        THEN 'Added' ELSE 'Missing' END
UNION ALL
SELECT 'SubOffices',
    CASE WHEN EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('SubOffices') AND name = 'PublicId')
        THEN 'Added' ELSE 'Missing' END
UNION ALL
SELECT 'ClassOfServices',
    CASE WHEN EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('ClassOfServices') AND name = 'PublicId')
        THEN 'Added' ELSE 'Missing' END
UNION ALL
SELECT 'UserPhones',
    CASE WHEN EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('UserPhones') AND name = 'PublicId')
        THEN 'Added' ELSE 'Missing' END;

PRINT 'PublicId columns added successfully!';