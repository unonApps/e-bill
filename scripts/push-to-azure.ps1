# Azure DevOps Push Script
# This script securely pushes code to Azure DevOps

$pat = Read-Host "Enter your Azure DevOps Personal Access Token"

# Encode the PAT for URL
$encodedPat = [System.Web.HttpUtility]::UrlEncode($pat)

# Update the remote URL with PAT (using empty username and PAT as password)
$remoteUrl = "https://:${pat}@dev.azure.com/dxmichuki/Bill/_git/Bill"
git remote set-url origin $remoteUrl

# Push all branches and tags
Write-Host "Pushing to Azure DevOps..." -ForegroundColor Green
try {
    git push -u origin --all
    git push -u origin --tags
    Write-Host "Push completed successfully!" -ForegroundColor Green
}
catch {
    Write-Host "Error during push: $_" -ForegroundColor Red
}
finally {
    # Remove PAT from remote URL for security
    git remote set-url origin "https://dev.azure.com/dxmichuki/Bill/_git/Bill"
    Write-Host "Remote URL cleaned for security." -ForegroundColor Yellow
}