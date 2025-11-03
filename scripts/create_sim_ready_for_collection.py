#!/usr/bin/env python3

html_content = '''<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>SIM Card Ready for Collection</title>
</head>
<body style="margin: 0; padding: 0; font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f3f4f6;">
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
                                Excellent news! Your SIM card request has been fully approved and processed. Your SIM card is now <strong style="color: #10b981;">ready for collection</strong>.
                            </p>

                            <!-- Status Box -->
                            <div style="background-color: #d1fae5; border-left: 4px solid #10b981; padding: 15px 20px; margin-bottom: 30px; border-radius: 8px;">
                                <p style="margin: 0 0 5px 0; color: #065f46; font-size: 14px; font-weight: 600;">
                                    <strong>Request ID:</strong> #{{RequestId}}
                                </p>
                                <p style="margin: 5px 0; color: #065f46; font-size: 13px;">
                                    <strong>Ready Date:</strong> {{ReadyDate}}
                                </p>
                                <p style="margin: 5px 0 0 0; color: #065f46; font-size: 13px;">
                                    <strong>Status:</strong> <span style="background-color: #10b981; color: #ffffff; padding: 2px 8px; border-radius: 4px; font-weight: 600;">Ready for Collection</span>
                                </p>
                            </div>

                            <!-- SIM Card Details -->
                            <table width="100%" cellpadding="0" cellspacing="0" style="margin-bottom: 30px;">
                                <tr>
                                    <td colspan="2" style="background-color: #dbeafe; padding: 12px 20px; border-radius: 8px 8px 0 0; border-bottom: 2px solid #009edb;">
                                        <h2 style="margin: 0; color: #1e40af; font-size: 18px; font-weight: 700;">Your SIM Card Details</h2>
                                    </td>
                                </tr>
                                <tr>
                                    <td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb; width: 40%;">
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
                                    <td style="padding: 15px 20px;">
                                        <strong style="color: #6b7280; font-size: 14px;">Phone Number:</strong>
                                    </td>
                                    <td style="padding: 15px 20px; text-align: right;">
                                        <span style="color: #1f2937; font-size: 14px; font-weight: 600;">{{PhoneNumber}}</span>
                                    </td>
                                </tr>
                            </table>

                            <!-- Collection Information -->
                            <table width="100%" cellpadding="0" cellspacing="0" style="margin-bottom: 30px;">
                                <tr>
                                    <td colspan="2" style="background-color: #d1fae5; padding: 12px 20px; border-radius: 8px 8px 0 0; border-bottom: 2px solid #10b981;">
                                        <h2 style="margin: 0; color: #065f46; font-size: 18px; font-weight: 700;">Collection Information</h2>
                                    </td>
                                </tr>
                                <tr>
                                    <td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb; width: 40%;">
                                        <strong style="color: #6b7280; font-size: 14px;">Collection Point:</strong>
                                    </td>
                                    <td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb; text-align: right;">
                                        <span style="color: #1f2937; font-size: 14px;">{{CollectionPoint}}</span>
                                    </td>
                                </tr>
                                <tr>
                                    <td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb;">
                                        <strong style="color: #6b7280; font-size: 14px;">Office Hours:</strong>
                                    </td>
                                    <td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb; text-align: right;">
                                        <span style="color: #1f2937; font-size: 14px;">Monday - Friday, 9:00 AM - 5:00 PM</span>
                                    </td>
                                </tr>
                                <tr>
                                    <td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb;">
                                        <strong style="color: #6b7280; font-size: 14px;">Contact Person:</strong>
                                    </td>
                                    <td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb; text-align: right;">
                                        <span style="color: #1f2937; font-size: 14px;">{{ContactPerson}}</span>
                                    </td>
                                </tr>
                                <tr>
                                    <td style="padding: 15px 20px;">
                                        <strong style="color: #6b7280; font-size: 14px;">Contact Phone:</strong>
                                    </td>
                                    <td style="padding: 15px 20px; text-align: right;">
                                        <span style="color: #1f2937; font-size: 14px;">{{ContactPhone}}</span>
                                    </td>
                                </tr>
                            </table>

                            <!-- Before You Collect -->
                            <div style="background-color: #fef3c7; border-radius: 8px; padding: 20px; margin-bottom: 30px;">
                                <h3 style="margin: 0 0 15px 0; color: #92400e; font-size: 16px; font-weight: 700;">Before You Collect</h3>
                                <p style="margin: 0 0 15px 0; color: #78350f; font-size: 14px; font-weight: 600;">Please bring the following items:</p>
                                <ul style="margin: 0; padding-left: 20px; color: #78350f; font-size: 14px; line-height: 1.8;">
                                    <li style="margin-bottom: 10px;"><strong style="color: #92400e;">Valid ID:</strong> Your staff ID card or passport</li>
                                    <li style="margin-bottom: 10px;"><strong style="color: #92400e;">Request ID:</strong> Reference number #{{RequestId}}</li>
                                    <li><strong style="color: #92400e;">This Email:</strong> Digital or printed copy of this notification</li>
                                </ul>
                            </div>

                            <!-- During Collection -->
                            <div style="background-color: #dbeafe; border-radius: 8px; padding: 20px; margin-bottom: 30px;">
                                <h3 style="margin: 0 0 15px 0; color: #1e40af; font-size: 16px; font-weight: 700;">During Collection</h3>
                                <ol style="margin: 0; padding-left: 20px; color: #1e40af; font-size: 14px; line-height: 1.8;">
                                    <li style="margin-bottom: 10px;"><strong style="color: #1e40af;">Identity Verification:</strong> Present your ID for verification</li>
                                    <li style="margin-bottom: 10px;"><strong style="color: #1e40af;">Receive SIM Card:</strong> Collect your SIM card and activation instructions</li>
                                    <li style="margin-bottom: 10px;"><strong style="color: #1e40af;">Sign Acknowledgment:</strong> Sign collection form to confirm receipt</li>
                                    <li><strong style="color: #1e40af;">Get Support:</strong> Staff will assist with any immediate questions</li>
                                </ol>
                            </div>

                            <!-- Collection Deadline -->
                            <div style="background-color: #fef2f2; border-left: 4px solid #dc2626; padding: 20px; margin-bottom: 30px; border-radius: 8px;">
                                <p style="margin: 0; color: #991b1b; font-size: 14px; line-height: 1.6;">
                                    <strong>Collection Deadline:</strong> Please collect your SIM card within <strong>5 business days</strong> (by {{CollectionDeadline}}). After this date, you may need to submit a new request.
                                </p>
                            </div>

                            <!-- Call to Action Button -->
                            <div style="text-align: center; margin: 30px 0;">
                                <a href="{{ViewRequestLink}}" style="display: inline-block; background-color: #009edb; color: #ffffff; text-decoration: none; padding: 16px 40px; border-radius: 8px; font-size: 16px; font-weight: 700; box-shadow: 0 4px 12px rgba(0, 158, 219, 0.3); transition: all 0.3s ease;">
                                    View Full Details →
                                </a>
                            </div>

                            <!-- Questions Section -->
                            <div style="background-color: #eff6ff; border-left: 4px solid #3b82f6; padding: 20px; margin-bottom: 30px; border-radius: 8px;">
                                <p style="margin: 0; color: #1e40af; font-size: 14px; line-height: 1.6;">
                                    <strong>Questions?</strong> Contact {{ContactPerson}} at {{ContactPhone}} or visit {{CollectionPoint}} during office hours.
                                </p>
                            </div>

                            <!-- Closing -->
                            <p style="margin: 0 0 10px 0; color: #4b5563; font-size: 15px; line-height: 1.6;">
                                We're excited for you to receive your new SIM card. Thank you for your patience throughout the approval process!
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
</html>'''

