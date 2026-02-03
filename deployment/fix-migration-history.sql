-- ========================================
-- Fix Migration History - Keep All Data
-- Run this script on your database
-- ========================================

USE [TABWeb]; -- Change this to your database name if different
GO

PRINT '========================================';
PRINT 'Fixing Migration History';
PRINT '========================================';
PRINT '';

-- Step 1: Check if __EFMigrationsHistory table exists
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = '__EFMigrationsHistory')
BEGIN
    PRINT '[1/3] Creating __EFMigrationsHistory table...';

    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );

    PRINT '  ✓ __EFMigrationsHistory table created';
END
ELSE
BEGIN
    PRINT '[1/3] __EFMigrationsHistory table already exists';
END

PRINT '';

-- Step 2: Clean up any existing migration records
PRINT '[2/3] Cleaning up existing migration records...';

DELETE FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20251107094921_InitialCreate';

PRINT '  ✓ Old migration records removed';
PRINT '';

-- Step 3: Mark the current migration as applied
PRINT '[3/3] Marking migration as applied...';

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES ('20251107094921_InitialCreate', '8.0.0');

PRINT '  ✓ Migration marked as applied';
PRINT '';

-- Step 4: Verify
PRINT '========================================';
PRINT 'Verification';
PRINT '========================================';
PRINT '';

SELECT
    [MigrationId],
    [ProductVersion]
FROM [__EFMigrationsHistory];

PRINT '';
PRINT '========================================';
PRINT 'Migration history fixed successfully!';
PRINT '========================================';
PRINT '';
PRINT 'Next steps:';
PRINT '1. Uncomment the migration code in Program.cs';
PRINT '2. Restart your application';
PRINT '';
