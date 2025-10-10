# Simple deployment script for Azure App Service
# This deploys the publish.zip file using Kudu API

$appName = "TABWeb20250926123812"
$username = '$TABWeb20250926123812'
$password = 'YOUR_DEPLOYMENT_PASSWORD_HERE'  # Get this from Azure Portal -> App Service -> Deployment Center -> FTPS credentials
$zipFile = "publish.zip"

Write-Host "Deploying to Azure App Service: $appName" -ForegroundColor Cyan

# Create base64 encoded credentials
$base64AuthInfo = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes(("{0}:{1}" -f $username, $password)))

# Deploy using Kudu ZipDeploy API
$apiUrl = "https://$appName.scm.azurewebsites.net/api/zipdeploy"

Write-Host "Uploading $zipFile to $apiUrl..." -ForegroundColor Yellow

try {
    $response = Invoke-RestMethod -Uri $apiUrl `
        -Headers @{Authorization=("Basic {0}" -f $base64AuthInfo)} `
        -Method POST `
        -InFile $zipFile `
        -ContentType "application/zip" `
        -TimeoutSec 300

    Write-Host "Deployment successful!" -ForegroundColor Green
    Write-Host "App URL: https://$appName.azurewebsites.net" -ForegroundColor Green
}
catch {
    Write-Host "Deployment failed: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Get deployment password from: Azure Portal -> $appName -> Deployment Center -> FTPS credentials" -ForegroundColor Yellow
}
