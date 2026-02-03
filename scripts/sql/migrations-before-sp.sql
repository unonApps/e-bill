BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251112153351_AddAzureAdProfileFields'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [CompanyName] nvarchar(200) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251112153351_AddAzureAdProfileFields'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [Department] nvarchar(200) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251112153351_AddAzureAdProfileFields'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [JobTitle] nvarchar(200) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251112153351_AddAzureAdProfileFields'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [MobilePhone] nvarchar(50) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251112153351_AddAzureAdProfileFields'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [OfficeLocation] nvarchar(200) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251112153351_AddAzureAdProfileFields'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251112153351_AddAzureAdProfileFields', N'8.0.6');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251112155719_IncreaseCallTypeColumnLength'
)
BEGIN
    DECLARE @var0 sysname;
    SELECT @var0 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Safaricom]') AND [c].[name] = N'call_type');
    IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [Safaricom] DROP CONSTRAINT [' + @var0 + '];');
    ALTER TABLE [Safaricom] ALTER COLUMN [call_type] nvarchar(100) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251112155719_IncreaseCallTypeColumnLength'
)
BEGIN
    DECLARE @var1 sysname;
    SELECT @var1 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[CallRecords]') AND [c].[name] = N'call_type');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [CallRecords] DROP CONSTRAINT [' + @var1 + '];');
    ALTER TABLE [CallRecords] ALTER COLUMN [call_type] nvarchar(100) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251112155719_IncreaseCallTypeColumnLength'
)
BEGIN
    DECLARE @var2 sysname;
    SELECT @var2 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Airtel]') AND [c].[name] = N'call_type');
    IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [Airtel] DROP CONSTRAINT [' + @var2 + '];');
    ALTER TABLE [Airtel] ALTER COLUMN [call_type] nvarchar(100) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251112155719_IncreaseCallTypeColumnLength'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251112155719_IncreaseCallTypeColumnLength', N'8.0.6');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251112161045_IncreaseCallLogStagingCallTypeLength'
)
BEGIN
    DECLARE @var3 sysname;
    SELECT @var3 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[CallLogStagings]') AND [c].[name] = N'CallType');
    IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [CallLogStagings] DROP CONSTRAINT [' + @var3 + '];');
    ALTER TABLE [CallLogStagings] ALTER COLUMN [CallType] nvarchar(100) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251112161045_IncreaseCallLogStagingCallTypeLength'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251112161045_IncreaseCallLogStagingCallTypeLength', N'8.0.6');
END;
GO

COMMIT;
GO

