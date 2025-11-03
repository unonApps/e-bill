-- Standardize final three REFUND templates with inline logo, solid colors, no emojis
SET NOCOUNT ON;
GO

-- 1. Update REFUND_CLAIMS_UNIT_NOTIFICATION
PRINT 'Updating REFUND_CLAIMS_UNIT_NOTIFICATION...';

UPDATE EmailTemplates
SET HtmlBody = '<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Claims Processing Required</title>
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
                                Dear <strong>Staff Claims Unit</strong>,
                            </p>

                            <p style="margin: 0 0 30px 0; color: #4b5563; font-size: 15px; line-height: 1.6;">
                                A device reimbursement request is ready for <strong>claims processing</strong> after receiving all required approvals.
                            </p>

                            <!-- Request Details -->
                            <table width="100%" cellpadding="0" cellspacing="0" style="margin-bottom: 30px;">
                                <tr>
                                    <td colspan="2" style="background-color: #fff3cd; padding: 12px 20px; border-radius: 8px 8px 0 0; border-bottom: 2px solid #ffc107;">
                                        <h2 style="margin: 0; color: #856404; font-size: 18px; font-weight: 700;">
                                            Ready for Claims Processing
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
                                        <strong style="color: #6b7280; font-size: 14px;">Organization:</strong>
                                    </td>
                                    <td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb; text-align: right;">
                                        <span style="color: #1f2937; font-size: 14px; font-weight: 600;">{{Organization}}</span>
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
                                    <td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb;">
                                        <strong style="color: #6b7280; font-size: 14px;">Bank:</strong>
                                    </td>
                                    <td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb; text-align: right;">
                                        <span style="color: #1f2937; font-size: 14px; font-weight: 600;">{{UmojaBankName}}</span>
                                    </td>
                                </tr>
                                <tr>
                                    <td style="padding: 15px 20px;">
                                        <strong style="color: #6b7280; font-size: 14px;">Prior Approvals:</strong>
                                    </td>
                                    <td style="padding: 15px 20px; text-align: right;">
                                        <span style="color: #28a745; font-size: 13px; font-weight: 600;">Supervisor (Approved) | Budget Officer (Approved)</span>
                                    </td>
                                </tr>
                            </table>

                            <!-- Action Required -->
                            <div style="background-color: #fff3cd; border-left: 4px solid #ffc107; padding: 20px; margin-bottom: 30px; border-radius: 8px;">
                                <p style="margin: 0; color: #856404; font-size: 14px; line-height: 1.6; font-weight: 600;">
                                    <strong>Action Required:</strong> Please process this claim and create the necessary documentation in Umoja for payment approval.
                                </p>
                            </div>

                            <!-- Call to Action Button -->
                            <div style="text-align: center; margin: 30px 0;">
                                <a href="{{ProcessLink}}" style="display: inline-block; background-color: #009edb; color: #ffffff; text-decoration: none; padding: 16px 40px; border-radius: 8px; font-size: 16px; font-weight: 700; box-shadow: 0 4px 12px rgba(0, 158, 219, 0.3); transition: all 0.3s ease;">
                                    Process Claim →
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
WHERE TemplateCode = 'REFUND_CLAIMS_UNIT_NOTIFICATION';

IF @@ROWCOUNT > 0
    PRINT 'REFUND_CLAIMS_UNIT_NOTIFICATION updated successfully.';
ELSE
    PRINT 'WARNING: REFUND_CLAIMS_UNIT_NOTIFICATION not found or not updated.';
GO

-- 2. Update REFUND_PAYMENT_APPROVER_NOTIFICATION
PRINT 'Updating REFUND_PAYMENT_APPROVER_NOTIFICATION...';

