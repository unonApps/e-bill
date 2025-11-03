-- =============================================
-- Script to Backfill CallCostKSHS in CallLogStaging
-- Updates existing staging records with KSH values from source telecom tables
-- =============================================

USE TABDB;
GO

PRINT '========================================';
PRINT 'Starting KSH Backfill for CallLogStaging';
PRINT '========================================';
PRINT '';

-- Check current state
PRINT 'Current Records with Zero KSH:';
SELECT
    SourceSystem,
    COUNT(*) AS RecordsWithZeroKSH
FROM CallLogStaging
WHERE CallCostKSHS = 0 OR CallCostKSHS IS NULL
GROUP BY SourceSystem;
PRINT '';

-- 1. Update Safaricom records
PRINT '1. Updating Safaricom records...';
UPDATE cls
SET cls.CallCostKSHS = s.Cost
FROM CallLogStaging cls
INNER JOIN Safaricom s ON cls.SourceRecordId = CAST(s.Id AS NVARCHAR(50))
WHERE cls.SourceSystem = 'Safaricom'
  AND (cls.CallCostKSHS = 0 OR cls.CallCostKSHS IS NULL)
  AND s.Cost IS NOT NULL;

PRINT 'Safaricom: ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + ' records updated';
PRINT '';

-- 2. Update Airtel records
PRINT '2. Updating Airtel records...';
UPDATE cls
SET cls.CallCostKSHS = a.Cost
FROM CallLogStaging cls
INNER JOIN Airtel a ON cls.SourceRecordId = CAST(a.Id AS NVARCHAR(50))
WHERE cls.SourceSystem = 'Airtel'
  AND (cls.CallCostKSHS = 0 OR cls.CallCostKSHS IS NULL)
  AND a.Cost IS NOT NULL;

PRINT 'Airtel: ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + ' records updated';
PRINT '';

-- 3. Update PSTN records
PRINT '3. Updating PSTN records...';
UPDATE cls
SET cls.CallCostKSHS = p.AmountKSH
FROM CallLogStaging cls
INNER JOIN PSTN p ON cls.SourceRecordId = CAST(p.Id AS NVARCHAR(50))
WHERE cls.SourceSystem = 'PSTN'
  AND (cls.CallCostKSHS = 0 OR cls.CallCostKSHS IS NULL)
  AND p.AmountKSH IS NOT NULL;

PRINT 'PSTN: ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + ' records updated';
PRINT '';

-- 4. Update PrivateWire records (calculate from USD if KSH not available)
PRINT '4. Updating PrivateWire records...';
UPDATE cls
SET cls.CallCostKSHS = COALESCE(pw.AmountKSH, pw.AmountUSD * 150)
FROM CallLogStaging cls
INNER JOIN PrivateWire pw ON cls.SourceRecordId = CAST(pw.Id AS NVARCHAR(50))
WHERE cls.SourceSystem = 'PrivateWire'
  AND (cls.CallCostKSHS = 0 OR cls.CallCostKSHS IS NULL);

PRINT 'PrivateWire: ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + ' records updated';
PRINT '';

-- Verify results
PRINT '========================================';
PRINT 'Verification - Records Still with Zero KSH:';
SELECT
    SourceSystem,
    COUNT(*) AS RecordsWithZeroKSH
FROM CallLogStaging
WHERE CallCostKSHS = 0 OR CallCostKSHS IS NULL
GROUP BY SourceSystem;
PRINT '';

-- Summary statistics
PRINT '========================================';
PRINT 'Summary - KSH Values by Source:';
SELECT
    SourceSystem,
    COUNT(*) AS TotalRecords,
    SUM(CallCostKSHS) AS TotalKSH,
    AVG(CallCostKSHS) AS AvgKSH,
    MIN(CallCostKSHS) AS MinKSH,
    MAX(CallCostKSHS) AS MaxKSH
FROM CallLogStaging
GROUP BY SourceSystem
ORDER BY SourceSystem;
PRINT '';

PRINT '========================================';
PRINT 'KSH Backfill Complete!';
PRINT '========================================';
