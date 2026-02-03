-- ================================================================
-- SEED TEST DATA FOR CALL LOG STAGING SYSTEM
-- ================================================================
-- This script creates minimal test data to verify the call log staging workflow
-- Order: Class of Service -> Organizations -> Offices -> SubOffices -> EbillUsers -> UserPhones -> Call Logs

USE TABDB;
GO

SET NOCOUNT ON;
PRINT '================================================================';
PRINT 'SEEDING TEST DATA FOR CALL LOG STAGING';
PRINT '================================================================';
PRINT '';

-- ================================================================
-- 1. SEED CLASS OF SERVICE
-- ================================================================
PRINT '1. Creating Class of Service entries...';

-- Clear existing data (optional - comment out if you want to keep existing data)
DELETE FROM ClassOfService WHERE Id IN (101, 102, 103);

-- Insert test Class of Service entries
SET IDENTITY_INSERT ClassOfService ON;

INSERT INTO ClassOfService (Id, ServiceCode, ServiceName, Description, MonthlyAllowance, InternationalCallsAllowed, DataAllowanceGB, HandsetAllowance, IsActive, CreatedDate)
VALUES
(101, 'BASIC', 'Basic Plan', 'Basic Staff phone plan', 50.00, 0, 5, 0, 1, GETDATE()),
(102, 'STANDARD', 'Standard Plan', 'Standard Staff phone plan with international', 150.00, 1, 15, 500.00, 1, GETDATE()),
(103, 'EXECUTIVE', 'Executive Plan', 'Executive unlimited plan', 500.00, 1, 50, 1500.00, 1, GETDATE());

SET IDENTITY_INSERT ClassOfService OFF;

PRINT '   Created 3 Class of Service entries';
PRINT '';

-- ================================================================
-- 2. SEED ORGANIZATIONS
-- ================================================================
PRINT '2. Creating Organizations...';

-- Check if organizations already exist
IF NOT EXISTS (SELECT 1 FROM Organizations WHERE Code = 'UNON')
BEGIN
    INSERT INTO Organizations (Code, Name, Description, IsActive, CreatedDate)
    VALUES ('UNON', 'United Nations Office at Nairobi', 'UN headquarters in Africa', 1, GETDATE());
    PRINT '   Created UNON organization';
END
ELSE
    PRINT '   UNON organization already exists';

IF NOT EXISTS (SELECT 1 FROM Organizations WHERE Code = 'UNEP')
BEGIN
    INSERT INTO Organizations (Code, Name, Description, IsActive, CreatedDate)
    VALUES ('UNEP', 'United Nations Environment Programme', 'UN Environment Programme', 1, GETDATE());
    PRINT '   Created UNEP organization';
END
ELSE
    PRINT '   UNEP organization already exists';

PRINT '';

-- ================================================================
-- 3. SEED OFFICES
-- ================================================================
PRINT '3. Creating Offices...';

DECLARE @UNONId INT = (SELECT Id FROM Organizations WHERE Code = 'UNON');
DECLARE @UNEPId INT = (SELECT Id FROM Organizations WHERE Code = 'UNEP');

-- UNON Offices
IF NOT EXISTS (SELECT 1 FROM Offices WHERE Code = 'UNON-HQ' AND OrganizationId = @UNONId)
BEGIN
    INSERT INTO Offices (OrganizationId, Code, Name, Description, Location, IsActive, CreatedDate)
    VALUES (@UNONId, 'UNON-HQ', 'UNON Headquarters', 'Main office in Gigiri', 'Nairobi, Kenya', 1, GETDATE());
    PRINT '   Created UNON-HQ office';
END

-- UNEP Offices
IF NOT EXISTS (SELECT 1 FROM Offices WHERE Code = 'UNEP-HQ' AND OrganizationId = @UNEPId)
BEGIN
    INSERT INTO Offices (OrganizationId, Code, Name, Description, Location, IsActive, CreatedDate)
    VALUES (@UNEPId, 'UNEP-HQ', 'UNEP Headquarters', 'UNEP main office', 'Nairobi, Kenya', 1, GETDATE());
    PRINT '   Created UNEP-HQ office';
END

PRINT '';

-- ================================================================
-- 4. SEED SUBOFFICES
-- ================================================================
PRINT '4. Creating SubOffices...';

DECLARE @UNONHQId INT = (SELECT Id FROM Offices WHERE Code = 'UNON-HQ');
DECLARE @UNEPHQId INT = (SELECT Id FROM Offices WHERE Code = 'UNEP-HQ');

