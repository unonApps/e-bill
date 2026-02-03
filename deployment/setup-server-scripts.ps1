# ========================================
# Copy Deployment Scripts to Server
# Run this ONCE to setup scripts on server
# ========================================

param(
    [string]$ServerIP = "10.104.104.78",
    [string]$ServerScriptPath = "C$\ebill",
    [PSCredential]$Credential = $null
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Setup Deployment Scripts on Server" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$localPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$remotePath = "\\$ServerIP\$ServerScriptPath"

# Scripts to copy
$scriptsToServer = @(
    "2-deploy-on-server.ps1",
    "ROLLBACK.ps1"
)

Write-Host "Copying scripts to: $remotePath" -ForegroundColor Yellow
Write-Host ""

# Create remote directory
try {
    New-Item -Path $remotePath -ItemType Directory -Force | Out-Null
} catch {
    Write-Host "Warning: Could not create directory (may already exist)" -ForegroundColor Yellow
}

# Copy each script
foreach ($script in $scriptsToServer) {
    $sourcePath = Join-Path $localPath $script
    $destPath = Join-Path $remotePath $script

    if (Test-Path $sourcePath) {
        try {
            if ($Credential) {
                Copy-Item -Path $sourcePath -Destination $destPath -Force -Credential $Credential
            } else {
                Copy-Item -Path $sourcePath -Destination $destPath -Force
            }
            Write-Host "  ✓ Copied: $script" -ForegroundColor Green
        } catch {
            Write-Host "  ✗ Failed: $script - $_" -ForegroundColor Red
        }
    } else {
        Write-Host "  ✗ Not found: $script" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "Setup complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Scripts are now available on the server at:" -ForegroundColor White
Write-Host "  $remotePath" -ForegroundColor Cyan
Write-Host ""
