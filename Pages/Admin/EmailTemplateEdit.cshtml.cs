using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TAB.Web.Models;
using TAB.Web.Services;

namespace TAB.Web.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class EmailTemplateEditModel : PageModel
    {
        private readonly IEmailTemplateService _templateService;
        private readonly ILogger<EmailTemplateEditModel> _logger;

        public EmailTemplateEditModel(
            IEmailTemplateService templateService,
            ILogger<EmailTemplateEditModel> logger)
        {
            _templateService = templateService;
            _logger = logger;
        }

        [BindProperty]
        public EmailTemplate Template { get; set; } = new();

        [TempData]
        public string? StatusMessage { get; set; }

        [TempData]
        public string? StatusMessageClass { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id.HasValue && id.Value > 0)
            {
                var template = await _templateService.GetTemplateByCodeAsync(string.Empty);

                // Load from database directly for editing
                template = await _templateService.GetActiveTemplatesAsync()
                    .ContinueWith(t => t.Result.FirstOrDefault(x => x.Id == id.Value));

                if (template == null)
                {
                    StatusMessage = "Template not found.";
                    StatusMessageClass = "danger";
                    return RedirectToPage("/Admin/EmailTemplates");
                }

                Template = template;
            }
            else
            {
                // Initialize new template with defaults
                Template = new EmailTemplate
                {
                    IsActive = true,
                    Category = "User Management"
                };
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                StatusMessage = "Please correct the errors and try again.";
                StatusMessageClass = "danger";
                return Page();
            }

            try
            {
                Template.ModifiedBy = User.Identity?.Name;

                if (Template.Id == 0)
                {
                    // Create new template
                    await _templateService.CreateTemplateAsync(Template);
                    StatusMessage = "Email template created successfully.";
                    StatusMessageClass = "success";
                    _logger.LogInformation("Template {TemplateCode} created by {User}", Template.TemplateCode, User.Identity?.Name);
                }
                else
                {
                    // Update existing template
                    await _templateService.UpdateTemplateAsync(Template);
                    StatusMessage = "Email template updated successfully.";
                    StatusMessageClass = "success";
                    _logger.LogInformation("Template {TemplateCode} updated by {User}", Template.TemplateCode, User.Identity?.Name);
                }

                return RedirectToPage("/Admin/EmailTemplates");
            }
            catch (InvalidOperationException ex)
            {
                StatusMessage = ex.Message;
                StatusMessageClass = "warning";
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving template");
                StatusMessage = $"Error: {ex.Message}";
                StatusMessageClass = "danger";
                return Page();
            }
        }
    }
}
