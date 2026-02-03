# ========================================
# Sync Database Schema to Match Migrations
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

Write-Host "========================================" -ForegroundColor Red
Write-Host "DATABASE SCHEMA SYNC" -ForegroundColor Red
Write-Host "========================================" -ForegroundColor Red
Write-Host ""
Write-Host "WARNING: This will drop and recreate the database!" -ForegroundColor Yellow
Write-Host ""
Write-Host "Target Server: $ServerName" -ForegroundColor White
Write-Host "Target Database: $DatabaseName" -ForegroundColor White
Write-Host ""

# Build connection string
if ($UseWindowsAuth) {
    $connectionString = "Server=$ServerName;Database=master;Integrated Security=True;TrustServerCertificate=True;"
    $dbConnectionString = "Server=$ServerName;Database=$DatabaseName;Integrated Security=True;TrustServerCertificate=True;"
} else {
    if ([string]::IsNullOrEmpty($Username) -or [string]::IsNullOrEmpty($Password)) {
        Write-Host "ERROR: Username and Password required!" -ForegroundColor Red
        exit 1
    }
    $connectionString = "Server=$ServerName;Database=master;User Id=$Username;Password=$Password;TrustServerCertificate=True;"
    $dbConnectionString = "Server=$ServerName;Database=$DatabaseName;User Id=$Username;Password=$Password;TrustServerCertificate=True;"
}

Write-Host "========================================" -ForegroundColor Yellow
Write-Host "BACKUP REMINDER" -ForegroundColor Yellow
Write-Host "========================================" -ForegroundColor Yellow
Write-Host ""
Write-Host "Have you backed up your data?" -ForegroundColor Red
Write-Host ""
Write-Host "This process will:" -ForegroundColor Cyan
Write-Host "  1. Drop the existing database (ALL DATA LOST!)" -ForegroundColor White
Write-Host "  2. Create fresh database from migrations" -ForegroundColor White
Write-Host "  3. Schema will match your local database exactly" -ForegroundColor White
Write-Host ""

$response = Read-Host "Type 'DELETE DATABASE' to continue"
if ($response -ne "DELETE DATABASE") {
    Write-Host "Cancelled" -ForegroundColor Yellow
    exit 0
}

Write-Host ""
Write-Host "[1/3] Dropping existing database..." -ForegroundColor Yellow

# Drop database
$dropSql = @"
IF EXISTS (SELECT name FROM sys.databases WHERE name = N'$DatabaseName')
BEGIN
    ALTER DATABASE [$DatabaseName] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [$DatabaseName];
    PRINT 'Database dropped';
END
ELSE
BEGIN
    PRINT 'Database does not exist';
END
"@

try {
    # Use sqlcmd if available, otherwise use Invoke-Sqlcmd
    $sqlcmdPath = Get-Command sqlcmd -ErrorAction SilentlyContinue
    if ($sqlcmdPath) {
        $dropSql | sqlcmd -S $ServerName -d master -U $Username -P $Password
    } else {
        Invoke-Sqlcmd -ConnectionString $connectionString -Query $dropSql
    }
    Write-Host "  Database dropped successfully" -ForegroundColor Green
} catch {
    Write-Host "  Error dropping database: $_" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "[2/3] Creating fresh database..." -ForegroundColor Yellow

# Create database
$createSql = "CREATE DATABASE [$DatabaseName];"
try {
    if ($sqlcmdPath) {
        $createSql | sqlcmd -S $ServerName -d master -U $Username -P $Password
    } else {
        Invoke-Sqlcmd -ConnectionString $connectionString -Query $createSql
    }
    Write-Host "  Database created successfully" -ForegroundColor Green
} catch {
    Write-Host "  Error creating database: $_" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "[3/3] Applying all migrations..." -ForegroundColor Yellow

# Apply migrations
cd "C:\Users\dxmic\Desktop\Do Net Template\DoNetTemplate.Web"
dotnet ef database update --connection $dbConnectionString

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "DATABASE SYNCED SUCCESSFULLY!" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "Server database now matches local schema exactly!" -ForegroundColor White
    Write-Host ""
} else {
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Red
    Write-Host "MIGRATION FAILED!" -ForegroundColor Red
    Write-Host "========================================" -ForegroundColor Red
    Write-Host ""
    exit 1
}
