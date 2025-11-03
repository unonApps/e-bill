-- ================================================================
-- DATA COLLECTION FOR BATCH: d78175f4-7772-434d-9ae9-464cd3e67179
-- Purpose: Collect all information about this specific INTERIM batch
-- Date: October 29, 2025
-- ================================================================

DECLARE @BatchId UNIQUEIDENTIFIER = 'd78175f4-7772-434d-9ae9-464cd3e67179';

PRINT '================================================================';
PRINT 'BATCH DATA COLLECTION';
PRINT '================================================================';
PRINT '';

-- ================================================================
-- 1. BATCH INFORMATION
-- ================================================================
PRINT '1. BATCH INFORMATION';
PRINT '----------------------------------------------------------------';

SELECT
    b.Id AS BatchId,
    b.BatchName,
    b.BatchType,
    b.BatchCategory,
    b.BatchStatus,
    b.RecoveryStatus,
    b.TotalRecords,
    b.VerifiedRecords,
    b.RejectedRecords,
    b.PendingRecords,
    b.RecordsWithAnomalies,
    b.CreatedDate,
    b.StartProcessingDate,
    b.EndProcessingDate,
    b.RecoveryProcessingDate,
    b.TotalRecoveredAmount,
    b.TotalPersonalAmount,
    b.TotalOfficialAmount,
    b.TotalClassOfServiceAmount,
    b.CreatedBy,
    b.VerifiedBy,
    b.PublishedBy,
    b.BillingPeriodId,
    b.SourceSystems,
    b.Notes
FROM StagingBatches b
WHERE b.Id = @BatchId;

PRINT '';
PRINT '';

-- ================================================================
-- 2. CALL LOG STAGING RECORDS (from StagingBatch)
-- ================================================================
PRINT '2. CALL LOG STAGING RECORDS';
PRINT '----------------------------------------------------------------';

SELECT
    COUNT(*) AS TotalStagingRecords,
    COUNT(DISTINCT cls.ResponsibleIndexNumber) AS UniqueStaffMembers,
    SUM(CASE WHEN cls.VerificationStatus = 0 THEN 1 ELSE 0 END) AS PendingVerification,
    SUM(CASE WHEN cls.VerificationStatus = 1 THEN 1 ELSE 0 END) AS Verified,
    SUM(CASE WHEN cls.VerificationStatus = 2 THEN 1 ELSE 0 END) AS Rejected,
    SUM(CASE WHEN cls.SupervisorApprovalStatus = 'Approved' THEN 1 ELSE 0 END) AS SupervisorApproved,
    SUM(CASE WHEN cls.SupervisorApprovalStatus = 'Rejected' THEN 1 ELSE 0 END) AS SupervisorRejected,
    SUM(CASE WHEN cls.SupervisorApprovalStatus = 'Pending' THEN 1 ELSE 0 END) AS SupervisorPending
FROM CallLogStaging cls
WHERE cls.BatchId = @BatchId;

PRINT '';

-- Staff members in staging
SELECT
    cls.ResponsibleIndexNumber,
    eu.FullName AS StaffName,
    org.Name AS Organization,
    COUNT(*) AS TotalCalls,
    SUM(cls.CallCostUSD) AS TotalCostUSD,
    SUM(CASE WHEN cls.VerificationType = 'Personal' THEN cls.CallCostUSD ELSE 0 END) AS PersonalCostUSD,
    SUM(CASE WHEN cls.VerificationType = 'Official' THEN cls.CallCostUSD ELSE 0 END) AS OfficialCostUSD,
    cls.VerificationStatus,
    cls.SupervisorApprovalStatus
FROM CallLogStaging cls
LEFT JOIN EbillUsers eu ON cls.ResponsibleIndexNumber = eu.IndexNumber
LEFT JOIN Organizations org ON eu.OrganizationId = org.Id
WHERE cls.BatchId = @BatchId
GROUP BY
    cls.ResponsibleIndexNumber,
    eu.FullName,
    org.Name,
    cls.VerificationStatus,
    cls.SupervisorApprovalStatus
ORDER BY TotalCostUSD DESC;

PRINT '';
PRINT '';

-- ================================================================
-- 3. CALL RECORDS (Published to CallRecords table)
-- ================================================================
PRINT '3. CALL RECORDS (Published Data)';
PRINT '----------------------------------------------------------------';

