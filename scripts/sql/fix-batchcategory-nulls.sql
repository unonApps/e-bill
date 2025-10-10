-- Fix NULL BatchCategory values in StagingBatches table
-- Set all NULL or empty BatchCategory values to 'MONTHLY' as the default

UPDATE StagingBatches
SET BatchCategory = 'MONTHLY'
WHERE BatchCategory IS NULL OR BatchCategory = '';

-- Make sure future records have a default value
-- First check if column exists and its properties
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[StagingBatches]') AND name = 'BatchCategory')
BEGIN
    -- Add default constraint if it doesn't exist
    IF NOT EXISTS (SELECT * FROM sys.default_constraints
                   WHERE parent_object_id = OBJECT_ID(N'[dbo].[StagingBatches]')
                   AND parent_column_id = (SELECT column_id FROM sys.columns
                                          WHERE object_id = OBJECT_ID(N'[dbo].[StagingBatches]')
                                          AND name = 'BatchCategory'))
    BEGIN
        ALTER TABLE StagingBatches
        ADD CONSTRAINT DF_StagingBatches_BatchCategory DEFAULT 'MONTHLY' FOR BatchCategory;
    END
END

PRINT 'Fixed NULL BatchCategory values and added default constraint';

-- Verify the fix
SELECT COUNT(*) as NullCount FROM StagingBatches WHERE BatchCategory IS NULL OR BatchCategory = '';