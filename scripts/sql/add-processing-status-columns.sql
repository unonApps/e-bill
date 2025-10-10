-- Add ProcessingStatus column to source tables
-- This column tracks whether records have been processed to prevent duplicates

-- Add to Safaricom table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Safaricom]') AND name = 'ProcessingStatus')
BEGIN
    ALTER TABLE [dbo].[Safaricom]
    ADD ProcessingStatus INT NOT NULL DEFAULT 0; -- 0 = Staged
END
GO

-- Add to Airtel table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Airtel]') AND name = 'ProcessingStatus')
BEGIN
    ALTER TABLE [dbo].[Airtel]
    ADD ProcessingStatus INT NOT NULL DEFAULT 0;
END
GO

-- Add to PSTNs table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PSTNs]') AND name = 'ProcessingStatus')
BEGIN
    ALTER TABLE [dbo].[PSTNs]
    ADD ProcessingStatus INT NOT NULL DEFAULT 0;
END
GO

-- Add to PrivateWires table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PrivateWires]') AND name = 'ProcessingStatus')
BEGIN
    ALTER TABLE [dbo].[PrivateWires]
    ADD ProcessingStatus INT NOT NULL DEFAULT 0;
END
GO

-- ProcessingStatus values:
-- 0 = Staged (default, not yet processed)
-- 1 = Processing (currently being processed)
-- 2 = Completed (successfully processed)
-- 3 = Failed (processing failed)

PRINT 'ProcessingStatus columns added successfully to source tables';