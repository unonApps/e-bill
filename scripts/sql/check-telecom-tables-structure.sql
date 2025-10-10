-- Script to check the structure of telecom tables for normalization analysis
USE [TABDB];
GO

PRINT '============================================';
PRINT 'TELECOM TABLES STRUCTURE ANALYSIS';
PRINT '============================================';
PRINT '';

-- Check Safaricom table structure
PRINT '1. SAFARICOM TABLE STRUCTURE:';
PRINT '------------------------------';
SELECT
    c.COLUMN_NAME,
    c.DATA_TYPE,
    c.CHARACTER_MAXIMUM_LENGTH,
    c.IS_NULLABLE,
    CASE
        WHEN pk.COLUMN_NAME IS NOT NULL THEN 'PK'
        WHEN fk.COLUMN_NAME IS NOT NULL THEN 'FK -> ' + fk.REFERENCED_TABLE
        WHEN idx.COLUMN_NAME IS NOT NULL THEN 'INDEXED'
        ELSE ''
    END as CONSTRAINT_TYPE
FROM INFORMATION_SCHEMA.COLUMNS c
LEFT JOIN (
    SELECT ku.COLUMN_NAME
    FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
    JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE ku
        ON tc.CONSTRAINT_NAME = ku.CONSTRAINT_NAME
    WHERE tc.TABLE_NAME = 'Safaricom' AND tc.CONSTRAINT_TYPE = 'PRIMARY KEY'
) pk ON c.COLUMN_NAME = pk.COLUMN_NAME
LEFT JOIN (
    SELECT
        COL_NAME(fc.parent_object_id, fc.parent_column_id) AS COLUMN_NAME,
        OBJECT_NAME(fc.referenced_object_id) AS REFERENCED_TABLE
    FROM sys.foreign_key_columns fc
    WHERE OBJECT_NAME(fc.parent_object_id) = 'Safaricom'
) fk ON c.COLUMN_NAME = fk.COLUMN_NAME
LEFT JOIN (
    SELECT DISTINCT COL_NAME(ic.object_id, ic.column_id) AS COLUMN_NAME
    FROM sys.index_columns ic
    JOIN sys.indexes i ON ic.object_id = i.object_id AND ic.index_id = i.index_id
    WHERE OBJECT_NAME(ic.object_id) = 'Safaricom' AND i.is_primary_key = 0
) idx ON c.COLUMN_NAME = idx.COLUMN_NAME
WHERE c.TABLE_NAME = 'Safaricom'
ORDER BY c.ORDINAL_POSITION;

PRINT '';
PRINT '2. AIRTEL TABLE STRUCTURE:';
PRINT '------------------------------';
SELECT
    c.COLUMN_NAME,
    c.DATA_TYPE,
    c.CHARACTER_MAXIMUM_LENGTH,
    c.IS_NULLABLE,
    CASE
        WHEN pk.COLUMN_NAME IS NOT NULL THEN 'PK'
        WHEN fk.COLUMN_NAME IS NOT NULL THEN 'FK -> ' + fk.REFERENCED_TABLE
        WHEN idx.COLUMN_NAME IS NOT NULL THEN 'INDEXED'
        ELSE ''
    END as CONSTRAINT_TYPE
FROM INFORMATION_SCHEMA.COLUMNS c
LEFT JOIN (
    SELECT ku.COLUMN_NAME
    FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
    JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE ku
        ON tc.CONSTRAINT_NAME = ku.CONSTRAINT_NAME
    WHERE tc.TABLE_NAME = 'Airtel' AND tc.CONSTRAINT_TYPE = 'PRIMARY KEY'
) pk ON c.COLUMN_NAME = pk.COLUMN_NAME
LEFT JOIN (
    SELECT
        COL_NAME(fc.parent_object_id, fc.parent_column_id) AS COLUMN_NAME,
        OBJECT_NAME(fc.referenced_object_id) AS REFERENCED_TABLE
    FROM sys.foreign_key_columns fc
    WHERE OBJECT_NAME(fc.parent_object_id) = 'Airtel'
) fk ON c.COLUMN_NAME = fk.COLUMN_NAME
LEFT JOIN (
    SELECT DISTINCT COL_NAME(ic.object_id, ic.column_id) AS COLUMN_NAME
    FROM sys.index_columns ic
    JOIN sys.indexes i ON ic.object_id = i.object_id AND ic.index_id = i.index_id
    WHERE OBJECT_NAME(ic.object_id) = 'Airtel' AND i.is_primary_key = 0
) idx ON c.COLUMN_NAME = idx.COLUMN_NAME
WHERE c.TABLE_NAME = 'Airtel'
ORDER BY c.ORDINAL_POSITION;

PRINT '';
PRINT '3. PSTNs TABLE STRUCTURE:';
PRINT '------------------------------';
SELECT
    c.COLUMN_NAME,
    c.DATA_TYPE,
    c.CHARACTER_MAXIMUM_LENGTH,
    c.IS_NULLABLE,
    CASE
        WHEN pk.COLUMN_NAME IS NOT NULL THEN 'PK'
        WHEN fk.COLUMN_NAME IS NOT NULL THEN 'FK -> ' + fk.REFERENCED_TABLE
        WHEN idx.COLUMN_NAME IS NOT NULL THEN 'INDEXED'
        ELSE ''
    END as CONSTRAINT_TYPE
