using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TAB.Web.Data;
using TAB.Web.Models;

namespace TAB.Web.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class AuditLogsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AuditLogsModel> _logger;

        public AuditLogsModel(ApplicationDbContext context, ILogger<AuditLogsModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public List<AuditLog> AuditLogs { get; set; } = new();
        public int TotalRecords { get; set; }
        public int PageSize { get; set; } = 50;
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }

        // Filter properties
        [BindProperty(SupportsGet = true)]
        public string? Module { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? EntityType { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Action { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? PerformedBy { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? StartDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? EndDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? PageNumber { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? PageSizeParam { get; set; }

        // Lists for dropdowns
        public List<string> AvailableModules { get; set; } = new();
        public List<string> AvailableEntityTypes { get; set; } = new();
        public List<string> AvailableActions { get; set; } = new();
        public List<string> AvailableUsers { get; set; } = new();

        // Statistics properties
        public int TotalLogsToday { get; set; }
        public int TotalSuccessful { get; set; }
        public int TotalFailed { get; set; }
        public int TotalActiveUsers { get; set; }

        // Pagination helpers
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
        public int TotalCount => TotalRecords;

        public async Task OnGetAsync()
        {
            CurrentPage = PageNumber ?? 1;
            PageSize = PageSizeParam ?? 25;

            // Get distinct values for filters
            AvailableModules = await _context.AuditLogs
                .Where(a => !string.IsNullOrEmpty(a.Module))
                .Select(a => a.Module!)
                .Distinct()
                .OrderBy(m => m)
                .ToListAsync();

            AvailableEntityTypes = await _context.AuditLogs
                .Select(a => a.EntityType)
                .Distinct()
                .OrderBy(e => e)
                .ToListAsync();

            AvailableActions = await _context.AuditLogs
                .Select(a => a.Action)
                .Distinct()
                .OrderBy(a => a)
                .ToListAsync();

            AvailableUsers = await _context.AuditLogs
                .Select(a => a.PerformedBy)
                .Distinct()
                .OrderBy(u => u)
                .ToListAsync();

            // Build query
            var query = _context.AuditLogs.AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(Module))
            {
                query = query.Where(a => a.Module == Module);
            }

            if (!string.IsNullOrEmpty(EntityType))
            {
                query = query.Where(a => a.EntityType == EntityType);
            }

            if (!string.IsNullOrEmpty(Action))
            {
                query = query.Where(a => a.Action == Action);
            }

            if (!string.IsNullOrEmpty(PerformedBy))
            {
                query = query.Where(a => a.PerformedBy == PerformedBy);
            }

            if (StartDate.HasValue)
            {
                query = query.Where(a => a.PerformedDate >= StartDate.Value);
            }

            if (EndDate.HasValue)
            {
                var endOfDay = EndDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(a => a.PerformedDate <= endOfDay);
            }

            if (!string.IsNullOrEmpty(SearchTerm))
            {
                query = query.Where(a =>
                    a.Description!.Contains(SearchTerm) ||
                    a.EntityId!.Contains(SearchTerm) ||
                    a.ErrorMessage!.Contains(SearchTerm) ||
                    a.AdditionalData!.Contains(SearchTerm));
            }

            // Get total count
            TotalRecords = await query.CountAsync();
            TotalPages = (int)Math.Ceiling((double)TotalRecords / PageSize);

            // Apply pagination and get results
            AuditLogs = await query
                .OrderByDescending(a => a.PerformedDate)
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            // Calculate statistics
            var today = DateTime.UtcNow.Date;
            TotalLogsToday = await _context.AuditLogs
                .CountAsync(a => a.PerformedDate >= today);

            TotalSuccessful = await _context.AuditLogs
                .CountAsync(a => a.IsSuccess);

            TotalFailed = await _context.AuditLogs
                .CountAsync(a => !a.IsSuccess);

            TotalActiveUsers = await _context.AuditLogs
                .Where(a => a.PerformedDate >= DateTime.UtcNow.AddDays(-7))
                .Select(a => a.PerformedBy)
                .Distinct()
                .CountAsync();
        }

        public async Task<IActionResult> OnGetDetailsAsync(int id)
        {
            var auditLog = await _context.AuditLogs.FindAsync(id);
            if (auditLog == null)
            {
                return NotFound();
            }

            return new JsonResult(new
            {
                auditLog.Id,
                auditLog.EntityType,
                auditLog.EntityId,
                auditLog.Action,
                auditLog.Description,
                auditLog.OldValues,
                auditLog.NewValues,
                auditLog.PerformedBy,
                PerformedDate = auditLog.PerformedDate.ToString("yyyy-MM-dd HH:mm:ss"),
                auditLog.IPAddress,
                auditLog.UserAgent,
                auditLog.Module,
                auditLog.IsSuccess,
                auditLog.ErrorMessage,
                auditLog.AdditionalData
            });
        }

        public async Task<IActionResult> OnPostExportAsync()
        {
            // Build the same query as OnGetAsync
            var query = _context.AuditLogs.AsQueryable();

            // Apply the same filters
            if (!string.IsNullOrEmpty(Module))
                query = query.Where(a => a.Module == Module);

            if (!string.IsNullOrEmpty(EntityType))
                query = query.Where(a => a.EntityType == EntityType);

            if (!string.IsNullOrEmpty(Action))
                query = query.Where(a => a.Action == Action);

            if (!string.IsNullOrEmpty(PerformedBy))
                query = query.Where(a => a.PerformedBy == PerformedBy);

            if (StartDate.HasValue)
                query = query.Where(a => a.PerformedDate >= StartDate.Value);

            if (EndDate.HasValue)
            {
                var endOfDay = EndDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(a => a.PerformedDate <= endOfDay);
            }

            if (!string.IsNullOrEmpty(SearchTerm))
            {
                query = query.Where(a =>
                    a.Description!.Contains(SearchTerm) ||
                    a.EntityId!.Contains(SearchTerm) ||
                    a.ErrorMessage!.Contains(SearchTerm) ||
                    a.AdditionalData!.Contains(SearchTerm));
            }

            var logs = await query
                .OrderByDescending(a => a.PerformedDate)
                .ToListAsync();

            // Create CSV content with proper escaping
            var csv = new System.Text.StringBuilder();
            csv.AppendLine("Date,User,Module,Entity Type,Entity ID,Action,Description,Success,IP Address,Error Message");

            foreach (var log in logs)
            {
                // Helper function to escape CSV fields
                string EscapeCsvField(string? field)
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

                csv.AppendLine($"{log.PerformedDate:yyyy-MM-dd HH:mm:ss}," +
                    $"{EscapeCsvField(log.PerformedBy)}," +
                    $"{EscapeCsvField(log.Module)}," +
                    $"{EscapeCsvField(log.EntityType)}," +
                    $"{EscapeCsvField(log.EntityId)}," +
                    $"{EscapeCsvField(log.Action)}," +
                    $"{EscapeCsvField(log.Description)}," +
                    $"{(log.IsSuccess ? "Yes" : "No")}," +
                    $"{EscapeCsvField(log.IPAddress)}," +
                    $"{EscapeCsvField(log.ErrorMessage)}");
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());

            // Add BOM for Excel compatibility
            var preamble = System.Text.Encoding.UTF8.GetPreamble();
            var bytesWithBom = new byte[preamble.Length + bytes.Length];
            preamble.CopyTo(bytesWithBom, 0);
            bytes.CopyTo(bytesWithBom, preamble.Length);

            return File(bytesWithBom, "text/csv", $"AuditLogs_{DateTime.Now:yyyyMMddHHmmss}.csv");
        }
    }
}