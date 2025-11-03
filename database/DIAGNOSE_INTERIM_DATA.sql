-- ================================================================
-- DIAGNOSE INTERIM BATCH AND CALLRECORDS DATA
-- ================================================================

PRINT '================================================================';
PRINT 'CHECKING INTERIM BATCH AND CALLRECORDS STATUS';
PRINT '================================================================';
PRINT '';

-- ================================================================
-- CHECK 1: INTERIM Batches
-- ================================================================
PRINT 'CHECK 1: INTERIM Batches in StagingBatches';
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
    RecoveryStatus,
    CreatedDate
FROM StagingBatches
WHERE BatchCategory = 'INTERIM'
ORDER BY CreatedDate DESC;

PRINT '';
PRINT '';

-- ================================================================
-- CHECK 2: CallRecords linked to INTERIM batches
-- ================================================================
PRINT 'CHECK 2: CallRecords linked to INTERIM Batches';
PRINT '----------------------------------------------------------------';

SELECT
    cr.Id,
    cr.ResponsibleIndexNumber,
    cr.ExtensionNumber,
    cr.CallCostUSD,
    cr.IsVerified,
    cr.SupervisorApprovalStatus,
    cr.RecoveryStatus,
    cr.SourceBatchId,
    sb.BatchName,
    sb.BatchCategory,
    sb.BatchStatus
FROM CallRecords cr
INNER JOIN StagingBatches sb ON cr.SourceBatchId = sb.Id
WHERE sb.BatchCategory = 'INTERIM'
ORDER BY cr.ResponsibleIndexNumber, cr.CallDate;

PRINT '';
PRINT '';

-- ================================================================
-- CHECK 3: Summary of INTERIM CallRecords by Status
-- ================================================================
PRINT 'CHECK 3: INTERIM CallRecords Summary by Status';
PRINT '----------------------------------------------------------------';

SELECT
    cr.ResponsibleIndexNumber,
    COUNT(*) AS TotalRecords,
    SUM(CASE WHEN cr.IsVerified = 1 THEN 1 ELSE 0 END) AS VerifiedCount,
    SUM(CASE WHEN cr.SupervisorApprovalStatus = 'Approved' THEN 1 ELSE 0 END) AS ApprovedCount,
    SUM(CASE WHEN cr.SupervisorApprovalStatus = 'Pending' THEN 1 ELSE 0 END) AS PendingCount,
    SUM(cr.CallCostUSD) AS TotalCost,
    sb.BatchStatus AS BatchStatus,
    CASE sb.BatchStatus
        WHEN 4 THEN 'Published'
        ELSE 'Not Published'
    END AS BatchStatusName
FROM CallRecords cr
INNER JOIN StagingBatches sb ON cr.SourceBatchId = sb.Id
WHERE sb.BatchCategory = 'INTERIM'
GROUP BY cr.ResponsibleIndexNumber, sb.BatchStatus
ORDER BY cr.ResponsibleIndexNumber;

PRINT '';
PRINT '';

-- ================================================================
-- CHECK 4: What the EOS Recovery query SHOULD find
-- ================================================================
PRINT 'CHECK 4: Records that SHOULD appear on EOS Recovery page';
PRINT '----------------------------------------------------------------';
PRINT 'Criteria: INTERIM batch, Published, Verified, (Approved OR Pending), Recovery not completed';
PRINT '';

SELECT
    cr.ResponsibleIndexNumber,
    eu.FullName,
    COUNT(*) AS TotalRecords,
    SUM(CASE WHEN cr.SupervisorApprovalStatus = 'Approved' THEN 1 ELSE 0 END) AS ApprovedCount,
    SUM(CASE WHEN cr.SupervisorApprovalStatus = 'Pending' THEN 1 ELSE 0 END) AS PendingCount,
    SUM(cr.CallCostUSD) AS TotalAmount,
    sb.BatchName,
    sb.BatchStatus
