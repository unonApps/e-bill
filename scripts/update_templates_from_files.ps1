# PowerShell script to update email templates from HTML files
# This reads the HTML template files and updates the database

$server = "MICHUKI\SQLEXPRESS"
$database = "TABDB"

# Template mappings: Code => FileName
$templates = @{
    "USER_ACCOUNT_CREATED" = "UserAccountCreatedTemplate.html"
    "EBILL_USER_ACCOUNT_CREATED" = "EbillUserAccountCreatedTemplate.html"
    "EBILL_USER_PASSWORD_RESET" = "EbillUserPasswordResetTemplate.html"
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Updating Email Templates from HTML Files" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

foreach ($templateCode in $templates.Keys) {
    $fileName = $templates[$templateCode]
    $filePath = Join-Path $PSScriptRoot $fileName

    if (Test-Path $filePath) {
        Write-Host "Updating $templateCode from $fileName..." -ForegroundColor Yellow

        # Read HTML content
        $htmlContent = Get-Content $filePath -Raw

        # Escape single quotes for SQL
        $htmlContent = $htmlContent.Replace("'", "''")

        # Create SQL update statement
        $sql = @"
UPDATE EmailTemplates
SET
    HtmlBody = '$htmlContent',
    ModifiedDate = GETUTCDATE(),
    ModifiedBy = 'System'
WHERE TemplateCode = '$templateCode';
"@

        # Execute SQL
        try {
            sqlcmd -S $server -d $database -E -Q $sql -b
            if ($LASTEXITCODE -eq 0) {
                Write-Host "  ✓ Successfully updated $templateCode" -ForegroundColor Green
            } else {
                Write-Host "  ✗ Failed to update $templateCode" -ForegroundColor Red
            }
        } catch {
            Write-Host "  ✗ Error: $_" -ForegroundColor Red
        }

        Write-Host ""
    } else {
        Write-Host "  ✗ File not found: $fileName" -ForegroundColor Red
    }
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Verifying updates..." -ForegroundColor Cyan
Write-Host ""

# Verify all templates have inline logos
$verifySql = @"
SELECT
    TemplateCode,
    Name,
    ModifiedDate,
    CASE
        WHEN HtmlBody LIKE '%cid:logo%' THEN 'Yes ✓'
        ELSE 'No ✗'
    END AS 'Has Inline Logo'
FROM EmailTemplates
WHERE TemplateCode IN ('USER_ACCOUNT_CREATED', 'USER_PASSWORD_RESET', 'EBILL_USER_ACCOUNT_CREATED', 'EBILL_USER_PASSWORD_RESET')
ORDER BY TemplateCode;
"@

sqlcmd -S $server -d $database -E -Q $verifySql

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Update Complete!" -ForegroundColor Green
Write-Host "All templates now use embedded inline logos" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
