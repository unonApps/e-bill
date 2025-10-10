-- Add UserPhoneId column to telecom tables if it doesn't exist

-- Check and add UserPhoneId to PrivateWires
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PrivateWires]') AND name = 'UserPhoneId')
BEGIN
    ALTER TABLE [dbo].[PrivateWires] ADD [UserPhoneId] int NULL;
    PRINT 'Added UserPhoneId column to PrivateWires table';
END
ELSE
BEGIN
    PRINT 'UserPhoneId column already exists in PrivateWires table';
END
GO

-- Check and add UserPhoneId to Airtel
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Airtel]') AND name = 'UserPhoneId')
BEGIN
    ALTER TABLE [dbo].[Airtel] ADD [UserPhoneId] int NULL;
    PRINT 'Added UserPhoneId column to Airtel table';
END
ELSE
BEGIN
    PRINT 'UserPhoneId column already exists in Airtel table';
END
GO

-- Check and add UserPhoneId to Safaricom
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Safaricom]') AND name = 'UserPhoneId')
BEGIN
    ALTER TABLE [dbo].[Safaricom] ADD [UserPhoneId] int NULL;
    PRINT 'Added UserPhoneId column to Safaricom table';
END
ELSE
BEGIN
    PRINT 'UserPhoneId column already exists in Safaricom table';
END
GO

-- Check and add UserPhoneId to PSTNs
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PSTNs]') AND name = 'UserPhoneId')
BEGIN
    ALTER TABLE [dbo].[PSTNs] ADD [UserPhoneId] int NULL;
    PRINT 'Added UserPhoneId column to PSTNs table';
END
ELSE
BEGIN
    PRINT 'UserPhoneId column already exists in PSTNs table';
END
GO

-- Add foreign key constraints if UserPhones table exists and constraints don't exist
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'UserPhones')
BEGIN
    -- Add FK for PrivateWires
    IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_PrivateWires_UserPhones_UserPhoneId' AND parent_object_id = OBJECT_ID('PrivateWires'))
    BEGIN
        ALTER TABLE [dbo].[PrivateWires]
        ADD CONSTRAINT [FK_PrivateWires_UserPhones_UserPhoneId]
        FOREIGN KEY ([UserPhoneId]) REFERENCES [dbo].[UserPhones]([Id]);
        PRINT 'Added foreign key constraint FK_PrivateWires_UserPhones_UserPhoneId';
    END

    -- Add FK for Airtel
    IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Airtel_UserPhones_UserPhoneId' AND parent_object_id = OBJECT_ID('Airtel'))
    BEGIN
        ALTER TABLE [dbo].[Airtel]
        ADD CONSTRAINT [FK_Airtel_UserPhones_UserPhoneId]
        FOREIGN KEY ([UserPhoneId]) REFERENCES [dbo].[UserPhones]([Id]);
        PRINT 'Added foreign key constraint FK_Airtel_UserPhones_UserPhoneId';
    END

    -- Add FK for Safaricom
    IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Safaricom_UserPhones_UserPhoneId' AND parent_object_id = OBJECT_ID('Safaricom'))
    BEGIN
        ALTER TABLE [dbo].[Safaricom]
        ADD CONSTRAINT [FK_Safaricom_UserPhones_UserPhoneId]
        FOREIGN KEY ([UserPhoneId]) REFERENCES [dbo].[UserPhones]([Id]);
        PRINT 'Added foreign key constraint FK_Safaricom_UserPhones_UserPhoneId';
    END

    -- Add FK for PSTNs
    IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_PSTNs_UserPhones_UserPhoneId' AND parent_object_id = OBJECT_ID('PSTNs'))
    BEGIN
        ALTER TABLE [dbo].[PSTNs]
        ADD CONSTRAINT [FK_PSTNs_UserPhones_UserPhoneId]
        FOREIGN KEY ([UserPhoneId]) REFERENCES [dbo].[UserPhones]([Id]);
        PRINT 'Added foreign key constraint FK_PSTNs_UserPhones_UserPhoneId';
    END

    -- Add indexes for better performance
    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_PrivateWires_UserPhoneId' AND object_id = OBJECT_ID('PrivateWires'))
    BEGIN
        CREATE INDEX [IX_PrivateWires_UserPhoneId] ON [dbo].[PrivateWires]([UserPhoneId]);
        PRINT 'Added index IX_PrivateWires_UserPhoneId';
    END

    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Airtel_UserPhoneId' AND object_id = OBJECT_ID('Airtel'))
    BEGIN
        CREATE INDEX [IX_Airtel_UserPhoneId] ON [dbo].[Airtel]([UserPhoneId]);
        PRINT 'Added index IX_Airtel_UserPhoneId';
    END

    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Safaricom_UserPhoneId' AND object_id = OBJECT_ID('Safaricom'))
    BEGIN
        CREATE INDEX [IX_Safaricom_UserPhoneId] ON [dbo].[Safaricom]([UserPhoneId]);
        PRINT 'Added index IX_Safaricom_UserPhoneId';
    END

    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_PSTNs_UserPhoneId' AND object_id = OBJECT_ID('PSTNs'))
    BEGIN
        CREATE INDEX [IX_PSTNs_UserPhoneId] ON [dbo].[PSTNs]([UserPhoneId]);
        PRINT 'Added index IX_PSTNs_UserPhoneId';
    END
END
GO

PRINT 'UserPhoneId column migration completed successfully!';
