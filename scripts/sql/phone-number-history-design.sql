-- Design for handling phone number changes over time
-- This maintains historical accuracy while tracking current assignments

USE [TABDB]
GO

-- 1. Create PhoneNumberHistory table to track all number assignments
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PhoneNumberHistory]'))
BEGIN
    CREATE TABLE [dbo].[PhoneNumberHistory] (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        IndexNumber NVARCHAR(50) NOT NULL,  -- User's permanent ID
        PhoneNumber NVARCHAR(20) NOT NULL,
        AssignedDate DATETIME NOT NULL,
        UnassignedDate DATETIME NULL,       -- NULL means currently active
        Reason NVARCHAR(200),                -- Why the change happened
        CreatedBy NVARCHAR(100),
        CreatedDate DATETIME DEFAULT GETUTCDATE(),

        -- Constraints
        CONSTRAINT FK_PhoneHistory_User FOREIGN KEY (IndexNumber)
            REFERENCES EbillUsers(IndexNumber) ON DELETE CASCADE,

        -- Index for performance
        INDEX IX_PhoneHistory_IndexNumber (IndexNumber),
        INDEX IX_PhoneHistory_PhoneNumber (PhoneNumber),
        INDEX IX_PhoneHistory_ActiveDates (AssignedDate, UnassignedDate)
    );

    PRINT 'Created PhoneNumberHistory table';
END
GO

-- 2. Create view to get current phone assignments
CREATE OR ALTER VIEW vw_CurrentPhoneAssignments
AS
SELECT
    ph.IndexNumber,
    ph.PhoneNumber as CurrentPhoneNumber,
    ph.AssignedDate,
    u.FirstName,
    u.LastName,
    u.Email,
    u.Location,
    org.Name as Organization,
    off.Name as Office
FROM PhoneNumberHistory ph
INNER JOIN EbillUsers u ON u.IndexNumber = ph.IndexNumber
LEFT JOIN Organizations org ON u.OrganizationId = org.Id
LEFT JOIN Offices off ON u.OfficeId = off.Id
WHERE ph.UnassignedDate IS NULL;  -- Only current assignments
GO

-- 3. Create stored procedure to handle phone number changes
CREATE OR ALTER PROCEDURE sp_ChangePhoneNumber
    @IndexNumber NVARCHAR(50),
    @NewPhoneNumber NVARCHAR(20),
    @Reason NVARCHAR(200) = 'Number reassignment',
    @ChangedBy NVARCHAR(100) = 'System'
AS
BEGIN
    BEGIN TRANSACTION;

    BEGIN TRY
        -- Check if user exists
        IF NOT EXISTS (SELECT 1 FROM EbillUsers WHERE IndexNumber = @IndexNumber)
        BEGIN
            RAISERROR('User with IndexNumber %s not found', 16, 1, @IndexNumber);
            RETURN;
        END

        -- Close current phone assignment
        UPDATE PhoneNumberHistory
        SET UnassignedDate = GETUTCDATE()
        WHERE IndexNumber = @IndexNumber
          AND UnassignedDate IS NULL;

        -- Create new phone assignment
        INSERT INTO PhoneNumberHistory (IndexNumber, PhoneNumber, AssignedDate, Reason, CreatedBy)
        VALUES (@IndexNumber, @NewPhoneNumber, GETUTCDATE(), @Reason, @ChangedBy);

        -- Update the EbillUsers table
        UPDATE EbillUsers
        SET OfficialMobileNumber = @NewPhoneNumber,
            LastModifiedDate = GETUTCDATE()
        WHERE IndexNumber = @IndexNumber;

        COMMIT TRANSACTION;

        PRINT 'Phone number changed successfully for IndexNumber: ' + @IndexNumber;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END
GO

