-- Standardize 3 REFUND supervisor/rejection templates with inline logo, solid colors, no emojis
SET NOCOUNT ON;
GO

-- 1. Update REFUND_REQUEST_REJECTED
PRINT 'Updating REFUND_REQUEST_REJECTED...';

UPDATE EmailTemplates
SET HtmlBody = '<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Request Rejected</title>
</head>
<body style="margin: 0; padding: 0; font-family: ''Segoe UI'', Tahoma, Geneva, Verdana, sans-serif; background-color: #f3f4f6;">
    <table width="100%" cellpadding="0" cellspacing="0" style="background-color: #f3f4f6; padding: 40px 20px;">
        <tr>
            <td align="center">
                <table width="600" cellpadding="0" cellspacing="0" style="background-color: #ffffff; border-radius: 16px; overflow: hidden; box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);">
                    <!-- Header with Logo -->
                    <tr>
                        <td style="background-color: #009edb; padding: 30px; text-align: center;">
                            <img src="cid:logo" alt="UNON E-Billing System" style="max-width: 100%; height: auto; display: block; margin: 0 auto;" width="550" />
                        </td>
                    </tr>

                    <!-- Content -->
                    <tr>
                        <td style="padding: 40px 30px;">
                            <!-- Greeting -->
                            <p style="margin: 0 0 20px 0; color: #1f2937; font-size: 16px; line-height: 1.6;">
                                Dear <strong>{{RequesterName}}</strong>,
                            </p>

                            <p style="margin: 0 0 30px 0; color: #4b5563; font-size: 15px; line-height: 1.6;">
                                Your device reimbursement request has not been approved at this time.
                            </p>

                            <!-- Rejection Details -->
                            <table width="100%" cellpadding="0" cellspacing="0" style="margin-bottom: 30px;">
                                <tr>
                                    <td colspan="2" style="background-color: #fee2e2; padding: 12px 20px; border-radius: 8px 8px 0 0; border-bottom: 2px solid #dc2626;">
                                        <h2 style="margin: 0; color: #991b1b; font-size: 18px; font-weight: 700;">
                                            Request Not Approved
                                        </h2>
                                    </td>
                                </tr>
                                <tr>
                                    <td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb; width: 40%;">
                                        <strong style="color: #6b7280; font-size: 14px;">Request ID:</strong>
                                    </td>
                                    <td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb; text-align: right;">
                                        <span style="color: #1f2937; font-size: 14px; font-weight: 700;">#{{RequestId}}</span>
                                    </td>
                                </tr>
                                <tr>
                                    <td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb;">
                                        <strong style="color: #6b7280; font-size: 14px;">Rejected By:</strong>
                                    </td>
                                    <td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb; text-align: right;">
                                        <span style="color: #1f2937; font-size: 14px; font-weight: 600;">{{RejectedBy}}</span>
                                    </td>
                                </tr>
                                <tr>
                                    <td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb;">
                                        <strong style="color: #6b7280; font-size: 14px;">Date:</strong>
                                    </td>
                                    <td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb; text-align: right;">
                                        <span style="color: #1f2937; font-size: 14px; font-weight: 600;">{{RejectionDate}}</span>
                                    </td>
                                </tr>
                                <tr>
                                    <td style="padding: 15px 20px; vertical-align: top;">
                                        <strong style="color: #6b7280; font-size: 14px;">Reason:</strong>
                                    </td>
                                    <td style="padding: 15px 20px; text-align: right;">
                                        <span style="color: #991b1b; font-size: 14px; font-weight: 600;">{{RejectionReason}}</span>
                                    </td>
                                </tr>
                            </table>

                            <!-- Information Box -->
                            <div style="background-color: #fef3c7; border-left: 4px solid #f59e0b; padding: 20px; margin-bottom: 30px; border-radius: 8px;">
                                <p style="margin: 0; color: #92400e; font-size: 14px; line-height: 1.6;">
                                    If you believe this decision was made in error or would like to discuss this further, please contact {{RejectedBy}} or the HR department.
                                </p>
                            </div>

                            <!-- Call to Action Button -->
                            <div style="text-align: center; margin: 30px 0;">
                                <a href="{{NewRequestLink}}" style="display: inline-block; background-color: #009edb; color: #ffffff; text-decoration: none; padding: 16px 40px; border-radius: 8px; font-size: 16px; font-weight: 700; box-shadow: 0 4px 12px rgba(0, 158, 219, 0.3); transition: all 0.3s ease;">
                                    Submit New Request →
                                </a>
                            </div>

                            <!-- Closing -->
                            <p style="margin: 30px 0 0 0; color: #4b5563; font-size: 14px; line-height: 1.6;">
                                Best regards,<br>
                                <strong>UNON E-Billing System Team</strong>
                            </p>
                        </td>
                    </tr>

                    <!-- Footer -->
                    <tr>
                        <td style="background-color: #f9fafb; padding: 30px; text-align: center; border-top: 1px solid #e5e7eb;">
                            <!-- Rainbow SDG Colors -->
                            <div style="display: flex; height: 5px; margin-bottom: 20px;">
                                <div style="flex: 1; background-color: #E5243B;"></div>
                                <div style="flex: 1; background-color: #DDA63A;"></div>
                                <div style="flex: 1; background-color: #4C9F38;"></div>
                                <div style="flex: 1; background-color: #C5192D;"></div>
                                <div style="flex: 1; background-color: #FF3A21;"></div>
                                <div style="flex: 1; background-color: #26BDE2;"></div>
                                <div style="flex: 1; background-color: #FCC30B;"></div>
                                <div style="flex: 1; background-color: #A21942;"></div>
                                <div style="flex: 1; background-color: #FD6925;"></div>
                                <div style="flex: 1; background-color: #DD1367;"></div>
                                <div style="flex: 1; background-color: #FD9D24;"></div>
                                <div style="flex: 1; background-color: #BF8B2E;"></div>
                                <div style="flex: 1; background-color: #3F7E44;"></div>
                                <div style="flex: 1; background-color: #0A97D9;"></div>
                                <div style="flex: 1; background-color: #56C02B;"></div>
                                <div style="flex: 1; background-color: #00689D;"></div>
                                <div style="flex: 1; background-color: #19486A;"></div>
                            </div>

                            <p style="margin: 0 0 10px 0; color: #6b7280; font-size: 12px;">
                                <strong>United Nations Office at Nairobi</strong><br>
                                UN Headquarters in Africa
                            </p>

                            <p style="margin: 0; color: #9ca3af; font-size: 11px;">
                                DAS | ICTS | Innovation Business Unit<br>
                                Powered by UNON © {{Year}}
                            </p>

                            <p style="margin: 15px 0 0 0; color: #9ca3af; font-size: 11px; line-height: 1.5;">
                                This is an automated message. Please do not reply to this email.<br>
                                For inquiries, please contact ICTS Service Desk at ICTS.Servicedesk@un.org
                            </p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>',
    ModifiedDate = GETDATE()
