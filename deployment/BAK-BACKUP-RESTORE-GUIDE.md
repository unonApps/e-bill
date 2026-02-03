# BAK File Backup & Restore Guide

This is the **easiest and most reliable** way to sync your database from local to server.

---

## 📋 Process Overview

1. **Backup** local database to BAK file (on your machine)
2. **Copy** BAK file to server
3. **Restore** BAK file on server (replaces existing database)
4. **Deploy** application and test

**Time:** 5-15 minutes total

---

## Step 1: Backup Local Database

### Option A: Using SSMS (Easiest) ✅

1. **Open SSMS** on your local machine
2. **Connect to** `(localdb)\mssqllocaldb`
3. **Expand Databases**
4. **Right-click `tabdb`** → **Tasks** → **Back Up...**

5. **In the Backup Database window:**
   - **Backup type:** Full
   - **Backup component:** Database
   - **Destination:** Remove any existing destinations, then click **Add...**
   - **File name:** Browse to `C:\temp\tabdb_full_backup.bak`
   - **Media Options:**
     - ✅ Check "Back up to the existing media set"
     - ✅ Check "Overwrite all existing backup sets"
   - **Backup Options:**
     - ✅ Check "Compress backup" (makes file smaller)
   - Click **OK**

6. **Wait for completion** (you'll see a success message)

### Option B: Using SQL Script

1. Open SSMS on local machine
2. Connect to `(localdb)\mssqllocaldb`
3. Open: `deployment/1-backup-local-database.sql`
4. Click **Execute** (F5)
5. Watch progress (shows 10%, 20%, 30%, etc.)

---

## Step 2: Copy BAK File to Server

### Option A: Copy via Network Share
```powershell
# On your local machine:
Copy-Item "C:\temp\tabdb_full_backup.bak" "\\keonstcwbsvwd01\C$\temp\tabdb_full_backup.bak"
```

### Option B: Copy via Remote Desktop
1. Connect to server via RDP
2. Copy file from local `C:\temp\`
3. Paste to server `C:\temp\`

### Option C: Copy via USB Drive
1. Copy `C:\temp\tabdb_full_backup.bak` to USB
2. Connect USB to server
3. Copy to server `C:\temp\`

---

## Step 3: Restore on Server

### ⚠️ IMPORTANT: Update Data Paths First!

Before running the restore script, you need to know where SQL Server stores data files on your server.

**Find the correct paths:**

In SSMS on the SERVER, run this:
```sql
-- Find where SQL Server stores database files
SELECT
    physical_name,
    type_desc
FROM sys.master_files
WHERE database_id = DB_ID('master');
```

This will show paths like:
- Data: `C:\Program Files\Microsoft SQL Server\MSSQL16.MSSQLSERVER\MSSQL\DATA\`
- Log: `C:\Program Files\Microsoft SQL Server\MSSQL16.MSSQLSERVER\MSSQL\DATA\`

**Update the restore script:**

1. Open `deployment/2-restore-to-server.sql`
2. Update these lines at the top (lines 11-12):
```sql
DECLARE @DataPath NVARCHAR(500) = 'YOUR_DATA_PATH_HERE';
DECLARE @LogPath NVARCHAR(500) = 'YOUR_LOG_PATH_HERE';
```

### Now Restore:

1. **Open SSMS on SERVER**
2. **Connect to SQL Server instance**
3. **Open:** `deployment/2-restore-to-server.sql`
4. **Review the script** (make sure paths are correct)
5. **Execute** (F5)

The script will:
- ✅ Create automatic safety backup
- ✅ Close all connections
- ✅ Restore database from BAK
- ✅ Verify the restore
- ✅ Show record counts

**Expected output:**
```
Restore completed successfully!
Database is ONLINE
[Record counts table showing your data]
```

---

## Step 4: Verify Success

After restore completes, check that data is there:

```sql
USE tabdb;
GO

-- Should show same counts as local database
SELECT 'Safaricom' AS TableName, COUNT(*) AS RecordCount FROM Safaricom
UNION ALL
SELECT 'Airtel', COUNT(*) FROM Airtel
UNION ALL
SELECT 'CallLogStagings', COUNT(*) FROM CallLogStagings;
```

---

## Step 5: Deploy Application

Now that database is synced:

```powershell
# Build
.\deployment\1-build-and-package.ps1

# Deploy (run ON SERVER)
.\deployment\2-deploy-on-server.ps1
```

---

## ✨ What This Fixes

After restore, your server will have:
- ✅ **Exact same schema** as local
- ✅ **All data** from local database
- ✅ **Correct column casing** (ext, dialed, dest)
- ✅ **IsAdjustment column** in stored procedures
- ✅ **All 23 migrations** applied
- ✅ **Updated stored procedures**

**Result:** Hangfire consolidation will work perfectly!

---

## 🔍 Troubleshooting

### "Cannot open backup device"

**Cause:** BAK file not found on server
**Fix:** Make sure you copied the file to `C:\temp\` on the server

### "Operating system error 5 (Access is denied)"

**Cause:** SQL Server service account doesn't have permission
**Fix 1:** Move BAK file to SQL Server backup folder:
```
C:\Program Files\Microsoft SQL Server\MSSQL16.MSSQLSERVER\MSSQL\Backup\
```

**Fix 2:** Grant SQL Server account read permission on C:\temp\

### "Database is in use"

**Cause:** Active connections to database
**Fix:** The script handles this automatically with `SET SINGLE_USER`

### "Incorrect physical path"

**Cause:** Wrong @DataPath or @LogPath
**Fix:** Run the query in Step 3 to find correct paths, update script

### "The backup set holds a backup of a database other than the existing database"

**Cause:** Database names don't match
**Fix:** This is fine - the script uses `WITH REPLACE` to handle this

---

## 📊 File Sizes

Expected BAK file sizes:

- **Empty database:** ~5 MB
- **Small dataset (few thousand records):** 10-50 MB
- **Medium dataset (hundreds of thousands):** 50-500 MB
- **Large dataset (millions of records):** 500+ MB

With compression enabled, files are typically 50-70% smaller.

---

## 🔄 Rollback (If Needed)

If something goes wrong, restore the safety backup:

```sql
USE master;
GO

-- Find your safety backup file
-- It's in C:\temp\ with name like: tabdb_safety_backup_20251120_153045.bak

ALTER DATABASE tabdb SET SINGLE_USER WITH ROLLBACK IMMEDIATE;

RESTORE DATABASE tabdb
FROM DISK = 'C:\temp\tabdb_safety_backup_YYYYMMDD_HHMMSS.bak'
WITH REPLACE;

ALTER DATABASE tabdb SET MULTI_USER;
```

---

## ⚡ Why BAK is Better Than BACPAC

| Feature | BAK | BACPAC |
|---------|-----|--------|
| Speed | ⚡ Fast | 🐌 Slow |
| Reliability | ✅ Very High | ⚠️ Can fail |
| File Size | 📦 Smaller (compressed) | 📦 Larger |
| Complexity | 🟢 Simple | 🔴 Complex |
| Native SQL Server | ✅ Yes | ❌ No |
| Preserves everything | ✅ Yes | ⚠️ Sometimes issues |

**Recommendation:** Use BAK for this task!

---

## 📝 Summary Commands

```powershell
# Local machine - Backup
# (Run in SSMS: deployment/1-backup-local-database.sql)

# Copy to server
Copy-Item "C:\temp\tabdb_full_backup.bak" "\\keonstcwbsvwd01\C$\temp\"

# Server - Restore
# (Run in SSMS: deployment/2-restore-to-server.sql)

# Deploy app
.\deployment\1-build-and-package.ps1
# (Then run 2-deploy-on-server.ps1 ON SERVER)
```

---

## ✅ Checklist

Before you start:
- [ ] Local database has data (check record counts)
- [ ] C:\temp\ folder exists on local machine
- [ ] C:\temp\ folder exists on server
- [ ] You have sysadmin rights on server SQL Server
- [ ] No critical processes using the database on server

After restore:
- [ ] Restore completed successfully
- [ ] Record counts match local database
- [ ] Database is ONLINE
- [ ] Safety backup was created

---

Good luck! This approach is much simpler than BACPAC and should work perfectly.
