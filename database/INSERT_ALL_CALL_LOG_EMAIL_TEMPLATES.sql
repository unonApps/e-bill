-- =============================================
-- Call Log Verification Workflow - Complete Email Templates
-- =============================================
-- This script inserts all email templates needed for the complete Call Log Verification workflow
-- Total templates: 7
--
-- Templates included:
-- 1. CALL_LOG_SUBMITTED_CONFIRMATION - Confirmation to staff after submission
-- 2. CALL_LOG_SUPERVISOR_NOTIFICATION - Notification to supervisor
-- 3. CALL_LOG_APPROVED - Full approval notification to staff
-- 4. CALL_LOG_PARTIALLY_APPROVED - Partial approval notification to staff
-- 5. CALL_LOG_REJECTED - Rejection notification to staff
-- 6. CALL_LOG_REVERTED - Revert notification to staff
-- 7. CALL_LOG_DEADLINE_REMINDER - Deadline reminder to staff
-- =============================================

USE TABDB;
GO

PRINT '============================================='
PRINT 'Installing Call Log Verification Email Templates'
PRINT '============================================='
PRINT ''

-- =============================================
-- Template 1: Call Log Submitted - Confirmation to Staff
-- =============================================

PRINT 'Installing: CALL_LOG_SUBMITTED_CONFIRMATION'
IF NOT EXISTS (SELECT 1 FROM EmailTemplates WHERE TemplateCode = 'CALL_LOG_SUBMITTED_CONFIRMATION')
BEGIN
    INSERT INTO EmailTemplates (TemplateCode, Name, Subject, HtmlBody, PlainTextBody, Description, Category, AvailablePlaceholders, IsActive, IsSystemTemplate, CreatedDate)
    VALUES (
    'CALL_LOG_SUBMITTED_CONFIRMATION',
    'Call Log Verification - Submitted Confirmation',
    'Call Log Verification Submitted - {{Month}} {{Year}}',
    '<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Call Log Verification Submitted</title>
</head>
<body style="margin: 0; padding: 0; font-family: Arial, sans-serif; background-color: #f4f4f4;">
    <table cellpadding="0" cellspacing="0" border="0" width="100%" style="background-color: #f4f4f4; padding: 20px;">
        <tr>
            <td align="center">
                <table cellpadding="0" cellspacing="0" border="0" width="600" style="background-color: #ffffff; border-radius: 8px;">
                    <tr>
                        <td style="background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); padding: 30px; text-align: center; border-radius: 8px 8px 0 0;">
                            <h1 style="color: #ffffff; margin: 0; font-size: 24px;">✓ Verification Submitted</h1>
                        </td>
                    </tr>
                    <tr>
                        <td style="padding: 30px;">
                            <p style="color: #333333; font-size: 16px; margin: 0 0 20px 0;">Dear <strong>{{StaffName}}</strong>,</p>
                            <p style="color: #555555; font-size: 14px; line-height: 1.6; margin: 0 0 20px 0;">
                                Your call log verification for <strong>{{Month}} {{Year}}</strong> has been successfully submitted to your supervisor for approval.
                            </p>
                            <table cellpadding="0" cellspacing="0" border="0" width="100%" style="background-color: #f8f9fa; padding: 20px; margin: 0 0 20px 0; border-radius: 5px;">
                                <tr><td style="padding: 8px 0; color: #666666; font-size: 14px;">Index Number:</td><td style="padding: 8px 0; font-weight: 600; text-align: right;">{{IndexNumber}}</td></tr>
                                <tr><td style="padding: 8px 0; color: #666666; font-size: 14px;">Period:</td><td style="padding: 8px 0; font-weight: 600; text-align: right;">{{Month}} {{Year}}</td></tr>
                                <tr><td style="padding: 8px 0; color: #666666; font-size: 14px;">Total Calls:</td><td style="padding: 8px 0; font-weight: 600; text-align: right;">{{TotalCalls}}</td></tr>
                                <tr><td style="padding: 8px 0; color: #666666; font-size: 14px;">Official Calls Cost:</td><td style="padding: 8px 0; font-weight: 600; font-size: 16px; color: #667eea; text-align: right;">USD {{TotalAmount}}</td></tr>
                                <tr><td style="padding: 8px 0; color: #666666; font-size: 14px;">Monthly Allowance:</td><td style="padding: 8px 0; font-weight: 600; text-align: right;">USD {{MonthlyAllowance}}</td></tr>
                                <tr><td style="padding: 8px 0; color: #666666; font-size: 14px;">Supervisor:</td><td style="padding: 8px 0; font-weight: 600; text-align: right;">{{SupervisorName}}</td></tr>
                            </table>
                            <div style="background-color: {{OverageBackgroundColor}}; padding: 15px; margin: 20px 0; border-left: 4px solid {{OverageBorderColor}}; border-radius: 4px;">
                                <p style="margin: 0; color: {{OverageTextColor}}; font-size: 14px;">
                                    <strong>{{OverageMessage}}</strong>
                                </p>
                            </div>
                            <div style="background-color: #e3f2fd; padding: 15px; margin: 20px 0; border-left: 4px solid #2196F3; border-radius: 4px;">
                                <p style="margin: 0; color: #1976d2; font-size: 14px;">
                                    <strong>Next Steps:</strong> Your supervisor will review your submission and take action. You will receive an email notification once the review is complete.
                                </p>
                            </div>
                            <div style="text-align: center; margin: 20px 0;">
                                <a href="{{ViewCallLogsLink}}" style="background-color: #667eea; color: #ffffff; text-decoration: none; padding: 12px 30px; border-radius: 5px; display: inline-block; font-weight: 600;">View My Call Logs</a>
                            </div>
                        </td>
                    </tr>
                    <tr>
                        <td style="background-color: #f8f9fa; padding: 20px; text-align: center; border-radius: 0 0 8px 8px;">
                            <p style="color: #999999; font-size: 12px; margin: 0;">© {{Year}} TAB System - Telephone Allowance & Billing</p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>',
    NULL,
    'Confirmation email sent to staff after submitting call log verification to supervisor',
    'Call Log Verification',
    'StaffName, IndexNumber, Month, Year, TotalCalls, TotalAmount, MonthlyAllowance, SupervisorName, OverageMessage, OverageBackgroundColor, OverageBorderColor, OverageTextColor, ViewCallLogsLink, Year',
    1,
    1,
    GETUTCDATE()
    );
    PRINT '✓ CALL_LOG_SUBMITTED_CONFIRMATION installed successfully'
