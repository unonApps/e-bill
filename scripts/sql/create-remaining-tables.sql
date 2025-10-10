-- Create CallLogReconciliations table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[CallLogReconciliations]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[CallLogReconciliations] (
        [Id] int IDENTITY(1,1) NOT NULL,
        [BillingPeriodId] int NOT NULL,
        [SourceRecordId] int NOT NULL,
        [SourceTable] nvarchar(50) NOT NULL,
        [Version] int NULL CONSTRAINT [DF_CallLogReconciliations_Version] DEFAULT (1),
        [ImportType] nvarchar(20) NOT NULL,
        [ImportBatchId] uniqueidentifier NOT NULL,
        [ImportDate] datetime NOT NULL,
        [PreviousAmount] decimal(18,2) NULL,
        [CurrentAmount] decimal(18,2) NOT NULL,
        [AdjustmentAmount] decimal(18,2) NULL,
        [AdjustmentReason] nvarchar(500) NULL,
        [IsSuperseded] bit NULL CONSTRAINT [DF_CallLogReconciliations_IsSuperseded] DEFAULT (0),
        [SupersededBy] int NULL,
        [SupersededDate] datetime NULL,
        CONSTRAINT [PK_CallLogReconciliations] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
    
    IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[BillingPeriods]') AND type in (N'U'))
    BEGIN
        ALTER TABLE [dbo].[CallLogReconciliations]
        ADD CONSTRAINT [FK_CallLogReconciliations_BillingPeriods_BillingPeriodId]
        FOREIGN KEY ([BillingPeriodId]) REFERENCES [BillingPeriods] ([Id]);
    END

    CREATE INDEX [IX_CallLogReconciliations_BillingPeriodId] ON [CallLogReconciliations] ([BillingPeriodId]);
    CREATE INDEX [IX_CallLogReconciliations_SourceRecordId] ON [CallLogReconciliations] ([SourceRecordId]);
    CREATE INDEX [IX_CallLogReconciliations_ImportBatchId] ON [CallLogReconciliations] ([ImportBatchId]);

    PRINT 'CallLogReconciliations table created successfully';
END
ELSE
BEGIN
    PRINT 'CallLogReconciliations table already exists';
END
GO

-- Create CallRecords table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[CallRecords]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[CallRecords] (
        [Id] int IDENTITY(1,1) NOT NULL,
        [ext_no] nvarchar(50) NOT NULL CONSTRAINT [DF_CallRecords_ext_no] DEFAULT (''),
        [call_date] datetime2 NOT NULL,
        [call_number] nvarchar(50) NOT NULL CONSTRAINT [DF_CallRecords_call_number] DEFAULT (''),
        [call_destination] nvarchar(100) NOT NULL CONSTRAINT [DF_CallRecords_call_destination] DEFAULT (''),
        [call_endtime] datetime2 NOT NULL,
        [call_duration] int NOT NULL CONSTRAINT [DF_CallRecords_call_duration] DEFAULT (0),
        [call_curr_code] nvarchar(10) NOT NULL CONSTRAINT [DF_CallRecords_call_curr_code] DEFAULT (''),
        [call_cost] decimal(18,2) NOT NULL CONSTRAINT [DF_CallRecords_call_cost] DEFAULT (0),
        [call_cost_usd] decimal(18,2) NOT NULL CONSTRAINT [DF_CallRecords_call_cost_usd] DEFAULT (0),
        [call_cost_kshs] decimal(18,2) NOT NULL CONSTRAINT [DF_CallRecords_call_cost_kshs] DEFAULT (0),
        [call_type] nvarchar(50) NOT NULL CONSTRAINT [DF_CallRecords_call_type] DEFAULT (''),
        [call_dest_type] nvarchar(50) NOT NULL CONSTRAINT [DF_CallRecords_call_dest_type] DEFAULT (''),
        [call_year] int NOT NULL,
        [call_month] int NOT NULL,
        [ext_resp_index] nvarchar(50) NULL,
        [call_pay_index] nvarchar(50) NULL,
        [call_ver_ind] bit NOT NULL CONSTRAINT [DF_CallRecords_call_ver_ind] DEFAULT (0),
        [call_ver_date] datetime2 NULL,
        [call_cert_ind] bit NOT NULL CONSTRAINT [DF_CallRecords_call_cert_ind] DEFAULT (0),
        [call_cert_date] datetime2 NULL,
        [call_cert_by] nvarchar(100) NULL,
        [call_proc_ind] bit NOT NULL CONSTRAINT [DF_CallRecords_call_proc_ind] DEFAULT (0),
        [entry_date] datetime2 NOT NULL CONSTRAINT [DF_CallRecords_entry_date] DEFAULT (GETUTCDATE()),
        [call_dest_descr] nvarchar(200) NULL,
        [SourceSystem] nvarchar(50) NULL,
        [SourceBatchId] uniqueidentifier NULL,
        [SourceStagingId] int NULL,
        CONSTRAINT [PK_CallRecords] PRIMARY KEY CLUSTERED ([Id] ASC)
    );

    CREATE INDEX [IX_CallRecords_ext_no] ON [CallRecords] ([ext_no]);
    CREATE INDEX [IX_CallRecords_call_date] ON [CallRecords] ([call_date]);
    CREATE INDEX [IX_CallRecords_ext_resp_index] ON [CallRecords] ([ext_resp_index]);
    CREATE INDEX [IX_CallRecords_call_pay_index] ON [CallRecords] ([call_pay_index]);
    CREATE INDEX [IX_CallRecords_SourceBatchId] ON [CallRecords] ([SourceBatchId]);

    PRINT 'CallRecords table created successfully';
END
ELSE
BEGIN
    PRINT 'CallRecords table already exists';
END
GO

-- Create InterimUpdates table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[InterimUpdates]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[InterimUpdates] (
        [Id] int IDENTITY(1,1) NOT NULL,
        [BillingPeriodId] int NOT NULL,
        [UpdateType] nvarchar(50) NOT NULL,
        [BatchId] uniqueidentifier NOT NULL,
        [RecordsAdded] int NULL CONSTRAINT [DF_InterimUpdates_RecordsAdded] DEFAULT (0),
        [RecordsModified] int NULL CONSTRAINT [DF_InterimUpdates_RecordsModified] DEFAULT (0),
        [RecordsDeleted] int NULL CONSTRAINT [DF_InterimUpdates_RecordsDeleted] DEFAULT (0),
        [NetAdjustmentAmount] decimal(18,2) NULL,
        [RequestedBy] nvarchar(100) NOT NULL,
        [RequestedDate] datetime NOT NULL,
        [ApprovedBy] nvarchar(100) NULL,
        [ApprovalDate] datetime NULL,
        [ApprovalStatus] nvarchar(20) NULL CONSTRAINT [DF_InterimUpdates_ApprovalStatus] DEFAULT ('PENDING'),
        [RejectionReason] nvarchar(500) NULL,
        [Justification] nvarchar(MAX) NOT NULL,
        [SupportingDocuments] nvarchar(MAX) NULL,
        [ProcessedDate] datetime NULL,
        [ProcessingNotes] nvarchar(MAX) NULL,
        CONSTRAINT [PK_InterimUpdates] PRIMARY KEY CLUSTERED ([Id] ASC)
    );

    IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[BillingPeriods]') AND type in (N'U'))
    BEGIN
        ALTER TABLE [dbo].[InterimUpdates]
        ADD CONSTRAINT [FK_InterimUpdates_BillingPeriods_BillingPeriodId]
        FOREIGN KEY ([BillingPeriodId]) REFERENCES [BillingPeriods] ([Id]);
    END

    CREATE INDEX [IX_InterimUpdates_BillingPeriodId] ON [InterimUpdates] ([BillingPeriodId]);
    CREATE INDEX [IX_InterimUpdates_BatchId] ON [InterimUpdates] ([BatchId]);
    CREATE INDEX [IX_InterimUpdates_ApprovalStatus] ON [InterimUpdates] ([ApprovalStatus]);

    PRINT 'InterimUpdates table created successfully';
END
ELSE
BEGIN
    PRINT 'InterimUpdates table already exists';
END
GO
