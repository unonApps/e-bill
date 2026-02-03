# ========================================
# TABWeb - IIS Setup Script
# Run this ONCE on the server to create IIS site
# ========================================

param(
    [string]$WebsiteName = "TABWeb",
    [string]$AppPoolName = "TABWeb_AppPool",
    [string]$WebsitePath = "C:\inetpub\wwwroot\TABWeb",
    [int]$Port = 80,
    [string]$HostHeader = ""
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "TABWeb - IIS Setup" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Check if running as Administrator
$currentPrincipal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
if (-not $currentPrincipal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Host "ERROR: This script must be run as Administrator!" -ForegroundColor Red
    exit 1
}

# Import IIS module
Write-Host "[1/5] Loading IIS module..." -ForegroundColor Yellow
try {
    Import-Module WebAdministration -ErrorAction Stop
    Write-Host "  - IIS module loaded" -ForegroundColor Gray
} catch {
    Write-Host "ERROR: IIS is not installed or module cannot be loaded!" -ForegroundColor Red
    Write-Host "Install IIS using: Install-WindowsFeature -name Web-Server -IncludeManagementTools" -ForegroundColor Yellow
    exit 1
}

# Create website directory
Write-Host "[2/5] Creating website directory..." -ForegroundColor Yellow
try {
    if (-not (Test-Path $WebsitePath)) {
        New-Item -Path $WebsitePath -ItemType Directory -Force | Out-Null
        Write-Host "  - Directory created: $WebsitePath" -ForegroundColor Gray
    } else {
        Write-Host "  - Directory already exists: $WebsitePath" -ForegroundColor Gray
    }
} catch {
    Write-Host "ERROR: Could not create directory: $_" -ForegroundColor Red
    exit 1
}

# Create Application Pool
Write-Host "[3/5] Creating application pool..." -ForegroundColor Yellow
try {
    if (Test-Path "IIS:\AppPools\$AppPoolName") {
        Write-Host "  - Application pool already exists: $AppPoolName" -ForegroundColor Gray
    } else {
        New-WebAppPool -Name $AppPoolName
        Write-Host "  - Application pool created: $AppPoolName" -ForegroundColor Gray
    }

    # Configure app pool settings
    Set-ItemProperty "IIS:\AppPools\$AppPoolName" -name "managedRuntimeVersion" -value ""
    Set-ItemProperty "IIS:\AppPools\$AppPoolName" -name "processModel.identityType" -value "ApplicationPoolIdentity"
    Write-Host "  - Configured for .NET Core (No Managed Code)" -ForegroundColor Gray
} catch {
    Write-Host "ERROR: Could not create application pool: $_" -ForegroundColor Red
    exit 1
}

# Create Website
Write-Host "[4/5] Creating website..." -ForegroundColor Yellow
try {
    if (Test-Path "IIS:\Sites\$WebsiteName") {
        Write-Host "  - Website already exists: $WebsiteName" -ForegroundColor Yellow
        Write-Host "  - Updating configuration..." -ForegroundColor Gray

        # Update existing site
        Set-ItemProperty "IIS:\Sites\$WebsiteName" -name physicalPath -value $WebsitePath
        Set-ItemProperty "IIS:\Sites\$WebsiteName" -name applicationPool -value $AppPoolName
    } else {
        # Create new site
        if ([string]::IsNullOrEmpty($HostHeader)) {
            New-Website -Name $WebsiteName `
                        -PhysicalPath $WebsitePath `
                        -ApplicationPool $AppPoolName `
                        -Port $Port
        } else {
            New-Website -Name $WebsiteName `
                        -PhysicalPath $WebsitePath `
                        -ApplicationPool $AppPoolName `
                        -Port $Port `
                        -HostHeader $HostHeader
        }
        Write-Host "  - Website created: $WebsiteName" -ForegroundColor Gray
    }

    Write-Host "  - Binding: http://*:$Port" -ForegroundColor Gray
} catch {
    Write-Host "ERROR: Could not create website: $_" -ForegroundColor Red
    exit 1
}

# Set Permissions
Write-Host "[5/5] Setting permissions..." -ForegroundColor Yellow
try {
    $acl = Get-Acl $WebsitePath

    # IIS_IUSRS
    $permission1 = "BUILTIN\IIS_IUSRS","Modify","ContainerInherit,ObjectInherit","None","Allow"
    $rule1 = New-Object System.Security.AccessControl.FileSystemAccessRule $permission1
    $acl.AddAccessRule($rule1)

    # App Pool Identity
    $permission2 = "IIS AppPool\$AppPoolName","Modify","ContainerInherit,ObjectInherit","None","Allow"
    $rule2 = New-Object System.Security.AccessControl.FileSystemAccessRule $permission2
    $acl.AddAccessRule($rule2)

    Set-Acl $WebsitePath $acl
    Write-Host "  - Permissions configured" -ForegroundColor Gray
} catch {
    Write-Host "  - Warning: Could not set permissions: $_" -ForegroundColor Yellow
}

# Display status
Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "IIS SETUP COMPLETE!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Configuration:" -ForegroundColor White
Write-Host "  - Website Name: $WebsiteName" -ForegroundColor White
Write-Host "  - App Pool: $AppPoolName" -ForegroundColor White
Write-Host "  - Physical Path: $WebsitePath" -ForegroundColor White
Write-Host "  - Port: $Port" -ForegroundColor White
Write-Host "  - URL: http://localhost:$Port" -ForegroundColor Cyan
Write-Host ""

# Check current status
$poolState = Get-WebAppPoolState -Name $AppPoolName
$websiteState = Get-Website -Name $WebsiteName

Write-Host "Current Status:" -ForegroundColor White
Write-Host "  - App Pool State: $($poolState.Value)" -ForegroundColor White
Write-Host "  - Website State: $($websiteState.State)" -ForegroundColor White
Write-Host ""

Write-Host "Next Steps:" -ForegroundColor Cyan
Write-Host "  1. Deploy your application using: .\2-deploy-on-server.ps1" -ForegroundColor Gray
Write-Host "  2. Or copy files to: $WebsitePath" -ForegroundColor Gray
Write-Host "  3. Start the site: Start-Website -Name '$WebsiteName'" -ForegroundColor Gray
Write-Host ""
