-- Simple Test Data for Interim Billing Feature
-- This script creates basic test data without UserPhones table

-- Check if test user already exists
IF EXISTS (SELECT 1 FROM EbillUsers WHERE IndexNumber = 'TEST001')
BEGIN
    PRINT 'Test user TEST001 already exists. Skipping EbillUser creation.';
END
ELSE
BEGIN
    -- Create test EbillUser
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
    AND ofc.Code = 'UNON';

    PRINT 'Created test user TEST001 successfully.';
END

-- Check and display the test user
SELECT
    'Test User Created' as Status,
    IndexNumber,
    FirstName + ' ' + LastName as FullName,
    Email,
    OfficialMobileNumber
FROM EbillUsers
WHERE IndexNumber = 'TEST001';

-- Check existing call logs for this phone number
DECLARE @ExistingCalls INT;
SELECT @ExistingCalls = COUNT(*)
FROM Safaricoms
WHERE CallNumber = '254722123456';

PRINT 'Existing Safaricom calls for 254722123456: ' + CAST(@ExistingCalls as VARCHAR(10));

-- Show summary of call logs
SELECT
    'Safaricom' as Provider,
    COUNT(*) as TotalCalls,
    MIN(CallDate) as EarliestCall,
    MAX(CallDate) as LatestCall,
    SUM(AmountUSD) as TotalUSD
FROM Safaricoms
WHERE CallNumber = '254722123456'

UNION ALL

SELECT
    'Airtel' as Provider,
    COUNT(*) as TotalCalls,
    MIN(CallDate) as EarliestCall,
    MAX(CallDate) as LatestCall,
    SUM(AmountUSD) as TotalUSD
FROM Airtels
WHERE CallNumber = '254722123456';

PRINT '';
PRINT '========================================';
PRINT 'TEST DATA READY!';
PRINT '========================================';
PRINT 'Test User Index: TEST001';
PRINT 'Test User Name: John Doe';
PRINT 'Test Phone: 254722123456';
PRINT '';
PRINT 'To test interim billing:';
PRINT '1. Go to /Admin/CallLogStaging';
PRINT '2. Click Consolidate New Batch';
PRINT '3. Select Staff Separation (Interim) tab';
PRINT '4. Enter Index Number: TEST001';
PRINT '========================================';