FROM CallRecords cr
INNER JOIN StagingBatches sb ON cr.SourceBatchId = sb.Id
LEFT JOIN EbillUsers eu ON cr.ResponsibleIndexNumber = eu.IndexNumber
WHERE sb.BatchCategory = 'INTERIM'
  AND sb.BatchStatus = 4  -- Published
  AND cr.IsVerified = 1   -- Verified by staff
  AND (cr.SupervisorApprovalStatus = 'Approved' OR cr.SupervisorApprovalStatus = 'Pending')
  AND (cr.RecoveryStatus = 'Pending' OR cr.RecoveryStatus = 'NotProcessed' OR cr.RecoveryStatus IS NULL)
GROUP BY cr.ResponsibleIndexNumber, eu.FullName, sb.BatchName, sb.BatchStatus
ORDER BY TotalAmount DESC;

PRINT '';
PRINT '';

-- ================================================================
-- CHECK 5: Troubleshooting - What's blocking records?
-- ================================================================
PRINT 'CHECK 5: Troubleshooting - Count records at each filter stage';
PRINT '----------------------------------------------------------------';

DECLARE @TotalInterimRecords INT;
DECLARE @PublishedBatchRecords INT;
DECLARE @VerifiedRecords INT;
DECLARE @ApprovedOrPendingRecords INT;
DECLARE @RecoveryPendingRecords INT;

SELECT @TotalInterimRecords = COUNT(*)
FROM CallRecords cr
INNER JOIN StagingBatches sb ON cr.SourceBatchId = sb.Id
WHERE sb.BatchCategory = 'INTERIM';

SELECT @PublishedBatchRecords = COUNT(*)
FROM CallRecords cr
INNER JOIN StagingBatches sb ON cr.SourceBatchId = sb.Id
WHERE sb.BatchCategory = 'INTERIM'
  AND sb.BatchStatus = 4;

SELECT @VerifiedRecords = COUNT(*)
FROM CallRecords cr
INNER JOIN StagingBatches sb ON cr.SourceBatchId = sb.Id
WHERE sb.BatchCategory = 'INTERIM'
  AND sb.BatchStatus = 4
  AND cr.IsVerified = 1;

SELECT @ApprovedOrPendingRecords = COUNT(*)
FROM CallRecords cr
INNER JOIN StagingBatches sb ON cr.SourceBatchId = sb.Id
WHERE sb.BatchCategory = 'INTERIM'
  AND sb.BatchStatus = 4
  AND cr.IsVerified = 1
  AND (cr.SupervisorApprovalStatus = 'Approved' OR cr.SupervisorApprovalStatus = 'Pending');

SELECT @RecoveryPendingRecords = COUNT(*)
FROM CallRecords cr
INNER JOIN StagingBatches sb ON cr.SourceBatchId = sb.Id
WHERE sb.BatchCategory = 'INTERIM'
  AND sb.BatchStatus = 4
  AND cr.IsVerified = 1
  AND (cr.SupervisorApprovalStatus = 'Approved' OR cr.SupervisorApprovalStatus = 'Pending')
  AND (cr.RecoveryStatus = 'Pending' OR cr.RecoveryStatus = 'NotProcessed' OR cr.RecoveryStatus IS NULL);

PRINT 'Total INTERIM records: ' + CAST(@TotalInterimRecords AS VARCHAR);
PRINT 'In Published batches: ' + CAST(@PublishedBatchRecords AS VARCHAR);
PRINT 'That are Verified: ' + CAST(@VerifiedRecords AS VARCHAR);
PRINT 'With Approved/Pending status: ' + CAST(@ApprovedOrPendingRecords AS VARCHAR);
PRINT 'With Recovery Pending: ' + CAST(@RecoveryPendingRecords AS VARCHAR);

PRINT '';
PRINT '================================================================';
PRINT 'END OF DIAGNOSIS';
PRINT '================================================================';
