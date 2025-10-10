-- Implementation: One User, Multiple Phone Numbers
-- Real-world scenario: User has desk phone (21236), mobile (0722123456), extension (35000)

USE [TABDB]
GO

PRINT '================================================================';
PRINT 'IMPLEMENTING MULTIPLE PHONES PER USER SYSTEM';
PRINT '================================================================';
PRINT '';

-- STEP 1: Create UserPhones table (Many-to-Many relationship)
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[UserPhones]'))
BEGIN
    CREATE TABLE [dbo].[UserPhones] (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        IndexNumber NVARCHAR(50) NOT NULL,
        PhoneNumber NVARCHAR(20) NOT NULL,
        PhoneType NVARCHAR(50) NOT NULL DEFAULT 'Mobile', -- 'Mobile', 'Desk', 'Extension', 'Home', 'Temporary'
        IsPrimary BIT DEFAULT 0,
        IsActive BIT DEFAULT 1,
        AssignedDate DATETIME NOT NULL DEFAULT GETUTCDATE(),
        UnassignedDate DATETIME NULL,
        Location NVARCHAR(200),  -- Where the phone physically is
        Notes NVARCHAR(500),      -- Any special notes
        CreatedBy NVARCHAR(100) DEFAULT SYSTEM_USER,
        CreatedDate DATETIME DEFAULT GETUTCDATE(),

        -- Constraints
        CONSTRAINT FK_UserPhones_User FOREIGN KEY (IndexNumber)
            REFERENCES EbillUsers(IndexNumber) ON DELETE CASCADE,

        -- Indexes
        INDEX IX_UserPhones_IndexNumber (IndexNumber),
        INDEX IX_UserPhones_PhoneNumber (PhoneNumber),
        INDEX IX_UserPhones_Active (IsActive) WHERE IsActive = 1,
        INDEX IX_UserPhones_Dates (AssignedDate, UnassignedDate)
    );

    PRINT '✓ Created UserPhones table';
END
ELSE
BEGIN
    PRINT '! UserPhones table already exists';
END
GO

-- STEP 2: Migrate existing phone numbers to UserPhones
PRINT '';
PRINT 'Migrating existing phone numbers...';

-- Insert existing phones from EbillUsers
INSERT INTO UserPhones (IndexNumber, PhoneNumber, PhoneType, IsPrimary, IsActive, Notes)
SELECT
    IndexNumber,
    OfficialMobileNumber,
    CASE
        WHEN OfficialMobileNumber LIKE '07%' THEN 'Mobile'
        WHEN OfficialMobileNumber LIKE '2%' THEN 'Desk'
        WHEN OfficialMobileNumber LIKE '3%' THEN 'Extension'
        ELSE 'Other'
    END,
    1, -- Set as primary since it's their official number
    1,
    'Migrated from OfficialMobileNumber'
FROM EbillUsers
WHERE OfficialMobileNumber IS NOT NULL
  AND OfficialMobileNumber != ''
  AND NOT EXISTS (
    SELECT 1 FROM UserPhones up
    WHERE up.IndexNumber = EbillUsers.IndexNumber
      AND up.PhoneNumber = EbillUsers.OfficialMobileNumber
  );

PRINT 'Migrated ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' phone numbers';
GO

-- STEP 3: Create view to see all user phones
CREATE OR ALTER VIEW vw_UserPhonesDetail
AS
SELECT
    u.IndexNumber,
    u.FirstName,
    u.LastName,
    u.Email,
    u.Location as UserLocation,
    up.PhoneNumber,
    up.PhoneType,
    up.IsPrimary,
    up.Location as PhoneLocation,
    up.AssignedDate,
    up.IsActive,
    org.Name as Organization,
    ofc.Name as Office
FROM EbillUsers u
LEFT JOIN UserPhones up ON u.IndexNumber = up.IndexNumber
LEFT JOIN Organizations org ON u.OrganizationId = org.Id
LEFT JOIN Offices ofc ON u.OfficeId = ofc.Id;
GO

