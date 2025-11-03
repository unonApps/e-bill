-- =============================================
-- Device Reimbursement Email Templates - Part 2
-- Continuing from Part 1
-- =============================================

USE TABDB;
GO

-- 3. SUPERVISOR APPROVED - Notification to Requester
INSERT INTO EmailTemplates (TemplateCode, Name, Subject, HtmlBody, PlainTextBody, Description, Category, AvailablePlaceholders, IsActive, IsSystemTemplate, CreatedDate)
VALUES (
'REFUND_SUPERVISOR_APPROVED',
'Device Reimbursement - Supervisor Approved',
'Your Device Reimbursement Request Has Been Approved - Request #{{RequestId}}',
'<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Request Approved by Supervisor</title>
</head>
<body style="margin: 0; padding: 0; font-family: ''Segoe UI'', Tahoma, Geneva, Verdana, sans-serif; background-color: #f4f4f4;">
    <table cellpadding="0" cellspacing="0" border="0" width="100%" style="background-color: #f4f4f4; padding: 20px;">
        <tr>
            <td align="center">
                <table cellpadding="0" cellspacing="0" border="0" width="600" style="background-color: #ffffff; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1);">
                    <!-- Header -->
                    <tr>
                        <td style="background: linear-gradient(135deg, #11998e 0%, #38ef7d 100%); padding: 40px 30px; text-align: center; border-radius: 8px 8px 0 0;">
                            <div style="background-color: rgba(255,255,255,0.2); width: 80px; height: 80px; border-radius: 50%; margin: 0 auto 15px; display: flex; align-items: center; justify-content: center;">
                                <span style="font-size: 40px;">✓</span>
                            </div>
                            <h1 style="color: #ffffff; margin: 0; font-size: 28px; font-weight: 600;">Approved by Supervisor</h1>
                            <p style="color: #ffffff; margin: 10px 0 0 0; font-size: 16px; opacity: 0.95;">Request #{{RequestId}}</p>
                        </td>
                    </tr>

                    <!-- Content -->
                    <tr>
                        <td style="padding: 40px 30px;">
                            <p style="color: #333333; font-size: 16px; line-height: 1.6; margin: 0 0 20px 0;">
                                Dear <strong>{{RequesterName}}</strong>,
                            </p>

                            <p style="color: #555555; font-size: 15px; line-height: 1.6; margin: 0 0 25px 0;">
                                Good news! Your device reimbursement request has been <strong style="color: #38ef7d;">approved</strong> by your supervisor and has been forwarded to the Budget Officer for the next stage of processing.
                            </p>

                            <!-- Status Update Box -->
                            <table cellpadding="0" cellspacing="0" border="0" width="100%" style="background-color: #d4edda; border-radius: 6px; margin: 0 0 25px 0; border: 1px solid #28a745;">
                                <tr>
                                    <td style="padding: 20px;">
                                        <h3 style="color: #155724; margin: 0 0 15px 0; font-size: 16px; font-weight: 600;">Approval Details</h3>
                                        <table cellpadding="0" cellspacing="0" border="0" width="100%">
                                            <tr>
                                                <td style="padding: 8px 0; color: #155724; font-size: 14px; width: 40%;">Request ID:</td>
                                                <td style="padding: 8px 0; color: #000000; font-size: 14px; font-weight: 600;">#{{RequestId}}</td>
                                            </tr>
                                            <tr>
                                                <td style="padding: 8px 0; color: #155724; font-size: 14px;">Approved By:</td>
                                                <td style="padding: 8px 0; color: #000000; font-size: 14px;">{{SupervisorName}}</td>
                                            </tr>
                                            <tr>
                                                <td style="padding: 8px 0; color: #155724; font-size: 14px;">Approval Date:</td>
                                                <td style="padding: 8px 0; color: #000000; font-size: 14px;">{{ApprovalDate}}</td>
                                            </tr>
                                            <tr>
                                                <td style="padding: 8px 0; color: #155724; font-size: 14px;">Amount:</td>
                                                <td style="padding: 8px 0; color: #000000; font-size: 14px; font-weight: 600;">{{DevicePurchaseCurrency}} {{DevicePurchaseAmount}}</td>
                                            </tr>
                                            <tr>
                                                <td style="padding: 8px 0; color: #155724; font-size: 14px;">Current Status:</td>
                                                <td style="padding: 8px 0;">
                                                    <span style="background-color: #ffc107; color: #000000; padding: 4px 12px; border-radius: 12px; font-size: 12px; font-weight: 600;">Pending Budget Officer</span>
                                                </td>
                                            </tr>
                                        </table>

                                        <div style="margin-top: 15px; padding-top: 15px; border-top: 1px solid #c3e6cb;">
                                            <p style="color: #155724; font-size: 14px; margin: 0; font-weight: 600;">Supervisor Comments:</p>
                                            <p style="color: #000000; font-size: 14px; margin: 5px 0 0 0;">{{SupervisorRemarks}}</p>
                                        </div>
                                    </td>
                                </tr>
                            </table>

                            <!-- Action Button -->
                            <table cellpadding="0" cellspacing="0" border="0" width="100%" style="margin: 0 0 25px 0;">
                                <tr>
                                    <td align="center">
                                        <a href="{{ViewRequestLink}}" style="display: inline-block; background-color: #38ef7d; color: #000000; text-decoration: none; padding: 14px 35px; border-radius: 6px; font-size: 15px; font-weight: 600; box-shadow: 0 2px 4px rgba(56, 239, 125, 0.4);">View Request Status</a>
                                    </td>
                                </tr>
                            </table>

                            <!-- Next Steps -->
                            <div style="background-color: #e3f2fd; border-left: 4px solid #2196F3; padding: 15px; margin: 0 0 25px 0; border-radius: 4px;">
                                <h4 style="color: #1976d2; margin: 0 0 10px 0; font-size: 14px; font-weight: 600;">Next Steps</h4>
                                <p style="color: #555555; font-size: 14px; line-height: 1.5; margin: 0;">
                                    Your request will now be reviewed by the Budget Officer who will validate the cost allocation and fund commitment. You will receive another notification once this step is completed.
                                </p>
                            </div>

                            <p style="color: #777777; font-size: 13px; line-height: 1.6; margin: 0;">
                                Thank you for your patience as we process your request.
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
'Confirmation email sent to requester when supervisor approves their device reimbursement request',
'Device Reimbursement',
'RequestId, RequesterName, SupervisorName, ApprovalDate, DevicePurchaseAmount, DevicePurchaseCurrency, SupervisorRemarks, ViewRequestLink, Year',
1,
1,
GETUTCDATE()
);

-- Continue with remaining templates...
