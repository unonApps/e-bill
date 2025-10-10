-- ======================================================================
-- Call Log Verification System - Database Schema Update
-- ======================================================================
-- This script adds the database schema for the Call Log Verification System
-- Run this on your database before running EF migrations
-- ======================================================================

USE TABDB;
GO

PRINT 'Starting Call Log Verification System schema update...';
GO

-- ======================================================================
-- 1. UPDATE CallRecords Table - Add Verification Fields
-- ======================================================================
PRINT '1. Adding verification fields to CallRecords table...';
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('CallRecords') AND name = 'verification_type')
BEGIN
    ALTER TABLE CallRecords ADD verification_type NVARCHAR(20) NULL;
    PRINT '  - Added verification_type column';
END
ELSE
    PRINT '  - verification_type column already exists';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('CallRecords') AND name = 'payment_assignment_id')
BEGIN
    ALTER TABLE CallRecords ADD payment_assignment_id INT NULL;
    PRINT '  - Added payment_assignment_id column';
END
ELSE
    PRINT '  - payment_assignment_id column already exists';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('CallRecords') AND name = 'overage_justified')
BEGIN
    ALTER TABLE CallRecords ADD overage_justified BIT NOT NULL DEFAULT 0;
    PRINT '  - Added overage_justified column';
END
ELSE
    PRINT '  - overage_justified column already exists';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('CallRecords') AND name = 'supervisor_approval_status')
BEGIN
    ALTER TABLE CallRecords ADD supervisor_approval_status NVARCHAR(20) NULL;
    PRINT '  - Added supervisor_approval_status column';
END
ELSE
    PRINT '  - supervisor_approval_status column already exists';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('CallRecords') AND name = 'supervisor_approved_by')
BEGIN
    ALTER TABLE CallRecords ADD supervisor_approved_by NVARCHAR(50) NULL;
    PRINT '  - Added supervisor_approved_by column';
END
ELSE
    PRINT '  - supervisor_approved_by column already exists';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('CallRecords') AND name = 'supervisor_approved_date')
BEGIN
    ALTER TABLE CallRecords ADD supervisor_approved_date DATETIME2 NULL;
    PRINT '  - Added supervisor_approved_date column';
END
ELSE
    PRINT '  - supervisor_approved_date column already exists';
GO

-- ======================================================================
-- 2. UPDATE ClassOfServices Table - Add Numeric Allowance Fields
-- ======================================================================
PRINT '2. Adding numeric allowance fields to ClassOfServices table...';
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('ClassOfServices') AND name = 'AirtimeAllowanceAmount')
BEGIN
    ALTER TABLE ClassOfServices ADD AirtimeAllowanceAmount DECIMAL(18,4) NULL;
    PRINT '  - Added AirtimeAllowanceAmount column';
END
ELSE
    PRINT '  - AirtimeAllowanceAmount column already exists';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('ClassOfServices') AND name = 'DataAllowanceAmount')
BEGIN
    ALTER TABLE ClassOfServices ADD DataAllowanceAmount DECIMAL(18,4) NULL;
    PRINT '  - Added DataAllowanceAmount column';
END
ELSE
    PRINT '  - DataAllowanceAmount column already exists';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('ClassOfServices') AND name = 'MonthlyCallCostLimit')
BEGIN
    ALTER TABLE ClassOfServices ADD MonthlyCallCostLimit DECIMAL(18,4) NULL;
    PRINT '  - Added MonthlyCallCostLimit column';
END
ELSE
    PRINT '  - MonthlyCallCostLimit column already exists';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('ClassOfServices') AND name = 'BillingPeriod')
BEGIN
    ALTER TABLE ClassOfServices ADD BillingPeriod NVARCHAR(20) NOT NULL DEFAULT 'Monthly';
    PRINT '  - Added BillingPeriod column';
END
ELSE
    PRINT '  - BillingPeriod column already exists';
GO

-- ======================================================================
-- 3. CREATE CallLogVerifications Table
-- ======================================================================
PRINT '3. Creating CallLogVerifications table...';
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'CallLogVerifications')
BEGIN
    CREATE TABLE CallLogVerifications (
        Id INT PRIMARY KEY IDENTITY(1,1),
        CallRecordId INT NOT NULL,
        VerifiedBy NVARCHAR(50) NOT NULL,
        VerifiedDate DATETIME2 NOT NULL,
        VerificationType INT NOT NULL, -- Enum: 0=Personal, 1=Official

        -- Class of Service Tracking
        ClassOfServiceId INT NULL,
        AllowanceAmount DECIMAL(18,4) NULL,
        ActualAmount DECIMAL(18,4) NOT NULL,
        IsOverage BIT NOT NULL DEFAULT 0,

        -- Overage Justification
        JustificationText NVARCHAR(MAX) NULL,
        SupportingDocuments NVARCHAR(MAX) NULL, -- JSON array

        -- Approval Workflow
        ApprovalStatus NVARCHAR(20) NOT NULL DEFAULT 'Pending',
        SubmittedToSupervisor BIT NOT NULL DEFAULT 0,
        SubmittedDate DATETIME2 NULL,

        SupervisorIndexNumber NVARCHAR(50) NULL,
        SupervisorAction NVARCHAR(20) NULL,
        SupervisorActionDate DATETIME2 NULL,
        SupervisorComments NVARCHAR(500) NULL,

        ApprovedAmount DECIMAL(18,4) NULL,
        RejectionReason NVARCHAR(500) NULL,

        -- Audit
        CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        ModifiedDate DATETIME2 NULL,

        -- Foreign Keys
        CONSTRAINT FK_CallLogVerifications_CallRecords FOREIGN KEY (CallRecordId)
            REFERENCES CallRecords(Id) ON DELETE NO ACTION,
        CONSTRAINT FK_CallLogVerifications_ClassOfServices FOREIGN KEY (ClassOfServiceId)
            REFERENCES ClassOfServices(Id) ON DELETE NO ACTION
    );

    -- Indexes for performance
    CREATE INDEX IX_CallLogVerifications_VerifiedBy ON CallLogVerifications(VerifiedBy);
    CREATE INDEX IX_CallLogVerifications_ApprovalStatus ON CallLogVerifications(ApprovalStatus);
    CREATE INDEX IX_CallLogVerifications_SupervisorIndexNumber ON CallLogVerifications(SupervisorIndexNumber);
    CREATE INDEX IX_CallLogVerifications_CallRecordId_VerifiedBy ON CallLogVerifications(CallRecordId, VerifiedBy);

    PRINT '  - CallLogVerifications table created successfully';
