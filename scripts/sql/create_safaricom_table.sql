-- Script to create Safaricom table for Safaricom telecommunications records
-- Run this in SQL Server Management Studio or via sqlcmd

USE [TABDB];
GO

-- Check if Safaricom table exists and create it if not
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Safaricom]') AND type in (N'U'))
BEGIN
    PRINT 'Creating Safaricom table...';

    CREATE TABLE [dbo].[Safaricom] (
        [Id] int IDENTITY(1,1) NOT NULL,

        -- Core call data fields matching source table
        [ext] nvarchar(50) NULL,
        [call_date] datetime2(7) NULL,
        [call_time] time(7) NULL,
        [dialed] nvarchar(100) NULL,
        [dest] nvarchar(200) NULL,
        [durx] decimal(18, 2) NULL,
        [cost] decimal(18, 2) NULL,
        [dur] decimal(18, 2) NULL,
        [call_type] nvarchar(50) NULL,
        [call_month] int NULL,
        [call_year] int NULL,
        [source] nvarchar(100) NULL,

        -- Additional organizational fields
        [Organization] nvarchar(100) NULL,
        [Office] nvarchar(100) NULL,
        [Department] nvarchar(100) NULL,
        [UserName] nvarchar(200) NULL,
        [IndexNumber] nvarchar(50) NULL,

        -- Audit fields
        [CreatedDate] datetime2(7) NOT NULL DEFAULT GETUTCDATE(),
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedDate] datetime2(7) NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [ImportAuditId] int NULL,

        CONSTRAINT [PK_Safaricom] PRIMARY KEY CLUSTERED ([Id] ASC)
    );

    PRINT 'Safaricom table created successfully.';
END
ELSE
BEGIN
    PRINT 'Safaricom table already exists.';
END
GO

-- Create foreign key to ImportAudits if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Safaricom_ImportAudits_ImportAuditId]'))
BEGIN
    ALTER TABLE [dbo].[Safaricom] WITH CHECK ADD CONSTRAINT [FK_Safaricom_ImportAudits_ImportAuditId]
    FOREIGN KEY([ImportAuditId]) REFERENCES [dbo].[ImportAudits] ([Id]);

    PRINT 'Foreign key constraint added.';
END
GO

-- Create indexes for performance
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Safaricom_ImportAuditId' AND object_id = OBJECT_ID('[dbo].[Safaricom]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Safaricom_ImportAuditId] ON [dbo].[Safaricom]
    (
        [ImportAuditId] ASC
    );
    PRINT 'Index created on ImportAuditId.';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Safaricom_CallDate' AND object_id = OBJECT_ID('[dbo].[Safaricom]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Safaricom_CallDate] ON [dbo].[Safaricom]
    (
        [call_date] ASC
    );
    PRINT 'Index created on call_date.';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Safaricom_Ext' AND object_id = OBJECT_ID('[dbo].[Safaricom]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Safaricom_Ext] ON [dbo].[Safaricom]
    (
        [ext] ASC
    );
    PRINT 'Index created on ext.';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Safaricom_CallMonth_CallYear' AND object_id = OBJECT_ID('[dbo].[Safaricom]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Safaricom_CallMonth_CallYear] ON [dbo].[Safaricom]
    (
        [call_month] ASC,
        [call_year] ASC
    );
    PRINT 'Index created on call_month and call_year.';
END
GO

PRINT 'Safaricom table setup completed successfully!';
GO