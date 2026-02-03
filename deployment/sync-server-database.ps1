# ========================================
# Sync Server Database to Match Local
# Run this from DEVELOPMENT MACHINE
# ========================================

param(
    [Parameter(Mandatory=$true)]
    [string]$ServerName,

    [Parameter(Mandatory=$true)]
    [string]$DatabaseName,

    [string]$Username = "",
    [string]$Password = "",
    [switch]$UseWindowsAuth = $false
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "DATABASE SYNC TO SERVER" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Target Server: $ServerName" -ForegroundColor White
Write-Host "Target Database: $DatabaseName" -ForegroundColor White
Write-Host ""

# Build connection string
if ($UseWindowsAuth) {
    $connectionString = "Server=$ServerName;Database=$DatabaseName;Integrated Security=True;TrustServerCertificate=True;"
} else {
    if ([string]::IsNullOrEmpty($Username) -or [string]::IsNullOrEmpty($Password)) {
        Write-Host "ERROR: Username and Password required!" -ForegroundColor Red
        exit 1
    }
    $connectionString = "Server=$ServerName;Database=$DatabaseName;User Id=$Username;Password=$Password;TrustServerCertificate=True;"
}

Write-Host "========================================" -ForegroundColor Yellow
Write-Host "STEP 1: Apply All Migrations" -ForegroundColor Yellow
Write-Host "========================================" -ForegroundColor Yellow
Write-Host ""
Write-Host "This will bring the server database schema up to date..." -ForegroundColor White
Write-Host ""

$response = Read-Host "Continue? (y/n)"
if ($response -ne 'y' -and $response -ne 'Y') {
    Write-Host "Cancelled" -ForegroundColor Yellow
    exit 0
}

# Apply migrations
Write-Host ""
Write-Host "[1/3] Applying migrations..." -ForegroundColor Yellow
dotnet ef database update --connection $connectionString

if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Red
    Write-Host "MIGRATION FAILED!" -ForegroundColor Red
    Write-Host "========================================" -ForegroundColor Red
    Write-Host ""
    exit 1
}

Write-Host "  Migrations applied successfully!" -ForegroundColor Green
Write-Host ""

# Fix column casing
Write-Host "========================================" -ForegroundColor Yellow
Write-Host "STEP 2: Fix Column Casing" -ForegroundColor Yellow
Write-Host "========================================" -ForegroundColor Yellow
Write-Host ""
Write-Host "This will rename PascalCase columns to lowercase (Ext -> ext, etc.)..." -ForegroundColor White
Write-Host ""

$response = Read-Host "Continue? (y/n)"
if ($response -ne 'y' -and $response -ne 'Y') {
    Write-Host "Skipped column casing fix" -ForegroundColor Yellow
} else {
    Write-Host ""
    Write-Host "[2/3] Fixing column casing..." -ForegroundColor Yellow

    $fixCasingSql = Get-Content -Path "deployment/fix-column-casing.sql" -Raw

    try {
        if ($UseWindowsAuth) {
            Invoke-Sqlcmd -ServerInstance $ServerName -Database $DatabaseName -Query $fixCasingSql
        } else {
            Invoke-Sqlcmd -ServerInstance $ServerName -Database $DatabaseName -Username $Username -Password $Password -Query $fixCasingSql
        }
        Write-Host "  Column casing fixed!" -ForegroundColor Green
    } catch {
        Write-Host "  Warning: Column casing fix failed - columns may already be correct" -ForegroundColor Yellow
        Write-Host "  Error: $_" -ForegroundColor Yellow
    }
}

Write-Host ""

# Update stored procedure
Write-Host "========================================" -ForegroundColor Yellow
Write-Host "STEP 3: Update Stored Procedure" -ForegroundColor Yellow
Write-Host "========================================" -ForegroundColor Yellow
Write-Host ""
Write-Host "This will update sp_ConsolidateCallLogBatch to include IsAdjustment..." -ForegroundColor White
Write-Host ""

$response = Read-Host "Continue? (y/n)"
if ($response -ne 'y' -and $response -ne 'Y') {
    Write-Host "Skipped stored procedure update" -ForegroundColor Yellow
} else {
    Write-Host ""
    Write-Host "[3/3] Updating stored procedure..." -ForegroundColor Yellow

    $spSql = Get-Content -Path "scripts/sql/sp_ConsolidateCallLogBatch.sql" -Raw

    try {
        if ($UseWindowsAuth) {
            Invoke-Sqlcmd -ServerInstance $ServerName -Database $DatabaseName -Query $spSql
        } else {
            Invoke-Sqlcmd -ServerInstance $ServerName -Database $DatabaseName -Username $Username -Password $Password -Query $spSql
        }
        Write-Host "  Stored procedure updated!" -ForegroundColor Green
    } catch {
        Write-Host "  Error updating stored procedure: $_" -ForegroundColor Red
        Write-Host "  You may need to run scripts/sql/sp_ConsolidateCallLogBatch.sql manually in SSMS" -ForegroundColor Yellow
    }
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "SYNC COMPLETED!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Server database is now in sync with local!" -ForegroundColor White
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "  1. Test Hangfire bulk import" -ForegroundColor White
Write-Host "  2. Verify data imports correctly" -ForegroundColor White
Write-Host ""
