-- ================================================================
-- MINIMAL SEED TEST DATA FOR CALL LOG STAGING
-- ================================================================
USE TABDB;
GO

SET NOCOUNT ON;
PRINT 'Starting minimal test data seed...';
PRINT '';

-- ================================================================
-- 1. ORGANIZATIONS
-- ================================================================
IF NOT EXISTS (SELECT 1 FROM Organizations WHERE Code = 'UNON')
BEGIN
    INSERT INTO Organizations (Code, Name, Description, CreatedDate)
    VALUES ('UNON', 'United Nations Office at Nairobi', 'UN headquarters in Africa', GETDATE());
    PRINT 'Created UNON organization';
END

-- ================================================================
-- 2. OFFICES
-- ================================================================
DECLARE @UNONId INT = (SELECT TOP 1 Id FROM Organizations WHERE Code = 'UNON');

IF @UNONId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM Offices WHERE Code = 'UNON-HQ')
BEGIN
    INSERT INTO Offices (OrganizationId, Code, Name, Description, CreatedDate)
    VALUES (@UNONId, 'UNON-HQ', 'UNON Headquarters', 'Main office', GETDATE());
    PRINT 'Created UNON-HQ office';
END

-- ================================================================
-- 3. SUBOFFICES
-- ================================================================
DECLARE @UNONHQId INT = (SELECT TOP 1 Id FROM Offices WHERE Code = 'UNON-HQ');

IF @UNONHQId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM SubOffices WHERE Code = 'UNON-IT')
BEGIN
    INSERT INTO SubOffices (OfficeId, Code, Name, Description, CreatedDate)
    VALUES (@UNONHQId, 'UNON-IT', 'Information Technology', 'IT Division', GETDATE());
    PRINT 'Created UNON-IT suboffice';
END

-- ================================================================
-- 4. EBILLUSERS
-- ================================================================
DECLARE @SubOfficeId INT = (SELECT TOP 1 Id FROM SubOffices WHERE Code = 'UNON-IT');

-- Clear test users
DELETE FROM UserPhones WHERE IndexNumber IN ('TEST001', 'TEST002', 'TEST003');
DELETE FROM EbillUsers WHERE IndexNumber IN ('TEST001', 'TEST002', 'TEST003');

-- Insert 3 test users
INSERT INTO EbillUsers (
    IndexNumber, FirstName, LastName, Email, OfficialMobileNumber,
    OrganizationId, OfficeId, SubOfficeId, Location,
    ClassOfService, IsActive, CreatedDate
)
VALUES
('TEST001', 'John', 'Smith', 'john.smith@un.org', '0722111111',
 @UNONId, @UNONHQId, @SubOfficeId, 'Gigiri',
 'STANDARD', 1, GETDATE()),

('TEST002', 'Jane', 'Doe', 'jane.doe@un.org', '0722222222',
 @UNONId, @UNONHQId, @SubOfficeId, 'Gigiri',
 'BASIC', 1, GETDATE()),

('TEST003', 'Mike', 'Johnson', 'mike.j@un.org', '0722333333',
 @UNONId, @UNONHQId, @SubOfficeId, 'Gigiri',
 'EXECUTIVE', 0, GETDATE()); -- Inactive user

PRINT 'Created 3 test EbillUsers';

-- ================================================================
-- 5. USERPHONES
-- ================================================================
INSERT INTO UserPhones (
    IndexNumber, PhoneNumber, PhoneType, IsPrimary, IsActive,
    AssignedDate, Location, Notes, CreatedDate
)
VALUES
('TEST001', '0722111111', 'Mobile', 1, 1, GETDATE(), 'Gigiri', 'Primary', GETDATE()),
('TEST001', '2501', 'Extension', 0, 1, GETDATE(), 'Office', 'Desk', GETDATE()),
('TEST002', '0722222222', 'Mobile', 1, 1, GETDATE(), 'Gigiri', 'Primary', GETDATE()),
('TEST003', '0722333333', 'Mobile', 1, 0, GETDATE(), 'Gigiri', 'Inactive', GETDATE());

PRINT 'Created UserPhone records';

-- ================================================================
-- 6. SAMPLE CALL LOGS
-- ================================================================
-- Clear test data
DELETE FROM Safaricom WHERE Ext IN ('0722111111', '0722222222', '0722333333', '0722666666');
DELETE FROM PSTNs WHERE Extension IN ('2501', '2502', '2506');

-- Safaricom calls
INSERT INTO Safaricom (Ext, Dialed, Dest, Dur, Cost, IndexNumber, CreatedDate)
VALUES
-- Normal call
('0722111111', '0733123456', 'Safaricom', 5.5, 25.00, 'TEST001', GETDATE()),
-- High cost call (>$100)
('0722111111', '+1234567890', 'USA', 15.0, 18000.00, 'TEST001', GETDATE()),
-- Call from inactive user
('0722333333', '0701234567', 'Safaricom', 4.0, 18.00, 'TEST003', GETDATE()),
-- Unregistered phone
('0722666666', '0712345678', 'Airtel', 7.0, 32.00, NULL, GETDATE());

PRINT 'Created 4 Safaricom test calls';

-- PSTN calls
INSERT INTO PSTNs (Extension, DialedNumber, Destination, Duration, AmountKSH, IndexNumber, Location, CreatedDate)
VALUES
('2501', '2502', 'Internal', 2.5, 0.00, 'TEST001', 'Gigiri', GETDATE()),
('2506', '0722111111', 'Mobile', 3.5, 22.00, NULL, 'Unknown', GETDATE());

PRINT 'Created 2 PSTN test calls';

PRINT '';
PRINT '================================================================';
PRINT 'TEST DATA READY';
PRINT '================================================================';
PRINT 'Created:';
PRINT '- 3 Users (2 active, 1 inactive)';
PRINT '- 4 UserPhones';
PRINT '- 4 Safaricom calls (including anomalies)';
PRINT '- 2 PSTN calls';
PRINT '';
PRINT 'Test scenarios:';
PRINT '- Normal calls';
PRINT '- High cost call ($120)';
PRINT '- Inactive user call';
PRINT '- Unregistered phone call';
PRINT '';
PRINT 'Navigate to /Admin/CallLogStaging to test consolidation';
PRINT '';