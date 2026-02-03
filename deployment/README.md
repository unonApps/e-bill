# TABWeb Deployment Scripts

This folder contains automated deployment scripts for the TABWeb application.

## Quick Start

### Option 1: Manual Step-by-Step (Recommended for First Time)

**On Your Development Machine:**
1. Run `1-build-and-package.ps1` - Builds and creates deployment package
2. Run `3-copy-to-server.ps1` - Copies package to server

**On the Server (10.104.104.78):**
3. Run `2-deploy-on-server.ps1` - Deploys the application

### Option 2: Automated (All Steps)

**On Your Development Machine:**
```powershell
.\DEPLOY-FULL.ps1
```

Then RDP to server and run step 2 manually.

---

## Script Details

### 1-build-and-package.ps1
**Where:** Development Machine
**What:** Builds the project in Release mode and creates deployment ZIP
**Output:** `deploy-to-iis.zip` and timestamped backup

**Usage:**
```powershell
.\1-build-and-package.ps1
```

---

### 2-deploy-on-server.ps1
**Where:** Server (10.104.104.78)
**What:** Deploys the application (stops, backs up, deploys, starts)
**Requires:** Administrator privileges

**Usage:**
```powershell
# Run as Administrator
.\2-deploy-on-server.ps1
```

**Optional Parameters:**
```powershell
# Custom paths
.\2-deploy-on-server.ps1 -ZipPath "C:\custom\path\deploy.zip" -WebsitePath "C:\custom\website\path"
```

---

### 3-copy-to-server.ps1
**Where:** Development Machine
**What:** Copies deployment package to the server

**Usage:**
```powershell
# Basic usage
.\3-copy-to-server.ps1

# With custom server IP
.\3-copy-to-server.ps1 -ServerIP "10.104.104.78"

# With credentials
$cred = Get-Credential
.\3-copy-to-server.ps1 -Credential $cred
```

---

### DEPLOY-FULL.ps1
**Where:** Development Machine
**What:** Runs all deployment steps automatically

**Usage:**
```powershell
# Complete deployment
.\DEPLOY-FULL.ps1

# Skip copy (if already copied)
.\DEPLOY-FULL.ps1 -SkipCopy
```

---

## Typical Deployment Workflow

```
┌─────────────────────────────────────────────────┐
│ Development Machine                             │
│                                                 │
│ 1. Make code changes                            │
│ 2. Run: .\1-build-and-package.ps1              │
│ 3. Run: .\3-copy-to-server.ps1                 │
└─────────────────────────────────────────────────┘
                     │
                     ↓ (Copy via network)
┌─────────────────────────────────────────────────┐
│ Server (10.104.104.78)                          │
│                                                 │
│ 4. RDP to server                                │
│ 5. Run: .\2-deploy-on-server.ps1               │
│ 6. Test: http://10.104.104.78                  │
└─────────────────────────────────────────────────┘
```

---

## What Each Script Does

### Build Script (Step 1)
- Cleans previous builds
- Compiles in Release mode
- Publishes application
- Creates ZIP package
- Creates timestamped backup

### Deploy Script (Step 2)
- Stops website and app pool
- Creates backup of current version
- Clears old files (preserves custom settings)
- Extracts new version
- Sets proper permissions
- Starts website and app pool
- Verifies deployment

### Copy Script (Step 3)
- Tests server connectivity
- Creates remote directory if needed
- Copies ZIP to server
- Provides next step instructions

---

## Configuration

### Default Paths

**Development Machine:**
- Project: `C:\Users\dxmic\Desktop\Do Net Template\DoNetTemplate.Web`
- Output: `deploy-to-iis.zip`

**Server:**
- Deployment Package: `C:\ebill\deploy-to-iis.zip`
- Website: `C:\inetpub\wwwroot\TABWeb`
- Backups: `C:\inetpub\backups\TABWeb_[timestamp]`
- Website Name: `TABWeb`
- App Pool: `TABWeb_AppPool`

### Customizing Paths

Edit the parameters in each script or pass them via command line:

```powershell
.\2-deploy-on-server.ps1 `
    -ZipPath "D:\deployments\app.zip" `
    -WebsitePath "D:\websites\myapp" `
    -WebsiteName "MyApp" `
    -AppPoolName "MyApp_Pool"
```

---

## Troubleshooting

### "Access Denied" when copying to server
**Solution:**
- Ensure you have admin rights on the server
- Try running with explicit credentials:
  ```powershell
  $cred = Get-Credential
  .\3-copy-to-server.ps1 -Credential $cred
  ```

### "Cannot find dotnet command"
**Solution:**
- Ensure .NET SDK is installed on development machine
- Add to PATH or use full path:
  ```powershell
  & "C:\Program Files\dotnet\dotnet.exe" publish ...
  ```

### Build fails
**Solution:**
- Check for compilation errors
- Ensure all NuGet packages are restored
- Run `dotnet restore` first

### Deployment succeeds but site doesn't work
**Solution:**
- Check Event Viewer on server:
  ```powershell
  Get-EventLog -LogName Application -Newest 10 -EntryType Error
  ```
- Check IIS logs: `C:\inetpub\logs\LogFiles`
- Verify database connection string in `appsettings.Production.json`

---

## Rollback

If deployment fails, restore from backup:

```powershell
# Stop the application
Stop-Website -Name "TABWeb"
Stop-WebAppPool -Name "TABWeb_AppPool"

# Find latest backup
$latestBackup = Get-ChildItem "C:\inetpub\backups" | Sort-Object LastWriteTime -Descending | Select-Object -First 1

# Restore
Remove-Item "C:\inetpub\wwwroot\TABWeb\*" -Recurse -Force
Copy-Item "$($latestBackup.FullName)\*" "C:\inetpub\wwwroot\TABWeb" -Recurse -Force

# Start the application
Start-WebAppPool -Name "TABWeb_AppPool"
Start-Website -Name "TABWeb"
```

---

## Security Notes

- Always run deployment scripts as Administrator on the server
- Keep backups for at least 30 days
- Test deployments in a staging environment first
- Review changes before deploying to production
- Keep deployment packages in a secure location

---

## Quick Reference

```powershell
# Development Machine - Full deployment
.\DEPLOY-FULL.ps1

# Development Machine - Build only
.\1-build-and-package.ps1

# Development Machine - Copy to server
.\3-copy-to-server.ps1

# Server - Deploy
.\2-deploy-on-server.ps1

# Server - Check status
Get-Website -Name "TABWeb"
Get-WebAppPoolState -Name "TABWeb_AppPool"

# Server - Restart application
Restart-WebAppPool -Name "TABWeb_AppPool"

# Server - View logs
Get-EventLog -LogName Application -Newest 10 -EntryType Error
```

---

## Support

For issues or questions:
1. Check Event Viewer on server for errors
2. Check IIS logs
3. Verify database connectivity
4. Verify Azure AD configuration

---

**Last Updated:** 2025-11-06
**Version:** 1.0