WHERE TemplateCode = 'REFUND_REQUEST_REJECTED';

IF @@ROWCOUNT > 0
    PRINT 'REFUND_REQUEST_REJECTED updated successfully.';
ELSE
    PRINT 'WARNING: REFUND_REQUEST_REJECTED not found or not updated.';
GO

-- 2. Update REFUND_SUPERVISOR_APPROVED
PRINT 'Updating REFUND_SUPERVISOR_APPROVED...';

UPDATE EmailTemplates
SET HtmlBody = '<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Supervisor Approved</title>
</head>
<body style="margin: 0; padding: 0; font-family: ''Segoe UI'', Tahoma, Geneva, Verdana, sans-serif; background-color: #f3f4f6;">
    <table width="100%" cellpadding="0" cellspacing="0" style="background-color: #f3f4f6; padding: 40px 20px;">
        <tr>
            <td align="center">
                <table width="600" cellpadding="0" cellspacing="0" style="background-color: #ffffff; border-radius: 16px; overflow: hidden; box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);">
                    <!-- Header with Logo -->
                    <tr>
                        <td style="background-color: #009edb; padding: 30px; text-align: center;">
                            <img src="cid:logo" alt="UNON E-Billing System" style="max-width: 100%; height: auto; display: block; margin: 0 auto;" width="550" />
                        </td>
                    </tr>

                    <!-- Content -->
                    <tr>
                        <td style="padding: 40px 30px;">
                            <!-- Greeting -->
                            <p style="margin: 0 0 20px 0; color: #1f2937; font-size: 16px; line-height: 1.6;">
                                Dear <strong>{{RequesterName}}</strong>,
                            </p>

                            <p style="margin: 0 0 30px 0; color: #4b5563; font-size: 15px; line-height: 1.6;">
                                Good news! Your device reimbursement request has been <strong>approved by your supervisor</strong> and is moving to the next approval stage.
                            </p>

                            <!-- Approval Details -->
                            <table width="100%" cellpadding="0" cellspacing="0" style="margin-bottom: 30px;">
                                <tr>
                                    <td colspan="2" style="background-color: #d1fae5; padding: 12px 20px; border-radius: 8px 8px 0 0; border-bottom: 2px solid #10b981;">
                                        <h2 style="margin: 0; color: #065f46; font-size: 18px; font-weight: 700;">
                                            Approved by Supervisor
                                        </h2>
                                    </td>
                                </tr>
                                <tr>
                                    <td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb; width: 40%;">
                                        <strong style="color: #6b7280; font-size: 14px;">Request ID:</strong>
                                    </td>
                                    <td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb; text-align: right;">
                                        <span style="color: #1f2937; font-size: 14px; font-weight: 700;">#{{RequestId}}</span>
                                    </td>
                                </tr>
                                <tr>
                                    <td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb;">
                                        <strong style="color: #6b7280; font-size: 14px;">Approved By:</strong>
                                    </td>
                                    <td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb; text-align: right;">
                                        <span style="color: #1f2937; font-size: 14px; font-weight: 600;">{{SupervisorName}}</span>
                                    </td>
                                </tr>
                                <tr>
                                    <td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb;">
                                        <strong style="color: #6b7280; font-size: 14px;">Amount:</strong>
                                    </td>
                                    <td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb; text-align: right;">
                                        <span style="color: #1f2937; font-size: 16px; font-weight: 700;">{{DevicePurchaseCurrency}} {{DevicePurchaseAmount}}</span>
                                    </td>
                                </tr>
                                <tr>
                                    <td style="padding: 15px 20px;">
                                        <strong style="color: #6b7280; font-size: 14px;">Status:</strong>
                                    </td>
                                    <td style="padding: 15px 20px; text-align: right;">
                                        <span style="background-color: #fbbf24; color: #1f2937; padding: 6px 16px; border-radius: 12px; font-size: 12px; font-weight: 700; text-transform: uppercase;">
                                            Pending Budget Officer
                                        </span>
                                    </td>
                                </tr>
                            </table>

                            <!-- Next Steps -->
                            <div style="background-color: #dbeafe; border-left: 4px solid #3b82f6; padding: 20px; margin-bottom: 30px; border-radius: 8px;">
                                <p style="margin: 0 0 10px 0; color: #1e40af; font-size: 15px; font-weight: 700;">
                                    Next Steps:
                                </p>
                                <p style="margin: 0; color: #1e40af; font-size: 14px; line-height: 1.6;">
                                    Your request will now be reviewed by the Budget Officer. You will be notified once the next approval is completed.
                                </p>
                            </div>

                            <!-- Call to Action Button -->
                            <div style="text-align: center; margin: 30px 0;">
                                <a href="{{ViewRequestLink}}" style="display: inline-block; background-color: #10b981; color: #ffffff; text-decoration: none; padding: 16px 40px; border-radius: 8px; font-size: 16px; font-weight: 700; box-shadow: 0 4px 12px rgba(16, 185, 129, 0.3); transition: all 0.3s ease;">
                                    View Request Status →
                                </a>
                            </div>

                            <!-- Closing -->
                            <p style="margin: 30px 0 0 0; color: #4b5563; font-size: 14px; line-height: 1.6;">
                                Best regards,<br>
                                <strong>UNON E-Billing System Team</strong>
                            </p>
                        </td>
                    </tr>

                    <!-- Footer -->
                    <tr>
                        <td style="background-color: #f9fafb; padding: 30px; text-align: center; border-top: 1px solid #e5e7eb;">
                            <!-- Rainbow SDG Colors -->
                            <div style="display: flex; height: 5px; margin-bottom: 20px;">
                                <div style="flex: 1; background-color: #E5243B;"></div>
                                <div style="flex: 1; background-color: #DDA63A;"></div>
                                <div style="flex: 1; background-color: #4C9F38;"></div>
                                <div style="flex: 1; background-color: #C5192D;"></div>
                                <div style="flex: 1; background-color: #FF3A21;"></div>
                                <div style="flex: 1; background-color: #26BDE2;"></div>
                                <div style="flex: 1; background-color: #FCC30B;"></div>
                                <div style="flex: 1; background-color: #A21942;"></div>
                                <div style="flex: 1; background-color: #FD6925;"></div>
                                <div style="flex: 1; background-color: #DD1367;"></div>
                                <div style="flex: 1; background-color: #FD9D24;"></div>
                                <div style="flex: 1; background-color: #BF8B2E;"></div>
                                <div style="flex: 1; background-color: #3F7E44;"></div>
                                <div style="flex: 1; background-color: #0A97D9;"></div>
                                <div style="flex: 1; background-color: #56C02B;"></div>
                                <div style="flex: 1; background-color: #00689D;"></div>
                                <div style="flex: 1; background-color: #19486A;"></div>
                            </div>

                            <p style="margin: 0 0 10px 0; color: #6b7280; font-size: 12px;">
                                <strong>United Nations Office at Nairobi</strong><br>
                                UN Headquarters in Africa
                            </p>

                            <p style="margin: 0; color: #9ca3af; font-size: 11px;">
                                DAS | ICTS | Innovation Business Unit<br>
                                Powered by UNON © {{Year}}
                            </p>

                            <p style="margin: 15px 0 0 0; color: #9ca3af; font-size: 11px; line-height: 1.5;">
                                This is an automated message. Please do not reply to this email.<br>
                                For inquiries, please contact ICTS Service Desk at ICTS.Servicedesk@un.org
                            </p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>',
    ModifiedDate = GETDATE()
