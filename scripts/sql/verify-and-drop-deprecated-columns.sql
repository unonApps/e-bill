-- Production-ready script to verify and drop deprecated columns
USE [TABDB];
GO

PRINT '============================================';
PRINT 'ENTERPRISE PRODUCTION: Final Column Cleanup';
PRINT '============================================';
PRINT '';

-- Step 1: Verify foreign key relationships are working
PRINT 'Step 1: Verifying foreign key relationships...';
PRINT '';

DECLARE @ValidationErrors INT = 0;

-- Check if all users with deprecated organization values have been mapped
IF EXISTS (SELECT 1 FROM [dbo].[EbillUsers] WHERE Organization_DEPRECATED IS NOT NULL AND OrganizationId IS NULL)
BEGIN
    PRINT 'ERROR: Found users with Organization data but no OrganizationId!';
    SELECT Id, FirstName, LastName, Organization_DEPRECATED
    FROM [dbo].[EbillUsers]
    WHERE Organization_DEPRECATED IS NOT NULL AND OrganizationId IS NULL;
    SET @ValidationErrors = @ValidationErrors + 1;
END
ELSE
BEGIN
    PRINT '✓ All organization mappings verified';
END

-- Check if all users with deprecated office values have been mapped
IF EXISTS (SELECT 1 FROM [dbo].[EbillUsers] WHERE Office_DEPRECATED IS NOT NULL AND OfficeId IS NULL)
BEGIN
    PRINT 'ERROR: Found users with Office data but no OfficeId!';
    SELECT Id, FirstName, LastName, Office_DEPRECATED
    FROM [dbo].[EbillUsers]
    WHERE Office_DEPRECATED IS NOT NULL AND OfficeId IS NULL;
    SET @ValidationErrors = @ValidationErrors + 1;
END
ELSE
BEGIN
    PRINT '✓ All office mappings verified';
END

-- Verify foreign key constraints exist
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_EbillUsers_Organizations_OrganizationId')
BEGIN
    PRINT 'ERROR: Foreign key to Organizations not found!';
    SET @ValidationErrors = @ValidationErrors + 1;
END
ELSE
BEGIN
    PRINT '✓ Foreign key to Organizations exists';
END

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_EbillUsers_Offices_OfficeId')
BEGIN
    PRINT 'ERROR: Foreign key to Offices not found!';
    SET @ValidationErrors = @ValidationErrors + 1;
END
ELSE
BEGIN
    PRINT '✓ Foreign key to Offices exists';
END

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_EbillUsers_SubOffices_SubOfficeId')
BEGIN
    PRINT 'ERROR: Foreign key to SubOffices not found!';
    SET @ValidationErrors = @ValidationErrors + 1;
END
ELSE
BEGIN
    PRINT '✓ Foreign key to SubOffices exists';
END

PRINT '';
PRINT '--------------------------------------------';

IF @ValidationErrors > 0
BEGIN
    PRINT 'VALIDATION FAILED! Fix errors before proceeding.';
    RAISERROR('Validation failed. Cannot proceed with column removal.', 16, 1);
    RETURN;
END

PRINT 'All validations passed. Proceeding with cleanup...';
PRINT '';

-- Step 2: Drop the deprecated columns
PRINT 'Step 2: Dropping deprecated columns...';

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[EbillUsers]') AND name = 'Organization_DEPRECATED')
BEGIN
    ALTER TABLE [dbo].[EbillUsers] DROP COLUMN [Organization_DEPRECATED];
    PRINT '✓ Dropped Organization_DEPRECATED column';
END

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[EbillUsers]') AND name = 'Office_DEPRECATED')
BEGIN
    ALTER TABLE [dbo].[EbillUsers] DROP COLUMN [Office_DEPRECATED];
    PRINT '✓ Dropped Office_DEPRECATED column';
END

-- Also check for original column names in case they weren't renamed yet
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[EbillUsers]') AND name = 'Organization')
BEGIN
    ALTER TABLE [dbo].[EbillUsers] DROP COLUMN [Organization];
    PRINT '✓ Dropped Organization column';
END

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[EbillUsers]') AND name = 'Office')
BEGIN
    ALTER TABLE [dbo].[EbillUsers] DROP COLUMN [Office];
    PRINT '✓ Dropped Office column';
END

PRINT '';
PRINT '--------------------------------------------';
PRINT 'Step 3: Final verification of table structure...';
PRINT '';

-- Display final table structure
SELECT
    c.COLUMN_NAME,
    c.DATA_TYPE,
    c.CHARACTER_MAXIMUM_LENGTH,
    c.IS_NULLABLE,
    CASE
        WHEN fk.CONSTRAINT_NAME IS NOT NULL THEN 'FK -> ' + fk.REFERENCED_TABLE_NAME
        ELSE ''
    END as FOREIGN_KEY_TO
FROM INFORMATION_SCHEMA.COLUMNS c
LEFT JOIN (
    SELECT
        COL_NAME(fc.parent_object_id, fc.parent_column_id) AS COLUMN_NAME,
        OBJECT_NAME(fc.referenced_object_id) AS REFERENCED_TABLE_NAME,
        fk.name AS CONSTRAINT_NAME
    FROM sys.foreign_key_columns fc
    JOIN sys.foreign_keys fk ON fc.constraint_object_id = fk.object_id
    WHERE fc.parent_object_id = OBJECT_ID('EbillUsers')
) fk ON c.COLUMN_NAME = fk.COLUMN_NAME
WHERE c.TABLE_NAME = 'EbillUsers'
ORDER BY c.ORDINAL_POSITION;

PRINT '';
PRINT '============================================';
PRINT '✓ PRODUCTION READY: Cleanup completed successfully!';
PRINT '============================================';
PRINT '';
PRINT 'The EbillUsers table now has:';
PRINT '- Proper foreign key relationships to Organizations, Offices, and SubOffices';
PRINT '- No redundant string columns';
PRINT '- Full referential integrity';
PRINT '- Normalized database structure';
PRINT '';
PRINT 'Use the EbillUsersView or JOIN queries to get organization/office names when needed.';
GO