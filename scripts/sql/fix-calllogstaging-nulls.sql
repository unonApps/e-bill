-- Fix NULL values in CallLogStagings table
-- Set default values for required string columns

-- Fix ImportType (should be MONTHLY by default)
UPDATE CallLogStagings
SET ImportType = 'MONTHLY'
WHERE ImportType IS NULL OR ImportType = '';

-- Fix IsAdjustment (should be false/0 by default)
UPDATE CallLogStagings
SET IsAdjustment = 0
WHERE IsAdjustment IS NULL;

-- Add default constraints if they don't exist
IF NOT EXISTS (SELECT * FROM sys.default_constraints
               WHERE parent_object_id = OBJECT_ID(N'[dbo].[CallLogStagings]')
               AND parent_column_id = (SELECT column_id FROM sys.columns
                                      WHERE object_id = OBJECT_ID(N'[dbo].[CallLogStagings]')
                                      AND name = 'ImportType'))
BEGIN
    ALTER TABLE CallLogStagings
    ADD CONSTRAINT DF_CallLogStagings_ImportType DEFAULT 'MONTHLY' FOR ImportType;
END

IF NOT EXISTS (SELECT * FROM sys.default_constraints
               WHERE parent_object_id = OBJECT_ID(N'[dbo].[CallLogStagings]')
               AND parent_column_id = (SELECT column_id FROM sys.columns
                                      WHERE object_id = OBJECT_ID(N'[dbo].[CallLogStagings]')
                                      AND name = 'IsAdjustment'))
BEGIN
    ALTER TABLE CallLogStagings
    ADD CONSTRAINT DF_CallLogStagings_IsAdjustment DEFAULT 0 FOR IsAdjustment;
END

PRINT 'Fixed NULL values in CallLogStagings table';

-- Verify the fix
SELECT
    COUNT(*) as TotalRecords,
    SUM(CASE WHEN ImportType IS NULL OR ImportType = '' THEN 1 ELSE 0 END) as NullImportType,
    SUM(CASE WHEN IsAdjustment IS NULL THEN 1 ELSE 0 END) as NullIsAdjustment
FROM CallLogStagings;