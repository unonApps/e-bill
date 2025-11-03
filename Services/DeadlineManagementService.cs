using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TAB.Web.Data;
using TAB.Web.Models;
using TAB.Web.Models.DTOs;

namespace TAB.Web.Services
{
    /// <summary>
    /// Service for managing verification and approval deadlines
    /// </summary>
    public class DeadlineManagementService : IDeadlineManagementService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DeadlineManagementService> _logger;
        private readonly INotificationService _notificationService;

        public DeadlineManagementService(
            ApplicationDbContext context,
            ILogger<DeadlineManagementService> logger,
            INotificationService notificationService)
        {
            _context = context;
            _logger = logger;
            _notificationService = notificationService;
        }

        public async Task<DeadlineTracking> CreateVerificationDeadlineAsync(Guid batchId, DateTime deadlineDate, string createdBy)
        {
            _logger.LogInformation("Creating verification deadline for batch {BatchId}: {DeadlineDate}", batchId, deadlineDate);

            var deadline = new DeadlineTracking
            {
                BatchId = batchId,
                DeadlineType = "InitialVerification",
                TargetEntity = "AllStaff",
                DeadlineDate = deadlineDate,
                DeadlineStatus = "Pending",
                CreatedDate = DateTime.UtcNow,
                CreatedBy = createdBy
            };

            _context.DeadlineTracking.Add(deadline);
            await _context.SaveChangesAsync();

            return deadline;
        }

        public async Task<DeadlineTracking> CreateApprovalDeadlineAsync(Guid batchId, DateTime deadlineDate, string createdBy)
        {
            _logger.LogInformation("Creating approval deadline for batch {BatchId}: {DeadlineDate}", batchId, deadlineDate);

            var deadline = new DeadlineTracking
            {
                BatchId = batchId,
                DeadlineType = "SupervisorApproval",
                TargetEntity = "AllSupervisors",
                DeadlineDate = deadlineDate,
                DeadlineStatus = "Pending",
                CreatedDate = DateTime.UtcNow,
                CreatedBy = createdBy
            };

            _context.DeadlineTracking.Add(deadline);
            await _context.SaveChangesAsync();

            return deadline;
        }

        public async Task<DeadlineTracking> CreateRevertDeadlineAsync(Guid batchId, string indexNumber, DateTime deadlineDate, string createdBy)
        {
            _logger.LogInformation(
                "Creating revert deadline for batch {BatchId}, staff {IndexNumber}: {DeadlineDate}",
                batchId,
                indexNumber,
                deadlineDate);

            var deadline = new DeadlineTracking
            {
                BatchId = batchId,
                DeadlineType = "ReVerification",
                TargetEntity = indexNumber,
                DeadlineDate = deadlineDate,
                DeadlineStatus = "Pending",
                CreatedDate = DateTime.UtcNow,
                CreatedBy = createdBy
            };

            _context.DeadlineTracking.Add(deadline);
            await _context.SaveChangesAsync();

            return deadline;
        }

        public async Task<List<DeadlineTracking>> GetExpiredVerificationDeadlinesAsync()
        {
            return await _context.DeadlineTracking
                .Where(dt => (dt.DeadlineType == "InitialVerification" || dt.DeadlineType == "ReVerification")
                          && dt.DeadlineDate < DateTime.UtcNow
                          && dt.DeadlineStatus == "Pending"
                          && !dt.RecoveryProcessed)
                .ToListAsync();
        }

        public async Task<List<DeadlineTracking>> GetExpiredApprovalDeadlinesAsync()
        {
            return await _context.DeadlineTracking
                .Where(dt => dt.DeadlineType == "SupervisorApproval"
                          && dt.DeadlineDate < DateTime.UtcNow
                          && dt.DeadlineStatus == "Pending"
                          && !dt.RecoveryProcessed)
                .ToListAsync();
        }

