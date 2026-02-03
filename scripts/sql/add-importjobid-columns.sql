-- Add ImportJobId column to telecom tables
-- This script adds the column if it doesn't exist (safe to run multiple times)

-- Add ImportJobId to Safaricom
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Safaricom]') AND name = 'ImportJobId')
BEGIN
    ALTER TABLE [Safaricom] ADD [ImportJobId] uniqueidentifier NULL;
    CREATE INDEX [IX_Safaricom_ImportJobId] ON [Safaricom] ([ImportJobId]);
    ALTER TABLE [Safaricom] ADD CONSTRAINT [FK_Safaricom_ImportJobs_ImportJobId] FOREIGN KEY ([ImportJobId]) REFERENCES [ImportJobs] ([Id]);
    PRINT 'Added ImportJobId to Safaricom table';
END
ELSE
    PRINT 'ImportJobId already exists in Safaricom table';
GO

-- Add ImportJobId to PSTNs
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[PSTNs]') AND name = 'ImportJobId')
BEGIN
    ALTER TABLE [PSTNs] ADD [ImportJobId] uniqueidentifier NULL;
    CREATE INDEX [IX_PSTNs_ImportJobId] ON [PSTNs] ([ImportJobId]);
    ALTER TABLE [PSTNs] ADD CONSTRAINT [FK_PSTNs_ImportJobs_ImportJobId] FOREIGN KEY ([ImportJobId]) REFERENCES [ImportJobs] ([Id]);
    PRINT 'Added ImportJobId to PSTNs table';
END
ELSE
    PRINT 'ImportJobId already exists in PSTNs table';
GO

-- Add ImportJobId to PrivateWires
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[PrivateWires]') AND name = 'ImportJobId')
BEGIN
    ALTER TABLE [PrivateWires] ADD [ImportJobId] uniqueidentifier NULL;
    CREATE INDEX [IX_PrivateWires_ImportJobId] ON [PrivateWires] ([ImportJobId]);
    ALTER TABLE [PrivateWires] ADD CONSTRAINT [FK_PrivateWires_ImportJobs_ImportJobId] FOREIGN KEY ([ImportJobId]) REFERENCES [ImportJobs] ([Id]);
    PRINT 'Added ImportJobId to PrivateWires table';
END
ELSE
    PRINT 'ImportJobId already exists in PrivateWires table';
GO

-- Add ImportJobId to Airtel
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Airtel]') AND name = 'ImportJobId')
BEGIN
    ALTER TABLE [Airtel] ADD [ImportJobId] uniqueidentifier NULL;
    CREATE INDEX [IX_Airtel_ImportJobId] ON [Airtel] ([ImportJobId]);
    ALTER TABLE [Airtel] ADD CONSTRAINT [FK_Airtel_ImportJobs_ImportJobId] FOREIGN KEY ([ImportJobId]) REFERENCES [ImportJobs] ([Id]);
    PRINT 'Added ImportJobId to Airtel table';
END
ELSE
    PRINT 'ImportJobId already exists in Airtel table';
GO

-- Ensure migration is recorded
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE MigrationId = '20260127060452_AddImportJobIdToTelecomModels')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260127060452_AddImportJobIdToTelecomModels', N'8.0.6');
    PRINT 'Added migration record to history';
END
ELSE
    PRINT 'Migration already recorded in history';
GO

PRINT 'ImportJobId migration script completed successfully';
