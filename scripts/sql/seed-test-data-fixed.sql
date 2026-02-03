-- ================================================================
-- SEED TEST DATA FOR CALL LOG STAGING SYSTEM (FIXED)
-- ================================================================
-- This script creates minimal test data to verify the call log staging workflow

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

-- Check if ClassOfService entries exist
IF NOT EXISTS (SELECT 1 FROM ClassOfService WHERE ServiceCode = 'BASIC')
BEGIN
    INSERT INTO ClassOfService (ServiceCode, ServiceName, Description, MonthlyAllowance, InternationalCallsAllowed, DataAllowanceGB, HandsetAllowance, IsActive, CreatedDate)
    VALUES ('BASIC', 'Basic Plan', 'Basic Staff phone plan', 50.00, 0, 5, 0, 1, GETDATE());
    PRINT '   Created BASIC Class of Service';
END

IF NOT EXISTS (SELECT 1 FROM ClassOfService WHERE ServiceCode = 'STANDARD')
BEGIN
    INSERT INTO ClassOfService (ServiceCode, ServiceName, Description, MonthlyAllowance, InternationalCallsAllowed, DataAllowanceGB, HandsetAllowance, IsActive, CreatedDate)
    VALUES ('STANDARD', 'Standard Plan', 'Standard Staff phone plan with international', 150.00, 1, 15, 500.00, 1, GETDATE());
    PRINT '   Created STANDARD Class of Service';
END

IF NOT EXISTS (SELECT 1 FROM ClassOfService WHERE ServiceCode = 'EXECUTIVE')
BEGIN
    INSERT INTO ClassOfService (ServiceCode, ServiceName, Description, MonthlyAllowance, InternationalCallsAllowed, DataAllowanceGB, HandsetAllowance, IsActive, CreatedDate)
    VALUES ('EXECUTIVE', 'Executive Plan', 'Executive unlimited plan', 500.00, 1, 50, 1500.00, 1, GETDATE());
    PRINT '   Created EXECUTIVE Class of Service';
END

PRINT '';

-- ================================================================
-- 2. SEED ORGANIZATIONS
-- ================================================================
PRINT '2. Creating Organizations...';

IF NOT EXISTS (SELECT 1 FROM Organizations WHERE Code = 'UNON')
BEGIN
    INSERT INTO Organizations (Code, Name, Description, IsActive, CreatedDate)
    VALUES ('UNON', 'United Nations Office at Nairobi', 'UN headquarters in Africa', 1, GETDATE());
    PRINT '   Created UNON organization';
END

IF NOT EXISTS (SELECT 1 FROM Organizations WHERE Code = 'UNEP')
BEGIN
    INSERT INTO Organizations (Code, Name, Description, IsActive, CreatedDate)
    VALUES ('UNEP', 'United Nations Environment Programme', 'UN Environment Programme', 1, GETDATE());
    PRINT '   Created UNEP organization';
END

PRINT '';

-- ================================================================
-- 3. SEED OFFICES
-- ================================================================
PRINT '3. Creating Offices...';

DECLARE @UNONId INT = (SELECT TOP 1 Id FROM Organizations WHERE Code = 'UNON');
DECLARE @UNEPId INT = (SELECT TOP 1 Id FROM Organizations WHERE Code = 'UNEP');

IF @UNONId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM Offices WHERE Code = 'UNON-HQ')
BEGIN
    INSERT INTO Offices (OrganizationId, Code, Name, Description, ContactPerson, PhoneNumber, Email, Address, IsActive, CreatedDate)
    VALUES (@UNONId, 'UNON-HQ', 'UNON Headquarters', 'Main office in Gigiri', 'Admin', '+254207621234', 'admin@unon.org', 'UN Avenue, Gigiri', 1, GETDATE());
    PRINT '   Created UNON-HQ office';
END

IF @UNEPId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM Offices WHERE Code = 'UNEP-HQ')
BEGIN
    INSERT INTO Offices (OrganizationId, Code, Name, Description, ContactPerson, PhoneNumber, Email, Address, IsActive, CreatedDate)
    VALUES (@UNEPId, 'UNEP-HQ', 'UNEP Headquarters', 'UNEP main office', 'Admin', '+254207625678', 'admin@unep.org', 'UN Avenue, Gigiri', 1, GETDATE());
    PRINT '   Created UNEP-HQ office';
END

PRINT '';

-- ================================================================
-- 4. SEED SUBOFFICES
-- ================================================================
PRINT '4. Creating SubOffices...';

DECLARE @UNONHQId INT = (SELECT TOP 1 Id FROM Offices WHERE Code = 'UNON-HQ');
DECLARE @UNEPHQId INT = (SELECT TOP 1 Id FROM Offices WHERE Code = 'UNEP-HQ');

IF @UNONHQId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM SubOffices WHERE Code = 'UNON-IT')
BEGIN
    INSERT INTO SubOffices (OfficeId, Code, Name, Description, IsActive, CreatedDate)
    VALUES (@UNONHQId, 'UNON-IT', 'Information Technology', 'IT Services Division', 1, GETDATE());
    PRINT '   Created UNON-IT suboffice';
