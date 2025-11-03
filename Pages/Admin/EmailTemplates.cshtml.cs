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
    public class EmailTemplatesModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailTemplateService _templateService;
        private readonly ILogger<EmailTemplatesModel> _logger;

        public EmailTemplatesModel(
            ApplicationDbContext context,
            IEmailTemplateService templateService,
            ILogger<EmailTemplatesModel> logger)
        {
            _context = context;
            _templateService = templateService;
            _logger = logger;
        }

        public List<EmailTemplate> Templates { get; set; } = new();
        public List<string> Categories { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? SelectedCategory { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SelectedStatus { get; set; }

        [TempData]
        public string? StatusMessage { get; set; }

        [TempData]
        public string? StatusMessageClass { get; set; }

        public async Task OnGetAsync()
        {
            var query = _context.EmailTemplates.AsQueryable();

            // Filter by category
            if (!string.IsNullOrEmpty(SelectedCategory))
            {
                query = query.Where(t => t.Category == SelectedCategory);
            }

            // Filter by status
            if (SelectedStatus == "active")
            {
                query = query.Where(t => t.IsActive);
            }
            else if (SelectedStatus == "inactive")
            {
                query = query.Where(t => !t.IsActive);
            }

            Templates = await query
                .OrderBy(t => t.Category)
                .ThenBy(t => t.Name)
                .ToListAsync();

            // Get all unique categories
            Categories = await _context.EmailTemplates
                .Where(t => !string.IsNullOrEmpty(t.Category))
                .Select(t => t.Category!)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            try
            {
                var success = await _templateService.DeleteTemplateAsync(id);

                if (success)
                {
                    StatusMessage = "Template deleted successfully.";
                    StatusMessageClass = "success";
                    _logger.LogInformation("Template {Id} deleted by {User}", id, User.Identity?.Name);
                }
                else
                {
                    StatusMessage = "Template not found.";
                    StatusMessageClass = "danger";
                }
            }
            catch (InvalidOperationException ex)
            {
                StatusMessage = ex.Message;
                StatusMessageClass = "warning";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting template {Id}", id);
                StatusMessage = $"Error: {ex.Message}";
                StatusMessageClass = "danger";
            }

            return RedirectToPage();
        }
    }
}
