using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TAB.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddBatchStoredProcedures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create sp_ConsolidateCallLogBatch stored procedure
            migrationBuilder.Sql(@"
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
END");

            // Create sp_DeleteBatch stored procedure
            migrationBuilder.Sql(@"
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
END");

            // Create sp_PushBatchToProduction stored procedure
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
END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_ConsolidateCallLogBatch");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_DeleteBatch");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_PushBatchToProduction");
        }
    }
}
