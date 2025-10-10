-- ================================================================
-- CREATE BILLING PERIOD MANAGEMENT TABLES
-- ================================================================
USE TABDB;
GO

SET NOCOUNT ON;
PRINT '================================================================';
PRINT 'CREATING BILLING PERIOD MANAGEMENT TABLES';
PRINT '================================================================';
PRINT '';

-- ================================================================
-- 1. BILLING PERIODS TABLE
-- ================================================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[BillingPeriods]') AND type in (N'U'))
BEGIN
    CREATE TABLE BillingPeriods (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        PeriodCode NVARCHAR(20) NOT NULL UNIQUE, -- '2024-09'
        StartDate DATETIME NOT NULL,
        EndDate DATETIME NOT NULL,
        Status NVARCHAR(20) NOT NULL DEFAULT 'OPEN', -- 'OPEN', 'PROCESSING', 'CLOSED', 'LOCKED'

        -- Monthly billing info
        MonthlyImportDate DATETIME NULL,
        MonthlyBatchId UNIQUEIDENTIFIER NULL,
        MonthlyRecordCount INT DEFAULT 0,
        MonthlyTotalCost DECIMAL(18,2) DEFAULT 0,

        -- Interim updates tracking
        InterimUpdateCount INT DEFAULT 0,
        LastInterimDate DATETIME NULL,
        InterimRecordCount INT DEFAULT 0,
        InterimAdjustmentAmount DECIMAL(18,2) DEFAULT 0,

        -- Closure info
        ClosedDate DATETIME NULL,
        ClosedBy NVARCHAR(100) NULL,
        LockedDate DATETIME NULL,
        LockedBy NVARCHAR(100) NULL,

        -- Audit
        CreatedDate DATETIME DEFAULT GETDATE(),
        CreatedBy NVARCHAR(100),
        ModifiedDate DATETIME NULL,
        ModifiedBy NVARCHAR(100) NULL,
        Notes NVARCHAR(MAX)
    );

    CREATE INDEX IX_BillingPeriods_PeriodCode ON BillingPeriods(PeriodCode);
    CREATE INDEX IX_BillingPeriods_Status ON BillingPeriods(Status);
    CREATE INDEX IX_BillingPeriods_Dates ON BillingPeriods(StartDate, EndDate);

    PRINT '✓ Created BillingPeriods table';
END
ELSE
    PRINT '- BillingPeriods table already exists';

-- ================================================================
-- 2. INTERIM UPDATES TABLE
-- ================================================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[InterimUpdates]') AND type in (N'U'))
BEGIN
    CREATE TABLE InterimUpdates (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        BillingPeriodId INT NOT NULL,
        UpdateType NVARCHAR(50) NOT NULL, -- 'CORRECTION', 'DISPUTE', 'LATE_ARRIVAL', 'ADJUSTMENT'
        BatchId UNIQUEIDENTIFIER NOT NULL,

        -- What changed
        RecordsAdded INT DEFAULT 0,
        RecordsModified INT DEFAULT 0,
        RecordsDeleted INT DEFAULT 0,
        NetAdjustmentAmount DECIMAL(18,2),

        -- Approval workflow
        RequestedBy NVARCHAR(100) NOT NULL,
        RequestedDate DATETIME NOT NULL,
        ApprovedBy NVARCHAR(100) NULL,
        ApprovalDate DATETIME NULL,
        ApprovalStatus NVARCHAR(20) DEFAULT 'PENDING', -- 'PENDING', 'APPROVED', 'REJECTED'
        RejectionReason NVARCHAR(500) NULL,

        -- Documentation
        Justification NVARCHAR(MAX) NOT NULL,
        SupportingDocuments NVARCHAR(MAX) NULL, -- JSON array of file paths

        -- Processing
        ProcessedDate DATETIME NULL,
        ProcessingNotes NVARCHAR(MAX),

        CONSTRAINT FK_InterimUpdates_BillingPeriod FOREIGN KEY (BillingPeriodId)
            REFERENCES BillingPeriods(Id)
    );

    CREATE INDEX IX_InterimUpdates_Period ON InterimUpdates(BillingPeriodId);
    CREATE INDEX IX_InterimUpdates_Status ON InterimUpdates(ApprovalStatus);
    CREATE INDEX IX_InterimUpdates_BatchId ON InterimUpdates(BatchId);

    PRINT '✓ Created InterimUpdates table';
