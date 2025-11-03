using System;

namespace TAB.Web.Models.DTOs
{
    /// <summary>
    /// Recovery breakdown by type (Personal, Official, Class of Service)
    /// </summary>
    public class RecoveryByTypeDTO
    {
        public decimal PersonalKSH { get; set; }
        public decimal PersonalUSD { get; set; }
        public int PersonalCallCount { get; set; }
        public decimal PersonalPercentage { get; set; }

        public decimal OfficialKSH { get; set; }
        public decimal OfficialUSD { get; set; }
        public int OfficialCallCount { get; set; }
        public decimal OfficialPercentage { get; set; }

        public decimal ClassOfServiceKSH { get; set; }
        public decimal ClassOfServiceUSD { get; set; }
        public int COSCallCount { get; set; }
        public decimal COSPercentage { get; set; }

        public decimal TotalKSH => PersonalKSH + OfficialKSH + ClassOfServiceKSH;
        public decimal TotalUSD => PersonalUSD + OfficialUSD + ClassOfServiceUSD;
        public int TotalCalls => PersonalCallCount + OfficialCallCount + COSCallCount;
    }

    /// <summary>
    /// Recovery breakdown by telecom provider with multi-currency support
    /// </summary>
    public class RecoveryByProviderDTO
    {
        public string Provider { get; set; } = string.Empty;
        public string NativeCurrency { get; set; } = string.Empty;
        public decimal AmountInNativeCurrency { get; set; }
        public decimal AmountInKSH { get; set; }
        public decimal AmountInUSD { get; set; }
        public int CallCount { get; set; }
        public decimal AvgPerCall { get; set; }
        public decimal PercentageOfTotal { get; set; }
    }

    /// <summary>
    /// Recovery summary by organization/agency
    /// </summary>
    public class RecoveryByOrganizationDTO
    {
        public int OrganizationId { get; set; }
        public string OrganizationName { get; set; } = string.Empty;
        public decimal TotalKSH { get; set; }
        public decimal TotalUSD { get; set; }

        public int PersonalCount { get; set; }
        public int OfficialCount { get; set; }
        public int COSCount { get; set; }
        public int TotalCalls => PersonalCount + OfficialCount + COSCount;

        public int UserCount { get; set; }
        public decimal AvgPerUser => UserCount > 0 ? TotalKSH / UserCount : 0;

        public decimal PersonalPercentage => TotalCalls > 0 ? (decimal)PersonalCount / TotalCalls * 100 : 0;
        public decimal OfficialPercentage => TotalCalls > 0 ? (decimal)OfficialCount / TotalCalls * 100 : 0;
        public decimal COSPercentage => TotalCalls > 0 ? (decimal)COSCount / TotalCalls * 100 : 0;

        public decimal ComplianceRate { get; set; }
    }

    /// <summary>
    /// Recovery breakdown by office within an organization
    /// </summary>
    public class RecoveryByOfficeDTO
    {
        public int OfficeId { get; set; }
        public string OfficeName { get; set; } = string.Empty;
        public decimal TotalKSH { get; set; }
        public decimal TotalUSD { get; set; }
        public int UserCount { get; set; }
        public decimal AvgPerUser => UserCount > 0 ? TotalKSH / UserCount : 0;
        public int TotalCalls { get; set; }
        public decimal ComplianceRate { get; set; }
    }

    /// <summary>
    /// Top users by recovery amount
    /// </summary>
    public class TopUserRecoveryDTO
    {
        public int Rank { get; set; }
        public string IndexNumber { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string OrganizationName { get; set; } = string.Empty;
        public string OfficeName { get; set; } = string.Empty;
        public decimal TotalKSH { get; set; }
        public decimal TotalUSD { get; set; }
        public int CallCount { get; set; }

        public int PersonalCalls { get; set; }
        public int OfficialCalls { get; set; }
        public int COSCalls { get; set; }

        public string PrimaryType
        {
            get
            {
                if (PersonalCalls > OfficialCalls && PersonalCalls > COSCalls) return "Personal";
                if (OfficialCalls > PersonalCalls && OfficialCalls > COSCalls) return "Official";
                if (COSCalls > PersonalCalls && COSCalls > OfficialCalls) return "COS";
                return "Mixed";
            }
        }
    }

    /// <summary>
    /// Recovery analysis by Class of Service
    /// </summary>
    public class RecoveryByCOSDTO
    {
        public int COSId { get; set; }
        public string COSClass { get; set; } = string.Empty;
        public string COSService { get; set; } = string.Empty;
        public decimal AllowanceKSH { get; set; }
        public decimal TotalUsedKSH { get; set; }
        public decimal RecoveredKSH { get; set; }
        public decimal RecoveredUSD { get; set; }
        public int UserCount { get; set; }
        public decimal AvgOveragePerUser => UserCount > 0 ? RecoveredKSH / UserCount : 0;
        public decimal OveragePercentage => AllowanceKSH > 0 ? (RecoveredKSH / AllowanceKSH) * 100 : 0;
    }

