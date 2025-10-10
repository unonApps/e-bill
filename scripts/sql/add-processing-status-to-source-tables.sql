-- ================================================================
-- ADD PROCESSING STATUS TO SOURCE TELECOM TABLES
-- ================================================================
-- This enables tracking which records have been processed
-- and prevents duplicate processing

USE TABDB;
GO

SET NOCOUNT ON;
PRINT '================================================================';
PRINT 'ADDING PROCESSING STATUS TO SOURCE TABLES';
PRINT '================================================================';
PRINT '';

-- ================================================================
-- 1. ADD COLUMNS TO SAFARICOM TABLE
-- ================================================================
PRINT '1. Updating Safaricom table...';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Safaricom]') AND name = 'ProcessingStatus')
BEGIN
    ALTER TABLE Safaricom ADD
        ProcessingStatus INT DEFAULT 0,  -- 0=New, 1=Staged, 2=Processed, 3=Archived
        ProcessedDate DATETIME NULL,
        StagingBatchId UNIQUEIDENTIFIER NULL;

    PRINT '   Added ProcessingStatus columns to Safaricom';
END
ELSE
    PRINT '   ProcessingStatus already exists in Safaricom';

-- ================================================================
-- 2. ADD COLUMNS TO AIRTEL TABLE
-- ================================================================
PRINT '2. Updating Airtel table...';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Airtel]') AND name = 'ProcessingStatus')
BEGIN
    ALTER TABLE Airtel ADD
        ProcessingStatus INT DEFAULT 0,
        ProcessedDate DATETIME NULL,
        StagingBatchId UNIQUEIDENTIFIER NULL;

    PRINT '   Added ProcessingStatus columns to Airtel';
END
ELSE
    PRINT '   ProcessingStatus already exists in Airtel';

-- ================================================================
-- 3. ADD COLUMNS TO PSTNS TABLE
-- ================================================================
PRINT '3. Updating PSTNs table...';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PSTNs]') AND name = 'ProcessingStatus')
BEGIN
    ALTER TABLE PSTNs ADD
        ProcessingStatus INT DEFAULT 0,
        ProcessedDate DATETIME NULL,
        StagingBatchId UNIQUEIDENTIFIER NULL;

    PRINT '   Added ProcessingStatus columns to PSTNs';
END
ELSE
    PRINT '   ProcessingStatus already exists in PSTNs';

-- ================================================================
-- 4. ADD COLUMNS TO PRIVATEWIRES TABLE
-- ================================================================
PRINT '4. Updating PrivateWires table...';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PrivateWires]') AND name = 'ProcessingStatus')
BEGIN
    ALTER TABLE PrivateWires ADD
        ProcessingStatus INT DEFAULT 0,
        ProcessedDate DATETIME NULL,
        StagingBatchId UNIQUEIDENTIFIER NULL;

    PRINT '   Added ProcessingStatus columns to PrivateWires';
END
ELSE
    PRINT '   ProcessingStatus already exists in PrivateWires';

PRINT '';

-- ================================================================
-- 5. CREATE INDEXES FOR PERFORMANCE
-- ================================================================
PRINT '5. Creating indexes for better query performance...';

-- Safaricom
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Safaricom_ProcessingStatus')
BEGIN
    CREATE INDEX IX_Safaricom_ProcessingStatus
    ON Safaricom(ProcessingStatus)
    INCLUDE(CallDate, StagingBatchId);
    PRINT '   Created index on Safaricom.ProcessingStatus';
END

-- Airtel
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Airtel_ProcessingStatus')
BEGIN
    CREATE INDEX IX_Airtel_ProcessingStatus
    ON Airtel(ProcessingStatus)
    INCLUDE(call_date, StagingBatchId);
    PRINT '   Created index on Airtel.ProcessingStatus';
END

