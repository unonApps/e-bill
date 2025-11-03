-- =============================================
-- SIM Request Workflow - Complete Email Templates
-- =============================================
-- This script inserts all email templates needed for the complete SIM request workflow
-- Total templates: 6 (Supervisor notification template was already created separately)
--
-- Templates included:
-- 1. SIM_REQUEST_SUBMITTED - Confirmation to requester
-- 2. SIM_REQUEST_APPROVED - Approval notification to requester
-- 3. SIM_REQUEST_REJECTED - Rejection notification to requester
-- 4. SIM_REQUEST_ICTS_NOTIFICATION - Notification to ICTS team
-- 5. SIM_READY_FOR_COLLECTION - Ready for pickup notification
-- 6. SIM_REQUEST_CANCELLED - Cancellation confirmation
-- =============================================

USE [TABDB]
GO

PRINT '============================================='
PRINT 'Installing SIM Request Workflow Email Templates'
PRINT '============================================='
PRINT ''

-- =============================================
-- Template 1: SIM Request Submitted (Confirmation to Requester)
-- =============================================

PRINT 'Installing Template 1/6: SIM_REQUEST_SUBMITTED...'

INSERT INTO EmailTemplates
(
    Name,
    TemplateCode,
    Subject,
    HtmlBody,
    Description,
    AvailablePlaceholders,
    Category,
    IsActive,
    IsSystemTemplate,
    CreatedDate
)
VALUES
(
    'SIM Request - Submitted Confirmation',
    'SIM_REQUEST_SUBMITTED',
    'SIM Card Request Submitted - Request #{{RequestId}}',
    '<!DOCTYPE html><html lang="en"><head><meta charset="UTF-8"><meta name="viewport" content="width=device-width, initial-scale=1.0"><title>SIM Card Request Submitted</title></head><body style="margin: 0; padding: 0; font-family: ''Segoe UI'', Tahoma, Geneva, Verdana, sans-serif; background-color: #f3f4f6;"><table width="100%" cellpadding="0" cellspacing="0" style="background-color: #f3f4f6; padding: 40px 20px;"><tr><td align="center"><table width="600" cellpadding="0" cellspacing="0" style="background-color: #ffffff; border-radius: 16px; overflow: hidden; box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);"><tr><td style="background: linear-gradient(135deg, #10b981 0%, #059669 100%); padding: 40px 30px; text-align: center;"><h1 style="margin: 0; color: #ffffff; font-size: 28px; font-weight: 700; letter-spacing: -0.5px;">✅ Request Submitted Successfully</h1><p style="margin: 10px 0 0 0; color: #d1fae5; font-size: 16px;">Your SIM Card Request Has Been Received</p></td></tr><tr><td style="padding: 40px 30px;"><p style="margin: 0 0 20px 0; color: #1f2937; font-size: 16px; line-height: 1.6;">Dear <strong>{{FirstName}} {{LastName}}</strong>,</p><p style="margin: 0 0 30px 0; color: #4b5563; font-size: 15px; line-height: 1.6;">Thank you for submitting your SIM card request. We have received your application and it is now being processed. Below is a summary of your request:</p><div style="background-color: #d1fae5; border-left: 4px solid #10b981; padding: 15px 20px; margin-bottom: 30px; border-radius: 8px;"><p style="margin: 0; color: #065f46; font-size: 14px; font-weight: 600;"><strong>Request ID:</strong> #{{RequestId}}</p><p style="margin: 5px 0 0 0; color: #065f46; font-size: 13px;"><strong>Submitted:</strong> {{RequestDate}}</p><p style="margin: 5px 0 0 0; color: #065f46; font-size: 13px;"><strong>Status:</strong> <span style="background-color: #fef3c7; color: #92400e; padding: 2px 8px; border-radius: 4px; font-weight: 600;">Pending Supervisor Approval</span></p></div><table width="100%" cellpadding="0" cellspacing="0" style="margin-bottom: 30px;"><tr><td colspan="2" style="background-color: #f9fafb; padding: 12px 20px; border-radius: 8px 8px 0 0; border-bottom: 2px solid #10b981;"><h2 style="margin: 0; color: #1f2937; font-size: 18px; font-weight: 700;">📋 Request Summary</h2></td></tr><tr><td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb; width: 40%;"><strong style="color: #6b7280; font-size: 14px;">SIM Type:</strong></td><td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb;"><span style="color: #1f2937; font-size: 14px;">{{SimType}}</span></td></tr><tr><td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb;"><strong style="color: #6b7280; font-size: 14px;">Service Provider:</strong></td><td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb;"><span style="color: #1f2937; font-size: 14px;">{{ServiceProvider}}</span></td></tr><tr><td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb;"><strong style="color: #6b7280; font-size: 14px;">Index Number:</strong></td><td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb;"><span style="color: #1f2937; font-size: 14px;">{{IndexNo}}</span></td></tr><tr><td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb;"><strong style="color: #6b7280; font-size: 14px;">Organization:</strong></td><td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb;"><span style="color: #1f2937; font-size: 14px;">{{Organization}}</span></td></tr><tr><td style="padding: 15px 20px;"><strong style="color: #6b7280; font-size: 14px;">Office:</strong></td><td style="padding: 15px 20px;"><span style="color: #1f2937; font-size: 14px;">{{Office}}</span></td></tr></table><table width="100%" cellpadding="0" cellspacing="0" style="margin-bottom: 30px;"><tr><td colspan="2" style="background-color: #f9fafb; padding: 12px 20px; border-radius: 8px 8px 0 0; border-bottom: 2px solid #009edb;"><h2 style="margin: 0; color: #1f2937; font-size: 18px; font-weight: 700;">🔄 What Happens Next?</h2></td></tr><tr><td style="padding: 20px;"><ol style="margin: 0; padding-left: 20px; color: #4b5563; font-size: 14px; line-height: 1.8;"><li style="margin-bottom: 10px;"><strong style="color: #1f2937;">Supervisor Review:</strong> Your supervisor (<strong>{{SupervisorName}}</strong>) will review and approve your request.</li><li style="margin-bottom: 10px;"><strong style="color: #1f2937;">ICTS Processing:</strong> Once approved, the ICTS team will process your request.</li><li style="margin-bottom: 10px;"><strong style="color: #1f2937;">Admin Approval:</strong> The admin team will perform final verification.</li><li><strong style="color: #1f2937;">SIM Collection:</strong> You will be notified when your SIM card is ready for collection.</li></ol></td></tr></table><table width="100%" cellpadding="0" cellspacing="0" style="margin-bottom: 30px;"><tr><td align="center" style="padding: 20px 0;"><table cellpadding="0" cellspacing="0"><tr><td align="center" style="border-radius: 12px; background: linear-gradient(135deg, #009edb 0%, #0086c3 100%); padding: 16px 40px;"><a href="{{ViewRequestLink}}" style="color: #ffffff; text-decoration: none; font-weight: 700; font-size: 16px; display: inline-block;">📄 View My Request</a></td></tr></table></td></tr></table><div style="background-color: #dbeafe; border-left: 4px solid #009edb; padding: 15px 20px; margin-bottom: 30px; border-radius: 8px;"><p style="margin: 0; color: #1e40af; font-size: 13px; line-height: 1.6;"><strong>ℹ️ Need Help?</strong> You can track your request status anytime by logging into the TAB System and navigating to SIM Management > My Requests.</p></div><p style="margin: 0 0 10px 0; color: #4b5563; font-size: 15px; line-height: 1.6;">We will notify you via email when there are updates to your request.</p><p style="margin: 0; color: #4b5563; font-size: 15px; line-height: 1.6;">Best regards,<br><strong>TAB System</strong></p></td></tr><tr><td style="background-color: #f9fafb; padding: 30px; text-align: center; border-top: 1px solid #e5e7eb;"><p style="margin: 0 0 10px 0; color: #6b7280; font-size: 13px;">This is an automated message from the TAB System.</p><p style="margin: 0 0 15px 0; color: #9ca3af; font-size: 12px;">Please do not reply directly to this email.</p><p style="margin: 0; color: #9ca3af; font-size: 12px;">© {{Year}} TAB System. All rights reserved.</p></td></tr></table></td></tr></table></body></html>',
    'Confirmation email sent to requester when they submit a SIM card request. Provides request summary and explains the approval workflow.',
    'RequestId, RequestDate, FirstName, LastName, SimType, ServiceProvider, IndexNo, Organization, Office, SupervisorName, ViewRequestLink, Year',
    'SIM Management',
    1,
    1,
    GETUTCDATE()
);