    /// <summary>
    /// Supervisor performance metrics
    /// </summary>
    public class SupervisorPerformanceDTO
    {
        public string SupervisorIndex { get; set; } = string.Empty;
        public string SupervisorName { get; set; } = string.Empty;
        public int TeamSize { get; set; }
        public int PendingCount { get; set; }
        public decimal ApprovedAmountKSH { get; set; }
        public decimal ApprovedAmountUSD { get; set; }
        public decimal RecoveredAmountKSH { get; set; }
        public decimal RecoveredAmountUSD { get; set; }

        public decimal ApprovalRate
        {
            get
            {
                var total = ApprovedAmountKSH + RecoveredAmountKSH;
                return total > 0 ? (ApprovedAmountKSH / total) * 100 : 0;
            }
        }

        public decimal AvgDaysToApprove { get; set; }
    }

    /// <summary>
    /// Recovery by reason/source
    /// </summary>
    public class RecoveryByReasonDTO
    {
        public string RecoveryType { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public decimal AmountKSH { get; set; }
        public decimal AmountUSD { get; set; }
        public int Count { get; set; }
        public decimal Percentage { get; set; }
    }

    /// <summary>
    /// Dashboard alert/action item
    /// </summary>
    public class DashboardAlertDTO
    {
        public string AlertType { get; set; } = string.Empty;
        public string Priority { get; set; } = "Medium"; // High, Medium, Low
        public string Message { get; set; } = string.Empty;
        public int AffectedCount { get; set; }
        public decimal AmountAtRisk { get; set; }
        public string Icon { get; set; } = "bi-exclamation-triangle";
        public string Link { get; set; } = string.Empty;
    }

    /// <summary>
    /// Filter parameters for dashboard queries
    /// </summary>
    public class DashboardFilterParams
    {
        public DateTime StartDate { get; set; } = DateTime.UtcNow.AddDays(-30);
        public DateTime EndDate { get; set; } = DateTime.UtcNow;

        public List<int> OrganizationIds { get; set; } = new();
        public List<int> OfficeIds { get; set; } = new();
        public string? UserIndexNumber { get; set; }

        public List<string> Providers { get; set; } = new(); // Safaricom, Airtel, PSTN, PrivateWire
        public List<string> RecoveryTypes { get; set; } = new(); // Personal, Official, ClassOfService

        public bool HasFilters => OrganizationIds.Any() || OfficeIds.Any() ||
                                  !string.IsNullOrEmpty(UserIndexNumber) ||
                                  Providers.Any() || RecoveryTypes.Any();
    }

    /// <summary>
    /// Enhanced dashboard metrics with currency support
    /// </summary>
    public class EnhancedDashboardMetrics
    {
        // Overall totals
        public decimal TotalAmountRecoveredKSH { get; set; }
        public decimal TotalAmountRecoveredUSD { get; set; }
        public decimal AmountRecoveredLast30DaysKSH { get; set; }
        public decimal AmountRecoveredLast30DaysUSD { get; set; }
        public decimal AmountRecoveredLast7DaysKSH { get; set; }
        public decimal AmountRecoveredLast7DaysUSD { get; set; }

        // Breakdown by type
        public decimal PersonalRecoveryKSH { get; set; }
        public decimal PersonalRecoveryUSD { get; set; }
        public decimal PersonalRecoveryPercentage { get; set; }

        public decimal OfficialRecoveryKSH { get; set; }
        public decimal OfficialRecoveryUSD { get; set; }
        public decimal OfficialRecoveryPercentage { get; set; }

        public decimal COSRecoveryKSH { get; set; }
        public decimal COSRecoveryUSD { get; set; }
        public decimal COSRecoveryPercentage { get; set; }

        // At-risk amounts
        public decimal AtRiskAmountKSH { get; set; }
        public decimal AtRiskAmountUSD { get; set; }
        public int ExpiringSoon { get; set; }

        // Counts
        public int TotalRecordsProcessed { get; set; }
        public int RecordsProcessedLast30Days { get; set; }
        public int TotalBatchesProcessed { get; set; }
        public int ActiveBatchesWithDeadlines { get; set; }

        // Rates
        public decimal SuccessRate { get; set; }
        public decimal TrendPercentage { get; set; }
        public decimal AverageRecoveryPerBatchKSH { get; set; }
        public decimal AverageRecoveryPerBatchUSD { get; set; }
    }
}
