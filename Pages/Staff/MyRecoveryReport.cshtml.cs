using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TAB.Web.Data;
using TAB.Web.Models;

namespace TAB.Web.Pages.Staff
{
    [Authorize] // All authenticated users can access
    public class MyRecoveryReportModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<MyRecoveryReportModel> _logger;

        public MyRecoveryReportModel(
            ApplicationDbContext context,
            ILogger<MyRecoveryReportModel> logger)
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
        public string? RecoveryType { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool HideZeroCost { get; set; }

        // User Information
        public string UserIndexNumber { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;

        // Summary Data
        public StaffRecoverySummary Summary { get; set; } = new();

        // Recovery Details (flat list - for backwards compatibility)
        public List<StaffRecoveryDetail> RecoveryDetails { get; set; } = new();

        // Grouped Recovery Data (for 2-level table view)
        public List<GroupedMonthlyRecovery> GroupedRecoveryData { get; set; } = new();

        // Monthly Breakdown
        public List<MonthlyRecovery> MonthlyBreakdown { get; set; } = new();

        // Recent Verifications
        public List<RecentVerification> RecentVerifications { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            // Get user information
            UserEmail = User.Identity?.Name ?? string.Empty;

            var ebillUser = await _context.EbillUsers
                .FirstOrDefaultAsync(u => u.Email == UserEmail);

            if (ebillUser != null)
            {
                UserIndexNumber = ebillUser.IndexNumber;
                UserName = $"{ebillUser.FirstName} {ebillUser.LastName}";
            }
            else
            {
                // User doesn't have an EbillUser account - allow access but they'll see no records
                _logger.LogInformation("User {Email} accessing recovery report without EbillUser account - will show empty results", UserEmail);
                UserIndexNumber = string.Empty;
                UserName = UserEmail;
            }

            // Set default date range (last 6 months)
            if (!StartDate.HasValue)
                StartDate = DateTime.UtcNow.AddMonths(-6);
            if (!EndDate.HasValue)
                EndDate = DateTime.UtcNow;

            await LoadRecoverySummaryAsync();
            await LoadRecoveryDetailsAsync();
            await LoadGroupedRecoveryDataAsync(); // New: Load grouped data for 2-level view
            await LoadMonthlyBreakdownAsync();
            await LoadRecentVerificationsAsync();

            return Page();
        }

        private async Task LoadRecoverySummaryAsync()
        {
            // Load from RecoveryLogs which has the correct amounts
            var query = _context.RecoveryLogs
                .Include(rl => rl.CallRecord)
                .Where(rl => rl.RecoveredFrom == UserIndexNumber);

            // Apply date filters
            if (StartDate.HasValue)
                query = query.Where(rl => rl.RecoveryDate >= StartDate.Value);
            if (EndDate.HasValue)
                query = query.Where(rl => rl.RecoveryDate <= EndDate.Value);

            if (HideZeroCost)
                query = query.Where(rl => rl.AmountRecovered > 0);

            var recoveryLogs = await query.ToListAsync();

            if (recoveryLogs.Any())
            {
                Summary.TotalAmountRecovered = recoveryLogs.Sum(rl => rl.AmountRecovered);
                Summary.TotalRecordsRecovered = recoveryLogs.Count;
                Summary.PersonalAmount = recoveryLogs
                    .Where(rl => rl.RecoveryAction == "Personal")
                    .Sum(rl => rl.AmountRecovered);
                Summary.ClassOfServiceAmount = recoveryLogs
                    .Where(rl => rl.RecoveryAction == "ClassOfService")
                    .Sum(rl => rl.AmountRecovered);

                Summary.ExpiredVerifications = recoveryLogs
                    .Count(rl => rl.RecoveryType == "StaffNonVerification");
                Summary.ExpiredApprovals = recoveryLogs
                    .Count(rl => rl.RecoveryType == "SupervisorNonApproval");
                Summary.RevertedVerifications = recoveryLogs
                    .Count(rl => rl.RecoveryType == "SupervisorRevertFailure");
            }

            // Get current pending verifications
            Summary.PendingVerifications = await _context.CallRecords
                .Where(c => c.ResponsibleIndexNumber == UserIndexNumber &&
                           !c.IsVerified &&
                           c.VerificationPeriod.HasValue &&
                           c.VerificationPeriod > DateTime.UtcNow)
                .CountAsync();

            // Get missed deadlines (not yet recovered)
            Summary.MissedDeadlines = await _context.CallRecords
                .Where(c => c.ResponsibleIndexNumber == UserIndexNumber &&
                           !c.IsVerified &&
                           c.VerificationPeriod.HasValue &&
                           c.VerificationPeriod < DateTime.UtcNow &&
                           (!c.RecoveryAmount.HasValue || c.RecoveryAmount == 0))
                .CountAsync();
        }

        private async Task LoadRecoveryDetailsAsync()
        {
            // Load from RecoveryLogs which has the correct amounts
            var query = _context.RecoveryLogs
                .Include(rl => rl.CallRecord)
                .Where(rl => rl.RecoveredFrom == UserIndexNumber);

            // Apply filters
            if (StartDate.HasValue)
                query = query.Where(rl => rl.RecoveryDate >= StartDate.Value);
            if (EndDate.HasValue)
                query = query.Where(rl => rl.RecoveryDate <= EndDate.Value);
            if (!string.IsNullOrEmpty(RecoveryType))
                query = query.Where(rl => rl.RecoveryType == RecoveryType);
            if (HideZeroCost)
                query = query.Where(rl => rl.AmountRecovered > 0);

            RecoveryDetails = await query
                .OrderByDescending(rl => rl.RecoveryDate)
                .Select(rl => new StaffRecoveryDetail
                {
                    CallId = rl.CallRecordId,
                    CallDate = rl.CallRecord != null ? rl.CallRecord.CallDate : DateTime.MinValue,
                    CallMonth = rl.CallRecord != null ? rl.CallRecord.CallMonth : 0,
                    CallYear = rl.CallRecord != null ? rl.CallRecord.CallYear : 0,
                    CallNumber = rl.CallRecord != null ? rl.CallRecord.CallNumber : "",
                    CallDestination = rl.CallRecord != null ? rl.CallRecord.CallDestination : "",
                    CallDuration = rl.CallRecord != null ? rl.CallRecord.CallDuration : 0,
                    CallCost = rl.CallRecord != null ? rl.CallRecord.CallCost : 0, // Original currency amount
                    Currency = rl.CallRecord != null ? (rl.CallRecord.CallCurrencyCode ?? "KES") : "KES",
                    RecoveryAmount = rl.AmountRecovered, // From RecoveryLogs
                    RecoveryDate = rl.RecoveryDate,
                    RecoveryReason = rl.RecoveryReason ?? rl.RecoveryType, // Use descriptive reason, fallback to type
                    RecoveryType = rl.RecoveryType, // Keep type for filtering/categorization
                    FinalAssignment = rl.RecoveryAction,
                    VerificationPeriod = rl.CallRecord != null ? rl.CallRecord.VerificationPeriod : null
                })
                .Take(100)
                .ToListAsync();
        }

        private async Task LoadGroupedRecoveryDataAsync()
        {
            // Load from RecoveryLogs which has the correct amounts
            var query = _context.RecoveryLogs
                .Include(rl => rl.CallRecord)
                .Where(rl => rl.RecoveredFrom == UserIndexNumber);

            // Apply filters
            if (StartDate.HasValue)
                query = query.Where(rl => rl.RecoveryDate >= StartDate.Value);
            if (EndDate.HasValue)
                query = query.Where(rl => rl.RecoveryDate <= EndDate.Value);
            if (!string.IsNullOrEmpty(RecoveryType))
                query = query.Where(rl => rl.RecoveryType == RecoveryType);
            if (HideZeroCost)
                query = query.Where(rl => rl.AmountRecovered > 0);

            var allRecoveries = await query
                .OrderByDescending(rl => rl.RecoveryDate)
                .ToListAsync();

            // Group by CallMonth and CallYear
            GroupedRecoveryData = allRecoveries
                .Where(rl => rl.CallRecord != null)
                .GroupBy(rl => new { rl.CallRecord!.CallYear, rl.CallRecord.CallMonth })
                .OrderByDescending(g => g.Key.CallYear)
                .ThenByDescending(g => g.Key.CallMonth)
                .Select(g => new GroupedMonthlyRecovery
                {
                    Month = g.Key.CallMonth,
                    Year = g.Key.CallYear,
                    GroupId = $"{g.Key.CallYear}-{g.Key.CallMonth}",
                    TotalCalls = g.Count(),

                    // Calculate totals by currency
                    TotalUSD = g.Where(rl => rl.CallRecord!.CallCurrencyCode == "USD")
                        .Sum(rl => rl.AmountRecovered),
                    TotalKES = g.Where(rl => rl.CallRecord!.CallCurrencyCode == "KES")
                        .Sum(rl => rl.AmountRecovered),

                    // Count by recovery type
                    StaffNonVerificationCount = g.Count(rl => rl.RecoveryType == "StaffNonVerification"),
                    SupervisorNonApprovalCount = g.Count(rl => rl.RecoveryType == "SupervisorNonApproval"),
                    SupervisorRevertFailureCount = g.Count(rl => rl.RecoveryType == "SupervisorRevertFailure"),
                    SupervisorRejectionCount = g.Count(rl => rl.RecoveryType == "SupervisorRejection"),
                    SupervisorPartialApprovalCount = g.Count(rl => rl.RecoveryType == "SupervisorPartialApproval"),
                    ManualOverrideCount = g.Count(rl => rl.RecoveryType == "ManualOverride"),

                    // Individual recovery details for this month
                    Details = g.Select(rl => new StaffRecoveryDetail
                    {
                        CallId = rl.CallRecordId,
                        CallDate = rl.CallRecord!.CallDate,
                        CallMonth = rl.CallRecord.CallMonth,
                        CallYear = rl.CallRecord.CallYear,
                        CallNumber = rl.CallRecord.CallNumber,
                        CallDestination = rl.CallRecord.CallDestination,
                        CallDuration = rl.CallRecord.CallDuration,
                        CallCost = rl.CallRecord.CallCost,
                        Currency = rl.CallRecord.CallCurrencyCode ?? "KES",
                        RecoveryAmount = rl.AmountRecovered,
                        RecoveryDate = rl.RecoveryDate,
                        RecoveryReason = rl.RecoveryReason ?? rl.RecoveryType,
                        RecoveryType = rl.RecoveryType,
                        FinalAssignment = rl.RecoveryAction,
                        VerificationPeriod = rl.CallRecord.VerificationPeriod
                    }).ToList()
                })
                .ToList();
        }

        private async Task LoadMonthlyBreakdownAsync()
        {
            // Load from RecoveryLogs which has the correct amounts
            var recoveries = await _context.RecoveryLogs
                .Include(rl => rl.CallRecord)
                .Where(rl => rl.RecoveredFrom == UserIndexNumber)
                .Where(rl => rl.RecoveryDate >= StartDate && rl.RecoveryDate <= EndDate)
                .ToListAsync();

            MonthlyBreakdown = recoveries
                .Where(rl => rl.CallRecord != null)
                .GroupBy(rl => new { rl.CallRecord!.CallYear, rl.CallRecord.CallMonth })
                .Select(g => new MonthlyRecovery
                {
                    Year = g.Key.CallYear,
                    Month = g.Key.CallMonth,
                    TotalAmount = g.Sum(rl => rl.AmountRecovered),
                    RecordCount = g.Count(),
                    PersonalAmount = g.Where(rl => rl.RecoveryAction == "Personal").Sum(rl => rl.AmountRecovered),
                    ClassOfServiceAmount = g.Where(rl => rl.RecoveryAction == "ClassOfService").Sum(rl => rl.AmountRecovered)
                })
                .OrderByDescending(m => m.Year)
                .ThenByDescending(m => m.Month)
                .ToList();
        }

        private async Task LoadRecentVerificationsAsync()
        {
            // Get recent verifications from call records
            var recentCalls = await _context.CallRecords
                .Where(c => c.ResponsibleIndexNumber == UserIndexNumber &&
                           c.IsVerified)
                .OrderByDescending(c => c.VerificationDate)
                .Take(50)
                .ToListAsync();

            // Group by verification date (approximate - same day)
            var verificationGroups = recentCalls
                .Where(c => c.VerificationDate.HasValue)
                .GroupBy(c => c.VerificationDate!.Value.Date)
                .OrderByDescending(g => g.Key)
                .Take(10)
                .Select(g => new RecentVerification
                {
                    VerificationId = 0,
                    VerificationDate = g.Key,
                    TotalCalls = g.Count(),
                    PersonalCalls = g.Count(c => c.VerificationType == "Personal"),
                    OfficialCalls = g.Count(c => c.VerificationType == "Official"),
                    TotalAmount = g.Sum(c => c.CallCostUSD),
                    Status = g.Any(c => c.SupervisorApprovalStatus == "Approved") ? "Approved" :
                             g.Any(c => c.SupervisorApprovalStatus == "Rejected") ? "Rejected" :
                             g.Any(c => c.SupervisorApprovalStatus == "Pending") ? "Pending" : "Verified",
                    SubmittedToSupervisor = g.Any(c => !string.IsNullOrEmpty(c.SupervisorApprovalStatus))
                })
                .ToList();

            RecentVerifications = verificationGroups;
        }

        public async Task<IActionResult> OnPostExportAsync()
        {
            await OnGetAsync();

            var csv = new System.Text.StringBuilder();

            // Header
            csv.AppendLine("My Recovery Report");
            csv.AppendLine($"Name,{UserName}");
            csv.AppendLine($"Index Number,{UserIndexNumber}");
            csv.AppendLine($"Period,{StartDate:yyyy-MM-dd} to {EndDate:yyyy-MM-dd}");
            csv.AppendLine($"Total Recovered,{Summary.TotalAmountRecovered:C}");
            csv.AppendLine();

            // Recovery Details
            csv.AppendLine("Recovery Details");
            csv.AppendLine("Call Date,Month,Year,Call Number,Destination,Duration,Cost,Currency,Recovery Amount,Recovery Date,Reason");
            foreach (var detail in RecoveryDetails)
            {
                var monthName = new DateTime(detail.CallYear, detail.CallMonth, 1).ToString("MMM");
                csv.AppendLine($"{detail.CallDate:yyyy-MM-dd},{monthName},{detail.CallYear},{detail.CallNumber},\"{detail.CallDestination}\",{detail.CallDuration},{detail.CallCost},{detail.Currency},{detail.RecoveryAmount},{detail.RecoveryDate:yyyy-MM-dd},\"{detail.RecoveryReason}\"");
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
            return File(bytes, "text/csv", $"MyRecoveryReport_{DateTime.UtcNow:yyyyMMdd}.csv");
        }
    }

    // DTOs
    public class StaffRecoverySummary
    {
        public decimal TotalAmountRecovered { get; set; }
        public int TotalRecordsRecovered { get; set; }
        public decimal PersonalAmount { get; set; }
        public decimal ClassOfServiceAmount { get; set; }
        public int ExpiredVerifications { get; set; }
        public int ExpiredApprovals { get; set; }
        public int RevertedVerifications { get; set; }
        public int PendingVerifications { get; set; }
        public int MissedDeadlines { get; set; }
    }

    public class StaffRecoveryDetail
    {
        public int CallId { get; set; }
        public DateTime CallDate { get; set; }
        public int CallMonth { get; set; }
        public int CallYear { get; set; }
        public string CallNumber { get; set; } = string.Empty;
        public string CallDestination { get; set; } = string.Empty;
        public int CallDuration { get; set; }
        public decimal CallCost { get; set; }
        public string Currency { get; set; } = "KES"; // Currency of the call
        public decimal RecoveryAmount { get; set; }
        public DateTime? RecoveryDate { get; set; }
        public string? RecoveryReason { get; set; } // Descriptive reason shown to user
        public string? RecoveryType { get; set; } // Type code for categorization
        public string? FinalAssignment { get; set; }
        public DateTime? VerificationPeriod { get; set; }
    }

    public class MonthlyRecovery
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public decimal TotalAmount { get; set; }
        public int RecordCount { get; set; }
        public decimal PersonalAmount { get; set; }
        public decimal ClassOfServiceAmount { get; set; }

        public string MonthName => new DateTime(Year, Month, 1).ToString("MMM yyyy");
    }

    public class RecentVerification
    {
        public int VerificationId { get; set; }
        public DateTime VerificationDate { get; set; }
        public int TotalCalls { get; set; }
        public int PersonalCalls { get; set; }
        public int OfficialCalls { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool SubmittedToSupervisor { get; set; }
    }

    public class GroupedMonthlyRecovery
    {
        public string GroupId { get; set; } = string.Empty;
        public int Month { get; set; }
        public int Year { get; set; }
        public int TotalCalls { get; set; }
        public decimal TotalUSD { get; set; }
        public decimal TotalKES { get; set; }

        // Recovery type counts
        public int StaffNonVerificationCount { get; set; }
        public int SupervisorNonApprovalCount { get; set; }
        public int SupervisorRevertFailureCount { get; set; }
        public int SupervisorRejectionCount { get; set; }
        public int SupervisorPartialApprovalCount { get; set; }
        public int ManualOverrideCount { get; set; }

        // Individual recovery details for this month
        public List<StaffRecoveryDetail> Details { get; set; } = new();

        // Helper properties
        public string MonthName => new DateTime(Year, Month, 1).ToString("MMMM yyyy");
        public string MonthShort => new DateTime(Year, Month, 1).ToString("MMM");
    }
}
