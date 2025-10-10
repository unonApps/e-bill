-- =====================================================================================
-- ADD MISSING CALL LOG VERIFICATION TABLES TO AZURE DATABASE
-- These tables should have been created by migration 20251002163017
-- Run this in Azure Portal Query Editor
-- =====================================================================================

PRINT '========================================';
PRINT 'Adding Missing Call Log Tables';
PRINT '========================================';
PRINT '';

-- =====================================================================================
-- Table: CallLogPaymentAssignments
-- =====================================================================================
PRINT 'Creating CallLogPaymentAssignments table...';

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'CallLogPaymentAssignments')
BEGIN
    CREATE TABLE [dbo].[CallLogPaymentAssignments] (
        [Id] int IDENTITY(1,1) NOT NULL,
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
        CONSTRAINT [FK_CallLogPaymentAssignments_CallRecords_CallRecordId]
            FOREIGN KEY ([CallRecordId]) REFERENCES [CallRecords] ([Id])
    );

    CREATE NONCLUSTERED INDEX [IX_CallLogPaymentAssignments_CallRecordId]
        ON [dbo].[CallLogPaymentAssignments] ([CallRecordId]);

    CREATE NONCLUSTERED INDEX [IX_CallLogPaymentAssignments_AssignedFrom_AssignedTo]
        ON [dbo].[CallLogPaymentAssignments] ([AssignedFrom], [AssignedTo]);

    CREATE NONCLUSTERED INDEX [IX_CallLogPaymentAssignments_AssignedTo]
        ON [dbo].[CallLogPaymentAssignments] ([AssignedTo]);

    CREATE NONCLUSTERED INDEX [IX_CallLogPaymentAssignments_AssignmentStatus]
        ON [dbo].[CallLogPaymentAssignments] ([AssignmentStatus]);

    PRINT '✓ Created CallLogPaymentAssignments table';
END
ELSE
    PRINT '- CallLogPaymentAssignments table already exists';
PRINT '';

-- =====================================================================================
-- Table: CallLogVerifications
-- =====================================================================================
PRINT 'Creating CallLogVerifications table...';

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'CallLogVerifications')
BEGIN
    CREATE TABLE [dbo].[CallLogVerifications] (
        [Id] int IDENTITY(1,1) NOT NULL,
        [CallRecordId] int NOT NULL,
        [VerifiedBy] nvarchar(50) NOT NULL,
        [VerifiedDate] datetime2 NOT NULL,
        [VerificationType] int NOT NULL,
        [ClassOfServiceId] int NULL,
        [AllowanceAmount] decimal(18,4) NULL,
        [ActualAmount] decimal(18,4) NOT NULL,
        [IsOverage] bit NOT NULL,
        [JustificationText] nvarchar(max) NULL,
        [SupportingDocuments] nvarchar(max) NULL,
        [ApprovalStatus] nvarchar(20) NOT NULL,
        [SubmittedToSupervisor] bit NOT NULL,
        [SubmittedDate] datetime2 NULL,
        [SupervisorIndexNumber] nvarchar(50) NULL,
        [SupervisorAction] nvarchar(20) NULL,
        [SupervisorActionDate] datetime2 NULL,
        [SupervisorComments] nvarchar(500) NULL,
        [ApprovedAmount] decimal(18,4) NULL,
        [RejectionReason] nvarchar(500) NULL,
        [CreatedDate] datetime2 NOT NULL,
        [ModifiedDate] datetime2 NULL,
        CONSTRAINT [PK_CallLogVerifications] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CallLogVerifications_CallRecords_CallRecordId]
            FOREIGN KEY ([CallRecordId]) REFERENCES [CallRecords] ([Id]),
        CONSTRAINT [FK_CallLogVerifications_ClassOfServices_ClassOfServiceId]
            FOREIGN KEY ([ClassOfServiceId]) REFERENCES [ClassOfServices] ([Id])
    );

    CREATE NONCLUSTERED INDEX [IX_CallLogVerifications_ApprovalStatus]
        ON [dbo].[CallLogVerifications] ([ApprovalStatus]);

    CREATE NONCLUSTERED INDEX [IX_CallLogVerifications_CallRecordId_VerifiedBy]
        ON [dbo].[CallLogVerifications] ([CallRecordId], [VerifiedBy]);

    CREATE NONCLUSTERED INDEX [IX_CallLogVerifications_ClassOfServiceId]
        ON [dbo].[CallLogVerifications] ([ClassOfServiceId]);

    CREATE NONCLUSTERED INDEX [IX_CallLogVerifications_SupervisorIndexNumber]
        ON [dbo].[CallLogVerifications] ([SupervisorIndexNumber]);

    CREATE NONCLUSTERED INDEX [IX_CallLogVerifications_VerifiedBy]
        ON [dbo].[CallLogVerifications] ([VerifiedBy]);

    PRINT '✓ Created CallLogVerifications table';
END
ELSE
    PRINT '- CallLogVerifications table already exists';
PRINT '';

-- =====================================================================================
-- Table: CallLogDocuments
-- =====================================================================================
PRINT 'Creating CallLogDocuments table...';

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'CallLogDocuments')
BEGIN
    CREATE TABLE [dbo].[CallLogDocuments] (
        [Id] int IDENTITY(1,1) NOT NULL,
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
        CONSTRAINT [FK_CallLogDocuments_CallLogVerifications_CallLogVerificationId]
            FOREIGN KEY ([CallLogVerificationId]) REFERENCES [CallLogVerifications] ([Id])
            ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX [IX_CallLogDocuments_CallLogVerificationId]
        ON [dbo].[CallLogDocuments] ([CallLogVerificationId]);

    PRINT '✓ Created CallLogDocuments table';
END
ELSE
    PRINT '- CallLogDocuments table already exists';
PRINT '';

PRINT '========================================';
PRINT 'Missing Tables Created Successfully!';
PRINT '========================================';
PRINT '';
PRINT 'Next step: Run fix-azure-missing-columns.sql to add missing columns to existing tables';
