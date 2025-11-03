-- ================================================================
-- VERIFY BATCH VISIBILITY ON EOS RECOVERY PAGE
-- BatchId: d78175f4-7772-434d-9ae9-464cd3e67179
-- ================================================================

DECLARE @BatchId UNIQUEIDENTIFIER = 'd78175f4-7772-434d-9ae9-464cd3e67179';

PRINT '================================================================';
PRINT 'CHECKING IF BATCH WILL APPEAR ON EOS RECOVERY PAGE';
PRINT '================================================================';
PRINT '';

-- ================================================================
-- CHECK 1: Batch Exists and Basic Properties
-- ================================================================
PRINT 'CHECK 1: Batch Properties';
PRINT '----------------------------------------------------------------';

IF NOT EXISTS (SELECT 1 FROM StagingBatches WHERE Id = @BatchId)
BEGIN
    PRINT '❌ BATCH NOT FOUND IN DATABASE!';
    RETURN;
END

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
    RecoveryStatus,
    CreatedDate,
    RecoveryProcessingDate,
    -- Visibility Check
    CASE
        WHEN BatchCategory = 'INTERIM' THEN '✅ Is INTERIM batch'
        ELSE '❌ Not INTERIM batch'
    END AS Check_Category,
    CASE
        WHEN BatchStatus = 4 THEN '✅ Is Published'
        ELSE '❌ Not Published (Status: ' + CAST(BatchStatus AS VARCHAR) + ')'
    END AS Check_Published,
    CASE
        WHEN RecoveryStatus IS NULL OR RecoveryStatus = 'Pending' OR RecoveryStatus = 'InProgress'
            THEN '✅ Recovery Not Completed'
        ELSE '❌ Recovery Already Completed'
    END AS Check_RecoveryStatus,
    -- Final Verdict
    CASE
        WHEN BatchCategory = 'INTERIM'
            AND BatchStatus = 4
            AND (RecoveryStatus IS NULL OR RecoveryStatus = 'Pending' OR RecoveryStatus = 'InProgress')
            THEN '✅✅✅ WILL SHOW ON EOS RECOVERY PAGE'
        ELSE '❌❌❌ WILL NOT SHOW ON EOS RECOVERY PAGE'
    END AS FinalVerdict
FROM StagingBatches
WHERE Id = @BatchId;

PRINT '';
PRINT '';

-- ================================================================
-- CHECK 2: Staff Members with Approved Records Pending Recovery
-- ================================================================
PRINT 'CHECK 2: Staff with Approved Records Pending Recovery';
PRINT '----------------------------------------------------------------';

SELECT
    COUNT(DISTINCT cr.ResponsibleIndexNumber) AS StaffCount,
    COUNT(*) AS RecordCount,
    SUM(cr.CallCostUSD) AS TotalAmountUSD
FROM CallRecords cr
WHERE cr.SourceBatchId = @BatchId
    AND cr.SupervisorApprovalStatus = 'Approved'
    AND (cr.RecoveryStatus = 'Pending' OR cr.RecoveryStatus = 'NotProcessed');

IF (SELECT COUNT(DISTINCT cr.ResponsibleIndexNumber)
    FROM CallRecords cr
    WHERE cr.SourceBatchId = @BatchId
        AND cr.SupervisorApprovalStatus = 'Approved'
        AND (cr.RecoveryStatus = 'Pending' OR cr.RecoveryStatus = 'NotProcessed')) > 0
BEGIN
    PRINT '✅ Has staff members with approved records pending recovery';
END
ELSE
BEGIN
    PRINT '❌ NO staff members with approved records pending recovery';
    PRINT '   (Batch will show but with 0 staff members)';
END

PRINT '';
PRINT '';

-- ================================================================
-- CHECK 3: Detailed Staff List
-- ================================================================
PRINT 'CHECK 3: Staff Members Details';
PRINT '----------------------------------------------------------------';

SELECT
    cr.ResponsibleIndexNumber,
    eu.FullName,
    org.Name AS Organization,
    COUNT(*) AS ApprovedRecords,
    SUM(cr.CallCostUSD) AS TotalRecoveryAmount,
    SUM(CASE WHEN cr.VerificationType = 'Personal' THEN cr.CallCostUSD ELSE 0 END) AS PersonalAmount,
    SUM(CASE WHEN cr.VerificationType = 'Official' THEN cr.CallCostUSD ELSE 0 END) AS OfficialAmount
FROM CallRecords cr
LEFT JOIN EbillUsers eu ON cr.ResponsibleIndexNumber = eu.IndexNumber
LEFT JOIN Organizations org ON eu.OrganizationId = org.Id
WHERE cr.SourceBatchId = @BatchId
    AND cr.SupervisorApprovalStatus = 'Approved'
    AND (cr.RecoveryStatus = 'Pending' OR cr.RecoveryStatus = 'NotProcessed')
GROUP BY
    cr.ResponsibleIndexNumber,
    eu.FullName,
    org.Name
ORDER BY TotalRecoveryAmount DESC;

PRINT '';
PRINT '';

