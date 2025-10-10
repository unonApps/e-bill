-- ================================================================
-- SEED AIRTEL TABLE TEST DATA
-- ================================================================
USE TABDB;
GO

SET NOCOUNT ON;
PRINT 'Seeding Airtel table with test data...';
PRINT '';

-- Clear existing test data from Airtel
DELETE FROM Airtel WHERE ext IN ('0722111111', '0722222222', '0722333333', '0722444444', '0722555555', '0722777777');

-- Get organization and office IDs
DECLARE @UNONId INT = (SELECT TOP 1 Id FROM Organizations WHERE Code = 'UNON');
DECLARE @UNONHQId INT = (SELECT TOP 1 Id FROM Offices WHERE Code = 'UNON-HQ');
DECLARE @SubOfficeId INT = (SELECT TOP 1 Id FROM SubOffices WHERE Code = 'UNON-IT');

-- Get EbillUser IDs for the test users
DECLARE @User1Id INT = (SELECT TOP 1 Id FROM EbillUsers WHERE IndexNumber = 'TEST001');
DECLARE @User2Id INT = (SELECT TOP 1 Id FROM EbillUsers WHERE IndexNumber = 'TEST002');
DECLARE @User3Id INT = (SELECT TOP 1 Id FROM EbillUsers WHERE IndexNumber = 'TEST003');

-- Insert Airtel call records
INSERT INTO Airtel (
    ext,
    call_date,
    call_time,
    dialed,
    dest,
    durx,
    cost,
    dur,
    call_type,
    call_month,
    call_year,
    source,
    IndexNumber,
    CreatedDate,
    CreatedBy,
    EbillUserId,
    OrganizationId,
    OfficeId,
    SubOfficeId
)
VALUES
-- TEST001 (John Smith) - Normal calls
('0722111111',
 DATEADD(day, -5, GETDATE()),
 CAST('09:30:00' AS TIME),
 '0733456789',
 'Safaricom Network',
 8.5,
 42.50,
 8.5,
 'Voice',
 MONTH(GETDATE()),
 YEAR(GETDATE()),
 'Airtel',
 'TEST001',
 GETDATE(),
 'System',
 @User1Id,
 @UNONId,
 @UNONHQId,
 @SubOfficeId),

-- TEST001 - International call
('0722111111',
 DATEADD(day, -4, GETDATE()),
 CAST('14:15:00' AS TIME),
 '+254733123456',
 'Kenya Mobile',
 12.0,
 65.00,
 12.0,
 'Voice',
 MONTH(GETDATE()),
 YEAR(GETDATE()),
 'Airtel',
 'TEST001',
 GETDATE(),
 'System',
 @User1Id,
 @UNONId,
 @UNONHQId,
 @SubOfficeId),

-- TEST002 (Jane Doe) - Normal calls
('0722222222',
 DATEADD(day, -3, GETDATE()),
 CAST('11:00:00' AS TIME),
 '0700123456',
 'Telkom Network',
 5.2,
 26.00,
 5.2,
 'Voice',
 MONTH(GETDATE()),
 YEAR(GETDATE()),
 'Airtel',
 'TEST002',
 GETDATE(),
 'System',
 @User2Id,
 @UNONId,
 @UNONHQId,
 @SubOfficeId),

-- TEST002 - SMS record
('0722222222',
 DATEADD(day, -2, GETDATE()),
 CAST('15:45:00' AS TIME),
 '0711234567',
 'SMS',
 0,
 2.00,
 0,
 'SMS',
 MONTH(GETDATE()),
 YEAR(GETDATE()),
 'Airtel',
 'TEST002',
 GETDATE(),
 'System',
 @User2Id,
 @UNONId,
 @UNONHQId,
 @SubOfficeId),

-- TEST003 (Mike Johnson - Inactive user) - Should trigger anomaly
('0722333333',
 DATEADD(day, -2, GETDATE()),
 CAST('10:30:00' AS TIME),
 '+971501234567',
 'UAE International',
 25.0,
 375.00,
 25.0,
 'International',
 MONTH(GETDATE()),
 YEAR(GETDATE()),
 'Airtel',
 'TEST003',
 GETDATE(),
 'System',
 @User3Id,
 @UNONId,
 @UNONHQId,
 @SubOfficeId),