SELECT
    COUNT(*) AS TotalCallRecords,
    COUNT(DISTINCT cr.ResponsibleIndexNumber) AS UniqueStaffMembers,
    SUM(CASE WHEN cr.SupervisorApprovalStatus = 'Approved' THEN 1 ELSE 0 END) AS ApprovedRecords,
    SUM(CASE WHEN cr.SupervisorApprovalStatus = 'Rejected' THEN 1 ELSE 0 END) AS RejectedRecords,
    SUM(CASE WHEN cr.SupervisorApprovalStatus = 'Pending' THEN 1 ELSE 0 END) AS PendingRecords,
    SUM(CASE WHEN cr.RecoveryStatus = 'Completed' THEN 1 ELSE 0 END) AS RecoveredRecords,
    SUM(CASE WHEN cr.RecoveryStatus = 'Pending' OR cr.RecoveryStatus = 'NotProcessed' THEN 1 ELSE 0 END) AS PendingRecovery,
    SUM(cr.CallCostUSD) AS TotalCostUSD,
    SUM(CASE WHEN cr.RecoveryStatus = 'Completed' THEN cr.RecoveryAmount ELSE 0 END) AS TotalRecoveredAmount,
    SUM(CASE WHEN cr.RecoveryStatus = 'Pending' OR cr.RecoveryStatus = 'NotProcessed' THEN cr.CallCostUSD ELSE 0 END) AS PendingRecoveryAmount
FROM CallRecords cr
WHERE cr.SourceBatchId = @BatchId;

PRINT '';

-- Staff members with approved records pending recovery
SELECT
    cr.ResponsibleIndexNumber,
    eu.FullName AS StaffName,
    org.Name AS Organization,
    COUNT(*) AS TotalRecords,
    SUM(cr.CallCostUSD) AS TotalCostUSD,
    SUM(CASE WHEN cr.VerificationType = 'Personal' THEN cr.CallCostUSD ELSE 0 END) AS PersonalCostUSD,
    SUM(CASE WHEN cr.VerificationType = 'Official' THEN cr.CallCostUSD ELSE 0 END) AS OfficialCostUSD,
    cr.SupervisorApprovalStatus,
    cr.RecoveryStatus,
    MAX(cr.VerificationPeriod) AS VerificationDeadline,
    MAX(cr.ApprovalPeriod) AS ApprovalDeadline
FROM CallRecords cr
LEFT JOIN EbillUsers eu ON cr.ResponsibleIndexNumber = eu.IndexNumber
LEFT JOIN Organizations org ON eu.OrganizationId = org.Id
WHERE cr.SourceBatchId = @BatchId
    AND cr.SupervisorApprovalStatus = 'Approved'
    AND (cr.RecoveryStatus = 'Pending' OR cr.RecoveryStatus = 'NotProcessed')
GROUP BY
    cr.ResponsibleIndexNumber,
    eu.FullName,
    org.Name,
    cr.SupervisorApprovalStatus,
    cr.RecoveryStatus
ORDER BY TotalCostUSD DESC;

PRINT '';
PRINT '';

-- ================================================================
-- 4. EOS STAFF DETAILS (Staff who left the organization)
-- ================================================================
PRINT '4. EOS STAFF DETAILS';
PRINT '----------------------------------------------------------------';

SELECT
    eu.IndexNumber,
    eu.FullName,
    eu.Email,
    org.Name AS Organization,
    off.Name AS Office,
    sub.Name AS SubOffice,
    eu.Status,
    eu.ContractType,
    eu.Grade,
    up.PrimaryMobileNumber,
    up.LineType,
    up.Status AS PhoneStatus
FROM CallRecords cr
INNER JOIN EbillUsers eu ON cr.ResponsibleIndexNumber = eu.IndexNumber
LEFT JOIN Organizations org ON eu.OrganizationId = org.Id
LEFT JOIN Offices off ON eu.OfficeId = off.Id
LEFT JOIN SubOffices sub ON eu.SubOfficeId = sub.Id
LEFT JOIN UserPhones up ON eu.IndexNumber = up.IndexNumber AND up.IsPrimary = 1
WHERE cr.SourceBatchId = @BatchId
    AND cr.SupervisorApprovalStatus = 'Approved'
    AND (cr.RecoveryStatus = 'Pending' OR cr.RecoveryStatus = 'NotProcessed')
