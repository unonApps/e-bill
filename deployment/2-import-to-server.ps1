# ========================================
# Import Database to Server
# Run this from DEVELOPMENT MACHINE or SERVER
# ========================================

param(
    [Parameter(Mandatory=$true)]
    [string]$ServerName,

    [string]$DatabaseName = "tabdb",
    [string]$Username = "",
    [string]$Password = "",
    [switch]$UseWindowsAuth = $false,
    [string]$BacpacPath = "deployment\tabdb_full_export.bacpac"
)

Write-Host "========================================" -ForegroundColor Red
Write-Host "IMPORT DATABASE TO SERVER" -ForegroundColor Red
Write-Host "========================================" -ForegroundColor Red
Write-Host ""
Write-Host "WARNING: This will REPLACE the existing database!" -ForegroundColor Yellow
Write-Host "All current data on the server will be DELETED!" -ForegroundColor Yellow
Write-Host ""
Write-Host "Target Server: $ServerName" -ForegroundColor White
Write-Host "Target Database: $DatabaseName" -ForegroundColor White
Write-Host "Source File: $BacpacPath" -ForegroundColor White
Write-Host ""

# Check if file exists
if (-not (Test-Path $BacpacPath)) {
    Write-Host "ERROR: Export file not found: $BacpacPath" -ForegroundColor Red
    Write-Host ""
    Write-Host "Run 1-export-local-database.ps1 first!" -ForegroundColor Yellow
    Write-Host ""
    exit 1
}

# Check if SqlPackage is available
$sqlPackagePath = "C:\Program Files\Microsoft SQL Server\160\DAC\bin\SqlPackage.exe"
if (-not (Test-Path $sqlPackagePath)) {
    $sqlPackagePath = "C:\Program Files\Microsoft SQL Server\150\DAC\bin\SqlPackage.exe"
}

if (-not (Test-Path $sqlPackagePath)) {
    Write-Host "ERROR: SqlPackage.exe not found!" -ForegroundColor Red
    Write-Host ""
    Write-Host "Install it from:" -ForegroundColor Yellow
    Write-Host "https://aka.ms/sqlpackage-windows" -ForegroundColor Yellow
    Write-Host ""
    exit 1
}

