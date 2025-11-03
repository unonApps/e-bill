using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TAB.Web.Models;
using TAB.Web.Services;

namespace TAB.Web.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class SendEmailModel : PageModel
    {
        private readonly IEnhancedEmailService _emailService;
        private readonly IEmailTemplateService _templateService;
        private readonly ILogger<SendEmailModel> _logger;

        public SendEmailModel(
            IEnhancedEmailService emailService,
            IEmailTemplateService templateService,
            ILogger<SendEmailModel> logger)
        {
            _emailService = emailService;
            _templateService = templateService;
            _logger = logger;
        }

        [BindProperty]
        public string ToEmail { get; set; } = string.Empty;

        [BindProperty]
        public string? CcEmails { get; set; }

        [BindProperty]
        public string? BccEmails { get; set; }

        [BindProperty]
        public string Subject { get; set; } = string.Empty;

        [BindProperty]
        public string HtmlBody { get; set; } = string.Empty;

        [BindProperty]
        public string? PlainTextBody { get; set; }

        [BindProperty]
        public string? SelectedTemplateCode { get; set; }

        [BindProperty]
        public Dictionary<string, string> TemplateData { get; set; } = new();

        public List<EmailTemplate> Templates { get; set; } = new();

        [TempData]
        public string? StatusMessage { get; set; }

        [TempData]
        public string? StatusMessageClass { get; set; }

        public async Task OnGetAsync()
        {
            Templates = await _templateService.GetActiveTemplatesAsync();
        }

        public async Task<IActionResult> OnPostSendCustomAsync()
        {
            if (!ModelState.IsValid)
            {
                Templates = await _templateService.GetActiveTemplatesAsync();
                StatusMessage = "Please correct the errors and try again.";
                StatusMessageClass = "danger";
                return Page();
            }

            try
            {
                var success = await _emailService.SendEmailAsync(
                    ToEmail,
                    Subject,
                    HtmlBody,
                    PlainTextBody,
                    CcEmails,
                    BccEmails,
                    createdBy: User.Identity?.Name);

                if (success)
                {
                    StatusMessage = $"Email sent successfully to {ToEmail}.";
                    StatusMessageClass = "success";
                    _logger.LogInformation("Custom email sent to {To} by {User}", ToEmail, User.Identity?.Name);
                    return RedirectToPage("/Admin/EmailLogs");
                }
                else
                {
                    StatusMessage = "Failed to send email. Please check the logs for details.";
                    StatusMessageClass = "danger";
                    Templates = await _templateService.GetActiveTemplatesAsync();
                    return Page();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending custom email to {To}", ToEmail);
                StatusMessage = $"Error: {ex.Message}";
                StatusMessageClass = "danger";
                Templates = await _templateService.GetActiveTemplatesAsync();
                return Page();
            }
        }

        public async Task<IActionResult> OnPostQueueCustomAsync()
        {
            if (!ModelState.IsValid)
            {
                Templates = await _templateService.GetActiveTemplatesAsync();
                StatusMessage = "Please correct the errors and try again.";
                StatusMessageClass = "danger";
                return Page();
            }

            try
            {
                var emailId = await _emailService.QueueEmailAsync(
                    ToEmail,
                    Subject,
                    HtmlBody,
                    PlainTextBody,
                    CcEmails,
                    BccEmails,
                    createdBy: User.Identity?.Name);

                StatusMessage = $"Email queued successfully (ID: {emailId}). It will be sent shortly.";
                StatusMessageClass = "success";
                _logger.LogInformation("Custom email queued for {To} by {User}", ToEmail, User.Identity?.Name);
                return RedirectToPage("/Admin/EmailLogs");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error queuing custom email for {To}", ToEmail);
                StatusMessage = $"Error: {ex.Message}";
                StatusMessageClass = "danger";
                Templates = await _templateService.GetActiveTemplatesAsync();
                return Page();
            }
        }

        public async Task<IActionResult> OnPostSendTemplateAsync()
        {
            if (string.IsNullOrEmpty(SelectedTemplateCode))
            {
                Templates = await _templateService.GetActiveTemplatesAsync();
                StatusMessage = "Please select a template.";
                StatusMessageClass = "danger";
                return Page();
            }

            try
            {
                var success = await _emailService.SendTemplatedEmailAsync(
                    ToEmail,
                    SelectedTemplateCode,
                    TemplateData,
                    CcEmails,
                    BccEmails,
                    createdBy: User.Identity?.Name);

                if (success)
                {
                    StatusMessage = $"Templated email sent successfully to {ToEmail}.";
                    StatusMessageClass = "success";
                    _logger.LogInformation("Templated email ({Template}) sent to {To} by {User}",
                        SelectedTemplateCode, ToEmail, User.Identity?.Name);
                    return RedirectToPage("/Admin/EmailLogs");
                }
                else
                {
                    StatusMessage = "Failed to send email. Please check the logs for details.";
                    StatusMessageClass = "danger";
                    Templates = await _templateService.GetActiveTemplatesAsync();
                    return Page();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending templated email to {To}", ToEmail);
                StatusMessage = $"Error: {ex.Message}";
                StatusMessageClass = "danger";
                Templates = await _templateService.GetActiveTemplatesAsync();
                return Page();
            }
        }

        public async Task<IActionResult> OnPostQueueTemplateAsync()
        {
            if (string.IsNullOrEmpty(SelectedTemplateCode))
            {
                Templates = await _templateService.GetActiveTemplatesAsync();
                StatusMessage = "Please select a template.";
                StatusMessageClass = "danger";
                return Page();
            }

            try
            {
                var emailId = await _emailService.QueueTemplatedEmailAsync(
                    ToEmail,
                    SelectedTemplateCode,
                    TemplateData,
                    CcEmails,
                    BccEmails,
                    createdBy: User.Identity?.Name);

                StatusMessage = $"Templated email queued successfully (ID: {emailId}). It will be sent shortly.";
                StatusMessageClass = "success";
                _logger.LogInformation("Templated email ({Template}) queued for {To} by {User}",
                    SelectedTemplateCode, ToEmail, User.Identity?.Name);
                return RedirectToPage("/Admin/EmailLogs");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error queuing templated email for {To}", ToEmail);
                StatusMessage = $"Error: {ex.Message}";
                StatusMessageClass = "danger";
                Templates = await _templateService.GetActiveTemplatesAsync();
                return Page();
            }
        }
    }
}
