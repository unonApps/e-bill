IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250620192830_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetRoles] (
        [Id] nvarchar(450) NOT NULL,
        [Name] nvarchar(256) NULL,
        [NormalizedName] nvarchar(256) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250620192830_InitialCreate'
)
BEGIN
    CREATE TABLE [Organizations] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(100) NOT NULL,
        [Description] nvarchar(500) NULL,
        [CreatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_Organizations] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250620192830_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetRoleClaims] (
        [Id] int NOT NULL IDENTITY,
        [RoleId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250620192830_InitialCreate'
)
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
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250620192830_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUsers] (
        [Id] nvarchar(450) NOT NULL,
        [FirstName] nvarchar(max) NULL,
        [LastName] nvarchar(max) NULL,
        [RequirePasswordChange] bit NOT NULL,
        [OrganizationId] int NULL,
        [OfficeId] int NULL,
        [UserName] nvarchar(256) NULL,
        [NormalizedUserName] nvarchar(256) NULL,
        [Email] nvarchar(256) NULL,
        [NormalizedEmail] nvarchar(256) NULL,
        [EmailConfirmed] bit NOT NULL,
        [PasswordHash] nvarchar(max) NULL,
        [SecurityStamp] nvarchar(max) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        [PhoneNumber] nvarchar(max) NULL,
        [PhoneNumberConfirmed] bit NOT NULL,
        [TwoFactorEnabled] bit NOT NULL,
        [LockoutEnd] datetimeoffset NULL,
        [LockoutEnabled] bit NOT NULL,
        [AccessFailedCount] int NOT NULL,
        CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetUsers_Offices_OfficeId] FOREIGN KEY ([OfficeId]) REFERENCES [Offices] ([Id]),
        CONSTRAINT [FK_AspNetUsers_Organizations_OrganizationId] FOREIGN KEY ([OrganizationId]) REFERENCES [Organizations] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250620192830_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUserClaims] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250620192830_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUserLogins] (
        [LoginProvider] nvarchar(450) NOT NULL,
        [ProviderKey] nvarchar(450) NOT NULL,
        [ProviderDisplayName] nvarchar(max) NULL,
        [UserId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
        CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250620192830_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUserRoles] (
        [UserId] nvarchar(450) NOT NULL,
        [RoleId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
        CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250620192830_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUserTokens] (
        [UserId] nvarchar(450) NOT NULL,
        [LoginProvider] nvarchar(450) NOT NULL,
        [Name] nvarchar(450) NOT NULL,
        [Value] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
        CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250620192830_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250620192830_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250620192830_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250620192830_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250620192830_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250620192830_InitialCreate'
)
BEGIN
    CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250620192830_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AspNetUsers_OfficeId] ON [AspNetUsers] ([OfficeId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250620192830_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AspNetUsers_OrganizationId] ON [AspNetUsers] ([OrganizationId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250620192830_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250620192830_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Offices_OrganizationId] ON [Offices] ([OrganizationId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250620192830_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Organizations_Name] ON [Organizations] ([Name]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250620192830_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250620192830_InitialCreate', N'8.0.6');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250620200642_AddUserStatus'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [Status] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250620200642_AddUserStatus'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250620200642_AddUserStatus', N'8.0.6');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250620205129_AddClassOfService'
)
BEGIN
    CREATE TABLE [ClassOfServices] (
        [Id] int NOT NULL IDENTITY,
        [Class] nvarchar(100) NOT NULL,
        [Service] nvarchar(200) NOT NULL,
        [EligibleStaff] nvarchar(200) NOT NULL,
        [AirtimeAllowance] nvarchar(50) NULL,
        [DataAllowance] nvarchar(50) NULL,
        [HandsetAIRemarks] nvarchar(500) NULL,
        [ServiceStatus] int NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_ClassOfServices] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250620205129_AddClassOfService'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250620205129_AddClassOfService', N'8.0.6');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250620211421_AddHandsetAllowanceToClassOfService'
)
BEGIN
    ALTER TABLE [ClassOfServices] ADD [HandsetAllowance] nvarchar(50) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250620211421_AddHandsetAllowanceToClassOfService'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250620211421_AddHandsetAllowanceToClassOfService', N'8.0.6');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250620213518_AddServiceProvider'
)
BEGIN
    CREATE TABLE [ServiceProviders] (
        [Id] int NOT NULL IDENTITY,
        [SPID] nvarchar(10) NOT NULL,
        [ServiceProviderName] nvarchar(200) NOT NULL,
        [SPMainCP] nvarchar(200) NOT NULL,
        [SPMainCPEmail] nvarchar(300) NOT NULL,
        [SPOtherCPsEmail] nvarchar(1000) NULL,
        [SPStatus] int NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_ServiceProviders] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250620213518_AddServiceProvider'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ServiceProviders_SPID] ON [ServiceProviders] ([SPID]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250620213518_AddServiceProvider'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250620213518_AddServiceProvider', N'8.0.6');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250620215436_AddRequestManagementTables'
)
BEGIN
    CREATE TABLE [Ebills] (
        [Id] int NOT NULL IDENTITY,
        [FullName] nvarchar(100) NOT NULL,
        [Email] nvarchar(300) NOT NULL,
        [PhoneNumber] nvarchar(20) NOT NULL,
        [Department] nvarchar(100) NOT NULL,
        [ServiceProviderId] int NOT NULL,
        [AccountNumber] nvarchar(50) NOT NULL,
        [BillMonth] datetime2 NOT NULL,
        [BillAmount] decimal(18,2) NOT NULL,
        [DueDate] datetime2 NOT NULL,
        [BillType] int NOT NULL,
        [Description] nvarchar(500) NULL,
        [AdditionalNotes] nvarchar(500) NULL,
        [Status] int NOT NULL,
        [RequestDate] datetime2 NOT NULL,
        [ProcessedDate] datetime2 NULL,
        [RequestedBy] nvarchar(450) NOT NULL,
        [ProcessedBy] nvarchar(450) NULL,
        [ProcessingNotes] nvarchar(500) NULL,
        [PaymentDate] datetime2 NULL,
        [PaidAmount] decimal(18,2) NULL,
        CONSTRAINT [PK_Ebills] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Ebills_ServiceProviders_ServiceProviderId] FOREIGN KEY ([ServiceProviderId]) REFERENCES [ServiceProviders] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250620215436_AddRequestManagementTables'
)
BEGIN
    CREATE TABLE [RefundRequests] (
        [Id] int NOT NULL IDENTITY,
        [FullName] nvarchar(100) NOT NULL,
        [Email] nvarchar(300) NOT NULL,
        [PhoneNumber] nvarchar(20) NOT NULL,
        [Department] nvarchar(100) NOT NULL,
        [DeviceType] nvarchar(100) NOT NULL,
        [DeviceModel] nvarchar(100) NOT NULL,
        [SerialNumber] nvarchar(50) NULL,
        [IMEINumber] nvarchar(50) NULL,
        [PurchaseDate] datetime2 NOT NULL,
        [RefundAmount] decimal(18,2) NOT NULL,
        [RefundReason] nvarchar(500) NOT NULL,
        [AdditionalDetails] nvarchar(500) NULL,
        [Status] int NOT NULL,
        [RequestDate] datetime2 NOT NULL,
        [ProcessedDate] datetime2 NULL,
        [RequestedBy] nvarchar(450) NOT NULL,
        [ProcessedBy] nvarchar(450) NULL,
        [ProcessingNotes] nvarchar(500) NULL,
        [ApprovedAmount] decimal(18,2) NULL,
        CONSTRAINT [PK_RefundRequests] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250620215436_AddRequestManagementTables'
)
BEGIN
    CREATE TABLE [SimRequests] (
        [Id] int NOT NULL IDENTITY,
        [FullName] nvarchar(100) NOT NULL,
        [Email] nvarchar(300) NOT NULL,
        [PhoneNumber] nvarchar(20) NOT NULL,
        [Department] nvarchar(100) NOT NULL,
        [SimType] int NOT NULL,
        [ServiceProviderId] int NOT NULL,
        [ClassOfServiceId] int NOT NULL,
        [Justification] nvarchar(500) NULL,
        [AdditionalNotes] nvarchar(500) NULL,
        [Status] int NOT NULL,
        [RequestDate] datetime2 NOT NULL,
        [ProcessedDate] datetime2 NULL,
        [RequestedBy] nvarchar(450) NOT NULL,
        [ProcessedBy] nvarchar(450) NULL,
        [ProcessingNotes] nvarchar(500) NULL,
        CONSTRAINT [PK_SimRequests] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SimRequests_ClassOfServices_ClassOfServiceId] FOREIGN KEY ([ClassOfServiceId]) REFERENCES [ClassOfServices] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_SimRequests_ServiceProviders_ServiceProviderId] FOREIGN KEY ([ServiceProviderId]) REFERENCES [ServiceProviders] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250620215436_AddRequestManagementTables'
)
BEGIN
    CREATE INDEX [IX_Ebills_ServiceProviderId] ON [Ebills] ([ServiceProviderId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250620215436_AddRequestManagementTables'
)
BEGIN
    CREATE INDEX [IX_SimRequests_ClassOfServiceId] ON [SimRequests] ([ClassOfServiceId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250620215436_AddRequestManagementTables'
)
BEGIN
    CREATE INDEX [IX_SimRequests_ServiceProviderId] ON [SimRequests] ([ServiceProviderId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250620215436_AddRequestManagementTables'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250620215436_AddRequestManagementTables', N'8.0.6');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250620220910_UpdateSimRequestModel'
)
BEGIN
    ALTER TABLE [SimRequests] DROP CONSTRAINT [FK_SimRequests_ClassOfServices_ClassOfServiceId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250620220910_UpdateSimRequestModel'
)
BEGIN
    DROP INDEX [IX_SimRequests_ClassOfServiceId] ON [SimRequests];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250620220910_UpdateSimRequestModel'
)
BEGIN
    DECLARE @var0 sysname;
    SELECT @var0 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[SimRequests]') AND [c].[name] = N'ClassOfServiceId');
    IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [SimRequests] DROP CONSTRAINT [' + @var0 + '];');
    ALTER TABLE [SimRequests] DROP COLUMN [ClassOfServiceId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250620220910_UpdateSimRequestModel'
)
BEGIN
    EXEC sp_rename N'[SimRequests].[PhoneNumber]', N'IndexNo', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250620220910_UpdateSimRequestModel'
)
BEGIN
    EXEC sp_rename N'[SimRequests].[Justification]', N'SupervisorNotes', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250620220910_UpdateSimRequestModel'
)
BEGIN
    EXEC sp_rename N'[SimRequests].[FullName]', N'LastName', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250620220910_UpdateSimRequestModel'
)
BEGIN
    EXEC sp_rename N'[SimRequests].[Email]', N'OfficialEmail', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250620220910_UpdateSimRequestModel'
)
BEGIN
    EXEC sp_rename N'[SimRequests].[Department]', N'FirstName', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250620220910_UpdateSimRequestModel'
)
BEGIN
    EXEC sp_rename N'[SimRequests].[AdditionalNotes]', N'Remarks', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250620220910_UpdateSimRequestModel'
)
BEGIN
    ALTER TABLE [SimRequests] ADD [FunctionalTitle] nvarchar(300) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250620220910_UpdateSimRequestModel'
)
BEGIN
    ALTER TABLE [SimRequests] ADD [Grade] nvarchar(50) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250620220910_UpdateSimRequestModel'
)
BEGIN
    ALTER TABLE [SimRequests] ADD [Office] nvarchar(200) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250620220910_UpdateSimRequestModel'
)
BEGIN
    ALTER TABLE [SimRequests] ADD [OfficeExtension] nvarchar(20) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250620220910_UpdateSimRequestModel'
)
BEGIN
    ALTER TABLE [SimRequests] ADD [Organization] nvarchar(200) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250620220910_UpdateSimRequestModel'
)
BEGIN
    ALTER TABLE [SimRequests] ADD [PreviouslyAssignedLines] nvarchar(1000) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250620220910_UpdateSimRequestModel'
)
BEGIN
    ALTER TABLE [SimRequests] ADD [SubmittedToSupervisor] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250620220910_UpdateSimRequestModel'
)
BEGIN
    ALTER TABLE [SimRequests] ADD [Supervisor] nvarchar(200) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250620220910_UpdateSimRequestModel'
)
BEGIN
    ALTER TABLE [SimRequests] ADD [SupervisorApprovalDate] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250620220910_UpdateSimRequestModel'
)
BEGIN
    CREATE UNIQUE INDEX [IX_SimRequests_IndexNo] ON [SimRequests] ([IndexNo]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250620220910_UpdateSimRequestModel'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250620220910_UpdateSimRequestModel', N'8.0.6');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250620234433_AddSimRequestHistory'
)
BEGIN
    CREATE TABLE [SimRequestHistories] (
        [Id] int NOT NULL IDENTITY,
        [SimRequestId] int NOT NULL,
        [Action] nvarchar(100) NOT NULL,
        [PreviousStatus] nvarchar(50) NULL,
        [NewStatus] nvarchar(50) NULL,
        [Comments] nvarchar(1000) NULL,
        [PerformedBy] nvarchar(450) NOT NULL,
        [UserName] nvarchar(200) NOT NULL,
        [Timestamp] datetime2 NOT NULL,
        [IpAddress] nvarchar(50) NULL,
        CONSTRAINT [PK_SimRequestHistories] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SimRequestHistories_SimRequests_SimRequestId] FOREIGN KEY ([SimRequestId]) REFERENCES [SimRequests] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250620234433_AddSimRequestHistory'
)
BEGIN
    CREATE INDEX [IX_SimRequestHistories_SimRequestId] ON [SimRequestHistories] ([SimRequestId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250620234433_AddSimRequestHistory'
)
BEGIN
    CREATE INDEX [IX_SimRequestHistories_Timestamp] ON [SimRequestHistories] ([Timestamp]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250620234433_AddSimRequestHistory'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250620234433_AddSimRequestHistory', N'8.0.6');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250621162443_AddClassOfServiceFieldsToSimRequest'
)
BEGIN
    ALTER TABLE [SimRequests] ADD [HandsetAllowance] nvarchar(100) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250621162443_AddClassOfServiceFieldsToSimRequest'
)
BEGIN
    ALTER TABLE [SimRequests] ADD [MobileService] nvarchar(100) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250621162443_AddClassOfServiceFieldsToSimRequest'
)
BEGIN
    ALTER TABLE [SimRequests] ADD [MobileServiceAllowance] nvarchar(100) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250621162443_AddClassOfServiceFieldsToSimRequest'
)
BEGIN
    ALTER TABLE [SimRequests] ADD [SupervisorEmail] nvarchar(300) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250621162443_AddClassOfServiceFieldsToSimRequest'
)
BEGIN
    ALTER TABLE [SimRequests] ADD [SupervisorName] nvarchar(300) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250621162443_AddClassOfServiceFieldsToSimRequest'
)
BEGIN
    ALTER TABLE [SimRequests] ADD [SupervisorRemarks] nvarchar(200) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250621162443_AddClassOfServiceFieldsToSimRequest'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250621162443_AddClassOfServiceFieldsToSimRequest', N'8.0.6');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250621164056_AddSupervisorFieldsToRefundRequestAndEbill'
)
BEGIN
    ALTER TABLE [RefundRequests] ADD [SubmittedToSupervisor] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250621164056_AddSupervisorFieldsToRefundRequestAndEbill'
)
BEGIN
    ALTER TABLE [RefundRequests] ADD [Supervisor] nvarchar(200) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250621164056_AddSupervisorFieldsToRefundRequestAndEbill'
)
BEGIN
    ALTER TABLE [RefundRequests] ADD [SupervisorApprovalDate] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250621164056_AddSupervisorFieldsToRefundRequestAndEbill'
)
BEGIN
    ALTER TABLE [RefundRequests] ADD [SupervisorEmail] nvarchar(300) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250621164056_AddSupervisorFieldsToRefundRequestAndEbill'
)
BEGIN
    ALTER TABLE [RefundRequests] ADD [SupervisorName] nvarchar(300) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250621164056_AddSupervisorFieldsToRefundRequestAndEbill'
)
BEGIN
    ALTER TABLE [RefundRequests] ADD [SupervisorNotes] nvarchar(500) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250621164056_AddSupervisorFieldsToRefundRequestAndEbill'
)
BEGIN
    ALTER TABLE [RefundRequests] ADD [SupervisorRemarks] nvarchar(200) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250621164056_AddSupervisorFieldsToRefundRequestAndEbill'
)
BEGIN
    ALTER TABLE [Ebills] ADD [SubmittedToSupervisor] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250621164056_AddSupervisorFieldsToRefundRequestAndEbill'
)
BEGIN
    ALTER TABLE [Ebills] ADD [Supervisor] nvarchar(200) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250621164056_AddSupervisorFieldsToRefundRequestAndEbill'
)
BEGIN
    ALTER TABLE [Ebills] ADD [SupervisorApprovalDate] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250621164056_AddSupervisorFieldsToRefundRequestAndEbill'
)
BEGIN
    ALTER TABLE [Ebills] ADD [SupervisorEmail] nvarchar(300) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250621164056_AddSupervisorFieldsToRefundRequestAndEbill'
)
BEGIN
    ALTER TABLE [Ebills] ADD [SupervisorName] nvarchar(300) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250621164056_AddSupervisorFieldsToRefundRequestAndEbill'
)
BEGIN
    ALTER TABLE [Ebills] ADD [SupervisorNotes] nvarchar(500) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250621164056_AddSupervisorFieldsToRefundRequestAndEbill'
)
BEGIN
    ALTER TABLE [Ebills] ADD [SupervisorRemarks] nvarchar(200) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250621164056_AddSupervisorFieldsToRefundRequestAndEbill'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250621164056_AddSupervisorFieldsToRefundRequestAndEbill', N'8.0.6');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250621175847_AddIctsFieldsToSimRequest'
)
BEGIN
    ALTER TABLE [SimRequests] ADD [AssignedNo] nvarchar(50) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250621175847_AddIctsFieldsToSimRequest'
)
BEGIN
    ALTER TABLE [SimRequests] ADD [CollectionNotifiedDate] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250621175847_AddIctsFieldsToSimRequest'
)
BEGIN
    ALTER TABLE [SimRequests] ADD [IctsRemark] nvarchar(200) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250621175847_AddIctsFieldsToSimRequest'
)
BEGIN
    ALTER TABLE [SimRequests] ADD [LineType] nvarchar(20) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250621175847_AddIctsFieldsToSimRequest'
)
BEGIN
    ALTER TABLE [SimRequests] ADD [LineUsage] nvarchar(20) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250621175847_AddIctsFieldsToSimRequest'
)
BEGIN
    ALTER TABLE [SimRequests] ADD [PreviousLines] nvarchar(500) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250621175847_AddIctsFieldsToSimRequest'
)
BEGIN
    ALTER TABLE [SimRequests] ADD [ServiceRequestNo] nvarchar(50) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250621175847_AddIctsFieldsToSimRequest'
)
BEGIN
    ALTER TABLE [SimRequests] ADD [SimCollectedBy] nvarchar(100) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250621175847_AddIctsFieldsToSimRequest'
)
BEGIN
    ALTER TABLE [SimRequests] ADD [SimCollectedDate] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250621175847_AddIctsFieldsToSimRequest'
)
BEGIN
    ALTER TABLE [SimRequests] ADD [SimIssuedBy] nvarchar(100) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250621175847_AddIctsFieldsToSimRequest'
)
BEGIN
    ALTER TABLE [SimRequests] ADD [SimPuk] nvarchar(20) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250621175847_AddIctsFieldsToSimRequest'
)
BEGIN
    ALTER TABLE [SimRequests] ADD [SimSerialNo] nvarchar(50) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250621175847_AddIctsFieldsToSimRequest'
)
BEGIN
    ALTER TABLE [SimRequests] ADD [SpNotifiedDate] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250621175847_AddIctsFieldsToSimRequest'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250621175847_AddIctsFieldsToSimRequest', N'8.0.6');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250622162737_AddPendingSIMCollectionStatus'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250622162737_AddPendingSIMCollectionStatus', N'8.0.6');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250622184626_UpdateRefundRequestWorkflow'
)
BEGIN
    ALTER TABLE [RefundRequests] ADD [BudgetOfficerApprovalDate] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250622184626_UpdateRefundRequestWorkflow'
)
BEGIN
    ALTER TABLE [RefundRequests] ADD [BudgetOfficerEmail] nvarchar(300) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250622184626_UpdateRefundRequestWorkflow'
)
BEGIN
    ALTER TABLE [RefundRequests] ADD [BudgetOfficerName] nvarchar(300) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250622184626_UpdateRefundRequestWorkflow'
)
BEGIN
    ALTER TABLE [RefundRequests] ADD [BudgetOfficerNotes] nvarchar(500) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250622184626_UpdateRefundRequestWorkflow'
)
BEGIN
    ALTER TABLE [RefundRequests] ADD [BudgetOfficerRemarks] nvarchar(200) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250622184626_UpdateRefundRequestWorkflow'
)
BEGIN
    ALTER TABLE [RefundRequests] ADD [CancellationDate] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250622184626_UpdateRefundRequestWorkflow'
)
BEGIN
    ALTER TABLE [RefundRequests] ADD [CancellationReason] nvarchar(500) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250622184626_UpdateRefundRequestWorkflow'
)
BEGIN
    ALTER TABLE [RefundRequests] ADD [CancelledBy] nvarchar(300) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250622184626_UpdateRefundRequestWorkflow'
)
BEGIN
    ALTER TABLE [RefundRequests] ADD [CompletionDate] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250622184626_UpdateRefundRequestWorkflow'
)
BEGIN
    ALTER TABLE [RefundRequests] ADD [CompletionNotes] nvarchar(500) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250622184626_UpdateRefundRequestWorkflow'
)
BEGIN
    ALTER TABLE [RefundRequests] ADD [PaymentApprovalDate] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250622184626_UpdateRefundRequestWorkflow'
)
BEGIN
    ALTER TABLE [RefundRequests] ADD [PaymentApprovalNotes] nvarchar(500) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250622184626_UpdateRefundRequestWorkflow'
)
BEGIN
    ALTER TABLE [RefundRequests] ADD [PaymentApprovalRemarks] nvarchar(200) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250622184626_UpdateRefundRequestWorkflow'
)
BEGIN
    ALTER TABLE [RefundRequests] ADD [PaymentApproverEmail] nvarchar(300) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250622184626_UpdateRefundRequestWorkflow'
)
BEGIN
    ALTER TABLE [RefundRequests] ADD [PaymentApproverName] nvarchar(300) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250622184626_UpdateRefundRequestWorkflow'
)
BEGIN
    ALTER TABLE [RefundRequests] ADD [PaymentReference] nvarchar(100) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250622184626_UpdateRefundRequestWorkflow'
)
BEGIN
    ALTER TABLE [RefundRequests] ADD [StaffClaimsApprovalDate] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250622184626_UpdateRefundRequestWorkflow'
)
BEGIN
    ALTER TABLE [RefundRequests] ADD [StaffClaimsNotes] nvarchar(500) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250622184626_UpdateRefundRequestWorkflow'
)
BEGIN
    ALTER TABLE [RefundRequests] ADD [StaffClaimsOfficerEmail] nvarchar(300) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250622184626_UpdateRefundRequestWorkflow'
)
BEGIN
    ALTER TABLE [RefundRequests] ADD [StaffClaimsOfficerName] nvarchar(300) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250622184626_UpdateRefundRequestWorkflow'
)
BEGIN
    ALTER TABLE [RefundRequests] ADD [StaffClaimsRemarks] nvarchar(200) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250622184626_UpdateRefundRequestWorkflow'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250622184626_UpdateRefundRequestWorkflow', N'8.0.6');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250622194507_UpdateRefundRequestModel'
)
BEGIN
    DECLARE @var1 sysname;
    SELECT @var1 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[RefundRequests]') AND [c].[name] = N'AdditionalDetails');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [RefundRequests] DROP CONSTRAINT [' + @var1 + '];');
    ALTER TABLE [RefundRequests] DROP COLUMN [AdditionalDetails];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250622194507_UpdateRefundRequestModel'
)
BEGIN
    DECLARE @var2 sysname;
    SELECT @var2 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[RefundRequests]') AND [c].[name] = N'ApprovedAmount');
    IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [RefundRequests] DROP CONSTRAINT [' + @var2 + '];');
    ALTER TABLE [RefundRequests] DROP COLUMN [ApprovedAmount];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250622194507_UpdateRefundRequestModel'
)
BEGIN
    DECLARE @var3 sysname;
    SELECT @var3 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[RefundRequests]') AND [c].[name] = N'Department');
    IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [RefundRequests] DROP CONSTRAINT [' + @var3 + '];');
    ALTER TABLE [RefundRequests] DROP COLUMN [Department];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250622194507_UpdateRefundRequestModel'
)
BEGIN
    DECLARE @var4 sysname;
    SELECT @var4 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[RefundRequests]') AND [c].[name] = N'DeviceModel');
    IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [RefundRequests] DROP CONSTRAINT [' + @var4 + '];');
    ALTER TABLE [RefundRequests] DROP COLUMN [DeviceModel];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250622194507_UpdateRefundRequestModel'
)
BEGIN
    DECLARE @var5 sysname;
    SELECT @var5 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[RefundRequests]') AND [c].[name] = N'Email');
    IF @var5 IS NOT NULL EXEC(N'ALTER TABLE [RefundRequests] DROP CONSTRAINT [' + @var5 + '];');
    ALTER TABLE [RefundRequests] DROP COLUMN [Email];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250622194507_UpdateRefundRequestModel'
)
BEGIN
    DECLARE @var6 sysname;
    SELECT @var6 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[RefundRequests]') AND [c].[name] = N'IMEINumber');
    IF @var6 IS NOT NULL EXEC(N'ALTER TABLE [RefundRequests] DROP CONSTRAINT [' + @var6 + '];');
    ALTER TABLE [RefundRequests] DROP COLUMN [IMEINumber];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250622194507_UpdateRefundRequestModel'
)
BEGIN
    DECLARE @var7 sysname;
    SELECT @var7 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[RefundRequests]') AND [c].[name] = N'PhoneNumber');
    IF @var7 IS NOT NULL EXEC(N'ALTER TABLE [RefundRequests] DROP CONSTRAINT [' + @var7 + '];');
    ALTER TABLE [RefundRequests] DROP COLUMN [PhoneNumber];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250622194507_UpdateRefundRequestModel'
)
BEGIN
    DECLARE @var8 sysname;
    SELECT @var8 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[RefundRequests]') AND [c].[name] = N'ProcessingNotes');
    IF @var8 IS NOT NULL EXEC(N'ALTER TABLE [RefundRequests] DROP CONSTRAINT [' + @var8 + '];');
    ALTER TABLE [RefundRequests] DROP COLUMN [ProcessingNotes];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250622194507_UpdateRefundRequestModel'
)
BEGIN
    DECLARE @var9 sysname;
    SELECT @var9 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[RefundRequests]') AND [c].[name] = N'PurchaseDate');
    IF @var9 IS NOT NULL EXEC(N'ALTER TABLE [RefundRequests] DROP CONSTRAINT [' + @var9 + '];');
    ALTER TABLE [RefundRequests] DROP COLUMN [PurchaseDate];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250622194507_UpdateRefundRequestModel'
)
BEGIN
    DECLARE @var10 sysname;
    SELECT @var10 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[RefundRequests]') AND [c].[name] = N'SerialNumber');
    IF @var10 IS NOT NULL EXEC(N'ALTER TABLE [RefundRequests] DROP CONSTRAINT [' + @var10 + '];');
    ALTER TABLE [RefundRequests] DROP COLUMN [SerialNumber];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250622194507_UpdateRefundRequestModel'
)
BEGIN
    EXEC sp_rename N'[RefundRequests].[RefundReason]', N'PurchaseReceiptPath', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250622194507_UpdateRefundRequestModel'
)
BEGIN
    EXEC sp_rename N'[RefundRequests].[RefundAmount]', N'DevicePurchaseAmount', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250622194507_UpdateRefundRequestModel'
)
BEGIN
    EXEC sp_rename N'[RefundRequests].[ProcessedDate]', N'PreviousDeviceReimbursedDate', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250622194507_UpdateRefundRequestModel'
)
BEGIN
    EXEC sp_rename N'[RefundRequests].[FullName]', N'MobileService', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250622194507_UpdateRefundRequestModel'
)
BEGIN
    EXEC sp_rename N'[RefundRequests].[DeviceType]', N'ClassOfService', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250622194507_UpdateRefundRequestModel'
)
BEGIN
    ALTER TABLE [RefundRequests] ADD [DeviceAllowance] decimal(18,2) NOT NULL DEFAULT 0.0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250622194507_UpdateRefundRequestModel'
)
BEGIN
    ALTER TABLE [RefundRequests] ADD [DevicePurchaseCurrency] nvarchar(3) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250622194507_UpdateRefundRequestModel'
)
BEGIN
    ALTER TABLE [RefundRequests] ADD [IndexNo] nvarchar(50) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250622194507_UpdateRefundRequestModel'
)
BEGIN
    ALTER TABLE [RefundRequests] ADD [MobileNumberAssignedTo] nvarchar(200) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250622194507_UpdateRefundRequestModel'
)
BEGIN
    ALTER TABLE [RefundRequests] ADD [Office] nvarchar(200) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250622194507_UpdateRefundRequestModel'
)
BEGIN
    ALTER TABLE [RefundRequests] ADD [OfficeExtension] nvarchar(20) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250622194507_UpdateRefundRequestModel'
)
BEGIN
    ALTER TABLE [RefundRequests] ADD [Organization] nvarchar(200) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250622194507_UpdateRefundRequestModel'
)
BEGIN
    ALTER TABLE [RefundRequests] ADD [PrimaryMobileNumber] nvarchar(9) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250622194507_UpdateRefundRequestModel'
)
BEGIN
    ALTER TABLE [RefundRequests] ADD [Remarks] nvarchar(200) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250622194507_UpdateRefundRequestModel'
)
BEGIN
    ALTER TABLE [RefundRequests] ADD [UmojaBankName] nvarchar(200) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250622194507_UpdateRefundRequestModel'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250622194507_UpdateRefundRequestModel', N'8.0.6');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250623013242_AddCostAccountingFields'
)
BEGIN
    ALTER TABLE [RefundRequests] ADD [CostCenter] nvarchar(100) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250623013242_AddCostAccountingFields'
)
BEGIN
    ALTER TABLE [RefundRequests] ADD [CostObject] nvarchar(100) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250623013242_AddCostAccountingFields'
)
BEGIN
    ALTER TABLE [RefundRequests] ADD [FundCommitment] nvarchar(100) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250623013242_AddCostAccountingFields'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250623013242_AddCostAccountingFields', N'8.0.6');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250623181816_AddClaimsUnitProcessingFields'
)
BEGIN
    ALTER TABLE [RefundRequests] ADD [ClaimsActionDate] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250623181816_AddClaimsUnitProcessingFields'
)
BEGIN
    ALTER TABLE [RefundRequests] ADD [RefundUsdAmount] decimal(18,2) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250623181816_AddClaimsUnitProcessingFields'
)
BEGIN
    ALTER TABLE [RefundRequests] ADD [UmojaPaymentDocumentId] nvarchar(100) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250623181816_AddClaimsUnitProcessingFields'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250623181816_AddClaimsUnitProcessingFields', N'8.0.6');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250703094239_AddEbillUserEntity'
)
BEGIN
    ALTER TABLE [SimRequestHistories] ADD [SimRequestId1] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250703094239_AddEbillUserEntity'
)
BEGIN
    CREATE TABLE [EbillUsers] (
        [Id] int NOT NULL IDENTITY,
        [FirstName] nvarchar(100) NOT NULL,
        [LastName] nvarchar(100) NOT NULL,
        [IndexNumber] nvarchar(50) NOT NULL,
        [OfficialMobileNumber] nvarchar(20) NOT NULL,
        [IssuedDeviceID] nvarchar(100) NULL,
        [Email] nvarchar(256) NOT NULL,
        [ClassOfService] nvarchar(100) NULL,
        [Organization] nvarchar(100) NULL,
        [Office] nvarchar(100) NULL,
        [IsActive] bit NOT NULL,
        [SupervisorIndexNumber] nvarchar(50) NULL,
        [SupervisorName] nvarchar(200) NULL,
        [SupervisorEmail] nvarchar(256) NULL,
        [CreatedDate] datetime2 NOT NULL,
        [LastModifiedDate] datetime2 NULL,
        CONSTRAINT [PK_EbillUsers] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250703094239_AddEbillUserEntity'
)
BEGIN
    CREATE INDEX [IX_SimRequestHistories_SimRequestId1] ON [SimRequestHistories] ([SimRequestId1]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250703094239_AddEbillUserEntity'
)
BEGIN
    CREATE UNIQUE INDEX [IX_EbillUsers_Email] ON [EbillUsers] ([Email]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250703094239_AddEbillUserEntity'
)
BEGIN
    CREATE UNIQUE INDEX [IX_EbillUsers_IndexNumber] ON [EbillUsers] ([IndexNumber]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250703094239_AddEbillUserEntity'
)
BEGIN
    ALTER TABLE [SimRequestHistories] ADD CONSTRAINT [FK_SimRequestHistories_SimRequests_SimRequestId1] FOREIGN KEY ([SimRequestId1]) REFERENCES [SimRequests] ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250703094239_AddEbillUserEntity'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250703094239_AddEbillUserEntity', N'8.0.6');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250710122644_AddCallLogEntity'
)
BEGIN
    CREATE TABLE [CallLogs] (
        [Id] int NOT NULL IDENTITY,
        [AccountNo] nvarchar(20) NOT NULL,
        [SubAccountNo] nvarchar(50) NOT NULL,
        [SubAccountName] nvarchar(200) NOT NULL,
        [MSISDN] nvarchar(20) NOT NULL,
        [TaxInvoiceSummaryNo] nvarchar(50) NOT NULL,
        [InvoiceNo] nvarchar(50) NOT NULL,
        [InvoiceDate] datetime2 NOT NULL,
        [NetAccessFee] decimal(18,2) NOT NULL,
        [NetUsageLessTax] decimal(18,2) NOT NULL,
        [LessTaxes] decimal(18,2) NOT NULL,
        [VAT16] decimal(18,2) NULL,
        [Excise15] decimal(18,2) NULL,
        [GrossTotal] decimal(18,2) NOT NULL,
        [EbillUserId] int NULL,
        [CreatedDate] datetime2 NOT NULL,
        [ImportedBy] nvarchar(max) NULL,
        [ImportedDate] datetime2 NULL,
        CONSTRAINT [PK_CallLogs] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CallLogs_EbillUsers_EbillUserId] FOREIGN KEY ([EbillUserId]) REFERENCES [EbillUsers] ([Id]) ON DELETE SET NULL
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250710122644_AddCallLogEntity'
)
BEGIN
    CREATE INDEX [IX_CallLogs_EbillUserId] ON [CallLogs] ([EbillUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250710122644_AddCallLogEntity'
)
BEGIN
    CREATE INDEX [IX_CallLogs_MSISDN] ON [CallLogs] ([MSISDN]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250710122644_AddCallLogEntity'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250710122644_AddCallLogEntity', N'8.0.6');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250710125334_AddImportAuditEntity'
)
BEGIN
    CREATE TABLE [ImportAudits] (
        [Id] int NOT NULL IDENTITY,
        [ImportType] nvarchar(50) NOT NULL,
        [FileName] nvarchar(200) NOT NULL,
        [FileSize] bigint NOT NULL,
        [TotalRecords] int NOT NULL,
        [SuccessCount] int NOT NULL,
        [SkippedCount] int NOT NULL,
        [ErrorCount] int NOT NULL,
        [UpdatedCount] int NOT NULL,
        [ImportDate] datetime2 NOT NULL,
        [ImportedBy] nvarchar(100) NOT NULL,
        [IpAddress] nvarchar(50) NULL,
        [ProcessingTime] time NOT NULL,
        [DetailedResults] nvarchar(max) NULL,
        [SummaryMessage] nvarchar(500) NULL,
        [ImportOptions] nvarchar(max) NULL,
        CONSTRAINT [PK_ImportAudits] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250710125334_AddImportAuditEntity'
)
BEGIN
    CREATE INDEX [IX_ImportAudits_ImportDate] ON [ImportAudits] ([ImportDate]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250710125334_AddImportAuditEntity'
)
BEGIN
    CREATE INDEX [IX_ImportAudits_ImportType] ON [ImportAudits] ([ImportType]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250710125334_AddImportAuditEntity'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250710125334_AddImportAuditEntity', N'8.0.6');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002123541_AddVerificationPeriodToCallRecords'
)
BEGIN
    ALTER TABLE [CallRecords] ADD [verification_period] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002123541_AddVerificationPeriodToCallRecords'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251002123541_AddVerificationPeriodToCallRecords', N'8.0.6');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002163017_AddCallLogVerificationSystemTables'
)
BEGIN
    CREATE TABLE [AnomalyTypes] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(50) NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [Description] nvarchar(500) NOT NULL,
        [Severity] int NOT NULL,
        [AutoReject] bit NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_AnomalyTypes] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002163017_AddCallLogVerificationSystemTables'
)
BEGIN
    CREATE TABLE [AspNetRoles] (
        [Id] nvarchar(450) NOT NULL,
        [Name] nvarchar(256) NULL,
        [NormalizedName] nvarchar(256) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002163017_AddCallLogVerificationSystemTables'
)
BEGIN
    CREATE TABLE [AuditLogs] (
        [Id] int NOT NULL IDENTITY,
        [EntityType] nvarchar(100) NOT NULL,
        [EntityId] nvarchar(100) NULL,
        [Action] nvarchar(50) NOT NULL,
        [Description] nvarchar(500) NULL,
        [OldValues] nvarchar(2000) NULL,
        [NewValues] nvarchar(2000) NULL,
        [PerformedBy] nvarchar(100) NOT NULL,
        [PerformedDate] datetime2 NOT NULL,
        [IPAddress] nvarchar(50) NULL,
        [UserAgent] nvarchar(500) NULL,
        [Module] nvarchar(50) NULL,
        [IsSuccess] bit NOT NULL,
        [ErrorMessage] nvarchar(1000) NULL,
        [AdditionalData] nvarchar(4000) NULL,
        CONSTRAINT [PK_AuditLogs] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002163017_AddCallLogVerificationSystemTables'
)
BEGIN
    CREATE TABLE [BillingPeriods] (
        [Id] int NOT NULL IDENTITY,
        [PeriodCode] nvarchar(20) NOT NULL,
        [StartDate] datetime2 NOT NULL,
        [EndDate] datetime2 NOT NULL,
        [Status] nvarchar(20) NOT NULL,
        [MonthlyImportDate] datetime2 NULL,
        [MonthlyBatchId] uniqueidentifier NULL,
        [MonthlyRecordCount] int NOT NULL,
        [MonthlyTotalCost] decimal(18,2) NOT NULL,
        [InterimUpdateCount] int NOT NULL,
        [LastInterimDate] datetime2 NULL,
        [InterimRecordCount] int NOT NULL,
        [InterimAdjustmentAmount] decimal(18,2) NOT NULL,
        [ClosedDate] datetime2 NULL,
        [ClosedBy] nvarchar(100) NULL,
        [LockedDate] datetime2 NULL,
        [LockedBy] nvarchar(100) NULL,
        [CreatedDate] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NOT NULL,
        [ModifiedDate] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [Notes] nvarchar(max) NULL,
        CONSTRAINT [PK_BillingPeriods] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002163017_AddCallLogVerificationSystemTables'
)
BEGIN
    CREATE TABLE [ClassOfServices] (
        [Id] int NOT NULL IDENTITY,
        [PublicId] uniqueidentifier NOT NULL,
        [Class] nvarchar(100) NOT NULL,
        [Service] nvarchar(200) NOT NULL,
        [EligibleStaff] nvarchar(200) NOT NULL,
        [AirtimeAllowance] nvarchar(50) NULL,
        [DataAllowance] nvarchar(50) NULL,
        [HandsetAllowance] nvarchar(50) NULL,
        [HandsetAIRemarks] nvarchar(500) NULL,
        [AirtimeAllowanceAmount] decimal(18,4) NULL,
        [DataAllowanceAmount] decimal(18,4) NULL,
        [MonthlyCallCostLimit] decimal(18,4) NULL,
        [BillingPeriod] nvarchar(20) NOT NULL,
        [ServiceStatus] int NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_ClassOfServices] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002163017_AddCallLogVerificationSystemTables'
)
BEGIN
    CREATE TABLE [ExchangeRates] (
        [Id] int NOT NULL IDENTITY,
        [Month] int NOT NULL,
        [Year] int NOT NULL,
        [Rate] decimal(18,4) NOT NULL,
        [CreatedBy] nvarchar(256) NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedBy] nvarchar(256) NULL,
        [UpdatedDate] datetime2 NULL,
        CONSTRAINT [PK_ExchangeRates] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002163017_AddCallLogVerificationSystemTables'
)
BEGIN
    CREATE TABLE [ImportAudits] (
        [Id] int NOT NULL IDENTITY,
        [ImportType] nvarchar(50) NOT NULL,
        [FileName] nvarchar(200) NOT NULL,
        [FileSize] bigint NOT NULL,
        [TotalRecords] int NOT NULL,
        [SuccessCount] int NOT NULL,
        [SkippedCount] int NOT NULL,
        [ErrorCount] int NOT NULL,
        [UpdatedCount] int NOT NULL,
        [ImportDate] datetime2 NOT NULL,
        [ImportedBy] nvarchar(100) NOT NULL,
        [IpAddress] nvarchar(50) NULL,
        [ProcessingTime] time NOT NULL,
        [DetailedResults] nvarchar(max) NULL,
        [SummaryMessage] nvarchar(500) NULL,
        [ImportOptions] nvarchar(max) NULL,
        [DateFormatPreferences] nvarchar(500) NULL,
        CONSTRAINT [PK_ImportAudits] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002163017_AddCallLogVerificationSystemTables'
)
BEGIN
    CREATE TABLE [Organizations] (
        [Id] int NOT NULL IDENTITY,
        [PublicId] uniqueidentifier NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [Code] nvarchar(10) NULL,
        [Description] nvarchar(500) NULL,
        [CreatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_Organizations] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002163017_AddCallLogVerificationSystemTables'
)
BEGIN
    CREATE TABLE [RefundRequests] (
        [Id] int NOT NULL IDENTITY,
        [PrimaryMobileNumber] nvarchar(9) NOT NULL,
        [IndexNo] nvarchar(50) NOT NULL,
        [MobileNumberAssignedTo] nvarchar(200) NOT NULL,
        [OfficeExtension] nvarchar(20) NULL,
        [Office] nvarchar(200) NOT NULL,
        [MobileService] nvarchar(100) NOT NULL,
        [ClassOfService] nvarchar(100) NOT NULL,
        [DeviceAllowance] decimal(18,2) NOT NULL,
        [PreviousDeviceReimbursedDate] datetime2 NULL,
        [PurchaseReceiptPath] nvarchar(500) NOT NULL,
        [DevicePurchaseCurrency] nvarchar(3) NOT NULL,
        [DevicePurchaseAmount] decimal(18,2) NOT NULL,
        [Organization] nvarchar(200) NOT NULL,
        [UmojaBankName] nvarchar(200) NOT NULL,
        [Supervisor] nvarchar(200) NOT NULL,
        [Remarks] nvarchar(200) NULL,
        [RequestDate] datetime2 NOT NULL,
        [RequestedBy] nvarchar(450) NOT NULL,
        [Status] int NOT NULL,
        [SubmittedToSupervisor] bit NOT NULL,
        [SupervisorApprovalDate] datetime2 NULL,
        [SupervisorNotes] nvarchar(500) NULL,
        [SupervisorRemarks] nvarchar(200) NULL,
        [SupervisorName] nvarchar(300) NULL,
        [SupervisorEmail] nvarchar(300) NULL,
        [BudgetOfficerApprovalDate] datetime2 NULL,
        [BudgetOfficerNotes] nvarchar(500) NULL,
        [BudgetOfficerRemarks] nvarchar(200) NULL,
        [BudgetOfficerName] nvarchar(300) NULL,
        [BudgetOfficerEmail] nvarchar(300) NULL,
        [CostObject] nvarchar(100) NULL,
        [CostCenter] nvarchar(100) NULL,
        [FundCommitment] nvarchar(100) NULL,
        [StaffClaimsApprovalDate] datetime2 NULL,
        [StaffClaimsNotes] nvarchar(500) NULL,
        [StaffClaimsRemarks] nvarchar(200) NULL,
        [StaffClaimsOfficerName] nvarchar(300) NULL,
        [StaffClaimsOfficerEmail] nvarchar(300) NULL,
        [UmojaPaymentDocumentId] nvarchar(100) NULL,
        [RefundUsdAmount] decimal(18,2) NULL,
        [ClaimsActionDate] datetime2 NULL,
        [PaymentApprovalDate] datetime2 NULL,
        [PaymentApprovalNotes] nvarchar(500) NULL,
        [PaymentApprovalRemarks] nvarchar(200) NULL,
        [PaymentApproverName] nvarchar(300) NULL,
        [PaymentApproverEmail] nvarchar(300) NULL,
        [CancellationDate] datetime2 NULL,
        [CancellationReason] nvarchar(500) NULL,
        [CancelledBy] nvarchar(300) NULL,
        [CompletionDate] datetime2 NULL,
        [PaymentReference] nvarchar(100) NULL,
        [CompletionNotes] nvarchar(500) NULL,
        [ProcessedBy] nvarchar(450) NULL,
        CONSTRAINT [PK_RefundRequests] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002163017_AddCallLogVerificationSystemTables'
)
BEGIN
    CREATE TABLE [ServiceProviders] (
        [Id] int NOT NULL IDENTITY,
        [SPID] nvarchar(10) NOT NULL,
        [ServiceProviderName] nvarchar(200) NOT NULL,
        [SPMainCP] nvarchar(200) NOT NULL,
        [SPMainCPEmail] nvarchar(300) NOT NULL,
        [SPOtherCPsEmail] nvarchar(1000) NULL,
        [SPStatus] int NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_ServiceProviders] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002163017_AddCallLogVerificationSystemTables'
)
BEGIN
    CREATE TABLE [AspNetRoleClaims] (
        [Id] int NOT NULL IDENTITY,
        [RoleId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002163017_AddCallLogVerificationSystemTables'
)
BEGIN
    CREATE TABLE [CallLogReconciliations] (
        [Id] int NOT NULL IDENTITY,
        [BillingPeriodId] int NOT NULL,
        [SourceRecordId] int NOT NULL,
        [SourceTable] nvarchar(50) NOT NULL,
        [Version] int NOT NULL,
        [ImportType] nvarchar(20) NOT NULL,
        [ImportBatchId] uniqueidentifier NOT NULL,
        [ImportDate] datetime2 NOT NULL,
        [PreviousAmount] decimal(18,2) NULL,
        [CurrentAmount] decimal(18,2) NOT NULL,
        [AdjustmentReason] nvarchar(500) NULL,
        [IsSuperseded] bit NOT NULL,
        [SupersededBy] int NULL,
        [SupersededDate] datetime2 NULL,
        CONSTRAINT [PK_CallLogReconciliations] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CallLogReconciliations_BillingPeriods_BillingPeriodId] FOREIGN KEY ([BillingPeriodId]) REFERENCES [BillingPeriods] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_CallLogReconciliations_CallLogReconciliations_SupersededBy] FOREIGN KEY ([SupersededBy]) REFERENCES [CallLogReconciliations] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002163017_AddCallLogVerificationSystemTables'
)
BEGIN
    CREATE TABLE [StagingBatches] (
        [Id] uniqueidentifier NOT NULL,
        [BatchName] nvarchar(100) NOT NULL,
        [BatchType] nvarchar(50) NOT NULL,
        [TotalRecords] int NOT NULL,
        [VerifiedRecords] int NOT NULL,
        [RejectedRecords] int NOT NULL,
        [PendingRecords] int NOT NULL,
        [RecordsWithAnomalies] int NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [StartProcessingDate] datetime2 NULL,
        [EndProcessingDate] datetime2 NULL,
        [BatchStatus] int NOT NULL,
        [CreatedBy] nvarchar(100) NOT NULL,
        [VerifiedBy] nvarchar(100) NULL,
        [PublishedBy] nvarchar(100) NULL,
        [SourceSystems] nvarchar(200) NULL,
        [Notes] nvarchar(max) NULL,
        [BillingPeriodId] int NULL,
        [BatchCategory] nvarchar(20) NOT NULL,
        CONSTRAINT [PK_StagingBatches] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_StagingBatches_BillingPeriods_BillingPeriodId] FOREIGN KEY ([BillingPeriodId]) REFERENCES [BillingPeriods] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002163017_AddCallLogVerificationSystemTables'
)
BEGIN
    CREATE TABLE [Offices] (
        [Id] int NOT NULL IDENTITY,
        [PublicId] uniqueidentifier NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [Code] nvarchar(10) NULL,
        [Description] nvarchar(500) NULL,
        [OrganizationId] int NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_Offices] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Offices_Organizations_OrganizationId] FOREIGN KEY ([OrganizationId]) REFERENCES [Organizations] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002163017_AddCallLogVerificationSystemTables'
)
BEGIN
    CREATE TABLE [Ebills] (
        [Id] int NOT NULL IDENTITY,
        [FullName] nvarchar(100) NOT NULL,
        [Email] nvarchar(300) NOT NULL,
        [PhoneNumber] nvarchar(20) NOT NULL,
        [Department] nvarchar(100) NOT NULL,
        [ServiceProviderId] int NOT NULL,
        [AccountNumber] nvarchar(50) NOT NULL,
        [BillMonth] datetime2 NOT NULL,
        [BillAmount] decimal(18,2) NOT NULL,
        [DueDate] datetime2 NOT NULL,
        [BillType] int NOT NULL,
        [Description] nvarchar(500) NULL,
        [AdditionalNotes] nvarchar(500) NULL,
        [Supervisor] nvarchar(200) NOT NULL,
        [Status] int NOT NULL,
        [RequestDate] datetime2 NOT NULL,
        [ProcessedDate] datetime2 NULL,
        [RequestedBy] nvarchar(450) NOT NULL,
        [ProcessedBy] nvarchar(450) NULL,
        [ProcessingNotes] nvarchar(500) NULL,
        [PaymentDate] datetime2 NULL,
        [PaidAmount] decimal(18,2) NULL,
        [SubmittedToSupervisor] bit NOT NULL,
        [SupervisorApprovalDate] datetime2 NULL,
        [SupervisorNotes] nvarchar(500) NULL,
        [SupervisorRemarks] nvarchar(200) NULL,
        [SupervisorName] nvarchar(300) NULL,
        [SupervisorEmail] nvarchar(300) NULL,
        CONSTRAINT [PK_Ebills] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Ebills_ServiceProviders_ServiceProviderId] FOREIGN KEY ([ServiceProviderId]) REFERENCES [ServiceProviders] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002163017_AddCallLogVerificationSystemTables'
)
BEGIN
    CREATE TABLE [SimRequests] (
        [Id] int NOT NULL IDENTITY,
        [IndexNo] nvarchar(20) NOT NULL,
        [FirstName] nvarchar(100) NOT NULL,
        [LastName] nvarchar(100) NOT NULL,
        [Organization] nvarchar(200) NOT NULL,
        [Office] nvarchar(200) NOT NULL,
        [Grade] nvarchar(50) NOT NULL,
        [FunctionalTitle] nvarchar(300) NOT NULL,
        [OfficeExtension] nvarchar(20) NULL,
        [OfficialEmail] nvarchar(300) NOT NULL,
        [SimType] int NOT NULL,
        [ServiceProviderId] int NOT NULL,
        [Supervisor] nvarchar(200) NOT NULL,
        [PreviouslyAssignedLines] nvarchar(1000) NULL,
        [Remarks] nvarchar(500) NULL,
        [Status] int NOT NULL,
        [RequestDate] datetime2 NOT NULL,
        [ProcessedDate] datetime2 NULL,
        [RequestedBy] nvarchar(450) NOT NULL,
        [ProcessedBy] nvarchar(450) NULL,
        [ProcessingNotes] nvarchar(500) NULL,
        [SubmittedToSupervisor] bit NOT NULL,
        [SupervisorApprovalDate] datetime2 NULL,
        [SupervisorNotes] nvarchar(500) NULL,
        [MobileService] nvarchar(100) NULL,
        [MobileServiceAllowance] nvarchar(100) NULL,
        [HandsetAllowance] nvarchar(100) NULL,
        [SupervisorRemarks] nvarchar(200) NULL,
        [SupervisorName] nvarchar(300) NULL,
        [SupervisorEmail] nvarchar(300) NULL,
        [SimSerialNo] nvarchar(50) NULL,
        [ServiceRequestNo] nvarchar(50) NULL,
        [LineType] nvarchar(20) NULL,
        [SimPuk] nvarchar(20) NULL,
        [LineUsage] nvarchar(20) NULL,
        [PreviousLines] nvarchar(500) NULL,
        [SpNotifiedDate] datetime2 NULL,
        [AssignedNo] nvarchar(50) NULL,
        [CollectionNotifiedDate] datetime2 NULL,
        [SimIssuedBy] nvarchar(100) NULL,
        [SimCollectedBy] nvarchar(100) NULL,
        [SimCollectedDate] datetime2 NULL,
        [IctsRemark] nvarchar(200) NULL,
        CONSTRAINT [PK_SimRequests] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SimRequests_ServiceProviders_ServiceProviderId] FOREIGN KEY ([ServiceProviderId]) REFERENCES [ServiceProviders] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002163017_AddCallLogVerificationSystemTables'
)
BEGIN
    CREATE TABLE [InterimUpdates] (
        [Id] int NOT NULL IDENTITY,
        [BillingPeriodId] int NOT NULL,
        [UpdateType] nvarchar(50) NOT NULL,
        [BatchId] uniqueidentifier NOT NULL,
        [RecordsAdded] int NOT NULL,
        [RecordsModified] int NOT NULL,
        [RecordsDeleted] int NOT NULL,
        [NetAdjustmentAmount] decimal(18,2) NOT NULL,
        [RequestedBy] nvarchar(100) NOT NULL,
        [RequestedDate] datetime2 NOT NULL,
        [ApprovedBy] nvarchar(100) NULL,
        [ApprovalDate] datetime2 NULL,
        [ApprovalStatus] nvarchar(20) NOT NULL,
        [RejectionReason] nvarchar(500) NULL,
        [Justification] nvarchar(max) NOT NULL,
        [SupportingDocuments] nvarchar(max) NULL,
        [ProcessedDate] datetime2 NULL,
        [ProcessingNotes] nvarchar(max) NULL,
        CONSTRAINT [PK_InterimUpdates] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_InterimUpdates_BillingPeriods_BillingPeriodId] FOREIGN KEY ([BillingPeriodId]) REFERENCES [BillingPeriods] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_InterimUpdates_StagingBatches_BatchId] FOREIGN KEY ([BatchId]) REFERENCES [StagingBatches] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002163017_AddCallLogVerificationSystemTables'
)
BEGIN
    CREATE TABLE [SubOffices] (
        [Id] int NOT NULL IDENTITY,
        [PublicId] uniqueidentifier NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [Code] nvarchar(10) NULL,
        [Description] nvarchar(500) NULL,
        [ContactPerson] nvarchar(100) NULL,
        [PhoneNumber] nvarchar(20) NULL,
        [Email] nvarchar(100) NULL,
        [Address] nvarchar(200) NULL,
        [OfficeId] int NOT NULL,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        [CreatedDate] datetime2 NOT NULL,
        [ModifiedDate] datetime2 NULL,
        CONSTRAINT [PK_SubOffices] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SubOffices_Offices_OfficeId] FOREIGN KEY ([OfficeId]) REFERENCES [Offices] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002163017_AddCallLogVerificationSystemTables'
)
BEGIN
    CREATE TABLE [SimRequestHistories] (
        [Id] int NOT NULL IDENTITY,
        [SimRequestId] int NOT NULL,
        [Action] nvarchar(100) NOT NULL,
        [PreviousStatus] nvarchar(50) NULL,
        [NewStatus] nvarchar(50) NULL,
        [Comments] nvarchar(1000) NULL,
        [PerformedBy] nvarchar(450) NOT NULL,
        [UserName] nvarchar(200) NOT NULL,
        [Timestamp] datetime2 NOT NULL,
        [IpAddress] nvarchar(50) NULL,
        [SimRequestId1] int NULL,
        CONSTRAINT [PK_SimRequestHistories] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SimRequestHistories_SimRequests_SimRequestId] FOREIGN KEY ([SimRequestId]) REFERENCES [SimRequests] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_SimRequestHistories_SimRequests_SimRequestId1] FOREIGN KEY ([SimRequestId1]) REFERENCES [SimRequests] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002163017_AddCallLogVerificationSystemTables'
)
BEGIN
    CREATE TABLE [EbillUsers] (
        [Id] int NOT NULL IDENTITY,
        [PublicId] uniqueidentifier NOT NULL,
        [FirstName] nvarchar(100) NOT NULL,
        [LastName] nvarchar(100) NOT NULL,
        [IndexNumber] nvarchar(50) NOT NULL,
        [OfficialMobileNumber] nvarchar(20) NOT NULL,
        [IssuedDeviceID] nvarchar(100) NULL,
        [Email] nvarchar(256) NOT NULL,
        [Location] nvarchar(200) NULL,
        [OrganizationId] int NULL,
        [OfficeId] int NULL,
        [SubOfficeId] int NULL,
        [IsActive] bit NOT NULL,
        [SupervisorIndexNumber] nvarchar(50) NULL,
        [SupervisorName] nvarchar(200) NULL,
        [SupervisorEmail] nvarchar(256) NULL,
        [CreatedDate] datetime2 NOT NULL,
        [LastModifiedDate] datetime2 NULL,
        CONSTRAINT [PK_EbillUsers] PRIMARY KEY ([Id]),
        CONSTRAINT [AK_EbillUsers_IndexNumber] UNIQUE ([IndexNumber]),
        CONSTRAINT [FK_EbillUsers_Offices_OfficeId] FOREIGN KEY ([OfficeId]) REFERENCES [Offices] ([Id]),
        CONSTRAINT [FK_EbillUsers_Organizations_OrganizationId] FOREIGN KEY ([OrganizationId]) REFERENCES [Organizations] ([Id]),
        CONSTRAINT [FK_EbillUsers_SubOffices_SubOfficeId] FOREIGN KEY ([SubOfficeId]) REFERENCES [SubOffices] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002163017_AddCallLogVerificationSystemTables'
)
BEGIN
    CREATE TABLE [AspNetUsers] (
        [Id] nvarchar(450) NOT NULL,
        [FirstName] nvarchar(max) NULL,
        [LastName] nvarchar(max) NULL,
        [RequirePasswordChange] bit NOT NULL,
        [Status] int NOT NULL,
        [AzureAdObjectId] nvarchar(100) NULL,
        [AzureAdTenantId] nvarchar(100) NULL,
        [AzureAdUpn] nvarchar(200) NULL,
        [EbillUserId] int NULL,
        [OrganizationId] int NULL,
        [OfficeId] int NULL,
        [SubOfficeId] int NULL,
        [UserName] nvarchar(256) NULL,
        [NormalizedUserName] nvarchar(256) NULL,
        [Email] nvarchar(256) NULL,
        [NormalizedEmail] nvarchar(256) NULL,
        [EmailConfirmed] bit NOT NULL,
        [PasswordHash] nvarchar(max) NULL,
        [SecurityStamp] nvarchar(max) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        [PhoneNumber] nvarchar(max) NULL,
        [PhoneNumberConfirmed] bit NOT NULL,
        [TwoFactorEnabled] bit NOT NULL,
        [LockoutEnd] datetimeoffset NULL,
        [LockoutEnabled] bit NOT NULL,
        [AccessFailedCount] int NOT NULL,
        CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetUsers_EbillUsers_EbillUserId] FOREIGN KEY ([EbillUserId]) REFERENCES [EbillUsers] ([Id]),
        CONSTRAINT [FK_AspNetUsers_Offices_OfficeId] FOREIGN KEY ([OfficeId]) REFERENCES [Offices] ([Id]),
        CONSTRAINT [FK_AspNetUsers_Organizations_OrganizationId] FOREIGN KEY ([OrganizationId]) REFERENCES [Organizations] ([Id]),
        CONSTRAINT [FK_AspNetUsers_SubOffices_SubOfficeId] FOREIGN KEY ([SubOfficeId]) REFERENCES [SubOffices] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002163017_AddCallLogVerificationSystemTables'
)
BEGIN
    CREATE TABLE [CallRecords] (
        [Id] int NOT NULL IDENTITY,
        [ext_no] nvarchar(50) NOT NULL,
        [call_date] datetime2 NOT NULL,
        [call_number] nvarchar(50) NOT NULL,
        [call_destination] nvarchar(100) NOT NULL,
        [call_endtime] datetime2 NOT NULL,
        [call_duration] int NOT NULL,
        [call_curr_code] nvarchar(10) NOT NULL,
        [call_cost] decimal(18,4) NOT NULL,
        [call_cost_usd] decimal(18,4) NOT NULL,
        [call_cost_kshs] decimal(18,4) NOT NULL,
        [call_type] nvarchar(50) NOT NULL,
        [call_dest_type] nvarchar(50) NOT NULL,
        [call_year] int NOT NULL,
        [call_month] int NOT NULL,
        [ext_resp_index] nvarchar(50) NULL,
        [call_pay_index] nvarchar(50) NULL,
        [call_ver_ind] bit NOT NULL,
        [call_ver_date] datetime2 NULL,
        [verification_period] datetime2 NULL,
        [verification_type] nvarchar(20) NULL,
        [payment_assignment_id] int NULL,
        [overage_justified] bit NOT NULL,
        [supervisor_approval_status] nvarchar(20) NULL,
        [supervisor_approved_by] nvarchar(50) NULL,
        [supervisor_approved_date] datetime2 NULL,
        [call_cert_ind] bit NOT NULL,
        [call_cert_date] datetime2 NULL,
        [call_cert_by] nvarchar(100) NULL,
        [call_proc_ind] bit NOT NULL,
        [entry_date] datetime2 NOT NULL,
        [call_dest_descr] nvarchar(200) NULL,
        [SourceSystem] nvarchar(50) NULL,
        [SourceBatchId] uniqueidentifier NULL,
        [SourceStagingId] int NULL,
        CONSTRAINT [PK_CallRecords] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CallRecords_EbillUsers_call_pay_index] FOREIGN KEY ([call_pay_index]) REFERENCES [EbillUsers] ([IndexNumber]),
        CONSTRAINT [FK_CallRecords_EbillUsers_ext_resp_index] FOREIGN KEY ([ext_resp_index]) REFERENCES [EbillUsers] ([IndexNumber])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002163017_AddCallLogVerificationSystemTables'
)
BEGIN
    CREATE TABLE [UserPhones] (
        [Id] int NOT NULL IDENTITY,
        [PublicId] uniqueidentifier NOT NULL,
        [IndexNumber] nvarchar(50) NOT NULL,
        [PhoneNumber] nvarchar(20) NOT NULL,
        [PhoneType] nvarchar(50) NOT NULL,
        [IsPrimary] bit NOT NULL,
        [IsActive] bit NOT NULL,
        [AssignedDate] datetime2 NOT NULL,
        [UnassignedDate] datetime2 NULL,
        [Location] nvarchar(200) NULL,
        [Notes] nvarchar(500) NULL,
        [CreatedBy] nvarchar(100) NULL,
        [CreatedDate] datetime2 NOT NULL,
        [ClassOfServiceId] int NULL,
        CONSTRAINT [PK_UserPhones] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UserPhones_ClassOfServices_ClassOfServiceId] FOREIGN KEY ([ClassOfServiceId]) REFERENCES [ClassOfServices] ([Id]),
        CONSTRAINT [FK_UserPhones_EbillUsers_IndexNumber] FOREIGN KEY ([IndexNumber]) REFERENCES [EbillUsers] ([IndexNumber]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002163017_AddCallLogVerificationSystemTables'
)
BEGIN
    CREATE TABLE [AspNetUserClaims] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002163017_AddCallLogVerificationSystemTables'
)
BEGIN
    CREATE TABLE [AspNetUserLogins] (
        [LoginProvider] nvarchar(450) NOT NULL,
        [ProviderKey] nvarchar(450) NOT NULL,
        [ProviderDisplayName] nvarchar(max) NULL,
        [UserId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
        CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002163017_AddCallLogVerificationSystemTables'
)
BEGIN
    CREATE TABLE [AspNetUserRoles] (
        [UserId] nvarchar(450) NOT NULL,
        [RoleId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
        CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002163017_AddCallLogVerificationSystemTables'
)
BEGIN
    CREATE TABLE [AspNetUserTokens] (
        [UserId] nvarchar(450) NOT NULL,
        [LoginProvider] nvarchar(450) NOT NULL,
        [Name] nvarchar(450) NOT NULL,
        [Value] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
        CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002163017_AddCallLogVerificationSystemTables'
)
BEGIN
    CREATE TABLE [CallLogPaymentAssignments] (
        [Id] int NOT NULL IDENTITY,
        [CallRecordId] int NOT NULL,
        [AssignedFrom] nvarchar(50) NOT NULL,
        [AssignedTo] nvarchar(50) NOT NULL,
        [AssignmentReason] nvarchar(500) NOT NULL,
        [AssignedDate] datetime2 NOT NULL,
        [AssignmentStatus] nvarchar(20) NOT NULL,
        [AcceptedDate] datetime2 NULL,
        [RejectionReason] nvarchar(500) NULL,
        [NotificationSent] bit NOT NULL,
        [NotificationSentDate] datetime2 NULL,
        [NotificationViewedDate] datetime2 NULL,
        [CreatedDate] datetime2 NOT NULL,
        [ModifiedDate] datetime2 NULL,
        CONSTRAINT [PK_CallLogPaymentAssignments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CallLogPaymentAssignments_CallRecords_CallRecordId] FOREIGN KEY ([CallRecordId]) REFERENCES [CallRecords] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002163017_AddCallLogVerificationSystemTables'
)
BEGIN
    CREATE TABLE [CallLogVerifications] (
        [Id] int NOT NULL IDENTITY,
        [CallRecordId] int NOT NULL,
        [VerifiedBy] nvarchar(50) NOT NULL,
        [VerifiedDate] datetime2 NOT NULL,
        [VerificationType] int NOT NULL,
        [ClassOfServiceId] int NULL,
        [AllowanceAmount] decimal(18,4) NULL,
        [ActualAmount] decimal(18,4) NOT NULL,
        [IsOverage] bit NOT NULL,
        [JustificationText] nvarchar(max) NULL,
        [SupportingDocuments] nvarchar(max) NULL,
        [ApprovalStatus] nvarchar(20) NOT NULL,
        [SubmittedToSupervisor] bit NOT NULL,
        [SubmittedDate] datetime2 NULL,
        [SupervisorIndexNumber] nvarchar(50) NULL,
        [SupervisorAction] nvarchar(20) NULL,
        [SupervisorActionDate] datetime2 NULL,
        [SupervisorComments] nvarchar(500) NULL,
        [ApprovedAmount] decimal(18,4) NULL,
        [RejectionReason] nvarchar(500) NULL,
        [CreatedDate] datetime2 NOT NULL,
        [ModifiedDate] datetime2 NULL,
        CONSTRAINT [PK_CallLogVerifications] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CallLogVerifications_CallRecords_CallRecordId] FOREIGN KEY ([CallRecordId]) REFERENCES [CallRecords] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_CallLogVerifications_ClassOfServices_ClassOfServiceId] FOREIGN KEY ([ClassOfServiceId]) REFERENCES [ClassOfServices] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002163017_AddCallLogVerificationSystemTables'
)
BEGIN
    CREATE TABLE [Airtel] (
        [Id] int NOT NULL IDENTITY,
        [Ext] nvarchar(50) NULL,
        [call_date] datetime2 NULL,
        [call_time] time NULL,
        [Dialed] nvarchar(100) NULL,
        [Dest] nvarchar(200) NULL,
        [Durx] decimal(18,2) NULL,
        [Cost] decimal(18,2) NULL,
        [AmountUSD] decimal(18,4) NULL,
        [Dur] decimal(18,2) NULL,
        [call_type] nvarchar(50) NULL,
        [call_month] int NULL,
        [call_year] int NULL,
        [IndexNumber] nvarchar(50) NULL,
        [UserPhoneId] int NULL,
        [CreatedDate] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedDate] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [EbillUserId] int NULL,
        [ImportAuditId] int NULL,
        [ProcessingStatus] int NOT NULL,
        [ProcessedDate] datetime2 NULL,
        [StagingBatchId] uniqueidentifier NULL,
        [BillingPeriod] nvarchar(20) NULL,
        CONSTRAINT [PK_Airtel] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Airtel_EbillUsers_EbillUserId] FOREIGN KEY ([EbillUserId]) REFERENCES [EbillUsers] ([Id]),
        CONSTRAINT [FK_Airtel_ImportAudits_ImportAuditId] FOREIGN KEY ([ImportAuditId]) REFERENCES [ImportAudits] ([Id]),
        CONSTRAINT [FK_Airtel_UserPhones_UserPhoneId] FOREIGN KEY ([UserPhoneId]) REFERENCES [UserPhones] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002163017_AddCallLogVerificationSystemTables'
)
BEGIN
    CREATE TABLE [CallLogs] (
        [Id] int NOT NULL IDENTITY,
        [AccountNo] nvarchar(20) NOT NULL,
        [SubAccountNo] nvarchar(50) NOT NULL,
        [SubAccountName] nvarchar(200) NOT NULL,
        [MSISDN] nvarchar(20) NOT NULL,
        [TaxInvoiceSummaryNo] nvarchar(50) NOT NULL,
        [InvoiceNo] nvarchar(50) NOT NULL,
        [InvoiceDate] datetime2 NOT NULL,
        [NetAccessFee] decimal(18,2) NOT NULL,
        [NetUsageLessTax] decimal(18,2) NOT NULL,
        [LessTaxes] decimal(18,2) NOT NULL,
        [VAT16] decimal(18,2) NULL,
        [Excise15] decimal(18,2) NULL,
        [GrossTotal] decimal(18,2) NOT NULL,
        [EbillUserId] int NULL,
        [UserPhoneId] int NULL,
        [CreatedDate] datetime2 NOT NULL,
        [ImportedBy] nvarchar(max) NULL,
        [ImportedDate] datetime2 NULL,
        CONSTRAINT [PK_CallLogs] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CallLogs_EbillUsers_EbillUserId] FOREIGN KEY ([EbillUserId]) REFERENCES [EbillUsers] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_CallLogs_UserPhones_UserPhoneId] FOREIGN KEY ([UserPhoneId]) REFERENCES [UserPhones] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002163017_AddCallLogVerificationSystemTables'
)
BEGIN
    CREATE TABLE [CallLogStagings] (
        [Id] int NOT NULL IDENTITY,
        [ExtensionNumber] nvarchar(50) NOT NULL,
        [CallDate] datetime2 NOT NULL,
        [CallNumber] nvarchar(50) NOT NULL,
        [CallDestination] nvarchar(100) NOT NULL,
        [CallEndTime] datetime2 NOT NULL,
        [CallDuration] int NOT NULL,
        [CallCurrencyCode] nvarchar(10) NOT NULL,
        [CallCost] decimal(18,4) NOT NULL,
        [CallCostUSD] decimal(18,4) NOT NULL,
        [CallCostKSHS] decimal(18,4) NOT NULL,
        [CallType] nvarchar(50) NOT NULL,
        [CallDestinationType] nvarchar(50) NOT NULL,
        [CallYear] int NOT NULL,
        [CallMonth] int NOT NULL,
        [ResponsibleIndexNumber] nvarchar(50) NULL,
        [PayingIndexNumber] nvarchar(50) NULL,
        [UserPhoneId] int NULL,
        [BillingPeriodId] int NULL,
        [ImportType] nvarchar(20) NOT NULL,
        [IsAdjustment] bit NOT NULL,
        [OriginalRecordId] int NULL,
        [AdjustmentReason] nvarchar(500) NULL,
        [SourceSystem] nvarchar(50) NOT NULL,
        [SourceRecordId] nvarchar(100) NULL,
        [BatchId] uniqueidentifier NOT NULL,
        [ImportedDate] datetime2 NOT NULL,
        [ImportedBy] nvarchar(100) NOT NULL,
        [VerificationStatus] int NOT NULL,
        [VerificationDate] datetime2 NULL,
        [VerifiedBy] nvarchar(100) NULL,
        [VerificationNotes] nvarchar(max) NULL,
        [HasAnomalies] bit NOT NULL,
        [AnomalyTypes] nvarchar(max) NULL,
        [AnomalyDetails] nvarchar(max) NULL,
        [ProcessingStatus] int NOT NULL,
        [ProcessedDate] datetime2 NULL,
        [ErrorDetails] nvarchar(max) NULL,
        [CreatedDate] datetime2 NOT NULL,
        [ModifiedDate] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_CallLogStagings] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CallLogStagings_BillingPeriods_BillingPeriodId] FOREIGN KEY ([BillingPeriodId]) REFERENCES [BillingPeriods] ([Id]),
        CONSTRAINT [FK_CallLogStagings_EbillUsers_PayingIndexNumber] FOREIGN KEY ([PayingIndexNumber]) REFERENCES [EbillUsers] ([IndexNumber]),
        CONSTRAINT [FK_CallLogStagings_EbillUsers_ResponsibleIndexNumber] FOREIGN KEY ([ResponsibleIndexNumber]) REFERENCES [EbillUsers] ([IndexNumber]),
        CONSTRAINT [FK_CallLogStagings_StagingBatches_BatchId] FOREIGN KEY ([BatchId]) REFERENCES [StagingBatches] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_CallLogStagings_UserPhones_UserPhoneId] FOREIGN KEY ([UserPhoneId]) REFERENCES [UserPhones] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002163017_AddCallLogVerificationSystemTables'
)
BEGIN
    CREATE TABLE [PrivateWires] (
        [Id] int NOT NULL IDENTITY,
        [Extension] nvarchar(50) NULL,
        [DestinationLine] nvarchar(50) NULL,
        [DurationExtended] decimal(18,2) NULL,
        [DialedNumber] nvarchar(100) NULL,
        [CallTime] time NULL,
        [Destination] nvarchar(200) NULL,
        [AmountUSD] decimal(18,4) NULL,
        [AmountKSH] decimal(18,4) NULL,
        [CallDate] datetime2 NULL,
        [CallMonth] int NOT NULL,
        [CallYear] int NOT NULL,
        [Duration] decimal(18,2) NULL,
        [IndexNumber] nvarchar(50) NULL,
        [UserPhoneId] int NULL,
        [CreatedDate] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedDate] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [EbillUserId] int NULL,
        [ImportAuditId] int NULL,
        [ProcessingStatus] int NOT NULL,
        [ProcessedDate] datetime2 NULL,
        [StagingBatchId] uniqueidentifier NULL,
        [BillingPeriod] nvarchar(20) NULL,
        CONSTRAINT [PK_PrivateWires] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PrivateWires_EbillUsers_EbillUserId] FOREIGN KEY ([EbillUserId]) REFERENCES [EbillUsers] ([Id]),
        CONSTRAINT [FK_PrivateWires_ImportAudits_ImportAuditId] FOREIGN KEY ([ImportAuditId]) REFERENCES [ImportAudits] ([Id]),
        CONSTRAINT [FK_PrivateWires_UserPhones_UserPhoneId] FOREIGN KEY ([UserPhoneId]) REFERENCES [UserPhones] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002163017_AddCallLogVerificationSystemTables'
)
BEGIN
    CREATE TABLE [PSTNs] (
        [Id] int NOT NULL IDENTITY,
        [Extension] nvarchar(50) NULL,
        [DialedNumber] nvarchar(100) NULL,
        [CallTime] time NULL,
        [Destination] nvarchar(200) NULL,
        [DestinationLine] nvarchar(50) NULL,
        [DurationExtended] decimal(18,2) NULL,
        [Duration] decimal(18,2) NULL,
        [CallDate] datetime2 NULL,
        [CallMonth] int NOT NULL,
        [CallYear] int NOT NULL,
        [AmountKSH] decimal(18,2) NULL,
        [AmountUSD] decimal(18,4) NULL,
        [IndexNumber] nvarchar(50) NULL,
        [UserPhoneId] int NULL,
        [Carrier] nvarchar(50) NULL,
        [CreatedDate] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedDate] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [EbillUserId] int NULL,
        [ImportAuditId] int NULL,
        [ProcessingStatus] int NOT NULL,
        [ProcessedDate] datetime2 NULL,
        [StagingBatchId] uniqueidentifier NULL,
        [BillingPeriod] nvarchar(20) NULL,
        CONSTRAINT [PK_PSTNs] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PSTNs_EbillUsers_EbillUserId] FOREIGN KEY ([EbillUserId]) REFERENCES [EbillUsers] ([Id]),
        CONSTRAINT [FK_PSTNs_ImportAudits_ImportAuditId] FOREIGN KEY ([ImportAuditId]) REFERENCES [ImportAudits] ([Id]),
        CONSTRAINT [FK_PSTNs_UserPhones_UserPhoneId] FOREIGN KEY ([UserPhoneId]) REFERENCES [UserPhones] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002163017_AddCallLogVerificationSystemTables'
)
BEGIN
    CREATE TABLE [Safaricom] (
        [Id] int NOT NULL IDENTITY,
        [Ext] nvarchar(50) NULL,
        [call_date] datetime2 NULL,
        [call_time] time NULL,
        [Dialed] nvarchar(100) NULL,
        [Dest] nvarchar(200) NULL,
        [Durx] decimal(18,2) NULL,
        [Cost] decimal(18,2) NULL,
        [AmountUSD] decimal(18,4) NULL,
        [Dur] decimal(18,2) NULL,
        [call_type] nvarchar(50) NULL,
        [call_month] int NULL,
        [call_year] int NULL,
        [IndexNumber] nvarchar(50) NULL,
        [UserPhoneId] int NULL,
        [CreatedDate] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedDate] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [EbillUserId] int NULL,
        [ImportAuditId] int NULL,
        [ProcessingStatus] int NOT NULL,
        [ProcessedDate] datetime2 NULL,
        [StagingBatchId] uniqueidentifier NULL,
        [BillingPeriod] nvarchar(20) NULL,
        CONSTRAINT [PK_Safaricom] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Safaricom_EbillUsers_EbillUserId] FOREIGN KEY ([EbillUserId]) REFERENCES [EbillUsers] ([Id]),
        CONSTRAINT [FK_Safaricom_ImportAudits_ImportAuditId] FOREIGN KEY ([ImportAuditId]) REFERENCES [ImportAudits] ([Id]),
        CONSTRAINT [FK_Safaricom_UserPhones_UserPhoneId] FOREIGN KEY ([UserPhoneId]) REFERENCES [UserPhones] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002163017_AddCallLogVerificationSystemTables'
)
BEGIN
    CREATE TABLE [CallLogDocuments] (
        [Id] int NOT NULL IDENTITY,
        [CallLogVerificationId] int NOT NULL,
        [FileName] nvarchar(255) NOT NULL,
        [FilePath] nvarchar(500) NOT NULL,
        [FileSize] bigint NOT NULL,
        [ContentType] nvarchar(100) NOT NULL,
        [DocumentType] nvarchar(50) NOT NULL,
        [Description] nvarchar(500) NULL,
        [UploadedBy] nvarchar(50) NOT NULL,
        [UploadedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_CallLogDocuments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CallLogDocuments_CallLogVerifications_CallLogVerificationId] FOREIGN KEY ([CallLogVerificationId]) REFERENCES [CallLogVerifications] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002163017_AddCallLogVerificationSystemTables'
)
BEGIN
    CREATE INDEX [IX_Airtel_EbillUserId] ON [Airtel] ([EbillUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002163017_AddCallLogVerificationSystemTables'
)
BEGIN
    CREATE INDEX [IX_Airtel_ImportAuditId] ON [Airtel] ([ImportAuditId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002163017_AddCallLogVerificationSystemTables'
)
BEGIN
    CREATE INDEX [IX_Airtel_UserPhoneId] ON [Airtel] ([UserPhoneId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002163017_AddCallLogVerificationSystemTables'
)
BEGIN
    CREATE UNIQUE INDEX [IX_AnomalyTypes_Code] ON [AnomalyTypes] ([Code]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002163017_AddCallLogVerificationSystemTables'
)
BEGIN
    CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002163017_AddCallLogVerificationSystemTables'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002163017_AddCallLogVerificationSystemTables'
)
BEGIN
    CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002163017_AddCallLogVerificationSystemTables'
)
BEGIN
    CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002163017_AddCallLogVerificationSystemTables'
)
BEGIN
    CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002163017_AddCallLogVerificationSystemTables'
)
BEGIN
    CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002163017_AddCallLogVerificationSystemTables'
)
BEGIN
    CREATE INDEX [IX_AspNetUsers_EbillUserId] ON [AspNetUsers] ([EbillUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002163017_AddCallLogVerificationSystemTables'
)
BEGIN
    CREATE INDEX [IX_AspNetUsers_OfficeId] ON [AspNetUsers] ([OfficeId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002163017_AddCallLogVerificationSystemTables'
)
BEGIN
    CREATE INDEX [IX_AspNetUsers_OrganizationId] ON [AspNetUsers] ([OrganizationId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002163017_AddCallLogVerificationSystemTables'
)
BEGIN
    CREATE INDEX [IX_AspNetUsers_SubOfficeId] ON [AspNetUsers] ([SubOfficeId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002163017_AddCallLogVerificationSystemTables'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002163017_AddCallLogVerificationSystemTables'
)
BEGIN
    CREATE INDEX [IX_CallLogDocuments_CallLogVerificationId] ON [CallLogDocuments] ([CallLogVerificationId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002163017_AddCallLogVerificationSystemTables'
)
BEGIN
    CREATE INDEX [IX_CallLogPaymentAssignments_AssignedFrom_AssignedTo] ON [CallLogPaymentAssignments] ([AssignedFrom], [AssignedTo]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002163017_AddCallLogVerificationSystemTables'
)
BEGIN
    CREATE INDEX [IX_CallLogPaymentAssignments_AssignedTo] ON [CallLogPaymentAssignments] ([AssignedTo]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002163017_AddCallLogVerificationSystemTables'
)
BEGIN
    CREATE INDEX [IX_CallLogPaymentAssignments_AssignmentStatus] ON [CallLogPaymentAssignments] ([AssignmentStatus]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002163017_AddCallLogVerificationSystemTables'
)
BEGIN
    CREATE INDEX [IX_CallLogPaymentAssignments_CallRecordId] ON [CallLogPaymentAssignments] ([CallRecordId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002163017_AddCallLogVerificationSystemTables'
)
BEGIN
    CREATE INDEX [IX_CallLogReconciliations_BillingPeriodId] ON [CallLogReconciliations] ([BillingPeriodId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002163017_AddCallLogVerificationSystemTables'
)
BEGIN
    CREATE INDEX [IX_CallLogReconciliations_SupersededBy] ON [CallLogReconciliations] ([SupersededBy]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002163017_AddCallLogVerificationSystemTables'
)
BEGIN
    CREATE INDEX [IX_CallLogs_EbillUserId] ON [CallLogs] ([EbillUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002163017_AddCallLogVerificationSystemTables'
)
BEGIN
    CREATE INDEX [IX_CallLogs_MSISDN] ON [CallLogs] ([MSISDN]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002163017_AddCallLogVerificationSystemTables'
)
BEGIN
    CREATE INDEX [IX_CallLogs_UserPhoneId] ON [CallLogs] ([UserPhoneId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002163017_AddCallLogVerificationSystemTables'
)
BEGIN
    CREATE INDEX [IX_CallLogStagings_BatchId] ON [CallLogStagings] ([BatchId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002163017_AddCallLogVerificationSystemTables'
)
BEGIN
    CREATE INDEX [IX_CallLogStagings_BillingPeriodId] ON [CallLogStagings] ([BillingPeriodId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002163017_AddCallLogVerificationSystemTables'
)
BEGIN
    CREATE INDEX [IX_CallLogStagings_CallDate] ON [CallLogStagings] ([CallDate]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002163017_AddCallLogVerificationSystemTables'
)
BEGIN
    CREATE INDEX [IX_CallLogStagings_ExtensionNumber_CallDate_CallNumber] ON [CallLogStagings] ([ExtensionNumber], [CallDate], [CallNumber]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002163017_AddCallLogVerificationSystemTables'
)
BEGIN
    CREATE INDEX [IX_CallLogStagings_PayingIndexNumber] ON [CallLogStagings] ([PayingIndexNumber]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002163017_AddCallLogVerificationSystemTables'
)
BEGIN
    CREATE INDEX [IX_CallLogStagings_ResponsibleIndexNumber] ON [CallLogStagings] ([ResponsibleIndexNumber]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002163017_AddCallLogVerificationSystemTables'
)
BEGIN
    CREATE INDEX [IX_CallLogStagings_UserPhoneId] ON [CallLogStagings] ([UserPhoneId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002163017_AddCallLogVerificationSystemTables'
)
BEGIN
    CREATE INDEX [IX_CallLogStagings_VerificationStatus] ON [CallLogStagings] ([VerificationStatus]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002163017_AddCallLogVerificationSystemTables'
)
BEGIN
    CREATE INDEX [IX_CallLogVerifications_ApprovalStatus] ON [CallLogVerifications] ([ApprovalStatus]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002163017_AddCallLogVerificationSystemTables'
)
BEGIN
    CREATE INDEX [IX_CallLogVerifications_CallRecordId_VerifiedBy] ON [CallLogVerifications] ([CallRecordId], [VerifiedBy]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002163017_AddCallLogVerificationSystemTables'
)
BEGIN
    CREATE INDEX [IX_CallLogVerifications_ClassOfServiceId] ON [CallLogVerifications] ([ClassOfServiceId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002163017_AddCallLogVerificationSystemTables'
)
BEGIN
    CREATE INDEX [IX_CallLogVerifications_SupervisorIndexNumber] ON [CallLogVerifications] ([SupervisorIndexNumber]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002163017_AddCallLogVerificationSystemTables'
)
BEGIN
    CREATE INDEX [IX_CallLogVerifications_VerifiedBy] ON [CallLogVerifications] ([VerifiedBy]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002163017_AddCallLogVerificationSystemTables'
)
BEGIN
    CREATE INDEX [IX_CallRecords_call_date] ON [CallRecords] ([call_date]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002163017_AddCallLogVerificationSystemTables'
)
BEGIN
    CREATE INDEX [IX_CallRecords_call_pay_index] ON [CallRecords] ([call_pay_index]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002163017_AddCallLogVerificationSystemTables'
)
BEGIN
    CREATE INDEX [IX_CallRecords_call_year_call_month] ON [CallRecords] ([call_year], [call_month]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002163017_AddCallLogVerificationSystemTables'
)
BEGIN
    CREATE INDEX [IX_CallRecords_ext_no] ON [CallRecords] ([ext_no]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002163017_AddCallLogVerificationSystemTables'
)
BEGIN
    CREATE INDEX [IX_CallRecords_ext_resp_index] ON [CallRecords] ([ext_resp_index]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002163017_AddCallLogVerificationSystemTables'
)
BEGIN
    CREATE INDEX [IX_Ebills_ServiceProviderId] ON [Ebills] ([ServiceProviderId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002163017_AddCallLogVerificationSystemTables'
)
BEGIN
    CREATE UNIQUE INDEX [IX_EbillUsers_Email] ON [EbillUsers] ([Email]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002163017_AddCallLogVerificationSystemTables'
)
BEGIN
    CREATE UNIQUE INDEX [IX_EbillUsers_IndexNumber] ON [EbillUsers] ([IndexNumber]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002163017_AddCallLogVerificationSystemTables'
)
BEGIN
    CREATE INDEX [IX_EbillUsers_OfficeId] ON [EbillUsers] ([OfficeId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002163017_AddCallLogVerificationSystemTables'
)
BEGIN
    CREATE INDEX [IX_EbillUsers_OrganizationId] ON [EbillUsers] ([OrganizationId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002163017_AddCallLogVerificationSystemTables'
)
BEGIN
    CREATE INDEX [IX_EbillUsers_SubOfficeId] ON [EbillUsers] ([SubOfficeId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002163017_AddCallLogVerificationSystemTables'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ExchangeRates_Month_Year] ON [ExchangeRates] ([Month], [Year]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002163017_AddCallLogVerificationSystemTables'
)
BEGIN
    CREATE INDEX [IX_ImportAudits_ImportDate] ON [ImportAudits] ([ImportDate]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002163017_AddCallLogVerificationSystemTables'
)
BEGIN
    CREATE INDEX [IX_ImportAudits_ImportType] ON [ImportAudits] ([ImportType]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002163017_AddCallLogVerificationSystemTables'
)
BEGIN
    CREATE INDEX [IX_InterimUpdates_BatchId] ON [InterimUpdates] ([BatchId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002163017_AddCallLogVerificationSystemTables'
)
BEGIN
    CREATE INDEX [IX_InterimUpdates_BillingPeriodId] ON [InterimUpdates] ([BillingPeriodId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002163017_AddCallLogVerificationSystemTables'
)
BEGIN
    CREATE INDEX [IX_Offices_OrganizationId] ON [Offices] ([OrganizationId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002163017_AddCallLogVerificationSystemTables'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Organizations_Name] ON [Organizations] ([Name]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002163017_AddCallLogVerificationSystemTables'
)
BEGIN
    CREATE INDEX [IX_PrivateWires_EbillUserId] ON [PrivateWires] ([EbillUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002163017_AddCallLogVerificationSystemTables'
)
BEGIN
    CREATE INDEX [IX_PrivateWires_ImportAuditId] ON [PrivateWires] ([ImportAuditId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002163017_AddCallLogVerificationSystemTables'
)
BEGIN
    CREATE INDEX [IX_PrivateWires_UserPhoneId] ON [PrivateWires] ([UserPhoneId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002163017_AddCallLogVerificationSystemTables'
)
BEGIN
    CREATE INDEX [IX_PSTNs_EbillUserId] ON [PSTNs] ([EbillUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002163017_AddCallLogVerificationSystemTables'
)
BEGIN
    CREATE INDEX [IX_PSTNs_ImportAuditId] ON [PSTNs] ([ImportAuditId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002163017_AddCallLogVerificationSystemTables'
)
BEGIN
    CREATE INDEX [IX_PSTNs_UserPhoneId] ON [PSTNs] ([UserPhoneId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002163017_AddCallLogVerificationSystemTables'
)
BEGIN
    CREATE INDEX [IX_Safaricom_EbillUserId] ON [Safaricom] ([EbillUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002163017_AddCallLogVerificationSystemTables'
)
BEGIN
    CREATE INDEX [IX_Safaricom_ImportAuditId] ON [Safaricom] ([ImportAuditId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002163017_AddCallLogVerificationSystemTables'
)
BEGIN
    CREATE INDEX [IX_Safaricom_UserPhoneId] ON [Safaricom] ([UserPhoneId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002163017_AddCallLogVerificationSystemTables'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ServiceProviders_SPID] ON [ServiceProviders] ([SPID]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002163017_AddCallLogVerificationSystemTables'
)
BEGIN
    CREATE INDEX [IX_SimRequestHistories_SimRequestId] ON [SimRequestHistories] ([SimRequestId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002163017_AddCallLogVerificationSystemTables'
)
BEGIN
    CREATE INDEX [IX_SimRequestHistories_SimRequestId1] ON [SimRequestHistories] ([SimRequestId1]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002163017_AddCallLogVerificationSystemTables'
)
BEGIN
    CREATE INDEX [IX_SimRequestHistories_Timestamp] ON [SimRequestHistories] ([Timestamp]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002163017_AddCallLogVerificationSystemTables'
)
BEGIN
    CREATE INDEX [IX_SimRequests_IndexNo] ON [SimRequests] ([IndexNo]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002163017_AddCallLogVerificationSystemTables'
)
BEGIN
    CREATE INDEX [IX_SimRequests_ServiceProviderId] ON [SimRequests] ([ServiceProviderId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002163017_AddCallLogVerificationSystemTables'
)
BEGIN
    CREATE INDEX [IX_StagingBatches_BatchStatus] ON [StagingBatches] ([BatchStatus]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002163017_AddCallLogVerificationSystemTables'
)
BEGIN
    CREATE INDEX [IX_StagingBatches_BillingPeriodId] ON [StagingBatches] ([BillingPeriodId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002163017_AddCallLogVerificationSystemTables'
)
BEGIN
    CREATE INDEX [IX_StagingBatches_CreatedDate] ON [StagingBatches] ([CreatedDate]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002163017_AddCallLogVerificationSystemTables'
)
BEGIN
    CREATE INDEX [IX_SubOffices_OfficeId] ON [SubOffices] ([OfficeId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002163017_AddCallLogVerificationSystemTables'
)
BEGIN
    CREATE INDEX [IX_UserPhones_ClassOfServiceId] ON [UserPhones] ([ClassOfServiceId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002163017_AddCallLogVerificationSystemTables'
)
BEGIN
    CREATE INDEX [IX_UserPhones_IndexNumber] ON [UserPhones] ([IndexNumber]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002163017_AddCallLogVerificationSystemTables'
)
BEGIN
    CREATE INDEX [IX_UserPhones_IndexNumber_PhoneNumber_IsActive] ON [UserPhones] ([IndexNumber], [PhoneNumber], [IsActive]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002163017_AddCallLogVerificationSystemTables'
)
BEGIN
    CREATE INDEX [IX_UserPhones_PhoneNumber] ON [UserPhones] ([PhoneNumber]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002163017_AddCallLogVerificationSystemTables'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251002163017_AddCallLogVerificationSystemTables', N'8.0.6');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002173558_AddUserPhoneRelationshipToCallRecords'
)
BEGIN
    ALTER TABLE [CallRecords] ADD [UserPhoneId] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002173558_AddUserPhoneRelationshipToCallRecords'
)
BEGIN
    CREATE INDEX [IX_CallRecords_UserPhoneId] ON [CallRecords] ([UserPhoneId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002173558_AddUserPhoneRelationshipToCallRecords'
)
BEGIN
    ALTER TABLE [CallRecords] ADD CONSTRAINT [FK_CallRecords_UserPhones_UserPhoneId] FOREIGN KEY ([UserPhoneId]) REFERENCES [UserPhones] ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251002173558_AddUserPhoneRelationshipToCallRecords'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251002173558_AddUserPhoneRelationshipToCallRecords', N'8.0.6');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251003180350_AddPhoneStatusToUserPhone'
)
BEGIN
    ALTER TABLE [SimRequestHistories] DROP CONSTRAINT [FK_SimRequestHistories_SimRequests_SimRequestId1];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251003180350_AddPhoneStatusToUserPhone'
)
BEGIN
    DROP INDEX [IX_SimRequestHistories_SimRequestId1] ON [SimRequestHistories];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251003180350_AddPhoneStatusToUserPhone'
)
BEGIN
    DECLARE @var11 sysname;
    SELECT @var11 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[SimRequestHistories]') AND [c].[name] = N'SimRequestId1');
    IF @var11 IS NOT NULL EXEC(N'ALTER TABLE [SimRequestHistories] DROP CONSTRAINT [' + @var11 + '];');
    ALTER TABLE [SimRequestHistories] DROP COLUMN [SimRequestId1];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251003180350_AddPhoneStatusToUserPhone'
)
BEGIN
    EXEC sp_rename N'[CallLogVerifications].[SupervisorActionDate]', N'SupervisorApprovedDate', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251003180350_AddPhoneStatusToUserPhone'
)
BEGIN
    EXEC sp_rename N'[CallLogVerifications].[SupervisorAction]', N'SupervisorApprovalStatus', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251003180350_AddPhoneStatusToUserPhone'
)
BEGIN
    ALTER TABLE [UserPhones] ADD [Status] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251003180350_AddPhoneStatusToUserPhone'
)
BEGIN
    ALTER TABLE [CallLogVerifications] ADD [OverageAmount] decimal(18,4) NOT NULL DEFAULT 0.0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251003180350_AddPhoneStatusToUserPhone'
)
BEGIN
    ALTER TABLE [CallLogVerifications] ADD [OverageJustified] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251003180350_AddPhoneStatusToUserPhone'
)
BEGIN
    ALTER TABLE [CallLogVerifications] ADD [PaymentAssignmentId] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251003180350_AddPhoneStatusToUserPhone'
)
BEGIN
    ALTER TABLE [CallLogVerifications] ADD [SupervisorApprovedBy] nvarchar(50) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251003180350_AddPhoneStatusToUserPhone'
)
BEGIN
    CREATE INDEX [IX_CallLogVerifications_PaymentAssignmentId] ON [CallLogVerifications] ([PaymentAssignmentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251003180350_AddPhoneStatusToUserPhone'
)
BEGIN
    ALTER TABLE [CallLogVerifications] ADD CONSTRAINT [FK_CallLogVerifications_CallLogPaymentAssignments_PaymentAssignmentId] FOREIGN KEY ([PaymentAssignmentId]) REFERENCES [CallLogPaymentAssignments] ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251003180350_AddPhoneStatusToUserPhone'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251003180350_AddPhoneStatusToUserPhone', N'8.0.6');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251003192422_AddEbillUserAuthentication'
)
BEGIN
    ALTER TABLE [AspNetUsers] DROP CONSTRAINT [FK_AspNetUsers_EbillUsers_EbillUserId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251003192422_AddEbillUserAuthentication'
)
BEGIN
    DROP INDEX [IX_AspNetUsers_EbillUserId] ON [AspNetUsers];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251003192422_AddEbillUserAuthentication'
)
BEGIN
    ALTER TABLE [EbillUsers] ADD [ApplicationUserId] nvarchar(450) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251003192422_AddEbillUserAuthentication'
)
BEGIN
    ALTER TABLE [EbillUsers] ADD [HasLoginAccount] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251003192422_AddEbillUserAuthentication'
)
BEGIN
    ALTER TABLE [EbillUsers] ADD [LoginEnabled] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251003192422_AddEbillUserAuthentication'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_AspNetUsers_EbillUserId] ON [AspNetUsers] ([EbillUserId]) WHERE [EbillUserId] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251003192422_AddEbillUserAuthentication'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD CONSTRAINT [FK_AspNetUsers_EbillUsers_EbillUserId] FOREIGN KEY ([EbillUserId]) REFERENCES [EbillUsers] ([Id]) ON DELETE SET NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251003192422_AddEbillUserAuthentication'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251003192422_AddEbillUserAuthentication', N'8.0.6');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251006074150_ReplaceMonthlyCallCostLimitWithHandsetAllowanceAmount'
)
BEGIN
    EXEC sp_rename N'[ClassOfServices].[MonthlyCallCostLimit]', N'HandsetAllowanceAmount', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251006074150_ReplaceMonthlyCallCostLimitWithHandsetAllowanceAmount'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251006074150_ReplaceMonthlyCallCostLimitWithHandsetAllowanceAmount', N'8.0.6');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251006175733_AddAssignmentStatusToCallRecord'
)
BEGIN
    ALTER TABLE [CallRecords] ADD [assignment_status] nvarchar(20) NOT NULL DEFAULT N'None';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251006175733_AddAssignmentStatusToCallRecord'
)
BEGIN
    UPDATE CallRecords
                      SET assignment_status = 'None'
                      WHERE assignment_status IS NULL OR assignment_status = ''
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251006175733_AddAssignmentStatusToCallRecord'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251006175733_AddAssignmentStatusToCallRecord', N'8.0.6');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251008095859_AddNotificationsTable'
)
BEGIN
    CREATE TABLE [Notifications] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(450) NOT NULL,
        [Title] nvarchar(200) NOT NULL,
        [Message] nvarchar(1000) NOT NULL,
        [Type] nvarchar(50) NOT NULL,
        [IsRead] bit NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [ReadDate] datetime2 NULL,
        [Link] nvarchar(500) NULL,
        [RelatedEntityType] nvarchar(100) NULL,
        [RelatedEntityId] nvarchar(100) NULL,
        [Icon] nvarchar(50) NULL,
        CONSTRAINT [PK_Notifications] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Notifications_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251008095859_AddNotificationsTable'
)
BEGIN
    CREATE INDEX [IX_Notifications_UserId] ON [Notifications] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251008095859_AddNotificationsTable'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251008095859_AddNotificationsTable', N'8.0.6');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251013055408_AddLineTypeToUserPhone'
)
BEGIN
    ALTER TABLE [UserPhones] ADD [LineType] int NOT NULL DEFAULT 2;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251013055408_AddLineTypeToUserPhone'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251013055408_AddLineTypeToUserPhone', N'8.0.6');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251013070229_AddUserPhoneHistoryTable'
)
BEGIN
    CREATE TABLE [UserPhoneHistories] (
        [Id] int NOT NULL IDENTITY,
        [UserPhoneId] int NOT NULL,
        [Action] nvarchar(100) NOT NULL,
        [FieldChanged] nvarchar(100) NULL,
        [OldValue] nvarchar(500) NULL,
        [NewValue] nvarchar(500) NULL,
        [Description] nvarchar(1000) NULL,
        [ChangedBy] nvarchar(200) NULL,
        [ChangedDate] datetime2 NOT NULL,
        [IPAddress] nvarchar(50) NULL,
        [UserAgent] nvarchar(500) NULL,
        CONSTRAINT [PK_UserPhoneHistories] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UserPhoneHistories_UserPhones_UserPhoneId] FOREIGN KEY ([UserPhoneId]) REFERENCES [UserPhones] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251013070229_AddUserPhoneHistoryTable'
)
BEGIN
    CREATE INDEX [IX_UserPhoneHistories_UserPhoneId] ON [UserPhoneHistories] ([UserPhoneId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251013070229_AddUserPhoneHistoryTable'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251013070229_AddUserPhoneHistoryTable', N'8.0.6');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251013093828_AddCallLogRecoveryAndReportingSystem'
)
BEGIN
    ALTER TABLE [StagingBatches] ADD [ApprovalDeadline] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251013093828_AddCallLogRecoveryAndReportingSystem'
)
BEGIN
    ALTER TABLE [StagingBatches] ADD [RecoveryProcessingDate] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251013093828_AddCallLogRecoveryAndReportingSystem'
)
BEGIN
    ALTER TABLE [StagingBatches] ADD [RecoveryStatus] nvarchar(50) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251013093828_AddCallLogRecoveryAndReportingSystem'
)
BEGIN
    ALTER TABLE [StagingBatches] ADD [TotalClassOfServiceAmount] decimal(18,2) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251013093828_AddCallLogRecoveryAndReportingSystem'
)
BEGIN
    ALTER TABLE [StagingBatches] ADD [TotalOfficialAmount] decimal(18,2) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251013093828_AddCallLogRecoveryAndReportingSystem'
)
BEGIN
    ALTER TABLE [StagingBatches] ADD [TotalPersonalAmount] decimal(18,2) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251013093828_AddCallLogRecoveryAndReportingSystem'
)
BEGIN
    ALTER TABLE [StagingBatches] ADD [TotalRecoveredAmount] decimal(18,2) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251013093828_AddCallLogRecoveryAndReportingSystem'
)
BEGIN
    ALTER TABLE [StagingBatches] ADD [VerificationDeadline] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251013093828_AddCallLogRecoveryAndReportingSystem'
)
BEGIN
    ALTER TABLE [CallRecords] ADD [final_assignment_type] nvarchar(50) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251013093828_AddCallLogRecoveryAndReportingSystem'
)
BEGIN
    ALTER TABLE [CallRecords] ADD [recovery_amount] decimal(18,2) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251013093828_AddCallLogRecoveryAndReportingSystem'
)
BEGIN
    ALTER TABLE [CallRecords] ADD [recovery_date] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251013093828_AddCallLogRecoveryAndReportingSystem'
)
BEGIN
    ALTER TABLE [CallRecords] ADD [recovery_processed_by] nvarchar(100) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251013093828_AddCallLogRecoveryAndReportingSystem'
)
BEGIN
    ALTER TABLE [CallRecords] ADD [recovery_status] nvarchar(50) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251013093828_AddCallLogRecoveryAndReportingSystem'
)
BEGIN
    ALTER TABLE [CallLogVerifications] ADD [ApprovalDeadline] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251013093828_AddCallLogRecoveryAndReportingSystem'
)
BEGIN
    ALTER TABLE [CallLogVerifications] ADD [BatchId] uniqueidentifier NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251013093828_AddCallLogRecoveryAndReportingSystem'
)
BEGIN
    ALTER TABLE [CallLogVerifications] ADD [DeadlineMissed] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251013093828_AddCallLogRecoveryAndReportingSystem'
)
BEGIN
    ALTER TABLE [CallLogVerifications] ADD [RevertCount] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251013093828_AddCallLogRecoveryAndReportingSystem'
)
BEGIN
    ALTER TABLE [CallLogVerifications] ADD [RevertDeadline] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251013093828_AddCallLogRecoveryAndReportingSystem'
)
BEGIN
    ALTER TABLE [CallLogVerifications] ADD [SubmissionDeadline] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251013093828_AddCallLogRecoveryAndReportingSystem'
)
BEGIN
    CREATE TABLE [DeadlineTracking] (
        [Id] int NOT NULL IDENTITY,
        [BatchId] uniqueidentifier NOT NULL,
        [DeadlineType] nvarchar(50) NOT NULL,
        [TargetEntity] nvarchar(100) NOT NULL,
        [DeadlineDate] datetime2 NOT NULL,
        [ExtendedDeadline] datetime2 NULL,
        [DeadlineStatus] nvarchar(50) NOT NULL,
        [MissedDate] datetime2 NULL,
        [RecoveryProcessed] bit NOT NULL,
        [RecoveryProcessedDate] datetime2 NULL,
        [ExtensionReason] nvarchar(500) NULL,
        [ExtensionApprovedBy] nvarchar(100) NULL,
        [ExtensionApprovedDate] datetime2 NULL,
        [CreatedDate] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [Notes] nvarchar(1000) NULL,
        CONSTRAINT [PK_DeadlineTracking] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_DeadlineTracking_StagingBatches_BatchId] FOREIGN KEY ([BatchId]) REFERENCES [StagingBatches] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251013093828_AddCallLogRecoveryAndReportingSystem'
)
BEGIN
    CREATE TABLE [RecoveryConfiguration] (
        [Id] int NOT NULL IDENTITY,
        [RuleName] nvarchar(100) NOT NULL,
        [RuleType] nvarchar(50) NOT NULL,
        [IsEnabled] bit NOT NULL,
        [DefaultVerificationDays] int NULL,
        [DefaultApprovalDays] int NULL,
        [AutomationEnabled] bit NOT NULL,
        [RequireApprovalForAutomation] bit NOT NULL,
        [NotificationEnabled] bit NOT NULL,
        [ReminderDaysBefore] int NULL,
        [ConfigValue] nvarchar(max) NULL,
        [Description] nvarchar(500) NULL,
        [CreatedDate] datetime2 NOT NULL,
        [ModifiedDate] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_RecoveryConfiguration] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251013093828_AddCallLogRecoveryAndReportingSystem'
)
BEGIN
    CREATE TABLE [RecoveryLogs] (
        [Id] int NOT NULL IDENTITY,
        [CallRecordId] int NOT NULL,
        [BatchId] uniqueidentifier NOT NULL,
        [RecoveryType] nvarchar(50) NOT NULL,
        [RecoveryAction] nvarchar(50) NOT NULL,
        [RecoveryDate] datetime2 NOT NULL,
        [RecoveryReason] nvarchar(1000) NOT NULL,
        [AmountRecovered] decimal(18,2) NOT NULL,
        [RecoveredFrom] nvarchar(100) NULL,
        [ProcessedBy] nvarchar(100) NULL,
        [DeadlineDate] datetime2 NULL,
        [IsAutomated] bit NOT NULL,
        [Metadata] nvarchar(max) NULL,
        CONSTRAINT [PK_RecoveryLogs] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RecoveryLogs_CallRecords_CallRecordId] FOREIGN KEY ([CallRecordId]) REFERENCES [CallRecords] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_RecoveryLogs_StagingBatches_BatchId] FOREIGN KEY ([BatchId]) REFERENCES [StagingBatches] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251013093828_AddCallLogRecoveryAndReportingSystem'
)
BEGIN
    CREATE INDEX [IX_CallLogVerifications_BatchId] ON [CallLogVerifications] ([BatchId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251013093828_AddCallLogRecoveryAndReportingSystem'
)
BEGIN
    CREATE INDEX [IX_DeadlineTracking_BatchId] ON [DeadlineTracking] ([BatchId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251013093828_AddCallLogRecoveryAndReportingSystem'
)
BEGIN
    CREATE INDEX [IX_DeadlineTracking_DeadlineDate] ON [DeadlineTracking] ([DeadlineDate]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251013093828_AddCallLogRecoveryAndReportingSystem'
)
BEGIN
    CREATE INDEX [IX_DeadlineTracking_DeadlineDate_DeadlineStatus] ON [DeadlineTracking] ([DeadlineDate], [DeadlineStatus]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251013093828_AddCallLogRecoveryAndReportingSystem'
)
BEGIN
    CREATE INDEX [IX_DeadlineTracking_DeadlineStatus] ON [DeadlineTracking] ([DeadlineStatus]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251013093828_AddCallLogRecoveryAndReportingSystem'
)
BEGIN
    CREATE INDEX [IX_DeadlineTracking_DeadlineType_TargetEntity] ON [DeadlineTracking] ([DeadlineType], [TargetEntity]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251013093828_AddCallLogRecoveryAndReportingSystem'
)
BEGIN
    CREATE INDEX [IX_DeadlineTracking_TargetEntity] ON [DeadlineTracking] ([TargetEntity]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251013093828_AddCallLogRecoveryAndReportingSystem'
)
BEGIN
    CREATE INDEX [IX_RecoveryConfiguration_IsEnabled] ON [RecoveryConfiguration] ([IsEnabled]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251013093828_AddCallLogRecoveryAndReportingSystem'
)
BEGIN
    CREATE UNIQUE INDEX [IX_RecoveryConfiguration_RuleName] ON [RecoveryConfiguration] ([RuleName]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251013093828_AddCallLogRecoveryAndReportingSystem'
)
BEGIN
    CREATE INDEX [IX_RecoveryConfiguration_RuleType] ON [RecoveryConfiguration] ([RuleType]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251013093828_AddCallLogRecoveryAndReportingSystem'
)
BEGIN
    CREATE INDEX [IX_RecoveryLogs_BatchId] ON [RecoveryLogs] ([BatchId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251013093828_AddCallLogRecoveryAndReportingSystem'
)
BEGIN
    CREATE INDEX [IX_RecoveryLogs_CallRecordId] ON [RecoveryLogs] ([CallRecordId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251013093828_AddCallLogRecoveryAndReportingSystem'
)
BEGIN
    CREATE INDEX [IX_RecoveryLogs_RecoveredFrom] ON [RecoveryLogs] ([RecoveredFrom]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251013093828_AddCallLogRecoveryAndReportingSystem'
)
BEGIN
    CREATE INDEX [IX_RecoveryLogs_RecoveryDate] ON [RecoveryLogs] ([RecoveryDate]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251013093828_AddCallLogRecoveryAndReportingSystem'
)
BEGIN
    CREATE INDEX [IX_RecoveryLogs_RecoveryDate_RecoveryType] ON [RecoveryLogs] ([RecoveryDate], [RecoveryType]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251013093828_AddCallLogRecoveryAndReportingSystem'
)
BEGIN
    CREATE INDEX [IX_RecoveryLogs_RecoveryType] ON [RecoveryLogs] ([RecoveryType]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251013093828_AddCallLogRecoveryAndReportingSystem'
)
BEGIN
    ALTER TABLE [CallLogVerifications] ADD CONSTRAINT [FK_CallLogVerifications_StagingBatches_BatchId] FOREIGN KEY ([BatchId]) REFERENCES [StagingBatches] ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251013093828_AddCallLogRecoveryAndReportingSystem'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251013093828_AddCallLogRecoveryAndReportingSystem', N'8.0.6');
END;
GO

COMMIT;
GO

