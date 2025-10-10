-- Script to prepare organization and office codes for EbillUsers import
-- This script ensures all codes from the CSV are mapped in the database

USE [TABDB]
GO

-- First, let's add/update organization codes based on common ones in the CSV
PRINT 'Updating Organization Codes...';
PRINT '==============================';

-- WHO and related
UPDATE Organizations SET Code = 'WHO' WHERE Name LIKE '%World Health Organization%' AND Code IS NULL;
IF NOT EXISTS (SELECT 1 FROM Organizations WHERE Code = 'WHO')
    INSERT INTO Organizations (Name, Code, Description, CreatedDate)
    VALUES ('World Health Organization', 'WHO', 'United Nations specialized agency for international public health', GETUTCDATE());

-- UN Main organizations
UPDATE Organizations SET Code = 'UNIC' WHERE Name LIKE '%United Nations Information Centre%' AND Code IS NULL;
UPDATE Organizations SET Code = 'UNON' WHERE Name LIKE '%United Nations Office at Nairobi%' AND Code IS NULL;
UPDATE Organizations SET Code = 'UNEP' WHERE Name LIKE '%United Nations Environment Programme%' AND Code IS NULL;
UPDATE Organizations SET Code = 'UN-HAB' WHERE Name LIKE '%United Nations Human Settlements Programme%' AND Code IS NULL;
UPDATE Organizations SET Code = 'OIOS' WHERE Name LIKE '%Office of Internal Oversight Services%' AND Code IS NULL;

-- Other UN agencies
UPDATE Organizations SET Code = 'FAO' WHERE Name LIKE '%Food and Agriculture Organization%' AND Code IS NULL;
UPDATE Organizations SET Code = 'ICAO' WHERE Name LIKE '%International Civil Aviation Organization%' AND Code IS NULL;
UPDATE Organizations SET Code = 'WFP' WHERE Name LIKE '%World Food Programme%' AND Code IS NULL;
UPDATE Organizations SET Code = 'UNDP' WHERE Name LIKE '%United Nations Development Programme%' AND Code IS NULL;
UPDATE Organizations SET Code = 'UNFPA' WHERE Name LIKE '%United Nations Population Fund%' AND Code IS NULL;
UPDATE Organizations SET Code = 'UNHCR' WHERE Name LIKE '%United Nations High Commissioner for Refugees%' AND Code IS NULL;
UPDATE Organizations SET Code = 'UNICEF' WHERE Name LIKE '%United Nations Children%' AND Code IS NULL;

-- Add organizations found in CSV that might be missing
IF NOT EXISTS (SELECT 1 FROM Organizations WHERE Code = 'CON')
    INSERT INTO Organizations (Name, Code, Description, CreatedDate)
    VALUES ('Consultants', 'CON', 'External consultants and contractors working with UN', GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM Organizations WHERE Code = 'CSD')
    INSERT INTO Organizations (Name, Code, Description, CreatedDate)
    VALUES ('Central Support Division', 'CSD', 'Central administrative support services', GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM Organizations WHERE Code = 'ESA')
    INSERT INTO Organizations (Name, Code, Description, CreatedDate)
    VALUES ('Economic and Social Affairs', 'ESA', 'Department of Economic and Social Affairs', GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM Organizations WHERE Code = 'GMCP')
    INSERT INTO Organizations (Name, Code, Description, CreatedDate)
    VALUES ('Global Maritime Crime Programme', 'GMCP', 'UNODC Global Maritime Crime Programme', GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM Organizations WHERE Code = 'FFM SUDAN')
    INSERT INTO Organizations (Name, Code, Description, CreatedDate)
    VALUES ('Fact-Finding Mission Sudan', 'FFM SUDAN', 'UN Fact-Finding Mission for Sudan', GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM Organizations WHERE Code = 'BFMS')
    INSERT INTO Organizations (Name, Code, Description, CreatedDate)
    VALUES ('Budget and Financial Management Service', 'BFMS', 'UN Budget and Financial Management Service', GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM Organizations WHERE Code = 'FMTS')
    INSERT INTO Organizations (Name, Code, Description, CreatedDate)
    VALUES ('Facilities Management and Transportation Section', 'FMTS', 'UN Facilities Management and Transportation Section', GETUTCDATE());

-- Add more organizations as needed
IF NOT EXISTS (SELECT 1 FROM Organizations WHERE Code = 'DSS DPSS TDS NAIROBI')
    INSERT INTO Organizations (Name, Code, Description, CreatedDate)
    VALUES ('Department of Safety and Security', 'DSS DPSS TDS NAIROBI', 'UN Department of Safety and Security - Nairobi', GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM Organizations WHERE Code = 'DSS DRO AS KENYA')
    INSERT INTO Organizations (Name, Code, Description, CreatedDate)
    VALUES ('Department of Safety and Security - Kenya', 'DSS DRO AS KENYA', 'UN Department of Safety and Security - Kenya Regional Office', GETUTCDATE());

PRINT 'Organizations updated/added.';
PRINT '';

-- Now add office codes
PRINT 'Adding Office Codes...';
PRINT '======================';

DECLARE @WHOId INT, @UNICId INT, @UNONId INT, @UNEPId INT, @UNHABId INT;

SELECT @WHOId = Id FROM Organizations WHERE Code = 'WHO';
SELECT @UNICId = Id FROM Organizations WHERE Code = 'UNIC';
SELECT @UNONId = Id FROM Organizations WHERE Code = 'UNON';
SELECT @UNEPId = Id FROM Organizations WHERE Code = 'UNEP';
SELECT @UNHABId = Id FROM Organizations WHERE Code = 'UN-HAB';

