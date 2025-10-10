using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using TAB.Web.Models;

namespace TAB.Web.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<EmailService> _logger;

        public EmailService(
            IOptions<EmailSettings> emailSettings,
            IWebHostEnvironment environment,
            ILogger<EmailService> logger)
        {
            _emailSettings = emailSettings.Value;
            _environment = environment;
            _logger = logger;
        }

        public async Task SendEmailAsync(string to, string subject, string htmlMessage, string? plainTextMessage = null)
        {
            try
            {
                // Log email attempt
                _logger.LogInformation("Attempting to send email to {To} with subject {Subject}", to, subject);
                
                // Log SMTP settings being used (not including password for security)
                _logger.LogInformation("Using SMTP settings: Server={Server}, Port={Port}, FromEmail={FromEmail}, SSL={SSL}", 
                    _emailSettings.SmtpServer, _emailSettings.SmtpPort, _emailSettings.FromEmail, _emailSettings.EnableSsl);
                
                // Validate SMTP settings before sending
                if (string.IsNullOrEmpty(_emailSettings.SmtpServer))
                {
                    throw new InvalidOperationException("SMTP Server address is not configured");
                }
                
                if (string.IsNullOrEmpty(_emailSettings.FromEmail))
                {
                    throw new InvalidOperationException("From Email address is not configured");
                }
                
                var message = new MailMessage
                {
                    From = new MailAddress(_emailSettings.FromEmail, _emailSettings.FromName ?? _emailSettings.FromEmail),
                    Subject = subject,
                    IsBodyHtml = true,
                    Body = htmlMessage
                };

                if (!string.IsNullOrEmpty(plainTextMessage))
                {
                    message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(plainTextMessage, null, "text/plain"));
                }

                message.To.Add(new MailAddress(to));

                using (var client = new SmtpClient(_emailSettings.SmtpServer, _emailSettings.SmtpPort))
                {
                    client.UseDefaultCredentials = false;
                    client.Credentials = new NetworkCredential(_emailSettings.Username ?? string.Empty, _emailSettings.Password ?? string.Empty);
                    client.EnableSsl = _emailSettings.EnableSsl;

                    await client.SendMailAsync(message);
                }
                
                _logger.LogInformation("Email sent successfully to {To}", to);
            }
            catch (Exception ex)
            {
                // Detailed error logging
                _logger.LogError(ex, "Failed to send email to {To} with subject {Subject}", to, subject);
                
                // Log more specific details about the exception
                var innerExceptionMessage = ex.InnerException != null ? ex.InnerException.Message : "No inner exception";
                _logger.LogError("Email sending error details: Message={Message}, InnerException={InnerException}", 
                    ex.Message, innerExceptionMessage);
                    
                throw;
            }
        }

        public async Task SendWelcomeEmailAsync(string to, string fullName, string initialPassword)
        {
            string subject = "Welcome to the Application";
            
            string htmlMessage = $@"
            <html>
            <head>
                <style>
                    body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                    .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                    h1 {{ color: #2c3e50; }}
                    .password {{ background-color: #f8f9fa; padding: 10px; border-radius: 4px; margin: 15px 0; }}
                    .footer {{ margin-top: 30px; font-size: 12px; color: #777; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <h1>Welcome, {WebUtility.HtmlEncode(fullName)}!</h1>
                    <p>Your account has been created successfully. You can now log in to the application using the following credentials:</p>
                    <div class='password'>
                        <p><strong>Email:</strong> {WebUtility.HtmlEncode(to)}</p>
                        <p><strong>Initial Password:</strong> {WebUtility.HtmlEncode(initialPassword)}</p>
                    </div>
                    <p>For security reasons, you will be required to change your password upon first login.</p>
                    <p>If you have any questions or need assistance, please contact your administrator.</p>
                    <div class='footer'>
                        <p>This is an automated message. Please do not reply to this email.</p>
                    </div>
                </div>
            </body>
            </html>";

            string plainTextMessage = $@"
Welcome, {fullName}!

Your account has been created successfully. You can now log in to the application using the following credentials:

Email: {to}
Initial Password: {initialPassword}

For security reasons, you will be required to change your password upon first login.

If you have any questions or need assistance, please contact your administrator.

This is an automated message. Please do not reply to this email.";

            await SendEmailAsync(to, subject, htmlMessage, plainTextMessage);
        }
    }
} 