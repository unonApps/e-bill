# ========================================
# TABWeb - Build and Package Script
# Run this on your DEVELOPMENT MACHINE
# ========================================

param(
    [string]$ProjectPath = "C:\Users\dxmic\Desktop\2026 Development\1 ebill\DoNetTemplate.Web"
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "TABWeb Deployment - Build and Package" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Navigate to project directory
Write-Host "[1/6] Navigating to project directory..." -ForegroundColor Yellow
cd $ProjectPath

# Remove old publish directory to avoid nested publish folders
Write-Host "[2/6] Removing old publish directory..." -ForegroundColor Yellow
if (Test-Path "publish") {
    Remove-Item -Path "publish" -Recurse -Force
    Write-Host "  Old publish directory removed" -ForegroundColor Gray
}

# Clean previous builds
Write-Host "[3/6] Cleaning previous builds..." -ForegroundColor Yellow
dotnet clean --configuration Release
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Clean failed!" -ForegroundColor Red
    exit 1
}

# Build the project
Write-Host "[4/6] Building project in Release mode..." -ForegroundColor Yellow
dotnet build --configuration Release
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Build failed!" -ForegroundColor Red
    exit 1
}

# Publish the application
Write-Host "[5/7] Publishing application..." -ForegroundColor Yellow
dotnet publish --configuration Release --output ./publish --runtime win-x64 --self-contained true
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Publish failed!" -ForegroundColor Red
    exit 1
}

# Note about database migrations
Write-Host "[6/7] Preparing deployment notes..." -ForegroundColor Yellow
Write-Host "  Database migrations will be applied using dotnet ef" -ForegroundColor Gray

# Create deployment package
Write-Host "[7/7] Creating deployment package..." -ForegroundColor Yellow
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$zipName = "deploy-to-iis.zip"
$zipNameWithTimestamp = "deploy-to-iis_$timestamp.zip"

# Remove old zip if exists
if (Test-Path $zipName) {
    Remove-Item $zipName -Force
}

# Create new zip
Compress-Archive -Path "publish\*" -DestinationPath $zipName -Force

# Also create timestamped backup
Compress-Archive -Path "publish\*" -DestinationPath $zipNameWithTimestamp -Force

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "BUILD SUCCESSFUL!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Package created:" -ForegroundColor White
Write-Host "  - $ProjectPath\$zipName" -ForegroundColor White
Write-Host "  - $ProjectPath\$zipNameWithTimestamp" -ForegroundColor White
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host ""
Write-Host "  [STEP 1] Apply Database Migrations (from dev machine):" -ForegroundColor Yellow
Write-Host "    cd deployment" -ForegroundColor Gray
Write-Host "    .\apply-migrations-to-server.ps1 -ServerName 'SERVER' -DatabaseName 'tabdb' -Username 'USER' -Password 'PASS'" -ForegroundColor Gray
Write-Host ""
Write-Host "  [STEP 2] Deploy Application (on server):" -ForegroundColor Yellow
Write-Host "    1. Copy $zipName to C:\ebill\ on the server" -ForegroundColor Gray
Write-Host "    2. Run C:\ebill\2-deploy-on-server.ps1 (as Administrator)" -ForegroundColor Gray
Write-Host ""
Write-Host "IMPORTANT: Always apply migrations (Step 1) BEFORE deploying code (Step 2)!" -ForegroundColor Red
Write-Host ""
