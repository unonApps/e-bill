using TAB.Web.Models;
using TAB.Web.Models.Enums;

namespace TAB.Web.Services
{
    public interface ICallLogVerificationService
    {
        // ===================================================================
        // User Verification Operations
        // ===================================================================

        /// <summary>
        /// Verifies a call log record
        /// </summary>
        Task<CallLogVerification> VerifyCallLogAsync(
            int callRecordId,
            string indexNumber,
            VerificationType verificationType,
            string? justification = null,
            List<IFormFile>? documents = null);

        /// <summary>
        /// Gets all verifications for a specific user
        /// </summary>
        Task<List<CallLogVerification>> GetUserVerificationsAsync(
            string indexNumber,
            bool pendingOnly = false);

        /// <summary>
        /// Gets a specific verification by ID
        /// </summary>
        Task<CallLogVerification?> GetVerificationByIdAsync(int verificationId);

        /// <summary>
        /// Updates an existing verification
        /// </summary>
        Task<bool> UpdateVerificationAsync(CallLogVerification verification);

        /// <summary>
        /// Deletes a verification (only if not submitted to supervisor)
        /// </summary>
        Task<bool> DeleteVerificationAsync(int verificationId, string indexNumber);

        // ===================================================================
        // Overage Detection & Calculation
        // ===================================================================

        /// <summary>
        /// Checks if a call record exceeds the allowance for the user's class of service
        /// </summary>
        Task<bool> IsOverageAsync(int callRecordId, string indexNumber);

        /// <summary>
        /// Gets the remaining allowance for a user in a specific month
        /// </summary>
        Task<decimal> GetRemainingAllowanceAsync(string indexNumber, int month, int year);

        /// <summary>
        /// Calculates total usage for a user in a specific month
        /// </summary>
        Task<decimal> GetMonthlyUsageAsync(string indexNumber, int month, int year);

        // ===================================================================
        // Payment Assignment Operations
        // ===================================================================

        /// <summary>
        /// Assigns payment responsibility to another user
        /// </summary>
        Task<CallLogPaymentAssignment> AssignPaymentAsync(
            int callRecordId,
            string assignedFrom,
            string assignedTo,
            string reason);

        /// <summary>
        /// Accepts a payment assignment
        /// </summary>
        Task<bool> AcceptPaymentAssignmentAsync(int assignmentId, string indexNumber);

        /// <summary>
        /// Rejects a payment assignment with a reason
        /// </summary>
        Task<bool> RejectPaymentAssignmentAsync(int assignmentId, string indexNumber, string reason);

        /// <summary>
        /// Gets all pending payment assignments for a user
        /// </summary>
        Task<List<CallLogPaymentAssignment>> GetPendingAssignmentsAsync(string indexNumber);

        /// <summary>
        /// Gets assignment history for a call record
        /// </summary>
        Task<List<CallLogPaymentAssignment>> GetAssignmentHistoryAsync(int callRecordId);

        // ===================================================================
        // Supervisor Approval Operations
        // ===================================================================

        /// <summary>
        /// Submits verified call logs to supervisor for approval
        /// </summary>
        Task<int> SubmitToSupervisorAsync(List<int> verificationIds, string indexNumber);

        /// <summary>
        /// Gets all pending approvals for a supervisor
        /// </summary>
        Task<List<CallLogVerification>> GetSupervisorPendingApprovalsAsync(string supervisorIndexNumber);

        /// <summary>
        /// Approves a verification (full or partial)
        /// </summary>
        Task<bool> ApproveVerificationAsync(
            int verificationId,
            string supervisorIndexNumber,
            decimal? approvedAmount = null,
            string? comments = null);

        /// <summary>
        /// Rejects a verification with a reason
        /// </summary>
        Task<bool> RejectVerificationAsync(
            int verificationId,
            string supervisorIndexNumber,
            string reason);

        /// <summary>
        /// Reverts a verification back to the user for corrections
        /// </summary>
        Task<bool> RevertVerificationAsync(
            int verificationId,
            string supervisorIndexNumber,
            string reason);

        /// <summary>
        /// Batch approve multiple verifications
        /// </summary>
        Task<int> BatchApproveVerificationsAsync(
            List<int> verificationIds,
            string supervisorIndexNumber);

        // ===================================================================
        // Reporting & Analytics
        // ===================================================================

        /// <summary>
        /// Gets verification summary for a user in a specific period
        /// </summary>
        Task<VerificationSummary> GetVerificationSummaryAsync(
            string indexNumber,
            int month,
            int year);

        /// <summary>
        /// Gets compliance rate for verification deadline
        /// </summary>
        Task<decimal> GetVerificationComplianceRateAsync(int month, int year);

        /// <summary>
        /// Gets all verifications that are overdue
        /// </summary>
        Task<List<CallLogVerification>> GetOverdueVerificationsAsync();
    }

    // ===================================================================
    // DTOs and Summary Classes
    // ===================================================================

    public class VerificationSummary
    {
        public string IndexNumber { get; set; } = string.Empty;
        public int Month { get; set; }
        public int Year { get; set; }

        // Counts
        public int TotalCalls { get; set; }
        public int VerifiedCalls { get; set; }
        public int UnverifiedCalls { get; set; }
        public int PersonalCalls { get; set; }
        public int OfficialCalls { get; set; }

        // Amounts
        public decimal TotalAmount { get; set; }
        public decimal VerifiedAmount { get; set; }
        public decimal PersonalAmount { get; set; }
        public decimal OfficialAmount { get; set; }

        // Class of Service
        public decimal AllowanceLimit { get; set; }
        public decimal TotalUsage { get; set; }
        public decimal RemainingAllowance { get; set; }
        public bool IsOverAllowance { get; set; }
        public decimal OverageAmount { get; set; }

        // Approval Status
        public int PendingApproval { get; set; }
        public int Approved { get; set; }
        public int Rejected { get; set; }
        public int PartiallyApproved { get; set; }

        // Payment Assignments
        public int AssignedToOthers { get; set; }
        public int AssignedFromOthers { get; set; }

        // Compliance
        public DateTime? VerificationDeadline { get; set; }
        public bool IsOverdue { get; set; }
        public decimal CompliancePercentage { get; set; }
    }
}