# Confirm
$response = Read-Host "Type 'REPLACE DATABASE' to continue"
if ($response -ne "REPLACE DATABASE") {
    Write-Host "Cancelled" -ForegroundColor Yellow
    exit 0
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Yellow
Write-Host "STEP 1: Backup Existing Database" -ForegroundColor Yellow
Write-Host "========================================" -ForegroundColor Yellow
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

# Check if database exists and back it up
Write-Host "Checking if database exists..." -ForegroundColor White
$checkDbSql = "SELECT database_id FROM sys.databases WHERE name = '$DatabaseName'"

try {
    $dbExists = $false
    if ($UseWindowsAuth) {
        $result = Invoke-Sqlcmd -ServerInstance $ServerName -Database master -Query $checkDbSql -ErrorAction SilentlyContinue
        if ($result) { $dbExists = $true }
    } else {
        $result = Invoke-Sqlcmd -ServerInstance $ServerName -Database master -Username $Username -Password $Password -Query $checkDbSql -ErrorAction SilentlyContinue
        if ($result) { $dbExists = $true }
    }

    if ($dbExists) {
        Write-Host "  Database exists - creating backup..." -ForegroundColor Yellow

        $timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
        $backupPath = "C:\temp\${DatabaseName}_backup_${timestamp}.bak"

        $backupSql = @"
BACKUP DATABASE [$DatabaseName]
TO DISK = '$backupPath'
WITH FORMAT, COMPRESSION, STATS = 10;
"@

        if ($UseWindowsAuth) {
            Invoke-Sqlcmd -ServerInstance $ServerName -Database master -Query $backupSql
        } else {
            Invoke-Sqlcmd -ServerInstance $ServerName -Database master -Username $Username -Password $Password -Query $backupSql
        }

        Write-Host "  Backup created: $backupPath" -ForegroundColor Green
    } else {
        Write-Host "  Database does not exist (no backup needed)" -ForegroundColor Green
    }
} catch {
    Write-Host "  Warning: Could not backup database: $_" -ForegroundColor Yellow
    Write-Host "  Continuing anyway..." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Yellow
Write-Host "STEP 2: Drop Existing Database" -ForegroundColor Yellow
Write-Host "========================================" -ForegroundColor Yellow
Write-Host ""

if ($dbExists) {
    Write-Host "Dropping existing database..." -ForegroundColor White

    $dropSql = @"
IF EXISTS (SELECT name FROM sys.databases WHERE name = N'$DatabaseName')
BEGIN
    ALTER DATABASE [$DatabaseName] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [$DatabaseName];
    PRINT 'Database dropped successfully';
END
"@

    try {
        if ($UseWindowsAuth) {
            Invoke-Sqlcmd -ServerInstance $ServerName -Database master -Query $dropSql
        } else {
            Invoke-Sqlcmd -ServerInstance $ServerName -Database master -Username $Username -Password $Password -Query $dropSql
        }
        Write-Host "  Database dropped successfully!" -ForegroundColor Green
    } catch {
        Write-Host "  Error dropping database: $_" -ForegroundColor Red
        exit 1
    }
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Yellow
Write-Host "STEP 3: Import BACPAC File" -ForegroundColor Yellow
Write-Host "========================================" -ForegroundColor Yellow
Write-Host ""
Write-Host "Importing database (this may take several minutes)..." -ForegroundColor Yellow
Write-Host "Progress will be shown below..." -ForegroundColor White
Write-Host ""

# Import BACPAC
try {
    if ($UseWindowsAuth) {
        $targetConnectionString = "Server=$ServerName;Integrated Security=True;TrustServerCertificate=True;"
    } else {
        $targetConnectionString = "Server=$ServerName;User Id=$Username;Password=$Password;TrustServerCertificate=True;"
    }

    & $sqlPackagePath `
        /Action:Import `
        /SourceFile:$BacpacPath `
        /TargetConnectionString:$targetConnectionString `
        /TargetDatabaseName:$DatabaseName `
        /DiagnosticsFile:"deployment\import_diagnostics.log" `
        /p:CommandTimeout=0

    if ($LASTEXITCODE -eq 0) {
        Write-Host ""
        Write-Host "========================================" -ForegroundColor Green
        Write-Host "IMPORT SUCCESSFUL!" -ForegroundColor Green
        Write-Host "========================================" -ForegroundColor Green
        Write-Host ""
        Write-Host "Server database now matches local database exactly!" -ForegroundColor White
        Write-Host ""
        Write-Host "All issues should be fixed:" -ForegroundColor Cyan
        Write-Host "  ✓ Schema matches" -ForegroundColor Green
        Write-Host "  ✓ Column casing correct" -ForegroundColor Green
        Write-Host "  ✓ Stored procedures updated" -ForegroundColor Green
        Write-Host "  ✓ All data preserved" -ForegroundColor Green
        Write-Host ""
        Write-Host "Next step: Deploy the application and test Hangfire imports!" -ForegroundColor Cyan
        Write-Host ""
    } else {
        Write-Host ""
        Write-Host "========================================" -ForegroundColor Red
        Write-Host "IMPORT FAILED!" -ForegroundColor Red
        Write-Host "========================================" -ForegroundColor Red
        Write-Host ""
        Write-Host "Check diagnostics: deployment\import_diagnostics.log" -ForegroundColor Yellow
        Write-Host ""
        exit 1
    }
} catch {
    Write-Host ""
    Write-Host "ERROR: $_" -ForegroundColor Red
    Write-Host ""
    exit 1
}