-- ================================================================
-- CHECK 4: Business Logic Verification Query
-- ================================================================
PRINT 'CHECK 4: Exact Query Used by EOS Recovery Page';
PRINT '----------------------------------------------------------------';
PRINT 'This is the exact WHERE clause used by the application:';
PRINT 'WHERE BatchCategory = INTERIM';
PRINT '  AND BatchStatus = 4 (Published)';
PRINT '  AND (RecoveryStatus IS NULL OR = Pending OR = InProgress)';
PRINT '';

SELECT
    b.Id,
    b.BatchName,
    b.BatchCategory,
    b.BatchStatus,
    b.RecoveryStatus,
    COUNT(DISTINCT cl.ResponsibleIndexNumber) AS StaffInStaging,
    '✅ MATCHES CRITERIA' AS Result
FROM StagingBatches b
LEFT JOIN CallLogStaging cl ON b.Id = cl.BatchId
WHERE b.BatchCategory = 'INTERIM'
    AND b.BatchStatus = 4  -- Published
    AND (b.RecoveryStatus IS NULL
         OR b.RecoveryStatus = 'Pending'
         OR b.RecoveryStatus = 'InProgress')
    AND b.Id = @BatchId
GROUP BY
    b.Id,
    b.BatchName,
    b.BatchCategory,
    b.BatchStatus,
    b.RecoveryStatus;

IF @@ROWCOUNT = 0
BEGIN
    PRINT '❌ Batch does NOT match the business logic criteria';
    PRINT '';
    PRINT 'Reason:';

    DECLARE @BatchCategory VARCHAR(50);
    DECLARE @BatchStatusValue INT;
    DECLARE @RecoveryStatusValue VARCHAR(50);

    SELECT
        @BatchCategory = BatchCategory,
        @BatchStatusValue = BatchStatus,
        @RecoveryStatusValue = RecoveryStatus
    FROM StagingBatches
    WHERE Id = @BatchId;

    IF @BatchCategory != 'INTERIM'
        PRINT '  ❌ BatchCategory = ' + ISNULL(@BatchCategory, 'NULL') + ' (Expected: INTERIM)';
    ELSE
        PRINT '  ✅ BatchCategory = INTERIM';

    IF @BatchStatusValue != 4
        PRINT '  ❌ BatchStatus = ' + CAST(@BatchStatusValue AS VARCHAR) + ' (Expected: 4 = Published)';
    ELSE
        PRINT '  ✅ BatchStatus = 4 (Published)';

    IF @RecoveryStatusValue NOT IN ('Pending', 'InProgress') AND @RecoveryStatusValue IS NOT NULL
        PRINT '  ❌ RecoveryStatus = ' + ISNULL(@RecoveryStatusValue, 'NULL') + ' (Expected: NULL, Pending, or InProgress)';
    ELSE
        PRINT '  ✅ RecoveryStatus = ' + ISNULL(@RecoveryStatusValue, 'NULL') + ' (Valid)';
END
ELSE
BEGIN
    PRINT '✅ Batch MATCHES all business logic criteria!';
END

PRINT '';
PRINT '';

-- ================================================================
-- FINAL SUMMARY
-- ================================================================
PRINT '================================================================';
PRINT 'FINAL VERDICT';
PRINT '================================================================';

DECLARE @WillShow BIT = 0;
DECLARE @Reason VARCHAR(500);

-- Check all conditions
IF EXISTS (
    SELECT 1
    FROM StagingBatches
    WHERE Id = @BatchId
        AND BatchCategory = 'INTERIM'
        AND BatchStatus = 4
        AND (RecoveryStatus IS NULL OR RecoveryStatus = 'Pending' OR RecoveryStatus = 'InProgress')
)
BEGIN
    SET @WillShow = 1;

    -- Check if there are staff members
    IF EXISTS (
        SELECT 1
        FROM CallRecords
        WHERE SourceBatchId = @BatchId
            AND SupervisorApprovalStatus = 'Approved'
            AND (RecoveryStatus = 'Pending' OR RecoveryStatus = 'NotProcessed')
    )
    BEGIN
        SET @Reason = '✅✅✅ BATCH WILL APPEAR ON EOS RECOVERY PAGE WITH STAFF MEMBERS';
    END
    ELSE
    BEGIN
        SET @Reason = '⚠️ Batch will appear but with 0 staff members (no approved records pending recovery)';
    END
END
ELSE
BEGIN
    SET @WillShow = 0;

    -- Determine specific reason
    DECLARE @Cat VARCHAR(50), @Stat INT, @Rec VARCHAR(50);
    SELECT @Cat = BatchCategory, @Stat = BatchStatus, @Rec = RecoveryStatus
    FROM StagingBatches WHERE Id = @BatchId;

    IF @Cat != 'INTERIM'
        SET @Reason = '❌ BatchCategory is "' + @Cat + '" (must be "INTERIM")';
    ELSE IF @Stat != 4
        SET @Reason = '❌ BatchStatus is ' + CAST(@Stat AS VARCHAR) + ' (must be 4 = Published)';
    ELSE IF @Rec = 'Completed'
        SET @Reason = '❌ RecoveryStatus is "Completed" (batch already processed)';
    ELSE
        SET @Reason = '❌ Unknown reason - check batch properties';
END

PRINT '';
PRINT @Reason;
PRINT '';
PRINT '================================================================';
