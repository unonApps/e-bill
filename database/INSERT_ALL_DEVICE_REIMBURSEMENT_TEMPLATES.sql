-- =============================================
-- COMPLETE Device Reimbursement Email Templates
-- All workflow stages included
-- =============================================

USE TABDB;
GO

PRINT '===================================='
PRINT 'Starting Device Reimbursement Email Template Installation'
PRINT '===================================='
PRINT ''

-- 1. REQUEST SUBMITTED - Confirmation to Requester
PRINT 'Installing: REFUND_REQUEST_SUBMITTED'
IF NOT EXISTS (SELECT 1 FROM EmailTemplates WHERE TemplateCode = 'REFUND_REQUEST_SUBMITTED')
BEGIN
    INSERT INTO EmailTemplates (TemplateCode, Name, Subject, HtmlBody, PlainTextBody, Description, Category, AvailablePlaceholders, IsActive, IsSystemTemplate, CreatedDate)
    VALUES (
    'REFUND_REQUEST_SUBMITTED',
    'Device Reimbursement Request Submitted',
    'Device Reimbursement Request Submitted - Request #{{RequestId}}',
    '<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Device Reimbursement Request Submitted</title>
</head>
<body style="margin: 0; padding: 0; font-family: Arial, sans-serif; background-color: #f4f4f4;">
    <table cellpadding="0" cellspacing="0" border="0" width="100%" style="background-color: #f4f4f4; padding: 20px;">
        <tr>
            <td align="center">
                <table cellpadding="0" cellspacing="0" border="0" width="600" style="background-color: #ffffff; border-radius: 8px;">
                    <tr>
                        <td style="background-color: #667eea; padding: 30px; text-align: center; border-radius: 8px 8px 0 0;">
                            <h1 style="color: #ffffff; margin: 0; font-size: 24px;">Request Submitted Successfully</h1>
                        </td>
                    </tr>
                    <tr>
                        <td style="padding: 30px;">
                            <p style="color: #333333; font-size: 16px; margin: 0 0 20px 0;">Dear <strong>{{RequesterName}}</strong>,</p>
                            <p style="color: #555555; font-size: 14px; line-height: 1.6; margin: 0 0 20px 0;">
                                Your device reimbursement request has been successfully submitted.
                            </p>
                            <table cellpadding="0" cellspacing="0" border="0" width="100%" style="background-color: #f8f9fa; padding: 15px; margin: 0 0 20px 0;">
                                <tr><td style="padding: 5px 0; color: #666666;">Request ID:</td><td style="padding: 5px 0; font-weight: 600;">#{{RequestId}}</td></tr>
                                <tr><td style="padding: 5px 0; color: #666666;">Date:</td><td style="padding: 5px 0;">{{RequestDate}}</td></tr>
                                <tr><td style="padding: 5px 0; color: #666666;">Amount:</td><td style="padding: 5px 0; font-weight: 600;">{{DevicePurchaseCurrency}} {{DevicePurchaseAmount}}</td></tr>
                                <tr><td style="padding: 5px 0; color: #666666;">Status:</td><td style="padding: 5px 0;"><span style="background-color: #ffc107; padding: 3px 10px; border-radius: 10px; font-size: 12px;">Pending Supervisor</span></td></tr>
                            </table>
                            <div style="text-align: center; margin: 20px 0;">
                                <a href="{{ViewRequestLink}}" style="background-color: #667eea; color: #ffffff; text-decoration: none; padding: 12px 30px; border-radius: 5px; display: inline-block;">View My Requests</a>
                            </div>
                        </td>
                    </tr>
                    <tr>
                        <td style="background-color: #f8f9fa; padding: 20px; text-align: center; border-radius: 0 0 8px 8px;">
                            <p style="color: #999999; font-size: 12px; margin: 0;">© {{Year}} TAB System</p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>',
    NULL,
    'Confirmation email sent to requester after submitting a device reimbursement request',
    'Device Reimbursement',
    'RequestId, RequestDate, RequesterName, DevicePurchaseAmount, DevicePurchaseCurrency, ViewRequestLink, Year',
    1,
    1,
    GETUTCDATE()
    );
    PRINT '✓ REFUND_REQUEST_SUBMITTED installed successfully'
