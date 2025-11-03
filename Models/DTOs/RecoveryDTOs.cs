using System;
using System.Collections.Generic;

namespace TAB.Web.Models.DTOs
{
    /// <summary>
    /// Result of a recovery operation
    /// </summary>
    public class RecoveryResult
    {
        public bool Success { get; set; }
        public int RecordsProcessed { get; set; }
        public decimal AmountRecovered { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<string> Errors { get; set; } = new List<string>();
        public Dictionary<string, object>? AdditionalData { get; set; }

        public static RecoveryResult CreateSuccess(int recordsProcessed, decimal amountRecovered, string message = "Recovery completed successfully")
        {
            return new RecoveryResult
            {
                Success = true,
                RecordsProcessed = recordsProcessed,
                AmountRecovered = amountRecovered,
                Message = message
            };
        }

        public static RecoveryResult CreateFailure(string errorMessage)
        {
            return new RecoveryResult
            {
                Success = false,
                RecordsProcessed = 0,
                AmountRecovered = 0,
                Message = $"Recovery failed: {errorMessage}",
                Errors = new List<string> { errorMessage }
            };
        }
    }

    /// <summary>
    /// Information about an expired deadline
    /// </summary>
    public class ExpiredDeadline
    {
        public int Id { get; set; }
        public Guid BatchId { get; set; }
        public string DeadlineType { get; set; } = string.Empty;
        public string TargetEntity { get; set; } = string.Empty;
        public DateTime DeadlineDate { get; set; }
        public DateTime? ExtendedDeadline { get; set; }
        public bool RecoveryProcessed { get; set; }
    }

    /// <summary>
    /// Filter parameters for reports
    /// </summary>
    public class ReportFilter
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? IndexNumber { get; set; }
        public Guid? BatchId { get; set; }
        public string? RecoveryType { get; set; }
        public string? RecoveryAction { get; set; }
        public string? Department { get; set; }
        public string? Office { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }

    /// <summary>
    /// Summary report of recovery operations
    /// </summary>
    public class RecoverySummaryReport
    {
        public int TotalRecords { get; set; }
        public decimal TotalAmountRecovered { get; set; }
        public RecoveryBreakdown PersonalRecovery { get; set; } = new RecoveryBreakdown();
        public RecoveryBreakdown OfficialRecovery { get; set; } = new RecoveryBreakdown();
        public RecoveryBreakdown ClassOfServiceRecovery { get; set; } = new RecoveryBreakdown();
        public List<RecoveryTypeBreakdown> RecoveryByType { get; set; } = new List<RecoveryTypeBreakdown>();
        public DateTime ReportGeneratedDate { get; set; } = DateTime.UtcNow;
        public ReportFilter? AppliedFilters { get; set; }
    }

    /// <summary>
    /// Breakdown of recovery by action type
    /// </summary>
    public class RecoveryBreakdown
    {
        public int Count { get; set; }
        public decimal Amount { get; set; }
        public decimal Percentage { get; set; }
    }

    /// <summary>
    /// Breakdown of recovery by recovery type (reason)
    /// </summary>
    public class RecoveryTypeBreakdown
    {
        public string Type { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal Amount { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    /// <summary>
    /// Detailed recovery information for a staff member
    /// </summary>
    public class StaffRecoveryDetail
    {
        public string IndexNumber { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string Office { get; set; } = string.Empty;

        // Totals
        public int TotalCalls { get; set; }
        public decimal TotalAmount { get; set; }

        // Personal calls
        public int PersonalCalls { get; set; }
        public decimal PersonalAmount { get; set; }

        // Official calls
        public int OfficialCalls { get; set; }
        public decimal OfficialAmount { get; set; }

        // Class of Service calls
        public int ClassOfServiceCalls { get; set; }
        public decimal ClassOfServiceAmount { get; set; }

        // Metrics
        public int MissedDeadlines { get; set; }
        public int TimesReverted { get; set; }
        public decimal ComplianceRate { get; set; }
    }

    /// <summary>
    /// Supervisor activity report
    /// </summary>
    public class SupervisorActivityReport
    {
        public string SupervisorIndexNumber { get; set; } = string.Empty;
        public string SupervisorName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        // Activity metrics
        public int TotalSubmissionsReceived { get; set; }
        public int SubmissionsApproved { get; set; }
        public int SubmissionsPartiallyApproved { get; set; }
        public int SubmissionsRejected { get; set; }
        public int SubmissionsReverted { get; set; }
        public int MissedDeadlines { get; set; }

        // Response metrics
        public double AverageResponseTimeHours { get; set; }
        public decimal ApprovalRate { get; set; }
        public int StaffSupervised { get; set; }

        // Amounts
        public decimal TotalAmountReviewed { get; set; }
        public decimal AmountApproved { get; set; }
    }

    /// <summary>
    /// Deadline tracking information
    /// </summary>
    public class DeadlineInfo
    {
        public int Id { get; set; }
        public Guid BatchId { get; set; }
        public string BatchName { get; set; } = string.Empty;
        public string DeadlineType { get; set; } = string.Empty;
        public string TargetEntity { get; set; } = string.Empty;
        public string TargetName { get; set; } = string.Empty;
        public DateTime DeadlineDate { get; set; }
        public DateTime? ExtendedDeadline { get; set; }
        public string DeadlineStatus { get; set; } = string.Empty;
        public int DaysRemaining { get; set; }
        public int HoursRemaining { get; set; }
        public bool IsOverdue { get; set; }
        public bool IsApproaching { get; set; } // Within reminder window
        public string Notes { get; set; } = string.Empty;
    }

    /// <summary>
    /// Batch analysis report
    /// </summary>
    public class BatchAnalysisReport
    {
        public Guid BatchId { get; set; }
        public string BatchName { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public DateTime? VerificationDeadline { get; set; }
        public DateTime? ApprovalDeadline { get; set; }

        // Call statistics
        public int TotalCalls { get; set; }
        public decimal TotalAmount { get; set; }
        public int VerifiedCalls { get; set; }
        public int UnverifiedCalls { get; set; }

        // Recovery statistics
        public int PersonalRecoveryCalls { get; set; }
        public decimal PersonalRecoveryAmount { get; set; }
        public int OfficialRecoveryCalls { get; set; }
        public decimal OfficialRecoveryAmount { get; set; }
        public int ClassOfServiceCalls { get; set; }
        public decimal ClassOfServiceAmount { get; set; }

        // Compliance metrics
        public int StaffWhoVerified { get; set; }
        public int StaffWhoMissedDeadline { get; set; }
        public decimal StaffComplianceRate { get; set; }
        public int SupervisorsWhoApproved { get; set; }
        public int SupervisorsWhoMissedDeadline { get; set; }
        public decimal SupervisorComplianceRate { get; set; }
    }

    /// <summary>
    /// Request to extend a deadline
    /// </summary>
    public class DeadlineExtensionRequest
    {
        public int DeadlineId { get; set; }
        public DateTime NewDeadline { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string RequestedBy { get; set; } = string.Empty;
    }

    /// <summary>
    /// Result of verification processing
    /// </summary>
    public class VerificationProcessResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int CallsProcessed { get; set; }
        public int CallsMarkedPersonal { get; set; }
        public int CallsMarkedOfficial { get; set; }
        public decimal TotalAmount { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
    }
}
