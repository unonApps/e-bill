-- Add Azure AD columns to AspNetUsers table

-- Add AzureAdObjectId column
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AspNetUsers]') AND name = 'AzureAdObjectId')
BEGIN
    ALTER TABLE [dbo].[AspNetUsers] ADD [AzureAdObjectId] nvarchar(100) NULL;
    PRINT 'Added AzureAdObjectId column';
END
ELSE
BEGIN
    PRINT 'AzureAdObjectId column already exists';
END
GO

-- Add AzureAdTenantId column
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AspNetUsers]') AND name = 'AzureAdTenantId')
BEGIN
    ALTER TABLE [dbo].[AspNetUsers] ADD [AzureAdTenantId] nvarchar(100) NULL;
    PRINT 'Added AzureAdTenantId column';
END
ELSE
BEGIN
    PRINT 'AzureAdTenantId column already exists';
END
GO

-- Add AzureAdUpn column
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AspNetUsers]') AND name = 'AzureAdUpn')
BEGIN
    ALTER TABLE [dbo].[AspNetUsers] ADD [AzureAdUpn] nvarchar(200) NULL;
    PRINT 'Added AzureAdUpn column';
END
ELSE
BEGIN
    PRINT 'AzureAdUpn column already exists';
END
GO

-- Add EbillUserId column (link to EbillUsers table)
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AspNetUsers]') AND name = 'EbillUserId')
BEGIN
    ALTER TABLE [dbo].[AspNetUsers] ADD [EbillUserId] int NULL;
    PRINT 'Added EbillUserId column';
END
ELSE
BEGIN
    PRINT 'EbillUserId column already exists';
END
GO

-- Add foreign key constraint if EbillUsers table exists
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'EbillUsers')
AND NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_AspNetUsers_EbillUsers_EbillUserId' AND parent_object_id = OBJECT_ID('AspNetUsers'))
BEGIN
    ALTER TABLE [dbo].[AspNetUsers]
    ADD CONSTRAINT [FK_AspNetUsers_EbillUsers_EbillUserId]
    FOREIGN KEY ([EbillUserId]) REFERENCES [dbo].[EbillUsers]([Id]);
    PRINT 'Added foreign key constraint';
END
GO

-- Add index on AzureAdObjectId for faster lookups
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_AspNetUsers_AzureAdObjectId' AND object_id = OBJECT_ID('AspNetUsers'))
BEGIN
    CREATE INDEX [IX_AspNetUsers_AzureAdObjectId] ON [dbo].[AspNetUsers]([AzureAdObjectId]);
    PRINT 'Added index on AzureAdObjectId';
END
GO

-- Add index on EbillUserId
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_AspNetUsers_EbillUserId' AND object_id = OBJECT_ID('AspNetUsers'))
BEGIN
    CREATE INDEX [IX_AspNetUsers_EbillUserId] ON [dbo].[AspNetUsers]([EbillUserId]);
    PRINT 'Added index on EbillUserId';
END
GO

PRINT 'Azure AD columns migration completed successfully!';
