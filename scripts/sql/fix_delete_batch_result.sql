-- =============================================
-- Fix sp_DeleteBatch to return all columns in result set
-- =============================================

USE [TABDB]
GO

DROP PROCEDURE IF EXISTS sp_DeleteBatch;
GO

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

PRINT 'sp_DeleteBatch fixed to return all columns in all cases';
GO
