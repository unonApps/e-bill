-- Quick Fix for Interim Batch d78175f4-7772-434d-9ae9-464cd3e67179
USE TABDB;
GO

PRINT '========================================';
PRINT 'Fixing Interim Batch KSH Values';
PRINT 'Batch ID: d78175f4-7772-434d-9ae9-464cd3e67179';
PRINT '========================================';
PRINT '';

-- Show current state
PRINT 'Current State:';
SELECT
    SourceSystem,
    COUNT(*) AS Records,
    SUM(CallCostUSD) AS TotalUSD,
    SUM(CallCostKSHS) AS TotalKSH
FROM CallLogStaging
WHERE BatchId = 'd78175f4-7772-434d-9ae9-464cd3e67179'
GROUP BY SourceSystem;
PRINT '';

-- Update Safaricom records
PRINT 'Updating Safaricom records...';
UPDATE cls
SET cls.CallCostKSHS = s.Cost
FROM CallLogStaging cls
INNER JOIN Safaricom s ON cls.SourceRecordId = CAST(s.Id AS NVARCHAR(50))
WHERE cls.BatchId = 'd78175f4-7772-434d-9ae9-464cd3e67179'
  AND cls.SourceSystem = 'Safaricom'
  AND s.Cost IS NOT NULL;
PRINT 'Updated: ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + ' Safaricom records';
PRINT '';

-- Update Airtel records
PRINT 'Updating Airtel records...';
UPDATE cls
SET cls.CallCostKSHS = a.Cost
FROM CallLogStaging cls
INNER JOIN Airtel a ON cls.SourceRecordId = CAST(a.Id AS NVARCHAR(50))
WHERE cls.BatchId = 'd78175f4-7772-434d-9ae9-464cd3e67179'
  AND cls.SourceSystem = 'Airtel'
  AND a.Cost IS NOT NULL;
PRINT 'Updated: ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + ' Airtel records';
PRINT '';

-- Update PSTN records
PRINT 'Updating PSTN records...';
UPDATE cls
SET cls.CallCostKSHS = p.AmountKSH
FROM CallLogStaging cls
INNER JOIN PSTN p ON cls.SourceRecordId = CAST(p.Id AS NVARCHAR(50))
WHERE cls.BatchId = 'd78175f4-7772-434d-9ae9-464cd3e67179'
  AND cls.SourceSystem = 'PSTN'
  AND p.AmountKSH IS NOT NULL;
PRINT 'Updated: ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + ' PSTN records';
PRINT '';

-- Update PrivateWire records
PRINT 'Updating PrivateWire records...';
UPDATE cls
SET cls.CallCostKSHS = COALESCE(pw.AmountKSH, pw.AmountUSD * 150)
FROM CallLogStaging cls
INNER JOIN PrivateWire pw ON cls.SourceRecordId = CAST(pw.Id AS NVARCHAR(50))
WHERE cls.BatchId = 'd78175f4-7772-434d-9ae9-464cd3e67179'
  AND cls.SourceSystem = 'PrivateWire';
PRINT 'Updated: ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + ' PrivateWire records';
PRINT '';

-- Show updated state
PRINT '========================================';
PRINT 'After Update:';
SELECT
    SourceSystem,
    COUNT(*) AS Records,
    SUM(CallCostUSD) AS TotalUSD,
    SUM(CallCostKSHS) AS TotalKSH
FROM CallLogStaging
WHERE BatchId = 'd78175f4-7772-434d-9ae9-464cd3e67179'
GROUP BY SourceSystem;
PRINT '';

-- Show individual records
PRINT '========================================';
PRINT 'Individual Records:';
SELECT
    SourceSystem,
    CallDate,
    CallNumber,
    CallCostUSD,
    CallCostKSHS
FROM CallLogStaging
WHERE BatchId = 'd78175f4-7772-434d-9ae9-464cd3e67179'
ORDER BY SourceSystem, CallDate;
PRINT '';

PRINT '========================================';
PRINT 'Fix Complete!';
PRINT '========================================';
