-- =============================================
-- Add CallMonth and CallYear to PSTN and PrivateWire tables
-- Then reseed with sample data
-- =============================================

-- Add CallMonth and CallYear columns to PSTNs table if they don't exist
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('PSTNs') AND name = 'CallMonth')
BEGIN
    ALTER TABLE PSTNs ADD CallMonth INT NOT NULL DEFAULT 1;
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('PSTNs') AND name = 'CallYear')
BEGIN
    ALTER TABLE PSTNs ADD CallYear INT NOT NULL DEFAULT 2024;
END

-- Add CallMonth and CallYear columns to PrivateWires table if they don't exist
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('PrivateWires') AND name = 'CallMonth')
BEGIN
    ALTER TABLE PrivateWires ADD CallMonth INT NOT NULL DEFAULT 1;
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('PrivateWires') AND name = 'CallYear')
BEGIN
    ALTER TABLE PrivateWires ADD CallYear INT NOT NULL DEFAULT 2024;
END

-- Update existing records to populate CallMonth and CallYear based on CallDate
UPDATE PSTNs
SET CallMonth = MONTH(CallDate),
    CallYear = YEAR(CallDate)
WHERE CallDate IS NOT NULL;

UPDATE PrivateWires
SET CallMonth = MONTH(CallDate),
    CallYear = YEAR(CallDate)
WHERE CallDate IS NOT NULL;

-- Clear existing data from all telecom tables
DELETE FROM CallLogStaging;
DELETE FROM Safaricoms;
DELETE FROM Airtels;
DELETE FROM PSTNs;
DELETE FROM PrivateWires;

-- Reset identity seeds
DBCC CHECKIDENT ('Safaricoms', RESEED, 0);
DBCC CHECKIDENT ('Airtels', RESEED, 0);
DBCC CHECKIDENT ('PSTNs', RESEED, 0);
DBCC CHECKIDENT ('PrivateWires', RESEED, 0);

PRINT 'Tables cleared and ready for seeding';

-- =============================================
-- SEED SAFARICOM DATA (August 2025)
-- =============================================
INSERT INTO Safaricoms (
    Ext, call_date, call_time, Dialed, Dest, Durx, Dur, [call currency], Amt, USD, KSH,
    CallMonth, CallYear, CreatedDate, ProcessingStatus
)
VALUES
-- August 2025 calls
('0720123456', '2025-08-01', '09:15:00', '0722987654', 'Nairobi', 5.5, 5, 'KES', 25.00, 0.20, 25.00, 8, 2025, GETDATE(), 'Pending'),
('0721234567', '2025-08-02', '14:30:00', '0733456789', 'Mombasa', 12.3, 12, 'KES', 45.00, 0.35, 45.00, 8, 2025, GETDATE(), 'Pending'),
('0722345678', '2025-08-03', '10:45:00', '+254711223344', 'Kisumu', 8.7, 9, 'KES', 35.00, 0.27, 35.00, 8, 2025, GETDATE(), 'Pending'),
('0723456789', '2025-08-05', '16:20:00', '0700111222', 'Nakuru', 3.2, 3, 'KES', 15.00, 0.12, 15.00, 8, 2025, GETDATE(), 'Pending'),
('0724567890', '2025-08-08', '11:00:00', '+447700900123', 'UK', 15.0, 15, 'USD', 5.00, 5.00, 650.00, 8, 2025, GETDATE(), 'Pending'),
('0725678901', '2025-08-10', '08:30:00', '0712345678', 'Eldoret', 7.5, 8, 'KES', 30.00, 0.23, 30.00, 8, 2025, GETDATE(), 'Pending'),
('0726789012', '2025-08-12', '13:45:00', '+1234567890', 'USA', 20.0, 20, 'USD', 10.00, 10.00, 1300.00, 8, 2025, GETDATE(), 'Pending'),
('0727890123', '2025-08-15', '09:00:00', '0788665544', 'Thika', 4.8, 5, 'KES', 20.00, 0.15, 20.00, 8, 2025, GETDATE(), 'Pending'),
('0728901234', '2025-08-18', '15:30:00', '0799776655', 'Nyeri', 6.3, 6, 'KES', 28.00, 0.22, 28.00, 8, 2025, GETDATE(), 'Pending'),
('0729012345', '2025-08-20', '10:15:00', '0711998877', 'Machakos', 9.1, 9, 'KES', 40.00, 0.31, 40.00, 8, 2025, GETDATE(), 'Pending');