-- PSTNs
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_PSTNs_ProcessingStatus')
BEGIN
    CREATE INDEX IX_PSTNs_ProcessingStatus
    ON PSTNs(ProcessingStatus)
    INCLUDE(CallDate, StagingBatchId);
    PRINT '   Created index on PSTNs.ProcessingStatus';
END

-- PrivateWires
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_PrivateWires_ProcessingStatus')
BEGIN
    CREATE INDEX IX_PrivateWires_ProcessingStatus
    ON PrivateWires(ProcessingStatus)
    INCLUDE(CallDate, StagingBatchId);
    PRINT '   Created index on PrivateWires.ProcessingStatus';
END

PRINT '';

-- ================================================================
-- 6. CREATE CLEANUP STORED PROCEDURE
-- ================================================================
PRINT '6. Creating cleanup stored procedure...';
GO

IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'sp_CleanupProcessedCallLogs')
    DROP PROCEDURE sp_CleanupProcessedCallLogs;
GO

CREATE PROCEDURE sp_CleanupProcessedCallLogs
    @DaysToKeep INT = 30,  -- Keep processed records for 30 days
    @BatchSize INT = 1000,  -- Delete in batches to avoid locking
    @TestMode BIT = 1       -- 1 = Show what would be deleted, 0 = Actually delete
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @CutoffDate DATETIME = DATEADD(day, -@DaysToKeep, GETDATE());
    DECLARE @DeletedCount INT;
    DECLARE @TotalDeleted INT = 0;

    PRINT 'Cleanup Process Started: ' + CONVERT(VARCHAR, GETDATE(), 120);
    PRINT 'Cutoff Date: ' + CONVERT(VARCHAR, @CutoffDate, 120);
    PRINT 'Mode: ' + CASE WHEN @TestMode = 1 THEN 'TEST (no deletions)' ELSE 'PRODUCTION (will delete)' END;
    PRINT '';

    -- Count records to be deleted
    SELECT
        'Safaricom' as TableName,
        COUNT(*) as RecordsToDelete,
        MIN(ProcessedDate) as OldestRecord,
        MAX(ProcessedDate) as NewestRecord
    FROM Safaricom
    WHERE ProcessingStatus = 2 AND ProcessedDate < @CutoffDate

    UNION ALL

    SELECT 'Airtel', COUNT(*), MIN(ProcessedDate), MAX(ProcessedDate)
    FROM Airtel
    WHERE ProcessingStatus = 2 AND ProcessedDate < @CutoffDate

    UNION ALL

    SELECT 'PSTNs', COUNT(*), MIN(ProcessedDate), MAX(ProcessedDate)
    FROM PSTNs
    WHERE ProcessingStatus = 2 AND ProcessedDate < @CutoffDate

    UNION ALL

    SELECT 'PrivateWires', COUNT(*), MIN(ProcessedDate), MAX(ProcessedDate)
    FROM PrivateWires
    WHERE ProcessingStatus = 2 AND ProcessedDate < @CutoffDate;

    IF @TestMode = 0
    BEGIN
        -- Actually delete records

        -- Safaricom
        WHILE 1 = 1
        BEGIN
            DELETE TOP(@BatchSize) FROM Safaricom
            WHERE ProcessingStatus = 2 AND ProcessedDate < @CutoffDate;

            SET @DeletedCount = @@ROWCOUNT;
            SET @TotalDeleted = @TotalDeleted + @DeletedCount;

            IF @DeletedCount = 0 BREAK;
            WAITFOR DELAY '00:00:01'; -- Brief pause between batches
        END
        PRINT 'Deleted ' + CAST(@TotalDeleted AS VARCHAR) + ' records from Safaricom';

        -- Repeat for other tables...
    END

    PRINT '';
    PRINT 'Cleanup Process Completed: ' + CONVERT(VARCHAR, GETDATE(), 120);
END
GO

PRINT '   Created sp_CleanupProcessedCallLogs procedure';
PRINT '';

-- ================================================================
-- 7. CREATE MONITORING VIEW
-- ================================================================
PRINT '7. Creating monitoring view...';
GO

