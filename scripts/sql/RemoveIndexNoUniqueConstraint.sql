-- Script to remove the unique constraint on IndexNo in SimRequests table
-- This allows multiple SIM requests for the same staff member (same IndexNo)

-- Drop the unique index
IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SimRequests_IndexNo' AND object_id = OBJECT_ID('dbo.SimRequests'))
BEGIN
    DROP INDEX IX_SimRequests_IndexNo ON dbo.SimRequests;
    PRINT 'Unique index IX_SimRequests_IndexNo dropped successfully.';
END
ELSE
BEGIN
    PRINT 'Index IX_SimRequests_IndexNo does not exist.';
END

-- Create a non-unique index for performance (optional but recommended)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SimRequests_IndexNo_NonUnique' AND object_id = OBJECT_ID('dbo.SimRequests'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_SimRequests_IndexNo_NonUnique
    ON dbo.SimRequests (IndexNo);
    PRINT 'Non-unique index IX_SimRequests_IndexNo_NonUnique created successfully.';
END
ELSE
BEGIN
    PRINT 'Non-unique index already exists.';
END

-- Verify the change
SELECT 
    i.name AS IndexName,
    i.is_unique AS IsUnique,
    c.name AS ColumnName
FROM sys.indexes i
INNER JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
INNER JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
WHERE i.object_id = OBJECT_ID('dbo.SimRequests')
    AND c.name = 'IndexNo';