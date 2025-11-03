using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TAB.Web.Models.DTOs;

namespace TAB.Web.Services
{
    /// <summary>
    /// Service for handling call log recovery operations based on deadlines and verification status
    /// </summary>
    public interface ICallLogRecoveryService
    {
        /// <summary>
        /// Process all expired verification deadlines for a batch.
        /// Marks unverified calls as PERSONAL when staff miss verification deadline.
        /// </summary>
        Task<RecoveryResult> ProcessExpiredVerificationsAsync(Guid batchId);

        /// <summary>
        /// Process verified personal calls and official calls not submitted to supervisor.
        /// Rule 1A: Verified as Personal → All calls = PERSONAL
        /// Rule 1B: Verified as Official but NOT submitted to supervisor → All calls = PERSONAL
        /// </summary>
        Task<RecoveryResult> ProcessVerifiedButNotSubmittedAsync(Guid batchId);

        /// <summary>
        /// Process expired supervisor approval deadlines.
        /// Marks unapproved calls as CLASS OF SERVICE when supervisor misses approval deadline.
        /// </summary>
        Task<RecoveryResult> ProcessExpiredApprovalsAsync(Guid batchId);

        /// <summary>
        /// Process supervisor partial approval.
        /// Marks approved calls as OFFICIAL, non-approved calls as PERSONAL.
        /// </summary>
        Task<RecoveryResult> ProcessPartialApprovalAsync(int verificationId, List<int> approvedCallIds, string supervisorIndexNumber);

        /// <summary>
        /// Process reverted verifications that missed re-verification deadline.
        /// Marks calls as PERSONAL when staff fails to re-verify after supervisor revert.
        /// </summary>
        Task<RecoveryResult> ProcessRevertedVerificationsAsync(Guid batchId);

        /// <summary>
        /// Process full supervisor approval.
        /// Marks all calls as OFFICIAL (as verified by staff).
        /// </summary>
        Task<RecoveryResult> ProcessFullApprovalAsync(int verificationId, string supervisorIndexNumber);

        /// <summary>
        /// Process supervisor rejection.
        /// Marks all rejected calls as PERSONAL.
        /// </summary>
        Task<RecoveryResult> ProcessRejectionAsync(int verificationId, string supervisorIndexNumber, string rejectionReason);

        /// <summary>
        /// Calculate recovery for a specific staff member and period.
        /// </summary>
        Task<StaffRecoveryDetail> CalculateRecoveryForStaffAsync(string indexNumber, DateTime? startDate, DateTime? endDate);

        /// <summary>
        /// Get class of service amount for a call record.
        /// </summary>
        Task<decimal> CalculateClassOfServiceAmountAsync(int callRecordId);

        /// <summary>
        /// Manually override recovery for a call record.
        /// </summary>
        Task<RecoveryResult> ManualRecoveryOverrideAsync(int callRecordId, string recoveryAction, string reason, string performedBy);

        /// <summary>
        /// Get recovery statistics for a batch.
        /// </summary>
        Task<BatchAnalysisReport> GetBatchRecoveryStatisticsAsync(Guid batchId);
    }
}
