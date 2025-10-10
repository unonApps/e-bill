-- Create CallLogStagings table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[CallLogStagings]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[CallLogStagings] (
        [Id] int IDENTITY(1,1) NOT NULL,
        [ExtensionNumber] nvarchar(50) NOT NULL CONSTRAINT [DF_CallLogStagings_ExtensionNumber] DEFAULT (''),
        [CallDate] datetime2 NOT NULL,
        [CallNumber] nvarchar(50) NOT NULL CONSTRAINT [DF_CallLogStagings_CallNumber] DEFAULT (''),
        [CallDestination] nvarchar(100) NOT NULL CONSTRAINT [DF_CallLogStagings_CallDestination] DEFAULT (''),
        [CallEndTime] datetime2 NOT NULL,
        [CallDuration] int NOT NULL CONSTRAINT [DF_CallLogStagings_CallDuration] DEFAULT (0),
        [CallCurrencyCode] nvarchar(10) NOT NULL CONSTRAINT [DF_CallLogStagings_CallCurrencyCode] DEFAULT (''),
        [CallCost] decimal(18,2) NOT NULL CONSTRAINT [DF_CallLogStagings_CallCost] DEFAULT (0),
        [CallCostUSD] decimal(18,2) NOT NULL CONSTRAINT [DF_CallLogStagings_CallCostUSD] DEFAULT (0),
        [CallCostKSHS] decimal(18,2) NOT NULL CONSTRAINT [DF_CallLogStagings_CallCostKSHS] DEFAULT (0),
        [CallType] nvarchar(50) NOT NULL CONSTRAINT [DF_CallLogStagings_CallType] DEFAULT (''),
        [CallDestinationType] nvarchar(50) NOT NULL CONSTRAINT [DF_CallLogStagings_CallDestinationType] DEFAULT (''),
        [CallYear] int NOT NULL,
        [CallMonth] int NOT NULL,
        [ResponsibleIndexNumber] nvarchar(50) NULL,
        [PayingIndexNumber] nvarchar(50) NULL,
        [SourceSystem] nvarchar(50) NOT NULL CONSTRAINT [DF_CallLogStagings_SourceSystem] DEFAULT (''),
        [SourceRecordId] nvarchar(100) NULL,
        [BatchId] uniqueidentifier NOT NULL,
        [ImportedDate] datetime2 NOT NULL CONSTRAINT [DF_CallLogStagings_ImportedDate] DEFAULT (GETUTCDATE()),
        [ImportedBy] nvarchar(100) NOT NULL CONSTRAINT [DF_CallLogStagings_ImportedBy] DEFAULT (''),
        [VerificationStatus] int NOT NULL CONSTRAINT [DF_CallLogStagings_VerificationStatus] DEFAULT (0),
        [VerificationDate] datetime2 NULL,
        [VerifiedBy] nvarchar(100) NULL,
        [VerificationNotes] nvarchar(MAX) NULL,
        [HasAnomalies] bit NOT NULL CONSTRAINT [DF_CallLogStagings_HasAnomalies] DEFAULT (0),
        [AnomalyTypes] nvarchar(MAX) NULL,
        [AnomalyDetails] nvarchar(MAX) NULL,
        [ProcessingStatus] int NOT NULL CONSTRAINT [DF_CallLogStagings_ProcessingStatus] DEFAULT (0),
        [ProcessedDate] datetime2 NULL,
        [ErrorDetails] nvarchar(MAX) NULL,
        [CreatedDate] datetime2 NOT NULL CONSTRAINT [DF_CallLogStagings_CreatedDate] DEFAULT (GETUTCDATE()),
        [ModifiedDate] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [UserPhoneId] int NULL,
        [BillingPeriodId] int NULL,
        [ImportType] nvarchar(20) NULL CONSTRAINT [DF_CallLogStagings_ImportType] DEFAULT ('MONTHLY'),
        [IsAdjustment] bit NULL CONSTRAINT [DF_CallLogStagings_IsAdjustment] DEFAULT (0),
        [OriginalRecordId] int NULL,
        [AdjustmentReason] nvarchar(500) NULL,
        CONSTRAINT [PK_CallLogStagings] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
    
    IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[StagingBatches]') AND type in (N'U'))
    BEGIN
        ALTER TABLE [dbo].[CallLogStagings]
        ADD CONSTRAINT [FK_CallLogStagings_StagingBatches_BatchId]
        FOREIGN KEY ([BatchId]) REFERENCES [StagingBatches] ([Id]);
    END

    IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[UserPhones]') AND type in (N'U'))
    BEGIN
        ALTER TABLE [dbo].[CallLogStagings]
        ADD CONSTRAINT [FK_CallLogStagings_UserPhones_UserPhoneId]
        FOREIGN KEY ([UserPhoneId]) REFERENCES [UserPhones] ([Id]);
    END

    IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[BillingPeriods]') AND type in (N'U'))
    BEGIN
        ALTER TABLE [dbo].[CallLogStagings]
        ADD CONSTRAINT [FK_CallLogStagings_BillingPeriods_BillingPeriodId]
        FOREIGN KEY ([BillingPeriodId]) REFERENCES [BillingPeriods] ([Id]);
    END

    CREATE INDEX [IX_CallLogStagings_BatchId] ON [CallLogStagings] ([BatchId]);
    CREATE INDEX [IX_CallLogStagings_VerificationStatus] ON [CallLogStagings] ([VerificationStatus]);
    CREATE INDEX [IX_CallLogStagings_ProcessingStatus] ON [CallLogStagings] ([ProcessingStatus]);
    CREATE INDEX [IX_CallLogStagings_HasAnomalies] ON [CallLogStagings] ([HasAnomalies]);
    CREATE INDEX [IX_CallLogStagings_UserPhoneId] ON [CallLogStagings] ([UserPhoneId]);
    CREATE INDEX [IX_CallLogStagings_BillingPeriodId] ON [CallLogStagings] ([BillingPeriodId]);
    CREATE INDEX [IX_CallLogStagings_CallDate] ON [CallLogStagings] ([CallDate]);
    CREATE INDEX [IX_CallLogStagings_ExtensionNumber] ON [CallLogStagings] ([ExtensionNumber]);

    PRINT 'CallLogStagings table created successfully';
END
ELSE
BEGIN
    PRINT 'CallLogStagings table already exists';
END
GO
