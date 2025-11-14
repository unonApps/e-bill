-- =============================================
-- Stored Procedure: sp_ConsolidateCallLogBatch
-- Description: Efficiently consolidates call logs from multiple sources into staging table
-- Purpose: Handles large datasets (1M+ records) without timeout or memory issues
-- Created: 2025-01-14
-- =============================================

-- Drop procedure if it exists
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
            CreatedDate
        )
        SELECT
            @BatchId,
            'Batch',
            ISNULL(s.Ext, ''),
            ISNULL(s.CallDate, '1900-01-01'),
            ISNULL(s.Dialed, ''),
            ISNULL(s.Dest, ''),
            DATEADD(SECOND, ISNULL(s.Dur, 0) * 60, ISNULL(s.CallDate, '1900-01-01')),
            ISNULL(s.Dur, 0) * 60, -- Convert minutes to seconds
            'KES',
            ISNULL(s.Cost, 0),
            ISNULL(s.Cost, 0) / 150.0, -- Convert KES to USD
            ISNULL(s.Cost, 0),
            ISNULL(s.CallType, 'Voice'),
            CASE
                WHEN s.Dest LIKE '254%' OR s.Dest LIKE '0%' THEN 'Domestic'
                WHEN s.Dest LIKE '+%' AND s.Dest NOT LIKE '+254%' THEN 'International'
                WHEN s.Dest LIKE '00%' THEN 'International'
                ELSE 'Unknown'
            END,
            ISNULL(s.CallMonth, @StartMonth),
            ISNULL(s.CallYear, @StartYear),
            ISNULL(up.IndexNumber, s.IndexNumber),
            up.Id, -- UserPhoneId
            'Safaricom',
            CAST(s.Id AS NVARCHAR(50)),
            @CreatedBy,
            GETUTCDATE(),
            0, -- Pending
            0, -- Staged
            GETUTCDATE()
        FROM Safaricoms s
        LEFT JOIN UserPhones up ON (
            up.PhoneNumber = s.Ext
            OR up.PhoneNumber = REPLACE(s.Ext, '+254', '0')
        ) AND up.IsActive = 1
        WHERE s.CallMonth >= @StartMonth
          AND s.CallMonth <= @EndMonth
          AND s.CallYear >= @StartYear
          AND s.CallYear <= @EndYear
          AND s.StagingBatchId IS NULL;  -- Only process records not already in a batch

        SET @SafaricomCount = @@ROWCOUNT;
        PRINT 'Safaricom records imported: ' + CAST(@SafaricomCount AS NVARCHAR(20));

        -- Update Safaricom source records with BatchId and UserPhoneId
        UPDATE s
        SET s.StagingBatchId = @BatchId,
            s.UserPhoneId = up.Id,
            s.ProcessingStatus = 0 -- Staged
        FROM Safaricoms s
        LEFT JOIN UserPhones up ON (
            up.PhoneNumber = s.Ext
            OR up.PhoneNumber = REPLACE(s.Ext, '+254', '0')
        ) AND up.IsActive = 1
        WHERE s.CallMonth >= @StartMonth
          AND s.CallMonth <= @EndMonth
          AND s.CallYear >= @StartYear
          AND s.CallYear <= @EndYear
          AND s.StagingBatchId = @BatchId;

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
            CreatedDate
        )
        SELECT
            @BatchId,
            'Batch',
            ISNULL(a.Ext, ''),
            ISNULL(a.CallDate, '1900-01-01'),
            ISNULL(a.Dialed, ''),
            ISNULL(a.Dest, ''),
            DATEADD(SECOND, ISNULL(a.Dur, 0) * 60, ISNULL(a.CallDate, '1900-01-01')),
            ISNULL(a.Dur, 0) * 60,
            'KES',
            ISNULL(a.Cost, 0),
            ISNULL(a.Cost, 0) / 150.0,
            ISNULL(a.Cost, 0),
            ISNULL(a.CallType, 'Voice'),
            CASE
                WHEN a.Dest LIKE '254%' OR a.Dest LIKE '0%' THEN 'Domestic'
                WHEN a.Dest LIKE '+%' AND a.Dest NOT LIKE '+254%' THEN 'International'
                WHEN a.Dest LIKE '00%' THEN 'International'
                ELSE 'Unknown'
            END,
            ISNULL(a.CallMonth, @StartMonth),
            ISNULL(a.CallYear, @StartYear),
            ISNULL(up.IndexNumber, a.IndexNumber),
            up.Id,
            'Airtel',
            CAST(a.Id AS NVARCHAR(50)),
            @CreatedBy,
            GETUTCDATE(),
            0, -- Pending
            0, -- Staged
            GETUTCDATE()
        FROM Airtels a
        LEFT JOIN UserPhones up ON (
            up.PhoneNumber = a.Ext
            OR up.PhoneNumber = REPLACE(a.Ext, '+254', '0')
        ) AND up.IsActive = 1
        WHERE a.CallMonth >= @StartMonth
          AND a.CallMonth <= @EndMonth
          AND a.CallYear >= @StartYear
          AND a.CallYear <= @EndYear
          AND a.StagingBatchId IS NULL;

        SET @AirtelCount = @@ROWCOUNT;
        PRINT 'Airtel records imported: ' + CAST(@AirtelCount AS NVARCHAR(20));

        -- Update Airtel source records
        UPDATE a
        SET a.StagingBatchId = @BatchId,
            a.UserPhoneId = up.Id,
            a.ProcessingStatus = 0
        FROM Airtels a
        LEFT JOIN UserPhones up ON (
            up.PhoneNumber = a.Ext
            OR up.PhoneNumber = REPLACE(a.Ext, '+254', '0')
        ) AND up.IsActive = 1
        WHERE a.CallMonth >= @StartMonth
          AND a.CallMonth <= @EndMonth
          AND a.CallYear >= @StartYear
          AND a.CallYear <= @EndYear
          AND a.StagingBatchId = @BatchId;

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
            CreatedDate
        )
        SELECT
            @BatchId,
            'Batch',
            ISNULL(p.Extension, ''),
            ISNULL(p.CallDate, '1900-01-01'),
            ISNULL(p.DialedNumber, ''),
            ISNULL(p.Destination, ''),
            DATEADD(SECOND, ISNULL(p.Duration, 0) * 60, ISNULL(p.CallDate, '1900-01-01')),
            ISNULL(p.Duration, 0) * 60,
            'KSH',
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
          AND p.StagingBatchId = @BatchId;

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
            CreatedDate
        )
        SELECT
            @BatchId,
            'Batch',
            ISNULL(pw.Extension, ''),
            ISNULL(pw.CallDate, '1900-01-01'),
            ISNULL(pw.DialedNumber, ''),
            ISNULL(pw.Destination, ''),
            DATEADD(SECOND, ISNULL(pw.Duration, 0) * 60, ISNULL(pw.CallDate, '1900-01-01')),
            ISNULL(pw.Duration, 0) * 60,
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
          AND pw.StagingBatchId = @BatchId;

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

-- Grant execute permissions (adjust role as needed)
GRANT EXECUTE ON sp_ConsolidateCallLogBatch TO public;
GO

PRINT 'Stored procedure sp_ConsolidateCallLogBatch created successfully!';
PRINT '';
PRINT 'Usage example:';
PRINT 'DECLARE @BatchId UNIQUEIDENTIFIER = NEWID();';
PRINT 'EXEC sp_ConsolidateCallLogBatch @BatchId, 1, 2025, 1, 2025, ''admin@example.com'';';
GO