END
ELSE
    PRINT '- InterimUpdates table already exists';

-- ================================================================
-- 3. CALL LOG RECONCILIATION TABLE
-- ================================================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[CallLogReconciliations]') AND type in (N'U'))
BEGIN
    CREATE TABLE CallLogReconciliations (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        BillingPeriodId INT NOT NULL,
        SourceRecordId INT NOT NULL,
        SourceTable NVARCHAR(50) NOT NULL, -- 'Safaricom', 'Airtel', etc.

        -- Version tracking
        Version INT DEFAULT 1,
        ImportType NVARCHAR(20) NOT NULL, -- 'MONTHLY', 'INTERIM'
        ImportBatchId UNIQUEIDENTIFIER NOT NULL,
        ImportDate DATETIME NOT NULL,

        -- Change tracking
        PreviousAmount DECIMAL(18,2) NULL,
        CurrentAmount DECIMAL(18,2) NOT NULL,
        AdjustmentAmount AS (CurrentAmount - ISNULL(PreviousAmount, 0)) PERSISTED,
        AdjustmentReason NVARCHAR(500) NULL,

        -- Supersession tracking
        IsSuperseded BIT DEFAULT 0,
        SupersededBy INT NULL,
        SupersededDate DATETIME NULL,

        CONSTRAINT FK_Reconciliations_BillingPeriod FOREIGN KEY (BillingPeriodId)
            REFERENCES BillingPeriods(Id)
    );

    CREATE INDEX IX_Reconciliation_Period_Source ON CallLogReconciliations(BillingPeriodId, SourceTable, SourceRecordId);
    CREATE INDEX IX_Reconciliation_Version ON CallLogReconciliations(BillingPeriodId, Version);
    CREATE INDEX IX_Reconciliation_Superseded ON CallLogReconciliations(IsSuperseded);

    PRINT '✓ Created CallLogReconciliations table';
END
ELSE
    PRINT '- CallLogReconciliations table already exists';

-- ================================================================
-- 4. UPDATE EXISTING TABLES
-- ================================================================
PRINT '';
PRINT 'Updating existing tables...';

-- Update CallLogStagings
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[CallLogStagings]') AND name = 'BillingPeriodId')
BEGIN
    ALTER TABLE CallLogStagings ADD
        BillingPeriodId INT NULL,
        ImportType NVARCHAR(20) DEFAULT 'MONTHLY',
        IsAdjustment BIT DEFAULT 0,
        OriginalRecordId INT NULL,
        AdjustmentReason NVARCHAR(500) NULL;

    CREATE INDEX IX_CallLogStagings_BillingPeriod ON CallLogStagings(BillingPeriodId);
    PRINT '✓ Added billing period columns to CallLogStagings';
END

-- Update StagingBatches
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[StagingBatches]') AND name = 'BillingPeriodId')
BEGIN
    ALTER TABLE StagingBatches ADD
        BillingPeriodId INT NULL,
        BatchCategory NVARCHAR(20) DEFAULT 'MONTHLY';

    PRINT '✓ Added billing period columns to StagingBatches';
END

-- Update source tables
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Safaricom]') AND name = 'BillingPeriod')
BEGIN
    ALTER TABLE Safaricom ADD BillingPeriod NVARCHAR(20) NULL;
    ALTER TABLE Airtel ADD BillingPeriod NVARCHAR(20) NULL;
    ALTER TABLE PSTNs ADD BillingPeriod NVARCHAR(20) NULL;
    ALTER TABLE PrivateWires ADD BillingPeriod NVARCHAR(20) NULL;

    PRINT '✓ Added BillingPeriod column to source tables';
