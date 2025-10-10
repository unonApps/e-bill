-- Script to add CON organization for consultancy companies
USE [TABDB];
GO

PRINT 'Adding CON organization...';

-- Check and insert CON organization if it doesn't exist
IF NOT EXISTS (SELECT 1 FROM [dbo].[Organizations] WHERE Code = 'CON')
BEGIN
    INSERT INTO [dbo].[Organizations] (Code, Name, Description, CreatedDate)
    VALUES (
        'CON',
        'Consultancy Companies',
        'Companies and consulting firms in consultation with the United Nations',
        GETUTCDATE()
    );

    PRINT 'CON organization added successfully.';
END
ELSE
BEGIN
    -- Update if it exists but needs description
    UPDATE [dbo].[Organizations]
    SET Description = 'Companies and consulting firms in consultation with the United Nations'
    WHERE Code = 'CON' AND (Description IS NULL OR Description = '');

    PRINT 'CON organization already exists.';
END

-- Display the CON organization details
SELECT Code, Name, Description
FROM [dbo].[Organizations]
WHERE Code = 'CON';

-- Display total count
DECLARE @TotalOrgs INT;
SELECT @TotalOrgs = COUNT(*) FROM [dbo].[Organizations];
PRINT '';
PRINT 'Total organizations in database: ' + CAST(@TotalOrgs AS VARCHAR(10));
GO