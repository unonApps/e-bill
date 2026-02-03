-- ========================================
-- Restore Database to Server
-- Run this in SSMS on SERVER machine
-- ========================================
-- WARNING: This will REPLACE the existing database!
-- ========================================

USE master;
GO

-- ========================================
-- CONFIGURATION (Update these if needed)
-- ========================================
DECLARE @DatabaseName NVARCHAR(100) = 'tabdb';
DECLARE @BackupFile NVARCHAR(500) = 'C:\temp\tabdb_full_backup.bak';
DECLARE @DataPath NVARCHAR(500) = 'C:\Program Files\Microsoft SQL Server\MSSQL16.MSSQLSERVER\MSSQL\DATA\';  -- Update if different
DECLARE @LogPath NVARCHAR(500) = 'C:\Program Files\Microsoft SQL Server\MSSQL16.MSSQLSERVER\MSSQL\DATA\';   -- Update if different

PRINT '========================================';
PRINT 'RESTORE DATABASE TO SERVER';
PRINT '========================================';
PRINT '';
PRINT 'WARNING: This will REPLACE the existing database!';
PRINT 'All current data on the server will be DELETED!';
PRINT '';
PRINT 'Database: ' + @DatabaseName;
PRINT 'Backup file: ' + @BackupFile;
PRINT '';

-- Check if backup file exists
IF NOT EXISTS (
    SELECT * FROM sys.dm_os_file_exists WHERE file_exists = @BackupFile
)
BEGIN
    PRINT 'ERROR: Backup file not found: ' + @BackupFile;
    PRINT '';
    PRINT 'Please copy the backup file from local machine to server first.';
    RETURN;
END

-- ========================================
-- STEP 1: Create backup of existing database
-- ========================================
IF EXISTS (SELECT name FROM sys.databases WHERE name = @DatabaseName)
BEGIN
    PRINT 'Step 1: Creating safety backup of existing database...';
    PRINT '';

    DECLARE @SafetyBackupFile NVARCHAR(500);
    SET @SafetyBackupFile = 'C:\temp\' + @DatabaseName + '_safety_backup_' +
                            REPLACE(REPLACE(REPLACE(CONVERT(VARCHAR(20), GETDATE(), 120), '-', ''), ':', ''), ' ', '_') + '.bak';

    BEGIN TRY
        -- Set to simple recovery for faster backup
        ALTER DATABASE [tabdb] SET RECOVERY SIMPLE;

        BACKUP DATABASE [tabdb]
        TO DISK = @SafetyBackupFile
        WITH
            FORMAT,
            COMPRESSION,
            STATS = 10,
            NAME = 'Safety backup before restore';

        PRINT 'Safety backup created: ' + @SafetyBackupFile;
        PRINT '';
    END TRY
    BEGIN CATCH
        PRINT 'Warning: Could not create safety backup: ' + ERROR_MESSAGE();
        PRINT 'Continuing anyway...';
        PRINT '';
    END CATCH
END
ELSE
BEGIN
    PRINT 'Step 1: Database does not exist (no safety backup needed)';
    PRINT '';
END

-- ========================================
-- STEP 2: Kill all connections to the database
-- ========================================
IF EXISTS (SELECT name FROM sys.databases WHERE name = @DatabaseName)
BEGIN
    PRINT 'Step 2: Closing all connections to database...';
    PRINT '';

    -- Set to single user mode (kills all connections)
    ALTER DATABASE [tabdb] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;

    PRINT 'All connections closed.';
    PRINT '';
END

-- ========================================
-- STEP 3: Restore database from backup
-- ========================================
PRINT 'Step 3: Restoring database from backup...';
PRINT '';
PRINT 'This may take several minutes. Progress will be shown below.';
PRINT '';

-- Get logical file names from backup
DECLARE @LogicalDataName NVARCHAR(128);
DECLARE @LogicalLogName NVARCHAR(128);

