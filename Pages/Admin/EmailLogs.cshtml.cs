using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TAB.Web.Models;
using TAB.Web.Services;

namespace TAB.Web.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class EmailLogsModel : PageModel
    {
        private readonly IEnhancedEmailService _emailService;
        private readonly ILogger<EmailLogsModel> _logger;

        public EmailLogsModel(
            IEnhancedEmailService emailService,
            ILogger<EmailLogsModel> logger)
        {
            _emailService = emailService;
            _logger = logger;
        }

        public List<EmailLog> EmailLogs { get; set; } = new();
        public EmailStatistics Statistics { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? ToEmailFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? StatusFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? StartDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? EndDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public int CurrentPageNumber { get; set; } = 1;

        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        public int CurrentPage => CurrentPageNumber;
        private const int PageSize = 50;

        [TempData]
        public string? StatusMessage { get; set; }

        [TempData]
        public string? StatusMessageClass { get; set; }

        public async Task OnGetAsync()
        {
            // Get email logs
            var (logs, totalCount) = await _emailService.GetEmailLogsAsync(
                ToEmailFilter,
                StatusFilter,
                StartDate,
                EndDate,
                CurrentPageNumber,
                PageSize);

            EmailLogs = logs;
            TotalCount = totalCount;

            // Get statistics
            Statistics = await _emailService.GetEmailStatisticsAsync(StartDate, EndDate);
        }

        public async Task<IActionResult> OnPostRetryAsync(int id)
        {
            try
            {
                var success = await _emailService.RetryEmailAsync(id);

                if (success)
                {
                    StatusMessage = "Email queued for retry.";
                    StatusMessageClass = "success";
                }
                else
                {
                    StatusMessage = "Failed to retry email. Maximum retries may have been exceeded.";
                    StatusMessageClass = "warning";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrying email {Id}", id);
                StatusMessage = $"Error: {ex.Message}";
                StatusMessageClass = "danger";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnGetEmailDetailsAsync(int id)
        {
            var emailLog = await _emailService.GetEmailLogByIdAsync(id);
            if (emailLog == null)
            {
                return Content(@"
                    <div class='text-center py-5'>
                        <i class='bi bi-exclamation-circle text-danger' style='font-size: 3rem;'></i>
                        <p class='text-danger mt-3 fw-semibold'>Email not found.</p>
                    </div>
                ", "text/html");
            }

            var statusBadgeClass = GetStatusBadgeClass(emailLog.Status);
            var statusIcon = emailLog.Status switch
            {
                "Sent" => "check-circle-fill",
                "Failed" => "x-circle-fill",
                "Pending" => "clock-history",
                "Queued" => "inbox",
                _ => "info-circle"
            };

            var html = $@"
                <div class='email-detail-row'>
                    <div class='email-detail-label'>
                        <i class='bi bi-person-circle'></i>
                        To
                    </div>
                    <div class='email-detail-value'>{emailLog.ToEmail}</div>
                </div>
                <div class='email-detail-row'>
                    <div class='email-detail-label'>
                        <i class='bi bi-chat-left-text'></i>
                        Subject
                    </div>
                    <div class='email-detail-value'>{emailLog.Subject}</div>
                </div>
                <div class='email-detail-row'>
                    <div class='email-detail-label'>
                        <i class='bi bi-flag'></i>
                        Status
                    </div>
                    <div class='email-detail-value'>
                        <span class='email-status-badge-modal badge-{statusBadgeClass}'>
                            <i class='bi bi-{statusIcon}'></i>
                            {emailLog.Status}
                        </span>
                    </div>
                </div>
                <div class='email-detail-row'>
                    <div class='email-detail-label'>
                        <i class='bi bi-calendar-plus'></i>
                        Created Date
                    </div>
                    <div class='email-detail-value'>{emailLog.CreatedDate:MMMM dd, yyyy 'at' HH:mm:ss}</div>
                </div>
                {(emailLog.SentDate.HasValue ? $@"
                <div class='email-detail-row'>
                    <div class='email-detail-label'>
                        <i class='bi bi-send-check'></i>
                        Sent Date
                    </div>
                    <div class='email-detail-value'>{emailLog.SentDate.Value:MMMM dd, yyyy 'at' HH:mm:ss}</div>
                </div>
                " : "")}
                {(!string.IsNullOrEmpty(emailLog.ErrorMessage) ? $@"
                <div class='email-detail-row'>
                    <div class='email-detail-label'>
                        <i class='bi bi-exclamation-triangle'></i>
                        Error Message
                    </div>
                    <div class='email-error-container'>
                        <i class='bi bi-x-circle me-2'></i>
                        {emailLog.ErrorMessage}
                    </div>
                </div>
                " : "")}
                <div class='email-detail-row'>
                    <div class='email-detail-label'>
                        <i class='bi bi-file-text'></i>
                        Email Body
                    </div>
                    <div class='email-body-container'>
                        {emailLog.Body}
                    </div>
                </div>
            ";

            return Content(html, "text/html");
        }

        private string GetStatusBadgeClass(string status)
        {
            return status switch
            {
                "Sent" => "success",
                "Failed" => "danger",
                "Pending" => "warning",
                "Queued" => "info",
                _ => "secondary"
            };
        }
    }
}