END

IF @UNONHQId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM SubOffices WHERE Code = 'UNON-FIN')
BEGIN
    INSERT INTO SubOffices (OfficeId, Code, Name, Description, IsActive, CreatedDate)
    VALUES (@UNONHQId, 'UNON-FIN', 'Finance Division', 'Financial Services', 1, GETDATE());
    PRINT '   Created UNON-FIN suboffice';
END

IF @UNEPHQId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM SubOffices WHERE Code = 'UNEP-PROG')
BEGIN
    INSERT INTO SubOffices (OfficeId, Code, Name, Description, IsActive, CreatedDate)
    VALUES (@UNEPHQId, 'UNEP-PROG', 'Programme Division', 'Programme Management', 1, GETDATE());
    PRINT '   Created UNEP-PROG suboffice';
END

PRINT '';

-- ================================================================
-- 5. SEED EBILLUSERS (Using actual column names)
-- ================================================================
PRINT '5. Creating EbillUsers...';

DECLARE @UNON_IT_Id INT = (SELECT TOP 1 Id FROM SubOffices WHERE Code = 'UNON-IT');
DECLARE @UNON_FIN_Id INT = (SELECT TOP 1 Id FROM SubOffices WHERE Code = 'UNON-FIN');
DECLARE @UNEP_PROG_Id INT = (SELECT TOP 1 Id FROM SubOffices WHERE Code = 'UNEP-PROG');

-- Clear test users if they exist
DELETE FROM EbillUsers WHERE IndexNumber IN ('TEST001', 'TEST002', 'TEST003', 'TEST004', 'TEST005');

-- Insert test EbillUsers
INSERT INTO EbillUsers (
    IndexNumber, FirstName, LastName, Email, OfficialMobileNumber,
    OrganizationId, OfficeId, SubOfficeId, Location,
    ClassOfService, IsActive, CreatedDate
)
VALUES
('TEST001', 'John', 'Smith', 'john.smith@un.org', '0722111111',
 @UNONId, @UNONHQId, @UNON_IT_Id, 'Gigiri Campus',
 'STANDARD', 1, GETDATE()),

('TEST002', 'Jane', 'Doe', 'jane.doe@un.org', '0722222222',
 @UNONId, @UNONHQId, @UNON_IT_Id, 'Gigiri Campus',
 'BASIC', 1, GETDATE()),

('TEST003', 'Michael', 'Johnson', 'michael.johnson@un.org', '0722333333',
 @UNONId, @UNONHQId, @UNON_FIN_Id, 'Gigiri Campus',
 'EXECUTIVE', 1, GETDATE()),

('TEST004', 'Sarah', 'Williams', 'sarah.williams@un.org', '0722444444',
 @UNEPId, @UNEPHQId, @UNEP_PROG_Id, 'Gigiri Campus',
 'STANDARD', 1, GETDATE()),

('TEST005', 'Robert', 'Brown', 'robert.brown@un.org', '0722555555',
 @UNONId, @UNONHQId, @UNON_IT_Id, 'Gigiri Campus',
 'BASIC', 0, GETDATE());

PRINT '   Created 5 test EbillUsers (4 active, 1 inactive)';
PRINT '';

-- ================================================================
-- 6. SEED USERPHONES
-- ================================================================
PRINT '6. Creating UserPhones...';

-- Clear existing test UserPhones
DELETE FROM UserPhones WHERE IndexNumber IN ('TEST001', 'TEST002', 'TEST003', 'TEST004', 'TEST005');

-- Insert UserPhones
INSERT INTO UserPhones (
    IndexNumber, PhoneNumber, PhoneType, IsPrimary, IsActive,
    AssignedDate, Location, Notes, CreatedBy, CreatedDate
)
VALUES
('TEST001', '0722111111', 'Mobile', 1, 1, DATEADD(month, -6, GETDATE()), 'Gigiri', 'Primary mobile', 'System', GETDATE()),
('TEST001', '2501', 'Extension', 0, 1, DATEADD(month, -6, GETDATE()), 'Office Block A', 'Desk extension', 'System', GETDATE()),
('TEST002', '0722222222', 'Mobile', 1, 1, DATEADD(month, -3, GETDATE()), 'Gigiri', 'Company mobile', 'System', GETDATE()),
('TEST002', '2502', 'Extension', 0, 1, DATEADD(month, -3, GETDATE()), 'Office Block A', 'Desk phone', 'System', GETDATE()),
('TEST003', '0722333333', 'Mobile', 1, 1, DATEADD(year, -1, GETDATE()), 'Gigiri', 'Executive mobile', 'System', GETDATE()),
('TEST003', '2503', 'Extension', 0, 1, DATEADD(year, -1, GETDATE()), 'Executive Wing', 'Office extension', 'System', GETDATE()),
('TEST004', '0722444444', 'Mobile', 1, 1, DATEADD(month, -4, GETDATE()), 'Gigiri', 'Programme mobile', 'System', GETDATE()),
('TEST004', '2504', 'Extension', 0, 1, DATEADD(month, -4, GETDATE()), 'UNEP Building', 'Desk extension', 'System', GETDATE()),
('TEST005', '0722555555', 'Mobile', 1, 0, DATEADD(month, -1, GETDATE()), 'Gigiri', 'Deactivated - user left', 'System', GETDATE());

