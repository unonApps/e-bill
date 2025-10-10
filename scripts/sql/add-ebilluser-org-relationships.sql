-- Script to add Organization, Office, and SubOffice foreign keys to EbillUsers table
USE [TABDB];
GO

PRINT '============================================';
PRINT 'Adding Organization Relationships to EbillUsers';
PRINT '============================================';
PRINT '';

-- Step 1: Add OrganizationId column
PRINT 'Step 1: Adding OrganizationId column...';
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[EbillUsers]') AND name = 'OrganizationId')
BEGIN
    ALTER TABLE [dbo].[EbillUsers] ADD [OrganizationId] int NULL;
    PRINT 'OrganizationId column added to EbillUsers table.';
END
ELSE
BEGIN
    PRINT 'OrganizationId column already exists in EbillUsers table.';
END
GO

-- Step 2: Add OfficeId column
PRINT 'Step 2: Adding OfficeId column...';
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[EbillUsers]') AND name = 'OfficeId')
BEGIN
    ALTER TABLE [dbo].[EbillUsers] ADD [OfficeId] int NULL;
    PRINT 'OfficeId column added to EbillUsers table.';
END
ELSE
BEGIN
    PRINT 'OfficeId column already exists in EbillUsers table.';
END
GO

-- Step 3: Add SubOfficeId column
PRINT 'Step 3: Adding SubOfficeId column...';
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[EbillUsers]') AND name = 'SubOfficeId')
BEGIN
    ALTER TABLE [dbo].[EbillUsers] ADD [SubOfficeId] int NULL;
    PRINT 'SubOfficeId column added to EbillUsers table.';
END
ELSE
BEGIN
    PRINT 'SubOfficeId column already exists in EbillUsers table.';
END
GO

-- Step 4: Create foreign key constraints
PRINT '';
PRINT 'Step 4: Creating foreign key constraints...';

-- Foreign key for OrganizationId
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_EbillUsers_Organizations_OrganizationId]'))
BEGIN
    ALTER TABLE [dbo].[EbillUsers] WITH CHECK ADD CONSTRAINT [FK_EbillUsers_Organizations_OrganizationId]
    FOREIGN KEY([OrganizationId]) REFERENCES [dbo].[Organizations] ([Id])
    ON DELETE SET NULL;
    PRINT 'Foreign key created for EbillUsers -> Organizations.';
END
ELSE
BEGIN
    PRINT 'Foreign key already exists for EbillUsers -> Organizations.';
END
GO

-- Foreign key for OfficeId
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_EbillUsers_Offices_OfficeId]'))
BEGIN
    ALTER TABLE [dbo].[EbillUsers] WITH CHECK ADD CONSTRAINT [FK_EbillUsers_Offices_OfficeId]
    FOREIGN KEY([OfficeId]) REFERENCES [dbo].[Offices] ([Id])
    ON DELETE NO ACTION;
    PRINT 'Foreign key created for EbillUsers -> Offices.';
END
ELSE
BEGIN
    PRINT 'Foreign key already exists for EbillUsers -> Offices.';
END
GO

-- Foreign key for SubOfficeId
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_EbillUsers_SubOffices_SubOfficeId]'))
BEGIN
    ALTER TABLE [dbo].[EbillUsers] WITH CHECK ADD CONSTRAINT [FK_EbillUsers_SubOffices_SubOfficeId]
    FOREIGN KEY([SubOfficeId]) REFERENCES [dbo].[SubOffices] ([Id])
    ON DELETE NO ACTION;
    PRINT 'Foreign key created for EbillUsers -> SubOffices.';
END
ELSE
BEGIN
    PRINT 'Foreign key already exists for EbillUsers -> SubOffices.';
END
GO

-- Step 5: Create indexes for better query performance
PRINT '';
PRINT 'Step 5: Creating indexes...';

