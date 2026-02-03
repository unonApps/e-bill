BEGIN TRANSACTION;
GO

CREATE TABLE [ImportJobs] (
    [Id] uniqueidentifier NOT NULL,
    [FileName] nvarchar(500) NOT NULL,
    [FileSize] bigint NOT NULL,
    [CallLogType] nvarchar(50) NOT NULL,
    [BillingMonth] int NOT NULL,
    [BillingYear] int NOT NULL,
    [DateFormat] nvarchar(50) NULL,
    [Status] nvarchar(50) NOT NULL,
    [RecordsProcessed] int NULL,
    [RecordsSuccess] int NULL,
    [RecordsError] int NULL,
    [ErrorMessage] nvarchar(max) NULL,
    [CreatedBy] nvarchar(256) NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [StartedDate] datetime2 NULL,
    [CompletedDate] datetime2 NULL,
    [HangfireJobId] nvarchar(100) NULL,
    [DurationSeconds] int NULL,
    [ProgressPercentage] int NULL,
    [Metadata] nvarchar(max) NULL,
    CONSTRAINT [PK_ImportJobs] PRIMARY KEY ([Id])
);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251114095248_AddImportJobsTable', N'8.0.6');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [StagingBatches] ADD [HangfireJobId] nvarchar(100) NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251118151108_AddHangfireJobIdToStagingBatch', N'8.0.6');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE INDEX [IX_CallLogStagings_BatchId_VerificationStatus] ON [CallLogStagings] ([BatchId], [VerificationStatus]);
GO

CREATE INDEX [IX_CallLogStagings_BatchId_HasAnomalies] ON [CallLogStagings] ([BatchId], [HasAnomalies]);
GO

