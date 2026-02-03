-- Migration: Add SupervisorEmail field to CallLogVerifications
-- This provides proper email-to-email comparison for supervisor matching

-- Step 1: Add the new SupervisorEmail column
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[CallLogVerifications]') AND name = 'SupervisorEmail')
BEGIN
    ALTER TABLE [dbo].[CallLogVerifications] ADD [SupervisorEmail] nvarchar(256) NULL;
    PRINT 'Added SupervisorEmail column to CallLogVerifications table';
END
ELSE
BEGIN
    PRINT 'SupervisorEmail column already exists';
END
GO

-- Step 2: Populate SupervisorEmail from existing data
-- If SupervisorIndexNumber contains an email (has @), use it directly
-- Otherwise, look up the email from EbillUsers using the index number
UPDATE clv
SET clv.SupervisorEmail =
    CASE
        -- If SupervisorIndexNumber contains @, it's already an email
        WHEN clv.SupervisorIndexNumber LIKE '%@%' THEN clv.SupervisorIndexNumber
        -- Otherwise, look up the email from EbillUsers
        ELSE eu.Email
    END
FROM [dbo].[CallLogVerifications] clv
LEFT JOIN [dbo].[EbillUsers] eu ON eu.IndexNumber = clv.SupervisorIndexNumber
WHERE clv.SupervisorEmail IS NULL
  AND clv.SupervisorIndexNumber IS NOT NULL
  AND clv.SubmittedToSupervisor = 1;

DECLARE @UpdatedRows INT = @@ROWCOUNT;
PRINT 'Updated ' + CAST(@UpdatedRows AS VARCHAR(10)) + ' rows with SupervisorEmail';
GO

-- Step 3: Add an index on SupervisorEmail for better query performance
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_CallLogVerifications_SupervisorEmail' AND object_id = OBJECT_ID('dbo.CallLogVerifications'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_CallLogVerifications_SupervisorEmail]
    ON [dbo].[CallLogVerifications] ([SupervisorEmail])
    WHERE [SupervisorEmail] IS NOT NULL;
    PRINT 'Created index IX_CallLogVerifications_SupervisorEmail';
END
ELSE
BEGIN
    PRINT 'Index IX_CallLogVerifications_SupervisorEmail already exists';
END
GO

-- Verification: Show count of records with and without SupervisorEmail
SELECT
    'Total submitted verifications' as Metric,
    COUNT(*) as Count
FROM [dbo].[CallLogVerifications]
WHERE SubmittedToSupervisor = 1

UNION ALL

SELECT
    'With SupervisorEmail populated' as Metric,
    COUNT(*) as Count
FROM [dbo].[CallLogVerifications]
WHERE SubmittedToSupervisor = 1 AND SupervisorEmail IS NOT NULL

UNION ALL

SELECT
    'Missing SupervisorEmail (needs manual fix)' as Metric,
    COUNT(*) as Count
FROM [dbo].[CallLogVerifications]
WHERE SubmittedToSupervisor = 1 AND SupervisorEmail IS NULL;
GO