-- =============================================
-- SEED AIRTEL DATA (August 2025)
-- =============================================
INSERT INTO Airtels (
    Ext, call_date, call_time, Dialed, Dest, Durx, Dur, [call currency], Amt, USD, KSH,
    CallMonth, CallYear, CreatedDate, ProcessingStatus
)
VALUES
-- August 2025 calls
('0730123456', '2025-08-01', '10:20:00', '0731987654', 'Nairobi', 4.5, 5, 'KES', 22.00, 0.17, 22.00, 8, 2025, GETDATE(), 'Pending'),
('0731234567', '2025-08-02', '13:15:00', '0732456789', 'Kisii', 11.2, 11, 'KES', 42.00, 0.32, 42.00, 8, 2025, GETDATE(), 'Pending'),
('0732345678', '2025-08-04', '09:30:00', '0733567890', 'Garissa', 7.8, 8, 'KES', 33.00, 0.25, 33.00, 8, 2025, GETDATE(), 'Pending'),
('0733456789', '2025-08-06', '14:45:00', '+254734678901', 'Malindi', 5.3, 5, 'KES', 24.00, 0.18, 24.00, 8, 2025, GETDATE(), 'Pending'),
('0734567890', '2025-08-09', '11:30:00', '+33123456789', 'France', 18.0, 18, 'USD', 8.00, 8.00, 1040.00, 8, 2025, GETDATE(), 'Pending'),
('0735678901', '2025-08-11', '08:00:00', '0736789012', 'Kitale', 6.7, 7, 'KES', 29.00, 0.22, 29.00, 8, 2025, GETDATE(), 'Pending'),
('0736789012', '2025-08-14', '16:00:00', '0737890123', 'Busia', 3.9, 4, 'KES', 18.00, 0.14, 18.00, 8, 2025, GETDATE(), 'Pending'),
('0737890123', '2025-08-17', '12:30:00', '+86123456789', 'China', 25.0, 25, 'USD', 12.00, 12.00, 1560.00, 8, 2025, GETDATE(), 'Pending'),
('0738901234', '2025-08-19', '09:45:00', '0739012345', 'Voi', 8.4, 8, 'KES', 35.00, 0.27, 35.00, 8, 2025, GETDATE(), 'Pending'),
('0739012345', '2025-08-21', '14:00:00', '0730112233', 'Nanyuki', 10.5, 11, 'KES', 44.00, 0.34, 44.00, 8, 2025, GETDATE(), 'Pending');

-- =============================================
-- SEED PSTN DATA (August 2025)
-- =============================================
INSERT INTO PSTNs (
    Extension, DialedNumber, CallTime, Destination, DestinationLine, DurationExtended, Duration,
    CallDate, CallMonth, CallYear, AmountKSH, Carrier, OCACode, Location, CreatedDate, ProcessingStatus
)
VALUES
-- August 2025 calls
('2001', '0202123456', '08:00:00', 'Nairobi CBD', 'Landline', 15.5, 16, '2025-08-01', 8, 2025, 75.00, 'Telkom', 'OCA001', 'HQ', GETDATE(), 'Pending'),
('2002', '0412345678', '09:30:00', 'Mombasa Office', 'Landline', 20.3, 20, '2025-08-02', 8, 2025, 95.00, 'Telkom', 'OCA002', 'Branch', GETDATE(), 'Pending'),
('2003', '0512345678', '10:15:00', 'Kisumu Branch', 'Landline', 12.7, 13, '2025-08-03', 8, 2025, 62.00, 'Telkom', 'OCA003', 'Regional', GETDATE(), 'Pending'),
('2004', '+441234567890', '11:45:00', 'London Office', 'International', 30.0, 30, '2025-08-05', 8, 2025, 450.00, 'Telkom', 'OCA004', 'HQ', GETDATE(), 'Pending'),
('2005', '0612345678', '13:00:00', 'Nakuru Unit', 'Landline', 8.5, 9, '2025-08-08', 8, 2025, 42.00, 'Telkom', 'OCA005', 'Field', GETDATE(), 'Pending'),
('2006', '0202987654', '14:30:00', 'Westlands', 'Landline', 18.2, 18, '2025-08-10', 8, 2025, 86.00, 'Telkom', 'OCA006', 'HQ', GETDATE(), 'Pending'),
('2007', '+12125551234', '15:00:00', 'New York', 'International', 45.0, 45, '2025-08-12', 8, 2025, 675.00, 'Telkom', 'OCA007', 'HQ', GETDATE(), 'Pending'),
('2008', '0572345678', '08:30:00', 'Eldoret Office', 'Landline', 14.8, 15, '2025-08-15', 8, 2025, 71.00, 'Telkom', 'OCA008', 'Regional', GETDATE(), 'Pending'),
('2009', '0622345678', '09:00:00', 'Nyeri Branch', 'Landline', 10.3, 10, '2025-08-18', 8, 2025, 48.00, 'Telkom', 'OCA009', 'Branch', GETDATE(), 'Pending'),
('2010', '0432345678', '10:45:00', 'Kilifi Office', 'Landline', 22.5, 23, '2025-08-20', 8, 2025, 108.00, 'Telkom', 'OCA010', 'Coastal', GETDATE(), 'Pending');

