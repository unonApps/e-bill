using Microsoft.EntityFrameworkCore;
using TAB.Web.Data;
using TAB.Web.Models;

namespace TAB.Web.Services
{
    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            ApplicationDbContext context,
            ILogger<NotificationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Notification> CreateNotificationAsync(
            string userId,
            string title,
            string message,
            NotificationType type = NotificationType.Info,
            string? link = null,
            string? icon = null,
            string? relatedEntityType = null,
            string? relatedEntityId = null)
        {
            try
            {
                var notification = new Notification
                {
                    UserId = userId,
                    Title = title,
                    Message = message,
                    Type = type.ToString(),
                    Link = link,
                    Icon = icon ?? GetDefaultIcon(type),
                    RelatedEntityType = relatedEntityType,
                    RelatedEntityId = relatedEntityId,
                    IsRead = false,
                    CreatedDate = DateTime.UtcNow
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Notification created for user {userId}: {title}");
                return notification;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating notification for user {userId}");
                throw;
            }
        }

        public async Task<List<Notification>> GetUnreadNotificationsAsync(string userId, int limit = 10)
        {
            return await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .OrderByDescending(n => n.CreatedDate)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<List<Notification>> GetUserNotificationsAsync(string userId, int pageNumber = 1, int pageSize = 20)
        {
            return await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> GetUnreadCountAsync(string userId)
        {
            return await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .CountAsync();
        }

        public async Task<bool> MarkAsReadAsync(int notificationId, string userId)
        {
            try
            {
                var notification = await _context.Notifications
                    .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

                if (notification == null)
                    return false;

                notification.IsRead = true;
                notification.ReadDate = DateTime.UtcNow;

                _context.Entry(notification).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error marking notification {notificationId} as read");
                return false;
            }
        }

        public async Task<int> MarkAllAsReadAsync(string userId)
        {
            try
            {
                var unreadNotifications = await _context.Notifications
                    .Where(n => n.UserId == userId && !n.IsRead)
                    .ToListAsync();

                var readDate = DateTime.UtcNow;
                foreach (var notification in unreadNotifications)
                {
                    notification.IsRead = true;
                    notification.ReadDate = readDate;
                    _context.Entry(notification).State = EntityState.Modified;
                }

                await _context.SaveChangesAsync();
                return unreadNotifications.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error marking all notifications as read for user {userId}");
                return 0;
            }
        }

        public async Task<bool> DeleteNotificationAsync(int notificationId, string userId)
        {
            try
            {
                var notification = await _context.Notifications
                    .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

                if (notification == null)
                    return false;

                _context.Notifications.Remove(notification);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting notification {notificationId}");
                return false;
            }
        }

        public async Task<int> DeleteAllReadAsync(string userId)
        {
            try
            {
                var readNotifications = await _context.Notifications
                    .Where(n => n.UserId == userId && n.IsRead)
                    .ToListAsync();

                _context.Notifications.RemoveRange(readNotifications);
                await _context.SaveChangesAsync();

                return readNotifications.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting read notifications for user {userId}");
                return 0;
            }
        }

        public async Task CreateApprovalNotificationAsync(int verificationId, string userId, bool isApproved, string? comments = null)
        {
            try
            {
                var verification = await _context.CallLogVerifications
                    .Include(v => v.CallRecord)
                    .FirstOrDefaultAsync(v => v.Id == verificationId);

                if (verification == null)
                    return;

                var title = isApproved ? "Call Log Approved" : "Call Log Rejected";
                var message = isApproved
                    ? $"Your call log verification for {verification.CallRecord?.CallDate:MMM dd, yyyy} has been approved by your supervisor."
                    : $"Your call log verification for {verification.CallRecord?.CallDate:MMM dd, yyyy} has been rejected. {comments}";

                var icon = isApproved ? "bi-check-circle-fill" : "bi-x-circle-fill";
                var type = isApproved ? NotificationType.Success : NotificationType.Error;

                await CreateNotificationAsync(
                    userId,
                    title,
                    message,
                    type,
                    "/Modules/EBillManagement/CallRecords/MyCallLogs",
                    icon,
                    "CallLogVerification",
                    verificationId.ToString()
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating approval notification for verification {verificationId}");
            }
        }

        public async Task CreateRevertNotificationAsync(int verificationId, string userId, string? reason = null)
        {
            try
            {
                var verification = await _context.CallLogVerifications
                    .Include(v => v.CallRecord)
                    .FirstOrDefaultAsync(v => v.Id == verificationId);

                if (verification == null)
                    return;

                var message = $"Your call log verification for {verification.CallRecord?.CallDate:MMM dd, yyyy} has been returned for revision. {reason}";

                await CreateNotificationAsync(
                    userId,
                    "Call Log Returned for Revision",
                    message,
                    NotificationType.Warning,
                    "/Modules/EBillManagement/CallRecords/MyCallLogs",
                    "bi-arrow-counterclockwise",
                    "CallLogVerification",
                    verificationId.ToString()
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating revert notification for verification {verificationId}");
            }
        }

        public async Task CreateSimRequestNotificationAsync(int requestId, string userId, string status, string? comments = null)
        {
            try
            {
                var title = $"SIM Request {status}";
                var message = $"Your SIM card request has been {status.ToLower()}.";
                if (!string.IsNullOrEmpty(comments))
                {
                    message += $" Comment: {comments}";
                }

                var type = status.ToUpper() == "APPROVED" ? NotificationType.Success : NotificationType.Error;
                var icon = status.ToUpper() == "APPROVED" ? "bi-check-circle-fill" : "bi-x-circle-fill";

                await CreateNotificationAsync(
                    userId,
                    title,
                    message,
                    type,
                    "/Modules/SimManagement/Requests/Index",
                    icon,
                    "SimRequest",
                    requestId.ToString()
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating SIM request notification for request {requestId}");
            }
        }

        public async Task CreateRefundRequestNotificationAsync(int requestId, string userId, string status, string? comments = null)
        {
            try
            {
                var title = $"Refund Request {status}";
                var message = $"Your refund request has been {status.ToLower()}.";
                if (!string.IsNullOrEmpty(comments))
                {
                    message += $" Comment: {comments}";
                }

                var type = status.ToUpper() == "APPROVED" ? NotificationType.Success : NotificationType.Error;
                var icon = status.ToUpper() == "APPROVED" ? "bi-check-circle-fill" : "bi-x-circle-fill";

                await CreateNotificationAsync(
                    userId,
                    title,
                    message,
                    type,
                    "/Modules/RefundManagement/Requests/Index",
                    icon,
                    "RefundRequest",
                    requestId.ToString()
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating refund request notification for request {requestId}");
            }
        }

        private string GetDefaultIcon(NotificationType type)
        {
            return type switch
            {
                NotificationType.Success => "bi-check-circle-fill",
                NotificationType.Error => "bi-x-circle-fill",
                NotificationType.Warning => "bi-exclamation-triangle-fill",
                NotificationType.Action => "bi-bell-fill",
                _ => "bi-info-circle-fill"
            };
        }

        // ==================================================================
        // SIM Request Workflow Notifications
        // ==================================================================

        public async Task NotifySimRequestSubmittedAsync(int requestId, string requesterUserId, string supervisorUserId)
        {
            try
            {
                // Notify requester
                await CreateNotificationAsync(
                    requesterUserId,
                    "SIM Request Submitted",
                    "Your SIM card request has been submitted to your supervisor for approval.",
                    NotificationType.Success,
                    "/Modules/SimManagement/Requests/Index",
                    "bi-check-circle-fill",
                    "SimRequest",
                    requestId.ToString()
                );

                // Notify supervisor
                await CreateNotificationAsync(
                    supervisorUserId,
                    "New SIM Request Pending",
                    "A new SIM card request is pending your approval.",
                    NotificationType.Action,
                    $"/Modules/SimManagement/Approvals/Supervisor?requestId={requestId}",
                    "bi-bell-fill",
                    "SimRequest",
                    requestId.ToString()
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating SIM request submission notifications for request {requestId}");
            }
        }

        public async Task NotifySimRequestSupervisorApprovedAsync(int requestId, string requesterUserId, string? comments = null)
        {
            try
            {
                var message = "Your SIM card request has been approved by your supervisor and forwarded to ICTS for processing.";
                if (!string.IsNullOrEmpty(comments))
                {
                    message += $" Supervisor note: {comments}";
                }

                await CreateNotificationAsync(
                    requesterUserId,
                    "SIM Request Approved",
                    message,
                    NotificationType.Success,
                    "/Modules/SimManagement/Requests/Index",
                    "bi-check-circle-fill",
                    "SimRequest",
                    requestId.ToString()
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating SIM request approval notification for request {requestId}");
            }
        }

        public async Task NotifySimRequestSupervisorRejectedAsync(int requestId, string requesterUserId, string? reason = null)
        {
            try
            {
                var message = "Your SIM card request has been rejected by your supervisor.";
                if (!string.IsNullOrEmpty(reason))
                {
                    message += $" Reason: {reason}";
                }

                await CreateNotificationAsync(
                    requesterUserId,
                    "SIM Request Rejected",
                    message,
                    NotificationType.Error,
                    "/Modules/SimManagement/Requests/Index",
                    "bi-x-circle-fill",
                    "SimRequest",
                    requestId.ToString()
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating SIM request rejection notification for request {requestId}");
            }
        }

        public async Task NotifySimRequestForwardedToIctsAsync(int requestId, string requesterUserId, string ictsUserId)
        {
            try
            {
                // Notify requester
                await CreateNotificationAsync(
                    requesterUserId,
                    "SIM Request Forwarded",
                    "Your SIM card request has been forwarded to ICTS for processing.",
                    NotificationType.Info,
                    "/Modules/SimManagement/Requests/Index",
                    "bi-arrow-right-circle-fill",
                    "SimRequest",
                    requestId.ToString()
                );

                // Notify ICTS
                await CreateNotificationAsync(
                    ictsUserId,
                    "New SIM Request",
                    "A new SIM card request requires your attention.",
                    NotificationType.Action,
                    $"/Modules/SimManagement/Approvals/ICTS?requestId={requestId}",
                    "bi-bell-fill",
                    "SimRequest",
                    requestId.ToString()
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating SIM request forwarding notifications for request {requestId}");
            }
        }

        public async Task NotifySimReadyForCollectionAsync(int requestId, string requesterUserId, string assignedNumber)
        {
            try
            {
                await CreateNotificationAsync(
                    requesterUserId,
                    "SIM Ready for Collection",
                    $"Your SIM card ({assignedNumber}) is ready for collection at ICTS Service Desk.",
                    NotificationType.Success,
                    "/Modules/SimManagement/Requests/Index",
                    "bi-check-circle-fill",
                    "SimRequest",
                    requestId.ToString()
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating SIM ready notification for request {requestId}");
            }
        }

        public async Task NotifySimRequestIctsProcessingAsync(int requestId, string requesterUserId, string? comments = null)
        {
            try
            {
                var message = "Your SIM request is being processed by ICTS.";
                if (!string.IsNullOrEmpty(comments))
                {
                    message += $" Note: {comments}";
                }

                await CreateNotificationAsync(
                    requesterUserId,
                    "SIM Request Processing",
                    message,
                    NotificationType.Info,
                    "/Modules/SimManagement/Requests/Index",
                    "bi-gear-fill",
                    "SimRequest",
                    requestId.ToString()
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating SIM ICTS processing notification for request {requestId}");
            }
        }

        public async Task NotifySimRequestReadyForCollectionAsync(int requestId, string requesterUserId, string? comments = null)
        {
            try
            {
                var message = "Your SIM card is ready for collection.";
                if (!string.IsNullOrEmpty(comments))
                {
                    message += $" Note: {comments}";
                }

                await CreateNotificationAsync(
                    requesterUserId,
                    "SIM Ready for Collection",
                    message,
                    NotificationType.Action,
                    "/Modules/SimManagement/Requests/Index",
                    "bi-collection",
                    "SimRequest",
                    requestId.ToString()
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating SIM ready for collection notification for request {requestId}");
            }
        }

        public async Task NotifySimRequestCompletedAsync(int requestId, string requesterUserId)
        {
            try
            {
                await CreateNotificationAsync(
                    requesterUserId,
                    "SIM Request Completed",
                    "Your SIM card request has been completed. Thank you!",
                    NotificationType.Success,
                    "/Modules/SimManagement/Requests/Index",
                    "bi-check-all",
                    "SimRequest",
                    requestId.ToString()
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating SIM completion notification for request {requestId}");
            }
        }

        public async Task NotifySimRequestCancelledAsync(int requestId, string requesterUserId, string? reason = null)
        {
            try
            {
                var message = "Your SIM card request has been cancelled.";
                if (!string.IsNullOrEmpty(reason))
                {
                    message += $" Reason: {reason}";
                }

                await CreateNotificationAsync(
                    requesterUserId,
                    "SIM Request Cancelled",
                    message,
                    NotificationType.Warning,
                    "/Modules/SimManagement/Requests/Index",
                    "bi-x-circle",
                    "SimRequest",
                    requestId.ToString()
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating SIM cancellation notification for request {requestId}");
            }
        }

        // ==================================================================
        // Refund Request Workflow Notifications
        // ==================================================================

        public async Task NotifyRefundRequestSubmittedAsync(int requestId, string requesterUserId, string supervisorUserId, Guid publicId)
        {
            try
            {
                // Notify requester
                await CreateNotificationAsync(
                    requesterUserId,
                    "Refund Request Submitted",
                    "Your refund request has been submitted to your supervisor for approval.",
                    NotificationType.Success,
                    "/Modules/RefundManagement/Requests/Index",
                    "bi-check-circle-fill",
                    "RefundRequest",
                    requestId.ToString()
                );

                // Notify supervisor
                await CreateNotificationAsync(
                    supervisorUserId,
                    "New Refund Request Pending",
                    "A new refund request is pending your approval.",
                    NotificationType.Action,
                    $"/Modules/RefundManagement/Approvals/Supervisor?requestId={publicId}&tab=supervisor",
                    "bi-bell-fill",
                    "RefundRequest",
                    requestId.ToString()
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating refund request submission notifications for request {requestId}");
            }
        }

        public async Task NotifyRefundSupervisorApprovedAsync(int requestId, string requesterUserId, string? comments = null)
        {
            try
            {
                var message = "Your refund request has been approved by your supervisor and forwarded to Budget Officer.";
                if (!string.IsNullOrEmpty(comments))
                {
                    message += $" Supervisor note: {comments}";
                }

                await CreateNotificationAsync(
                    requesterUserId,
                    "Refund Request Approved by Supervisor",
                    message,
                    NotificationType.Success,
                    "/Modules/RefundManagement/Requests/Index",
                    "bi-check-circle-fill",
                    "RefundRequest",
                    requestId.ToString()
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating refund supervisor approval notification for request {requestId}");
            }
        }

        public async Task NotifyRefundSupervisorRejectedAsync(int requestId, string requesterUserId, string? reason = null)
        {
            try
            {
                var message = "Your refund request has been rejected by your supervisor.";
                if (!string.IsNullOrEmpty(reason))
                {
                    message += $" Reason: {reason}";
                }

                await CreateNotificationAsync(
                    requesterUserId,
                    "Refund Request Rejected",
                    message,
                    NotificationType.Error,
                    "/Modules/RefundManagement/Requests/Index",
                    "bi-x-circle-fill",
                    "RefundRequest",
                    requestId.ToString()
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating refund supervisor rejection notification for request {requestId}");
            }
        }

        public async Task NotifyRefundBudgetOfficerApprovedAsync(int requestId, string requesterUserId, string? comments = null)
        {
            try
            {
                var message = "Your refund request has been approved by Budget Officer and forwarded to Staff Claims Unit.";
                if (!string.IsNullOrEmpty(comments))
                {
                    message += $" Note: {comments}";
                }

                await CreateNotificationAsync(
                    requesterUserId,
                    "Refund Approved by Budget Officer",
                    message,
                    NotificationType.Success,
                    "/Modules/RefundManagement/Requests/Index",
                    "bi-check-circle-fill",
                    "RefundRequest",
                    requestId.ToString()
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating refund budget approval notification for request {requestId}");
            }
        }

        public async Task NotifyRefundBudgetOfficerRejectedAsync(int requestId, string requesterUserId, string? reason = null)
        {
            try
            {
                var message = "Your refund request has been rejected by Budget Officer.";
                if (!string.IsNullOrEmpty(reason))
                {
                    message += $" Reason: {reason}";
                }

                await CreateNotificationAsync(
                    requesterUserId,
                    "Refund Rejected by Budget Officer",
                    message,
                    NotificationType.Error,
                    "/Modules/RefundManagement/Requests/Index",
                    "bi-x-circle-fill",
                    "RefundRequest",
                    requestId.ToString()
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating refund budget rejection notification for request {requestId}");
            }
        }

        public async Task NotifyRefundClaimsUnitApprovedAsync(int requestId, string requesterUserId, string? comments = null)
        {
            try
            {
                var message = "Your refund request has been processed by Staff Claims Unit and forwarded for final payment approval.";
                if (!string.IsNullOrEmpty(comments))
                {
                    message += $" Note: {comments}";
                }

                await CreateNotificationAsync(
                    requesterUserId,
                    "Refund Processed by Claims Unit",
                    message,
                    NotificationType.Success,
                    "/Modules/RefundManagement/Requests/Index",
                    "bi-check-circle-fill",
                    "RefundRequest",
                    requestId.ToString()
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating refund claims approval notification for request {requestId}");
            }
        }

        public async Task NotifyRefundClaimsUnitRejectedAsync(int requestId, string requesterUserId, string? reason = null)
        {
            try
            {
                var message = "Your refund request has been rejected by Staff Claims Unit.";
                if (!string.IsNullOrEmpty(reason))
                {
                    message += $" Reason: {reason}";
                }

                await CreateNotificationAsync(
                    requesterUserId,
                    "Refund Rejected by Claims Unit",
                    message,
                    NotificationType.Error,
                    "/Modules/RefundManagement/Requests/Index",
                    "bi-x-circle-fill",
                    "RefundRequest",
                    requestId.ToString()
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating refund claims rejection notification for request {requestId}");
            }
        }

        public async Task NotifyRefundPaymentApprovedAsync(int requestId, string requesterUserId, string? paymentReference = null)
        {
            try
            {
                var message = "Your refund request has been approved for payment.";
                if (!string.IsNullOrEmpty(paymentReference))
                {
                    message += $" Payment reference: {paymentReference}";
                }

                await CreateNotificationAsync(
                    requesterUserId,
                    "Refund Payment Approved",
                    message,
                    NotificationType.Success,
                    "/Modules/RefundManagement/Requests/Index",
                    "bi-check-circle-fill",
                    "RefundRequest",
                    requestId.ToString()
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating refund payment approval notification for request {requestId}");
            }
        }

        public async Task NotifyRefundPaymentRejectedAsync(int requestId, string requesterUserId, string? reason = null)
        {
            try
            {
                var message = "Your refund request has been rejected at the payment approval stage.";
                if (!string.IsNullOrEmpty(reason))
                {
                    message += $" Reason: {reason}";
                }

                await CreateNotificationAsync(
                    requesterUserId,
                    "Refund Payment Rejected",
                    message,
                    NotificationType.Error,
                    "/Modules/RefundManagement/Requests/Index",
                    "bi-x-circle-fill",
                    "RefundRequest",
                    requestId.ToString()
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating refund payment rejection notification for request {requestId}");
            }
        }

        public async Task NotifyRefundCompletedAsync(int requestId, string requesterUserId, string? paymentReference = null)
        {
            try
            {
                var message = "Your refund request has been completed!";
                if (!string.IsNullOrEmpty(paymentReference))
                {
                    message += $" Payment reference: {paymentReference}";
                }

                await CreateNotificationAsync(
                    requesterUserId,
                    "Refund Completed",
                    message,
                    NotificationType.Success,
                    "/Modules/RefundManagement/Requests/Index",
                    "bi-check-all",
                    "RefundRequest",
                    requestId.ToString()
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating refund completion notification for request {requestId}");
            }
        }

        public async Task NotifyRefundCancelledAsync(int requestId, string requesterUserId, string? reason = null)
        {
            try
            {
                var message = "Your refund request has been cancelled.";
                if (!string.IsNullOrEmpty(reason))
                {
                    message += $" Reason: {reason}";
                }

                await CreateNotificationAsync(
                    requesterUserId,
                    "Refund Request Cancelled",
                    message,
                    NotificationType.Warning,
                    "/Modules/RefundManagement/Requests/Index",
                    "bi-x-circle",
                    "RefundRequest",
                    requestId.ToString()
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating refund cancellation notification for request {requestId}");
            }
        }

        // ==================================================================
        // Approver Notifications
        // ==================================================================

        public async Task NotifyNewSimRequestPendingApprovalAsync(int requestId, string supervisorUserId, string requesterName)
        {
            try
            {
                await CreateNotificationAsync(
                    supervisorUserId,
                    "New SIM Request",
                    $"{requesterName} has submitted a SIM card request that requires your approval.",
                    NotificationType.Action,
                    $"/Modules/SimManagement/Approvals/Supervisor?requestId={requestId}",
                    "bi-bell-fill",
                    "SimRequest",
                    requestId.ToString()
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating SIM request pending notification for supervisor");
            }
        }

        public async Task NotifyNewRefundRequestPendingApprovalAsync(int requestId, string approverUserId, string requesterName, string approverRole, Guid publicId)
        {
            try
            {
                var link = approverRole switch
                {
                    "Supervisor" => $"/Modules/RefundManagement/Approvals/Supervisor?requestId={publicId}&tab=supervisor",
                    "Budget Officer" => $"/Modules/RefundManagement/Approvals/BudgetOfficer?requestId={publicId}&tab=budget",
                    "Staff Claims Unit" => $"/Modules/RefundManagement/Approvals/ClaimsUnit?requestId={publicId}&tab=claims",
                    "Payment Approver" => $"/Modules/RefundManagement/Approvals/PaymentApprover?requestId={publicId}&tab=payment",
                    _ => $"/Modules/RefundManagement/Approvals?requestId={publicId}"
                };

                await CreateNotificationAsync(
                    approverUserId,
                    "New Refund Request",
                    $"{requesterName} has submitted a refund request that requires your approval.",
                    NotificationType.Action,
                    link,
                    "bi-bell-fill",
                    "RefundRequest",
                    requestId.ToString()
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating refund request pending notification for approver");
            }
        }

        public async Task NotifyNewPaymentAssignmentAsync(int assignmentId, string assigneeUserId, string assignerName, string phoneNumber)
        {
            try
            {
                await CreateNotificationAsync(
                    assigneeUserId,
                    "Payment Responsibility Assigned",
                    $"{assignerName} has assigned payment responsibility for phone number {phoneNumber} to you. Please review and accept/reject.",
                    NotificationType.Action,
                    "/Modules/EBillManagement/CallRecords/PaymentAssignments",
                    "bi-cash-coin",
                    "PaymentAssignment",
                    assignmentId.ToString()
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating payment assignment notification for assignment {assignmentId}");
            }
        }

        public async Task NotifyPaymentAssignmentAcceptedAsync(int assignmentId, string assignerUserId, string assigneeName)
        {
            try
            {
                await CreateNotificationAsync(
                    assignerUserId,
                    "Payment Assignment Accepted",
                    $"{assigneeName} has accepted the payment assignment.",
                    NotificationType.Success,
                    "/Modules/EBillManagement/CallRecords/PaymentAssignments",
                    "bi-check-circle-fill",
                    "PaymentAssignment",
                    assignmentId.ToString()
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating payment accepted notification for assignment {assignmentId}");
            }
        }

        public async Task NotifyPaymentAssignmentRejectedAsync(int assignmentId, string assignerUserId, string assigneeName, string? reason = null)
        {
            try
            {
                var message = $"{assigneeName} has rejected the payment assignment.";
                if (!string.IsNullOrEmpty(reason))
                {
                    message += $" Reason: {reason}";
                }

                await CreateNotificationAsync(
                    assignerUserId,
                    "Payment Assignment Rejected",
                    message,
                    NotificationType.Warning,
                    "/Modules/EBillManagement/CallRecords/PaymentAssignments",
                    "bi-x-circle",
                    "PaymentAssignment",
                    assignmentId.ToString()
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating payment rejected notification for assignment {assignmentId}");
            }
        }

        // ===================================================================
        // Call Log Verification Notifications
        // ===================================================================

        public async Task NotifyCallLogVerificationSubmittedAsync(int verificationId, string requesterUserId, string supervisorUserId, int callCount, decimal totalAmount)
        {
            try
            {
                // Notify requester
                await CreateNotificationAsync(
                    requesterUserId,
                    "Call Log Verification Submitted",
                    $"Your call log verification with {callCount} call(s) (${totalAmount:F2}) has been submitted to your supervisor for approval.",
                    NotificationType.Success,
                    "/Modules/EBillManagement/CallRecords/MyCallLogs",
                    "bi-check-circle-fill",
                    "CallLogVerification",
                    verificationId.ToString()
                );

                // Notify supervisor
                await CreateNotificationAsync(
                    supervisorUserId,
                    "New Call Log Verification Pending",
                    $"A new call log verification with {callCount} call(s) (${totalAmount:F2}) is pending your approval.",
                    NotificationType.Action,
                    "/Modules/EBillManagement/CallRecords/SupervisorApprovals",
                    "bi-bell-fill",
                    "CallLogVerification",
                    verificationId.ToString()
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating call log verification submitted notifications for verification {verificationId}");
            }
        }

        public async Task NotifyCallLogVerificationApprovedAsync(int verificationId, string requesterUserId, int callCount, decimal totalAmount, string? comments = null)
        {
            try
            {
                var message = $"Your call log verification with {callCount} call(s) (${totalAmount:F2}) has been approved by your supervisor.";
                if (!string.IsNullOrEmpty(comments))
                {
                    message += $" Comments: {comments}";
                }

                await CreateNotificationAsync(
                    requesterUserId,
                    "Call Log Verification Approved",
                    message,
                    NotificationType.Success,
                    "/Modules/EBillManagement/CallRecords/MyCallLogs",
                    "bi-check-circle-fill",
                    "CallLogVerification",
                    verificationId.ToString()
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating call log verification approved notification for verification {verificationId}");
            }
        }

        public async Task NotifyCallLogVerificationRejectedAsync(int verificationId, string requesterUserId, int callCount, decimal totalAmount, string? reason = null)
        {
            try
            {
                var message = $"Your call log verification with {callCount} call(s) (${totalAmount:F2}) has been rejected by your supervisor.";
                if (!string.IsNullOrEmpty(reason))
                {
                    message += $" Reason: {reason}";
                }

                await CreateNotificationAsync(
                    requesterUserId,
                    "Call Log Verification Rejected",
                    message,
                    NotificationType.Error,
                    "/Modules/EBillManagement/CallRecords/MyCallLogs",
                    "bi-x-circle-fill",
                    "CallLogVerification",
                    verificationId.ToString()
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating call log verification rejected notification for verification {verificationId}");
            }
        }

        // ===================================================================
        // UserPhone Management Notifications
        // ===================================================================

        public async Task NotifyPhoneAssignedAsync(string userId, string phoneNumber, string phoneType)
        {
            try
            {
                await CreateNotificationAsync(
                    userId,
                    "Phone Number Assigned",
                    $"A {phoneType} phone number ({phoneNumber}) has been assigned to your account.",
                    NotificationType.Info,
                    null, // No link - informational only
                    "bi-telephone-fill",
                    "UserPhone",
                    phoneNumber
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating phone assigned notification for user {userId}");
            }
        }

        public async Task NotifyPhoneReassignedAsync(string oldUserId, string newUserId, string phoneNumber)
        {
            try
            {
                // Notify old user that phone was removed
                await CreateNotificationAsync(
                    oldUserId,
                    "Phone Number Reassigned",
                    $"The phone number {phoneNumber} has been reassigned to another staff member.",
                    NotificationType.Warning,
                    null, // No link - informational only
                    "bi-exclamation-triangle-fill",
                    "UserPhone",
                    phoneNumber
                );

                // Notify new user that phone was assigned
                await CreateNotificationAsync(
                    newUserId,
                    "Phone Number Assigned",
                    $"The phone number {phoneNumber} has been reassigned to your account.",
                    NotificationType.Info,
                    null, // No link - informational only
                    "bi-telephone-fill",
                    "UserPhone",
                    phoneNumber
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating phone reassigned notifications for phone {phoneNumber}");
            }
        }

        public async Task NotifyPhoneUnassignedAsync(string userId, string phoneNumber)
        {
            try
            {
                await CreateNotificationAsync(
                    userId,
                    "Phone Number Unassigned",
                    $"The phone number {phoneNumber} has been removed from your account.",
                    NotificationType.Warning,
                    null, // No link - informational only
                    "bi-telephone-x-fill",
                    "UserPhone",
                    phoneNumber
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating phone unassigned notification for user {userId}");
            }
        }

        public async Task NotifyPhoneStatusChangedAsync(string userId, string phoneNumber, PhoneStatus oldStatus, PhoneStatus newStatus)
        {
            try
            {
                var statusMessage = newStatus switch
                {
                    PhoneStatus.Active => $"Your phone number {phoneNumber} has been activated.",
                    PhoneStatus.Deactivated => $"Your phone number {phoneNumber} has been deactivated.",
                    PhoneStatus.Suspended => $"Your phone number {phoneNumber} has been suspended.",
                    _ => $"The status of your phone number {phoneNumber} has been changed to {newStatus}."
                };

                var notificationType = newStatus switch
                {
                    PhoneStatus.Active => NotificationType.Success,
                    PhoneStatus.Suspended => NotificationType.Warning,
                    PhoneStatus.Deactivated => NotificationType.Warning,
                    _ => NotificationType.Info
                };

                var icon = newStatus switch
                {
                    PhoneStatus.Active => "bi-check-circle-fill",
                    PhoneStatus.Suspended => "bi-pause-circle-fill",
                    PhoneStatus.Deactivated => "bi-x-circle-fill",
                    _ => "bi-info-circle-fill"
                };

                await CreateNotificationAsync(
                    userId,
                    "Phone Status Changed",
                    statusMessage,
                    notificationType,
                    null, // No link - informational only
                    icon,
                    "UserPhone",
                    phoneNumber
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating phone status changed notification for user {userId}");
            }
        }

        public async Task NotifyPhoneNumberChangedAsync(string userId, string oldPhoneNumber, string newPhoneNumber)
        {
            try
            {
                await CreateNotificationAsync(
                    userId,
                    "Phone Number Updated",
                    $"Your phone number has been updated from {oldPhoneNumber} to {newPhoneNumber}.",
                    NotificationType.Info,
                    null, // No link - informational only
                    "bi-telephone-fill",
                    "UserPhone",
                    newPhoneNumber
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating phone number changed notification for user {userId}");
            }
        }
    }
}
