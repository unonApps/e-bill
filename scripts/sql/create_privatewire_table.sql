-- Script to create PrivateWires table for Private Wire telecommunications records
-- Run this in SQL Server Management Studio or via sqlcmd

USE [TABDB];
GO

-- Check if PrivateWires table exists and create it if not
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PrivateWires]') AND type in (N'U'))
BEGIN
    PRINT 'Creating PrivateWires table...';

    CREATE TABLE [dbo].[PrivateWires] (
        [Id] int IDENTITY(1,1) NOT NULL,

        -- Call Origin Information
        [Extension] nvarchar(50) NULL,
        [DestinationLine] nvarchar(50) NULL,
        [DurationExtended] decimal(18, 2) NULL,

        -- Call Details
        [DialedNumber] nvarchar(100) NULL,
        [CallTime] time(7) NULL,
        [Destination] nvarchar(200) NULL,

        -- Billing Information
        [AmountUSD] decimal(18, 2) NULL,

        -- Organization Structure
        [Organization] nvarchar(100) NULL,
        [Office] nvarchar(100) NULL,
        [SubOffice] nvarchar(100) NULL,
        [Level4Unit] nvarchar(100) NULL,
        [OrganizationalUnit] nvarchar(100) NULL,

        -- Caller Information
        [CallerName] nvarchar(200) NULL,
        [CallDate] datetime2(7) NULL,
        [Duration] decimal(18, 2) NULL,
        [IndexNumber] nvarchar(50) NULL,
        [Location] nvarchar(200) NULL,
        [OCACode] nvarchar(50) NULL,

        -- Audit fields
        [CreatedDate] datetime2(7) NOT NULL DEFAULT GETUTCDATE(),
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedDate] datetime2(7) NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [ImportAuditId] int NULL,

        CONSTRAINT [PK_PrivateWires] PRIMARY KEY CLUSTERED ([Id] ASC)
    );

    PRINT 'PrivateWires table created successfully.';
END
ELSE
BEGIN
    PRINT 'PrivateWires table already exists.';
END
GO

-- Create foreign key to ImportAudits if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_PrivateWires_ImportAudits_ImportAuditId]'))
BEGIN
    ALTER TABLE [dbo].[PrivateWires] WITH CHECK ADD CONSTRAINT [FK_PrivateWires_ImportAudits_ImportAuditId]
    FOREIGN KEY([ImportAuditId]) REFERENCES [dbo].[ImportAudits] ([Id]);

    PRINT 'Foreign key constraint added.';
END
GO

-- Create index on ImportAuditId if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_PrivateWires_ImportAuditId' AND object_id = OBJECT_ID('[dbo].[PrivateWires]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_PrivateWires_ImportAuditId] ON [dbo].[PrivateWires]
    (
        [ImportAuditId] ASC
    );

    PRINT 'Index created on ImportAuditId.';
END
GO

-- Create additional indexes for performance
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_PrivateWires_CallDate' AND object_id = OBJECT_ID('[dbo].[PrivateWires]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_PrivateWires_CallDate] ON [dbo].[PrivateWires]
    (
        [CallDate] ASC
    );
    PRINT 'Index created on CallDate.';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_PrivateWires_Organization_Office' AND object_id = OBJECT_ID('[dbo].[PrivateWires]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_PrivateWires_Organization_Office] ON [dbo].[PrivateWires]
    (
        [Organization] ASC,
        [Office] ASC
    );
    PRINT 'Index created on Organization and Office.';
END
GO

PRINT 'PrivateWires table setup completed successfully!';