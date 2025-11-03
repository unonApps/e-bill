using Microsoft.EntityFrameworkCore;
using TAB.Web.Data;
using TAB.Web.Models;

namespace TAB.Web.Services
{
    public class EmailTemplateSeeder
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<EmailTemplateSeeder> _logger;

        public EmailTemplateSeeder(
            ApplicationDbContext context,
            ILogger<EmailTemplateSeeder> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task SeedDefaultTemplatesAsync()
        {
            try
            {
                // Check if templates already exist
                var existingTemplates = await _context.EmailTemplates.CountAsync();
                if (existingTemplates > 0)
                {
                    _logger.LogInformation("Email templates already seeded. Skipping.");
                    return;
                }

                var templates = GetDefaultTemplates();

                foreach (var template in templates)
                {
                    var exists = await _context.EmailTemplates
                        .AnyAsync(t => t.TemplateCode == template.TemplateCode);

                    if (!exists)
                    {
                        _context.EmailTemplates.Add(template);
                    }
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Default email templates seeded successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding email templates");
                throw;
            }
        }

        private List<EmailTemplate> GetDefaultTemplates()
        {
            return new List<EmailTemplate>
            {
                // Welcome Email Template
                new EmailTemplate
                {
                    Name = "Welcome Email",
                    TemplateCode = "WELCOME_EMAIL",
                    Category = "User Management",
                    Description = "Welcome email sent to new users with their initial password",
                    Subject = "Welcome to TAB System - Your Account Has Been Created",
                    HtmlBody = @"
<!DOCTYPE html>
<html>
<head>
    <style>
        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }
        .container { max-width: 600px; margin: 0 auto; padding: 20px; }
        h1 { color: #2c3e50; }
        .info-box { background-color: #f8f9fa; padding: 15px; border-radius: 5px; margin: 20px 0; border-left: 4px solid #007bff; }
        .credentials { background-color: #fff3cd; padding: 15px; border-radius: 5px; margin: 20px 0; }
        .footer { margin-top: 30px; padding-top: 20px; border-top: 1px solid #dee2e6; font-size: 12px; color: #777; }
        .btn { display: inline-block; padding: 10px 20px; background-color: #007bff; color: white; text-decoration: none; border-radius: 5px; }
    </style>
</head>
<body>
    <div class='container'>
        <h1>Welcome to TAB System, {{FullName}}!</h1>
        <p>Your account has been successfully created. You can now access the TAB application using the credentials below.</p>

        <div class='credentials'>
            <h3>Your Login Credentials</h3>
            <p><strong>Email:</strong> {{Email}}</p>
            <p><strong>Temporary Password:</strong> {{InitialPassword}}</p>
        </div>

        <div class='info-box'>
            <p><strong>Important:</strong> For security reasons, you will be required to change your password upon first login.</p>
        </div>

        <p>If you have any questions or need assistance, please contact your system administrator.</p>

        <div class='footer'>
            <p>This is an automated message from the TAB System. Please do not reply to this email.</p>
        </div>
    </div>
</body>
</html>",
                    PlainTextBody = @"Welcome to TAB System, {{FullName}}!

Your account has been successfully created. You can now access the TAB application using the credentials below.

Your Login Credentials
Email: {{Email}}
Temporary Password: {{InitialPassword}}

Important: For security reasons, you will be required to change your password upon first login.

If you have any questions or need assistance, please contact your system administrator.

This is an automated message from the TAB System. Please do not reply to this email.",
                    AvailablePlaceholders = "FullName, Email, InitialPassword",
                    IsActive = true,
                    IsSystemTemplate = true,
                    CreatedDate = DateTime.UtcNow
                },

                // Password Reset Template
                new EmailTemplate
                {
                    Name = "Password Reset",
                    TemplateCode = "PASSWORD_RESET",
                    Category = "User Management",
                    Description = "Password reset email with reset link",
                    Subject = "Password Reset Request - TAB System",
                    HtmlBody = @"
<!DOCTYPE html>
<html>
<head>
    <style>
        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }
        .container { max-width: 600px; margin: 0 auto; padding: 20px; }
        h1 { color: #2c3e50; }
        .info-box { background-color: #fff3cd; padding: 15px; border-radius: 5px; margin: 20px 0; }
        .btn { display: inline-block; padding: 12px 24px; background-color: #007bff; color: white; text-decoration: none; border-radius: 5px; margin: 20px 0; }
        .footer { margin-top: 30px; padding-top: 20px; border-top: 1px solid #dee2e6; font-size: 12px; color: #777; }
    </style>
</head>
<body>
    <div class='container'>
        <h1>Password Reset Request</h1>
        <p>Hello {{FullName}},</p>
        <p>We received a request to reset your password for your TAB System account.</p>

        <div class='info-box'>
            <p><strong>If you did not request this password reset, please ignore this email.</strong></p>
        </div>

        <p>To reset your password, click the button below:</p>
        <a href='{{ResetLink}}' class='btn'>Reset Password</a>

        <p><small>This link will expire in 24 hours.</small></p>

        <div class='footer'>
            <p>This is an automated message from the TAB System. Please do not reply to this email.</p>
        </div>
    </div>
</body>
</html>",
                    PlainTextBody = @"Password Reset Request

Hello {{FullName}},

We received a request to reset your password for your TAB System account.

If you did not request this password reset, please ignore this email.

To reset your password, visit the following link:
{{ResetLink}}

This link will expire in 24 hours.

This is an automated message from the TAB System. Please do not reply to this email.",
                    AvailablePlaceholders = "FullName, Email, ResetLink",
                    IsActive = true,
                    IsSystemTemplate = true,
                    CreatedDate = DateTime.UtcNow
                },

                // Notification Template
                new EmailTemplate
                {
                    Name = "General Notification",
                    TemplateCode = "NOTIFICATION",
                    Category = "Notifications",
                    Description = "General notification template for system alerts",
                    Subject = "{{NotificationTitle}} - TAB System",
                    HtmlBody = @"
<!DOCTYPE html>
<html>
<head>
    <style>
        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }
        .container { max-width: 600px; margin: 0 auto; padding: 20px; }
        h1 { color: #2c3e50; }
        .content-box { background-color: #f8f9fa; padding: 20px; border-radius: 5px; margin: 20px 0; }
        .footer { margin-top: 30px; padding-top: 20px; border-top: 1px solid #dee2e6; font-size: 12px; color: #777; }
    </style>
</head>
<body>
    <div class='container'>
        <h1>{{NotificationTitle}}</h1>
        <p>Hello {{FullName}},</p>

        <div class='content-box'>
            {{NotificationContent}}
        </div>

        <p>Date: {{Date}}</p>

        <div class='footer'>
            <p>This is an automated notification from the TAB System.</p>
        </div>
    </div>
</body>
</html>",
                    PlainTextBody = @"{{NotificationTitle}}

Hello {{FullName}},

{{NotificationContent}}

Date: {{Date}}

This is an automated notification from the TAB System.",
                    AvailablePlaceholders = "FullName, NotificationTitle, NotificationContent, Date",
                    IsActive = true,
                    IsSystemTemplate = false,
                    CreatedDate = DateTime.UtcNow
                }
            };
        }
    }
}