CREATE INDEX [IX_CallLogStagings_VerificationDate] ON [CallLogStagings] ([VerificationDate]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251118190451_AddCallLogStagingIndexes', N'8.0.6');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [StagingBatches] ADD [CurrentOperation] nvarchar(100) NULL;
GO

ALTER TABLE [StagingBatches] ADD [ProcessingProgress] int NOT NULL DEFAULT 0;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251118200259_AddConsolidationProgressTracking', N'8.0.6');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO


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
                            SET @Result = JSON_QUERY('{"success": false, "error": "Batch not found"}');
                            ROLLBACK TRANSACTION;
                            RETURN;
                        END

                        -- STEP 2: Check if batch can be deleted
                        IF @BatchStatus = 4
                        BEGIN
                            SET @Result = JSON_QUERY('{"success": false, "error": "Cannot delete batch - already published to production"}');
                            ROLLBACK TRANSACTION;
                            RETURN;
                        END

                        IF EXISTS (SELECT 1 FROM CallLogStagings WHERE BatchId = @BatchId AND ProcessingStatus = 3)
                        BEGIN
                            SET @Result = JSON_QUERY('{"success": false, "error": "Cannot delete batch - has records in production"}');
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
            
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251119101704_FixDeleteBatchStoredProcedureTableNames', N'8.0.6');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO


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
                            SET @Result = JSON_QUERY('{"success": false, "error": "Batch not found"}');
                            ROLLBACK TRANSACTION;
                            -- Return with all columns
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
                            RETURN;
                        END

                        -- STEP 2: Check if batch can be deleted
                        IF @BatchStatus = 4
                        BEGIN
                            SET @Result = JSON_QUERY('{"success": false, "error": "Cannot delete batch - already published to production"}');
                            ROLLBACK TRANSACTION;
                            SELECT
                                0 AS Success,
                                @BatchName AS BatchName,
                                0 AS StagingRecordsDeleted,
                                0 AS SafaricomRecordsReset,
                                0 AS AirtelRecordsReset,
                                0 AS PSTNRecordsReset,
                                0 AS PrivateWireRecordsReset,
                                NULL AS DeletedAt,
                                'Cannot delete batch - already published to production' AS Error;
                            RETURN;
                        END

                        IF EXISTS (SELECT 1 FROM CallLogStagings WHERE BatchId = @BatchId AND ProcessingStatus = 3)
                        BEGIN
                            SET @Result = JSON_QUERY('{"success": false, "error": "Cannot delete batch - has records in production"}');
                            ROLLBACK TRANSACTION;
                            SELECT
                                0 AS Success,
                                @BatchName AS BatchName,
                                0 AS StagingRecordsDeleted,
                                0 AS SafaricomRecordsReset,
                                0 AS AirtelRecordsReset,
                                0 AS PSTNRecordsReset,
                                0 AS PrivateWireRecordsReset,
                                NULL AS DeletedAt,
                                'Cannot delete batch - has records in production' AS Error;
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

                        -- Return result set for EF Core with all columns
                        SELECT
                            1 AS Success,
                            @BatchName AS BatchName,
                            @StagingRecordsDeleted AS StagingRecordsDeleted,
                            @SafaricomRecordsReset AS SafaricomRecordsReset,
                            @AirtelRecordsReset AS AirtelRecordsReset,
                            @PSTNRecordsReset AS PSTNRecordsReset,
                            @PrivateWireRecordsReset AS PrivateWireRecordsReset,
                            GETUTCDATE() AS DeletedAt,
                            CAST(NULL AS NVARCHAR(4000)) AS Error;

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

                        -- Return error result set with all columns
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

                        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
                    END CATCH
                END
            
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251119102253_FixDeleteBatchResultColumns', N'8.0.6');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

DECLARE @var0 sysname;
SELECT @var0 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[SimRequests]') AND [c].[name] = N'ServiceProviderId');
IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [SimRequests] DROP CONSTRAINT [' + @var0 + '];');
ALTER TABLE [SimRequests] ALTER COLUMN [ServiceProviderId] int NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251119171057_MakeServiceProviderIdNullable', N'8.0.6');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO


CREATE PROCEDURE sp_VerifyBatch
    @BatchId UNIQUEIDENTIFIER,
    @VerifiedBy NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @RecordsVerified INT = 0;
    DECLARE @ErrorMessage NVARCHAR(4000) = NULL;

    BEGIN TRY
        BEGIN TRANSACTION;

        -- Update all pending and requires review records in one statement
        UPDATE CallLogStagings
        SET
            VerificationStatus = 2, -- Verified
            VerificationDate = GETUTCDATE(),
            VerifiedBy = @VerifiedBy,
            ModifiedDate = GETUTCDATE(),
            ModifiedBy = @VerifiedBy
        WHERE
            BatchId = @BatchId
            AND (VerificationStatus = 0 OR VerificationStatus = 3); -- Pending or RequiresReview

        SET @RecordsVerified = @@ROWCOUNT;

        -- Update batch statistics
        DECLARE @TotalRecords INT, @VerifiedRecords INT, @RejectedRecords INT, @PendingRecords INT, @RecordsWithAnomalies INT;

        SELECT
            @TotalRecords = COUNT(*),
            @VerifiedRecords = SUM(CASE WHEN VerificationStatus = 2 THEN 1 ELSE 0 END),
            @RejectedRecords = SUM(CASE WHEN VerificationStatus = 1 THEN 1 ELSE 0 END),
            @PendingRecords = SUM(CASE WHEN VerificationStatus = 0 OR VerificationStatus = 3 THEN 1 ELSE 0 END),
            @RecordsWithAnomalies = SUM(CASE WHEN HasAnomalies = 1 THEN 1 ELSE 0 END)
        FROM CallLogStagings
        WHERE BatchId = @BatchId;

        UPDATE StagingBatches
        SET
            TotalRecords = @TotalRecords,
            VerifiedRecords = @VerifiedRecords,
            RejectedRecords = @RejectedRecords,
            PendingRecords = @PendingRecords,
            RecordsWithAnomalies = @RecordsWithAnomalies,
            VerifiedBy = @VerifiedBy,
            -- Update status based on verification state
            BatchStatus = CASE
                WHEN @PendingRecords = 0 AND @VerifiedRecords > 0 THEN 3 -- Verified
                WHEN @VerifiedRecords > 0 THEN 2 -- PartiallyVerified
                ELSE BatchStatus
            END
        WHERE Id = @BatchId;

        COMMIT TRANSACTION;

        -- Return result
        SELECT
            1 AS Success,
            @RecordsVerified AS RecordsVerified,
            @TotalRecords AS TotalRecords,
            @VerifiedRecords AS TotalVerified,
            @PendingRecords AS TotalPending,
            NULL AS Error;

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        SET @ErrorMessage = ERROR_MESSAGE();

        SELECT
            0 AS Success,
            0 AS RecordsVerified,
            0 AS TotalRecords,
            0 AS TotalVerified,
            0 AS TotalPending,
            @ErrorMessage AS Error;
    END CATCH
END
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251119203144_AddVerifyBatchStoredProcedure', N'8.0.6');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

DROP PROCEDURE IF EXISTS sp_VerifyBatch
GO


CREATE PROCEDURE sp_VerifyBatch
    @BatchId UNIQUEIDENTIFIER,
    @VerifiedBy NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @RecordsVerified INT = 0;
    DECLARE @ErrorMessage NVARCHAR(4000) = NULL;

    BEGIN TRY
        BEGIN TRANSACTION;

        -- Update all pending and requires review records in one statement
        UPDATE CallLogStagings
        SET
            VerificationStatus = 1, -- Verified (0=Pending, 1=Verified, 2=Rejected, 3=RequiresReview)
            VerificationDate = GETUTCDATE(),
            VerifiedBy = @VerifiedBy,
            ModifiedDate = GETUTCDATE(),
            ModifiedBy = @VerifiedBy
        WHERE
            BatchId = @BatchId
            AND (VerificationStatus = 0 OR VerificationStatus = 3); -- Pending or RequiresReview

        SET @RecordsVerified = @@ROWCOUNT;

        -- Update batch statistics
        DECLARE @TotalRecords INT, @VerifiedRecords INT, @RejectedRecords INT, @PendingRecords INT, @RecordsWithAnomalies INT;

        SELECT
            @TotalRecords = COUNT(*),
            @VerifiedRecords = SUM(CASE WHEN VerificationStatus = 1 THEN 1 ELSE 0 END), -- 1=Verified
            @RejectedRecords = SUM(CASE WHEN VerificationStatus = 2 THEN 1 ELSE 0 END), -- 2=Rejected
            @PendingRecords = SUM(CASE WHEN VerificationStatus = 0 OR VerificationStatus = 3 THEN 1 ELSE 0 END),
            @RecordsWithAnomalies = SUM(CASE WHEN HasAnomalies = 1 THEN 1 ELSE 0 END)
        FROM CallLogStagings
        WHERE BatchId = @BatchId;

        UPDATE StagingBatches
        SET
            TotalRecords = @TotalRecords,
            VerifiedRecords = @VerifiedRecords,
            RejectedRecords = @RejectedRecords,
            PendingRecords = @PendingRecords,
            RecordsWithAnomalies = @RecordsWithAnomalies,
            VerifiedBy = @VerifiedBy,
            -- Update status based on verification state
            -- BatchStatus: 0=Created, 1=Processing, 2=PartiallyVerified, 3=Verified, 4=Published, 5=Failed
            BatchStatus = CASE
                WHEN @PendingRecords = 0 AND @VerifiedRecords > 0 THEN 3 -- Verified
                WHEN @VerifiedRecords > 0 THEN 2 -- PartiallyVerified
                ELSE BatchStatus
            END
        WHERE Id = @BatchId;

        COMMIT TRANSACTION;

        -- Return result
        SELECT
            1 AS Success,
            @RecordsVerified AS RecordsVerified,
            @TotalRecords AS TotalRecords,
            @VerifiedRecords AS TotalVerified,
            @PendingRecords AS TotalPending,
            NULL AS Error;

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        SET @ErrorMessage = ERROR_MESSAGE();

        SELECT
            0 AS Success,
            0 AS RecordsVerified,
            0 AS TotalRecords,
            0 AS TotalVerified,
            0 AS TotalPending,
            @ErrorMessage AS Error;
    END CATCH
END
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251119210633_FixVerifyBatchStoredProcedureEnumValues', N'8.0.6');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

-- =============================================
-- Migration: Fix Duration Handling for Safaricom Voice Calls
-- Description: Updates sp_ConsolidateCallLogBatch to use 'dur' column (minutes)
--              instead of 'durx' column for voice call duration calculation
-- Date: 2026-02-03
-- =============================================

IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'sp_ConsolidateCallLogBatch')
    DROP PROCEDURE sp_ConsolidateCallLogBatch;
