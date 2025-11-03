-- Diagnostic query for index number 10283784
-- Run this in SQL Server Management Studio or Azure Data Studio

DECLARE @IndexNumber NVARCHAR(50) = '10283784';
DECLARE @StartDate DATETIME2 = '2024-01-01'; -- Adjust this to your date range
DECLARE @EndDate DATETIME2 = '2025-12-31';   -- Adjust this to your date range

-- 1. Check if user exists
SELECT 'User Info' AS CheckType,
       Id, IndexNumber, FirstName, LastName, IsActive, Email
FROM EbillUsers
WHERE IndexNumber = @IndexNumber;

-- 2. Check user's phone numbers
SELECT 'Active Phone Numbers' AS CheckType,
       PhoneNumber, PhoneType, LineType, IsActive, IsPrimary, AssignedDate
FROM UserPhones
WHERE IndexNumber = @IndexNumber;

-- 3. Check Safaricom records
SELECT 'Safaricom Records' AS CheckType,
       COUNT(*) AS TotalRecords,
       SUM(CASE WHEN ProcessingStatus = 0 THEN 1 ELSE 0 END) AS Staged,
       SUM(CASE WHEN ProcessingStatus = 1 THEN 1 ELSE 0 END) AS Processing,
       SUM(CASE WHEN ProcessingStatus = 2 THEN 1 ELSE 0 END) AS Completed,
       SUM(CASE WHEN ProcessingStatus = 3 THEN 1 ELSE 0 END) AS Failed,
       MIN(call_date) AS EarliestCall,
       MAX(call_date) AS LatestCall
FROM Safaricom
WHERE Ext IN (SELECT PhoneNumber FROM UserPhones WHERE IndexNumber = @IndexNumber AND IsActive = 1);

-- 4. Check Airtel records
SELECT 'Airtel Records' AS CheckType,
       COUNT(*) AS TotalRecords,
       SUM(CASE WHEN ProcessingStatus = 0 THEN 1 ELSE 0 END) AS Staged,
       SUM(CASE WHEN ProcessingStatus = 1 THEN 1 ELSE 0 END) AS Processing,
       SUM(CASE WHEN ProcessingStatus = 2 THEN 1 ELSE 0 END) AS Completed,
       SUM(CASE WHEN ProcessingStatus = 3 THEN 1 ELSE 0 END) AS Failed,
       MIN(call_date) AS EarliestCall,
       MAX(call_date) AS LatestCall
FROM Airtel
WHERE Ext IN (SELECT PhoneNumber FROM UserPhones WHERE IndexNumber = @IndexNumber AND IsActive = 1);

-- 5. Check PSTN records
SELECT 'PSTN Records' AS CheckType,
       COUNT(*) AS TotalRecords,
       SUM(CASE WHEN ProcessingStatus = 0 THEN 1 ELSE 0 END) AS Staged,
       SUM(CASE WHEN ProcessingStatus = 1 THEN 1 ELSE 0 END) AS Processing,
       SUM(CASE WHEN ProcessingStatus = 2 THEN 1 ELSE 0 END) AS Completed,
       SUM(CASE WHEN ProcessingStatus = 3 THEN 1 ELSE 0 END) AS Failed,
       MIN(CallDate) AS EarliestCall,
       MAX(CallDate) AS LatestCall
FROM PSTNs
WHERE Extension IN (SELECT PhoneNumber FROM UserPhones WHERE IndexNumber = @IndexNumber AND IsActive = 1);

-- 6. Check PrivateWire records
SELECT 'PrivateWire Records' AS CheckType,
       COUNT(*) AS TotalRecords,
       SUM(CASE WHEN ProcessingStatus = 0 THEN 1 ELSE 0 END) AS Staged,
       SUM(CASE WHEN ProcessingStatus = 1 THEN 1 ELSE 0 END) AS Processing,
       SUM(CASE WHEN ProcessingStatus = 2 THEN 1 ELSE 0 END) AS Completed,
       SUM(CASE WHEN ProcessingStatus = 3 THEN 1 ELSE 0 END) AS Failed,
       MIN(CallDate) AS EarliestCall,
       MAX(CallDate) AS LatestCall
FROM PrivateWires
WHERE Extension IN (SELECT PhoneNumber FROM UserPhones WHERE IndexNumber = @IndexNumber AND IsActive = 1);

-- 7. Check for available (Staged or Failed) records in date range
SELECT 'Available Safaricom in Range' AS CheckType, COUNT(*) AS Count
FROM Safaricom
WHERE Ext IN (SELECT PhoneNumber FROM UserPhones WHERE IndexNumber = @IndexNumber AND IsActive = 1)
  AND call_date >= @StartDate AND call_date <= @EndDate
  AND ProcessingStatus IN (0, 3);

SELECT 'Available Airtel in Range' AS CheckType, COUNT(*) AS Count
FROM Airtel
WHERE Ext IN (SELECT PhoneNumber FROM UserPhones WHERE IndexNumber = @IndexNumber AND IsActive = 1)
  AND call_date >= @StartDate AND call_date <= @EndDate
  AND ProcessingStatus IN (0, 3);

SELECT 'Available PSTN in Range' AS CheckType, COUNT(*) AS Count
FROM PSTNs
WHERE Extension IN (SELECT PhoneNumber FROM UserPhones WHERE IndexNumber = @IndexNumber AND IsActive = 1)
  AND CallDate >= @StartDate AND CallDate <= @EndDate
  AND ProcessingStatus IN (0, 3);

SELECT 'Available PrivateWire in Range' AS CheckType, COUNT(*) AS Count
FROM PrivateWires
WHERE Extension IN (SELECT PhoneNumber FROM UserPhones WHERE IndexNumber = @IndexNumber AND IsActive = 1)
  AND CallDate >= @StartDate AND CallDate <= @EndDate
  AND ProcessingStatus IN (0, 3);
