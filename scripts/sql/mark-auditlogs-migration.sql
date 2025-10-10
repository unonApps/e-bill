-- Mark the AuditLogs migration as applied in EF history
USE TABDB;
GO

-- Check if the migration hasn't been added already
IF NOT EXISTS (SELECT * FROM __EFMigrationsHistory WHERE MigrationId = '20250924120000_AddAuditLogsTableOnly')
BEGIN
    INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20250924120000_AddAuditLogsTableOnly', '8.0.0');

    PRINT 'Migration marked as applied successfully';
END
ELSE
BEGIN
    PRINT 'Migration already marked as applied';
END