END

PRINT '';

-- ================================================================
-- 5. CREATE INITIAL BILLING PERIODS
-- ================================================================
PRINT 'Creating initial billing periods...';

DECLARE @CurrentDate DATETIME = GETDATE();
DECLARE @StartMonth DATETIME = DATEADD(MONTH, -2, @CurrentDate); -- Start 2 months back

WHILE @StartMonth <= @CurrentDate
BEGIN
    DECLARE @PeriodCode NVARCHAR(20) = FORMAT(@StartMonth, 'yyyy-MM');

    IF NOT EXISTS (SELECT 1 FROM BillingPeriods WHERE PeriodCode = @PeriodCode)
    BEGIN
        INSERT INTO BillingPeriods (
            PeriodCode,
            StartDate,
            EndDate,
            Status,
            CreatedDate,
            CreatedBy,
            Notes
        ) VALUES (
            @PeriodCode,
            DATEFROMPARTS(YEAR(@StartMonth), MONTH(@StartMonth), 1),
            EOMONTH(@StartMonth),
            CASE
                WHEN @StartMonth < DATEADD(MONTH, -1, @CurrentDate) THEN 'CLOSED'
                WHEN MONTH(@StartMonth) = MONTH(@CurrentDate) THEN 'OPEN'
                ELSE 'PROCESSING'
            END,
            GETDATE(),
            'System',
            'Auto-created during setup'
        );

        PRINT '✓ Created billing period: ' + @PeriodCode;
    END

    SET @StartMonth = DATEADD(MONTH, 1, @StartMonth);
END

PRINT '';

-- ================================================================
-- 6. CREATE HELPER FUNCTIONS
-- ================================================================
PRINT 'Creating helper functions...';
GO

-- Function to get current billing period
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[fn_GetCurrentBillingPeriod]'))
    DROP FUNCTION fn_GetCurrentBillingPeriod;
GO

CREATE FUNCTION fn_GetCurrentBillingPeriod()
RETURNS INT
AS
BEGIN
    DECLARE @PeriodId INT;

    SELECT TOP 1 @PeriodId = Id
    FROM BillingPeriods
    WHERE Status IN ('OPEN', 'PROCESSING')
    ORDER BY StartDate DESC;

    RETURN @PeriodId;
END
GO

PRINT '✓ Created fn_GetCurrentBillingPeriod function';

-- ================================================================
-- 7. CREATE STORED PROCEDURES
-- ================================================================
PRINT 'Creating stored procedures...';
GO

-- Procedure to close billing period
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_CloseBillingPeriod]'))
    DROP PROCEDURE sp_CloseBillingPeriod;
GO

