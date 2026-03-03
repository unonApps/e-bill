using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using TAB.Web.Data;
using TAB.Web.Models;

namespace TAB.Web.Pages.Admin
{
    [Authorize(Roles = "Admin,Agency Focal Point")]
    public class RecoverySettingsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<RecoverySettingsModel> _logger;

        public RecoverySettingsModel(
            ApplicationDbContext context,
            ILogger<RecoverySettingsModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        [TempData]
        public string? SuccessMessage { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        [BindProperty]
        public RecoverySettingsViewModel Settings { get; set; } = new();

        public List<Organization> Organizations { get; set; } = new();

        public async Task OnGetAsync()
        {
            // Load existing configuration or create defaults
            var config = await _context.RecoveryConfigurations
                .FirstOrDefaultAsync(rc => rc.RuleName == "SystemConfiguration");

            if (config != null)
            {
                Settings.JobIntervalMinutes = config.JobIntervalMinutes ?? 60;
                Settings.ReminderDaysBefore = config.ReminderDaysBefore ?? 2;
                Settings.AutomationEnabled = config.AutomationEnabled;
                Settings.NotificationEnabled = config.NotificationEnabled;
                Settings.DefaultApprovalDays = config.DefaultApprovalDays ?? 5;
                Settings.DefaultRevertDays = config.DefaultRevertDays ?? 3;
                Settings.EnableEmailNotifications = config.EnableEmailNotifications;
                Settings.AdminNotificationEmail = config.AdminNotificationEmail ?? "";
            }
            else
            {
                // Set defaults
                Settings.JobIntervalMinutes = 60;
                Settings.ReminderDaysBefore = 2;
                Settings.AutomationEnabled = true;
                Settings.NotificationEnabled = true;
                Settings.DefaultApprovalDays = 5;
                Settings.DefaultRevertDays = 3;
                Settings.EnableEmailNotifications = false;
                Settings.AdminNotificationEmail = "";
            }

            await LoadOrganizationsAsync();
        }

        public async Task<IActionResult> OnPostSaveSettingsAsync()
        {
            await LoadOrganizationsAsync();

            if (!ModelState.IsValid)
            {
                ErrorMessage = "Please correct the validation errors.";
                return Page();
            }

            try
            {
                // Validate settings
                if (Settings.JobIntervalMinutes < 10)
                {
                    ErrorMessage = "Job interval must be at least 10 minutes.";
                    return Page();
                }

                if (Settings.JobIntervalMinutes > 1440)
                {
                    ErrorMessage = "Job interval cannot exceed 24 hours (1440 minutes).";
                    return Page();
                }

                if (Settings.ReminderDaysBefore < 0 || Settings.ReminderDaysBefore > 30)
                {
                    ErrorMessage = "Reminder days must be between 0 and 30.";
                    return Page();
                }

                if (Settings.DefaultApprovalDays < 1 || Settings.DefaultApprovalDays > 90)
                {
                    ErrorMessage = "Approval days must be between 1 and 90.";
                    return Page();
                }

                if (Settings.DefaultRevertDays < 1 || Settings.DefaultRevertDays > 90)
                {
                    ErrorMessage = "Re-verification days must be between 1 and 90.";
                    return Page();
                }

                // Validate email if email notifications are enabled
                if (Settings.EnableEmailNotifications)
                {
                    if (string.IsNullOrWhiteSpace(Settings.AdminNotificationEmail))
                    {
                        ModelState.AddModelError("Settings.AdminNotificationEmail", "Admin notification email is required when email notifications are enabled.");
                        ErrorMessage = "Admin notification email is required when email notifications are enabled.";
                        return Page();
                    }

                    // Validate email format
                    if (!IsValidEmail(Settings.AdminNotificationEmail))
                    {
                        ModelState.AddModelError("Settings.AdminNotificationEmail", "Please enter a valid email address.");
                        ErrorMessage = "Please enter a valid email address for admin notifications.";
                        return Page();
                    }
                }

                // Get or create configuration
                var config = await _context.RecoveryConfigurations
                    .FirstOrDefaultAsync(rc => rc.RuleName == "SystemConfiguration");

                if (config == null)
                {
                    config = new RecoveryConfiguration
                    {
                        RuleName = "SystemConfiguration",
                        RuleType = "System",
                        IsEnabled = true,
                        CreatedDate = DateTime.UtcNow,
                        CreatedBy = User.Identity?.Name ?? "System"
                    };
                    _context.RecoveryConfigurations.Add(config);
                }

                // Update configuration
                config.JobIntervalMinutes = Settings.JobIntervalMinutes;
                config.ReminderDaysBefore = Settings.ReminderDaysBefore;
                config.AutomationEnabled = Settings.AutomationEnabled;
                config.NotificationEnabled = Settings.NotificationEnabled;
                config.DefaultApprovalDays = Settings.DefaultApprovalDays;
                config.DefaultRevertDays = Settings.DefaultRevertDays;
                config.EnableEmailNotifications = Settings.EnableEmailNotifications;
                config.AdminNotificationEmail = Settings.AdminNotificationEmail;
                config.LastModifiedDate = DateTime.UtcNow;
                config.LastModifiedBy = User.Identity?.Name ?? "System";

                await _context.SaveChangesAsync();

                _logger.LogInformation("Recovery configuration updated by {User}", User.Identity?.Name);

                SuccessMessage = $"Settings saved successfully! Note: Job interval changes will take effect after the next scheduled run. Current interval: {Settings.JobIntervalMinutes} minutes.";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving recovery configuration");
                ErrorMessage = $"Error saving settings: {ex.Message}";
                return Page();
            }
        }

        public async Task<IActionResult> OnPostResetToDefaultsAsync()
        {
            try
            {
                var config = await _context.RecoveryConfigurations
                    .FirstOrDefaultAsync(rc => rc.RuleName == "SystemConfiguration");

                if (config != null)
                {
                    config.JobIntervalMinutes = 60;
                    config.ReminderDaysBefore = 2;
                    config.AutomationEnabled = true;
                    config.NotificationEnabled = true;
                    config.DefaultApprovalDays = 5;
                    config.DefaultRevertDays = 3;
                    config.EnableEmailNotifications = false;
                    config.AdminNotificationEmail = null;
                    config.LastModifiedDate = DateTime.UtcNow;
                    config.LastModifiedBy = User.Identity?.Name ?? "System";

                    await _context.SaveChangesAsync();
                }

                _logger.LogInformation("Recovery configuration reset to defaults by {User}", User.Identity?.Name);

                SuccessMessage = "Settings reset to defaults successfully!";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting recovery configuration");
                ErrorMessage = $"Error resetting settings: {ex.Message}";
                return RedirectToPage();
            }
        }

        public async Task<IActionResult> OnPostToggleCOSSettingAsync(int orgId, bool enabled)
        {
            var org = await _context.Organizations.FindAsync(orgId);
            if (org == null) return NotFound();

            org.SkipVerificationWithinCOS = enabled;
            await _context.SaveChangesAsync();

            _logger.LogInformation("COS auto-verification {Status} for organization {OrgId} ({OrgName}) by {User}",
                enabled ? "enabled" : "disabled", orgId, org.Name, User.Identity?.Name);

            return new JsonResult(new { success = true });
        }

        private async Task LoadOrganizationsAsync()
        {
            Organizations = await _context.Organizations
                .OrderBy(o => o.Code).ThenBy(o => o.Name)
                .ToListAsync();
        }

        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
    }

    public class RecoverySettingsViewModel
    {
        // Job Execution Settings
        [Required(ErrorMessage = "Job interval is required")]
        [Range(10, 1440, ErrorMessage = "Job interval must be between 10 minutes and 1440 minutes (24 hours)")]
        public int JobIntervalMinutes { get; set; } = 60;

        // Notification Settings
        [Required(ErrorMessage = "Reminder days before deadline is required")]
        [Range(0, 30, ErrorMessage = "Reminder days must be between 0 and 30")]
        public int ReminderDaysBefore { get; set; } = 2;

        public bool NotificationEnabled { get; set; } = true;
        public bool EnableEmailNotifications { get; set; }

        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        public string AdminNotificationEmail { get; set; } = "";

        // Automation Settings
        public bool AutomationEnabled { get; set; } = true;

        // Default Deadline Settings
        [Required(ErrorMessage = "Supervisor approval days is required")]
        [Range(1, 90, ErrorMessage = "Approval days must be between 1 and 90")]
        public int DefaultApprovalDays { get; set; } = 5;

        [Required(ErrorMessage = "Re-verification days is required")]
        [Range(1, 90, ErrorMessage = "Re-verification days must be between 1 and 90")]
        public int DefaultRevertDays { get; set; } = 3;
    }
}
