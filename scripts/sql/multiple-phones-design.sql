-- Design for handling users with multiple phone numbers
-- Common scenarios:
-- 1. User has desk phone + mobile
-- 2. User has multiple mobiles (personal + official)
-- 3. Shared phones in conference rooms/reception

USE [TABDB]
GO

-- APPROACH: Create UserPhones table for many-to-many relationship
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[UserPhones]'))
BEGIN
    CREATE TABLE [dbo].[UserPhones] (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        IndexNumber NVARCHAR(50) NOT NULL,
        PhoneNumber NVARCHAR(20) NOT NULL,
        PhoneType NVARCHAR(50) NOT NULL, -- 'Mobile', 'Desk', 'Alternative', 'Temporary'
        IsPrimary BIT DEFAULT 0,          -- Main contact number
        IsActive BIT DEFAULT 1,
        AssignedDate DATETIME NOT NULL DEFAULT GETUTCDATE(),
        UnassignedDate DATETIME NULL,
        Notes NVARCHAR(500),              -- e.g., "Conference room phone", "Shared with team"
        CreatedDate DATETIME DEFAULT GETUTCDATE(),

        -- Ensure one primary phone per user
        CONSTRAINT UQ_OnePrimaryPerUser UNIQUE (IndexNumber, IsPrimary) WHERE IsPrimary = 1,

        -- Prevent duplicate active assignments
        CONSTRAINT UQ_ActivePhoneAssignment UNIQUE (IndexNumber, PhoneNumber) WHERE IsActive = 1,

        -- Indexes for performance
        INDEX IX_UserPhones_IndexNumber (IndexNumber),
        INDEX IX_UserPhones_PhoneNumber (PhoneNumber),
        INDEX IX_UserPhones_Active (IsActive)
    );

    PRINT 'Created UserPhones table for multiple phone management';
END
GO

-- View to see all active phones per user
CREATE OR ALTER VIEW vw_UserPhonesSummary
AS
SELECT
    u.IndexNumber,
    u.FirstName + ' ' + u.LastName as FullName,
    u.Location,
    STRING_AGG(
        up.PhoneNumber + ' (' + up.PhoneType + CASE WHEN up.IsPrimary = 1 THEN ', Primary' ELSE '' END + ')',
        '; '
    ) as AllPhones,
    COUNT(up.PhoneNumber) as TotalPhones,
    MAX(CASE WHEN up.IsPrimary = 1 THEN up.PhoneNumber END) as PrimaryPhone
FROM EbillUsers u
LEFT JOIN UserPhones up ON u.IndexNumber = up.IndexNumber AND up.IsActive = 1
GROUP BY u.IndexNumber, u.FirstName, u.LastName, u.Location;
GO

-- Stored procedure to assign additional phone to user
CREATE OR ALTER PROCEDURE sp_AssignAdditionalPhone
    @IndexNumber NVARCHAR(50),
    @PhoneNumber NVARCHAR(20),
    @PhoneType NVARCHAR(50) = 'Mobile',
    @IsPrimary BIT = 0,
    @Notes NVARCHAR(500) = NULL
AS
BEGIN
    BEGIN TRANSACTION;

    BEGIN TRY
        -- If setting as primary, remove primary flag from others
        IF @IsPrimary = 1
        BEGIN
            UPDATE UserPhones
            SET IsPrimary = 0
            WHERE IndexNumber = @IndexNumber AND IsActive = 1;
        END

        -- Check if this phone is already assigned to someone else
        IF EXISTS (
            SELECT 1 FROM UserPhones
            WHERE PhoneNumber = @PhoneNumber
              AND IsActive = 1
              AND IndexNumber != @IndexNumber
        )
        BEGIN
            RAISERROR('Phone %s is already assigned to another user', 16, 1, @PhoneNumber);
            ROLLBACK;
            RETURN;
        END

        -- Assign the phone
        INSERT INTO UserPhones (IndexNumber, PhoneNumber, PhoneType, IsPrimary, IsActive, Notes)
        VALUES (@IndexNumber, @PhoneNumber, @PhoneType, @IsPrimary, 1, @Notes);

        -- Update EbillUsers primary phone if needed
        IF @IsPrimary = 1
        BEGIN
            UPDATE EbillUsers
            SET OfficialMobileNumber = @PhoneNumber
            WHERE IndexNumber = @IndexNumber;
        END

        COMMIT TRANSACTION;
        PRINT 'Phone ' + @PhoneNumber + ' assigned successfully to ' + @IndexNumber;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END
