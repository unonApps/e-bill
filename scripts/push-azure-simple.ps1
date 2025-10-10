# Simple Azure DevOps Push Script
Write-Host "Azure DevOps Git Push Helper" -ForegroundColor Cyan
Write-Host "==============================" -ForegroundColor Cyan

# Get PAT from user
$pat = Read-Host "Enter your Personal Access Token"

# Set the remote with PAT
Write-Host "`nSetting up authentication..." -ForegroundColor Yellow
$url = "https://PAT:$pat@dev.azure.com/dxmichuki/Bill/_git/Bill"
git remote set-url origin $url

# Push the code
Write-Host "`nPushing code to Azure DevOps..." -ForegroundColor Green
git push -u origin main --force

# Clean up
Write-Host "`nCleaning up credentials..." -ForegroundColor Yellow
git remote set-url origin https://dev.azure.com/dxmichuki/Bill/_git/Bill

Write-Host "`nDone! Your code has been pushed to Azure DevOps." -ForegroundColor Green