FROM INFORMATION_SCHEMA.COLUMNS c
LEFT JOIN (
    SELECT ku.COLUMN_NAME
    FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
    JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE ku
        ON tc.CONSTRAINT_NAME = ku.CONSTRAINT_NAME
    WHERE tc.TABLE_NAME = 'PSTNs' AND tc.CONSTRAINT_TYPE = 'PRIMARY KEY'
) pk ON c.COLUMN_NAME = pk.COLUMN_NAME
LEFT JOIN (
    SELECT
        COL_NAME(fc.parent_object_id, fc.parent_column_id) AS COLUMN_NAME,
        OBJECT_NAME(fc.referenced_object_id) AS REFERENCED_TABLE
    FROM sys.foreign_key_columns fc
    WHERE OBJECT_NAME(fc.parent_object_id) = 'PSTNs'
) fk ON c.COLUMN_NAME = fk.COLUMN_NAME
LEFT JOIN (
    SELECT DISTINCT COL_NAME(ic.object_id, ic.column_id) AS COLUMN_NAME
    FROM sys.index_columns ic
    JOIN sys.indexes i ON ic.object_id = i.object_id AND ic.index_id = i.index_id
    WHERE OBJECT_NAME(ic.object_id) = 'PSTNs' AND i.is_primary_key = 0
) idx ON c.COLUMN_NAME = idx.COLUMN_NAME
WHERE c.TABLE_NAME = 'PSTNs'
ORDER BY c.ORDINAL_POSITION;

PRINT '';
PRINT '4. PrivateWires TABLE STRUCTURE:';
PRINT '------------------------------';
SELECT
    c.COLUMN_NAME,
    c.DATA_TYPE,
    c.CHARACTER_MAXIMUM_LENGTH,
    c.IS_NULLABLE,
    CASE
        WHEN pk.COLUMN_NAME IS NOT NULL THEN 'PK'
        WHEN fk.COLUMN_NAME IS NOT NULL THEN 'FK -> ' + fk.REFERENCED_TABLE
        WHEN idx.COLUMN_NAME IS NOT NULL THEN 'INDEXED'
        ELSE ''
    END as CONSTRAINT_TYPE
FROM INFORMATION_SCHEMA.COLUMNS c
LEFT JOIN (
    SELECT ku.COLUMN_NAME
    FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
    JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE ku
        ON tc.CONSTRAINT_NAME = ku.CONSTRAINT_NAME
    WHERE tc.TABLE_NAME = 'PrivateWires' AND tc.CONSTRAINT_TYPE = 'PRIMARY KEY'
) pk ON c.COLUMN_NAME = pk.COLUMN_NAME
LEFT JOIN (
    SELECT
        COL_NAME(fc.parent_object_id, fc.parent_column_id) AS COLUMN_NAME,
        OBJECT_NAME(fc.referenced_object_id) AS REFERENCED_TABLE
    FROM sys.foreign_key_columns fc
    WHERE OBJECT_NAME(fc.parent_object_id) = 'PrivateWires'
) fk ON c.COLUMN_NAME = fk.COLUMN_NAME
LEFT JOIN (
    SELECT DISTINCT COL_NAME(ic.object_id, ic.column_id) AS COLUMN_NAME
    FROM sys.index_columns ic
    JOIN sys.indexes i ON ic.object_id = i.object_id AND ic.index_id = i.index_id
    WHERE OBJECT_NAME(ic.object_id) = 'PrivateWires' AND i.is_primary_key = 0
) idx ON c.COLUMN_NAME = idx.COLUMN_NAME
WHERE c.TABLE_NAME = 'PrivateWires'
ORDER BY c.ORDINAL_POSITION;

PRINT '';
PRINT '============================================';
PRINT 'NORMALIZATION ANALYSIS:';
PRINT '============================================';

-- Check for duplicate/redundant data patterns
PRINT '';
PRINT 'Checking for redundant string columns that should be normalized...';

-- Check Organization column values
PRINT '';
PRINT 'Unique Organization values across tables:';
SELECT 'Safaricom' as TableName, COUNT(DISTINCT Organization) as UniqueOrgs FROM Safaricom WHERE Organization IS NOT NULL
UNION ALL
SELECT 'Airtel', COUNT(DISTINCT Organization) FROM Airtel WHERE Organization IS NOT NULL
UNION ALL
SELECT 'PSTNs', COUNT(DISTINCT Organization) FROM PSTNs WHERE Organization IS NOT NULL
UNION ALL
SELECT 'PrivateWires', COUNT(DISTINCT Organization) FROM PrivateWires WHERE Organization IS NOT NULL;

-- Check Office column values
PRINT '';
PRINT 'Unique Office values across tables:';
SELECT 'Safaricom' as TableName, COUNT(DISTINCT Office) as UniqueOffices FROM Safaricom WHERE Office IS NOT NULL
UNION ALL
SELECT 'Airtel', COUNT(DISTINCT Office) FROM Airtel WHERE Office IS NOT NULL
UNION ALL
SELECT 'PSTNs', COUNT(DISTINCT Office) FROM PSTNs WHERE Office IS NOT NULL
UNION ALL
SELECT 'PrivateWires', COUNT(DISTINCT Office) FROM PrivateWires WHERE Office IS NOT NULL;

-- Sample redundant data
PRINT '';
PRINT 'Sample of Organization values in PSTNs:';
SELECT TOP 5 Organization, COUNT(*) as RecordCount
FROM PSTNs
WHERE Organization IS NOT NULL
GROUP BY Organization
ORDER BY COUNT(*) DESC;

PRINT '';
PRINT 'Sample of Office values in PSTNs:';
SELECT TOP 5 Office, COUNT(*) as RecordCount
FROM PSTNs
WHERE Office IS NOT NULL
GROUP BY Office
ORDER BY COUNT(*) DESC;

GO