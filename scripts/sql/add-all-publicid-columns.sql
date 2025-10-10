-- Add PublicId to UserPhones table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[UserPhones]') AND name = 'PublicId')
BEGIN
    ALTER TABLE [UserPhones] ADD [PublicId] uniqueidentifier NOT NULL DEFAULT NEWID();
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_UserPhones_PublicId' AND object_id = OBJECT_ID('[dbo].[UserPhones]'))
BEGIN
    CREATE UNIQUE INDEX [IX_UserPhones_PublicId] ON [UserPhones] ([PublicId]);
END
GO

-- Add PublicId to EbillUsers table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[EbillUsers]') AND name = 'PublicId')
BEGIN
    ALTER TABLE [EbillUsers] ADD [PublicId] uniqueidentifier NOT NULL DEFAULT NEWID();
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_EbillUsers_PublicId' AND object_id = OBJECT_ID('[dbo].[EbillUsers]'))
BEGIN
    CREATE UNIQUE INDEX [IX_EbillUsers_PublicId] ON [EbillUsers] ([PublicId]);
END
GO

-- Add PublicId to ServiceProviders table if exists
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ServiceProviders]') AND type in (N'U'))
BEGIN
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[ServiceProviders]') AND name = 'PublicId')
    BEGIN
        ALTER TABLE [ServiceProviders] ADD [PublicId] uniqueidentifier NOT NULL DEFAULT NEWID();
    END

    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ServiceProviders_PublicId' AND object_id = OBJECT_ID('[dbo].[ServiceProviders]'))
    BEGIN
        CREATE UNIQUE INDEX [IX_ServiceProviders_PublicId] ON [ServiceProviders] ([PublicId]);
    END
END
GO