END
ELSE
    PRINT '⊗ REFUND_REQUEST_SUBMITTED already exists - skipping'
PRINT ''

-- 2. SUPERVISOR NOTIFICATION
PRINT 'Installing: REFUND_SUPERVISOR_NOTIFICATION'
IF NOT EXISTS (SELECT 1 FROM EmailTemplates WHERE TemplateCode = 'REFUND_SUPERVISOR_NOTIFICATION')
BEGIN
    INSERT INTO EmailTemplates (TemplateCode, Name, Subject, HtmlBody, PlainTextBody, Description, Category, AvailablePlaceholders, IsActive, IsSystemTemplate, CreatedDate)
    VALUES (
    'REFUND_SUPERVISOR_NOTIFICATION',
    'Device Reimbursement - Supervisor Notification',
    'New Device Reimbursement Request - {{RequesterName}}',
    '<!DOCTYPE html>
<html><head><meta charset="UTF-8"></head>
<body style="margin: 0; padding: 0; font-family: Arial, sans-serif; background-color: #f4f4f4;">
    <table width="100%" cellpadding="20"><tr><td align="center">
        <table width="600" style="background-color: #ffffff; border-radius: 8px;">
            <tr><td style="background-color: #f5576c; padding: 30px; text-align: center; color: #ffffff;"><h1 style="margin: 0;">Approval Required</h1></td></tr>
            <tr><td style="padding: 30px;">
                <p style="font-size: 16px;">Dear <strong>{{SupervisorName}}</strong>,</p>
                <p style="color: #555555;">A device reimbursement request from <strong>{{RequesterName}}</strong> requires your approval.</p>
                <table width="100%" style="background-color: #fff3cd; padding: 15px; margin: 20px 0;">
                    <tr><td style="color: #856404;">Request ID:</td><td><strong>#{{RequestId}}</strong></td></tr>
                    <tr><td style="color: #856404;">Staff:</td><td>{{RequesterName}}</td></tr>
                    <tr><td style="color: #856404;">Amount:</td><td><strong>{{DevicePurchaseCurrency}} {{DevicePurchaseAmount}}</strong></td></tr>
                    <tr><td style="color: #856404;">Mobile:</td><td>{{PrimaryMobileNumber}}</td></tr>
                </table>
                <div style="text-align: center; margin: 20px 0;">
                    <a href="{{ApprovalLink}}" style="background-color: #f5576c; color: #ffffff; text-decoration: none; padding: 12px 30px; border-radius: 5px; display: inline-block;">Review Request</a>
                </div>
            </td></tr>
            <tr><td style="background-color: #f8f9fa; padding: 20px; text-align: center;"><p style="color: #999999; font-size: 12px; margin: 0;">© {{Year}} TAB System</p></td></tr>
        </table>
    </td></tr></table>
</body></html>',
    NULL,
    'Notification to supervisor when device reimbursement request is submitted',
    'Device Reimbursement',
    'RequestId, RequesterName, SupervisorName, DevicePurchaseAmount, DevicePurchaseCurrency, PrimaryMobileNumber, ApprovalLink, Year',
    1,
    1,
    GETUTCDATE()
    );
    PRINT '✓ REFUND_SUPERVISOR_NOTIFICATION installed successfully'
END
ELSE
    PRINT '⊗ REFUND_SUPERVISOR_NOTIFICATION already exists - skipping'
PRINT ''

