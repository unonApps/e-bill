-- Add StagingBatchId column to telecom tables
-- This column tracks which staging batch a record belongs to

-- Add to PSTNs table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PSTNs]') AND name = 'StagingBatchId')
BEGIN
    ALTER TABLE [dbo].[PSTNs]
    ADD [StagingBatchId] uniqueidentifier NULL;

    PRINT 'Added StagingBatchId column to PSTNs table';
END
ELSE
BEGIN
    PRINT 'StagingBatchId column already exists in PSTNs table';
END
GO

-- Add to PrivateWires table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PrivateWires]') AND name = 'StagingBatchId')
BEGIN
    ALTER TABLE [dbo].[PrivateWires]
    ADD [StagingBatchId] uniqueidentifier NULL;

    PRINT 'Added StagingBatchId column to PrivateWires table';
END
ELSE
BEGIN
    PRINT 'StagingBatchId column already exists in PrivateWires table';
END
GO

-- Add to Safaricom table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Safaricom]') AND name = 'StagingBatchId')
BEGIN
    ALTER TABLE [dbo].[Safaricom]
    ADD [StagingBatchId] uniqueidentifier NULL;

    PRINT 'Added StagingBatchId column to Safaricom table';
END
ELSE
BEGIN
    PRINT 'StagingBatchId column already exists in Safaricom table';
END
GO

-- Add to Airtel table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Airtel]') AND name = 'StagingBatchId')
BEGIN
    ALTER TABLE [dbo].[Airtel]
    ADD [StagingBatchId] uniqueidentifier NULL;

    PRINT 'Added StagingBatchId column to Airtel table';
END
ELSE
BEGIN
    PRINT 'StagingBatchId column already exists in Airtel table';
END
GO

PRINT 'StagingBatchId column migration completed successfully!';