        public async Task<List<DeadlineInfo>> GetApproachingDeadlinesAsync(int daysAhead = 2)
        {
            var reminderDate = DateTime.UtcNow.AddDays(daysAhead);

            var deadlines = await _context.DeadlineTracking
                .Include(dt => dt.StagingBatch)
                .Where(dt => dt.DeadlineDate <= reminderDate
                          && dt.DeadlineDate > DateTime.UtcNow
                          && dt.DeadlineStatus == "Pending")
                .ToListAsync();

            return deadlines.Select(MapToDeadlineInfo).ToList();
        }

        public async Task SendDeadlineRemindersAsync()
        {
            try
            {
                _logger.LogInformation("Sending deadline reminders");

                // Get configuration for reminder settings
                var config = await _context.RecoveryConfigurations
                    .FirstOrDefaultAsync(rc => rc.RuleName == "DeadlineReminders");

                if (config == null || !config.NotificationEnabled)
                {
                    _logger.LogInformation("Deadline reminders are disabled");
                    return;
                }

                var reminderDays = config.ReminderDaysBefore ?? 2;
                var upcomingDeadlines = await GetApproachingDeadlinesAsync(reminderDays);

                foreach (var deadline in upcomingDeadlines)
                {
                    if (deadline.TargetEntity == "AllStaff")
                    {
                        // Send to all staff with pending verifications
                        await SendStaffDeadlineReminderAsync(deadline);
                    }
                    else if (deadline.TargetEntity == "AllSupervisors")
                    {
                        // Send to all supervisors with pending approvals
                        await SendSupervisorDeadlineReminderAsync(deadline);
                    }
                    else
                    {
                        // Send to specific staff member
                        await SendIndividualDeadlineReminderAsync(deadline.TargetEntity, deadline);
                    }
                }

                _logger.LogInformation("Sent {Count} deadline reminders", upcomingDeadlines.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending deadline reminders");
            }
        }

        public async Task<bool> ExtendDeadlineAsync(int deadlineId, DateTime newDeadline, string reason, string approvedBy)
        {
            try
            {
                var deadline = await _context.DeadlineTracking.FindAsync(deadlineId);
                if (deadline == null)
                {
                    _logger.LogWarning("Deadline {DeadlineId} not found", deadlineId);
                    return false;
                }

                _logger.LogInformation(
                    "Extending deadline {DeadlineId} from {OldDate} to {NewDate}",
                    deadlineId,
                    deadline.DeadlineDate,
                    newDeadline);

                deadline.ExtendedDeadline = newDeadline;
                deadline.ExtensionReason = reason;
                deadline.ExtensionApprovedBy = approvedBy;
                deadline.ExtensionApprovedDate = DateTime.UtcNow;
                deadline.DeadlineStatus = "Extended";

                await _context.SaveChangesAsync();

                // Send notification about extension
                await NotifyDeadlineExtensionAsync(deadline, newDeadline, reason);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extending deadline {DeadlineId}", deadlineId);
                return false;
            }
        }

        public async Task<bool> CancelDeadlineAsync(int deadlineId, string reason, string cancelledBy)
        {
            try
            {
                var deadline = await _context.DeadlineTracking.FindAsync(deadlineId);
                if (deadline == null)
                {
                    _logger.LogWarning("Deadline {DeadlineId} not found", deadlineId);
                    return false;
                }

                _logger.LogInformation("Cancelling deadline {DeadlineId}", deadlineId);

                deadline.DeadlineStatus = "Cancelled";
                deadline.Notes = $"Cancelled by {cancelledBy}. Reason: {reason}";

                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling deadline {DeadlineId}", deadlineId);
                return false;
            }
        }

        public async Task<List<DeadlineInfo>> GetBatchDeadlinesAsync(Guid batchId)
        {
            var deadlines = await _context.DeadlineTracking
                .Include(dt => dt.StagingBatch)
                .Where(dt => dt.BatchId == batchId)
                .OrderBy(dt => dt.DeadlineDate)
                .ToListAsync();

            return deadlines.Select(MapToDeadlineInfo).ToList();
        }

        public async Task<List<DeadlineInfo>> GetStaffDeadlinesAsync(string indexNumber)
        {
            var deadlines = await _context.DeadlineTracking
                .Include(dt => dt.StagingBatch)
                .Where(dt => dt.TargetEntity == indexNumber || dt.TargetEntity == "AllStaff")
                .Where(dt => dt.DeadlineStatus == "Pending" || dt.DeadlineStatus == "Extended")
                .OrderBy(dt => dt.DeadlineDate)
                .ToListAsync();

            return deadlines.Select(MapToDeadlineInfo).ToList();
        }

        public async Task<bool> IsDeadlineExpiredAsync(int deadlineId)
        {
            var deadline = await _context.DeadlineTracking.FindAsync(deadlineId);
            if (deadline == null)
            {
                return false;
            }

            var effectiveDeadline = deadline.ExtendedDeadline ?? deadline.DeadlineDate;
            return DateTime.UtcNow > effectiveDeadline;
        }

        public async Task MarkDeadlineAsMetAsync(int deadlineId)
        {
            var deadline = await _context.DeadlineTracking.FindAsync(deadlineId);
            if (deadline != null)
            {
                deadline.DeadlineStatus = "Met";
                await _context.SaveChangesAsync();

                _logger.LogInformation("Marked deadline {DeadlineId} as met", deadlineId);
            }
        }

        public async Task<DeadlineStatistics> GetDeadlineStatisticsAsync(DateTime? startDate, DateTime? endDate)
        {
            var query = _context.DeadlineTracking.AsQueryable();

            if (startDate.HasValue)
                query = query.Where(dt => dt.DeadlineDate >= startDate.Value);
            if (endDate.HasValue)
                query = query.Where(dt => dt.DeadlineDate <= endDate.Value);

            var deadlines = await query.ToListAsync();

            var stats = new DeadlineStatistics
            {
                TotalDeadlines = deadlines.Count,
                DeadlinesMet = deadlines.Count(d => d.DeadlineStatus == "Met"),
                DeadlinesMissed = deadlines.Count(d => d.DeadlineStatus == "Missed"),
                DeadlinesExtended = deadlines.Count(d => d.DeadlineStatus == "Extended"),
                DeadlinesCancelled = deadlines.Count(d => d.DeadlineStatus == "Cancelled")
            };

            if (stats.TotalDeadlines > 0)
            {
                stats.ComplianceRate = (decimal)stats.DeadlinesMet / stats.TotalDeadlines * 100;
            }

            var extensions = deadlines.Where(d => d.ExtendedDeadline != null).ToList();
            if (extensions.Any())
            {
                stats.AverageExtensionDays = (int)extensions
                    .Average(d => (d.ExtendedDeadline!.Value - d.DeadlineDate).TotalDays);
            }

            return stats;
        }

        #region Private Helper Methods

        private DeadlineInfo MapToDeadlineInfo(DeadlineTracking deadline)
        {
            var effectiveDeadline = deadline.ExtendedDeadline ?? deadline.DeadlineDate;
            var timeRemaining = effectiveDeadline - DateTime.UtcNow;

            return new DeadlineInfo
            {
                Id = deadline.Id,
                BatchId = deadline.BatchId,
                BatchName = deadline.StagingBatch?.BatchName ?? "Unknown",
                DeadlineType = deadline.DeadlineType,
                TargetEntity = deadline.TargetEntity,
                DeadlineDate = deadline.DeadlineDate,
                ExtendedDeadline = deadline.ExtendedDeadline,
                DeadlineStatus = deadline.DeadlineStatus,
                DaysRemaining = (int)timeRemaining.TotalDays,
                HoursRemaining = (int)timeRemaining.TotalHours,
                IsOverdue = timeRemaining.TotalHours < 0,
                IsApproaching = timeRemaining.TotalHours > 0 && timeRemaining.TotalHours <= 48,
                Notes = deadline.Notes ?? string.Empty
            };
        }

        private async Task SendStaffDeadlineReminderAsync(DeadlineInfo deadline)
        {
            try
            {
                // Get all staff with pending verifications for this batch
                var staffWithPending = await _context.CallRecords
                    .Where(cr => cr.SourceBatchId == deadline.BatchId
                              && (cr.AssignmentStatus == "None" || cr.AssignmentStatus == null))
                    .Select(cr => cr.ResponsibleIndexNumber)
                    .Distinct()
                    .ToListAsync();

                foreach (var indexNumber in staffWithPending)
                {
                    if (string.IsNullOrEmpty(indexNumber)) continue;

                    var user = await _context.EbillUsers.FirstOrDefaultAsync(u => u.IndexNumber == indexNumber);
                    if (user != null)
                    {
                        await _notificationService.CreateNotificationAsync(
                            user.Id.ToString(),
                            "Call Verification Deadline Reminder",
                            $"Reminder: Verification deadline for batch '{deadline.BatchName}' is approaching ({deadline.DeadlineDate:yyyy-MM-dd HH:mm}). Please verify your calls.",
                            NotificationType.Info
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending staff deadline reminders for batch {BatchId}", deadline.BatchId);
            }
        }

        private async Task SendSupervisorDeadlineReminderAsync(DeadlineInfo deadline)
        {
            try
            {
                // Get all supervisors with pending approvals for this batch
                var supervisorsWithPending = await _context.CallLogVerifications
                    .Where(v => v.BatchId == deadline.BatchId
                             && v.SubmittedToSupervisor
                             && (v.SupervisorApprovalStatus == "Pending" || v.SupervisorApprovalStatus == null))
                    .Select(v => v.SupervisorIndexNumber)
                    .Distinct()
                    .ToListAsync();

                foreach (var indexNumber in supervisorsWithPending)
                {
                    if (string.IsNullOrEmpty(indexNumber)) continue;

                    var user = await _context.EbillUsers.FirstOrDefaultAsync(u => u.IndexNumber == indexNumber);
                    if (user != null)
                    {
                        await _notificationService.CreateNotificationAsync(
                            user.Id.ToString(),
                            "Call Approval Deadline Reminder",
                            $"Reminder: Approval deadline for batch '{deadline.BatchName}' is approaching ({deadline.DeadlineDate:yyyy-MM-dd HH:mm}). Please review pending verifications.",
                            NotificationType.Info
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending supervisor deadline reminders for batch {BatchId}", deadline.BatchId);
            }
        }

        private async Task SendIndividualDeadlineReminderAsync(string indexNumber, DeadlineInfo deadline)
        {
            try
            {
                var user = await _context.EbillUsers.FirstOrDefaultAsync(u => u.IndexNumber == indexNumber);
                if (user != null)
                {
                    await _notificationService.CreateNotificationAsync(
                        user.Id.ToString(),
                        "Re-Verification Deadline Reminder",
                        $"Reminder: Re-verification deadline for batch '{deadline.BatchName}' is approaching ({deadline.DeadlineDate:yyyy-MM-dd HH:mm}). Please complete re-verification.",
                        NotificationType.Warning
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending individual deadline reminder to {IndexNumber}", indexNumber);
            }
        }

        private async Task NotifyDeadlineExtensionAsync(DeadlineTracking deadline, DateTime newDeadline, string reason)
        {
            try
            {
                if (deadline.TargetEntity == "AllStaff" || deadline.TargetEntity == "AllSupervisors")
                {
                    // Batch notification - would need to get all affected users
                    _logger.LogInformation("Deadline extended for all {Target}", deadline.TargetEntity);
                }
                else
                {
                    // Individual notification
                    var user = await _context.EbillUsers.FirstOrDefaultAsync(u => u.IndexNumber == deadline.TargetEntity);
                    if (user != null)
                    {
                        await _notificationService.CreateNotificationAsync(
                            user.Id.ToString(),
                            "Deadline Extended",
                            $"Your deadline has been extended to {newDeadline:yyyy-MM-dd HH:mm}. Reason: {reason}",
                            NotificationType.Info
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending deadline extension notification");
            }
        }

        #endregion
    }
}
