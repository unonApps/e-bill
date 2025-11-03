#!/usr/bin/env python3

html_content = '''<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>SIM Card Request Rejected</title>
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
                                We regret to inform you that your SIM card request has been <strong style="color: #dc2626;">rejected</strong>. Please review the details below and contact your supervisor if you have any questions.
                            </p>

                            <!-- Status Box -->
                            <div style="background-color: #fee2e2; border-left: 4px solid #dc2626; padding: 15px 20px; margin-bottom: 30px; border-radius: 8px;">
                                <p style="margin: 0 0 5px 0; color: #991b1b; font-size: 14px; font-weight: 600;">
                                    <strong>Request ID:</strong> #{{RequestId}}
                                </p>
                                <p style="margin: 5px 0; color: #991b1b; font-size: 13px;">
                                    <strong>Submitted:</strong> {{RequestDate}}
                                </p>
                                <p style="margin: 5px 0; color: #991b1b; font-size: 13px;">
                                    <strong>Rejected:</strong> {{RejectionDate}}
                                </p>
                                <p style="margin: 5px 0 0 0; color: #991b1b; font-size: 13px;">
                                    <strong>Status:</strong> <span style="background-color: #dc2626; color: #ffffff; padding: 2px 8px; border-radius: 4px; font-weight: 600;">Rejected</span>
                                </p>
                            </div>

                            <!-- Request Details -->
                            <table width="100%" cellpadding="0" cellspacing="0" style="margin-bottom: 30px;">
                                <tr>
                                    <td colspan="2" style="background-color: #fee2e2; padding: 12px 20px; border-radius: 8px 8px 0 0; border-bottom: 2px solid #dc2626;">
                                        <h2 style="margin: 0; color: #991b1b; font-size: 18px; font-weight: 700;">Request Details</h2>
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
                                    <td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb;">
                                        <strong style="color: #6b7280; font-size: 14px;">Rejected By:</strong>
                                    </td>
                                    <td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb; text-align: right;">
                                        <span style="color: #1f2937; font-size: 14px;">{{RejectedBy}} ({{RejectedByRole}})</span>
                                    </td>
                                </tr>
                                <tr>
                                    <td style="padding: 15px 20px;">
                                        <strong style="color: #6b7280; font-size: 14px;">Rejection Date:</strong>
                                    </td>
                                    <td style="padding: 15px 20px; text-align: right;">
                                        <span style="color: #1f2937; font-size: 14px;">{{RejectionDate}}</span>
                                    </td>
                                </tr>
                            </table>

                            <!-- Rejection Reason -->
                            <div style="background-color: #fef3c7; border-left: 4px solid #f59e0b; padding: 20px; margin-bottom: 30px; border-radius: 8px;">
                                <p style="margin: 0 0 10px 0; color: #92400e; font-size: 15px; font-weight: 700;">Reason for Rejection:</p>
                                <p style="margin: 0; color: #78350f; font-size: 14px; line-height: 1.6;">
                                    {{RejectionReason}}
                                </p>
                            </div>

                            <!-- What This Means -->
                            <div style="background-color: #f9fafb; border-radius: 8px; padding: 20px; margin-bottom: 30px;">
                                <h3 style="margin: 0 0 15px 0; color: #1f2937; font-size: 16px; font-weight: 700;">What This Means</h3>
                                <ul style="margin: 0; padding-left: 20px; color: #4b5563; font-size: 14px; line-height: 1.8;">
                                    <li style="margin-bottom: 10px;">Your request #{{RequestId}} has not been approved and will not be processed.</li>
                                    <li style="margin-bottom: 10px;">No SIM card will be issued for this request.</li>
                                    <li style="margin-bottom: 10px;">Please review the rejection reason carefully.</li>
                                    <li>You may submit a new request after addressing the concerns raised.</li>
                                </ul>
                            </div>

                            <!-- Next Steps -->
                            <div style="background-color: #dbeafe; border-radius: 8px; padding: 20px; margin-bottom: 30px;">
                                <h3 style="margin: 0 0 15px 0; color: #1e40af; font-size: 16px; font-weight: 700;">Next Steps</h3>
                                <p style="margin: 0 0 15px 0; color: #1e40af; font-size: 14px; line-height: 1.6;">If you believe this rejection was made in error or would like to address the concerns raised:</p>
                                <ol style="margin: 0; padding-left: 20px; color: #1e40af; font-size: 14px; line-height: 1.8;">
                                    <li style="margin-bottom: 10px;"><strong style="color: #1e40af;">Contact Your Supervisor:</strong> Discuss the rejection reason and any concerns.</li>
                                    <li style="margin-bottom: 10px;"><strong style="color: #1e40af;">Address Issues:</strong> Take necessary steps to resolve the concerns mentioned.</li>
                                    <li><strong style="color: #1e40af;">Resubmit Request:</strong> You may submit a new request once issues are addressed.</li>
                                </ol>
                            </div>

                            <!-- Call to Action Buttons -->
                            <div style="text-align: center; margin: 30px 0;">
                                <table width="100%" cellpadding="0" cellspacing="0">
                                    <tr>
                                        <td align="center" style="padding-bottom: 15px;">
                                            <a href="{{NewRequestLink}}" style="display: inline-block; background-color: #10b981; color: #ffffff; text-decoration: none; padding: 16px 40px; border-radius: 8px; font-size: 16px; font-weight: 700; box-shadow: 0 4px 12px rgba(16, 185, 129, 0.3);">
                                                Submit New Request →
                                            </a>
                                        </td>
                                    </tr>
                                    <tr>
                                        <td align="center">
                                            <a href="{{MyRequestsLink}}" style="display: inline-block; background-color: #009edb; color: #ffffff; text-decoration: none; padding: 16px 40px; border-radius: 8px; font-size: 16px; font-weight: 700; box-shadow: 0 4px 12px rgba(0, 158, 219, 0.3);">
                                                View My Requests →
                                            </a>
                                        </td>
                                    </tr>
                                </table>
                            </div>

                            <!-- Contact Information -->
                            <div style="background-color: #eff6ff; border-left: 4px solid #3b82f6; padding: 20px; margin-bottom: 30px; border-radius: 8px;">
                                <p style="margin: 0 0 10px 0; color: #1e40af; font-size: 14px; font-weight: 700;">Need Assistance?</p>
                                <p style="margin: 0; color: #1e40af; font-size: 13px; line-height: 1.6;">
                                    If you have questions about this rejection, please contact your supervisor or the ICTS Help Desk for clarification.
                                </p>
                            </div>

                            <!-- Closing -->
                            <p style="margin: 0 0 10px 0; color: #4b5563; font-size: 15px; line-height: 1.6;">
                                We appreciate your understanding.
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
sql_script = f"""-- Update SIM_REQUEST_REJECTED template
SET NOCOUNT ON;
GO

PRINT 'Updating SIM_REQUEST_REJECTED...';

UPDATE EmailTemplates
SET HtmlBody = '{html_escaped}',
    ModifiedDate = GETDATE()
WHERE TemplateCode = 'SIM_REQUEST_REJECTED';

IF @@ROWCOUNT > 0
    PRINT 'SIM_REQUEST_REJECTED updated successfully.';
ELSE
    PRINT 'WARNING: SIM_REQUEST_REJECTED not found or not updated.';
GO

SELECT TemplateCode, Name, ModifiedDate, LEN(HtmlBody) as 'Size',
CASE WHEN HtmlBody LIKE '%cid:logo%' THEN 'Yes' ELSE 'No' END AS 'Has Logo',
CASE WHEN HtmlBody LIKE '%linear-gradient%' THEN 'Gradient' ELSE 'Clean' END AS 'Status'
FROM EmailTemplates WHERE TemplateCode = 'SIM_REQUEST_REJECTED';
GO
"""

# Write to file
with open('UPDATE_SIM_REQUEST_REJECTED.sql', 'w', encoding='utf-8') as f:
    f.write(sql_script)

print("SQL script for SIM_REQUEST_REJECTED created successfully")
