-- Drop indexes that depend on deprecated columns, then drop the columns
USE [TABDB];
GO

PRINT 'Dropping dependent indexes first...';
PRINT '';

-- Drop index on PrivateWires that depends on deprecated columns
IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_PrivateWires_Organization_Office')
BEGIN
    DROP INDEX IX_PrivateWires_Organization_Office ON PrivateWires;
    PRINT 'Dropped IX_PrivateWires_Organization_Office';
END

-- Check for any other indexes on deprecated columns
IF EXISTS (SELECT * FROM sys.indexes WHERE name LIKE '%Organization%' AND object_id = OBJECT_ID('PrivateWires'))
BEGIN
    DECLARE @indexName NVARCHAR(200);
    DECLARE index_cursor CURSOR FOR
        SELECT name FROM sys.indexes
        WHERE name LIKE '%Organization%' AND object_id = OBJECT_ID('PrivateWires');

    OPEN index_cursor;
    FETCH NEXT FROM index_cursor INTO @indexName;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        EXEC('DROP INDEX ' + @indexName + ' ON PrivateWires');
        PRINT 'Dropped index: ' + @indexName;
        FETCH NEXT FROM index_cursor INTO @indexName;
    END

    CLOSE index_cursor;
    DEALLOCATE index_cursor;
END
GO

-- Now drop the deprecated columns from PrivateWires
PRINT '';
PRINT 'Dropping deprecated columns from PrivateWires...';

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('PrivateWires') AND name = 'Organization_DEPRECATED')
BEGIN
    ALTER TABLE PrivateWires DROP COLUMN Organization_DEPRECATED;
    PRINT 'Dropped PrivateWires.Organization_DEPRECATED';
END

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('PrivateWires') AND name = 'Office_DEPRECATED')
BEGIN
    ALTER TABLE PrivateWires DROP COLUMN Office_DEPRECATED;
    PRINT 'Dropped PrivateWires.Office_DEPRECATED';
END
GO

PRINT '';
PRINT 'Deprecated columns removed successfully!';
GO