CREATE PROCEDURE sp_CloseBillingPeriod
    @PeriodId INT,
    @ClosedBy NVARCHAR(100),
    @Force BIT = 0
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @CurrentStatus NVARCHAR(20);
    DECLARE @UnprocessedCount INT;

    -- Get current status
    SELECT @CurrentStatus = Status
    FROM BillingPeriods
    WHERE Id = @PeriodId;

    IF @CurrentStatus IS NULL
    BEGIN
        RAISERROR('Billing period not found', 16, 1);
        RETURN;
    END

    IF @CurrentStatus IN ('CLOSED', 'LOCKED')
    BEGIN
        RAISERROR('Billing period is already closed', 16, 1);
        RETURN;
    END

    -- Check for unprocessed records
    SELECT @UnprocessedCount = COUNT(*)
    FROM CallLogStagings
    WHERE BillingPeriodId = @PeriodId
    AND VerificationStatus = 0; -- Pending

    IF @UnprocessedCount > 0 AND @Force = 0
    BEGIN
        RAISERROR('Cannot close period: %d unprocessed staging records exist', 16, 1, @UnprocessedCount);
        RETURN;
    END

    -- Calculate final statistics
    UPDATE bp
    SET
        Status = 'CLOSED',
        ClosedDate = GETDATE(),
        ClosedBy = @ClosedBy,
        MonthlyRecordCount = (
            SELECT COUNT(*)
            FROM CallRecords
            WHERE YEAR(call_date) = YEAR(bp.StartDate)
            AND MONTH(call_date) = MONTH(bp.StartDate)
        ),
        MonthlyTotalCost = (
            SELECT ISNULL(SUM(call_cost_usd), 0)
            FROM CallRecords
            WHERE YEAR(call_date) = YEAR(bp.StartDate)
            AND MONTH(call_date) = MONTH(bp.StartDate)
        ),
        ModifiedDate = GETDATE(),
        ModifiedBy = @ClosedBy,
        Notes = ISNULL(Notes, '') + CHAR(13) + CHAR(10) +
                'Closed on ' + CONVERT(VARCHAR, GETDATE(), 120) + ' by ' + @ClosedBy
    FROM BillingPeriods bp
    WHERE bp.Id = @PeriodId;

    PRINT 'Billing period closed successfully';
END
GO

PRINT '✓ Created sp_CloseBillingPeriod procedure';

-- ================================================================
-- 8. CREATE VIEW FOR PERIOD SUMMARY
-- ================================================================
IF EXISTS (SELECT * FROM sys.views WHERE name = 'vw_BillingPeriodSummary')
    DROP VIEW vw_BillingPeriodSummary;
GO

CREATE VIEW vw_BillingPeriodSummary
AS
SELECT
    bp.Id,
    bp.PeriodCode,
    bp.StartDate,
    bp.EndDate,
    bp.Status,
    bp.MonthlyRecordCount,
    bp.MonthlyTotalCost,
    bp.InterimUpdateCount,
    bp.InterimAdjustmentAmount,
    bp.MonthlyTotalCost + bp.InterimAdjustmentAmount AS TotalAmount,

    -- Pending items
    (SELECT COUNT(*) FROM InterimUpdates
     WHERE BillingPeriodId = bp.Id AND ApprovalStatus = 'PENDING') AS PendingApprovals,

    (SELECT COUNT(*) FROM CallLogStagings
     WHERE BillingPeriodId = bp.Id AND VerificationStatus = 0) AS UnverifiedRecords,

    -- Status indicators
    CASE
        WHEN bp.Status = 'LOCKED' THEN '🔒 Locked'
        WHEN bp.Status = 'CLOSED' THEN '✅ Closed'
        WHEN bp.Status = 'PROCESSING' THEN '⚙️ Processing'
        WHEN bp.Status = 'OPEN' THEN '📂 Open'
        ELSE bp.Status
    END AS StatusDisplay,

    bp.ClosedDate,
    bp.ClosedBy
FROM BillingPeriods bp;
GO

PRINT '✓ Created vw_BillingPeriodSummary view';
PRINT '';

-- ================================================================
-- FINAL STATUS
-- ================================================================
PRINT '================================================================';
PRINT 'BILLING PERIOD SETUP COMPLETED';
PRINT '================================================================';
PRINT '';
PRINT 'Summary:';
SELECT
    COUNT(*) as TotalPeriods,
    SUM(CASE WHEN Status = 'OPEN' THEN 1 ELSE 0 END) as OpenPeriods,
    SUM(CASE WHEN Status = 'PROCESSING' THEN 1 ELSE 0 END) as ProcessingPeriods,
    SUM(CASE WHEN Status = 'CLOSED' THEN 1 ELSE 0 END) as ClosedPeriods
FROM BillingPeriods;

PRINT '';
PRINT 'Current billing periods:';
SELECT * FROM vw_BillingPeriodSummary ORDER BY StartDate DESC;