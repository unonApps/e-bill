# Configure Microsoft 365 Email in Azure App Service
# Run this script to add email settings to your Azure App Service

$appName = "TABWeb20250926123812"
$resourceGroup = "YOUR_RESOURCE_GROUP_NAME"  # Replace with your resource group

Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "Microsoft 365 Email Configuration" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

# Prompt for email settings
Write-Host "Enter your Microsoft 365 email settings:" -ForegroundColor Yellow
Write-Host ""

$fromEmail = Read-Host "From Email Address (e.g., notifications@yourdomain.com)"
$fromName = Read-Host "From Display Name (e.g., TAB System)"
$username = Read-Host "Username (usually same as From Email)"
$password = Read-Host "Password" -AsSecureString
$passwordText = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($password))

Write-Host ""
Write-Host "Configuring Azure App Service..." -ForegroundColor Yellow
Write-Host ""

try {
    # Check if logged in to Azure
    $context = Get-AzContext -ErrorAction SilentlyContinue
    if (!$context) {
        Write-Host "Not logged in to Azure. Logging in..." -ForegroundColor Yellow
        Connect-AzAccount
    }

    # Set the email configuration
    $settings = @{
        "EmailSettings__SmtpServer" = "smtp.office365.com"
        "EmailSettings__SmtpPort" = "587"
        "EmailSettings__FromEmail" = $fromEmail
        "EmailSettings__FromName" = $fromName
        "EmailSettings__Username" = $username
        "EmailSettings__Password" = $passwordText
        "EmailSettings__EnableSsl" = "true"
    }

    Write-Host "Adding settings to App Service..." -ForegroundColor Cyan

    foreach ($key in $settings.Keys) {
        Write-Host "  Setting: $key" -ForegroundColor Gray
        Set-AzWebApp -ResourceGroupName $resourceGroup -Name $appName -AppSettings @{$key = $settings[$key]} -ErrorAction Stop
    }

    Write-Host ""
    Write-Host "✓ Email settings configured successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Yellow
    Write-Host "1. Restart your app service (or wait for auto-restart)" -ForegroundColor White
    Write-Host "2. Go to: https://$appName.azurewebsites.net/Admin/EmailSettings" -ForegroundColor White
    Write-Host "3. Send a test email to verify configuration" -ForegroundColor White
    Write-Host ""
}
catch {
    Write-Host ""
    Write-Host "✗ Error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
    Write-Host "Manual Configuration:" -ForegroundColor Yellow
    Write-Host "1. Go to Azure Portal: https://portal.azure.com" -ForegroundColor White
    Write-Host "2. Navigate to: App Services → $appName → Configuration" -ForegroundColor White
    Write-Host "3. Add these Application Settings:" -ForegroundColor White
    Write-Host ""
    foreach ($key in $settings.Keys) {
        Write-Host "   Name: $key" -ForegroundColor Cyan
        if ($key -eq "EmailSettings__Password") {
            Write-Host "   Value: [YOUR_PASSWORD]" -ForegroundColor Gray
        } else {
            Write-Host "   Value: $($settings[$key])" -ForegroundColor Gray
        }
        Write-Host ""
    }
}