-- Index for OrganizationId
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_EbillUsers_OrganizationId' AND object_id = OBJECT_ID('[dbo].[EbillUsers]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_EbillUsers_OrganizationId] ON [dbo].[EbillUsers]
    ([OrganizationId] ASC);
    PRINT 'Index created on EbillUsers.OrganizationId.';
END
GO

-- Index for OfficeId
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_EbillUsers_OfficeId' AND object_id = OBJECT_ID('[dbo].[EbillUsers]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_EbillUsers_OfficeId] ON [dbo].[EbillUsers]
    ([OfficeId] ASC);
    PRINT 'Index created on EbillUsers.OfficeId.';
END
GO

-- Index for SubOfficeId
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_EbillUsers_SubOfficeId' AND object_id = OBJECT_ID('[dbo].[EbillUsers]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_EbillUsers_SubOfficeId] ON [dbo].[EbillUsers]
    ([SubOfficeId] ASC);
    PRINT 'Index created on EbillUsers.SubOfficeId.';
END
GO

-- Step 6: Update existing EbillUsers by matching Organization and Office strings
PRINT '';
PRINT 'Step 6: Linking existing EbillUsers with Organizations and Offices...';

-- Update OrganizationId based on Organization string
UPDATE eu
SET eu.OrganizationId = o.Id
FROM [dbo].[EbillUsers] eu
INNER JOIN [dbo].[Organizations] o ON eu.Organization = o.Code OR eu.Organization = o.Name
WHERE eu.OrganizationId IS NULL AND eu.Organization IS NOT NULL;

DECLARE @OrgMatched INT;
SELECT @OrgMatched = COUNT(*) FROM [dbo].[EbillUsers] WHERE OrganizationId IS NOT NULL;
PRINT 'EbillUsers matched with Organizations: ' + CAST(@OrgMatched AS VARCHAR(10));

-- Update OfficeId based on Office string and OrganizationId
UPDATE eu
SET eu.OfficeId = ofc.Id
FROM [dbo].[EbillUsers] eu
INNER JOIN [dbo].[Offices] ofc ON
    (eu.Office = ofc.Code OR eu.Office = ofc.Name)
    AND (eu.OrganizationId = ofc.OrganizationId OR eu.OrganizationId IS NULL)
WHERE eu.OfficeId IS NULL AND eu.Office IS NOT NULL;

DECLARE @OfficeMatched INT;
SELECT @OfficeMatched = COUNT(*) FROM [dbo].[EbillUsers] WHERE OfficeId IS NOT NULL;
PRINT 'EbillUsers matched with Offices: ' + CAST(@OfficeMatched AS VARCHAR(10));

-- Display summary
PRINT '';
PRINT '============================================';
PRINT 'Summary of EbillUser Relationships';
PRINT '============================================';

SELECT
    COUNT(*) as TotalUsers,
    COUNT(OrganizationId) as UsersWithOrganization,
    COUNT(OfficeId) as UsersWithOffice,
    COUNT(SubOfficeId) as UsersWithSubOffice,
    COUNT(CASE WHEN Organization IS NOT NULL AND OrganizationId IS NULL THEN 1 END) as UnmatchedOrganizations,
    COUNT(CASE WHEN Office IS NOT NULL AND OfficeId IS NULL THEN 1 END) as UnmatchedOffices
FROM [dbo].[EbillUsers];

-- Show unmatched organizations
PRINT '';
PRINT 'Unmatched Organization values in EbillUsers:';
SELECT DISTINCT Organization
FROM [dbo].[EbillUsers]
WHERE Organization IS NOT NULL
    AND OrganizationId IS NULL
ORDER BY Organization;

-- Show unmatched offices
PRINT '';
PRINT 'Unmatched Office values in EbillUsers:';
SELECT DISTINCT Office
FROM [dbo].[EbillUsers]
WHERE Office IS NOT NULL
    AND OfficeId IS NULL
ORDER BY Office;

PRINT '';
PRINT 'EbillUser organization relationships setup completed!';
PRINT 'Note: You may need to manually map unmatched organizations and offices.';
GO