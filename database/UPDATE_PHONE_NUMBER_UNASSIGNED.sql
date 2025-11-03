-- Update PHONE_NUMBER_UNASSIGNED template
SET NOCOUNT ON;
GO

PRINT 'Updating PHONE_NUMBER_UNASSIGNED...';

UPDATE EmailTemplates
SET HtmlBody = '<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Phone Number Unassigned</title>
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
                            <p style="margin: 0 0 20px 0; color: #1f2937; font-size: 16px; line-height: 1.6;">
                                Dear <strong>{{FirstName}} {{LastName}}</strong>,
                            </p>

                            <p style="margin: 0 0 30px 0; color: #4b5563; font-size: 15px; line-height: 1.6;">
                                A phone number has been unassigned from your account in the UNON E-Billing System. Please review the details below and note the important information regarding this change.
                            </p>

                            <!-- Phone Details Section -->
                            <table width="100%" cellpadding="0" cellspacing="0" style="margin-bottom: 30px;">
                                <tr>
                                    <td colspan="2" style="background-color: #fee2e2; padding: 12px 20px; border-radius: 8px 8px 0 0; border-bottom: 2px solid #dc2626;">
                                        <h2 style="margin: 0; color: #991b1b; font-size: 18px; font-weight: 700;">
                                            Unassigned Phone Details
                                        </h2>
                                    </td>
                                </tr>
                                <tr>
                                    <td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb; width: 40%;">
                                        <strong style="color: #6b7280; font-size: 14px;">Phone Number:</strong>
                                    </td>
                                    <td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb; text-align: right;">
                                        <span style="color: #1f2937; font-size: 16px; font-weight: 700; text-decoration: line-through;">{{PhoneNumber}}</span>
                                    </td>
                                </tr>
                                <tr>
                                    <td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb;">
                                        <strong style="color: #6b7280; font-size: 14px;">Previous Phone Type:</strong>
                                    </td>
                                    <td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb; text-align: right;">
                                        <span style="background-color: #fee2e2; color: #991b1b; padding: 4px 12px; border-radius: 12px; font-size: 13px; font-weight: 600;">{{PhoneType}}</span>
                                    </td>
                                </tr>
                                <tr>
                                    <td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb;">
                                        <strong style="color: #6b7280; font-size: 14px;">Previous Line Type:</strong>
                                    </td>
                                    <td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb; text-align: right;">
                                        <span style="background-color: #fee2e2; color: #991b1b; padding: 4px 12px; border-radius: 12px; font-size: 13px; font-weight: 600;">{{LineType}}</span>
                                    </td>
                                </tr>
                                <tr>
                                    <td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb;">
                                        <strong style="color: #6b7280; font-size: 14px;">Index Number:</strong>
                                    </td>
                                    <td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb; text-align: right;">
                                        <span style="color: #1f2937; font-size: 14px; font-weight: 600;">{{IndexNumber}}</span>
                                    </td>
                                </tr>
                                <tr>
                                    <td style="padding: 15px 20px;">
                                        <strong style="color: #6b7280; font-size: 14px;">Unassigned Date:</strong>
                                    </td>
                                    <td style="padding: 15px 20px; text-align: right;">
                                        <span style="color: #1f2937; font-size: 14px;">{{UnassignedDate}}</span>
                                    </td>
                                </tr>
                            </table>

                            <!-- Important Notice -->
                            <div style="background-color: #fef2f2; border-left: 4px solid #dc2626; padding: 20px; margin-bottom: 30px; border-radius: 8px;">
                                <p style="margin: 0 0 10px 0; color: #991b1b; font-size: 15px; font-weight: 700;">
                                    Important Notice
                                </p>
                                <p style="margin: 0 0 10px 0; color: #7f1d1d; font-size: 14px; line-height: 1.6;">
                                    This phone number is no longer assigned to you and will not appear in future call logs under your account.
                                </p>
                                <p style="margin: 0; color: #7f1d1d; font-size: 14px; line-height: 1.6;">
                                    <strong>Note:</strong> Historical call logs for this number remain in the system for record-keeping purposes.
                                </p>
                            </div>

                            <!-- What This Means -->
                            <div style="background-color: #fffbeb; border-radius: 8px; padding: 20px; margin-bottom: 30px;">
                                <h3 style="margin: 0 0 15px 0; color: #78350f; font-size: 16px; font-weight: 700;">
                                    What This Means For You:
                                </h3>
                                <ul style="margin: 0; padding-left: 20px; color: #78350f; font-size: 14px; line-height: 1.8;">
                                    <li style="margin-bottom: 8px;">This phone number is no longer officially assigned to you</li>
                                    <li style="margin-bottom: 8px;">New call logs for this number will NOT appear in your account</li>
                                    <li style="margin-bottom: 8px;">You are no longer responsible for verifying charges for this number</li>
                                    <li style="margin-bottom: 8px;">Historical records remain accessible for auditing purposes</li>
                                    <li>If this number was set as your primary contact, your official mobile number has been updated</li>
                                </ul>
                            </div>

                            <!-- Reason for Unassignment -->
                            {{#if Reason}}
                            <div style="background-color: #eff6ff; border-left: 4px solid #3b82f6; padding: 20px; margin-bottom: 30px; border-radius: 8px;">
                                <p style="margin: 0 0 10px 0; color: #1e40af; font-size: 15px; font-weight: 700;">Reason for Unassignment:</p>
                                <p style="margin: 0; color: #1e40af; font-size: 14px; line-height: 1.6;">
                                    {{Reason}}
                                </p>
                            </div>
                            {{/if}}

                            <!-- Call to Action Button -->
                            <div style="text-align: center; margin: 30px 0;">
                                <a href="{{ViewPhoneDetailsLink}}" style="display: inline-block; background-color: #009edb; color: #ffffff; text-decoration: none; padding: 16px 40px; border-radius: 8px; font-size: 16px; font-weight: 700; box-shadow: 0 4px 12px rgba(0, 158, 219, 0.3); transition: all 0.3s ease;">
                                    View My Current Phones →
                                </a>
                            </div>

                            <!-- Contact Information -->
                            <div style="background-color: #fef3c7; border-left: 4px solid #f59e0b; padding: 20px; margin-bottom: 30px; border-radius: 8px;">
                                <p style="margin: 0 0 10px 0; color: #92400e; font-size: 14px; font-weight: 700;">Questions About This Change?</p>
                                <p style="margin: 0 0 10px 0; color: #78350f; font-size: 13px; line-height: 1.6;">
                                    If you believe this phone number should still be assigned to you or if you have any questions, please contact:
                                </p>
                                <p style="margin: 0; color: #78350f; font-size: 13px; line-height: 1.6;">
                                    <strong>UNON ICTS Service Desk</strong><br>
                                    Email: <a href="mailto:ICTS.Servicedesk@un.org" style="color: #f59e0b; text-decoration: none;">ICTS.Servicedesk@un.org</a><br>
                                    Phone: +254 20 76 21111
                                </p>
                            </div>

                            <!-- Closing -->
                            <p style="margin: 0; color: #4b5563; font-size: 14px; line-height: 1.6;">
                                Best regards,<br>
                                <strong>UNON E-Billing System Team</strong>
                            </p>
                        </td>
                    </tr>

                    <!-- Footer -->
                    <tr>
                        <td style="background-color: #f9fafb; padding: 30px; text-align: center; border-top: 1px solid #e5e7eb;">
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
WHERE TemplateCode = 'PHONE_NUMBER_UNASSIGNED';

IF @@ROWCOUNT > 0
    PRINT 'PHONE_NUMBER_UNASSIGNED updated successfully.';
ELSE
    PRINT 'WARNING: PHONE_NUMBER_UNASSIGNED not found or not updated.';
GO

SELECT TemplateCode, Name, ModifiedDate, LEN(HtmlBody) as 'Size',
CASE WHEN HtmlBody LIKE '%cid:logo%' THEN 'Yes' ELSE 'No' END AS 'Has Logo',
CASE WHEN HtmlBody LIKE '%linear-gradient%' OR HtmlBody LIKE '%📵%' THEN 'Old' ELSE 'Clean' END AS 'Status'
FROM EmailTemplates WHERE TemplateCode = 'PHONE_NUMBER_UNASSIGNED';
GO
