-- Create AnomalyTypes table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[AnomalyTypes]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[AnomalyTypes] (
        [Id] int IDENTITY(1,1) NOT NULL,
        [Code] nvarchar(50) NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [Description] nvarchar(500) NULL,
        [Severity] int NOT NULL,
        [AutoReject] bit NOT NULL CONSTRAINT [DF_AnomalyTypes_AutoReject] DEFAULT (0),
        [IsActive] bit NOT NULL CONSTRAINT [DF_AnomalyTypes_IsActive] DEFAULT (1),
        CONSTRAINT [PK_AnomalyTypes] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
    CREATE UNIQUE INDEX [IX_AnomalyTypes_Code] ON [AnomalyTypes] ([Code]);
    PRINT 'AnomalyTypes table created successfully';
END
ELSE
BEGIN
    PRINT 'AnomalyTypes table already exists';
END
GO

-- Create BillingPeriods table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[BillingPeriods]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[BillingPeriods] (
        [Id] int IDENTITY(1,1) NOT NULL,
        [PeriodCode] nvarchar(20) NOT NULL,
        [StartDate] datetime NOT NULL,
        [EndDate] datetime NOT NULL,
        [Status] nvarchar(20) NOT NULL CONSTRAINT [DF_BillingPeriods_Status] DEFAULT ('OPEN'),
        [MonthlyImportDate] datetime NULL,
        [MonthlyBatchId] uniqueidentifier NULL,
        [MonthlyRecordCount] int NULL CONSTRAINT [DF_BillingPeriods_MonthlyRecordCount] DEFAULT (0),
        [MonthlyTotalCost] decimal(18,2) NULL CONSTRAINT [DF_BillingPeriods_MonthlyTotalCost] DEFAULT (0),
        [InterimUpdateCount] int NULL CONSTRAINT [DF_BillingPeriods_InterimUpdateCount] DEFAULT (0),
        [LastInterimDate] datetime NULL,
        [InterimRecordCount] int NULL CONSTRAINT [DF_BillingPeriods_InterimRecordCount] DEFAULT (0),
        [InterimAdjustmentAmount] decimal(18,2) NULL CONSTRAINT [DF_BillingPeriods_InterimAdjustmentAmount] DEFAULT (0),
        [ClosedDate] datetime NULL,
        [ClosedBy] nvarchar(100) NULL,
        [LockedDate] datetime NULL,
        [LockedBy] nvarchar(100) NULL,
        [CreatedDate] datetime NULL CONSTRAINT [DF_BillingPeriods_CreatedDate] DEFAULT (GETDATE()),
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedDate] datetime NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [Notes] nvarchar(MAX) NULL,
        CONSTRAINT [PK_BillingPeriods] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
    CREATE UNIQUE INDEX [IX_BillingPeriods_PeriodCode] ON [BillingPeriods] ([PeriodCode]);
    CREATE INDEX [IX_BillingPeriods_Status] ON [BillingPeriods] ([Status]);
    CREATE INDEX [IX_BillingPeriods_StartDate] ON [BillingPeriods] ([StartDate]);
    PRINT 'BillingPeriods table created successfully';
END
ELSE
BEGIN
    PRINT 'BillingPeriods table already exists';
END
GO

-- Create StagingBatches table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[StagingBatches]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[StagingBatches] (
        [Id] uniqueidentifier NOT NULL CONSTRAINT [DF_StagingBatches_Id] DEFAULT (NEWID()),
        [BatchName] nvarchar(100) NOT NULL,
        [BatchType] nvarchar(50) NULL,
        [TotalRecords] int NOT NULL CONSTRAINT [DF_StagingBatches_TotalRecords] DEFAULT (0),
        [VerifiedRecords] int NOT NULL CONSTRAINT [DF_StagingBatches_VerifiedRecords] DEFAULT (0),
        [RejectedRecords] int NOT NULL CONSTRAINT [DF_StagingBatches_RejectedRecords] DEFAULT (0),
        [PendingRecords] int NOT NULL CONSTRAINT [DF_StagingBatches_PendingRecords] DEFAULT (0),
        [RecordsWithAnomalies] int NOT NULL CONSTRAINT [DF_StagingBatches_RecordsWithAnomalies] DEFAULT (0),
        [CreatedDate] datetime2 NOT NULL CONSTRAINT [DF_StagingBatches_CreatedDate] DEFAULT (GETUTCDATE()),
        [StartProcessingDate] datetime2 NULL,
        [EndProcessingDate] datetime2 NULL,
        [BatchStatus] int NOT NULL CONSTRAINT [DF_StagingBatches_BatchStatus] DEFAULT (0),
        [CreatedBy] nvarchar(100) NOT NULL,
        [VerifiedBy] nvarchar(100) NULL,
        [PublishedBy] nvarchar(100) NULL,
        [SourceSystems] nvarchar(200) NULL,
        [Notes] nvarchar(MAX) NULL,
        [BillingPeriodId] int NULL,
        [BatchCategory] nvarchar(20) NULL CONSTRAINT [DF_StagingBatches_BatchCategory] DEFAULT ('MONTHLY'),
        CONSTRAINT [PK_StagingBatches] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
    IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[BillingPeriods]') AND type in (N'U'))
    BEGIN
        ALTER TABLE [dbo].[StagingBatches]
        ADD CONSTRAINT [FK_StagingBatches_BillingPeriods_BillingPeriodId]
        FOREIGN KEY ([BillingPeriodId]) REFERENCES [BillingPeriods] ([Id]);
    END
    CREATE INDEX [IX_StagingBatches_BatchStatus] ON [StagingBatches] ([BatchStatus]);
    CREATE INDEX [IX_StagingBatches_CreatedDate] ON [StagingBatches] ([CreatedDate]);
    CREATE INDEX [IX_StagingBatches_BillingPeriodId] ON [StagingBatches] ([BillingPeriodId]);
    PRINT 'StagingBatches table created successfully';
END
ELSE
BEGIN
    PRINT 'StagingBatches table already exists';
END
GO
