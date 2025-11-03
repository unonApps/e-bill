-- =============================================
-- Device Reimbursement Email Templates
-- Complete workflow email notification system
-- =============================================

USE TABDB;
GO

-- 1. REQUEST SUBMITTED - Confirmation to Requester
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
<body style="margin: 0; padding: 0; font-family: ''Segoe UI'', Tahoma, Geneva, Verdana, sans-serif; background-color: #f4f4f4;">
    <table cellpadding="0" cellspacing="0" border="0" width="100%" style="background-color: #f4f4f4; padding: 20px;">
        <tr>
            <td align="center">
                <table cellpadding="0" cellspacing="0" border="0" width="600" style="background-color: #ffffff; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1);">
                    <!-- Header -->
                    <tr>
                        <td style="background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); padding: 40px 30px; text-align: center; border-radius: 8px 8px 0 0;">
                            <h1 style="color: #ffffff; margin: 0; font-size: 28px; font-weight: 600;">Request Submitted Successfully</h1>
                            <p style="color: #ffffff; margin: 10px 0 0 0; font-size: 16px; opacity: 0.95;">Device Reimbursement Request</p>
                        </td>
                    </tr>

                    <!-- Content -->
                    <tr>
                        <td style="padding: 40px 30px;">
                            <p style="color: #333333; font-size: 16px; line-height: 1.6; margin: 0 0 20px 0;">
                                Dear <strong>{{RequesterName}}</strong>,
                            </p>

                            <p style="color: #555555; font-size: 15px; line-height: 1.6; margin: 0 0 25px 0;">
                                Your device reimbursement request has been successfully submitted and is now pending review by your supervisor.
                            </p>

                            <!-- Request Summary Box -->
                            <table cellpadding="0" cellspacing="0" border="0" width="100%" style="background-color: #f8f9fa; border-radius: 6px; margin: 0 0 25px 0;">
                                <tr>
                                    <td style="padding: 20px;">
                                        <h3 style="color: #667eea; margin: 0 0 15px 0; font-size: 16px; font-weight: 600;">Request Summary</h3>
                                        <table cellpadding="0" cellspacing="0" border="0" width="100%">
                                            <tr>
                                                <td style="padding: 8px 0; color: #666666; font-size: 14px; width: 40%;">Request ID:</td>
                                                <td style="padding: 8px 0; color: #333333; font-size: 14px; font-weight: 600;">#{{RequestId}}</td>
                                            </tr>
                                            <tr>
                                                <td style="padding: 8px 0; color: #666666; font-size: 14px;">Submission Date:</td>
                                                <td style="padding: 8px 0; color: #333333; font-size: 14px;">{{RequestDate}}</td>
                                            </tr>
                                            <tr>
                                                <td style="padding: 8px 0; color: #666666; font-size: 14px;">Mobile Number:</td>
                                                <td style="padding: 8px 0; color: #333333; font-size: 14px;">{{PrimaryMobileNumber}}</td>
                                            </tr>
                                            <tr>
                                                <td style="padding: 8px 0; color: #666666; font-size: 14px;">Device Amount:</td>
                                                <td style="padding: 8px 0; color: #333333; font-size: 14px; font-weight: 600;">{{DevicePurchaseCurrency}} {{DevicePurchaseAmount}}</td>
                                            </tr>
                                            <tr>
                                                <td style="padding: 8px 0; color: #666666; font-size: 14px;">Device Allowance:</td>
                                                <td style="padding: 8px 0; color: #333333; font-size: 14px;">{{DeviceAllowance}}</td>
                                            </tr>
                                            <tr>
                                                <td style="padding: 8px 0; color: #666666; font-size: 14px;">Supervisor:</td>
                                                <td style="padding: 8px 0; color: #333333; font-size: 14px;">{{SupervisorName}}</td>
                                            </tr>
                                            <tr>
                                                <td style="padding: 8px 0; color: #666666; font-size: 14px;">Status:</td>
                                                <td style="padding: 8px 0;">
                                                    <span style="background-color: #ffc107; color: #000000; padding: 4px 12px; border-radius: 12px; font-size: 12px; font-weight: 600;">Pending Supervisor Approval</span>
                                                </td>
                                            </tr>
                                        </table>
                                    </td>
                                </tr>
                            </table>

                            <!-- Action Button -->
                            <table cellpadding="0" cellspacing="0" border="0" width="100%" style="margin: 0 0 25px 0;">
                                <tr>
                                    <td align="center">
                                        <a href="{{ViewRequestLink}}" style="display: inline-block; background-color: #667eea; color: #ffffff; text-decoration: none; padding: 14px 35px; border-radius: 6px; font-size: 15px; font-weight: 600; box-shadow: 0 2px 4px rgba(102, 126, 234, 0.4);">View My Requests</a>
                                    </td>
                                </tr>
                            </table>

                            <!-- Next Steps -->
                            <div style="background-color: #e3f2fd; border-left: 4px solid #2196F3; padding: 15px; margin: 0 0 25px 0; border-radius: 4px;">
                                <h4 style="color: #1976d2; margin: 0 0 10px 0; font-size: 14px; font-weight: 600;">Next Steps</h4>
                                <p style="color: #555555; font-size: 14px; line-height: 1.5; margin: 0;">
                                    Your supervisor will review your request and either approve it for further processing or return it with feedback. You will be notified via email once a decision is made.
                                </p>
                            </div>

                            <p style="color: #777777; font-size: 13px; line-height: 1.6; margin: 0;">
                                If you have any questions or need to make changes to your request, please contact your supervisor or the HR department.
                            </p>
                        </td>
                    </tr>

                    <!-- Footer -->
                    <tr>
                        <td style="background-color: #f8f9fa; padding: 20px 30px; text-align: center; border-radius: 0 0 8px 8px; border-top: 1px solid #e9ecef;">
                            <p style="color: #999999; font-size: 12px; margin: 0 0 5px 0;">
                                This is an automated message from the TAB System.
                            </p>
                            <p style="color: #999999; font-size: 12px; margin: 0;">
                                Please do not reply directly to this email.
                            </p>
                            <p style="color: #bbbbbb; font-size: 11px; margin: 15px 0 0 0;">
                                © {{Year}} TAB System. All rights reserved.
                            </p>
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
'RequestId, RequestDate, RequesterName, PrimaryMobileNumber, DevicePurchaseAmount, DevicePurchaseCurrency, DeviceAllowance, SupervisorName, ViewRequestLink, Year',
1,
1,
GETUTCDATE()
);