PRINT '✓ Template 1/6 installed successfully'
PRINT ''

-- =============================================
-- Template 2: SIM Request Approved
-- =============================================

PRINT 'Installing Template 2/6: SIM_REQUEST_APPROVED...'

INSERT INTO EmailTemplates
(
    Name,
    TemplateCode,
    Subject,
    HtmlBody,
    Description,
    AvailablePlaceholders,
    Category,
    IsActive,
    IsSystemTemplate,
    CreatedDate
)
VALUES
(
    'SIM Request - Approved Notification',
    'SIM_REQUEST_APPROVED',
    'Great News! Your SIM Request Has Been Approved - #{{RequestId}}',
    '<!DOCTYPE html><html lang="en"><head><meta charset="UTF-8"><meta name="viewport" content="width=device-width, initial-scale=1.0"><title>SIM Card Request Approved</title></head><body style="margin: 0; padding: 0; font-family: ''Segoe UI'', Tahoma, Geneva, Verdana, sans-serif; background-color: #f3f4f6;"><table width="100%" cellpadding="0" cellspacing="0" style="background-color: #f3f4f6; padding: 40px 20px;"><tr><td align="center"><table width="600" cellpadding="0" cellspacing="0" style="background-color: #ffffff; border-radius: 16px; overflow: hidden; box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);"><tr><td style="background: linear-gradient(135deg, #10b981 0%, #059669 100%); padding: 40px 30px; text-align: center;"><div style="font-size: 48px; margin-bottom: 10px;">🎉</div><h1 style="margin: 0; color: #ffffff; font-size: 28px; font-weight: 700; letter-spacing: -0.5px;">Request Approved!</h1><p style="margin: 10px 0 0 0; color: #d1fae5; font-size: 16px;">Your SIM Card Request Has Been Approved</p></td></tr><tr><td style="padding: 40px 30px;"><p style="margin: 0 0 20px 0; color: #1f2937; font-size: 16px; line-height: 1.6;">Dear <strong>{{FirstName}} {{LastName}}</strong>,</p><p style="margin: 0 0 30px 0; color: #4b5563; font-size: 15px; line-height: 1.6;">Great news! Your SIM card request has been <strong style="color: #10b981;">approved by {{ApproverName}}</strong>. Your request is now moving forward in the approval process.</p><div style="background-color: #d1fae5; border-left: 4px solid #10b981; padding: 15px 20px; margin-bottom: 30px; border-radius: 8px;"><p style="margin: 0; color: #065f46; font-size: 14px; font-weight: 600;"><strong>Request ID:</strong> #{{RequestId}}</p><p style="margin: 5px 0 0 0; color: #065f46; font-size: 13px;"><strong>Approved:</strong> {{ApprovalDate}}</p><p style="margin: 5px 0 0 0; color: #065f46; font-size: 13px;"><strong>Current Status:</strong> <span style="background-color: #10b981; color: #ffffff; padding: 2px 8px; border-radius: 4px; font-weight: 600;">{{CurrentStatus}}</span></p></div><table width="100%" cellpadding="0" cellspacing="0" style="margin-bottom: 30px;"><tr><td colspan="2" style="background-color: #f9fafb; padding: 12px 20px; border-radius: 8px 8px 0 0; border-bottom: 2px solid #10b981;"><h2 style="margin: 0; color: #1f2937; font-size: 18px; font-weight: 700;">📋 Request Details</h2></td></tr><tr><td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb; width: 40%;"><strong style="color: #6b7280; font-size: 14px;">SIM Type:</strong></td><td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb;"><span style="color: #1f2937; font-size: 14px;">{{SimType}}</span></td></tr><tr><td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb;"><strong style="color: #6b7280; font-size: 14px;">Service Provider:</strong></td><td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb;"><span style="color: #1f2937; font-size: 14px;">{{ServiceProvider}}</span></td></tr><tr><td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb;"><strong style="color: #6b7280; font-size: 14px;">Approved By:</strong></td><td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb;"><span style="color: #1f2937; font-size: 14px;">{{ApproverName}} ({{ApproverRole}})</span></td></tr><tr><td style="padding: 15px 20px;"><strong style="color: #6b7280; font-size: 14px;">Approval Date:</strong></td><td style="padding: 15px 20px;"><span style="color: #1f2937; font-size: 14px;">{{ApprovalDate}}</span></td></tr></table>{{#if ApprovalComments}}<table width="100%" cellpadding="0" cellspacing="0" style="margin-bottom: 30px;"><tr><td style="background-color: #dbeafe; border-left: 4px solid #009edb; padding: 15px 20px; border-radius: 8px;"><p style="margin: 0 0 8px 0; color: #1e40af; font-size: 14px; font-weight: 600;">💬 Approver Comments:</p><p style="margin: 0; color: #1e40af; font-size: 14px; line-height: 1.5;">{{ApprovalComments}}</p></td></tr></table>{{/if}}<table width="100%" cellpadding="0" cellspacing="0" style="margin-bottom: 30px;"><tr><td colspan="2" style="background-color: #f9fafb; padding: 12px 20px; border-radius: 8px 8px 0 0; border-bottom: 2px solid #009edb;"><h2 style="margin: 0; color: #1f2937; font-size: 18px; font-weight: 700;">🔄 What Happens Next?</h2></td></tr><tr><td style="padding: 20px;"><p style="margin: 0 0 15px 0; color: #4b5563; font-size: 14px; line-height: 1.6;">Your request will now proceed through the following steps:</p><ol style="margin: 0; padding-left: 20px; color: #4b5563; font-size: 14px; line-height: 1.8;"><li style="margin-bottom: 10px;"><strong style="color: #1f2937;">ICTS Processing:</strong> The ICTS team will review and process your request.</li><li style="margin-bottom: 10px;"><strong style="color: #1f2937;">Admin Final Approval:</strong> Admin team will perform final verification.</li><li><strong style="color: #1f2937;">SIM Preparation:</strong> Your SIM card will be prepared and you''ll be notified when ready for collection.</li></ol></td></tr></table><table width="100%" cellpadding="0" cellspacing="0" style="margin-bottom: 30px;"><tr><td align="center" style="padding: 20px 0;"><table cellpadding="0" cellspacing="0"><tr><td align="center" style="border-radius: 12px; background: linear-gradient(135deg, #009edb 0%, #0086c3 100%); padding: 16px 40px;"><a href="{{ViewRequestLink}}" style="color: #ffffff; text-decoration: none; font-weight: 700; font-size: 16px; display: inline-block;">📄 Track Request Status</a></td></tr></table></td></tr></table><div style="background-color: #fef3c7; border-left: 4px solid #f59e0b; padding: 15px 20px; margin-bottom: 30px; border-radius: 8px;"><p style="margin: 0; color: #92400e; font-size: 13px; line-height: 1.6;"><strong>⏱️ Estimated Timeline:</strong> The remaining approval and processing steps typically take 2-3 business days. You will receive email notifications at each stage.</p></div><p style="margin: 0 0 10px 0; color: #4b5563; font-size: 15px; line-height: 1.6;">We appreciate your patience and will keep you updated on the progress of your request.</p><p style="margin: 0; color: #4b5563; font-size: 15px; line-height: 1.6;">Best regards,<br><strong>TAB System</strong></p></td></tr><tr><td style="background-color: #f9fafb; padding: 30px; text-align: center; border-top: 1px solid #e5e7eb;"><p style="margin: 0 0 10px 0; color: #6b7280; font-size: 13px;">This is an automated message from the TAB System.</p><p style="margin: 0 0 15px 0; color: #9ca3af; font-size: 12px;">Please do not reply directly to this email.</p><p style="margin: 0; color: #9ca3af; font-size: 12px;">© {{Year}} TAB System. All rights reserved.</p></td></tr></table></td></tr></table></body></html>',
    'Notification sent to requester when their SIM card request is approved. Informs them of next steps in the process.',
    'RequestId, FirstName, LastName, ApproverName, ApproverRole, ApprovalDate, CurrentStatus, SimType, ServiceProvider, ApprovalComments, ViewRequestLink, Year',
    'SIM Management',
    1,
    1,
    GETUTCDATE()
);

