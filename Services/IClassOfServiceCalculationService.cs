using TAB.Web.Models;

namespace TAB.Web.Services
{
    public interface IClassOfServiceCalculationService
    {
        /// <summary>
        /// Checks if a user is within their allowance for a specific month
        /// </summary>
        Task<bool> IsWithinAllowanceAsync(string indexNumber, decimal amount, int month, int year);

        /// <summary>
        /// Gets total usage for a user in a specific month
        /// </summary>
        Task<decimal> GetMonthlyUsageAsync(string indexNumber, int month, int year);

        /// <summary>
        /// Gets pending (unverified) call costs for a user in a specific month
        /// </summary>
        Task<decimal> GetPendingVerificationAsync(string indexNumber, int month, int year);

        /// <summary>
        /// Gets the allowance limit for a user's class of service
        /// Returns NULL for unlimited allowance
        /// </summary>
        Task<decimal?> GetAllowanceLimitAsync(string indexNumber);

        // ===================================================================
        // Phone-Specific Methods (Per UserPhone, not per User)
        // ===================================================================

        /// <summary>
        /// Gets the allowance limit for a specific phone
        /// Returns NULL for unlimited allowance
        /// </summary>
        Task<decimal?> GetPhoneAllowanceLimitAsync(int userPhoneId);

        /// <summary>
        /// Gets total usage for a specific phone in a specific month
        /// </summary>
        Task<decimal> GetPhoneMonthlyUsageAsync(int userPhoneId, int month, int year);

        /// <summary>
        /// Checks if a specific phone is within its allowance for a specific month
        /// </summary>
        Task<bool> IsPhoneWithinAllowanceAsync(int userPhoneId, decimal amount, int month, int year);

        /// <summary>
        /// Gets overage report for a user in a specific period
        /// </summary>
        Task<OverageReport> GetOverageReportAsync(string indexNumber, int month, int year);

        /// <summary>
        /// Gets all users who are over their allowance in a specific month
        /// </summary>
        Task<List<OverageReport>> GetAllOveragesAsync(int month, int year);

        /// <summary>
        /// Calculates what percentage of allowance has been used
        /// </summary>
        Task<decimal> GetAllowanceUsagePercentageAsync(string indexNumber, int month, int year);

        /// <summary>
        /// Predicts if user will exceed allowance based on current usage pattern
        /// </summary>
        Task<AllowancePrediction> PredictMonthEndUsageAsync(string indexNumber, int month, int year);

        /// <summary>
        /// Gets class of service details for a user
        /// </summary>
        Task<ClassOfService?> GetUserClassOfServiceAsync(string indexNumber);

        /// <summary>
        /// Gets usage breakdown by call type (local, international, mobile, etc.)
        /// </summary>
        Task<UsageBreakdown> GetUsageBreakdownAsync(string indexNumber, int month, int year);
    }

    // ===================================================================
    // DTOs and Report Classes
    // ===================================================================

    public class OverageReport
    {
        public string IndexNumber { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int Month { get; set; }
        public int Year { get; set; }

        public decimal AllowanceLimit { get; set; }
        public decimal TotalUsage { get; set; }
        public decimal OverageAmount { get; set; }
        public decimal OveragePercentage { get; set; }

        public int TotalCalls { get; set; }
        public int OfficialCalls { get; set; }
        public int PersonalCalls { get; set; }

        public bool HasJustification { get; set; }
        public bool SupervisorApproved { get; set; }

        public string ClassOfService { get; set; } = string.Empty;
        public string Office { get; set; } = string.Empty;
    }

    public class AllowancePrediction
    {
        public string IndexNumber { get; set; } = string.Empty;
        public int Month { get; set; }
        public int Year { get; set; }

        public decimal CurrentUsage { get; set; }
        public decimal AllowanceLimit { get; set; }
        public decimal DailyAverageUsage { get; set; }
        public decimal PredictedMonthEndUsage { get; set; }
        public decimal PredictedOverage { get; set; }

        public bool WillExceedAllowance { get; set; }
        public int DaysRemainingInMonth { get; set; }
        public decimal RemainingBudget { get; set; }
        public decimal RecommendedDailyLimit { get; set; }
    }

    public class UsageBreakdown
    {
        public string IndexNumber { get; set; } = string.Empty;
        public int Month { get; set; }
        public int Year { get; set; }

        public decimal TotalCost { get; set; }

        // By Destination Type
        public decimal LocalCost { get; set; }
        public decimal InternationalCost { get; set; }
        public decimal MobileCost { get; set; }
        public decimal PremiumCost { get; set; }

        // By Call Type
        public decimal VoiceCost { get; set; }
        public decimal SMSCost { get; set; }
        public decimal DataCost { get; set; }

        // By Verification Type
        public decimal OfficialCost { get; set; }
        public decimal PersonalCost { get; set; }

        // Call Counts
        public int TotalCalls { get; set; }
        public int LocalCalls { get; set; }
        public int InternationalCalls { get; set; }
        public int MobileCalls { get; set; }

        // Top Destinations
        public List<DestinationSummary> TopDestinations { get; set; } = new();
    }

    public class DestinationSummary
    {
        public string Destination { get; set; } = string.Empty;
        public int CallCount { get; set; }
        public decimal TotalCost { get; set; }
        public decimal AverageCost { get; set; }
    }
}