-- 2. REQUEST SUBMITTED - Notification to Supervisor
INSERT INTO EmailTemplates (TemplateCode, Name, Subject, HtmlBody, PlainTextBody, Description, Category, AvailablePlaceholders, IsActive, IsSystemTemplate, CreatedDate)
VALUES (
'REFUND_SUPERVISOR_NOTIFICATION',
'Device Reimbursement - Supervisor Notification',
'New Device Reimbursement Request Requires Your Approval - {{RequesterName}}',
'<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Device Reimbursement - Supervisor Approval Required</title>
</head>
<body style="margin: 0; padding: 0; font-family: ''Segoe UI'', Tahoma, Geneva, Verdana, sans-serif; background-color: #f4f4f4;">
    <table cellpadding="0" cellspacing="0" border="0" width="100%" style="background-color: #f4f4f4; padding: 20px;">
        <tr>
            <td align="center">
                <table cellpadding="0" cellspacing="0" border="0" width="600" style="background-color: #ffffff; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1);">
                    <!-- Header -->
                    <tr>
                        <td style="background: linear-gradient(135deg, #f093fb 0%, #f5576c 100%); padding: 40px 30px; text-align: center; border-radius: 8px 8px 0 0;">
                            <h1 style="color: #ffffff; margin: 0; font-size: 28px; font-weight: 600;">Approval Required</h1>
                            <p style="color: #ffffff; margin: 10px 0 0 0; font-size: 16px; opacity: 0.95;">Device Reimbursement Request</p>
                        </td>
                    </tr>

                    <!-- Content -->
                    <tr>
                        <td style="padding: 40px 30px;">
                            <p style="color: #333333; font-size: 16px; line-height: 1.6; margin: 0 0 20px 0;">
                                Dear <strong>{{SupervisorName}}</strong>,
                            </p>

                            <p style="color: #555555; font-size: 15px; line-height: 1.6; margin: 0 0 25px 0;">
                                A new device reimbursement request has been submitted by <strong>{{RequesterName}}</strong> and requires your approval.
                            </p>

                            <!-- Request Details -->
                            <table cellpadding="0" cellspacing="0" border="0" width="100%" style="background-color: #fff3cd; border-radius: 6px; margin: 0 0 25px 0; border: 1px solid #ffc107;">
                                <tr>
                                    <td style="padding: 20px;">
                                        <h3 style="color: #856404; margin: 0 0 15px 0; font-size: 16px; font-weight: 600;">Request Details</h3>
                                        <table cellpadding="0" cellspacing="0" border="0" width="100%">
                                            <tr>
                                                <td style="padding: 8px 0; color: #856404; font-size: 14px; width: 45%;">Request ID:</td>
                                                <td style="padding: 8px 0; color: #000000; font-size: 14px; font-weight: 600;">#{{RequestId}}</td>
                                            </tr>
                                            <tr>
                                                <td style="padding: 8px 0; color: #856404; font-size: 14px;">Submitted:</td>
                                                <td style="padding: 8px 0; color: #000000; font-size: 14px;">{{RequestDate}}</td>
                                            </tr>
                                            <tr>
                                                <td style="padding: 8px 0; color: #856404; font-size: 14px;">Staff Member:</td>
                                                <td style="padding: 8px 0; color: #000000; font-size: 14px;">{{RequesterName}}</td>
                                            </tr>
                                            <tr>
                                                <td style="padding: 8px 0; color: #856404; font-size: 14px;">Index Number:</td>
                                                <td style="padding: 8px 0; color: #000000; font-size: 14px;">{{IndexNo}}</td>
                                            </tr>
                                            <tr>
                                                <td style="padding: 8px 0; color: #856404; font-size: 14px;">Organization:</td>
                                                <td style="padding: 8px 0; color: #000000; font-size: 14px;">{{Organization}}</td>
                                            </tr>
                                            <tr>
                                                <td style="padding: 8px 0; color: #856404; font-size: 14px;">Office:</td>
                                                <td style="padding: 8px 0; color: #000000; font-size: 14px;">{{Office}}</td>
                                            </tr>
                                            <tr>
                                                <td style="padding: 8px 0; color: #856404; font-size: 14px;">Mobile Number:</td>
                                                <td style="padding: 8px 0; color: #000000; font-size: 14px;">{{PrimaryMobileNumber}}</td>
                                            </tr>
                                            <tr>
                                                <td style="padding: 8px 0; color: #856404; font-size: 14px;">Class of Service:</td>
                                                <td style="padding: 8px 0; color: #000000; font-size: 14px;">{{ClassOfService}}</td>
                                            </tr>
                                            <tr>
                                                <td style="padding: 8px 0; color: #856404; font-size: 14px;">Device Allowance:</td>
                                                <td style="padding: 8px 0; color: #000000; font-size: 14px; font-weight: 600;">{{DeviceAllowance}}</td>
                                            </tr>
                                            <tr>
                                                <td style="padding: 8px 0; color: #856404; font-size: 14px;">Purchase Amount:</td>
                                                <td style="padding: 8px 0; color: #000000; font-size: 14px; font-weight: 600;">{{DevicePurchaseCurrency}} {{DevicePurchaseAmount}}</td>
                                            </tr>
                                        </table>
                                    </td>
                                </tr>
                            </table>

                            <!-- Action Button -->
                            <table cellpadding="0" cellspacing="0" border="0" width="100%" style="margin: 0 0 25px 0;">
                                <tr>
                                    <td align="center">
                                        <a href="{{ApprovalLink}}" style="display: inline-block; background-color: #f5576c; color: #ffffff; text-decoration: none; padding: 14px 35px; border-radius: 6px; font-size: 15px; font-weight: 600; box-shadow: 0 2px 4px rgba(245, 87, 108, 0.4);">Review & Approve Request</a>
                                    </td>
                                </tr>
                            </table>

                            <!-- Important Notice -->
                            <div style="background-color: #fff3cd; border-left: 4px solid #ffc107; padding: 15px; margin: 0 0 25px 0; border-radius: 4px;">
                                <h4 style="color: #856404; margin: 0 0 10px 0; font-size: 14px; font-weight: 600;">⚠ Action Required</h4>
                                <p style="color: #856404; font-size: 14px; line-height: 1.5; margin: 0;">
                                    Please review this request at your earliest convenience. Your approval is required for this request to proceed to the Budget Officer for further processing.
                                </p>
                            </div>

                            <p style="color: #777777; font-size: 13px; line-height: 1.6; margin: 0;">
                                Thank you for your prompt attention to this matter.
                            </p>
                        </td>
                    </tr>

                    <!-- Footer -->
                    <tr>
                        <td style="background-color: #f8f9fa; padding: 20px 30px; text-align: center; border-radius: 0 0 8px 8px; border-top: 1px solid #e9ecef;">
                            <p style="color: #999999; font-size: 12px; margin: 0 0 5px 0;">
                                This is an automated message from the TAB System.
                            </p>
                            <p style="color: #999999; font-size: 12px; margin: 0;">
                                Please do not reply directly to this email.
                            </p>
                            <p style="color: #bbbbbb; font-size: 11px; margin: 15px 0 0 0;">
                                © {{Year}} TAB System. All rights reserved.
                            </p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>',
NULL,
'Notification email sent to supervisor when a device reimbursement request is submitted',
'Device Reimbursement',
'RequestId, RequestDate, RequesterName, IndexNo, Organization, Office, PrimaryMobileNumber, ClassOfService, DeviceAllowance, DevicePurchaseAmount, DevicePurchaseCurrency, SupervisorName, ApprovalLink, Year',
1,
1,
GETUTCDATE()
);

-- Continue in next message due to length...