-- 3. SUPERVISOR APPROVED
PRINT 'Installing: REFUND_SUPERVISOR_APPROVED'
IF NOT EXISTS (SELECT 1 FROM EmailTemplates WHERE TemplateCode = 'REFUND_SUPERVISOR_APPROVED')
BEGIN
    INSERT INTO EmailTemplates (TemplateCode, Name, Subject, HtmlBody, PlainTextBody, Description, Category, AvailablePlaceholders, IsActive, IsSystemTemplate, CreatedDate)
    VALUES (
    'REFUND_SUPERVISOR_APPROVED',
    'Device Reimbursement - Supervisor Approved',
    'Your Device Reimbursement Request Approved - #{{RequestId}}',
    '<!DOCTYPE html>
<html><head><meta charset="UTF-8"></head>
<body style="margin: 0; padding: 0; font-family: Arial, sans-serif; background-color: #f4f4f4;">
    <table width="100%" cellpadding="20"><tr><td align="center">
        <table width="600" style="background-color: #ffffff; border-radius: 8px;">
            <tr><td style="background-color: #38ef7d; padding: 30px; text-align: center; color: #ffffff;"><h1 style="margin: 0;">✓ Approved by Supervisor</h1></td></tr>
            <tr><td style="padding: 30px;">
                <p style="font-size: 16px;">Dear <strong>{{RequesterName}}</strong>,</p>
                <p style="color: #555555;">Good news! Your request has been approved by your supervisor.</p>
                <table width="100%" style="background-color: #d4edda; padding: 15px; margin: 20px 0;">
                    <tr><td>Request ID:</td><td><strong>#{{RequestId}}</strong></td></tr>
                    <tr><td>Approved By:</td><td>{{SupervisorName}}</td></tr>
                    <tr><td>Amount:</td><td><strong>{{DevicePurchaseCurrency}} {{DevicePurchaseAmount}}</strong></td></tr>
                    <tr><td>Status:</td><td><span style="background-color: #ffc107; padding: 3px 10px; border-radius: 10px; font-size: 12px;">Pending Budget Officer</span></td></tr>
                </table>
                <div style="text-align: center;">
                    <a href="{{ViewRequestLink}}" style="background-color: #38ef7d; color: #000000; text-decoration: none; padding: 12px 30px; border-radius: 5px; display: inline-block;">View Status</a>
                </div>
            </td></tr>
            <tr><td style="background-color: #f8f9fa; padding: 20px; text-align: center;"><p style="color: #999999; font-size: 12px; margin: 0;">© {{Year}} TAB System</p></td></tr>
        </table>
    </td></tr></table>
</body></html>',
    NULL,
    'Notification to requester when supervisor approves request',
    'Device Reimbursement',
    'RequestId, RequesterName, SupervisorName, DevicePurchaseAmount, DevicePurchaseCurrency, ViewRequestLink, Year',
    1,
    1,
    GETUTCDATE()
    );
    PRINT '✓ REFUND_SUPERVISOR_APPROVED installed successfully'
END
ELSE
    PRINT '⊗ REFUND_SUPERVISOR_APPROVED already exists - skipping'
PRINT ''

-- 4. BUDGET OFFICER NOTIFICATION
PRINT 'Installing: REFUND_BUDGET_OFFICER_NOTIFICATION'
IF NOT EXISTS (SELECT 1 FROM EmailTemplates WHERE TemplateCode = 'REFUND_BUDGET_OFFICER_NOTIFICATION')
BEGIN
    INSERT INTO EmailTemplates (TemplateCode, Name, Subject, HtmlBody, PlainTextBody, Description, Category, AvailablePlaceholders, IsActive, IsSystemTemplate, CreatedDate)
    VALUES (
    'REFUND_BUDGET_OFFICER_NOTIFICATION',
    'Device Reimbursement - Budget Officer Notification',
    'Device Reimbursement Requires Budget Approval - #{{RequestId}}',
    '<!DOCTYPE html>
<html><head><meta charset="UTF-8"></head>
<body style="margin: 0; padding: 0; font-family: Arial, sans-serif; background-color: #f4f4f4;">
    <table width="100%" cellpadding="20"><tr><td align="center">
        <table width="600" style="background-color: #ffffff; border-radius: 8px;">
            <tr><td style="background-color: #4a90e2; padding: 30px; text-align: center; color: #ffffff;"><h1 style="margin: 0;">Budget Approval Required</h1></td></tr>
            <tr><td style="padding: 30px;">
                <p style="font-size: 16px;">Dear <strong>{{BudgetOfficerName}}</strong>,</p>
                <p style="color: #555555;">A device reimbursement request requires your budget approval.</p>
                <table width="100%" style="background-color: #e3f2fd; padding: 15px; margin: 20px 0;">
                    <tr><td>Request ID:</td><td><strong>#{{RequestId}}</strong></td></tr>
                    <tr><td>Staff:</td><td>{{RequesterName}}</td></tr>
                    <tr><td>Organization:</td><td>{{Organization}}</td></tr>
                    <tr><td>Amount:</td><td><strong>{{DevicePurchaseCurrency}} {{DevicePurchaseAmount}}</strong></td></tr>
                    <tr><td>Supervisor:</td><td>{{SupervisorName}} ✓</td></tr>
                </table>
                <div style="background-color: #fff3cd; padding: 15px; margin: 20px 0; border-left: 4px solid #ffc107;">
                    <p style="margin: 0; color: #856404;"><strong>Action Required:</strong> Please review cost allocation and fund commitment.</p>
                </div>
                <div style="text-align: center;">
                    <a href="{{ApprovalLink}}" style="background-color: #4a90e2; color: #ffffff; text-decoration: none; padding: 12px 30px; border-radius: 5px; display: inline-block;">Review & Approve</a>
                </div>
            </td></tr>
            <tr><td style="background-color: #f8f9fa; padding: 20px; text-align: center;"><p style="color: #999999; font-size: 12px; margin: 0;">© {{Year}} TAB System</p></td></tr>
        </table>
    </td></tr></table>
</body></html>',
    NULL,
    'Notification to budget officer when request needs budget approval',
    'Device Reimbursement',
    'RequestId, RequesterName, BudgetOfficerName, Organization, SupervisorName, DevicePurchaseAmount, DevicePurchaseCurrency, ApprovalLink, Year',
    1,
    1,
    GETUTCDATE()
    );
    PRINT '✓ REFUND_BUDGET_OFFICER_NOTIFICATION installed successfully'
