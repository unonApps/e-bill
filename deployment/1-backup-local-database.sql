-- ========================================
-- Backup Local Database to BAK File
-- Run this in SSMS on LOCAL machine
-- ========================================

USE master;
GO

-- Check database exists
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'tabdb')
BEGIN
    PRINT 'ERROR: Database tabdb does not exist!';
    RETURN;
END

PRINT '========================================';
PRINT 'BACKING UP LOCAL DATABASE';
PRINT '========================================';
PRINT '';

-- Create backup folder if it doesn't exist
DECLARE @BackupPath NVARCHAR(500) = 'C:\temp\tabdb_full_backup.bak';

PRINT 'Backup file: ' + @BackupPath;
PRINT '';
PRINT 'Starting backup (this may take several minutes)...';
PRINT '';

-- Backup database with compression
BACKUP DATABASE [tabdb]
TO DISK = @BackupPath
WITH
    FORMAT,              -- Overwrite existing file
    COMPRESSION,         -- Compress the backup (smaller file)
    STATS = 10,          -- Show progress every 10%
    NAME = 'tabdb Full Backup',
    DESCRIPTION = 'Complete backup of tabdb database (schema + data)';

PRINT '';
PRINT '========================================';
PRINT 'BACKUP COMPLETED SUCCESSFULLY!';
PRINT '========================================';
PRINT '';

-- Show backup file info
RESTORE HEADERONLY FROM DISK = @BackupPath;

PRINT '';
PRINT 'Next steps:';
PRINT '1. Copy C:\temp\tabdb_full_backup.bak to the server';
PRINT '2. Run 2-restore-to-server.sql on the server';
PRINT '';
