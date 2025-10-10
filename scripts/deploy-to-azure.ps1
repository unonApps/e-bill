# Azure Deployment Script for TAB.Web
# Prerequisites: Azure CLI installed and logged in

Write-Host "Starting Azure deployment process..." -ForegroundColor Green

# Step 1: Clean and build
Write-Host "Step 1: Cleaning and building project..." -ForegroundColor Yellow
dotnet clean
dotnet restore
dotnet build --configuration Release

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed. Exiting..." -ForegroundColor Red
    exit 1
}

# Step 2: Run tests (if any)
Write-Host "Step 2: Running tests..." -ForegroundColor Yellow
# Uncomment if you have tests
# dotnet test --configuration Release --no-build

# Step 3: Publish
Write-Host "Step 3: Publishing application..." -ForegroundColor Yellow
Remove-Item -Path ./publish -Recurse -Force -ErrorAction SilentlyContinue
dotnet publish -c Release -o ./publish

if ($LASTEXITCODE -ne 0) {
    Write-Host "Publish failed. Exiting..." -ForegroundColor Red
    exit 1
}

# Step 4: Create deployment package
Write-Host "Step 4: Creating deployment package..." -ForegroundColor Yellow
Remove-Item -Path ./publish.zip -Force -ErrorAction SilentlyContinue
Compress-Archive -Path ./publish/* -DestinationPath ./publish.zip -Force

# Step 5: Deploy to Azure
Write-Host "Step 5: Deploying to Azure App Service..." -ForegroundColor Yellow
az webapp deployment source config-zip `
    --resource-group TABWeb20250926123812ResourceGroup `
    --name TABWeb20250926123812 `
    --src ./publish.zip

if ($LASTEXITCODE -ne 0) {
    Write-Host "Deployment failed. Please check Azure logs." -ForegroundColor Red
    exit 1
}

# Step 6: Clean up
Write-Host "Step 6: Cleaning up temporary files..." -ForegroundColor Yellow
Remove-Item -Path ./publish -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -Path ./publish.zip -Force -ErrorAction SilentlyContinue

Write-Host "Deployment completed successfully!" -ForegroundColor Green
Write-Host "App URL: https://tabweb20250926123812.azurewebsites.net" -ForegroundColor Cyan

# Optional: Open the website
$openSite = Read-Host "Do you want to open the website now? (Y/N)"
if ($openSite -eq 'Y' -or $openSite -eq 'y') {
    Start-Process "https://tabweb20250926123812.azurewebsites.net"
}