using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TAB.Web.Data;
using TAB.Web.Models;
using TAB.Web.Models.DTOs;
using TAB.Web.Services;

namespace TAB.Web.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class RecoveryDashboardModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<RecoveryDashboardModel> _logger;
        private readonly ICurrencyConversionService _currencyService;

        public RecoveryDashboardModel(
            ApplicationDbContext context,
            ILogger<RecoveryDashboardModel> logger,
            ICurrencyConversionService currencyService)
        {
            _context = context;
            _logger = logger;
            _currencyService = currencyService;
        }

        // Dashboard Metrics
        public DashboardMetrics Metrics { get; set; } = new();

        // Recovery Trends (Last 30 days)
        public List<DailyRecoveryTrend> RecoveryTrends { get; set; } = new();

        // Monthly Recovery Trends (Personal vs Official)
        public List<MonthlyRecoveryTrend> MonthlyRecoveryTrends { get; set; } = new();

        // Deadline Compliance
        public DeadlineComplianceMetrics DeadlineCompliance { get; set; } = new();

        // Top Batches by Recovery Amount
        public List<BatchRecoveryInfo> TopBatches { get; set; } = new();

        // Recent Recovery Activities
        public List<RecoveryActivityInfo> RecentActivities { get; set; } = new();

        // Recovery Type Breakdown
        public RecoveryBreakdown RecoveryBreakdown { get; set; } = new();

        // Enhanced Dashboard Properties
        public DashboardFilterParams Filters { get; set; } = new();
        public EnhancedDashboardMetrics EnhancedMetrics { get; set; } = new();
        public RecoveryByTypeDTO? RecoveryByType { get; set; }
        public List<RecoveryByProviderDTO> RecoveryByProvider { get; set; } = new();
        public List<RecoveryByOrganizationDTO> RecoveryByOrganization { get; set; } = new();
        public List<TopUserRecoveryDTO> TopUserRecoveries { get; set; } = new();
        public List<DashboardAlertDTO> Alerts { get; set; } = new();

        // Official Calls Metrics (Organization Paid)
        public int TotalOfficialCallsCount { get; set; }
        public decimal TotalOfficialAmountKSH { get; set; }
        public decimal TotalOfficialAmountUSD { get; set; }

        // Provider Summary (for first card)
        public Dictionary<string, ProviderSummary> ProviderSummaries { get; set; } = new();
        public int TotalPersonalCalls { get; set; }
        public decimal TotalPersonalAmountKSH { get; set; }
        public int TotalOfficialCallsAll { get; set; }
        public decimal TotalOfficialAmountKSHAll { get; set; }

        public class ProviderSummary
        {
            public string ProviderName { get; set; } = string.Empty;
            public int CallCount { get; set; }
            public decimal AmountKSH { get; set; }
            public decimal AmountUSD { get; set; }
            public string Currency { get; set; } = "KSH";
        }

        public async Task OnGetAsync(
            DateTime? startDate = null,
            DateTime? endDate = null,
            List<int>? organizationIds = null,
            List<int>? officeIds = null,
            string? userIndexNumber = null,
            List<string>? providers = null,
            List<string>? recoveryTypes = null)
        {
            // Initialize filters
            Filters.StartDate = startDate ?? DateTime.UtcNow.AddDays(-30);
            Filters.EndDate = endDate ?? DateTime.UtcNow;
            Filters.OrganizationIds = organizationIds ?? new List<int>();
            Filters.OfficeIds = officeIds ?? new List<int>();
            Filters.UserIndexNumber = userIndexNumber;
            Filters.Providers = providers ?? new List<string>();
            Filters.RecoveryTypes = recoveryTypes ?? new List<string>();

            // Load existing dashboard sections
            await LoadDashboardMetricsAsync();
            await LoadRecoveryTrendsAsync();
            await LoadDeadlineComplianceAsync();
            await LoadTopBatchesAsync();
            await LoadRecentActivitiesAsync();
            await LoadRecoveryBreakdownAsync();

            // Load enhanced dashboard sections
            await LoadEnhancedMetricsAsync();
            await LoadRecoveryByTypeAsync();
            await LoadRecoveryByProviderAsync();
            await LoadRecoveryByOrganizationAsync();
            await LoadTopUserRecoveriesAsync();
            await LoadAlertsAsync();
            await LoadOfficialCallsMetricsAsync();
            await LoadProviderSummaryAsync();
            await LoadMonthlyRecoveryTrendsAsync();
        }

        private async Task LoadDashboardMetricsAsync()
        {
            var last30Days = DateTime.UtcNow.AddDays(-30);
            var last7Days = DateTime.UtcNow.AddDays(-7);

            // Total amount recovered (all time)
            Metrics.TotalAmountRecovered = await _context.RecoveryJobExecutions
                .Where(e => e.Status == "Completed")
                .SumAsync(e => (decimal?)e.TotalAmountRecovered) ?? 0;

            // Amount recovered last 30 days
            Metrics.AmountRecoveredLast30Days = await _context.RecoveryJobExecutions
                .Where(e => e.Status == "Completed" && e.StartTime >= last30Days)
                .SumAsync(e => (decimal?)e.TotalAmountRecovered) ?? 0;

            // Amount recovered last 7 days
            Metrics.AmountRecoveredLast7Days = await _context.RecoveryJobExecutions
                .Where(e => e.Status == "Completed" && e.StartTime >= last7Days)
                .SumAsync(e => (decimal?)e.TotalAmountRecovered) ?? 0;

            // Total records processed
            Metrics.TotalRecordsProcessed = await _context.RecoveryJobExecutions
                .Where(e => e.Status == "Completed")
                .SumAsync(e => (int?)e.TotalRecordsProcessed) ?? 0;

            // Records processed last 30 days
            Metrics.RecordsProcessedLast30Days = await _context.RecoveryJobExecutions
                .Where(e => e.Status == "Completed" && e.StartTime >= last30Days)
                .SumAsync(e => (int?)e.TotalRecordsProcessed) ?? 0;

            // Total batches processed
            Metrics.TotalBatchesProcessed = await _context.StagingBatches
                .Where(b => b.RecoveryStatus == "Completed")
                .CountAsync();

            // Active batches
            Metrics.ActiveBatchesWithDeadlines = await _context.StagingBatches
                .Where(b => b.BatchStatus == BatchStatus.Processing ||
                            b.BatchStatus == BatchStatus.PartiallyVerified ||
                            b.BatchStatus == BatchStatus.Verified)
                .CountAsync();

            // Success rate
            var totalJobs = await _context.RecoveryJobExecutions
                .Where(e => e.Status == "Completed" || e.Status == "Failed")
                .CountAsync();

            if (totalJobs > 0)
            {
                var successfulJobs = await _context.RecoveryJobExecutions
                    .Where(e => e.Status == "Completed")
                    .CountAsync();
                Metrics.SuccessRate = (decimal)successfulJobs / totalJobs * 100;
            }

            // Average recovery per batch
            var batchesWithRecovery = await _context.StagingBatches
                .Where(b => b.TotalRecoveredAmount.HasValue && b.TotalRecoveredAmount > 0)
                .Select(b => b.TotalRecoveredAmount!.Value)
                .ToListAsync();

            if (batchesWithRecovery.Any())
            {
                Metrics.AverageRecoveryPerBatch = batchesWithRecovery.Average();
            }

            // Calculate trend percentages
            if (Metrics.AmountRecoveredLast30Days > 0)
            {
                var previous30Days = DateTime.UtcNow.AddDays(-60);
                var previous30DaysEnd = DateTime.UtcNow.AddDays(-30);
                var previousPeriodAmount = await _context.RecoveryJobExecutions
                    .Where(e => e.Status == "Completed" && e.StartTime >= previous30Days && e.StartTime < previous30DaysEnd)
                    .SumAsync(e => (decimal?)e.TotalAmountRecovered) ?? 0;

                if (previousPeriodAmount > 0)
                {
                    Metrics.TrendPercentage = ((Metrics.AmountRecoveredLast30Days - previousPeriodAmount) / previousPeriodAmount) * 100;
                }
            }
        }

        private async Task LoadRecoveryTrendsAsync()
        {
            var last30Days = DateTime.UtcNow.AddDays(-30).Date;

            var dailyRecoveries = await _context.RecoveryJobExecutions
                .Where(e => e.Status == "Completed" && e.StartTime >= last30Days)
                .GroupBy(e => e.StartTime.Date)
                .Select(g => new DailyRecoveryTrend
                {
                    Date = g.Key,
                    AmountRecovered = g.Sum(e => e.TotalAmountRecovered),
                    RecordsProcessed = g.Sum(e => e.TotalRecordsProcessed),
                    JobsRun = g.Count()
                })
                .OrderBy(t => t.Date)
                .ToListAsync();

            // Fill in missing dates with zeros
            var currentDate = last30Days;
            while (currentDate <= DateTime.UtcNow.Date)
            {
                if (!dailyRecoveries.Any(d => d.Date == currentDate))
                {
                    RecoveryTrends.Add(new DailyRecoveryTrend
                    {
                        Date = currentDate,
                        AmountRecovered = 0,
                        RecordsProcessed = 0,
                        JobsRun = 0
                    });
                }
                else
                {
                    RecoveryTrends.Add(dailyRecoveries.First(d => d.Date == currentDate));
                }
                currentDate = currentDate.AddDays(1);
            }
        }

        private async Task LoadDeadlineComplianceAsync()
        {
            var totalDeadlines = await _context.DeadlineTracking
                .Where(d => d.DeadlineType == "Verification" || d.DeadlineType == "Approval")
                .CountAsync();

            if (totalDeadlines > 0)
            {
                DeadlineCompliance.TotalDeadlines = totalDeadlines;

                DeadlineCompliance.MetDeadlines = await _context.DeadlineTracking
                    .Where(d => (d.DeadlineType == "Verification" || d.DeadlineType == "Approval") &&
                               d.DeadlineStatus == "Met")
                    .CountAsync();

                DeadlineCompliance.MissedDeadlines = await _context.DeadlineTracking
                    .Where(d => (d.DeadlineType == "Verification" || d.DeadlineType == "Approval") &&
                               d.DeadlineStatus == "Missed")
                    .CountAsync();

                DeadlineCompliance.ExtendedDeadlines = await _context.ImportAudits
                    .Where(a => a.ImportType == "Deadline Extension")
                    .CountAsync();

                DeadlineCompliance.ComplianceRate = (decimal)DeadlineCompliance.MetDeadlines / DeadlineCompliance.TotalDeadlines * 100;
            }

            // Current deadlines at risk (expiring in next 48 hours)
            var next48Hours = DateTime.UtcNow.AddHours(48);
            DeadlineCompliance.DeadlinesAtRisk = await _context.DeadlineTracking
                .Where(d => d.DeadlineDate > DateTime.UtcNow && d.DeadlineDate <= next48Hours &&
                           d.DeadlineStatus == "Pending")
                .CountAsync();
        }

        private async Task LoadTopBatchesAsync()
        {
            var topBatches = await _context.StagingBatches
                .Where(b => b.TotalRecoveredAmount.HasValue && b.TotalRecoveredAmount > 0)
                .OrderByDescending(b => b.TotalRecoveredAmount)
                .Take(10)
                .Select(b => new
                {
                    b.Id,
                    b.BatchName,
                    b.TotalRecoveredAmount,
                    b.TotalPersonalAmount,
                    b.TotalClassOfServiceAmount,
                    b.TotalRecords,
                    b.RecoveryProcessingDate,
                    b.SourceSystems
                })
                .ToListAsync();

            TopBatches = new List<BatchRecoveryInfo>();

            foreach (var batch in topBatches)
            {
                // Determine primary currency based on source system
                // PrivateWire uses USD, all others use KSH
                var primaryCurrency = "KSH";
                var containsPrivateWire = batch.SourceSystems?.Contains("PrivateWire", StringComparison.OrdinalIgnoreCase) == true;
                var containsOthers = batch.SourceSystems?.Contains("Safaricom", StringComparison.OrdinalIgnoreCase) == true ||
                                    batch.SourceSystems?.Contains("Airtel", StringComparison.OrdinalIgnoreCase) == true ||
                                    batch.SourceSystems?.Contains("PSTN", StringComparison.OrdinalIgnoreCase) == true;

                // If ONLY PrivateWire, primary is USD; otherwise KSH
                if (containsPrivateWire && !containsOthers)
                {
                    primaryCurrency = "USD";
                }

                decimal totalKSH = 0;
                decimal totalUSD = 0;
                var recoveredAmount = batch.TotalRecoveredAmount ?? 0;

                // Use the batch's stored total and convert to both currencies
                if (primaryCurrency == "KSH")
                {
                    totalKSH = recoveredAmount;
                    totalUSD = await _currencyService.ConvertCurrencyAsync(recoveredAmount, "KSH", "USD");
                }
                else
                {
                    totalUSD = recoveredAmount;
                    totalKSH = await _currencyService.ConvertCurrencyAsync(recoveredAmount, "USD", "KSH");
                }

                TopBatches.Add(new BatchRecoveryInfo
                {
                    BatchId = batch.Id,
                    BatchName = batch.BatchName,
                    TotalAmountRecovered = recoveredAmount,
                    TotalAmountKSH = totalKSH,
                    TotalAmountUSD = totalUSD,
                    PrimaryCurrency = primaryCurrency,
                    PersonalAmount = batch.TotalPersonalAmount ?? 0,
                    ClassOfServiceAmount = batch.TotalClassOfServiceAmount ?? 0,
                    TotalRecords = batch.TotalRecords,
                    RecoveryDate = batch.RecoveryProcessingDate
                });
            }
        }

        private async Task LoadRecentActivitiesAsync()
        {
            RecentActivities = await _context.RecoveryJobExecutions
                .Where(e => e.Status == "Completed")
                .OrderByDescending(e => e.EndTime)
                .Take(15)
                .Select(e => new RecoveryActivityInfo
                {
                    ExecutionId = e.Id,
                    ExecutionDate = e.EndTime ?? e.StartTime,
                    RunType = e.RunType ?? "Automatic",
                    TriggeredBy = e.TriggeredBy,
                    RecordsProcessed = e.TotalRecordsProcessed,
                    AmountRecovered = e.TotalAmountRecovered,
                    DurationMs = e.DurationMs ?? 0,
                    ExpiredVerifications = e.ExpiredVerificationsProcessed,
                    ExpiredApprovals = e.ExpiredApprovalsProcessed,
                    RevertedVerifications = e.RevertedVerificationsProcessed
                })
                .ToListAsync();
        }

        private async Task LoadRecoveryBreakdownAsync()
        {
            var completedExecutions = await _context.RecoveryJobExecutions
                .Where(e => e.Status == "Completed")
                .ToListAsync();

            if (completedExecutions.Any())
            {
                RecoveryBreakdown.ExpiredVerificationCount = completedExecutions.Sum(e => e.ExpiredVerificationsProcessed);
                RecoveryBreakdown.ExpiredApprovalCount = completedExecutions.Sum(e => e.ExpiredApprovalsProcessed);
                RecoveryBreakdown.RevertedVerificationCount = completedExecutions.Sum(e => e.RevertedVerificationsProcessed);
            }

            // Get amounts by type from batches
            var batches = await _context.StagingBatches
                .Where(b => b.TotalRecoveredAmount.HasValue && b.TotalRecoveredAmount > 0)
                .ToListAsync();

            if (batches.Any())
            {
                RecoveryBreakdown.PersonalAmountTotal = batches.Sum(b => b.TotalPersonalAmount ?? 0);
                RecoveryBreakdown.ClassOfServiceAmountTotal = batches.Sum(b => b.TotalClassOfServiceAmount ?? 0);
            }
        }

        private async Task LoadEnhancedMetricsAsync()
        {
            var filteredRecoveryLogs = GetFilteredRecoveryLogs();

            // Overall totals (all time)
            var allRecoveryLogs = await _context.RecoveryLogs
                .Include(r => r.CallRecord)
                .ToListAsync();

            decimal totalKSH = 0;
            decimal totalUSD = 0;

            foreach (var log in allRecoveryLogs)
            {
                var currency = log.CallRecord?.CallCurrencyCode?.ToUpper() ?? "KES";

                // Handle both KES (ISO code) and KSH (common abbreviation)
                if (currency == "KES" || currency == "KSH")
                {
                    totalKSH += log.AmountRecovered;
                    totalUSD += await _currencyService.ConvertCurrencyAsync(log.AmountRecovered, "KSH", "USD");
                }
                else if (currency == "USD")
                {
                    totalUSD += log.AmountRecovered;
                    totalKSH += await _currencyService.ConvertCurrencyAsync(log.AmountRecovered, "USD", "KSH");
                }
            }

            EnhancedMetrics.TotalAmountRecoveredKSH = totalKSH;
            EnhancedMetrics.TotalAmountRecoveredUSD = totalUSD;

            // Last 30 days with filters
            var last30DaysLogs = await filteredRecoveryLogs
                .Where(r => r.RecoveryDate >= Filters.StartDate && r.RecoveryDate <= Filters.EndDate)
                .Include(r => r.CallRecord)
                .ToListAsync();

            decimal last30KSH = 0;
            decimal last30USD = 0;

            foreach (var log in last30DaysLogs)
            {
                var currency = log.CallRecord?.CallCurrencyCode?.ToUpper() ?? "KES";

                // Handle both KES (ISO code) and KSH (common abbreviation)
                if (currency == "KES" || currency == "KSH")
                {
                    last30KSH += log.AmountRecovered;
                    last30USD += await _currencyService.ConvertCurrencyAsync(log.AmountRecovered, "KSH", "USD");
                }
                else if (currency == "USD")
                {
                    last30USD += log.AmountRecovered;
                    last30KSH += await _currencyService.ConvertCurrencyAsync(log.AmountRecovered, "USD", "KSH");
                }
            }

            EnhancedMetrics.AmountRecoveredLast30DaysKSH = last30KSH;
            EnhancedMetrics.AmountRecoveredLast30DaysUSD = last30USD;

            // Last 7 days
            var last7DaysLogs = await filteredRecoveryLogs
                .Where(r => r.RecoveryDate >= DateTime.UtcNow.AddDays(-7))
                .Include(r => r.CallRecord)
                .ToListAsync();

            decimal last7KSH = 0;
            decimal last7USD = 0;

            foreach (var log in last7DaysLogs)
            {
                var currency = log.CallRecord?.CallCurrencyCode?.ToUpper() ?? "KES";

                // Handle both KES (ISO code) and KSH (common abbreviation)
                if (currency == "KES" || currency == "KSH")
                {
                    last7KSH += log.AmountRecovered;
                    last7USD += await _currencyService.ConvertCurrencyAsync(log.AmountRecovered, "KSH", "USD");
                }
                else if (currency == "USD")
                {
                    last7USD += log.AmountRecovered;
                    last7KSH += await _currencyService.ConvertCurrencyAsync(log.AmountRecovered, "USD", "KSH");
                }
            }

            EnhancedMetrics.AmountRecoveredLast7DaysKSH = last7KSH;
            EnhancedMetrics.AmountRecoveredLast7DaysUSD = last7USD;

            // Copy existing metrics
            EnhancedMetrics.TotalRecordsProcessed = Metrics.TotalRecordsProcessed;
            EnhancedMetrics.RecordsProcessedLast30Days = Metrics.RecordsProcessedLast30Days;
            EnhancedMetrics.TotalBatchesProcessed = Metrics.TotalBatchesProcessed;
            EnhancedMetrics.ActiveBatchesWithDeadlines = Metrics.ActiveBatchesWithDeadlines;
            EnhancedMetrics.SuccessRate = Metrics.SuccessRate;
            EnhancedMetrics.TrendPercentage = Metrics.TrendPercentage;

            // Calculate average recovery per batch in both currencies
            if (Metrics.TotalBatchesProcessed > 0)
            {
                EnhancedMetrics.AverageRecoveryPerBatchKSH = totalKSH / Metrics.TotalBatchesProcessed;
                EnhancedMetrics.AverageRecoveryPerBatchUSD = totalUSD / Metrics.TotalBatchesProcessed;
            }
        }

        private async Task LoadRecoveryByTypeAsync()
        {
            // Get all call records that have been assigned (not "None")
            // Use FinalAssignmentType to determine the breakdown
            var allCalls = await _context.CallRecords
                .Where(c => c.FinalAssignmentType != null && c.FinalAssignmentType != "None")
                .ToListAsync();

            var dto = new RecoveryByTypeDTO();

            foreach (var call in allCalls)
            {
                var currency = call.CallCurrencyCode?.ToUpper() ?? "KES";
                decimal amountKSH = 0;
                decimal amountUSD = 0;

                // Handle both KES (ISO code) and KSH (common abbreviation)
                if (currency == "KES" || currency == "KSH")
                {
                    amountKSH = call.CallCostKSHS;
                    amountUSD = await _currencyService.ConvertCurrencyAsync(call.CallCostKSHS, "KSH", "USD");
                }
                else if (currency == "USD")
                {
                    amountUSD = call.CallCostUSD;
                    amountKSH = await _currencyService.ConvertCurrencyAsync(call.CallCostUSD, "USD", "KSH");
                }

                // Categorize by FinalAssignmentType
                switch (call.FinalAssignmentType?.ToLower())
                {
                    case "personal":
                        dto.PersonalKSH += amountKSH;
                        dto.PersonalUSD += amountUSD;
                        dto.PersonalCallCount++;
                        break;
                    case "official":
                        dto.OfficialKSH += amountKSH;
                        dto.OfficialUSD += amountUSD;
                        dto.OfficialCallCount++;
                        break;
                    case "classofservice":
                        dto.ClassOfServiceKSH += amountKSH;
                        dto.ClassOfServiceUSD += amountUSD;
                        dto.COSCallCount++;
                        break;
                }
            }

            // Calculate percentages
            if (dto.TotalCalls > 0)
            {
                dto.PersonalPercentage = (decimal)dto.PersonalCallCount / dto.TotalCalls * 100;
                dto.OfficialPercentage = (decimal)dto.OfficialCallCount / dto.TotalCalls * 100;
                dto.COSPercentage = (decimal)dto.COSCallCount / dto.TotalCalls * 100;
            }

            // Store breakdown in enhanced metrics
            EnhancedMetrics.PersonalRecoveryKSH = dto.PersonalKSH;
            EnhancedMetrics.PersonalRecoveryUSD = dto.PersonalUSD;
            EnhancedMetrics.PersonalRecoveryPercentage = dto.PersonalPercentage;

            EnhancedMetrics.OfficialRecoveryKSH = dto.OfficialKSH;
            EnhancedMetrics.OfficialRecoveryUSD = dto.OfficialUSD;
            EnhancedMetrics.OfficialRecoveryPercentage = dto.OfficialPercentage;

            EnhancedMetrics.COSRecoveryKSH = dto.ClassOfServiceKSH;
            EnhancedMetrics.COSRecoveryUSD = dto.ClassOfServiceUSD;
            EnhancedMetrics.COSRecoveryPercentage = dto.COSPercentage;

            RecoveryByType = dto;
        }

        private async Task LoadRecoveryByProviderAsync()
        {
            var filteredLogs = await GetFilteredRecoveryLogs()
                .Include(r => r.CallRecord)
                .ToListAsync();

            // Group by provider (SourceSystem)
            var providerGroups = filteredLogs
                .Where(r => r.CallRecord != null)
                .GroupBy(r => r.CallRecord!.SourceSystem ?? "Unknown");

            var providerList = new List<RecoveryByProviderDTO>();

            foreach (var group in providerGroups)
            {
                var provider = group.Key;
                var logs = group.ToList();

                // Determine native currency for this provider
                var nativeCurrency = provider.ToUpper() == "PRIVATEWIRE" ? "USD" : "KSH";

                decimal nativeAmount = 0;
                decimal totalKSH = 0;
                decimal totalUSD = 0;

                foreach (var log in logs)
                {
                    var currency = log.CallRecord?.CallCurrencyCode?.ToUpper() ?? "KES";
                    decimal amountKSH = 0;
                    decimal amountUSD = 0;

                    // Handle both KES (ISO code) and KSH (common abbreviation)
                    if (currency == "KES" || currency == "KSH")
                    {
                        amountKSH = log.AmountRecovered;
                        amountUSD = await _currencyService.ConvertCurrencyAsync(log.AmountRecovered, "KSH", "USD");
                    }
                    else if (currency == "USD")
                    {
                        amountUSD = log.AmountRecovered;
                        amountKSH = await _currencyService.ConvertCurrencyAsync(log.AmountRecovered, "USD", "KSH");
                    }

                    totalKSH += amountKSH;
                    totalUSD += amountUSD;
                }

                nativeAmount = nativeCurrency == "USD" ? totalUSD : totalKSH;

                providerList.Add(new RecoveryByProviderDTO
                {
                    Provider = provider,
                    NativeCurrency = nativeCurrency,
                    AmountInNativeCurrency = nativeAmount,
                    AmountInKSH = totalKSH,
                    AmountInUSD = totalUSD,
                    CallCount = logs.Count,
                    AvgPerCall = logs.Count > 0 ? nativeAmount / logs.Count : 0
                });
            }

            // Calculate percentages
            var grandTotal = providerList.Sum(p => p.AmountInKSH);
            if (grandTotal > 0)
            {
                foreach (var provider in providerList)
                {
                    provider.PercentageOfTotal = (provider.AmountInKSH / grandTotal) * 100;
                }
            }

            RecoveryByProvider = providerList.OrderByDescending(p => p.AmountInKSH).ToList();
        }

        private async Task LoadRecoveryByOrganizationAsync()
        {
            var filteredLogs = await GetFilteredRecoveryLogs()
                .Include(r => r.CallRecord)
                    .ThenInclude(c => c!.ResponsibleUser)
                        .ThenInclude(u => u!.OrganizationEntity)
                .ToListAsync();

            // Group by organization
            var orgGroups = filteredLogs
                .Where(r => r.CallRecord?.ResponsibleUser?.OrganizationEntity != null)
                .GroupBy(r => r.CallRecord!.ResponsibleUser!.OrganizationEntity);

            var orgList = new List<RecoveryByOrganizationDTO>();

            foreach (var group in orgGroups)
            {
                var org = group.Key;
                var logs = group.ToList();

                decimal totalKSH = 0;
                decimal totalUSD = 0;
                int personalCount = 0;
                int officialCount = 0;
                int cosCount = 0;

                foreach (var log in logs)
                {
                    var currency = log.CallRecord?.CallCurrencyCode?.ToUpper() ?? "KES";
                    decimal amountKSH = 0;
                    decimal amountUSD = 0;

                    // Handle both KES (ISO code) and KSH (common abbreviation)
                    if (currency == "KES" || currency == "KSH")
                    {
                        amountKSH = log.AmountRecovered;
                        amountUSD = await _currencyService.ConvertCurrencyAsync(log.AmountRecovered, "KSH", "USD");
                    }
                    else if (currency == "USD")
                    {
                        amountUSD = log.AmountRecovered;
                        amountKSH = await _currencyService.ConvertCurrencyAsync(log.AmountRecovered, "USD", "KSH");
                    }

                    totalKSH += amountKSH;
                    totalUSD += amountUSD;

                    // Count by type
                    switch (log.RecoveryAction?.ToLower())
                    {
                        case "personal":
                            personalCount++;
                            break;
                        case "official":
                            officialCount++;
                            break;
                        case "classofservice":
                        case "cos":
                            cosCount++;
                            break;
                    }
                }

                // Get unique user count
                var userCount = logs
                    .Where(r => r.RecoveredFrom != null)
                    .Select(r => r.RecoveredFrom)
                    .Distinct()
                    .Count();

                orgList.Add(new RecoveryByOrganizationDTO
                {
                    OrganizationId = org!.Id,
                    OrganizationName = org.Name,
                    TotalKSH = totalKSH,
                    TotalUSD = totalUSD,
                    PersonalCount = personalCount,
                    OfficialCount = officialCount,
                    COSCount = cosCount,
                    UserCount = userCount,
                    ComplianceRate = 0 // TODO: Calculate compliance rate
                });
            }

            RecoveryByOrganization = orgList.OrderByDescending(o => o.TotalKSH).ToList();
        }

        private async Task LoadTopUserRecoveriesAsync()
        {
            var filteredLogs = await GetFilteredRecoveryLogs()
                .Include(r => r.CallRecord)
                    .ThenInclude(c => c!.ResponsibleUser)
                        .ThenInclude(u => u!.OrganizationEntity)
                .Include(r => r.CallRecord)
                    .ThenInclude(c => c!.ResponsibleUser)
                        .ThenInclude(u => u!.OfficeEntity)
                .ToListAsync();

            // Group by user
            var userGroups = filteredLogs
                .Where(r => r.RecoveredFrom != null && r.CallRecord?.ResponsibleUser != null)
                .GroupBy(r => r.RecoveredFrom);

            var userList = new List<TopUserRecoveryDTO>();

            foreach (var group in userGroups)
            {
                var indexNumber = group.Key!;
                var logs = group.ToList();
                var user = logs.First().CallRecord?.ResponsibleUser;

                if (user == null) continue;

                decimal totalKSH = 0;
                decimal totalUSD = 0;
                int personalCalls = 0;
                int officialCalls = 0;
                int cosCalls = 0;

                foreach (var log in logs)
                {
                    var currency = log.CallRecord?.CallCurrencyCode?.ToUpper() ?? "KES";
                    decimal amountKSH = 0;
                    decimal amountUSD = 0;

                    // Handle both KES (ISO code) and KSH (common abbreviation)
                    if (currency == "KES" || currency == "KSH")
                    {
                        amountKSH = log.AmountRecovered;
                        amountUSD = await _currencyService.ConvertCurrencyAsync(log.AmountRecovered, "KSH", "USD");
                    }
                    else if (currency == "USD")
                    {
                        amountUSD = log.AmountRecovered;
                        amountKSH = await _currencyService.ConvertCurrencyAsync(log.AmountRecovered, "USD", "KSH");
                    }

                    totalKSH += amountKSH;
                    totalUSD += amountUSD;

                    // Count by type
                    switch (log.RecoveryAction?.ToLower())
                    {
                        case "personal":
                            personalCalls++;
                            break;
                        case "official":
                            officialCalls++;
                            break;
                        case "classofservice":
                        case "cos":
                            cosCalls++;
                            break;
                    }
                }

                userList.Add(new TopUserRecoveryDTO
                {
                    IndexNumber = indexNumber,
                    FullName = $"{user.FirstName} {user.LastName}",
                    OrganizationName = user.OrganizationEntity?.Name ?? "N/A",
                    OfficeName = user.OfficeEntity?.Name ?? "N/A",
                    TotalKSH = totalKSH,
                    TotalUSD = totalUSD,
                    CallCount = logs.Count(),
                    PersonalCalls = personalCalls,
                    OfficialCalls = officialCalls,
                    COSCalls = cosCalls
                });
            }

            // Rank and take top 20
            var ranked = userList
                .OrderByDescending(u => u.TotalKSH)
                .Take(20)
                .Select((u, index) =>
                {
                    u.Rank = index + 1;
                    return u;
                })
                .ToList();

            TopUserRecoveries = ranked;
        }

        private async Task LoadAlertsAsync()
        {
            var alerts = new List<DashboardAlertDTO>();

            // Check for pending recoveries
            var pendingRecoveries = await _context.CallRecords
                .Where(c => c.RecoveryStatus == "Pending")
                .CountAsync();

            if (pendingRecoveries > 0)
            {
                var pendingAmount = await _context.CallRecords
                    .Where(c => c.RecoveryStatus == "Pending")
                    .SumAsync(c => c.RecoveryAmount ?? 0);

                alerts.Add(new DashboardAlertDTO
                {
                    AlertType = "Pending Recoveries",
                    Priority = "Medium",
                    Message = $"{pendingRecoveries} call records pending recovery processing",
                    AffectedCount = pendingRecoveries,
                    AmountAtRisk = pendingAmount,
                    Icon = "bi-clock-history",
                    Link = "/Admin/CallLogs?filter=pending"
                });
            }

            // Check for upcoming deadlines
            var upcomingDeadlines = await _context.DeadlineTracking
                .Where(d => d.DeadlineDate > DateTime.UtcNow &&
                           d.DeadlineDate <= DateTime.UtcNow.AddDays(2) &&
                           d.DeadlineStatus == "Pending")
                .CountAsync();

            if (upcomingDeadlines > 0)
            {
                alerts.Add(new DashboardAlertDTO
                {
                    AlertType = "Upcoming Deadlines",
                    Priority = "High",
                    Message = $"{upcomingDeadlines} deadlines expiring in the next 48 hours",
                    AffectedCount = upcomingDeadlines,
                    AmountAtRisk = 0,
                    Icon = "bi-exclamation-triangle-fill",
                    Link = "/Admin/RecoveryDashboard#deadlines"
                });
            }

            Alerts = alerts;
        }

        private async Task LoadOfficialCallsMetricsAsync()
        {
            // Get all calls where FinalAssignmentType = "Official"
            // These are calls approved by supervisor as legitimate business calls
            // Organization pays for these - NOT recovered from staff
            var officialCalls = await _context.CallRecords
                .Where(c => c.FinalAssignmentType == "Official")
                .ToListAsync();

            TotalOfficialCallsCount = officialCalls.Count;

            // Calculate total amounts in both currencies
            decimal totalKSH = 0;
            decimal totalUSD = 0;

            foreach (var call in officialCalls)
            {
                var currency = call.CallCurrencyCode?.ToUpper() ?? "KES";

                // Handle both KES (ISO code) and KSH (common abbreviation)
                if (currency == "KES" || currency == "KSH")
                {
                    totalKSH += call.CallCostKSHS;
                    totalUSD += await _currencyService.ConvertCurrencyAsync(call.CallCostKSHS, "KSH", "USD");
                }
                else if (currency == "USD")
                {
                    totalUSD += call.CallCostUSD;
                    totalKSH += await _currencyService.ConvertCurrencyAsync(call.CallCostUSD, "USD", "KSH");
                }
            }

            TotalOfficialAmountKSH = totalKSH;
            TotalOfficialAmountUSD = totalUSD;
        }

        private async Task LoadProviderSummaryAsync()
        {
            // Get all processed call records (those with FinalAssignmentType set)
            var allProcessedCalls = await _context.CallRecords
                .Where(c => c.FinalAssignmentType != null && c.FinalAssignmentType != "None")
                .ToListAsync();

            // Group by provider (SourceSystem)
            var providerGroups = allProcessedCalls.GroupBy(c => c.SourceSystem ?? "Unknown");

            ProviderSummaries.Clear();

            // Calculate totals for each provider
            foreach (var group in providerGroups)
            {
                var provider = group.Key;
                var calls = group.ToList();

                decimal totalKSH = 0;
                decimal totalUSD = 0;

                foreach (var call in calls)
                {
                    var currency = call.CallCurrencyCode?.ToUpper() ?? "KES";

                    // Handle both KES (ISO code) and KSH (common abbreviation)
                    if (currency == "KES" || currency == "KSH")
                    {
                        totalKSH += call.CallCostKSHS;
                        totalUSD += await _currencyService.ConvertCurrencyAsync(call.CallCostKSHS, "KSH", "USD");
                    }
                    else if (currency == "USD")
                    {
                        totalUSD += call.CallCostUSD;
                        totalKSH += await _currencyService.ConvertCurrencyAsync(call.CallCostUSD, "USD", "KSH");
                    }
                }

                // Determine native currency for this provider
                var nativeCurrency = provider.ToUpper() == "PRIVATEWIRE" ? "USD" : "KSH";

                ProviderSummaries[provider] = new ProviderSummary
                {
                    ProviderName = provider,
                    CallCount = calls.Count,
                    AmountKSH = totalKSH,
                    AmountUSD = totalUSD,
                    Currency = nativeCurrency
                };
            }

            // Calculate Personal vs Official totals across all providers
            var personalCalls = allProcessedCalls.Where(c => c.FinalAssignmentType == "Personal").ToList();
            var officialCalls = allProcessedCalls.Where(c => c.FinalAssignmentType == "Official").ToList();

            TotalPersonalCalls = personalCalls.Count;
            TotalPersonalAmountKSH = 0;

            foreach (var call in personalCalls)
            {
                var currency = call.CallCurrencyCode?.ToUpper() ?? "KES";

                if (currency == "KES" || currency == "KSH")
                {
                    TotalPersonalAmountKSH += call.CallCostKSHS;
                }
                else if (currency == "USD")
                {
                    TotalPersonalAmountKSH += await _currencyService.ConvertCurrencyAsync(call.CallCostUSD, "USD", "KSH");
                }
            }

            TotalOfficialCallsAll = officialCalls.Count;
            TotalOfficialAmountKSHAll = 0;

            foreach (var call in officialCalls)
            {
                var currency = call.CallCurrencyCode?.ToUpper() ?? "KES";

                if (currency == "KES" || currency == "KSH")
                {
                    TotalOfficialAmountKSHAll += call.CallCostKSHS;
                }
                else if (currency == "USD")
                {
                    TotalOfficialAmountKSHAll += await _currencyService.ConvertCurrencyAsync(call.CallCostUSD, "USD", "KSH");
                }
            }
        }

        private async Task LoadMonthlyRecoveryTrendsAsync()
        {
            // Get data from the last 12 months based on billing period (call_year/call_month)
            var twelveMonthsAgo = DateTime.UtcNow.AddMonths(-12);
            var cutoffYear = twelveMonthsAgo.Year;
            var cutoffMonth = twelveMonthsAgo.Month;

            // Get all processed call records with recovery data using call_year/call_month (billing period)
            var callRecords = await _context.CallRecords
                .Where(c => c.FinalAssignmentType != null &&
                           c.FinalAssignmentType != "None" &&
                           (c.CallYear > cutoffYear || (c.CallYear == cutoffYear && c.CallMonth >= cutoffMonth)))
                .Select(c => new
                {
                    c.CallYear,
                    c.CallMonth,
                    c.FinalAssignmentType,
                    c.CallCostKSHS,
                    c.CallCostUSD,
                    c.CallCurrencyCode
                })
                .ToListAsync();

            // Group by billing month (call_year/call_month) and assignment type
            var monthlyData = callRecords
                .GroupBy(c => new
                {
                    Year = c.CallYear,
                    Month = c.CallMonth
                })
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Month,
                    PersonalCalls = g.Where(c => c.FinalAssignmentType == "Personal").ToList(),
                    OfficialCalls = g.Where(c => c.FinalAssignmentType == "Official").ToList()
                })
                .OrderBy(g => g.Year)
                .ThenBy(g => g.Month)
                .ToList();

            MonthlyRecoveryTrends.Clear();

            foreach (var monthData in monthlyData)
            {
                decimal personalKSH = 0;
                decimal officialKSH = 0;

                // Calculate Personal amount in KSH
                foreach (var call in monthData.PersonalCalls)
                {
                    var currency = call.CallCurrencyCode?.ToUpper() ?? "KES";
                    if (currency == "KES" || currency == "KSH")
                    {
                        personalKSH += call.CallCostKSHS;
                    }
                    else if (currency == "USD")
                    {
                        personalKSH += await _currencyService.ConvertCurrencyAsync(call.CallCostUSD, "USD", "KSH");
                    }
                }

                // Calculate Official amount in KSH
                foreach (var call in monthData.OfficialCalls)
                {
                    var currency = call.CallCurrencyCode?.ToUpper() ?? "KES";
                    if (currency == "KES" || currency == "KSH")
                    {
                        officialKSH += call.CallCostKSHS;
                    }
                    else if (currency == "USD")
                    {
                        officialKSH += await _currencyService.ConvertCurrencyAsync(call.CallCostUSD, "USD", "KSH");
                    }
                }

                var monthName = new DateTime(monthData.Year, monthData.Month, 1).ToString("MMM yyyy");

                MonthlyRecoveryTrends.Add(new MonthlyRecoveryTrend
                {
                    MonthYear = monthName,
                    Year = monthData.Year,
                    Month = monthData.Month,
                    PersonalAmountKSH = personalKSH,
                    OfficialAmountKSH = officialKSH,
                    PersonalCallCount = monthData.PersonalCalls.Count,
                    OfficialCallCount = monthData.OfficialCalls.Count
                });
            }
        }

        private IQueryable<RecoveryLog> GetFilteredRecoveryLogs()
        {
            var query = _context.RecoveryLogs
                .Where(r => r.RecoveryDate >= Filters.StartDate && r.RecoveryDate <= Filters.EndDate);

            // Filter by organization
            if (Filters.OrganizationIds.Any())
            {
                query = query.Where(r => r.CallRecord!.ResponsibleUser!.OrganizationId != null &&
                                        Filters.OrganizationIds.Contains(r.CallRecord.ResponsibleUser.OrganizationId.Value));
            }

            // Filter by office
            if (Filters.OfficeIds.Any())
            {
                query = query.Where(r => r.CallRecord!.ResponsibleUser!.OfficeId != null &&
                                        Filters.OfficeIds.Contains(r.CallRecord.ResponsibleUser.OfficeId.Value));
            }

            // Filter by user
            if (!string.IsNullOrEmpty(Filters.UserIndexNumber))
            {
                query = query.Where(r => r.RecoveredFrom == Filters.UserIndexNumber);
            }

            // Filter by provider
            if (Filters.Providers.Any())
            {
                query = query.Where(r => r.CallRecord!.SourceSystem != null &&
                                        Filters.Providers.Contains(r.CallRecord.SourceSystem));
            }

            // Filter by recovery type
            if (Filters.RecoveryTypes.Any())
            {
                query = query.Where(r => Filters.RecoveryTypes.Contains(r.RecoveryAction));
            }

            return query;
        }
    }

    // Data Transfer Objects
    public class DashboardMetrics
    {
        public decimal TotalAmountRecovered { get; set; }
        public decimal AmountRecoveredLast30Days { get; set; }
        public decimal AmountRecoveredLast7Days { get; set; }
        public int TotalRecordsProcessed { get; set; }
        public int RecordsProcessedLast30Days { get; set; }
        public int TotalBatchesProcessed { get; set; }
        public int ActiveBatchesWithDeadlines { get; set; }
        public decimal SuccessRate { get; set; }
        public decimal AverageRecoveryPerBatch { get; set; }
        public decimal TrendPercentage { get; set; }
    }

    public class DailyRecoveryTrend
    {
        public DateTime Date { get; set; }
        public decimal AmountRecovered { get; set; }
        public int RecordsProcessed { get; set; }
        public int JobsRun { get; set; }
    }

    public class MonthlyRecoveryTrend
    {
        public string MonthYear { get; set; } = string.Empty; // e.g., "Jan 2024"
        public int Year { get; set; }
        public int Month { get; set; }
        public decimal PersonalAmountKSH { get; set; }
        public decimal OfficialAmountKSH { get; set; }
        public int PersonalCallCount { get; set; }
        public int OfficialCallCount { get; set; }
    }

    public class DeadlineComplianceMetrics
    {
        public int TotalDeadlines { get; set; }
        public int MetDeadlines { get; set; }
        public int MissedDeadlines { get; set; }
        public int ExtendedDeadlines { get; set; }
        public int DeadlinesAtRisk { get; set; }
        public decimal ComplianceRate { get; set; }
    }

    public class BatchRecoveryInfo
    {
        public Guid BatchId { get; set; }
        public string BatchName { get; set; } = string.Empty;
        public decimal TotalAmountRecovered { get; set; }
        public decimal TotalAmountKSH { get; set; }
        public decimal TotalAmountUSD { get; set; }
        public string PrimaryCurrency { get; set; } = "KSH";
        public decimal PersonalAmount { get; set; }
        public decimal ClassOfServiceAmount { get; set; }
        public int TotalRecords { get; set; }
        public DateTime? RecoveryDate { get; set; }
    }

    public class RecoveryActivityInfo
    {
        public int ExecutionId { get; set; }
        public DateTime ExecutionDate { get; set; }
        public string RunType { get; set; } = string.Empty;
        public string? TriggeredBy { get; set; }
        public int RecordsProcessed { get; set; }
        public decimal AmountRecovered { get; set; }
        public long DurationMs { get; set; }
        public int ExpiredVerifications { get; set; }
        public int ExpiredApprovals { get; set; }
        public int RevertedVerifications { get; set; }
    }

    public class RecoveryBreakdown
    {
        public int ExpiredVerificationCount { get; set; }
        public int ExpiredApprovalCount { get; set; }
        public int RevertedVerificationCount { get; set; }
        public decimal PersonalAmountTotal { get; set; }
        public decimal ClassOfServiceAmountTotal { get; set; }
    }
}