-- =============================================
-- SEED PRIVATEWIRE DATA (August 2025)
-- =============================================
INSERT INTO PrivateWires (
    Extension, DestinationLine, DurationExtended, DialedNumber, Destination, CallTime,
    CallDate, CallMonth, CallYear, Duration, AmountKSH, Carrier, OCACode, Location, CreatedDate, ProcessingStatus
)
VALUES
-- August 2025 calls
('3001', 'PW-Line-01', 5.5, 'EXT-4001', 'Conference Room A', '08:15:00', '2025-08-01', 8, 2025, 6, 0.00, 'Internal', 'PW001', 'HQ', GETDATE(), 'Pending'),
('3002', 'PW-Line-02', 8.3, 'EXT-4002', 'Executive Office', '09:00:00', '2025-08-02', 8, 2025, 8, 0.00, 'Internal', 'PW002', 'HQ', GETDATE(), 'Pending'),
('3003', 'PW-Line-03', 12.7, 'EXT-4003', 'Board Room', '10:30:00', '2025-08-03', 8, 2025, 13, 0.00, 'Internal', 'PW003', 'HQ', GETDATE(), 'Pending'),
('3004', 'PW-Line-04', 3.2, 'EXT-4004', 'Reception', '11:00:00', '2025-08-05', 8, 2025, 3, 0.00, 'Internal', 'PW004', 'Lobby', GETDATE(), 'Pending'),
('3005', 'PW-Line-05', 45.0, 'EXT-4005', 'Training Room', '13:30:00', '2025-08-08', 8, 2025, 45, 0.00, 'Internal', 'PW005', 'Training', GETDATE(), 'Pending'),
('3006', 'PW-Line-06', 7.8, 'EXT-4006', 'IT Department', '14:45:00', '2025-08-10', 8, 2025, 8, 0.00, 'Internal', 'PW006', 'Tech', GETDATE(), 'Pending'),
('3007', 'PW-Line-07', 15.3, 'EXT-4007', 'Finance Office', '15:30:00', '2025-08-12', 8, 2025, 15, 0.00, 'Internal', 'PW007', 'Finance', GETDATE(), 'Pending'),
('3008', 'PW-Line-08', 6.5, 'EXT-4008', 'HR Department', '08:00:00', '2025-08-15', 8, 2025, 7, 0.00, 'Internal', 'PW008', 'HR', GETDATE(), 'Pending'),
('3009', 'PW-Line-09', 20.0, 'EXT-4009', 'Security Post', '09:15:00', '2025-08-18', 8, 2025, 20, 0.00, 'Internal', 'PW009', 'Security', GETDATE(), 'Pending'),
('3010', 'PW-Line-10', 9.7, 'EXT-4010', 'Cafeteria', '12:00:00', '2025-08-20', 8, 2025, 10, 0.00, 'Internal', 'PW010', 'Services', GETDATE(), 'Pending');

PRINT 'Sample data inserted for August 2025';
PRINT 'Safaricom: 10 records';
PRINT 'Airtel: 10 records';
PRINT 'PSTN: 10 records';
PRINT 'PrivateWire: 10 records';
PRINT 'Total: 40 records for consolidation';