-- Comprehensive diagnostic query for index number 10283784
-- This checks for ALL call records (staged, processing, completed, failed)

DECLARE @IndexNumber NVARCHAR(50) = '10283784';

-- Step 1: Get user information
SELECT '=== USER INFORMATION ===' AS Section;
SELECT
    e.Id,
    e.IndexNumber,
    e.FirstName,
    e.LastName,
    e.Email,
    e.IsActive,
    o.Name AS Organization
FROM EbillUsers e
LEFT JOIN Organizations o ON e.OrganizationId = o.Id
WHERE e.IndexNumber = @IndexNumber;

-- Step 2: Get ALL phone numbers for this user (active and inactive)
SELECT '=== ALL PHONE NUMBERS (Active and Inactive) ===' AS Section;
SELECT
    Id,
    PhoneNumber,
    PhoneType,
    LineType,
    IsActive,
    Status,
    AssignedDate,
    UnassignedDate,
    CASE
        WHEN IsActive = 1 THEN 'Currently Active'
        WHEN UnassignedDate IS NOT NULL THEN 'Unassigned on ' + CONVERT(VARCHAR, UnassignedDate, 120)
        ELSE 'Inactive'
    END AS PhoneStatus
FROM UserPhones
WHERE IndexNumber = @IndexNumber
ORDER BY AssignedDate DESC;

-- Step 3: Get the phone numbers into a temp variable for queries
DECLARE @PhoneNumbers TABLE (PhoneNumber NVARCHAR(20));
INSERT INTO @PhoneNumbers
SELECT PhoneNumber FROM UserPhones WHERE IndexNumber = @IndexNumber;

-- Step 4: Check Safaricom records with ALL statuses
SELECT '=== SAFARICOM RECORDS ===' AS Section;
SELECT
    ProcessingStatus,
    CASE ProcessingStatus
        WHEN 0 THEN 'Staged (Available for Import)'
        WHEN 1 THEN 'Processing'
        WHEN 2 THEN 'Completed'
        WHEN 3 THEN 'Failed (Available for Import)'
        ELSE 'Unknown'
    END AS StatusName,
    COUNT(*) AS RecordCount,
    MIN(call_date) AS EarliestCall,
    MAX(call_date) AS LatestCall,
    SUM(CAST(ISNULL(Cost, 0) AS DECIMAL(18,2))) AS TotalCost
FROM Safaricom
WHERE Ext IN (SELECT PhoneNumber FROM @PhoneNumbers)
GROUP BY ProcessingStatus
ORDER BY ProcessingStatus;

-- Show sample Safaricom records
SELECT 'Sample Safaricom Records (First 5)' AS Section;
SELECT TOP 5
    Id,
    Ext AS Extension,
    call_date AS CallDate,
    Dialed,
    Dest AS Destination,
    Dur AS Duration,
    Cost,
    ProcessingStatus,
    CASE ProcessingStatus
        WHEN 0 THEN 'Staged'
        WHEN 1 THEN 'Processing'
        WHEN 2 THEN 'Completed'
        WHEN 3 THEN 'Failed'
    END AS Status
FROM Safaricom
WHERE Ext IN (SELECT PhoneNumber FROM @PhoneNumbers)
ORDER BY call_date DESC;

-- Step 5: Check Airtel records
SELECT '=== AIRTEL RECORDS ===' AS Section;
SELECT
    ProcessingStatus,
    CASE ProcessingStatus
        WHEN 0 THEN 'Staged (Available for Import)'
        WHEN 1 THEN 'Processing'
        WHEN 2 THEN 'Completed'
        WHEN 3 THEN 'Failed (Available for Import)'
        ELSE 'Unknown'
    END AS StatusName,
    COUNT(*) AS RecordCount,
    MIN(call_date) AS EarliestCall,
    MAX(call_date) AS LatestCall,
    SUM(CAST(ISNULL(Cost, 0) AS DECIMAL(18,2))) AS TotalCost
FROM Airtel
WHERE Ext IN (SELECT PhoneNumber FROM @PhoneNumbers)
GROUP BY ProcessingStatus
ORDER BY ProcessingStatus;

-- Show sample Airtel records
SELECT 'Sample Airtel Records (First 5)' AS Section;
SELECT TOP 5
    Id,
    Ext AS Extension,
    call_date AS CallDate,
    Dialed,
    Dest AS Destination,
    Dur AS Duration,
    Cost,
    ProcessingStatus
FROM Airtel
WHERE Ext IN (SELECT PhoneNumber FROM @PhoneNumbers)
ORDER BY call_date DESC;

