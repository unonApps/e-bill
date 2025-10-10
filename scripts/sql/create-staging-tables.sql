-- Create Staging Tables for Call Logs System
-- Run this script in your SQL Server database

-- 1. Create AnomalyTypes table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='AnomalyTypes' AND xtype='U')
BEGIN
    CREATE TABLE AnomalyTypes (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Code NVARCHAR(50) NOT NULL UNIQUE,
        Name NVARCHAR(100) NOT NULL,
        Description NVARCHAR(500),
        Severity INT NOT NULL,
        AutoReject BIT NOT NULL DEFAULT 0,
        IsActive BIT NOT NULL DEFAULT 1
    );

    -- Insert default anomaly types
    INSERT INTO AnomalyTypes (Code, Name, Description, Severity, AutoReject, IsActive) VALUES
    ('NO_USER', 'No Active User', 'Extension number not linked to any active eBill user', 2, 0, 1),
    ('INACTIVE_USER', 'Inactive User', 'Extension linked to inactive eBill user', 1, 0, 1),
    ('HIGH_COST', 'Unusually High Cost', 'Call cost exceeds threshold', 2, 0, 1),
    ('DUPLICATE', 'Duplicate Record', 'Potential duplicate call record', 0, 0, 1),
    ('INVALID_NUMBER', 'Invalid Phone Number', 'Destination number format is invalid', 1, 0, 1),
    ('FUTURE_DATE', 'Future Date', 'Call date is in the future', 3, 1, 1),
    ('EXCESSIVE_DURATION', 'Excessive Duration', 'Call duration exceeds reasonable limits', 1, 0, 1);

    PRINT 'AnomalyTypes table created successfully';
END
ELSE
BEGIN
    PRINT 'AnomalyTypes table already exists';
END
GO

-- 2. Create StagingBatches table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='StagingBatches' AND xtype='U')
BEGIN
    CREATE TABLE StagingBatches (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        BatchName NVARCHAR(100) NOT NULL,
        BatchType NVARCHAR(50),
        TotalRecords INT NOT NULL DEFAULT 0,
        VerifiedRecords INT NOT NULL DEFAULT 0,
        RejectedRecords INT NOT NULL DEFAULT 0,
        PendingRecords INT NOT NULL DEFAULT 0,
        RecordsWithAnomalies INT NOT NULL DEFAULT 0,
        CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        StartProcessingDate DATETIME2 NULL,
        EndProcessingDate DATETIME2 NULL,
        BatchStatus INT NOT NULL DEFAULT 0,
        CreatedBy NVARCHAR(100) NOT NULL,
        VerifiedBy NVARCHAR(100) NULL,
        PublishedBy NVARCHAR(100) NULL,
        SourceSystems NVARCHAR(200) NULL,
        Notes NVARCHAR(MAX) NULL
    );

    CREATE INDEX IX_StagingBatches_CreatedDate ON StagingBatches(CreatedDate);
    CREATE INDEX IX_StagingBatches_BatchStatus ON StagingBatches(BatchStatus);

    PRINT 'StagingBatches table created successfully';
END
ELSE
BEGIN
    PRINT 'StagingBatches table already exists';
END
GO

