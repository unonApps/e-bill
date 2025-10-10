-- Create Office table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Offices' AND xtype='U')
BEGIN
    CREATE TABLE [Offices] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(100) NOT NULL,
        [Description] nvarchar(500) NULL,
        [OrganizationId] int NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_Offices] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Offices_Organizations_OrganizationId] FOREIGN KEY ([OrganizationId]) REFERENCES [Organizations] ([Id]) ON DELETE CASCADE
    );

    CREATE INDEX [IX_Offices_OrganizationId] ON [Offices] ([OrganizationId]);
END

-- Create SubOffice table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='SubOffices' AND xtype='U')
BEGIN
    CREATE TABLE [SubOffices] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(100) NOT NULL,
        [Description] nvarchar(500) NULL,
        [ContactPerson] nvarchar(100) NULL,
        [PhoneNumber] nvarchar(20) NULL,
        [Email] nvarchar(100) NULL,
        [Address] nvarchar(200) NULL,
        [OfficeId] int NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [ModifiedDate] datetime2 NULL,
        CONSTRAINT [PK_SubOffices] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SubOffices_Offices_OfficeId] FOREIGN KEY ([OfficeId]) REFERENCES [Offices] ([Id]) ON DELETE CASCADE
    );

    CREATE INDEX [IX_SubOffices_OfficeId] ON [SubOffices] ([OfficeId]);
END

-- Add OfficeId to AspNetUsers if not exists
IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'OfficeId' AND Object_ID = Object_ID(N'AspNetUsers'))
BEGIN
    ALTER TABLE [AspNetUsers] ADD [OfficeId] int NULL;
    ALTER TABLE [AspNetUsers] ADD CONSTRAINT [FK_AspNetUsers_Offices_OfficeId] FOREIGN KEY ([OfficeId]) REFERENCES [Offices] ([Id]) ON DELETE SET NULL;
    CREATE INDEX [IX_AspNetUsers_OfficeId] ON [AspNetUsers] ([OfficeId]);
END

-- Add SubOfficeId to AspNetUsers if not exists
IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'SubOfficeId' AND Object_ID = Object_ID(N'AspNetUsers'))
BEGIN
    ALTER TABLE [AspNetUsers] ADD [SubOfficeId] int NULL;
    ALTER TABLE [AspNetUsers] ADD CONSTRAINT [FK_AspNetUsers_SubOffices_SubOfficeId] FOREIGN KEY ([SubOfficeId]) REFERENCES [SubOffices] ([Id]) ON DELETE SET NULL;
    CREATE INDEX [IX_AspNetUsers_SubOfficeId] ON [AspNetUsers] ([SubOfficeId]);
END

-- Mark migration as applied
INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
SELECT '20250915090747_AddOfficeAndSubOfficeTables', '8.0.0'
WHERE NOT EXISTS (SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId = '20250915090747_AddOfficeAndSubOfficeTables');

-- Mark previous migrations as applied if they aren't already
INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
SELECT MigrationId, '8.0.0' FROM (VALUES
    ('20250620192830_InitialCreate'),
    ('20250620200642_AddUserStatus'),
    ('20250620205129_AddClassOfService'),
    ('20250620211421_AddHandsetAllowanceToClassOfService'),
    ('20250620213518_AddServiceProvider'),
    ('20250620215436_AddRequestManagementTables'),
    ('20250620220910_UpdateSimRequestModel'),
    ('20250620234433_AddSimRequestHistory'),
    ('20250621162443_AddClassOfServiceFieldsToSimRequest'),
    ('20250621164056_AddSupervisorFieldsToRefundRequestAndEbill'),
    ('20250621175847_AddIctsFieldsToSimRequest'),
    ('20250622162737_AddPendingSIMCollectionStatus'),
    ('20250622184626_UpdateRefundRequestWorkflow'),
    ('20250622194507_UpdateRefundRequestModel'),
    ('20250623013242_AddCostAccountingFields'),
    ('20250623181816_AddClaimsUnitProcessingFields'),
    ('20250703094239_AddEbillUserEntity'),
    ('20250710122644_AddCallLogEntity'),
    ('20250710125334_AddImportAuditEntity')
) AS Migrations(MigrationId)
WHERE NOT EXISTS (SELECT 1 FROM __EFMigrationsHistory WHERE __EFMigrationsHistory.MigrationId = Migrations.MigrationId);

PRINT 'Office and SubOffice tables created successfully!';