WHERE TemplateCode = 'REFUND_SUPERVISOR_APPROVED';

IF @@ROWCOUNT > 0
    PRINT 'REFUND_SUPERVISOR_APPROVED updated successfully.';
ELSE
    PRINT 'WARNING: REFUND_SUPERVISOR_APPROVED not found or not updated.';
GO

-- 3. Update REFUND_SUPERVISOR_NOTIFICATION
PRINT 'Updating REFUND_SUPERVISOR_NOTIFICATION...';

UPDATE EmailTemplates
SET HtmlBody = '<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Supervisor Approval Required</title>
</head>
<body style="margin: 0; padding: 0; font-family: ''Segoe UI'', Tahoma, Geneva, Verdana, sans-serif; background-color: #f3f4f6;">
    <table width="100%" cellpadding="0" cellspacing="0" style="background-color: #f3f4f6; padding: 40px 20px;">
        <tr>
            <td align="center">
                <table width="600" cellpadding="0" cellspacing="0" style="background-color: #ffffff; border-radius: 16px; overflow: hidden; box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);">
                    <!-- Header with Logo -->
                    <tr>
                        <td style="background-color: #009edb; padding: 30px; text-align: center;">
                            <img src="cid:logo" alt="UNON E-Billing System" style="max-width: 100%; height: auto; display: block; margin: 0 auto;" width="550" />
                        </td>
                    </tr>

                    <!-- Content -->
                    <tr>
                        <td style="padding: 40px 30px;">
                            <!-- Greeting -->
                            <p style="margin: 0 0 20px 0; color: #1f2937; font-size: 16px; line-height: 1.6;">
                                Dear <strong>{{SupervisorName}}</strong>,
                            </p>

                            <p style="margin: 0 0 30px 0; color: #4b5563; font-size: 15px; line-height: 1.6;">
                                A device reimbursement request from <strong>{{RequesterName}}</strong> requires your approval.
                            </p>

                            <!-- Request Details -->
                            <table width="100%" cellpadding="0" cellspacing="0" style="margin-bottom: 30px;">
                                <tr>
                                    <td colspan="2" style="background-color: #fef3c7; padding: 12px 20px; border-radius: 8px 8px 0 0; border-bottom: 2px solid #f59e0b;">
                                        <h2 style="margin: 0; color: #92400e; font-size: 18px; font-weight: 700;">
                                            Approval Required
                                        </h2>
                                    </td>
                                </tr>
                                <tr>
                                    <td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb; width: 40%;">
                                        <strong style="color: #6b7280; font-size: 14px;">Request ID:</strong>
                                    </td>
                                    <td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb; text-align: right;">
                                        <span style="color: #1f2937; font-size: 14px; font-weight: 700;">#{{RequestId}}</span>
                                    </td>
                                </tr>
                                <tr>
                                    <td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb;">
                                        <strong style="color: #6b7280; font-size: 14px;">Staff Member:</strong>
                                    </td>
                                    <td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb; text-align: right;">
                                        <span style="color: #1f2937; font-size: 14px; font-weight: 600;">{{RequesterName}}</span>
                                    </td>
                                </tr>
                                <tr>
                                    <td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb;">
                                        <strong style="color: #6b7280; font-size: 14px;">Amount:</strong>
                                    </td>
                                    <td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb; text-align: right;">
                                        <span style="color: #1f2937; font-size: 16px; font-weight: 700;">{{DevicePurchaseCurrency}} {{DevicePurchaseAmount}}</span>
                                    </td>
                                </tr>
                                <tr>
                                    <td style="padding: 15px 20px;">
                                        <strong style="color: #6b7280; font-size: 14px;">Mobile:</strong>
                                    </td>
                                    <td style="padding: 15px 20px; text-align: right;">
                                        <span style="color: #1f2937; font-size: 14px; font-weight: 600;">{{PrimaryMobileNumber}}</span>
                                    </td>
                                </tr>
                            </table>

                            <!-- Action Required -->
                            <div style="background-color: #fef3c7; border-left: 4px solid #f59e0b; padding: 20px; margin-bottom: 30px; border-radius: 8px;">
                                <p style="margin: 0; color: #92400e; font-size: 14px; line-height: 1.6; font-weight: 600;">
                                    <strong>Action Required:</strong> Please review this device reimbursement request and provide your approval decision.
                                </p>
                            </div>

                            <!-- Call to Action Button -->
                            <div style="text-align: center; margin: 30px 0;">
                                <a href="{{ApprovalLink}}" style="display: inline-block; background-color: #f59e0b; color: #ffffff; text-decoration: none; padding: 16px 40px; border-radius: 8px; font-size: 16px; font-weight: 700; box-shadow: 0 4px 12px rgba(245, 158, 11, 0.3); transition: all 0.3s ease;">
                                    Review Request →
                                </a>
                            </div>

                            <!-- Closing -->
                            <p style="margin: 30px 0 0 0; color: #4b5563; font-size: 14px; line-height: 1.6;">
                                Best regards,<br>
                                <strong>UNON E-Billing System Team</strong>
                            </p>
                        </td>
                    </tr>

                    <!-- Footer -->
                    <tr>
                        <td style="background-color: #f9fafb; padding: 30px; text-align: center; border-top: 1px solid #e5e7eb;">
                            <!-- Rainbow SDG Colors -->
                            <div style="display: flex; height: 5px; margin-bottom: 20px;">
                                <div style="flex: 1; background-color: #E5243B;"></div>
                                <div style="flex: 1; background-color: #DDA63A;"></div>
                                <div style="flex: 1; background-color: #4C9F38;"></div>
                                <div style="flex: 1; background-color: #C5192D;"></div>
                                <div style="flex: 1; background-color: #FF3A21;"></div>
                                <div style="flex: 1; background-color: #26BDE2;"></div>
                                <div style="flex: 1; background-color: #FCC30B;"></div>
                                <div style="flex: 1; background-color: #A21942;"></div>
                                <div style="flex: 1; background-color: #FD6925;"></div>
                                <div style="flex: 1; background-color: #DD1367;"></div>
                                <div style="flex: 1; background-color: #FD9D24;"></div>
                                <div style="flex: 1; background-color: #BF8B2E;"></div>
                                <div style="flex: 1; background-color: #3F7E44;"></div>
                                <div style="flex: 1; background-color: #0A97D9;"></div>
                                <div style="flex: 1; background-color: #56C02B;"></div>
                                <div style="flex: 1; background-color: #00689D;"></div>
                                <div style="flex: 1; background-color: #19486A;"></div>
                            </div>

                            <p style="margin: 0 0 10px 0; color: #6b7280; font-size: 12px;">
                                <strong>United Nations Office at Nairobi</strong><br>
                                UN Headquarters in Africa
                            </p>

                            <p style="margin: 0; color: #9ca3af; font-size: 11px;">
                                DAS | ICTS | Innovation Business Unit<br>
                                Powered by UNON © {{Year}}
                            </p>

                            <p style="margin: 15px 0 0 0; color: #9ca3af; font-size: 11px; line-height: 1.5;">
                                This is an automated message. Please do not reply to this email.<br>
                                For inquiries, please contact ICTS Service Desk at ICTS.Servicedesk@un.org
                            </p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>',
    ModifiedDate = GETDATE()
