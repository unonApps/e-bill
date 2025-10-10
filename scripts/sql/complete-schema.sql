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

