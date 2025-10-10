# PowerShell script to create and apply GUID migration

Write-Host "Starting GUID Migration Process..." -ForegroundColor Cyan

# Step 1: Create EF Core Migration
Write-Host "`nStep 1: Creating EF Core Migration..." -ForegroundColor Yellow
dotnet ef migrations add AddPublicIdToAllTables

if ($LASTEXITCODE -ne 0) {
    Write-Host "Failed to create migration!" -ForegroundColor Red
    exit 1
}

Write-Host "Migration created successfully!" -ForegroundColor Green

# Step 2: Show migration SQL (for review)
Write-Host "`nStep 2: Generating SQL script for review..." -ForegroundColor Yellow
dotnet ef migrations script --idempotent --output guid-migration.sql

# Step 3: Apply migration to database
Write-Host "`nStep 3: Apply migration to database? (y/n)" -ForegroundColor Yellow
$response = Read-Host

if ($response -eq 'y') {
    Write-Host "Applying migration..." -ForegroundColor Yellow
    dotnet ef database update

    if ($LASTEXITCODE -eq 0) {
        Write-Host "Migration applied successfully!" -ForegroundColor Green
    } else {
        Write-Host "Failed to apply migration!" -ForegroundColor Red
        exit 1
    }
} else {
    Write-Host "Migration not applied. Review the generated SQL script." -ForegroundColor Yellow
}

Write-Host "`nGUID Migration Process Complete!" -ForegroundColor Green
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "1. Update controllers to use PublicId instead of Id in URLs"
Write-Host "2. Update views to display and link using PublicId"
Write-Host "3. Test all CRUD operations with GUIDs"
Write-Host "4. Ensure no integer IDs are exposed in URLs"