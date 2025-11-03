using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TAB.Web.Data;
using TAB.Web.Models;

namespace TAB.Web.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class EmailTemplatePreviewModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<EmailTemplatePreviewModel> _logger;

        public EmailTemplatePreviewModel(
            ApplicationDbContext context,
            ILogger<EmailTemplatePreviewModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public EmailTemplate? Template { get; set; }
        public string? PreviewHtml { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            Template = await _context.EmailTemplates
                .FirstOrDefaultAsync(t => t.Id == id);

            if (Template == null)
            {
                TempData["StatusMessage"] = "Template not found.";
                TempData["StatusMessageClass"] = "danger";
                return RedirectToPage("/Admin/EmailTemplates");
            }

            // Replace cid: references with actual paths for preview
            PreviewHtml = Template.HtmlBody;

            // Replace cid:logo with actual image path for browser preview
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            PreviewHtml = PreviewHtml.Replace("cid:logo", $"{baseUrl}/images/ebilling-login.jpg");

            return Page();
        }
    }
}
