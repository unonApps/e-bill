using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TAB.Web.Data;
using TAB.Web.Models;
using System.Text.Json;

namespace TAB.Web.Pages.Admin
{
    [Authorize]
    public class CallLogsUploadModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CallLogsUploadModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [TempData]
        public string? StatusMessage { get; set; }

        [TempData]
        public string? StatusMessageClass { get; set; }

        public string? LastUsedDateFormat { get; set; }
        public string? LastUsedCallLogType { get; set; }

        public async Task OnGetAsync()
        {
            // Load last used preferences for this user
            var lastImport = await _context.ImportAudits
                .Where(a => a.ImportedBy == User.Identity!.Name && a.ImportType == "CallLogs")
                .OrderByDescending(a => a.ImportDate)
                .FirstOrDefaultAsync();

            if (lastImport != null && !string.IsNullOrEmpty(lastImport.DateFormatPreferences))
            {
                try
                {
                    var preferences = JsonSerializer.Deserialize<Dictionary<string, string>>(lastImport.DateFormatPreferences);
                    if (preferences != null)
                    {
                        LastUsedDateFormat = preferences.ContainsKey("dateFormat") ? preferences["dateFormat"] : null;
                        LastUsedCallLogType = preferences.ContainsKey("callLogType") ? preferences["callLogType"] : null;
                    }
                }
                catch
                {
                    // Ignore JSON parsing errors
                }
            }
        }

        // The import handler will redirect back to CallLogs page with the handler
        public async Task<IActionResult> OnPostAsync(IFormFile callLogFile, string callLogType = "regular", bool updateExisting = false, bool skipUnmatched = true, int? billingMonth = null, int? billingYear = null, string? dateFormat = null)
        {
            // Redirect to CallLogs page with the import handler
            return RedirectToPage("/Admin/CallLogs", "ImportCallLogs", new
            {
                callLogFile = callLogFile,
                callLogType = callLogType,
                updateExisting = updateExisting,
                skipUnmatched = skipUnmatched,
                billingMonth = billingMonth,
                billingYear = billingYear,
                dateFormat = dateFormat
            });
        }
    }
}
