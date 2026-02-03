# ========================================
# TABWeb - Complete Deployment Script
# Run this on your DEVELOPMENT MACHINE
# This runs all steps automatically
# ========================================

param(
    [string]$ServerIP = "10.104.104.78",
    [PSCredential]$Credential = $null,
    [switch]$SkipCopy = $false
)

$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "TABWeb - COMPLETE DEPLOYMENT" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path

# Step 1: Build and Package
Write-Host "STEP 1: Building and packaging..." -ForegroundColor Magenta
& "$scriptPath\1-build-and-package.ps1"
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Build failed!" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Step 2: Copy to Server (if not skipped)
if (-not $SkipCopy) {
    Write-Host "STEP 2: Copying to server..." -ForegroundColor Magenta

    if ($Credential) {
        & "$scriptPath\3-copy-to-server.ps1" -ServerIP $ServerIP -Credential $Credential
    } else {
        & "$scriptPath\3-copy-to-server.ps1" -ServerIP $ServerIP
    }

    if ($LASTEXITCODE -ne 0) {
        Write-Host "ERROR: Copy failed!" -ForegroundColor Red
        exit 1
    }

    Write-Host ""
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host ""
}

# Step 3: Instructions for server deployment
Write-Host "FINAL STEP: Deploy on Server" -ForegroundColor Magenta
Write-Host ""
Write-Host "The package is ready on the server." -ForegroundColor Green
Write-Host ""
Write-Host "To complete deployment:" -ForegroundColor Yellow
Write-Host "  1. RDP to: $ServerIP" -ForegroundColor White
Write-Host "  2. Open PowerShell as Administrator" -ForegroundColor White
Write-Host "  3. Run:" -ForegroundColor White
Write-Host "     cd C:\ebill" -ForegroundColor Cyan
Write-Host "     .\2-deploy-on-server.ps1" -ForegroundColor Cyan
Write-Host ""
Write-Host "Or run remotely (if PSRemoting enabled):" -ForegroundColor Yellow
Write-Host "  Invoke-Command -ComputerName $ServerIP -FilePath '$scriptPath\2-deploy-on-server.ps1'" -ForegroundColor White
Write-Host ""