END
ELSE
    PRINT '⊗ CALL_LOG_SUBMITTED_CONFIRMATION already exists - skipping'
PRINT ''

-- =============================================
-- Template 2: Supervisor Notification
-- =============================================

PRINT 'Installing: CALL_LOG_SUPERVISOR_NOTIFICATION'
IF NOT EXISTS (SELECT 1 FROM EmailTemplates WHERE TemplateCode = 'CALL_LOG_SUPERVISOR_NOTIFICATION')
BEGIN
    INSERT INTO EmailTemplates (TemplateCode, Name, Subject, HtmlBody, PlainTextBody, Description, Category, AvailablePlaceholders, IsActive, IsSystemTemplate, CreatedDate)
    VALUES (
    'CALL_LOG_SUPERVISOR_NOTIFICATION',
    'Call Log Verification - Supervisor Notification',
    'Call Log Verification Pending - {{StaffName}} ({{Month}} {{Year}})',
    '<!DOCTYPE html>
<html><head><meta charset="UTF-8"><meta name="viewport" content="width=device-width, initial-scale=1.0"></head>
<body style="margin: 0; padding: 0; font-family: Arial, sans-serif; background-color: #f4f4f4;">
    <table width="100%" cellpadding="20" style="background-color: #f4f4f4;"><tr><td align="center">
        <table width="600" style="background-color: #ffffff; border-radius: 8px;">
            <tr><td style="background: linear-gradient(135deg, #f5576c 0%, #e74c3c 100%); padding: 30px; text-align: center; color: #ffffff; border-radius: 8px 8px 0 0;">
                <h1 style="margin: 0; font-size: 24px;">⏰ Approval Required</h1>
            </td></tr>
            <tr><td style="padding: 30px;">
                <p style="font-size: 16px; color: #333;">Dear <strong>{{SupervisorName}}</strong>,</p>
                <p style="color: #555555; font-size: 14px; line-height: 1.6;">
                    <strong>{{StaffName}}</strong> has submitted their call log verification for <strong>{{Month}} {{Year}}</strong> for your approval.
                </p>
                <table width="100%" style="background-color: #fff3cd; padding: 20px; margin: 20px 0; border-radius: 5px;">
                    <tr><td style="color: #856404; padding: 5px 0;">Staff Name:</td><td style="font-weight: 600; text-align: right;"><strong>{{StaffName}}</strong></td></tr>
                    <tr><td style="color: #856404; padding: 5px 0;">Index Number:</td><td style="font-weight: 600; text-align: right;">{{IndexNumber}}</td></tr>
                    <tr><td style="color: #856404; padding: 5px 0;">Period:</td><td style="font-weight: 600; text-align: right;">{{Month}} {{Year}}</td></tr>
                    <tr><td style="color: #856404; padding: 5px 0;">Total Official Calls:</td><td style="font-weight: 600; text-align: right;">{{TotalCalls}}</td></tr>
                    <tr><td style="color: #856404; padding: 5px 0;">Total Cost:</td><td style="font-weight: 600; font-size: 16px; text-align: right;"><strong>USD {{TotalAmount}}</strong></td></tr>
                    <tr><td style="color: #856404; padding: 5px 0;">Monthly Allowance:</td><td style="font-weight: 600; text-align: right;">USD {{MonthlyAllowance}}</td></tr>
                </table>
                <div style="background-color: {{OverageBackgroundColor}}; padding: 15px; margin: 20px 0; border-left: 4px solid {{OverageBorderColor}}; border-radius: 4px;">
                    <p style="margin: 0; color: {{OverageTextColor}}; font-weight: 600;">{{OverageMessage}}</p>
                    <p style="margin: 10px 0 0 0; color: {{OverageTextColor}}; font-size: 13px;">{{JustificationText}}</p>
                </div>
                <div style="background-color: #e3f2fd; padding: 15px; margin: 20px 0; border-left: 4px solid #2196F3; border-radius: 4px;">
                    <p style="margin: 0; color: #1976d2; font-size: 14px;">
                        <strong>Action Required:</strong> Please review and approve, partially approve, reject, or revert the submission.
                    </p>
                </div>
                <div style="text-align: center; margin: 20px 0;">
                    <a href="{{ApprovalLink}}" style="background-color: #f5576c; color: #ffffff; text-decoration: none; padding: 14px 35px; border-radius: 5px; display: inline-block; font-weight: 600; font-size: 15px;">Review & Approve</a>
                </div>
            </td></tr>
            <tr><td style="background-color: #f8f9fa; padding: 20px; text-align: center; border-radius: 0 0 8px 8px;">
                <p style="color: #999999; font-size: 12px; margin: 0;">© {{Year}} TAB System - Telephone Allowance & Billing</p>
            </td></tr>
        </table>
    </td></tr></table>
</body></html>',
    NULL,
    'Notification to supervisor when staff submits call log verification',
    'Call Log Verification',
    'SupervisorName, StaffName, IndexNumber, Month, Year, TotalCalls, TotalAmount, MonthlyAllowance, OverageMessage, OverageBackgroundColor, OverageBorderColor, OverageTextColor, JustificationText, ApprovalLink, Year',
    1,
    1,
    GETUTCDATE()
    );
    PRINT '✓ CALL_LOG_SUPERVISOR_NOTIFICATION installed successfully'