PRINT '✓ Template 2/6 installed successfully'
PRINT ''

-- =============================================
-- Template 3: SIM Request Rejected
-- =============================================

PRINT 'Installing Template 3/6: SIM_REQUEST_REJECTED...'

INSERT INTO EmailTemplates
(
    Name,
    TemplateCode,
    Subject,
    HtmlBody,
    Description,
    AvailablePlaceholders,
    Category,
    IsActive,
    IsSystemTemplate,
    CreatedDate
)
VALUES
(
    'SIM Request - Rejection Notification',
    'SIM_REQUEST_REJECTED',
    'SIM Card Request Update - Action Required - #{{RequestId}}',
    '<!DOCTYPE html><html lang="en"><head><meta charset="UTF-8"><meta name="viewport" content="width=device-width, initial-scale=1.0"><title>SIM Card Request - Decision Required</title></head><body style="margin: 0; padding: 0; font-family: ''Segoe UI'', Tahoma, Geneva, Verdana, sans-serif; background-color: #f3f4f6;"><table width="100%" cellpadding="0" cellspacing="0" style="background-color: #f3f4f6; padding: 40px 20px;"><tr><td align="center"><table width="600" cellpadding="0" cellspacing="0" style="background-color: #ffffff; border-radius: 16px; overflow: hidden; box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);"><tr><td style="background: linear-gradient(135deg, #ef4444 0%, #dc2626 100%); padding: 40px 30px; text-align: center;"><h1 style="margin: 0; color: #ffffff; font-size: 28px; font-weight: 700; letter-spacing: -0.5px;">⚠️ Request Requires Attention</h1><p style="margin: 10px 0 0 0; color: #fee2e2; font-size: 16px;">Your SIM Card Request Was Not Approved</p></td></tr><tr><td style="padding: 40px 30px;"><p style="margin: 0 0 20px 0; color: #1f2937; font-size: 16px; line-height: 1.6;">Dear <strong>{{FirstName}} {{LastName}}</strong>,</p><p style="margin: 0 0 30px 0; color: #4b5563; font-size: 15px; line-height: 1.6;">We regret to inform you that your SIM card request has not been approved at this time. Please review the details below.</p><div style="background-color: #fee2e2; border-left: 4px solid #ef4444; padding: 15px 20px; margin-bottom: 30px; border-radius: 8px;"><p style="margin: 0; color: #991b1b; font-size: 14px; font-weight: 600;"><strong>Request ID:</strong> #{{RequestId}}</p><p style="margin: 5px 0 0 0; color: #991b1b; font-size: 13px;"><strong>Reviewed:</strong> {{RejectionDate}}</p><p style="margin: 5px 0 0 0; color: #991b1b; font-size: 13px;"><strong>Status:</strong> <span style="background-color: #ef4444; color: #ffffff; padding: 2px 8px; border-radius: 4px; font-weight: 600;">Not Approved</span></p></div><table width="100%" cellpadding="0" cellspacing="0" style="margin-bottom: 30px;"><tr><td colspan="2" style="background-color: #f9fafb; padding: 12px 20px; border-radius: 8px 8px 0 0; border-bottom: 2px solid #ef4444;"><h2 style="margin: 0; color: #1f2937; font-size: 18px; font-weight: 700;">📋 Request Details</h2></td></tr><tr><td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb; width: 40%;"><strong style="color: #6b7280; font-size: 14px;">SIM Type:</strong></td><td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb;"><span style="color: #1f2937; font-size: 14px;">{{SimType}}</span></td></tr><tr><td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb;"><strong style="color: #6b7280; font-size: 14px;">Service Provider:</strong></td><td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb;"><span style="color: #1f2937; font-size: 14px;">{{ServiceProvider}}</span></td></tr><tr><td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb;"><strong style="color: #6b7280; font-size: 14px;">Reviewed By:</strong></td><td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb;"><span style="color: #1f2937; font-size: 14px;">{{ReviewerName}} ({{ReviewerRole}})</span></td></tr><tr><td style="padding: 15px 20px;"><strong style="color: #6b7280; font-size: 14px;">Decision Date:</strong></td><td style="padding: 15px 20px;"><span style="color: #1f2937; font-size: 14px;">{{RejectionDate}}</span></td></tr></table><table width="100%" cellpadding="0" cellspacing="0" style="margin-bottom: 30px;"><tr><td style="background-color: #fef3c7; border-left: 4px solid #f59e0b; padding: 15px 20px; border-radius: 8px;"><p style="margin: 0 0 8px 0; color: #92400e; font-size: 14px; font-weight: 600;">📝 Reason for Decision:</p><p style="margin: 0; color: #78350f; font-size: 14px; line-height: 1.5;">{{RejectionReason}}</p></td></tr></table><table width="100%" cellpadding="0" cellspacing="0" style="margin-bottom: 30px;"><tr><td colspan="2" style="background-color: #f9fafb; padding: 12px 20px; border-radius: 8px 8px 0 0; border-bottom: 2px solid #009edb;"><h2 style="margin: 0; color: #1f2937; font-size: 18px; font-weight: 700;">🔄 What Can You Do Next?</h2></td></tr><tr><td style="padding: 20px;"><ol style="margin: 0; padding-left: 20px; color: #4b5563; font-size: 14px; line-height: 1.8;"><li style="margin-bottom: 10px;"><strong style="color: #1f2937;">Review the Feedback:</strong> Carefully read the reason provided for the decision.</li><li style="margin-bottom: 10px;"><strong style="color: #1f2937;">Address Concerns:</strong> If possible, address the concerns mentioned in the feedback.</li><li style="margin-bottom: 10px;"><strong style="color: #1f2937;">Contact Your Supervisor:</strong> Discuss the decision with {{ReviewerName}} for clarification.</li><li><strong style="color: #1f2937;">Resubmit:</strong> You may submit a new request after addressing the concerns.</li></ol></td></tr></table><table width="100%" cellpadding="0" cellspacing="0" style="margin-bottom: 30px;"><tr><td align="center" style="padding: 20px 0;"><table cellpadding="0" cellspacing="0"><tr><td align="center" style="border-radius: 12px; background: linear-gradient(135deg, #009edb 0%, #0086c3 100%); padding: 16px 40px;"><a href="{{ViewRequestLink}}" style="color: #ffffff; text-decoration: none; font-weight: 700; font-size: 16px; display: inline-block;">📄 View Request Details</a></td></tr></table></td></tr><tr><td align="center" style="padding: 0 0 20px 0;"><table cellpadding="0" cellspacing="0"><tr><td align="center" style="border-radius: 12px; background: linear-gradient(135deg, #10b981 0%, #059669 100%); padding: 16px 40px;"><a href="{{NewRequestLink}}" style="color: #ffffff; text-decoration: none; font-weight: 700; font-size: 16px; display: inline-block;">➕ Submit New Request</a></td></tr></table></td></tr></table><div style="background-color: #dbeafe; border-left: 4px solid #009edb; padding: 15px 20px; margin-bottom: 30px; border-radius: 8px;"><p style="margin: 0; color: #1e40af; font-size: 13px; line-height: 1.6;"><strong>ℹ️ Need Assistance?</strong> If you have questions about this decision or need help with your next steps, please contact the ICTS Help Desk or your supervisor.</p></div><p style="margin: 0 0 10px 0; color: #4b5563; font-size: 15px; line-height: 1.6;">We appreciate your understanding and are here to assist you with any questions.</p><p style="margin: 0; color: #4b5563; font-size: 15px; line-height: 1.6;">Best regards,<br><strong>TAB System</strong></p></td></tr><tr><td style="background-color: #f9fafb; padding: 30px; text-align: center; border-top: 1px solid #e5e7eb;"><p style="margin: 0 0 10px 0; color: #6b7280; font-size: 13px;">This is an automated message from the TAB System.</p><p style="margin: 0 0 15px 0; color: #9ca3af; font-size: 12px;">Please do not reply directly to this email.</p><p style="margin: 0; color: #9ca3af; font-size: 12px;">© {{Year}} TAB System. All rights reserved.</p></td></tr></table></td></tr></table></body></html>',
    'Notification sent when a SIM card request is rejected. Provides rejection reason and guidance on next steps.',
    'RequestId, FirstName, LastName, SimType, ServiceProvider, ReviewerName, ReviewerRole, RejectionDate, RejectionReason, ViewRequestLink, NewRequestLink, Year',
    'SIM Management',
    1,
    1,
    GETUTCDATE()
);