-- WHO Offices
IF @WHOId IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM Offices WHERE Code = 'SOM' AND OrganizationId = @WHOId)
        INSERT INTO Offices (Name, Code, OrganizationId, CreatedDate)
        VALUES ('Somalia Office', 'SOM', @WHOId, GETUTCDATE());

    IF NOT EXISTS (SELECT 1 FROM Offices WHERE Code = 'KCO' AND OrganizationId = @WHOId)
        INSERT INTO Offices (Name, Code, OrganizationId, CreatedDate)
        VALUES ('Kenya Country Office', 'KCO', @WHOId, GETUTCDATE());
END

-- UNIC Offices
IF @UNICId IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM Offices WHERE Code = 'VS' AND OrganizationId = @UNICId)
        INSERT INTO Offices (Name, Code, OrganizationId, CreatedDate)
        VALUES ('Visitor Services', 'VS', @UNICId, GETUTCDATE());

    IF NOT EXISTS (SELECT 1 FROM Offices WHERE Code = 'VSA' AND OrganizationId = @UNICId)
        INSERT INTO Offices (Name, Code, OrganizationId, CreatedDate)
        VALUES ('Visitor Services Administration', 'VSA', @UNICId, GETUTCDATE());
END

-- UNON Offices
IF @UNONId IS NOT NULL
BEGIN
    -- Add common UNON office codes found in CSV
    IF NOT EXISTS (SELECT 1 FROM Offices WHERE Code = 'CSD' AND OrganizationId = @UNONId)
        INSERT INTO Offices (Name, Code, OrganizationId, CreatedDate)
        VALUES ('Central Support Division', 'CSD', @UNONId, GETUTCDATE());

    IF NOT EXISTS (SELECT 1 FROM Offices WHERE Code = 'ICTS' AND OrganizationId = @UNONId)
        INSERT INTO Offices (Name, Code, OrganizationId, CreatedDate)
        VALUES ('Information and Communication Technology Service', 'ICTS', @UNONId, GETUTCDATE());

    IF NOT EXISTS (SELECT 1 FROM Offices WHERE Code = 'CSS' AND OrganizationId = @UNONId)
        INSERT INTO Offices (Name, Code, OrganizationId, CreatedDate)
        VALUES ('Conference Services Section', 'CSS', @UNONId, GETUTCDATE());

    IF NOT EXISTS (SELECT 1 FROM Offices WHERE Code = 'HMS' AND OrganizationId = @UNONId)
        INSERT INTO Offices (Name, Code, OrganizationId, CreatedDate)
        VALUES ('Human Resources Management Service', 'HMS', @UNONId, GETUTCDATE());

    IF NOT EXISTS (SELECT 1 FROM Offices WHERE Code = 'BM Security' AND OrganizationId = @UNONId)
        INSERT INTO Offices (Name, Code, OrganizationId, CreatedDate)
        VALUES ('Building Management and Security', 'BM Security', @UNONId, GETUTCDATE());

    IF NOT EXISTS (SELECT 1 FROM Offices WHERE Code = 'BFMS' AND OrganizationId = @UNONId)
        INSERT INTO Offices (Name, Code, OrganizationId, CreatedDate)
        VALUES ('Budget and Financial Management Service', 'BFMS', @UNONId, GETUTCDATE());
END

-- Add Consultant offices (CON organization)
DECLARE @CONId INT;
SELECT @CONId = Id FROM Organizations WHERE Code = 'CON';

IF @CONId IS NOT NULL
BEGIN
    -- Add specific consultant offices if they appear in CSV
    IF NOT EXISTS (SELECT 1 FROM Offices WHERE Code = 'BCD TRAVEL' AND OrganizationId = @CONId)
        INSERT INTO Offices (Name, Code, OrganizationId, CreatedDate)
        VALUES ('BCD Travel Services', 'BCD TRAVEL', @CONId, GETUTCDATE());

    IF NOT EXISTS (SELECT 1 FROM Offices WHERE Code = 'COMPUTECH' AND OrganizationId = @CONId)
        INSERT INTO Offices (Name, Code, OrganizationId, CreatedDate)
        VALUES ('Computech Limited', 'COMPUTECH', @CONId, GETUTCDATE());
END

PRINT 'Office codes updated/added.';
PRINT '';

-- Summary report
PRINT 'Code Mapping Summary';
PRINT '====================';

SELECT 'Organizations with Codes' as Category, COUNT(*) as Count
FROM Organizations WHERE Code IS NOT NULL
UNION ALL
SELECT 'Organizations without Codes', COUNT(*)
FROM Organizations WHERE Code IS NULL
UNION ALL
SELECT 'Offices with Codes', COUNT(*)
FROM Offices WHERE Code IS NOT NULL
UNION ALL
SELECT 'Offices without Codes', COUNT(*)
FROM Offices WHERE Code IS NULL;

-- Show organizations and their codes
PRINT '';
PRINT 'Organizations with Codes:';
SELECT Id, Name, Code
FROM Organizations
WHERE Code IS NOT NULL
ORDER BY Code;

-- Show offices and their codes grouped by organization
PRINT '';
PRINT 'Offices with Codes by Organization:';
SELECT
    o.Name as Organization,
    o.Code as OrgCode,
    STRING_AGG(CONCAT(off.Name, ' (', off.Code, ')'), ', ') as Offices
FROM Organizations o
INNER JOIN Offices off ON off.OrganizationId = o.Id
WHERE off.Code IS NOT NULL
GROUP BY o.Id, o.Name, o.Code
ORDER BY o.Code;

PRINT '';
PRINT 'Code preparation completed. Ready for EbillUsers import.';
GO