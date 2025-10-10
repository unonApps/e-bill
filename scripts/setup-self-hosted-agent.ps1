# Setup Self-Hosted Agent for Azure DevOps
Write-Host "Setting up Self-Hosted Agent for Azure DevOps" -ForegroundColor Cyan
Write-Host "=============================================" -ForegroundColor Cyan

# Create agent directory
$agentDir = "C:\agents"
if (-not (Test-Path $agentDir)) {
    New-Item -ItemType Directory -Path $agentDir
}

Set-Location $agentDir

# Download agent
Write-Host "`nDownloading Azure Pipelines Agent..." -ForegroundColor Yellow
$agentZip = "vsts-agent-win-x64-latest.zip"
$agentUrl = "https://vstsagentpackage.azureedge.net/agent/3.232.0/vsts-agent-win-x64-3.232.0.zip"
Invoke-WebRequest -Uri $agentUrl -OutFile $agentZip

# Extract agent
Write-Host "Extracting agent files..." -ForegroundColor Yellow
Expand-Archive -Path $agentZip -DestinationPath ".\agent" -Force
Set-Location ".\agent"

Write-Host "`n========================================" -ForegroundColor Green
Write-Host "NEXT STEPS:" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "1. Go to Azure DevOps: https://dev.azure.com/dxmichuki/Bill" -ForegroundColor Cyan
Write-Host "2. Click on 'Project Settings' (bottom left)" -ForegroundColor Cyan
Write-Host "3. Under 'Pipelines', click 'Agent pools'" -ForegroundColor Cyan
Write-Host "4. Click 'Default' pool" -ForegroundColor Cyan
Write-Host "5. Click 'New agent' and get your PAT" -ForegroundColor Cyan
Write-Host "6. Run the configuration:" -ForegroundColor Cyan
Write-Host ""
Write-Host "   .\config.cmd" -ForegroundColor Yellow
Write-Host ""
Write-Host "   When prompted, enter:" -ForegroundColor White
Write-Host "   - Server URL: https://dev.azure.com/dxmichuki" -ForegroundColor Gray
Write-Host "   - PAT: (your personal access token)" -ForegroundColor Gray
Write-Host "   - Agent pool: Default" -ForegroundColor Gray
Write-Host "   - Agent name: MyLocalAgent" -ForegroundColor Gray
Write-Host ""
Write-Host "7. Run the agent:" -ForegroundColor Cyan
Write-Host "   .\run.cmd" -ForegroundColor Yellow
Write-Host ""
Write-Host "Your agent will then be available for running pipelines!" -ForegroundColor Green