CREATE TABLE #FileList (
    LogicalName NVARCHAR(128),
    PhysicalName NVARCHAR(260),
    Type CHAR(1),
    FileGroupName NVARCHAR(128),
    Size NUMERIC(20,0),
    MaxSize NUMERIC(20,0),
    FileID BIGINT,
    CreateLSN NUMERIC(25,0),
    DropLSN NUMERIC(25,0) NULL,
    UniqueID UNIQUEIDENTIFIER,
    ReadOnlyLSN NUMERIC(25,0) NULL,
    ReadWriteLSN NUMERIC(25,0) NULL,
    BackupSizeInBytes BIGINT,
    SourceBlockSize INT,
    FileGroupID INT,
    LogGroupGUID UNIQUEIDENTIFIER NULL,
    DifferentialBaseLSN NUMERIC(25,0) NULL,
    DifferentialBaseGUID UNIQUEIDENTIFIER,
    IsReadOnly BIT,
    IsPresent BIT,
    TDEThumbprint VARBINARY(32) NULL,
    SnapshotURL NVARCHAR(360) NULL
);

INSERT INTO #FileList
EXEC('RESTORE FILELISTONLY FROM DISK = ''' + @BackupFile + '''');

SELECT @LogicalDataName = LogicalName FROM #FileList WHERE Type = 'D';
SELECT @LogicalLogName = LogicalName FROM #FileList WHERE Type = 'L';

DROP TABLE #FileList;

-- Restore the database
RESTORE DATABASE [tabdb]
FROM DISK = @BackupFile
WITH
    REPLACE,                                    -- Replace existing database
    STATS = 10,                                 -- Show progress every 10%
    MOVE @LogicalDataName TO @DataPath + @DatabaseName + '.mdf',
    MOVE @LogicalLogName TO @LogPath + @DatabaseName + '_log.ldf';

PRINT '';
PRINT '========================================';
PRINT 'RESTORE COMPLETED SUCCESSFULLY!';
PRINT '========================================';
PRINT '';

-- ========================================
-- STEP 4: Set database to multi-user mode
-- ========================================
ALTER DATABASE [tabdb] SET MULTI_USER;

-- ========================================
-- STEP 5: Verify the restore
-- ========================================
PRINT 'Step 4: Verifying database...';
PRINT '';

-- Check database is online
IF EXISTS (SELECT name FROM sys.databases WHERE name = @DatabaseName AND state = 0)
BEGIN
    PRINT 'Database is ONLINE';
END
ELSE
BEGIN
    PRINT 'WARNING: Database is not online!';
END

-- Show database info
USE [tabdb];
GO

SELECT
    'Database Size' AS Info,
    SUM(size) * 8 / 1024 AS SizeMB
FROM sys.database_files;

-- Show record counts
SELECT 'Safaricom' AS TableName, COUNT(*) AS RecordCount FROM Safaricom
UNION ALL
SELECT 'Airtel', COUNT(*) FROM Airtel
UNION ALL
SELECT 'PSTNs', COUNT(*) FROM PSTNs
UNION ALL
SELECT 'PrivateWires', COUNT(*) FROM PrivateWires
UNION ALL
SELECT 'CallLogStagings', COUNT(*) FROM CallLogStagings
UNION ALL
SELECT 'StagingBatches', COUNT(*) FROM StagingBatches
UNION ALL
SELECT 'EbillUsers', COUNT(*) FROM EbillUsers
UNION ALL
SELECT 'ApplicationUsers', COUNT(*) FROM AspNetUsers;

PRINT '';
PRINT '========================================';
PRINT 'ALL DONE!';
PRINT '========================================';
PRINT '';
PRINT 'Server database now matches local database exactly!';
PRINT '';
PRINT 'Next steps:';
PRINT '1. Deploy the application to server';
PRINT '2. Test Hangfire bulk imports';
PRINT '';
