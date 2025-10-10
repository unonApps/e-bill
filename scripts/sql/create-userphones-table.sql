-- Create UserPhones table for Entity Framework
-- This allows multiple phone numbers per user

USE [TABDB]
GO

-- Create UserPhones table if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[UserPhones]'))
BEGIN
    CREATE TABLE [dbo].[UserPhones] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [IndexNumber] NVARCHAR(50) NOT NULL,
        [PhoneNumber] NVARCHAR(20) NOT NULL,
        [PhoneType] NVARCHAR(50) NOT NULL DEFAULT 'Mobile',
        [IsPrimary] BIT NOT NULL DEFAULT 0,
        [IsActive] BIT NOT NULL DEFAULT 1,
        [AssignedDate] DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
        [UnassignedDate] DATETIME2(7) NULL,
        [Location] NVARCHAR(200) NULL,
        [Notes] NVARCHAR(500) NULL,
        [CreatedBy] NVARCHAR(100) NULL,
        [CreatedDate] DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),

        CONSTRAINT [PK_UserPhones] PRIMARY KEY CLUSTERED ([Id] ASC),

        CONSTRAINT [FK_UserPhones_EbillUsers_IndexNumber] FOREIGN KEY([IndexNumber])
            REFERENCES [dbo].[EbillUsers] ([IndexNumber]) ON DELETE CASCADE
    );

    -- Create indexes
    CREATE NONCLUSTERED INDEX [IX_UserPhones_IndexNumber] ON [dbo].[UserPhones]([IndexNumber]);
    CREATE NONCLUSTERED INDEX [IX_UserPhones_PhoneNumber] ON [dbo].[UserPhones]([PhoneNumber]);
    CREATE NONCLUSTERED INDEX [IX_UserPhones_IndexNumber_PhoneNumber_IsActive] ON [dbo].[UserPhones]([IndexNumber], [PhoneNumber], [IsActive]);

    PRINT 'UserPhones table created successfully';
END
ELSE
BEGIN
    PRINT 'UserPhones table already exists';
END
GO

-- Add to EF Migrations table to mark as applied
IF NOT EXISTS (SELECT * FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] = '20250122_AddUserPhonesTable')
BEGIN
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250122_AddUserPhonesTable', N'8.0.0');

    PRINT 'Migration marked as applied';
END
GO

PRINT 'UserPhones table setup complete';
GO