# Database Export/Import Guide

This will export your working local database (with all data) and import it to the server, fixing all consolidation errors.

## ⚠️ IMPORTANT WARNINGS

- **Server data will be REPLACED** with local data
- All current data on server will be deleted
- The server database will be an exact copy of your local database
- A backup will be created automatically before replacement

---

## Process Overview

1. **Export** local database (schema + data)
2. **Import** to server (replaces existing database)
3. **Deploy** application to server
4. **Test** Hangfire imports

---

## Step 1: Export Local Database

Run this from your development machine:

```powershell
cd "C:\Users\dxmic\Desktop\Do Net Template\DoNetTemplate.Web"

.\deployment\1-export-local-database.ps1
```

**What it does:**
- Exports local `tabdb` database to `deployment\tabdb_full_export.bacpac`
- Includes all schema (tables, stored procedures, etc.)
- Includes all data
- Creates export diagnostics log

**Expected output:**
```
File: deployment\tabdb_full_export.bacpac
Size: XX MB
```

**Time:** 2-10 minutes depending on data size

---

## Step 2: Import to Server

Run this from your development machine (or copy files to server and run there):

```powershell
.\deployment\2-import-to-server.ps1 `
    -ServerName "keonstcwbsvwd01" `
    -DatabaseName "tabdb" `
    -Username "sa" `
    -Password "your_password"
```

**What it does:**
1. Creates automatic backup of existing server database (saved to C:\temp\)
2. Drops existing server database
3. Imports the BACPAC file
4. Restores all data from local database

**Safety confirmation required:**
- You must type `REPLACE DATABASE` to proceed
- Automatic backup is created first

**Expected output:**
```
✓ Schema matches
✓ Column casing correct
✓ Stored procedures updated
✓ All data preserved
```

**Time:** 5-30 minutes depending on data size

---

## Step 3: Deploy Application

After import completes:

```powershell
# Build and package
.\deployment\1-build-and-package.ps1

# Deploy to server (run ON SERVER)
.\deployment\2-deploy-on-server.ps1
```

---

## Step 4: Test

1. Open application in browser
2. Go to `/Admin/ImportJobs`
3. Try a Hangfire bulk import
4. Verify no errors:
   - ✓ No column mapping errors
   - ✓ No IsAdjustment NULL errors
   - ✓ No page refresh loops

---

## What Gets Fixed

This process fixes ALL these issues:

1. **Schema mismatches** - Server will have exact same schema as local
2. **Column casing** - ext/dialed/dest will be lowercase (correct)
3. **Missing columns** - IsAdjustment and all other columns present
4. **Stored procedures** - All updated with correct logic
5. **Missing migrations** - All 23 migrations applied
6. **Index differences** - All indexes match

---

## Requirements

**On development machine:**
- SQL Server Management Studio (SSMS) OR
- SqlPackage.exe (download from https://aka.ms/sqlpackage-windows)

**On server:**
- SQL Server installed
- Enough disk space for database (check local db size first)
- C:\temp\ folder exists (for automatic backup)

---

## Troubleshooting

### "SqlPackage.exe not found"

**Solution 1:** Install SqlPackage
- Download: https://aka.ms/sqlpackage-windows
- Install to default location

**Solution 2:** Use SSMS manually
1. Open SSMS on local machine
2. Right-click `tabdb` database
3. Tasks → Export Data-tier Application...
4. Save as: `deployment\tabdb_full_export.bacpac`
5. Then run import script

### "Connection failed"

- Check server name is correct: `keonstcwbsvwd01`
- Check SQL Server is running on server
- Check firewall allows SQL Server port (1433)
- Verify username/password

### "Timeout during import"

- This is normal for large databases
- Script uses CommandTimeout=0 (no timeout)
- Just wait - progress is shown in console

### "Import failed - constraint violation"

- This shouldn't happen with BACPAC format
- Check diagnostics: `deployment\import_diagnostics.log`
- Contact support if issues persist

---

## Rollback (if needed)

If something goes wrong, restore the backup:

```sql
-- In SSMS on server:
USE master;
GO

-- Drop current database
ALTER DATABASE tabdb SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
DROP DATABASE tabdb;
GO

-- Restore from backup
RESTORE DATABASE tabdb
FROM DISK = 'C:\temp\tabdb_backup_YYYYMMDD_HHMMSS.bak'
WITH REPLACE;
GO
```

Replace `YYYYMMDD_HHMMSS` with the timestamp from the backup file.

---

## Alternative: Manual Process

If scripts don't work, you can do this manually:

### Export (Manual):
1. Open SSMS
2. Connect to `(localdb)\mssqllocaldb`
3. Right-click `tabdb` → Tasks → Export Data-tier Application
4. Save as: `tabdb_full_export.bacpac`

### Import (Manual):
1. Copy BACPAC file to server
2. Open SSMS on server
3. Connect to SQL Server
4. Right-click Databases → Import Data-tier Application
5. Select the BACPAC file
6. Follow wizard

---

## Notes

- BACPAC format is Microsoft's standard for database export/import
- Includes schema + data in a single compressed file
- More reliable than SQL scripts for large databases
- Preserves all relationships, constraints, indexes
- Can be used across different SQL Server versions

---

## Questions?

If you have any issues with this process, check:
1. Export/import diagnostic logs in `deployment/` folder
2. SQL Server error logs
3. Verify file was created and has reasonable size (> 1 MB)
