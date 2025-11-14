-- =============================================
-- Script to fix stored procedures with correct table and column names
-- Run this script to fix the batch operations stored procedures
-- =============================================

USE [TABDB]
GO

-- Drop existing procedures
DROP PROCEDURE IF EXISTS sp_ConsolidateCallLogBatch;
DROP PROCEDURE IF EXISTS sp_DeleteBatch;
GO

-- Recreate sp_ConsolidateCallLogBatch with correct table/column names
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

        -- Import from PSTN (table name: PSTNs)
        INSERT INTO CallLogStagings (BatchId, ImportType, ExtensionNumber, CallDate, CallNumber, CallDestination, CallEndTime, CallDuration, CallCurrencyCode, CallCost, CallCostUSD, CallCostKSHS, CallType, CallDestinationType, CallMonth, CallYear, ResponsibleIndexNumber, UserPhoneId, SourceSystem, SourceRecordId, ImportedBy, ImportedDate, VerificationStatus, ProcessingStatus, CreatedDate)
        SELECT @BatchId, 'Batch', ISNULL(p.Extension, ''), ISNULL(p.CallDate, '1900-01-01'), ISNULL(p.DialedNumber, ''), ISNULL(p.Destination, ''), DATEADD(SECOND, ISNULL(p.Duration, 0) * 60, ISNULL(p.CallDate, '1900-01-01')), ISNULL(p.Duration, 0) * 60, 'KSH', ISNULL(p.AmountKSH, 0), ISNULL(p.AmountKSH, 0) / 150.0, ISNULL(p.AmountKSH, 0), 'Voice', CASE WHEN p.Destination LIKE '254%' OR p.Destination LIKE '0%' THEN 'Domestic' WHEN p.Destination LIKE '+%' AND p.Destination NOT LIKE '+254%' THEN 'International' WHEN p.Destination LIKE '00%' THEN 'International' ELSE 'Unknown' END, CASE WHEN p.CallMonth > 0 THEN p.CallMonth ELSE @StartMonth END, CASE WHEN p.CallYear > 0 THEN p.CallYear ELSE @StartYear END, ISNULL(up.IndexNumber, p.IndexNumber), up.Id, 'PSTN', CAST(p.Id AS NVARCHAR(50)), @CreatedBy, GETUTCDATE(), 0, 0, GETUTCDATE()
        FROM PSTNs p LEFT JOIN UserPhones up ON (up.PhoneNumber = p.Extension OR up.PhoneNumber = REPLACE(p.Extension, '+254', '0')) AND up.IsActive = 1
        WHERE p.CallMonth >= @StartMonth AND p.CallMonth <= @EndMonth AND p.CallYear >= @StartYear AND p.CallYear <= @EndYear AND p.StagingBatchId IS NULL;
        SET @PSTNCount = @@ROWCOUNT;
        UPDATE p SET p.StagingBatchId = @BatchId, p.UserPhoneId = up.Id, p.ProcessingStatus = 0 FROM PSTNs p LEFT JOIN UserPhones up ON (up.PhoneNumber = p.Extension OR up.PhoneNumber = REPLACE(p.Extension, '+254', '0')) AND up.IsActive = 1 WHERE p.CallMonth >= @StartMonth AND p.CallMonth <= @EndMonth AND p.CallYear >= @StartYear AND p.CallYear <= @EndYear AND p.StagingBatchId = @BatchId;

        -- Import from PrivateWire (table name: PrivateWires)
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
GO

-- Recreate sp_DeleteBatch with correct table names
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
            -- Return all columns with error
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
            ROLLBACK TRANSACTION;
            RETURN;
        END
        IF @BatchStatus = 4 BEGIN
            -- Return all columns with error
            SELECT
                0 AS Success,
                @BatchName AS BatchName,
                0 AS StagingRecordsDeleted,
                0 AS SafaricomRecordsReset,
                0 AS AirtelRecordsReset,
                0 AS PSTNRecordsReset,
                0 AS PrivateWireRecordsReset,
                NULL AS DeletedAt,
                'Cannot delete batch - already published' AS Error;
            ROLLBACK TRANSACTION;
            RETURN;
        END
        IF EXISTS (SELECT 1 FROM CallLogStagings WHERE BatchId = @BatchId AND ProcessingStatus = 3) BEGIN
            -- Return all columns with error
            SELECT
                0 AS Success,
                @BatchName AS BatchName,
                0 AS StagingRecordsDeleted,
                0 AS SafaricomRecordsReset,
                0 AS AirtelRecordsReset,
                0 AS PSTNRecordsReset,
                0 AS PrivateWireRecordsReset,
                NULL AS DeletedAt,
                'Cannot delete batch - has production records' AS Error;
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

        -- Return all columns on success (with NULL Error)
        SELECT
            1 AS Success,
            @BatchName AS BatchName,
            @StagingRecordsDeleted AS StagingRecordsDeleted,
            @SafaricomRecordsReset AS SafaricomRecordsReset,
            @AirtelRecordsReset AS AirtelRecordsReset,
            @PSTNRecordsReset AS PSTNRecordsReset,
            @PrivateWireRecordsReset AS PrivateWireRecordsReset,
            GETUTCDATE() AS DeletedAt,
            NULL AS Error;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        SELECT @ErrorMessage = ERROR_MESSAGE(), @ErrorSeverity = ERROR_SEVERITY(), @ErrorState = ERROR_STATE();

        -- Return all columns on exception (with Error populated)
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

        -- Still raise the error for logging
        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
    END CATCH
END
GO

PRINT 'Stored procedures recreated successfully with correct table and column names!';
PRINT '';
PRINT 'Test with:';
PRINT 'DECLARE @BatchId UNIQUEIDENTIFIER = NEWID();';
PRINT 'EXEC sp_ConsolidateCallLogBatch @BatchId, 1, 2025, 1, 2025, ''admin@example.com'';';
GO
