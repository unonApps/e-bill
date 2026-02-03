-- Fixed migration script for server deployment
-- Run this on TABDB1 on server KEONSTCWBSVWD01

-- =====================================================
-- Migration 1: AddRefundRequestHistory
-- =====================================================
BEGIN TRANSACTION;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251127053250_AddRefundRequestHistory'
)
BEGIN
    CREATE TABLE [RefundRequestHistories] (
        [Id] int NOT NULL IDENTITY,
        [RefundRequestId] int NOT NULL,
        [Action] nvarchar(100) NOT NULL,
        [PreviousStatus] nvarchar(50) NULL,
        [NewStatus] nvarchar(50) NULL,
        [Comments] nvarchar(1000) NULL,
        [PerformedBy] nvarchar(450) NOT NULL,
        [UserName] nvarchar(200) NOT NULL,
        [Timestamp] datetime2 NOT NULL,
        [IpAddress] nvarchar(50) NULL,
        CONSTRAINT [PK_RefundRequestHistories] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RefundRequestHistories_RefundRequests_RefundRequestId] FOREIGN KEY ([RefundRequestId]) REFERENCES [RefundRequests] ([Id]) ON DELETE CASCADE
    );

    CREATE INDEX [IX_RefundRequestHistories_RefundRequestId] ON [RefundRequestHistories] ([RefundRequestId]);

    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251127053250_AddRefundRequestHistory', N'8.0.6');

    PRINT 'Migration AddRefundRequestHistory applied successfully';
END
ELSE
BEGIN
    PRINT 'Migration AddRefundRequestHistory already applied - skipping';
END

COMMIT;
GO

-- =====================================================
-- Migration 2: RemoveHighCostAnomalyType
-- =====================================================
BEGIN TRANSACTION;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251127120408_RemoveHighCostAnomalyType'
)
BEGIN
    DELETE FROM AnomalyTypes WHERE Code = 'HIGH_COST';

    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251127120408_RemoveHighCostAnomalyType', N'8.0.6');

    PRINT 'Migration RemoveHighCostAnomalyType applied successfully';
END
ELSE
BEGIN
    PRINT 'Migration RemoveHighCostAnomalyType already applied - skipping';
END

COMMIT;
GO

-- =====================================================
-- Migration 3: FixPushToProductionProcessingStatusValue
-- =====================================================

-- Check if migration already applied
IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251127122328_FixPushToProductionProcessingStatusValue'
)
BEGIN
    PRINT 'Migration FixPushToProductionProcessingStatusValue already applied - skipping';
END
ELSE
BEGIN
    PRINT 'Applying migration FixPushToProductionProcessingStatusValue...';

    -- Step 1: Drop existing stored procedure
    IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'sp_PushBatchToProduction')
    BEGIN
        DROP PROCEDURE sp_PushBatchToProduction;
        PRINT 'Dropped existing sp_PushBatchToProduction';
    END
END
GO

-- Step 2: Create the fixed stored procedure (must be first statement in batch)
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251127122328_FixPushToProductionProcessingStatusValue'
)
BEGIN
    PRINT 'Creating sp_PushBatchToProduction...';
END
GO

