-- =============================================
-- Stored Procedure: sp_DeleteBatch
-- Description: Efficiently deletes a call log batch and all related records
-- Purpose: Handles large batches (1M+ records) without timeout or memory issues
-- Created: 2025-01-14
-- =============================================

-- Drop procedure if it exists
IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'sp_DeleteBatch')
    DROP PROCEDURE sp_DeleteBatch;
GO

CREATE PROCEDURE sp_DeleteBatch
    @BatchId UNIQUEIDENTIFIER,
    @DeletedBy NVARCHAR(256),
    @Result NVARCHAR(MAX) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON; -- Automatically rollback on error

    DECLARE @BatchName NVARCHAR(200);
    DECLARE @BatchStatus INT;
    DECLARE @TotalRecords INT;
    DECLARE @StagingRecordsDeleted INT = 0;
    DECLARE @SafaricomRecordsReset INT = 0;
    DECLARE @AirtelRecordsReset INT = 0;
    DECLARE @PSTNRecordsReset INT = 0;
    DECLARE @PrivateWireRecordsReset INT = 0;
    DECLARE @ErrorMessage NVARCHAR(4000);
    DECLARE @ErrorSeverity INT;
    DECLARE @ErrorState INT;

    BEGIN TRY
        BEGIN TRANSACTION;

        -- =============================================
        -- STEP 1: Validate batch exists
        -- =============================================
        SELECT
            @BatchName = BatchName,
            @BatchStatus = BatchStatus,
            @TotalRecords = TotalRecords
        FROM StagingBatches
        WHERE Id = @BatchId;

        IF @BatchName IS NULL
        BEGIN
            SET @Result = JSON_QUERY('{"success": false, "error": "Batch not found"}');
            ROLLBACK TRANSACTION;
            RETURN;
        END

        PRINT 'Found batch: ' + @BatchName;

        -- =============================================
        -- STEP 2: Check if batch can be deleted
        -- =============================================
        -- Cannot delete if already published (BatchStatus.Published = 4)
        IF @BatchStatus = 4
        BEGIN
            SET @Result = JSON_QUERY('{"success": false, "error": "Cannot delete batch - already published to production"}');
            ROLLBACK TRANSACTION;
            RETURN;
        END

        -- Check if any records from this batch are in production (ProcessingStatus.Completed = 2)
        IF EXISTS (SELECT 1 FROM CallLogStagings WHERE BatchId = @BatchId AND ProcessingStatus = 2)
        BEGIN
            SET @Result = JSON_QUERY('{"success": false, "error": "Cannot delete batch - has records in production"}');
            ROLLBACK TRANSACTION;
            RETURN;
        END

        PRINT 'Validation passed. Proceeding with deletion...';

        -- =============================================
        -- STEP 3: Delete all staging records for this batch
        -- =============================================
        PRINT 'Deleting staging records...';

        DELETE FROM CallLogStagings
        WHERE BatchId = @BatchId;

        SET @StagingRecordsDeleted = @@ROWCOUNT;
        PRINT 'Deleted ' + CAST(@StagingRecordsDeleted AS NVARCHAR(20)) + ' staging records';

        -- =============================================
        -- STEP 4: Reset source records (set StagingBatchId = NULL)
        -- =============================================
        PRINT 'Resetting source records...';

        -- Reset Safaricom records
        UPDATE Safaricom
        SET StagingBatchId = NULL,
            ProcessingStatus = 0,  -- Reset to Staged
            ProcessedDate = NULL   -- Clear processed date
        WHERE StagingBatchId = @BatchId;

        SET @SafaricomRecordsReset = @@ROWCOUNT;
        IF @SafaricomRecordsReset > 0
            PRINT 'Reset ' + CAST(@SafaricomRecordsReset AS NVARCHAR(20)) + ' Safaricom records';

        -- Reset Airtel records
        UPDATE Airtel
        SET StagingBatchId = NULL,
            ProcessingStatus = 0,
            ProcessedDate = NULL
        WHERE StagingBatchId = @BatchId;

        SET @AirtelRecordsReset = @@ROWCOUNT;
        IF @AirtelRecordsReset > 0
            PRINT 'Reset ' + CAST(@AirtelRecordsReset AS NVARCHAR(20)) + ' Airtel records';

        -- Reset PSTN records
        UPDATE PSTNs
        SET StagingBatchId = NULL,
            ProcessingStatus = 0,
            ProcessedDate = NULL
        WHERE StagingBatchId = @BatchId;

        SET @PSTNRecordsReset = @@ROWCOUNT;
        IF @PSTNRecordsReset > 0
            PRINT 'Reset ' + CAST(@PSTNRecordsReset AS NVARCHAR(20)) + ' PSTN records';

        -- Reset PrivateWire records
        UPDATE PrivateWires
        SET StagingBatchId = NULL,
            ProcessingStatus = 0,
            ProcessedDate = NULL
        WHERE StagingBatchId = @BatchId;

        SET @PrivateWireRecordsReset = @@ROWCOUNT;
        IF @PrivateWireRecordsReset > 0
            PRINT 'Reset ' + CAST(@PrivateWireRecordsReset AS NVARCHAR(20)) + ' PrivateWire records';

        -- =============================================
        -- STEP 5: Create audit log entry
        -- =============================================
        PRINT 'Creating audit log entry...';

        INSERT INTO AuditLogs (
            EntityType,
            EntityId,
            Action,
            Description,
            OldValues,
            PerformedBy,
            PerformedDate,
            Module,
            IsSuccess,
            AdditionalData
        )
        SELECT
            'StagingBatch',
            CAST(@BatchId AS NVARCHAR(50)),
            'Deleted',
            'Deleted batch ''' + @BatchName + ''' with ' + CAST(@StagingRecordsDeleted AS NVARCHAR(20)) + ' staging records',
            (SELECT
                BatchName,
                BatchStatus,
                TotalRecords,
                VerifiedRecords,
                RejectedRecords,
                RecordsWithAnomalies,
                CreatedDate,
                CreatedBy
             FROM StagingBatches
             WHERE Id = @BatchId
             FOR JSON PATH, WITHOUT_ARRAY_WRAPPER),
            @DeletedBy,
            GETUTCDATE(),
            'CallLogStaging',
            1,
            (SELECT
                @StagingRecordsDeleted AS RecordsDeleted,
                @SafaricomRecordsReset AS SafaricomRecordsReset,
                @AirtelRecordsReset AS AirtelRecordsReset,
                @PSTNRecordsReset AS PSTNRecordsReset,
                @PrivateWireRecordsReset AS PrivateWireRecordsReset
             FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);

        -- =============================================
        -- STEP 6: Delete the batch itself
        -- =============================================
        PRINT 'Deleting batch record...';

        DELETE FROM StagingBatches
        WHERE Id = @BatchId;

        -- =============================================
        -- STEP 7: Prepare success result
        -- =============================================
        SET @Result = (
            SELECT
                1 AS success,
                @BatchName AS batchName,
                @StagingRecordsDeleted AS stagingRecordsDeleted,
                @SafaricomRecordsReset AS safaricomRecordsReset,
                @AirtelRecordsReset AS airtelRecordsReset,
                @PSTNRecordsReset AS pstnRecordsReset,
                @PrivateWireRecordsReset AS privateWireRecordsReset,
                GETUTCDATE() AS deletedAt
            FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
        );

        PRINT '================================================';
        PRINT 'Batch deletion completed successfully!';
        PRINT 'Batch name: ' + @BatchName;
        PRINT 'Staging records deleted: ' + CAST(@StagingRecordsDeleted AS NVARCHAR(20));
        PRINT 'Source records reset: ' + CAST(
            @SafaricomRecordsReset + @AirtelRecordsReset + @PSTNRecordsReset + @PrivateWireRecordsReset
            AS NVARCHAR(20));
        PRINT '================================================';

        COMMIT TRANSACTION;

        -- Return result set for EF Core
        SELECT
            1 AS Success,
            @BatchName AS BatchName,
            @StagingRecordsDeleted AS StagingRecordsDeleted,
            @SafaricomRecordsReset AS SafaricomRecordsReset,
            @AirtelRecordsReset AS AirtelRecordsReset,
            @PSTNRecordsReset AS PSTNRecordsReset,
            @PrivateWireRecordsReset AS PrivateWireRecordsReset,
            GETUTCDATE() AS DeletedAt;

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        SELECT @ErrorMessage = ERROR_MESSAGE(),
               @ErrorSeverity = ERROR_SEVERITY(),
               @ErrorState = ERROR_STATE();

        PRINT 'ERROR: ' + @ErrorMessage;

        SET @Result = (
            SELECT
                0 AS success,
                @ErrorMessage AS error
            FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
        );

        -- Return error result set
        SELECT
            0 AS Success,
            @ErrorMessage AS Error;

        -- Re-throw the error
        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
    END CATCH
END
GO

-- Grant execute permissions
GRANT EXECUTE ON sp_DeleteBatch TO public;
GO

PRINT 'Stored procedure sp_DeleteBatch created successfully!';
PRINT '';
PRINT 'Usage example:';
PRINT 'DECLARE @Result NVARCHAR(MAX);';
PRINT 'EXEC sp_DeleteBatch @BatchId = ''YOUR-BATCH-GUID'', @DeletedBy = ''admin@example.com'', @Result = @Result OUTPUT;';
PRINT 'SELECT @Result AS Result;';
GO