PRINT '   Created 9 UserPhone records for 5 users';
PRINT '';

-- ================================================================
-- 7. SEED SAMPLE CALL LOGS IN SAFARICOM TABLE
-- ================================================================
PRINT '7. Creating sample Safaricom call logs...';

-- Clear existing test data
DELETE FROM Safaricom WHERE Ext IN ('0722111111', '0722222222', '0722333333', '0722444444', '0722555555', '0722666666');

-- Insert Safaricom calls (using actual column names)
INSERT INTO Safaricom (Ext, Dialed, Dest, Dur, Cost, IndexNumber, CreatedDate)
VALUES
-- John's calls
('0722111111', '0733123456', 'Safaricom Mobile', 5.5, 25.00, 'TEST001', GETDATE()),
('0722111111', '+1234567890', 'USA International', 15.0, 450.00, 'TEST001', GETDATE()),

-- Jane's calls
('0722222222', '0712987654', 'Airtel Mobile', 3.0, 15.00, 'TEST002', GETDATE()),
('0722222222', '0700555888', 'Safaricom Mobile', 8.2, 35.00, 'TEST002', GETDATE()),

-- Michael's calls (including high-cost)
('0722333333', '+44789012345', 'UK International', 45.0, 1500.00, 'TEST003', GETDATE()),
('0722333333', '0711234567', 'Airtel Mobile', 12.5, 55.00, 'TEST003', GETDATE()),

-- Sarah's calls
('0722444444', '0723456789', 'Safaricom Mobile', 6.0, 28.00, 'TEST004', GETDATE()),

-- Inactive user call (anomaly test)
('0722555555', '0701234567', 'Safaricom Mobile', 4.0, 18.00, 'TEST005', GETDATE()),

-- Unregistered phone (anomaly test)
('0722666666', '0712345678', 'Airtel Mobile', 7.0, 32.00, NULL, GETDATE());

PRINT '   Created 9 Safaricom call records';
PRINT '';

-- ================================================================
-- 8. SEED SAMPLE CALL LOGS IN PSTN TABLE
-- ================================================================
PRINT '8. Creating sample PSTN call logs...';

-- Clear existing test data
DELETE FROM PSTNs WHERE Extension IN ('2501', '2502', '2503', '2504', '2505', '2506');

-- Insert PSTN calls (using actual column names)
INSERT INTO PSTNs (Extension, DialedNumber, Destination, Duration, AmountKSH, IndexNumber, Location, CreatedDate)
VALUES
('2501', '2502', 'Internal', 2.5, 0.00, 'TEST001', 'Gigiri', GETDATE()),
('2502', '+254202504', 'Local Landline', 8.0, 45.00, 'TEST002', 'Gigiri', GETDATE()),
('2503', '+33123456789', 'France International', 25.0, 850.00, 'TEST003', 'Gigiri', GETDATE()),
('2504', '2501', 'Internal', 5.0, 0.00, 'TEST004', 'UNEP Building', GETDATE()),
('2506', '0722111111', 'Safaricom Mobile', 3.5, 22.00, NULL, 'Unknown', GETDATE());

PRINT '   Created 5 PSTN call records';
PRINT '';

PRINT '================================================================';
PRINT 'TEST DATA SEEDING COMPLETED SUCCESSFULLY';
PRINT '================================================================';
PRINT '';
PRINT 'Summary:';
PRINT '--------';
PRINT '✓ Class of Service plans created';
PRINT '✓ 2 Organizations (UNON, UNEP)';
PRINT '✓ Offices and SubOffices created';
PRINT '✓ 5 EbillUsers (4 active, 1 inactive)';
PRINT '✓ 9 UserPhone records';
PRINT '✓ 9 Safaricom call records';
PRINT '✓ 5 PSTN call records';
PRINT '';
PRINT 'Test Scenarios Included:';
PRINT '------------------------';
PRINT '• Normal calls from registered users';
PRINT '• High-cost international call ($1500)';
PRINT '• Calls from inactive users';
PRINT '• Calls from unregistered phone numbers';
PRINT '• Internal extension calls';
PRINT '';
PRINT 'Next Steps:';
PRINT '-----------';
PRINT '1. Go to /Admin/CallLogStaging';
PRINT '2. Click "Consolidate Call Logs"';
PRINT '3. Select date range (last 7 days)';
PRINT '4. Review staged records';
PRINT '5. Check for detected anomalies';
PRINT '6. Verify records before pushing to production';
PRINT '';