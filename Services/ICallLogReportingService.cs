using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TAB.Web.Models.DTOs;

namespace TAB.Web.Services
{
    /// <summary>
    /// Service for generating call log recovery reports
    /// </summary>
    public interface ICallLogReportingService
    {
        /// <summary>
        /// Generate recovery summary report
        /// </summary>
        Task<RecoverySummaryReport> GetRecoverySummaryAsync(ReportFilter filter);

        /// <summary>
        /// Get detailed staff recovery report
        /// </summary>
        Task<List<StaffRecoveryDetail>> GetStaffRecoveryDetailsAsync(ReportFilter filter);

        /// <summary>
        /// Get supervisor activity report
        /// </summary>
        Task<List<SupervisorActivityReport>> GetSupervisorActivityAsync(ReportFilter filter);

        /// <summary>
        /// Get batch analysis report
        /// </summary>
        Task<List<BatchAnalysisReport>> GetBatchAnalysisAsync(ReportFilter filter);

        /// <summary>
        /// Get all recovery logs with filters
        /// </summary>
        Task<(List<Models.RecoveryLog> Logs, int TotalCount)> GetRecoveryLogsAsync(ReportFilter filter);

        /// <summary>
        /// Export report to Excel format
        /// </summary>
        Task<byte[]> ExportToExcelAsync(string reportType, ReportFilter filter);

        /// <summary>
        /// Get recovery trend data for charts (by month/week)
        /// </summary>
        Task<List<RecoveryTrendData>> GetRecoveryTrendDataAsync(DateTime startDate, DateTime endDate, string groupBy = "month");

        /// <summary>
        /// Get top staff by personal recovery amount
        /// </summary>
        Task<List<StaffRecoveryDetail>> GetTopStaffByPersonalRecoveryAsync(int topCount = 10, DateTime? startDate = null, DateTime? endDate = null);

        /// <summary>
        /// Get department-wise recovery summary
        /// </summary>
        Task<List<DepartmentRecoverySummary>> GetDepartmentRecoverySummaryAsync(ReportFilter filter);

        /// <summary>
        /// Get compliance metrics
        /// </summary>
        Task<ComplianceMetrics> GetComplianceMetricsAsync(DateTime? startDate, DateTime? endDate);
    }

    /// <summary>
    /// Recovery trend data for charting
    /// </summary>
    public class RecoveryTrendData
    {
        public string Period { get; set; } = string.Empty; // "2025-10", "Week 41", etc.
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public decimal PersonalAmount { get; set; }
        public decimal OfficialAmount { get; set; }
        public decimal ClassOfServiceAmount { get; set; }
        public int TotalCalls { get; set; }
    }

    /// <summary>
    /// Department-wise recovery summary
    /// </summary>
    public class DepartmentRecoverySummary
    {
        public string Department { get; set; } = string.Empty;
        public int StaffCount { get; set; }
        public int TotalCalls { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PersonalAmount { get; set; }
        public decimal OfficialAmount { get; set; }
        public decimal ClassOfServiceAmount { get; set; }
        public int MissedDeadlines { get; set; }
        public decimal ComplianceRate { get; set; }
    }

    /// <summary>
    /// Compliance metrics for the organization
    /// </summary>
    public class ComplianceMetrics
    {
        public int TotalVerifications { get; set; }
        public int VerificationsOnTime { get; set; }
        public int VerificationsMissedDeadline { get; set; }
        public decimal StaffComplianceRate { get; set; }

        public int TotalApprovals { get; set; }
        public int ApprovalsOnTime { get; set; }
        public int ApprovalsMissedDeadline { get; set; }
        public decimal SupervisorComplianceRate { get; set; }

        public decimal OverallComplianceRate { get; set; }

        public int TotalReverts { get; set; }
        public int RevertsResolvedOnTime { get; set; }
        public decimal RevertResolutionRate { get; set; }
    }
}
