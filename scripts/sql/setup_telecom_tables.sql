-- Master setup script for PSTN and Private Wire tables
-- Run this script to create tables and seed initial data
-- Execute in SQL Server Management Studio or via sqlcmd

USE [TABDB];
GO

PRINT '============================================';
PRINT 'Setting up Telecom Tables (PSTN & Private Wire)';
PRINT '============================================';
PRINT '';

-- Step 1: Create PSTN table
PRINT 'Step 1: Creating PSTN table...';
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PSTNs]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[PSTNs] (
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
        [AmountKSH] decimal(18, 2) NULL,

        -- Organization Structure
        [Organization] nvarchar(100) NULL,
        [Office] nvarchar(100) NULL,
        [SubOffice] nvarchar(100) NULL,
        [OrganizationalUnit] nvarchar(100) NULL,

        -- Caller Information
        [CallerName] nvarchar(200) NULL,
        [CallDate] datetime2(7) NULL,
        [Duration] decimal(18, 2) NULL,
        [IndexNumber] nvarchar(50) NULL,
        [Location] nvarchar(200) NULL,
        [OCACode] nvarchar(50) NULL,
        [Carrier] nvarchar(100) NULL,

        -- Audit fields
        [CreatedDate] datetime2(7) NOT NULL DEFAULT GETUTCDATE(),
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedDate] datetime2(7) NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [ImportAuditId] int NULL,

        CONSTRAINT [PK_PSTNs] PRIMARY KEY CLUSTERED ([Id] ASC)
    );

    -- Create indexes for performance
    CREATE NONCLUSTERED INDEX [IX_PSTNs_CallDate] ON [dbo].[PSTNs] ([CallDate] ASC);
    CREATE NONCLUSTERED INDEX [IX_PSTNs_Organization_Office] ON [dbo].[PSTNs] ([Organization] ASC, [Office] ASC);
    CREATE NONCLUSTERED INDEX [IX_PSTNs_ImportAuditId] ON [dbo].[PSTNs] ([ImportAuditId] ASC);

    PRINT 'PSTN table created successfully.';
END
ELSE
BEGIN
    PRINT 'PSTN table already exists.';
END
GO

-- Step 2: Create PrivateWires table
PRINT '';
PRINT 'Step 2: Creating PrivateWires table...';
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PrivateWires]') AND type in (N'U'))
BEGIN
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

    -- Create indexes for performance
    CREATE NONCLUSTERED INDEX [IX_PrivateWires_CallDate] ON [dbo].[PrivateWires] ([CallDate] ASC);
    CREATE NONCLUSTERED INDEX [IX_PrivateWires_Organization_Office] ON [dbo].[PrivateWires] ([Organization] ASC, [Office] ASC);
    CREATE NONCLUSTERED INDEX [IX_PrivateWires_ImportAuditId] ON [dbo].[PrivateWires] ([ImportAuditId] ASC);

    PRINT 'PrivateWires table created successfully.';
END
ELSE
BEGIN
    PRINT 'PrivateWires table already exists.';
END
GO

-- Step 3: Add foreign key constraints (if ImportAudits table exists)
PRINT '';
PRINT 'Step 3: Adding foreign key constraints...';
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ImportAudits]') AND type in (N'U'))
BEGIN
    -- Foreign key for PSTNs
    IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_PSTNs_ImportAudits_ImportAuditId]'))
    BEGIN
        ALTER TABLE [dbo].[PSTNs] WITH CHECK ADD CONSTRAINT [FK_PSTNs_ImportAudits_ImportAuditId]
        FOREIGN KEY([ImportAuditId]) REFERENCES [dbo].[ImportAudits] ([Id]);
        PRINT 'Added foreign key constraint for PSTNs table.';
    END

    -- Foreign key for PrivateWires
    IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_PrivateWires_ImportAudits_ImportAuditId]'))
    BEGIN
        ALTER TABLE [dbo].[PrivateWires] WITH CHECK ADD CONSTRAINT [FK_PrivateWires_ImportAudits_ImportAuditId]
        FOREIGN KEY([ImportAuditId]) REFERENCES [dbo].[ImportAudits] ([Id]);
        PRINT 'Added foreign key constraint for PrivateWires table.';
    END
