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

        // Cached exchange rate to avoid repeated lookups
        private decimal _kshToUsdRate;
        private decimal _usdToKshRate;

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

            // Cache exchange rates upfront to avoid repeated lookups
            await CacheExchangeRatesAsync();

            // Run queries sequentially - DbContext is not thread-safe
            await LoadDashboardMetricsAsync();
            await LoadRecoveryTrendsAsync();
            await LoadDeadlineComplianceAsync();
            await LoadTopBatchesAsync();
            await LoadRecentActivitiesAsync();
            await LoadRecoveryBreakdownAsync();
            await LoadAlertsAsync();

            // These depend on base metrics being loaded
            await LoadEnhancedMetricsAsync();

            // Continue with remaining queries
            await LoadRecoveryByTypeAsync();
            await LoadRecoveryByProviderAsync();
            await LoadRecoveryByOrganizationAsync();
            await LoadTopUserRecoveriesAsync();
            await LoadOfficialCallsMetricsAsync();
            await LoadProviderSummaryAsync();
            await LoadMonthlyRecoveryTrendsAsync();
        }

        private async Task CacheExchangeRatesAsync()
        {
            _kshToUsdRate = await _currencyService.ConvertCurrencyAsync(1, "KSH", "USD");
            _usdToKshRate = await _currencyService.ConvertCurrencyAsync(1, "USD", "KSH");
        }

        private decimal ConvertToUsd(decimal amount, string currency)
        {
            if (currency == "USD") return amount;
            return amount * _kshToUsdRate;
        }

        private decimal ConvertToKsh(decimal amount, string currency)
        {
            if (currency == "KES" || currency == "KSH") return amount;
            return amount * _usdToKshRate;
        }

        private async Task LoadDashboardMetricsAsync()
        {
            var last30Days = DateTime.UtcNow.AddDays(-30);
            var last7Days = DateTime.UtcNow.AddDays(-7);
            var previous30DaysStart = DateTime.UtcNow.AddDays(-60);

            // Single query with aggregation for job executions
            var jobStats = await _context.RecoveryJobExecutions
                .AsNoTracking()
                .Where(e => e.Status == "Completed")
                .GroupBy(e => 1)
                .Select(g => new
                {
                    TotalAmount = g.Sum(e => (decimal?)e.TotalAmountRecovered) ?? 0,
                    Last30DaysAmount = g.Where(e => e.StartTime >= last30Days).Sum(e => (decimal?)e.TotalAmountRecovered) ?? 0,
                    Last7DaysAmount = g.Where(e => e.StartTime >= last7Days).Sum(e => (decimal?)e.TotalAmountRecovered) ?? 0,
                    TotalRecords = g.Sum(e => (int?)e.TotalRecordsProcessed) ?? 0,
                    Last30DaysRecords = g.Where(e => e.StartTime >= last30Days).Sum(e => (int?)e.TotalRecordsProcessed) ?? 0,
                    Previous30DaysAmount = g.Where(e => e.StartTime >= previous30DaysStart && e.StartTime < last30Days).Sum(e => (decimal?)e.TotalAmountRecovered) ?? 0,
                    TotalJobs = g.Count(),
                    SuccessfulJobs = g.Count()
                })
                .FirstOrDefaultAsync();

            // Count failed jobs separately
            var failedJobs = await _context.RecoveryJobExecutions
                .AsNoTracking()
                .Where(e => e.Status == "Failed")
                .CountAsync();

            // Batch counts
            var batchStats = await _context.StagingBatches
                .AsNoTracking()
                .GroupBy(b => 1)
                .Select(g => new
                {
                    CompletedBatches = g.Count(b => b.RecoveryStatus == "Completed"),
                    ActiveBatches = g.Count(b => b.BatchStatus == BatchStatus.Processing ||
                                                  b.BatchStatus == BatchStatus.PartiallyVerified ||
                                                  b.BatchStatus == BatchStatus.Verified)
                })
                .FirstOrDefaultAsync();

            // Average recovery per batch
            var avgRecovery = await _context.StagingBatches
                .AsNoTracking()
                .Where(b => b.TotalRecoveredAmount.HasValue && b.TotalRecoveredAmount > 0)
                .AverageAsync(b => (decimal?)b.TotalRecoveredAmount) ?? 0;

            if (jobStats != null)
            {
                Metrics.TotalAmountRecovered = jobStats.TotalAmount;
                Metrics.AmountRecoveredLast30Days = jobStats.Last30DaysAmount;
                Metrics.AmountRecoveredLast7Days = jobStats.Last7DaysAmount;
                Metrics.TotalRecordsProcessed = jobStats.TotalRecords;
                Metrics.RecordsProcessedLast30Days = jobStats.Last30DaysRecords;

                var totalJobs = jobStats.TotalJobs + failedJobs;
                if (totalJobs > 0)
                {
                    Metrics.SuccessRate = (decimal)jobStats.SuccessfulJobs / totalJobs * 100;
                }

                if (jobStats.Previous30DaysAmount > 0)
                {
                    Metrics.TrendPercentage = ((jobStats.Last30DaysAmount - jobStats.Previous30DaysAmount) / jobStats.Previous30DaysAmount) * 100;
                }
            }

            if (batchStats != null)
            {
                Metrics.TotalBatchesProcessed = batchStats.CompletedBatches;
                Metrics.ActiveBatchesWithDeadlines = batchStats.ActiveBatches;
            }

            Metrics.AverageRecoveryPerBatch = avgRecovery;
        }

        private async Task LoadRecoveryTrendsAsync()
        {
            var last30Days = DateTime.UtcNow.AddDays(-30).Date;

            var dailyRecoveries = await _context.RecoveryJobExecutions
                .AsNoTracking()
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

            // Fill in missing dates with zeros using a dictionary for O(1) lookup
            var recoveryDict = dailyRecoveries.ToDictionary(d => d.Date, d => d);
            var currentDate = last30Days;

            while (currentDate <= DateTime.UtcNow.Date)
            {
                if (recoveryDict.TryGetValue(currentDate, out var trend))
                {
                    RecoveryTrends.Add(trend);
                }
                else
                {
                    RecoveryTrends.Add(new DailyRecoveryTrend
                    {
                        Date = currentDate,
                        AmountRecovered = 0,
                        RecordsProcessed = 0,
                        JobsRun = 0
                    });
                }
                currentDate = currentDate.AddDays(1);
            }
        }

        private async Task LoadDeadlineComplianceAsync()
        {
            var next48Hours = DateTime.UtcNow.AddHours(48);

            // Single query with all counts
            var deadlineStats = await _context.DeadlineTracking
                .AsNoTracking()
                .Where(d => d.DeadlineType == "Verification" || d.DeadlineType == "Approval")
                .GroupBy(d => 1)
                .Select(g => new
                {
                    Total = g.Count(),
                    Met = g.Count(d => d.DeadlineStatus == "Met"),
                    Missed = g.Count(d => d.DeadlineStatus == "Missed"),
                    AtRisk = g.Count(d => d.DeadlineDate > DateTime.UtcNow && d.DeadlineDate <= next48Hours && d.DeadlineStatus == "Pending")
                })
                .FirstOrDefaultAsync();

            var extendedCount = await _context.ImportAudits
                .AsNoTracking()
                .Where(a => a.ImportType == "Deadline Extension")
                .CountAsync();

            if (deadlineStats != null && deadlineStats.Total > 0)
            {
                DeadlineCompliance.TotalDeadlines = deadlineStats.Total;
                DeadlineCompliance.MetDeadlines = deadlineStats.Met;
                DeadlineCompliance.MissedDeadlines = deadlineStats.Missed;
                DeadlineCompliance.ExtendedDeadlines = extendedCount;
                DeadlineCompliance.DeadlinesAtRisk = deadlineStats.AtRisk;
                DeadlineCompliance.ComplianceRate = (decimal)deadlineStats.Met / deadlineStats.Total * 100;
            }
        }

        private async Task LoadTopBatchesAsync()
        {
            var topBatches = await _context.StagingBatches
                .AsNoTracking()
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

            TopBatches = topBatches.Select(batch =>
            {
                var primaryCurrency = "KSH";
                var containsPrivateWire = batch.SourceSystems?.Contains("PrivateWire", StringComparison.OrdinalIgnoreCase) == true;
                var containsOthers = batch.SourceSystems?.Contains("Safaricom", StringComparison.OrdinalIgnoreCase) == true ||
                                    batch.SourceSystems?.Contains("Airtel", StringComparison.OrdinalIgnoreCase) == true ||
                                    batch.SourceSystems?.Contains("PSTN", StringComparison.OrdinalIgnoreCase) == true;

                if (containsPrivateWire && !containsOthers)
                {
                    primaryCurrency = "USD";
                }

                var recoveredAmount = batch.TotalRecoveredAmount ?? 0;
                decimal totalKSH, totalUSD;

                if (primaryCurrency == "KSH")
                {
                    totalKSH = recoveredAmount;
                    totalUSD = recoveredAmount * _kshToUsdRate;
                }
                else
                {
                    totalUSD = recoveredAmount;
                    totalKSH = recoveredAmount * _usdToKshRate;
                }

                return new BatchRecoveryInfo
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
                };
            }).ToList();
        }

        private async Task LoadRecentActivitiesAsync()
        {
            RecentActivities = await _context.RecoveryJobExecutions
                .AsNoTracking()
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
            // Get execution breakdown in single query
            var executionBreakdown = await _context.RecoveryJobExecutions
                .AsNoTracking()
                .Where(e => e.Status == "Completed")
                .GroupBy(e => 1)
                .Select(g => new
                {
                    ExpiredVerifications = g.Sum(e => e.ExpiredVerificationsProcessed),
                    ExpiredApprovals = g.Sum(e => e.ExpiredApprovalsProcessed),
                    RevertedVerifications = g.Sum(e => e.RevertedVerificationsProcessed)
                })
                .FirstOrDefaultAsync();

            // Get batch amounts in single query
            var batchAmounts = await _context.StagingBatches
                .AsNoTracking()
                .Where(b => b.TotalRecoveredAmount.HasValue && b.TotalRecoveredAmount > 0)
                .GroupBy(b => 1)
                .Select(g => new
                {
                    PersonalTotal = g.Sum(b => b.TotalPersonalAmount ?? 0),
                    COSTotal = g.Sum(b => b.TotalClassOfServiceAmount ?? 0)
                })
                .FirstOrDefaultAsync();

            if (executionBreakdown != null)
            {
                RecoveryBreakdown.ExpiredVerificationCount = executionBreakdown.ExpiredVerifications;
                RecoveryBreakdown.ExpiredApprovalCount = executionBreakdown.ExpiredApprovals;
                RecoveryBreakdown.RevertedVerificationCount = executionBreakdown.RevertedVerifications;
            }

            if (batchAmounts != null)
            {
                RecoveryBreakdown.PersonalAmountTotal = batchAmounts.PersonalTotal;
                RecoveryBreakdown.ClassOfServiceAmountTotal = batchAmounts.COSTotal;
            }
        }

        private async Task LoadEnhancedMetricsAsync()
        {
            // Get all recovery logs with currency info in single query
            var recoveryStats = await _context.RecoveryLogs
                .AsNoTracking()
                .Include(r => r.CallRecord)
                .GroupBy(r => r.CallRecord != null ? r.CallRecord.CallCurrencyCode : "KES")
                .Select(g => new
                {
                    Currency = g.Key ?? "KES",
                    TotalAmount = g.Sum(r => r.AmountRecovered),
                    Last30DaysAmount = g.Where(r => r.RecoveryDate >= Filters.StartDate && r.RecoveryDate <= Filters.EndDate)
                                        .Sum(r => r.AmountRecovered),
                    Last7DaysAmount = g.Where(r => r.RecoveryDate >= DateTime.UtcNow.AddDays(-7))
                                       .Sum(r => r.AmountRecovered)
                })
                .ToListAsync();

            decimal totalKSH = 0, totalUSD = 0;
            decimal last30KSH = 0, last30USD = 0;
            decimal last7KSH = 0, last7USD = 0;

            foreach (var stat in recoveryStats)
            {
                var currency = stat.Currency?.ToUpper() ?? "KES";
                if (currency == "KES" || currency == "KSH")
                {
                    totalKSH += stat.TotalAmount;
                    totalUSD += stat.TotalAmount * _kshToUsdRate;
                    last30KSH += stat.Last30DaysAmount;
                    last30USD += stat.Last30DaysAmount * _kshToUsdRate;
                    last7KSH += stat.Last7DaysAmount;
                    last7USD += stat.Last7DaysAmount * _kshToUsdRate;
                }
                else if (currency == "USD")
                {
                    totalUSD += stat.TotalAmount;
                    totalKSH += stat.TotalAmount * _usdToKshRate;
                    last30USD += stat.Last30DaysAmount;
                    last30KSH += stat.Last30DaysAmount * _usdToKshRate;
                    last7USD += stat.Last7DaysAmount;
                    last7KSH += stat.Last7DaysAmount * _usdToKshRate;
                }
            }

            EnhancedMetrics.TotalAmountRecoveredKSH = totalKSH;
            EnhancedMetrics.TotalAmountRecoveredUSD = totalUSD;
            EnhancedMetrics.AmountRecoveredLast30DaysKSH = last30KSH;
            EnhancedMetrics.AmountRecoveredLast30DaysUSD = last30USD;
            EnhancedMetrics.AmountRecoveredLast7DaysKSH = last7KSH;
            EnhancedMetrics.AmountRecoveredLast7DaysUSD = last7USD;

            // Copy existing metrics
            EnhancedMetrics.TotalRecordsProcessed = Metrics.TotalRecordsProcessed;
            EnhancedMetrics.RecordsProcessedLast30Days = Metrics.RecordsProcessedLast30Days;
            EnhancedMetrics.TotalBatchesProcessed = Metrics.TotalBatchesProcessed;
            EnhancedMetrics.ActiveBatchesWithDeadlines = Metrics.ActiveBatchesWithDeadlines;
            EnhancedMetrics.SuccessRate = Metrics.SuccessRate;
            EnhancedMetrics.TrendPercentage = Metrics.TrendPercentage;

            if (Metrics.TotalBatchesProcessed > 0)
            {
                EnhancedMetrics.AverageRecoveryPerBatchKSH = totalKSH / Metrics.TotalBatchesProcessed;
                EnhancedMetrics.AverageRecoveryPerBatchUSD = totalUSD / Metrics.TotalBatchesProcessed;
            }
        }

        private async Task LoadRecoveryByTypeAsync()
        {
            // Single query with grouping by assignment type and currency
            var callStats = await _context.CallRecords
                .AsNoTracking()
                .Where(c => c.FinalAssignmentType != null && c.FinalAssignmentType != "None")
                .GroupBy(c => new { c.FinalAssignmentType, c.CallCurrencyCode })
                .Select(g => new
                {
                    AssignmentType = g.Key.FinalAssignmentType,
                    Currency = g.Key.CallCurrencyCode ?? "KES",
                    TotalKSH = g.Sum(c => c.CallCostKSHS),
                    TotalUSD = g.Sum(c => c.CallCostUSD),
                    Count = g.Count()
                })
                .ToListAsync();

            var dto = new RecoveryByTypeDTO();

            foreach (var stat in callStats)
            {
                var currency = stat.Currency?.ToUpper() ?? "KES";
                decimal amountKSH, amountUSD;

                if (currency == "KES" || currency == "KSH")
                {
                    amountKSH = stat.TotalKSH;
                    amountUSD = stat.TotalKSH * _kshToUsdRate;
                }
                else
                {
                    amountUSD = stat.TotalUSD;
                    amountKSH = stat.TotalUSD * _usdToKshRate;
                }

                switch (stat.AssignmentType?.ToLower())
                {
                    case "personal":
                        dto.PersonalKSH += amountKSH;
                        dto.PersonalUSD += amountUSD;
                        dto.PersonalCallCount += stat.Count;
                        break;
                    case "official":
                        dto.OfficialKSH += amountKSH;
                        dto.OfficialUSD += amountUSD;
                        dto.OfficialCallCount += stat.Count;
                        break;
                    case "classofservice":
                        dto.ClassOfServiceKSH += amountKSH;
                        dto.ClassOfServiceUSD += amountUSD;
                        dto.COSCallCount += stat.Count;
                        break;
                }
            }

            if (dto.TotalCalls > 0)
            {
                dto.PersonalPercentage = (decimal)dto.PersonalCallCount / dto.TotalCalls * 100;
                dto.OfficialPercentage = (decimal)dto.OfficialCallCount / dto.TotalCalls * 100;
                dto.COSPercentage = (decimal)dto.COSCallCount / dto.TotalCalls * 100;
            }

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
            var query = GetFilteredRecoveryLogsQuery();

            // Group by provider and currency in database
            var providerStats = await query
                .GroupBy(r => new
                {
                    Provider = r.CallRecord!.SourceSystem ?? "Unknown",
                    Currency = r.CallRecord.CallCurrencyCode ?? "KES"
                })
                .Select(g => new
                {
                    g.Key.Provider,
                    g.Key.Currency,
                    TotalAmount = g.Sum(r => r.AmountRecovered),
                    Count = g.Count()
                })
                .ToListAsync();

            // Aggregate by provider
            var providerDict = new Dictionary<string, (decimal ksh, decimal usd, int count)>();

            foreach (var stat in providerStats)
            {
                var currency = stat.Currency?.ToUpper() ?? "KES";
                decimal amountKSH, amountUSD;

                if (currency == "KES" || currency == "KSH")
                {
                    amountKSH = stat.TotalAmount;
                    amountUSD = stat.TotalAmount * _kshToUsdRate;
                }
                else
                {
                    amountUSD = stat.TotalAmount;
                    amountKSH = stat.TotalAmount * _usdToKshRate;
                }

                if (providerDict.ContainsKey(stat.Provider))
                {
                    var (ksh, usd, count) = providerDict[stat.Provider];
                    providerDict[stat.Provider] = (ksh + amountKSH, usd + amountUSD, count + stat.Count);
                }
                else
                {
                    providerDict[stat.Provider] = (amountKSH, amountUSD, stat.Count);
                }
            }

            var providerList = providerDict.Select(kvp =>
            {
                var nativeCurrency = kvp.Key.ToUpper() == "PRIVATEWIRE" ? "USD" : "KSH";
                var nativeAmount = nativeCurrency == "USD" ? kvp.Value.usd : kvp.Value.ksh;

                return new RecoveryByProviderDTO
                {
                    Provider = kvp.Key,
                    NativeCurrency = nativeCurrency,
                    AmountInNativeCurrency = nativeAmount,
                    AmountInKSH = kvp.Value.ksh,
                    AmountInUSD = kvp.Value.usd,
                    CallCount = kvp.Value.count,
                    AvgPerCall = kvp.Value.count > 0 ? nativeAmount / kvp.Value.count : 0
                };
            }).ToList();

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
            var query = GetFilteredRecoveryLogsQuery();

            // Get recovery data grouped by organization in database
            var orgStats = await query
                .Where(r => r.CallRecord!.ResponsibleUser!.OrganizationEntity != null)
                .GroupBy(r => new
                {
                    OrgId = r.CallRecord!.ResponsibleUser!.OrganizationEntity!.Id,
                    OrgName = r.CallRecord.ResponsibleUser.OrganizationEntity.Name,
                    Currency = r.CallRecord.CallCurrencyCode ?? "KES",
                    Action = r.RecoveryAction
                })
                .Select(g => new
                {
                    g.Key.OrgId,
                    g.Key.OrgName,
                    g.Key.Currency,
                    g.Key.Action,
                    TotalAmount = g.Sum(r => r.AmountRecovered),
                    Count = g.Count(),
                    UserCount = g.Select(r => r.RecoveredFrom).Distinct().Count()
                })
                .ToListAsync();

            // Aggregate by organization
            var orgDict = new Dictionary<int, RecoveryByOrganizationDTO>();

            foreach (var stat in orgStats)
            {
                var currency = stat.Currency?.ToUpper() ?? "KES";
                decimal amountKSH, amountUSD;

                if (currency == "KES" || currency == "KSH")
                {
                    amountKSH = stat.TotalAmount;
                    amountUSD = stat.TotalAmount * _kshToUsdRate;
                }
                else
                {
                    amountUSD = stat.TotalAmount;
                    amountKSH = stat.TotalAmount * _usdToKshRate;
                }

                if (!orgDict.ContainsKey(stat.OrgId))
                {
                    orgDict[stat.OrgId] = new RecoveryByOrganizationDTO
                    {
                        OrganizationId = stat.OrgId,
                        OrganizationName = stat.OrgName
                    };
                }

                var org = orgDict[stat.OrgId];
                org.TotalKSH += amountKSH;
                org.TotalUSD += amountUSD;
                org.UserCount = Math.Max(org.UserCount, stat.UserCount);

                switch (stat.Action?.ToLower())
                {
                    case "personal":
                        org.PersonalCount += stat.Count;
                        break;
                    case "official":
                        org.OfficialCount += stat.Count;
                        break;
                    case "classofservice":
                    case "cos":
                        org.COSCount += stat.Count;
                        break;
                }
            }

            RecoveryByOrganization = orgDict.Values.OrderByDescending(o => o.TotalKSH).ToList();
        }

        private async Task LoadTopUserRecoveriesAsync()
        {
            var query = GetFilteredRecoveryLogsQuery();

            // Get recovery data grouped by user in database
            var userStats = await query
                .Where(r => r.RecoveredFrom != null && r.CallRecord!.ResponsibleUser != null)
                .GroupBy(r => new
                {
                    IndexNumber = r.RecoveredFrom,
                    FullName = r.CallRecord!.ResponsibleUser!.FirstName + " " + r.CallRecord.ResponsibleUser.LastName,
                    OrgName = r.CallRecord.ResponsibleUser.OrganizationEntity != null ? r.CallRecord.ResponsibleUser.OrganizationEntity.Name : "N/A",
                    OfficeName = r.CallRecord.ResponsibleUser.OfficeEntity != null ? r.CallRecord.ResponsibleUser.OfficeEntity.Name : "N/A",
                    Currency = r.CallRecord.CallCurrencyCode ?? "KES",
                    Action = r.RecoveryAction
                })
                .Select(g => new
                {
                    g.Key.IndexNumber,
                    g.Key.FullName,
                    g.Key.OrgName,
                    g.Key.OfficeName,
                    g.Key.Currency,
                    g.Key.Action,
                    TotalAmount = g.Sum(r => r.AmountRecovered),
                    Count = g.Count()
                })
                .ToListAsync();

            // Aggregate by user
            var userDict = new Dictionary<string, TopUserRecoveryDTO>();

            foreach (var stat in userStats)
            {
                if (stat.IndexNumber == null) continue;

                var currency = stat.Currency?.ToUpper() ?? "KES";
                decimal amountKSH, amountUSD;

                if (currency == "KES" || currency == "KSH")
                {
                    amountKSH = stat.TotalAmount;
                    amountUSD = stat.TotalAmount * _kshToUsdRate;
                }
                else
                {
                    amountUSD = stat.TotalAmount;
                    amountKSH = stat.TotalAmount * _usdToKshRate;
                }

                if (!userDict.ContainsKey(stat.IndexNumber))
                {
                    userDict[stat.IndexNumber] = new TopUserRecoveryDTO
                    {
                        IndexNumber = stat.IndexNumber,
                        FullName = stat.FullName,
                        OrganizationName = stat.OrgName,
                        OfficeName = stat.OfficeName
                    };
                }

                var user = userDict[stat.IndexNumber];
                user.TotalKSH += amountKSH;
                user.TotalUSD += amountUSD;
                user.CallCount += stat.Count;

                switch (stat.Action?.ToLower())
                {
                    case "personal":
                        user.PersonalCalls += stat.Count;
                        break;
                    case "official":
                        user.OfficialCalls += stat.Count;
                        break;
                    case "classofservice":
                    case "cos":
                        user.COSCalls += stat.Count;
                        break;
                }
            }

            TopUserRecoveries = userDict.Values
                .OrderByDescending(u => u.TotalKSH)
                .Take(20)
                .Select((u, index) =>
                {
                    u.Rank = index + 1;
                    return u;
                })
                .ToList();
        }

        private async Task LoadAlertsAsync()
        {
            var alerts = new List<DashboardAlertDTO>();
            var next48Hours = DateTime.UtcNow.AddHours(48);

            // Run queries sequentially - DbContext is not thread-safe
            var pendingResult = await _context.CallRecords
                .AsNoTracking()
                .Where(c => c.RecoveryStatus == "Pending")
                .GroupBy(c => 1)
                .Select(g => new { Count = g.Count(), Amount = g.Sum(c => c.RecoveryAmount ?? 0) })
                .FirstOrDefaultAsync();

            var upcomingDeadlines = await _context.DeadlineTracking
                .AsNoTracking()
                .Where(d => d.DeadlineDate > DateTime.UtcNow &&
                           d.DeadlineDate <= next48Hours &&
                           d.DeadlineStatus == "Pending")
                .CountAsync();

            if (pendingResult != null && pendingResult.Count > 0)
            {
                alerts.Add(new DashboardAlertDTO
                {
                    AlertType = "Pending Recoveries",
                    Priority = "Medium",
                    Message = $"{pendingResult.Count} call records pending recovery processing",
                    AffectedCount = pendingResult.Count,
                    AmountAtRisk = pendingResult.Amount,
                    Icon = "bi-clock-history",
                    Link = "/Admin/CallLogs?filter=pending"
                });
            }

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
            // Single query with grouping by currency
            var officialStats = await _context.CallRecords
                .AsNoTracking()
                .Where(c => c.FinalAssignmentType == "Official")
                .GroupBy(c => c.CallCurrencyCode ?? "KES")
                .Select(g => new
                {
                    Currency = g.Key,
                    Count = g.Count(),
                    TotalKSH = g.Sum(c => c.CallCostKSHS),
                    TotalUSD = g.Sum(c => c.CallCostUSD)
                })
                .ToListAsync();

            decimal totalKSH = 0, totalUSD = 0;
            int totalCount = 0;

            foreach (var stat in officialStats)
            {
                totalCount += stat.Count;
                var currency = stat.Currency?.ToUpper() ?? "KES";

                if (currency == "KES" || currency == "KSH")
                {
                    totalKSH += stat.TotalKSH;
                    totalUSD += stat.TotalKSH * _kshToUsdRate;
                }
                else
                {
                    totalUSD += stat.TotalUSD;
                    totalKSH += stat.TotalUSD * _usdToKshRate;
                }
            }

            TotalOfficialCallsCount = totalCount;
            TotalOfficialAmountKSH = totalKSH;
            TotalOfficialAmountUSD = totalUSD;
        }

        private async Task LoadProviderSummaryAsync()
        {
            // Single query with grouping by provider, currency, and assignment type
            var providerStats = await _context.CallRecords
                .AsNoTracking()
                .Where(c => c.FinalAssignmentType != null && c.FinalAssignmentType != "None")
                .GroupBy(c => new
                {
                    Provider = c.SourceSystem ?? "Unknown",
                    Currency = c.CallCurrencyCode ?? "KES",
                    AssignmentType = c.FinalAssignmentType
                })
                .Select(g => new
                {
                    g.Key.Provider,
                    g.Key.Currency,
                    g.Key.AssignmentType,
                    Count = g.Count(),
                    TotalKSH = g.Sum(c => c.CallCostKSHS),
                    TotalUSD = g.Sum(c => c.CallCostUSD)
                })
                .ToListAsync();

            ProviderSummaries.Clear();
            TotalPersonalCalls = 0;
            TotalPersonalAmountKSH = 0;
            TotalOfficialCallsAll = 0;
            TotalOfficialAmountKSHAll = 0;

            foreach (var stat in providerStats)
            {
                var currency = stat.Currency?.ToUpper() ?? "KES";
                decimal amountKSH, amountUSD;

                if (currency == "KES" || currency == "KSH")
                {
                    amountKSH = stat.TotalKSH;
                    amountUSD = stat.TotalKSH * _kshToUsdRate;
                }
                else
                {
                    amountUSD = stat.TotalUSD;
                    amountKSH = stat.TotalUSD * _usdToKshRate;
                }

                // Update provider summary
                if (!ProviderSummaries.ContainsKey(stat.Provider))
                {
                    var nativeCurrency = stat.Provider.ToUpper() == "PRIVATEWIRE" ? "USD" : "KSH";
                    ProviderSummaries[stat.Provider] = new ProviderSummary
                    {
                        ProviderName = stat.Provider,
                        Currency = nativeCurrency
                    };
                }

                var summary = ProviderSummaries[stat.Provider];
                summary.CallCount += stat.Count;
                summary.AmountKSH += amountKSH;
                summary.AmountUSD += amountUSD;

                // Update totals by assignment type
                if (stat.AssignmentType == "Personal")
                {
                    TotalPersonalCalls += stat.Count;
                    TotalPersonalAmountKSH += amountKSH;
                }
                else if (stat.AssignmentType == "Official")
                {
                    TotalOfficialCallsAll += stat.Count;
                    TotalOfficialAmountKSHAll += amountKSH;
                }
            }
        }

        private async Task LoadMonthlyRecoveryTrendsAsync()
        {
            var twelveMonthsAgo = DateTime.UtcNow.AddMonths(-12);
            var cutoffYear = twelveMonthsAgo.Year;
            var cutoffMonth = twelveMonthsAgo.Month;

            // Single query with grouping by month, year, currency, and assignment type
            var monthlyStats = await _context.CallRecords
                .AsNoTracking()
                .Where(c => c.FinalAssignmentType != null &&
                           c.FinalAssignmentType != "None" &&
                           (c.CallYear > cutoffYear || (c.CallYear == cutoffYear && c.CallMonth >= cutoffMonth)))
                .GroupBy(c => new
                {
                    c.CallYear,
                    c.CallMonth,
                    c.FinalAssignmentType,
                    Currency = c.CallCurrencyCode ?? "KES"
                })
                .Select(g => new
                {
                    g.Key.CallYear,
                    g.Key.CallMonth,
                    g.Key.FinalAssignmentType,
                    g.Key.Currency,
                    TotalKSH = g.Sum(c => c.CallCostKSHS),
                    TotalUSD = g.Sum(c => c.CallCostUSD),
                    Count = g.Count()
                })
                .ToListAsync();

            // Aggregate by month
            var monthDict = new Dictionary<(int Year, int Month), MonthlyRecoveryTrend>();

            foreach (var stat in monthlyStats)
            {
                var key = (stat.CallYear, stat.CallMonth);
                var currency = stat.Currency?.ToUpper() ?? "KES";
                decimal amountKSH;

                if (currency == "KES" || currency == "KSH")
                {
                    amountKSH = stat.TotalKSH;
                }
                else
                {
                    amountKSH = stat.TotalUSD * _usdToKshRate;
                }

                if (!monthDict.ContainsKey(key))
                {
                    monthDict[key] = new MonthlyRecoveryTrend
                    {
                        MonthYear = new DateTime(stat.CallYear, stat.CallMonth, 1).ToString("MMM yyyy"),
                        Year = stat.CallYear,
                        Month = stat.CallMonth
                    };
                }

                var trend = monthDict[key];
                if (stat.FinalAssignmentType == "Personal")
                {
                    trend.PersonalAmountKSH += amountKSH;
                    trend.PersonalCallCount += stat.Count;
                }
                else if (stat.FinalAssignmentType == "Official")
                {
                    trend.OfficialAmountKSH += amountKSH;
                    trend.OfficialCallCount += stat.Count;
                }
            }

            MonthlyRecoveryTrends = monthDict.Values
                .OrderBy(t => t.Year)
                .ThenBy(t => t.Month)
                .ToList();
        }

        private IQueryable<RecoveryLog> GetFilteredRecoveryLogsQuery()
        {
            var query = _context.RecoveryLogs
                .AsNoTracking()
                .Include(r => r.CallRecord)
                    .ThenInclude(c => c!.ResponsibleUser)
                        .ThenInclude(u => u!.OrganizationEntity)
                .Include(r => r.CallRecord)
                    .ThenInclude(c => c!.ResponsibleUser)
                        .ThenInclude(u => u!.OfficeEntity)
                .Where(r => r.RecoveryDate >= Filters.StartDate && r.RecoveryDate <= Filters.EndDate);

            if (Filters.OrganizationIds.Any())
            {
                query = query.Where(r => r.CallRecord!.ResponsibleUser!.OrganizationId != null &&
                                        Filters.OrganizationIds.Contains(r.CallRecord.ResponsibleUser.OrganizationId.Value));
            }

            if (Filters.OfficeIds.Any())
            {
                query = query.Where(r => r.CallRecord!.ResponsibleUser!.OfficeId != null &&
                                        Filters.OfficeIds.Contains(r.CallRecord.ResponsibleUser.OfficeId.Value));
            }

            if (!string.IsNullOrEmpty(Filters.UserIndexNumber))
            {
                query = query.Where(r => r.RecoveredFrom == Filters.UserIndexNumber);
            }

            if (Filters.Providers.Any())
            {
                query = query.Where(r => r.CallRecord!.SourceSystem != null &&
                                        Filters.Providers.Contains(r.CallRecord.SourceSystem));
            }

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
        public string MonthYear { get; set; } = string.Empty;
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
