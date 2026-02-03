using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TAB.Web.Data;
using TAB.Web.Models;
using TAB.Web.Services;

namespace TAB.Web.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class EmailConfigurationModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IEnhancedEmailService _emailService;
        private readonly ILogger<EmailConfigurationModel> _logger;

        public EmailConfigurationModel(
            ApplicationDbContext context,
            IEnhancedEmailService emailService,
            ILogger<EmailConfigurationModel> logger)
        {
            _context = context;
            _emailService = emailService;
            _logger = logger;
        }

        [BindProperty]
        public EmailConfiguration Configuration { get; set; } = new();

        [BindProperty]
        public string TestEmailAddress { get; set; } = string.Empty;

        public List<EmailConfiguration> AllConfigurations { get; set; } = new();

        [TempData]
        public string? StatusMessage { get; set; }

        [TempData]
        public string? StatusMessageClass { get; set; }

        public async Task OnGetAsync(int? id)
        {
            // Load all configurations for the modal
            AllConfigurations = await _context.EmailConfigurations
                .OrderByDescending(c => c.IsActive)
                .ThenByDescending(c => c.CreatedDate)
                .ToListAsync();

            // Load specific configuration if ID provided
            if (id.HasValue)
            {
                var config = await _context.EmailConfigurations.FindAsync(id.Value);
                if (config != null)
                {
                    Configuration = config;
                    return;
                }
            }

            // Otherwise, load the active configuration or create a new one
            var activeConfig = await _context.EmailConfigurations
                .Where(c => c.IsActive)
                .OrderByDescending(c => c.CreatedDate)
                .FirstOrDefaultAsync();

            if (activeConfig != null)
            {
                Configuration = activeConfig;
            }
            else
            {
                // Initialize with default values
                Configuration = new EmailConfiguration
                {
                    SmtpPort = 587,
                    EnableSsl = true,
                    Timeout = 30,
                    IsActive = true
                };
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Remove validation errors for fields that aren't in the form
            ModelState.Remove("Configuration.CreatedDate");
            ModelState.Remove("Configuration.ModifiedDate");
            ModelState.Remove("Configuration.ModifiedBy");
            ModelState.Remove("TestEmailAddress"); // Not needed for saving configuration

            if (!ModelState.IsValid)
            {
                // Log validation errors for debugging
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                _logger.LogWarning("Email configuration validation failed: {Errors}", string.Join(", ", errors));

                StatusMessage = $"Validation errors: {string.Join(", ", errors)}";
                StatusMessageClass = "danger";
                return Page();
            }

            try
            {
                if (Configuration.Id == 0)
                {
                    // Creating new configuration
                    Configuration.CreatedDate = DateTime.UtcNow;
                    Configuration.ModifiedBy = User.Identity?.Name;

                    // Deactivate all other configurations
                    var existingConfigs = await _context.EmailConfigurations.ToListAsync();
                    foreach (var config in existingConfigs)
                    {
                        config.IsActive = false;
                    }

                    _context.EmailConfigurations.Add(Configuration);
                }
                else
                {
                    // Updating existing configuration
                    var existing = await _context.EmailConfigurations.FindAsync(Configuration.Id);
                    if (existing == null)
                    {
                        StatusMessage = "Configuration not found.";
                        StatusMessageClass = "danger";
                        return Page();
                    }

                    existing.SmtpServer = Configuration.SmtpServer;
                    existing.SmtpPort = Configuration.SmtpPort;
                    existing.FromEmail = Configuration.FromEmail;
                    existing.FromName = Configuration.FromName;
                    existing.Username = Configuration.Username;

                    // Only update password if a new one was provided
                    if (!string.IsNullOrEmpty(Configuration.Password))
                    {
                        existing.Password = Configuration.Password;
                    }
                    // Otherwise keep the existing password

                    existing.EnableSsl = Configuration.EnableSsl;
                    existing.UseDefaultCredentials = Configuration.UseDefaultCredentials;
                    existing.Timeout = Configuration.Timeout;
                    existing.IsActive = Configuration.IsActive;
                    existing.Notes = Configuration.Notes;
                    existing.ModifiedDate = DateTime.UtcNow;
                    existing.ModifiedBy = User.Identity?.Name;

                    // If this is being activated, deactivate others
                    if (Configuration.IsActive)
                    {
                        var otherConfigs = await _context.EmailConfigurations
                            .Where(c => c.Id != Configuration.Id)
                            .ToListAsync();

                        foreach (var config in otherConfigs)
                        {
                            config.IsActive = false;
                        }
                    }
                }

                await _context.SaveChangesAsync();

                StatusMessage = "Email configuration saved successfully.";
                StatusMessageClass = "success";

                _logger.LogInformation("Email configuration updated by {User}", User.Identity?.Name);

                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving email configuration");
                StatusMessage = $"Error: {ex.Message}";
                StatusMessageClass = "danger";
                return Page();
            }
        }

        public async Task<IActionResult> OnPostTestConnectionAsync()
        {
            // Clear validation for fields not needed for testing
            ModelState.Remove("Configuration.CreatedDate");
            ModelState.Remove("Configuration.ModifiedDate");
            ModelState.Remove("Configuration.ModifiedBy");
            ModelState.Remove("TestEmailAddress"); // Not needed for connection test

            // If this is a saved configuration (has ID), load password from database
            if (Configuration.Id > 0)
            {
                var savedConfig = await _context.EmailConfigurations.FindAsync(Configuration.Id);
                if (savedConfig != null)
                {
                    _logger.LogInformation("Loading saved configuration from database for testing");
                    // Use password from database if form password is empty
                    if (string.IsNullOrEmpty(Configuration.Password))
                    {
                        Configuration.Password = savedConfig.Password;
                        _logger.LogInformation("Using saved password from database");
                    }
                }
            }

            // Log what we're using for testing
            _logger.LogInformation("Connection test request - Server: {Server}, Port: {Port}, Username: {Username}, HasPassword: {HasPassword}",
                Configuration.SmtpServer,
                Configuration.SmtpPort,
                Configuration.Username,
                !string.IsNullOrEmpty(Configuration.Password));

            // Validate that required SMTP settings are filled
            if (string.IsNullOrEmpty(Configuration.SmtpServer))
            {
                StatusMessage = "SMTP Server is required.";
                StatusMessageClass = "warning";
                await LoadConfigurationsAsync();
                return Page();
            }
            // Username and Password are optional when UseDefaultCredentials is true
            if (!Configuration.UseDefaultCredentials && string.IsNullOrEmpty(Configuration.Username))
            {
                StatusMessage = "Username is required when not using Default Credentials.";
                StatusMessageClass = "warning";
                await LoadConfigurationsAsync();
                return Page();
            }
            if (!Configuration.UseDefaultCredentials && string.IsNullOrEmpty(Configuration.Password))
            {
                StatusMessage = "Password is required when not using Default Credentials.";
                StatusMessageClass = "warning";
                await LoadConfigurationsAsync();
                return Page();
            }

            try
            {
                _logger.LogInformation("Testing SMTP connection to {Server}:{Port}",
                    Configuration.SmtpServer, Configuration.SmtpPort);

                // Test the connection without sending an email
                using var client = new System.Net.Mail.SmtpClient(Configuration.SmtpServer, Configuration.SmtpPort)
                {
                    UseDefaultCredentials = Configuration.UseDefaultCredentials,
                    EnableSsl = Configuration.EnableSsl,
                    Timeout = Configuration.Timeout * 1000 // Convert to milliseconds
                };

                // Only set credentials if not using default credentials
                if (!Configuration.UseDefaultCredentials)
                {
                    client.Credentials = new System.Net.NetworkCredential(Configuration.Username, Configuration.Password);
                }

                // Create a minimal test message to verify authentication
                using var message = new System.Net.Mail.MailMessage
                {
                    From = new System.Net.Mail.MailAddress(
                        string.IsNullOrEmpty(Configuration.FromEmail) ? Configuration.Username : Configuration.FromEmail,
                        Configuration.FromName ?? "Test"),
                    Subject = "Connection Test",
                    Body = "Connection test"
                };
                message.To.Add(Configuration.Username); // Use username as dummy recipient

                // This will attempt to connect and authenticate
                await client.SendMailAsync(message);

                StatusMessage = "✅ Connection successful! SMTP server authenticated successfully. You can now send a test email or save the configuration.";
                StatusMessageClass = "success";
                _logger.LogInformation("SMTP connection test successful for {Server}:{Port}",
                    Configuration.SmtpServer, Configuration.SmtpPort);
            }
            catch (System.Net.Mail.SmtpException ex)
            {
                var errorDetails = $"SMTP Error: {ex.Message}";
                if (ex.StatusCode != 0)
                {
                    errorDetails += $" (Status Code: {ex.StatusCode})";
                }

                _logger.LogError(ex, "SMTP connection test failed for {Server}:{Port}",
                    Configuration.SmtpServer, Configuration.SmtpPort);

                StatusMessage = $"❌ Connection failed: {errorDetails}";
                StatusMessageClass = "danger";

                // Provide specific guidance based on common errors
                if (ex.Message.Contains("5.7.0") || ex.Message.Contains("Authentication"))
                {
                    StatusMessage += " | Check: Username must be full email, Use Default Credentials must be UNCHECKED, Password must be app password.";
                }
                else if (ex.Message.Contains("timed out") || ex.Message.Contains("Unable to connect"))
                {
                    StatusMessage += " | Check: SMTP server address and port are correct, Enable SSL is checked for port 587.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during SMTP connection test");
                StatusMessage = $"❌ Unexpected error: {ex.Message}";
                StatusMessageClass = "danger";
            }

            // Load configurations for modal
            await LoadConfigurationsAsync();
            return Page(); // Return Page() instead of RedirectToPage() to preserve form values
        }

        public async Task<IActionResult> OnPostSendTestEmailAsync()
        {
            // Clear validation for fields not needed for testing
            ModelState.Remove("Configuration.CreatedDate");
            ModelState.Remove("Configuration.ModifiedDate");
            ModelState.Remove("Configuration.ModifiedBy");

            if (string.IsNullOrEmpty(TestEmailAddress))
            {
                StatusMessage = "Please enter a test email address.";
                StatusMessageClass = "danger";
                await LoadConfigurationsAsync();
                return Page();
            }

            // If this is a saved configuration (has ID), load password from database
            if (Configuration.Id > 0)
            {
                var savedConfig = await _context.EmailConfigurations.FindAsync(Configuration.Id);
                if (savedConfig != null)
                {
                    _logger.LogInformation("Loading saved configuration from database for test email");
                    // Use password from database if form password is empty
                    if (string.IsNullOrEmpty(Configuration.Password))
                    {
                        Configuration.Password = savedConfig.Password;
                        _logger.LogInformation("Using saved password from database");
                    }
                }
            }

            // Log what we're using for testing
            _logger.LogInformation("Test email request - Server: {Server}, Port: {Port}, FromEmail: {FromEmail}, Username: {Username}, HasPassword: {HasPassword}, UseDefaultCreds: {UseDefaultCreds}, EnableSsl: {EnableSsl}",
                Configuration.SmtpServer,
                Configuration.SmtpPort,
                Configuration.FromEmail,
                Configuration.Username,
                !string.IsNullOrEmpty(Configuration.Password),
                Configuration.UseDefaultCredentials,
                Configuration.EnableSsl);

            // Validate that required SMTP settings are filled
            if (string.IsNullOrEmpty(Configuration.SmtpServer))
            {
                StatusMessage = "SMTP Server is required.";
                StatusMessageClass = "warning";
                await LoadConfigurationsAsync();
                return Page();
            }
            if (string.IsNullOrEmpty(Configuration.FromEmail))
            {
                StatusMessage = "From Email is required.";
                StatusMessageClass = "warning";
                await LoadConfigurationsAsync();
                return Page();
            }
            // Username and Password are optional when UseDefaultCredentials is true
            if (!Configuration.UseDefaultCredentials && string.IsNullOrEmpty(Configuration.Username))
            {
                StatusMessage = "Username is required when not using Default Credentials.";
                StatusMessageClass = "warning";
                await LoadConfigurationsAsync();
                return Page();
            }
            if (!Configuration.UseDefaultCredentials && string.IsNullOrEmpty(Configuration.Password))
            {
                StatusMessage = "Password is required when not using Default Credentials.";
                StatusMessageClass = "warning";
                await LoadConfigurationsAsync();
                return Page();
            }

            try
            {
                // Use the settings from the form (not database) for testing
                _logger.LogInformation("Testing email configuration with Server={Server}, Port={Port}",
                    Configuration.SmtpServer, Configuration.SmtpPort);

                var subject = "Test Email from TAB System";
                var htmlMessage = @"
                    <html>
                    <head>
                        <style>
                            body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }
                            .container { max-width: 600px; margin: 0 auto; padding: 20px; }
                            h1 { color: #2c3e50; }
                            .info-box { background-color: #f8f9fa; padding: 15px; border-radius: 5px; margin: 20px 0; }
                            .footer { margin-top: 30px; font-size: 12px; color: #777; }
                        </style>
                    </head>
                    <body>
                        <div class='container'>
                            <h1>Test Email</h1>
                            <p>This is a test email from the TAB Email Management System.</p>
                            <div class='info-box'>
                                <p><strong>Email Configuration Test</strong></p>
                                <p>If you received this email, your SMTP settings are configured correctly and the system can successfully send emails.</p>
                            </div>
                            <p>Test sent at: " + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC") + @"</p>
                            <div class='footer'>
                                <p>This is an automated test email from the TAB System.</p>
                            </div>
                        </div>
                    </body>
                    </html>";

                var plainTextMessage = $@"
Test Email

This is a test email from the TAB Email Management System.

Email Configuration Test
If you received this email, your SMTP settings are configured correctly and the system can successfully send emails.

Test sent at: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC}

This is an automated test email from the TAB System.";

                // Send test email using the configuration from the form
                await SendTestEmailDirectAsync(
                    Configuration,
                    TestEmailAddress,
                    subject,
                    htmlMessage,
                    plainTextMessage);

                StatusMessage = $"Test email sent successfully to {TestEmailAddress}. Please check your inbox (and spam folder).";
                StatusMessageClass = "success";
                _logger.LogInformation("Test email sent to {Email} by {User}", TestEmailAddress, User.Identity?.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending test email to {Email}", TestEmailAddress);
                StatusMessage = $"Error sending test email: {ex.Message}";
                StatusMessageClass = "danger";
            }

            // Load configurations for modal
            await LoadConfigurationsAsync();
            return Page(); // Return Page() instead of RedirectToPage() to preserve form values
        }

        /// <summary>
        /// Helper method to load all configurations for the modal
        /// </summary>
        private async Task LoadConfigurationsAsync()
        {
            AllConfigurations = await _context.EmailConfigurations
                .OrderByDescending(c => c.IsActive)
                .ThenByDescending(c => c.CreatedDate)
                .ToListAsync();
        }

        /// <summary>
        /// Sends a test email directly using the provided configuration (without saving to database)
        /// </summary>
        private async Task SendTestEmailDirectAsync(
            EmailConfiguration config,
            string to,
            string subject,
            string htmlMessage,
            string? plainTextMessage)
        {
            using var message = new System.Net.Mail.MailMessage
            {
                From = new System.Net.Mail.MailAddress(config.FromEmail, config.FromName),
                Subject = subject,
                IsBodyHtml = true,
                Body = htmlMessage
            };

            if (!string.IsNullOrEmpty(plainTextMessage))
            {
                message.AlternateViews.Add(
                    System.Net.Mail.AlternateView.CreateAlternateViewFromString(
                        plainTextMessage, null, "text/plain"));
            }

            message.To.Add(new System.Net.Mail.MailAddress(to));

            using var client = new System.Net.Mail.SmtpClient(config.SmtpServer, config.SmtpPort)
            {
                UseDefaultCredentials = config.UseDefaultCredentials,
                EnableSsl = config.EnableSsl,
                Timeout = config.Timeout * 1000 // Convert to milliseconds
            };

            // Only set credentials if not using default credentials
            if (!config.UseDefaultCredentials && !string.IsNullOrEmpty(config.Username))
            {
                client.Credentials = new System.Net.NetworkCredential(config.Username, config.Password);
            }

            await client.SendMailAsync(message);
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            try
            {
                var config = await _context.EmailConfigurations.FindAsync(id);
                if (config == null)
                {
                    StatusMessage = "Configuration not found.";
                    StatusMessageClass = "danger";
                    return RedirectToPage();
                }

                // Prevent deleting the active configuration
                if (config.IsActive)
                {
                    StatusMessage = "Cannot delete the active configuration. Please activate a different configuration first.";
                    StatusMessageClass = "warning";
                    return RedirectToPage();
                }

                _context.EmailConfigurations.Remove(config);
                await _context.SaveChangesAsync();

                StatusMessage = $"Email configuration for {config.SmtpServer} deleted successfully.";
                StatusMessageClass = "success";
                _logger.LogInformation("Email configuration {Id} deleted by {User}", id, User.Identity?.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting email configuration {Id}", id);
                StatusMessage = $"Error deleting configuration: {ex.Message}";
                StatusMessageClass = "danger";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostActivateAsync(int id)
        {
            try
            {
                var config = await _context.EmailConfigurations.FindAsync(id);
                if (config == null)
                {
                    StatusMessage = "Configuration not found.";
                    StatusMessageClass = "danger";
                    return RedirectToPage();
                }

                // Deactivate all other configurations
                var allConfigs = await _context.EmailConfigurations.ToListAsync();
                foreach (var c in allConfigs)
                {
                    c.IsActive = false;
                }

                // Activate the selected configuration
                config.IsActive = true;
                config.ModifiedDate = DateTime.UtcNow;
                config.ModifiedBy = User.Identity?.Name;

                await _context.SaveChangesAsync();

                StatusMessage = $"Email configuration for {config.SmtpServer} activated successfully.";
                StatusMessageClass = "success";
                _logger.LogInformation("Email configuration {Id} activated by {User}", id, User.Identity?.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error activating email configuration {Id}", id);
                StatusMessage = $"Error activating configuration: {ex.Message}";
                StatusMessageClass = "danger";
            }

            return RedirectToPage();
        }
    }
}
