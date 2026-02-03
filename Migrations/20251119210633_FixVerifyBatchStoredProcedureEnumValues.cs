using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TAB.Web.Migrations
{
    /// <inheritdoc />
    public partial class FixVerifyBatchStoredProcedureEnumValues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop and recreate sp_VerifyBatch with correct enum values
            // VerificationStatus: 0=Pending, 1=Verified, 2=Rejected, 3=RequiresReview
            // BatchStatus: 0=Created, 1=Processing, 2=PartiallyVerified, 3=Verified, 4=Published, 5=Failed

            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_VerifyBatch");

            migrationBuilder.Sql(@"
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
END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert to previous (incorrect) version - but this should not be needed
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_VerifyBatch");
        }
    }
}
