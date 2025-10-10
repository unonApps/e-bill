-- Script to seed EbillUsers from CSV data
-- This script maps organization/office codes to their IDs and imports users

USE [TABDB]
GO

-- First, ensure all required organization codes exist
-- Update existing organizations with their codes if not already set
UPDATE Organizations SET Code = 'WHO' WHERE Name = 'World Health Organization' AND Code IS NULL;
UPDATE Organizations SET Code = 'FAO' WHERE Name = 'Food and Agriculture Organization' AND Code IS NULL;
UPDATE Organizations SET Code = 'ICAO' WHERE Name = 'International Civil Aviation Organization' AND Code IS NULL;
UPDATE Organizations SET Code = 'WFP' WHERE Name = 'World Food Programme' AND Code IS NULL;
UPDATE Organizations SET Code = 'UNDP' WHERE Name = 'United Nations Development Programme' AND Code IS NULL;
UPDATE Organizations SET Code = 'UNFPA' WHERE Name = 'United Nations Population Fund' AND Code IS NULL;
UPDATE Organizations SET Code = 'UNHCR' WHERE Name = 'United Nations High Commissioner for Refugees' AND Code IS NULL;
UPDATE Organizations SET Code = 'UNICEF' WHERE Name = 'United Nations International Children''s Emergency Fund' AND Code IS NULL;
UPDATE Organizations SET Code = 'UN-WOMEN' WHERE Name = 'United Nations Entity for Gender Equality and the Empowerment of Women' AND Code IS NULL;
UPDATE Organizations SET Code = 'CON' WHERE Name = 'Consultants' AND Code IS NULL;

-- Add any missing organizations found in the CSV
IF NOT EXISTS (SELECT 1 FROM Organizations WHERE Code = 'CON')
    INSERT INTO Organizations (Name, Code, Description) VALUES ('Consultants', 'CON', 'External consultants and contractors');

IF NOT EXISTS (SELECT 1 FROM Organizations WHERE Code = 'CSD')
    INSERT INTO Organizations (Name, Code, Description) VALUES ('Central Support Division', 'CSD', 'Central Support Division');

IF NOT EXISTS (SELECT 1 FROM Organizations WHERE Code = 'ESA')
    INSERT INTO Organizations (Name, Code, Description) VALUES ('Economic and Social Affairs', 'ESA', 'Department of Economic and Social Affairs');

IF NOT EXISTS (SELECT 1 FROM Organizations WHERE Code = 'FFM SUDAN')
    INSERT INTO Organizations (Name, Code, Description) VALUES ('Fact-Finding Mission Sudan', 'FFM SUDAN', 'UN Fact-Finding Mission for Sudan');

IF NOT EXISTS (SELECT 1 FROM Organizations WHERE Code = 'GMCP')
    INSERT INTO Organizations (Name, Code, Description) VALUES ('Global Maritime Crime Programme', 'GMCP', 'UNODC Global Maritime Crime Programme');

IF NOT EXISTS (SELECT 1 FROM Organizations WHERE Code = 'CON')
    INSERT INTO Organizations (Name, Code, Description) VALUES ('Consultants', 'CON', 'External consultants and contractors');

-- Create temporary table to hold CSV data for processing
IF OBJECT_ID('tempdb..#TempEbillUsers') IS NOT NULL
    DROP TABLE #TempEbillUsers;

CREATE TABLE #TempEbillUsers (
    OfficialMobileNumber NVARCHAR(20),
    FirstName NVARCHAR(100),
    LastName NVARCHAR(100),
    IndexNumber NVARCHAR(50),
    Location NVARCHAR(200),
    OrgCode NVARCHAR(50),
    OfficeCode NVARCHAR(50),
    SubOfficeCode NVARCHAR(50),
    ClassOfService NVARCHAR(100),
    Email NVARCHAR(256),
    ProcessingStatus NVARCHAR(50) DEFAULT 'Pending'
);

