-- =====================================================
-- Remove redundant 'source' column from Safaricom and Airtel tables
-- The source is already implied by the table name
-- =====================================================

-- Remove source column from Safaricom
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Safaricom') AND name = 'source')
BEGIN
    ALTER TABLE Safaricom DROP COLUMN source;
    PRINT 'Dropped Safaricom.source column';
END

-- Remove source column from Airtel
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Airtel') AND name = 'source')
BEGIN
    ALTER TABLE Airtel DROP COLUMN source;
    PRINT 'Dropped Airtel.source column';
END

-- Verify the columns have been removed
PRINT '';
PRINT 'Remaining columns in Safaricom:';
SELECT COLUMN_NAME
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Safaricom'
ORDER BY ORDINAL_POSITION;

PRINT '';
PRINT 'Remaining columns in Airtel:';
SELECT COLUMN_NAME
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Airtel'
ORDER BY ORDINAL_POSITION;

PRINT '';
PRINT 'Source columns removed successfully!';