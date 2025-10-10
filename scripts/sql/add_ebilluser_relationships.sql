-- Script to add EbillUserId column and foreign key relationships to telecom tables
-- This links call records to EbillUsers via their phone numbers

USE [TABDB];
GO

PRINT '============================================';
PRINT 'Adding EbillUser Relationships to Telecom Tables';
PRINT '============================================';
PRINT '';

-- Step 1: Add EbillUserId column to PSTNs table
PRINT 'Step 1: Adding EbillUserId to PSTNs table...';
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PSTNs]') AND name = 'EbillUserId')
BEGIN
    ALTER TABLE [dbo].[PSTNs] ADD [EbillUserId] int NULL;
    PRINT 'EbillUserId column added to PSTNs table.';
END
ELSE
BEGIN
    PRINT 'EbillUserId column already exists in PSTNs table.';
END
GO

-- Step 2: Add EbillUserId column to PrivateWires table
PRINT 'Step 2: Adding EbillUserId to PrivateWires table...';
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PrivateWires]') AND name = 'EbillUserId')
BEGIN
    ALTER TABLE [dbo].[PrivateWires] ADD [EbillUserId] int NULL;
    PRINT 'EbillUserId column added to PrivateWires table.';
END
ELSE
BEGIN
    PRINT 'EbillUserId column already exists in PrivateWires table.';
END
GO

-- Step 3: Add EbillUserId column to Safaricom table
PRINT 'Step 3: Adding EbillUserId to Safaricom table...';
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Safaricom]') AND name = 'EbillUserId')
BEGIN
    ALTER TABLE [dbo].[Safaricom] ADD [EbillUserId] int NULL;
    PRINT 'EbillUserId column added to Safaricom table.';
END
ELSE
BEGIN
    PRINT 'EbillUserId column already exists in Safaricom table.';
END
GO

-- Step 4: Add EbillUserId column to Airtel table
PRINT 'Step 4: Adding EbillUserId to Airtel table...';
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Airtel]') AND name = 'EbillUserId')
BEGIN
    ALTER TABLE [dbo].[Airtel] ADD [EbillUserId] int NULL;
    PRINT 'EbillUserId column added to Airtel table.';
END
ELSE
BEGIN
    PRINT 'EbillUserId column already exists in Airtel table.';
END
GO

-- Step 5: Create foreign key constraints
PRINT '';
PRINT 'Step 5: Creating foreign key constraints...';

-- Foreign key for PSTNs
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_PSTNs_EbillUsers_EbillUserId]'))
BEGIN
    ALTER TABLE [dbo].[PSTNs] WITH CHECK ADD CONSTRAINT [FK_PSTNs_EbillUsers_EbillUserId]
    FOREIGN KEY([EbillUserId]) REFERENCES [dbo].[EbillUsers] ([Id])
    ON DELETE SET NULL;
    PRINT 'Foreign key created for PSTNs -> EbillUsers.';
END
ELSE
BEGIN
    PRINT 'Foreign key already exists for PSTNs -> EbillUsers.';
END
GO

-- Foreign key for PrivateWires
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_PrivateWires_EbillUsers_EbillUserId]'))
BEGIN
    ALTER TABLE [dbo].[PrivateWires] WITH CHECK ADD CONSTRAINT [FK_PrivateWires_EbillUsers_EbillUserId]
    FOREIGN KEY([EbillUserId]) REFERENCES [dbo].[EbillUsers] ([Id])
    ON DELETE SET NULL;
    PRINT 'Foreign key created for PrivateWires -> EbillUsers.';
END
ELSE
BEGIN
    PRINT 'Foreign key already exists for PrivateWires -> EbillUsers.';
END
GO

-- Foreign key for Safaricom
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Safaricom_EbillUsers_EbillUserId]'))
BEGIN
    ALTER TABLE [dbo].[Safaricom] WITH CHECK ADD CONSTRAINT [FK_Safaricom_EbillUsers_EbillUserId]
    FOREIGN KEY([EbillUserId]) REFERENCES [dbo].[EbillUsers] ([Id])
    ON DELETE SET NULL;
    PRINT 'Foreign key created for Safaricom -> EbillUsers.';
END
ELSE
BEGIN
    PRINT 'Foreign key already exists for Safaricom -> EbillUsers.';
END
GO

-- Foreign key for Airtel
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Airtel_EbillUsers_EbillUserId]'))
BEGIN
    ALTER TABLE [dbo].[Airtel] WITH CHECK ADD CONSTRAINT [FK_Airtel_EbillUsers_EbillUserId]
    FOREIGN KEY([EbillUserId]) REFERENCES [dbo].[EbillUsers] ([Id])
    ON DELETE SET NULL;
    PRINT 'Foreign key created for Airtel -> EbillUsers.';
END
ELSE
BEGIN
    PRINT 'Foreign key already exists for Airtel -> EbillUsers.';
