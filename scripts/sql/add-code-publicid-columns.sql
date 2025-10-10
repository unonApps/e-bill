-- Add Code and PublicId columns to Organizations table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Organizations]') AND name = 'Code')
BEGIN
    ALTER TABLE [Organizations] ADD [Code] nvarchar(10) NULL;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Organizations]') AND name = 'PublicId')
BEGIN
    ALTER TABLE [Organizations] ADD [PublicId] uniqueidentifier NOT NULL DEFAULT NEWID();
END
GO

-- Add Code and PublicId columns to Offices table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Offices]') AND name = 'Code')
BEGIN
    ALTER TABLE [Offices] ADD [Code] nvarchar(10) NULL;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Offices]') AND name = 'PublicId')
BEGIN
    ALTER TABLE [Offices] ADD [PublicId] uniqueidentifier NOT NULL DEFAULT NEWID();
END
GO

-- Add Code and PublicId columns to SubOffices table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[SubOffices]') AND name = 'Code')
BEGIN
    ALTER TABLE [SubOffices] ADD [Code] nvarchar(10) NULL;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[SubOffices]') AND name = 'PublicId')
BEGIN
    ALTER TABLE [SubOffices] ADD [PublicId] uniqueidentifier NOT NULL DEFAULT NEWID();
END
GO

-- Create indexes on PublicId columns for better performance
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Organizations_PublicId' AND object_id = OBJECT_ID('[dbo].[Organizations]'))
BEGIN
    CREATE UNIQUE INDEX [IX_Organizations_PublicId] ON [Organizations] ([PublicId]);
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Offices_PublicId' AND object_id = OBJECT_ID('[dbo].[Offices]'))
BEGIN
    CREATE UNIQUE INDEX [IX_Offices_PublicId] ON [Offices] ([PublicId]);
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SubOffices_PublicId' AND object_id = OBJECT_ID('[dbo].[SubOffices]'))
BEGIN
    CREATE UNIQUE INDEX [IX_SubOffices_PublicId] ON [SubOffices] ([PublicId]);
END
GO