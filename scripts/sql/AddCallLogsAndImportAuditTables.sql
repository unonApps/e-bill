-- This script creates both CallLogs and ImportAudits tables
-- Run this script in your database to add the new tables

-- 1. Create CallLogs table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='CallLogs' AND xtype='U')
BEGIN
    CREATE TABLE [dbo].[CallLogs] (
        [Id] int IDENTITY(1,1) NOT NULL,
        [AccountNo] nvarchar(20) NOT NULL,
        [SubAccountNo] nvarchar(50) NOT NULL,
        [SubAccountName] nvarchar(200) NOT NULL,
        [MSISDN] nvarchar(20) NOT NULL,
        [TaxInvoiceSummaryNo] nvarchar(50) NOT NULL DEFAULT '',
        [InvoiceNo] nvarchar(50) NOT NULL DEFAULT '',
        [InvoiceDate] datetime2 NOT NULL,
        [NetAccessFee] decimal(18,2) NOT NULL,
        [NetUsageLessTax] decimal(18,2) NOT NULL,
        [LessTaxes] decimal(18,2) NOT NULL,
        [VAT16] decimal(18,2) NULL,
        [Excise15] decimal(18,2) NULL,
        [GrossTotal] decimal(18,2) NOT NULL,
        [EbillUserId] int NULL,
        [ImportedBy] nvarchar(max) NULL,
        [ImportedDate] datetime2 NULL,
        [CreatedDate] datetime2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_CallLogs] PRIMARY KEY CLUSTERED ([Id] ASC)
    );

    -- Create foreign key to EbillUsers
    ALTER TABLE [dbo].[CallLogs]
    ADD CONSTRAINT [FK_CallLogs_EbillUsers_EbillUserId] 
    FOREIGN KEY ([EbillUserId]) 
    REFERENCES [dbo].[EbillUsers] ([Id])
    ON DELETE SET NULL;

    -- Create indexes for performance
    CREATE NONCLUSTERED INDEX [IX_CallLogs_MSISDN] 
    ON [dbo].[CallLogs] ([MSISDN] ASC);

    CREATE NONCLUSTERED INDEX [IX_CallLogs_EbillUserId] 
    ON [dbo].[CallLogs] ([EbillUserId] ASC);

    PRINT 'CallLogs table created successfully.';
END
ELSE
BEGIN
    PRINT 'CallLogs table already exists.';
END

-- 2. Create ImportAudits table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='ImportAudits' AND xtype='U')
BEGIN
    CREATE TABLE [dbo].[ImportAudits] (
        [Id] int IDENTITY(1,1) NOT NULL,
        [ImportType] nvarchar(50) NOT NULL,
        [FileName] nvarchar(200) NOT NULL,
        [FileSize] bigint NOT NULL,
        [TotalRecords] int NOT NULL,
        [SuccessCount] int NOT NULL,
        [SkippedCount] int NOT NULL,
        [ErrorCount] int NOT NULL,
        [UpdatedCount] int NOT NULL,
        [ImportDate] datetime2 NOT NULL,
        [ImportedBy] nvarchar(100) NOT NULL,
        [IpAddress] nvarchar(50) NULL,
        [ProcessingTime] time NOT NULL,
        [DetailedResults] nvarchar(max) NULL,
        [SummaryMessage] nvarchar(500) NULL,
        [ImportOptions] nvarchar(max) NULL,
        CONSTRAINT [PK_ImportAudits] PRIMARY KEY CLUSTERED ([Id] ASC)
    );

    -- Create indexes for performance
    CREATE NONCLUSTERED INDEX [IX_ImportAudits_ImportDate] 
    ON [dbo].[ImportAudits] ([ImportDate] ASC);

    CREATE NONCLUSTERED INDEX [IX_ImportAudits_ImportType] 
    ON [dbo].[ImportAudits] ([ImportType] ASC);

    PRINT 'ImportAudits table created successfully.';
END
ELSE
BEGIN
    PRINT 'ImportAudits table already exists.';
END

-- 3. Add migration history entries (if using EF Core)
IF EXISTS (SELECT * FROM sysobjects WHERE name='__EFMigrationsHistory' AND xtype='U')
BEGIN
    IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20250710151414_AddCallLogEntity')
    BEGIN
        INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
        VALUES ('20250710151414_AddCallLogEntity', '8.0.0');
        PRINT 'Added CallLog migration history entry.';
    END

    IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20250710152000_AddImportAuditEntity')
    BEGIN
        INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
        VALUES ('20250710152000_AddImportAuditEntity', '8.0.0');
        PRINT 'Added ImportAudit migration history entry.';
    END
END

PRINT 'Script completed successfully!';