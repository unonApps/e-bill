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
        public string? RecoveryType { get; set; } // "Expired Verification", "Expired Approval", "Reverted"

        [BindProperty(SupportsGet = true)]
        public string? TableType { get; set; } // "Safaricom", "Airtel", "PSTN", "PrivateWire"

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
        public string? SearchText { get; set; } // Search by username or index number

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

        // Pagination
        [BindProperty(SupportsGet = true)]
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int TotalPages { get; set; }
        public int TotalRecords { get; set; }

        public async Task OnGetAsync()
        {
            // Set default date range if not specified (last 30 days)
            if (!StartDate.HasValue)
                StartDate = DateTime.UtcNow.AddDays(-30);
            if (!EndDate.HasValue)
                EndDate = DateTime.UtcNow;

            await LoadAvailableBatchesAsync();
            await LoadFilterOptionsAsync();
            await LoadReportSummaryAsync();
            await LoadBatchDetailsAsync();
            await LoadUserRecoveryDetailsAsync();
            await LoadJobExecutionsAsync();
            await LoadDeadlinePerformanceAsync();
            await LoadFinanceRecoveryDetailsAsync();
        }

        private async Task LoadAvailableBatchesAsync()
        {
            AvailableBatches = await _context.StagingBatches
                .Where(b => b.TotalRecoveredAmount.HasValue && b.TotalRecoveredAmount > 0)
                .OrderByDescending(b => b.RecoveryProcessingDate)
                .Select(b => new BatchFilterOption
                {
                    BatchId = b.Id,
                    BatchName = b.BatchName,
                    RecoveryDate = b.RecoveryProcessingDate,
                    TotalRecovered = b.TotalRecoveredAmount ?? 0
                })
                .Take(100)
                .ToListAsync();
        }

        private async Task LoadFilterOptionsAsync()
        {
            // Load unique organizations from EbillUsers who have recoveries
            AvailableOrganizations = await _context.EbillUsers
                .Where(u => u.OrganizationId.HasValue)
                .Include(u => u.OrganizationEntity)
                .Select(u => new { u.OrganizationId, u.OrganizationEntity!.Name })
                .Distinct()
                .OrderBy(o => o.Name)
                .Select(o => new OrganizationOption { Id = o.OrganizationId!.Value, Name = o.Name })
                .ToListAsync();

            // Load unique offices from EbillUsers who have recoveries
            AvailableOffices = await _context.EbillUsers
                .Where(u => u.OfficeId.HasValue)
                .Include(u => u.OfficeEntity)
                .Select(u => new { u.OfficeId, u.OfficeEntity!.Name })
                .Distinct()
                .OrderBy(o => o.Name)
                .Select(o => new OfficeOption { Id = o.OfficeId!.Value, Name = o.Name })
                .ToListAsync();

            // Load unique sub offices from EbillUsers who have recoveries
            AvailableSubOffices = await _context.EbillUsers
                .Where(u => u.SubOfficeId.HasValue)
                .Include(u => u.SubOfficeEntity)
                .Select(u => new { u.SubOfficeId, u.SubOfficeEntity!.Name })
                .Distinct()
                .OrderBy(o => o.Name)
                .Select(o => new SubOfficeOption { Id = o.SubOfficeId!.Value, Name = o.Name })
                .ToListAsync();
        }

        private async Task LoadReportSummaryAsync()
        {
            var query = _context.RecoveryJobExecutions
                .Where(e => e.Status == "Completed");

            // Apply filters - ensure EndDate includes the full day
            var startDate = StartDate ?? DateTime.UtcNow.AddDays(-30);
            var endDate = EndDate?.Date.AddDays(1).AddTicks(-1) ?? DateTime.UtcNow.Date.AddDays(1).AddTicks(-1);

            query = query.Where(e => e.StartTime >= startDate && e.StartTime <= endDate);

            var executions = await query.ToListAsync();

            if (executions.Any())
            {
                Summary.TotalAmountRecovered = executions.Sum(e => e.TotalAmountRecovered);
                Summary.TotalRecordsProcessed = executions.Sum(e => e.TotalRecordsProcessed);
                Summary.TotalJobsRun = executions.Count;
                Summary.AverageJobDuration = executions.Average(e => e.DurationMs ?? 0) / 1000.0; // Convert to seconds

                Summary.ExpiredVerificationsCount = executions.Sum(e => e.ExpiredVerificationsProcessed);
                Summary.ExpiredApprovalsCount = executions.Sum(e => e.ExpiredApprovalsProcessed);
                Summary.RevertedVerificationsCount = executions.Sum(e => e.RevertedVerificationsProcessed);
            }

            // Get batch-specific summaries if batch filter is applied
            if (BatchId.HasValue)
            {
                var batch = await _context.StagingBatches
                    .FirstOrDefaultAsync(b => b.Id == BatchId.Value);

                if (batch != null)
                {
                    Summary.BatchName = batch.BatchName;
                    Summary.BatchTotalRecovered = batch.TotalRecoveredAmount ?? 0;
                    Summary.BatchPersonalAmount = batch.TotalPersonalAmount ?? 0;
                    Summary.BatchClassOfServiceAmount = batch.TotalClassOfServiceAmount ?? 0;
                    Summary.BatchTotalRecords = batch.TotalRecords;
                }
            }
        }

        private async Task LoadBatchDetailsAsync()
        {
            var query = _context.StagingBatches
                .Where(b => b.TotalRecoveredAmount.HasValue && b.TotalRecoveredAmount > 0);

            // Apply filters
            if (BatchId.HasValue)
                query = query.Where(b => b.Id == BatchId.Value);

            // Apply date filters - ensure EndDate includes the full day
            if (StartDate.HasValue || EndDate.HasValue)
            {
                var startDate = StartDate ?? DateTime.UtcNow.AddDays(-30);
                var endDate = EndDate?.Date.AddDays(1).AddTicks(-1) ?? DateTime.UtcNow.Date.AddDays(1).AddTicks(-1);
                query = query.Where(b => b.RecoveryProcessingDate >= startDate && b.RecoveryProcessingDate <= endDate);
            }

            BatchDetails = await query
                .OrderByDescending(b => b.RecoveryProcessingDate)
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
                .Take(50)
                .ToListAsync();
        }

        private async Task LoadUserRecoveryDetailsAsync()
        {
            // Use RecoveryLogs for accurate recovery amounts
            // Apply date filters - ensure EndDate includes the full day
            var startDate = StartDate ?? DateTime.UtcNow.AddDays(-30);
            var endDate = EndDate?.Date.AddDays(1).AddTicks(-1) ?? DateTime.UtcNow.Date.AddDays(1).AddTicks(-1);

            var query = _context.RecoveryLogs
                .Include(rl => rl.CallRecord)
                .AsQueryable();

            query = query.Where(rl => rl.RecoveryDate >= startDate && rl.RecoveryDate <= endDate);

            // Filter by batch
            if (BatchId.HasValue)
                query = query.Where(rl => rl.BatchId == BatchId.Value);

            // IMPORTANT: Group by BOTH IndexNumber AND Currency to keep KSH and USD separate!
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

            // Get user names
            var indexNumbers = userRecoveries.Select(u => u.IndexNumber).Distinct().ToList();
            var users = await _context.EbillUsers
                .Where(u => indexNumbers.Contains(u.IndexNumber))
                .Select(u => new { u.IndexNumber, u.FirstName, u.LastName })
                .ToListAsync();

            foreach (var recovery in userRecoveries)
            {
                var user = users.FirstOrDefault(u => u.IndexNumber == recovery.IndexNumber);
                if (user != null)
                {
                    recovery.UserName = $"{user.FirstName} {user.LastName}";
                }
            }

            UserRecoveryDetails = userRecoveries;
        }

        private async Task LoadJobExecutionsAsync()
        {
            var query = _context.RecoveryJobExecutions
                .Where(e => e.Status == "Completed");

            // Apply filters - ensure EndDate includes the full day
            var startDate = StartDate ?? DateTime.UtcNow.AddDays(-30);
            var endDate = EndDate?.Date.AddDays(1).AddTicks(-1) ?? DateTime.UtcNow.Date.AddDays(1).AddTicks(-1);

            query = query.Where(e => e.StartTime >= startDate && e.StartTime <= endDate);

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
                    ErrorCount = 0, // Not tracked in this model
                    Status = e.Status
                })
                .ToListAsync();
        }

        private async Task LoadDeadlinePerformanceAsync()
        {
            var query = _context.DeadlineTracking.AsQueryable();

            // Apply filters - ensure EndDate includes the full day
            var startDate = StartDate ?? DateTime.UtcNow.AddDays(-30);
            var endDate = EndDate?.Date.AddDays(1).AddTicks(-1) ?? DateTime.UtcNow.Date.AddDays(1).AddTicks(-1);

            query = query.Where(d => d.DeadlineDate >= startDate && d.DeadlineDate <= endDate);

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
                    AverageResponseTimeHours = 0 // Could calculate from recovery processed date if needed
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
            await OnGetAsync(); // Load data with current filters

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

            // Summary Header
            csv.AppendLine("Recovery Report Summary");
            csv.AppendLine($"Period,{StartDate:yyyy-MM-dd} to {EndDate:yyyy-MM-dd}");
            csv.AppendLine($"Total Recovered,{Summary.TotalAmountRecovered:C}");
            csv.AppendLine($"Total Records,{Summary.TotalRecordsProcessed}");
            csv.AppendLine($"Total Jobs,{Summary.TotalJobsRun}");
            csv.AppendLine();

            // Batch Details
            csv.AppendLine("Batch Details");
            csv.AppendLine("Batch Name,Total Recovered,Personal Amount,Class of Service,Total Records,Recovery Date,Status");
            foreach (var batch in BatchDetails)
            {
                csv.AppendLine($"\"{batch.BatchName}\",{batch.TotalRecovered},{batch.PersonalAmount},{batch.ClassOfServiceAmount},{batch.TotalRecords},{batch.RecoveryDate:yyyy-MM-dd},{batch.RecoveryStatus}");
            }
            csv.AppendLine();

            // User Recovery Details
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
            // For now, return CSV with Excel MIME type
            // In a real implementation, use a library like EPPlus or ClosedXML
            var csv = await ExportToCsvAsync();
            return File(((FileContentResult)csv).FileContents, "application/vnd.ms-excel", $"RecoveryReport_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xls");
        }

        /// <summary>
        /// Export Finance Recovery Report for payroll deductions
        /// Format: Index_Number, Staff_name, org_unit, Amount, Currency, Number, office, suboffice, Effective_Date, Expiration_Date
        /// </summary>
        private async Task<IActionResult> ExportFinanceReportAsync()
        {
            var csv = new System.Text.StringBuilder();

            // CSV Header for Finance
            csv.AppendLine("Index_Number,Staff_Name,Org_Unit,Amount,Currency,Phone_Number,Office,SubOffice,Effective_Date,Expiration_Date");

            // Query recovery data with all required joins
            var financeData = await _context.RecoveryLogs
                .Include(rl => rl.CallRecord)
                .Where(rl => rl.RecoveryDate >= StartDate && rl.RecoveryDate <= EndDate)
                .Where(rl => !BatchId.HasValue || rl.BatchId == BatchId.Value)
                .Where(rl => rl.RecoveryAction == "Personal") // Only Personal recoveries need payroll deduction
                .GroupBy(rl => new { rl.RecoveredFrom })
                .Select(g => new
                {
                    IndexNumber = g.Key.RecoveredFrom,
                    TotalAmount = g.Sum(rl => rl.AmountRecovered),
                    Currency = g.Select(rl => rl.CallRecord != null ? rl.CallRecord.CallCurrencyCode : null).FirstOrDefault() ?? "KES",
                    PhoneNumber = g.Select(rl => rl.CallRecord != null ? rl.CallRecord.CallNumber : null).FirstOrDefault(),
                    RecoveryDate = g.Max(rl => rl.RecoveryDate)
                })
                .ToListAsync();

            // Get user details for each recovery
            foreach (var recovery in financeData)
            {
                var user = await _context.EbillUsers
                    .FirstOrDefaultAsync(u => u.IndexNumber == recovery.IndexNumber);

                if (user != null)
                {
                    var staffName = $"{user.FirstName} {user.LastName}".Trim();

                    // Get organization name
                    var orgUnit = "";
                    if (user.OrganizationId.HasValue)
                    {
                        var org = await _context.Organizations.FindAsync(user.OrganizationId.Value);
                        orgUnit = org?.Name ?? user.OrganizationId.ToString() ?? "";
                    }

                    // Get office name
                    var office = "";
                    if (user.OfficeId.HasValue)
                    {
                        var officeEntity = await _context.Offices.FindAsync(user.OfficeId.Value);
                        office = officeEntity?.Name ?? user.OfficeId.ToString() ?? "";
                    }

                    // Get suboffice name
                    var subOffice = "";
                    if (user.SubOfficeId.HasValue)
                    {
                        var subOfficeEntity = await _context.SubOffices.FindAsync(user.SubOfficeId.Value);
                        subOffice = subOfficeEntity?.Name ?? user.SubOfficeId.ToString() ?? "";
                    }

                    var effectiveDate = recovery.RecoveryDate.ToString("yyyy-MM-dd");
                    // Expiration date: typically end of pay period (e.g., end of current month + 1 month)
                    var expirationDate = recovery.RecoveryDate.AddMonths(2).ToString("yyyy-MM-dd");

                    csv.AppendLine($"{recovery.IndexNumber},\"{staffName}\",\"{orgUnit}\",{recovery.TotalAmount:F2},{recovery.Currency},\"{recovery.PhoneNumber}\",\"{office}\",\"{subOffice}\",{effectiveDate},{expirationDate}");
                }
                else
                {
                    // User not found, export with index number only
                    var effectiveDate = recovery.RecoveryDate.ToString("yyyy-MM-dd");
                    var expirationDate = recovery.RecoveryDate.AddMonths(2).ToString("yyyy-MM-dd");

                    csv.AppendLine($"{recovery.IndexNumber},\"Unknown User\",\"\",{recovery.TotalAmount:F2},{recovery.Currency},\"{recovery.PhoneNumber}\",\"\",\"\",{effectiveDate},{expirationDate}");
                }
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
            return File(bytes, "text/csv", $"FinanceRecoveryReport_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv");
        }

        private async Task LoadFinanceRecoveryDetailsAsync()
        {
            // Ensure we have date values
            var startDate = StartDate ?? DateTime.UtcNow.AddDays(-30);
            var endDate = EndDate?.Date.AddDays(1).AddTicks(-1) ?? DateTime.UtcNow.Date.AddDays(1).AddTicks(-1); // End of day

            // Query recovery data with all required joins
            // IMPORTANT: Group by BOTH IndexNumber AND Currency to keep KSH and USD separate!
            var query = _context.RecoveryLogs
                .Include(rl => rl.CallRecord)
                .Where(rl => rl.RecoveryDate >= startDate && rl.RecoveryDate <= endDate)
                .Where(rl => !BatchId.HasValue || rl.BatchId == BatchId.Value)
                .Where(rl => rl.RecoveryAction == "Personal"); // Only Personal recoveries need payroll deduction

            // Apply Month/Year filter
            if (FilterMonth.HasValue && FilterYear.HasValue)
            {
                query = query.Where(rl => rl.RecoveryDate.Month == FilterMonth.Value && rl.RecoveryDate.Year == FilterYear.Value);
            }
            else if (FilterYear.HasValue)
            {
                query = query.Where(rl => rl.RecoveryDate.Year == FilterYear.Value);
            }

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
                    RecoveryDate = g.Max(rl => rl.RecoveryDate),
                    SourceSystem = g.Select(rl => rl.CallRecord != null ? rl.CallRecord.SourceSystem : null).FirstOrDefault()
                })
                .OrderBy(g => g.IndexNumber)
                .ThenBy(g => g.Currency)
                .ToListAsync();

            var financeDetails = new List<FinanceRecoveryDetail>();

            // Get user details for each recovery
            foreach (var recovery in financeData)
            {
                var user = await _context.EbillUsers
                    .FirstOrDefaultAsync(u => u.IndexNumber == recovery.IndexNumber);

                if (user != null)
                {
                    var staffName = $"{user.FirstName} {user.LastName}".Trim();

                    // Get organization name
                    var orgUnit = "";
                    if (user.OrganizationId.HasValue)
                    {
                        var org = await _context.Organizations.FindAsync(user.OrganizationId.Value);
                        orgUnit = org?.Name ?? user.OrganizationId.ToString() ?? "";
                    }

                    // Get office name
                    var office = "";
                    if (user.OfficeId.HasValue)
                    {
                        var officeEntity = await _context.Offices.FindAsync(user.OfficeId.Value);
                        office = officeEntity?.Name ?? user.OfficeId.ToString() ?? "";
                    }

                    // Get suboffice name
                    var subOffice = "";
                    if (user.SubOfficeId.HasValue)
                    {
                        var subOfficeEntity = await _context.SubOffices.FindAsync(user.SubOfficeId.Value);
                        subOffice = subOfficeEntity?.Name ?? user.SubOfficeId.ToString() ?? "";
                    }

                    financeDetails.Add(new FinanceRecoveryDetail
                    {
                        IndexNumber = recovery.IndexNumber ?? "",
                        StaffName = staffName,
                        OrgUnit = orgUnit,
                        Amount = recovery.TotalAmount,
                        Currency = recovery.Currency,
                        PhoneNumber = recovery.PhoneNumber ?? "",
                        Office = office,
                        SubOffice = subOffice,
                        EffectiveDate = recovery.RecoveryDate,
                        ExpirationDate = recovery.RecoveryDate.AddMonths(2)
                    });
                }
                else
                {
                    // User not found, add with index number only
                    financeDetails.Add(new FinanceRecoveryDetail
                    {
                        IndexNumber = recovery.IndexNumber ?? "",
                        StaffName = "Unknown User",
                        OrgUnit = "",
                        Amount = recovery.TotalAmount,
                        Currency = recovery.Currency,
                        PhoneNumber = recovery.PhoneNumber ?? "",
                        Office = "",
                        SubOffice = "",
                        EffectiveDate = recovery.RecoveryDate,
                        ExpirationDate = recovery.RecoveryDate.AddMonths(2)
                    });
                }
            }

            // Apply post-processing filters
            var filteredDetails = financeDetails.AsEnumerable();

            // Filter by Organization ID
            if (FilterOrganizationId.HasValue)
            {
                var orgIndexNumbers = await _context.EbillUsers
                    .Where(u => u.OrganizationId == FilterOrganizationId.Value)
                    .Select(u => u.IndexNumber)
                    .ToListAsync();
                filteredDetails = filteredDetails.Where(f => orgIndexNumbers.Contains(f.IndexNumber));
            }

            // Filter by Office ID
            if (FilterOfficeId.HasValue)
            {
                var officeIndexNumbers = await _context.EbillUsers
                    .Where(u => u.OfficeId == FilterOfficeId.Value)
                    .Select(u => u.IndexNumber)
                    .ToListAsync();
                filteredDetails = filteredDetails.Where(f => officeIndexNumbers.Contains(f.IndexNumber));
            }

            // Filter by SubOffice ID
            if (FilterSubOfficeId.HasValue)
            {
                var subOfficeIndexNumbers = await _context.EbillUsers
                    .Where(u => u.SubOfficeId == FilterSubOfficeId.Value)
                    .Select(u => u.IndexNumber)
                    .ToListAsync();
                filteredDetails = filteredDetails.Where(f => subOfficeIndexNumbers.Contains(f.IndexNumber));
            }

            // Search by Username or Index Number
            if (!string.IsNullOrEmpty(SearchText))
            {
                filteredDetails = filteredDetails.Where(f =>
                    f.StaffName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    f.IndexNumber.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
            }

            FinanceRecoveryDetails = filteredDetails.OrderBy(f => f.IndexNumber).ToList();
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

        // Batch-specific
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
