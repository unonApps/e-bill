-- =============================================
-- Stored Procedure: sp_PushBatchToProduction
-- Description: Efficiently pushes verified call log staging records to production
-- Purpose: Handles large datasets (1M+ records) without timeout or memory issues
-- Created: 2025-01-14
-- =============================================

USE [TABDB]
GO

-- Drop procedure if it exists
DROP PROCEDURE IF EXISTS sp_PushBatchToProduction;
GO

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
        SELECT @BatchStatus = BatchStatus, @BatchName = BatchName
        FROM StagingBatches
        WHERE Id = @BatchId;

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
        SELECT @VerifiedCount = COUNT(*)
        FROM CallLogStagings
        WHERE BatchId = @BatchId AND VerificationStatus = 1; -- Verified

        IF @VerifiedCount = 0 BEGIN
            SELECT 0 AS Success, 0 AS RecordsPushed, 0 AS RemainingUnprocessed, 'No verified records found in batch' AS Error;
            ROLLBACK TRANSACTION;
            RETURN;
        END

        -- =============================================
        -- STEP 1: Insert verified records into CallRecords (production)
        -- Using correct column names from CallRecord model
        -- =============================================
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
            'None', -- assignment_status: belongs to original phone owner
            0, -- call_ver_ind: false initially for user verification workflow
            NULL, -- call_ver_date
            @VerificationType, -- verification_type (Official/Personal)
            @VerificationPeriod, -- verification_period deadline
            @ApprovalPeriod, -- approval_period deadline
            0, -- revert_count
            0, -- call_cert_ind
            0, -- call_proc_ind
            @CurrentDateTime, -- entry_date
            SourceSystem, -- Keep source system
            BatchId, -- SourceBatchId
            Id -- SourceStagingId
        FROM CallLogStagings
        WHERE BatchId = @BatchId AND VerificationStatus = 1; -- Verified only

        SET @CallRecordsInserted = @@ROWCOUNT;

        -- =============================================
        -- STEP 2: Update CallLogStagings records as Completed
        -- =============================================
        UPDATE CallLogStagings
        SET ProcessingStatus = 3, -- Completed
            ProcessedDate = @CurrentDateTime
        WHERE BatchId = @BatchId AND VerificationStatus = 1;

        SET @StagingUpdated = @@ROWCOUNT;

        -- =============================================
        -- STEP 3: Update source tables (Safaricom, Airtel, PSTN, PrivateWire)
        -- Mark records as Completed
        -- =============================================

        -- Update Safaricom source records
        UPDATE s
        SET s.ProcessingStatus = 3, -- Completed
            s.ProcessedDate = @CurrentDateTime
        FROM Safaricom s
        INNER JOIN CallLogStagings cls ON cls.SourceRecordId = CAST(s.Id AS NVARCHAR(50))
        WHERE cls.BatchId = @BatchId
          AND cls.VerificationStatus = 1
          AND cls.SourceSystem = 'Safaricom';

        SET @SafaricomUpdated = @@ROWCOUNT;

        -- Update Airtel source records
        UPDATE a
        SET a.ProcessingStatus = 3, -- Completed
            a.ProcessedDate = @CurrentDateTime
        FROM Airtel a
        INNER JOIN CallLogStagings cls ON cls.SourceRecordId = CAST(a.Id AS NVARCHAR(50))
        WHERE cls.BatchId = @BatchId
          AND cls.VerificationStatus = 1
          AND cls.SourceSystem = 'Airtel';

        SET @AirtelUpdated = @@ROWCOUNT;

        -- Update PSTN source records
        UPDATE p
        SET p.ProcessingStatus = 3, -- Completed
            p.ProcessedDate = @CurrentDateTime
        FROM PSTNs p
        INNER JOIN CallLogStagings cls ON cls.SourceRecordId = CAST(p.Id AS NVARCHAR(50))
        WHERE cls.BatchId = @BatchId
          AND cls.VerificationStatus = 1
          AND cls.SourceSystem = 'PSTN';

        SET @PSTNUpdated = @@ROWCOUNT;

        -- Update PrivateWire source records
        UPDATE pw
        SET pw.ProcessingStatus = 3, -- Completed
            pw.ProcessedDate = @CurrentDateTime
        FROM PrivateWires pw
        INNER JOIN CallLogStagings cls ON cls.SourceRecordId = CAST(pw.Id AS NVARCHAR(50))
        WHERE cls.BatchId = @BatchId
          AND cls.VerificationStatus = 1
          AND cls.SourceSystem = 'PrivateWire';

        SET @PrivateWireUpdated = @@ROWCOUNT;

        -- =============================================
        -- STEP 4: Count remaining unverified/rejected records
        -- =============================================
        SELECT @RemainingUnprocessed = COUNT(*)
        FROM CallLogStagings
        WHERE BatchId = @BatchId
          AND VerificationStatus != 1 -- Not Verified
          AND ProcessingStatus != 3; -- Not Completed

        -- =============================================
        -- STEP 5: Update batch status
        -- =============================================
        IF @RemainingUnprocessed = 0 BEGIN
            -- All records processed - mark as Published
            UPDATE StagingBatches
            SET BatchStatus = 4, -- Published
                EndProcessingDate = @CurrentDateTime,
                PublishedBy = @PublishedBy
            WHERE Id = @BatchId;
        END
        ELSE BEGIN
            -- Some records remain - keep as PartiallyVerified
            UPDATE StagingBatches
            SET BatchStatus = 3, -- PartiallyVerified
                PublishedBy = @PublishedBy
            WHERE Id = @BatchId;
        END

        -- Return success result with all columns
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

        -- Return error result with all columns
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
GO

PRINT 'Stored procedure sp_PushBatchToProduction created successfully!';
PRINT '';
PRINT 'Usage example:';
PRINT 'DECLARE @BatchId UNIQUEIDENTIFIER = ''YOUR-BATCH-ID-HERE'';';
PRINT 'DECLARE @VerificationDeadline DATETIME = DATEADD(DAY, 7, GETUTCDATE());';
PRINT 'DECLARE @ApprovalDeadline DATETIME = DATEADD(DAY, 14, GETUTCDATE());';
PRINT 'EXEC sp_PushBatchToProduction @BatchId, @VerificationDeadline, ''Official'', @ApprovalDeadline, ''admin@example.com'';';
GO
