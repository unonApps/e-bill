using Microsoft.EntityFrameworkCore;
using TAB.Web.Data;
using TAB.Web.Models;
using TAB.Web.Models.Enums;

namespace TAB.Web.Services
{
    public class ClassOfServiceCalculationService : IClassOfServiceCalculationService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ClassOfServiceCalculationService> _logger;
        private readonly IClassOfServiceVersioningService _versioningService;

        public ClassOfServiceCalculationService(
            ApplicationDbContext context,
            ILogger<ClassOfServiceCalculationService> logger,
            IClassOfServiceVersioningService versioningService)
        {
            _context = context;
            _logger = logger;
            _versioningService = versioningService;
        }

        public async Task<bool> IsWithinAllowanceAsync(string indexNumber, decimal amount, int month, int year)
        {
            var limit = await GetAllowanceLimitAsync(indexNumber);
            if (limit == null || limit == 0) return true; // Unlimited or no limit set

            var usage = await GetMonthlyUsageAsync(indexNumber, month, year);
            return (usage + amount) <= limit.Value;
        }

        public async Task<decimal> GetMonthlyUsageAsync(string indexNumber, int month, int year)
        {
            // Include calls where user is responsible OR calls assigned to them (accepted)
            return await _context.CallRecords
                .Where(c => (c.ResponsibleIndexNumber == indexNumber ||
                            (c.PayingIndexNumber == indexNumber && c.AssignmentStatus == "Accepted"))
                    && c.CallMonth == month
                    && c.CallYear == year
                    && c.VerificationType == VerificationType.Official.ToString())
                .SumAsync(c => c.CallCostUSD);
        }

        public async Task<decimal> GetPendingVerificationAsync(string indexNumber, int month, int year)
        {
            return await _context.CallRecords
                .Where(c => c.ResponsibleIndexNumber == indexNumber
                    && c.CallMonth == month
                    && c.CallYear == year
                    && (c.VerificationType == null || c.VerificationType == string.Empty))
                .SumAsync(c => c.CallCostUSD);
        }

        public async Task<decimal?> GetAllowanceLimitAsync(string indexNumber)
        {
            var userPhones = await _context.UserPhones
                .Include(up => up.ClassOfService)
                .Where(up => up.IndexNumber == indexNumber && up.IsActive)
                .ToListAsync();

            if (!userPhones.Any())
                return null;

            // Check if any extension has unlimited allowance (NULL)
            if (userPhones.Any(up => up.ClassOfService?.AirtimeAllowanceAmount == null))
                return null; // If any extension is unlimited, treat the entire allowance as unlimited

            // Sum up allowances from all active extensions
            var totalAllowance = userPhones
                .Where(up => up.ClassOfService?.AirtimeAllowanceAmount != null)
                .Sum(up => up.ClassOfService.AirtimeAllowanceAmount.Value);

            return totalAllowance > 0 ? totalAllowance : null;
        }

        public async Task<OverageReport> GetOverageReportAsync(string indexNumber, int month, int year)
        {
            var user = await _context.EbillUsers
                .FirstOrDefaultAsync(u => u.IndexNumber == indexNumber);

            var userPhone = await _context.UserPhones
                .Include(up => up.ClassOfService)
                .FirstOrDefaultAsync(up => up.IndexNumber == indexNumber && up.IsActive);

            // Get the Class of Service version that was effective during the billing period
            ClassOfService? effectiveClassOfService = null;
            if (userPhone?.ClassOfServiceId != null)
            {
                // Use the first day of the billing month as the effective date
                var billingPeriodDate = new DateTime(year, month, 1);
                effectiveClassOfService = await _versioningService.GetEffectiveVersionAsync(
                    userPhone.ClassOfServiceId.Value,
                    billingPeriodDate);
            }

            // AirtimeAllowanceAmount represents the total monthly allowance (includes airtime AND data)
            // NULL means Unlimited
            var allowanceLimit = effectiveClassOfService?.AirtimeAllowanceAmount ?? 0;
            var totalUsage = await GetMonthlyUsageAsync(indexNumber, month, year);

            // Include calls where user is responsible OR calls assigned to them (accepted)
            var calls = await _context.CallRecords
                .Where(c => (c.ResponsibleIndexNumber == indexNumber ||
                            (c.PayingIndexNumber == indexNumber && c.AssignmentStatus == "Accepted"))
                    && c.CallMonth == month
                    && c.CallYear == year)
                .ToListAsync();

            var verifications = await _context.CallLogVerifications
                .Where(v => v.VerifiedBy == indexNumber
                    && v.CallRecord.CallMonth == month
                    && v.CallRecord.CallYear == year)
                .Include(v => v.CallRecord)
                .ToListAsync();

            return new OverageReport
            {
                IndexNumber = indexNumber,
                UserName = user != null ? $"{user.FirstName} {user.LastName}" : indexNumber,
                Email = user?.Email ?? "",
                Month = month,
                Year = year,
                AllowanceLimit = allowanceLimit,
                TotalUsage = totalUsage,
                // If allowanceLimit is 0 (Unlimited), no overage
                OverageAmount = allowanceLimit > 0 ? Math.Max(0, totalUsage - allowanceLimit) : 0,
                OveragePercentage = allowanceLimit > 0 ? (totalUsage / allowanceLimit * 100) - 100 : 0,
                TotalCalls = calls.Count,
                OfficialCalls = calls.Count(c => c.VerificationType == VerificationType.Official.ToString()),
                PersonalCalls = calls.Count(c => c.VerificationType == VerificationType.Personal.ToString()),
                HasJustification = verifications.Any(v => v.IsOverage && !string.IsNullOrWhiteSpace(v.JustificationText)),
                SupervisorApproved = verifications.Any(v => v.ApprovalStatus == "Approved" || v.ApprovalStatus == "PartiallyApproved"),
                ClassOfService = effectiveClassOfService?.Class ?? "N/A",
                Office = user?.Location ?? "N/A"
            };
        }

