# ========================================
# Generate Safe Migration Script
# Run this on DEVELOPMENT MACHINE
# ========================================

param(
    [string]$ProjectPath = "C:\Users\dxmic\Desktop\Do Net Template\DoNetTemplate.Web"
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Generating Database Migration Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

cd $ProjectPath

# Generate the idempotent script
Write-Host "[1/2] Generating migration script..." -ForegroundColor Yellow
dotnet ef migrations script --idempotent --output "./temp-migration.sql"

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Migration script generation failed!" -ForegroundColor Red
    exit 1
}

# Post-process the script to fix stored procedure issues
Write-Host "[2/2] Post-processing script (fixing stored procedures)..." -ForegroundColor Yellow

$content = Get-Content "./temp-migration.sql" -Raw

# Add GO statement after each CREATE PROCEDURE (before closing parenthesis of the Sql() call)
$content = $content -replace '(CREATE PROCEDURE [^\r\n]+[\s\S]+?END)("\);)', '$1$2
GO
'

# Save the fixed script
$content | Set-Content "./server-migration.sql" -NoNewline

# Remove temp file
Remove-Item "./temp-migration.sql" -Force

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "MIGRATION SCRIPT GENERATED!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Output file:" -ForegroundColor White
Write-Host "  $ProjectPath\server-migration.sql" -ForegroundColor White
Write-Host ""
Write-Host "This script is safe to run multiple times (idempotent)" -ForegroundColor Gray
Write-Host ""
