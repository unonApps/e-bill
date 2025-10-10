-- Add Location column to EbillUsers table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[EbillUsers]') AND name = 'Location')
BEGIN
    ALTER TABLE [EbillUsers] ADD [Location] nvarchar(200) NULL;
END
GO

-- Add OrganizationId column to EbillUsers table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[EbillUsers]') AND name = 'OrganizationId')
BEGIN
    ALTER TABLE [EbillUsers] ADD [OrganizationId] int NULL;
END
GO

-- Add OfficeId column to EbillUsers table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[EbillUsers]') AND name = 'OfficeId')
BEGIN
    ALTER TABLE [EbillUsers] ADD [OfficeId] int NULL;
END
GO

-- Add SubOfficeId column to EbillUsers table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[EbillUsers]') AND name = 'SubOfficeId')
BEGIN
    ALTER TABLE [EbillUsers] ADD [SubOfficeId] int NULL;
END
GO

-- Create foreign key constraints
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_EbillUsers_Organizations_OrganizationId')
BEGIN
    ALTER TABLE [EbillUsers] ADD CONSTRAINT [FK_EbillUsers_Organizations_OrganizationId]
    FOREIGN KEY ([OrganizationId]) REFERENCES [Organizations] ([Id]);
END
GO

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_EbillUsers_Offices_OfficeId')
BEGIN
    ALTER TABLE [EbillUsers] ADD CONSTRAINT [FK_EbillUsers_Offices_OfficeId]
    FOREIGN KEY ([OfficeId]) REFERENCES [Offices] ([Id]);
END
GO

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_EbillUsers_SubOffices_SubOfficeId')
BEGIN
    ALTER TABLE [EbillUsers] ADD CONSTRAINT [FK_EbillUsers_SubOffices_SubOfficeId]
    FOREIGN KEY ([SubOfficeId]) REFERENCES [SubOffices] ([Id]);
END
GO

-- Create indexes for better performance
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_EbillUsers_OrganizationId' AND object_id = OBJECT_ID('[dbo].[EbillUsers]'))
BEGIN
    CREATE INDEX [IX_EbillUsers_OrganizationId] ON [EbillUsers] ([OrganizationId]);
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_EbillUsers_OfficeId' AND object_id = OBJECT_ID('[dbo].[EbillUsers]'))
BEGIN
    CREATE INDEX [IX_EbillUsers_OfficeId] ON [EbillUsers] ([OfficeId]);
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_EbillUsers_SubOfficeId' AND object_id = OBJECT_ID('[dbo].[EbillUsers]'))
BEGIN
    CREATE INDEX [IX_EbillUsers_SubOfficeId] ON [EbillUsers] ([SubOfficeId]);
END
GO