END
GO

-- Step 6: Create indexes for better query performance
PRINT '';
PRINT 'Step 6: Creating indexes for EbillUserId...';

-- Index for PSTNs
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_PSTNs_EbillUserId' AND object_id = OBJECT_ID('[dbo].[PSTNs]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_PSTNs_EbillUserId] ON [dbo].[PSTNs]
    ([EbillUserId] ASC);
    PRINT 'Index created on PSTNs.EbillUserId.';
END
GO

-- Index for PrivateWires
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_PrivateWires_EbillUserId' AND object_id = OBJECT_ID('[dbo].[PrivateWires]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_PrivateWires_EbillUserId] ON [dbo].[PrivateWires]
    ([EbillUserId] ASC);
    PRINT 'Index created on PrivateWires.EbillUserId.';
END
GO

-- Index for Safaricom
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Safaricom_EbillUserId' AND object_id = OBJECT_ID('[dbo].[Safaricom]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Safaricom_EbillUserId] ON [dbo].[Safaricom]
    ([EbillUserId] ASC);
    PRINT 'Index created on Safaricom.EbillUserId.';
END
GO

-- Index for Airtel
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Airtel_EbillUserId' AND object_id = OBJECT_ID('[dbo].[Airtel]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Airtel_EbillUserId] ON [dbo].[Airtel]
    ([EbillUserId] ASC);
    PRINT 'Index created on Airtel.EbillUserId.';
END
GO

-- Step 7: Update existing records by matching phone numbers
PRINT '';
PRINT 'Step 7: Matching existing records with EbillUsers...';

-- Match PSTN records with EbillUsers
UPDATE p
SET p.EbillUserId = e.Id
FROM [dbo].[PSTNs] p
INNER JOIN [dbo].[EbillUsers] e ON
    (p.DialedNumber = e.OfficialMobileNumber OR
     p.DialedNumber = '+' + e.OfficialMobileNumber OR
     '+' + p.DialedNumber = e.OfficialMobileNumber)
WHERE p.EbillUserId IS NULL;

PRINT 'PSTN records matched with EbillUsers.';
GO

-- Match PrivateWire records with EbillUsers
UPDATE pw
SET pw.EbillUserId = e.Id
FROM [dbo].[PrivateWires] pw
INNER JOIN [dbo].[EbillUsers] e ON
    (pw.DialedNumber = e.OfficialMobileNumber OR
     pw.DialedNumber = '+' + e.OfficialMobileNumber OR
     '+' + pw.DialedNumber = e.OfficialMobileNumber)
WHERE pw.EbillUserId IS NULL;

PRINT 'PrivateWire records matched with EbillUsers.';
GO

-- Match Safaricom records with EbillUsers
UPDATE s
SET s.EbillUserId = e.Id
FROM [dbo].[Safaricom] s
INNER JOIN [dbo].[EbillUsers] e ON
    (s.Dialed = e.OfficialMobileNumber OR
     s.Dialed = '+' + e.OfficialMobileNumber OR
     '+' + s.Dialed = e.OfficialMobileNumber)
WHERE s.EbillUserId IS NULL;

PRINT 'Safaricom records matched with EbillUsers.';
GO

-- Match Airtel records with EbillUsers
UPDATE a
SET a.EbillUserId = e.Id
FROM [dbo].[Airtel] a
INNER JOIN [dbo].[EbillUsers] e ON
    (a.Dialed = e.OfficialMobileNumber OR
     a.Dialed = '+' + e.OfficialMobileNumber OR
     '+' + a.Dialed = e.OfficialMobileNumber)
WHERE a.EbillUserId IS NULL;

PRINT 'Airtel records matched with EbillUsers.';
GO

-- Display summary
PRINT '';
PRINT '============================================';
PRINT 'Summary of EbillUser Relationships';
PRINT '============================================';

SELECT
    'PSTNs' as TableName,
    COUNT(*) as TotalRecords,
    COUNT(EbillUserId) as MatchedRecords,
    COUNT(*) - COUNT(EbillUserId) as UnmatchedRecords
FROM [dbo].[PSTNs]
UNION ALL
SELECT
    'PrivateWires',
    COUNT(*),
    COUNT(EbillUserId),
    COUNT(*) - COUNT(EbillUserId)
FROM [dbo].[PrivateWires]
UNION ALL
SELECT
    'Safaricom',
    COUNT(*),
    COUNT(EbillUserId),
    COUNT(*) - COUNT(EbillUserId)
FROM [dbo].[Safaricom]
UNION ALL
SELECT
    'Airtel',
    COUNT(*),
    COUNT(EbillUserId),
    COUNT(*) - COUNT(EbillUserId)
FROM [dbo].[Airtel];

PRINT '';
PRINT 'EbillUser relationships setup completed successfully!';
PRINT 'Note: Records without matching EbillUsers will need manual review.';
GO