-- Add UserPhoneId column to CallLogStagings table
USE TABDB;
GO

-- Check if column already exists
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[CallLogStagings]') AND name = 'UserPhoneId')
BEGIN
    ALTER TABLE [dbo].[CallLogStagings]
    ADD [UserPhoneId] INT NULL;

    PRINT 'Added UserPhoneId column to CallLogStagings table';
END
ELSE
BEGIN
    PRINT 'UserPhoneId column already exists in CallLogStagings table';
END
GO

-- Add foreign key constraint to UserPhones table (only if UserPhones table exists)
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'UserPhones')
BEGIN
    IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_CallLogStagings_UserPhones_UserPhoneId')
    BEGIN
        ALTER TABLE [dbo].[CallLogStagings]
        ADD CONSTRAINT FK_CallLogStagings_UserPhones_UserPhoneId
        FOREIGN KEY ([UserPhoneId]) REFERENCES [dbo].[UserPhones]([Id]);

        PRINT 'Added foreign key constraint FK_CallLogStagings_UserPhones_UserPhoneId';
    END
    ELSE
    BEGIN
        PRINT 'Foreign key constraint already exists';
    END
END
ELSE
BEGIN
    PRINT 'UserPhones table does not exist. Skipping foreign key creation.';
END
GO

-- Create index for better performance
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_CallLogStagings_UserPhoneId')
BEGIN
    CREATE INDEX IX_CallLogStagings_UserPhoneId
    ON [dbo].[CallLogStagings]([UserPhoneId]);

    PRINT 'Created index IX_CallLogStagings_UserPhoneId';
END
ELSE
BEGIN
    PRINT 'Index IX_CallLogStagings_UserPhoneId already exists';
END
GO