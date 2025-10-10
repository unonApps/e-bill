-- Add ClassOfService relationship to UserPhone table
-- This script adds a foreign key relationship between UserPhone and ClassOfService tables

-- First check if UserPhones table exists
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'UserPhones')
BEGIN
    PRINT 'Creating UserPhones table...'
    CREATE TABLE UserPhones (
        Id int IDENTITY(1,1) NOT NULL,
        IndexNumber nvarchar(50) NOT NULL,
        PhoneNumber nvarchar(20) NOT NULL,
        PhoneType nvarchar(50) NOT NULL,
        IsPrimary bit NOT NULL DEFAULT 0,
        IsActive bit NOT NULL DEFAULT 1,
        AssignedDate datetime2 NOT NULL DEFAULT GETUTCDATE(),
        UnassignedDate datetime2 NULL,
        Location nvarchar(200) NULL,
        Notes nvarchar(500) NULL,
        CreatedBy nvarchar(100) NULL,
        CreatedDate datetime2 NOT NULL DEFAULT GETUTCDATE(),
        ClassOfServiceId int NULL,
        CONSTRAINT PK_UserPhones PRIMARY KEY (Id)
    );

    -- Create indexes
    CREATE INDEX IX_UserPhones_IndexNumber ON UserPhones(IndexNumber);
    CREATE INDEX IX_UserPhones_PhoneNumber ON UserPhones(PhoneNumber);
    CREATE INDEX IX_UserPhones_IndexNumber_PhoneNumber_IsActive ON UserPhones(IndexNumber, PhoneNumber, IsActive);
END
ELSE
BEGIN
    PRINT 'UserPhones table already exists, checking for ClassOfServiceId column...'

    -- Check if ClassOfServiceId column exists
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('UserPhones') AND name = 'ClassOfServiceId')
    BEGIN
        PRINT 'Adding ClassOfServiceId column to UserPhones table...'
        ALTER TABLE UserPhones ADD ClassOfServiceId int NULL;
    END
    ELSE
    BEGIN
        PRINT 'ClassOfServiceId column already exists in UserPhones table.'
    END
END

-- Add foreign key constraint if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_UserPhones_ClassOfServices_ClassOfServiceId')
BEGIN
    PRINT 'Adding foreign key constraint FK_UserPhones_ClassOfServices_ClassOfServiceId...'
    ALTER TABLE UserPhones
    ADD CONSTRAINT FK_UserPhones_ClassOfServices_ClassOfServiceId
    FOREIGN KEY (ClassOfServiceId) REFERENCES ClassOfServices(Id);
END
ELSE
BEGIN
    PRINT 'Foreign key constraint already exists.'
END

-- Create index on ClassOfServiceId if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_UserPhones_ClassOfServiceId' AND object_id = OBJECT_ID('UserPhones'))
BEGIN
    PRINT 'Creating index IX_UserPhones_ClassOfServiceId...'
    CREATE INDEX IX_UserPhones_ClassOfServiceId ON UserPhones(ClassOfServiceId);
END
ELSE
BEGIN
    PRINT 'Index IX_UserPhones_ClassOfServiceId already exists.'
END

-- Add foreign key to EbillUsers if not exists
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_UserPhones_EbillUsers_IndexNumber')
BEGIN
    PRINT 'Adding foreign key constraint FK_UserPhones_EbillUsers_IndexNumber...'

    -- First ensure EbillUsers has the unique constraint on IndexNumber
    IF NOT EXISTS (SELECT * FROM sys.key_constraints WHERE name = 'AK_EbillUsers_IndexNumber')
    BEGIN
        ALTER TABLE EbillUsers ADD CONSTRAINT AK_EbillUsers_IndexNumber UNIQUE (IndexNumber);
    END

    ALTER TABLE UserPhones
    ADD CONSTRAINT FK_UserPhones_EbillUsers_IndexNumber
    FOREIGN KEY (IndexNumber) REFERENCES EbillUsers(IndexNumber) ON DELETE CASCADE;
END
ELSE
BEGIN
    PRINT 'Foreign key constraint FK_UserPhones_EbillUsers_IndexNumber already exists.'
END

PRINT 'ClassOfService to UserPhone relationship setup complete!';