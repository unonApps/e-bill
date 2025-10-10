-- Test Data for Interim Billing Feature
-- This script creates test data for testing staff separation interim billing

-- Step 1: Create a test EbillUser (staff member who is separating)
INSERT INTO EbillUsers (
    IndexNumber,
    FirstName,
    LastName,
    Email,
    OfficialMobileNumber,
    OrganizationId,
    OfficeId,
    Location,
    IsActive,
    CreatedDate
)
SELECT TOP 1
    'TEST001' as IndexNumber,
    'John' as FirstName,
    'Doe' as LastName,
    'john.doe.test@example.org' as Email,
    '254722123456' as OfficialMobileNumber,
    o.Id as OrganizationId,
    ofc.Id as OfficeId,
    'Nairobi, Kenya' as Location,
    1 as IsActive,
    GETUTCDATE() as CreatedDate
FROM Organizations o
CROSS JOIN Offices ofc
WHERE o.Code = 'UNON'
AND ofc.Code = 'UNON'
AND NOT EXISTS (SELECT 1 FROM EbillUsers WHERE IndexNumber = 'TEST001');

-- Step 2: Create UserPhone record for this test staff
-- First check if UserPhones table exists (it should based on our design)
IF OBJECT_ID('UserPhones', 'U') IS NOT NULL
BEGIN
    INSERT INTO UserPhones (
        IndexNumber,
        PhoneNumber,
        Provider,
        PhoneType,
        ClassOfServiceId,
        IsActive,
        IsPrimary,
        AssignedDate,
        CreatedDate
    )
    SELECT
        'TEST001' as IndexNumber,
        '254722123456' as PhoneNumber,
        'Safaricom' as Provider,
        'Mobile' as PhoneType,
        (SELECT TOP 1 Id FROM ClassOfServices WHERE Level = 'Standard') as ClassOfServiceId,
        1 as IsActive,
        1 as IsPrimary,
        DATEADD(month, -6, GETUTCDATE()) as AssignedDate,
        GETUTCDATE() as CreatedDate
    WHERE NOT EXISTS (
        SELECT 1 FROM UserPhones
        WHERE IndexNumber = 'TEST001'
        AND PhoneNumber = '254722123456'
    );
END

-- Step 3: Create Safaricom call logs for the test phone number
-- Creating calls for the current and previous month to test interim billing

-- Current month calls (for interim billing)
DECLARE @CurrentMonth DATE = DATEFROMPARTS(YEAR(GETDATE()), MONTH(GETDATE()), 1);
DECLARE @Counter INT = 1;

WHILE @Counter <= 20
BEGIN
    INSERT INTO Safaricoms (
        CallDate,
        CallTime,
        Dur,
        CallType,
        CallNumber,
        CalledNumber,
        CallCostKES,
        CallCostUSD,
        AmountKES,
        AmountUSD,
        CallDestination,
        ServiceProvider,
        ProcessingStatus,
        CreatedDate
    )
    VALUES (
        DATEADD(day, @Counter % 15, @CurrentMonth), -- Spread calls across first 15 days
        CAST(DATEADD(hour, @Counter % 24, '00:00:00') as TIME), -- Different times
        CAST((5 + (@Counter * 2)) as DECIMAL(10,2)), -- Duration in minutes
        CASE
            WHEN @Counter % 3 = 0 THEN 'International'
            WHEN @Counter % 3 = 1 THEN 'Local'
            ELSE 'Mobile'
        END,
        '254722123456', -- Test staff phone number
        CASE
            WHEN @Counter % 3 = 0 THEN '+1234567890'
            WHEN @Counter % 3 = 1 THEN '254720' + RIGHT('000000' + CAST(100000 + @Counter as VARCHAR), 6)
            ELSE '254733' + RIGHT('000000' + CAST(200000 + @Counter as VARCHAR), 6)
        END,
        CAST((10 + (@Counter * 1.5)) as DECIMAL(18,2)), -- Cost in KES
        CAST((0.08 + (@Counter * 0.012)) as DECIMAL(18,4)), -- Cost in USD
        CAST((10 + (@Counter * 1.5)) as DECIMAL(18,2)), -- Amount in KES
        CAST((0.08 + (@Counter * 0.012)) as DECIMAL(18,4)), -- Amount in USD
        CASE
            WHEN @Counter % 3 = 0 THEN 'USA'
            WHEN @Counter % 3 = 1 THEN 'Kenya-Local'
            ELSE 'Kenya-Mobile'
        END,
        'Safaricom',
        0, -- ProcessingStatus.Staged
        GETUTCDATE()
    );

    SET @Counter = @Counter + 1;
END;

