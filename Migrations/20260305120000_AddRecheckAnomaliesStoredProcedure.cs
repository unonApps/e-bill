using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TAB.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddRecheckAnomaliesStoredProcedure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE OR ALTER PROCEDURE ebill.sp_RecheckAnomalies
    @BatchId UNIQUEIDENTIFIER,
    @VerifiedBy NVARCHAR(256) = 'System'
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @OriginalAnomalyCount INT;
    DECLARE @RemainingAnomalyCount INT;
    DECLARE @VerifiedCount INT;
    DECLARE @CurrentDateTime DATETIME = GETUTCDATE();
    DECLARE @VerifiedByAuto NVARCHAR(260) = @VerifiedBy + ' (Auto)';

    -- Count original anomalies
    SELECT @OriginalAnomalyCount = COUNT(*)
    FROM CallLogStagings
    WHERE BatchId = @BatchId AND HasAnomalies = 1;

    -- Step 1a: Match UserPhoneId - exact match
    UPDATE cls
    SET cls.UserPhoneId = up.Id,
        cls.ResponsibleIndexNumber = ISNULL(cls.ResponsibleIndexNumber, up.IndexNumber)
    FROM CallLogStagings cls
    INNER JOIN UserPhones up ON up.PhoneNumber = cls.ExtensionNumber AND up.IsActive = 1
    WHERE cls.BatchId = @BatchId
      AND cls.UserPhoneId IS NULL;

    -- Step 1b: Match UserPhoneId - +254 to 0 format
    UPDATE cls
    SET cls.UserPhoneId = up.Id,
        cls.ResponsibleIndexNumber = ISNULL(cls.ResponsibleIndexNumber, up.IndexNumber)
    FROM CallLogStagings cls
    INNER JOIN UserPhones up ON up.PhoneNumber = REPLACE(cls.ExtensionNumber, '+254', '0') AND up.IsActive = 1
    WHERE cls.BatchId = @BatchId
      AND cls.UserPhoneId IS NULL
      AND cls.ExtensionNumber LIKE '+254%';

    -- Step 1c: Match UserPhoneId - 0 to +254 format
    UPDATE cls
    SET cls.UserPhoneId = up.Id,
        cls.ResponsibleIndexNumber = ISNULL(cls.ResponsibleIndexNumber, up.IndexNumber)
    FROM CallLogStagings cls
    INNER JOIN UserPhones up ON up.PhoneNumber = REPLACE(cls.ExtensionNumber, '0', '+254') AND up.IsActive = 1
    WHERE cls.BatchId = @BatchId
      AND cls.UserPhoneId IS NULL
      AND cls.ExtensionNumber LIKE '07%'
      AND LEN(cls.ExtensionNumber) = 10;

    -- Step 1d: Fill missing ResponsibleIndexNumber from matched phone
    UPDATE cls
    SET cls.ResponsibleIndexNumber = up.IndexNumber
    FROM CallLogStagings cls
    INNER JOIN UserPhones up ON cls.UserPhoneId = up.Id
    WHERE cls.BatchId = @BatchId
      AND (cls.ResponsibleIndexNumber IS NULL OR cls.ResponsibleIndexNumber = '')
      AND up.IndexNumber IS NOT NULL
      AND up.IndexNumber != '';

    -- Step 1e: Update ResponsibleIndexNumber if phone was reassigned
    UPDATE cls
    SET cls.ResponsibleIndexNumber = up.IndexNumber
    FROM CallLogStagings cls
    INNER JOIN UserPhones up ON cls.UserPhoneId = up.Id
    WHERE cls.BatchId = @BatchId
      AND cls.HasAnomalies = 1
      AND up.IndexNumber IS NOT NULL
      AND up.IndexNumber != ''
      AND (cls.ResponsibleIndexNumber IS NULL OR cls.ResponsibleIndexNumber != up.IndexNumber);

    -- Step 2: Re-run anomaly detection
    UPDATE cls
    SET
        HasAnomalies = CASE
            WHEN cls.ResponsibleIndexNumber IS NULL OR cls.ResponsibleIndexNumber = '' THEN 1
            WHEN cls.UserPhoneId IS NULL THEN 1
            WHEN u.IsActive = 0 THEN 1
            ELSE 0
        END,
        AnomalyTypes = CASE
            WHEN cls.ResponsibleIndexNumber IS NULL OR cls.ResponsibleIndexNumber = '' THEN '[""MISSING_USER""]'
            WHEN cls.UserPhoneId IS NULL THEN '[""UNASSIGNED_PHONE""]'
            WHEN u.IsActive = 0 THEN '[""INACTIVE_USER""]'
            ELSE NULL
        END,
        AnomalyDetails = CASE
            WHEN cls.ResponsibleIndexNumber IS NULL OR cls.ResponsibleIndexNumber = '' THEN '{""MISSING_USER"":""No responsible user assigned""}'
            WHEN cls.UserPhoneId IS NULL THEN '{""UNASSIGNED_PHONE"":""Phone number not assigned to any user""}'
            WHEN u.IsActive = 0 THEN '{""INACTIVE_USER"":""User is inactive""}'
            ELSE NULL
        END
    FROM CallLogStagings cls
    LEFT JOIN EbillUsers u ON cls.ResponsibleIndexNumber = u.IndexNumber
    WHERE cls.BatchId = @BatchId;

    -- Step 3: Auto-verify clean records (no anomalies, still pending, not already pushed)
    UPDATE CallLogStagings
    SET VerificationStatus = 1,
        VerificationDate = @CurrentDateTime,
        VerifiedBy = @VerifiedByAuto,
        VerificationNotes = 'Auto-verified: No anomalies detected',
        ModifiedDate = @CurrentDateTime,
        ModifiedBy = @VerifiedByAuto
    WHERE BatchId = @BatchId
      AND HasAnomalies = 0
      AND VerificationStatus = 0
      AND ProcessingStatus != 2;

    SET @VerifiedCount = @@ROWCOUNT;

    -- Count remaining anomalies
    SELECT @RemainingAnomalyCount = COUNT(*)
    FROM CallLogStagings
    WHERE BatchId = @BatchId AND HasAnomalies = 1;

    -- Update batch statistics
    UPDATE StagingBatches
    SET RecordsWithAnomalies = @RemainingAnomalyCount,
        VerifiedRecords = (
            SELECT COUNT(*) FROM CallLogStagings
            WHERE BatchId = @BatchId AND VerificationStatus = 1
        )
    WHERE Id = @BatchId;

    -- Return results
    SELECT
        @OriginalAnomalyCount AS OriginalCount,
        @OriginalAnomalyCount - @RemainingAnomalyCount AS ResolvedCount,
        @RemainingAnomalyCount AS RemainingCount,
        @VerifiedCount AS VerifiedCount;
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS ebill.sp_RecheckAnomalies;");
        }
    }
}