WHERE TemplateCode = 'REFUND_SUPERVISOR_NOTIFICATION';

IF @@ROWCOUNT > 0
    PRINT 'REFUND_SUPERVISOR_NOTIFICATION updated successfully.';
ELSE
    PRINT 'WARNING: REFUND_SUPERVISOR_NOTIFICATION not found or not updated.';
GO

-- Final Verification
PRINT '';
PRINT '=== Final Verification ===';
SELECT
    TemplateCode,
    Name,
    ModifiedDate,
    CASE
        WHEN HtmlBody LIKE '%cid:logo%' THEN 'Yes'
        ELSE 'No'
    END AS 'Has Logo',
    CASE
        WHEN HtmlBody LIKE '%linear-gradient%' OR HtmlBody LIKE '%#38ef7d%' OR HtmlBody LIKE '%#f5576c%' OR HtmlBody LIKE '%#e74c3c%' THEN 'Has Gradient/Old Color'
        ELSE 'Clean'
    END AS 'Style',
    CASE
        WHEN HtmlBody LIKE '%✓%' THEN 'Has Emoji'
        ELSE 'No Emoji'
    END AS 'Emoji Status',
    LEN(HtmlBody) as 'Size'
FROM EmailTemplates
WHERE TemplateCode IN (
    'REFUND_REQUEST_REJECTED',
    'REFUND_SUPERVISOR_APPROVED',
    'REFUND_SUPERVISOR_NOTIFICATION'
)
ORDER BY TemplateCode;

PRINT '';
PRINT '=== Update Complete ===';
PRINT 'All 3 REFUND templates have been standardized with:';
PRINT '  - Inline logo (cid:logo)';
PRINT '  - Solid UN blue header (#009edb)';
PRINT '  - SDG rainbow footer';
PRINT '  - No emojis';
PRINT '  - No gradients';
