-- Update EBILL_USER_ACCOUNT_CREATED template with inline logo
SET NOCOUNT ON;
GO

UPDATE EmailTemplates
SET
    HtmlBody = '<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Welcome to UNON E-Billing System</title>
</head>
<body style="margin: 0; padding: 0; font-family: ''Segoe UI'', Tahoma, Geneva, Verdana, sans-serif; background-color: #f3f4f6;">
    <table width="100%" cellpadding="0" cellspacing="0" style="background-color: #f3f4f6; padding: 40px 20px;">
        <tr>
            <td align="center">
                <table width="600" cellpadding="0" cellspacing="0" style="background-color: #ffffff; border-radius: 16px; overflow: hidden; box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);">

                    <!-- Header with Logo -->
                    <tr>
                        <td style="background: linear-gradient(135deg, #009edb 0%, #0077b6 100%); padding: 30px; text-align: center;">
                            <img src="cid:logo"
                                 alt="UNON E-Billing System"
                                 style="max-width: 100%; height: auto; display: block; margin: 0 auto;"
                                 width="550" />
                        </td>
                    </tr>

                    <!-- Content -->
                    <tr>
                        <td style="padding: 40px 30px;">

                            <!-- Greeting -->
                            <p style="margin: 0 0 20px 0; color: #1f2937; font-size: 16px; line-height: 1.6;">
                                Dear <strong>{{FirstName}} {{LastName}}</strong>,
                            </p>

                            <p style="margin: 0 0 30px 0; color: #4b5563; font-size: 15px; line-height: 1.6;">
                                Your login account for the UNON E-Billing System has been successfully created! You can now access the system to manage your call logs, view your phone allocations, and submit verification requests.
                            </p>

                            <!-- Account Details Section -->
                            <table width="100%" cellpadding="0" cellspacing="0" style="margin-bottom: 30px;">
                                <tr>
                                    <td colspan="2" style="background-color: #f9fafb; padding: 12px 20px; border-radius: 8px 8px 0 0; border-bottom: 2px solid #10b981;">
                                        <h2 style="margin: 0; color: #1f2937; font-size: 18px; font-weight: 700;">
                                            🔑 Your Login Credentials
                                        </h2>
                                    </td>
                                </tr>
                                <tr>
                                    <td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb; width: 40%;">
                                        <strong style="color: #6b7280; font-size: 14px;">Email / Username:</strong>
                                    </td>
                                    <td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb;">
                                        <span style="color: #1f2937; font-size: 14px; font-weight: 600;">{{Email}}</span>
                                    </td>
                                </tr>
                                <tr>
                                    <td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb;">
                                        <strong style="color: #6b7280; font-size: 14px;">Temporary Password:</strong>
                                    </td>
                                    <td style="padding: 15px 20px; border-bottom: 1px solid #e5e7eb;">
                                        <code style="background-color: #fef3c7; color: #92400e; padding: 8px 12px; border-radius: 4px; font-family: ''Courier New'', monospace; font-size: 14px; font-weight: 600; display: inline-block;">{{TempPassword}}</code>
                                    </td>
                                </tr>
                                <tr>
                                    <td style="padding: 15px 20px;">
                                        <strong style="color: #6b7280; font-size: 14px;">Index Number:</strong>
                                    </td>
                                    <td style="padding: 15px 20px;">
                                        <span style="background-color: #dbeafe; color: #1e40af; padding: 4px 12px; border-radius: 12px; font-size: 13px; font-weight: 600;">{{IndexNumber}}</span>
                                    </td>
                                </tr>
                            </table>

                            <!-- Important Security Notice -->
                            <div style="background-color: #fef2f2; border-left: 4px solid #dc2626; padding: 20px; margin-bottom: 30px; border-radius: 8px;">
                                <p style="margin: 0 0 10px 0; color: #991b1b; font-size: 15px; font-weight: 700;">
                                    🔒 Important Security Notice
                                </p>
                                <p style="margin: 0; color: #7f1d1d; font-size: 14px; line-height: 1.6;">
                                    For security reasons, you <strong>must change your password</strong> when you first log in. The temporary password above is only for initial access.
                                </p>
                            </div>

                            <!-- Getting Started Steps -->
                            <div style="background-color: #ecfdf5; border-radius: 8px; padding: 20px; margin-bottom: 30px;">
                                <h3 style="margin: 0 0 15px 0; color: #065f46; font-size: 16px; font-weight: 700;">
                                    📝 Getting Started - Follow These Steps:
                                </h3>
                                <ol style="margin: 0; padding-left: 20px; color: #065f46; font-size: 14px; line-height: 1.8;">
                                    <li style="margin-bottom: 10px;">
                                        <strong>Visit the login page:</strong> Click the button below or navigate to the E-Billing System
                                    </li>
                                    <li style="margin-bottom: 10px;">
                                        <strong>Enter your credentials:</strong> Use your email and the temporary password provided above
                                    </li>
                                    <li style="margin-bottom: 10px;">
                                        <strong>Change your password:</strong> You will be prompted to create a new, secure password
                                    </li>
                                    <li>
                                        <strong>Explore the system:</strong> Access your call logs, verify charges, and manage requests
                                    </li>
                                </ol>
                            </div>

                            <!-- Password Requirements -->
                            <div style="background-color: #fef3c7; border-radius: 8px; padding: 20px; margin-bottom: 30px;">
                                <h3 style="margin: 0 0 15px 0; color: #78350f; font-size: 16px; font-weight: 700;">
                                    🔐 Password Requirements
                                </h3>
                                <p style="margin: 0 0 10px 0; color: #78350f; font-size: 14px;">
                                    When creating your new password, ensure it meets these requirements:
                                </p>
                                <ul style="margin: 0; padding-left: 20px; color: #78350f; font-size: 13px; line-height: 1.8;">
                                    <li>At least 8 characters long</li>
                                    <li>Contains at least one uppercase letter (A-Z)</li>
                                    <li>Contains at least one lowercase letter (a-z)</li>
                                    <li>Contains at least one number (0-9)</li>
                                    <li>Contains at least one special character (@, #, $, %, etc.)</li>
                                </ul>
                            </div>

                            <!-- System Features -->
                            <div style="border: 1px solid #e5e7eb; border-radius: 8px; padding: 20px; margin-bottom: 30px;">
                                <h3 style="margin: 0 0 15px 0; color: #1f2937; font-size: 16px; font-weight: 700;">
                                    ✨ What You Can Do in E-Billing:
                                </h3>
                                <ul style="margin: 0; padding-left: 20px; color: #4b5563; font-size: 14px; line-height: 1.8;">
                                    <li style="margin-bottom: 8px;">View and verify your monthly call logs</li>
                                    <li style="margin-bottom: 8px;">Check your assigned phone numbers and devices</li>
                                    <li style="margin-bottom: 8px;">Submit call log verifications to your supervisor</li>
                                    <li style="margin-bottom: 8px;">Request SIM cards and manage phone allocations</li>
                                    <li style="margin-bottom: 8px;">Submit device refund requests</li>
                                    <li>Track the status of your requests and approvals</li>
                                </ul>
                            </div>

                            <!-- Support Contact -->
                            <div style="background-color: #f9fafb; border-radius: 8px; padding: 20px; margin-bottom: 30px;">
                                <h3 style="margin: 0 0 10px 0; color: #1f2937; font-size: 16px; font-weight: 700;">
                                    📞 Need Help?
                                </h3>
                                <p style="margin: 0 0 10px 0; color: #4b5563; font-size: 14px; line-height: 1.6;">
                                    If you have any questions or need assistance, please contact:
                                </p>
                                <p style="margin: 0; color: #1f2937; font-size: 14px; line-height: 1.6;">
                                    <strong>UNON ICTS Service Desk</strong><br>
                                    <strong>Email:</strong> <a href="mailto:ICTS.Servicedesk@un.org" style="color: #009edb; text-decoration: none;">ICTS.Servicedesk@un.org</a><br>
                                    <strong>Phone:</strong> +254 20 76 21111<br>
                                    <strong>Hours:</strong> Monday - Friday, 8:00 AM - 6:00 PM
                                </p>
                            </div>

                            <!-- Call to Action Button -->
                            <div style="text-align: center; margin: 30px 0;">
                                <a href="{{LoginUrl}}" style="display: inline-block; background: linear-gradient(135deg, #10b981 0%, #059669 100%); color: #ffffff; text-decoration: none; padding: 16px 40px; border-radius: 8px; font-size: 16px; font-weight: 700; box-shadow: 0 4px 12px rgba(16, 185, 129, 0.3); transition: all 0.3s ease;">
                                    Login to E-Billing →
                                </a>
                            </div>

                            <!-- Closing -->
                            <p style="margin: 30px 0 0 0; color: #4b5563; font-size: 14px; line-height: 1.6;">
                                We''re here to help you manage your telecom services efficiently!<br>
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
                                Powered by UNON © 2025
                            </p>
                            <p style="margin: 15px 0 0 0; color: #9ca3af; font-size: 11px; line-height: 1.5;">
                                This is an automated message. Please do not reply to this email.<br>
                                If you did not request this account, please contact ICTS Service Desk immediately.
                            </p>
                        </td>
                    </tr>

                </table>
            </td>
        </tr>
    </table>
</body>
</html>
',
    ModifiedDate = GETUTCDATE(),
    ModifiedBy = 'System'
WHERE TemplateCode = 'EBILL_USER_ACCOUNT_CREATED';
GO

PRINT 'EBILL_USER_ACCOUNT_CREATED updated successfully with inline logo!';
GO
