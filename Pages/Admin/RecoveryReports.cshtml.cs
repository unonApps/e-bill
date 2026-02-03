using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TAB.Web.Data;
using TAB.Web.Models;

namespace TAB.Web.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class RecoveryReportsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<RecoveryReportsModel> _logger;

        public RecoveryReportsModel(
            ApplicationDbContext context,
            ILogger<RecoveryReportsModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Filter Parameters
        [BindProperty(SupportsGet = true)]
        public DateTime? StartDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? EndDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public Guid? BatchId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? RecoveryType { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? TableType { get; set; }

        // Finance Report Filters
        [BindProperty(SupportsGet = true)]
        public int? FilterMonth { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? FilterYear { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? FilterOrganizationId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? FilterOfficeId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? FilterSubOfficeId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SearchText { get; set; }

        // Report Data
        public RecoveryReportSummary Summary { get; set; } = new();
        public List<BatchRecoveryDetail> BatchDetails { get; set; } = new();
        public List<UserRecoveryDetail> UserRecoveryDetails { get; set; } = new();
        public List<RecoveryJobDetail> JobExecutions { get; set; } = new();
        public List<DeadlinePerformance> DeadlinePerformanceData { get; set; } = new();
        public List<FinanceRecoveryDetail> FinanceRecoveryDetails { get; set; } = new();

        // Available Batches for Filter
        public List<BatchFilterOption> AvailableBatches { get; set; } = new();

        // Available filter options from EbillUsers
        public List<OrganizationOption> AvailableOrganizations { get; set; } = new();
        public List<OfficeOption> AvailableOffices { get; set; } = new();
        public List<SubOfficeOption> AvailableSubOffices { get; set; } = new();

        // Pagination for Job Executions
        [BindProperty(SupportsGet = true)]
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int TotalPages { get; set; }
        public int TotalRecords { get; set; }

        // Pagination for Finance Recovery Details
        [BindProperty(SupportsGet = true)]
        public int FinancePage { get; set; } = 1;
        public int FinancePageSize { get; set; } = 25;
        public int FinanceTotalPages { get; set; }
        public int FinanceTotalRecords { get; set; }

        // Total amounts across all pages (for summary display)
        public Dictionary<string, decimal> FinanceTotalsByCurrency { get; set; } = new();

        public async Task OnGetAsync()
        {
            // Set default date range if not specified (last 30 days)
            if (!StartDate.HasValue)
                StartDate = DateTime.UtcNow.AddDays(-30);
            if (!EndDate.HasValue)
                EndDate = DateTime.UtcNow;

            // Load filter options and data in parallel
            var batchesTask = LoadAvailableBatchesAsync();
            var filterOptionsTask = LoadFilterOptionsAsync();
            var summaryTask = LoadReportSummaryAsync();
            var batchDetailsTask = LoadBatchDetailsAsync();
            var userDetailsTask = LoadUserRecoveryDetailsAsync();
            var jobsTask = LoadJobExecutionsAsync();
            var deadlinesTask = LoadDeadlinePerformanceAsync();
            var financeTask = LoadFinanceRecoveryDetailsAsync();

            await Task.WhenAll(
                batchesTask,
                filterOptionsTask,
                summaryTask,
                batchDetailsTask,
                userDetailsTask,
                jobsTask,
                deadlinesTask,
                financeTask
            );
        }

        private async Task LoadAvailableBatchesAsync()
        {
            AvailableBatches = await _context.StagingBatches
                .AsNoTracking()
                .Where(b => b.TotalRecoveredAmount.HasValue && b.TotalRecoveredAmount > 0)
                .OrderByDescending(b => b.RecoveryProcessingDate)
                .Take(100)
                .Select(b => new BatchFilterOption
                {
                    BatchId = b.Id,
                    BatchName = b.BatchName,
                    RecoveryDate = b.RecoveryProcessingDate,
                    TotalRecovered = b.TotalRecoveredAmount ?? 0
                })
                .ToListAsync();
        }

        private async Task LoadFilterOptionsAsync()
        {
            // Run all three filter option queries in parallel
            var orgsTask = _context.EbillUsers
                .AsNoTracking()
                .Where(u => u.OrganizationId.HasValue)
                .Select(u => new { u.OrganizationId, u.OrganizationEntity!.Name })
                .Distinct()
                .OrderBy(o => o.Name)
                .Select(o => new OrganizationOption { Id = o.OrganizationId!.Value, Name = o.Name })
                .ToListAsync();

            var officesTask = _context.EbillUsers
                .AsNoTracking()
                .Where(u => u.OfficeId.HasValue)
                .Select(u => new { u.OfficeId, u.OfficeEntity!.Name })
                .Distinct()
                .OrderBy(o => o.Name)
                .Select(o => new OfficeOption { Id = o.OfficeId!.Value, Name = o.Name })
                .ToListAsync();

            var subOfficesTask = _context.EbillUsers
                .AsNoTracking()
                .Where(u => u.SubOfficeId.HasValue)
                .Select(u => new { u.SubOfficeId, u.SubOfficeEntity!.Name })
                .Distinct()
                .OrderBy(o => o.Name)
                .Select(o => new SubOfficeOption { Id = o.SubOfficeId!.Value, Name = o.Name })
                .ToListAsync();

            await Task.WhenAll(orgsTask, officesTask, subOfficesTask);

            AvailableOrganizations = await orgsTask;
            AvailableOffices = await officesTask;
            AvailableSubOffices = await subOfficesTask;
        }

        private async Task LoadReportSummaryAsync()
        {
            var startDate = StartDate ?? DateTime.UtcNow.AddDays(-30);
            var endDate = EndDate?.Date.AddDays(1).AddTicks(-1) ?? DateTime.UtcNow.Date.AddDays(1).AddTicks(-1);

            // Single query with aggregation
            var execStats = await _context.RecoveryJobExecutions
                .AsNoTracking()
                .Where(e => e.Status == "Completed" && e.StartTime >= startDate && e.StartTime <= endDate)
                .GroupBy(e => 1)
                .Select(g => new
                {
                    TotalAmount = g.Sum(e => e.TotalAmountRecovered),
                    TotalRecords = g.Sum(e => e.TotalRecordsProcessed),
                    JobCount = g.Count(),
                    AvgDuration = g.Average(e => e.DurationMs ?? 0),
                    ExpiredVerifications = g.Sum(e => e.ExpiredVerificationsProcessed),
                    ExpiredApprovals = g.Sum(e => e.ExpiredApprovalsProcessed),
                    RevertedVerifications = g.Sum(e => e.RevertedVerificationsProcessed)
                })
                .FirstOrDefaultAsync();

            if (execStats != null)
            {
                Summary.TotalAmountRecovered = execStats.TotalAmount;
                Summary.TotalRecordsProcessed = execStats.TotalRecords;
                Summary.TotalJobsRun = execStats.JobCount;
                Summary.AverageJobDuration = execStats.AvgDuration / 1000.0;
                Summary.ExpiredVerificationsCount = execStats.ExpiredVerifications;
                Summary.ExpiredApprovalsCount = execStats.ExpiredApprovals;
                Summary.RevertedVerificationsCount = execStats.RevertedVerifications;
            }

            // Get batch-specific summaries if batch filter is applied
            if (BatchId.HasValue)
            {
                var batch = await _context.StagingBatches
                    .AsNoTracking()
                    .Where(b => b.Id == BatchId.Value)
                    .Select(b => new
                    {
                        b.BatchName,
                        TotalRecovered = b.TotalRecoveredAmount ?? 0,
                        PersonalAmount = b.TotalPersonalAmount ?? 0,
                        COSAmount = b.TotalClassOfServiceAmount ?? 0,
                        b.TotalRecords
                    })
                    .FirstOrDefaultAsync();

                if (batch != null)
                {
                    Summary.BatchName = batch.BatchName;
                    Summary.BatchTotalRecovered = batch.TotalRecovered;
                    Summary.BatchPersonalAmount = batch.PersonalAmount;
                    Summary.BatchClassOfServiceAmount = batch.COSAmount;
                    Summary.BatchTotalRecords = batch.TotalRecords;
                }
            }
        }

        private async Task LoadBatchDetailsAsync()
        {
            var startDate = StartDate ?? DateTime.UtcNow.AddDays(-30);
            var endDate = EndDate?.Date.AddDays(1).AddTicks(-1) ?? DateTime.UtcNow.Date.AddDays(1).AddTicks(-1);

            var query = _context.StagingBatches
                .AsNoTracking()
                .Where(b => b.TotalRecoveredAmount.HasValue && b.TotalRecoveredAmount > 0);

            if (BatchId.HasValue)
                query = query.Where(b => b.Id == BatchId.Value);

            query = query.Where(b => b.RecoveryProcessingDate >= startDate && b.RecoveryProcessingDate <= endDate);

            BatchDetails = await query
                .OrderByDescending(b => b.RecoveryProcessingDate)
                .Take(50)
                .Select(b => new BatchRecoveryDetail
                {
                    BatchId = b.Id,
                    BatchName = b.BatchName,
                    TotalRecovered = b.TotalRecoveredAmount ?? 0,
                    PersonalAmount = b.TotalPersonalAmount ?? 0,
                    ClassOfServiceAmount = b.TotalClassOfServiceAmount ?? 0,
                    TotalRecords = b.TotalRecords,
                    VerifiedRecords = b.VerifiedRecords,
                    RecoveryDate = b.RecoveryProcessingDate,
                    RecoveryStatus = b.RecoveryStatus
                })
                .ToListAsync();
        }

        private async Task LoadUserRecoveryDetailsAsync()
        {
            var startDate = StartDate ?? DateTime.UtcNow.AddDays(-30);
            var endDate = EndDate?.Date.AddDays(1).AddTicks(-1) ?? DateTime.UtcNow.Date.AddDays(1).AddTicks(-1);

            var query = _context.RecoveryLogs
                .AsNoTracking()
                .Include(rl => rl.CallRecord)
                .Where(rl => rl.RecoveryDate >= startDate && rl.RecoveryDate <= endDate);

            if (BatchId.HasValue)
                query = query.Where(rl => rl.BatchId == BatchId.Value);

            // Group by IndexNumber AND Currency to keep KSH and USD separate
            var userRecoveries = await query
                .GroupBy(rl => new
                {
                    rl.RecoveredFrom,
                    Currency = rl.CallRecord != null ? rl.CallRecord.CallCurrencyCode : "KES"
                })
                .Select(g => new UserRecoveryDetail
                {
                    IndexNumber = g.Key.RecoveredFrom ?? "Unknown",
                    UserPhoneId = null,
                    TotalRecovered = g.Sum(rl => rl.AmountRecovered),
                    PersonalRecovered = g.Where(rl => rl.RecoveryAction == "Personal").Sum(rl => rl.AmountRecovered),
                    ClassOfServiceRecovered = g.Where(rl => rl.RecoveryAction == "ClassOfService").Sum(rl => rl.AmountRecovered),
                    RecordCount = g.Count(),
                    ExpiredVerifications = g.Count(rl => rl.RecoveryType == "StaffNonVerification"),
                    ExpiredApprovals = g.Count(rl => rl.RecoveryType == "SupervisorNonApproval"),
                    Currency = g.Key.Currency ?? "KES"
                })
                .OrderBy(u => u.IndexNumber)
                .ThenBy(u => u.Currency)
                .ThenByDescending(u => u.TotalRecovered)
                .ToListAsync();

            // Get user names in bulk
            var indexNumbers = userRecoveries.Select(u => u.IndexNumber).Distinct().ToList();
            var users = await _context.EbillUsers
                .AsNoTracking()
                .Where(u => indexNumbers.Contains(u.IndexNumber))
                .Select(u => new { u.IndexNumber, u.FirstName, u.LastName })
                .ToDictionaryAsync(u => u.IndexNumber, u => $"{u.FirstName} {u.LastName}");

            foreach (var recovery in userRecoveries)
            {
                if (users.TryGetValue(recovery.IndexNumber, out var name))
                {
                    recovery.UserName = name;
                }
            }

            UserRecoveryDetails = userRecoveries;
        }

        private async Task LoadJobExecutionsAsync()
        {
            var startDate = StartDate ?? DateTime.UtcNow.AddDays(-30);
            var endDate = EndDate?.Date.AddDays(1).AddTicks(-1) ?? DateTime.UtcNow.Date.AddDays(1).AddTicks(-1);

            var query = _context.RecoveryJobExecutions
                .AsNoTracking()
                .Where(e => e.Status == "Completed" && e.StartTime >= startDate && e.StartTime <= endDate);

            // Pagination
            TotalRecords = await query.CountAsync();
            TotalPages = (int)Math.Ceiling(TotalRecords / (double)PageSize);

            JobExecutions = await query
                .OrderByDescending(e => e.EndTime)
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .Select(e => new RecoveryJobDetail
                {
                    ExecutionId = e.Id,
                    StartTime = e.StartTime,
                    EndTime = e.EndTime ?? e.StartTime,
                    DurationSeconds = (e.DurationMs ?? 0) / 1000.0,
                    RunType = e.RunType ?? "Automatic",
                    TriggeredBy = e.TriggeredBy,
                    TotalRecordsProcessed = e.TotalRecordsProcessed,
                    AmountRecovered = e.TotalAmountRecovered,
                    ExpiredVerifications = e.ExpiredVerificationsProcessed,
                    ExpiredApprovals = e.ExpiredApprovalsProcessed,
                    RevertedVerifications = e.RevertedVerificationsProcessed,
                    ErrorCount = 0,
                    Status = e.Status
                })
                .ToListAsync();
        }

        private async Task LoadDeadlinePerformanceAsync()
        {
            var startDate = StartDate ?? DateTime.UtcNow.AddDays(-30);
            var endDate = EndDate?.Date.AddDays(1).AddTicks(-1) ?? DateTime.UtcNow.Date.AddDays(1).AddTicks(-1);

            var query = _context.DeadlineTracking
                .AsNoTracking()
                .Where(d => d.DeadlineDate >= startDate && d.DeadlineDate <= endDate);

            if (BatchId.HasValue)
                query = query.Where(d => d.BatchId == BatchId.Value);

            var deadlines = await query
                .GroupBy(d => d.DeadlineType)
                .Select(g => new DeadlinePerformance
                {
                    DeadlineType = g.Key,
                    TotalDeadlines = g.Count(),
                    MetDeadlines = g.Count(d => d.DeadlineStatus == "Met"),
                    MissedDeadlines = g.Count(d => d.DeadlineStatus == "Missed"),
                    ExtendedDeadlines = g.Count(d => d.ExtendedDeadline.HasValue),
                    AverageResponseTimeHours = 0
                })
                .ToListAsync();

            foreach (var perf in deadlines)
            {
                if (perf.TotalDeadlines > 0)
                {
                    perf.ComplianceRate = (decimal)perf.MetDeadlines / perf.TotalDeadlines * 100;
                }
            }

            DeadlinePerformanceData = deadlines;
        }

        public async Task<IActionResult> OnPostExportAsync(string format)
        {
            await OnGetAsync();

            if (format == "csv")
            {
                return await ExportToCsvAsync();
            }
            else if (format == "excel")
            {
                return await ExportToExcelAsync();
            }
            else if (format == "finance")
            {
                return await ExportFinanceReportAsync();
            }

            return Page();
        }

        private async Task<IActionResult> ExportToCsvAsync()
        {
            var csv = new System.Text.StringBuilder();

            csv.AppendLine("Recovery Report Summary");
            csv.AppendLine($"Period,{StartDate:yyyy-MM-dd} to {EndDate:yyyy-MM-dd}");
            csv.AppendLine($"Total Recovered,{Summary.TotalAmountRecovered:C}");
            csv.AppendLine($"Total Records,{Summary.TotalRecordsProcessed}");
            csv.AppendLine($"Total Jobs,{Summary.TotalJobsRun}");
            csv.AppendLine();

            csv.AppendLine("Batch Details");
            csv.AppendLine("Batch Name,Total Recovered,Personal Amount,Class of Service,Total Records,Recovery Date,Status");
            foreach (var batch in BatchDetails)
            {
                csv.AppendLine($"\"{batch.BatchName}\",{batch.TotalRecovered},{batch.PersonalAmount},{batch.ClassOfServiceAmount},{batch.TotalRecords},{batch.RecoveryDate:yyyy-MM-dd},{batch.RecoveryStatus}");
            }
            csv.AppendLine();

            csv.AppendLine("User Recovery Details");
            csv.AppendLine("Index Number,User Name,Total Recovered,Personal Recovered,Class of Service Recovered,Record Count,Expired Verifications,Expired Approvals");
            foreach (var user in UserRecoveryDetails)
            {
                csv.AppendLine($"{user.IndexNumber},\"{user.UserName}\",{user.TotalRecovered},{user.PersonalRecovered},{user.ClassOfServiceRecovered},{user.RecordCount},{user.ExpiredVerifications},{user.ExpiredApprovals}");
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
            return File(bytes, "text/csv", $"RecoveryReport_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv");
        }

        private async Task<IActionResult> ExportToExcelAsync()
        {
            var csv = await ExportToCsvAsync();
            return File(((FileContentResult)csv).FileContents, "application/vnd.ms-excel", $"RecoveryReport_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xls");
        }

        private async Task<IActionResult> ExportFinanceReportAsync()
        {
            // Temporarily disable pagination for export (export all records)
            var originalPageSize = FinancePageSize;
            FinancePageSize = int.MaxValue;
            FinancePage = 1;

            await LoadFinanceRecoveryDetailsAsync();

            // Restore original page size
            FinancePageSize = originalPageSize;

            var csv = new System.Text.StringBuilder();
            csv.AppendLine("Index_Number,Staff_Name,Org_Unit,Amount,Currency,Phone_Number,Office,SubOffice,Effective_Date,Expiration_Date");

            foreach (var finance in FinanceRecoveryDetails)
            {
                csv.AppendLine($"{finance.IndexNumber},\"{finance.StaffName}\",\"{finance.OrgUnit}\",{finance.Amount:F2},{finance.Currency},\"{finance.PhoneNumber}\",\"{finance.Office}\",\"{finance.SubOffice}\",{finance.EffectiveDate:yyyy-MM-dd},{finance.ExpirationDate:yyyy-MM-dd}");
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
            return File(bytes, "text/csv", $"FinanceRecoveryReport_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv");
        }

        private async Task LoadFinanceRecoveryDetailsAsync()
        {
            var startDate = StartDate ?? DateTime.UtcNow.AddDays(-30);
            var endDate = EndDate?.Date.AddDays(1).AddTicks(-1) ?? DateTime.UtcNow.Date.AddDays(1).AddTicks(-1);

            var query = _context.RecoveryLogs
                .AsNoTracking()
                .Include(rl => rl.CallRecord)
                .Where(rl => rl.RecoveryDate >= startDate && rl.RecoveryDate <= endDate)
                .Where(rl => rl.RecoveryAction == "Personal");

            if (BatchId.HasValue)
                query = query.Where(rl => rl.BatchId == BatchId.Value);

            if (FilterMonth.HasValue && FilterYear.HasValue)
            {
                query = query.Where(rl => rl.RecoveryDate.Month == FilterMonth.Value && rl.RecoveryDate.Year == FilterYear.Value);
            }
            else if (FilterYear.HasValue)
            {
                query = query.Where(rl => rl.RecoveryDate.Year == FilterYear.Value);
            }

            // Group by IndexNumber AND Currency
            var financeData = await query
                .GroupBy(rl => new
                {
                    rl.RecoveredFrom,
                    Currency = rl.CallRecord != null ? rl.CallRecord.CallCurrencyCode : "KES"
                })
                .Select(g => new
                {
                    IndexNumber = g.Key.RecoveredFrom,
                    Currency = g.Key.Currency ?? "KES",
                    TotalAmount = g.Sum(rl => rl.AmountRecovered),
                    PhoneNumber = g.Select(rl => rl.CallRecord != null ? rl.CallRecord.CallNumber : null).FirstOrDefault(),
                    RecoveryDate = g.Max(rl => rl.RecoveryDate)
                })
                .OrderBy(g => g.IndexNumber)
                .ThenBy(g => g.Currency)
                .ToListAsync();

            // Get all user details in bulk
            var indexNumbers = financeData.Select(f => f.IndexNumber).Where(i => i != null).Cast<string>().Distinct().ToList();

            var userDetails = await _context.EbillUsers
                .AsNoTracking()
                .Where(u => indexNumbers.Contains(u.IndexNumber))
                .Select(u => new
                {
                    u.IndexNumber,
                    FullName = u.FirstName + " " + u.LastName,
                    u.OrganizationId,
                    u.OfficeId,
                    u.SubOfficeId
                })
                .ToDictionaryAsync(u => u.IndexNumber, u => u);

            // Get organization, office, and suboffice names in bulk
            var orgIds = userDetails.Values.Where(u => u.OrganizationId.HasValue).Select(u => u.OrganizationId!.Value).Distinct().ToList();
            var officeIds = userDetails.Values.Where(u => u.OfficeId.HasValue).Select(u => u.OfficeId!.Value).Distinct().ToList();
            var subOfficeIds = userDetails.Values.Where(u => u.SubOfficeId.HasValue).Select(u => u.SubOfficeId!.Value).Distinct().ToList();

            var orgNames = await _context.Organizations
                .AsNoTracking()
                .Where(o => orgIds.Contains(o.Id))
                .ToDictionaryAsync(o => o.Id, o => o.Name);

            var officeNames = await _context.Offices
                .AsNoTracking()
                .Where(o => officeIds.Contains(o.Id))
                .ToDictionaryAsync(o => o.Id, o => o.Name);

            var subOfficeNames = await _context.SubOffices
                .AsNoTracking()
                .Where(o => subOfficeIds.Contains(o.Id))
                .ToDictionaryAsync(o => o.Id, o => o.Name);

            var financeDetails = new List<FinanceRecoveryDetail>();

            foreach (var recovery in financeData)
            {
                var user = recovery.IndexNumber != null && userDetails.TryGetValue(recovery.IndexNumber, out var u) ? u : null;

                var detail = new FinanceRecoveryDetail
                {
                    IndexNumber = recovery.IndexNumber ?? "",
                    StaffName = user?.FullName ?? "Unknown User",
                    OrgUnit = user?.OrganizationId.HasValue == true && orgNames.TryGetValue(user.OrganizationId.Value, out var orgName) ? orgName : "",
                    Amount = recovery.TotalAmount,
                    Currency = recovery.Currency,
                    PhoneNumber = recovery.PhoneNumber ?? "",
                    Office = user?.OfficeId.HasValue == true && officeNames.TryGetValue(user.OfficeId.Value, out var officeName) ? officeName : "",
                    SubOffice = user?.SubOfficeId.HasValue == true && subOfficeNames.TryGetValue(user.SubOfficeId.Value, out var subOfficeName) ? subOfficeName : "",
                    EffectiveDate = recovery.RecoveryDate,
                    ExpirationDate = recovery.RecoveryDate.AddMonths(2)
                };

                financeDetails.Add(detail);
            }

            // Apply post-processing filters
            var filteredDetails = financeDetails.AsEnumerable();

            if (FilterOrganizationId.HasValue)
            {
                var filteredIndexNumbers = userDetails.Values
                    .Where(u => u.OrganizationId == FilterOrganizationId.Value)
                    .Select(u => u.IndexNumber)
                    .ToHashSet();
                filteredDetails = filteredDetails.Where(f => filteredIndexNumbers.Contains(f.IndexNumber));
            }

            if (FilterOfficeId.HasValue)
            {
                var filteredIndexNumbers = userDetails.Values
                    .Where(u => u.OfficeId == FilterOfficeId.Value)
                    .Select(u => u.IndexNumber)
                    .ToHashSet();
                filteredDetails = filteredDetails.Where(f => filteredIndexNumbers.Contains(f.IndexNumber));
            }

            if (FilterSubOfficeId.HasValue)
            {
                var filteredIndexNumbers = userDetails.Values
                    .Where(u => u.SubOfficeId == FilterSubOfficeId.Value)
                    .Select(u => u.IndexNumber)
                    .ToHashSet();
                filteredDetails = filteredDetails.Where(f => filteredIndexNumbers.Contains(f.IndexNumber));
            }

            if (!string.IsNullOrEmpty(SearchText))
            {
                filteredDetails = filteredDetails.Where(f =>
                    f.StaffName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    f.IndexNumber.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
            }

            // Apply pagination
            var orderedDetails = filteredDetails.OrderBy(f => f.IndexNumber).ThenBy(f => f.Currency).ToList();
            FinanceTotalRecords = orderedDetails.Count;
            FinanceTotalPages = (int)Math.Ceiling(FinanceTotalRecords / (double)FinancePageSize);

            // Calculate totals by currency BEFORE pagination (for summary display)
            FinanceTotalsByCurrency = orderedDetails
                .GroupBy(f => f.Currency)
                .ToDictionary(g => g.Key, g => g.Sum(f => f.Amount));

            // Ensure current page is valid
            if (FinancePage < 1) FinancePage = 1;
            if (FinancePage > FinanceTotalPages && FinanceTotalPages > 0) FinancePage = FinanceTotalPages;

            FinanceRecoveryDetails = orderedDetails
                .Skip((FinancePage - 1) * FinancePageSize)
                .Take(FinancePageSize)
                .ToList();
        }
    }

    // Data Transfer Objects
    public class RecoveryReportSummary
    {
        public decimal TotalAmountRecovered { get; set; }
        public int TotalRecordsProcessed { get; set; }
        public int TotalJobsRun { get; set; }
        public double AverageJobDuration { get; set; }
        public int ExpiredVerificationsCount { get; set; }
        public int ExpiredApprovalsCount { get; set; }
        public int RevertedVerificationsCount { get; set; }
        public string? BatchName { get; set; }
        public decimal BatchTotalRecovered { get; set; }
        public decimal BatchPersonalAmount { get; set; }
        public decimal BatchClassOfServiceAmount { get; set; }
        public int BatchTotalRecords { get; set; }
    }

    public class BatchRecoveryDetail
    {
        public Guid BatchId { get; set; }
        public string BatchName { get; set; } = string.Empty;
        public decimal TotalRecovered { get; set; }
        public decimal PersonalAmount { get; set; }
        public decimal ClassOfServiceAmount { get; set; }
        public int TotalRecords { get; set; }
        public int VerifiedRecords { get; set; }
        public DateTime? RecoveryDate { get; set; }
        public DateTime? VerificationDeadline { get; set; }
        public DateTime? ApprovalDeadline { get; set; }
        public string? RecoveryStatus { get; set; }
    }

    public class UserRecoveryDetail
    {
        public string IndexNumber { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public int? UserPhoneId { get; set; }
        public decimal TotalRecovered { get; set; }
        public decimal PersonalRecovered { get; set; }
        public decimal ClassOfServiceRecovered { get; set; }
        public int RecordCount { get; set; }
        public int ExpiredVerifications { get; set; }
        public int ExpiredApprovals { get; set; }
        public string Currency { get; set; } = "KES";
    }

    public class RecoveryJobDetail
    {
        public int ExecutionId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public double DurationSeconds { get; set; }
        public string RunType { get; set; } = string.Empty;
        public string? TriggeredBy { get; set; }
        public int TotalRecordsProcessed { get; set; }
        public decimal AmountRecovered { get; set; }
        public int ExpiredVerifications { get; set; }
        public int ExpiredApprovals { get; set; }
        public int RevertedVerifications { get; set; }
        public int ErrorCount { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class DeadlinePerformance
    {
        public string DeadlineType { get; set; } = string.Empty;
        public int TotalDeadlines { get; set; }
        public int MetDeadlines { get; set; }
        public int MissedDeadlines { get; set; }
        public int ExtendedDeadlines { get; set; }
        public double AverageResponseTimeHours { get; set; }
        public decimal ComplianceRate { get; set; }
    }

    public class BatchFilterOption
    {
        public Guid BatchId { get; set; }
        public string BatchName { get; set; } = string.Empty;
        public DateTime? RecoveryDate { get; set; }
        public decimal TotalRecovered { get; set; }
    }

    public class FinanceRecoveryDetail
    {
        public string IndexNumber { get; set; } = string.Empty;
        public string StaffName { get; set; } = string.Empty;
        public string OrgUnit { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "KES";
        public string PhoneNumber { get; set; } = string.Empty;
        public string Office { get; set; } = string.Empty;
        public string SubOffice { get; set; } = string.Empty;
        public DateTime EffectiveDate { get; set; }
        public DateTime ExpirationDate { get; set; }
    }

    public class OrganizationOption
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class OfficeOption
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class SubOfficeOption
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
