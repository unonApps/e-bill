-- =============================================
-- Add UserPhoneHistory Table to Azure SQL Database
-- Migration: 20251013070229_AddUserPhoneHistoryTable
-- =============================================

BEGIN TRANSACTION;

-- Check if table already exists
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'UserPhoneHistories' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    PRINT 'Creating UserPhoneHistories table...';

    -- Create the UserPhoneHistories table
    CREATE TABLE [dbo].[UserPhoneHistories] (
        [Id] int NOT NULL IDENTITY(1,1),
        [UserPhoneId] int NOT NULL,
        [Action] nvarchar(100) NOT NULL,
        [FieldChanged] nvarchar(100) NULL,
        [OldValue] nvarchar(500) NULL,
        [NewValue] nvarchar(500) NULL,
        [Description] nvarchar(1000) NULL,
        [ChangedBy] nvarchar(200) NULL,
        [ChangedDate] datetime2 NOT NULL,
        [IPAddress] nvarchar(50) NULL,
        [UserAgent] nvarchar(500) NULL,
        CONSTRAINT [PK_UserPhoneHistories] PRIMARY KEY ([Id])
    );

    PRINT 'UserPhoneHistories table created successfully.';
END
ELSE
BEGIN
    PRINT 'UserPhoneHistories table already exists.';
END

-- Check if foreign key exists before creating
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_UserPhoneHistories_UserPhones_UserPhoneId')
BEGIN
    PRINT 'Creating foreign key constraint...';

    ALTER TABLE [dbo].[UserPhoneHistories]
    ADD CONSTRAINT [FK_UserPhoneHistories_UserPhones_UserPhoneId]
    FOREIGN KEY ([UserPhoneId])
    REFERENCES [dbo].[UserPhones] ([Id])
    ON DELETE CASCADE;

    PRINT 'Foreign key constraint created successfully.';
END
ELSE
BEGIN
    PRINT 'Foreign key constraint already exists.';
END

-- Check if index exists before creating
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_UserPhoneHistories_UserPhoneId' AND object_id = OBJECT_ID('dbo.UserPhoneHistories'))
BEGIN
    PRINT 'Creating index on UserPhoneId...';

    CREATE INDEX [IX_UserPhoneHistories_UserPhoneId]
    ON [dbo].[UserPhoneHistories] ([UserPhoneId]);

    PRINT 'Index created successfully.';
END
ELSE
BEGIN
    PRINT 'Index already exists.';
END

-- Add migration record to track this migration
IF NOT EXISTS (SELECT * FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] = N'20251013070229_AddUserPhoneHistoryTable')
BEGIN
    PRINT 'Adding migration record to __EFMigrationsHistory...';

    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251013070229_AddUserPhoneHistoryTable', N'8.0.0');

    PRINT 'Migration record added successfully.';
END
ELSE
BEGIN
    PRINT 'Migration record already exists in __EFMigrationsHistory.';
END

-- Verify the table was created
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'UserPhoneHistories' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    PRINT '✓ SUCCESS: UserPhoneHistories table exists and is ready to use!';

    -- Show table structure
    SELECT
        COLUMN_NAME,
        DATA_TYPE,
        CHARACTER_MAXIMUM_LENGTH,
        IS_NULLABLE
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'UserPhoneHistories'
    ORDER BY ORDINAL_POSITION;
END
ELSE
BEGIN
    PRINT '✗ ERROR: UserPhoneHistories table was not created!';
END

COMMIT TRANSACTION;

PRINT 'Migration completed successfully!';