END
ELSE
    PRINT '⊗ CALL_LOG_SUPERVISOR_NOTIFICATION already exists - skipping'
PRINT ''

-- =============================================
-- Template 3: Call Log Approved (Full)
-- =============================================

PRINT 'Installing: CALL_LOG_APPROVED'
IF NOT EXISTS (SELECT 1 FROM EmailTemplates WHERE TemplateCode = 'CALL_LOG_APPROVED')
BEGIN
    INSERT INTO EmailTemplates (TemplateCode, Name, Subject, HtmlBody, PlainTextBody, Description, Category, AvailablePlaceholders, IsActive, IsSystemTemplate, CreatedDate)
    VALUES (
    'CALL_LOG_APPROVED',
    'Call Log Verification - Approved',
    'Call Log Approved - {{Month}} {{Year}}',
    '<!DOCTYPE html>
<html><head><meta charset="UTF-8"><meta name="viewport" content="width=device-width, initial-scale=1.0"></head>
<body style="margin: 0; padding: 0; font-family: Arial, sans-serif; background-color: #f4f4f4;">
    <table width="100%" cellpadding="20" style="background-color: #f4f4f4;"><tr><td align="center">
        <table width="600" style="background-color: #ffffff; border-radius: 8px;">
            <tr><td style="background: linear-gradient(135deg, #38ef7d 0%, #11998e 100%); padding: 30px; text-align: center; color: #ffffff; border-radius: 8px 8px 0 0;">
                <div style="font-size: 50px; margin-bottom: 10px;">✓</div>
                <h1 style="margin: 0; font-size: 24px;">Call Log Approved!</h1>
            </td></tr>
            <tr><td style="padding: 30px;">
                <p style="font-size: 16px; color: #333;">Dear <strong>{{StaffName}}</strong>,</p>
                <p style="color: #555555; font-size: 14px; line-height: 1.6;">
                    Great news! Your supervisor has <strong>approved</strong> your call log verification for <strong>{{Month}} {{Year}}</strong>.
                </p>
                <table width="100%" style="background-color: #d4edda; padding: 20px; margin: 20px 0; border: 2px solid #28a745; border-radius: 5px;">
                    <tr><td style="padding: 5px 0; color: #155724;">Period:</td><td style="font-weight: 600; text-align: right;">{{Month}} {{Year}}</td></tr>
                    <tr><td style="padding: 5px 0; color: #155724;">Total Calls:</td><td style="font-weight: 600; text-align: right;">{{TotalCalls}}</td></tr>
                    <tr><td style="padding: 5px 0; color: #155724;">Approved Amount:</td><td style="font-weight: 600; font-size: 18px; color: #28a745; text-align: right;"><strong>USD {{ApprovedAmount}}</strong></td></tr>
                    <tr><td style="padding: 5px 0; color: #155724;">Approved By:</td><td style="font-weight: 600; text-align: right;">{{SupervisorName}}</td></tr>
                    <tr><td style="padding: 5px 0; color: #155724;">Approval Date:</td><td style="font-weight: 600; text-align: right;">{{ApprovedDate}}</td></tr>
                    <tr><td style="padding: 5px 0; color: #155724;">Status:</td><td style="text-align: right;"><span style="background-color: #28a745; color: #ffffff; padding: 4px 12px; border-radius: 12px; font-size: 12px; font-weight: 600;">APPROVED</span></td></tr>
                </table>
                <div style="background-color: #f8f9fa; padding: 15px; margin: 20px 0; border-left: 4px solid #667eea; border-radius: 4px;">
                    <p style="margin: 0; color: #495057; font-size: 14px;"><strong>Supervisor Comments:</strong></p>
                    <p style="margin: 5px 0 0 0; color: #6c757d; font-size: 13px; font-style: italic;">{{SupervisorComments}}</p>
                </div>
                <div style="background-color: #e3f2fd; padding: 15px; margin: 20px 0; border-left: 4px solid #2196F3; border-radius: 4px;">
                    <p style="margin: 0; color: #1976d2; font-size: 14px;">
                        <strong>Payment:</strong> The approved amount will be covered by the organization. No payment required from you.
                    </p>
                </div>
                <div style="text-align: center; margin: 20px 0;">
                    <a href="{{ViewCallLogsLink}}" style="background-color: #28a745; color: #ffffff; text-decoration: none; padding: 12px 30px; border-radius: 5px; display: inline-block; font-weight: 600;">View Call Logs</a>
                </div>
            </td></tr>
            <tr><td style="background-color: #f8f9fa; padding: 20px; text-align: center; border-radius: 0 0 8px 8px;">
                <p style="color: #999999; font-size: 12px; margin: 0;">© {{Year}} TAB System - Telephone Allowance & Billing</p>
            </td></tr>
        </table>
    </td></tr></table>
</body></html>',
    NULL,
    'Notification to staff when supervisor fully approves call log verification',
    'Call Log Verification',
    'StaffName, Month, Year, TotalCalls, ApprovedAmount, SupervisorName, ApprovedDate, SupervisorComments, ViewCallLogsLink, Year',
    1,
    1,
    GETUTCDATE()
    );
    PRINT '✓ CALL_LOG_APPROVED installed successfully'