PRINT '✓ Template 3/6 installed successfully'
PRINT ''

-- =============================================
-- Template 4: ICTS Team Notification
-- =============================================

PRINT 'Installing Template 4/6: SIM_REQUEST_ICTS_NOTIFICATION...'

INSERT INTO EmailTemplates
(
    Name,
    TemplateCode,
    Subject,
    HtmlBody,
    Description,
    AvailablePlaceholders,
    Category,
    IsActive,
    IsSystemTemplate,
    CreatedDate
)
VALUES
(
    'SIM Request - ICTS Team Notification',
    'SIM_REQUEST_ICTS_NOTIFICATION',
    'New SIM Request Awaiting ICTS Processing - #{{RequestId}}',
    '<!DOCTYPE html><html lang="en"><head><meta charset="UTF-8"><meta name="viewport" content="width=device-width, initial-scale=1.0"><title>New SIM Request - ICTS Action Required</title></head><body style="margin: 0; padding: 0; font-family: ''Segoe UI'', Tahoma, Geneva, Verdana, sans-serif; background-color: #f3f4f6;"><table width="100%" cellpadding="0" cellspacing="0" style="background-color: #f3f4f6; padding: 40px 20px;"><tr><td align="center"><table width="600" cellpadding="0" cellspacing="0" style="background-color: #ffffff; border-radius: 16px; overflow: hidden; box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);"><tr><td style="background: linear-gradient(135deg, #6366f1 0%, #4f46e5 100%); padding: 40px 30px; text-align: center;"><h1 style="margin: 0; color: #ffffff; font-size: 28px; font-weight: 700; letter-spacing: -0.5px;">🔔 New SIM Request - ICTS</h1><p style="margin: 10px 0 0 0; color: #e0e7ff; font-size: 16px;">Action Required: SIM Card Request Approved</p></td></tr><tr><td style="padding: 40px 30px;"><p style="margin: 0 0 20px 0; color: #1f2937; font-size: 16px; line-height: 1.6;">Dear <strong>ICTS Team</strong>,</p><p style="margin: 0 0 30px 0; color: #4b5563; font-size: 15px; line-height: 1.6;">A SIM card request has been approved by the supervisor and requires ICTS processing. Please review and process this request at your earliest convenience.</p><div style="background-color: #e0e7ff; border-left: 4px solid #6366f1; padding: 15px 20px; margin-bottom: 30px; border-radius: 8px;"><p style="margin: 0; color: #3730a3; font-size: 14px; font-weight: 600;"><strong>Request ID:</strong> #{{RequestId}}</p><p style="margin: 5px 0 0 0; color: #3730a3; font-size: 13px;"><strong>Submitted:</strong> {{RequestDate}}</p><p style="margin: 5px 0 0 0; color: #3730a3; font-size: 13px;"><strong>Supervisor Approved:</strong> {{SupervisorApprovalDate}}</p><p style="margin: 5px 0 0 0; color: #3730a3; font-size: 13px;"><strong>Priority:</strong> <span style="background-color: #fef3c7; color: #92400e; padding: 2px 8px; border-radius: 4px; font-weight: 600;">Normal</span></p></div><table width="100%" cellpadding="0" cellspacing="0" style="margin-bottom: 30px;"><tr><td colspan="2" style="background-color: #f9fafb; padding: 12px 20px; border-radius: 8px 8px 0 0; border-bottom: 2px solid #6366f1;"><h2 style="margin: 0; color: #1f2937; font-size: 18px; font-weight: 700;">👤 Requester Information</h2></td></tr><tr><td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb; width: 40%;"><strong style="color: #6b7280; font-size: 14px;">Full Name:</strong></td><td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb;"><span style="color: #1f2937; font-size: 14px;">{{FirstName}} {{LastName}}</span></td></tr><tr><td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb;"><strong style="color: #6b7280; font-size: 14px;">Index Number:</strong></td><td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb;"><span style="color: #1f2937; font-size: 14px;">{{IndexNo}}</span></td></tr><tr><td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb;"><strong style="color: #6b7280; font-size: 14px;">Organization:</strong></td><td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb;"><span style="color: #1f2937; font-size: 14px;">{{Organization}}</span></td></tr><tr><td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb;"><strong style="color: #6b7280; font-size: 14px;">Office:</strong></td><td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb;"><span style="color: #1f2937; font-size: 14px;">{{Office}}</span></td></tr><tr><td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb;"><strong style="color: #6b7280; font-size: 14px;">Grade:</strong></td><td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb;"><span style="color: #1f2937; font-size: 14px;">{{Grade}}</span></td></tr><tr><td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb;"><strong style="color: #6b7280; font-size: 14px;">Functional Title:</strong></td><td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb;"><span style="color: #1f2937; font-size: 14px;">{{FunctionalTitle}}</span></td></tr><tr><td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb;"><strong style="color: #6b7280; font-size: 14px;">Official Email:</strong></td><td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb;"><span style="color: #1f2937; font-size: 14px;">{{OfficialEmail}}</span></td></tr><tr><td style="padding: 15px 20px;"><strong style="color: #6b7280; font-size: 14px;">Office Extension:</strong></td><td style="padding: 15px 20px;"><span style="color: #1f2937; font-size: 14px;">{{OfficeExtension}}</span></td></tr></table><table width="100%" cellpadding="0" cellspacing="0" style="margin-bottom: 30px;"><tr><td colspan="2" style="background-color: #f9fafb; padding: 12px 20px; border-radius: 8px 8px 0 0; border-bottom: 2px solid #10b981;"><h2 style="margin: 0; color: #1f2937; font-size: 18px; font-weight: 700;">📱 SIM Card Details</h2></td></tr><tr><td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb; width: 40%;"><strong style="color: #6b7280; font-size: 14px;">SIM Type:</strong></td><td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb;"><span style="color: #1f2937; font-size: 14px;">{{SimType}}</span></td></tr><tr><td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb;"><strong style="color: #6b7280; font-size: 14px;">Service Provider:</strong></td><td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb;"><span style="color: #1f2937; font-size: 14px;">{{ServiceProvider}}</span></td></tr><tr><td style="padding: 15px 20px;"><strong style="color: #6b7280; font-size: 14px;">Approved By:</strong></td><td style="padding: 15px 20px;"><span style="color: #1f2937; font-size: 14px;">{{SupervisorName}}</span></td></tr></table>{{#if Remarks}}<table width="100%" cellpadding="0" cellspacing="0" style="margin-bottom: 30px;"><tr><td style="background-color: #fef3c7; border-left: 4px solid #f59e0b; padding: 15px 20px; border-radius: 8px;"><p style="margin: 0 0 8px 0; color: #92400e; font-size: 14px; font-weight: 600;">📝 Additional Remarks:</p><p style="margin: 0; color: #78350f; font-size: 14px; line-height: 1.5;">{{Remarks}}</p></td></tr></table>{{/if}}<table width="100%" cellpadding="0" cellspacing="0" style="margin-bottom: 30px;"><tr><td colspan="2" style="background-color: #f9fafb; padding: 12px 20px; border-radius: 8px 8px 0 0; border-bottom: 2px solid #f59e0b;"><h2 style="margin: 0; color: #1f2937; font-size: 18px; font-weight: 700;">✅ ICTS Action Items</h2></td></tr><tr><td style="padding: 20px;"><ol style="margin: 0; padding-left: 20px; color: #4b5563; font-size: 14px; line-height: 1.8;"><li style="margin-bottom: 10px;"><strong style="color: #1f2937;">Verify Request:</strong> Review all requester information and request details.</li><li style="margin-bottom: 10px;"><strong style="color: #1f2937;">Check Eligibility:</strong> Confirm that the requester is eligible for the requested SIM type.</li><li style="margin-bottom: 10px;"><strong style="color: #1f2937;">Coordinate with Provider:</strong> Contact {{ServiceProvider}} to arrange SIM card procurement.</li><li style="margin-bottom: 10px;"><strong style="color: #1f2937;">Update Status:</strong> Update the request status in the system after processing.</li><li><strong style="color: #1f2937;">Notify Admin:</strong> Forward to admin team for final approval once processed.</li></ol></td></tr></table><table width="100%" cellpadding="0" cellspacing="0" style="margin-bottom: 30px;"><tr><td align="center" style="padding: 20px 0;"><table cellpadding="0" cellspacing="0"><tr><td align="center" style="border-radius: 12px; background: linear-gradient(135deg, #6366f1 0%, #4f46e5 100%); padding: 16px 40px;"><a href="{{ProcessRequestLink}}" style="color: #ffffff; text-decoration: none; font-weight: 700; font-size: 16px; display: inline-block;">🔧 Process Request</a></td></tr></table></td></tr></table><div style="background-color: #fee2e2; border-left: 4px solid #ef4444; padding: 15px 20px; margin-bottom: 30px; border-radius: 8px;"><p style="margin: 0; color: #991b1b; font-size: 13px; line-height: 1.6;"><strong>⚠️ SLA Reminder:</strong> Please process this request within 2 business days to maintain service level agreements. The requester and supervisor have been notified of expected timelines.</p></div><p style="margin: 0 0 10px 0; color: #4b5563; font-size: 15px; line-height: 1.6;">Thank you for your prompt attention to this request.</p><p style="margin: 0; color: #4b5563; font-size: 15px; line-height: 1.6;">Best regards,<br><strong>TAB System</strong></p></td></tr><tr><td style="background-color: #f9fafb; padding: 30px; text-align: center; border-top: 1px solid #e5e7eb;"><p style="margin: 0 0 10px 0; color: #6b7280; font-size: 13px;">This is an automated message from the TAB System.</p><p style="margin: 0 0 15px 0; color: #9ca3af; font-size: 12px;">Please do not reply directly to this email.</p><p style="margin: 0; color: #9ca3af; font-size: 12px;">© {{Year}} TAB System. All rights reserved.</p></td></tr></table></td></tr></table></body></html>',
    'Notification sent to ICTS team when a SIM request is approved by supervisor and requires ICTS processing.',
    'RequestId, RequestDate, SupervisorApprovalDate, FirstName, LastName, IndexNo, Organization, Office, Grade, FunctionalTitle, OfficialEmail, OfficeExtension, SimType, ServiceProvider, SupervisorName, Remarks, ProcessRequestLink, Year',
    'SIM Management',
    1,
    1,
    GETUTCDATE()
);