GO

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
    SET XACT_ABORT ON; -- Automatically rollback on error

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

        -- =============================================
        -- STEP 1: Import from Safaricom
        -- =============================================
        PRINT 'Starting Safaricom import...';

        INSERT INTO CallLogStagings (
            BatchId,
            ImportType,
            ExtensionNumber,
            CallDate,
            CallNumber,
            CallDestination,
            CallEndTime,
            CallDuration,
            CallCurrencyCode,
            CallCost,
            CallCostUSD,
            CallCostKSHS,
            CallType,
            CallDestinationType,
            CallMonth,
            CallYear,
            ResponsibleIndexNumber,
            UserPhoneId,
            SourceSystem,
            SourceRecordId,
            ImportedBy,
            ImportedDate,
            VerificationStatus,
            ProcessingStatus,
            IsAdjustment,
            CreatedDate
        )
        SELECT
            @BatchId,
            'Batch',
            ISNULL(s.ext, ''),
            ISNULL(s.call_date, '1900-01-01'),
            ISNULL(s.dialed, ''),
            ISNULL(s.dest, ''),
            -- Safaricom CallEndTime:
            -- Internet (dialed starts with 'safaricom'): durx is KB, add as seconds placeholder
            -- Voice (dialed is phone number): Use dur (minutes) if available, fall back to durx (mm.ss)
            -- Other (SMS, ROAMING, RENT, MMS, Bundle): no duration, use call_date as-is
            -- NOTE: Cap calculations to prevent INT overflow (max INT = 2147483647)
            CASE
                WHEN LOWER(ISNULL(s.dialed, '')) LIKE 'safaricom%'
                    THEN DATEADD(SECOND, CAST(CASE WHEN ISNULL(s.durx, 0) > 2147483647 THEN 2147483647 ELSE ISNULL(s.durx, 0) END AS INT), ISNULL(s.call_date, '1900-01-01'))
                WHEN ISNULL(s.dialed, '') LIKE '[0-9]%' OR ISNULL(s.dialed, '') LIKE '+%'
                    -- Voice call: Use dur (minutes) * 60, or fall back to durx (mm.ss) conversion
                    THEN DATEADD(SECOND,
                        CAST(CASE
                            WHEN ISNULL(s.dur, 0) > 0 THEN ISNULL(s.dur, 0) * 60  -- dur is in minutes
                            WHEN ISNULL(s.durx, 0) > 0 THEN FLOOR(ISNULL(s.durx, 0)) * 60 + (ISNULL(s.durx, 0) - FLOOR(ISNULL(s.durx, 0))) * 100
                            ELSE 0
                        END AS INT),
                        ISNULL(s.call_date, '1900-01-01'))
                ELSE ISNULL(s.call_date, '1900-01-01')  -- SMS, ROAMING, RENT, MMS, Bundle - no duration
            END,
            -- Safaricom CallDuration:
            -- Internet: durx is KB, convert to MB (÷ 1024)
            -- Voice: Use dur (minutes) * 60, or fall back to durx (mm.ss) conversion
            -- Other: store as 0 or count
            -- NOTE: Cap calculations to prevent INT overflow
            CASE
                WHEN LOWER(ISNULL(s.dialed, '')) LIKE 'safaricom%'
                    THEN CAST(CASE WHEN ISNULL(s.durx, 0) / 1024.0 > 2147483647 THEN 2147483647 ELSE ISNULL(s.durx, 0) / 1024.0 END AS INT)  -- KB to MB (capped)
                WHEN ISNULL(s.dialed, '') LIKE '[0-9]%' OR ISNULL(s.dialed, '') LIKE '+%'
                    -- Voice: Use dur * 60 (minutes to seconds), or durx (mm.ss) conversion
                    THEN CAST(CASE
                        WHEN ISNULL(s.dur, 0) > 0 THEN ISNULL(s.dur, 0) * 60
                        WHEN ISNULL(s.durx, 0) > 0 THEN FLOOR(ISNULL(s.durx, 0)) * 60 + (ISNULL(s.durx, 0) - FLOOR(ISNULL(s.durx, 0))) * 100
                        ELSE 0
                    END AS INT)
                ELSE CAST(CASE WHEN ISNULL(s.durx, 0) > 2147483647 THEN 2147483647 ELSE ISNULL(s.durx, 0) END AS INT)  -- SMS count, or 0 for others (capped)
            END,
            'KES',
            ISNULL(s.cost, 0),
            ISNULL(s.cost, 0) / 150.0, -- Convert KES to USD
            ISNULL(s.cost, 0),
            -- Set CallType based on dialed value
            CASE
                WHEN LOWER(ISNULL(s.dialed, '')) LIKE 'safaricom%' THEN 'Internet Usage'
                WHEN LOWER(ISNULL(s.dialed, '')) = 'sms' THEN 'SMS'
                WHEN LOWER(ISNULL(s.dialed, '')) = 'roaming' THEN 'Roaming'
                WHEN LOWER(ISNULL(s.dialed, '')) = 'rent' THEN 'Rent'
                WHEN LOWER(ISNULL(s.dialed, '')) = 'mms' THEN 'MMS'
                WHEN LOWER(ISNULL(s.dialed, '')) LIKE '%bundle%' THEN 'Bundle'
                WHEN ISNULL(s.dialed, '') LIKE '[0-9]%' OR ISNULL(s.dialed, '') LIKE '+%' THEN 'Voice'
                ELSE ISNULL(s.call_type, 'Other')
            END,
            CASE
                WHEN s.dest LIKE '254%' OR s.dest LIKE '0%' THEN 'Domestic'
                WHEN s.dest LIKE '+%' AND s.dest NOT LIKE '+254%' THEN 'International'
                WHEN s.dest LIKE '00%' THEN 'International'
                ELSE 'Unknown'
            END,
            ISNULL(s.call_month, @StartMonth),
            ISNULL(s.call_year, @StartYear),
            ISNULL(up.IndexNumber, s.IndexNumber),
            up.Id, -- UserPhoneId
            'Safaricom',
            CAST(s.Id AS NVARCHAR(50)),
            @CreatedBy,
            GETUTCDATE(),
            0, -- Pending
            0, -- Staged
            0, -- IsAdjustment = false
            GETUTCDATE()
        FROM Safaricom s
        LEFT JOIN UserPhones up ON (
            up.PhoneNumber = s.ext
            OR up.PhoneNumber = REPLACE(s.ext, '+254', '0')
        ) AND up.IsActive = 1
        WHERE s.call_month >= @StartMonth
          AND s.call_month <= @EndMonth
          AND s.call_year >= @StartYear
          AND s.call_year <= @EndYear
          AND s.StagingBatchId IS NULL;  -- Only process records not already in a batch

        SET @SafaricomCount = @@ROWCOUNT;
        PRINT 'Safaricom records imported: ' + CAST(@SafaricomCount AS NVARCHAR(20));

        -- Update Safaricom source records with BatchId and UserPhoneId
        UPDATE s
        SET s.StagingBatchId = @BatchId,
            s.UserPhoneId = up.Id,
            s.ProcessingStatus = 0 -- Staged
        FROM Safaricom s
        LEFT JOIN UserPhones up ON (
            up.PhoneNumber = s.ext
            OR up.PhoneNumber = REPLACE(s.ext, '+254', '0')
        ) AND up.IsActive = 1
        WHERE s.call_month >= @StartMonth
          AND s.call_month <= @EndMonth
          AND s.call_year >= @StartYear
          AND s.call_year <= @EndYear
          AND s.StagingBatchId IS NULL;

        -- =============================================
        -- STEP 2: Import from Airtel
        -- =============================================
        PRINT 'Starting Airtel import...';

        INSERT INTO CallLogStagings (
            BatchId,
            ImportType,
            ExtensionNumber,
            CallDate,
            CallNumber,
            CallDestination,
            CallEndTime,
            CallDuration,
            CallCurrencyCode,
            CallCost,
            CallCostUSD,
            CallCostKSHS,
            CallType,
            CallDestinationType,
            CallMonth,
            CallYear,
            ResponsibleIndexNumber,
            UserPhoneId,
            SourceSystem,
            SourceRecordId,
            ImportedBy,
            ImportedDate,
            VerificationStatus,
            ProcessingStatus,
            IsAdjustment,
            CreatedDate
        )
        SELECT
            @BatchId,
            'Batch',
            ISNULL(a.ext, ''),
            ISNULL(a.call_date, '1900-01-01'),
            ISNULL(a.dialed, ''),
            ISNULL(a.dest, ''),
            -- Airtel: dur is in minutes, convert to seconds (cap at max INT to prevent overflow)
            DATEADD(SECOND, CAST(CASE WHEN ISNULL(a.dur, 0) > 35791394 THEN 2147483647 ELSE ISNULL(a.dur, 0) * 60 END AS INT), ISNULL(a.call_date, '1900-01-01')),
            CAST(CASE WHEN ISNULL(a.dur, 0) > 35791394 THEN 2147483647 ELSE ISNULL(a.dur, 0) * 60 END AS INT),  -- Convert minutes to seconds (capped)
            'KES',
            ISNULL(a.cost, 0),
            ISNULL(a.cost, 0) / 150.0,
            ISNULL(a.cost, 0),
            ISNULL(a.call_type, 'Voice'),
            CASE
                WHEN a.dest LIKE '254%' OR a.dest LIKE '0%' THEN 'Domestic'
                WHEN a.dest LIKE '+%' AND a.dest NOT LIKE '+254%' THEN 'International'
                WHEN a.dest LIKE '00%' THEN 'International'
                ELSE 'Unknown'
            END,
            ISNULL(a.call_month, @StartMonth),
            ISNULL(a.call_year, @StartYear),
            ISNULL(up.IndexNumber, a.IndexNumber),
            up.Id,
            'Airtel',
            CAST(a.Id AS NVARCHAR(50)),
            @CreatedBy,
            GETUTCDATE(),
            0, -- Pending
            0, -- Staged
            0, -- IsAdjustment = false
            GETUTCDATE()
        FROM Airtel a
        LEFT JOIN UserPhones up ON (
            up.PhoneNumber = a.ext
            OR up.PhoneNumber = REPLACE(a.ext, '+254', '0')
        ) AND up.IsActive = 1
        WHERE a.call_month >= @StartMonth
          AND a.call_month <= @EndMonth
          AND a.call_year >= @StartYear
          AND a.call_year <= @EndYear
          AND a.StagingBatchId IS NULL;

        SET @AirtelCount = @@ROWCOUNT;
        PRINT 'Airtel records imported: ' + CAST(@AirtelCount AS NVARCHAR(20));

        -- Update Airtel source records
        UPDATE a
        SET a.StagingBatchId = @BatchId,
            a.UserPhoneId = up.Id,
            a.ProcessingStatus = 0
        FROM Airtel a
        LEFT JOIN UserPhones up ON (
            up.PhoneNumber = a.ext
            OR up.PhoneNumber = REPLACE(a.ext, '+254', '0')
        ) AND up.IsActive = 1
        WHERE a.call_month >= @StartMonth
          AND a.call_month <= @EndMonth
          AND a.call_year >= @StartYear
          AND a.call_year <= @EndYear
          AND a.StagingBatchId IS NULL;

        -- =============================================
        -- STEP 3: Import from PSTN
        -- =============================================
        PRINT 'Starting PSTN import...';

        INSERT INTO CallLogStagings (
            BatchId,
            ImportType,
            ExtensionNumber,
            CallDate,
            CallNumber,
            CallDestination,
            CallEndTime,
            CallDuration,
            CallCurrencyCode,
            CallCost,
            CallCostUSD,
            CallCostKSHS,
            CallType,
            CallDestinationType,
            CallMonth,
            CallYear,
            ResponsibleIndexNumber,
            UserPhoneId,
            SourceSystem,
            SourceRecordId,
            ImportedBy,
            ImportedDate,
            VerificationStatus,
            ProcessingStatus,
            IsAdjustment,
            CreatedDate
        )
        SELECT
            @BatchId,
            'Batch',
            ISNULL(p.Extension, ''),
            ISNULL(p.CallDate, '1900-01-01'),
            ISNULL(p.DialedNumber, ''),
            ISNULL(p.Destination, ''),
            -- PSTN: Duration is in minutes, convert to seconds (cap at max INT to prevent overflow)
            DATEADD(SECOND, CAST(CASE WHEN ISNULL(p.Duration, 0) > 35791394 THEN 2147483647 ELSE ISNULL(p.Duration, 0) * 60 END AS INT), ISNULL(p.CallDate, '1900-01-01')),
            CAST(CASE WHEN ISNULL(p.Duration, 0) > 35791394 THEN 2147483647 ELSE ISNULL(p.Duration, 0) * 60 END AS INT),
            'KES',
            ISNULL(p.AmountKSH, 0),
            ISNULL(p.AmountKSH, 0) / 150.0,
            ISNULL(p.AmountKSH, 0),
            'Voice',
            CASE
                WHEN p.Destination LIKE '254%' OR p.Destination LIKE '0%' THEN 'Domestic'
                WHEN p.Destination LIKE '+%' AND p.Destination NOT LIKE '+254%' THEN 'International'
                WHEN p.Destination LIKE '00%' THEN 'International'
                ELSE 'Unknown'
            END,
            CASE WHEN p.CallMonth > 0 THEN p.CallMonth ELSE @StartMonth END,
            CASE WHEN p.CallYear > 0 THEN p.CallYear ELSE @StartYear END,
            ISNULL(up.IndexNumber, p.IndexNumber),
            up.Id,
            'PSTN',
            CAST(p.Id AS NVARCHAR(50)),
            @CreatedBy,
            GETUTCDATE(),
            0, -- Pending
            0, -- Staged
            0, -- IsAdjustment = false
            GETUTCDATE()
        FROM PSTNs p
        LEFT JOIN UserPhones up ON (
            up.PhoneNumber = p.Extension
            OR up.PhoneNumber = REPLACE(p.Extension, '+254', '0')
        ) AND up.IsActive = 1
        WHERE p.CallMonth >= @StartMonth
          AND p.CallMonth <= @EndMonth
          AND p.CallYear >= @StartYear
          AND p.CallYear <= @EndYear
          AND p.StagingBatchId IS NULL;

        SET @PSTNCount = @@ROWCOUNT;
        PRINT 'PSTN records imported: ' + CAST(@PSTNCount AS NVARCHAR(20));

        -- Update PSTN source records
        UPDATE p
        SET p.StagingBatchId = @BatchId,
            p.UserPhoneId = up.Id,
            p.ProcessingStatus = 0
        FROM PSTNs p
        LEFT JOIN UserPhones up ON (
            up.PhoneNumber = p.Extension
            OR up.PhoneNumber = REPLACE(p.Extension, '+254', '0')
        ) AND up.IsActive = 1
        WHERE p.CallMonth >= @StartMonth
          AND p.CallMonth <= @EndMonth
          AND p.CallYear >= @StartYear
          AND p.CallYear <= @EndYear
          AND p.StagingBatchId IS NULL;

        -- =============================================
        -- STEP 4: Import from PrivateWire
        -- =============================================
        PRINT 'Starting PrivateWire import...';

        INSERT INTO CallLogStagings (
            BatchId,
            ImportType,
            ExtensionNumber,
            CallDate,
            CallNumber,
            CallDestination,
            CallEndTime,
            CallDuration,
            CallCurrencyCode,
            CallCost,
            CallCostUSD,
            CallCostKSHS,
            CallType,
            CallDestinationType,
            CallMonth,
            CallYear,
            ResponsibleIndexNumber,
            UserPhoneId,
            SourceSystem,
            SourceRecordId,
            ImportedBy,
            ImportedDate,
            VerificationStatus,
            ProcessingStatus,
            IsAdjustment,
            CreatedDate
        )
        SELECT
            @BatchId,
            'Batch',
            ISNULL(pw.Extension, ''),
            ISNULL(pw.CallDate, '1900-01-01'),
            ISNULL(pw.DialedNumber, ''),
            ISNULL(pw.Destination, ''),
            -- PrivateWire: Duration is in minutes, convert to seconds (cap at max INT to prevent overflow)
            DATEADD(SECOND, CAST(CASE WHEN ISNULL(pw.Duration, 0) > 35791394 THEN 2147483647 ELSE ISNULL(pw.Duration, 0) * 60 END AS INT), ISNULL(pw.CallDate, '1900-01-01')),
            CAST(CASE WHEN ISNULL(pw.Duration, 0) > 35791394 THEN 2147483647 ELSE ISNULL(pw.Duration, 0) * 60 END AS INT),
            'USD',
            ISNULL(pw.AmountKSH, 0),
            ISNULL(pw.AmountUSD, 0),
            ISNULL(pw.AmountKSH, 0),
            'Voice',
            CASE
                WHEN pw.Destination LIKE '254%' OR pw.Destination LIKE '0%' THEN 'Domestic'
                WHEN pw.Destination LIKE '+%' AND pw.Destination NOT LIKE '+254%' THEN 'International'
                WHEN pw.Destination LIKE '00%' THEN 'International'
                ELSE 'Unknown'
            END,
            CASE WHEN pw.CallMonth > 0 THEN pw.CallMonth ELSE @StartMonth END,
            CASE WHEN pw.CallYear > 0 THEN pw.CallYear ELSE @StartYear END,
            ISNULL(up.IndexNumber, pw.IndexNumber),
            up.Id,
            'PrivateWire',
            CAST(pw.Id AS NVARCHAR(50)),
            @CreatedBy,
            GETUTCDATE(),
            0, -- Pending
            0, -- Staged
            0, -- IsAdjustment = false
            GETUTCDATE()
        FROM PrivateWires pw
        LEFT JOIN UserPhones up ON (
            up.PhoneNumber = pw.Extension
            OR up.PhoneNumber = REPLACE(pw.Extension, '+254', '0')
        ) AND up.IsActive = 1
        WHERE pw.CallMonth >= @StartMonth
          AND pw.CallMonth <= @EndMonth
          AND pw.CallYear >= @StartYear
          AND pw.CallYear <= @EndYear
          AND pw.StagingBatchId IS NULL;

        SET @PrivateWireCount = @@ROWCOUNT;
        PRINT 'PrivateWire records imported: ' + CAST(@PrivateWireCount AS NVARCHAR(20));

        -- Update PrivateWire source records
        UPDATE pw
        SET pw.StagingBatchId = @BatchId,
            pw.UserPhoneId = up.Id,
            pw.ProcessingStatus = 0
        FROM PrivateWires pw
        LEFT JOIN UserPhones up ON (
            up.PhoneNumber = pw.Extension
            OR up.PhoneNumber = REPLACE(pw.Extension, '+254', '0')
        ) AND up.IsActive = 1
        WHERE pw.CallMonth >= @StartMonth
          AND pw.CallMonth <= @EndMonth
          AND pw.CallYear >= @StartYear
          AND pw.CallYear <= @EndYear
          AND pw.StagingBatchId IS NULL;

        -- =============================================
        -- STEP 5: Calculate totals and return results
        -- =============================================
        SET @TotalImported = @SafaricomCount + @AirtelCount + @PSTNCount + @PrivateWireCount;

        PRINT '================================================';
        PRINT 'Consolidation completed successfully!';
        PRINT 'Total records imported: ' + CAST(@TotalImported AS NVARCHAR(20));
        PRINT '  - Safaricom: ' + CAST(@SafaricomCount AS NVARCHAR(20));
        PRINT '  - Airtel: ' + CAST(@AirtelCount AS NVARCHAR(20));
        PRINT '  - PSTN: ' + CAST(@PSTNCount AS NVARCHAR(20));
        PRINT '  - PrivateWire: ' + CAST(@PrivateWireCount AS NVARCHAR(20));
        PRINT '================================================';

        -- Return results as a result set
        SELECT
            @TotalImported AS TotalRecords,
            @SafaricomCount AS SafaricomRecords,
            @AirtelCount AS AirtelRecords,
            @PSTNCount AS PSTNRecords,
            @PrivateWireCount AS PrivateWireRecords,
            GETUTCDATE() AS CompletedAt;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        SELECT @ErrorMessage = ERROR_MESSAGE(),
               @ErrorSeverity = ERROR_SEVERITY(),
               @ErrorState = ERROR_STATE();

        PRINT 'ERROR: ' + @ErrorMessage;

        -- Re-throw the error
        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
    END CATCH
