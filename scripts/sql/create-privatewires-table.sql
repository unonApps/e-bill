-- Create PrivateWires table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PrivateWires]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[PrivateWires] (
        [Id] int IDENTITY(1,1) NOT NULL,
        [Extension] nvarchar(50) NULL,
        [DestinationLine] nvarchar(50) NULL,
        [DurationExtended] decimal(18,2) NULL,
        [DialedNumber] nvarchar(100) NULL,
        [CallTime] time NULL,
        [Destination] nvarchar(200) NULL,
        [AmountUSD] decimal(18,2) NULL,
        [CallDate] datetime2 NULL,
        [Duration] decimal(18,2) NULL,
        [IndexNumber] nvarchar(50) NULL,
        [CreatedDate] datetime2 NOT NULL CONSTRAINT [DF_PrivateWires_CreatedDate] DEFAULT (GETUTCDATE()),
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedDate] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [ImportAuditId] int NULL,
        [EbillUserId] int NULL,
        [ProcessingStatus] int NULL CONSTRAINT [DF_PrivateWires_ProcessingStatus] DEFAULT (0),
        [ProcessedDate] datetime NULL,
        [StagingBatchId] uniqueidentifier NULL,
        [BillingPeriod] nvarchar(20) NULL,
        [CallMonth] int NOT NULL CONSTRAINT [DF_PrivateWires_CallMonth] DEFAULT (1),
        [CallYear] int NOT NULL CONSTRAINT [DF_PrivateWires_CallYear] DEFAULT (2024),
        CONSTRAINT [PK_PrivateWires] PRIMARY KEY CLUSTERED ([Id] ASC)
    );

    -- Create foreign key to ImportAudits
    IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ImportAudits]') AND type in (N'U'))
    BEGIN
        ALTER TABLE [dbo].[PrivateWires]
        ADD CONSTRAINT [FK_PrivateWires_ImportAudits_ImportAuditId]
        FOREIGN KEY ([ImportAuditId]) REFERENCES [ImportAudits] ([Id]);
    END

    -- Create foreign key to EbillUsers
    IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[EbillUsers]') AND type in (N'U'))
    BEGIN
        ALTER TABLE [dbo].[PrivateWires]
        ADD CONSTRAINT [FK_PrivateWires_EbillUsers_EbillUserId]
        FOREIGN KEY ([EbillUserId]) REFERENCES [EbillUsers] ([Id]);
    END

    -- Create indexes
    CREATE INDEX [IX_PrivateWires_ImportAuditId] ON [PrivateWires] ([ImportAuditId]);
    CREATE INDEX [IX_PrivateWires_EbillUserId] ON [PrivateWires] ([EbillUserId]);
    CREATE INDEX [IX_PrivateWires_IndexNumber] ON [PrivateWires] ([IndexNumber]);
    CREATE INDEX [IX_PrivateWires_CallDate] ON [PrivateWires] ([CallDate]);
    CREATE INDEX [IX_PrivateWires_ProcessingStatus] ON [PrivateWires] ([ProcessingStatus]);

    PRINT 'PrivateWires table created successfully';
END
ELSE
BEGIN
    PRINT 'PrivateWires table already exists';
END
GO