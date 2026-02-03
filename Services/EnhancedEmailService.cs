using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;
using TAB.Web.Data;
using TAB.Web.Models;

namespace TAB.Web.Services
{
    public class EnhancedEmailService : IEnhancedEmailService
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailTemplateService _templateService;
        private readonly ILogger<EnhancedEmailService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;

        // Static cache to prevent repeated DB queries when email is not configured
        private static DateTime _lastEmailConfigCheck = DateTime.MinValue;
        private static bool _hasEmailConfig = false;
        private static readonly TimeSpan _configCacheDuration = TimeSpan.FromMinutes(5);
        private static readonly object _cacheLock = new object();

        public EnhancedEmailService(
            ApplicationDbContext context,
            IEmailTemplateService templateService,
            ILogger<EnhancedEmailService> logger,
            IConfiguration configuration,
            IWebHostEnvironment environment)
        {
            _context = context;
            _templateService = templateService;
            _logger = logger;
            _configuration = configuration;
            _environment = environment;
        }

        public async Task<bool> SendEmailAsync(
            string to,
            string subject,
            string htmlMessage,
            string? plainTextMessage = null,
            string? cc = null,
            string? bcc = null,
            List<string>? attachmentPaths = null,
            string? createdBy = null,
            string? relatedEntityType = null,
            string? relatedEntityId = null)
        {
            // Create email log entry
            var emailLog = new EmailLog
            {
                ToEmail = to,
                CcEmails = cc,
                BccEmails = bcc,
                Subject = subject,
                Body = htmlMessage,
                PlainTextBody = plainTextMessage,
                Status = "Pending",
                CreatedDate = DateTime.UtcNow,
                CreatedBy = createdBy,
                TrackingId = Guid.NewGuid().ToString(),
                RelatedEntityType = relatedEntityType,
                RelatedEntityId = relatedEntityId
            };

            _context.EmailLogs.Add(emailLog);
            await _context.SaveChangesAsync();

            try
            {
                // Get email configuration
                var config = await GetActiveEmailConfigurationAsync();
                if (config == null)
                {
                    throw new InvalidOperationException("No active email configuration found");
                }

                // Send the email
                await SendEmailInternalAsync(config, to, subject, htmlMessage, plainTextMessage, cc, bcc, attachmentPaths, null);

                // Update email log as sent
                emailLog.Status = "Sent";
                emailLog.SentDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Email sent successfully to {To}", to);
                return true;
            }
            catch (Exception ex)
            {
                // Update email log as failed
                emailLog.Status = "Failed";
                emailLog.ErrorMessage = ex.Message;
                emailLog.RetryCount++;
                await _context.SaveChangesAsync();

                _logger.LogError(ex, "Failed to send email to {To}", to);
                return false;
            }
        }