        public async Task<List<OverageReport>> GetAllOveragesAsync(int month, int year)
        {
            var users = await _context.EbillUsers.ToListAsync();
            var overageReports = new List<OverageReport>();

            foreach (var user in users)
            {
                var report = await GetOverageReportAsync(user.IndexNumber, month, year);
                if (report.OverageAmount > 0)
                {
                    overageReports.Add(report);
                }
            }

            return overageReports.OrderByDescending(r => r.OverageAmount).ToList();
        }

        public async Task<decimal> GetAllowanceUsagePercentageAsync(string indexNumber, int month, int year)
        {
            var limit = await GetAllowanceLimitAsync(indexNumber);
            if (!limit.HasValue || limit == 0) return 0;

            var usage = await GetMonthlyUsageAsync(indexNumber, month, year);
            return (usage / limit.Value) * 100;
        }

        public async Task<AllowancePrediction> PredictMonthEndUsageAsync(string indexNumber, int month, int year)
        {
            var currentUsage = await GetMonthlyUsageAsync(indexNumber, month, year);
            var allowanceLimit = await GetAllowanceLimitAsync(indexNumber);
            var limitValue = allowanceLimit ?? 0; // Treat unlimited as 0 for calculations

            var firstDayOfMonth = new DateTime(year, month, 1);
            var lastDayOfMonth = new DateTime(year, month, DateTime.DaysInMonth(year, month));
            var today = DateTime.UtcNow.Date;

            // Ensure we're calculating for the current or past months
            if (today < firstDayOfMonth)
            {
                // Future month - return zeros
                return new AllowancePrediction
                {
                    IndexNumber = indexNumber,
                    Month = month,
                    Year = year,
                    CurrentUsage = 0,
                    AllowanceLimit = limitValue,
                    DaysRemainingInMonth = (lastDayOfMonth - today).Days
                };
            }

            var daysElapsed = (today - firstDayOfMonth).Days + 1;
            var daysRemaining = (lastDayOfMonth - today).Days;
            var dailyAverage = daysElapsed > 0 ? currentUsage / daysElapsed : 0;

            var predictedMonthEndUsage = currentUsage + (dailyAverage * daysRemaining);
            var predictedOverage = limitValue > 0 ? Math.Max(0, predictedMonthEndUsage - limitValue) : 0;

            var remainingBudget = limitValue > 0 ? Math.Max(0, limitValue - currentUsage) : decimal.MaxValue;
            var recommendedDailyLimit = daysRemaining > 0 && limitValue > 0 ? remainingBudget / daysRemaining : 0;

            return new AllowancePrediction
            {
                IndexNumber = indexNumber,
                Month = month,
                Year = year,
                CurrentUsage = currentUsage,
                AllowanceLimit = limitValue,
                DailyAverageUsage = dailyAverage,
                PredictedMonthEndUsage = predictedMonthEndUsage,
                PredictedOverage = predictedOverage,
                WillExceedAllowance = limitValue > 0 && predictedMonthEndUsage > limitValue,
                DaysRemainingInMonth = daysRemaining,
                RemainingBudget = remainingBudget,
                RecommendedDailyLimit = recommendedDailyLimit
            };
        }