GO

-- BILLING CONSIDERATION: How to handle bills for multiple phones
-- APPROACH 1: Link bills to phone numbers, then aggregate by user

-- Example billing query that handles multiple phones per user
CREATE OR ALTER VIEW vw_UserBillingSummary
AS
WITH PhoneBills AS (
    -- Combine bills from all sources
    SELECT 'PSTN' as Source, CallingNumber as PhoneNumber, CallCharges as Amount, CallDate as BillDate
    FROM PSTNs
    UNION ALL
    SELECT 'PrivateWire', ServiceNumber, TotalAmount, BillingDate
    FROM PrivateWires
    UNION ALL
    SELECT 'Safaricom', CallingNumber, TotalCost, BillDate
    FROM Safaricom
    UNION ALL
    SELECT 'Airtel', CallingNumber, TotalAmount, BillDate
    FROM Airtel
)
SELECT
    u.IndexNumber,
    u.FirstName + ' ' + u.LastName as UserName,
    YEAR(pb.BillDate) as BillYear,
    MONTH(pb.BillDate) as BillMonth,
    up.PhoneNumber,
    up.PhoneType,
    pb.Source,
    SUM(pb.Amount) as TotalAmount,
    COUNT(*) as CallCount
FROM PhoneBills pb
INNER JOIN UserPhones up ON up.PhoneNumber = pb.PhoneNumber
INNER JOIN EbillUsers u ON u.IndexNumber = up.IndexNumber
WHERE pb.BillDate >= up.AssignedDate
  AND (up.UnassignedDate IS NULL OR pb.BillDate < up.UnassignedDate)
GROUP BY u.IndexNumber, u.FirstName, u.LastName, YEAR(pb.BillDate), MONTH(pb.BillDate),
         up.PhoneNumber, up.PhoneType, pb.Source;
GO

-- Example: Import current phones from CSV data
PRINT 'Example of handling multiple phones from import:';
PRINT '=============================================';

/*
-- If user has multiple numbers in CSV (separated by semicolon or multiple rows)
-- Example data:
-- IndexNumber: 120329, Phones: "21236; 35000"
-- OR
-- Row 1: IndexNumber: 120329, Phone: 21236
-- Row 2: IndexNumber: 120329, Phone: 35000

-- Process them like this:
INSERT INTO UserPhones (IndexNumber, PhoneNumber, PhoneType, IsPrimary)
SELECT
    IndexNumber,
    PhoneNumber,
    CASE
        WHEN PhoneNumber LIKE '2%' THEN 'Desk'
        WHEN PhoneNumber LIKE '3%' THEN 'Extension'
        WHEN PhoneNumber LIKE '07%' THEN 'Mobile'
        ELSE 'Other'
    END as PhoneType,
    CASE
        WHEN ROW_NUMBER() OVER (PARTITION BY IndexNumber ORDER BY PhoneNumber) = 1
        THEN 1 ELSE 0
    END as IsPrimary
FROM (
    -- Your staging data here
    SELECT DISTINCT IndexNumber, OfficialMobileNumber as PhoneNumber
    FROM EbillUsers_Staging
    WHERE OfficialMobileNumber IS NOT NULL
) as phones
WHERE NOT EXISTS (
    SELECT 1 FROM UserPhones up
    WHERE up.IndexNumber = phones.IndexNumber
      AND up.PhoneNumber = phones.PhoneNumber
);
*/

-- Sample queries for common scenarios
PRINT '';
PRINT 'USEFUL QUERIES:';
PRINT '==============';
PRINT '';
PRINT '-- Find users with multiple phones:';
PRINT 'SELECT * FROM vw_UserPhonesSummary WHERE TotalPhones > 1;';
PRINT '';
PRINT '-- Get all bills for a user across all their phones:';
PRINT 'SELECT * FROM vw_UserBillingSummary WHERE IndexNumber = ''120329'';';
PRINT '';
PRINT '-- Find shared phones (assigned to multiple users):';
PRINT 'SELECT PhoneNumber, COUNT(DISTINCT IndexNumber) as Users';
PRINT 'FROM UserPhones WHERE IsActive = 1';
PRINT 'GROUP BY PhoneNumber HAVING COUNT(DISTINCT IndexNumber) > 1;';

GO