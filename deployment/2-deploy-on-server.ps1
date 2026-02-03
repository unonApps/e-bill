# ========================================
# TABWeb - Server Deployment Script
# Run this on the SERVER (10.104.104.78)
# ========================================

param(
    [string]$ZipPath = "",
    [string]$DeploymentFolder = "C:\ebill",
    [string]$WebsitePath = "C:\inetpub\wwwroot\TABWeb",
    [string]$WebsiteName = "TABWeb",
    [string]$AppPoolName = "TABWeb_AppPool",
    [string]$BackupPath = "C:\inetpub\backups"
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "TABWeb Deployment - Server Deployment" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Auto-detect latest ZIP file if not specified
if ([string]::IsNullOrEmpty($ZipPath)) {
    Write-Host "Auto-detecting latest deployment package..." -ForegroundColor Yellow

    # Look for deploy-to-iis*.zip files
    $zipFiles = Get-ChildItem -Path $DeploymentFolder -Filter "deploy-to-iis*.zip" -File | Sort-Object LastWriteTime -Descending

    if ($zipFiles.Count -eq 0) {
        Write-Host "ERROR: No deployment packages found in $DeploymentFolder" -ForegroundColor Red
        Write-Host "Please copy deploy-to-iis.zip to the server first!" -ForegroundColor Yellow
        exit 1
    }

    $ZipPath = $zipFiles[0].FullName
    Write-Host "  - Found: $($zipFiles[0].Name)" -ForegroundColor Gray
    Write-Host "  - Modified: $($zipFiles[0].LastWriteTime)" -ForegroundColor Gray
    Write-Host "  - Size: $([math]::Round($zipFiles[0].Length / 1MB, 2)) MB" -ForegroundColor Gray
}

# Check if ZIP file exists
if (-not (Test-Path $ZipPath)) {
    Write-Host "ERROR: Deployment package not found at: $ZipPath" -ForegroundColor Red
    Write-Host "Please copy deploy-to-iis.zip to the server first!" -ForegroundColor Yellow
    exit 1
}

Write-Host ""
Write-Host "Using deployment package: $ZipPath" -ForegroundColor Cyan
Write-Host ""

# Reminder about migrations
Write-Host "========================================" -ForegroundColor Yellow
Write-Host "PRE-DEPLOYMENT CHECKLIST" -ForegroundColor Yellow
Write-Host "========================================" -ForegroundColor Yellow
Write-Host ""
Write-Host "IMPORTANT: Have you applied database migrations?" -ForegroundColor Red
Write-Host ""
Write-Host "Migrations should be applied from your dev machine using:" -ForegroundColor Cyan
Write-Host "  .\apply-migrations-to-server.ps1 -ServerName 'SERVER' -DatabaseName 'tabdb' ..." -ForegroundColor Gray
Write-Host ""
$response = Read-Host "Migrations applied? (y/n)"
if ($response -ne 'y' -and $response -ne 'Y') {
    Write-Host ""
    Write-Host "Deployment cancelled. Please apply migrations first!" -ForegroundColor Red
    Write-Host ""
    exit 1
}
Write-Host ""

# Check if running as Administrator
$currentPrincipal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
if (-not $currentPrincipal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Host "ERROR: This script must be run as Administrator!" -ForegroundColor Red
    exit 1
}

# Import IIS module
Import-Module WebAdministration -ErrorAction SilentlyContinue

# Stop the application
Write-Host "[1/6] Stopping application..." -ForegroundColor Yellow
try {
    Stop-Website -Name $WebsiteName -ErrorAction SilentlyContinue
    Stop-WebAppPool -Name $AppPoolName -ErrorAction SilentlyContinue
    Write-Host "  - Website stopped" -ForegroundColor Gray
    Write-Host "  - Application pool stopped" -ForegroundColor Gray
} catch {
    Write-Host "  - Warning: Could not stop application (may not be running)" -ForegroundColor Yellow
}

# Wait for processes to stop
Write-Host "  - Waiting for processes to stop..." -ForegroundColor Gray
Start-Sleep -Seconds 5

# Create backup
Write-Host "[2/6] Creating backup..." -ForegroundColor Yellow
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$backupFolder = "$BackupPath\TABWeb_$timestamp"

if (Test-Path $WebsitePath) {
    try {
        New-Item -Path $BackupPath -ItemType Directory -Force | Out-Null
        Copy-Item -Path $WebsitePath -Destination $backupFolder -Recurse -Force
        Write-Host "  - Backup created at: $backupFolder" -ForegroundColor Gray
    } catch {
        Write-Host "  - Warning: Could not create backup: $_" -ForegroundColor Yellow
    }
} else {
    Write-Host "  - No existing installation to backup" -ForegroundColor Gray
}

# Clear old files (but keep appsettings.Production.json if customized)
Write-Host "[3/6] Clearing old files..." -ForegroundColor Yellow
try {
    # Save appsettings.Production.json if it exists
    $productionSettings = "$WebsitePath\appsettings.Production.json"
    $tempSettings = "$env:TEMP\appsettings.Production.json.backup"

    if (Test-Path $productionSettings) {
        Copy-Item $productionSettings $tempSettings -Force
        Write-Host "  - Saved custom production settings" -ForegroundColor Gray
    }

    # Remove old files
    if (Test-Path $WebsitePath) {
        Get-ChildItem $WebsitePath | Remove-Item -Recurse -Force
        Write-Host "  - Old files removed" -ForegroundColor Gray
    } else {
        New-Item -Path $WebsitePath -ItemType Directory -Force | Out-Null
        Write-Host "  - Created website directory" -ForegroundColor Gray
    }
} catch {
    Write-Host "ERROR: Could not clear old files: $_" -ForegroundColor Red
    exit 1
}

# Extract new files
Write-Host "[4/6] Extracting new version..." -ForegroundColor Yellow
try {
    Expand-Archive -Path $ZipPath -DestinationPath $WebsitePath -Force
    Write-Host "  - New files extracted" -ForegroundColor Gray

    # Restore custom production settings if they existed
    if (Test-Path $tempSettings) {
        Copy-Item $tempSettings $productionSettings -Force
        Remove-Item $tempSettings -Force
        Write-Host "  - Restored custom production settings" -ForegroundColor Gray
    }
} catch {
    Write-Host "ERROR: Could not extract files: $_" -ForegroundColor Red

    # Attempt to restore backup
    if (Test-Path $backupFolder) {
        Write-Host "Attempting to restore from backup..." -ForegroundColor Yellow
        Copy-Item -Path "$backupFolder\*" -Destination $WebsitePath -Recurse -Force
    }
    exit 1
}

# Set permissions
Write-Host "[5/6] Setting permissions..." -ForegroundColor Yellow
try {
    $acl = Get-Acl $WebsitePath

    $permission1 = "BUILTIN\IIS_IUSRS","Modify","ContainerInherit,ObjectInherit","None","Allow"
    $rule1 = New-Object System.Security.AccessControl.FileSystemAccessRule $permission1
    $acl.AddAccessRule($rule1)

    $permission2 = "IIS AppPool\$AppPoolName","Modify","ContainerInherit,ObjectInherit","None","Allow"
    $rule2 = New-Object System.Security.AccessControl.FileSystemAccessRule $permission2
    $acl.AddAccessRule($rule2)

    Set-Acl $WebsitePath $acl
    Write-Host "  - Permissions configured" -ForegroundColor Gray
} catch {
    Write-Host "  - Warning: Could not set permissions: $_" -ForegroundColor Yellow
}

# Start the application
Write-Host "[6/6] Starting application..." -ForegroundColor Yellow
try {
    Start-WebAppPool -Name $AppPoolName
    Write-Host "  - Application pool started" -ForegroundColor Gray

    Start-Sleep -Seconds 2

    Start-Website -Name $WebsiteName
    Write-Host "  - Website started" -ForegroundColor Gray
} catch {
    Write-Host "ERROR: Could not start application: $_" -ForegroundColor Red
    exit 1
}

# Verify deployment
Write-Host ""
Write-Host "Verifying deployment..." -ForegroundColor Yellow
Start-Sleep -Seconds 3

$poolState = Get-WebAppPoolState -Name $AppPoolName
$website = Get-Website -Name $WebsiteName

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "DEPLOYMENT SUCCESSFUL!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Status:" -ForegroundColor White
Write-Host "  - Application Pool: $($poolState.Value)" -ForegroundColor White
Write-Host "  - Website State: $($website.State)" -ForegroundColor White
Write-Host "  - Backup Location: $backupFolder" -ForegroundColor White
Write-Host ""
Write-Host "Testing..." -ForegroundColor Cyan
Start-Process "http://localhost"
Write-Host ""
Write-Host "Deployment completed at: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Gray
Write-Host ""
