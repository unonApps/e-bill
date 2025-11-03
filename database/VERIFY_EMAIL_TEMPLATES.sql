-- =============================================
-- Verify Email Templates Installation
-- This script checks if all required email templates are properly installed
-- =============================================

SET NOCOUNT ON;
GO

PRINT '========================================';
PRINT 'EMAIL TEMPLATES VERIFICATION REPORT';
PRINT '========================================';
PRINT '';

-- Check User Management Templates
PRINT '1. USER MANAGEMENT TEMPLATES:';
PRINT '------------------------------';
SELECT
    CASE
        WHEN EXISTS (SELECT 1 FROM EmailTemplates WHERE TemplateCode = 'USER_ACCOUNT_CREATED' AND IsActive = 1)
        THEN '[✓] USER_ACCOUNT_CREATED - Installed and Active'
        ELSE '[✗] USER_ACCOUNT_CREATED - MISSING or INACTIVE'
    END AS Status;

SELECT
    CASE
        WHEN EXISTS (SELECT 1 FROM EmailTemplates WHERE TemplateCode = 'USER_PASSWORD_RESET' AND IsActive = 1)
        THEN '[✓] USER_PASSWORD_RESET - Installed and Active'
        ELSE '[✗] USER_PASSWORD_RESET - MISSING or INACTIVE'
    END AS Status;

PRINT '';

-- Check E-Bill User Management Templates
PRINT '2. E-BILL USER MANAGEMENT TEMPLATES:';
PRINT '------------------------------------';
SELECT
    CASE
        WHEN EXISTS (SELECT 1 FROM EmailTemplates WHERE TemplateCode = 'EBILL_USER_ACCOUNT_CREATED' AND IsActive = 1)
        THEN '[✓] EBILL_USER_ACCOUNT_CREATED - Installed and Active'
        ELSE '[✗] EBILL_USER_ACCOUNT_CREATED - MISSING or INACTIVE'
    END AS Status;

SELECT
    CASE
        WHEN EXISTS (SELECT 1 FROM EmailTemplates WHERE TemplateCode = 'EBILL_USER_PASSWORD_RESET' AND IsActive = 1)
        THEN '[✓] EBILL_USER_PASSWORD_RESET - Installed and Active'
        ELSE '[✗] EBILL_USER_PASSWORD_RESET - MISSING or INACTIVE'
    END AS Status;

PRINT '';

-- Check Phone Management Templates
PRINT '3. PHONE MANAGEMENT TEMPLATES:';
PRINT '------------------------------';
SELECT
    CASE
        WHEN EXISTS (SELECT 1 FROM EmailTemplates WHERE TemplateCode = 'PHONE_NUMBER_ASSIGNED' AND IsActive = 1)
        THEN '[✓] PHONE_NUMBER_ASSIGNED - Installed and Active'
        ELSE '[✗] PHONE_NUMBER_ASSIGNED - MISSING or INACTIVE'
    END AS Status;

SELECT
    CASE
        WHEN EXISTS (SELECT 1 FROM EmailTemplates WHERE TemplateCode = 'PHONE_TYPE_CHANGED' AND IsActive = 1)
        THEN '[✓] PHONE_TYPE_CHANGED - Installed and Active'
        ELSE '[✗] PHONE_TYPE_CHANGED - MISSING or INACTIVE'
    END AS Status;

SELECT
    CASE
        WHEN EXISTS (SELECT 1 FROM EmailTemplates WHERE TemplateCode = 'PHONE_NUMBER_UNASSIGNED' AND IsActive = 1)
        THEN '[✓] PHONE_NUMBER_UNASSIGNED - Installed and Active'
        ELSE '[✗] PHONE_NUMBER_UNASSIGNED - MISSING or INACTIVE'
    END AS Status;

PRINT '';
PRINT '========================================';
PRINT 'DETAILED TEMPLATE INFORMATION:';
PRINT '========================================';

-- Show all relevant templates with details
SELECT
    TemplateCode,
    Name,
    Category,
    IsActive,
    IsSystemTemplate,
    CreatedDate,
    CASE
        WHEN LEN(HtmlBody) > 100 THEN 'Yes (' + CAST(LEN(HtmlBody) AS VARCHAR) + ' chars)'
        ELSE 'No or Empty'
    END AS HasContent
FROM EmailTemplates
WHERE TemplateCode IN (
    'USER_ACCOUNT_CREATED',
    'USER_PASSWORD_RESET',
    'EBILL_USER_ACCOUNT_CREATED',
    'EBILL_USER_PASSWORD_RESET',
    'PHONE_NUMBER_ASSIGNED',
    'PHONE_TYPE_CHANGED',
    'PHONE_NUMBER_UNASSIGNED'
)
ORDER BY Category, TemplateCode;

PRINT '';
PRINT '========================================';
PRINT 'EMAIL CONFIGURATION CHECK:';
PRINT '========================================';

-- Check if email configuration exists
IF EXISTS (SELECT 1 FROM EmailConfigurations WHERE IsActive = 1)
BEGIN
    SELECT
        'Email Configuration: [✓] Active configuration found' AS Status,
        SmtpServer,
        SmtpPort,
        FromEmail,
        FromName,
        EnableSsl,
        IsActive
    FROM EmailConfigurations
    WHERE IsActive = 1;
END
ELSE
BEGIN
    PRINT 'Email Configuration: [✗] NO ACTIVE CONFIGURATION FOUND';
    PRINT 'ACTION REQUIRED: Configure email settings at /Admin/EmailConfiguration';
END

PRINT '';
PRINT '========================================';
PRINT 'RECENT EMAIL LOGS (Last 10):';
PRINT '========================================';

-- Show recent email attempts
IF EXISTS (SELECT 1 FROM EmailLogs)
BEGIN
    SELECT TOP 10
        Id,
        ToEmail,
        Subject,
        Status,
        SentDate,
        ErrorMessage,
        TemplateCode
    FROM EmailLogs
    ORDER BY CreatedDate DESC;
END
ELSE
BEGIN
    PRINT 'No email logs found. This could mean:';
    PRINT '  1. No emails have been sent yet';
    PRINT '  2. Email logging is not working';
    PRINT '  3. EmailLogs table is empty';
END

PRINT '';
PRINT '========================================';
PRINT 'VERIFICATION COMPLETE';
PRINT '========================================';
GO
