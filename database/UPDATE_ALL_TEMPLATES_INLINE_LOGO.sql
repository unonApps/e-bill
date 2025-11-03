-- =============================================
-- Update All User Management Email Templates with Inline Logo
-- Updates: USER_ACCOUNT_CREATED, USER_PASSWORD_RESET,
--          EBILL_USER_ACCOUNT_CREATED, EBILL_USER_PASSWORD_RESET
-- Uses cid:logo reference for embedded logo attachment
-- =============================================

SET NOCOUNT ON;
GO

PRINT '========================================';
PRINT 'Updating All Email Templates with Inline Logo';
PRINT '========================================';
PRINT '';
GO

-- Update templates to use cid:logo instead of {{BaseUrl}}/images/ebilling-login.jpg or text headers
UPDATE EmailTemplates
SET
    HtmlBody = REPLACE(HtmlBody, '{{BaseUrl}}/images/ebilling-login.jpg', 'cid:logo'),
    AvailablePlaceholders = REPLACE(AvailablePlaceholders, '{{BaseUrl}}, ', ''),
    ModifiedDate = GETUTCDATE(),
    ModifiedBy = 'System'
WHERE TemplateCode IN (
    'USER_ACCOUNT_CREATED',
    'USER_PASSWORD_RESET',
    'EBILL_USER_ACCOUNT_CREATED',
    'EBILL_USER_PASSWORD_RESET'
)
AND HtmlBody LIKE '%{{BaseUrl}}/images/ebilling-login.jpg%';
GO

-- Display results
PRINT 'Updated templates:';
PRINT '';

SELECT
    TemplateCode,
    Name,
    Category,
    IsActive,
    ModifiedDate,
    CASE
        WHEN HtmlBody LIKE '%cid:logo%' THEN 'Yes - Inline Logo'
        ELSE 'No'
    END AS 'Has Inline Logo'
FROM EmailTemplates
WHERE TemplateCode IN (
    'USER_ACCOUNT_CREATED',
    'USER_PASSWORD_RESET',
    'EBILL_USER_ACCOUNT_CREATED',
    'EBILL_USER_PASSWORD_RESET'
)
ORDER BY TemplateCode;
GO

PRINT '';
PRINT '========================================';
PRINT 'Update Complete!';
PRINT 'All email templates now use embedded logos';
PRINT 'Logo will be automatically attached to all templated emails';
PRINT '========================================';
GO
