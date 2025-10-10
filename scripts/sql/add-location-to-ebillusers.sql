-- Add Location column to EbillUsers table
-- This script adds a Location column to store user location information

USE [TABDB]
GO

-- Check if the Location column already exists before adding
IF NOT EXISTS (
    SELECT *
    FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[dbo].[EbillUsers]')
    AND name = 'Location'
)
BEGIN
    ALTER TABLE [dbo].[EbillUsers]
    ADD [Location] NVARCHAR(200) NULL

    PRINT 'Location column added successfully to EbillUsers table'
END
ELSE
BEGIN
    PRINT 'Location column already exists in EbillUsers table'
END
GO

-- Display the updated table structure
SELECT
    COLUMN_NAME,
    DATA_TYPE,
    CHARACTER_MAXIMUM_LENGTH,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'EbillUsers'
ORDER BY ORDINAL_POSITION
GO