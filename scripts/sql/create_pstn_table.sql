-- This script creates the PSTNs table for your database
-- Run this in SQL Server Management Studio or via sqlcmd

USE [YourDatabaseName]; -- Replace with your actual database name
GO

-- Check if PSTNs table exists and create it if not
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PSTNs]') AND type in (N'U'))
BEGIN
    PRINT 'Creating PSTNs table...';

    CREATE TABLE [dbo].[PSTNs] (
        [Id] int IDENTITY(1,1) NOT NULL,
        [Ext] nvarchar(50) NULL,
        [Dialed] nvarchar(100) NULL,
        [Time] time(7) NULL,
        [Dest] nvarchar(200) NULL,
        [Dl] nvarchar(50) NULL,
        [Durx] decimal(18, 2) NULL,
        [Org] nvarchar(100) NULL,
        [Office] nvarchar(100) NULL,
        [SubOffice] nvarchar(100) NULL,
        [Org_Unit] nvarchar(100) NULL,
        [Name] nvarchar(200) NULL,
        [Date] datetime2(7) NULL,
        [Dur] decimal(18, 2) NULL,
        [Kshs] decimal(18, 2) NULL,
        [Inde_] nvarchar(50) NULL,
        [Location] nvarchar(200) NULL,
        [Oca] nvarchar(50) NULL,
        [Car] nvarchar(50) NULL,
        [CreatedDate] datetime2(7) NOT NULL DEFAULT GETUTCDATE(),
        [CreatedBy] nvarchar(max) NULL,
        [ModifiedDate] datetime2(7) NULL,
        [ModifiedBy] nvarchar(max) NULL,
        [ImportAuditId] int NULL,
        CONSTRAINT [PK_PSTNs] PRIMARY KEY CLUSTERED ([Id] ASC)
    );

    PRINT 'PSTNs table created successfully.';
END
ELSE
BEGIN
    PRINT 'PSTNs table already exists.';
END
GO

-- Create foreign key to ImportAudits if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_PSTNs_ImportAudits_ImportAuditId]'))
BEGIN
    ALTER TABLE [dbo].[PSTNs] WITH CHECK ADD CONSTRAINT [FK_PSTNs_ImportAudits_ImportAuditId]
    FOREIGN KEY([ImportAuditId]) REFERENCES [dbo].[ImportAudits] ([Id]);

    PRINT 'Foreign key constraint added.';
END
GO

-- Create index on ImportAuditId if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_PSTNs_ImportAuditId' AND object_id = OBJECT_ID('[dbo].[PSTNs]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_PSTNs_ImportAuditId] ON [dbo].[PSTNs]
    (
        [ImportAuditId] ASC
    );

    PRINT 'Index created on ImportAuditId.';
END
GO

-- Add migration record so EF knows this has been applied
IF NOT EXISTS (SELECT * FROM __EFMigrationsHistory WHERE MigrationId = '20250922120000_AddPSTNTableProperly')
BEGIN
    INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20250922120000_AddPSTNTableProperly', '8.0.6');

    PRINT 'Migration record added to history.';
END
GO

PRINT 'Script completed successfully!';