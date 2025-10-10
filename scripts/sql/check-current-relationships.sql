-- Check current relationships between EbillUsers and Telecom tables
USE [TABDB]
GO

PRINT '==========================================================';
PRINT 'CURRENT RELATIONSHIPS: EBILLUSERS <-> TELECOM TABLES';
PRINT '==========================================================';
PRINT '';

-- 1. Check EbillUsers table structure
PRINT '1. EBILLUSERS TABLE STRUCTURE:';
PRINT '-------------------------------';
SELECT
    COLUMN_NAME,
    DATA_TYPE,
    CHARACTER_MAXIMUM_LENGTH,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'EbillUsers'
  AND COLUMN_NAME IN ('Id', 'IndexNumber', 'OfficialMobileNumber', 'Email')
ORDER BY ORDINAL_POSITION;

-- 2. Check Telecom tables for relationship columns
PRINT '';
PRINT '2. TELECOM TABLES RELATIONSHIP COLUMNS:';
PRINT '----------------------------------------';

-- Check PSTNs
PRINT '';
PRINT 'PSTNs Table:';
SELECT
    COLUMN_NAME,
    DATA_TYPE,
    CHARACTER_MAXIMUM_LENGTH,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'PSTNs'
  AND COLUMN_NAME IN ('CallingNumber', 'IndexNumber', 'EbillUserId')
ORDER BY ORDINAL_POSITION;

-- Check PrivateWires
PRINT '';
PRINT 'PrivateWires Table:';
SELECT
    COLUMN_NAME,
    DATA_TYPE,
    CHARACTER_MAXIMUM_LENGTH,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'PrivateWires'
  AND COLUMN_NAME IN ('ServiceNumber', 'IndexNumber', 'EbillUserId')
ORDER BY ORDINAL_POSITION;

-- Check Safaricom
PRINT '';
PRINT 'Safaricom Table:';
SELECT
    COLUMN_NAME,
    DATA_TYPE,
    CHARACTER_MAXIMUM_LENGTH,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Safaricom'
  AND COLUMN_NAME IN ('CallingNumber', 'IndexNumber', 'EbillUserId')
ORDER BY ORDINAL_POSITION;

-- Check Airtel
PRINT '';
PRINT 'Airtel Table:';
SELECT
    COLUMN_NAME,
    DATA_TYPE,
    CHARACTER_MAXIMUM_LENGTH,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Airtel'
  AND COLUMN_NAME IN ('CallingNumber', 'IndexNumber', 'EbillUserId')
ORDER BY ORDINAL_POSITION;

-- 3. Check Foreign Key Constraints
PRINT '';
PRINT '3. FOREIGN KEY RELATIONSHIPS:';
PRINT '------------------------------';
SELECT
    fk.name AS FK_Name,
    OBJECT_NAME(fk.parent_object_id) AS Table_Name,
    COL_NAME(fkc.parent_object_id, fkc.parent_column_id) AS Column_Name,
    OBJECT_NAME(fk.referenced_object_id) AS Referenced_Table,
    COL_NAME(fkc.referenced_object_id, fkc.referenced_column_id) AS Referenced_Column
FROM sys.foreign_keys fk
INNER JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
WHERE OBJECT_NAME(fk.referenced_object_id) = 'EbillUsers'
   OR OBJECT_NAME(fk.parent_object_id) IN ('PSTNs', 'PrivateWires', 'Safaricom', 'Airtel');

-- 4. Current Data Analysis
PRINT '';
PRINT '4. CURRENT DATA RELATIONSHIPS:';
PRINT '-------------------------------';

-- Check how bills are currently linked
PRINT '';
PRINT 'How many bills are linked by each method?';

WITH BillLinkage AS (
    SELECT 'PSTNs' as TableName,
           COUNT(*) as TotalRecords,
           COUNT(IndexNumber) as HasIndexNumber,
           COUNT(EbillUserId) as HasEbillUserId,
           COUNT(DISTINCT CallingNumber) as UniquePhoneNumbers
    FROM PSTNs
    UNION ALL
    SELECT 'PrivateWires',
           COUNT(*),
           COUNT(IndexNumber),
           COUNT(EbillUserId),
           COUNT(DISTINCT ServiceNumber)
    FROM PrivateWires
    UNION ALL
    SELECT 'Safaricom',
           COUNT(*),
           COUNT(IndexNumber),
           COUNT(EbillUserId),
           COUNT(DISTINCT CallingNumber)
    FROM Safaricom
    UNION ALL
    SELECT 'Airtel',
           COUNT(*),
           COUNT(IndexNumber),
           COUNT(EbillUserId),
           COUNT(DISTINCT CallingNumber)
    FROM Airtel
)
SELECT * FROM BillLinkage;

-- 5. Check for matching logic
PRINT '';
PRINT '5. MATCHING ANALYSIS:';
PRINT '---------------------';
PRINT 'How would bills match to users?';

-- Sample matching by phone number
SELECT TOP 5
    'By Phone Number' as MatchType,
    e.IndexNumber,
    e.FirstName + ' ' + e.LastName as UserName,
    e.OfficialMobileNumber,
    COUNT(DISTINCT p.Id) as PSTN_Bills
FROM EbillUsers e
LEFT JOIN PSTNs p ON p.CallingNumber = e.OfficialMobileNumber
WHERE e.OfficialMobileNumber IS NOT NULL
GROUP BY e.IndexNumber, e.FirstName, e.LastName, e.OfficialMobileNumber
HAVING COUNT(DISTINCT p.Id) > 0;

-- Sample matching by IndexNumber
SELECT TOP 5
    'By IndexNumber' as MatchType,
    e.IndexNumber,
    e.FirstName + ' ' + e.LastName as UserName,
    COUNT(DISTINCT p.Id) as PSTN_Bills
FROM EbillUsers e
LEFT JOIN PSTNs p ON p.IndexNumber = e.IndexNumber
WHERE e.IndexNumber IS NOT NULL
GROUP BY e.IndexNumber, e.FirstName, e.LastName
HAVING COUNT(DISTINCT p.Id) > 0;

PRINT '';
PRINT '==========================================================';
PRINT 'SUMMARY OF CURRENT DESIGN:';
PRINT '==========================================================';
PRINT '';
PRINT 'Current relationship structure:';
PRINT '1. Each telecom table has: IndexNumber (string) and EbillUserId (int)';
PRINT '2. EbillUsers has: Id (int), IndexNumber (string), OfficialMobileNumber (string)';
PRINT '';
PRINT 'Possible linking methods:';
PRINT 'Method 1: Phone-based - Match CallingNumber to OfficialMobileNumber';
PRINT 'Method 2: IndexNumber-based - Match IndexNumber fields';
PRINT 'Method 3: ID-based - Use EbillUserId foreign key to EbillUsers.Id';
PRINT '';
PRINT 'RECOMMENDATION: Use IndexNumber as primary link (stable, survives phone changes)';
PRINT 'Keep phone numbers for historical reference and validation';
GO