        public async Task<ClassOfService?> GetUserClassOfServiceAsync(string indexNumber)
        {
            var userPhone = await _context.UserPhones
                .Include(up => up.ClassOfService)
                .FirstOrDefaultAsync(up => up.IndexNumber == indexNumber && up.IsActive);

            return userPhone?.ClassOfService;
        }

        public async Task<UsageBreakdown> GetUsageBreakdownAsync(string indexNumber, int month, int year)
        {
            // Include calls where user is responsible OR calls assigned to them (accepted)
            var calls = await _context.CallRecords
                .Where(c => (c.ResponsibleIndexNumber == indexNumber ||
                            (c.PayingIndexNumber == indexNumber && c.AssignmentStatus == "Accepted"))
                    && c.CallMonth == month
                    && c.CallYear == year)
                .ToListAsync();

            var topDestinations = calls
                .GroupBy(c => c.CallDestination)
                .Select(g => new DestinationSummary
                {
                    Destination = g.Key,
                    CallCount = g.Count(),
                    TotalCost = g.Sum(c => c.CallCostUSD),
                    AverageCost = g.Average(c => c.CallCostUSD)
                })
                .OrderByDescending(d => d.TotalCost)
                .Take(10)
                .ToList();

            return new UsageBreakdown
            {
                IndexNumber = indexNumber,
                Month = month,
                Year = year,
                TotalCost = calls.Sum(c => c.CallCostUSD),

                // By Destination Type
                LocalCost = calls.Where(c => c.CallDestinationType.ToLower() == "local").Sum(c => c.CallCostUSD),
                InternationalCost = calls.Where(c => c.CallDestinationType.ToLower() == "international").Sum(c => c.CallCostUSD),
                MobileCost = calls.Where(c => c.CallDestinationType.ToLower() == "mobile").Sum(c => c.CallCostUSD),
                PremiumCost = calls.Where(c => c.CallDestinationType.ToLower() == "premium").Sum(c => c.CallCostUSD),

                // By Call Type
                VoiceCost = calls.Where(c => c.CallType.ToLower() == "voice").Sum(c => c.CallCostUSD),
                SMSCost = calls.Where(c => c.CallType.ToLower() == "sms").Sum(c => c.CallCostUSD),
                DataCost = calls.Where(c => c.CallType.ToLower() == "data").Sum(c => c.CallCostUSD),

                // By Verification Type
                OfficialCost = calls.Where(c => c.VerificationType == VerificationType.Official.ToString()).Sum(c => c.CallCostUSD),
                PersonalCost = calls.Where(c => c.VerificationType == VerificationType.Personal.ToString()).Sum(c => c.CallCostUSD),

                // Call Counts
                TotalCalls = calls.Count,
                LocalCalls = calls.Count(c => c.CallDestinationType.ToLower() == "local"),
                InternationalCalls = calls.Count(c => c.CallDestinationType.ToLower() == "international"),
                MobileCalls = calls.Count(c => c.CallDestinationType.ToLower() == "mobile"),

                TopDestinations = topDestinations
            };
        }

        // ===================================================================
        // Phone-Specific Methods (Per UserPhone, not per User)
        // ===================================================================

        public async Task<decimal?> GetPhoneAllowanceLimitAsync(int userPhoneId)
        {
            var userPhone = await _context.UserPhones
                .Include(up => up.ClassOfService)
                .FirstOrDefaultAsync(up => up.Id == userPhoneId && up.IsActive);

            if (userPhone?.ClassOfService == null)
                return null;

            // NULL means Unlimited
            return userPhone.ClassOfService.AirtimeAllowanceAmount;
        }

        public async Task<decimal> GetPhoneMonthlyUsageAsync(int userPhoneId, int month, int year)
        {
            // Get calls for this specific phone only
            return await _context.CallRecords
                .Where(c => c.UserPhoneId == userPhoneId
                    && c.CallMonth == month
                    && c.CallYear == year
                    && c.VerificationType == VerificationType.Official.ToString())
                .SumAsync(c => c.CallCostUSD);
        }

        public async Task<bool> IsPhoneWithinAllowanceAsync(int userPhoneId, decimal amount, int month, int year)
        {
            var limit = await GetPhoneAllowanceLimitAsync(userPhoneId);
            if (limit == null || limit == 0) return true; // Unlimited or no limit set

            var usage = await GetPhoneMonthlyUsageAsync(userPhoneId, month, year);
            return (usage + amount) <= limit.Value;
        }
    }
}