-- STEP 4: Create procedure to assign additional phone
CREATE OR ALTER PROCEDURE sp_AssignPhone
    @IndexNumber NVARCHAR(50),
    @PhoneNumber NVARCHAR(20),
    @PhoneType NVARCHAR(50) = 'Mobile',
    @Location NVARCHAR(200) = NULL,
    @MakePrimary BIT = 0,
    @Notes NVARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- Validate user exists
    IF NOT EXISTS (SELECT 1 FROM EbillUsers WHERE IndexNumber = @IndexNumber)
    BEGIN
        RAISERROR('User with IndexNumber %s does not exist', 16, 1, @IndexNumber);
        RETURN;
    END

    -- Check if this phone is already active for this user
    IF EXISTS (SELECT 1 FROM UserPhones
               WHERE IndexNumber = @IndexNumber
                 AND PhoneNumber = @PhoneNumber
                 AND IsActive = 1)
    BEGIN
        PRINT 'Phone ' + @PhoneNumber + ' is already assigned to this user';
        RETURN;
    END

    BEGIN TRANSACTION;
    BEGIN TRY
        -- If making primary, remove primary from others
        IF @MakePrimary = 1
        BEGIN
            UPDATE UserPhones
            SET IsPrimary = 0
            WHERE IndexNumber = @IndexNumber
              AND IsActive = 1;

            -- Also update the main table
            UPDATE EbillUsers
            SET OfficialMobileNumber = @PhoneNumber
            WHERE IndexNumber = @IndexNumber;
        END

        -- Check if phone was previously assigned to this user (reactivate)
        IF EXISTS (SELECT 1 FROM UserPhones
                   WHERE IndexNumber = @IndexNumber
                     AND PhoneNumber = @PhoneNumber
                     AND IsActive = 0)
        BEGIN
            UPDATE UserPhones
            SET IsActive = 1,
                UnassignedDate = NULL,
                AssignedDate = GETUTCDATE(),
                IsPrimary = @MakePrimary,
                PhoneType = @PhoneType,
                Location = ISNULL(@Location, Location),
                Notes = ISNULL(@Notes, Notes)
            WHERE IndexNumber = @IndexNumber
              AND PhoneNumber = @PhoneNumber;

            PRINT 'Phone ' + @PhoneNumber + ' reactivated for user ' + @IndexNumber;
        END
        ELSE
        BEGIN
            -- New assignment
            INSERT INTO UserPhones (IndexNumber, PhoneNumber, PhoneType, IsPrimary, Location, Notes)
            VALUES (@IndexNumber, @PhoneNumber, @PhoneType, @MakePrimary, @Location, @Notes);

            PRINT 'Phone ' + @PhoneNumber + ' assigned to user ' + @IndexNumber;
        END

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END
GO

-- STEP 5: Create function to link bills to users
CREATE OR ALTER FUNCTION fn_GetUserByPhone
(
    @PhoneNumber NVARCHAR(20),
    @BillDate DATETIME
)
RETURNS NVARCHAR(50)
AS
BEGIN
    DECLARE @IndexNumber NVARCHAR(50);

    SELECT TOP 1 @IndexNumber = IndexNumber
    FROM UserPhones
    WHERE PhoneNumber = @PhoneNumber
      AND IsActive = 1
      AND AssignedDate <= @BillDate
      AND (UnassignedDate IS NULL OR UnassignedDate > @BillDate)
    ORDER BY IsPrimary DESC, AssignedDate DESC;

    RETURN @IndexNumber;
END
GO

-- STEP 6: Update billing tables to use UserPhones
PRINT '';
PRINT 'Creating billing view that handles multiple phones...';

CREATE OR ALTER VIEW vw_ConsolidatedBills
AS
-- Get all bills and match them to users through UserPhones
WITH AllBills AS (
    SELECT
        'PSTN' as BillType,
        CallingNumber as PhoneNumber,
        CallDate as BillDate,
        CallCharges as Amount,
        Id as BillId
    FROM PSTNs

    UNION ALL

    SELECT
        'PrivateWire',
        ServiceNumber,
        BillingDate,
        TotalAmount,
        Id
    FROM PrivateWires

    UNION ALL

    SELECT
        'Safaricom',
        CallingNumber,
        BillDate,
        TotalCost,
        Id
    FROM Safaricom

    UNION ALL

    SELECT
        'Airtel',
        CallingNumber,
        BillDate,
        TotalAmount,
        Id
    FROM Airtel
)
SELECT
    ab.BillType,
    ab.PhoneNumber,
    ab.BillDate,
    ab.Amount,
    ab.BillId,
    up.IndexNumber,
    u.FirstName + ' ' + u.LastName as UserName,
    up.PhoneType,
    up.IsPrimary,
    u.Email,
    org.Name as Organization,
    ofc.Name as Office