-- 3. Create CallLogStagings table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='CallLogStagings' AND xtype='U')
BEGIN
    CREATE TABLE CallLogStagings (
        Id INT IDENTITY(1,1) PRIMARY KEY,

        -- Core Call Data
        ExtensionNumber NVARCHAR(50) NOT NULL DEFAULT '',
        CallDate DATETIME2 NOT NULL,
        CallNumber NVARCHAR(50) NOT NULL DEFAULT '',
        CallDestination NVARCHAR(100) NOT NULL DEFAULT '',
        CallEndTime DATETIME2 NOT NULL,
        CallDuration INT NOT NULL DEFAULT 0, -- in seconds
        CallCurrencyCode NVARCHAR(10) NOT NULL DEFAULT '',
        CallCost DECIMAL(18,4) NOT NULL DEFAULT 0,
        CallCostUSD DECIMAL(18,4) NOT NULL DEFAULT 0,
        CallCostKSHS DECIMAL(18,4) NOT NULL DEFAULT 0,
        CallType NVARCHAR(50) NOT NULL DEFAULT '', -- Voice/SMS/Data
        CallDestinationType NVARCHAR(50) NOT NULL DEFAULT '', -- Local/International/Mobile

        -- Date Dimensions
        CallYear INT NOT NULL,
        CallMonth INT NOT NULL,

        -- User Mapping
        ResponsibleIndexNumber NVARCHAR(50) NULL,
        PayingIndexNumber NVARCHAR(50) NULL,

        -- Source Information
        SourceSystem NVARCHAR(50) NOT NULL DEFAULT '', -- Safaricom/Airtel/PSTN/PrivateWire
        SourceRecordId NVARCHAR(100) NULL,

        -- Staging Metadata
        BatchId UNIQUEIDENTIFIER NOT NULL,
        ImportedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        ImportedBy NVARCHAR(100) NOT NULL DEFAULT '',

        -- Verification Status
        VerificationStatus INT NOT NULL DEFAULT 0, -- 0=Pending, 1=Verified, 2=Rejected, 3=RequiresReview
        VerificationDate DATETIME2 NULL,
        VerifiedBy NVARCHAR(100) NULL,
        VerificationNotes NVARCHAR(MAX) NULL,

        -- Anomaly Detection
        HasAnomalies BIT NOT NULL DEFAULT 0,
        AnomalyTypes NVARCHAR(MAX) NULL, -- JSON array of anomaly types
        AnomalyDetails NVARCHAR(MAX) NULL, -- JSON object with details

        -- Processing Status
        ProcessingStatus INT NOT NULL DEFAULT 0, -- 0=Staged, 1=Processing, 2=Completed, 3=Failed
        ProcessedDate DATETIME2 NULL,
        ErrorDetails NVARCHAR(MAX) NULL,

        -- Audit
        CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        ModifiedDate DATETIME2 NULL,
        ModifiedBy NVARCHAR(100) NULL,

        -- Foreign Key Constraints
        CONSTRAINT FK_CallLogStagings_Batch FOREIGN KEY (BatchId) REFERENCES StagingBatches(Id) ON DELETE CASCADE
    );

    -- Create indexes for performance
    CREATE INDEX IX_CallLogStagings_BatchId ON CallLogStagings(BatchId);
    CREATE INDEX IX_CallLogStagings_VerificationStatus ON CallLogStagings(VerificationStatus);
    CREATE INDEX IX_CallLogStagings_ResponsibleIndexNumber ON CallLogStagings(ResponsibleIndexNumber);
    CREATE INDEX IX_CallLogStagings_CallDate ON CallLogStagings(CallDate);
    CREATE INDEX IX_CallLogStagings_ExtCallDate ON CallLogStagings(ExtensionNumber, CallDate, CallNumber);

    PRINT 'CallLogStagings table created successfully';
END
ELSE
BEGIN
    PRINT 'CallLogStagings table already exists';
END
GO

-- 4. Create CallRecords table (Production)
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='CallRecords' AND xtype='U')
BEGIN
    CREATE TABLE CallRecords (
        Id INT IDENTITY(1,1) PRIMARY KEY,

        -- Core Call Data
        ext_no NVARCHAR(50) NOT NULL DEFAULT '',
        call_date DATETIME2 NOT NULL,
        call_number NVARCHAR(50) NOT NULL DEFAULT '',
        call_destination NVARCHAR(100) NOT NULL DEFAULT '',
        call_endtime DATETIME2 NOT NULL,
        call_duration INT NOT NULL DEFAULT 0,
        call_curr_code NVARCHAR(10) NOT NULL DEFAULT '',
        call_cost DECIMAL(18,4) NOT NULL DEFAULT 0,
        call_cost_usd DECIMAL(18,4) NOT NULL DEFAULT 0,
        call_cost_kshs DECIMAL(18,4) NOT NULL DEFAULT 0,
        call_type NVARCHAR(50) NOT NULL DEFAULT '',
        call_dest_type NVARCHAR(50) NOT NULL DEFAULT '',
        call_year INT NOT NULL,
        call_month INT NOT NULL,

        -- User responsibility and payment
        ext_resp_index NVARCHAR(50) NULL,
        call_pay_index NVARCHAR(50) NULL,

        -- Verification indicators
        call_ver_ind BIT NOT NULL DEFAULT 0,
        call_ver_date DATETIME2 NULL,

        -- Certification indicators
        call_cert_ind BIT NOT NULL DEFAULT 0,
        call_cert_date DATETIME2 NULL,
        call_cert_by NVARCHAR(100) NULL,

        -- Processing indicator
        call_proc_ind BIT NOT NULL DEFAULT 0,
        entry_date DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        call_dest_descr NVARCHAR(200) NULL,

        -- Additional metadata for tracking
        SourceSystem NVARCHAR(50) NULL,
        SourceBatchId UNIQUEIDENTIFIER NULL,
        SourceStagingId INT NULL
    );

    -- Create indexes
    CREATE INDEX IX_CallRecords_CallDate ON CallRecords(call_date);
    CREATE INDEX IX_CallRecords_ExtensionNumber ON CallRecords(ext_no);
    CREATE INDEX IX_CallRecords_ResponsibleIndex ON CallRecords(ext_resp_index);
    CREATE INDEX IX_CallRecords_YearMonth ON CallRecords(call_year, call_month);

    PRINT 'CallRecords table created successfully';
END
ELSE
BEGIN
    PRINT 'CallRecords table already exists';
END
GO

PRINT '';
PRINT 'All staging tables have been created successfully!';
PRINT 'You can now use the Call Log Staging feature.';