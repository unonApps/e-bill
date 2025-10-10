-- Seed data for PSTNs table
-- This script adds sample PSTN (Public Switched Telephone Network) records

USE [TABDB];
GO

-- Check if PSTNs table exists and has no data
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PSTNs]') AND type in (N'U'))
BEGIN
    -- Only insert if table is empty to avoid duplicates
    IF NOT EXISTS (SELECT 1 FROM [dbo].[PSTNs])
    BEGIN
        PRINT 'Inserting seed data into PSTNs table...';

        INSERT INTO [dbo].[PSTNs] (
            [Extension], [DialedNumber], [CallTime], [Destination], [DestinationLine],
            [DurationExtended], [Organization], [Office], [SubOffice], [OrganizationalUnit],
            [CallerName], [CallDate], [Duration], [AmountKSH], [IndexNumber],
            [Location], [OCACode], [Carrier], [CreatedDate], [CreatedBy]
        )
        VALUES
        -- Local Kenyan calls
        ('2001', '0722123456', '08:30:00', 'Nairobi', 'Mobile', 5.50,
         'UNON', 'Nairobi', 'Admin Services', 'ADM-GEN',
         'Alice Kamau', '2024-01-15', 5.50, 55.00, 'UN10001',
         'Gigiri Complex', 'OCA101', 'Safaricom', GETUTCDATE(), 'seed_script'),

        ('2002', '0733987654', '09:15:00', 'Mombasa', 'Mobile', 8.25,
         'UNON', 'Nairobi', 'Procurement', 'PROC-OPS',
         'Brian Ochieng', '2024-01-15', 8.25, 82.50, 'UN10002',
         'Gigiri Complex', 'OCA102', 'Airtel', GETUTCDATE(), 'seed_script'),

        ('2003', '0205551234', '10:00:00', 'Nairobi CBD', 'Landline', 3.00,
         'UNEP', 'Nairobi', 'Communications', 'ENV-COMM',
         'Catherine Muthoni', '2024-01-16', 3.00, 30.00, 'EP20001',
         'UNEP HQ', 'OCA201', 'Telkom', GETUTCDATE(), 'seed_script'),

        -- Regional East Africa calls
        ('2004', '+256701234567', '11:30:00', 'Kampala, Uganda', 'Mobile', 15.00,
         'UNHABITAT', 'Nairobi', 'Regional Office', 'HAB-EA',
         'Daniel Njoroge', '2024-01-16', 15.00, 450.00, 'HA30001',
         'UN Complex', 'OCA301', 'Safaricom', GETUTCDATE(), 'seed_script'),

        ('2005', '+255765432100', '14:00:00', 'Dar es Salaam, Tanzania', 'Mobile', 12.50,
         'UNON', 'Nairobi', 'Finance', 'FIN-ACC',
         'Elizabeth Wanjiru', '2024-01-17', 12.50, 375.00, 'UN10003',
         'Gigiri Complex', 'OCA103', 'Airtel', GETUTCDATE(), 'seed_script'),

        ('2006', '+251911223344', '15:30:00', 'Addis Ababa, Ethiopia', 'Mobile', 18.75,
         'OIOS', 'Nairobi', 'Audit Division', 'OIOS-AFR',
         'Francis Kiprop', '2024-01-17', 18.75, 562.50, 'IO40001',
         'NOF Building', 'OCA401', 'Telkom', GETUTCDATE(), 'seed_script'),

        -- International calls
        ('2007', '+44207946000', '16:00:00', 'London, UK', 'Landline', 25.00,
         'UNEP', 'Nairobi', 'Partnerships', 'ENV-EUR',
         'Grace Akinyi', '2024-01-18', 25.00, 1250.00, 'EP20002',
         'UNEP HQ', 'OCA202', 'Safaricom', GETUTCDATE(), 'seed_script'),

        ('2008', '+12125551000', '17:45:00', 'New York, USA', 'Landline', 30.00,
         'UNON', 'Nairobi', 'Executive Office', 'EXEC-DIR',
         'Henry Mutua', '2024-01-18', 30.00, 1500.00, 'UN10004',
         'Gigiri Complex', 'OCA104', 'Airtel', GETUTCDATE(), 'seed_script'),

        ('2009', '+33145681000', '09:00:00', 'Paris, France', 'Landline', 22.50,
         'UNHABITAT', 'Nairobi', 'Urban Planning', 'HAB-PLAN',
         'Irene Nyambura', '2024-01-19', 22.50, 1125.00, 'HA30002',
         'UN Complex', 'OCA302', 'Telkom', GETUTCDATE(), 'seed_script'),

        -- Mobile calls within Kenya
        ('2010', '0712345678', '10:30:00', 'Kisumu', 'Mobile', 7.75,
         'UNON', 'Nairobi', 'Medical Services', 'MED-CLINIC',
         'James Otieno', '2024-01-19', 7.75, 77.50, 'UN10005',
         'Gigiri Complex', 'OCA105', 'Safaricom', GETUTCDATE(), 'seed_script'),

        ('2011', '0723456789', '11:15:00', 'Nakuru', 'Mobile', 6.50,
         'UNEP', 'Nairobi', 'Field Operations', 'ENV-FIELD',
         'Karen Chebet', '2024-01-20', 6.50, 65.00, 'EP20003',
         'UNEP HQ', 'OCA203', 'Airtel', GETUTCDATE(), 'seed_script'),

        ('2012', '0734567890', '12:00:00', 'Eldoret', 'Mobile', 9.00,
         'UNHABITAT', 'Nairobi', 'Country Programs', 'HAB-KEN',
         'Leonard Kimani', '2024-01-20', 9.00, 90.00, 'HA30003',
         'UN Complex', 'OCA303', 'Telkom', GETUTCDATE(), 'seed_script'),

        -- Conference calls
        ('2013', '0203334444', '14:00:00', 'Conference Bridge', 'Conference', 60.00,
         'UNON', 'Nairobi', 'Conference Services', 'CONF-MGT',
         'Meeting Room 1', '2024-01-21', 60.00, 600.00, 'CONF10001',
         'Conference Center', 'OCA106', 'Safaricom', GETUTCDATE(), 'seed_script'),

        -- Emergency calls
        ('2014', '999', '23:45:00', 'Emergency Services', 'Emergency', 2.00,
         'UNON', 'Nairobi', 'Security', 'SEC-DUTY',
         'Security Control Room', '2024-01-21', 2.00, 0.00, 'SEC10001',
         'Main Gate', 'OCA107', 'Safaricom', GETUTCDATE(), 'seed_script'),

        -- Additional regular calls
        ('2015', '0720111222', '08:00:00', 'Nairobi', 'Mobile', 4.25,
         'OIOS', 'Nairobi', 'Investigations', 'OIOS-INV',
         'Margaret Wambui', '2024-01-22', 4.25, 42.50, 'IO40002',
         'NOF Building', 'OCA402', 'Airtel', GETUTCDATE(), 'seed_script'),

        ('2016', '0731222333', '09:30:00', 'Thika', 'Mobile', 5.75,
         'UNEP', 'Nairobi', 'Science Division', 'ENV-SCI',
         'Nicholas Kiptoo', '2024-01-22', 5.75, 57.50, 'EP20004',
         'UNEP HQ', 'OCA204', 'Telkom', GETUTCDATE(), 'seed_script'),

        ('2017', '+27115551234', '10:45:00', 'Johannesburg, SA', 'Landline', 20.00,
         'UNHABITAT', 'Nairobi', 'Africa Regional', 'HAB-AFR',
         'Olivia Ndung''u', '2024-01-23', 20.00, 800.00, 'HA30004',
         'UN Complex', 'OCA304', 'Safaricom', GETUTCDATE(), 'seed_script'),

        ('2018', '0722333444', '11:30:00', 'Nairobi', 'Mobile', 3.50,
         'UNON', 'Nairobi', 'HR Department', 'HR-REC',
         'Peter Mwangi', '2024-01-23', 3.50, 35.00, 'UN10006',
         'Gigiri Complex', 'OCA108', 'Airtel', GETUTCDATE(), 'seed_script'),

        ('2019', '0733444555', '13:00:00', 'Kiambu', 'Mobile', 4.75,
         'UNEP', 'Nairobi', 'Legal Affairs', 'ENV-LEG',
         'Queen Adhiambo', '2024-01-24', 4.75, 47.50, 'EP20005',
         'UNEP HQ', 'OCA205', 'Telkom', GETUTCDATE(), 'seed_script'),

        ('2020', '+919876543210', '14:30:00', 'New Delhi, India', 'Mobile', 28.00,
         'UNON', 'Nairobi', 'IT Services', 'IT-SUPPORT',
         'Richard Omondi', '2024-01-24', 28.00, 1400.00, 'UN10007',
         'Gigiri Complex', 'OCA109', 'Safaricom', GETUTCDATE(), 'seed_script');

        PRINT 'Successfully inserted 20 sample PSTN records.';
    END
    ELSE
    BEGIN
        PRINT 'PSTNs table already contains data. Skipping seed data insertion.';
    END