FROM AllBills ab
INNER JOIN UserPhones up ON up.PhoneNumber = ab.PhoneNumber
    AND up.AssignedDate <= ab.BillDate
    AND (up.UnassignedDate IS NULL OR up.UnassignedDate > ab.BillDate)
INNER JOIN EbillUsers u ON u.IndexNumber = up.IndexNumber
LEFT JOIN Organizations org ON u.OrganizationId = org.Id
LEFT JOIN Offices ofc ON u.OfficeId = ofc.Id;
GO

-- STEP 7: Sample data and testing
PRINT '';
PRINT 'SAMPLE USAGE:';
PRINT '=============';
PRINT '';

-- Example: Assign multiple phones to a user
/*
-- User Stella Vuzo (120329) has:
-- 1. Desk phone: 21236
-- 2. Mobile: 0722345678
-- 3. Extension: 35000

EXEC sp_AssignPhone @IndexNumber = '120329', @PhoneNumber = '21236', @PhoneType = 'Desk', @Location = 'Upper Library', @MakePrimary = 1;
EXEC sp_AssignPhone @IndexNumber = '120329', @PhoneNumber = '0722345678', @PhoneType = 'Mobile';
EXEC sp_AssignPhone @IndexNumber = '120329', @PhoneNumber = '35000', @PhoneType = 'Extension', @Location = 'Upper Library';

-- View all phones for this user
SELECT * FROM vw_UserPhonesDetail WHERE IndexNumber = '120329';

-- Get all bills for this user across all phones
SELECT * FROM vw_ConsolidatedBills WHERE IndexNumber = '120329';
*/

-- STEP 8: Summary report
PRINT '';
PRINT 'SYSTEM STATUS:';
PRINT '==============';

SELECT
    (SELECT COUNT(DISTINCT IndexNumber) FROM UserPhones WHERE IsActive = 1) as UsersWithPhones,
    (SELECT COUNT(*) FROM UserPhones WHERE IsActive = 1) as TotalActivePhones,
    (SELECT COUNT(*) FROM UserPhones WHERE IsActive = 1 AND IsPrimary = 1) as PrimaryPhones,
    (SELECT COUNT(DISTINCT IndexNumber) FROM UserPhones WHERE IsActive = 1 GROUP BY IndexNumber HAVING COUNT(*) > 1) as UsersWithMultiplePhones;

-- Show users with multiple phones
PRINT '';
PRINT 'Users with multiple phones:';
SELECT TOP 10
    u.IndexNumber,
    u.FirstName + ' ' + u.LastName as UserName,
    COUNT(up.PhoneNumber) as PhoneCount,
    STRING_AGG(up.PhoneNumber + ' (' + up.PhoneType + ')', ', ') as Phones
FROM EbillUsers u
INNER JOIN UserPhones up ON u.IndexNumber = up.IndexNumber AND up.IsActive = 1
GROUP BY u.IndexNumber, u.FirstName, u.LastName
HAVING COUNT(up.PhoneNumber) > 1
ORDER BY COUNT(up.PhoneNumber) DESC;

PRINT '';
PRINT '================================================================';
PRINT 'IMPLEMENTATION COMPLETE!';
PRINT '================================================================';
PRINT '';
PRINT 'Key Features Implemented:';
PRINT '✓ Users can have unlimited phone numbers';
PRINT '✓ Each phone tracks: type, location, primary status, active dates';
PRINT '✓ Bills automatically link to correct user based on phone and date';
PRINT '✓ Historical tracking - know who had which phone when';
PRINT '✓ Consolidated billing view across all phones';
PRINT '';
PRINT 'Next Steps:';
PRINT '1. Run this script to create the structure';
PRINT '2. Use sp_AssignPhone to add additional phones to users';
PRINT '3. Query vw_ConsolidatedBills for complete billing view';
GO