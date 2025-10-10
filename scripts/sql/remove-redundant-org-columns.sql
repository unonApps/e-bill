-- Script to remove redundant Organization and Office string columns from EbillUsers
-- Now that we have proper foreign key relationships, these string fields are no longer needed

USE [TABDB];
GO

PRINT '============================================';
PRINT 'Removing Redundant String Columns from EbillUsers';
PRINT '============================================';
PRINT '';

-- First, let's check if any data would be lost
PRINT 'Checking for any unmatched data before removal...';

SELECT
    COUNT(CASE WHEN Organization IS NOT NULL AND OrganizationId IS NULL THEN 1 END) as UnmatchedOrganizations,
    COUNT(CASE WHEN Office IS NOT NULL AND OfficeId IS NULL THEN 1 END) as UnmatchedOffices
FROM [dbo].[EbillUsers];

-- Show any unmatched values
IF EXISTS (SELECT 1 FROM [dbo].[EbillUsers] WHERE Organization IS NOT NULL AND OrganizationId IS NULL)
BEGIN
    PRINT 'Warning: Found unmatched Organization values:';
    SELECT DISTINCT Organization
    FROM [dbo].[EbillUsers]
    WHERE Organization IS NOT NULL AND OrganizationId IS NULL;
END

IF EXISTS (SELECT 1 FROM [dbo].[EbillUsers] WHERE Office IS NOT NULL AND OfficeId IS NULL)
BEGIN
    PRINT 'Warning: Found unmatched Office values:';
    SELECT DISTINCT Office
    FROM [dbo].[EbillUsers]
    WHERE Office IS NOT NULL AND OfficeId IS NULL;
END

PRINT '';
PRINT 'To proceed with column removal, run the following commands:';
PRINT '-- ALTER TABLE [dbo].[EbillUsers] DROP COLUMN [Organization];';
PRINT '-- ALTER TABLE [dbo].[EbillUsers] DROP COLUMN [Office];';
PRINT '';
PRINT 'Or to keep the columns for now, you can make them computed columns:';
PRINT '(This way they will always show the current name from the related table)';
PRINT '';

-- Show sample data with JOINs to verify relationships work
PRINT 'Sample data showing the relationships:';
SELECT TOP 5
    eu.Id,
    eu.FirstName + ' ' + eu.LastName as FullName,
    eu.IndexNumber,
    org.Code as OrgCode,
    org.Name as OrganizationName,
    ofc.Code as OfficeCode,
    ofc.Name as OfficeName,
    so.Code as SubOfficeCode,
    so.Name as SubOfficeName
FROM [dbo].[EbillUsers] eu
LEFT JOIN [dbo].[Organizations] org ON eu.OrganizationId = org.Id
LEFT JOIN [dbo].[Offices] ofc ON eu.OfficeId = ofc.Id
LEFT JOIN [dbo].[SubOffices] so ON eu.SubOfficeId = so.Id
ORDER BY eu.Id;

GO