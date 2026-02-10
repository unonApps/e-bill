using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TAB.Web.Migrations
{
    /// <inheritdoc />
    public partial class FixConsolidateStoredProcHasAnomalies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop the old stored procedure that's missing HasAnomalies column
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_ConsolidateCallLogBatch");

            // Recreate with HasAnomalies included in all INSERT statements
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

        -- =============================================
        -- STEP 1: Import from Safaricom
        -- =============================================
        INSERT INTO CallLogStagings (
            BatchId, ImportType, ExtensionNumber, CallDate, CallNumber, CallDestination,
            CallEndTime, CallDuration, CallCurrencyCode, CallCost, CallCostUSD, CallCostKSHS,
            CallType, CallDestinationType, CallMonth, CallYear, ResponsibleIndexNumber,
            UserPhoneId, SourceSystem, SourceRecordId, ImportedBy, ImportedDate,
            VerificationStatus, ProcessingStatus, IsAdjustment, HasAnomalies, CreatedDate
        )
        SELECT
            @BatchId, 'Batch', ISNULL(s.Ext, ''), ISNULL(s.call_date, '1900-01-01'),
            ISNULL(s.Dialed, ''), ISNULL(s.Dest, ''),
            CASE
                WHEN LOWER(ISNULL(s.Dialed, '')) LIKE 'safaricom%'
                    THEN DATEADD(SECOND, CAST(CASE WHEN ISNULL(s.Durx, 0) > 2147483647 THEN 2147483647 ELSE ISNULL(s.Durx, 0) END AS INT), ISNULL(s.call_date, '1900-01-01'))
                WHEN ISNULL(s.Dialed, '') LIKE '[0-9]%' OR ISNULL(s.Dialed, '') LIKE '+%'
                    THEN DATEADD(SECOND,
                        CAST(CASE
                            WHEN ISNULL(s.Dur, 0) > 0 THEN ISNULL(s.Dur, 0) * 60
                            WHEN ISNULL(s.Durx, 0) > 0 THEN FLOOR(ISNULL(s.Durx, 0)) * 60 + (ISNULL(s.Durx, 0) - FLOOR(ISNULL(s.Durx, 0))) * 100
                            ELSE 0
                        END AS INT),
                        ISNULL(s.call_date, '1900-01-01'))
                ELSE ISNULL(s.call_date, '1900-01-01')
            END,
            CASE
                WHEN LOWER(ISNULL(s.Dialed, '')) LIKE 'safaricom%'
                    THEN CAST(CASE WHEN ISNULL(s.Durx, 0) / 1024.0 > 2147483647 THEN 2147483647 ELSE ISNULL(s.Durx, 0) / 1024.0 END AS INT)
                WHEN ISNULL(s.Dialed, '') LIKE '[0-9]%' OR ISNULL(s.Dialed, '') LIKE '+%'
                    THEN CAST(CASE
                        WHEN ISNULL(s.Dur, 0) > 0 THEN ISNULL(s.Dur, 0) * 60
                        WHEN ISNULL(s.Durx, 0) > 0 THEN FLOOR(ISNULL(s.Durx, 0)) * 60 + (ISNULL(s.Durx, 0) - FLOOR(ISNULL(s.Durx, 0))) * 100
                        ELSE 0
                    END AS INT)
                ELSE CAST(CASE WHEN ISNULL(s.Durx, 0) > 2147483647 THEN 2147483647 ELSE ISNULL(s.Durx, 0) END AS INT)
            END,
            'KES', ISNULL(s.Cost, 0), ISNULL(s.Cost, 0) / 150.0, ISNULL(s.Cost, 0),
            CASE
                WHEN LOWER(ISNULL(s.Dialed, '')) LIKE 'safaricom%' THEN 'Internet Usage'
                WHEN LOWER(ISNULL(s.Dialed, '')) = 'sms' THEN 'SMS'
                WHEN LOWER(ISNULL(s.Dialed, '')) = 'roaming' THEN 'Roaming'
                WHEN LOWER(ISNULL(s.Dialed, '')) = 'rent' THEN 'Rent'
                WHEN LOWER(ISNULL(s.Dialed, '')) = 'mms' THEN 'MMS'
                WHEN LOWER(ISNULL(s.Dialed, '')) LIKE '%bundle%' THEN 'Bundle'
                WHEN ISNULL(s.Dialed, '') LIKE '[0-9]%' OR ISNULL(s.Dialed, '') LIKE '+%' THEN 'Voice'
                ELSE ISNULL(s.call_type, 'Other')
            END,
            CASE
                WHEN s.Dest LIKE '254%' OR s.Dest LIKE '0%' THEN 'Domestic'
                WHEN s.Dest LIKE '+%' AND s.Dest NOT LIKE '+254%' THEN 'International'
                WHEN s.Dest LIKE '00%' THEN 'International'
                ELSE 'Unknown'
            END,
            ISNULL(s.call_month, @StartMonth), ISNULL(s.call_year, @StartYear),
            ISNULL(up.IndexNumber, s.IndexNumber), up.Id,
            'Safaricom', CAST(s.Id AS NVARCHAR(50)), @CreatedBy, GETUTCDATE(),
            0, 0, 0, 0, GETUTCDATE()
        FROM Safaricom s
        LEFT JOIN UserPhones up ON (up.PhoneNumber = s.Ext OR up.PhoneNumber = REPLACE(s.Ext, '+254', '0')) AND up.IsActive = 1
        WHERE s.call_month >= @StartMonth AND s.call_month <= @EndMonth
          AND s.call_year >= @StartYear AND s.call_year <= @EndYear
          AND s.StagingBatchId IS NULL;

        SET @SafaricomCount = @@ROWCOUNT;

        UPDATE s SET s.StagingBatchId = @BatchId, s.UserPhoneId = up.Id, s.ProcessingStatus = 0
        FROM Safaricom s
        LEFT JOIN UserPhones up ON (up.PhoneNumber = s.Ext OR up.PhoneNumber = REPLACE(s.Ext, '+254', '0')) AND up.IsActive = 1
        WHERE s.call_month >= @StartMonth AND s.call_month <= @EndMonth
          AND s.call_year >= @StartYear AND s.call_year <= @EndYear
          AND s.StagingBatchId IS NULL;

        -- =============================================
        -- STEP 2: Import from Airtel
        -- =============================================
        INSERT INTO CallLogStagings (
            BatchId, ImportType, ExtensionNumber, CallDate, CallNumber, CallDestination,
            CallEndTime, CallDuration, CallCurrencyCode, CallCost, CallCostUSD, CallCostKSHS,
            CallType, CallDestinationType, CallMonth, CallYear, ResponsibleIndexNumber,
            UserPhoneId, SourceSystem, SourceRecordId, ImportedBy, ImportedDate,
            VerificationStatus, ProcessingStatus, IsAdjustment, HasAnomalies, CreatedDate
        )
        SELECT
            @BatchId, 'Batch', ISNULL(a.Ext, ''), ISNULL(a.call_date, '1900-01-01'),
            ISNULL(a.Dialed, ''), ISNULL(a.Dest, ''),
            DATEADD(SECOND, CAST(CASE WHEN ISNULL(a.Dur, 0) > 35791394 THEN 2147483647 ELSE ISNULL(a.Dur, 0) * 60 END AS INT), ISNULL(a.call_date, '1900-01-01')),
            CAST(CASE WHEN ISNULL(a.Dur, 0) > 35791394 THEN 2147483647 ELSE ISNULL(a.Dur, 0) * 60 END AS INT),
            'KES', ISNULL(a.Cost, 0), ISNULL(a.Cost, 0) / 150.0, ISNULL(a.Cost, 0),
            ISNULL(a.call_type, 'Voice'),
            CASE
                WHEN a.Dest LIKE '254%' OR a.Dest LIKE '0%' THEN 'Domestic'
                WHEN a.Dest LIKE '+%' AND a.Dest NOT LIKE '+254%' THEN 'International'
                WHEN a.Dest LIKE '00%' THEN 'International'
                ELSE 'Unknown'
            END,
            ISNULL(a.call_month, @StartMonth), ISNULL(a.call_year, @StartYear),
            ISNULL(up.IndexNumber, a.IndexNumber), up.Id,
            'Airtel', CAST(a.Id AS NVARCHAR(50)), @CreatedBy, GETUTCDATE(),
            0, 0, 0, 0, GETUTCDATE()
        FROM Airtel a
        LEFT JOIN UserPhones up ON (up.PhoneNumber = a.Ext OR up.PhoneNumber = REPLACE(a.Ext, '+254', '0')) AND up.IsActive = 1
        WHERE a.call_month >= @StartMonth AND a.call_month <= @EndMonth
          AND a.call_year >= @StartYear AND a.call_year <= @EndYear
          AND a.StagingBatchId IS NULL;

        SET @AirtelCount = @@ROWCOUNT;

        UPDATE a SET a.StagingBatchId = @BatchId, a.UserPhoneId = up.Id, a.ProcessingStatus = 0
        FROM Airtel a
        LEFT JOIN UserPhones up ON (up.PhoneNumber = a.Ext OR up.PhoneNumber = REPLACE(a.Ext, '+254', '0')) AND up.IsActive = 1
        WHERE a.call_month >= @StartMonth AND a.call_month <= @EndMonth
          AND a.call_year >= @StartYear AND a.call_year <= @EndYear
          AND a.StagingBatchId IS NULL;

        -- =============================================
        -- STEP 3: Import from PSTN
        -- =============================================
        INSERT INTO CallLogStagings (
            BatchId, ImportType, ExtensionNumber, CallDate, CallNumber, CallDestination,
            CallEndTime, CallDuration, CallCurrencyCode, CallCost, CallCostUSD, CallCostKSHS,
            CallType, CallDestinationType, CallMonth, CallYear, ResponsibleIndexNumber,
            UserPhoneId, SourceSystem, SourceRecordId, ImportedBy, ImportedDate,
            VerificationStatus, ProcessingStatus, IsAdjustment, HasAnomalies, CreatedDate
        )
        SELECT
            @BatchId, 'Batch', ISNULL(p.Extension, ''), ISNULL(p.CallDate, '1900-01-01'),
            ISNULL(p.DialedNumber, ''), ISNULL(p.Destination, ''),
            DATEADD(SECOND, CAST(CASE WHEN ISNULL(p.Duration, 0) > 35791394 THEN 2147483647 ELSE ISNULL(p.Duration, 0) * 60 END AS INT), ISNULL(p.CallDate, '1900-01-01')),
            CAST(CASE WHEN ISNULL(p.Duration, 0) > 35791394 THEN 2147483647 ELSE ISNULL(p.Duration, 0) * 60 END AS INT),
            'KES', ISNULL(p.AmountKSH, 0), ISNULL(p.AmountKSH, 0) / 150.0, ISNULL(p.AmountKSH, 0),
            'Voice',
            CASE
                WHEN p.Destination LIKE '254%' OR p.Destination LIKE '0%' THEN 'Domestic'
                WHEN p.Destination LIKE '+%' AND p.Destination NOT LIKE '+254%' THEN 'International'
                WHEN p.Destination LIKE '00%' THEN 'International'
                ELSE 'Unknown'
            END,
            CASE WHEN p.CallMonth > 0 THEN p.CallMonth ELSE @StartMonth END,
            CASE WHEN p.CallYear > 0 THEN p.CallYear ELSE @StartYear END,
            ISNULL(up.IndexNumber, p.IndexNumber), up.Id,
            'PSTN', CAST(p.Id AS NVARCHAR(50)), @CreatedBy, GETUTCDATE(),
            0, 0, 0, 0, GETUTCDATE()
        FROM PSTNs p
        LEFT JOIN UserPhones up ON (up.PhoneNumber = p.Extension OR up.PhoneNumber = REPLACE(p.Extension, '+254', '0')) AND up.IsActive = 1
        WHERE p.CallMonth >= @StartMonth AND p.CallMonth <= @EndMonth
          AND p.CallYear >= @StartYear AND p.CallYear <= @EndYear
          AND p.StagingBatchId IS NULL;

        SET @PSTNCount = @@ROWCOUNT;

        UPDATE p SET p.StagingBatchId = @BatchId, p.UserPhoneId = up.Id, p.ProcessingStatus = 0
        FROM PSTNs p
        LEFT JOIN UserPhones up ON (up.PhoneNumber = p.Extension OR up.PhoneNumber = REPLACE(p.Extension, '+254', '0')) AND up.IsActive = 1
        WHERE p.CallMonth >= @StartMonth AND p.CallMonth <= @EndMonth
          AND p.CallYear >= @StartYear AND p.CallYear <= @EndYear
          AND p.StagingBatchId IS NULL;

        -- =============================================
        -- STEP 4: Import from PrivateWire
        -- =============================================
        INSERT INTO CallLogStagings (
            BatchId, ImportType, ExtensionNumber, CallDate, CallNumber, CallDestination,
            CallEndTime, CallDuration, CallCurrencyCode, CallCost, CallCostUSD, CallCostKSHS,
            CallType, CallDestinationType, CallMonth, CallYear, ResponsibleIndexNumber,
            UserPhoneId, SourceSystem, SourceRecordId, ImportedBy, ImportedDate,
            VerificationStatus, ProcessingStatus, IsAdjustment, HasAnomalies, CreatedDate
        )
        SELECT
            @BatchId, 'Batch', ISNULL(pw.Extension, ''), ISNULL(pw.CallDate, '1900-01-01'),
            ISNULL(pw.DialedNumber, ''), ISNULL(pw.Destination, ''),
            DATEADD(SECOND, CAST(CASE WHEN ISNULL(pw.Duration, 0) > 35791394 THEN 2147483647 ELSE ISNULL(pw.Duration, 0) * 60 END AS INT), ISNULL(pw.CallDate, '1900-01-01')),
            CAST(CASE WHEN ISNULL(pw.Duration, 0) > 35791394 THEN 2147483647 ELSE ISNULL(pw.Duration, 0) * 60 END AS INT),
            'USD', ISNULL(pw.AmountKSH, 0), ISNULL(pw.AmountUSD, 0), ISNULL(pw.AmountKSH, 0),
            'Voice',
            CASE
                WHEN pw.Destination LIKE '254%' OR pw.Destination LIKE '0%' THEN 'Domestic'
                WHEN pw.Destination LIKE '+%' AND pw.Destination NOT LIKE '+254%' THEN 'International'
                WHEN pw.Destination LIKE '00%' THEN 'International'
                ELSE 'Unknown'
            END,
            CASE WHEN pw.CallMonth > 0 THEN pw.CallMonth ELSE @StartMonth END,
            CASE WHEN pw.CallYear > 0 THEN pw.CallYear ELSE @StartYear END,
            ISNULL(up.IndexNumber, pw.IndexNumber), up.Id,
            'PrivateWire', CAST(pw.Id AS NVARCHAR(50)), @CreatedBy, GETUTCDATE(),
            0, 0, 0, 0, GETUTCDATE()
        FROM PrivateWires pw
        LEFT JOIN UserPhones up ON (up.PhoneNumber = pw.Extension OR up.PhoneNumber = REPLACE(pw.Extension, '+254', '0')) AND up.IsActive = 1
        WHERE pw.CallMonth >= @StartMonth AND pw.CallMonth <= @EndMonth
          AND pw.CallYear >= @StartYear AND pw.CallYear <= @EndYear
          AND pw.StagingBatchId IS NULL;

        SET @PrivateWireCount = @@ROWCOUNT;

        UPDATE pw SET pw.StagingBatchId = @BatchId, pw.UserPhoneId = up.Id, pw.ProcessingStatus = 0
        FROM PrivateWires pw
        LEFT JOIN UserPhones up ON (up.PhoneNumber = pw.Extension OR up.PhoneNumber = REPLACE(pw.Extension, '+254', '0')) AND up.IsActive = 1
        WHERE pw.CallMonth >= @StartMonth AND pw.CallMonth <= @EndMonth
          AND pw.CallYear >= @StartYear AND pw.CallYear <= @EndYear
          AND pw.StagingBatchId IS NULL;

        -- =============================================
        -- STEP 5: Calculate totals and return results
        -- =============================================
        SET @TotalImported = @SafaricomCount + @AirtelCount + @PSTNCount + @PrivateWireCount;

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

        SELECT @ErrorMessage = ERROR_MESSAGE(), @ErrorSeverity = ERROR_SEVERITY(), @ErrorState = ERROR_STATE();
        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
    END CATCH
END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_ConsolidateCallLogBatch");
        }
    }
}
