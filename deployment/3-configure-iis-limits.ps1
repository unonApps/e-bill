# ========================================
# Configure IIS Header Size Limits
# Run this on the SERVER as Administrator
# Required for Azure AD authentication
# ========================================

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Configuring IIS Header Size Limits" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Check if running as Administrator
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Host "ERROR: This script must be run as Administrator!" -ForegroundColor Red
    Write-Host "Right-click PowerShell and select 'Run as Administrator'" -ForegroundColor Yellow
    exit 1
}

# Import WebAdministration module
Import-Module WebAdministration -ErrorAction Stop

Write-Host "[1/5] Configuring IIS request limits..." -ForegroundColor Yellow

# Set limits at server level
Set-WebConfigurationProperty -PSPath 'MACHINE/WEBROOT/APPHOST' -Filter "system.webServer/security/requestFiltering/requestLimits" -Name "maxAllowedContentLength" -Value 104857600
Set-WebConfigurationProperty -PSPath 'MACHINE/WEBROOT/APPHOST' -Filter "system.webServer/security/requestFiltering/requestLimits" -Name "maxQueryString" -Value 32768

Write-Host "  Request limits configured" -ForegroundColor Gray

Write-Host "[2/5] Configuring IIS header limits..." -ForegroundColor Yellow

# Increase header size limits for http.sys
# These are the critical settings for Azure AD
$regPath = "HKLM:\System\CurrentControlSet\Services\HTTP\Parameters"

# MaxFieldLength: Maximum size of each header field (default 16KB, setting to 64KB)
Set-ItemProperty -Path $regPath -Name "MaxFieldLength" -Value 65536 -Type DWord -Force
Write-Host "  MaxFieldLength set to 64KB" -ForegroundColor Gray

# MaxRequestBytes: Maximum size of request line and headers combined (default 16KB, setting to 256KB)
Set-ItemProperty -Path $regPath -Name "MaxRequestBytes" -Value 262144 -Type DWord -Force
Write-Host "  MaxRequestBytes set to 256KB" -ForegroundColor Gray

Write-Host "[3/5] Verifying TABWeb site configuration..." -ForegroundColor Yellow

# Configure TABWeb site specifically
$siteName = "TABWeb"
$sitePath = "IIS:\Sites\$siteName"

if (Test-Path $sitePath) {
    Write-Host "  TABWeb site found" -ForegroundColor Gray

    # Set site-specific limits
    Set-WebConfigurationProperty -PSPath "MACHINE/WEBROOT/APPHOST/$siteName" -Filter "system.webServer/security/requestFiltering/requestLimits" -Name "maxAllowedContentLength" -Value 104857600
    Write-Host "  Site-specific limits configured" -ForegroundColor Gray
} else {
    Write-Host "  WARNING: TABWeb site not found in IIS" -ForegroundColor Yellow
}

Write-Host "[4/5] Configuring application pool..." -ForegroundColor Yellow

# Configure TABWeb application pool
$appPoolName = "TABWeb"
if (Test-Path "IIS:\AppPools\$appPoolName") {
    # Increase queue length for high load
    Set-ItemProperty "IIS:\AppPools\$appPoolName" -Name queueLength -Value 5000
    Write-Host "  Application pool queue length increased" -ForegroundColor Gray
} else {
    Write-Host "  WARNING: TABWeb application pool not found" -ForegroundColor Yellow
}

Write-Host "[5/5] Restarting HTTP service..." -ForegroundColor Yellow

# Restart HTTP service to apply http.sys changes
Write-Host "  Stopping HTTP service..." -ForegroundColor Gray
net stop http /y

Write-Host "  Starting HTTP service..." -ForegroundColor Gray
net start http

# Restart dependent services
Write-Host "  Restarting W3SVC..." -ForegroundColor Gray
net start w3svc

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "IIS CONFIGURATION COMPLETE!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Changes applied:" -ForegroundColor White
Write-Host "  - Max header field length: 64 KB" -ForegroundColor White
Write-Host "  - Max request size: 256 KB" -ForegroundColor White
Write-Host "  - Content length limit: 100 MB" -ForegroundColor White
Write-Host "  - Query string limit: 32 KB" -ForegroundColor White
Write-Host ""
Write-Host "IMPORTANT: The server has been restarted." -ForegroundColor Cyan
Write-Host "Please test Azure AD login at: https://ebill-dev.unon.org" -ForegroundColor Cyan
Write-Host ""
