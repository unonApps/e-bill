using TAB.Web.Models;

namespace TAB.Web.Services
{
    public interface IEnhancedEmailService
    {
        /// <summary>
        /// Sends an email with the specified parameters and logs the activity
        /// </summary>
        Task<bool> SendEmailAsync(
            string to,
            string subject,
            string htmlMessage,
            string? plainTextMessage = null,
            string? cc = null,
            string? bcc = null,
            List<string>? attachmentPaths = null,
            string? createdBy = null,
            string? relatedEntityType = null,
            string? relatedEntityId = null);

        /// <summary>
        /// Sends an email using a template
        /// </summary>
        Task<bool> SendTemplatedEmailAsync(
            string to,
            string templateCode,
            Dictionary<string, string> data,
            string? cc = null,
            string? bcc = null,
            List<string>? attachmentPaths = null,
            string? createdBy = null,
            string? relatedEntityType = null,
            string? relatedEntityId = null,
            bool redactBody = false);

        /// <summary>
        /// Queues an email for later sending
        /// </summary>
        Task<int> QueueEmailAsync(
            string to,
            string subject,
            string htmlMessage,
            string? plainTextMessage = null,
            string? cc = null,
            string? bcc = null,
            DateTime? scheduledSendDate = null,
            int priority = 5,
            string? createdBy = null);

        /// <summary>
        /// Queues a templated email for later sending
        /// </summary>
        Task<int> QueueTemplatedEmailAsync(
            string to,
            string templateCode,
            Dictionary<string, string> data,
            string? cc = null,
            string? bcc = null,
            DateTime? scheduledSendDate = null,
            int priority = 5,
            string? createdBy = null);

        /// <summary>
        /// Processes queued emails
        /// </summary>
        Task ProcessQueueAsync(int maxEmails = 50);

        /// <summary>
        /// Gets email logs with filtering and paging
        /// </summary>
        Task<(List<EmailLog> logs, int totalCount)> GetEmailLogsAsync(
            string? toEmail = null,
            string? status = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            int page = 1,
            int pageSize = 50);

        /// <summary>
        /// Gets a specific email log by ID
        /// </summary>
        Task<EmailLog?> GetEmailLogByIdAsync(int id);

        /// <summary>
        /// Retries sending a failed email
        /// </summary>
        Task<bool> RetryEmailAsync(int emailLogId);

        /// <summary>
        /// Gets email statistics
        /// </summary>
        Task<EmailStatistics> GetEmailStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null);

        /// <summary>
        /// Sends a welcome email using the template
        /// </summary>
        Task SendWelcomeEmailAsync(string to, string fullName, string initialPassword);
    }

    public class EmailStatistics
    {
        public int TotalSent { get; set; }
        public int TotalFailed { get; set; }
        public int TotalPending { get; set; }
        public int TotalQueued { get; set; }
        public int TotalOpened { get; set; }
        public double OpenRate { get; set; }
        public Dictionary<string, int> EmailsByStatus { get; set; } = new();
        public Dictionary<string, int> EmailsByTemplate { get; set; } = new();
    }
}