END
ELSE
    PRINT '⊗ REFUND_BUDGET_OFFICER_NOTIFICATION already exists - skipping'
PRINT ''

-- 5. BUDGET OFFICER APPROVED
PRINT 'Installing: REFUND_BUDGET_OFFICER_APPROVED'
IF NOT EXISTS (SELECT 1 FROM EmailTemplates WHERE TemplateCode = 'REFUND_BUDGET_OFFICER_APPROVED')
BEGIN
    INSERT INTO EmailTemplates (TemplateCode, Name, Subject, HtmlBody, PlainTextBody, Description, Category, AvailablePlaceholders, IsActive, IsSystemTemplate, CreatedDate)
    VALUES (
    'REFUND_BUDGET_OFFICER_APPROVED',
    'Device Reimbursement - Budget Approved',
    'Budget Approval Completed - Request #{{RequestId}}',
    '<!DOCTYPE html>
<html><head><meta charset="UTF-8"></head>
<body style="margin: 0; padding: 0; font-family: Arial, sans-serif; background-color: #f4f4f4;">
    <table width="100%" cellpadding="20"><tr><td align="center">
        <table width="600" style="background-color: #ffffff; border-radius: 8px;">
            <tr><td style="background-color: #4a90e2; padding: 30px; text-align: center; color: #ffffff;"><h1 style="margin: 0;">✓ Budget Approved</h1></td></tr>
            <tr><td style="padding: 30px;">
                <p style="font-size: 16px;">Dear <strong>{{RequesterName}}</strong>,</p>
                <p style="color: #555555;">Your request has been approved by the Budget Officer and forwarded to Staff Claims Unit.</p>
                <table width="100%" style="background-color: #d4edda; padding: 15px; margin: 20px 0;">
                    <tr><td>Request ID:</td><td><strong>#{{RequestId}}</strong></td></tr>
                    <tr><td>Amount:</td><td><strong>{{DevicePurchaseCurrency}} {{DevicePurchaseAmount}}</strong></td></tr>
                    <tr><td>Cost Object:</td><td>{{CostObject}}</td></tr>
                    <tr><td>Status:</td><td><span style="background-color: #ffc107; padding: 3px 10px; border-radius: 10px; font-size: 12px;">Pending Claims Unit</span></td></tr>
                </table>
                <div style="text-align: center;">
                    <a href="{{ViewRequestLink}}" style="background-color: #4a90e2; color: #ffffff; text-decoration: none; padding: 12px 30px; border-radius: 5px; display: inline-block;">View Status</a>
                </div>
            </td></tr>
            <tr><td style="background-color: #f8f9fa; padding: 20px; text-align: center;"><p style="color: #999999; font-size: 12px; margin: 0;">© {{Year}} TAB System</p></td></tr>
        </table>
    </td></tr></table>
</body></html>',
    NULL,
    'Notification to requester when budget officer approves',
    'Device Reimbursement',
    'RequestId, RequesterName, DevicePurchaseAmount, DevicePurchaseCurrency, CostObject, ViewRequestLink, Year',
    1,
    1,
    GETUTCDATE()
    );
    PRINT '✓ REFUND_BUDGET_OFFICER_APPROVED installed successfully'
