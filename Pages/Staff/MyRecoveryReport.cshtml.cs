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

        // User Information
        public string UserIndexNumber { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;

        // Summary Data
        public StaffRecoverySummary Summary { get; set; } = new();

        // Recovery Details
        public List<StaffRecoveryDetail> RecoveryDetails { get; set; } = new();

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

            if (ebillUser == null)
            {
                return RedirectToPage("/Error");
            }

            UserIndexNumber = ebillUser.IndexNumber;
            UserName = $"{ebillUser.FirstName} {ebillUser.LastName}";

            // Set default date range (last 6 months)
            if (!StartDate.HasValue)
                StartDate = DateTime.UtcNow.AddMonths(-6);
            if (!EndDate.HasValue)
                EndDate = DateTime.UtcNow;

            await LoadRecoverySummaryAsync();
            await LoadRecoveryDetailsAsync();
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

            RecoveryDetails = await query
                .OrderByDescending(rl => rl.RecoveryDate)
                .Select(rl => new StaffRecoveryDetail
                {
                    CallId = rl.CallRecordId,
                    CallDate = rl.CallRecord != null ? rl.CallRecord.CallDate : DateTime.MinValue,
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
            csv.AppendLine("Call Date,Call Number,Destination,Duration,Cost,Recovery Amount,Recovery Date,Reason");
            foreach (var detail in RecoveryDetails)
            {
                csv.AppendLine($"{detail.CallDate:yyyy-MM-dd},{detail.CallNumber},\"{detail.CallDestination}\",{detail.CallDuration},{detail.CallCost},{detail.RecoveryAmount},{detail.RecoveryDate:yyyy-MM-dd},\"{detail.RecoveryReason}\"");
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
}
