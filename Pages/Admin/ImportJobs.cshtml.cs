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
        public List<StagingBatch> ConsolidationJobs { get; set; } = new();
        public List<EmailLog> EmailJobs { get; set; } = new();

        // Statistics (includes all job types)
        public int TotalJobs { get; set; }
        public int CompletedJobs { get; set; }
        public int RunningJobs { get; set; }
        public int FailedJobs { get; set; }
        public long TotalRecordsProcessed { get; set; }

        // Separate counts for each type
        public int TotalImportJobs { get; set; }
        public int TotalConsolidationJobs { get; set; }
        public int TotalEmailJobs { get; set; }

        // Email-specific stats
        public int EmailsQueued { get; set; }
        public int EmailsSent { get; set; }
        public int EmailsFailed { get; set; }

        // Filters
        public string? SearchTerm { get; set; }
        public string FilterStatus { get; set; } = "all";
        public string FilterCallLogType { get; set; } = "all";
        public string FilterJobType { get; set; } = "all";
        public string? DateRange { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        // Pagination
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 25;
        public int TotalFilteredJobs { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalFilteredJobs / PageSize);
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;

        public async Task OnGetAsync(
            string? searchTerm,
            string? filterStatus,
            string? filterCallLogType,
            string? filterJobType,
            string? dateRange,
            DateTime? startDate,
            DateTime? endDate,
            int page = 1,
            int pageSize = 25)
        {
            SearchTerm = searchTerm;
            FilterStatus = filterStatus ?? "all";
            FilterCallLogType = filterCallLogType ?? "all";
            FilterJobType = filterJobType ?? "all";
            DateRange = dateRange;
            CurrentPage = page < 1 ? 1 : page;
            PageSize = pageSize < 10 ? 10 : (pageSize > 100 ? 100 : pageSize);

            // Apply date range presets
            if (!string.IsNullOrEmpty(dateRange) && dateRange != "custom")
            {
                var today = DateTime.Today;
                switch (dateRange)
                {
                    case "today":
                        StartDate = today;
                        EndDate = today;
                        break;
                    case "yesterday":
                        StartDate = today.AddDays(-1);
                        EndDate = today.AddDays(-1);
                        break;
                    case "last7days":
                        StartDate = today.AddDays(-7);
                        EndDate = today;
                        break;
                    case "last30days":
                        StartDate = today.AddDays(-30);
                        EndDate = today;
                        break;
                    case "thisMonth":
                        StartDate = new DateTime(today.Year, today.Month, 1);
                        EndDate = today;
                        break;
                    case "lastMonth":
                        var lastMonth = today.AddMonths(-1);
                        StartDate = new DateTime(lastMonth.Year, lastMonth.Month, 1);
                        EndDate = new DateTime(lastMonth.Year, lastMonth.Month, DateTime.DaysInMonth(lastMonth.Year, lastMonth.Month));
                        break;
                    default:
                        StartDate = startDate;
                        EndDate = endDate;
                        break;
                }
            }
            else
            {
                StartDate = startDate;
                EndDate = endDate;
            }

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

            // Get total count for filtered import jobs
            var importJobsCount = await query.CountAsync();

            // =============================================
            // Load consolidation jobs (StagingBatches)
            // =============================================
            var consolidationQuery = _context.StagingBatches.AsQueryable();

            // Apply filters
            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                consolidationQuery = consolidationQuery.Where(b =>
                    b.BatchName.Contains(SearchTerm) ||
                    b.CreatedBy.Contains(SearchTerm));
            }

            if (FilterStatus != "all")
            {
                // Map ImportJob statuses to BatchStatus
                consolidationQuery = FilterStatus switch
                {
                    "Completed" => consolidationQuery.Where(b => b.BatchStatus == BatchStatus.Published || b.BatchStatus == BatchStatus.Verified),
                    "Processing" => consolidationQuery.Where(b => b.BatchStatus == BatchStatus.Processing || b.BatchStatus == BatchStatus.Created),
                    "Failed" => consolidationQuery.Where(b => b.BatchStatus == BatchStatus.Failed),
                    _ => consolidationQuery
                };
            }

            if (StartDate.HasValue)
            {
                consolidationQuery = consolidationQuery.Where(b => b.CreatedDate >= StartDate.Value);
            }

            if (EndDate.HasValue)
            {
                var endOfDay = EndDate.Value.AddDays(1).AddTicks(-1);
                consolidationQuery = consolidationQuery.Where(b => b.CreatedDate <= endOfDay);
            }

            // Get total count for filtered consolidation jobs
            var consolidationJobsCount = await consolidationQuery.CountAsync();

            // =============================================
            // Load email jobs (recent 100)
            // =============================================
            var emailQuery = _context.EmailLogs.AsQueryable();

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                emailQuery = emailQuery.Where(e =>
                    e.ToEmail.Contains(SearchTerm) ||
                    e.Subject.Contains(SearchTerm) ||
                    (e.CreatedBy != null && e.CreatedBy.Contains(SearchTerm)));
            }

            if (FilterStatus != "all")
            {
                emailQuery = FilterStatus switch
                {
                    "Queued" => emailQuery.Where(e => e.Status == "Queued"),
                    "Processing" => emailQuery.Where(e => e.Status == "Sending"),
                    "Completed" => emailQuery.Where(e => e.Status == "Sent"),
                    "Failed" => emailQuery.Where(e => e.Status == "Failed"),
                    _ => emailQuery
                };
            }

            if (StartDate.HasValue)
            {
                emailQuery = emailQuery.Where(e => e.CreatedDate >= StartDate.Value);
            }

            if (EndDate.HasValue)
            {
                var endOfDay = EndDate.Value.AddDays(1).AddTicks(-1);
                emailQuery = emailQuery.Where(e => e.CreatedDate <= endOfDay);
            }

            // Get total count for filtered email jobs
            var emailJobsCount = await emailQuery.CountAsync();

            // =============================================
            // Apply Job Type filter
            // =============================================
            var includeImports = FilterJobType == "all" || FilterJobType == "Import";
            var includeConsolidations = FilterJobType == "all" || FilterJobType == "Consolidation";
            var includeEmails = FilterJobType == "all" || FilterJobType == "Email";

            // Adjust counts based on job type filter
            var filteredImportCount = includeImports ? importJobsCount : 0;
            var filteredConsolidationCount = includeConsolidations ? consolidationJobsCount : 0;
            var filteredEmailCount = includeEmails ? emailJobsCount : 0;

            // =============================================
            // Calculate pagination for combined results
            // =============================================
            TotalFilteredJobs = filteredImportCount + filteredConsolidationCount + filteredEmailCount;

            // For combined pagination, we need to fetch all and paginate in memory
            // This is a simpler approach for mixed job types
            var allFilteredJobs = new List<(object Job, DateTime CreatedDate, string Type)>();

            // Fetch all filtered import jobs with date (if included)
            if (includeImports)
            {
                var importList = await query
                    .OrderByDescending(j => j.CreatedDate)
                    .ToListAsync();
                allFilteredJobs.AddRange(importList.Select(j => ((object)j, j.CreatedDate, "Import")));
            }

            // Fetch all filtered consolidation jobs with date (if included)
            if (includeConsolidations)
            {
                var consolidationList = await consolidationQuery
                    .Include(b => b.BillingPeriod)
                    .OrderByDescending(b => b.CreatedDate)
                    .ToListAsync();
                allFilteredJobs.AddRange(consolidationList.Select(b => ((object)b, b.CreatedDate, "Consolidation")));
            }

            // Fetch all filtered email jobs with date (if included)
            if (includeEmails)
            {
                var emailList = await emailQuery
                    .OrderByDescending(e => e.CreatedDate)
                    .ToListAsync();
                allFilteredJobs.AddRange(emailList.Select(e => ((object)e, e.CreatedDate, "Email")));
            }

            // Sort all by date descending and paginate
            var paginatedJobs = allFilteredJobs
                .OrderByDescending(x => x.CreatedDate)
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            // Separate back into typed lists for the view
            ImportJobs = paginatedJobs
                .Where(x => x.Type == "Import")
                .Select(x => (ImportJob)x.Job)
                .ToList();

            ConsolidationJobs = paginatedJobs
                .Where(x => x.Type == "Consolidation")
                .Select(x => (StagingBatch)x.Job)
                .ToList();

            EmailJobs = paginatedJobs
                .Where(x => x.Type == "Email")
                .Select(x => (EmailLog)x.Job)
                .ToList();

            // =============================================
            // Calculate combined statistics
            // =============================================
            // Import jobs stats
            var allImportJobs = await _context.ImportJobs.ToListAsync();
            var importCompleted = allImportJobs.Count(j => j.Status == "Completed");
            var importRunning = allImportJobs.Count(j => j.Status == "Processing" || j.Status == "Queued");
            var importFailed = allImportJobs.Count(j => j.Status == "Failed");
            var importRecordsProcessed = allImportJobs.Where(j => j.Status == "Completed").Sum(j => j.RecordsSuccess ?? 0);

            // Consolidation jobs stats
            var allConsolidationJobs = await _context.StagingBatches.ToListAsync();
            var consolidationCompleted = allConsolidationJobs.Count(b => b.BatchStatus == BatchStatus.Published || (b.BatchStatus == BatchStatus.Verified && string.IsNullOrEmpty(b.CurrentOperation)));
            var consolidationRunning = allConsolidationJobs.Count(b => b.BatchStatus == BatchStatus.Created || b.BatchStatus == BatchStatus.Processing || !string.IsNullOrEmpty(b.CurrentOperation));
            var consolidationFailed = allConsolidationJobs.Count(b => b.BatchStatus == BatchStatus.Failed);
            var consolidationRecordsProcessed = allConsolidationJobs.Where(b => b.BatchStatus == BatchStatus.Published || b.BatchStatus == BatchStatus.Verified).Sum(b => b.TotalRecords);

            // Email jobs stats
            var allEmailJobs = await _context.EmailLogs.ToListAsync();
            EmailsQueued = allEmailJobs.Count(e => e.Status == "Queued");
            EmailsSent = allEmailJobs.Count(e => e.Status == "Sent");
            EmailsFailed = allEmailJobs.Count(e => e.Status == "Failed");
            TotalEmailJobs = allEmailJobs.Count;

            // Combined totals
            TotalImportJobs = allImportJobs.Count;
            TotalConsolidationJobs = allConsolidationJobs.Count;
            TotalJobs = TotalImportJobs + TotalConsolidationJobs + TotalEmailJobs;
            CompletedJobs = importCompleted + consolidationCompleted + EmailsSent;
            RunningJobs = importRunning + consolidationRunning + EmailsQueued;
            FailedJobs = importFailed + consolidationFailed + EmailsFailed;
            TotalRecordsProcessed = importRecordsProcessed + consolidationRecordsProcessed;
        }
    }
}