END
ELSE
    PRINT '⊗ REFUND_BUDGET_OFFICER_APPROVED already exists - skipping'
PRINT ''

-- 6. CLAIMS UNIT NOTIFICATION
PRINT 'Installing: REFUND_CLAIMS_UNIT_NOTIFICATION'
IF NOT EXISTS (SELECT 1 FROM EmailTemplates WHERE TemplateCode = 'REFUND_CLAIMS_UNIT_NOTIFICATION')
BEGIN
    INSERT INTO EmailTemplates (TemplateCode, Name, Subject, HtmlBody, PlainTextBody, Description, Category, AvailablePlaceholders, IsActive, IsSystemTemplate, CreatedDate)
    VALUES (
    'REFUND_CLAIMS_UNIT_NOTIFICATION',
    'Device Reimbursement - Claims Unit Notification',
    'Device Reimbursement Ready for Processing - #{{RequestId}}',
    '<!DOCTYPE html>
<html><head><meta charset="UTF-8"></head>
<body style="margin: 0; padding: 0; font-family: Arial, sans-serif; background-color: #f4f4f4;">
    <table width="100%" cellpadding="20"><tr><td align="center">
        <table width="600" style="background-color: #ffffff; border-radius: 8px;">
            <tr><td style="background-color: #9b59b6; padding: 30px; text-align: center; color: #ffffff;"><h1 style="margin: 0;">Claims Processing Required</h1></td></tr>
            <tr><td style="padding: 30px;">
                <p style="font-size: 16px;">Dear Staff Claims Unit,</p>
                <p style="color: #555555;">A device reimbursement request is ready for claims processing.</p>
                <table width="100%" style="background-color: #f3e5f5; padding: 15px; margin: 20px 0;">
                    <tr><td>Request ID:</td><td><strong>#{{RequestId}}</strong></td></tr>
                    <tr><td>Staff:</td><td>{{RequesterName}}</td></tr>
                    <tr><td>Organization:</td><td>{{Organization}}</td></tr>
                    <tr><td>Amount:</td><td><strong>{{DevicePurchaseCurrency}} {{DevicePurchaseAmount}}</strong></td></tr>
                    <tr><td>Bank:</td><td>{{UmojaBankName}}</td></tr>
                    <tr><td>Approvals:</td><td>✓ Supervisor ✓ Budget Officer</td></tr>
                </table>
                <div style="text-align: center;">
                    <a href="{{ProcessLink}}" style="background-color: #9b59b6; color: #ffffff; text-decoration: none; padding: 12px 30px; border-radius: 5px; display: inline-block;">Process Claim</a>
                </div>
            </td></tr>
            <tr><td style="background-color: #f8f9fa; padding: 20px; text-align: center;"><p style="color: #999999; font-size: 12px; margin: 0;">© {{Year}} TAB System</p></td></tr>
        </table>
    </td></tr></table>
</body></html>',
    NULL,
    'Notification to claims unit when request is ready for processing',
    'Device Reimbursement',
    'RequestId, RequesterName, Organization, UmojaBankName, DevicePurchaseAmount, DevicePurchaseCurrency, ProcessLink, Year',
    1,
    1,
    GETUTCDATE()
    );
    PRINT '✓ REFUND_CLAIMS_UNIT_NOTIFICATION installed successfully'
END
ELSE
    PRINT '⊗ REFUND_CLAIMS_UNIT_NOTIFICATION already exists - skipping'
PRINT ''