END
ELSE
    PRINT '  - CallLogVerifications table already exists';
GO

-- ======================================================================
-- 4. CREATE CallLogPaymentAssignments Table
-- ======================================================================
PRINT '4. Creating CallLogPaymentAssignments table...';
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'CallLogPaymentAssignments')
BEGIN
    CREATE TABLE CallLogPaymentAssignments (
        Id INT PRIMARY KEY IDENTITY(1,1),
        CallRecordId INT NOT NULL,

        -- Assignment Details
        AssignedFrom NVARCHAR(50) NOT NULL,
        AssignedTo NVARCHAR(50) NOT NULL,
        AssignmentReason NVARCHAR(500) NOT NULL,
        AssignedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

        -- Acceptance
        AssignmentStatus NVARCHAR(20) NOT NULL DEFAULT 'Pending',
        AcceptedDate DATETIME2 NULL,
        RejectionReason NVARCHAR(500) NULL,

        -- Notification Tracking
        NotificationSent BIT NOT NULL DEFAULT 0,
        NotificationSentDate DATETIME2 NULL,
        NotificationViewedDate DATETIME2 NULL,

        -- Audit
        CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        ModifiedDate DATETIME2 NULL,

        -- Foreign Keys
        CONSTRAINT FK_CallLogPaymentAssignments_CallRecords FOREIGN KEY (CallRecordId)
            REFERENCES CallRecords(Id) ON DELETE NO ACTION
    );

    -- Indexes for performance
    CREATE INDEX IX_CallLogPaymentAssignments_AssignedTo ON CallLogPaymentAssignments(AssignedTo);
    CREATE INDEX IX_CallLogPaymentAssignments_AssignmentStatus ON CallLogPaymentAssignments(AssignmentStatus);
    CREATE INDEX IX_CallLogPaymentAssignments_AssignedFrom_AssignedTo ON CallLogPaymentAssignments(AssignedFrom, AssignedTo);

    PRINT '  - CallLogPaymentAssignments table created successfully';
END
ELSE
    PRINT '  - CallLogPaymentAssignments table already exists';
GO

-- ======================================================================
-- 5. CREATE CallLogDocuments Table
-- ======================================================================
PRINT '5. Creating CallLogDocuments table...';
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'CallLogDocuments')
BEGIN
    CREATE TABLE CallLogDocuments (
        Id INT PRIMARY KEY IDENTITY(1,1),
        CallLogVerificationId INT NOT NULL,

        -- File Details
        FileName NVARCHAR(255) NOT NULL,
        FilePath NVARCHAR(500) NOT NULL,
        FileSize BIGINT NOT NULL,
        ContentType NVARCHAR(100) NOT NULL,

        -- Document Type
        DocumentType NVARCHAR(50) NOT NULL,
        Description NVARCHAR(500) NULL,

        -- Upload Details
        UploadedBy NVARCHAR(50) NOT NULL,
        UploadedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

        -- Foreign Keys with CASCADE delete
        CONSTRAINT FK_CallLogDocuments_CallLogVerifications FOREIGN KEY (CallLogVerificationId)
            REFERENCES CallLogVerifications(Id) ON DELETE CASCADE
    );

    -- Indexes
    CREATE INDEX IX_CallLogDocuments_CallLogVerificationId ON CallLogDocuments(CallLogVerificationId);

    PRINT '  - CallLogDocuments table created successfully';
END
ELSE
    PRINT '  - CallLogDocuments table already exists';
GO

-- ======================================================================
-- 6. Verification Summary
-- ======================================================================
PRINT '';
PRINT '========================================';
PRINT 'Call Log Verification System Schema Update Complete!';
PRINT '========================================';
PRINT '';
PRINT 'Tables created/updated:';
PRINT '  ✓ CallRecords - Added verification columns';
PRINT '  ✓ ClassOfServices - Added numeric allowance columns';
PRINT '  ✓ CallLogVerifications';
PRINT '  ✓ CallLogPaymentAssignments';
PRINT '  ✓ CallLogDocuments';
PRINT '';
PRINT 'Next steps:';
PRINT '1. Create EF Core migration to sync with code';
PRINT '2. Implement services layer';
PRINT '3. Build UI components';
PRINT '';
GO