END
GO

GRANT EXECUTE ON sp_ConsolidateCallLogBatch TO public;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260203120000_FixConsolidateBatchDurationHandling', N'8.0.6');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

-- =============================================
-- Migration: Update sp_DeleteBatch stored procedure
-- Description: Efficiently deletes a call log batch and all related records
-- Date: 2026-02-03
-- =============================================

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
            SET @Result = JSON_QUERY('{"success": false, "error": "Batch not found"}');
            ROLLBACK TRANSACTION;
            RETURN;
        END

        PRINT 'Found batch: ' + @BatchName;

        -- STEP 2: Check if batch can be deleted
        IF @BatchStatus = 4
        BEGIN
            SET @Result = JSON_QUERY('{"success": false, "error": "Cannot delete batch - already published to production"}');
            ROLLBACK TRANSACTION;
            RETURN;
        END

        IF EXISTS (SELECT 1 FROM CallLogStagings WHERE BatchId = @BatchId AND ProcessingStatus = 2)
        BEGIN
            SET @Result = JSON_QUERY('{"success": false, "error": "Cannot delete batch - has records in production"}');
            ROLLBACK TRANSACTION;
            RETURN;
        END

        PRINT 'Validation passed. Proceeding with deletion...';

        -- STEP 3: Delete all staging records for this batch
        PRINT 'Deleting staging records...';

        DELETE FROM CallLogStagings
        WHERE BatchId = @BatchId;

        SET @StagingRecordsDeleted = @@ROWCOUNT;
        PRINT 'Deleted ' + CAST(@StagingRecordsDeleted AS NVARCHAR(20)) + ' staging records';

        -- STEP 4: Reset source records (set StagingBatchId = NULL)
        PRINT 'Resetting source records...';

        UPDATE Safaricom
        SET StagingBatchId = NULL,
            ProcessingStatus = 0,
            ProcessedDate = NULL
        WHERE StagingBatchId = @BatchId;

        SET @SafaricomRecordsReset = @@ROWCOUNT;
        IF @SafaricomRecordsReset > 0
            PRINT 'Reset ' + CAST(@SafaricomRecordsReset AS NVARCHAR(20)) + ' Safaricom records';

        UPDATE Airtel
        SET StagingBatchId = NULL,
            ProcessingStatus = 0,
            ProcessedDate = NULL
        WHERE StagingBatchId = @BatchId;

        SET @AirtelRecordsReset = @@ROWCOUNT;
        IF @AirtelRecordsReset > 0
            PRINT 'Reset ' + CAST(@AirtelRecordsReset AS NVARCHAR(20)) + ' Airtel records';

        UPDATE PSTNs
        SET StagingBatchId = NULL,
            ProcessingStatus = 0,
            ProcessedDate = NULL
        WHERE StagingBatchId = @BatchId;

        SET @PSTNRecordsReset = @@ROWCOUNT;
        IF @PSTNRecordsReset > 0
            PRINT 'Reset ' + CAST(@PSTNRecordsReset AS NVARCHAR(20)) + ' PSTN records';

        UPDATE PrivateWires
        SET StagingBatchId = NULL,
            ProcessingStatus = 0,
            ProcessedDate = NULL
        WHERE StagingBatchId = @BatchId;

        SET @PrivateWireRecordsReset = @@ROWCOUNT;
        IF @PrivateWireRecordsReset > 0
            PRINT 'Reset ' + CAST(@PrivateWireRecordsReset AS NVARCHAR(20)) + ' PrivateWire records';

        -- STEP 5: Create audit log entry
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

        -- STEP 6: Delete the batch itself
        PRINT 'Deleting batch record...';

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

        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
    END CATCH