-- UNON SubOffices
IF NOT EXISTS (SELECT 1 FROM SubOffices WHERE Code = 'UNON-IT' AND OfficeId = @UNONHQId)
BEGIN
    INSERT INTO SubOffices (OfficeId, Code, Name, Description, IsActive, CreatedDate)
    VALUES (@UNONHQId, 'UNON-IT', 'Information Technology', 'IT Services Division', 1, GETDATE());
    PRINT '   Created UNON-IT suboffice';
END

IF NOT EXISTS (SELECT 1 FROM SubOffices WHERE Code = 'UNON-FIN' AND OfficeId = @UNONHQId)
BEGIN
    INSERT INTO SubOffices (OfficeId, Code, Name, Description, IsActive, CreatedDate)
    VALUES (@UNONHQId, 'UNON-FIN', 'Finance Division', 'Financial Services', 1, GETDATE());
    PRINT '   Created UNON-FIN suboffice';
END

-- UNEP SubOffices
IF NOT EXISTS (SELECT 1 FROM SubOffices WHERE Code = 'UNEP-PROG' AND OfficeId = @UNEPHQId)
BEGIN
    INSERT INTO SubOffices (OfficeId, Code, Name, Description, IsActive, CreatedDate)
    VALUES (@UNEPHQId, 'UNEP-PROG', 'Programme Division', 'Programme Management', 1, GETDATE());
    PRINT '   Created UNEP-PROG suboffice';
END

PRINT '';

-- ================================================================
-- 5. SEED EBILLUSERS
-- ================================================================
PRINT '5. Creating EbillUsers...';

DECLARE @UNON_IT_Id INT = (SELECT Id FROM SubOffices WHERE Code = 'UNON-IT');
DECLARE @UNON_FIN_Id INT = (SELECT Id FROM SubOffices WHERE Code = 'UNON-FIN');
DECLARE @UNEP_PROG_Id INT = (SELECT Id FROM SubOffices WHERE Code = 'UNEP-PROG');

-- Clear test users if they exist
DELETE FROM EbillUsers WHERE IndexNumber IN ('TEST001', 'TEST002', 'TEST003', 'TEST004', 'TEST005');

-- Insert test EbillUsers
INSERT INTO EbillUsers (
    IndexNumber, FirstName, LastName, Email, StaffType, StaffCategory,
    OrganizationId, OfficeId, SubOfficeId, Location,
    Extension, MobileNumber, DirectLine, SIMNumbers,
    ClassOfServiceId, IsActive, CreatedDate
)
VALUES
-- UNON IT Staff
('TEST001', 'John', 'Smith', 'john.smith@un.org', 'Staff', 'Professional',
 @UNONId, @UNONHQId, @UNON_IT_Id, 'Gigiri Campus',
 '2501', '0722111111', '+254202501', 'SIM001', 102, 1, GETDATE()),

('TEST002', 'Jane', 'Doe', 'jane.doe@un.org', 'Staff', 'Professional',
 @UNONId, @UNONHQId, @UNON_IT_Id, 'Gigiri Campus',
 '2502', '0722222222', '+254202502', 'SIM002', 101, 1, GETDATE()),

-- UNON Finance Staff
('TEST003', 'Michael', 'Johnson', 'michael.johnson@un.org', 'Staff', 'Director',
 @UNONId, @UNONHQId, @UNON_FIN_Id, 'Gigiri Campus',
 '2503', '0722333333', '+254202503', 'SIM003', 103, 1, GETDATE()),

-- UNEP Programme Staff
('TEST004', 'Sarah', 'Williams', 'sarah.williams@un.org', 'Staff', 'Professional',
 @UNEPId, @UNEPHQId, @UNEP_PROG_Id, 'Gigiri Campus',
 '2504', '0722444444', '+254202504', 'SIM004', 102, 1, GETDATE()),

-- Inactive user for testing
('TEST005', 'Robert', 'Brown', 'robert.brown@un.org', 'Consultant', 'Consultant',
 @UNONId, @UNONHQId, @UNON_IT_Id, 'Gigiri Campus',
 '2505', '0722555555', '+254202505', 'SIM005', 101, 0, GETDATE());

PRINT '   Created 5 test EbillUsers (4 active, 1 inactive)';
PRINT '';

-- ================================================================
-- 6. SEED USERPHONES
-- ================================================================
PRINT '6. Creating UserPhones...';

-- Clear existing test UserPhones
DELETE FROM UserPhones WHERE IndexNumber IN ('TEST001', 'TEST002', 'TEST003', 'TEST004', 'TEST005');

