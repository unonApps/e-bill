-- Mark the problematic migration as already applied
-- This will skip it when running update-database

USE TABDB;
GO

-- Check if migrations table exists and insert the migration entry
IF OBJECT_ID('[__EFMigrationsHistory]', 'U') IS NOT NULL
BEGIN
    -- Only insert if not already present
    IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE MigrationId = '20250923061236_AddUserPhonesTable')
    BEGIN
        INSERT INTO [__EFMigrationsHistory] (MigrationId, ProductVersion)
        VALUES ('20250923061236_AddUserPhonesTable', '8.0.0');
        PRINT 'Marked migration 20250923061236_AddUserPhonesTable as applied';
    END
    ELSE
    BEGIN
        PRINT 'Migration 20250923061236_AddUserPhonesTable already marked as applied';
    END
END
ELSE
BEGIN
    PRINT 'Migrations history table does not exist';
END
GO