        public async Task<bool> SendTemplatedEmailAsync(
            string to,
            string templateCode,
            Dictionary<string, string> data,
            string? cc = null,
            string? bcc = null,
            List<string>? attachmentPaths = null,
            string? createdBy = null,
            string? relatedEntityType = null,
            string? relatedEntityId = null)
        {
            try
            {
                // Get and render the template
                var template = await _templateService.GetTemplateByCodeAsync(templateCode);
                if (template == null)
                {
                    throw new InvalidOperationException($"Template '{templateCode}' not found");
                }

                var (subject, htmlBody, plainTextBody) = await _templateService.RenderTemplateAsync(templateCode, data);

                // Create email log entry with template reference
                var emailLog = new EmailLog
                {
                    ToEmail = to,
                    CcEmails = cc,
                    BccEmails = bcc,
                    Subject = subject,
                    Body = htmlBody,
                    PlainTextBody = plainTextBody,
                    EmailTemplateId = template.Id,
                    Status = "Pending",
                    CreatedDate = DateTime.UtcNow,
                    CreatedBy = createdBy,
                    TrackingId = Guid.NewGuid().ToString(),
                    RelatedEntityType = relatedEntityType,
                    RelatedEntityId = relatedEntityId
                };

                _context.EmailLogs.Add(emailLog);
                await _context.SaveChangesAsync();

                // Get email configuration
                var config = await GetActiveEmailConfigurationAsync();
                if (config == null)
                {
                    throw new InvalidOperationException("No active email configuration found");
                }

                // Send the email with inline logo attachment
                var logoPath = Path.Combine(_environment.WebRootPath, "images", "ebilling-login.jpg");
                var inlineAttachments = new Dictionary<string, string>
                {
                    { "logo", logoPath }
                };
                await SendEmailInternalAsync(config, to, subject, htmlBody, plainTextBody, cc, bcc, attachmentPaths, inlineAttachments);

                // Update email log as sent
                emailLog.Status = "Sent";
                emailLog.SentDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Templated email sent successfully to {To} using template {TemplateCode}", to, templateCode);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send templated email to {To} using template {TemplateCode}", to, templateCode);
                return false;
            }
        }

        public async Task<int> QueueEmailAsync(
            string to,
            string subject,
            string htmlMessage,
            string? plainTextMessage = null,
            string? cc = null,
            string? bcc = null,
            DateTime? scheduledSendDate = null,
            int priority = 5,
            string? createdBy = null)
        {
            var emailLog = new EmailLog
            {
                ToEmail = to,
                CcEmails = cc,
                BccEmails = bcc,
                Subject = subject,
                Body = htmlMessage,
                PlainTextBody = plainTextMessage,
                Status = "Queued",
                ScheduledSendDate = scheduledSendDate,
                Priority = priority,
                CreatedDate = DateTime.UtcNow,
                CreatedBy = createdBy,
                TrackingId = Guid.NewGuid().ToString()
            };

            _context.EmailLogs.Add(emailLog);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Email queued for {To}", to);
            return emailLog.Id;
        }

        public async Task<int> QueueTemplatedEmailAsync(
            string to,
            string templateCode,
            Dictionary<string, string> data,
            string? cc = null,
            string? bcc = null,
            DateTime? scheduledSendDate = null,
            int priority = 5,
            string? createdBy = null)
        {
            var template = await _templateService.GetTemplateByCodeAsync(templateCode);
            if (template == null)
            {
                throw new InvalidOperationException($"Template '{templateCode}' not found");
            }

            var (subject, htmlBody, plainTextBody) = await _templateService.RenderTemplateAsync(templateCode, data);

            return await QueueEmailAsync(to, subject, htmlBody, plainTextBody, cc, bcc, scheduledSendDate, priority, createdBy);
        }

        public async Task ProcessQueueAsync(int maxEmails = 50)
        {
            // Check cache first to avoid DB hit when email is not configured
            bool shouldCheckDb = false;
            lock (_cacheLock)
            {
                if (DateTime.UtcNow - _lastEmailConfigCheck > _configCacheDuration)
                {
                    shouldCheckDb = true;
                }
                else if (!_hasEmailConfig)
                {
                    // Email was not configured last time we checked, and cache is still valid
                    _logger.LogDebug("Email configuration not available (cached). Skipping queue processing.");
                    return;
                }
            }

            // Check for email configuration first (before querying emails)
            EmailConfiguration? config = null;
            if (shouldCheckDb || _hasEmailConfig)
            {
                config = await GetActiveEmailConfigurationAsync();

                lock (_cacheLock)
                {
                    _lastEmailConfigCheck = DateTime.UtcNow;
                    _hasEmailConfig = config != null;
                }

                if (config == null)
                {
                    _logger.LogWarning("No active email configuration found. Cannot process queue.");
                    return;
                }
            }
            else
            {
                // Use cached config status - if we got here, config exists
                config = await GetActiveEmailConfigurationAsync();
                if (config == null)
                {
                    lock (_cacheLock)
                    {
                        _hasEmailConfig = false;
                        _lastEmailConfigCheck = DateTime.UtcNow;
                    }
                    _logger.LogWarning("No active email configuration found. Cannot process queue.");
                    return;
                }
            }

            // Now query for queued emails (only if we have a valid config)
            var queuedEmails = await _context.EmailLogs
                .Where(e => e.Status == "Queued" &&
                           (e.ScheduledSendDate == null || e.ScheduledSendDate <= DateTime.UtcNow))
                .OrderBy(e => e.Priority)
                .ThenBy(e => e.CreatedDate)
                .Take(maxEmails)
                .ToListAsync();

            if (!queuedEmails.Any())
            {
                _logger.LogDebug("No queued emails to process.");
                return;
            }

            foreach (var email in queuedEmails)
            {
                try
                {
                    await SendEmailInternalAsync(
                        config,
                        email.ToEmail,
                        email.Subject,
                        email.Body,
                        email.PlainTextBody,
                        email.CcEmails,
                        email.BccEmails,
                        null,
                        null);

                    email.Status = "Sent";
                    email.SentDate = DateTime.UtcNow;
                }
                catch (Exception ex)
                {
                    email.Status = "Failed";
                    email.ErrorMessage = ex.Message;
                    email.RetryCount++;

                    _logger.LogError(ex, "Failed to process queued email {EmailId}", email.Id);
                }
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Processed {Count} queued emails", queuedEmails.Count);
        }

        public async Task<(List<EmailLog> logs, int totalCount)> GetEmailLogsAsync(
            string? toEmail = null,
            string? status = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            int page = 1,
            int pageSize = 50)
        {
            var query = _context.EmailLogs.AsQueryable();

            if (!string.IsNullOrEmpty(toEmail))
            {
                query = query.Where(e => e.ToEmail.Contains(toEmail));
            }

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(e => e.Status == status);
            }

            if (startDate.HasValue)
            {
                query = query.Where(e => e.CreatedDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(e => e.CreatedDate <= endDate.Value);
            }

            var totalCount = await query.CountAsync();

            var logs = await query
                .OrderByDescending(e => e.CreatedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Include(e => e.EmailTemplate)
                .ToListAsync();

            return (logs, totalCount);
        }

        public async Task<EmailLog?> GetEmailLogByIdAsync(int id)
        {
            return await _context.EmailLogs
                .Include(e => e.EmailTemplate)
                .FirstOrDefaultAsync(e => e.Id == id);
        }

        public async Task<bool> RetryEmailAsync(int emailLogId)
        {
            var emailLog = await _context.EmailLogs.FindAsync(emailLogId);
            if (emailLog == null)
            {
                return false;
            }

            if (emailLog.RetryCount >= emailLog.MaxRetries)
            {
                _logger.LogWarning("Email {EmailId} has exceeded max retries", emailLogId);
                return false;
            }

            try
            {
                var config = await GetActiveEmailConfigurationAsync();
                if (config == null)
                {
                    throw new InvalidOperationException("No active email configuration found");
                }

                await SendEmailInternalAsync(
                    config,
                    emailLog.ToEmail,
                    emailLog.Subject,
                    emailLog.Body,
                    emailLog.PlainTextBody,
                    emailLog.CcEmails,
                    emailLog.BccEmails,
                    null,
                    null);

                emailLog.Status = "Sent";
                emailLog.SentDate = DateTime.UtcNow;
                emailLog.ErrorMessage = null;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully retried email {EmailId}", emailLogId);
                return true;
            }
            catch (Exception ex)
            {
                emailLog.RetryCount++;
                emailLog.ErrorMessage = ex.Message;
                await _context.SaveChangesAsync();

                _logger.LogError(ex, "Failed to retry email {EmailId}", emailLogId);
                return false;
            }
        }

        public async Task<EmailStatistics> GetEmailStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.EmailLogs.AsQueryable();

            if (startDate.HasValue)
            {
                query = query.Where(e => e.CreatedDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(e => e.CreatedDate <= endDate.Value);
            }

            var stats = new EmailStatistics
            {
                TotalSent = await query.CountAsync(e => e.Status == "Sent"),
                TotalFailed = await query.CountAsync(e => e.Status == "Failed"),
                TotalPending = await query.CountAsync(e => e.Status == "Pending"),
                TotalQueued = await query.CountAsync(e => e.Status == "Queued"),
                TotalOpened = await query.CountAsync(e => e.OpenedDate != null)
            };

            var totalSent = stats.TotalSent;
            stats.OpenRate = totalSent > 0 ? (double)stats.TotalOpened / totalSent * 100 : 0;

            // Group by status
            stats.EmailsByStatus = await query
                .GroupBy(e => e.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Status, x => x.Count);

            // Group by template
            stats.EmailsByTemplate = await query
                .Where(e => e.EmailTemplateId != null)
                .Include(e => e.EmailTemplate)
                .GroupBy(e => e.EmailTemplate!.Name)
                .Select(g => new { Template = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Template, x => x.Count);

            return stats;
        }

        public async Task SendWelcomeEmailAsync(string to, string fullName, string initialPassword)
        {
            var data = new Dictionary<string, string>
            {
                { "FullName", fullName },
                { "Email", to },
                { "InitialPassword", initialPassword }
            };

            await SendTemplatedEmailAsync(
                to,
                "WELCOME_EMAIL",
                data,
                createdBy: "System",
                relatedEntityType: "User",
                relatedEntityId: to);
        }

        private async Task<EmailConfiguration?> GetActiveEmailConfigurationAsync()
        {
            return await _context.EmailConfigurations
                .Where(c => c.IsActive)
                .OrderByDescending(c => c.CreatedDate)
                .FirstOrDefaultAsync();
        }

        private async Task SendEmailInternalAsync(
            EmailConfiguration config,
            string to,
            string subject,
            string htmlMessage,
            string? plainTextMessage,
            string? cc,
            string? bcc,
            List<string>? attachmentPaths,
            Dictionary<string, string>? inlineAttachments = null)
        {
            var message = new MailMessage
            {
                From = new MailAddress(config.FromEmail, config.FromName),
                Subject = subject
            };

            // Create multipart/alternative with HTML as preferred format
            // Add plain text first (lower priority)
            if (!string.IsNullOrEmpty(plainTextMessage))
            {
                var plainView = AlternateView.CreateAlternateViewFromString(plainTextMessage, null, "text/plain");
                message.AlternateViews.Add(plainView);
            }

            // Add HTML last (higher priority - this is what email clients should display)
            var htmlView = AlternateView.CreateAlternateViewFromString(htmlMessage, null, "text/html");

            // Add inline attachments (embedded images)
            if (inlineAttachments != null && inlineAttachments.Any())
            {
                foreach (var inline in inlineAttachments)
                {
                    var contentId = inline.Key;
                    var filePath = inline.Value;

                    if (File.Exists(filePath))
                    {
                        var linkedResource = new LinkedResource(filePath);
                        linkedResource.ContentId = contentId;
                        linkedResource.ContentType.MediaType = GetMediaType(filePath);
                        linkedResource.TransferEncoding = System.Net.Mime.TransferEncoding.Base64;
                        htmlView.LinkedResources.Add(linkedResource);
                    }
                }
            }

            message.AlternateViews.Add(htmlView);

            message.To.Add(new MailAddress(to));

            // Add CC recipients
            if (!string.IsNullOrEmpty(cc))
            {
                foreach (var ccEmail in cc.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    message.CC.Add(new MailAddress(ccEmail.Trim()));
                }
            }

            // Add BCC recipients
            if (!string.IsNullOrEmpty(bcc))
            {
                foreach (var bccEmail in bcc.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    message.Bcc.Add(new MailAddress(bccEmail.Trim()));
                }
            }

            // Add attachments
            if (attachmentPaths != null && attachmentPaths.Any())
            {
                foreach (var path in attachmentPaths)
                {
                    if (File.Exists(path))
                    {
                        message.Attachments.Add(new Attachment(path));
                    }
                }
            }

            using (var client = new SmtpClient(config.SmtpServer, config.SmtpPort))
            {
                client.UseDefaultCredentials = config.UseDefaultCredentials;
                if (!config.UseDefaultCredentials)
                {
                    client.Credentials = new NetworkCredential(config.Username, config.Password);
                }
                client.EnableSsl = config.EnableSsl;
                client.Timeout = config.Timeout * 1000; // Convert to milliseconds

                await client.SendMailAsync(message);
            }
        }

        private string GetMediaType(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            return extension switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".bmp" => "image/bmp",
                ".svg" => "image/svg+xml",
                _ => "application/octet-stream"
            };
        }
    }
}
