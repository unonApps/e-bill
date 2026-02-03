IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE TABLE [AnomalyTypes] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(50) NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [Description] nvarchar(500) NOT NULL,
        [Severity] int NOT NULL,
        [AutoReject] bit NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_AnomalyTypes] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetRoles] (
        [Id] nvarchar(450) NOT NULL,
        [Name] nvarchar(256) NULL,
        [NormalizedName] nvarchar(256) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE TABLE [AuditLogs] (
        [Id] int NOT NULL IDENTITY,
        [EntityType] nvarchar(100) NOT NULL,
        [EntityId] nvarchar(100) NULL,
        [Action] nvarchar(50) NOT NULL,
        [Description] nvarchar(500) NULL,
        [OldValues] nvarchar(2000) NULL,
        [NewValues] nvarchar(2000) NULL,
        [PerformedBy] nvarchar(100) NOT NULL,
        [PerformedDate] datetime2 NOT NULL,
        [IPAddress] nvarchar(50) NULL,
        [UserAgent] nvarchar(500) NULL,
        [Module] nvarchar(50) NULL,
        [IsSuccess] bit NOT NULL,
        [ErrorMessage] nvarchar(1000) NULL,
        [AdditionalData] nvarchar(4000) NULL,
        CONSTRAINT [PK_AuditLogs] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE TABLE [BillingPeriods] (
        [Id] int NOT NULL IDENTITY,
        [PeriodCode] nvarchar(20) NOT NULL,
        [StartDate] datetime2 NOT NULL,
        [EndDate] datetime2 NOT NULL,
        [Status] nvarchar(20) NOT NULL,
        [MonthlyImportDate] datetime2 NULL,
        [MonthlyBatchId] uniqueidentifier NULL,
        [MonthlyRecordCount] int NOT NULL,
        [MonthlyTotalCost] decimal(18,2) NOT NULL,
        [InterimUpdateCount] int NOT NULL,
        [LastInterimDate] datetime2 NULL,
        [InterimRecordCount] int NOT NULL,
        [InterimAdjustmentAmount] decimal(18,2) NOT NULL,
        [ClosedDate] datetime2 NULL,
        [ClosedBy] nvarchar(100) NULL,
        [LockedDate] datetime2 NULL,
        [LockedBy] nvarchar(100) NULL,
        [CreatedDate] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NOT NULL,
        [ModifiedDate] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [Notes] nvarchar(max) NULL,
        CONSTRAINT [PK_BillingPeriods] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE TABLE [ClassOfServices] (
        [Id] int NOT NULL IDENTITY,
        [PublicId] uniqueidentifier NOT NULL,
        [Class] nvarchar(100) NOT NULL,
        [Service] nvarchar(200) NOT NULL,
        [EligibleStaff] nvarchar(200) NOT NULL,
        [AirtimeAllowance] nvarchar(50) NULL,
        [DataAllowance] nvarchar(50) NULL,
        [HandsetAllowance] nvarchar(50) NULL,
        [HandsetAIRemarks] nvarchar(500) NULL,
        [AirtimeAllowanceAmount] decimal(18,4) NULL,
        [DataAllowanceAmount] decimal(18,4) NULL,
        [HandsetAllowanceAmount] decimal(18,4) NULL,
        [BillingPeriod] nvarchar(20) NOT NULL,
        [ServiceStatus] int NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_ClassOfServices] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE TABLE [EmailConfigurations] (
        [Id] int NOT NULL IDENTITY,
        [SmtpServer] nvarchar(255) NOT NULL,
        [SmtpPort] int NOT NULL,
        [FromEmail] nvarchar(255) NOT NULL,
        [FromName] nvarchar(255) NOT NULL,
        [Username] nvarchar(255) NOT NULL,
        [Password] nvarchar(500) NOT NULL,
        [EnableSsl] bit NOT NULL,
        [IsActive] bit NOT NULL,
        [UseDefaultCredentials] bit NOT NULL,
        [Timeout] int NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [ModifiedDate] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [Notes] nvarchar(500) NULL,
        CONSTRAINT [PK_EmailConfigurations] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE TABLE [EmailTemplates] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(255) NOT NULL,
        [TemplateCode] nvarchar(100) NOT NULL,
        [Subject] nvarchar(500) NOT NULL,
        [HtmlBody] nvarchar(max) NOT NULL,
        [PlainTextBody] nvarchar(max) NULL,
        [Description] nvarchar(1000) NULL,
        [AvailablePlaceholders] nvarchar(2000) NULL,
        [Category] nvarchar(100) NULL,
        [IsActive] bit NOT NULL,
        [IsSystemTemplate] bit NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [ModifiedDate] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_EmailTemplates] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE TABLE [ExchangeRates] (
        [Id] int NOT NULL IDENTITY,
        [Month] int NOT NULL,
        [Year] int NOT NULL,
        [Rate] decimal(18,4) NOT NULL,
        [CreatedBy] nvarchar(256) NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedBy] nvarchar(256) NULL,
        [UpdatedDate] datetime2 NULL,
        CONSTRAINT [PK_ExchangeRates] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE TABLE [ImportAudits] (
        [Id] int NOT NULL IDENTITY,
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
        [DateFormatPreferences] nvarchar(500) NULL,
        CONSTRAINT [PK_ImportAudits] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE TABLE [Organizations] (
        [Id] int NOT NULL IDENTITY,
        [PublicId] uniqueidentifier NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [Code] nvarchar(10) NULL,
        [Description] nvarchar(500) NULL,
        [CreatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_Organizations] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE TABLE [RecoveryConfiguration] (
        [Id] int NOT NULL IDENTITY,
        [RuleName] nvarchar(100) NOT NULL,
        [RuleType] nvarchar(50) NOT NULL,
        [IsEnabled] bit NOT NULL,
        [DefaultVerificationDays] int NULL,
        [DefaultApprovalDays] int NULL,
        [DefaultRevertDays] int NULL,
        [MaxRevertsAllowed] int NOT NULL,
        [JobIntervalMinutes] int NULL,
        [AutomationEnabled] bit NOT NULL,
        [RequireApprovalForAutomation] bit NOT NULL,
        [NotificationEnabled] bit NOT NULL,
        [ReminderDaysBefore] int NULL,
        [EnableEmailNotifications] bit NOT NULL,
        [AdminNotificationEmail] nvarchar(200) NULL,
        [ConfigValue] nvarchar(max) NULL,
        [Description] nvarchar(500) NULL,
        [CreatedDate] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedDate] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_RecoveryConfiguration] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE TABLE [RecoveryJobExecutions] (
        [Id] int NOT NULL IDENTITY,
        [StartTime] datetime2 NOT NULL,
        [EndTime] datetime2 NULL,
        [DurationMs] bigint NULL,
        [Status] nvarchar(50) NOT NULL,
        [RunType] nvarchar(50) NOT NULL,
        [TriggeredBy] nvarchar(100) NULL,
        [ExpiredVerificationsProcessed] int NOT NULL,
        [ExpiredApprovalsProcessed] int NOT NULL,
        [RevertedVerificationsProcessed] int NOT NULL,
        [TotalRecordsProcessed] int NOT NULL,
        [TotalAmountRecovered] decimal(18,2) NOT NULL,
        [RemindersSent] int NOT NULL,
        [ErrorMessage] nvarchar(max) NULL,
        [ExecutionLog] nvarchar(max) NULL,
        [NextScheduledRun] datetime2 NULL,
        CONSTRAINT [PK_RecoveryJobExecutions] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE TABLE [RefundRequests] (
        [Id] int NOT NULL IDENTITY,
        [PrimaryMobileNumber] nvarchar(9) NOT NULL,
        [IndexNo] nvarchar(50) NOT NULL,
        [MobileNumberAssignedTo] nvarchar(200) NOT NULL,
        [OfficeExtension] nvarchar(20) NULL,
        [Office] nvarchar(200) NOT NULL,
        [MobileService] nvarchar(100) NOT NULL,
        [ClassOfService] nvarchar(100) NOT NULL,
        [DeviceAllowance] decimal(18,2) NOT NULL,
        [PreviousDeviceReimbursedDate] datetime2 NULL,
        [PurchaseReceiptPath] nvarchar(500) NOT NULL,
        [PurchaseReceiptData] varbinary(max) NULL,
        [PurchaseReceiptFileName] nvarchar(255) NULL,
        [PurchaseReceiptContentType] nvarchar(100) NULL,
        [PurchaseReceiptUploadDate] datetime2 NULL,
        [DevicePurchaseCurrency] nvarchar(3) NOT NULL,
        [DevicePurchaseAmount] decimal(18,2) NOT NULL,
        [Organization] nvarchar(200) NOT NULL,
        [UmojaBankName] nvarchar(200) NOT NULL,
        [Supervisor] nvarchar(200) NOT NULL,
        [Remarks] nvarchar(200) NULL,
        [RequestDate] datetime2 NOT NULL,
        [RequestedBy] nvarchar(450) NOT NULL,
        [Status] int NOT NULL,
        [SubmittedToSupervisor] bit NOT NULL,
        [SupervisorApprovalDate] datetime2 NULL,
        [SupervisorNotes] nvarchar(500) NULL,
        [SupervisorRemarks] nvarchar(200) NULL,
        [SupervisorName] nvarchar(300) NULL,
        [SupervisorEmail] nvarchar(300) NULL,
        [BudgetOfficerApprovalDate] datetime2 NULL,
        [BudgetOfficerNotes] nvarchar(500) NULL,
        [BudgetOfficerRemarks] nvarchar(200) NULL,
        [BudgetOfficerName] nvarchar(300) NULL,
        [BudgetOfficerEmail] nvarchar(300) NULL,
        [CostObject] nvarchar(100) NULL,
        [CostCenter] nvarchar(100) NULL,
        [FundCommitment] nvarchar(100) NULL,
        [StaffClaimsApprovalDate] datetime2 NULL,
        [StaffClaimsNotes] nvarchar(500) NULL,
        [StaffClaimsRemarks] nvarchar(200) NULL,
        [StaffClaimsOfficerName] nvarchar(300) NULL,
        [StaffClaimsOfficerEmail] nvarchar(300) NULL,
        [UmojaPaymentDocumentId] nvarchar(100) NULL,
        [RefundUsdAmount] decimal(18,2) NULL,
        [ClaimsActionDate] datetime2 NULL,
        [PaymentApprovalDate] datetime2 NULL,
        [PaymentApprovalNotes] nvarchar(500) NULL,
        [PaymentApprovalRemarks] nvarchar(200) NULL,
        [PaymentApproverName] nvarchar(300) NULL,
        [PaymentApproverEmail] nvarchar(300) NULL,
        [CancellationDate] datetime2 NULL,
        [CancellationReason] nvarchar(500) NULL,
        [CancelledBy] nvarchar(300) NULL,
        [CompletionDate] datetime2 NULL,
        [PaymentReference] nvarchar(100) NULL,
        [CompletionNotes] nvarchar(500) NULL,
        [ProcessedBy] nvarchar(450) NULL,
        CONSTRAINT [PK_RefundRequests] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE TABLE [ServiceProviders] (
        [Id] int NOT NULL IDENTITY,
        [SPID] nvarchar(10) NOT NULL,
        [ServiceProviderName] nvarchar(200) NOT NULL,
        [SPMainCP] nvarchar(200) NOT NULL,
        [SPMainCPEmail] nvarchar(300) NOT NULL,
        [SPOtherCPsEmail] nvarchar(1000) NULL,
        [SPStatus] int NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_ServiceProviders] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetRoleClaims] (
        [Id] int NOT NULL IDENTITY,
        [RoleId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE TABLE [CallLogReconciliations] (
        [Id] int NOT NULL IDENTITY,
        [BillingPeriodId] int NOT NULL,
        [SourceRecordId] int NOT NULL,
        [SourceTable] nvarchar(50) NOT NULL,
        [Version] int NOT NULL,
        [ImportType] nvarchar(20) NOT NULL,
        [ImportBatchId] uniqueidentifier NOT NULL,
        [ImportDate] datetime2 NOT NULL,
        [PreviousAmount] decimal(18,2) NULL,
        [CurrentAmount] decimal(18,2) NOT NULL,
        [AdjustmentReason] nvarchar(500) NULL,
        [IsSuperseded] bit NOT NULL,
        [SupersededBy] int NULL,
        [SupersededDate] datetime2 NULL,
        CONSTRAINT [PK_CallLogReconciliations] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CallLogReconciliations_BillingPeriods_BillingPeriodId] FOREIGN KEY ([BillingPeriodId]) REFERENCES [BillingPeriods] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_CallLogReconciliations_CallLogReconciliations_SupersededBy] FOREIGN KEY ([SupersededBy]) REFERENCES [CallLogReconciliations] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE TABLE [StagingBatches] (
        [Id] uniqueidentifier NOT NULL,
        [BatchName] nvarchar(100) NOT NULL,
        [BatchType] nvarchar(50) NOT NULL,
        [TotalRecords] int NOT NULL,
        [VerifiedRecords] int NOT NULL,
        [RejectedRecords] int NOT NULL,
        [PendingRecords] int NOT NULL,
        [RecordsWithAnomalies] int NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [StartProcessingDate] datetime2 NULL,
        [EndProcessingDate] datetime2 NULL,
        [RecoveryProcessingDate] datetime2 NULL,
        [RecoveryStatus] nvarchar(50) NULL,
        [TotalRecoveredAmount] decimal(18,2) NULL,
        [TotalPersonalAmount] decimal(18,2) NULL,
        [TotalOfficialAmount] decimal(18,2) NULL,
        [TotalClassOfServiceAmount] decimal(18,2) NULL,
        [BatchStatus] int NOT NULL,
        [CreatedBy] nvarchar(100) NOT NULL,
        [VerifiedBy] nvarchar(100) NULL,
        [PublishedBy] nvarchar(100) NULL,
        [SourceSystems] nvarchar(200) NULL,
        [Notes] nvarchar(max) NULL,
        [BillingPeriodId] int NULL,
        [BatchCategory] nvarchar(20) NOT NULL,
        CONSTRAINT [PK_StagingBatches] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_StagingBatches_BillingPeriods_BillingPeriodId] FOREIGN KEY ([BillingPeriodId]) REFERENCES [BillingPeriods] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE TABLE [EmailLogs] (
        [Id] int NOT NULL IDENTITY,
        [ToEmail] nvarchar(255) NOT NULL,
        [CcEmails] nvarchar(1000) NULL,
        [BccEmails] nvarchar(1000) NULL,
        [Subject] nvarchar(500) NOT NULL,
        [Body] nvarchar(max) NOT NULL,
        [PlainTextBody] nvarchar(max) NULL,
        [EmailTemplateId] int NULL,
        [Status] nvarchar(50) NOT NULL,
        [SentDate] datetime2 NULL,
        [ErrorMessage] nvarchar(2000) NULL,
        [RetryCount] int NOT NULL,
        [MaxRetries] int NOT NULL,
        [Priority] int NOT NULL,
        [ScheduledSendDate] datetime2 NULL,
        [CreatedDate] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [OpenedDate] datetime2 NULL,
        [OpenCount] int NOT NULL,
        [TrackingId] nvarchar(100) NULL,
        [RelatedEntityType] nvarchar(100) NULL,
        [RelatedEntityId] nvarchar(100) NULL,
        CONSTRAINT [PK_EmailLogs] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_EmailLogs_EmailTemplates_EmailTemplateId] FOREIGN KEY ([EmailTemplateId]) REFERENCES [EmailTemplates] ([Id]) ON DELETE SET NULL
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE TABLE [Offices] (
        [Id] int NOT NULL IDENTITY,
        [PublicId] uniqueidentifier NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [Code] nvarchar(10) NULL,
        [Description] nvarchar(500) NULL,
        [OrganizationId] int NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_Offices] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Offices_Organizations_OrganizationId] FOREIGN KEY ([OrganizationId]) REFERENCES [Organizations] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE TABLE [Ebills] (
        [Id] int NOT NULL IDENTITY,
        [FullName] nvarchar(100) NOT NULL,
        [Email] nvarchar(300) NOT NULL,
        [PhoneNumber] nvarchar(20) NOT NULL,
        [Department] nvarchar(100) NOT NULL,
        [ServiceProviderId] int NOT NULL,
        [AccountNumber] nvarchar(50) NOT NULL,
        [BillMonth] datetime2 NOT NULL,
        [BillAmount] decimal(18,2) NOT NULL,
        [DueDate] datetime2 NOT NULL,
        [BillType] int NOT NULL,
        [Description] nvarchar(500) NULL,
        [AdditionalNotes] nvarchar(500) NULL,
        [Supervisor] nvarchar(200) NOT NULL,
        [Status] int NOT NULL,
        [RequestDate] datetime2 NOT NULL,
        [ProcessedDate] datetime2 NULL,
        [RequestedBy] nvarchar(450) NOT NULL,
        [ProcessedBy] nvarchar(450) NULL,
        [ProcessingNotes] nvarchar(500) NULL,
        [PaymentDate] datetime2 NULL,
        [PaidAmount] decimal(18,2) NULL,
        [SubmittedToSupervisor] bit NOT NULL,
        [SupervisorApprovalDate] datetime2 NULL,
        [SupervisorNotes] nvarchar(500) NULL,
        [SupervisorRemarks] nvarchar(200) NULL,
        [SupervisorName] nvarchar(300) NULL,
        [SupervisorEmail] nvarchar(300) NULL,
        CONSTRAINT [PK_Ebills] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Ebills_ServiceProviders_ServiceProviderId] FOREIGN KEY ([ServiceProviderId]) REFERENCES [ServiceProviders] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE TABLE [SimRequests] (
        [Id] int NOT NULL IDENTITY,
        [IndexNo] nvarchar(20) NOT NULL,
        [FirstName] nvarchar(100) NOT NULL,
        [LastName] nvarchar(100) NOT NULL,
        [Organization] nvarchar(200) NOT NULL,
        [Office] nvarchar(200) NOT NULL,
        [Grade] nvarchar(50) NOT NULL,
        [FunctionalTitle] nvarchar(300) NOT NULL,
        [OfficeExtension] nvarchar(20) NULL,
        [OfficialEmail] nvarchar(300) NOT NULL,
        [SimType] int NOT NULL,
        [ServiceProviderId] int NOT NULL,
        [Supervisor] nvarchar(200) NOT NULL,
        [PreviouslyAssignedLines] nvarchar(1000) NULL,
        [Remarks] nvarchar(500) NULL,
        [Status] int NOT NULL,
        [RequestDate] datetime2 NOT NULL,
        [ProcessedDate] datetime2 NULL,
        [RequestedBy] nvarchar(450) NOT NULL,
        [ProcessedBy] nvarchar(450) NULL,
        [ProcessingNotes] nvarchar(500) NULL,
        [SubmittedToSupervisor] bit NOT NULL,
        [SupervisorApprovalDate] datetime2 NULL,
        [SupervisorNotes] nvarchar(500) NULL,
        [MobileService] nvarchar(100) NULL,
        [MobileServiceAllowance] nvarchar(100) NULL,
        [HandsetAllowance] nvarchar(100) NULL,
        [SupervisorRemarks] nvarchar(200) NULL,
        [SupervisorName] nvarchar(300) NULL,
        [SupervisorEmail] nvarchar(300) NULL,
        [SimSerialNo] nvarchar(50) NULL,
        [ServiceRequestNo] nvarchar(50) NULL,
        [LineType] nvarchar(20) NULL,
        [SimPuk] nvarchar(20) NULL,
        [LineUsage] nvarchar(20) NULL,
        [PreviousLines] nvarchar(500) NULL,
        [SpNotifiedDate] datetime2 NULL,
        [AssignedNo] nvarchar(50) NULL,
        [CollectionNotifiedDate] datetime2 NULL,
        [SimIssuedBy] nvarchar(100) NULL,
        [SimCollectedBy] nvarchar(100) NULL,
        [SimCollectedDate] datetime2 NULL,
        [IctsRemark] nvarchar(200) NULL,
        CONSTRAINT [PK_SimRequests] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SimRequests_ServiceProviders_ServiceProviderId] FOREIGN KEY ([ServiceProviderId]) REFERENCES [ServiceProviders] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE TABLE [DeadlineTracking] (
        [Id] int NOT NULL IDENTITY,
        [BatchId] uniqueidentifier NOT NULL,
        [DeadlineType] nvarchar(50) NOT NULL,
        [TargetEntity] nvarchar(100) NOT NULL,
        [DeadlineDate] datetime2 NOT NULL,
        [ExtendedDeadline] datetime2 NULL,
        [DeadlineStatus] nvarchar(50) NOT NULL,
        [MissedDate] datetime2 NULL,
        [RecoveryProcessed] bit NOT NULL,
        [RecoveryProcessedDate] datetime2 NULL,
        [ExtensionReason] nvarchar(500) NULL,
        [ExtensionApprovedBy] nvarchar(100) NULL,
        [ExtensionApprovedDate] datetime2 NULL,
        [CreatedDate] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [Notes] nvarchar(1000) NULL,
        CONSTRAINT [PK_DeadlineTracking] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_DeadlineTracking_StagingBatches_BatchId] FOREIGN KEY ([BatchId]) REFERENCES [StagingBatches] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE TABLE [InterimUpdates] (
        [Id] int NOT NULL IDENTITY,
        [BillingPeriodId] int NOT NULL,
        [UpdateType] nvarchar(50) NOT NULL,
        [BatchId] uniqueidentifier NOT NULL,
        [RecordsAdded] int NOT NULL,
        [RecordsModified] int NOT NULL,
        [RecordsDeleted] int NOT NULL,
        [NetAdjustmentAmount] decimal(18,2) NOT NULL,
        [RequestedBy] nvarchar(100) NOT NULL,
        [RequestedDate] datetime2 NOT NULL,
        [ApprovedBy] nvarchar(100) NULL,
        [ApprovalDate] datetime2 NULL,
        [ApprovalStatus] nvarchar(20) NOT NULL,
        [RejectionReason] nvarchar(500) NULL,
        [Justification] nvarchar(max) NOT NULL,
        [SupportingDocuments] nvarchar(max) NULL,
        [ProcessedDate] datetime2 NULL,
        [ProcessingNotes] nvarchar(max) NULL,
        CONSTRAINT [PK_InterimUpdates] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_InterimUpdates_BillingPeriods_BillingPeriodId] FOREIGN KEY ([BillingPeriodId]) REFERENCES [BillingPeriods] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_InterimUpdates_StagingBatches_BatchId] FOREIGN KEY ([BatchId]) REFERENCES [StagingBatches] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE TABLE [EmailAttachments] (
        [Id] int NOT NULL IDENTITY,
        [EmailLogId] int NOT NULL,
        [FileName] nvarchar(255) NOT NULL,
        [FilePath] nvarchar(500) NOT NULL,
        [FileSize] bigint NOT NULL,
        [ContentType] nvarchar(100) NULL,
        [CreatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_EmailAttachments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_EmailAttachments_EmailLogs_EmailLogId] FOREIGN KEY ([EmailLogId]) REFERENCES [EmailLogs] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE TABLE [SubOffices] (
        [Id] int NOT NULL IDENTITY,
        [PublicId] uniqueidentifier NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [Code] nvarchar(10) NULL,
        [Description] nvarchar(500) NULL,
        [ContactPerson] nvarchar(100) NULL,
        [PhoneNumber] nvarchar(20) NULL,
        [Email] nvarchar(100) NULL,
        [Address] nvarchar(200) NULL,
        [OfficeId] int NOT NULL,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        [CreatedDate] datetime2 NOT NULL,
        [ModifiedDate] datetime2 NULL,
        CONSTRAINT [PK_SubOffices] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SubOffices_Offices_OfficeId] FOREIGN KEY ([OfficeId]) REFERENCES [Offices] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE TABLE [SimRequestHistories] (
        [Id] int NOT NULL IDENTITY,
        [SimRequestId] int NOT NULL,
        [Action] nvarchar(100) NOT NULL,
        [PreviousStatus] nvarchar(50) NULL,
        [NewStatus] nvarchar(50) NULL,
        [Comments] nvarchar(1000) NULL,
        [PerformedBy] nvarchar(450) NOT NULL,
        [UserName] nvarchar(200) NOT NULL,
        [Timestamp] datetime2 NOT NULL,
        [IpAddress] nvarchar(50) NULL,
        CONSTRAINT [PK_SimRequestHistories] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SimRequestHistories_SimRequests_SimRequestId] FOREIGN KEY ([SimRequestId]) REFERENCES [SimRequests] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE TABLE [EbillUsers] (
        [Id] int NOT NULL IDENTITY,
        [PublicId] uniqueidentifier NOT NULL,
        [FirstName] nvarchar(100) NOT NULL,
        [LastName] nvarchar(100) NOT NULL,
        [IndexNumber] nvarchar(50) NOT NULL,
        [OfficialMobileNumber] nvarchar(20) NOT NULL,
        [IssuedDeviceID] nvarchar(100) NULL,
        [Email] nvarchar(256) NOT NULL,
        [Location] nvarchar(200) NULL,
        [OrganizationId] int NULL,
        [OfficeId] int NULL,
        [SubOfficeId] int NULL,
        [IsActive] bit NOT NULL,
        [SupervisorIndexNumber] nvarchar(50) NULL,
        [SupervisorName] nvarchar(200) NULL,
        [SupervisorEmail] nvarchar(256) NULL,
        [CreatedDate] datetime2 NOT NULL,
        [LastModifiedDate] datetime2 NULL,
        [ApplicationUserId] nvarchar(450) NULL,
        [HasLoginAccount] bit NOT NULL,
        [LoginEnabled] bit NOT NULL,
        CONSTRAINT [PK_EbillUsers] PRIMARY KEY ([Id]),
        CONSTRAINT [AK_EbillUsers_IndexNumber] UNIQUE ([IndexNumber]),
        CONSTRAINT [FK_EbillUsers_Offices_OfficeId] FOREIGN KEY ([OfficeId]) REFERENCES [Offices] ([Id]),
        CONSTRAINT [FK_EbillUsers_Organizations_OrganizationId] FOREIGN KEY ([OrganizationId]) REFERENCES [Organizations] ([Id]),
        CONSTRAINT [FK_EbillUsers_SubOffices_SubOfficeId] FOREIGN KEY ([SubOfficeId]) REFERENCES [SubOffices] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUsers] (
        [Id] nvarchar(450) NOT NULL,
        [FirstName] nvarchar(max) NULL,
        [LastName] nvarchar(max) NULL,
        [RequirePasswordChange] bit NOT NULL,
        [Status] int NOT NULL,
        [AzureAdObjectId] nvarchar(100) NULL,
        [AzureAdTenantId] nvarchar(100) NULL,
        [AzureAdUpn] nvarchar(200) NULL,
        [EbillUserId] int NULL,
        [OrganizationId] int NULL,
        [OfficeId] int NULL,
        [SubOfficeId] int NULL,
        [UserName] nvarchar(256) NULL,
        [NormalizedUserName] nvarchar(256) NULL,
        [Email] nvarchar(256) NULL,
        [NormalizedEmail] nvarchar(256) NULL,
        [EmailConfirmed] bit NOT NULL,
        [PasswordHash] nvarchar(max) NULL,
        [SecurityStamp] nvarchar(max) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        [PhoneNumber] nvarchar(max) NULL,
        [PhoneNumberConfirmed] bit NOT NULL,
        [TwoFactorEnabled] bit NOT NULL,
        [LockoutEnd] datetimeoffset NULL,
        [LockoutEnabled] bit NOT NULL,
        [AccessFailedCount] int NOT NULL,
        CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetUsers_EbillUsers_EbillUserId] FOREIGN KEY ([EbillUserId]) REFERENCES [EbillUsers] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_AspNetUsers_Offices_OfficeId] FOREIGN KEY ([OfficeId]) REFERENCES [Offices] ([Id]),
        CONSTRAINT [FK_AspNetUsers_Organizations_OrganizationId] FOREIGN KEY ([OrganizationId]) REFERENCES [Organizations] ([Id]),
        CONSTRAINT [FK_AspNetUsers_SubOffices_SubOfficeId] FOREIGN KEY ([SubOfficeId]) REFERENCES [SubOffices] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE TABLE [UserPhones] (
        [Id] int NOT NULL IDENTITY,
        [PublicId] uniqueidentifier NOT NULL,
        [IndexNumber] nvarchar(50) NOT NULL,
        [PhoneNumber] nvarchar(20) NOT NULL,
        [PhoneType] nvarchar(50) NOT NULL,
        [IsPrimary] bit NOT NULL,
        [LineType] int NOT NULL,
        [IsActive] bit NOT NULL,
        [Status] int NOT NULL,
        [AssignedDate] datetime2 NOT NULL,
        [UnassignedDate] datetime2 NULL,
        [Location] nvarchar(200) NULL,
        [Notes] nvarchar(500) NULL,
        [CreatedBy] nvarchar(100) NULL,
        [CreatedDate] datetime2 NOT NULL,
        [ClassOfServiceId] int NULL,
        CONSTRAINT [PK_UserPhones] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UserPhones_ClassOfServices_ClassOfServiceId] FOREIGN KEY ([ClassOfServiceId]) REFERENCES [ClassOfServices] ([Id]),
        CONSTRAINT [FK_UserPhones_EbillUsers_IndexNumber] FOREIGN KEY ([IndexNumber]) REFERENCES [EbillUsers] ([IndexNumber]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUserClaims] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUserLogins] (
        [LoginProvider] nvarchar(450) NOT NULL,
        [ProviderKey] nvarchar(450) NOT NULL,
        [ProviderDisplayName] nvarchar(max) NULL,
        [UserId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
        CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUserRoles] (
        [UserId] nvarchar(450) NOT NULL,
        [RoleId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
        CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUserTokens] (
        [UserId] nvarchar(450) NOT NULL,
        [LoginProvider] nvarchar(450) NOT NULL,
        [Name] nvarchar(450) NOT NULL,
        [Value] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
        CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE TABLE [Notifications] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(450) NOT NULL,
        [Title] nvarchar(200) NOT NULL,
        [Message] nvarchar(1000) NOT NULL,
        [Type] nvarchar(50) NOT NULL,
        [IsRead] bit NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [ReadDate] datetime2 NULL,
        [Link] nvarchar(500) NULL,
        [RelatedEntityType] nvarchar(100) NULL,
        [RelatedEntityId] nvarchar(100) NULL,
        [Icon] nvarchar(50) NULL,
        CONSTRAINT [PK_Notifications] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Notifications_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE TABLE [Airtel] (
        [Id] int NOT NULL IDENTITY,
        [Ext] nvarchar(50) NULL,
        [call_date] datetime2 NULL,
        [call_time] time NULL,
        [Dialed] nvarchar(100) NULL,
        [Dest] nvarchar(200) NULL,
        [Durx] decimal(18,2) NULL,
        [Cost] decimal(18,2) NULL,
        [AmountUSD] decimal(18,4) NULL,
        [Dur] decimal(18,2) NULL,
        [call_type] nvarchar(50) NULL,
        [call_month] int NULL,
        [call_year] int NULL,
        [IndexNumber] nvarchar(50) NULL,
        [UserPhoneId] int NULL,
        [CreatedDate] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedDate] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [EbillUserId] int NULL,
        [ImportAuditId] int NULL,
        [ProcessingStatus] int NOT NULL,
        [ProcessedDate] datetime2 NULL,
        [StagingBatchId] uniqueidentifier NULL,
        [BillingPeriod] nvarchar(20) NULL,
        CONSTRAINT [PK_Airtel] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Airtel_EbillUsers_EbillUserId] FOREIGN KEY ([EbillUserId]) REFERENCES [EbillUsers] ([Id]),
        CONSTRAINT [FK_Airtel_ImportAudits_ImportAuditId] FOREIGN KEY ([ImportAuditId]) REFERENCES [ImportAudits] ([Id]),
        CONSTRAINT [FK_Airtel_UserPhones_UserPhoneId] FOREIGN KEY ([UserPhoneId]) REFERENCES [UserPhones] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE TABLE [CallLogs] (
        [Id] int NOT NULL IDENTITY,
        [AccountNo] nvarchar(20) NOT NULL,
        [SubAccountNo] nvarchar(50) NOT NULL,
        [SubAccountName] nvarchar(200) NOT NULL,
        [MSISDN] nvarchar(20) NOT NULL,
        [TaxInvoiceSummaryNo] nvarchar(50) NOT NULL,
        [InvoiceNo] nvarchar(50) NOT NULL,
        [InvoiceDate] datetime2 NOT NULL,
        [NetAccessFee] decimal(18,2) NOT NULL,
        [NetUsageLessTax] decimal(18,2) NOT NULL,
        [LessTaxes] decimal(18,2) NOT NULL,
        [VAT16] decimal(18,2) NULL,
        [Excise15] decimal(18,2) NULL,
        [GrossTotal] decimal(18,2) NOT NULL,
        [EbillUserId] int NULL,
        [UserPhoneId] int NULL,
        [CreatedDate] datetime2 NOT NULL,
        [ImportedBy] nvarchar(max) NULL,
        [ImportedDate] datetime2 NULL,
        CONSTRAINT [PK_CallLogs] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CallLogs_EbillUsers_EbillUserId] FOREIGN KEY ([EbillUserId]) REFERENCES [EbillUsers] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_CallLogs_UserPhones_UserPhoneId] FOREIGN KEY ([UserPhoneId]) REFERENCES [UserPhones] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE TABLE [CallLogStagings] (
        [Id] int NOT NULL IDENTITY,
        [ExtensionNumber] nvarchar(50) NOT NULL,
        [CallDate] datetime2 NOT NULL,
        [CallNumber] nvarchar(50) NOT NULL,
        [CallDestination] nvarchar(100) NOT NULL,
        [CallEndTime] datetime2 NOT NULL,
        [CallDuration] int NOT NULL,
        [CallCurrencyCode] nvarchar(10) NOT NULL,
        [CallCost] decimal(18,4) NOT NULL,
        [CallCostUSD] decimal(18,4) NOT NULL,
        [CallCostKSHS] decimal(18,4) NOT NULL,
        [CallType] nvarchar(50) NOT NULL,
        [CallDestinationType] nvarchar(50) NOT NULL,
        [CallYear] int NOT NULL,
        [CallMonth] int NOT NULL,
        [ResponsibleIndexNumber] nvarchar(50) NULL,
        [PayingIndexNumber] nvarchar(50) NULL,
        [UserPhoneId] int NULL,
        [BillingPeriodId] int NULL,
        [ImportType] nvarchar(20) NOT NULL,
        [IsAdjustment] bit NOT NULL,
        [OriginalRecordId] int NULL,
        [AdjustmentReason] nvarchar(500) NULL,
        [SourceSystem] nvarchar(50) NOT NULL,
        [SourceRecordId] nvarchar(100) NULL,
        [BatchId] uniqueidentifier NOT NULL,
        [ImportedDate] datetime2 NOT NULL,
        [ImportedBy] nvarchar(100) NOT NULL,
        [VerificationStatus] int NOT NULL,
        [VerificationDate] datetime2 NULL,
        [VerifiedBy] nvarchar(100) NULL,
        [VerificationNotes] nvarchar(max) NULL,
        [HasAnomalies] bit NOT NULL,
        [AnomalyTypes] nvarchar(max) NULL,
        [AnomalyDetails] nvarchar(max) NULL,
        [ProcessingStatus] int NOT NULL,
        [ProcessedDate] datetime2 NULL,
        [ErrorDetails] nvarchar(max) NULL,
        [CreatedDate] datetime2 NOT NULL,
        [ModifiedDate] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_CallLogStagings] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CallLogStagings_BillingPeriods_BillingPeriodId] FOREIGN KEY ([BillingPeriodId]) REFERENCES [BillingPeriods] ([Id]),
        CONSTRAINT [FK_CallLogStagings_EbillUsers_PayingIndexNumber] FOREIGN KEY ([PayingIndexNumber]) REFERENCES [EbillUsers] ([IndexNumber]),
        CONSTRAINT [FK_CallLogStagings_EbillUsers_ResponsibleIndexNumber] FOREIGN KEY ([ResponsibleIndexNumber]) REFERENCES [EbillUsers] ([IndexNumber]),
        CONSTRAINT [FK_CallLogStagings_StagingBatches_BatchId] FOREIGN KEY ([BatchId]) REFERENCES [StagingBatches] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_CallLogStagings_UserPhones_UserPhoneId] FOREIGN KEY ([UserPhoneId]) REFERENCES [UserPhones] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE TABLE [CallRecords] (
        [Id] int NOT NULL IDENTITY,
        [ext_no] nvarchar(50) NOT NULL,
        [call_date] datetime2 NOT NULL,
        [call_number] nvarchar(50) NOT NULL,
        [call_destination] nvarchar(100) NOT NULL,
        [call_endtime] datetime2 NOT NULL,
        [call_duration] int NOT NULL,
        [call_curr_code] nvarchar(10) NOT NULL,
        [call_cost] decimal(18,4) NOT NULL,
        [call_cost_usd] decimal(18,4) NOT NULL,
        [call_cost_kshs] decimal(18,4) NOT NULL,
        [call_type] nvarchar(50) NOT NULL,
        [call_dest_type] nvarchar(50) NOT NULL,
        [call_year] int NOT NULL,
        [call_month] int NOT NULL,
        [ext_resp_index] nvarchar(50) NULL,
        [call_pay_index] nvarchar(50) NULL,
        [call_ver_ind] bit NOT NULL,
        [call_ver_date] datetime2 NULL,
        [verification_period] datetime2 NULL,
        [approval_period] datetime2 NULL,
        [revert_count] int NOT NULL,
        [last_revert_date] datetime2 NULL,
        [revert_reason] nvarchar(500) NULL,
        [verification_type] nvarchar(20) NULL,
        [payment_assignment_id] int NULL,
        [assignment_status] nvarchar(20) NOT NULL,
        [overage_justified] bit NOT NULL,
        [supervisor_approval_status] nvarchar(20) NULL,
        [supervisor_approved_by] nvarchar(50) NULL,
        [supervisor_approved_date] datetime2 NULL,
        [recovery_status] nvarchar(50) NULL,
        [recovery_date] datetime2 NULL,
        [recovery_processed_by] nvarchar(100) NULL,
        [final_assignment_type] nvarchar(50) NULL,
        [recovery_amount] decimal(18,2) NULL,
        [call_cert_ind] bit NOT NULL,
        [call_cert_date] datetime2 NULL,
        [call_cert_by] nvarchar(100) NULL,
        [call_proc_ind] bit NOT NULL,
        [entry_date] datetime2 NOT NULL,
        [call_dest_descr] nvarchar(200) NULL,
        [SourceSystem] nvarchar(50) NULL,
        [SourceBatchId] uniqueidentifier NULL,
        [SourceStagingId] int NULL,
        [UserPhoneId] int NULL,
        CONSTRAINT [PK_CallRecords] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CallRecords_EbillUsers_call_pay_index] FOREIGN KEY ([call_pay_index]) REFERENCES [EbillUsers] ([IndexNumber]),
        CONSTRAINT [FK_CallRecords_EbillUsers_ext_resp_index] FOREIGN KEY ([ext_resp_index]) REFERENCES [EbillUsers] ([IndexNumber]),
        CONSTRAINT [FK_CallRecords_UserPhones_UserPhoneId] FOREIGN KEY ([UserPhoneId]) REFERENCES [UserPhones] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE TABLE [PhoneOverageJustifications] (
        [Id] int NOT NULL IDENTITY,
        [UserPhoneId] int NOT NULL,
        [Month] int NOT NULL,
        [Year] int NOT NULL,
        [AllowanceLimit] decimal(18,4) NOT NULL,
        [TotalUsage] decimal(18,4) NOT NULL,
        [OverageAmount] decimal(18,4) NOT NULL,
        [JustificationText] nvarchar(max) NOT NULL,
        [SubmittedBy] nvarchar(50) NOT NULL,
        [SubmittedDate] datetime2 NOT NULL,
        [ApprovalStatus] nvarchar(20) NULL,
        [ApprovedBy] nvarchar(50) NULL,
        [ApprovedDate] datetime2 NULL,
        [ApprovalComments] nvarchar(500) NULL,
        CONSTRAINT [PK_PhoneOverageJustifications] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PhoneOverageJustifications_UserPhones_UserPhoneId] FOREIGN KEY ([UserPhoneId]) REFERENCES [UserPhones] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE TABLE [PrivateWires] (
        [Id] int NOT NULL IDENTITY,
        [Extension] nvarchar(50) NULL,
        [DestinationLine] nvarchar(50) NULL,
        [DurationExtended] decimal(18,2) NULL,
        [DialedNumber] nvarchar(100) NULL,
        [CallTime] time NULL,
        [Destination] nvarchar(200) NULL,
        [AmountUSD] decimal(18,4) NULL,
        [AmountKSH] decimal(18,4) NULL,
        [CallDate] datetime2 NULL,
        [CallMonth] int NOT NULL,
        [CallYear] int NOT NULL,
        [Duration] decimal(18,2) NULL,
        [IndexNumber] nvarchar(50) NULL,
        [UserPhoneId] int NULL,
        [CreatedDate] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedDate] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [EbillUserId] int NULL,
        [ImportAuditId] int NULL,
        [ProcessingStatus] int NOT NULL,
        [ProcessedDate] datetime2 NULL,
        [StagingBatchId] uniqueidentifier NULL,
        [BillingPeriod] nvarchar(20) NULL,
        CONSTRAINT [PK_PrivateWires] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PrivateWires_EbillUsers_EbillUserId] FOREIGN KEY ([EbillUserId]) REFERENCES [EbillUsers] ([Id]),
        CONSTRAINT [FK_PrivateWires_ImportAudits_ImportAuditId] FOREIGN KEY ([ImportAuditId]) REFERENCES [ImportAudits] ([Id]),
        CONSTRAINT [FK_PrivateWires_UserPhones_UserPhoneId] FOREIGN KEY ([UserPhoneId]) REFERENCES [UserPhones] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE TABLE [PSTNs] (
        [Id] int NOT NULL IDENTITY,
        [Extension] nvarchar(50) NULL,
        [DialedNumber] nvarchar(100) NULL,
        [CallTime] time NULL,
        [Destination] nvarchar(200) NULL,
        [DestinationLine] nvarchar(50) NULL,
        [DurationExtended] decimal(18,2) NULL,
        [Duration] decimal(18,2) NULL,
        [CallDate] datetime2 NULL,
        [CallMonth] int NOT NULL,
        [CallYear] int NOT NULL,
        [AmountKSH] decimal(18,2) NULL,
        [AmountUSD] decimal(18,4) NULL,
        [IndexNumber] nvarchar(50) NULL,
        [UserPhoneId] int NULL,
        [Carrier] nvarchar(50) NULL,
        [CreatedDate] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedDate] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [EbillUserId] int NULL,
        [ImportAuditId] int NULL,
        [ProcessingStatus] int NOT NULL,
        [ProcessedDate] datetime2 NULL,
        [StagingBatchId] uniqueidentifier NULL,
        [BillingPeriod] nvarchar(20) NULL,
        CONSTRAINT [PK_PSTNs] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PSTNs_EbillUsers_EbillUserId] FOREIGN KEY ([EbillUserId]) REFERENCES [EbillUsers] ([Id]),
        CONSTRAINT [FK_PSTNs_ImportAudits_ImportAuditId] FOREIGN KEY ([ImportAuditId]) REFERENCES [ImportAudits] ([Id]),
        CONSTRAINT [FK_PSTNs_UserPhones_UserPhoneId] FOREIGN KEY ([UserPhoneId]) REFERENCES [UserPhones] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE TABLE [Safaricom] (
        [Id] int NOT NULL IDENTITY,
        [Ext] nvarchar(50) NULL,
        [call_date] datetime2 NULL,
        [call_time] time NULL,
        [Dialed] nvarchar(100) NULL,
        [Dest] nvarchar(200) NULL,
        [Durx] decimal(18,2) NULL,
        [Cost] decimal(18,2) NULL,
        [AmountUSD] decimal(18,4) NULL,
        [Dur] decimal(18,2) NULL,
        [call_type] nvarchar(50) NULL,
        [call_month] int NULL,
        [call_year] int NULL,
        [IndexNumber] nvarchar(50) NULL,
        [UserPhoneId] int NULL,
        [CreatedDate] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedDate] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [EbillUserId] int NULL,
        [ImportAuditId] int NULL,
        [ProcessingStatus] int NOT NULL,
        [ProcessedDate] datetime2 NULL,
        [StagingBatchId] uniqueidentifier NULL,
        [BillingPeriod] nvarchar(20) NULL,
        CONSTRAINT [PK_Safaricom] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Safaricom_EbillUsers_EbillUserId] FOREIGN KEY ([EbillUserId]) REFERENCES [EbillUsers] ([Id]),
        CONSTRAINT [FK_Safaricom_ImportAudits_ImportAuditId] FOREIGN KEY ([ImportAuditId]) REFERENCES [ImportAudits] ([Id]),
        CONSTRAINT [FK_Safaricom_UserPhones_UserPhoneId] FOREIGN KEY ([UserPhoneId]) REFERENCES [UserPhones] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE TABLE [UserPhoneHistories] (
        [Id] int NOT NULL IDENTITY,
        [UserPhoneId] int NOT NULL,
        [Action] nvarchar(100) NOT NULL,
        [FieldChanged] nvarchar(100) NULL,
        [OldValue] nvarchar(500) NULL,
        [NewValue] nvarchar(500) NULL,
        [Description] nvarchar(1000) NULL,
        [ChangedBy] nvarchar(200) NULL,
        [ChangedDate] datetime2 NOT NULL,
        [IPAddress] nvarchar(50) NULL,
        [UserAgent] nvarchar(500) NULL,
        CONSTRAINT [PK_UserPhoneHistories] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UserPhoneHistories_UserPhones_UserPhoneId] FOREIGN KEY ([UserPhoneId]) REFERENCES [UserPhones] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE TABLE [CallLogPaymentAssignments] (
        [Id] int NOT NULL IDENTITY,
        [CallRecordId] int NOT NULL,
        [AssignedFrom] nvarchar(50) NOT NULL,
        [AssignedTo] nvarchar(50) NOT NULL,
        [AssignmentReason] nvarchar(500) NOT NULL,
        [AssignedDate] datetime2 NOT NULL,
        [AssignmentStatus] nvarchar(20) NOT NULL,
        [AcceptedDate] datetime2 NULL,
        [RejectionReason] nvarchar(500) NULL,
        [NotificationSent] bit NOT NULL,
        [NotificationSentDate] datetime2 NULL,
        [NotificationViewedDate] datetime2 NULL,
        [CreatedDate] datetime2 NOT NULL,
        [ModifiedDate] datetime2 NULL,
        CONSTRAINT [PK_CallLogPaymentAssignments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CallLogPaymentAssignments_CallRecords_CallRecordId] FOREIGN KEY ([CallRecordId]) REFERENCES [CallRecords] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE TABLE [RecoveryLogs] (
        [Id] int NOT NULL IDENTITY,
        [CallRecordId] int NOT NULL,
        [BatchId] uniqueidentifier NOT NULL,
        [RecoveryType] nvarchar(50) NOT NULL,
        [RecoveryAction] nvarchar(50) NOT NULL,
        [RecoveryDate] datetime2 NOT NULL,
        [RecoveryReason] nvarchar(1000) NOT NULL,
        [AmountRecovered] decimal(18,2) NOT NULL,
        [RecoveredFrom] nvarchar(100) NULL,
        [ProcessedBy] nvarchar(100) NULL,
        [DeadlineDate] datetime2 NULL,
        [IsAutomated] bit NOT NULL,
        [Metadata] nvarchar(max) NULL,
        CONSTRAINT [PK_RecoveryLogs] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RecoveryLogs_CallRecords_CallRecordId] FOREIGN KEY ([CallRecordId]) REFERENCES [CallRecords] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_RecoveryLogs_StagingBatches_BatchId] FOREIGN KEY ([BatchId]) REFERENCES [StagingBatches] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE TABLE [PhoneOverageDocuments] (
        [Id] int NOT NULL IDENTITY,
        [PhoneOverageJustificationId] int NOT NULL,
        [FileName] nvarchar(255) NOT NULL,
        [FilePath] nvarchar(500) NOT NULL,
        [FileSize] bigint NOT NULL,
        [ContentType] nvarchar(100) NOT NULL,
        [Description] nvarchar(500) NULL,
        [UploadedBy] nvarchar(50) NOT NULL,
        [UploadedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_PhoneOverageDocuments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PhoneOverageDocuments_PhoneOverageJustifications_PhoneOverageJustificationId] FOREIGN KEY ([PhoneOverageJustificationId]) REFERENCES [PhoneOverageJustifications] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE TABLE [CallLogVerifications] (
        [Id] int NOT NULL IDENTITY,
        [CallRecordId] int NOT NULL,
        [VerifiedBy] nvarchar(50) NOT NULL,
        [VerifiedDate] datetime2 NOT NULL,
        [VerificationType] int NOT NULL,
        [ClassOfServiceId] int NULL,
        [AllowanceAmount] decimal(18,4) NULL,
        [ActualAmount] decimal(18,4) NOT NULL,
        [IsOverage] bit NOT NULL,
        [OverageAmount] decimal(18,4) NOT NULL,
        [OverageJustified] bit NOT NULL,
        [JustificationText] nvarchar(max) NULL,
        [SupportingDocuments] nvarchar(max) NULL,
        [PaymentAssignmentId] int NULL,
        [ApprovalStatus] nvarchar(20) NOT NULL,
        [SubmittedToSupervisor] bit NOT NULL,
        [SubmittedDate] datetime2 NULL,
        [SupervisorIndexNumber] nvarchar(50) NULL,
        [SupervisorApprovalStatus] nvarchar(20) NULL,
        [SupervisorApprovedBy] nvarchar(50) NULL,
        [SupervisorApprovedDate] datetime2 NULL,
        [SupervisorComments] nvarchar(500) NULL,
        [ApprovedAmount] decimal(18,4) NULL,
        [RejectionReason] nvarchar(500) NULL,
        [BatchId] uniqueidentifier NULL,
        [SubmissionDeadline] datetime2 NULL,
        [ApprovalDeadline] datetime2 NULL,
        [DeadlineMissed] bit NOT NULL,
        [RevertDeadline] datetime2 NULL,
        [RevertCount] int NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [ModifiedDate] datetime2 NULL,
        CONSTRAINT [PK_CallLogVerifications] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CallLogVerifications_CallLogPaymentAssignments_PaymentAssignmentId] FOREIGN KEY ([PaymentAssignmentId]) REFERENCES [CallLogPaymentAssignments] ([Id]),
        CONSTRAINT [FK_CallLogVerifications_CallRecords_CallRecordId] FOREIGN KEY ([CallRecordId]) REFERENCES [CallRecords] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_CallLogVerifications_ClassOfServices_ClassOfServiceId] FOREIGN KEY ([ClassOfServiceId]) REFERENCES [ClassOfServices] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_CallLogVerifications_StagingBatches_BatchId] FOREIGN KEY ([BatchId]) REFERENCES [StagingBatches] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE TABLE [CallLogDocuments] (
        [Id] int NOT NULL IDENTITY,
        [CallLogVerificationId] int NOT NULL,
        [FileName] nvarchar(255) NOT NULL,
        [FilePath] nvarchar(500) NOT NULL,
        [FileSize] bigint NOT NULL,
        [ContentType] nvarchar(100) NOT NULL,
        [DocumentType] nvarchar(50) NOT NULL,
        [Description] nvarchar(500) NULL,
        [UploadedBy] nvarchar(50) NOT NULL,
        [UploadedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_CallLogDocuments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CallLogDocuments_CallLogVerifications_CallLogVerificationId] FOREIGN KEY ([CallLogVerificationId]) REFERENCES [CallLogVerifications] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Airtel_EbillUserId] ON [Airtel] ([EbillUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Airtel_ImportAuditId] ON [Airtel] ([ImportAuditId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Airtel_UserPhoneId] ON [Airtel] ([UserPhoneId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_AnomalyTypes_Code] ON [AnomalyTypes] ([Code]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_AspNetUsers_EbillUserId] ON [AspNetUsers] ([EbillUserId]) WHERE [EbillUserId] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AspNetUsers_OfficeId] ON [AspNetUsers] ([OfficeId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AspNetUsers_OrganizationId] ON [AspNetUsers] ([OrganizationId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AspNetUsers_SubOfficeId] ON [AspNetUsers] ([SubOfficeId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_CallLogDocuments_CallLogVerificationId] ON [CallLogDocuments] ([CallLogVerificationId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_CallLogPaymentAssignments_AssignedFrom_AssignedTo] ON [CallLogPaymentAssignments] ([AssignedFrom], [AssignedTo]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_CallLogPaymentAssignments_AssignedTo] ON [CallLogPaymentAssignments] ([AssignedTo]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_CallLogPaymentAssignments_AssignmentStatus] ON [CallLogPaymentAssignments] ([AssignmentStatus]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_CallLogPaymentAssignments_CallRecordId] ON [CallLogPaymentAssignments] ([CallRecordId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_CallLogReconciliations_BillingPeriodId] ON [CallLogReconciliations] ([BillingPeriodId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_CallLogReconciliations_SupersededBy] ON [CallLogReconciliations] ([SupersededBy]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_CallLogs_EbillUserId] ON [CallLogs] ([EbillUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_CallLogs_MSISDN] ON [CallLogs] ([MSISDN]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_CallLogs_UserPhoneId] ON [CallLogs] ([UserPhoneId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_CallLogStagings_BatchId] ON [CallLogStagings] ([BatchId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_CallLogStagings_BillingPeriodId] ON [CallLogStagings] ([BillingPeriodId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_CallLogStagings_CallDate] ON [CallLogStagings] ([CallDate]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_CallLogStagings_ExtensionNumber_CallDate_CallNumber] ON [CallLogStagings] ([ExtensionNumber], [CallDate], [CallNumber]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_CallLogStagings_PayingIndexNumber] ON [CallLogStagings] ([PayingIndexNumber]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_CallLogStagings_ResponsibleIndexNumber] ON [CallLogStagings] ([ResponsibleIndexNumber]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_CallLogStagings_UserPhoneId] ON [CallLogStagings] ([UserPhoneId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_CallLogStagings_VerificationStatus] ON [CallLogStagings] ([VerificationStatus]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_CallLogVerifications_ApprovalStatus] ON [CallLogVerifications] ([ApprovalStatus]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_CallLogVerifications_BatchId] ON [CallLogVerifications] ([BatchId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_CallLogVerifications_CallRecordId_VerifiedBy] ON [CallLogVerifications] ([CallRecordId], [VerifiedBy]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_CallLogVerifications_ClassOfServiceId] ON [CallLogVerifications] ([ClassOfServiceId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_CallLogVerifications_PaymentAssignmentId] ON [CallLogVerifications] ([PaymentAssignmentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_CallLogVerifications_SupervisorIndexNumber] ON [CallLogVerifications] ([SupervisorIndexNumber]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_CallLogVerifications_VerifiedBy] ON [CallLogVerifications] ([VerifiedBy]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_CallRecords_call_date] ON [CallRecords] ([call_date]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_CallRecords_call_pay_index] ON [CallRecords] ([call_pay_index]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_CallRecords_call_year_call_month] ON [CallRecords] ([call_year], [call_month]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_CallRecords_ext_no] ON [CallRecords] ([ext_no]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_CallRecords_ext_resp_index] ON [CallRecords] ([ext_resp_index]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_CallRecords_UserPhoneId] ON [CallRecords] ([UserPhoneId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_DeadlineTracking_BatchId] ON [DeadlineTracking] ([BatchId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_DeadlineTracking_DeadlineDate] ON [DeadlineTracking] ([DeadlineDate]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_DeadlineTracking_DeadlineDate_DeadlineStatus] ON [DeadlineTracking] ([DeadlineDate], [DeadlineStatus]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_DeadlineTracking_DeadlineStatus] ON [DeadlineTracking] ([DeadlineStatus]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_DeadlineTracking_DeadlineType_TargetEntity] ON [DeadlineTracking] ([DeadlineType], [TargetEntity]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_DeadlineTracking_TargetEntity] ON [DeadlineTracking] ([TargetEntity]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Ebills_ServiceProviderId] ON [Ebills] ([ServiceProviderId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_EbillUsers_Email] ON [EbillUsers] ([Email]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_EbillUsers_IndexNumber] ON [EbillUsers] ([IndexNumber]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_EbillUsers_OfficeId] ON [EbillUsers] ([OfficeId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_EbillUsers_OrganizationId] ON [EbillUsers] ([OrganizationId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_EbillUsers_SubOfficeId] ON [EbillUsers] ([SubOfficeId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_EmailAttachments_EmailLogId] ON [EmailAttachments] ([EmailLogId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_EmailConfigurations_IsActive] ON [EmailConfigurations] ([IsActive]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_EmailLogs_CreatedDate] ON [EmailLogs] ([CreatedDate]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_EmailLogs_EmailTemplateId] ON [EmailLogs] ([EmailTemplateId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_EmailLogs_RelatedEntityType_RelatedEntityId] ON [EmailLogs] ([RelatedEntityType], [RelatedEntityId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_EmailLogs_SentDate] ON [EmailLogs] ([SentDate]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_EmailLogs_Status] ON [EmailLogs] ([Status]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_EmailLogs_Status_CreatedDate] ON [EmailLogs] ([Status], [CreatedDate]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_EmailLogs_ToEmail] ON [EmailLogs] ([ToEmail]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_EmailLogs_TrackingId] ON [EmailLogs] ([TrackingId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_EmailTemplates_Category] ON [EmailTemplates] ([Category]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_EmailTemplates_IsActive] ON [EmailTemplates] ([IsActive]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_EmailTemplates_IsSystemTemplate] ON [EmailTemplates] ([IsSystemTemplate]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_EmailTemplates_TemplateCode] ON [EmailTemplates] ([TemplateCode]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ExchangeRates_Month_Year] ON [ExchangeRates] ([Month], [Year]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ImportAudits_ImportDate] ON [ImportAudits] ([ImportDate]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ImportAudits_ImportType] ON [ImportAudits] ([ImportType]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_InterimUpdates_BatchId] ON [InterimUpdates] ([BatchId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_InterimUpdates_BillingPeriodId] ON [InterimUpdates] ([BillingPeriodId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Notifications_UserId] ON [Notifications] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Offices_OrganizationId] ON [Offices] ([OrganizationId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Organizations_Name] ON [Organizations] ([Name]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_PhoneOverageDocuments_PhoneOverageJustificationId] ON [PhoneOverageDocuments] ([PhoneOverageJustificationId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_PhoneOverageJustifications_ApprovalStatus] ON [PhoneOverageJustifications] ([ApprovalStatus]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_PhoneOverageJustifications_Month_Year] ON [PhoneOverageJustifications] ([Month], [Year]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_PhoneOverageJustifications_UserPhoneId] ON [PhoneOverageJustifications] ([UserPhoneId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PhoneOverageJustifications_UserPhoneId_Month_Year] ON [PhoneOverageJustifications] ([UserPhoneId], [Month], [Year]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_PrivateWires_EbillUserId] ON [PrivateWires] ([EbillUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_PrivateWires_ImportAuditId] ON [PrivateWires] ([ImportAuditId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_PrivateWires_UserPhoneId] ON [PrivateWires] ([UserPhoneId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_PSTNs_EbillUserId] ON [PSTNs] ([EbillUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_PSTNs_ImportAuditId] ON [PSTNs] ([ImportAuditId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_PSTNs_UserPhoneId] ON [PSTNs] ([UserPhoneId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_RecoveryConfiguration_IsEnabled] ON [RecoveryConfiguration] ([IsEnabled]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_RecoveryConfiguration_RuleName] ON [RecoveryConfiguration] ([RuleName]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_RecoveryConfiguration_RuleType] ON [RecoveryConfiguration] ([RuleType]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_RecoveryLogs_BatchId] ON [RecoveryLogs] ([BatchId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_RecoveryLogs_CallRecordId] ON [RecoveryLogs] ([CallRecordId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_RecoveryLogs_RecoveredFrom] ON [RecoveryLogs] ([RecoveredFrom]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_RecoveryLogs_RecoveryDate] ON [RecoveryLogs] ([RecoveryDate]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_RecoveryLogs_RecoveryDate_RecoveryType] ON [RecoveryLogs] ([RecoveryDate], [RecoveryType]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_RecoveryLogs_RecoveryType] ON [RecoveryLogs] ([RecoveryType]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Safaricom_EbillUserId] ON [Safaricom] ([EbillUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Safaricom_ImportAuditId] ON [Safaricom] ([ImportAuditId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Safaricom_UserPhoneId] ON [Safaricom] ([UserPhoneId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ServiceProviders_SPID] ON [ServiceProviders] ([SPID]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_SimRequestHistories_SimRequestId] ON [SimRequestHistories] ([SimRequestId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_SimRequestHistories_Timestamp] ON [SimRequestHistories] ([Timestamp]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_SimRequests_IndexNo] ON [SimRequests] ([IndexNo]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_SimRequests_ServiceProviderId] ON [SimRequests] ([ServiceProviderId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_StagingBatches_BatchStatus] ON [StagingBatches] ([BatchStatus]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_StagingBatches_BillingPeriodId] ON [StagingBatches] ([BillingPeriodId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_StagingBatches_CreatedDate] ON [StagingBatches] ([CreatedDate]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_SubOffices_OfficeId] ON [SubOffices] ([OfficeId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_UserPhoneHistories_UserPhoneId] ON [UserPhoneHistories] ([UserPhoneId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_UserPhones_ClassOfServiceId] ON [UserPhones] ([ClassOfServiceId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_UserPhones_IndexNumber] ON [UserPhones] ([IndexNumber]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_UserPhones_IndexNumber_PhoneNumber_IsActive] ON [UserPhones] ([IndexNumber], [PhoneNumber], [IsActive]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_UserPhones_PhoneNumber] ON [UserPhones] ([PhoneNumber]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107094921_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251107094921_InitialCreate', N'8.0.6');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251112153351_AddAzureAdProfileFields'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [CompanyName] nvarchar(200) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251112153351_AddAzureAdProfileFields'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [Department] nvarchar(200) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251112153351_AddAzureAdProfileFields'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [JobTitle] nvarchar(200) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251112153351_AddAzureAdProfileFields'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [MobilePhone] nvarchar(50) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251112153351_AddAzureAdProfileFields'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [OfficeLocation] nvarchar(200) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251112153351_AddAzureAdProfileFields'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251112153351_AddAzureAdProfileFields', N'8.0.6');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251112155719_IncreaseCallTypeColumnLength'
)
BEGIN
    DECLARE @var0 sysname;
    SELECT @var0 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Safaricom]') AND [c].[name] = N'call_type');
    IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [Safaricom] DROP CONSTRAINT [' + @var0 + '];');
    ALTER TABLE [Safaricom] ALTER COLUMN [call_type] nvarchar(100) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251112155719_IncreaseCallTypeColumnLength'
)
BEGIN
    DECLARE @var1 sysname;
    SELECT @var1 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[CallRecords]') AND [c].[name] = N'call_type');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [CallRecords] DROP CONSTRAINT [' + @var1 + '];');
    ALTER TABLE [CallRecords] ALTER COLUMN [call_type] nvarchar(100) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251112155719_IncreaseCallTypeColumnLength'
)
BEGIN
    DECLARE @var2 sysname;
    SELECT @var2 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Airtel]') AND [c].[name] = N'call_type');
    IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [Airtel] DROP CONSTRAINT [' + @var2 + '];');
    ALTER TABLE [Airtel] ALTER COLUMN [call_type] nvarchar(100) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251112155719_IncreaseCallTypeColumnLength'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251112155719_IncreaseCallTypeColumnLength', N'8.0.6');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251112161045_IncreaseCallLogStagingCallTypeLength'
)
BEGIN
    DECLARE @var3 sysname;
    SELECT @var3 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[CallLogStagings]') AND [c].[name] = N'CallType');
    IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [CallLogStagings] DROP CONSTRAINT [' + @var3 + '];');
    ALTER TABLE [CallLogStagings] ALTER COLUMN [CallType] nvarchar(100) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251112161045_IncreaseCallLogStagingCallTypeLength'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251112161045_IncreaseCallLogStagingCallTypeLength', N'8.0.6');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251114083309_AddBatchStoredProcedures'
)
BEGIN

    CREATE PROCEDURE sp_ConsolidateCallLogBatch
        @BatchId UNIQUEIDENTIFIER,
        @StartMonth INT,
        @StartYear INT,
        @EndMonth INT,
        @EndYear INT,
        @CreatedBy NVARCHAR(256) = 'System'
    AS
    BEGIN
        SET NOCOUNT ON;
        SET XACT_ABORT ON;

        DECLARE @TotalImported INT = 0;
        DECLARE @SafaricomCount INT = 0;
        DECLARE @AirtelCount INT = 0;
        DECLARE @PSTNCount INT = 0;
        DECLARE @PrivateWireCount INT = 0;
        DECLARE @ErrorMessage NVARCHAR(4000);
        DECLARE @ErrorSeverity INT;
        DECLARE @ErrorState INT;

        BEGIN TRY
            BEGIN TRANSACTION;

            -- Import from Safaricom (using correct column names: call_date, call_month, call_year, call_type)
            INSERT INTO CallLogStagings (BatchId, ImportType, ExtensionNumber, CallDate, CallNumber, CallDestination, CallEndTime, CallDuration, CallCurrencyCode, CallCost, CallCostUSD, CallCostKSHS, CallType, CallDestinationType, CallMonth, CallYear, ResponsibleIndexNumber, UserPhoneId, SourceSystem, SourceRecordId, ImportedBy, ImportedDate, VerificationStatus, ProcessingStatus, CreatedDate)
            SELECT @BatchId, 'Batch', ISNULL(s.Ext, ''), ISNULL(s.call_date, '1900-01-01'), ISNULL(s.Dialed, ''), ISNULL(s.Dest, ''), DATEADD(SECOND, ISNULL(s.Dur, 0) * 60, ISNULL(s.call_date, '1900-01-01')), ISNULL(s.Dur, 0) * 60, 'KES', ISNULL(s.Cost, 0), ISNULL(s.Cost, 0) / 150.0, ISNULL(s.Cost, 0), ISNULL(s.call_type, 'Voice'), CASE WHEN s.Dest LIKE '254%' OR s.Dest LIKE '0%' THEN 'Domestic' WHEN s.Dest LIKE '+%' AND s.Dest NOT LIKE '+254%' THEN 'International' WHEN s.Dest LIKE '00%' THEN 'International' ELSE 'Unknown' END, ISNULL(s.call_month, @StartMonth), ISNULL(s.call_year, @StartYear), ISNULL(up.IndexNumber, s.IndexNumber), up.Id, 'Safaricom', CAST(s.Id AS NVARCHAR(50)), @CreatedBy, GETUTCDATE(), 0, 0, GETUTCDATE()
            FROM Safaricom s LEFT JOIN UserPhones up ON (up.PhoneNumber = s.Ext OR up.PhoneNumber = REPLACE(s.Ext, '+254', '0')) AND up.IsActive = 1
            WHERE s.call_month >= @StartMonth AND s.call_month <= @EndMonth AND s.call_year >= @StartYear AND s.call_year <= @EndYear AND s.StagingBatchId IS NULL;
            SET @SafaricomCount = @@ROWCOUNT;
            UPDATE s SET s.StagingBatchId = @BatchId, s.UserPhoneId = up.Id, s.ProcessingStatus = 0 FROM Safaricom s LEFT JOIN UserPhones up ON (up.PhoneNumber = s.Ext OR up.PhoneNumber = REPLACE(s.Ext, '+254', '0')) AND up.IsActive = 1 WHERE s.call_month >= @StartMonth AND s.call_month <= @EndMonth AND s.call_year >= @StartYear AND s.call_year <= @EndYear AND s.StagingBatchId = @BatchId;

            -- Import from Airtel (using correct column names: call_date, call_month, call_year, call_type)
            INSERT INTO CallLogStagings (BatchId, ImportType, ExtensionNumber, CallDate, CallNumber, CallDestination, CallEndTime, CallDuration, CallCurrencyCode, CallCost, CallCostUSD, CallCostKSHS, CallType, CallDestinationType, CallMonth, CallYear, ResponsibleIndexNumber, UserPhoneId, SourceSystem, SourceRecordId, ImportedBy, ImportedDate, VerificationStatus, ProcessingStatus, CreatedDate)
            SELECT @BatchId, 'Batch', ISNULL(a.Ext, ''), ISNULL(a.call_date, '1900-01-01'), ISNULL(a.Dialed, ''), ISNULL(a.Dest, ''), DATEADD(SECOND, ISNULL(a.Dur, 0) * 60, ISNULL(a.call_date, '1900-01-01')), ISNULL(a.Dur, 0) * 60, 'KES', ISNULL(a.Cost, 0), ISNULL(a.Cost, 0) / 150.0, ISNULL(a.Cost, 0), ISNULL(a.call_type, 'Voice'), CASE WHEN a.Dest LIKE '254%' OR a.Dest LIKE '0%' THEN 'Domestic' WHEN a.Dest LIKE '+%' AND a.Dest NOT LIKE '+254%' THEN 'International' WHEN a.Dest LIKE '00%' THEN 'International' ELSE 'Unknown' END, ISNULL(a.call_month, @StartMonth), ISNULL(a.call_year, @StartYear), ISNULL(up.IndexNumber, a.IndexNumber), up.Id, 'Airtel', CAST(a.Id AS NVARCHAR(50)), @CreatedBy, GETUTCDATE(), 0, 0, GETUTCDATE()
            FROM Airtel a LEFT JOIN UserPhones up ON (up.PhoneNumber = a.Ext OR up.PhoneNumber = REPLACE(a.Ext, '+254', '0')) AND up.IsActive = 1
            WHERE a.call_month >= @StartMonth AND a.call_month <= @EndMonth AND a.call_year >= @StartYear AND a.call_year <= @EndYear AND a.StagingBatchId IS NULL;
            SET @AirtelCount = @@ROWCOUNT;
            UPDATE a SET a.StagingBatchId = @BatchId, a.UserPhoneId = up.Id, a.ProcessingStatus = 0 FROM Airtel a LEFT JOIN UserPhones up ON (up.PhoneNumber = a.Ext OR up.PhoneNumber = REPLACE(a.Ext, '+254', '0')) AND up.IsActive = 1 WHERE a.call_month >= @StartMonth AND a.call_month <= @EndMonth AND a.call_year >= @StartYear AND a.call_year <= @EndYear AND a.StagingBatchId = @BatchId;

            -- Import from PSTN
            INSERT INTO CallLogStagings (BatchId, ImportType, ExtensionNumber, CallDate, CallNumber, CallDestination, CallEndTime, CallDuration, CallCurrencyCode, CallCost, CallCostUSD, CallCostKSHS, CallType, CallDestinationType, CallMonth, CallYear, ResponsibleIndexNumber, UserPhoneId, SourceSystem, SourceRecordId, ImportedBy, ImportedDate, VerificationStatus, ProcessingStatus, CreatedDate)
            SELECT @BatchId, 'Batch', ISNULL(p.Extension, ''), ISNULL(p.CallDate, '1900-01-01'), ISNULL(p.DialedNumber, ''), ISNULL(p.Destination, ''), DATEADD(SECOND, ISNULL(p.Duration, 0) * 60, ISNULL(p.CallDate, '1900-01-01')), ISNULL(p.Duration, 0) * 60, 'KSH', ISNULL(p.AmountKSH, 0), ISNULL(p.AmountKSH, 0) / 150.0, ISNULL(p.AmountKSH, 0), 'Voice', CASE WHEN p.Destination LIKE '254%' OR p.Destination LIKE '0%' THEN 'Domestic' WHEN p.Destination LIKE '+%' AND p.Destination NOT LIKE '+254%' THEN 'International' WHEN p.Destination LIKE '00%' THEN 'International' ELSE 'Unknown' END, CASE WHEN p.CallMonth > 0 THEN p.CallMonth ELSE @StartMonth END, CASE WHEN p.CallYear > 0 THEN p.CallYear ELSE @StartYear END, ISNULL(up.IndexNumber, p.IndexNumber), up.Id, 'PSTN', CAST(p.Id AS NVARCHAR(50)), @CreatedBy, GETUTCDATE(), 0, 0, GETUTCDATE()
            FROM PSTNs p LEFT JOIN UserPhones up ON (up.PhoneNumber = p.Extension OR up.PhoneNumber = REPLACE(p.Extension, '+254', '0')) AND up.IsActive = 1
            WHERE p.CallMonth >= @StartMonth AND p.CallMonth <= @EndMonth AND p.CallYear >= @StartYear AND p.CallYear <= @EndYear AND p.StagingBatchId IS NULL;
            SET @PSTNCount = @@ROWCOUNT;
            UPDATE p SET p.StagingBatchId = @BatchId, p.UserPhoneId = up.Id, p.ProcessingStatus = 0 FROM PSTNs p LEFT JOIN UserPhones up ON (up.PhoneNumber = p.Extension OR up.PhoneNumber = REPLACE(p.Extension, '+254', '0')) AND up.IsActive = 1 WHERE p.CallMonth >= @StartMonth AND p.CallMonth <= @EndMonth AND p.CallYear >= @StartYear AND p.CallYear <= @EndYear AND p.StagingBatchId = @BatchId;

            -- Import from PrivateWire
            INSERT INTO CallLogStagings (BatchId, ImportType, ExtensionNumber, CallDate, CallNumber, CallDestination, CallEndTime, CallDuration, CallCurrencyCode, CallCost, CallCostUSD, CallCostKSHS, CallType, CallDestinationType, CallMonth, CallYear, ResponsibleIndexNumber, UserPhoneId, SourceSystem, SourceRecordId, ImportedBy, ImportedDate, VerificationStatus, ProcessingStatus, CreatedDate)
            SELECT @BatchId, 'Batch', ISNULL(pw.Extension, ''), ISNULL(pw.CallDate, '1900-01-01'), ISNULL(pw.DialedNumber, ''), ISNULL(pw.Destination, ''), DATEADD(SECOND, ISNULL(pw.Duration, 0) * 60, ISNULL(pw.CallDate, '1900-01-01')), ISNULL(pw.Duration, 0) * 60, 'USD', ISNULL(pw.AmountKSH, 0), ISNULL(pw.AmountUSD, 0), ISNULL(pw.AmountKSH, 0), 'Voice', CASE WHEN pw.Destination LIKE '254%' OR pw.Destination LIKE '0%' THEN 'Domestic' WHEN pw.Destination LIKE '+%' AND pw.Destination NOT LIKE '+254%' THEN 'International' WHEN pw.Destination LIKE '00%' THEN 'International' ELSE 'Unknown' END, CASE WHEN pw.CallMonth > 0 THEN pw.CallMonth ELSE @StartMonth END, CASE WHEN pw.CallYear > 0 THEN pw.CallYear ELSE @StartYear END, ISNULL(up.IndexNumber, pw.IndexNumber), up.Id, 'PrivateWire', CAST(pw.Id AS NVARCHAR(50)), @CreatedBy, GETUTCDATE(), 0, 0, GETUTCDATE()
            FROM PrivateWires pw LEFT JOIN UserPhones up ON (up.PhoneNumber = pw.Extension OR up.PhoneNumber = REPLACE(pw.Extension, '+254', '0')) AND up.IsActive = 1
            WHERE pw.CallMonth >= @StartMonth AND pw.CallMonth <= @EndMonth AND pw.CallYear >= @StartYear AND pw.CallYear <= @EndYear AND pw.StagingBatchId IS NULL;
            SET @PrivateWireCount = @@ROWCOUNT;
            UPDATE pw SET pw.StagingBatchId = @BatchId, pw.UserPhoneId = up.Id, pw.ProcessingStatus = 0 FROM PrivateWires pw LEFT JOIN UserPhones up ON (up.PhoneNumber = pw.Extension OR up.PhoneNumber = REPLACE(pw.Extension, '+254', '0')) AND up.IsActive = 1 WHERE pw.CallMonth >= @StartMonth AND pw.CallMonth <= @EndMonth AND pw.CallYear >= @StartYear AND pw.CallYear <= @EndYear AND pw.StagingBatchId = @BatchId;

            SET @TotalImported = @SafaricomCount + @AirtelCount + @PSTNCount + @PrivateWireCount;

            SELECT @TotalImported AS TotalRecords, @SafaricomCount AS SafaricomRecords, @AirtelCount AS AirtelRecords, @PSTNCount AS PSTNRecords, @PrivateWireCount AS PrivateWireRecords, GETUTCDATE() AS CompletedAt;

            COMMIT TRANSACTION;
        END TRY
        BEGIN CATCH
            IF @@TRANCOUNT > 0
                ROLLBACK TRANSACTION;
            SELECT @ErrorMessage = ERROR_MESSAGE(), @ErrorSeverity = ERROR_SEVERITY(), @ErrorState = ERROR_STATE();
            RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
        END CATCH
    END
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251114083309_AddBatchStoredProcedures'
)
BEGIN

    CREATE PROCEDURE sp_DeleteBatch
        @BatchId UNIQUEIDENTIFIER,
        @DeletedBy NVARCHAR(256),
        @Result NVARCHAR(MAX) OUTPUT
    AS
    BEGIN
        SET NOCOUNT ON;
        SET XACT_ABORT ON;

        DECLARE @BatchName NVARCHAR(200), @BatchStatus INT, @TotalRecords INT, @StagingRecordsDeleted INT = 0, @SafaricomRecordsReset INT = 0, @AirtelRecordsReset INT = 0, @PSTNRecordsReset INT = 0, @PrivateWireRecordsReset INT = 0, @ErrorMessage NVARCHAR(4000), @ErrorSeverity INT, @ErrorState INT;

        BEGIN TRY
            BEGIN TRANSACTION;

            SELECT @BatchName = BatchName, @BatchStatus = BatchStatus, @TotalRecords = TotalRecords FROM StagingBatches WHERE Id = @BatchId;
            IF @BatchName IS NULL BEGIN
                SELECT 0 AS Success, NULL AS BatchName, 0 AS StagingRecordsDeleted, 0 AS SafaricomRecordsReset, 0 AS AirtelRecordsReset, 0 AS PSTNRecordsReset, 0 AS PrivateWireRecordsReset, NULL AS DeletedAt, 'Batch not found' AS Error;
                ROLLBACK TRANSACTION;
                RETURN;
            END
            IF @BatchStatus = 4 BEGIN
                SELECT 0 AS Success, @BatchName AS BatchName, 0 AS StagingRecordsDeleted, 0 AS SafaricomRecordsReset, 0 AS AirtelRecordsReset, 0 AS PSTNRecordsReset, 0 AS PrivateWireRecordsReset, NULL AS DeletedAt, 'Cannot delete batch - already published' AS Error;
                ROLLBACK TRANSACTION;
                RETURN;
            END
            IF EXISTS (SELECT 1 FROM CallLogStagings WHERE BatchId = @BatchId AND ProcessingStatus = 3) BEGIN
                SELECT 0 AS Success, @BatchName AS BatchName, 0 AS StagingRecordsDeleted, 0 AS SafaricomRecordsReset, 0 AS AirtelRecordsReset, 0 AS PSTNRecordsReset, 0 AS PrivateWireRecordsReset, NULL AS DeletedAt, 'Cannot delete batch - has production records' AS Error;
                ROLLBACK TRANSACTION;
                RETURN;
            END

            DELETE FROM CallLogStagings WHERE BatchId = @BatchId; SET @StagingRecordsDeleted = @@ROWCOUNT;
            UPDATE Safaricom SET StagingBatchId = NULL, ProcessingStatus = 0 WHERE StagingBatchId = @BatchId; SET @SafaricomRecordsReset = @@ROWCOUNT;
            UPDATE Airtel SET StagingBatchId = NULL, ProcessingStatus = 0 WHERE StagingBatchId = @BatchId; SET @AirtelRecordsReset = @@ROWCOUNT;
            UPDATE PSTNs SET StagingBatchId = NULL, ProcessingStatus = 0 WHERE StagingBatchId = @BatchId; SET @PSTNRecordsReset = @@ROWCOUNT;
            UPDATE PrivateWires SET StagingBatchId = NULL, ProcessingStatus = 0 WHERE StagingBatchId = @BatchId; SET @PrivateWireRecordsReset = @@ROWCOUNT;

            INSERT INTO AuditLogs (EntityType, EntityId, Action, Description, OldValues, PerformedBy, PerformedDate, Module, IsSuccess, AdditionalData)
            SELECT 'StagingBatch', CAST(@BatchId AS NVARCHAR(50)), 'Deleted', 'Deleted batch ''' + @BatchName + ''' with ' + CAST(@StagingRecordsDeleted AS NVARCHAR(20)) + ' records', (SELECT BatchName, BatchStatus, TotalRecords, CreatedDate FROM StagingBatches WHERE Id = @BatchId FOR JSON PATH, WITHOUT_ARRAY_WRAPPER), @DeletedBy, GETUTCDATE(), 'CallLogStaging', 1, '{}';

            DELETE FROM StagingBatches WHERE Id = @BatchId;

            SELECT 1 AS Success, @BatchName AS BatchName, @StagingRecordsDeleted AS StagingRecordsDeleted, @SafaricomRecordsReset AS SafaricomRecordsReset, @AirtelRecordsReset AS AirtelRecordsReset, @PSTNRecordsReset AS PSTNRecordsReset, @PrivateWireRecordsReset AS PrivateWireRecordsReset, GETUTCDATE() AS DeletedAt, NULL AS Error;

            COMMIT TRANSACTION;
        END TRY
        BEGIN CATCH
            IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
            SELECT @ErrorMessage = ERROR_MESSAGE(), @ErrorSeverity = ERROR_SEVERITY(), @ErrorState = ERROR_STATE();
            SELECT 0 AS Success, @BatchName AS BatchName, 0 AS StagingRecordsDeleted, 0 AS SafaricomRecordsReset, 0 AS AirtelRecordsReset, 0 AS PSTNRecordsReset, 0 AS PrivateWireRecordsReset, NULL AS DeletedAt, @ErrorMessage AS Error;
            RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
        END CATCH
    END
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251114083309_AddBatchStoredProcedures'
)
BEGIN

    CREATE PROCEDURE sp_PushBatchToProduction
        @BatchId UNIQUEIDENTIFIER,
        @VerificationPeriod DATETIME = NULL,
        @VerificationType NVARCHAR(50) = NULL,
        @ApprovalPeriod DATETIME = NULL,
        @PublishedBy NVARCHAR(256) = 'System'
    AS
    BEGIN
        SET NOCOUNT ON;
        SET XACT_ABORT ON;

        DECLARE @VerifiedCount INT = 0;
        DECLARE @CallRecordsInserted INT = 0;
        DECLARE @StagingUpdated INT = 0;
        DECLARE @SafaricomUpdated INT = 0;
        DECLARE @AirtelUpdated INT = 0;
        DECLARE @PSTNUpdated INT = 0;
        DECLARE @PrivateWireUpdated INT = 0;
        DECLARE @RemainingUnprocessed INT = 0;
        DECLARE @BatchStatus INT;
        DECLARE @BatchName NVARCHAR(200);
        DECLARE @ErrorMessage NVARCHAR(4000);
        DECLARE @ErrorSeverity INT;
        DECLARE @ErrorState INT;
        DECLARE @CurrentDateTime DATETIME = GETUTCDATE();

        BEGIN TRY
            BEGIN TRANSACTION;

            -- Validate batch exists and has correct status
            SELECT @BatchStatus = BatchStatus, @BatchName = BatchName FROM StagingBatches WHERE Id = @BatchId;
            IF @BatchName IS NULL BEGIN
                SELECT 0 AS Success, 0 AS RecordsPushed, 0 AS RemainingUnprocessed, 'Batch not found' AS Error;
                ROLLBACK TRANSACTION;
                RETURN;
            END

            -- BatchStatus: Verified=2, PartiallyVerified=3
            IF @BatchStatus NOT IN (2, 3) BEGIN
                SELECT 0 AS Success, 0 AS RecordsPushed, 0 AS RemainingUnprocessed, 'Batch must be Verified or PartiallyVerified to push to production' AS Error;
                ROLLBACK TRANSACTION;
                RETURN;
            END

            -- Count verified records
            SELECT @VerifiedCount = COUNT(*) FROM CallLogStagings WHERE BatchId = @BatchId AND VerificationStatus = 1;
            IF @VerifiedCount = 0 BEGIN
                SELECT 0 AS Success, 0 AS RecordsPushed, 0 AS RemainingUnprocessed, 'No verified records found in batch' AS Error;
                ROLLBACK TRANSACTION;
                RETURN;
            END

            -- Insert verified records into CallRecords (production)
            INSERT INTO CallRecords (ext_no, call_date, call_number, call_destination, call_endtime, call_duration, call_curr_code, call_cost, call_cost_usd, call_cost_kshs, call_type, call_dest_type, call_year, call_month, ext_resp_index, call_pay_index, UserPhoneId, assignment_status, call_ver_ind, call_ver_date, verification_type, verification_period, approval_period, revert_count, call_cert_ind, call_proc_ind, entry_date, SourceSystem, SourceBatchId, SourceStagingId)
            SELECT ExtensionNumber, CallDate, CallNumber, CallDestination, CallEndTime, CallDuration, CallCurrencyCode, CallCost, CallCostUSD, CallCostKSHS, CallType, CallDestinationType, CallYear, CallMonth, ResponsibleIndexNumber, PayingIndexNumber, UserPhoneId, 'None', 0, NULL, @VerificationType, @VerificationPeriod, @ApprovalPeriod, 0, 0, 0, @CurrentDateTime, SourceSystem, BatchId, Id FROM CallLogStagings WHERE BatchId = @BatchId AND VerificationStatus = 1;
            SET @CallRecordsInserted = @@ROWCOUNT;

            -- Update CallLogStagings records as Completed
            UPDATE CallLogStagings SET ProcessingStatus = 3, ProcessedDate = @CurrentDateTime WHERE BatchId = @BatchId AND VerificationStatus = 1;
            SET @StagingUpdated = @@ROWCOUNT;

            -- Update source tables (Safaricom, Airtel, PSTN, PrivateWire) - Mark records as Completed
            UPDATE s SET s.ProcessingStatus = 3, s.ProcessedDate = @CurrentDateTime FROM Safaricom s INNER JOIN CallLogStagings cls ON cls.SourceRecordId = CAST(s.Id AS NVARCHAR(50)) WHERE cls.BatchId = @BatchId AND cls.VerificationStatus = 1 AND cls.SourceSystem = 'Safaricom';
            SET @SafaricomUpdated = @@ROWCOUNT;

            UPDATE a SET a.ProcessingStatus = 3, a.ProcessedDate = @CurrentDateTime FROM Airtel a INNER JOIN CallLogStagings cls ON cls.SourceRecordId = CAST(a.Id AS NVARCHAR(50)) WHERE cls.BatchId = @BatchId AND cls.VerificationStatus = 1 AND cls.SourceSystem = 'Airtel';
            SET @AirtelUpdated = @@ROWCOUNT;

            UPDATE p SET p.ProcessingStatus = 3, p.ProcessedDate = @CurrentDateTime FROM PSTNs p INNER JOIN CallLogStagings cls ON cls.SourceRecordId = CAST(p.Id AS NVARCHAR(50)) WHERE cls.BatchId = @BatchId AND cls.VerificationStatus = 1 AND cls.SourceSystem = 'PSTN';
            SET @PSTNUpdated = @@ROWCOUNT;

            UPDATE pw SET pw.ProcessingStatus = 3, pw.ProcessedDate = @CurrentDateTime FROM PrivateWires pw INNER JOIN CallLogStagings cls ON cls.SourceRecordId = CAST(pw.Id AS NVARCHAR(50)) WHERE cls.BatchId = @BatchId AND cls.VerificationStatus = 1 AND cls.SourceSystem = 'PrivateWire';
            SET @PrivateWireUpdated = @@ROWCOUNT;

            -- Count remaining unverified/rejected records
            SELECT @RemainingUnprocessed = COUNT(*) FROM CallLogStagings WHERE BatchId = @BatchId AND VerificationStatus != 1 AND ProcessingStatus != 3;

            -- Update batch status
            IF @RemainingUnprocessed = 0 BEGIN
                UPDATE StagingBatches SET BatchStatus = 4, EndProcessingDate = @CurrentDateTime, PublishedBy = @PublishedBy WHERE Id = @BatchId;
            END
            ELSE BEGIN
                UPDATE StagingBatches SET BatchStatus = 3, PublishedBy = @PublishedBy WHERE Id = @BatchId;
            END

            -- Return success result with all columns
            SELECT 1 AS Success, @CallRecordsInserted AS RecordsPushed, @RemainingUnprocessed AS RemainingUnprocessed, @SafaricomUpdated AS SafaricomUpdated, @AirtelUpdated AS AirtelUpdated, @PSTNUpdated AS PSTNUpdated, @PrivateWireUpdated AS PrivateWireUpdated, @CurrentDateTime AS CompletedAt, NULL AS Error;

            COMMIT TRANSACTION;
        END TRY
        BEGIN CATCH
            IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
            SELECT @ErrorMessage = ERROR_MESSAGE(), @ErrorSeverity = ERROR_SEVERITY(), @ErrorState = ERROR_STATE();
            SELECT 0 AS Success, 0 AS RecordsPushed, 0 AS RemainingUnprocessed, 0 AS SafaricomUpdated, 0 AS AirtelUpdated, 0 AS PSTNUpdated, 0 AS PrivateWireUpdated, NULL AS CompletedAt, @ErrorMessage AS Error;
            RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
        END CATCH
    END
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251114083309_AddBatchStoredProcedures'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251114083309_AddBatchStoredProcedures', N'8.0.6');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251114095248_AddImportJobsTable'
)
BEGIN
    CREATE TABLE [ImportJobs] (
        [Id] uniqueidentifier NOT NULL,
        [FileName] nvarchar(500) NOT NULL,
        [FileSize] bigint NOT NULL,
        [CallLogType] nvarchar(50) NOT NULL,
        [BillingMonth] int NOT NULL,
        [BillingYear] int NOT NULL,
        [DateFormat] nvarchar(50) NULL,
        [Status] nvarchar(50) NOT NULL,
        [RecordsProcessed] int NULL,
        [RecordsSuccess] int NULL,
        [RecordsError] int NULL,
        [ErrorMessage] nvarchar(max) NULL,
        [CreatedBy] nvarchar(256) NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [StartedDate] datetime2 NULL,
        [CompletedDate] datetime2 NULL,
        [HangfireJobId] nvarchar(100) NULL,
        [DurationSeconds] int NULL,
        [ProgressPercentage] int NULL,
        [Metadata] nvarchar(max) NULL,
        CONSTRAINT [PK_ImportJobs] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251114095248_AddImportJobsTable'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251114095248_AddImportJobsTable', N'8.0.6');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251118151108_AddHangfireJobIdToStagingBatch'
)
BEGIN
    ALTER TABLE [StagingBatches] ADD [HangfireJobId] nvarchar(100) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251118151108_AddHangfireJobIdToStagingBatch'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251118151108_AddHangfireJobIdToStagingBatch', N'8.0.6');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251118190451_AddCallLogStagingIndexes'
)
BEGIN
    CREATE INDEX [IX_CallLogStagings_BatchId_VerificationStatus] ON [CallLogStagings] ([BatchId], [VerificationStatus]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251118190451_AddCallLogStagingIndexes'
)
BEGIN
    CREATE INDEX [IX_CallLogStagings_BatchId_HasAnomalies] ON [CallLogStagings] ([BatchId], [HasAnomalies]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251118190451_AddCallLogStagingIndexes'
)
BEGIN
    CREATE INDEX [IX_CallLogStagings_VerificationDate] ON [CallLogStagings] ([VerificationDate]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251118190451_AddCallLogStagingIndexes'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251118190451_AddCallLogStagingIndexes', N'8.0.6');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251118200259_AddConsolidationProgressTracking'
)
BEGIN
    ALTER TABLE [StagingBatches] ADD [CurrentOperation] nvarchar(100) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251118200259_AddConsolidationProgressTracking'
)
BEGIN
    ALTER TABLE [StagingBatches] ADD [ProcessingProgress] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251118200259_AddConsolidationProgressTracking'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251118200259_AddConsolidationProgressTracking', N'8.0.6');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251119101704_FixDeleteBatchStoredProcedureTableNames'
)
BEGIN

                    IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'sp_DeleteBatch')
                        DROP PROCEDURE sp_DeleteBatch;
                
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251119101704_FixDeleteBatchStoredProcedureTableNames'
)
BEGIN

                    CREATE PROCEDURE sp_DeleteBatch
                        @BatchId UNIQUEIDENTIFIER,
                        @DeletedBy NVARCHAR(256),
                        @Result NVARCHAR(MAX) OUTPUT
                    AS
                    BEGIN
                        SET NOCOUNT ON;
                        SET XACT_ABORT ON;

                        DECLARE @BatchName NVARCHAR(200);
                        DECLARE @BatchStatus INT;
                        DECLARE @TotalRecords INT;
                        DECLARE @StagingRecordsDeleted INT = 0;
                        DECLARE @SafaricomRecordsReset INT = 0;
                        DECLARE @AirtelRecordsReset INT = 0;
                        DECLARE @PSTNRecordsReset INT = 0;
                        DECLARE @PrivateWireRecordsReset INT = 0;
                        DECLARE @ErrorMessage NVARCHAR(4000);
                        DECLARE @ErrorSeverity INT;
                        DECLARE @ErrorState INT;

                        BEGIN TRY
                            BEGIN TRANSACTION;

                            -- STEP 1: Validate batch exists
                            SELECT
                                @BatchName = BatchName,
                                @BatchStatus = BatchStatus,
                                @TotalRecords = TotalRecords
                            FROM StagingBatches
                            WHERE Id = @BatchId;

                            IF @BatchName IS NULL
                            BEGIN
                                SET @Result = JSON_QUERY('{"success": false, "error": "Batch not found"}');
                                ROLLBACK TRANSACTION;
                                RETURN;
                            END

                            -- STEP 2: Check if batch can be deleted
                            IF @BatchStatus = 4
                            BEGIN
                                SET @Result = JSON_QUERY('{"success": false, "error": "Cannot delete batch - already published to production"}');
                                ROLLBACK TRANSACTION;
                                RETURN;
                            END

                            IF EXISTS (SELECT 1 FROM CallLogStagings WHERE BatchId = @BatchId AND ProcessingStatus = 3)
                            BEGIN
                                SET @Result = JSON_QUERY('{"success": false, "error": "Cannot delete batch - has records in production"}');
                                ROLLBACK TRANSACTION;
                                RETURN;
                            END

                            -- STEP 3: Delete all staging records for this batch
                            DELETE FROM CallLogStagings
                            WHERE BatchId = @BatchId;

                            SET @StagingRecordsDeleted = @@ROWCOUNT;

                            -- STEP 4: Reset source records (set StagingBatchId = NULL)
                            -- Reset Safaricom records (singular table name)
                            UPDATE Safaricom
                            SET StagingBatchId = NULL,
                                ProcessingStatus = 0,
                                ProcessedDate = NULL
                            WHERE StagingBatchId = @BatchId;

                            SET @SafaricomRecordsReset = @@ROWCOUNT;

                            -- Reset Airtel records (singular table name)
                            UPDATE Airtel
                            SET StagingBatchId = NULL,
                                ProcessingStatus = 0,
                                ProcessedDate = NULL
                            WHERE StagingBatchId = @BatchId;

                            SET @AirtelRecordsReset = @@ROWCOUNT;

                            -- Reset PSTN records (plural table name)
                            UPDATE PSTNs
                            SET StagingBatchId = NULL,
                                ProcessingStatus = 0,
                                ProcessedDate = NULL
                            WHERE StagingBatchId = @BatchId;

                            SET @PSTNRecordsReset = @@ROWCOUNT;

                            -- Reset PrivateWire records (plural table name)
                            UPDATE PrivateWires
                            SET StagingBatchId = NULL,
                                ProcessingStatus = 0,
                                ProcessedDate = NULL
                            WHERE StagingBatchId = @BatchId;

                            SET @PrivateWireRecordsReset = @@ROWCOUNT;

                            -- STEP 5: Create audit log entry
                            INSERT INTO AuditLogs (
                                EntityType,
                                EntityId,
                                Action,
                                Description,
                                OldValues,
                                PerformedBy,
                                PerformedDate,
                                Module,
                                IsSuccess,
                                AdditionalData
                            )
                            SELECT
                                'StagingBatch',
                                CAST(@BatchId AS NVARCHAR(50)),
                                'Deleted',
                                'Deleted batch ''' + @BatchName + ''' with ' + CAST(@StagingRecordsDeleted AS NVARCHAR(20)) + ' staging records',
                                (SELECT
                                    BatchName,
                                    BatchStatus,
                                    TotalRecords,
                                    VerifiedRecords,
                                    RejectedRecords,
                                    RecordsWithAnomalies,
                                    CreatedDate,
                                    CreatedBy
                                 FROM StagingBatches
                                 WHERE Id = @BatchId
                                 FOR JSON PATH, WITHOUT_ARRAY_WRAPPER),
                                @DeletedBy,
                                GETUTCDATE(),
                                'CallLogStaging',
                                1,
                                (SELECT
                                    @StagingRecordsDeleted AS RecordsDeleted,
                                    @SafaricomRecordsReset AS SafaricomRecordsReset,
                                    @AirtelRecordsReset AS AirtelRecordsReset,
                                    @PSTNRecordsReset AS PSTNRecordsReset,
                                    @PrivateWireRecordsReset AS PrivateWireRecordsReset
                                 FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);

                            -- STEP 6: Delete the batch itself
                            DELETE FROM StagingBatches
                            WHERE Id = @BatchId;

                            -- STEP 7: Prepare success result
                            SET @Result = (
                                SELECT
                                    1 AS success,
                                    @BatchName AS batchName,
                                    @StagingRecordsDeleted AS stagingRecordsDeleted,
                                    @SafaricomRecordsReset AS safaricomRecordsReset,
                                    @AirtelRecordsReset AS airtelRecordsReset,
                                    @PSTNRecordsReset AS pstnRecordsReset,
                                    @PrivateWireRecordsReset AS privateWireRecordsReset,
                                    GETUTCDATE() AS deletedAt
                                FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
                            );

                            COMMIT TRANSACTION;

                            -- Return result set for EF Core
                            SELECT
                                1 AS Success,
                                @BatchName AS BatchName,
                                @StagingRecordsDeleted AS StagingRecordsDeleted,
                                @SafaricomRecordsReset AS SafaricomRecordsReset,
                                @AirtelRecordsReset AS AirtelRecordsReset,
                                @PSTNRecordsReset AS PSTNRecordsReset,
                                @PrivateWireRecordsReset AS PrivateWireRecordsReset,
                                GETUTCDATE() AS DeletedAt;

                        END TRY
                        BEGIN CATCH
                            IF @@TRANCOUNT > 0
                                ROLLBACK TRANSACTION;

                            SELECT @ErrorMessage = ERROR_MESSAGE(),
                                   @ErrorSeverity = ERROR_SEVERITY(),
                                   @ErrorState = ERROR_STATE();

                            SET @Result = (
                                SELECT
                                    0 AS success,
                                    @ErrorMessage AS error
                                FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
                            );

                            -- Return error result set
                            SELECT
                                0 AS Success,
                                @ErrorMessage AS Error;

                            RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
                        END CATCH
                    END
                
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251119101704_FixDeleteBatchStoredProcedureTableNames'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251119101704_FixDeleteBatchStoredProcedureTableNames', N'8.0.6');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251119102253_FixDeleteBatchResultColumns'
)
BEGIN

                    IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'sp_DeleteBatch')
                        DROP PROCEDURE sp_DeleteBatch;
                
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251119102253_FixDeleteBatchResultColumns'
)
BEGIN

                    CREATE PROCEDURE sp_DeleteBatch
                        @BatchId UNIQUEIDENTIFIER,
                        @DeletedBy NVARCHAR(256),
                        @Result NVARCHAR(MAX) OUTPUT
                    AS
                    BEGIN
                        SET NOCOUNT ON;
                        SET XACT_ABORT ON;

                        DECLARE @BatchName NVARCHAR(200);
                        DECLARE @BatchStatus INT;
                        DECLARE @TotalRecords INT;
                        DECLARE @StagingRecordsDeleted INT = 0;
                        DECLARE @SafaricomRecordsReset INT = 0;
                        DECLARE @AirtelRecordsReset INT = 0;
                        DECLARE @PSTNRecordsReset INT = 0;
                        DECLARE @PrivateWireRecordsReset INT = 0;
                        DECLARE @ErrorMessage NVARCHAR(4000);
                        DECLARE @ErrorSeverity INT;
                        DECLARE @ErrorState INT;

                        BEGIN TRY
                            BEGIN TRANSACTION;

                            -- STEP 1: Validate batch exists
                            SELECT
                                @BatchName = BatchName,
                                @BatchStatus = BatchStatus,
                                @TotalRecords = TotalRecords
                            FROM StagingBatches
                            WHERE Id = @BatchId;

                            IF @BatchName IS NULL
                            BEGIN
                                SET @Result = JSON_QUERY('{"success": false, "error": "Batch not found"}');
                                ROLLBACK TRANSACTION;
                                -- Return with all columns
                                SELECT
                                    0 AS Success,
                                    NULL AS BatchName,
                                    0 AS StagingRecordsDeleted,
                                    0 AS SafaricomRecordsReset,
                                    0 AS AirtelRecordsReset,
                                    0 AS PSTNRecordsReset,
                                    0 AS PrivateWireRecordsReset,
                                    NULL AS DeletedAt,
                                    'Batch not found' AS Error;
                                RETURN;
                            END

                            -- STEP 2: Check if batch can be deleted
                            IF @BatchStatus = 4
                            BEGIN
                                SET @Result = JSON_QUERY('{"success": false, "error": "Cannot delete batch - already published to production"}');
                                ROLLBACK TRANSACTION;
                                SELECT
                                    0 AS Success,
                                    @BatchName AS BatchName,
                                    0 AS StagingRecordsDeleted,
                                    0 AS SafaricomRecordsReset,
                                    0 AS AirtelRecordsReset,
                                    0 AS PSTNRecordsReset,
                                    0 AS PrivateWireRecordsReset,
                                    NULL AS DeletedAt,
                                    'Cannot delete batch - already published to production' AS Error;
                                RETURN;
                            END

                            IF EXISTS (SELECT 1 FROM CallLogStagings WHERE BatchId = @BatchId AND ProcessingStatus = 3)
                            BEGIN
                                SET @Result = JSON_QUERY('{"success": false, "error": "Cannot delete batch - has records in production"}');
                                ROLLBACK TRANSACTION;
                                SELECT
                                    0 AS Success,
                                    @BatchName AS BatchName,
                                    0 AS StagingRecordsDeleted,
                                    0 AS SafaricomRecordsReset,
                                    0 AS AirtelRecordsReset,
                                    0 AS PSTNRecordsReset,
                                    0 AS PrivateWireRecordsReset,
                                    NULL AS DeletedAt,
                                    'Cannot delete batch - has records in production' AS Error;
                                RETURN;
                            END

                            -- STEP 3: Delete all staging records for this batch
                            DELETE FROM CallLogStagings
                            WHERE BatchId = @BatchId;

                            SET @StagingRecordsDeleted = @@ROWCOUNT;

                            -- STEP 4: Reset source records (set StagingBatchId = NULL)
                            -- Reset Safaricom records (singular table name)
                            UPDATE Safaricom
                            SET StagingBatchId = NULL,
                                ProcessingStatus = 0,
                                ProcessedDate = NULL
                            WHERE StagingBatchId = @BatchId;

                            SET @SafaricomRecordsReset = @@ROWCOUNT;

                            -- Reset Airtel records (singular table name)
                            UPDATE Airtel
                            SET StagingBatchId = NULL,
                                ProcessingStatus = 0,
                                ProcessedDate = NULL
                            WHERE StagingBatchId = @BatchId;

                            SET @AirtelRecordsReset = @@ROWCOUNT;

                            -- Reset PSTN records (plural table name)
                            UPDATE PSTNs
                            SET StagingBatchId = NULL,
                                ProcessingStatus = 0,
                                ProcessedDate = NULL
                            WHERE StagingBatchId = @BatchId;

                            SET @PSTNRecordsReset = @@ROWCOUNT;

                            -- Reset PrivateWire records (plural table name)
                            UPDATE PrivateWires
                            SET StagingBatchId = NULL,
                                ProcessingStatus = 0,
                                ProcessedDate = NULL
                            WHERE StagingBatchId = @BatchId;

                            SET @PrivateWireRecordsReset = @@ROWCOUNT;

                            -- STEP 5: Create audit log entry
                            INSERT INTO AuditLogs (
                                EntityType,
                                EntityId,
                                Action,
                                Description,
                                OldValues,
                                PerformedBy,
                                PerformedDate,
                                Module,
                                IsSuccess,
                                AdditionalData
                            )
                            SELECT
                                'StagingBatch',
                                CAST(@BatchId AS NVARCHAR(50)),
                                'Deleted',
                                'Deleted batch ''' + @BatchName + ''' with ' + CAST(@StagingRecordsDeleted AS NVARCHAR(20)) + ' staging records',
                                (SELECT
                                    BatchName,
                                    BatchStatus,
                                    TotalRecords,
                                    VerifiedRecords,
                                    RejectedRecords,
                                    RecordsWithAnomalies,
                                    CreatedDate,
                                    CreatedBy
                                 FROM StagingBatches
                                 WHERE Id = @BatchId
                                 FOR JSON PATH, WITHOUT_ARRAY_WRAPPER),
                                @DeletedBy,
                                GETUTCDATE(),
                                'CallLogStaging',
                                1,
                                (SELECT
                                    @StagingRecordsDeleted AS RecordsDeleted,
                                    @SafaricomRecordsReset AS SafaricomRecordsReset,
                                    @AirtelRecordsReset AS AirtelRecordsReset,
                                    @PSTNRecordsReset AS PSTNRecordsReset,
                                    @PrivateWireRecordsReset AS PrivateWireRecordsReset
                                 FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);

                            -- STEP 6: Delete the batch itself
                            DELETE FROM StagingBatches
                            WHERE Id = @BatchId;

                            -- STEP 7: Prepare success result
                            SET @Result = (
                                SELECT
                                    1 AS success,
                                    @BatchName AS batchName,
                                    @StagingRecordsDeleted AS stagingRecordsDeleted,
                                    @SafaricomRecordsReset AS safaricomRecordsReset,
                                    @AirtelRecordsReset AS airtelRecordsReset,
                                    @PSTNRecordsReset AS pstnRecordsReset,
                                    @PrivateWireRecordsReset AS privateWireRecordsReset,
                                    GETUTCDATE() AS deletedAt
                                FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
                            );

                            COMMIT TRANSACTION;

                            -- Return result set for EF Core with all columns
                            SELECT
                                1 AS Success,
                                @BatchName AS BatchName,
                                @StagingRecordsDeleted AS StagingRecordsDeleted,
                                @SafaricomRecordsReset AS SafaricomRecordsReset,
                                @AirtelRecordsReset AS AirtelRecordsReset,
                                @PSTNRecordsReset AS PSTNRecordsReset,
                                @PrivateWireRecordsReset AS PrivateWireRecordsReset,
                                GETUTCDATE() AS DeletedAt,
                                CAST(NULL AS NVARCHAR(4000)) AS Error;

                        END TRY
                        BEGIN CATCH
                            IF @@TRANCOUNT > 0
                                ROLLBACK TRANSACTION;

                            SELECT @ErrorMessage = ERROR_MESSAGE(),
                                   @ErrorSeverity = ERROR_SEVERITY(),
                                   @ErrorState = ERROR_STATE();

                            SET @Result = (
                                SELECT
                                    0 AS success,
                                    @ErrorMessage AS error
                                FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
                            );

                            -- Return error result set with all columns
                            SELECT
                                0 AS Success,
                                @BatchName AS BatchName,
                                0 AS StagingRecordsDeleted,
                                0 AS SafaricomRecordsReset,
                                0 AS AirtelRecordsReset,
                                0 AS PSTNRecordsReset,
                                0 AS PrivateWireRecordsReset,
                                NULL AS DeletedAt,
                                @ErrorMessage AS Error;

                            RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
                        END CATCH
                    END
                
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251119102253_FixDeleteBatchResultColumns'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251119102253_FixDeleteBatchResultColumns', N'8.0.6');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251119171057_MakeServiceProviderIdNullable'
)
BEGIN
    DECLARE @var4 sysname;
    SELECT @var4 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[SimRequests]') AND [c].[name] = N'ServiceProviderId');
    IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [SimRequests] DROP CONSTRAINT [' + @var4 + '];');
    ALTER TABLE [SimRequests] ALTER COLUMN [ServiceProviderId] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251119171057_MakeServiceProviderIdNullable'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251119171057_MakeServiceProviderIdNullable', N'8.0.6');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251119203144_AddVerifyBatchStoredProcedure'
)
BEGIN

    CREATE PROCEDURE sp_VerifyBatch
        @BatchId UNIQUEIDENTIFIER,
        @VerifiedBy NVARCHAR(100)
    AS
    BEGIN
        SET NOCOUNT ON;

        DECLARE @RecordsVerified INT = 0;
        DECLARE @ErrorMessage NVARCHAR(4000) = NULL;

        BEGIN TRY
            BEGIN TRANSACTION;

            -- Update all pending and requires review records in one statement
            UPDATE CallLogStagings
            SET
                VerificationStatus = 2, -- Verified
                VerificationDate = GETUTCDATE(),
                VerifiedBy = @VerifiedBy,
                ModifiedDate = GETUTCDATE(),
                ModifiedBy = @VerifiedBy
            WHERE
                BatchId = @BatchId
                AND (VerificationStatus = 0 OR VerificationStatus = 3); -- Pending or RequiresReview

            SET @RecordsVerified = @@ROWCOUNT;

            -- Update batch statistics
            DECLARE @TotalRecords INT, @VerifiedRecords INT, @RejectedRecords INT, @PendingRecords INT, @RecordsWithAnomalies INT;

            SELECT
                @TotalRecords = COUNT(*),
                @VerifiedRecords = SUM(CASE WHEN VerificationStatus = 2 THEN 1 ELSE 0 END),
                @RejectedRecords = SUM(CASE WHEN VerificationStatus = 1 THEN 1 ELSE 0 END),
                @PendingRecords = SUM(CASE WHEN VerificationStatus = 0 OR VerificationStatus = 3 THEN 1 ELSE 0 END),
                @RecordsWithAnomalies = SUM(CASE WHEN HasAnomalies = 1 THEN 1 ELSE 0 END)
            FROM CallLogStagings
            WHERE BatchId = @BatchId;

            UPDATE StagingBatches
            SET
                TotalRecords = @TotalRecords,
                VerifiedRecords = @VerifiedRecords,
                RejectedRecords = @RejectedRecords,
                PendingRecords = @PendingRecords,
                RecordsWithAnomalies = @RecordsWithAnomalies,
                VerifiedBy = @VerifiedBy,
                -- Update status based on verification state
                BatchStatus = CASE
                    WHEN @PendingRecords = 0 AND @VerifiedRecords > 0 THEN 3 -- Verified
                    WHEN @VerifiedRecords > 0 THEN 2 -- PartiallyVerified
                    ELSE BatchStatus
                END
            WHERE Id = @BatchId;

            COMMIT TRANSACTION;

            -- Return result
            SELECT
                1 AS Success,
                @RecordsVerified AS RecordsVerified,
                @TotalRecords AS TotalRecords,
                @VerifiedRecords AS TotalVerified,
                @PendingRecords AS TotalPending,
                NULL AS Error;

        END TRY
        BEGIN CATCH
            IF @@TRANCOUNT > 0
                ROLLBACK TRANSACTION;

            SET @ErrorMessage = ERROR_MESSAGE();

            SELECT
                0 AS Success,
                0 AS RecordsVerified,
                0 AS TotalRecords,
                0 AS TotalVerified,
                0 AS TotalPending,
                @ErrorMessage AS Error;
        END CATCH
    END
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251119203144_AddVerifyBatchStoredProcedure'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251119203144_AddVerifyBatchStoredProcedure', N'8.0.6');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251119210633_FixVerifyBatchStoredProcedureEnumValues'
)
BEGIN
    DROP PROCEDURE IF EXISTS sp_VerifyBatch
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251119210633_FixVerifyBatchStoredProcedureEnumValues'
)
BEGIN

    CREATE PROCEDURE sp_VerifyBatch
        @BatchId UNIQUEIDENTIFIER,
        @VerifiedBy NVARCHAR(100)
    AS
    BEGIN
        SET NOCOUNT ON;

        DECLARE @RecordsVerified INT = 0;
        DECLARE @ErrorMessage NVARCHAR(4000) = NULL;

        BEGIN TRY
            BEGIN TRANSACTION;

            -- Update all pending and requires review records in one statement
            UPDATE CallLogStagings
            SET
                VerificationStatus = 1, -- Verified (0=Pending, 1=Verified, 2=Rejected, 3=RequiresReview)
                VerificationDate = GETUTCDATE(),
                VerifiedBy = @VerifiedBy,
                ModifiedDate = GETUTCDATE(),
                ModifiedBy = @VerifiedBy
            WHERE
                BatchId = @BatchId
                AND (VerificationStatus = 0 OR VerificationStatus = 3); -- Pending or RequiresReview

            SET @RecordsVerified = @@ROWCOUNT;

            -- Update batch statistics
            DECLARE @TotalRecords INT, @VerifiedRecords INT, @RejectedRecords INT, @PendingRecords INT, @RecordsWithAnomalies INT;

            SELECT
                @TotalRecords = COUNT(*),
                @VerifiedRecords = SUM(CASE WHEN VerificationStatus = 1 THEN 1 ELSE 0 END), -- 1=Verified
                @RejectedRecords = SUM(CASE WHEN VerificationStatus = 2 THEN 1 ELSE 0 END), -- 2=Rejected
                @PendingRecords = SUM(CASE WHEN VerificationStatus = 0 OR VerificationStatus = 3 THEN 1 ELSE 0 END),
                @RecordsWithAnomalies = SUM(CASE WHEN HasAnomalies = 1 THEN 1 ELSE 0 END)
            FROM CallLogStagings
            WHERE BatchId = @BatchId;

            UPDATE StagingBatches
            SET
                TotalRecords = @TotalRecords,
                VerifiedRecords = @VerifiedRecords,
                RejectedRecords = @RejectedRecords,
                PendingRecords = @PendingRecords,
                RecordsWithAnomalies = @RecordsWithAnomalies,
                VerifiedBy = @VerifiedBy,
                -- Update status based on verification state
                -- BatchStatus: 0=Created, 1=Processing, 2=PartiallyVerified, 3=Verified, 4=Published, 5=Failed
                BatchStatus = CASE
                    WHEN @PendingRecords = 0 AND @VerifiedRecords > 0 THEN 3 -- Verified
                    WHEN @VerifiedRecords > 0 THEN 2 -- PartiallyVerified
                    ELSE BatchStatus
                END
            WHERE Id = @BatchId;

            COMMIT TRANSACTION;

            -- Return result
            SELECT
                1 AS Success,
                @RecordsVerified AS RecordsVerified,
                @TotalRecords AS TotalRecords,
                @VerifiedRecords AS TotalVerified,
                @PendingRecords AS TotalPending,
                NULL AS Error;

        END TRY
        BEGIN CATCH
            IF @@TRANCOUNT > 0
                ROLLBACK TRANSACTION;

            SET @ErrorMessage = ERROR_MESSAGE();

            SELECT
                0 AS Success,
                0 AS RecordsVerified,
                0 AS TotalRecords,
                0 AS TotalVerified,
                0 AS TotalPending,
                @ErrorMessage AS Error;
        END CATCH
    END
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251119210633_FixVerifyBatchStoredProcedureEnumValues'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251119210633_FixVerifyBatchStoredProcedureEnumValues', N'8.0.6');
END;
GO

COMMIT;
GO

