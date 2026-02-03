# ========================================
# Apply Migrations Directly to Server
# Run this on SERVER or DEV MACHINE
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
Write-Host "Apply Migrations to Server Database" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "Target Server: $ServerName" -ForegroundColor White
Write-Host "Target Database: $DatabaseName" -ForegroundColor White
Write-Host ""

# Build connection string
if ($UseWindowsAuth) {
    $connectionString = "Server=$ServerName;Database=$DatabaseName;Integrated Security=True;TrustServerCertificate=True;"
    Write-Host "Authentication: Windows" -ForegroundColor Gray
} else {
    if ([string]::IsNullOrEmpty($Username) -or [string]::IsNullOrEmpty($Password)) {
        Write-Host "ERROR: Username and Password required for SQL authentication!" -ForegroundColor Red
        Write-Host "Usage: .\apply-migrations-to-server.ps1 -ServerName 'myserver' -DatabaseName 'tabdb' -Username 'sa' -Password 'password'" -ForegroundColor Yellow
        Write-Host "   OR: .\apply-migrations-to-server.ps1 -ServerName 'myserver' -DatabaseName 'tabdb' -UseWindowsAuth" -ForegroundColor Yellow
        exit 1
    }
    $connectionString = "Server=$ServerName;Database=$DatabaseName;User Id=$Username;Password=$Password;TrustServerCertificate=True;"
    Write-Host "Authentication: SQL Server ($Username)" -ForegroundColor Gray
}

Write-Host ""
$response = Read-Host "Continue with migration? (y/n)"
if ($response -ne 'y' -and $response -ne 'Y') {
    Write-Host "Cancelled" -ForegroundColor Yellow
    exit 0
}

Write-Host ""
Write-Host "Applying migrations..." -ForegroundColor Yellow
Write-Host ""

# Apply migrations using dotnet ef
dotnet ef database update --connection $connectionString

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "MIGRATIONS APPLIED SUCCESSFULLY!" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    Write-Host ""
} else {
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Red
    Write-Host "MIGRATION FAILED!" -ForegroundColor Red
    Write-Host "========================================" -ForegroundColor Red
    Write-Host ""
    exit 1
}