END
GO

GRANT EXECUTE ON sp_DeleteBatch TO public;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260203120001_UpdateDeleteBatchStoredProcedure', N'8.0.6');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

-- =============================================
-- Migration: Update sp_PushBatchToProduction stored procedure
-- Description: Efficiently pushes verified call log staging records to production
-- Date: 2026-02-03
-- =============================================

IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'sp_PushBatchToProduction')
    DROP PROCEDURE sp_PushBatchToProduction;
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
            'None', -- assignment_status
            0, -- call_ver_ind
            NULL, -- call_ver_date
            @VerificationType, -- verification_type
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

        -- STEP 2: Update CallLogStagings records as Completed
        UPDATE CallLogStagings
        SET ProcessingStatus = 2, -- Completed
            ProcessedDate = @CurrentDateTime
        WHERE BatchId = @BatchId AND VerificationStatus = 1;

        SET @StagingUpdated = @@ROWCOUNT;

        -- STEP 3: Update source tables
        UPDATE s
        SET s.ProcessingStatus = 2, -- Completed
            s.ProcessedDate = @CurrentDateTime
        FROM Safaricom s
        INNER JOIN CallLogStagings cls ON cls.SourceRecordId = CAST(s.Id AS NVARCHAR(50))
        WHERE cls.BatchId = @BatchId
          AND cls.VerificationStatus = 1
          AND cls.SourceSystem = 'Safaricom';

        SET @SafaricomUpdated = @@ROWCOUNT;

        UPDATE a
        SET a.ProcessingStatus = 2, -- Completed
            a.ProcessedDate = @CurrentDateTime
        FROM Airtel a
        INNER JOIN CallLogStagings cls ON cls.SourceRecordId = CAST(a.Id AS NVARCHAR(50))
        WHERE cls.BatchId = @BatchId
          AND cls.VerificationStatus = 1
          AND cls.SourceSystem = 'Airtel';

        SET @AirtelUpdated = @@ROWCOUNT;

        UPDATE p
        SET p.ProcessingStatus = 2, -- Completed
            p.ProcessedDate = @CurrentDateTime
        FROM PSTNs p
        INNER JOIN CallLogStagings cls ON cls.SourceRecordId = CAST(p.Id AS NVARCHAR(50))
        WHERE cls.BatchId = @BatchId
          AND cls.VerificationStatus = 1
          AND cls.SourceSystem = 'PSTN';

        SET @PSTNUpdated = @@ROWCOUNT;

        UPDATE pw
        SET pw.ProcessingStatus = 2, -- Completed
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
          AND VerificationStatus != 1 -- Not Verified
          AND ProcessingStatus != 2; -- Not Completed

        -- STEP 5: Update batch status
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

        -- Return error result
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

