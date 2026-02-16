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
            IsAdjustment,
            HasAnomalies,
            CreatedDate
        )
        SELECT
            @BatchId,
            'Batch',
            ISNULL(s.ext, ''),
            ISNULL(s.call_date, '1900-01-01'),
            ISNULL(s.dialed, ''),
            ISNULL(s.dest, ''),
            -- Safaricom CallEndTime: Use call_date + call_time (actual call time from CSV)
            CASE WHEN s.call_date IS NULL OR s.call_date < '1753-01-01'
                THEN '1900-01-01'
                ELSE DATEADD(SECOND, ISNULL(DATEDIFF(SECOND, '00:00:00', s.call_time), 0), CAST(s.call_date AS DATETIME))
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
            0, -- HasAnomalies = false
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
            HasAnomalies,
            CreatedDate
        )
        SELECT
            @BatchId,
            'Batch',
            ISNULL(a.ext, ''),
            ISNULL(a.call_date, '1900-01-01'),
            ISNULL(a.dialed, ''),
            ISNULL(a.dest, ''),
            -- Airtel CallEndTime: Use call_date + call_time (actual call time from CSV)
            CASE WHEN a.call_date IS NULL OR a.call_date < '1753-01-01'
                THEN '1900-01-01'
                ELSE DATEADD(SECOND, ISNULL(DATEDIFF(SECOND, '00:00:00', a.call_time), 0), CAST(a.call_date AS DATETIME))
            END,
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
            0, -- HasAnomalies = false
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
            HasAnomalies,
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
            0, -- HasAnomalies = false
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
            HasAnomalies,
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
            0, -- HasAnomalies = false
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

-- Grant execute permissions (adjust role as needed)
GRANT EXECUTE ON sp_ConsolidateCallLogBatch TO public;
GO

PRINT 'Stored procedure sp_ConsolidateCallLogBatch created successfully!';
PRINT '';
PRINT 'Usage example:';
PRINT 'DECLARE @BatchId UNIQUEIDENTIFIER = NEWID();';
PRINT 'EXEC sp_ConsolidateCallLogBatch @BatchId, 1, 2025, 1, 2025, ''admin@example.com'';';
GO
