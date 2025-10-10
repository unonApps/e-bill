-- Add UserPhone for TEST001 user
-- First check if user exists
IF EXISTS (SELECT 1 FROM EbillUsers WHERE IndexNumber = 'TEST001')
BEGIN
    -- Check if phone already assigned
    IF NOT EXISTS (SELECT 1 FROM UserPhones WHERE IndexNumber = 'TEST001' AND PhoneNumber = '254722123456')
    BEGIN
        INSERT INTO UserPhones (
            IndexNumber,
            PhoneNumber,
            PhoneType,
            IsPrimary,
            IsActive,
            AssignedDate,
            CreatedDate,
            CreatedBy,
            ClassOfServiceId
        )
        VALUES (
            'TEST001',
            '254722123456',
            'Mobile',
            1, -- IsPrimary
            1, -- IsActive
            DATEADD(month, -1, GETUTCDATE()), -- Assigned a month ago
            GETUTCDATE(),
            'TestDataScript',
            (SELECT TOP 1 Id FROM ClassOfServices) -- Get any available class of service
        );

        PRINT 'UserPhone created successfully for TEST001 with number 254722123456';
    END
    ELSE
    BEGIN
        PRINT 'UserPhone already exists for TEST001 with number 254722123456';
    END
END
ELSE
BEGIN
    PRINT 'ERROR: User TEST001 not found. Please create the user first.';
END

-- Verify the UserPhone was created
SELECT
    'UserPhone Record' as RecordType,
    up.IndexNumber,
    eu.FirstName + ' ' + eu.LastName as UserName,
    up.PhoneNumber,
    up.PhoneType,
    up.IsPrimary,
    up.IsActive,
    up.AssignedDate
FROM UserPhones up
LEFT JOIN EbillUsers eu ON eu.IndexNumber = up.IndexNumber
WHERE up.IndexNumber = 'TEST001';

-- Show summary of call logs for this phone
SELECT
    'Call Logs Summary' as Info,
    'Total Safaricom Calls' as Description,
    COUNT(*) as Count,
    SUM(AmountUSD) as TotalUSD
FROM Safaricoms
WHERE CallNumber = '254722123456'

UNION ALL

SELECT
    'Call Logs Summary' as Info,
    'Total Airtel Calls' as Description,
    COUNT(*) as Count,
    SUM(AmountUSD) as TotalUSD
FROM Airtels
WHERE CallNumber = '254722123456';

PRINT '';
PRINT '========================================';
PRINT 'INTERIM BILLING TEST DATA COMPLETE!';
PRINT '========================================';
PRINT 'Test User: TEST001 (John Doe)';
PRINT 'Phone Number: 254722123456';
PRINT 'Call Records: Check summary above';
PRINT '';
PRINT 'Ready to test interim billing feature!';
PRINT '========================================';