UPDATE EmailTemplates
SET HtmlBody = '<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Final Payment Approval Required</title>
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
                                Dear <strong>Payment Approver</strong>,
                            </p>

                            <p style="margin: 0 0 30px 0; color: #4b5563; font-size: 15px; line-height: 1.6;">
                                A device reimbursement payment requires your <strong>final approval</strong> before funds can be disbursed.
                            </p>

                            <!-- Payment Details -->
                            <table width="100%" cellpadding="0" cellspacing="0" style="margin-bottom: 30px;">
                                <tr>
                                    <td colspan="2" style="background-color: #fef2f2; padding: 12px 20px; border-radius: 8px 8px 0 0; border-bottom: 2px solid #dc3545;">
                                        <h2 style="margin: 0; color: #991b1b; font-size: 18px; font-weight: 700;">
                                            Final Payment Approval Required
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
                                        <strong style="color: #6b7280; font-size: 14px;">Payment Amount:</strong>
                                    </td>
                                    <td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb; text-align: right;">
                                        <span style="color: #dc3545; font-size: 18px; font-weight: 700;">USD {{RefundUsdAmount}}</span>
                                    </td>
                                </tr>
                                <tr>
                                    <td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb;">
                                        <strong style="color: #6b7280; font-size: 14px;">Umoja Document ID:</strong>
                                    </td>
                                    <td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb; text-align: right;">
                                        <span style="color: #1f2937; font-size: 14px; font-weight: 600;">{{UmojaPaymentDocumentId}}</span>
                                    </td>
                                </tr>
                                <tr>
                                    <td style="padding: 15px 20px;">
                                        <strong style="color: #6b7280; font-size: 14px;">Prior Approvals:</strong>
                                    </td>
                                    <td style="padding: 15px 20px; text-align: right;">
                                        <span style="color: #28a745; font-size: 13px; font-weight: 600;">Supervisor | Budget | Claims (All Approved)</span>
                                    </td>
                                </tr>
                            </table>

                            <!-- Action Required -->
                            <div style="background-color: #fef2f2; border-left: 4px solid #dc3545; padding: 20px; margin-bottom: 30px; border-radius: 8px;">
                                <p style="margin: 0; color: #991b1b; font-size: 14px; line-height: 1.6; font-weight: 600;">
                                    <strong>Action Required:</strong> This is the final approval step. Please review the payment details and approve to authorize disbursement.
                                </p>
                            </div>

                            <!-- Call to Action Button -->
                            <div style="text-align: center; margin: 30px 0;">
                                <a href="{{ApprovalLink}}" style="display: inline-block; background-color: #dc3545; color: #ffffff; text-decoration: none; padding: 16px 40px; border-radius: 8px; font-size: 16px; font-weight: 700; box-shadow: 0 4px 12px rgba(220, 53, 69, 0.3); transition: all 0.3s ease;">
                                    Approve Payment →
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
WHERE TemplateCode = 'REFUND_PAYMENT_APPROVER_NOTIFICATION';

IF @@ROWCOUNT > 0
    PRINT 'REFUND_PAYMENT_APPROVER_NOTIFICATION updated successfully.';
ELSE
    PRINT 'WARNING: REFUND_PAYMENT_APPROVER_NOTIFICATION not found or not updated.';
GO

-- 3. Update REFUND_PAYMENT_APPROVED
PRINT 'Updating REFUND_PAYMENT_APPROVED...';