IF EXISTS (SELECT * FROM sys.views WHERE name = 'vw_CallLogProcessingStatus')
    DROP VIEW vw_CallLogProcessingStatus;
GO

CREATE VIEW vw_CallLogProcessingStatus
AS
SELECT
    'Safaricom' as SourceTable,
    ProcessingStatus,
    CASE ProcessingStatus
        WHEN 0 THEN 'New'
        WHEN 1 THEN 'Staged'
        WHEN 2 THEN 'Processed'
        WHEN 3 THEN 'Archived'
        ELSE 'Unknown'
    END as StatusName,
    COUNT(*) as RecordCount,
    MIN(CreatedDate) as OldestRecord,
    MAX(CreatedDate) as NewestRecord,
    SUM(CAST(Cost as DECIMAL(18,2))) as TotalCost
FROM Safaricom
GROUP BY ProcessingStatus

UNION ALL

SELECT
    'Airtel',
    ProcessingStatus,
    CASE ProcessingStatus
        WHEN 0 THEN 'New'
        WHEN 1 THEN 'Staged'
        WHEN 2 THEN 'Processed'
        WHEN 3 THEN 'Archived'
        ELSE 'Unknown'
    END,
    COUNT(*),
    MIN(CreatedDate),
    MAX(CreatedDate),
    SUM(CAST(cost as DECIMAL(18,2)))
FROM Airtel
GROUP BY ProcessingStatus

UNION ALL

SELECT
    'PSTNs',
    ProcessingStatus,
    CASE ProcessingStatus
        WHEN 0 THEN 'New'
        WHEN 1 THEN 'Staged'
        WHEN 2 THEN 'Processed'
        WHEN 3 THEN 'Archived'
        ELSE 'Unknown'
    END,
    COUNT(*),
    MIN(CreatedDate),
    MAX(CreatedDate),
    SUM(AmountKSH)
FROM PSTNs
GROUP BY ProcessingStatus

UNION ALL

SELECT
    'PrivateWires',
    ProcessingStatus,
    CASE ProcessingStatus
        WHEN 0 THEN 'New'
        WHEN 1 THEN 'Staged'
        WHEN 2 THEN 'Processed'
        WHEN 3 THEN 'Archived'
        ELSE 'Unknown'
    END,
    COUNT(*),
    MIN(CreatedDate),
    MAX(CreatedDate),
    SUM(AmountUSD * 150) -- Convert to KSH for consistency
FROM PrivateWires
GROUP BY ProcessingStatus;
GO

PRINT '   Created vw_CallLogProcessingStatus view';
PRINT '';

-- ================================================================
-- 8. SHOW CURRENT STATUS
-- ================================================================
PRINT '8. Current Processing Status:';
PRINT '------------------------------';

SELECT * FROM vw_CallLogProcessingStatus
ORDER BY SourceTable, ProcessingStatus;

PRINT '';
PRINT '================================================================';
PRINT 'PROCESSING STATUS SETUP COMPLETED';
PRINT '================================================================';
PRINT '';
PRINT 'Next Steps:';
PRINT '-----------';
PRINT '1. Update CallLogStagingService to use ProcessingStatus';
PRINT '2. Schedule sp_CleanupProcessedCallLogs to run daily';
PRINT '3. Monitor using vw_CallLogProcessingStatus';
PRINT '';
PRINT 'Usage:';
PRINT '------';
PRINT '-- Test cleanup (shows what would be deleted):';
PRINT 'EXEC sp_CleanupProcessedCallLogs @DaysToKeep=30, @TestMode=1;';
PRINT '';
PRINT '-- Actual cleanup:';
PRINT 'EXEC sp_CleanupProcessedCallLogs @DaysToKeep=30, @TestMode=0;';
PRINT '';
PRINT '-- Monitor status:';
PRINT 'SELECT * FROM vw_CallLogProcessingStatus;';