-- Insert UserPhones (multiple phones per user for testing)
INSERT INTO UserPhones (
    IndexNumber, PhoneNumber, PhoneType, IsPrimary, IsActive,
    AssignedDate, Location, Notes, CreatedBy, CreatedDate
)
VALUES
-- John Smith - 2 phones
('TEST001', '0722111111', 'Mobile', 1, 1, DATEADD(month, -6, GETDATE()), 'Gigiri', 'Primary mobile', 'System', GETDATE()),
('TEST001', '2501', 'Extension', 0, 1, DATEADD(month, -6, GETDATE()), 'Office Block A', 'Desk extension', 'System', GETDATE()),

-- Jane Doe - 1 phone
('TEST002', '0722222222', 'Mobile', 1, 1, DATEADD(month, -3, GETDATE()), 'Gigiri', 'Company mobile', 'System', GETDATE()),
('TEST002', '2502', 'Extension', 0, 1, DATEADD(month, -3, GETDATE()), 'Office Block A', 'Desk phone', 'System', GETDATE()),

-- Michael Johnson - 3 phones (director with multiple lines)
('TEST003', '0722333333', 'Mobile', 1, 1, DATEADD(year, -1, GETDATE()), 'Gigiri', 'Executive mobile', 'System', GETDATE()),
('TEST003', '2503', 'Extension', 0, 1, DATEADD(year, -1, GETDATE()), 'Executive Wing', 'Office extension', 'System', GETDATE()),
('TEST003', '+254202503', 'Direct Line', 0, 1, DATEADD(year, -1, GETDATE()), 'Executive Wing', 'Direct line', 'System', GETDATE()),

-- Sarah Williams - 2 phones
('TEST004', '0722444444', 'Mobile', 1, 1, DATEADD(month, -4, GETDATE()), 'Gigiri', 'Programme mobile', 'System', GETDATE()),
('TEST004', '2504', 'Extension', 0, 1, DATEADD(month, -4, GETDATE()), 'UNEP Building', 'Desk extension', 'System', GETDATE()),

-- Robert Brown - Inactive user but with phone history
('TEST005', '0722555555', 'Mobile', 1, 0, DATEADD(month, -1, GETDATE()), 'Gigiri', 'Deactivated - user left', 'System', GETDATE());

PRINT '   Created 10 UserPhone records for 5 users';
PRINT '';

-- ================================================================
-- 7. SEED SAMPLE CALL LOGS IN SOURCE TABLES
-- ================================================================
PRINT '7. Creating sample call logs in source tables...';

-- Get current month dates
DECLARE @StartDate DATETIME = DATEADD(day, -7, GETDATE());
DECLARE @EndDate DATETIME = GETDATE();

-- Clear existing test data
DELETE FROM Safaricom WHERE Ext IN ('0722111111', '0722222222', '0722333333', '0722444444', '0722555555', '0722666666');
DELETE FROM PSTN WHERE Extension IN ('2501', '2502', '2503', '2504', '2505', '2506');

-- Insert Safaricom mobile calls
INSERT INTO Safaricom (Ext, CallDate, CallTime, Dialed, Dest, Dur, Cost, CallType, CallMonth, CallYear, IndexNumber, CreatedDate)
VALUES
-- John's mobile calls
('0722111111', DATEADD(day, -6, GETDATE()), CAST('10:30:00' AS TIME), '0733123456', 'Safaricom Mobile', 5.5, 25.00, 'Voice', MONTH(GETDATE()), YEAR(GETDATE()), 'TEST001', GETDATE()),
('0722111111', DATEADD(day, -5, GETDATE()), CAST('14:15:00' AS TIME), '+1234567890', 'USA', 15.0, 450.00, 'International', MONTH(GETDATE()), YEAR(GETDATE()), 'TEST001', GETDATE()),

-- Jane's mobile calls
('0722222222', DATEADD(day, -4, GETDATE()), CAST('09:00:00' AS TIME), '0712987654', 'Airtel Mobile', 3.0, 15.00, 'Voice', MONTH(GETDATE()), YEAR(GETDATE()), 'TEST002', GETDATE()),
('0722222222', DATEADD(day, -3, GETDATE()), CAST('16:45:00' AS TIME), '0700555888', 'Safaricom Mobile', 8.2, 35.00, 'Voice', MONTH(GETDATE()), YEAR(GETDATE()), 'TEST002', GETDATE()),

-- Michael's mobile calls (high-cost for testing)
('0722333333', DATEADD(day, -2, GETDATE()), CAST('11:20:00' AS TIME), '+44789012345', 'UK', 45.0, 1500.00, 'International', MONTH(GETDATE()), YEAR(GETDATE()), 'TEST003', GETDATE()),
('0722333333', DATEADD(day, -1, GETDATE()), CAST('13:00:00' AS TIME), '0711234567', 'Airtel Mobile', 12.5, 55.00, 'Voice', MONTH(GETDATE()), YEAR(GETDATE()), 'TEST003', GETDATE()),