GROUP BY
    eu.IndexNumber,
    eu.FullName,
    eu.Email,
    org.Name,
    off.Name,
    sub.Name,
    eu.Status,
    eu.ContractType,
    eu.Grade,
    up.PrimaryMobileNumber,
    up.LineType,
    up.Status
ORDER BY eu.FullName;

PRINT '';
PRINT '';

-- ================================================================
-- 5. RECOVERY BREAKDOWN BY VERIFICATION TYPE
-- ================================================================
PRINT '5. RECOVERY BREAKDOWN';
PRINT '----------------------------------------------------------------';

SELECT
    cr.VerificationType,
    COUNT(*) AS RecordCount,
    SUM(cr.CallCostUSD) AS TotalCostUSD,
    AVG(cr.CallCostUSD) AS AverageCostUSD,
    MIN(cr.CallCostUSD) AS MinCostUSD,
    MAX(cr.CallCostUSD) AS MaxCostUSD
FROM CallRecords cr
WHERE cr.SourceBatchId = @BatchId
    AND cr.SupervisorApprovalStatus = 'Approved'
    AND (cr.RecoveryStatus = 'Pending' OR cr.RecoveryStatus = 'NotProcessed')
GROUP BY cr.VerificationType;

PRINT '';
PRINT '';

-- ================================================================
-- 6. VERIFICATION STATUS DETAILS
-- ================================================================
PRINT '6. VERIFICATION & APPROVAL STATUS';
PRINT '----------------------------------------------------------------';

SELECT
    cr.SupervisorApprovalStatus,
    cr.RecoveryStatus,
    COUNT(*) AS RecordCount,
    SUM(cr.CallCostUSD) AS TotalCostUSD,
    COUNT(DISTINCT cr.ResponsibleIndexNumber) AS UniqueStaff
FROM CallRecords cr
WHERE cr.SourceBatchId = @BatchId
GROUP BY
    cr.SupervisorApprovalStatus,
    cr.RecoveryStatus
ORDER BY cr.SupervisorApprovalStatus, cr.RecoveryStatus;

PRINT '';
PRINT '';

-- ================================================================
-- 7. CALL DETAILS BY MONTH
-- ================================================================
PRINT '7. CALL DETAILS BY MONTH';
PRINT '----------------------------------------------------------------';

SELECT
    cr.CallYear,
    cr.CallMonth,
    COUNT(*) AS RecordCount,
    SUM(cr.CallCostUSD) AS TotalCostUSD,
    COUNT(DISTINCT cr.ResponsibleIndexNumber) AS UniqueStaff,
    SUM(CASE WHEN cr.VerificationType = 'Personal' THEN cr.CallCostUSD ELSE 0 END) AS PersonalCostUSD,
    SUM(CASE WHEN cr.VerificationType = 'Official' THEN cr.CallCostUSD ELSE 0 END) AS OfficialCostUSD
FROM CallRecords cr
WHERE cr.SourceBatchId = @BatchId
    AND cr.SupervisorApprovalStatus = 'Approved'
    AND (cr.RecoveryStatus = 'Pending' OR cr.RecoveryStatus = 'NotProcessed')
GROUP BY cr.CallYear, cr.CallMonth
ORDER BY cr.CallYear, cr.CallMonth;

PRINT '';
PRINT '';

-- ================================================================
-- 8. RECOVERY LOGS (if any recovery has been processed)
-- ================================================================
PRINT '8. EXISTING RECOVERY LOGS';
PRINT '----------------------------------------------------------------';

SELECT
    rl.Id AS RecoveryLogId,
    rl.RecoveryType,
    rl.RecoveryAction,
    rl.RecoveryDate,
    rl.RecoveryReason,
    rl.AmountRecovered,
    rl.RecoveredFrom,
    rl.ProcessedBy,
    rl.IsAutomated,
    cr.ResponsibleIndexNumber,
    eu.FullName AS StaffName
FROM RecoveryLogs rl
INNER JOIN CallRecords cr ON rl.CallRecordId = cr.Id
LEFT JOIN EbillUsers eu ON cr.ResponsibleIndexNumber = eu.IndexNumber
WHERE cr.SourceBatchId = @BatchId
ORDER BY rl.RecoveryDate DESC;

PRINT '';
PRINT '';

-- ================================================================
-- 9. DEADLINES SUMMARY
-- ================================================================
PRINT '9. DEADLINES SUMMARY';
PRINT '----------------------------------------------------------------';

