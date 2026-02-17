-- =============================================
-- SIM Request Completed - Email Template
-- =============================================
-- This script inserts the email template for SIM request completion notification
-- Sent to the requester when their SIM card request is fully completed
-- =============================================

USE [TABDB]
GO

PRINT 'Installing SIM_REQUEST_COMPLETED email template...'

-- Check if template already exists
IF NOT EXISTS (SELECT 1 FROM EmailTemplates WHERE TemplateCode = 'SIM_REQUEST_COMPLETED')
BEGIN
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
        'SIM Request - Completion Notification',
        'SIM_REQUEST_COMPLETED',
        'Your SIM Card Request is Complete! - #{{RequestId}}',
        '<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>SIM Card Request Completed</title>
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
                                We are pleased to inform you that your SIM card request has been <strong style="color: #10b981;">successfully completed</strong>. Your SIM card has been issued and is now active.
                            </p>

                            <!-- Status Box -->
                            <div style="background-color: #d1fae5; border-left: 4px solid #10b981; padding: 15px 20px; margin-bottom: 30px; border-radius: 8px;">
                                <p style="margin: 0 0 5px 0; color: #065f46; font-size: 14px; font-weight: 600;">
                                    <strong>Request ID:</strong> #{{RequestId}}
                                </p>
                                <p style="margin: 5px 0; color: #065f46; font-size: 13px;">
                                    <strong>Completed:</strong> {{CompletionDate}}
                                </p>
                                <p style="margin: 5px 0 0 0; color: #065f46; font-size: 13px;">
                                    <strong>Status:</strong> <span style="background-color: #10b981; color: #ffffff; padding: 2px 8px; border-radius: 4px; font-weight: 600;">Completed</span>
                                </p>
                            </div>

                            <!-- SIM Card Details -->
                            <table width="100%" cellpadding="0" cellspacing="0" style="margin-bottom: 30px;">
                                <tr>
                                    <td colspan="2" style="background-color: #d1fae5; padding: 12px 20px; border-radius: 8px 8px 0 0; border-bottom: 2px solid #10b981;">
                                        <h2 style="margin: 0; color: #065f46; font-size: 18px; font-weight: 700;">Your SIM Card Details</h2>
                                    </td>
                                </tr>
                                <tr>
                                    <td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb; width: 40%;">
                                        <strong style="color: #6b7280; font-size: 14px;">Phone Number:</strong>
                                    </td>
                                    <td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb; text-align: right;">
                                        <span style="color: #1f2937; font-size: 14px; font-weight: 600;">{{PhoneNumber}}</span>
                                    </td>
                                </tr>
                                <tr>
                                    <td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb;">
                                        <strong style="color: #6b7280; font-size: 14px;">SIM Type:</strong>
                                    </td>
                                    <td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb; text-align: right;">
                                        <span style="color: #1f2937; font-size: 14px;">{{SimType}}</span>
                                    </td>
                                </tr>
                                <tr>
                                    <td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb;">
                                        <strong style="color: #6b7280; font-size: 14px;">Service Provider:</strong>
                                    </td>
                                    <td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb; text-align: right;">
                                        <span style="color: #1f2937; font-size: 14px;">{{ServiceProvider}}</span>
                                    </td>
                                </tr>
                                <tr>
                                    <td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb;">
                                        <strong style="color: #6b7280; font-size: 14px;">Index Number:</strong>
                                    </td>
                                    <td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb; text-align: right;">
                                        <span style="color: #1f2937; font-size: 14px;">{{IndexNo}}</span>
                                    </td>
                                </tr>
                                <tr>
                                    <td style="padding: 15px 20px;">
                                        <strong style="color: #6b7280; font-size: 14px;">Processed By:</strong>
                                    </td>
                                    <td style="padding: 15px 20px; text-align: right;">
                                        <span style="color: #1f2937; font-size: 14px;">{{ProcessedBy}}</span>
                                    </td>
                                </tr>
                            </table>

                            <!-- E-Bill Account Info -->
                            <div style="background-color: #dbeafe; border-left: 4px solid #009edb; padding: 15px 20px; margin-bottom: 30px; border-radius: 8px;">
                                <p style="margin: 0 0 8px 0; color: #1e40af; font-size: 14px; font-weight: 600;">E-Bill Account Created</p>
                                <p style="margin: 0; color: #1e40af; font-size: 14px; line-height: 1.5;">An e-bill account has been set up for your new phone number. You can now view and manage your call logs through the E-Billing System.</p>
                            </div>

                            <!-- What Happens Next -->
                            <div style="background-color: #f9fafb; border-radius: 8px; padding: 20px; margin-bottom: 30px;">
                                <h2 style="margin: 0 0 15px 0; color: #1f2937; font-size: 18px; font-weight: 700;">What Happens Next?</h2>
                                <ol style="margin: 0; padding-left: 20px; color: #4b5563; font-size: 14px; line-height: 1.8;">
                                    <li style="margin-bottom: 10px;"><strong style="color: #1f2937;">Activate Your SIM:</strong> If not already activated, follow the instructions provided with your SIM card.</li>
                                    <li style="margin-bottom: 10px;"><strong style="color: #1f2937;">E-Bill Access:</strong> Log into the E-Billing System to view your call logs and manage your account.</li>
                                    <li><strong style="color: #1f2937;">Monthly Verification:</strong> Remember to verify your call logs each month through the E-Billing portal.</li>
                                </ol>
                            </div>

                            <!-- Call to Action Button -->
                            <div style="text-align: center; margin: 30px 0;">
                                <a href="{{ViewRequestLink}}" style="display: inline-block; background-color: #10b981; color: #ffffff; text-decoration: none; padding: 16px 40px; border-radius: 8px; font-size: 16px; font-weight: 700; box-shadow: 0 4px 12px rgba(16, 185, 129, 0.3);">
                                    View Request Details
                                </a>
                            </div>

                            <!-- Closing -->
                            <p style="margin: 30px 0 10px 0; color: #4b5563; font-size: 15px; line-height: 1.6;">
                                Thank you for your patience throughout the approval process. If you have any questions about your SIM card or e-bill account, please contact the ICTS Service Desk.
                            </p>
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
                                Powered by UNON &copy; {{Year}}
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
        'Notification sent to requester when their SIM card request is fully completed. Includes SIM details, phone number, and e-bill account information.',
        'RequestId, FirstName, LastName, CompletionDate, PhoneNumber, SimType, ServiceProvider, IndexNo, ProcessedBy, ViewRequestLink, Year',
        'SIM Management',
        1,
        1,
        GETUTCDATE()
    );

    PRINT 'SIM_REQUEST_COMPLETED template installed successfully.';
END
ELSE
BEGIN
    PRINT 'SIM_REQUEST_COMPLETED template already exists - skipping.';
END
GO

-- Verify installation
SELECT TemplateCode, Name, Category, IsActive,
    CASE WHEN HtmlBody LIKE '%cid:logo%' THEN 'Yes' ELSE 'No' END AS [Has Logo],
    LEN(HtmlBody) AS [Size]
FROM EmailTemplates
WHERE TemplateCode = 'SIM_REQUEST_COMPLETED';
GO