PRINT '✓ Template 4/6 installed successfully'
PRINT ''

-- =============================================
-- Template 5: SIM Ready for Collection
-- =============================================

PRINT 'Installing Template 5/6: SIM_READY_FOR_COLLECTION...'

INSERT INTO EmailTemplates
(
    Name,
    TemplateCode,
    Subject,
    HtmlBody,
    Description,
    AvailablePlaceholders,
    Category,
    IsActive,
    IsSystemTemplate,
    CreatedDate
)
VALUES
(
    'SIM Request - Ready for Collection',
    'SIM_READY_FOR_COLLECTION',
    'Your SIM Card is Ready for Collection! - #{{RequestId}}',
    '<!DOCTYPE html><html lang="en"><head><meta charset="UTF-8"><meta name="viewport" content="width=device-width, initial-scale=1.0"><title>SIM Card Ready for Collection</title></head><body style="margin: 0; padding: 0; font-family: ''Segoe UI'', Tahoma, Geneva, Verdana, sans-serif; background-color: #f3f4f6;"><table width="100%" cellpadding="0" cellspacing="0" style="background-color: #f3f4f6; padding: 40px 20px;"><tr><td align="center"><table width="600" cellpadding="0" cellspacing="0" style="background-color: #ffffff; border-radius: 16px; overflow: hidden; box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);"><tr><td style="background: linear-gradient(135deg, #8b5cf6 0%, #7c3aed 100%); padding: 40px 30px; text-align: center;"><div style="font-size: 48px; margin-bottom: 10px;">📦</div><h1 style="margin: 0; color: #ffffff; font-size: 28px; font-weight: 700; letter-spacing: -0.5px;">Your SIM Card is Ready!</h1><p style="margin: 10px 0 0 0; color: #ede9fe; font-size: 16px;">Please Collect Your SIM Card</p></td></tr><tr><td style="padding: 40px 30px;"><p style="margin: 0 0 20px 0; color: #1f2937; font-size: 16px; line-height: 1.6;">Dear <strong>{{FirstName}} {{LastName}}</strong>,</p><p style="margin: 0 0 30px 0; color: #4b5563; font-size: 15px; line-height: 1.6;">Excellent news! Your SIM card request has been fully approved and processed. Your SIM card is now ready for collection.</p><div style="background-color: #ede9fe; border-left: 4px solid #8b5cf6; padding: 15px 20px; margin-bottom: 30px; border-radius: 8px;"><p style="margin: 0; color: #5b21b6; font-size: 14px; font-weight: 600;"><strong>Request ID:</strong> #{{RequestId}}</p><p style="margin: 5px 0 0 0; color: #5b21b6; font-size: 13px;"><strong>Ready Date:</strong> {{ReadyDate}}</p><p style="margin: 5px 0 0 0; color: #5b21b6; font-size: 13px;"><strong>Status:</strong> <span style="background-color: #10b981; color: #ffffff; padding: 2px 8px; border-radius: 4px; font-weight: 600;">Ready for Collection</span></p></div><table width="100%" cellpadding="0" cellspacing="0" style="margin-bottom: 30px;"><tr><td colspan="2" style="background-color: #f9fafb; padding: 12px 20px; border-radius: 8px 8px 0 0; border-bottom: 2px solid #8b5cf6;"><h2 style="margin: 0; color: #1f2937; font-size: 18px; font-weight: 700;">📱 Your SIM Card Details</h2></td></tr><tr><td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb; width: 40%;"><strong style="color: #6b7280; font-size: 14px;">SIM Type:</strong></td><td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb;"><span style="color: #1f2937; font-size: 14px;">{{SimType}}</span></td></tr><tr><td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb;"><strong style="color: #6b7280; font-size: 14px;">Service Provider:</strong></td><td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb;"><span style="color: #1f2937; font-size: 14px;">{{ServiceProvider}}</span></td></tr><tr><td style="padding: 15px 20px;"><strong style="color: #6b7280; font-size: 14px;">Phone Number:</strong></td><td style="padding: 15px 20px;"><span style="color: #1f2937; font-size: 14px; font-weight: 600;">{{PhoneNumber}}</span></td></tr></table><table width="100%" cellpadding="0" cellspacing="0" style="margin-bottom: 30px;"><tr><td colspan="2" style="background-color: #f9fafb; padding: 12px 20px; border-radius: 8px 8px 0 0; border-bottom: 2px solid #10b981;"><h2 style="margin: 0; color: #1f2937; font-size: 18px; font-weight: 700;">📍 Collection Information</h2></td></tr><tr><td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb; width: 40%;"><strong style="color: #6b7280; font-size: 14px;">Collection Point:</strong></td><td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb;"><span style="color: #1f2937; font-size: 14px;">{{CollectionPoint}}</span></td></tr><tr><td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb;"><strong style="color: #6b7280; font-size: 14px;">Office Hours:</strong></td><td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb;"><span style="color: #1f2937; font-size: 14px;">Monday - Friday, 9:00 AM - 5:00 PM</span></td></tr><tr><td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb;"><strong style="color: #6b7280; font-size: 14px;">Contact Person:</strong></td><td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb;"><span style="color: #1f2937; font-size: 14px;">{{ContactPerson}}</span></td></tr><tr><td style="padding: 15px 20px;"><strong style="color: #6b7280; font-size: 14px;">Contact Phone:</strong></td><td style="padding: 15px 20px;"><span style="color: #1f2937; font-size: 14px;">{{ContactPhone}}</span></td></tr></table><table width="100%" cellpadding="0" cellspacing="0" style="margin-bottom: 30px;"><tr><td colspan="2" style="background-color: #f9fafb; padding: 12px 20px; border-radius: 8px 8px 0 0; border-bottom: 2px solid #f59e0b;"><h2 style="margin: 0; color: #1f2937; font-size: 18px; font-weight: 700;">📋 Before You Collect</h2></td></tr><tr><td style="padding: 20px;"><p style="margin: 0 0 15px 0; color: #4b5563; font-size: 14px; font-weight: 600;">Please bring the following items:</p><ul style="margin: 0; padding-left: 20px; color: #4b5563; font-size: 14px; line-height: 1.8;"><li style="margin-bottom: 10px;"><strong style="color: #1f2937;">Valid ID:</strong> Your staff ID card or passport</li><li style="margin-bottom: 10px;"><strong style="color: #1f2937;">Request ID:</strong> Reference number #{{RequestId}}</li><li><strong style="color: #1f2937;">This Email:</strong> Digital or printed copy of this notification</li></ul></td></tr></table><table width="100%" cellpadding="0" cellspacing="0" style="margin-bottom: 30px;"><tr><td colspan="2" style="background-color: #f9fafb; padding: 12px 20px; border-radius: 8px 8px 0 0; border-bottom: 2px solid #009edb;"><h2 style="margin: 0; color: #1f2937; font-size: 18px; font-weight: 700;">🔄 During Collection</h2></td></tr><tr><td style="padding: 20px;"><ol style="margin: 0; padding-left: 20px; color: #4b5563; font-size: 14px; line-height: 1.8;"><li style="margin-bottom: 10px;"><strong style="color: #1f2937;">Identity Verification:</strong> Present your ID for verification</li><li style="margin-bottom: 10px;"><strong style="color: #1f2937;">Receive SIM Card:</strong> Collect your SIM card and activation instructions</li><li style="margin-bottom: 10px;"><strong style="color: #1f2937;">Sign Acknowledgment:</strong> Sign collection form to confirm receipt</li><li><strong style="color: #1f2937;">Get Support:</strong> Staff will assist with any immediate questions</li></ol></td></tr></table><div style="background-color: #fef3c7; border-left: 4px solid #f59e0b; padding: 15px 20px; margin-bottom: 30px; border-radius: 8px;"><p style="margin: 0; color: #92400e; font-size: 13px; line-height: 1.6;"><strong>⏰ Collection Deadline:</strong> Please collect your SIM card within <strong>5 business days</strong> (by {{CollectionDeadline}}). After this date, you may need to submit a new request.</p></div><table width="100%" cellpadding="0" cellspacing="0" style="margin-bottom: 30px;"><tr><td align="center" style="padding: 20px 0;"><table cellpadding="0" cellspacing="0"><tr><td align="center" style="border-radius: 12px; background: linear-gradient(135deg, #8b5cf6 0%, #7c3aed 100%); padding: 16px 40px;"><a href="{{ViewRequestLink}}" style="color: #ffffff; text-decoration: none; font-weight: 700; font-size: 16px; display: inline-block;">📄 View Full Details</a></td></tr></table></td></tr></table><div style="background-color: #dbeafe; border-left: 4px solid #009edb; padding: 15px 20px; margin-bottom: 30px; border-radius: 8px;"><p style="margin: 0; color: #1e40af; font-size: 13px; line-height: 1.6;"><strong>❓ Questions?</strong> Contact {{ContactPerson}} at {{ContactPhone}} or visit {{CollectionPoint}} during office hours.</p></div><p style="margin: 0 0 10px 0; color: #4b5563; font-size: 15px; line-height: 1.6;">We''re excited for you to receive your new SIM card. Thank you for your patience throughout the approval process!</p><p style="margin: 0; color: #4b5563; font-size: 15px; line-height: 1.6;">Best regards,<br><strong>TAB System</strong></p></td></tr><tr><td style="background-color: #f9fafb; padding: 30px; text-align: center; border-top: 1px solid #e5e7eb;"><p style="margin: 0 0 10px 0; color: #6b7280; font-size: 13px;">This is an automated message from the TAB System.</p><p style="margin: 0 0 15px 0; color: #9ca3af; font-size: 12px;">Please do not reply directly to this email.</p><p style="margin: 0; color: #9ca3af; font-size: 12px;">© {{Year}} TAB System. All rights reserved.</p></td></tr></table></td></tr></table></body></html>',
    'Notification sent when SIM card is ready for collection. Includes collection location, requirements, and deadline.',
    'RequestId, FirstName, LastName, ReadyDate, SimType, ServiceProvider, PhoneNumber, CollectionPoint, ContactPerson, ContactPhone, CollectionDeadline, ViewRequestLink, Year',
    'SIM Management',
    1,
    1,
    GETUTCDATE()
);

