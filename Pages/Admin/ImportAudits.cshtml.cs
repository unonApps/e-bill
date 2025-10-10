using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using TAB.Web.Data;
using TAB.Web.Models;

namespace TAB.Web.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class ImportAuditsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ImportAuditsModel> _logger;
        private readonly UserManager<ApplicationUser> _userManager;

        public ImportAuditsModel(ApplicationDbContext context, ILogger<ImportAuditsModel> logger, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _logger = logger;
            _userManager = userManager;
        }

        public List<ImportAudit> ImportAudits { get; set; } = new();
        
        [BindProperty(SupportsGet = true)]
        public string FilterType { get; set; } = "all";
        
        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }
        
        [BindProperty(SupportsGet = true)]
        public DateTime? StartDate { get; set; }
        
        [BindProperty(SupportsGet = true)]
        public DateTime? EndDate { get; set; }
        
        [BindProperty(SupportsGet = true)]
        public string? ResultFilter { get; set; }

        public int TotalImports { get; set; }
        public int TotalRecordsImported { get; set; }
        public int TotalErrors { get; set; }
        public DateTime? LatestImportDate { get; set; }

        public async Task OnGetAsync()
        {
            var query = _context.ImportAudits.AsQueryable();

            // Apply type filter
            if (!string.IsNullOrEmpty(FilterType) && FilterType != "all")
            {
                query = query.Where(a => a.ImportType == FilterType);
            }

            // Apply search filter
            if (!string.IsNullOrEmpty(SearchTerm))
            {
                query = query.Where(a => 
                    a.FileName.Contains(SearchTerm) || 
                    a.ImportedBy.Contains(SearchTerm) ||
                    a.ImportType.Contains(SearchTerm));
            }

            // Apply date range filter
            if (StartDate.HasValue)
            {
                query = query.Where(a => a.ImportDate >= StartDate.Value);
            }
            if (EndDate.HasValue)
            {
                var endOfDay = EndDate.Value.AddDays(1).AddSeconds(-1);
                query = query.Where(a => a.ImportDate <= endOfDay);
            }

            // Apply result filter
            if (!string.IsNullOrEmpty(ResultFilter))
            {
                switch (ResultFilter)
                {
                    case "success":
                        query = query.Where(a => a.ErrorCount == 0 && a.SuccessCount > 0);
                        break;
                    case "errors":
                        query = query.Where(a => a.ErrorCount > 0);
                        break;
                    case "partial":
                        query = query.Where(a => a.ErrorCount > 0 && a.SuccessCount > 0);
                        break;
                }
            }

            ImportAudits = await query
                .OrderByDescending(a => a.ImportDate)
                .ToListAsync();

            // Calculate statistics
            TotalImports = ImportAudits.Count;
            TotalRecordsImported = ImportAudits.Sum(a => a.SuccessCount);
            TotalErrors = ImportAudits.Sum(a => a.ErrorCount);
            LatestImportDate = ImportAudits.Any() ? ImportAudits.Max(a => a.ImportDate) : null;
        }

        public async Task<IActionResult> OnGetDownloadImportResultsAsync(int auditId, string type = "all")
        {
            var audit = await _context.ImportAudits.FindAsync(auditId);
            if (audit == null)
            {
                return NotFound();
            }

            var csv = new System.Text.StringBuilder();
            var fileName = $"{audit.ImportType}_{audit.ImportDate:yyyyMMdd_HHmmss}_{type}.csv";

            // Create CSV header based on import type
            if (audit.ImportType == "CallLogs")
            {
                csv.AppendLine("Status,Line,Phone Number,User,Date,Duration,Cost,Error Message");
            }
            else if (audit.ImportType == "EbillUsers")
            {
                csv.AppendLine("Status,Line,Index Number,Name,Email,Phone,Organization,Error Message");
            }
            else
            {
                csv.AppendLine("Status,Line,Data,Details,Message");
            }

            // Parse detailed results if available
            if (!string.IsNullOrEmpty(audit.DetailedResults))
            {
                try
                {
                    var results = JsonSerializer.Deserialize<ImportDetailedResult>(audit.DetailedResults);

                    if (type == "errors" || type == "all")
                    {
                        if (results?.Errors != null && results.Errors.Any())
                        {
                            foreach (var error in results.Errors)
                            {
                                csv.AppendLine($"Error,{error.LineNumber},{EscapeCsvField(error.OriginalData)},{EscapeCsvField(error.FieldName)},{EscapeCsvField(error.ErrorMessage)}");
                            }
                        }
                    }

                    if (type == "skipped" || type == "all")
                    {
                        if (results?.Skipped != null && results.Skipped.Any())
                        {
                            foreach (var skipped in results.Skipped)
                            {
                                csv.AppendLine($"Skipped,{skipped.LineNumber},{EscapeCsvField(skipped.OriginalData)},{EscapeCsvField(skipped.LookupValue)},{EscapeCsvField(skipped.Reason)}");
                            }
                        }
                    }

                    if (type == "all")
                    {
                        if (results?.Successes != null && results.Successes.Any())
                        {
                            foreach (var success in results.Successes)
                            {
                                csv.AppendLine($"Success,{success.LineNumber},{success.RecordId},{EscapeCsvField(success.Summary)},");
                            }
                        }

                        if (results?.Updated != null && results.Updated.Any())
                        {
                            foreach (var updated in results.Updated)
                            {
                                csv.AppendLine($"Updated,{updated.LineNumber},{updated.RecordId},{EscapeCsvField(updated.Summary)},{EscapeCsvField(updated.ChangedFields)}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error parsing detailed results for audit {AuditId}", auditId);
                    csv.AppendLine($"Error parsing detailed results: {ex.Message}");
                }
            }
            else
            {
                // If no detailed results, provide summary
                csv.AppendLine($"Summary: {audit.SuccessCount} imported, {audit.UpdatedCount} updated, {audit.SkippedCount} skipped, {audit.ErrorCount} errors");
                if (!string.IsNullOrEmpty(audit.SummaryMessage))
                {
                    csv.AppendLine($"Message: {audit.SummaryMessage}");
                }
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
            return File(bytes, "text/csv", fileName);
        }

        private string EscapeCsvField(string? field)
        {
            if (string.IsNullOrEmpty(field))
                return "";

            // If field contains comma, newline, or quote, wrap in quotes and escape internal quotes
            if (field.Contains(',') || field.Contains('\n') || field.Contains('"'))
            {
                return $"\"{field.Replace("\"", "\"\"")}\"";
            }
            return field;
        }
    }
}