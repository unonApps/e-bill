-- Create Airtel table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Airtel]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Airtel] (
        [Id] int IDENTITY(1,1) NOT NULL,
        [ext] nvarchar(50) NULL,
        [call_date] datetime2 NULL,
        [call_time] time NULL,
        [dialed] nvarchar(100) NULL,
        [dest] nvarchar(200) NULL,
        [durx] decimal(18,2) NULL,
        [cost] decimal(18,2) NULL,
        [dur] decimal(18,2) NULL,
        [call_type] nvarchar(50) NULL,
        [call_month] int NULL,
        [call_year] int NULL,
        [IndexNumber] nvarchar(50) NULL,
        [CreatedDate] datetime2 NOT NULL CONSTRAINT [DF_Airtel_CreatedDate] DEFAULT (GETUTCDATE()),
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedDate] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [ImportAuditId] int NULL,
        [EbillUserId] int NULL,
        [ProcessingStatus] int NULL CONSTRAINT [DF_Airtel_ProcessingStatus] DEFAULT (0),
        [ProcessedDate] datetime NULL,
        [StagingBatchId] uniqueidentifier NULL,
        [BillingPeriod] nvarchar(20) NULL,
        CONSTRAINT [PK_Airtel] PRIMARY KEY CLUSTERED ([Id] ASC)
    );

    -- Create foreign keys
    IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ImportAudits]') AND type in (N'U'))
    BEGIN
        ALTER TABLE [dbo].[Airtel]
        ADD CONSTRAINT [FK_Airtel_ImportAudits_ImportAuditId]
        FOREIGN KEY ([ImportAuditId]) REFERENCES [ImportAudits] ([Id]);
    END

    IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[EbillUsers]') AND type in (N'U'))
    BEGIN
        ALTER TABLE [dbo].[Airtel]
        ADD CONSTRAINT [FK_Airtel_EbillUsers_EbillUserId]
        FOREIGN KEY ([EbillUserId]) REFERENCES [EbillUsers] ([Id]);
    END

    -- Create indexes
    CREATE INDEX [IX_Airtel_ImportAuditId] ON [Airtel] ([ImportAuditId]);
    CREATE INDEX [IX_Airtel_EbillUserId] ON [Airtel] ([EbillUserId]);
    CREATE INDEX [IX_Airtel_IndexNumber] ON [Airtel] ([IndexNumber]);
    CREATE INDEX [IX_Airtel_call_date] ON [Airtel] ([call_date]);
    CREATE INDEX [IX_Airtel_ProcessingStatus] ON [Airtel] ([ProcessingStatus]);

    PRINT 'Airtel table created successfully';
END
ELSE
BEGIN
    PRINT 'Airtel table already exists';
END
GO

-- Create Safaricom table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Safaricom]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Safaricom] (
        [Id] int IDENTITY(1,1) NOT NULL,
        [ext] nvarchar(50) NULL,
        [call_date] datetime2 NULL,
        [call_time] time NULL,
        [dialed] nvarchar(100) NULL,
        [dest] nvarchar(200) NULL,
        [durx] decimal(18,2) NULL,
        [cost] decimal(18,2) NULL,
        [dur] decimal(18,2) NULL,
        [call_type] nvarchar(50) NULL,
        [call_month] int NULL,
        [call_year] int NULL,
        [IndexNumber] nvarchar(50) NULL,
        [CreatedDate] datetime2 NOT NULL CONSTRAINT [DF_Safaricom_CreatedDate] DEFAULT (GETUTCDATE()),
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedDate] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [ImportAuditId] int NULL,
        [EbillUserId] int NULL,
        [ProcessingStatus] int NULL CONSTRAINT [DF_Safaricom_ProcessingStatus] DEFAULT (0),
        [ProcessedDate] datetime NULL,
        [StagingBatchId] uniqueidentifier NULL,
        [BillingPeriod] nvarchar(20) NULL,
        CONSTRAINT [PK_Safaricom] PRIMARY KEY CLUSTERED ([Id] ASC)
    );

    -- Create foreign keys
    IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ImportAudits]') AND type in (N'U'))
    BEGIN
        ALTER TABLE [dbo].[Safaricom]
        ADD CONSTRAINT [FK_Safaricom_ImportAudits_ImportAuditId]
        FOREIGN KEY ([ImportAuditId]) REFERENCES [ImportAudits] ([Id]);
    END

    IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[EbillUsers]') AND type in (N'U'))
    BEGIN
        ALTER TABLE [dbo].[Safaricom]
        ADD CONSTRAINT [FK_Safaricom_EbillUsers_EbillUserId]
        FOREIGN KEY ([EbillUserId]) REFERENCES [EbillUsers] ([Id]);
    END

    -- Create indexes
    CREATE INDEX [IX_Safaricom_ImportAuditId] ON [Safaricom] ([ImportAuditId]);
    CREATE INDEX [IX_Safaricom_EbillUserId] ON [Safaricom] ([EbillUserId]);
    CREATE INDEX [IX_Safaricom_IndexNumber] ON [Safaricom] ([IndexNumber]);
    CREATE INDEX [IX_Safaricom_call_date] ON [Safaricom] ([call_date]);
    CREATE INDEX [IX_Safaricom_ProcessingStatus] ON [Safaricom] ([ProcessingStatus]);

    PRINT 'Safaricom table created successfully';
END
ELSE
BEGIN
    PRINT 'Safaricom table already exists';
END
GO