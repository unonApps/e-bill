-- Add ClassOfServiceId column to UserPhones table
-- This script ONLY adds the ClassOfServiceId column if it doesn't exist

-- Check if the column already exists
IF NOT EXISTS (
    SELECT *
    FROM sys.columns
    WHERE object_id = OBJECT_ID('UserPhones')
    AND name = 'ClassOfServiceId'
)
BEGIN
    PRINT 'Adding ClassOfServiceId column to UserPhones table...'
    ALTER TABLE UserPhones ADD ClassOfServiceId int NULL;
    PRINT 'ClassOfServiceId column added successfully.'
END
ELSE
BEGIN
    PRINT 'ClassOfServiceId column already exists in UserPhones table.'
END
GO

-- Add foreign key constraint if it doesn't exist
IF NOT EXISTS (
    SELECT *
    FROM sys.foreign_keys
    WHERE name = 'FK_UserPhones_ClassOfServices_ClassOfServiceId'
)
BEGIN
    PRINT 'Adding foreign key constraint FK_UserPhones_ClassOfServices_ClassOfServiceId...'
    ALTER TABLE UserPhones
    ADD CONSTRAINT FK_UserPhones_ClassOfServices_ClassOfServiceId
    FOREIGN KEY (ClassOfServiceId) REFERENCES ClassOfServices(Id);
    PRINT 'Foreign key constraint added successfully.'
END
ELSE
BEGIN
    PRINT 'Foreign key constraint already exists.'
END
GO

-- Create index on ClassOfServiceId if it doesn't exist
IF NOT EXISTS (
    SELECT *
    FROM sys.indexes
    WHERE name = 'IX_UserPhones_ClassOfServiceId'
    AND object_id = OBJECT_ID('UserPhones')
)
BEGIN
    PRINT 'Creating index IX_UserPhones_ClassOfServiceId...'
    CREATE INDEX IX_UserPhones_ClassOfServiceId ON UserPhones(ClassOfServiceId);
    PRINT 'Index created successfully.'
END
ELSE
BEGIN
    PRINT 'Index IX_UserPhones_ClassOfServiceId already exists.'
END
GO

PRINT 'ClassOfService to UserPhone relationship setup complete!'