END
ELSE
    PRINT '⊗ CALL_LOG_APPROVED already exists - skipping'
PRINT ''

-- =============================================
-- Template 4: Call Log Partially Approved
-- =============================================

PRINT 'Installing: CALL_LOG_PARTIALLY_APPROVED'
IF NOT EXISTS (SELECT 1 FROM EmailTemplates WHERE TemplateCode = 'CALL_LOG_PARTIALLY_APPROVED')
BEGIN
    INSERT INTO EmailTemplates (TemplateCode, Name, Subject, HtmlBody, PlainTextBody, Description, Category, AvailablePlaceholders, IsActive, IsSystemTemplate, CreatedDate)
    VALUES (
    'CALL_LOG_PARTIALLY_APPROVED',
    'Call Log Verification - Partially Approved',
    'Call Log Partially Approved - {{Month}} {{Year}}',
    '<!DOCTYPE html>
<html><head><meta charset="UTF-8"><meta name="viewport" content="width=device-width, initial-scale=1.0"></head>
<body style="margin: 0; padding: 0; font-family: Arial, sans-serif; background-color: #f4f4f4;">
    <table width="100%" cellpadding="20" style="background-color: #f4f4f4;"><tr><td align="center">
        <table width="600" style="background-color: #ffffff; border-radius: 8px;">
            <tr><td style="background: linear-gradient(135deg, #ffc107 0%, #ff9800 100%); padding: 30px; text-align: center; color: #ffffff; border-radius: 8px 8px 0 0;">
                <h1 style="margin: 0; font-size: 24px;">⚠ Partially Approved</h1>
            </td></tr>
            <tr><td style="padding: 30px;">
                <p style="font-size: 16px; color: #333;">Dear <strong>{{StaffName}}</strong>,</p>
                <p style="color: #555555; font-size: 14px; line-height: 1.6;">
                    Your supervisor has <strong>partially approved</strong> your call log verification for <strong>{{Month}} {{Year}}</strong>.
                </p>
                <table width="100%" style="background-color: #fff3cd; padding: 20px; margin: 20px 0; border: 2px solid #ffc107; border-radius: 5px;">
                    <tr><td style="padding: 5px 0; color: #856404;">Period:</td><td style="font-weight: 600; text-align: right;">{{Month}} {{Year}}</td></tr>
                    <tr><td style="padding: 5px 0; color: #856404;">Original Amount:</td><td style="font-weight: 600; text-align: right; text-decoration: line-through;">USD {{TotalAmount}}</td></tr>
                    <tr><td style="padding: 5px 0; color: #856404;">Approved Amount:</td><td style="font-weight: 600; font-size: 16px; color: #28a745; text-align: right;"><strong>USD {{ApprovedAmount}}</strong></td></tr>
                    <tr><td style="padding: 5px 0; color: #856404;">You Must Pay:</td><td style="font-weight: 600; font-size: 18px; color: #dc3545; text-align: right;"><strong>USD {{StaffPayableAmount}}</strong></td></tr>
                    <tr><td style="padding: 5px 0; color: #856404;">Reviewed By:</td><td style="font-weight: 600; text-align: right;">{{SupervisorName}}</td></tr>
                    <tr><td style="padding: 5px 0; color: #856404;">Status:</td><td style="text-align: right;"><span style="background-color: #ffc107; color: #000000; padding: 4px 12px; border-radius: 12px; font-size: 12px; font-weight: 600;">PARTIALLY APPROVED</span></td></tr>
                </table>
                <div style="background-color: #f8f9fa; padding: 15px; margin: 20px 0; border-left: 4px solid #667eea; border-radius: 4px;">
                    <p style="margin: 0; color: #495057; font-size: 14px;"><strong>Supervisor Comments:</strong></p>
                    <p style="margin: 5px 0 0 0; color: #6c757d; font-size: 13px; font-style: italic;">{{SupervisorComments}}</p>
                </div>
                <div style="background-color: #fadbd8; padding: 15px; margin: 20px 0; border-left: 4px solid #dc3545; border-radius: 4px;">
                    <p style="margin: 0; color: #721c24; font-size: 14px;">
                        <strong>Payment Required:</strong> The organization will cover <strong>USD {{ApprovedAmount}}</strong>. You are responsible for paying <strong>USD {{StaffPayableAmount}}</strong>. Payment instructions will be provided separately.
                    </p>
                </div>
                <div style="text-align: center; margin: 20px 0;">
                    <a href="{{ViewCallLogsLink}}" style="background-color: #ffc107; color: #000000; text-decoration: none; padding: 12px 30px; border-radius: 5px; display: inline-block; font-weight: 600;">View Call Logs</a>
                </div>
            </td></tr>
            <tr><td style="background-color: #f8f9fa; padding: 20px; text-align: center; border-radius: 0 0 8px 8px;">
                <p style="color: #999999; font-size: 12px; margin: 0;">© {{Year}} TAB System - Telephone Allowance & Billing</p>
            </td></tr>
        </table>
    </td></tr></table>
</body></html>',
    NULL,
    'Notification to staff when supervisor partially approves call log verification',
    'Call Log Verification',
    'StaffName, Month, Year, TotalAmount, ApprovedAmount, StaffPayableAmount, SupervisorName, SupervisorComments, ViewCallLogsLink, Year',
    1,
    1,
    GETUTCDATE()
    );
    PRINT '✓ CALL_LOG_PARTIALLY_APPROVED installed successfully'
