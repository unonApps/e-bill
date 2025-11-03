using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TAB.Web.Models;
using TAB.Web.Models.DTOs;

namespace TAB.Web.Services
{
    /// <summary>
    /// Service for managing verification and approval deadlines
    /// </summary>
    public interface IDeadlineManagementService
    {
        /// <summary>
        /// Create verification deadline for a batch
        /// </summary>
        Task<DeadlineTracking> CreateVerificationDeadlineAsync(Guid batchId, DateTime deadlineDate, string createdBy);

        /// <summary>
        /// Create approval deadline for a batch
        /// </summary>
        Task<DeadlineTracking> CreateApprovalDeadlineAsync(Guid batchId, DateTime deadlineDate, string createdBy);

        /// <summary>
        /// Create revert deadline for a specific staff member
        /// </summary>
        Task<DeadlineTracking> CreateRevertDeadlineAsync(Guid batchId, string indexNumber, DateTime deadlineDate, string createdBy);

        /// <summary>
        /// Get all expired verification deadlines that haven't been processed
        /// </summary>
        Task<List<DeadlineTracking>> GetExpiredVerificationDeadlinesAsync();

        /// <summary>
        /// Get all expired approval deadlines that haven't been processed
        /// </summary>
        Task<List<DeadlineTracking>> GetExpiredApprovalDeadlinesAsync();

        /// <summary>
        /// Get all approaching deadlines (within reminder window)
        /// </summary>
        Task<List<DeadlineInfo>> GetApproachingDeadlinesAsync(int daysAhead = 2);

        /// <summary>
        /// Send deadline reminders to staff/supervisors
        /// </summary>
        Task SendDeadlineRemindersAsync();

        /// <summary>
        /// Extend deadline with approval
        /// </summary>
        Task<bool> ExtendDeadlineAsync(int deadlineId, DateTime newDeadline, string reason, string approvedBy);

        /// <summary>
        /// Cancel a deadline
        /// </summary>
        Task<bool> CancelDeadlineAsync(int deadlineId, string reason, string cancelledBy);

        /// <summary>
        /// Get all deadlines for a batch
        /// </summary>
        Task<List<DeadlineInfo>> GetBatchDeadlinesAsync(Guid batchId);

        /// <summary>
        /// Get deadlines for a specific staff member
        /// </summary>
        Task<List<DeadlineInfo>> GetStaffDeadlinesAsync(string indexNumber);

        /// <summary>
        /// Check if deadline has passed
        /// </summary>
        Task<bool> IsDeadlineExpiredAsync(int deadlineId);

        /// <summary>
        /// Mark deadline as met (staff/supervisor completed on time)
        /// </summary>
        Task MarkDeadlineAsMetAsync(int deadlineId);

        /// <summary>
        /// Get deadline statistics for reporting
        /// </summary>
        Task<DeadlineStatistics> GetDeadlineStatisticsAsync(DateTime? startDate, DateTime? endDate);
    }

    /// <summary>
    /// Statistics about deadline compliance
    /// </summary>
    public class DeadlineStatistics
    {
        public int TotalDeadlines { get; set; }
        public int DeadlinesMet { get; set; }
        public int DeadlinesMissed { get; set; }
        public int DeadlinesExtended { get; set; }
        public int DeadlinesCancelled { get; set; }
        public decimal ComplianceRate { get; set; }
        public int AverageExtensionDays { get; set; }
    }
}