-- Only create if migration not yet applied
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251127122328_FixPushToProductionProcessingStatusValue'
)
EXEC('
CREATE PROCEDURE sp_PushBatchToProduction
    @BatchId UNIQUEIDENTIFIER,
    @VerificationPeriod DATETIME = NULL,
    @VerificationType NVARCHAR(50) = NULL,
    @ApprovalPeriod DATETIME = NULL,
    @PublishedBy NVARCHAR(256) = ''System''
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
        SELECT @BatchStatus = BatchStatus, @BatchName = BatchName
        FROM StagingBatches
        WHERE Id = @BatchId;

        IF @BatchName IS NULL BEGIN
            SELECT 0 AS Success, 0 AS RecordsPushed, 0 AS RemainingUnprocessed, ''Batch not found'' AS Error;
            ROLLBACK TRANSACTION;
            RETURN;
        END

        -- BatchStatus: Verified=2, PartiallyVerified=3
        IF @BatchStatus NOT IN (2, 3) BEGIN
            SELECT 0 AS Success, 0 AS RecordsPushed, 0 AS RemainingUnprocessed, ''Batch must be Verified or PartiallyVerified to push to production'' AS Error;
            ROLLBACK TRANSACTION;
            RETURN;
        END

        -- Count verified records (VerificationStatus: 0=Pending, 1=Verified, 2=Rejected, 3=RequiresReview)
        SELECT @VerifiedCount = COUNT(*)
        FROM CallLogStagings
        WHERE BatchId = @BatchId AND VerificationStatus = 1;

        IF @VerifiedCount = 0 BEGIN
            SELECT 0 AS Success, 0 AS RecordsPushed, 0 AS RemainingUnprocessed, ''No verified records found in batch'' AS Error;
            ROLLBACK TRANSACTION;
            RETURN;
        END

        -- STEP 1: Insert verified records into CallRecords (production)
        INSERT INTO CallRecords (
            ext_no, call_date, call_number, call_destination, call_endtime, call_duration,
            call_curr_code, call_cost, call_cost_usd, call_cost_kshs, call_type, call_dest_type,
            call_year, call_month, ext_resp_index, call_pay_index, UserPhoneId,
            assignment_status, call_ver_ind, call_ver_date, verification_type, verification_period,
            approval_period, revert_count, call_cert_ind, call_proc_ind, entry_date,
            SourceSystem, SourceBatchId, SourceStagingId
        )
        SELECT
            ExtensionNumber, CallDate, CallNumber, CallDestination, CallEndTime, CallDuration,
            CallCurrencyCode, CallCost, CallCostUSD, CallCostKSHS, CallType, CallDestinationType,
            CallYear, CallMonth, ResponsibleIndexNumber, PayingIndexNumber, UserPhoneId,
            ''None'', 0, NULL, @VerificationType, @VerificationPeriod,
            @ApprovalPeriod, 0, 0, 0, @CurrentDateTime,
            SourceSystem, BatchId, Id
        FROM CallLogStagings
        WHERE BatchId = @BatchId AND VerificationStatus = 1;

        SET @CallRecordsInserted = @@ROWCOUNT;

        -- STEP 2: Update CallLogStagings records as Completed
        -- ProcessingStatus: 0=Staged, 1=Processing, 2=Completed, 3=Failed, 4=Verified
        UPDATE CallLogStagings
        SET ProcessingStatus = 2, -- Completed (FIXED: was incorrectly 3)
            ProcessedDate = @CurrentDateTime
        WHERE BatchId = @BatchId AND VerificationStatus = 1;

        SET @StagingUpdated = @@ROWCOUNT;

        -- STEP 3: Update source tables (Safaricom, Airtel, PSTN, PrivateWire)
        -- Mark records as Completed (ProcessingStatus = 2)

        -- Update Safaricom source records
        UPDATE s
        SET s.ProcessingStatus = 2, -- Completed (FIXED: was incorrectly 3)
            s.ProcessedDate = @CurrentDateTime
        FROM Safaricom s
        INNER JOIN CallLogStagings cls ON cls.SourceRecordId = CAST(s.Id AS NVARCHAR(50))
        WHERE cls.BatchId = @BatchId
          AND cls.VerificationStatus = 1
          AND cls.SourceSystem = ''Safaricom'';

        SET @SafaricomUpdated = @@ROWCOUNT;

        -- Update Airtel source records
        UPDATE a
        SET a.ProcessingStatus = 2, -- Completed (FIXED: was incorrectly 3)
            a.ProcessedDate = @CurrentDateTime
        FROM Airtel a
        INNER JOIN CallLogStagings cls ON cls.SourceRecordId = CAST(a.Id AS NVARCHAR(50))
        WHERE cls.BatchId = @BatchId
          AND cls.VerificationStatus = 1
          AND cls.SourceSystem = ''Airtel'';

        SET @AirtelUpdated = @@ROWCOUNT;

        -- Update PSTN source records
        UPDATE p
        SET p.ProcessingStatus = 2, -- Completed (FIXED: was incorrectly 3)
            p.ProcessedDate = @CurrentDateTime
        FROM PSTNs p
        INNER JOIN CallLogStagings cls ON cls.SourceRecordId = CAST(p.Id AS NVARCHAR(50))
        WHERE cls.BatchId = @BatchId
          AND cls.VerificationStatus = 1
          AND cls.SourceSystem = ''PSTN'';

        SET @PSTNUpdated = @@ROWCOUNT;

        -- Update PrivateWire source records
        UPDATE pw
        SET pw.ProcessingStatus = 2, -- Completed (FIXED: was incorrectly 3)
            pw.ProcessedDate = @CurrentDateTime
        FROM PrivateWires pw
        INNER JOIN CallLogStagings cls ON cls.SourceRecordId = CAST(pw.Id AS NVARCHAR(50))
        WHERE cls.BatchId = @BatchId
          AND cls.VerificationStatus = 1
          AND cls.SourceSystem = ''PrivateWire'';

        SET @PrivateWireUpdated = @@ROWCOUNT;

        -- STEP 4: Count remaining unverified/rejected records
        SELECT @RemainingUnprocessed = COUNT(*)
        FROM CallLogStagings
        WHERE BatchId = @BatchId
          AND VerificationStatus != 1
          AND ProcessingStatus != 2;

        -- STEP 5: Update batch status
        IF @RemainingUnprocessed = 0 BEGIN
            UPDATE StagingBatches
            SET BatchStatus = 4, -- Published
                EndProcessingDate = @CurrentDateTime,
                PublishedBy = @PublishedBy
            WHERE Id = @BatchId;
        END
        ELSE BEGIN
            UPDATE StagingBatches
            SET BatchStatus = 3, -- PartiallyVerified
                PublishedBy = @PublishedBy
            WHERE Id = @BatchId;
        END

        -- Return success result
        SELECT
            1 AS Success,
            @CallRecordsInserted AS RecordsPushed,
            @RemainingUnprocessed AS RemainingUnprocessed,
            @SafaricomUpdated AS SafaricomUpdated,
            @AirtelUpdated AS AirtelUpdated,
            @PSTNUpdated AS PSTNUpdated,
            @PrivateWireUpdated AS PrivateWireUpdated,
            @CurrentDateTime AS CompletedAt,
            NULL AS Error;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;

        SELECT @ErrorMessage = ERROR_MESSAGE(),
               @ErrorSeverity = ERROR_SEVERITY(),
               @ErrorState = ERROR_STATE();

        SELECT
            0 AS Success,
            0 AS RecordsPushed,
            0 AS RemainingUnprocessed,
            0 AS SafaricomUpdated,
            0 AS AirtelUpdated,
            0 AS PSTNUpdated,
            0 AS PrivateWireUpdated,
            NULL AS CompletedAt,
            @ErrorMessage AS Error;

        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
    END CATCH
END
');
GO

-- Step 3: Fix existing records that were incorrectly marked as Failed
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251127122328_FixPushToProductionProcessingStatusValue'
)
BEGIN
    PRINT 'Fixing existing records with incorrect ProcessingStatus...';

    -- Fix CallLogStagings records that were incorrectly marked as Failed instead of Completed
    UPDATE CallLogStagings
    SET ProcessingStatus = 2  -- Completed
    WHERE ProcessingStatus = 3  -- Was incorrectly set to Failed
      AND ProcessedDate IS NOT NULL;
    PRINT 'Fixed CallLogStagings: ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' records';

    -- Fix Safaricom records
    UPDATE Safaricom
    SET ProcessingStatus = 2
    WHERE ProcessingStatus = 3
      AND ProcessedDate IS NOT NULL;
    PRINT 'Fixed Safaricom: ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' records';

    -- Fix Airtel records
    UPDATE Airtel
    SET ProcessingStatus = 2
    WHERE ProcessingStatus = 3
      AND ProcessedDate IS NOT NULL;
    PRINT 'Fixed Airtel: ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' records';

    -- Fix PSTN records
    UPDATE PSTNs
    SET ProcessingStatus = 2
    WHERE ProcessingStatus = 3
      AND ProcessedDate IS NOT NULL;
    PRINT 'Fixed PSTNs: ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' records';

    -- Fix PrivateWire records
    UPDATE PrivateWires
    SET ProcessingStatus = 2
    WHERE ProcessingStatus = 3
      AND ProcessedDate IS NOT NULL;
    PRINT 'Fixed PrivateWires: ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' records';

    -- Record migration as applied
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251127122328_FixPushToProductionProcessingStatusValue', N'8.0.6');

    PRINT 'Migration FixPushToProductionProcessingStatusValue applied successfully';
END
GO

PRINT '================================================';
PRINT 'All migrations completed!';
PRINT '================================================';
GO