END
ELSE
    PRINT '⊗ CALL_LOG_PARTIALLY_APPROVED already exists - skipping'
PRINT ''

-- =============================================
-- Template 5: Call Log Rejected
-- =============================================

PRINT 'Installing: CALL_LOG_REJECTED'
IF NOT EXISTS (SELECT 1 FROM EmailTemplates WHERE TemplateCode = 'CALL_LOG_REJECTED')
BEGIN
    INSERT INTO EmailTemplates (TemplateCode, Name, Subject, HtmlBody, PlainTextBody, Description, Category, AvailablePlaceholders, IsActive, IsSystemTemplate, CreatedDate)
    VALUES (
    'CALL_LOG_REJECTED',
    'Call Log Verification - Rejected',
    'Call Log Rejected - {{Month}} {{Year}}',
    '<!DOCTYPE html>
<html><head><meta charset="UTF-8"><meta name="viewport" content="width=device-width, initial-scale=1.0"></head>
<body style="margin: 0; padding: 0; font-family: Arial, sans-serif; background-color: #f4f4f4;">
    <table width="100%" cellpadding="20" style="background-color: #f4f4f4;"><tr><td align="center">
        <table width="600" style="background-color: #ffffff; border-radius: 8px;">
            <tr><td style="background-color: #dc3545; padding: 30px; text-align: center; color: #ffffff; border-radius: 8px 8px 0 0;">
                <h1 style="margin: 0; font-size: 24px;">✗ Call Log Rejected</h1>
            </td></tr>
            <tr><td style="padding: 30px;">
                <p style="font-size: 16px; color: #333;">Dear <strong>{{StaffName}}</strong>,</p>
                <p style="color: #555555; font-size: 14px; line-height: 1.6;">
                    Your supervisor has <strong>rejected</strong> your call log verification for <strong>{{Month}} {{Year}}</strong>.
                </p>
                <table width="100%" style="background-color: #fadbd8; padding: 20px; margin: 20px 0; border: 2px solid #dc3545; border-radius: 5px;">
                    <tr><td style="padding: 5px 0; color: #721c24;">Period:</td><td style="font-weight: 600; text-align: right;">{{Month}} {{Year}}</td></tr>
                    <tr><td style="padding: 5px 0; color: #721c24;">Total Amount:</td><td style="font-weight: 600; font-size: 18px; color: #dc3545; text-align: right;"><strong>USD {{TotalAmount}}</strong></td></tr>
                    <tr><td style="padding: 5px 0; color: #721c24;">You Must Pay:</td><td style="font-weight: 600; font-size: 18px; color: #dc3545; text-align: right;"><strong>USD {{StaffPayableAmount}}</strong></td></tr>
                    <tr><td style="padding: 5px 0; color: #721c24;">Rejected By:</td><td style="font-weight: 600; text-align: right;">{{SupervisorName}}</td></tr>
                    <tr><td style="padding: 5px 0; color: #721c24;">Rejection Date:</td><td style="font-weight: 600; text-align: right;">{{RejectionDate}}</td></tr>
                    <tr><td style="padding: 5px 0; color: #721c24;">Status:</td><td style="text-align: right;"><span style="background-color: #dc3545; color: #ffffff; padding: 4px 12px; border-radius: 12px; font-size: 12px; font-weight: 600;">REJECTED</span></td></tr>
                </table>
                <div style="background-color: #f8f9fa; padding: 15px; margin: 20px 0; border-left: 4px solid #dc3545; border-radius: 4px;">
                    <p style="margin: 0; color: #495057; font-size: 14px;"><strong>Rejection Reason:</strong></p>
                    <p style="margin: 5px 0 0 0; color: #dc3545; font-size: 14px; font-weight: 600;">{{RejectionReason}}</p>
                </div>
                <div style="background-color: #fadbd8; padding: 15px; margin: 20px 0; border-left: 4px solid #dc3545; border-radius: 4px;">
                    <p style="margin: 0; color: #721c24; font-size: 14px;">
                        <strong>Payment Required:</strong> Since your call log was rejected, you are responsible for paying the full amount of <strong>USD {{StaffPayableAmount}}</strong>. Payment instructions will be provided separately by the Finance department.
                    </p>
                </div>
                <div style="background-color: #fff3cd; padding: 15px; margin: 20px 0; border-left: 4px solid #ffc107; border-radius: 4px;">
                    <p style="margin: 0; color: #856404; font-size: 14px;">
                        If you believe this decision was made in error or would like to discuss this further, please contact your supervisor <strong>{{SupervisorName}}</strong>.
                    </p>
                </div>
                <div style="text-align: center; margin: 20px 0;">
                    <a href="{{ViewCallLogsLink}}" style="background-color: #6c757d; color: #ffffff; text-decoration: none; padding: 12px 30px; border-radius: 5px; display: inline-block; font-weight: 600;">View Call Logs</a>
                </div>
            </td></tr>
            <tr><td style="background-color: #f8f9fa; padding: 20px; text-align: center; border-radius: 0 0 8px 8px;">
                <p style="color: #999999; font-size: 12px; margin: 0;">© {{Year}} TAB System - Telephone Allowance & Billing</p>
            </td></tr>
        </table>
    </td></tr></table>
</body></html>',
    NULL,
    'Notification to staff when supervisor rejects call log verification',
    'Call Log Verification',
    'StaffName, Month, Year, TotalAmount, StaffPayableAmount, SupervisorName, RejectionDate, RejectionReason, ViewCallLogsLink, Year',
    1,
    1,
    GETUTCDATE()
    );
    PRINT '✓ CALL_LOG_REJECTED installed successfully'
