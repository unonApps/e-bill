-- Add UserPhoneId column to telecom tables for linking calls to registered phones
-- This enables automatic matching of call records to users during import

-- Add to PSTN table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PSTNs]') AND name = 'UserPhoneId')
BEGIN
    ALTER TABLE [dbo].[PSTNs]
    ADD [UserPhoneId] int NULL;

    -- Add foreign key constraint
    ALTER TABLE [dbo].[PSTNs]
    ADD CONSTRAINT [FK_PSTNs_UserPhones_UserPhoneId]
    FOREIGN KEY ([UserPhoneId])
    REFERENCES [dbo].[UserPhones]([Id])
    ON DELETE NO ACTION;

    -- Add index for performance
    CREATE INDEX [IX_PSTNs_UserPhoneId] ON [dbo].[PSTNs]([UserPhoneId]);

    PRINT 'Added UserPhoneId column to PSTNs table';
END
ELSE
BEGIN
    PRINT 'UserPhoneId column already exists in PSTNs table';
END
GO

-- Add to PrivateWires table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PrivateWires]') AND name = 'UserPhoneId')
BEGIN
    ALTER TABLE [dbo].[PrivateWires]
    ADD [UserPhoneId] int NULL;

    ALTER TABLE [dbo].[PrivateWires]
    ADD CONSTRAINT [FK_PrivateWires_UserPhones_UserPhoneId]
    FOREIGN KEY ([UserPhoneId])
    REFERENCES [dbo].[UserPhones]([Id])
    ON DELETE NO ACTION;

    CREATE INDEX [IX_PrivateWires_UserPhoneId] ON [dbo].[PrivateWires]([UserPhoneId]);

    PRINT 'Added UserPhoneId column to PrivateWires table';
END
ELSE
BEGIN
    PRINT 'UserPhoneId column already exists in PrivateWires table';
END
GO

-- Add to Safaricom table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Safaricom]') AND name = 'UserPhoneId')
BEGIN
    ALTER TABLE [dbo].[Safaricom]
    ADD [UserPhoneId] int NULL;

    ALTER TABLE [dbo].[Safaricom]
    ADD CONSTRAINT [FK_Safaricom_UserPhones_UserPhoneId]
    FOREIGN KEY ([UserPhoneId])
    REFERENCES [dbo].[UserPhones]([Id])
    ON DELETE NO ACTION;

    CREATE INDEX [IX_Safaricom_UserPhoneId] ON [dbo].[Safaricom]([UserPhoneId]);

    PRINT 'Added UserPhoneId column to Safaricom table';
END
ELSE
BEGIN
    PRINT 'UserPhoneId column already exists in Safaricom table';
END
GO

-- Add to Airtel table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Airtel]') AND name = 'UserPhoneId')
BEGIN
    ALTER TABLE [dbo].[Airtel]
    ADD [UserPhoneId] int NULL;

    ALTER TABLE [dbo].[Airtel]
    ADD CONSTRAINT [FK_Airtel_UserPhones_UserPhoneId]
    FOREIGN KEY ([UserPhoneId])
    REFERENCES [dbo].[UserPhones]([Id])
    ON DELETE NO ACTION;

    CREATE INDEX [IX_Airtel_UserPhoneId] ON [dbo].[Airtel]([UserPhoneId]);

    PRINT 'Added UserPhoneId column to Airtel table';
END
ELSE
BEGIN
    PRINT 'UserPhoneId column already exists in Airtel table';
END
GO

PRINT 'UserPhoneId column migration completed successfully!';
PRINT 'Next step: Update import logic to populate UserPhoneId when importing records';
