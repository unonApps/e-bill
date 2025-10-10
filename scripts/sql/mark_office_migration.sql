-- Mark the AddOfficeAndSubOfficeTables migration as applied since the tables/columns already exist
INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
VALUES ('20250915121903_AddOfficeAndSubOfficeTables', '8.0.6');

-- Create the PSTN table manually since the migration can't run
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='PSTNs' AND xtype='U')
BEGIN
    CREATE TABLE [PSTNs] (
        [Id] int NOT NULL IDENTITY,
        [Ext] nvarchar(50) NULL,
        [Dialed] nvarchar(100) NULL,
        [Time] time NULL,
        [Dest] nvarchar(200) NULL,
        [Dl] nvarchar(50) NULL,
        [Durx] decimal(18,2) NULL,
        [Org] nvarchar(100) NULL,
        [Office] nvarchar(100) NULL,
        [SubOffice] nvarchar(100) NULL,
        [Org_Unit] nvarchar(100) NULL,
        [Name] nvarchar(200) NULL,
        [Date] datetime2 NULL,
        [Dur] decimal(18,2) NULL,
        [Kshs] decimal(18,2) NULL,
        [Inde_] nvarchar(50) NULL,
        [Location] nvarchar(200) NULL,
        [Oca] nvarchar(50) NULL,
        [Car] nvarchar(50) NULL,
        [CreatedDate] datetime2 NOT NULL,
        [CreatedBy] nvarchar(max) NULL,
        [ModifiedDate] datetime2 NULL,
        [ModifiedBy] nvarchar(max) NULL,
        [ImportAuditId] int NULL,
        CONSTRAINT [PK_PSTNs] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PSTNs_ImportAudits_ImportAuditId] FOREIGN KEY ([ImportAuditId]) REFERENCES [ImportAudits] ([Id])
    );

    CREATE INDEX [IX_PSTNs_ImportAuditId] ON [PSTNs] ([ImportAuditId]);

    PRINT 'PSTNs table created successfully';
END
ELSE
BEGIN
    PRINT 'PSTNs table already exists';
END

-- Mark the PSTN migration as applied
IF NOT EXISTS (SELECT * FROM __EFMigrationsHistory WHERE MigrationId = '20250922000001_AddPSTNTable')
BEGIN
    INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20250922000001_AddPSTNTable', '8.0.6');
    PRINT 'PSTN migration marked as applied';
END