SELECT
    'Verification Deadline' AS DeadlineType,
    MIN(cr.VerificationPeriod) AS EarliestDeadline,
    MAX(cr.VerificationPeriod) AS LatestDeadline,
    COUNT(CASE WHEN cr.VerificationPeriod < GETDATE() THEN 1 END) AS OverdueCount,
    COUNT(CASE WHEN cr.VerificationPeriod >= GETDATE() THEN 1 END) AS UpcomingCount
FROM CallRecords cr
WHERE cr.SourceBatchId = @BatchId
    AND cr.VerificationPeriod IS NOT NULL

UNION ALL

SELECT
    'Approval Deadline' AS DeadlineType,
    MIN(cr.ApprovalPeriod) AS EarliestDeadline,
    MAX(cr.ApprovalPeriod) AS LatestDeadline,
    COUNT(CASE WHEN cr.ApprovalPeriod < GETDATE() THEN 1 END) AS OverdueCount,
    COUNT(CASE WHEN cr.ApprovalPeriod >= GETDATE() THEN 1 END) AS UpcomingCount
FROM CallRecords cr
WHERE cr.SourceBatchId = @BatchId
    AND cr.ApprovalPeriod IS NOT NULL;

PRINT '';
PRINT '';

-- ================================================================
-- 10. BILLING PERIOD INFORMATION
-- ================================================================
PRINT '10. BILLING PERIOD INFORMATION';
PRINT '----------------------------------------------------------------';

SELECT
    bp.Id AS BillingPeriodId,
    bp.Name AS PeriodName,
    bp.StartDate,
    bp.EndDate,
    bp.Year,
    bp.Month,
    bp.Status,
    bp.IsActive,
    bp.Notes
FROM StagingBatches b
INNER JOIN BillingPeriods bp ON b.BillingPeriodId = bp.Id
WHERE b.Id = @BatchId;

PRINT '';
PRINT '';

-- ================================================================
-- 11. SUMMARY STATISTICS
-- ================================================================
PRINT '11. SUMMARY STATISTICS';
PRINT '================================================================';

SELECT
    'BATCH SUMMARY' AS Category,
    (SELECT BatchName FROM StagingBatches WHERE Id = @BatchId) AS BatchName,
    (SELECT BatchStatus FROM StagingBatches WHERE Id = @BatchId) AS BatchStatus,
    (SELECT RecoveryStatus FROM StagingBatches WHERE Id = @BatchId) AS RecoveryStatus,
    (SELECT COUNT(DISTINCT ResponsibleIndexNumber)
     FROM CallRecords
     WHERE SourceBatchId = @BatchId
       AND SupervisorApprovalStatus = 'Approved'
       AND (RecoveryStatus = 'Pending' OR RecoveryStatus = 'NotProcessed')) AS StaffPendingRecovery,
    (SELECT COUNT(*)
     FROM CallRecords
     WHERE SourceBatchId = @BatchId
       AND SupervisorApprovalStatus = 'Approved'
       AND (RecoveryStatus = 'Pending' OR RecoveryStatus = 'NotProcessed')) AS RecordsPendingRecovery,
    (SELECT SUM(CallCostUSD)
     FROM CallRecords
     WHERE SourceBatchId = @BatchId
       AND SupervisorApprovalStatus = 'Approved'
       AND (RecoveryStatus = 'Pending' OR RecoveryStatus = 'NotProcessed')) AS TotalPendingRecoveryUSD,
    (SELECT SUM(CASE WHEN VerificationType = 'Personal' THEN CallCostUSD ELSE 0 END)
     FROM CallRecords
     WHERE SourceBatchId = @BatchId
       AND SupervisorApprovalStatus = 'Approved'
       AND (RecoveryStatus = 'Pending' OR RecoveryStatus = 'NotProcessed')) AS PersonalRecoveryUSD,
    (SELECT SUM(CASE WHEN VerificationType = 'Official' THEN CallCostUSD ELSE 0 END)
     FROM CallRecords
     WHERE SourceBatchId = @BatchId
       AND SupervisorApprovalStatus = 'Approved'
       AND (RecoveryStatus = 'Pending' OR RecoveryStatus = 'NotProcessed')) AS OfficialRecoveryUSD;

PRINT '';
PRINT '================================================================';
PRINT 'DATA COLLECTION COMPLETE';
PRINT '================================================================';