END
ELSE
    PRINT '⊗ CALL_LOG_REJECTED already exists - skipping'
PRINT ''

-- =============================================
-- Template 6: Call Log Reverted
-- =============================================

PRINT 'Installing: CALL_LOG_REVERTED'
IF NOT EXISTS (SELECT 1 FROM EmailTemplates WHERE TemplateCode = 'CALL_LOG_REVERTED')
BEGIN
    INSERT INTO EmailTemplates (TemplateCode, Name, Subject, HtmlBody, PlainTextBody, Description, Category, AvailablePlaceholders, IsActive, IsSystemTemplate, CreatedDate)
    VALUES (
    'CALL_LOG_REVERTED',
    'Call Log Verification - Reverted for Revision',
    'Call Log Returned for Revision - {{Month}} {{Year}}',
    '<!DOCTYPE html>
<html><head><meta charset="UTF-8"><meta name="viewport" content="width=device-width, initial-scale=1.0"></head>
<body style="margin: 0; padding: 0; font-family: Arial, sans-serif; background-color: #f4f4f4;">
    <table width="100%" cellpadding="20" style="background-color: #f4f4f4;"><tr><td align="center">
        <table width="600" style="background-color: #ffffff; border-radius: 8px;">
            <tr><td style="background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); padding: 30px; text-align: center; color: #ffffff; border-radius: 8px 8px 0 0;">
                <h1 style="margin: 0; font-size: 24px;">↩ Action Required</h1>
            </td></tr>
            <tr><td style="padding: 30px;">
                <p style="font-size: 16px; color: #333;">Dear <strong>{{StaffName}}</strong>,</p>
                <p style="color: #555555; font-size: 14px; line-height: 1.6;">
                    Your supervisor has <strong>reverted</strong> your call log verification for <strong>{{Month}} {{Year}}</strong> back to you for revision.
                </p>
                <table width="100%" style="background-color: #e3f2fd; padding: 20px; margin: 20px 0; border: 2px solid #2196F3; border-radius: 5px;">
                    <tr><td style="padding: 5px 0; color: #1976d2;">Period:</td><td style="font-weight: 600; text-align: right;">{{Month}} {{Year}}</td></tr>
                    <tr><td style="padding: 5px 0; color: #1976d2;">Total Calls:</td><td style="font-weight: 600; text-align: right;">{{TotalCalls}}</td></tr>
                    <tr><td style="padding: 5px 0; color: #1976d2;">Reverted By:</td><td style="font-weight: 600; text-align: right;">{{SupervisorName}}</td></tr>
                    <tr><td style="padding: 5px 0; color: #1976d2;">Revert Date:</td><td style="font-weight: 600; text-align: right;">{{RevertDate}}</td></tr>
                    <tr><td style="padding: 5px 0; color: #1976d2;">Deadline to Resubmit:</td><td style="font-weight: 600; color: #dc3545; text-align: right;"><strong>{{RevertDeadline}}</strong></td></tr>
                    <tr><td style="padding: 5px 0; color: #1976d2;">Status:</td><td style="text-align: right;"><span style="background-color: #2196F3; color: #ffffff; padding: 4px 12px; border-radius: 12px; font-size: 12px; font-weight: 600;">REVERTED</span></td></tr>
                </table>
                <div style="background-color: #f8f9fa; padding: 15px; margin: 20px 0; border-left: 4px solid #667eea; border-radius: 4px;">
                    <p style="margin: 0; color: #495057; font-size: 14px;"><strong>Supervisor Comments:</strong></p>
                    <p style="margin: 5px 0 0 0; color: #6c757d; font-size: 13px; font-style: italic;">{{SupervisorComments}}</p>
                </div>
                <div style="background-color: #fff3cd; padding: 15px; margin: 20px 0; border-left: 4px solid #ffc107; border-radius: 4px;">
                    <p style="margin: 0 0 10px 0; color: #856404; font-size: 14px; font-weight: 600;">
                        Action Required:
                    </p>
                    <ul style="margin: 0; padding-left: 20px; color: #856404; font-size: 13px;">
                        <li>Review your supervisor''s comments</li>
                        <li>Re-verify the call records and make necessary corrections</li>
                        <li>Resubmit your verification before <strong>{{RevertDeadline}}</strong></li>
                    </ul>
                </div>
                <div style="background-color: #fadbd8; padding: 15px; margin: 20px 0; border-left: 4px solid #dc3545; border-radius: 4px;">
                    <p style="margin: 0; color: #721c24; font-size: 14px;">
                        <strong>Important:</strong> If you do not resubmit before the deadline, the calls may be marked as personal and you will be responsible for payment.
                    </p>
                </div>
                <div style="text-align: center; margin: 20px 0;">
                    <a href="{{ViewCallLogsLink}}" style="background-color: #667eea; color: #ffffff; text-decoration: none; padding: 12px 30px; border-radius: 5px; display: inline-block; font-weight: 600;">Re-verify Call Logs</a>
                </div>
            </td></tr>
            <tr><td style="background-color: #f8f9fa; padding: 20px; text-align: center; border-radius: 0 0 8px 8px;">
                <p style="color: #999999; font-size: 12px; margin: 0;">© {{Year}} TAB System - Telephone Allowance & Billing</p>
            </td></tr>
        </table>
    </td></tr></table>
</body></html>',
    NULL,
    'Notification to staff when supervisor reverts call log for revision',
    'Call Log Verification',
    'StaffName, Month, Year, TotalCalls, SupervisorName, RevertDate, RevertDeadline, SupervisorComments, ViewCallLogsLink, Year',
    1,
    1,
    GETUTCDATE()
    );
    PRINT '✓ CALL_LOG_REVERTED installed successfully'