-- Insert sample data from CSV (you'll need to use BULK INSERT or import wizard for full data)
-- For now, let's insert some sample records to demonstrate the process

INSERT INTO #TempEbillUsers (OfficialMobileNumber, FirstName, LastName, IndexNumber, Location, OrgCode, OfficeCode, SubOfficeCode, ClassOfService, Email)
VALUES
('21236', 'Stella', 'Vuzo', '120329', 'Upper Library', 'UNIC', '', '', '0', 'stella.vuzo@un.org'),
('23677', 'Mohamed Ayman', 'Suliman', '893444', 'Sudan', 'UNIC', '', '', '0', 'mohamed.suliman@un.org'),
('22034', 'Service', 'Visitors', '10077492', 'Upper Library', 'UNIC', 'VS', '', '0', 'visitors@un.org'),
('24560', 'Marwan', 'Elbliety', '10076077', 'Upper Library', 'UNIC', 'VS', '', '0', 'marwan.elbliety@un.org'),
('21596', 'Hellen', 'Fayemi', '91872', 'PREFAB-D2', 'WHO', 'SOM', '', '0', 'hellen.fayemi@who.int'),
('21595', 'Rita', 'Awori', '206444', 'PREFAB-D2', 'WHO', 'SOM', '', '0', 'rita.awori@who.int'),
('35045', 'Nollascuis', 'Ganda', '0', 'U-LEVEL-3', 'WHO', 'KCO', '', '0', 'nollascuis.ganda@who.int'),
('35002', 'Rudi', 'Eggers', '73118', 'U-LEVEL-3', 'WHO', 'KCO', '', '0', 'rudi.eggers@who.int');

-- Clean up data
UPDATE #TempEbillUsers
SET FirstName = LTRIM(RTRIM(FirstName)),
    LastName = LTRIM(RTRIM(LastName)),
    IndexNumber = LTRIM(RTRIM(IndexNumber)),
    Location = LTRIM(RTRIM(Location)),
    OrgCode = LTRIM(RTRIM(OrgCode)),
    OfficeCode = LTRIM(RTRIM(OfficeCode)),
    SubOfficeCode = LTRIM(RTRIM(SubOfficeCode));

-- Remove special characters from names
UPDATE #TempEbillUsers
SET FirstName = REPLACE(REPLACE(REPLACE(REPLACE(FirstName, '-', ''), '&', ''), '$', ''), '#', ''),
    LastName = REPLACE(REPLACE(REPLACE(REPLACE(LastName, '(', ''), ')', ''), ',', ''), ' ', '');

-- Skip records with invalid data
UPDATE #TempEbillUsers
SET ProcessingStatus = 'Invalid'
WHERE FirstName LIKE '%Service%'
   OR FirstName LIKE '%Reception%'
   OR FirstName LIKE '%Library%'
   OR FirstName LIKE '%Fax%'
   OR FirstName LIKE '%Office%'
   OR FirstName LIKE '%Consultant%'
   OR FirstName LIKE '%Intern%'
   OR FirstName LIKE '%CONF%'
   OR IndexNumber = '0'
   OR IndexNumber IS NULL;

-- Generate email addresses where missing
UPDATE #TempEbillUsers
SET Email = LOWER(FirstName) + '.' + LOWER(LastName) + '@un.org'
WHERE (Email IS NULL OR Email = '')
  AND ProcessingStatus = 'Pending';

-- Clean up email addresses
UPDATE #TempEbillUsers
SET Email = REPLACE(REPLACE(Email, ' ', '.'), '..', '.');

-- Now insert valid records into EbillUsers table
-- First, we'll create a mapping query that resolves codes to IDs

INSERT INTO EbillUsers (
    FirstName,
    LastName,
    IndexNumber,
    Email,
    OfficialMobileNumber,
    Location,
    ClassOfService,
    OrganizationId,
    OfficeId,
    SubOfficeId,
    IsActive,
    CreatedDate
)
SELECT DISTINCT
    t.FirstName,
    t.LastName,
    t.IndexNumber,
    t.Email,
    t.OfficialMobileNumber,
    t.Location,
    CASE
        WHEN t.ClassOfService = '0' THEN NULL
        ELSE t.ClassOfService
    END,
    org.Id as OrganizationId,
    off.Id as OfficeId,
    sub.Id as SubOfficeId,
    1 as IsActive,
    GETUTCDATE() as CreatedDate
FROM #TempEbillUsers t
LEFT JOIN Organizations org ON org.Code = t.OrgCode
LEFT JOIN Offices off ON off.Code = t.OfficeCode AND off.OrganizationId = org.Id
LEFT JOIN SubOffices sub ON sub.Code = t.SubOfficeCode AND sub.OfficeId = off.Id
WHERE t.ProcessingStatus = 'Pending'
  AND NOT EXISTS (
    SELECT 1 FROM EbillUsers e
    WHERE e.IndexNumber = t.IndexNumber
       OR (e.FirstName = t.FirstName AND e.LastName = t.LastName AND e.Email = t.Email)
  );

-- Report on import results
PRINT 'Import Results:';
PRINT '================';

DECLARE @TotalRecords INT = (SELECT COUNT(*) FROM #TempEbillUsers);
DECLARE @ValidRecords INT = (SELECT COUNT(*) FROM #TempEbillUsers WHERE ProcessingStatus = 'Pending');
DECLARE @InvalidRecords INT = (SELECT COUNT(*) FROM #TempEbillUsers WHERE ProcessingStatus = 'Invalid');
DECLARE @ImportedRecords INT = @@ROWCOUNT;

PRINT 'Total records in CSV: ' + CAST(@TotalRecords AS VARCHAR(10));
PRINT 'Valid records: ' + CAST(@ValidRecords AS VARCHAR(10));
PRINT 'Invalid records (skipped): ' + CAST(@InvalidRecords AS VARCHAR(10));
PRINT 'Successfully imported: ' + CAST(@ImportedRecords AS VARCHAR(10));

-- Show any organizations that couldn't be mapped
PRINT '';
PRINT 'Unmapped Organizations:';
SELECT DISTINCT t.OrgCode, COUNT(*) as RecordCount
FROM #TempEbillUsers t
LEFT JOIN Organizations org ON org.Code = t.OrgCode
WHERE org.Id IS NULL AND t.OrgCode IS NOT NULL AND t.OrgCode != ''
GROUP BY t.OrgCode;

-- Show any offices that couldn't be mapped
PRINT '';
PRINT 'Unmapped Offices:';
SELECT DISTINCT t.OfficeCode, t.OrgCode, COUNT(*) as RecordCount
FROM #TempEbillUsers t
LEFT JOIN Organizations org ON org.Code = t.OrgCode
LEFT JOIN Offices off ON off.Code = t.OfficeCode AND off.OrganizationId = org.Id
WHERE off.Id IS NULL AND t.OfficeCode IS NOT NULL AND t.OfficeCode != ''
GROUP BY t.OfficeCode, t.OrgCode;

-- Clean up
DROP TABLE #TempEbillUsers;

PRINT '';
PRINT 'Import process completed.';
GO