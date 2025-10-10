using TAB.Web.Models;

namespace TAB.Web.Services
{
    public interface INotificationService
    {
        /// <summary>
        /// Creates a new notification for a user
        /// </summary>
        Task<Notification> CreateNotificationAsync(
            string userId,
            string title,
            string message,
            NotificationType type = NotificationType.Info,
            string? link = null,
            string? icon = null,
            string? relatedEntityType = null,
            string? relatedEntityId = null);

        /// <summary>
        /// Gets unread notifications for a user
        /// </summary>
        Task<List<Notification>> GetUnreadNotificationsAsync(string userId, int limit = 10);

        /// <summary>
        /// Gets all notifications for a user (with pagination)
        /// </summary>
        Task<List<Notification>> GetUserNotificationsAsync(string userId, int pageNumber = 1, int pageSize = 20);

        /// <summary>
        /// Gets count of unread notifications for a user
        /// </summary>
        Task<int> GetUnreadCountAsync(string userId);

        /// <summary>
        /// Marks a notification as read
        /// </summary>
        Task<bool> MarkAsReadAsync(int notificationId, string userId);

        /// <summary>
        /// Marks all notifications as read for a user
        /// </summary>
        Task<int> MarkAllAsReadAsync(string userId);

        /// <summary>
        /// Deletes a notification
        /// </summary>
        Task<bool> DeleteNotificationAsync(int notificationId, string userId);

        /// <summary>
        /// Deletes all read notifications for a user
        /// </summary>
        Task<int> DeleteAllReadAsync(string userId);

        /// <summary>
        /// Creates notification when supervisor approves call verification
        /// </summary>
        Task CreateApprovalNotificationAsync(int verificationId, string userId, bool isApproved, string? comments = null);

        /// <summary>
        /// Creates notification when supervisor reverts call verification
        /// </summary>
        Task CreateRevertNotificationAsync(int verificationId, string userId, string? reason = null);

        /// <summary>
        /// Creates notification for sim request approval/rejection
        /// </summary>
        Task CreateSimRequestNotificationAsync(int requestId, string userId, string status, string? comments = null);

        /// <summary>
        /// Creates notification for refund request approval/rejection
        /// </summary>
        Task CreateRefundRequestNotificationAsync(int requestId, string userId, string status, string? comments = null);

        // SIM Request Workflow Notifications
        Task NotifySimRequestSubmittedAsync(int requestId, string requesterUserId, string supervisorUserId);
        Task NotifySimRequestSupervisorApprovedAsync(int requestId, string requesterUserId, string? comments = null);
        Task NotifySimRequestSupervisorRejectedAsync(int requestId, string requesterUserId, string? reason = null);
        Task NotifySimRequestForwardedToIctsAsync(int requestId, string requesterUserId, string ictsUserId);
        Task NotifySimRequestIctsProcessingAsync(int requestId, string requesterUserId, string? comments = null);
        Task NotifySimRequestReadyForCollectionAsync(int requestId, string requesterUserId, string? comments = null);
        Task NotifySimReadyForCollectionAsync(int requestId, string requesterUserId, string assignedNumber);
        Task NotifySimRequestCompletedAsync(int requestId, string requesterUserId);
        Task NotifySimRequestCancelledAsync(int requestId, string requesterUserId, string? reason = null);

        // Refund Request Workflow Notifications
        Task NotifyRefundRequestSubmittedAsync(int requestId, string requesterUserId, string supervisorUserId);
        Task NotifyRefundSupervisorApprovedAsync(int requestId, string requesterUserId, string? comments = null);
        Task NotifyRefundSupervisorRejectedAsync(int requestId, string requesterUserId, string? reason = null);
        Task NotifyRefundBudgetOfficerApprovedAsync(int requestId, string requesterUserId, string? comments = null);
        Task NotifyRefundBudgetOfficerRejectedAsync(int requestId, string requesterUserId, string? reason = null);
        Task NotifyRefundClaimsUnitApprovedAsync(int requestId, string requesterUserId, string? comments = null);
        Task NotifyRefundClaimsUnitRejectedAsync(int requestId, string requesterUserId, string? reason = null);
        Task NotifyRefundPaymentApprovedAsync(int requestId, string requesterUserId, string? paymentReference = null);
        Task NotifyRefundPaymentRejectedAsync(int requestId, string requesterUserId, string? reason = null);
        Task NotifyRefundCompletedAsync(int requestId, string requesterUserId, string? paymentReference = null);
        Task NotifyRefundCancelledAsync(int requestId, string requesterUserId, string? reason = null);

        // Approver Notifications
        Task NotifyNewSimRequestPendingApprovalAsync(int requestId, string supervisorUserId, string requesterName);
        Task NotifyNewRefundRequestPendingApprovalAsync(int requestId, string approverUserId, string requesterName, string approverRole);
        Task NotifyNewPaymentAssignmentAsync(int assignmentId, string assigneeUserId, string assignerName, string phoneNumber);
        Task NotifyPaymentAssignmentAcceptedAsync(int assignmentId, string assignerUserId, string assigneeName);
        Task NotifyPaymentAssignmentRejectedAsync(int assignmentId, string assignerUserId, string assigneeName, string? reason = null);

        // Call Log Verification Notifications
        Task NotifyCallLogVerificationSubmittedAsync(int verificationId, string requesterUserId, string supervisorUserId, int callCount, decimal totalAmount);
        Task NotifyCallLogVerificationApprovedAsync(int verificationId, string requesterUserId, int callCount, decimal totalAmount, string? comments = null);
        Task NotifyCallLogVerificationRejectedAsync(int verificationId, string requesterUserId, int callCount, decimal totalAmount, string? reason = null);

        // UserPhone Management Notifications
        Task NotifyPhoneAssignedAsync(string userId, string phoneNumber, string phoneType);
        Task NotifyPhoneReassignedAsync(string oldUserId, string newUserId, string phoneNumber);
        Task NotifyPhoneUnassignedAsync(string userId, string phoneNumber);
        Task NotifyPhoneStatusChangedAsync(string userId, string phoneNumber, PhoneStatus oldStatus, PhoneStatus newStatus);
        Task NotifyPhoneNumberChangedAsync(string userId, string oldPhoneNumber, string newPhoneNumber);
    }
}