UPDATE EmailTemplates
SET HtmlBody = '<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Payment Approved - Reimbursement Complete</title>
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

                    <!-- Success Banner -->
                    <tr>
                        <td style="background-color: #d4edda; padding: 20px; text-align: center; border-bottom: 3px solid #28a745;">
                            <h1 style="margin: 0; font-size: 28px; color: #155724; font-weight: 700;">
                                Payment Approved!
                            </h1>
                            <p style="margin: 8px 0 0 0; color: #155724; font-size: 14px;">
                                Your device reimbursement has been fully processed
                            </p>
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
                                Congratulations! Your device reimbursement has been <strong>approved</strong> and the payment is being processed.
                            </p>

                            <!-- Payment Details -->
                            <table width="100%" cellpadding="0" cellspacing="0" style="margin-bottom: 30px;">
                                <tr>
                                    <td colspan="2" style="background-color: #d4edda; padding: 12px 20px; border-radius: 8px 8px 0 0; border-bottom: 2px solid #28a745;">
                                        <h2 style="margin: 0; color: #155724; font-size: 18px; font-weight: 700;">
                                            Payment Approved
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
                                        <strong style="color: #6b7280; font-size: 14px;">Approved Amount:</strong>
                                    </td>
                                    <td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb; text-align: right;">
                                        <span style="color: #28a745; font-size: 20px; font-weight: 700;">USD {{RefundUsdAmount}}</span>
                                    </td>
                                </tr>
                                <tr>
                                    <td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb;">
                                        <strong style="color: #6b7280; font-size: 14px;">Payment Reference:</strong>
                                    </td>
                                    <td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb; text-align: right;">
                                        <span style="color: #1f2937; font-size: 14px; font-weight: 600;">{{PaymentReference}}</span>
                                    </td>
                                </tr>
                                <tr>
                                    <td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb;">
                                        <strong style="color: #6b7280; font-size: 14px;">Completion Date:</strong>
                                    </td>
                                    <td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb; text-align: right;">
                                        <span style="color: #1f2937; font-size: 14px; font-weight: 600;">{{CompletionDate}}</span>
                                    </td>
                                </tr>
                                <tr>
                                    <td style="padding: 15px 20px;">
                                        <strong style="color: #6b7280; font-size: 14px;">Status:</strong>
                                    </td>
                                    <td style="padding: 15px 20px; text-align: right;">
                                        <span style="background-color: #28a745; color: #ffffff; padding: 6px 16px; border-radius: 12px; font-size: 12px; font-weight: 700; text-transform: uppercase;">
                                            COMPLETED
                                        </span>
                                    </td>
                                </tr>
                            </table>

                            <!-- Payment Timeline -->
                            <div style="background-color: #f0f9ff; border-left: 4px solid #009edb; padding: 20px; margin-bottom: 30px; border-radius: 8px;">
                                <p style="margin: 0 0 10px 0; color: #0c4a6e; font-size: 15px; font-weight: 700;">
                                    Payment Processing Timeline:
                                </p>
                                <p style="margin: 0; color: #0c4a6e; font-size: 14px; line-height: 1.6;">
                                    The payment will be processed according to standard procedures. Please allow <strong>5-7 business days</strong> for the funds to reflect in your account.
                                </p>
                            </div>

                            <!-- Success Message -->
                            <div style="background-color: #d4edda; border-left: 4px solid #28a745; padding: 20px; margin-bottom: 30px; border-radius: 8px;">
                                <p style="margin: 0; color: #155724; font-size: 14px; line-height: 1.6;">
                                    Your reimbursement request has been successfully completed! All approvals have been obtained and payment has been authorized.
                                </p>
                            </div>

                            <!-- Call to Action Button -->
                            <div style="text-align: center; margin: 30px 0;">
                                <a href="{{ViewRequestLink}}" style="display: inline-block; background-color: #28a745; color: #ffffff; text-decoration: none; padding: 16px 40px; border-radius: 8px; font-size: 16px; font-weight: 700; box-shadow: 0 4px 12px rgba(40, 167, 69, 0.3); transition: all 0.3s ease;">
                                    View Completed Request →
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
WHERE TemplateCode = 'REFUND_PAYMENT_APPROVED';

IF @@ROWCOUNT > 0
    PRINT 'REFUND_PAYMENT_APPROVED updated successfully.';
ELSE
    PRINT 'WARNING: REFUND_PAYMENT_APPROVED not found or not updated.';
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
        WHEN HtmlBody LIKE '%linear-gradient%' THEN 'Gradient'
        ELSE 'Clean'
    END AS 'Style',
    LEN(HtmlBody) as 'Size'
FROM EmailTemplates
WHERE TemplateCode IN (
    'REFUND_CLAIMS_UNIT_NOTIFICATION',
    'REFUND_PAYMENT_APPROVER_NOTIFICATION',
    'REFUND_PAYMENT_APPROVED'
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
