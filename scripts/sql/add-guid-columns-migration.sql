-- =====================================================
-- GUID Migration Script for TAB Database
-- This script adds GUID columns alongside existing INT IDs
-- Phase 1: Add GUID columns
-- =====================================================

-- Add GUID columns to all tables (non-breaking change)
-- These will coexist with INT IDs during transition

-- 1. EbillUsers table
ALTER TABLE EbillUsers
ADD PublicId UNIQUEIDENTIFIER DEFAULT NEWID() NOT NULL;

CREATE UNIQUE INDEX IX_EbillUsers_PublicId ON EbillUsers(PublicId);

-- 2. Organizations table
ALTER TABLE Organizations
ADD PublicId UNIQUEIDENTIFIER DEFAULT NEWID() NOT NULL;

CREATE UNIQUE INDEX IX_Organizations_PublicId ON Organizations(PublicId);

-- 3. Offices table
ALTER TABLE Offices
ADD PublicId UNIQUEIDENTIFIER DEFAULT NEWID() NOT NULL;

CREATE UNIQUE INDEX IX_Offices_PublicId ON Offices(PublicId);

-- 4. SubOffices table
ALTER TABLE SubOffices
ADD PublicId UNIQUEIDENTIFIER DEFAULT NEWID() NOT NULL;

CREATE UNIQUE INDEX IX_SubOffices_PublicId ON SubOffices(PublicId);

-- 5. ClassOfServices table
ALTER TABLE ClassOfServices
ADD PublicId UNIQUEIDENTIFIER DEFAULT NEWID() NOT NULL;

CREATE UNIQUE INDEX IX_ClassOfServices_PublicId ON ClassOfServices(PublicId);

-- 6. UserPhones table
ALTER TABLE UserPhones
ADD PublicId UNIQUEIDENTIFIER DEFAULT NEWID() NOT NULL;

CREATE UNIQUE INDEX IX_UserPhones_PublicId ON UserPhones(PublicId);

-- 7. ApplicationUser (AspNetUsers) table
ALTER TABLE AspNetUsers
ADD PublicId UNIQUEIDENTIFIER DEFAULT NEWID() NOT NULL;

CREATE UNIQUE INDEX IX_AspNetUsers_PublicId ON AspNetUsers(PublicId);

-- Populate GUIDs for existing records
UPDATE EbillUsers SET PublicId = NEWID() WHERE PublicId IS NULL;
UPDATE Organizations SET PublicId = NEWID() WHERE PublicId IS NULL;
UPDATE Offices SET PublicId = NEWID() WHERE PublicId IS NULL;
UPDATE SubOffices SET PublicId = NEWID() WHERE PublicId IS NULL;
UPDATE ClassOfServices SET PublicId = NEWID() WHERE PublicId IS NULL;
UPDATE UserPhones SET PublicId = NEWID() WHERE PublicId IS NULL;
UPDATE AspNetUsers SET PublicId = NEWID() WHERE PublicId IS NULL;

-- Verify all records have GUIDs
SELECT 'EbillUsers' AS TableName, COUNT(*) AS RecordsWithoutGUID FROM EbillUsers WHERE PublicId IS NULL
UNION ALL
SELECT 'Organizations', COUNT(*) FROM Organizations WHERE PublicId IS NULL
UNION ALL
SELECT 'Offices', COUNT(*) FROM Offices WHERE PublicId IS NULL
UNION ALL
SELECT 'SubOffices', COUNT(*) FROM SubOffices WHERE PublicId IS NULL
UNION ALL
SELECT 'ClassOfServices', COUNT(*) FROM ClassOfServices WHERE PublicId IS NULL
UNION ALL
SELECT 'UserPhones', COUNT(*) FROM UserPhones WHERE PublicId IS NULL
UNION ALL
SELECT 'AspNetUsers', COUNT(*) FROM AspNetUsers WHERE PublicId IS NULL;