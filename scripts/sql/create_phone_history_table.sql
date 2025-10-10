-- Script to create Phone Number History table
-- This tracks all phone numbers assigned to users over time

USE [TABDB];
GO

-- Create PhoneNumberHistory table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PhoneNumberHistory]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[PhoneNumberHistory] (
        [Id] int IDENTITY(1,1) NOT NULL,
        [EbillUserId] int NOT NULL,
        [PhoneNumber] nvarchar(20) NOT NULL,
        [AssignedDate] datetime2(7) NOT NULL,
        [UnassignedDate] datetime2(7) NULL,  -- NULL means currently active
        [IsActive] bit NOT NULL DEFAULT 1,
        [Reason] nvarchar(200) NULL,  -- Why the number changed
        [CreatedDate] datetime2(7) NOT NULL DEFAULT GETUTCDATE(),
        [CreatedBy] nvarchar(100) NULL,

        CONSTRAINT [PK_PhoneNumberHistory] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_PhoneNumberHistory_EbillUsers] FOREIGN KEY ([EbillUserId])
            REFERENCES [dbo].[EbillUsers] ([Id]) ON DELETE CASCADE
    );

    -- Create indexes
    CREATE NONCLUSTERED INDEX [IX_PhoneNumberHistory_PhoneNumber]
        ON [dbo].[PhoneNumberHistory] ([PhoneNumber] ASC);

    CREATE NONCLUSTERED INDEX [IX_PhoneNumberHistory_EbillUserId]
        ON [dbo].[PhoneNumberHistory] ([EbillUserId] ASC);

    CREATE NONCLUSTERED INDEX [IX_PhoneNumberHistory_Dates]
        ON [dbo].[PhoneNumberHistory] ([AssignedDate] ASC, [UnassignedDate] ASC);

    PRINT 'PhoneNumberHistory table created successfully.';
END
GO

-- Populate with current phone numbers from EbillUsers
INSERT INTO [dbo].[PhoneNumberHistory] (EbillUserId, PhoneNumber, AssignedDate, IsActive, CreatedBy)
SELECT
    Id,
    OfficialMobileNumber,
    ISNULL(CreatedDate, GETUTCDATE()),
    1,
    'Initial Migration'
FROM [dbo].[EbillUsers]
WHERE OfficialMobileNumber IS NOT NULL
    AND OfficialMobileNumber != ''
    AND NOT EXISTS (
        SELECT 1 FROM [dbo].[PhoneNumberHistory]
        WHERE EbillUserId = [dbo].[EbillUsers].Id
        AND PhoneNumber = [dbo].[EbillUsers].OfficialMobileNumber
    );

PRINT 'Current phone numbers migrated to history table.';
GO

-- Create trigger to track phone number changes
IF EXISTS (SELECT * FROM sys.triggers WHERE name = 'TR_EbillUsers_PhoneNumberChange')
    DROP TRIGGER [dbo].[TR_EbillUsers_PhoneNumberChange];
GO

CREATE TRIGGER [dbo].[TR_EbillUsers_PhoneNumberChange]
ON [dbo].[EbillUsers]
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    -- Check if OfficialMobileNumber changed
    IF UPDATE(OfficialMobileNumber)
    BEGIN
        -- Deactivate old phone numbers
        UPDATE pnh
        SET
            IsActive = 0,
            UnassignedDate = GETUTCDATE()
        FROM [dbo].[PhoneNumberHistory] pnh
        INNER JOIN deleted d ON pnh.EbillUserId = d.Id
        WHERE pnh.IsActive = 1
            AND pnh.PhoneNumber = d.OfficialMobileNumber;

        -- Add new phone numbers
        INSERT INTO [dbo].[PhoneNumberHistory] (EbillUserId, PhoneNumber, AssignedDate, IsActive, CreatedBy)
        SELECT
            i.Id,
            i.OfficialMobileNumber,
            GETUTCDATE(),
            1,
            'Phone Number Change'
        FROM inserted i
        INNER JOIN deleted d ON i.Id = d.Id
        WHERE i.OfficialMobileNumber != d.OfficialMobileNumber
            AND i.OfficialMobileNumber IS NOT NULL
            AND i.OfficialMobileNumber != '';
    END
END
GO

PRINT 'Phone number change trigger created.';
GO

-- Create function to get user by any historical phone number
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[GetEbillUserByHistoricalPhone]'))
    DROP FUNCTION [dbo].[GetEbillUserByHistoricalPhone];
GO

CREATE FUNCTION [dbo].[GetEbillUserByHistoricalPhone]
(
    @PhoneNumber nvarchar(20),
    @CallDate datetime2(7) = NULL
)
RETURNS INT
AS
BEGIN
    DECLARE @UserId INT;

    -- If no date provided, check all historical records
    IF @CallDate IS NULL
    BEGIN
        SELECT TOP 1 @UserId = EbillUserId
        FROM [dbo].[PhoneNumberHistory]
        WHERE PhoneNumber = @PhoneNumber
            OR PhoneNumber = '+' + @PhoneNumber
            OR '+' + PhoneNumber = @PhoneNumber
        ORDER BY AssignedDate DESC;
    END
    ELSE
    BEGIN
        -- Find who had this number on the specific date
        SELECT TOP 1 @UserId = EbillUserId
        FROM [dbo].[PhoneNumberHistory]
        WHERE (PhoneNumber = @PhoneNumber
            OR PhoneNumber = '+' + @PhoneNumber
            OR '+' + PhoneNumber = @PhoneNumber)
            AND AssignedDate <= @CallDate
            AND (UnassignedDate IS NULL OR UnassignedDate > @CallDate);
    END

    RETURN @UserId;
END
GO

PRINT 'Function to lookup users by historical phone number created.';
GO

-- Update existing telecom records using historical data
PRINT 'Updating existing telecom records with historical phone matches...';

-- Update PSTNs
UPDATE p
SET p.EbillUserId = dbo.GetEbillUserByHistoricalPhone(p.DialedNumber, p.CallDate)
FROM [dbo].[PSTNs] p
WHERE p.EbillUserId IS NULL;

PRINT 'PSTNs updated.';

-- Update PrivateWires
UPDATE pw
SET pw.EbillUserId = dbo.GetEbillUserByHistoricalPhone(pw.DialedNumber, pw.CallDate)
FROM [dbo].[PrivateWires] pw
WHERE pw.EbillUserId IS NULL;

PRINT 'PrivateWires updated.';

-- Update Safaricom
UPDATE s
SET s.EbillUserId = dbo.GetEbillUserByHistoricalPhone(s.Dialed, s.CallDate)
FROM [dbo].[Safaricom] s
WHERE s.EbillUserId IS NULL;

PRINT 'Safaricom updated.';

-- Update Airtel
UPDATE a
SET a.EbillUserId = dbo.GetEbillUserByHistoricalPhone(a.Dialed, a.CallDate)
FROM [dbo].[Airtel] a
WHERE a.EbillUserId IS NULL;

PRINT 'Airtel updated.';

PRINT '';
PRINT '============================================';
PRINT 'Phone Number History Setup Complete!';
PRINT '============================================';
PRINT 'The system now:';
PRINT '1. Tracks all historical phone numbers for each user';
PRINT '2. Automatically records when phone numbers change';
PRINT '3. Can match calls to users based on the date of the call';
PRINT '4. Preserves billing history even when numbers change';
GO