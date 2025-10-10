-- Safe migration approach: Rename old columns instead of dropping them immediately
-- This allows for rollback if needed

USE [TABDB];
GO

PRINT '============================================';
PRINT 'Safe Migration: Deprecating String Columns';
PRINT '============================================';
PRINT '';

-- Step 1: Create an office for ICTS if it doesn't exist
IF NOT EXISTS (SELECT 1 FROM [dbo].[Offices] WHERE Code = 'ICTS' OR Name = 'ICTS')
BEGIN
    -- Assuming ICTS belongs to UNON organization
    DECLARE @UNONId INT;
    SELECT @UNONId = Id FROM [dbo].[Organizations] WHERE Code = 'UNON';

    IF @UNONId IS NOT NULL
    BEGIN
        INSERT INTO [dbo].[Offices] (Name, Code, Description, OrganizationId, CreatedDate)
        VALUES ('Information and Communication Technology Service', 'ICTS',
                'ICT Services for UN Nairobi', @UNONId, GETUTCDATE());

        PRINT 'Created ICTS office under UNON organization.';
    END
END

-- Step 2: Update any remaining unmatched offices
UPDATE eu
SET eu.OfficeId = ofc.Id
FROM [dbo].[EbillUsers] eu
INNER JOIN [dbo].[Offices] ofc ON eu.Office = ofc.Code OR eu.Office = ofc.Name
WHERE eu.OfficeId IS NULL AND eu.Office IS NOT NULL;

-- Step 3: Rename columns to mark them as deprecated (keeps data for safety)
-- This approach is safer than dropping immediately
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[EbillUsers]') AND name = 'Organization')
BEGIN
    EXEC sp_rename '[dbo].[EbillUsers].[Organization]', 'Organization_DEPRECATED', 'COLUMN';
    PRINT 'Renamed Organization column to Organization_DEPRECATED';
END

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[EbillUsers]') AND name = 'Office')
BEGIN
    EXEC sp_rename '[dbo].[EbillUsers].[Office]', 'Office_DEPRECATED', 'COLUMN';
    PRINT 'Renamed Office column to Office_DEPRECATED';
END

-- Step 4: Create view for backward compatibility if needed
IF EXISTS (SELECT * FROM sys.views WHERE name = 'EbillUsersView')
    DROP VIEW [dbo].[EbillUsersView];
GO

CREATE VIEW [dbo].[EbillUsersView] AS
SELECT
    eu.Id,
    eu.FirstName,
    eu.LastName,
    eu.IndexNumber,
    eu.OfficialMobileNumber,
    eu.IssuedDeviceID,
    eu.Email,
    eu.ClassOfService,
    -- Computed organization and office names from foreign keys
    org.Name as Organization,
    ofc.Name as Office,
    so.Name as SubOffice,
    eu.IsActive,
    eu.SupervisorIndexNumber,
    eu.SupervisorName,
    eu.SupervisorEmail,
    eu.CreatedDate,
    eu.LastModifiedDate,
    eu.OrganizationId,
    eu.OfficeId,
    eu.SubOfficeId
FROM [dbo].[EbillUsers] eu
LEFT JOIN [dbo].[Organizations] org ON eu.OrganizationId = org.Id
LEFT JOIN [dbo].[Offices] ofc ON eu.OfficeId = ofc.Id
LEFT JOIN [dbo].[SubOffices] so ON eu.SubOfficeId = so.Id;
GO

PRINT 'Created EbillUsersView for backward compatibility.';

-- Show the final result
PRINT '';
PRINT 'Final EbillUsers data with relationships:';
SELECT TOP 5 * FROM [dbo].[EbillUsersView];

PRINT '';
PRINT 'Migration completed safely!';
PRINT 'Old columns renamed to _DEPRECATED and can be dropped later after verification.';
PRINT 'Use EbillUsersView for queries that need organization/office names.';
GO