-- Check UserPhones table structure
SELECT
    c.COLUMN_NAME,
    c.DATA_TYPE,
    c.CHARACTER_MAXIMUM_LENGTH,
    c.IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS c
WHERE c.TABLE_NAME = 'UserPhones'
ORDER BY c.ORDINAL_POSITION;

-- Show sample data if exists
SELECT TOP 5 * FROM UserPhones;