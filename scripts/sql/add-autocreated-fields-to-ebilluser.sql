-- Migration: Add IsAutoCreated and AutoCreatedFromImportJobId fields to EbillUsers table
-- Purpose: Track which users were auto-created during call log imports

-- Add IsAutoCreated column (default to false for existing users)
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = 'IsAutoCreated' AND Object_ID = Object_ID('EbillUsers'))
BEGIN
    ALTER TABLE EbillUsers ADD IsAutoCreated BIT NOT NULL DEFAULT 0;
    PRINT 'Added IsAutoCreated column to EbillUsers table';
END
ELSE
BEGIN
    PRINT 'IsAutoCreated column already exists';
END
GO

-- Add AutoCreatedFromImportJobId column (nullable, links to ImportJobs)
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = 'AutoCreatedFromImportJobId' AND Object_ID = Object_ID('EbillUsers'))
BEGIN
    ALTER TABLE EbillUsers ADD AutoCreatedFromImportJobId UNIQUEIDENTIFIER NULL;
    PRINT 'Added AutoCreatedFromImportJobId column to EbillUsers table';
END
ELSE
BEGIN
    PRINT 'AutoCreatedFromImportJobId column already exists';
END
GO

-- Update existing users that have "Auto-created from PSTN/PW import" in Location field
-- These are already auto-created users from before tracking was added
UPDATE EbillUsers
SET IsAutoCreated = 1
WHERE Location LIKE '%Auto-created from PSTN/PW import%'
  AND IsAutoCreated = 0;

PRINT 'Updated existing auto-created users based on Location field';
GO

-- Create index for efficient filtering by IsAutoCreated
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE Name = 'IX_EbillUsers_IsAutoCreated' AND object_id = OBJECT_ID('EbillUsers'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_EbillUsers_IsAutoCreated ON EbillUsers(IsAutoCreated);
    PRINT 'Created index IX_EbillUsers_IsAutoCreated';
END
GO
