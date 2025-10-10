-- Script to create Airtel table for Airtel telecommunications records
-- Run this in SQL Server Management Studio or via sqlcmd

USE [TABDB];
GO

-- Check if Airtel table exists and create it if not
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Airtel]') AND type in (N'U'))
BEGIN
    PRINT 'Creating Airtel table...';

    CREATE TABLE [dbo].[Airtel] (
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

        CONSTRAINT [PK_Airtel] PRIMARY KEY CLUSTERED ([Id] ASC)
    );

    PRINT 'Airtel table created successfully.';
END
ELSE
BEGIN
    PRINT 'Airtel table already exists.';
END
GO

-- Create foreign key to ImportAudits if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Airtel_ImportAudits_ImportAuditId]'))
BEGIN
    ALTER TABLE [dbo].[Airtel] WITH CHECK ADD CONSTRAINT [FK_Airtel_ImportAudits_ImportAuditId]
    FOREIGN KEY([ImportAuditId]) REFERENCES [dbo].[ImportAudits] ([Id]);

    PRINT 'Foreign key constraint added.';
END
GO

-- Create indexes for performance
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Airtel_ImportAuditId' AND object_id = OBJECT_ID('[dbo].[Airtel]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Airtel_ImportAuditId] ON [dbo].[Airtel]
    (
        [ImportAuditId] ASC
    );
    PRINT 'Index created on ImportAuditId.';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Airtel_CallDate' AND object_id = OBJECT_ID('[dbo].[Airtel]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Airtel_CallDate] ON [dbo].[Airtel]
    (
        [call_date] ASC
    );
    PRINT 'Index created on call_date.';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Airtel_Ext' AND object_id = OBJECT_ID('[dbo].[Airtel]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Airtel_Ext] ON [dbo].[Airtel]
    (
        [ext] ASC
    );
    PRINT 'Index created on ext.';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Airtel_CallMonth_CallYear' AND object_id = OBJECT_ID('[dbo].[Airtel]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Airtel_CallMonth_CallYear] ON [dbo].[Airtel]
    (
        [call_month] ASC,
        [call_year] ASC
    );
    PRINT 'Index created on call_month and call_year.';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Airtel_Dialed' AND object_id = OBJECT_ID('[dbo].[Airtel]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Airtel_Dialed] ON [dbo].[Airtel]
    (
        [dialed] ASC
    );
    PRINT 'Index created on dialed number.';
END
GO

PRINT 'Airtel table setup completed successfully!';
GO