PRINT '✓ Template 5/6 installed successfully'
PRINT ''

-- =============================================
-- Template 6: SIM Request Cancelled
-- =============================================

PRINT 'Installing Template 6/6: SIM_REQUEST_CANCELLED...'

INSERT INTO EmailTemplates
(
    Name,
    TemplateCode,
    Subject,
    HtmlBody,
    Description,
    AvailablePlaceholders,
    Category,
    IsActive,
    IsSystemTemplate,
    CreatedDate
)
VALUES
(
    'SIM Request - Cancellation Confirmation',
    'SIM_REQUEST_CANCELLED',
    'SIM Card Request Cancelled - #{{RequestId}}',
    '<!DOCTYPE html><html lang="en"><head><meta charset="UTF-8"><meta name="viewport" content="width=device-width, initial-scale=1.0"><title>SIM Card Request Cancelled</title></head><body style="margin: 0; padding: 0; font-family: ''Segoe UI'', Tahoma, Geneva, Verdana, sans-serif; background-color: #f3f4f6;"><table width="100%" cellpadding="0" cellspacing="0" style="background-color: #f3f4f6; padding: 40px 20px;"><tr><td align="center"><table width="600" cellpadding="0" cellspacing="0" style="background-color: #ffffff; border-radius: 16px; overflow: hidden; box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);"><tr><td style="background: linear-gradient(135deg, #6b7280 0%, #4b5563 100%); padding: 40px 30px; text-align: center;"><h1 style="margin: 0; color: #ffffff; font-size: 28px; font-weight: 700; letter-spacing: -0.5px;">🚫 Request Cancelled</h1><p style="margin: 10px 0 0 0; color: #e5e7eb; font-size: 16px;">Your SIM Card Request Has Been Cancelled</p></td></tr><tr><td style="padding: 40px 30px;"><p style="margin: 0 0 20px 0; color: #1f2937; font-size: 16px; line-height: 1.6;">Dear <strong>{{FirstName}} {{LastName}}</strong>,</p><p style="margin: 0 0 30px 0; color: #4b5563; font-size: 15px; line-height: 1.6;">This email confirms that your SIM card request has been cancelled as requested. Below are the details of the cancelled request.</p><div style="background-color: #f3f4f6; border-left: 4px solid #6b7280; padding: 15px 20px; margin-bottom: 30px; border-radius: 8px;"><p style="margin: 0; color: #374151; font-size: 14px; font-weight: 600;"><strong>Request ID:</strong> #{{RequestId}}</p><p style="margin: 5px 0 0 0; color: #374151; font-size: 13px;"><strong>Submitted:</strong> {{RequestDate}}</p><p style="margin: 5px 0 0 0; color: #374151; font-size: 13px;"><strong>Cancelled:</strong> {{CancellationDate}}</p><p style="margin: 5px 0 0 0; color: #374151; font-size: 13px;"><strong>Status:</strong> <span style="background-color: #6b7280; color: #ffffff; padding: 2px 8px; border-radius: 4px; font-weight: 600;">Cancelled</span></p></div><table width="100%" cellpadding="0" cellspacing="0" style="margin-bottom: 30px;"><tr><td colspan="2" style="background-color: #f9fafb; padding: 12px 20px; border-radius: 8px 8px 0 0; border-bottom: 2px solid #6b7280;"><h2 style="margin: 0; color: #1f2937; font-size: 18px; font-weight: 700;">📋 Cancelled Request Details</h2></td></tr><tr><td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb; width: 40%;"><strong style="color: #6b7280; font-size: 14px;">SIM Type:</strong></td><td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb;"><span style="color: #1f2937; font-size: 14px;">{{SimType}}</span></td></tr><tr><td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb;"><strong style="color: #6b7280; font-size: 14px;">Service Provider:</strong></td><td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb;"><span style="color: #1f2937; font-size: 14px;">{{ServiceProvider}}</span></td></tr><tr><td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb;"><strong style="color: #6b7280; font-size: 14px;">Previous Status:</strong></td><td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb;"><span style="color: #1f2937; font-size: 14px;">{{PreviousStatus}}</span></td></tr><tr><td style="padding: 15px 20px;"><strong style="color: #6b7280; font-size: 14px;">Cancelled By:</strong></td><td style="padding: 15px 20px;"><span style="color: #1f2937; font-size: 14px;">{{CancelledBy}}</span></td></tr></table>{{#if CancellationReason}}<table width="100%" cellpadding="0" cellspacing="0" style="margin-bottom: 30px;"><tr><td style="background-color: #fef3c7; border-left: 4px solid #f59e0b; padding: 15px 20px; border-radius: 8px;"><p style="margin: 0 0 8px 0; color: #92400e; font-size: 14px; font-weight: 600;">📝 Cancellation Reason:</p><p style="margin: 0; color: #78350f; font-size: 14px; line-height: 1.5;">{{CancellationReason}}</p></td></tr></table>{{/if}}<table width="100%" cellpadding="0" cellspacing="0" style="margin-bottom: 30px;"><tr><td colspan="2" style="background-color: #f9fafb; padding: 12px 20px; border-radius: 8px 8px 0 0; border-bottom: 2px solid #009edb;"><h2 style="margin: 0; color: #1f2937; font-size: 18px; font-weight: 700;">ℹ️ What This Means</h2></td></tr><tr><td style="padding: 20px;"><ul style="margin: 0; padding-left: 20px; color: #4b5563; font-size: 14px; line-height: 1.8;"><li style="margin-bottom: 10px;">Your request #{{RequestId}} is now closed and will not be processed further.</li><li style="margin-bottom: 10px;">No SIM card will be issued for this request.</li><li style="margin-bottom: 10px;">All approvals associated with this request have been voided.</li><li>You may submit a new request at any time if you still need a SIM card.</li></ul></td></tr></table><table width="100%" cellpadding="0" cellspacing="0" style="margin-bottom: 30px;"><tr><td colspan="2" style="background-color: #f9fafb; padding: 12px 20px; border-radius: 8px 8px 0 0; border-bottom: 2px solid #10b981;"><h2 style="margin: 0; color: #1f2937; font-size: 18px; font-weight: 700;">🔄 Need a SIM Card?</h2></td></tr><tr><td style="padding: 20px;"><p style="margin: 0 0 15px 0; color: #4b5563; font-size: 14px; line-height: 1.6;">If you still need a SIM card, you can submit a new request anytime:</p><ol style="margin: 0; padding-left: 20px; color: #4b5563; font-size: 14px; line-height: 1.8;"><li style="margin-bottom: 10px;"><strong style="color: #1f2937;">Log into TAB System:</strong> Access the SIM Management module</li><li style="margin-bottom: 10px;"><strong style="color: #1f2937;">Create New Request:</strong> Fill out the SIM request form</li><li><strong style="color: #1f2937;">Submit for Approval:</strong> Your request will go through the standard approval workflow</li></ol></td></tr></table><table width="100%" cellpadding="0" cellspacing="0" style="margin-bottom: 30px;"><tr><td align="center" style="padding: 20px 0;"><table cellpadding="0" cellspacing="0"><tr><td align="center" style="border-radius: 12px; background: linear-gradient(135deg, #10b981 0%, #059669 100%); padding: 16px 40px;"><a href="{{NewRequestLink}}" style="color: #ffffff; text-decoration: none; font-weight: 700; font-size: 16px; display: inline-block;">➕ Submit New Request</a></td></tr></table></td></tr><tr><td align="center" style="padding: 0 0 20px 0;"><table cellpadding="0" cellspacing="0"><tr><td align="center" style="border-radius: 12px; background: linear-gradient(135deg, #009edb 0%, #0086c3 100%); padding: 16px 40px;"><a href="{{MyRequestsLink}}" style="color: #ffffff; text-decoration: none; font-weight: 700; font-size: 16px; display: inline-block;">📋 View My Requests</a></td></tr></table></td></tr></table><div style="background-color: #dbeafe; border-left: 4px solid #009edb; padding: 15px 20px; margin-bottom: 30px; border-radius: 8px;"><p style="margin: 0; color: #1e40af; font-size: 13px; line-height: 1.6;"><strong>❓ Questions?</strong> If you did not request this cancellation or have questions about it, please contact the ICTS Help Desk or your supervisor immediately.</p></div><p style="margin: 0 0 10px 0; color: #4b5563; font-size: 15px; line-height: 1.6;">Thank you for using the TAB System.</p><p style="margin: 0; color: #4b5563; font-size: 15px; line-height: 1.6;">Best regards,<br><strong>TAB System</strong></p></td></tr><tr><td style="background-color: #f9fafb; padding: 30px; text-align: center; border-top: 1px solid #e5e7eb;"><p style="margin: 0 0 10px 0; color: #6b7280; font-size: 13px;">This is an automated message from the TAB System.</p><p style="margin: 0 0 15px 0; color: #9ca3af; font-size: 12px;">Please do not reply directly to this email.</p><p style="margin: 0; color: #9ca3af; font-size: 12px;">© {{Year}} TAB System. All rights reserved.</p></td></tr></table></td></tr></table></body></html>',
    'Confirmation sent when a SIM card request is cancelled. Explains implications and next steps.',
    'RequestId, FirstName, LastName, RequestDate, CancellationDate, SimType, ServiceProvider, PreviousStatus, CancelledBy, CancellationReason, NewRequestLink, MyRequestsLink, Year',
    'SIM Management',
    1,
    1,
    GETUTCDATE()
);

