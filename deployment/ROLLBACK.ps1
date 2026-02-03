# ========================================
# TABWeb - Rollback to Previous Version
# Run this on the SERVER (10.104.104.78)
# ========================================

param(
    [string]$BackupPath = "C:\inetpub\backups",
    [string]$WebsitePath = "C:\inetpub\wwwroot\TABWeb",
    [string]$WebsiteName = "TABWeb",
    [string]$AppPoolName = "TABWeb_AppPool",
    [int]$BackupIndex = 1  # 1 = latest, 2 = second latest, etc.
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "TABWeb - ROLLBACK" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Check if running as Administrator
$currentPrincipal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
if (-not $currentPrincipal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Host "ERROR: This script must be run as Administrator!" -ForegroundColor Red
    exit 1
}

# Import IIS module
Import-Module WebAdministration -ErrorAction SilentlyContinue

# Find available backups
Write-Host "Finding available backups..." -ForegroundColor Yellow
if (-not (Test-Path $BackupPath)) {
    Write-Host "ERROR: No backups found at $BackupPath" -ForegroundColor Red
    exit 1
}

$backups = Get-ChildItem $BackupPath | Where-Object { $_.PSIsContainer } | Sort-Object LastWriteTime -Descending

if ($backups.Count -eq 0) {
    Write-Host "ERROR: No backups found!" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Available backups:" -ForegroundColor Cyan
for ($i = 0; $i -lt $backups.Count; $i++) {
    $backup = $backups[$i]
    $marker = if ($i + 1 -eq $BackupIndex) { " <-- SELECTED" } else { "" }
    Write-Host "  [$($i + 1)] $($backup.Name) - $($backup.LastWriteTime)$marker" -ForegroundColor White
}
Write-Host ""

if ($BackupIndex -gt $backups.Count) {
    Write-Host "ERROR: Backup index $BackupIndex not found. Only $($backups.Count) backups available." -ForegroundColor Red
    exit 1
}

$selectedBackup = $backups[$BackupIndex - 1]
Write-Host "Rolling back to: $($selectedBackup.Name)" -ForegroundColor Yellow
Write-Host ""

# Confirm rollback
Write-Host "WARNING: This will replace the current version with the backup!" -ForegroundColor Yellow
$confirm = Read-Host "Type 'YES' to continue"
if ($confirm -ne "YES") {
    Write-Host "Rollback cancelled." -ForegroundColor Gray
    exit 0
}

Write-Host ""

# Stop the application
Write-Host "[1/5] Stopping application..." -ForegroundColor Yellow
try {
    Stop-Website -Name $WebsiteName -ErrorAction SilentlyContinue
    Stop-WebAppPool -Name $AppPoolName -ErrorAction SilentlyContinue
    Write-Host "  - Application stopped" -ForegroundColor Gray
} catch {
    Write-Host "  - Warning: Could not stop application: $_" -ForegroundColor Yellow
}

Start-Sleep -Seconds 5

# Create a backup of current version before rollback
Write-Host "[2/5] Creating backup of current version..." -ForegroundColor Yellow
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$preRollbackBackup = "$BackupPath\TABWeb_PreRollback_$timestamp"
try {
    Copy-Item -Path $WebsitePath -Destination $preRollbackBackup -Recurse -Force
    Write-Host "  - Current version backed up to: $preRollbackBackup" -ForegroundColor Gray
} catch {
    Write-Host "  - Warning: Could not backup current version: $_" -ForegroundColor Yellow
}

# Clear current files
Write-Host "[3/5] Clearing current files..." -ForegroundColor Yellow
try {
    Get-ChildItem $WebsitePath | Remove-Item -Recurse -Force
    Write-Host "  - Current files removed" -ForegroundColor Gray
} catch {
    Write-Host "ERROR: Could not clear current files: $_" -ForegroundColor Red
    exit 1
}

# Restore from backup
Write-Host "[4/5] Restoring from backup..." -ForegroundColor Yellow
try {
    Copy-Item -Path "$($selectedBackup.FullName)\*" -Destination $WebsitePath -Recurse -Force
    Write-Host "  - Files restored from backup" -ForegroundColor Gray
} catch {
    Write-Host "ERROR: Could not restore from backup: $_" -ForegroundColor Red
    exit 1
}

# Start the application
Write-Host "[5/5] Starting application..." -ForegroundColor Yellow
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

# Verify
Write-Host ""
Write-Host "Verifying rollback..." -ForegroundColor Yellow
Start-Sleep -Seconds 3

$poolState = Get-WebAppPoolState -Name $AppPoolName
$website = Get-Website -Name $WebsiteName

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "ROLLBACK SUCCESSFUL!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Restored from: $($selectedBackup.Name)" -ForegroundColor White
Write-Host "Application Pool: $($poolState.Value)" -ForegroundColor White
Write-Host "Website State: $($website.State)" -ForegroundColor White
Write-Host "Pre-rollback backup: $preRollbackBackup" -ForegroundColor White
Write-Host ""
Write-Host "Testing..." -ForegroundColor Cyan
Start-Process "http://localhost"
Write-Host ""
