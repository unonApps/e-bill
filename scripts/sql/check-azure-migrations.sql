-- Run this in Azure Portal Query Editor to see what migrations are recorded
SELECT MigrationId, ProductVersion
FROM __EFMigrationsHistory
ORDER BY MigrationId;

-- Also check if the table exists
SELECT COUNT(*) as RecordedMigrations FROM __EFMigrationsHistory;