END
ELSE
BEGIN
    PRINT 'ImportAudits table not found. Skipping foreign key constraints.';
END
GO

-- Step 4: Seed PSTN data
PRINT '';
PRINT 'Step 4: Seeding PSTN data...';
IF NOT EXISTS (SELECT 1 FROM [dbo].[PSTNs])
BEGIN
    INSERT INTO [dbo].[PSTNs] (
        [Extension], [DialedNumber], [CallTime], [Destination],
        [Organization], [Office], [CallerName], [CallDate],
        [Duration], [AmountKSH], [IndexNumber], [Location], [Carrier],
        [CreatedDate], [CreatedBy]
    )
    VALUES
        ('2001', '0722123456', '08:30:00', 'Nairobi',
         'UNON', 'Nairobi', 'Alice Kamau', '2024-01-15',
         5.50, 55.00, 'UN10001', 'Gigiri Complex', 'Safaricom',
         GETUTCDATE(), 'setup_script'),

        ('2002', '0733987654', '09:15:00', 'Mombasa',
         'UNON', 'Nairobi', 'Brian Ochieng', '2024-01-16',
         8.25, 82.50, 'UN10002', 'Gigiri Complex', 'Airtel',
         GETUTCDATE(), 'setup_script'),

        ('2003', '+44207946000', '16:00:00', 'London, UK',
         'UNEP', 'Nairobi', 'Grace Akinyi', '2024-01-18',
         25.00, 1250.00, 'EP20002', 'UNEP HQ', 'Safaricom',
         GETUTCDATE(), 'setup_script');

    PRINT 'Inserted sample PSTN records.';
END
ELSE
BEGIN
    PRINT 'PSTN table already contains data.';
END
GO

-- Step 5: Seed Private Wire data
PRINT '';
PRINT 'Step 5: Seeding Private Wire data...';
IF NOT EXISTS (SELECT 1 FROM [dbo].[PrivateWires])
BEGIN
    INSERT INTO [dbo].[PrivateWires] (
        [Extension], [DialedNumber], [CallTime], [Destination],
        [AmountUSD], [Organization], [Office], [CallerName],
        [CallDate], [Duration], [IndexNumber], [Location],
        [CreatedDate], [CreatedBy]
    )
    VALUES
        ('2301', '+12125551234', '09:30:00', 'New York HQ',
         25.75, 'UNON', 'Nairobi', 'John Smith',
         '2024-01-15', 15.50, 'UN12345', 'Gigiri Complex',
         GETUTCDATE(), 'setup_script'),

        ('2302', '+41227910000', '14:15:00', 'Geneva Office',
         18.50, 'UNON', 'Nairobi', 'Mary Johnson',
         '2024-01-16', 8.25, 'UN12346', 'Gigiri Complex',
         GETUTCDATE(), 'setup_script'),

        ('2306', '+81352185000', '10:30:00', 'Tokyo Liaison Office',
         125.00, 'UNEP', 'Nairobi', 'Jennifer Lee',
         '2024-01-20', 45.00, 'EP78902', 'UNEP HQ',
         GETUTCDATE(), 'setup_script');

    PRINT 'Inserted sample Private Wire records.';
END
ELSE
BEGIN
    PRINT 'PrivateWires table already contains data.';
END
GO

-- Step 6: Display summary
PRINT '';
PRINT '============================================';
PRINT 'Setup Complete - Summary';
PRINT '============================================';

-- Check table existence and record counts
IF EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PSTNs]'))
BEGIN
    DECLARE @pstnCount int;
    SELECT @pstnCount = COUNT(*) FROM [dbo].[PSTNs];
    PRINT 'PSTNs table: ' + CAST(@pstnCount as varchar(10)) + ' records';
END

IF EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PrivateWires]'))
BEGIN
    DECLARE @pwCount int;
    SELECT @pwCount = COUNT(*) FROM [dbo].[PrivateWires];
    PRINT 'PrivateWires table: ' + CAST(@pwCount as varchar(10)) + ' records';
END

PRINT '';
PRINT 'Tables are ready for use!';
PRINT 'You can now import CSV data through the CallLogs page.';
GO