GRANT EXECUTE ON sp_PushBatchToProduction TO public;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260203120002_UpdatePushBatchToProductionStoredProcedure', N'8.0.6');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

-- =============================================
-- Migration: Update sp_VerifyBatch stored procedure
-- Description: Efficiently verifies all pending records in a batch
-- Date: 2026-02-03
-- =============================================

IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'sp_VerifyBatch')
    DROP PROCEDURE sp_VerifyBatch;
GO

CREATE PROCEDURE sp_VerifyBatch
    @BatchId UNIQUEIDENTIFIER,
    @VerifiedBy NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @RecordsVerified INT = 0;
    DECLARE @ErrorMessage NVARCHAR(4000) = NULL;

    BEGIN TRY
        BEGIN TRANSACTION;

        -- Update all pending and requires review records in one statement
        UPDATE CallLogStagings
        SET
            VerificationStatus = 1, -- Verified (0=Pending, 1=Verified, 2=Rejected, 3=RequiresReview)
            VerificationDate = GETUTCDATE(),
            VerifiedBy = @VerifiedBy,
            ModifiedDate = GETUTCDATE(),
            ModifiedBy = @VerifiedBy
        WHERE
            BatchId = @BatchId
            AND (VerificationStatus = 0 OR VerificationStatus = 3); -- Pending or RequiresReview

        SET @RecordsVerified = @@ROWCOUNT;

        -- Update batch statistics
        DECLARE @TotalRecords INT, @VerifiedRecords INT, @RejectedRecords INT, @PendingRecords INT, @RecordsWithAnomalies INT;

        SELECT
            @TotalRecords = COUNT(*),
            @VerifiedRecords = SUM(CASE WHEN VerificationStatus = 1 THEN 1 ELSE 0 END), -- 1=Verified
            @RejectedRecords = SUM(CASE WHEN VerificationStatus = 2 THEN 1 ELSE 0 END), -- 2=Rejected
            @PendingRecords = SUM(CASE WHEN VerificationStatus = 0 OR VerificationStatus = 3 THEN 1 ELSE 0 END),
            @RecordsWithAnomalies = SUM(CASE WHEN HasAnomalies = 1 THEN 1 ELSE 0 END)
        FROM CallLogStagings
        WHERE BatchId = @BatchId;

        UPDATE StagingBatches
        SET
            TotalRecords = @TotalRecords,
            VerifiedRecords = @VerifiedRecords,
            RejectedRecords = @RejectedRecords,
            PendingRecords = @PendingRecords,
            RecordsWithAnomalies = @RecordsWithAnomalies,
            VerifiedBy = @VerifiedBy,
            -- Update status based on verification state
            BatchStatus = CASE
                WHEN @PendingRecords = 0 AND @VerifiedRecords > 0 THEN 3 -- Verified
                WHEN @VerifiedRecords > 0 THEN 2 -- PartiallyVerified
                ELSE BatchStatus
            END
        WHERE Id = @BatchId;

        COMMIT TRANSACTION;

        -- Return result
        SELECT
            1 AS Success,
            @RecordsVerified AS RecordsVerified,
            @TotalRecords AS TotalRecords,
            @VerifiedRecords AS TotalVerified,
            @PendingRecords AS TotalPending,
            NULL AS Error;

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        SET @ErrorMessage = ERROR_MESSAGE();

        SELECT
            0 AS Success,
            0 AS RecordsVerified,
            0 AS TotalRecords,
            0 AS TotalVerified,
            0 AS TotalPending,
            @ErrorMessage AS Error;
    END CATCH
END
GO

GRANT EXECUTE ON sp_VerifyBatch TO public;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260203120003_UpdateVerifyBatchStoredProcedure', N'8.0.6');
GO

COMMIT;
GO

