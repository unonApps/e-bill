using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TAB.Web.Migrations
{
    /// <inheritdoc />
    public partial class FixDeleteBatchStoredProcedureTableNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop and recreate sp_DeleteBatch with correct table names
            // Fix: Safaricoms -> Safaricom, Airtels -> Airtel
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'sp_DeleteBatch')
                    DROP PROCEDURE sp_DeleteBatch;
            ");

            migrationBuilder.Sql(@"
                CREATE PROCEDURE sp_DeleteBatch
                    @BatchId UNIQUEIDENTIFIER,
                    @DeletedBy NVARCHAR(256),
                    @Result NVARCHAR(MAX) OUTPUT
                AS
                BEGIN
                    SET NOCOUNT ON;
                    SET XACT_ABORT ON;

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

                        -- STEP 1: Validate batch exists
                        SELECT
                            @BatchName = BatchName,
                            @BatchStatus = BatchStatus,
                            @TotalRecords = TotalRecords
                        FROM StagingBatches
                        WHERE Id = @BatchId;

                        IF @BatchName IS NULL
                        BEGIN
                            SET @Result = JSON_QUERY('{""success"": false, ""error"": ""Batch not found""}');
                            ROLLBACK TRANSACTION;
                            RETURN;
                        END

                        -- STEP 2: Check if batch can be deleted
                        IF @BatchStatus = 4
                        BEGIN
                            SET @Result = JSON_QUERY('{""success"": false, ""error"": ""Cannot delete batch - already published to production""}');
                            ROLLBACK TRANSACTION;
                            RETURN;
                        END

                        IF EXISTS (SELECT 1 FROM CallLogStagings WHERE BatchId = @BatchId AND ProcessingStatus = 3)
                        BEGIN
                            SET @Result = JSON_QUERY('{""success"": false, ""error"": ""Cannot delete batch - has records in production""}');
                            ROLLBACK TRANSACTION;
                            RETURN;
                        END

                        -- STEP 3: Delete all staging records for this batch
                        DELETE FROM CallLogStagings
                        WHERE BatchId = @BatchId;

                        SET @StagingRecordsDeleted = @@ROWCOUNT;

                        -- STEP 4: Reset source records (set StagingBatchId = NULL)
                        -- Reset Safaricom records (singular table name)
                        UPDATE Safaricom
                        SET StagingBatchId = NULL,
                            ProcessingStatus = 0,
                            ProcessedDate = NULL
                        WHERE StagingBatchId = @BatchId;

                        SET @SafaricomRecordsReset = @@ROWCOUNT;

                        -- Reset Airtel records (singular table name)
                        UPDATE Airtel
                        SET StagingBatchId = NULL,
                            ProcessingStatus = 0,
                            ProcessedDate = NULL
                        WHERE StagingBatchId = @BatchId;

                        SET @AirtelRecordsReset = @@ROWCOUNT;

                        -- Reset PSTN records (plural table name)
                        UPDATE PSTNs
                        SET StagingBatchId = NULL,
                            ProcessingStatus = 0,
                            ProcessedDate = NULL
                        WHERE StagingBatchId = @BatchId;

                        SET @PSTNRecordsReset = @@ROWCOUNT;

                        -- Reset PrivateWire records (plural table name)
                        UPDATE PrivateWires
                        SET StagingBatchId = NULL,
                            ProcessingStatus = 0,
                            ProcessedDate = NULL
                        WHERE StagingBatchId = @BatchId;

                        SET @PrivateWireRecordsReset = @@ROWCOUNT;

                        -- STEP 5: Create audit log entry
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

                        -- STEP 6: Delete the batch itself
                        DELETE FROM StagingBatches
                        WHERE Id = @BatchId;

                        -- STEP 7: Prepare success result
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

                        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
                    END CATCH
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert to old table names (with typos)
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'sp_DeleteBatch')
                    DROP PROCEDURE sp_DeleteBatch;
            ");
        }
    }
}
