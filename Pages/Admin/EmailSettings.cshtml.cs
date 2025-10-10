using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using TAB.Web.Models;
using TAB.Web.Services;

namespace TAB.Web.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class EmailSettingsModel : PageModel
    {
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;
        private readonly IOptions<EmailSettings> _emailSettings;
        private readonly ILogger<EmailSettingsModel> _logger;

        public EmailSettingsModel(
            IConfiguration configuration,
            IEmailService emailService,
            IOptions<EmailSettings> emailSettings,
            ILogger<EmailSettingsModel> logger)
        {
            _configuration = configuration;
            _emailService = emailService;
            _emailSettings = emailSettings;
            _logger = logger;
        }

        [BindProperty]
        public EmailSettings Settings { get; set; } = new();

        [TempData]
        public string? StatusMessage { get; set; }

        [TempData]
        public string? StatusMessageClass { get; set; }

        [BindProperty]
        public string TestEmailAddress { get; set; } = string.Empty;

        public void OnGet()
        {
            try
            {
                // First try to load settings from TempData (our temporary storage)
                if (TempData.ContainsKey("SmtpServer"))
                {
                    Settings = new EmailSettings
                    {
                        SmtpServer = TempData["SmtpServer"]?.ToString() ?? string.Empty,
                        SmtpPort = TempData.ContainsKey("SmtpPort") ? Convert.ToInt32(TempData["SmtpPort"]) : 587,
                        FromEmail = TempData["FromEmail"]?.ToString() ?? string.Empty,
                        FromName = TempData["FromName"]?.ToString() ?? string.Empty,
                        Username = TempData["Username"]?.ToString() ?? string.Empty,
                        Password = TempData["Password"]?.ToString() ?? string.Empty,
                        EnableSsl = TempData.ContainsKey("EnableSsl") ? Convert.ToBoolean(TempData["EnableSsl"]) : true
                    };
                    
                    // Keep values for next request
                    TempData.Keep();
                }
                else
                {
                    // Otherwise load from configuration
                    Settings = _emailSettings.Value ?? new EmailSettings();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading email settings");
                Settings = new EmailSettings();
                StatusMessage = "Error loading email settings. Using default values.";
                StatusMessageClass = "warning";
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                // Log ModelState errors to help debugging
                foreach (var key in ModelState.Keys)
                {
                    var state = ModelState[key];
                    if (state == null) continue;
                    if (state.Errors.Count > 0)
                    {
                        _logger.LogError("Validation error for field {Field}: {Error}", 
                            key, string.Join(", ", state.Errors.Select(e => e.ErrorMessage)));
                    }
                }
                
                StatusMessage = "Error: Please check the form fields.";
                StatusMessageClass = "danger";
                return Page();
            }

            try
            {
                // In a production application, you would save these settings to a database
                // or securely update the appsettings.json file
                
                // For now, we're using the Options pattern to update the settings in memory
                _logger.LogInformation("Email settings updated: Server={Server}, Port={Port}, FromEmail={FromEmail}",
                    Settings.SmtpServer, Settings.SmtpPort, Settings.FromEmail);
                
                // Store these settings in TempData so they're available across requests
                TempData["SmtpServer"] = Settings.SmtpServer;
                TempData["SmtpPort"] = Settings.SmtpPort;
                TempData["FromEmail"] = Settings.FromEmail;
                TempData["FromName"] = Settings.FromName;
                TempData["Username"] = Settings.Username;
                TempData["Password"] = Settings.Password;
                TempData["EnableSsl"] = Settings.EnableSsl;
                
                StatusMessage = "Email settings saved successfully. You can now send emails.";
                StatusMessageClass = "success";
                
                await Task.CompletedTask;
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating email settings");
                StatusMessage = $"Error: {ex.Message}";
                StatusMessageClass = "danger";
                return Page();
            }
        }

        public async Task<IActionResult> OnPostSendTestEmailAsync()
        {
            if (string.IsNullOrEmpty(TestEmailAddress))
            {
                StatusMessage = "Error: Please enter a test email address.";
                StatusMessageClass = "danger";
                return RedirectToPage();
            }
            
            try
            {
                // Validate email format
                if (!System.Text.RegularExpressions.Regex.IsMatch(TestEmailAddress, 
                    @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                {
                    StatusMessage = "Error: Please enter a valid email address format.";
                    StatusMessageClass = "danger";
                    return RedirectToPage();
                }
                
                // Create a custom EmailService with the settings from TempData
                EmailSettings? customSettings = null;
                
                // Check if we have settings in TempData
                if (TempData.ContainsKey("SmtpServer"))
                {
                    customSettings = new EmailSettings
                    {
                        SmtpServer = TempData["SmtpServer"]?.ToString() ?? string.Empty,
                        SmtpPort = TempData.ContainsKey("SmtpPort") ? Convert.ToInt32(TempData["SmtpPort"]) : 587,
                        FromEmail = TempData["FromEmail"]?.ToString() ?? string.Empty,
                        FromName = TempData["FromName"]?.ToString() ?? string.Empty,
                        Username = TempData["Username"]?.ToString() ?? string.Empty,
                        Password = TempData["Password"]?.ToString() ?? string.Empty,
                        EnableSsl = TempData.ContainsKey("EnableSsl") ? Convert.ToBoolean(TempData["EnableSsl"]) : true
                    };
                    
                    // Keep the values for the next request
                    TempData.Keep();
                }
                
                string subject = "Test Email";
                string htmlMessage = @"
                <html>
                <body>
                    <h1>Test Email</h1>
                    <p>This is a test email from your application.</p>
                    <p>If you received this email, your email settings are working correctly.</p>
                </body>
                </html>";
                
                string plainTextMessage = @"
Test Email

This is a test email from your application.
If you received this email, your email settings are working correctly.";

                // If we have custom settings, use them for this test
                if (customSettings != null)
                {
                    try
                    {
                        // Create a new options wrapper with our custom settings
                        var options = Microsoft.Extensions.Options.Options.Create(customSettings);
                        
                        // Get the required services from DI
                        var environment = HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>();
                        var emailServiceLogger = HttpContext.RequestServices.GetRequiredService<ILogger<EmailService>>();
                        
                        var customEmailService = new EmailService(options, environment, emailServiceLogger);
                        
                        await customEmailService.SendEmailAsync(
                            TestEmailAddress, 
                            subject,
                            htmlMessage,
                            plainTextMessage);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error creating or using custom email service");
                        throw;
                    }
                }
                else
                {
                    // Use the default email service
                    await _emailService.SendEmailAsync(
                        TestEmailAddress, 
                        subject,
                        htmlMessage,
                        plainTextMessage);
                }
                
                StatusMessage = $"Test email sent successfully to {TestEmailAddress}.";
                StatusMessageClass = "success";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending test email");
                
                // Get inner exception details for more helpful error message
                var message = ex.Message;
                var innerException = ex.InnerException;
                while (innerException != null)
                {
                    message += $" > {innerException.Message}";
                    innerException = innerException.InnerException;
                }
                
                StatusMessage = $"Error sending test email: {message}";
                StatusMessageClass = "danger";
            }
            
            return RedirectToPage();
        }
    }
} 