-- 4. Function to get phone number at a specific date (for historical billing)
CREATE OR ALTER FUNCTION fn_GetPhoneNumberAtDate
(
    @IndexNumber NVARCHAR(50),
    @Date DATETIME
)
RETURNS NVARCHAR(20)
AS
BEGIN
    DECLARE @PhoneNumber NVARCHAR(20);

    SELECT TOP 1 @PhoneNumber = PhoneNumber
    FROM PhoneNumberHistory
    WHERE IndexNumber = @IndexNumber
      AND AssignedDate <= @Date
      AND (UnassignedDate IS NULL OR UnassignedDate > @Date)
    ORDER BY AssignedDate DESC;

    RETURN @PhoneNumber;
END
GO

-- 5. Populate initial history from current data
PRINT 'Populating initial phone history from EbillUsers...';

INSERT INTO PhoneNumberHistory (IndexNumber, PhoneNumber, AssignedDate, Reason, CreatedBy)
SELECT
    IndexNumber,
    OfficialMobileNumber,
    ISNULL(CreatedDate, GETUTCDATE()),
    'Initial import',
    'System'
FROM EbillUsers
WHERE OfficialMobileNumber IS NOT NULL
  AND NOT EXISTS (
    SELECT 1 FROM PhoneNumberHistory ph
    WHERE ph.IndexNumber = EbillUsers.IndexNumber
      AND ph.PhoneNumber = EbillUsers.OfficialMobileNumber
  );

PRINT 'Added ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' phone history records.';
GO

-- 6. Example queries for billing reconciliation

-- Query 1: Find who had a specific phone number on a specific date
-- This is crucial for matching historical bills
/*
DECLARE @PhoneNumber NVARCHAR(20) = '21236';
DECLARE @BillDate DATETIME = '2024-06-15';

SELECT
    u.IndexNumber,
    u.FirstName,
    u.LastName,
    ph.PhoneNumber,
    ph.AssignedDate,
    ph.UnassignedDate,
    'Active on bill date' as Status
FROM PhoneNumberHistory ph
INNER JOIN EbillUsers u ON u.IndexNumber = ph.IndexNumber
WHERE ph.PhoneNumber = @PhoneNumber
  AND ph.AssignedDate <= @BillDate
  AND (ph.UnassignedDate IS NULL OR ph.UnassignedDate > @BillDate);
*/

-- Query 2: Get complete phone history for a user
/*
DECLARE @UserIndex NVARCHAR(50) = '120329';

SELECT
    ph.PhoneNumber,
    ph.AssignedDate,
    ph.UnassignedDate,
    DATEDIFF(DAY, ph.AssignedDate, ISNULL(ph.UnassignedDate, GETUTCDATE())) as DaysUsed,
    ph.Reason,
    CASE WHEN ph.UnassignedDate IS NULL THEN 'Current' ELSE 'Historical' END as Status
FROM PhoneNumberHistory ph
WHERE ph.IndexNumber = @UserIndex
ORDER BY ph.AssignedDate DESC;
*/

-- 7. Update billing tables to use IndexNumber for relationships
PRINT '';
PRINT 'IMPORTANT: Billing tables should be updated to store:';
PRINT '1. IndexNumber (for permanent user reference)';
PRINT '2. PhoneNumber (for the actual number on the bill)';
PRINT '3. BillDate (to determine who should be charged)';
PRINT '';
PRINT 'Example structure for billing tables:';

/*
ALTER TABLE PSTNs ADD IndexNumber NVARCHAR(50);
ALTER TABLE PrivateWires ADD IndexNumber NVARCHAR(50);
ALTER TABLE Safaricom ADD IndexNumber NVARCHAR(50);
ALTER TABLE Airtel ADD IndexNumber NVARCHAR(50);

-- Then populate IndexNumber based on phone number and bill date:
UPDATE PSTNs
SET IndexNumber = dbo.fn_GetUserByPhoneAtDate(CallingNumber, BillDate)
WHERE IndexNumber IS NULL;
*/

PRINT 'Phone number history system setup complete!';
GO