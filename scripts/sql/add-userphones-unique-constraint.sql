-- ================================================================
-- ADD UNIQUE CONSTRAINT TO USERPHONES TABLE
-- ================================================================
-- Ensures each phone number can only be assigned to one active user at a time

USE TABDB;
GO

PRINT 'Adding unique constraint to UserPhones table...';
PRINT '';

-- First, check for any duplicate active phone numbers
PRINT 'Checking for duplicate active phone numbers...';

WITH DuplicatePhones AS (
    SELECT
        PhoneNumber,
        COUNT(*) as Count
    FROM UserPhones
    WHERE IsActive = 1
    GROUP BY PhoneNumber
    HAVING COUNT(*) > 1
)
SELECT
    'WARNING: Duplicate active phone number: ' + PhoneNumber + ' (appears ' + CAST(Count as VARCHAR) + ' times)'
FROM DuplicatePhones;

-- Check if any duplicates exist
IF EXISTS (
    SELECT 1
    FROM UserPhones
    WHERE IsActive = 1
    GROUP BY PhoneNumber
    HAVING COUNT(*) > 1
)
BEGIN
    PRINT '';
    PRINT 'ERROR: Cannot add unique constraint - duplicate active phone numbers exist!';
    PRINT 'Please deactivate duplicate phone numbers first.';

    -- Show the duplicates with user details
    SELECT
        up.PhoneNumber,
        up.IndexNumber,
        eu.FirstName + ' ' + eu.LastName as UserName,
        up.PhoneType,
        up.IsActive,
        up.AssignedDate
    FROM UserPhones up
    JOIN EbillUsers eu ON up.IndexNumber = eu.IndexNumber
    WHERE up.PhoneNumber IN (
        SELECT PhoneNumber
        FROM UserPhones
        WHERE IsActive = 1
        GROUP BY PhoneNumber
        HAVING COUNT(*) > 1
    )
    ORDER BY up.PhoneNumber, up.AssignedDate;
END
ELSE
BEGIN
    -- No duplicates, safe to add constraint

    -- First drop existing index if it exists
    IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_UserPhones_PhoneNumber_Active_Unique')
    BEGIN
        DROP INDEX IX_UserPhones_PhoneNumber_Active_Unique ON UserPhones;
        PRINT 'Dropped existing index.';
    END

    -- Create unique filtered index for active phone numbers only
    -- This allows the same phone number to exist as inactive (for history)
    -- But prevents the same phone number from being active for multiple users
    CREATE UNIQUE INDEX IX_UserPhones_PhoneNumber_Active_Unique
    ON UserPhones(PhoneNumber)
    WHERE IsActive = 1;

    PRINT 'SUCCESS: Added unique constraint for active phone numbers.';
    PRINT '';
    PRINT 'Rules enforced:';
    PRINT '✓ Each phone number can only be ACTIVE for one user at a time';
    PRINT '✓ The same phone number can exist as INACTIVE multiple times (for history)';
    PRINT '✓ When reassigning a phone, first deactivate it for the old user';
END

GO

-- Additional check: Show current phone number assignments
PRINT '';
PRINT 'Current active phone assignments:';

SELECT
    up.PhoneNumber,
    up.IndexNumber,
    eu.FirstName + ' ' + eu.LastName as UserName,
    up.PhoneType,
    up.IsPrimary,
    up.AssignedDate,
    up.Location
FROM UserPhones up
JOIN EbillUsers eu ON up.IndexNumber = eu.IndexNumber
WHERE up.IsActive = 1
ORDER BY up.PhoneNumber;

PRINT '';
PRINT 'Phone Number Assignment Rules:';
PRINT '-------------------------------';
PRINT '1. Before assigning a phone to a new user:';
PRINT '   - Check if it''s already active for another user';
PRINT '   - If yes, deactivate it first (set IsActive = 0, UnassignedDate = GETDATE())';
PRINT '   - Then create a new record for the new user';
PRINT '';
PRINT '2. This maintains history of who had which phone when';
PRINT '3. Helps with auditing and historical billing queries';