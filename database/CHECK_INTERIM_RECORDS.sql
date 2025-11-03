-- ================================================================
-- CHECK IF INTERIM BATCH RECORDS ARE IN CALLRECORDS TABLE
-- BatchId: d78175f4-7772-434d-9ae9-464cd3e67179
-- ================================================================

DECLARE @BatchId UNIQUEIDENTIFIER = 'd78175f4-7772-434d-9ae9-464cd3e67179';

PRINT '================================================================';
PRINT 'CHECKING WHERE INTERIM BATCH RECORDS ARE LOCATED';
PRINT '================================================================';
PRINT '';

-- ================================================================
-- CHECK 1: Batch Information
-- ================================================================
PRINT 'CHECK 1: Batch Information';
PRINT '----------------------------------------------------------------';

SELECT
    Id,
    BatchName,
    BatchCategory,
    BatchStatus,
    CASE BatchStatus
        WHEN 0 THEN 'Created'
        WHEN 1 THEN 'Processing'
        WHEN 2 THEN 'PartiallyVerified'
        WHEN 3 THEN 'Verified'
        WHEN 4 THEN 'Published'
        WHEN 5 THEN 'Failed'
        ELSE 'Unknown'
    END AS BatchStatusName,
    TotalRecords,
    VerifiedRecords,
    CreatedDate
FROM StagingBatches
WHERE Id = @BatchId;

PRINT '';
PRINT '';

-- ================================================================
-- CHECK 2: Records in CallLogStaging (Should be 0 if published)
-- ================================================================
PRINT 'CHECK 2: Records Still in CallLogStaging';
PRINT '----------------------------------------------------------------';

SELECT
    COUNT(*) AS RecordsInStaging,
    COUNT(DISTINCT ResponsibleIndexNumber) AS UniqueStaff
FROM CallLogStagings
WHERE BatchId = @BatchId;

PRINT '';

IF EXISTS (SELECT 1 FROM CallLogStagings WHERE BatchId = @BatchId)
BEGIN
    PRINT '⚠️  Records are still in CallLogStaging (NOT published to CallRecords yet)';

    -- Show staff in staging
    SELECT
        ResponsibleIndexNumber,
        COUNT(*) AS RecordCount,
        VerificationStatus,
        SUM(CallCostUSD) AS TotalCost
    FROM CallLogStagings
    WHERE BatchId = @BatchId
    GROUP BY ResponsibleIndexNumber, VerificationStatus
    ORDER BY ResponsibleIndexNumber;
END
ELSE
BEGIN
    PRINT '✅ No records in CallLogStaging (expected after publishing)';
END

PRINT '';
PRINT '';

-- ================================================================
-- CHECK 3: Records in CallRecords (Should have records if published)
-- ================================================================
PRINT 'CHECK 3: Records in CallRecords Table';
PRINT '----------------------------------------------------------------';

SELECT
    COUNT(*) AS RecordsInCallRecords,
    COUNT(DISTINCT ResponsibleIndexNumber) AS UniqueStaff,
    SUM(CallCostUSD) AS TotalCost
FROM CallRecords
WHERE SourceBatchId = @BatchId;

PRINT '';

IF EXISTS (SELECT 1 FROM CallRecords WHERE SourceBatchId = @BatchId)
BEGIN
    PRINT '✅ Records found in CallRecords (published successfully)';

    -- Show staff with records
    SELECT
        cr.ResponsibleIndexNumber,
        eu.FullName,
        COUNT(*) AS RecordCount,
        cr.IsVerified,
        cr.SupervisorApprovalStatus,
        SUM(cr.CallCostUSD) AS TotalCost
    FROM CallRecords cr
    LEFT JOIN EbillUsers eu ON cr.ResponsibleIndexNumber = eu.IndexNumber
    WHERE cr.SourceBatchId = @BatchId
    GROUP BY cr.ResponsibleIndexNumber, eu.FullName, cr.IsVerified, cr.SupervisorApprovalStatus
    ORDER BY cr.ResponsibleIndexNumber;
END
ELSE
BEGIN
    PRINT '❌ NO records found in CallRecords (batch not published or publishing failed)';
END

PRINT '';
PRINT '';

-- ================================================================
-- CHECK 4: Summary and Recommendations
-- ================================================================
PRINT '================================================================';
PRINT 'SUMMARY AND RECOMMENDATIONS';
PRINT '================================================================';
PRINT '';

DECLARE @BatchStatus INT;
DECLARE @RecordsInStaging INT;
DECLARE @RecordsInCallRecords INT;

SELECT @BatchStatus = BatchStatus FROM StagingBatches WHERE Id = @BatchId;
SELECT @RecordsInStaging = COUNT(*) FROM CallLogStagings WHERE BatchId = @BatchId;
SELECT @RecordsInCallRecords = COUNT(*) FROM CallRecords WHERE SourceBatchId = @BatchId;

PRINT 'Batch Status: ' + CASE @BatchStatus
    WHEN 4 THEN '✅ Published'
    ELSE '❌ NOT Published (Status: ' + CAST(@BatchStatus AS VARCHAR) + ')'
END;

PRINT 'Records in Staging: ' + CAST(@RecordsInStaging AS VARCHAR);
PRINT 'Records in CallRecords: ' + CAST(@RecordsInCallRecords AS VARCHAR);
PRINT '';

IF @BatchStatus = 4 AND @RecordsInCallRecords > 0
BEGIN
    PRINT '✅ DIAGNOSIS: Records are published and should be visible on MyCallLogs page';
    PRINT '';
    PRINT 'If staff still cannot see records, check:';
    PRINT '1. User is logged in with correct email matching their IndexNumber';
    PRINT '2. Filter settings (Month/Year) on MyCallLogs page';
    PRINT '3. Records belong to the correct ResponsibleIndexNumber';
END
ELSE IF @BatchStatus = 4 AND @RecordsInCallRecords = 0 AND @RecordsInStaging > 0
BEGIN
    PRINT '❌ ISSUE FOUND: Batch is marked as Published but records are still in Staging';
    PRINT '';
    PRINT 'SOLUTION: The publishing process did not complete properly.';
    PRINT 'Records need to be moved from CallLogStaging to CallRecords.';
    PRINT 'This usually happens automatically when batch is published.';
END
ELSE IF @BatchStatus != 4
BEGIN
    PRINT '❌ ISSUE FOUND: Batch is not published yet';
    PRINT '';
    PRINT 'SOLUTION: Publish the batch from Call Log Staging page.';
    PRINT 'Once published, records will be moved to CallRecords and visible on MyCallLogs.';
END
ELSE
BEGIN
    PRINT '⚠️  UNKNOWN STATE: Please check batch manually';
END

PRINT '';
PRINT '================================================================';