-- 7. CLAIMS PROCESSED
PRINT 'Installing: REFUND_CLAIMS_PROCESSED'
IF NOT EXISTS (SELECT 1 FROM EmailTemplates WHERE TemplateCode = 'REFUND_CLAIMS_PROCESSED')
BEGIN
    INSERT INTO EmailTemplates (TemplateCode, Name, Subject, HtmlBody, PlainTextBody, Description, Category, AvailablePlaceholders, IsActive, IsSystemTemplate, CreatedDate)
    VALUES (
    'REFUND_CLAIMS_PROCESSED',
    'Device Reimbursement - Claims Processed',
    'Your Reimbursement is Being Processed - #{{RequestId}}',
    '<!DOCTYPE html>
<html><head><meta charset="UTF-8"></head>
<body style="margin: 0; padding: 0; font-family: Arial, sans-serif; background-color: #f4f4f4;">
    <table width="100%" cellpadding="20"><tr><td align="center">
        <table width="600" style="background-color: #ffffff; border-radius: 8px;">
            <tr><td style="background-color: #9b59b6; padding: 30px; text-align: center; color: #ffffff;"><h1 style="margin: 0;">✓ Claims Processed</h1></td></tr>
            <tr><td style="padding: 30px;">
                <p style="font-size: 16px;">Dear <strong>{{RequesterName}}</strong>,</p>
                <p style="color: #555555;">Your reimbursement claim has been processed and is awaiting final payment approval.</p>
                <table width="100%" style="background-color: #d4edda; padding: 15px; margin: 20px 0;">
                    <tr><td>Request ID:</td><td><strong>#{{RequestId}}</strong></td></tr>
                    <tr><td>Refund Amount:</td><td><strong>USD {{RefundUsdAmount}}</strong></td></tr>
                    <tr><td>Umoja Doc ID:</td><td>{{UmojaPaymentDocumentId}}</td></tr>
                    <tr><td>Status:</td><td><span style="background-color: #ffc107; padding: 3px 10px; border-radius: 10px; font-size: 12px;">Pending Payment Approval</span></td></tr>
                </table>
                <div style="text-align: center;">
                    <a href="{{ViewRequestLink}}" style="background-color: #9b59b6; color: #ffffff; text-decoration: none; padding: 12px 30px; border-radius: 5px; display: inline-block;">View Status</a>
                </div>
            </td></tr>
            <tr><td style="background-color: #f8f9fa; padding: 20px; text-align: center;"><p style="color: #999999; font-size: 12px; margin: 0;">© {{Year}} TAB System</p></td></tr>
        </table>
    </td></tr></table>
</body></html>',
    NULL,
    'Notification to requester when claims unit processes request',
    'Device Reimbursement',
    'RequestId, RequesterName, RefundUsdAmount, UmojaPaymentDocumentId, ViewRequestLink, Year',
    1,
    1,
    GETUTCDATE()
    );
    PRINT '✓ REFUND_CLAIMS_PROCESSED installed successfully'
END
ELSE
    PRINT '⊗ REFUND_CLAIMS_PROCESSED already exists - skipping'
PRINT ''

-- 8. PAYMENT APPROVER NOTIFICATION
PRINT 'Installing: REFUND_PAYMENT_APPROVER_NOTIFICATION'
IF NOT EXISTS (SELECT 1 FROM EmailTemplates WHERE TemplateCode = 'REFUND_PAYMENT_APPROVER_NOTIFICATION')
BEGIN
    INSERT INTO EmailTemplates (TemplateCode, Name, Subject, HtmlBody, PlainTextBody, Description, Category, AvailablePlaceholders, IsActive, IsSystemTemplate, CreatedDate)
    VALUES (
    'REFUND_PAYMENT_APPROVER_NOTIFICATION',
    'Device Reimbursement - Payment Approval Required',
    'Payment Approval Required - Request #{{RequestId}}',
    '<!DOCTYPE html>
<html><head><meta charset="UTF-8"></head>
<body style="margin: 0; padding: 0; font-family: Arial, sans-serif; background-color: #f4f4f4;">
    <table width="100%" cellpadding="20"><tr><td align="center">
        <table width="600" style="background-color: #ffffff; border-radius: 8px;">
            <tr><td style="background-color: #e74c3c; padding: 30px; text-align: center; color: #ffffff;"><h1 style="margin: 0;">Final Payment Approval</h1></td></tr>
            <tr><td style="padding: 30px;">
                <p style="font-size: 16px;">Dear Payment Approver,</p>
                <p style="color: #555555;">A device reimbursement payment requires your final approval.</p>
                <table width="100%" style="background-color: #fadbd8; padding: 15px; margin: 20px 0;">
                    <tr><td>Request ID:</td><td><strong>#{{RequestId}}</strong></td></tr>
                    <tr><td>Staff:</td><td>{{RequesterName}}</td></tr>
                    <tr><td>Payment Amount:</td><td><strong>USD {{RefundUsdAmount}}</strong></td></tr>
                    <tr><td>Umoja Doc ID:</td><td>{{UmojaPaymentDocumentId}}</td></tr>
                    <tr><td>Approvals:</td><td>✓ Supervisor ✓ Budget ✓ Claims</td></tr>
                </table>
                <div style="text-align: center;">
                    <a href="{{ApprovalLink}}" style="background-color: #e74c3c; color: #ffffff; text-decoration: none; padding: 12px 30px; border-radius: 5px; display: inline-block;">Approve Payment</a>
                </div>
            </td></tr>
            <tr><td style="background-color: #f8f9fa; padding: 20px; text-align: center;"><p style="color: #999999; font-size: 12px; margin: 0;">© {{Year}} TAB System</p></td></tr>
        </table>
    </td></tr></table>
</body></html>',
    NULL,
    'Notification to payment approver for final approval',
    'Device Reimbursement',
    'RequestId, RequesterName, RefundUsdAmount, UmojaPaymentDocumentId, ApprovalLink, Year',
    1,
    1,
    GETUTCDATE()
    );
    PRINT '✓ REFUND_PAYMENT_APPROVER_NOTIFICATION installed successfully'
