using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TAB.Web.Data;
using TAB.Web.Models;

namespace TAB.Web.Pages.Admin
{
    [Authorize]
    public class ImportJobsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ImportJobsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<ImportJob> ImportJobs { get; set; } = new();

        // Statistics
        public int TotalJobs { get; set; }
        public int CompletedJobs { get; set; }
        public int RunningJobs { get; set; }
        public int FailedJobs { get; set; }
        public long TotalRecordsProcessed { get; set; }

        // Filters
        public string? SearchTerm { get; set; }
        public string FilterStatus { get; set; } = "all";
        public string FilterCallLogType { get; set; } = "all";
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public async Task OnGetAsync(
            string? searchTerm,
            string? filterStatus,
            string? filterCallLogType,
            DateTime? startDate,
            DateTime? endDate)
        {
            SearchTerm = searchTerm;
            FilterStatus = filterStatus ?? "all";
            FilterCallLogType = filterCallLogType ?? "all";
            StartDate = startDate;
            EndDate = endDate;

            // Build query
            var query = _context.ImportJobs.AsQueryable();

            // Apply filters
            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                query = query.Where(j =>
                    j.FileName.Contains(SearchTerm) ||
                    j.CreatedBy.Contains(SearchTerm) ||
                    j.CallLogType.Contains(SearchTerm));
            }

            if (FilterStatus != "all")
            {
                query = query.Where(j => j.Status == FilterStatus);
            }

            if (FilterCallLogType != "all")
            {
                query = query.Where(j => j.CallLogType.ToLower() == FilterCallLogType.ToLower());
            }

            if (StartDate.HasValue)
            {
                query = query.Where(j => j.CreatedDate >= StartDate.Value);
            }

            if (EndDate.HasValue)
            {
                var endOfDay = EndDate.Value.AddDays(1).AddTicks(-1);
                query = query.Where(j => j.CreatedDate <= endOfDay);
            }

            // Get filtered jobs
            ImportJobs = await query
                .OrderByDescending(j => j.CreatedDate)
                .Take(100)
                .ToListAsync();

            // Calculate statistics (on all jobs, not filtered)
            var allJobs = await _context.ImportJobs.ToListAsync();
            TotalJobs = allJobs.Count;
            CompletedJobs = allJobs.Count(j => j.Status == "Completed");
            RunningJobs = allJobs.Count(j => j.Status == "Processing" || j.Status == "Queued");
            FailedJobs = allJobs.Count(j => j.Status == "Failed");
            TotalRecordsProcessed = allJobs.Where(j => j.Status == "Completed").Sum(j => j.RecordsSuccess ?? 0);
        }
    }
}
