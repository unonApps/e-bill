-- Design Analysis: Unique Phone Numbers, Duplicate Emails
USE [TABDB]
GO

-- First, check current constraints
PRINT '==========================================================';
PRINT 'CURRENT UNIQUE CONSTRAINTS ON EBILLUSERS';
PRINT '==========================================================';

SELECT
    i.name AS ConstraintName,
    c.name AS ColumnName,
    i.is_unique AS IsUnique,
    i.type_desc AS ConstraintType
FROM sys.indexes i
INNER JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
INNER JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
WHERE i.object_id = OBJECT_ID('EbillUsers')
  AND (i.is_unique = 1 OR i.is_primary_key = 1);

-- Check current data for duplicates
PRINT '';
PRINT 'CURRENT DATA ANALYSIS:';
PRINT '----------------------';

-- Check duplicate emails
PRINT '';
PRINT 'Duplicate Emails in current data:';
SELECT Email, COUNT(*) as Count
FROM EbillUsers
GROUP BY Email
HAVING COUNT(*) > 1;

-- Check duplicate phone numbers
PRINT '';
PRINT 'Duplicate Phone Numbers in current data:';
SELECT OfficialMobileNumber, COUNT(*) as Count
FROM EbillUsers
WHERE OfficialMobileNumber IS NOT NULL
GROUP BY OfficialMobileNumber
HAVING COUNT(*) > 1;

PRINT '';
PRINT '==========================================================';
PRINT 'PROPOSED DESIGN: UNIQUE PHONE, DUPLICATE EMAIL';
PRINT '==========================================================';
GO

-- OPTION 1: Modify constraints for unique phone, non-unique email
/*
-- Remove unique constraint on Email
IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_EbillUsers_Email' AND object_id = OBJECT_ID('EbillUsers'))
    DROP INDEX IX_EbillUsers_Email ON EbillUsers;

-- Add unique constraint on OfficialMobileNumber (excluding NULLs)
CREATE UNIQUE NONCLUSTERED INDEX IX_EbillUsers_PhoneNumber
ON EbillUsers(OfficialMobileNumber)
WHERE OfficialMobileNumber IS NOT NULL;
*/

-- OPTION 2: Better approach - Separate tables for different concerns
IF OBJECT_ID('dbo.PhoneBillingAccounts') IS NULL
BEGIN
    CREATE TABLE PhoneBillingAccounts (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        PhoneNumber NVARCHAR(20) NOT NULL UNIQUE, -- Each phone is unique
        IndexNumber NVARCHAR(50) NOT NULL,        -- Who owns it
        AccountType NVARCHAR(50),                 -- Personal, Official, Shared
        AssignedDate DATETIME NOT NULL DEFAULT GETUTCDATE(),
        IsActive BIT DEFAULT 1,

        -- Allow multiple phones per user
        INDEX IX_PhoneBilling_IndexNumber (IndexNumber),

        -- Link to user
        CONSTRAINT FK_PhoneBilling_User
            FOREIGN KEY (IndexNumber)
            REFERENCES EbillUsers(IndexNumber)
    );

    PRINT 'Created PhoneBillingAccounts table';
END
GO

PRINT '';
PRINT '==========================================================';
PRINT 'MY RECOMMENDATION - PROS & CONS:';
PRINT '==========================================================';
PRINT '';
PRINT 'UNIQUE PHONE + DUPLICATE EMAIL:';
PRINT '--------------------------------';
PRINT 'PROS:';
PRINT '✓ Phone number is the billing identifier - should be unique';
PRINT '✓ Prevents billing confusion (one phone = one account)';
PRINT '✓ Some users share emails (assistants, departments)';
PRINT '✓ Generic emails like info@office.un.org used by multiple people';
PRINT '';
PRINT 'CONS:';
PRINT '✗ Email typically used for authentication/login';
PRINT '✗ Password reset issues with duplicate emails';
PRINT '✗ Communication problems - which user gets notifications?';
PRINT '✗ Goes against standard user management patterns';
PRINT '';
PRINT 'MY RECOMMENDATION:';
PRINT '==================';
PRINT '1. Keep Email UNIQUE for authentication/communication';
PRINT '2. Make OfficialMobileNumber UNIQUE (where not NULL)';
PRINT '3. For shared emails, use pattern: user.name+dept@un.org';
PRINT '4. For multiple phones, use separate PhoneBillingAccounts table';
PRINT '';
PRINT 'BETTER SOLUTION FOR YOUR SCENARIO:';
PRINT '===================================';
GO

-- Proposed solution that handles both requirements
CREATE OR ALTER PROCEDURE sp_CreateBillingUser
    @FirstName NVARCHAR(100),
    @LastName NVARCHAR(100),
    @IndexNumber NVARCHAR(50),
    @Email NVARCHAR(256),
    @SharedEmail BIT = 0,  -- Flag for shared emails
    @PhoneNumber NVARCHAR(20)
AS
BEGIN
    DECLARE @ActualEmail NVARCHAR(256) = @Email;

    -- If shared email, make it unique by appending IndexNumber
    IF @SharedEmail = 1 AND EXISTS (SELECT 1 FROM EbillUsers WHERE Email = @Email)
    BEGIN
        SET @ActualEmail = REPLACE(@Email, '@', '+' + @IndexNumber + '@');
        PRINT 'Shared email detected. Using: ' + @ActualEmail;
    END

    -- Insert user
    INSERT INTO EbillUsers (
        FirstName, LastName, IndexNumber, Email,
        OfficialMobileNumber, IsActive, CreatedDate
    )
    VALUES (
        @FirstName, @LastName, @IndexNumber, @ActualEmail,
        @PhoneNumber, 1, GETUTCDATE()
    );

    PRINT 'User created successfully';
END
GO

-- Example of handling duplicate emails in import
PRINT '';
PRINT 'EXAMPLE: Handling duplicate emails during import';
PRINT '------------------------------------------------';
/*
-- During CSV import, when you encounter duplicate emails:

WITH EmailCounts AS (
    SELECT Email, COUNT(*) as DupeCount
    FROM EbillUsers_Staging
    GROUP BY Email
)
UPDATE s
SET s.Email =
    CASE
        WHEN ec.DupeCount > 1 THEN
            -- Make email unique by adding IndexNumber
            REPLACE(s.Email, '@', '.' + s.IndexNumber + '@')
        ELSE
            s.Email
    END
FROM EbillUsers_Staging s
INNER JOIN EmailCounts ec ON s.Email = ec.Email;
*/

PRINT '';
PRINT 'REAL-WORLD SCENARIOS:';
PRINT '=====================';
PRINT '';
PRINT 'Scenario 1: Reception Desk Phone';
PRINT '- Phone: 21000';
PRINT '- Used by: Multiple receptionists on shifts';
PRINT '- Solution: Create as shared phone, bills split by time/shift';
PRINT '';
PRINT 'Scenario 2: Department Email';
PRINT '- Email: finance@office.un.org';
PRINT '- Used by: 5 people in finance';
PRINT '- Solution: Create individual accounts with finance+name@office.un.org';
PRINT '';
PRINT 'Scenario 3: Executive with Assistant';
PRINT '- Executive has phone but assistant manages email';
PRINT '- Solution: Separate email for billing notifications';
GO