END
ELSE
    PRINT '⊗ CALL_LOG_REVERTED already exists - skipping'
PRINT ''

-- =============================================
-- Template 7: Deadline Reminder (Optional)
-- =============================================

PRINT 'Installing: CALL_LOG_DEADLINE_REMINDER'
IF NOT EXISTS (SELECT 1 FROM EmailTemplates WHERE TemplateCode = 'CALL_LOG_DEADLINE_REMINDER')
BEGIN
    INSERT INTO EmailTemplates (TemplateCode, Name, Subject, HtmlBody, PlainTextBody, Description, Category, AvailablePlaceholders, IsActive, IsSystemTemplate, CreatedDate)
    VALUES (
    'CALL_LOG_DEADLINE_REMINDER',
    'Call Log Verification - Deadline Reminder',
    'Reminder: Call Log Verification Deadline - {{Month}} {{Year}}',
    '<!DOCTYPE html>
<html><head><meta charset="UTF-8"><meta name="viewport" content="width=device-width, initial-scale=1.0"></head>
<body style="margin: 0; padding: 0; font-family: Arial, sans-serif; background-color: #f4f4f4;">
    <table width="100%" cellpadding="20" style="background-color: #f4f4f4;"><tr><td align="center">
        <table width="600" style="background-color: #ffffff; border-radius: 8px;">
            <tr><td style="background: linear-gradient(135deg, #ff6b6b 0%, #feca57 100%); padding: 30px; text-align: center; color: #ffffff; border-radius: 8px 8px 0 0;">
                <div style="font-size: 50px; margin-bottom: 10px;">⏰</div>
                <h1 style="margin: 0; font-size: 24px;">Deadline Approaching</h1>
            </td></tr>
            <tr><td style="padding: 30px;">
                <p style="font-size: 16px; color: #333;">Dear <strong>{{StaffName}}</strong>,</p>
                <p style="color: #555555; font-size: 14px; line-height: 1.6;">
                    This is a friendly reminder that you have <strong>unverified call records</strong> for <strong>{{Month}} {{Year}}</strong> that need your attention.
                </p>
                <table width="100%" style="background-color: #fff3cd; padding: 20px; margin: 20px 0; border: 2px solid #ffc107; border-radius: 5px;">
                    <tr><td style="padding: 5px 0; color: #856404;">Period:</td><td style="font-weight: 600; text-align: right;">{{Month}} {{Year}}</td></tr>
                    <tr><td style="padding: 5px 0; color: #856404;">Unverified Calls:</td><td style="font-weight: 600; font-size: 16px; text-align: right;"><strong>{{UnverifiedCount}}</strong></td></tr>
                    <tr><td style="padding: 5px 0; color: #856404;">Total Unverified Cost:</td><td style="font-weight: 600; font-size: 16px; color: #dc3545; text-align: right;"><strong>USD {{UnverifiedAmount}}</strong></td></tr>
                    <tr><td style="padding: 5px 0; color: #856404;">Verification Deadline:</td><td style="font-weight: 600; color: #dc3545; font-size: 16px; text-align: right;"><strong>{{VerificationDeadline}}</strong></td></tr>
                    <tr><td style="padding: 5px 0; color: #856404;">Days Remaining:</td><td style="font-weight: 600; color: #dc3545; font-size: 16px; text-align: right;"><strong>{{DaysRemaining}}</strong></td></tr>
                </table>
                <div style="background-color: #fadbd8; padding: 15px; margin: 20px 0; border-left: 4px solid #dc3545; border-radius: 4px;">
                    <p style="margin: 0; color: #721c24; font-size: 14px; font-weight: 600;">
                        Important: If you do not verify and submit your calls before the deadline, they will automatically be classified as PERSONAL calls and you will be responsible for payment.
                    </p>
                </div>
                <div style="background-color: #e3f2fd; padding: 15px; margin: 20px 0; border-left: 4px solid #2196F3; border-radius: 4px;">
                    <p style="margin: 0 0 10px 0; color: #1976d2; font-size: 14px; font-weight: 600;">
                        What You Need to Do:
                    </p>
                    <ol style="margin: 0; padding-left: 20px; color: #1976d2; font-size: 13px;">
                        <li>Log in to the TAB System</li>
                        <li>Go to "My Call Logs"</li>
                        <li>Verify each call as Personal or Official</li>
                        <li>Submit official calls to your supervisor for approval</li>
                    </ol>
                </div>
                <div style="text-align: center; margin: 20px 0;">
                    <a href="{{ViewCallLogsLink}}" style="background-color: #dc3545; color: #ffffff; text-decoration: none; padding: 14px 35px; border-radius: 5px; display: inline-block; font-weight: 600; font-size: 15px;">Verify Calls Now</a>
                </div>
            </td></tr>
            <tr><td style="background-color: #f8f9fa; padding: 20px; text-align: center; border-radius: 0 0 8px 8px;">
                <p style="color: #999999; font-size: 12px; margin: 0;">© {{Year}} TAB System - Telephone Allowance & Billing</p>
            </td></tr>
        </table>
    </td></tr></table>
</body></html>',
    NULL,
    'Reminder email sent to staff when verification deadline is approaching',
    'Call Log Verification',
    'StaffName, Month, Year, UnverifiedCount, UnverifiedAmount, VerificationDeadline, DaysRemaining, ViewCallLogsLink, Year',
    1,
    1,
    GETUTCDATE()
    );
    PRINT '✓ CALL_LOG_DEADLINE_REMINDER installed successfully'
END
ELSE
    PRINT '⊗ CALL_LOG_DEADLINE_REMINDER already exists - skipping'
PRINT ''

PRINT '===================================='
PRINT 'Call Log Verification Email Template Installation Complete!'
PRINT 'Total Templates: 7'
PRINT '===================================='
GO
