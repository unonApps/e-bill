-- Check KSH values for the interim batch
USE TABDB;
GO

PRINT '========================================';
PRINT 'Checking Interim Batch KSH Values';
PRINT '========================================';
PRINT '';

-- Check the batch
SELECT
    'Batch Info' AS Section,
    BatchName,
    BatchType,
    BatchCategory,
    TotalRecords,
    CreatedDate
FROM StagingBatches
WHERE Id = 'd78175f4-7772-434d-9ae9-464cd3e67179';

PRINT '';
PRINT '========================================';
PRINT 'Staging Records - KSH Status';
PRINT '========================================';

-- Check staging records
SELECT
    Id,
    SourceSystem,
    SourceRecordId,
    CallDate,
    CallNumber,
    CallCost,
    CallCostUSD,
    CallCostKSHS,
    ResponsibleIndexNumber
FROM CallLogStaging
WHERE BatchId = 'd78175f4-7772-434d-9ae9-464cd3e67179'
ORDER BY SourceSystem, CallDate;

PRINT '';
PRINT '========================================';
PRINT 'Source Table Values - Safaricom';
PRINT '========================================';

-- Check if Safaricom source records have Cost values
SELECT
    s.Id,
    s.Ext,
    s.CallDate,
    s.Dialed,
    s.Cost,
    s.AmountUSD,
    s.StagingBatchId
FROM Safaricom s
WHERE s.StagingBatchId = 'd78175f4-7772-434d-9ae9-464cd3e67179';

PRINT '';
PRINT '========================================';
PRINT 'Match Test - Can we join staging to source?';
PRINT '========================================';

-- Test the join to see if we can match records
SELECT
    cls.Id AS StagingId,
    cls.SourceSystem,
    cls.SourceRecordId,
    cls.CallCostKSHS AS CurrentKSH,
    s.Cost AS SafaricomCost,
    CASE
        WHEN s.Id IS NOT NULL THEN 'MATCH FOUND'
        ELSE 'NO MATCH'
    END AS Status
FROM CallLogStaging cls
LEFT JOIN Safaricom s ON cls.SourceRecordId = CAST(s.Id AS NVARCHAR(50))
WHERE cls.BatchId = 'd78175f4-7772-434d-9ae9-464cd3e67179'
  AND cls.SourceSystem = 'Safaricom';

PRINT '';
PRINT 'Done!';