# Escape single quotes for SQL
html_escaped = html_content.replace("'", "''")

# Create SQL UPDATE script
sql_script = f"""-- Update SIM_READY_FOR_COLLECTION template
SET NOCOUNT ON;
GO

PRINT 'Updating SIM_READY_FOR_COLLECTION...';

UPDATE EmailTemplates
SET HtmlBody = '{html_escaped}',
    ModifiedDate = GETDATE()
WHERE TemplateCode = 'SIM_READY_FOR_COLLECTION';

IF @@ROWCOUNT > 0
    PRINT 'SIM_READY_FOR_COLLECTION updated successfully.';
ELSE
    PRINT 'WARNING: SIM_READY_FOR_COLLECTION not found or not updated.';
GO

SELECT TemplateCode, Name, ModifiedDate, LEN(HtmlBody) as 'Size',
CASE WHEN HtmlBody LIKE '%cid:logo%' THEN 'Yes' ELSE 'No' END AS 'Has Logo',
CASE WHEN HtmlBody LIKE '%linear-gradient%' THEN 'Gradient' ELSE 'Clean' END AS 'Status'
FROM EmailTemplates WHERE TemplateCode = 'SIM_READY_FOR_COLLECTION';
GO
"""

# Write to file
with open('UPDATE_SIM_READY_FOR_COLLECTION.sql', 'w', encoding='utf-8') as f:
    f.write(sql_script)

print("SQL script for SIM_READY_FOR_COLLECTION created successfully")
