-- Quick fix to remove unique constraint on SimRequests.IndexNo
-- Run this in SQL Server Management Studio or any SQL client

USE [YourDatabaseName] -- Replace with your actual database name
GO

-- Drop the unique index
DROP INDEX IF EXISTS IX_SimRequests_IndexNo ON dbo.SimRequests;
GO

-- Create a non-unique index for performance
CREATE NONCLUSTERED INDEX IX_SimRequests_IndexNo ON dbo.SimRequests (IndexNo);
GO

PRINT 'Successfully removed unique constraint on IndexNo. Multiple SIM requests per staff member are now allowed.';

-- Verify the change
SELECT 
    'Index Status' as Info,
    i.name AS IndexName,
    CASE WHEN i.is_unique = 1 THEN 'UNIQUE' ELSE 'NON-UNIQUE' END AS IndexType,
    c.name AS ColumnName
FROM sys.indexes i
INNER JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
INNER JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
WHERE i.object_id = OBJECT_ID('dbo.SimRequests')
    AND c.name = 'IndexNo';