-- Previous month calls (already processed - to show separation between monthly and interim)
DECLARE @PrevMonth DATE = DATEADD(month, -1, @CurrentMonth);
SET @Counter = 1;

WHILE @Counter <= 15
BEGIN
    INSERT INTO Safaricoms (
        CallDate,
        CallTime,
        Dur,
        CallType,
        CallNumber,
        CalledNumber,
        CallCostKES,
        CallCostUSD,
        AmountKES,
        AmountUSD,
        CallDestination,
        ServiceProvider,
        ProcessingStatus,
        CreatedDate
    )
    VALUES (
        DATEADD(day, @Counter, @PrevMonth),
        CAST(DATEADD(hour, @Counter % 24, '00:00:00') as TIME),
        CAST((3 + (@Counter * 1.5)) as DECIMAL(10,2)),
        CASE
            WHEN @Counter % 2 = 0 THEN 'Local'
            ELSE 'Mobile'
        END,
        '254722123456',
        '254720' + RIGHT('000000' + CAST(300000 + @Counter as VARCHAR), 6),
        CAST((5 + (@Counter * 0.8)) as DECIMAL(18,2)),
        CAST((0.04 + (@Counter * 0.006)) as DECIMAL(18,4)),
        CAST((5 + (@Counter * 0.8)) as DECIMAL(18,2)),
        CAST((0.04 + (@Counter * 0.006)) as DECIMAL(18,4)),
        'Kenya-Mobile',
        'Safaricom',
        0, -- ProcessingStatus.Staged
        GETUTCDATE()
    );

    SET @Counter = @Counter + 1;
END;

-- Step 4: Add some Airtel records for the same user (optional)
SET @Counter = 1;

WHILE @Counter <= 10
BEGIN
    INSERT INTO Airtels (
        CallDate,
        CallTime,
        Dur,
        CallType,
        CallNumber,
        CalledNumber,
        CallCostKES,
        CallCostUSD,
        AmountKES,
        AmountUSD,
        CallDestination,
        ServiceProvider,
        ProcessingStatus,
        CreatedDate
    )
    VALUES (
        DATEADD(day, @Counter % 10, @CurrentMonth),
        CAST(DATEADD(hour, 8 + (@Counter % 12), '00:00:00') as TIME),
        CAST((2 + (@Counter * 0.5)) as DECIMAL(10,2)),
        'Mobile',
        '254722123456',
        '254731' + RIGHT('000000' + CAST(400000 + @Counter as VARCHAR), 6),
        CAST((3 + (@Counter * 0.5)) as DECIMAL(18,2)),
        CAST((0.02 + (@Counter * 0.004)) as DECIMAL(18,4)),
        CAST((3 + (@Counter * 0.5)) as DECIMAL(18,2)),
        CAST((0.02 + (@Counter * 0.004)) as DECIMAL(18,4)),
        'Kenya-Mobile',
        'Airtel',
        0, -- ProcessingStatus.Staged
        GETUTCDATE()
    );

    SET @Counter = @Counter + 1;
END;

-- Verification Query - Check what was created
SELECT 'Test Data Created Successfully!' as Status;

-- Show the test user
SELECT
    'EbillUser' as RecordType,
    IndexNumber,
    FirstName + ' ' + LastName as FullName,
    Email,
    OfficialMobileNumber,
    IsActive
FROM EbillUsers
WHERE IndexNumber = 'TEST001';

-- Show call log summary
SELECT
    'CallLogs Summary' as RecordType,
    'Safaricom' as Provider,
    CallNumber,
    COUNT(*) as TotalCalls,
    MIN(CallDate) as FirstCall,
    MAX(CallDate) as LastCall,
    SUM(AmountUSD) as TotalAmountUSD,
    CASE
        WHEN MONTH(CallDate) = MONTH(GETDATE()) THEN 'Current Month'
        ELSE 'Previous Month'
    END as Period
FROM Safaricoms
WHERE CallNumber = '254722123456'
GROUP BY CallNumber, MONTH(CallDate)

UNION ALL

SELECT
    'CallLogs Summary' as RecordType,
    'Airtel' as Provider,
    CallNumber,
    COUNT(*) as TotalCalls,
    MIN(CallDate) as FirstCall,
    MAX(CallDate) as LastCall,
    SUM(AmountUSD) as TotalAmountUSD,
    'Current Month' as Period
FROM Airtels
WHERE CallNumber = '254722123456'
GROUP BY CallNumber;

PRINT 'Test data created successfully!';
PRINT 'Test User Index Number: TEST001';
PRINT 'Test Phone Number: 254722123456';
PRINT 'You can now test the interim billing feature with this staff member.';