-- High-cost international call - Should trigger HIGH_COST anomaly
('0722111111',
 DATEADD(day, -1, GETDATE()),
 CAST('08:00:00' AS TIME),
 '+81901234567',
 'Japan International',
 60.0,
 25000.00, -- Very high cost (>$100 when converted)
 60.0,
 'International',
 MONTH(GETDATE()),
 YEAR(GETDATE()),
 'Airtel',
 'TEST001',
 GETDATE(),
 'System',
 @User1Id,
 @UNONId,
 @UNONHQId,
 @SubOfficeId),

-- Excessive duration call - Should trigger EXCESSIVE_DURATION anomaly
('0722222222',
 DATEADD(day, -1, GETDATE()),
 CAST('20:00:00' AS TIME),
 '0788999888',
 'Conference Call',
 300.0, -- 5 hours
 1500.00,
 300.0,
 'Voice',
 MONTH(GETDATE()),
 YEAR(GETDATE()),
 'Airtel',
 'TEST002',
 GETDATE(),
 'System',
 @User2Id,
 @UNONId,
 @UNONHQId,
 @SubOfficeId),

-- Unregistered phone number - Should trigger NO_PHONE anomaly
('0722777777',
 GETDATE(),
 CAST('09:00:00' AS TIME),
 '0711888999',
 'Unknown Mobile',
 10.0,
 50.00,
 10.0,
 'Voice',
 MONTH(GETDATE()),
 YEAR(GETDATE()),
 'Airtel',
 NULL, -- No IndexNumber
 GETDATE(),
 'System',
 NULL, -- No EbillUserId
 NULL,
 NULL,
 NULL),

-- Future date call - Should trigger FUTURE_DATE anomaly
('0722111111',
 DATEADD(day, 5, GETDATE()), -- Future date
 CAST('12:00:00' AS TIME),
 '0799123456',
 'Future Call',
 7.5,
 35.00,
 7.5,
 'Voice',
 MONTH(DATEADD(day, 5, GETDATE())),
 YEAR(DATEADD(day, 5, GETDATE())),
 'Airtel',
 'TEST001',
 GETDATE(),
 'System',
 @User1Id,
 @UNONId,
 @UNONHQId,
 @SubOfficeId),

-- Data usage record
('0722111111',
 DATEADD(day, -2, GETDATE()),
 CAST('00:00:00' AS TIME),
 'DATA',
 'Internet Bundle',
 0,
 100.00,
 0,
 'Data',
 MONTH(GETDATE()),
 YEAR(GETDATE()),
 'Airtel',
 'TEST001',
 GETDATE(),
 'System',
 @User1Id,
 @UNONId,
 @UNONHQId,
 @SubOfficeId);

PRINT 'Created 10 Airtel call records with various test scenarios:';
PRINT '  - Normal voice calls';
PRINT '  - SMS records';
PRINT '  - Data usage';
PRINT '  - International calls';
PRINT '  - High-cost call (KSH 25,000 = ~$166)';
PRINT '  - Excessive duration (5 hours)';
PRINT '  - Call from inactive user (TEST003)';
PRINT '  - Call from unregistered number';
PRINT '  - Future-dated call';
PRINT '';

-- Show summary
SELECT
    COUNT(*) as TotalRecords,
    COUNT(DISTINCT ext) as UniqueNumbers,
    SUM(cost) as TotalCost,
    AVG(dur) as AvgDuration,
    MIN(call_date) as EarliestCall,
    MAX(call_date) as LatestCall
FROM Airtel
WHERE ext IN ('0722111111', '0722222222', '0722333333', '0722777777');

PRINT '';
PRINT 'Airtel test data ready for consolidation testing.';
PRINT 'These records will be imported when you run Call Log Consolidation.';