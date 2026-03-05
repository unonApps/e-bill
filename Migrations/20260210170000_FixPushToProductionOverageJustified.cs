using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TAB.Web.Migrations
{
    /// <inheritdoc />
    public partial class FixPushToProductionOverageJustified : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop the old stored procedure that's missing overage_justified column
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_PushBatchToProduction");

            // Recreate with overage_justified included in INSERT into CallRecords
            migrationBuilder.Sql(@"
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

        -- Count verified records NOT already pushed (ProcessingStatus != 2)
        SELECT @VerifiedCount = COUNT(*)
        FROM CallLogStagings
        WHERE BatchId = @BatchId AND VerificationStatus = 1 AND ProcessingStatus != 2;

        IF @VerifiedCount = 0 BEGIN
            SELECT 0 AS Success, 0 AS RecordsPushed, 0 AS RemainingUnprocessed, 'No verified records found in batch (all already pushed)' AS Error;
            ROLLBACK TRANSACTION;
            RETURN;
        END

        -- STEP 1: Insert only verified records NOT already pushed
        INSERT INTO CallRecords (
            ext_no, call_date, call_number, call_destination, call_endtime, call_duration,
            call_curr_code, call_cost, call_cost_usd, call_cost_kshs, call_type, call_dest_type,
            call_year, call_month, ext_resp_index, call_pay_index, UserPhoneId,
            assignment_status, overage_justified, call_ver_ind, call_ver_date, verification_type,
            verification_period, approval_period, revert_count, call_cert_ind, call_proc_ind,
            entry_date, SourceSystem, SourceBatchId, SourceStagingId
        )
        SELECT
            ExtensionNumber, CallDate, CallNumber, CallDestination, CallEndTime, CallDuration,
            CallCurrencyCode, CallCost, CallCostUSD, CallCostKSHS, CallType, CallDestinationType,
            CallYear, CallMonth, ResponsibleIndexNumber, PayingIndexNumber, UserPhoneId,
            'None', 0, 0, NULL, @VerificationType,
            @VerificationPeriod, @ApprovalPeriod, 0, 0, 0,
            @CurrentDateTime, SourceSystem, BatchId, Id
        FROM CallLogStagings
        WHERE BatchId = @BatchId AND VerificationStatus = 1 AND ProcessingStatus != 2;

        SET @CallRecordsInserted = @@ROWCOUNT;

        -- STEP 2: Mark pushed staging records as Completed
        UPDATE CallLogStagings
        SET ProcessingStatus = 2,
            ProcessedDate = @CurrentDateTime
        WHERE BatchId = @BatchId AND VerificationStatus = 1 AND ProcessingStatus != 2;

        SET @StagingUpdated = @@ROWCOUNT;

        -- STEP 3: Update source tables (Safaricom, Airtel, PSTN, PrivateWire)
        -- Mark records as Completed (ProcessingStatus = 2)

        -- Update Safaricom source records
        UPDATE s
        SET s.ProcessingStatus = 2,
            s.ProcessedDate = @CurrentDateTime
        FROM Safaricom s
        INNER JOIN CallLogStagings cls ON cls.SourceRecordId = CAST(s.Id AS NVARCHAR(50))
        WHERE cls.BatchId = @BatchId
          AND cls.VerificationStatus = 1
          AND cls.SourceSystem = 'Safaricom';

        SET @SafaricomUpdated = @@ROWCOUNT;

        -- Update Airtel source records
        UPDATE a
        SET a.ProcessingStatus = 2,
            a.ProcessedDate = @CurrentDateTime
        FROM Airtel a
        INNER JOIN CallLogStagings cls ON cls.SourceRecordId = CAST(a.Id AS NVARCHAR(50))
        WHERE cls.BatchId = @BatchId
          AND cls.VerificationStatus = 1
          AND cls.SourceSystem = 'Airtel';

        SET @AirtelUpdated = @@ROWCOUNT;

        -- Update PSTN source records
        UPDATE p
        SET p.ProcessingStatus = 2,
            p.ProcessedDate = @CurrentDateTime
        FROM PSTNs p
        INNER JOIN CallLogStagings cls ON cls.SourceRecordId = CAST(p.Id AS NVARCHAR(50))
        WHERE cls.BatchId = @BatchId
          AND cls.VerificationStatus = 1
          AND cls.SourceSystem = 'PSTN';

        SET @PSTNUpdated = @@ROWCOUNT;

        -- Update PrivateWire source records
        UPDATE pw
        SET pw.ProcessingStatus = 2,
            pw.ProcessedDate = @CurrentDateTime
        FROM PrivateWires pw
        INNER JOIN CallLogStagings cls ON cls.SourceRecordId = CAST(pw.Id AS NVARCHAR(50))
        WHERE cls.BatchId = @BatchId
          AND cls.VerificationStatus = 1
          AND cls.SourceSystem = 'PrivateWire';

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
END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_PushBatchToProduction");
        }
    }
}