END
ELSE
    PRINT '⊗ REFUND_PAYMENT_APPROVER_NOTIFICATION already exists - skipping'
PRINT ''

-- 9. PAYMENT APPROVED / COMPLETED
PRINT 'Installing: REFUND_PAYMENT_APPROVED'
IF NOT EXISTS (SELECT 1 FROM EmailTemplates WHERE TemplateCode = 'REFUND_PAYMENT_APPROVED')
BEGIN
    INSERT INTO EmailTemplates (TemplateCode, Name, Subject, HtmlBody, PlainTextBody, Description, Category, AvailablePlaceholders, IsActive, IsSystemTemplate, CreatedDate)
    VALUES (
    'REFUND_PAYMENT_APPROVED',
    'Device Reimbursement - Payment Approved',
    'Reimbursement Approved & Completed - Request #{{RequestId}}',
    '<!DOCTYPE html>
<html><head><meta charset="UTF-8"></head>
<body style="margin: 0; padding: 0; font-family: Arial, sans-serif; background-color: #f4f4f4;">
    <table width="100%" cellpadding="20"><tr><td align="center">
        <table width="600" style="background-color: #ffffff; border-radius: 8px;">
            <tr><td style="background-color: #27ae60; padding: 30px; text-align: center; color: #ffffff;"><div style="font-size: 50px;">🎉</div><h1 style="margin: 10px 0 0 0;">Payment Approved!</h1></td></tr>
            <tr><td style="padding: 30px;">
                <p style="font-size: 16px;">Dear <strong>{{RequesterName}}</strong>,</p>
                <p style="color: #555555;">Congratulations! Your device reimbursement has been approved and the payment is being processed.</p>
                <table width="100%" style="background-color: #d4edda; padding: 15px; margin: 20px 0; border: 2px solid #27ae60;">
                    <tr><td>Request ID:</td><td><strong>#{{RequestId}}</strong></td></tr>
                    <tr><td>Approved Amount:</td><td><strong style="font-size: 18px; color: #27ae60;">USD {{RefundUsdAmount}}</strong></td></tr>
                    <tr><td>Payment Reference:</td><td>{{PaymentReference}}</td></tr>
                    <tr><td>Completion Date:</td><td>{{CompletionDate}}</td></tr>
                    <tr><td>Status:</td><td><span style="background-color: #27ae60; color: #ffffff; padding: 3px 10px; border-radius: 10px; font-size: 12px; font-weight: 600;">COMPLETED</span></td></tr>
                </table>
                <div style="background-color: #e3f2fd; padding: 15px; margin: 20px 0; border-left: 4px solid #2196F3;">
                    <p style="margin: 0; color: #1976d2;"><strong>Next Steps:</strong> The payment will be processed according to standard procedures. Please allow 5-7 business days for the funds to reflect in your account.</p>
                </div>
                <div style="text-align: center;">
                    <a href="{{ViewRequestLink}}" style="background-color: #27ae60; color: #ffffff; text-decoration: none; padding: 12px 30px; border-radius: 5px; display: inline-block;">View Completed Request</a>
                </div>
            </td></tr>
            <tr><td style="background-color: #f8f9fa; padding: 20px; text-align: center;"><p style="color: #999999; font-size: 12px; margin: 0;">© {{Year}} TAB System</p></td></tr>
        </table>
    </td></tr></table>
</body></html>',
    NULL,
    'Notification to requester when payment is approved and request completed',
    'Device Reimbursement',
    'RequestId, RequesterName, RefundUsdAmount, PaymentReference, CompletionDate, ViewRequestLink, Year',
    1,
    1,
    GETUTCDATE()
    );
    PRINT '✓ REFUND_PAYMENT_APPROVED installed successfully'