-- Step 6: Check PSTN records
SELECT '=== PSTN RECORDS ===' AS Section;
SELECT
    ProcessingStatus,
    CASE ProcessingStatus
        WHEN 0 THEN 'Staged (Available for Import)'
        WHEN 1 THEN 'Processing'
        WHEN 2 THEN 'Completed'
        WHEN 3 THEN 'Failed (Available for Import)'
        ELSE 'Unknown'
    END AS StatusName,
    COUNT(*) AS RecordCount,
    MIN(CallDate) AS EarliestCall,
    MAX(CallDate) AS LatestCall,
    SUM(CAST(ISNULL(AmountKSH, 0) AS DECIMAL(18,2))) AS TotalCost
FROM PSTNs
WHERE Extension IN (SELECT PhoneNumber FROM @PhoneNumbers)
GROUP BY ProcessingStatus
ORDER BY ProcessingStatus;

-- Show sample PSTN records
SELECT 'Sample PSTN Records (First 5)' AS Section;
SELECT TOP 5
    Id,
    Extension,
    CallDate,
    DialedNumber,
    Destination,
    Duration,
    AmountKSH AS TotalCost,
    ProcessingStatus
FROM PSTNs
WHERE Extension IN (SELECT PhoneNumber FROM @PhoneNumbers)
ORDER BY CallDate DESC;

-- Step 7: Check PrivateWire records
SELECT '=== PRIVATEWIRE RECORDS ===' AS Section;
SELECT
    ProcessingStatus,
    CASE ProcessingStatus
        WHEN 0 THEN 'Staged (Available for Import)'
        WHEN 1 THEN 'Processing'
        WHEN 2 THEN 'Completed'
        WHEN 3 THEN 'Failed (Available for Import)'
        ELSE 'Unknown'
    END AS StatusName,
    COUNT(*) AS RecordCount,
    MIN(CallDate) AS EarliestCall,
    MAX(CallDate) AS LatestCall,
    SUM(CAST(ISNULL(AmountUSD, 0) AS DECIMAL(18,2))) AS TotalCostUSD
FROM PrivateWires
WHERE Extension IN (SELECT PhoneNumber FROM @PhoneNumbers)
GROUP BY ProcessingStatus
ORDER BY ProcessingStatus;

-- Show sample PrivateWire records
SELECT 'Sample PrivateWire Records (First 5)' AS Section;
SELECT TOP 5
    Id,
    Extension,
    CallDate,
    DialedNumber,
    Destination,
    Duration,
    AmountUSD AS TotalCostUSD,
    ProcessingStatus
FROM PrivateWires
WHERE Extension IN (SELECT PhoneNumber FROM @PhoneNumbers)
ORDER BY CallDate DESC;

-- Step 8: Summary of available records (Staged or Failed)
SELECT '=== SUMMARY: RECORDS AVAILABLE FOR IMPORT ===' AS Section;
SELECT
    'Safaricom' AS SourceSystem,
    COUNT(*) AS AvailableRecords
FROM Safaricom
WHERE Ext IN (SELECT PhoneNumber FROM @PhoneNumbers)
  AND ProcessingStatus IN (0, 3)
UNION ALL
SELECT
    'Airtel' AS SourceSystem,
    COUNT(*) AS AvailableRecords
FROM Airtel
WHERE Ext IN (SELECT PhoneNumber FROM @PhoneNumbers)
  AND ProcessingStatus IN (0, 3)
UNION ALL
SELECT
    'PSTN' AS SourceSystem,
    COUNT(*) AS AvailableRecords
FROM PSTNs
WHERE Extension IN (SELECT PhoneNumber FROM @PhoneNumbers)
  AND ProcessingStatus IN (0, 3)
UNION ALL
SELECT
    'PrivateWire' AS SourceSystem,
    COUNT(*) AS AvailableRecords
FROM PrivateWires
WHERE Extension IN (SELECT PhoneNumber FROM @PhoneNumbers)
  AND ProcessingStatus IN (0, 3);

-- Step 9: Check if records were already imported to staging
SELECT '=== EXISTING STAGING RECORDS FOR THIS USER ===' AS Section;
SELECT
    b.BatchName,
    b.BatchCategory,
    b.BatchType,
    b.CreatedDate,
    COUNT(cs.Id) AS RecordsInBatch,
    cs.SourceSystem
FROM CallLogStagings cs
INNER JOIN StagingBatches b ON cs.BatchId = b.Id
WHERE cs.ResponsibleIndexNumber = @IndexNumber
GROUP BY b.BatchName, b.BatchCategory, b.BatchType, b.CreatedDate, cs.SourceSystem
ORDER BY b.CreatedDate DESC;

-- Step 10: Check if records were already published to production
SELECT '=== EXISTING PRODUCTION RECORDS FOR THIS USER ===' AS Section;
SELECT
    SourceSystem,
    COUNT(*) AS ProductionRecords,
    MIN(call_date) AS EarliestCall,
    MAX(call_date) AS LatestCall
FROM CallRecords
WHERE ext_resp_index = @IndexNumber
GROUP BY SourceSystem;
