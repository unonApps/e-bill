# ========================================
# TABWeb - Copy Package to Server
# Run this on your DEVELOPMENT MACHINE
# ========================================

param(
    [string]$LocalZipPath = "C:\Users\dxmic\Desktop\Do Net Template\DoNetTemplate.Web\deploy-to-iis.zip",
    [string]$ServerIP = "10.104.104.78",
    [string]$ServerPath = "C$\ebill",
    [PSCredential]$Credential = $null
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "TABWeb - Copy Package to Server" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Check if ZIP exists
if (-not (Test-Path $LocalZipPath)) {
    Write-Host "ERROR: Deployment package not found at: $LocalZipPath" -ForegroundColor Red
    Write-Host "Please run 1-build-and-package.ps1 first!" -ForegroundColor Yellow
    exit 1
}

# Build remote path
$remotePath = "\\$ServerIP\$ServerPath"

Write-Host "Source: $LocalZipPath" -ForegroundColor Gray
Write-Host "Destination: $remotePath" -ForegroundColor Gray
Write-Host ""

# Test connection to server
Write-Host "Testing connection to server..." -ForegroundColor Yellow
if (-not (Test-NetConnection -ComputerName $ServerIP -Port 445 -InformationLevel Quiet)) {
    Write-Host "ERROR: Cannot connect to server $ServerIP" -ForegroundColor Red
    Write-Host "Please check:" -ForegroundColor Yellow
    Write-Host "  1. Server is online" -ForegroundColor White
    Write-Host "  2. File sharing is enabled" -ForegroundColor White
    Write-Host "  3. Firewall allows SMB traffic (port 445)" -ForegroundColor White
    exit 1
}

# Create remote directory if it doesn't exist
Write-Host "Ensuring remote directory exists..." -ForegroundColor Yellow
try {
    if ($Credential) {
        New-Item -Path $remotePath -ItemType Directory -Force -Credential $Credential | Out-Null
    } else {
        New-Item -Path $remotePath -ItemType Directory -Force | Out-Null
    }
} catch {
    Write-Host "Warning: Could not create remote directory (may already exist)" -ForegroundColor Yellow
}

# Copy file to server
Write-Host "Copying deployment package to server..." -ForegroundColor Yellow
try {
    if ($Credential) {
        Copy-Item -Path $LocalZipPath -Destination $remotePath -Force -Credential $Credential
    } else {
        Copy-Item -Path $LocalZipPath -Destination $remotePath -Force
    }

    Write-Host ""
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "COPY SUCCESSFUL!" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "Next step:" -ForegroundColor Cyan
    Write-Host "  1. RDP to server: $ServerIP" -ForegroundColor White
    Write-Host "  2. Open PowerShell as Administrator" -ForegroundColor White
    Write-Host "  3. Run: cd C:\ebill" -ForegroundColor White
    Write-Host "  4. Run: .\2-deploy-on-server.ps1" -ForegroundColor White
    Write-Host ""
} catch {
    Write-Host "ERROR: Could not copy file to server: $_" -ForegroundColor Red
    Write-Host ""
    Write-Host "Try manually:" -ForegroundColor Yellow
    Write-Host "  1. Open File Explorer" -ForegroundColor White
    Write-Host "  2. Navigate to: $remotePath" -ForegroundColor White
    Write-Host "  3. Copy deploy-to-iis.zip manually" -ForegroundColor White
    exit 1
}
