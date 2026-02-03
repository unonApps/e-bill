-- ========================================
-- Fix Column Casing on Server Database
-- Run this in SSMS on SERVER database
-- ========================================

-- This preserves your data while fixing column names

USE tabdb;
GO

PRINT 'Fixing Safaricom table...';

-- Safaricom: Rename PascalCase to lowercase
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Safaricom') AND name = 'Ext')
    EXEC sp_rename 'Safaricom.Ext', 'ext', 'COLUMN';

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Safaricom') AND name = 'Dialed')
    EXEC sp_rename 'Safaricom.Dialed', 'dialed', 'COLUMN';

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Safaricom') AND name = 'Dest')
    EXEC sp_rename 'Safaricom.Dest', 'dest', 'COLUMN';

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Safaricom') AND name = 'Durx')
    EXEC sp_rename 'Safaricom.Durx', 'durx', 'COLUMN';

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Safaricom') AND name = 'Cost')
    EXEC sp_rename 'Safaricom.Cost', 'cost', 'COLUMN';

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Safaricom') AND name = 'Dur')
    EXEC sp_rename 'Safaricom.Dur', 'dur', 'COLUMN';

PRINT 'Safaricom table fixed!';
GO

PRINT 'Fixing Airtel table...';

-- Airtel: Same column names as Safaricom
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Airtel') AND name = 'Ext')
    EXEC sp_rename 'Airtel.Ext', 'ext', 'COLUMN';

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Airtel') AND name = 'Dialed')
    EXEC sp_rename 'Airtel.Dialed', 'dialed', 'COLUMN';

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Airtel') AND name = 'Dest')
    EXEC sp_rename 'Airtel.Dest', 'dest', 'COLUMN';

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Airtel') AND name = 'Durx')
    EXEC sp_rename 'Airtel.Durx', 'durx', 'COLUMN';

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Airtel') AND name = 'Cost')
    EXEC sp_rename 'Airtel.Cost', 'cost', 'COLUMN';

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Airtel') AND name = 'Dur')
    EXEC sp_rename 'Airtel.Dur', 'dur', 'COLUMN';

PRINT 'Airtel table fixed!';
GO

PRINT 'Column casing fixed! Schema now matches migrations.';
PRINT 'You can now run Hangfire imports successfully.';
GO

-- Verify the changes
SELECT 'Safaricom' AS TableName, COLUMN_NAME
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Safaricom'
ORDER BY ORDINAL_POSITION;

SELECT 'Airtel' AS TableName, COLUMN_NAME
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Airtel'
ORDER BY ORDINAL_POSITION;