-- Sarah's mobile calls
('0722444444', DATEADD(day, -3, GETDATE()), CAST('08:30:00' AS TIME), '0723456789', 'Safaricom Mobile', 6.0, 28.00, 'Voice', MONTH(GETDATE()), YEAR(GETDATE()), 'TEST004', GETDATE()),

-- Inactive user call (for anomaly detection)
('0722555555', DATEADD(day, -1, GETDATE()), CAST('10:00:00' AS TIME), '0701234567', 'Safaricom Mobile', 4.0, 18.00, 'Voice', MONTH(GETDATE()), YEAR(GETDATE()), 'TEST005', GETDATE()),

-- Unregistered phone call (for anomaly detection)
('0722666666', DATEADD(day, -1, GETDATE()), CAST('15:30:00' AS TIME), '0712345678', 'Airtel Mobile', 7.0, 32.00, 'Voice', MONTH(GETDATE()), YEAR(GETDATE()), NULL, GETDATE());

PRINT '   Created 9 Safaricom call records';

-- Insert PSTN desk phone calls
INSERT INTO PSTNs (Extension, CallDate, CallTime, DialedNumber, Destination, Duration, AmountKSH, IndexNumber, Location, CreatedDate)
VALUES
-- Extension calls
('2501', DATEADD(day, -5, GETDATE()), CAST('11:00:00' AS TIME), '2502', 'Internal', 2.5, 0.00, 'TEST001', 'Gigiri', GETDATE()),
('2502', DATEADD(day, -4, GETDATE()), CAST('10:15:00' AS TIME), '+254202504', 'Local Landline', 8.0, 45.00, 'TEST002', 'Gigiri', GETDATE()),
('2503', DATEADD(day, -3, GETDATE()), CAST('09:30:00' AS TIME), '+33123456789', 'France', 25.0, 850.00, 'TEST003', 'Gigiri', GETDATE()),
('2504', DATEADD(day, -2, GETDATE()), CAST('14:20:00' AS TIME), '2501', 'Internal', 5.0, 0.00, 'TEST004', 'UNEP Building', GETDATE()),

-- Unregistered extension (anomaly test)
('2506', DATEADD(day, -1, GETDATE()), CAST('12:00:00' AS TIME), '0722111111', 'Safaricom Mobile', 3.5, 22.00, NULL, 'Unknown', GETDATE());

PRINT '   Created 5 PSTN call records';

-- Insert a future date call for anomaly testing
INSERT INTO Safaricom (Ext, CallDate, CallTime, Dialed, Dest, Dur, Cost, CallType, CallMonth, CallYear, IndexNumber, CreatedDate)
VALUES
('0722111111', DATEADD(day, 7, GETDATE()), CAST('10:00:00' AS TIME), '0700123456', 'Future Call', 5.0, 25.00, 'Voice', MONTH(GETDATE()), YEAR(GETDATE()), 'TEST001', GETDATE());

PRINT '   Created 1 future-dated call for anomaly testing';

PRINT '';
PRINT '================================================================';
PRINT 'TEST DATA SEEDING COMPLETED';
PRINT '================================================================';
PRINT '';
PRINT 'Summary:';
PRINT '--------';
PRINT '✓ 3 Class of Service plans created';
PRINT '✓ 2 Organizations (UNON, UNEP)';
PRINT '✓ 2 Offices (UNON-HQ, UNEP-HQ)';
PRINT '✓ 3 SubOffices (IT, Finance, Programme)';
PRINT '✓ 5 EbillUsers (4 active, 1 inactive)';
PRINT '✓ 10 UserPhone records';
PRINT '✓ 10 Safaricom call records';
PRINT '✓ 5 PSTN call records';
PRINT '';
PRINT 'Test Scenarios Included:';
PRINT '------------------------';
PRINT '• Normal calls from registered users';
PRINT '• High-cost international calls (>$100)';
PRINT '• Calls from inactive users';
PRINT '• Calls from unregistered phone numbers';
PRINT '• Future-dated calls';
PRINT '• Internal extension-to-extension calls';
PRINT '';
PRINT 'Next Steps:';
PRINT '-----------';
PRINT '1. Navigate to Admin > Call Log Staging';
PRINT '2. Click "Consolidate Call Logs"';
PRINT '3. Select date range to include test data';
PRINT '4. Review staged records and anomalies';
PRINT '5. Verify or reject records as needed';
PRINT '';