PRINT '✓ Template 6/6 installed successfully'
PRINT ''

-- =============================================
-- Verification and Summary
-- =============================================

PRINT '============================================='
PRINT 'Installation Complete!'
PRINT '============================================='
PRINT ''
PRINT 'Summary of installed templates:'
PRINT ''

SELECT
    ROW_NUMBER() OVER (ORDER BY CreatedDate DESC) AS [#],
    Name AS [Template Name],
    TemplateCode AS [Code],
    Category,
    CASE
        WHEN IsActive = 1 THEN 'Active'
        ELSE 'Inactive'
    END AS [Status],
    CASE
        WHEN IsSystemTemplate = 1 THEN 'Yes'
        ELSE 'No'
    END AS [System Template],
    FORMAT(CreatedDate, 'yyyy-MM-dd HH:mm') AS [Created]
FROM EmailTemplates
WHERE Category = 'SIM Management'
    AND TemplateCode IN (
        'SIM_REQUEST_SUBMITTED',
        'SIM_REQUEST_APPROVED',
        'SIM_REQUEST_REJECTED',
        'SIM_REQUEST_ICTS_NOTIFICATION',
        'SIM_READY_FOR_COLLECTION',
        'SIM_REQUEST_CANCELLED'
    )
ORDER BY CreatedDate DESC;

PRINT ''
PRINT '============================================='
PRINT 'Next Steps:'
PRINT '============================================='
PRINT '1. Review templates at /Admin/EmailTemplates'
PRINT '2. Test templates using /Admin/SendEmail'
PRINT '3. Check template previews at /Admin/EmailTemplatePreview'
PRINT '4. Implement email sending in your SIM request workflow'
PRINT '5. Refer to implementation guide for code examples'
PRINT ''
PRINT 'All templates are ready to use!'
PRINT '============================================='

GO