END
ELSE
BEGIN
    PRINT 'PSTNs table does not exist. Please create the table first using create_pstn_table.sql';
END
GO

-- Display summary of seeded data
IF EXISTS (SELECT 1 FROM [dbo].[PSTNs])
BEGIN
    PRINT '';
    PRINT 'PSTN Data Summary:';
    PRINT '==================';

    SELECT
        Organization,
        Office,
        COUNT(*) as TotalCalls,
        SUM(Duration) as TotalMinutes,
        SUM(AmountKSH) as TotalCostKSH,
        AVG(AmountKSH) as AvgCostPerCall,
        MIN(CallDate) as FirstCall,
        MAX(CallDate) as LastCall
    FROM [dbo].[PSTNs]
    GROUP BY Organization, Office
    ORDER BY Organization, Office;

    PRINT '';
    PRINT 'Top 5 Most Expensive PSTN Calls:';
    SELECT TOP 5
        CallerName,
        DialedNumber,
        Destination,
        Duration,
        AmountKSH,
        CallDate
    FROM [dbo].[PSTNs]
    ORDER BY AmountKSH DESC;

    PRINT '';
    PRINT 'Call Distribution by Carrier:';
    SELECT
        Carrier,
        COUNT(*) as NumberOfCalls,
        SUM(Duration) as TotalMinutes,
        SUM(AmountKSH) as TotalCostKSH
    FROM [dbo].[PSTNs]
    GROUP BY Carrier
    ORDER BY TotalCostKSH DESC;
END
GO