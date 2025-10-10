-- Check if tables exist
SELECT 'Organizations' as TableName,
       CASE WHEN EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Organizations')
            THEN 'EXISTS' ELSE 'NOT EXISTS' END as Status
UNION ALL
SELECT 'Offices',
       CASE WHEN EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Offices')
            THEN 'EXISTS' ELSE 'NOT EXISTS' END
UNION ALL
SELECT 'SubOffices',
       CASE WHEN EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'SubOffices')
            THEN 'EXISTS' ELSE 'NOT EXISTS' END
UNION ALL
SELECT 'PSTNs',
       CASE WHEN EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'PSTNs')
            THEN 'EXISTS' ELSE 'NOT EXISTS' END;

-- Check if columns exist in AspNetUsers
SELECT 'AspNetUsers.OrganizationId' as ColumnName,
       CASE WHEN EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS
                         WHERE TABLE_NAME = 'AspNetUsers' AND COLUMN_NAME = 'OrganizationId')
            THEN 'EXISTS' ELSE 'NOT EXISTS' END as Status
UNION ALL
SELECT 'AspNetUsers.OfficeId',
       CASE WHEN EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS
                         WHERE TABLE_NAME = 'AspNetUsers' AND COLUMN_NAME = 'OfficeId')
            THEN 'EXISTS' ELSE 'NOT EXISTS' END
UNION ALL
SELECT 'AspNetUsers.SubOfficeId',
       CASE WHEN EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS
                         WHERE TABLE_NAME = 'AspNetUsers' AND COLUMN_NAME = 'SubOfficeId')
            THEN 'EXISTS' ELSE 'NOT EXISTS' END;

-- Check migration history
SELECT MigrationId, ProductVersion
FROM __EFMigrationsHistory
ORDER BY MigrationId;