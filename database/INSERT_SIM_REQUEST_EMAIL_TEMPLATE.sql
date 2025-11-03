-- =============================================
-- Add SIM Request Supervisor Notification Email Template
-- =============================================

USE [TABDB]
GO

-- Insert the email template
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
    'SIM Request - Supervisor Approval Notification',
    'SIM_REQUEST_SUPERVISOR_NOTIFICATION',
    'New SIM Card Request Requires Your Approval - {{FirstName}} {{LastName}}',
    '<!DOCTYPE html><html lang="en"><head><meta charset="UTF-8"><meta name="viewport" content="width=device-width, initial-scale=1.0"><title>SIM Card Request - Supervisor Approval Required</title></head><body style="margin: 0; padding: 0; font-family: ''Segoe UI'', Tahoma, Geneva, Verdana, sans-serif; background-color: #f3f4f6;"><table width="100%" cellpadding="0" cellspacing="0" style="background-color: #f3f4f6; padding: 40px 20px;"><tr><td align="center"><table width="600" cellpadding="0" cellspacing="0" style="background-color: #ffffff; border-radius: 16px; overflow: hidden; box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);"><tr><td style="background: linear-gradient(135deg, #009edb 0%, #0086c3 100%); padding: 40px 30px; text-align: center;"><h1 style="margin: 0; color: #ffffff; font-size: 28px; font-weight: 700; letter-spacing: -0.5px;">🆕 New SIM Card Request</h1><p style="margin: 10px 0 0 0; color: #e0f2fe; font-size: 16px;">Requires Your Approval</p></td></tr><tr><td style="padding: 40px 30px;"><p style="margin: 0 0 20px 0; color: #1f2937; font-size: 16px; line-height: 1.6;">Dear <strong>{{SupervisorName}}</strong>,</p><p style="margin: 0 0 30px 0; color: #4b5563; font-size: 15px; line-height: 1.6;">A new SIM card request has been submitted and requires your approval. Please review the details below:</p><div style="background-color: #dbeafe; border-left: 4px solid #009edb; padding: 15px 20px; margin-bottom: 30px; border-radius: 8px;"><p style="margin: 0; color: #1e40af; font-size: 14px; font-weight: 600;"><strong>Request ID:</strong> #{{RequestId}}</p><p style="margin: 5px 0 0 0; color: #1e40af; font-size: 13px;"><strong>Submitted:</strong> {{RequestDate}}</p></div><table width="100%" cellpadding="0" cellspacing="0" style="margin-bottom: 30px;"><tr><td colspan="2" style="background-color: #f9fafb; padding: 12px 20px; border-radius: 8px 8px 0 0; border-bottom: 2px solid #009edb;"><h2 style="margin: 0; color: #1f2937; font-size: 18px; font-weight: 700;">👤 Requester Information</h2></td></tr><tr><td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb; width: 40%;"><strong style="color: #6b7280; font-size: 14px;">Full Name:</strong></td><td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb;"><span style="color: #1f2937; font-size: 14px;">{{FirstName}} {{LastName}}</span></td></tr><tr><td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb;"><strong style="color: #6b7280; font-size: 14px;">Index Number:</strong></td><td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb;"><span style="color: #1f2937; font-size: 14px;">{{IndexNo}}</span></td></tr><tr><td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb;"><strong style="color: #6b7280; font-size: 14px;">Organization:</strong></td><td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb;"><span style="color: #1f2937; font-size: 14px;">{{Organization}}</span></td></tr><tr><td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb;"><strong style="color: #6b7280; font-size: 14px;">Office:</strong></td><td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb;"><span style="color: #1f2937; font-size: 14px;">{{Office}}</span></td></tr><tr><td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb;"><strong style="color: #6b7280; font-size: 14px;">Grade:</strong></td><td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb;"><span style="color: #1f2937; font-size: 14px;">{{Grade}}</span></td></tr><tr><td style="padding: 15px 20px;"><strong style="color: #6b7280; font-size: 14px;">Functional Title:</strong></td><td style="padding: 15px 20px;"><span style="color: #1f2937; font-size: 14px;">{{FunctionalTitle}}</span></td></tr></table><table width="100%" cellpadding="0" cellspacing="0" style="margin-bottom: 30px;"><tr><td colspan="2" style="background-color: #f9fafb; padding: 12px 20px; border-radius: 8px 8px 0 0; border-bottom: 2px solid #10b981;"><h2 style="margin: 0; color: #1f2937; font-size: 18px; font-weight: 700;">📱 Request Details</h2></td></tr><tr><td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb; width: 40%;"><strong style="color: #6b7280; font-size: 14px;">SIM Type:</strong></td><td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb;"><span style="color: #1f2937; font-size: 14px;">{{SimType}}</span></td></tr><tr><td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb;"><strong style="color: #6b7280; font-size: 14px;">Service Provider:</strong></td><td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb;"><span style="color: #1f2937; font-size: 14px;">{{ServiceProvider}}</span></td></tr><tr><td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb;"><strong style="color: #6b7280; font-size: 14px;">Official Email:</strong></td><td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb;"><span style="color: #1f2937; font-size: 14px;">{{OfficialEmail}}</span></td></tr><tr><td style="padding: 15px 20px;"><strong style="color: #6b7280; font-size: 14px;">Office Extension:</strong></td><td style="padding: 15px 20px;"><span style="color: #1f2937; font-size: 14px;">{{OfficeExtension}}</span></td></tr></table><table width="100%" cellpadding="0" cellspacing="0" style="margin-bottom: 30px;"><tr><td align="center" style="padding: 20px 0;"><table cellpadding="0" cellspacing="0"><tr><td align="center" style="border-radius: 12px; background: linear-gradient(135deg, #10b981 0%, #059669 100%); padding: 16px 40px;"><a href="{{ApprovalLink}}" style="color: #ffffff; text-decoration: none; font-weight: 700; font-size: 16px; display: inline-block;">✅ Review & Approve Request</a></td></tr></table></td></tr></table><div style="background-color: #fee2e2; border-left: 4px solid #ef4444; padding: 15px 20px; margin-bottom: 30px; border-radius: 8px;"><p style="margin: 0; color: #991b1b; font-size: 13px; line-height: 1.6;"><strong>⚠️ Important:</strong> Please review this request at your earliest convenience. Timely approval is essential for the requester to receive their SIM card and continue their work without interruption.</p></div><p style="margin: 0 0 10px 0; color: #4b5563; font-size: 15px; line-height: 1.6;">Thank you for your prompt attention to this matter.</p><p style="margin: 0; color: #4b5563; font-size: 15px; line-height: 1.6;">Best regards,<br><strong>TAB System</strong></p></td></tr><tr><td style="background-color: #f9fafb; padding: 30px; text-align: center; border-top: 1px solid #e5e7eb;"><p style="margin: 0 0 10px 0; color: #6b7280; font-size: 13px;">This is an automated message from the TAB System.</p><p style="margin: 0 0 15px 0; color: #9ca3af; font-size: 12px;">Please do not reply directly to this email.</p><p style="margin: 0; color: #9ca3af; font-size: 12px;">© {{Year}} TAB System. All rights reserved.</p></td></tr></table></td></tr></table></body></html>',
    'Email notification sent to supervisors when a new SIM card request is submitted and requires their approval. Includes requester details, request information, and a direct link to review the request.',
    'RequestId, RequestDate, SimType, ServiceProvider, Remarks, FirstName, LastName, IndexNo, Organization, Office, Grade, FunctionalTitle, OfficialEmail, OfficeExtension, SupervisorName, SupervisorEmail, ApprovalLink, Year',
    'SIM Management',
    1, -- IsActive
    1, -- IsSystemTemplate (set to 0 if you want it to be editable)
    GETUTCDATE()
);

GO

-- Verify insertion
SELECT 
    Id,
    Name,
    TemplateCode,
    Category,
    IsActive,
    CreatedDate
FROM EmailTemplates
WHERE TemplateCode = 'SIM_REQUEST_SUPERVISOR_NOTIFICATION';

PRINT 'Email template created successfully!';
PRINT 'Template Code: SIM_REQUEST_SUPERVISOR_NOTIFICATION';
PRINT 'You can now use this template in your SIM request submission code.';

GO
