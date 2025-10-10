-- Seed data for PrivateWires table
-- This script adds sample Private Wire telecommunications records

USE [TABDB];
GO

-- Check if PrivateWires table exists and has no data
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PrivateWires]') AND type in (N'U'))
BEGIN
    -- Only insert if table is empty to avoid duplicates
    IF NOT EXISTS (SELECT 1 FROM [dbo].[PrivateWires])
    BEGIN
        PRINT 'Inserting seed data into PrivateWires table...';

        INSERT INTO [dbo].[PrivateWires] (
            [Extension], [DestinationLine], [DurationExtended],
            [DialedNumber], [CallTime], [Destination],
            [AmountUSD],
            [Organization], [Office], [SubOffice], [Level4Unit], [OrganizationalUnit],
            [CallerName], [CallDate], [Duration], [IndexNumber], [Location], [OCACode],
            [CreatedDate], [CreatedBy]
        )
        VALUES
        -- Sample data for UNON Private Wire calls
        ('2301', 'PW-NY-001', 15.50, '+12125551234', '09:30:00', 'New York HQ',
         25.75, 'UNON', 'Nairobi', 'Executive Office', 'Communications', 'EXEC-COMM',
         'John Smith', '2024-01-15', 15.50, 'UN12345', 'Gigiri Complex', 'OCA001',
         GETUTCDATE(), 'seed_script'),

        ('2302', 'PW-GE-002', 8.25, '+41227910000', '14:15:00', 'Geneva Office',
         18.50, 'UNON', 'Nairobi', 'Political Affairs', 'Regional Desk', 'POL-REG',
         'Mary Johnson', '2024-01-16', 8.25, 'UN12346', 'Gigiri Complex', 'OCA002',
         GETUTCDATE(), 'seed_script'),

        ('2303', 'PW-VI-003', 22.00, '+43126060', '11:00:00', 'Vienna International Centre',
         35.00, 'UNEP', 'Nairobi', 'Climate Action', 'Policy Unit', 'ENV-POL',
         'David Wilson', '2024-01-17', 22.00, 'EP78901', 'UNEP HQ', 'OCA003',
         GETUTCDATE(), 'seed_script'),

        ('2304', 'PW-BN-004', 12.75, '+66226881234', '16:45:00', 'Bangkok Regional Office',
         22.25, 'UNHABITAT', 'Nairobi', 'Asia Pacific', 'Urban Planning', 'HAB-APAC',
         'Sarah Chen', '2024-01-18', 12.75, 'HA45678', 'UN Complex', 'OCA004',
         GETUTCDATE(), 'seed_script'),

        ('2305', 'PW-AD-005', 5.50, '+2519115571', '08:00:00', 'Addis Ababa ECA',
         12.00, 'UNON', 'Nairobi', 'Finance', 'Budget Section', 'FIN-BGT',
         'Michael Brown', '2024-01-19', 5.50, 'UN12347', 'Gigiri Complex', 'OCA005',
         GETUTCDATE(), 'seed_script'),

        -- High-cost international Private Wire calls
        ('2306', 'PW-TK-006', 45.00, '+81352185000', '10:30:00', 'Tokyo Liaison Office',
         125.00, 'UNEP', 'Nairobi', 'Disasters & Conflicts', 'Emergency Response', 'ENV-EMER',
         'Jennifer Lee', '2024-01-20', 45.00, 'EP78902', 'UNEP HQ', 'OCA006',
         GETUTCDATE(), 'seed_script'),

        ('2307', 'PW-SY-007', 38.50, '+61292234567', '13:00:00', 'Sydney Regional Hub',
         95.75, 'UNHABITAT', 'Nairobi', 'Pacific Region', 'Island States', 'HAB-PAC',
         'Robert Taylor', '2024-01-21', 38.50, 'HA45679', 'UN Complex', 'OCA007',
         GETUTCDATE(), 'seed_script'),

        -- Regular Private Wire traffic
        ('2308', 'PW-NY-008', 18.00, '+12125551000', '09:00:00', 'New York HQ',
         32.50, 'UNON', 'Nairobi', 'Security', 'Operations Center', 'SEC-OPS',
         'Patricia Davis', '2024-01-22', 18.00, 'UN12348', 'Gigiri Complex', 'OCA008',
         GETUTCDATE(), 'seed_script'),

        ('2309', 'PW-GE-009', 25.25, '+41227918888', '15:30:00', 'Geneva Office',
         45.00, 'OIOS', 'Nairobi', 'Internal Audit', 'Risk Assessment', 'OIOS-AUD',
         'James Anderson', '2024-01-23', 25.25, 'IO56789', 'NOF Building', 'OCA009',
         GETUTCDATE(), 'seed_script'),

        ('2310', 'PW-WA-010', 30.00, '+12024561111', '07:45:00', 'Washington DC Liaison',
         52.50, 'UNEP', 'Nairobi', 'Partnerships', 'Government Relations', 'ENV-PART',
         'Linda Martinez', '2024-01-24', 30.00, 'EP78903', 'UNEP HQ', 'OCA010',
         GETUTCDATE(), 'seed_script'),

        -- Conference calls via Private Wire
        ('2311', 'PW-CONF-011', 120.00, 'CONF-BRIDGE-001', '14:00:00', 'Multi-point Conference',
         250.00, 'UNON', 'Nairobi', 'Conference Services', 'Technical Support', 'CONF-TECH',
         'Conference Room A', '2024-01-25', 120.00, 'CONF001', 'Conference Center', 'OCA011',
         GETUTCDATE(), 'seed_script'),

        ('2312', 'PW-CONF-012', 90.00, 'CONF-BRIDGE-002', '10:00:00', 'Regional Directors Meeting',
         185.00, 'UNHABITAT', 'Nairobi', 'Executive Office', 'Regional Coordination', 'HAB-EXEC',
         'Conference Room B', '2024-01-26', 90.00, 'CONF002', 'Executive Wing', 'OCA012',
         GETUTCDATE(), 'seed_script'),

        -- Emergency Private Wire calls
        ('2313', 'PW-EMER-013', 35.50, '+442079460000', '23:30:00', 'London Crisis Center',
         75.00, 'UNON', 'Nairobi', 'Security', 'Crisis Management', 'SEC-CRISIS',
         'Emergency Duty Officer', '2024-01-27', 35.50, 'EMER001', 'Operations Center', 'OCA013',
         GETUTCDATE(), 'seed_script'),

        ('2314', 'PW-BJ-014', 28.75, '+861065323114', '06:00:00', 'Beijing Office',
         58.25, 'UNEP', 'Nairobi', 'Asia Pacific', 'China Desk', 'ENV-APAC',
         'Zhang Wei', '2024-01-28', 28.75, 'EP78904', 'UNEP HQ', 'OCA014',
         GETUTCDATE(), 'seed_script'),

        ('2315', 'PW-DU-015', 42.00, '+97143372222', '12:30:00', 'Dubai Regional Office',
         88.50, 'UNHABITAT', 'Nairobi', 'Arab States', 'Gulf Region', 'HAB-ARAB',
         'Ahmed Hassan', '2024-01-29', 42.00, 'HA45680', 'UN Complex', 'OCA015',
         GETUTCDATE(), 'seed_script');

        PRINT 'Successfully inserted 15 sample Private Wire records.';
    END
    ELSE
    BEGIN
        PRINT 'PrivateWires table already contains data. Skipping seed data insertion.';
    END
END
ELSE
BEGIN
    PRINT 'PrivateWires table does not exist. Please create the table first using create_privatewire_table.sql';
END
GO

-- Display summary of seeded data
IF EXISTS (SELECT 1 FROM [dbo].[PrivateWires])
BEGIN
    PRINT '';
    PRINT 'Private Wire Data Summary:';
    PRINT '==========================';

    SELECT
        Organization,
        Office,
        COUNT(*) as TotalCalls,
        SUM(Duration) as TotalMinutes,
        SUM(AmountUSD) as TotalCostUSD,
        AVG(AmountUSD) as AvgCostPerCall,
        MIN(CallDate) as FirstCall,
        MAX(CallDate) as LastCall
    FROM [dbo].[PrivateWires]
    GROUP BY Organization, Office
    ORDER BY Organization, Office;

    PRINT '';
    PRINT 'Top 5 Most Expensive Private Wire Calls:';
    SELECT TOP 5
        CallerName,
        DialedNumber,
        Destination,
        Duration,
        AmountUSD,
        CallDate
    FROM [dbo].[PrivateWires]
    ORDER BY AmountUSD DESC;
END
GO