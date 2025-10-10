-- Create Notifications table if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Notifications]') AND type in (N'U'))
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
        CONSTRAINT [FK_Notifications_AspNetUsers_UserId] FOREIGN KEY ([UserId])
            REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );

    CREATE INDEX [IX_Notifications_UserId] ON [Notifications] ([UserId]);

    PRINT 'Notifications table created successfully';
END
ELSE
BEGIN
    PRINT 'Notifications table already exists';
END
GO

-- Mark the AddNotificationsTable migration as applied if it's not already
IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20251008095859_AddNotificationsTable')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251008095859_AddNotificationsTable', N'8.0.0');
    PRINT 'Migration 20251008095859_AddNotificationsTable marked as applied';
END
ELSE
BEGIN
    PRINT 'Migration already recorded in history';
END
GO

-- Also mark other pending migrations as applied if their tables already exist
-- This prevents errors when running ef database update

-- Check and mark 20251002163017_AddCallLogVerificationSystemTables
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[CallLogVerifications]') AND type in (N'U'))
   AND NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20251002163017_AddCallLogVerificationSystemTables')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251002163017_AddCallLogVerificationSystemTables', N'8.0.0');
    PRINT 'Migration 20251002163017_AddCallLogVerificationSystemTables marked as applied';
END

-- Check and mark 20251002173558_AddUserPhoneRelationshipToCallRecords
IF EXISTS (SELECT * FROM sys.columns WHERE Name = N'UserPhoneId' AND Object_ID = Object_ID(N'[dbo].[CallRecords]'))
   AND NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20251002173558_AddUserPhoneRelationshipToCallRecords')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251002173558_AddUserPhoneRelationshipToCallRecords', N'8.0.0');
    PRINT 'Migration 20251002173558_AddUserPhoneRelationshipToCallRecords marked as applied';
END

-- Check and mark 20251003180350_AddPhoneStatusToUserPhone
IF EXISTS (SELECT * FROM sys.columns WHERE Name = N'PhoneStatus' AND Object_ID = Object_ID(N'[dbo].[UserPhones]'))
   AND NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20251003180350_AddPhoneStatusToUserPhone')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251003180350_AddPhoneStatusToUserPhone', N'8.0.0');
    PRINT 'Migration 20251003180350_AddPhoneStatusToUserPhone marked as applied';
END

-- Check and mark 20251003192422_AddEbillUserAuthentication
IF EXISTS (SELECT * FROM sys.columns WHERE Name = N'ApplicationUserId' AND Object_ID = Object_ID(N'[dbo].[EbillUsers]'))
   AND NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20251003192422_AddEbillUserAuthentication')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251003192422_AddEbillUserAuthentication', N'8.0.0');
    PRINT 'Migration 20251003192422_AddEbillUserAuthentication marked as applied';
END

-- Check and mark 20251006074150_ReplaceMonthlyCallCostLimitWithHandsetAllowanceAmount
IF EXISTS (SELECT * FROM sys.columns WHERE Name = N'HandsetAllowanceAmount' AND Object_ID = Object_ID(N'[dbo].[ClassOfServices]'))
   AND NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20251006074150_ReplaceMonthlyCallCostLimitWithHandsetAllowanceAmount')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251006074150_ReplaceMonthlyCallCostLimitWithHandsetAllowanceAmount', N'8.0.0');
    PRINT 'Migration 20251006074150_ReplaceMonthlyCallCostLimitWithHandsetAllowanceAmount marked as applied';
END

-- Check and mark 20251006175733_AddAssignmentStatusToCallRecord
IF EXISTS (SELECT * FROM sys.columns WHERE Name = N'AssignmentStatus' AND Object_ID = Object_ID(N'[dbo].[CallRecords]'))
   AND NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20251006175733_AddAssignmentStatusToCallRecord')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251006175733_AddAssignmentStatusToCallRecord', N'8.0.0');
    PRINT 'Migration 20251006175733_AddAssignmentStatusToCallRecord marked as applied';
END
GO

PRINT 'All migrations synchronized successfully!';