END
ELSE
    PRINT '⊗ REFUND_PAYMENT_APPROVED already exists - skipping'
PRINT ''

-- 10. REQUEST REJECTED
PRINT 'Installing: REFUND_REQUEST_REJECTED'
IF NOT EXISTS (SELECT 1 FROM EmailTemplates WHERE TemplateCode = 'REFUND_REQUEST_REJECTED')
BEGIN
    INSERT INTO EmailTemplates (TemplateCode, Name, Subject, HtmlBody, PlainTextBody, Description, Category, AvailablePlaceholders, IsActive, IsSystemTemplate, CreatedDate)
    VALUES (
    'REFUND_REQUEST_REJECTED',
    'Device Reimbursement - Request Rejected',
    'Device Reimbursement Request Rejected - #{{RequestId}}',
    '<!DOCTYPE html>
<html><head><meta charset="UTF-8"></head>
<body style="margin: 0; padding: 0; font-family: Arial, sans-serif; background-color: #f4f4f4;">
    <table width="100%" cellpadding="20"><tr><td align="center">
        <table width="600" style="background-color: #ffffff; border-radius: 8px;">
            <tr><td style="background-color: #e74c3c; padding: 30px; text-align: center; color: #ffffff;"><h1 style="margin: 0;">Request Not Approved</h1></td></tr>
            <tr><td style="padding: 30px;">
                <p style="font-size: 16px;">Dear <strong>{{RequesterName}}</strong>,</p>
                <p style="color: #555555;">Your device reimbursement request #{{RequestId}} has been rejected.</p>
                <table width="100%" style="background-color: #fadbd8; padding: 15px; margin: 20px 0; border-left: 4px solid #e74c3c;">
                    <tr><td>Request ID:</td><td><strong>#{{RequestId}}</strong></td></tr>
                    <tr><td>Rejected By:</td><td>{{RejectedBy}}</td></tr>
                    <tr><td>Date:</td><td>{{RejectionDate}}</td></tr>
                    <tr><td>Reason:</td><td style="color: #721c24;"><strong>{{RejectionReason}}</strong></td></tr>
                </table>
                <div style="background-color: #fff3cd; padding: 15px; margin: 20px 0; border-left: 4px solid #ffc107;">
                    <p style="margin: 0; color: #856404;">If you believe this decision was made in error or would like to discuss this further, please contact {{RejectedBy}} or the HR department.</p>
                </div>
                <div style="text-align: center;">
                    <a href="{{NewRequestLink}}" style="background-color: #667eea; color: #ffffff; text-decoration: none; padding: 12px 30px; border-radius: 5px; display: inline-block;">Submit New Request</a>
                </div>
            </td></tr>
            <tr><td style="background-color: #f8f9fa; padding: 20px; text-align: center;"><p style="color: #999999; font-size: 12px; margin: 0;">© {{Year}} TAB System</p></td></tr>
        </table>
    </td></tr></table>
</body></html>',
    NULL,
    'Notification to requester when request is rejected or cancelled',
    'Device Reimbursement',
    'RequestId, RequesterName, RejectedBy, RejectionDate, RejectionReason, NewRequestLink, Year',
    1,
    1,
    GETUTCDATE()
    );
    PRINT '✓ REFUND_REQUEST_REJECTED installed successfully'
END
ELSE
    PRINT '⊗ REFUND_REQUEST_REJECTED already exists - skipping'
PRINT ''

PRINT '===================================='
PRINT 'Device Reimbursement Email Template Installation Complete!'
PRINT 'Total Templates: 10'
PRINT '===================================='
GO
