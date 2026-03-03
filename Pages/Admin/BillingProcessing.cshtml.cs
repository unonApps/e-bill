using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TAB.Web.Data;
using TAB.Web.Models;
using TAB.Web.Services;

namespace TAB.Web.Pages.Admin
{
    [Authorize(Roles = "Admin,Agency Focal Point")]
    public class BillingProcessingModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<BillingProcessingModel> _logger;
        private readonly ICallLogStagingService _stagingService;
        private readonly IBackgroundJobClient _backgroundJobs;
        private readonly IEnhancedEmailService _emailService;

        public BillingProcessingModel(
            ApplicationDbContext context,
            ILogger<BillingProcessingModel> logger,
            ICallLogStagingService stagingService,
            IBackgroundJobClient backgroundJobs,
            IEnhancedEmailService emailService)
        {
            _context = context;
            _logger = logger;
            _stagingService = stagingService;
            _backgroundJobs = backgroundJobs;
            _emailService = emailService;
        }

        // Input properties
        [BindProperty]
        public int BillingMonth { get; set; } = DateTime.Now.Month;

        [BindProperty]
        public int BillingYear { get; set; } = DateTime.Now.Year;

        [BindProperty]
        public int NumberOfDays { get; set; } = 14;

        [BindProperty]
        public string VerificationType { get; set; } = "Official";

        // Display properties
        public List<BillingProcessingHistory> ProcessingHistory { get; set; } = new();
        public StagingBatch? CurrentProcessingBatch { get; set; }
        public ExchangeRate? CurrentExchangeRate { get; set; }

        // Preview counts
        public int SafaricomCount { get; set; }
        public int AirtelCount { get; set; }
        public int PSTNCount { get; set; }
        public int PrivateWireCount { get; set; }
        public int TotalRecordCount => SafaricomCount + AirtelCount + PSTNCount + PrivateWireCount;

        // Calculated deadlines
        public DateTime? StaffVerificationDeadline { get; set; }
        public DateTime? SupervisorApprovalDeadline { get; set; }

        // Email queue statistics
        public int EmailsQueued { get; set; }
        public int EmailsSent { get; set; }
        public int EmailsFailed { get; set; }
        public int TotalEmails { get; set; }

        // Status messages
        [TempData]
        public string? StatusMessage { get; set; }

        [TempData]
        public string? StatusMessageClass { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            await LoadProcessingHistoryAsync();
            await CheckCurrentProcessingBatchAsync();
            await LoadEmailStatisticsAsync();
            return Page();
        }

        private async Task LoadEmailStatisticsAsync()
        {
            var emailStats = await _context.EmailLogs
                .GroupBy(e => e.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            EmailsQueued = emailStats.FirstOrDefault(s => s.Status == "Queued")?.Count ?? 0;
            EmailsSent = emailStats.FirstOrDefault(s => s.Status == "Sent")?.Count ?? 0;
            EmailsFailed = emailStats.FirstOrDefault(s => s.Status == "Failed")?.Count ?? 0;
            TotalEmails = emailStats.Sum(s => s.Count);
        }

        public async Task<IActionResult> OnGetPreviewAsync(int month, int year, int days)
        {
            BillingMonth = month;
            BillingYear = year;
            NumberOfDays = days;

            // Check exchange rate
            CurrentExchangeRate = await _context.ExchangeRates
                .FirstOrDefaultAsync(e => e.Month == month && e.Year == year);

            // Get record counts and costs per provider
            var safaricomData = await _context.Safaricoms
                .Where(s => s.CallMonth == month && s.CallYear == year)
                .GroupBy(s => 1)
                .Select(g => new { Count = g.Count(), TotalKES = g.Sum(s => s.Cost ?? 0), TotalUSD = g.Sum(s => s.AmountUSD ?? 0) })
                .FirstOrDefaultAsync();

            var airtelData = await _context.Airtels
                .Where(a => a.CallMonth == month && a.CallYear == year)
                .GroupBy(a => 1)
                .Select(g => new { Count = g.Count(), TotalKES = g.Sum(a => a.Cost ?? 0), TotalUSD = g.Sum(a => a.AmountUSD ?? 0) })
                .FirstOrDefaultAsync();

            var pstnData = await _context.PSTNs
                .Where(p => p.CallMonth == month && p.CallYear == year)
                .GroupBy(p => 1)
                .Select(g => new { Count = g.Count(), TotalKES = g.Sum(p => p.AmountKSH ?? 0), TotalUSD = g.Sum(p => p.AmountUSD ?? 0) })
                .FirstOrDefaultAsync();

            var privateWireData = await _context.PrivateWires
                .Where(p => p.CallMonth == month && p.CallYear == year)
                .GroupBy(p => 1)
                .Select(g => new { Count = g.Count(), TotalKES = g.Sum(p => p.AmountKSH ?? 0), TotalUSD = g.Sum(p => p.AmountUSD ?? 0) })
                .FirstOrDefaultAsync();

            SafaricomCount = safaricomData?.Count ?? 0;
            AirtelCount = airtelData?.Count ?? 0;
            PSTNCount = pstnData?.Count ?? 0;
            PrivateWireCount = privateWireData?.Count ?? 0;

            var safaricomCostKES = safaricomData?.TotalKES ?? 0;
            var safaricomCostUSD = safaricomData?.TotalUSD ?? 0;
            var airtelCostKES = airtelData?.TotalKES ?? 0;
            var airtelCostUSD = airtelData?.TotalUSD ?? 0;
            var pstnCostKES = pstnData?.TotalKES ?? 0;
            var pstnCostUSD = pstnData?.TotalUSD ?? 0;
            var privateWireCostKES = privateWireData?.TotalKES ?? 0;
            var privateWireCostUSD = privateWireData?.TotalUSD ?? 0;

            var totalCostKES = safaricomCostKES + airtelCostKES + pstnCostKES + privateWireCostKES;
            var totalCostUSD = safaricomCostUSD + airtelCostUSD + pstnCostUSD + privateWireCostUSD;

            // Calculate deadlines
            var baseDate = DateTime.UtcNow;
            StaffVerificationDeadline = baseDate.AddDays(days);
            SupervisorApprovalDeadline = baseDate.AddDays(days + 3); // 3 extra days for supervisor

            // Check if any batch is currently processing
            var processingBatch = await _context.StagingBatches
                .FirstOrDefaultAsync(b => b.BatchStatus == BatchStatus.Processing &&
                                         b.CreatedDate.Month == month &&
                                         b.CreatedDate.Year == year);

            // Count previous runs for this month
            var previousRunsCount = await _context.StagingBatches
                .CountAsync(b => b.BatchType == "BillingProcessing" &&
                                b.CreatedDate.Month == month &&
                                b.CreatedDate.Year == year);

            return new JsonResult(new
            {
                success = true,
                hasExchangeRate = CurrentExchangeRate != null,
                exchangeRate = CurrentExchangeRate?.Rate,
                safaricomCount = SafaricomCount,
                safaricomCostKES = safaricomCostKES,
                safaricomCostUSD = safaricomCostUSD,
                airtelCount = AirtelCount,
                airtelCostKES = airtelCostKES,
                airtelCostUSD = airtelCostUSD,
                pstnCount = PSTNCount,
                pstnCostKES = pstnCostKES,
                pstnCostUSD = pstnCostUSD,
                privateWireCount = PrivateWireCount,
                privateWireCostKES = privateWireCostKES,
                privateWireCostUSD = privateWireCostUSD,
                totalCount = TotalRecordCount,
                totalCostKES = totalCostKES,
                totalCostUSD = totalCostUSD,
                staffDeadline = StaffVerificationDeadline?.ToString("MMM dd, yyyy HH:mm"),
                supervisorDeadline = SupervisorApprovalDeadline?.ToString("MMM dd, yyyy HH:mm"),
                isProcessing = processingBatch != null,
                processingBatchName = processingBatch?.BatchName,
                previousRunsCount = previousRunsCount
            });
        }

        public async Task<IActionResult> OnPostAddExchangeRateAsync(int month, int year, decimal rate)
        {
            try
            {
                var existingRate = await _context.ExchangeRates
                    .FirstOrDefaultAsync(e => e.Month == month && e.Year == year);

                if (existingRate != null)
                {
                    existingRate.Rate = rate;
                    existingRate.UpdatedBy = User.Identity?.Name ?? "System";
                    existingRate.UpdatedDate = DateTime.UtcNow;
                }
                else
                {
                    var newRate = new ExchangeRate
                    {
                        Month = month,
                        Year = year,
                        Rate = rate,
                        CreatedBy = User.Identity?.Name ?? "System",
                        CreatedDate = DateTime.UtcNow
                    };
                    _context.ExchangeRates.Add(newRate);
                }

                await _context.SaveChangesAsync();

                return new JsonResult(new { success = true, message = "Exchange rate saved successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving exchange rate");
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }

        public async Task<IActionResult> OnPostStartProcessingAsync()
        {
            try
            {
                var userName = User.Identity?.Name ?? "System";

                // Validate exchange rate exists
                var exchangeRate = await _context.ExchangeRates
                    .FirstOrDefaultAsync(e => e.Month == BillingMonth && e.Year == BillingYear);

                if (exchangeRate == null)
                {
                    return new JsonResult(new { success = false, message = "Exchange rate not configured for this month." });
                }

                // Check if any batch is currently processing for this month
                var processingBatch = await _context.StagingBatches
                    .FirstOrDefaultAsync(b => b.BatchStatus == BatchStatus.Processing &&
                                             b.CreatedDate.Month == BillingMonth &&
                                             b.CreatedDate.Year == BillingYear);

                if (processingBatch != null)
                {
                    return new JsonResult(new {
                        success = false,
                        message = $"A batch is currently being processed. Batch '{processingBatch.BatchName}' is still running. Please wait for it to complete."
                    });
                }

                // Always create a NEW batch for each billing processing run
                // Previous batches (Published, Failed, etc.) are closed - we don't reuse them
                var batchCount = await _context.StagingBatches
                    .CountAsync(b => b.BatchType == "BillingProcessing" &&
                                    b.CreatedDate.Month == BillingMonth &&
                                    b.CreatedDate.Year == BillingYear);

                var batch = new StagingBatch
                {
                    Id = Guid.NewGuid(),
                    BatchName = batchCount > 0
                        ? $"Billing {new DateTime(BillingYear, BillingMonth, 1):MMMM yyyy} (Run {batchCount + 1})"
                        : $"Billing {new DateTime(BillingYear, BillingMonth, 1):MMMM yyyy}",
                    BatchType = "BillingProcessing",
                    CreatedBy = userName,
                    CreatedDate = DateTime.UtcNow,
                    BatchStatus = BatchStatus.Created,
                    SourceSystems = "Safaricom,Airtel,PSTN,PrivateWire",
                    CurrentOperation = "Initializing...",
                    ProcessingProgress = 0
                };

                _context.StagingBatches.Add(batch);
                await _context.SaveChangesAsync();

                // Start background job
                var jobId = _backgroundJobs.Enqueue<BillingProcessingModel>(
                    x => x.ProcessBillingInBackgroundAsync(
                        batch.Id,
                        BillingMonth,
                        BillingYear,
                        NumberOfDays,
                        VerificationType,
                        userName));

                // Store job ID
                batch.HangfireJobId = jobId;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created new batch {BatchId} for billing processing", batch.Id);

                return new JsonResult(new {
                    success = true,
                    batchId = batch.Id,
                    jobId = jobId,
                    message = "Processing started successfully."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting billing processing");
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }

        public async Task<IActionResult> OnGetProgressAsync(Guid batchId)
        {
            var batch = await _context.StagingBatches.FindAsync(batchId);
            if (batch == null)
            {
                return new JsonResult(new { success = false, message = "Batch not found" });
            }

            return new JsonResult(new
            {
                success = true,
                status = batch.BatchStatus.ToString(),
                progress = batch.ProcessingProgress,
                currentOperation = batch.CurrentOperation,
                totalRecords = batch.TotalRecords,
                verifiedRecords = batch.VerifiedRecords,
                anomalyRecords = batch.RecordsWithAnomalies,
                isComplete = batch.BatchStatus == BatchStatus.Published || batch.BatchStatus == BatchStatus.Failed
            });
        }

        public async Task<IActionResult> OnGetDownloadAnomaliesAsync(Guid batchId)
        {
            var anomalies = await _context.CallLogStagings
                .Where(c => c.BatchId == batchId && c.HasAnomalies)
                .ToListAsync();

            if (!anomalies.Any())
            {
                return new JsonResult(new { success = false, message = "No anomalies found for this batch." });
            }

            var csv = new StringBuilder();
            csv.AppendLine("Provider,Extension,Call Date,Dialed Number,Call Duration (sec),Call Cost,Call Type,Index Number,Anomaly Reason,Action Required");

            foreach (var record in anomalies)
            {
                var anomalyTypes = record.GetAnomalyTypesList();
                var anomalyReasons = anomalyTypes.Select(a => GetAnomalyDisplayName(a));
                var actions = anomalyTypes.Select(a => GetAnomalyAction(a));

                csv.AppendLine($"\"{record.SourceSystem}\"," +
                              $"\"{record.ExtensionNumber}\"," +
                              $"\"{record.CallDate:yyyy-MM-dd HH:mm:ss}\"," +
                              $"\"{record.CallNumber}\"," +
                              $"{record.CallDuration}," +
                              $"{record.CallCost:F2}," +
                              $"\"{record.CallType}\"," +
                              $"\"{record.ResponsibleIndexNumber ?? ""}\"," +
                              $"\"{string.Join("; ", anomalyReasons)}\"," +
                              $"\"{string.Join("; ", actions)}\"");
            }

            var bytes = Encoding.UTF8.GetBytes(csv.ToString());
            var fileName = $"Anomalies_{batchId:N}.csv";

            return File(bytes, "text/csv", fileName);
        }

        [AutomaticRetry(Attempts = 0)]
        public async Task ProcessBillingInBackgroundAsync(Guid batchId, int month, int year, int numberOfDays, string verificationType, string userName)
        {
            var batch = await _context.StagingBatches.FindAsync(batchId);
            if (batch == null)
            {
                _logger.LogError("Batch {BatchId} not found for processing", batchId);
                return;
            }

            try
            {
                // Step 1: Consolidate
                batch.BatchStatus = BatchStatus.Processing;
                batch.CurrentOperation = "Consolidating records from provider tables...";
                batch.ProcessingProgress = 10;
                batch.StartProcessingDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                var startDate = new DateTime(year, month, 1);
                var endDate = startDate.AddMonths(1).AddDays(-1);

                // Call the consolidation stored procedure
                await _stagingService.ConsolidateCallLogsInBackgroundAsync(batchId, month, year, month, year, userName);

                // Reload batch to get updated stats
                await _context.Entry(batch).ReloadAsync();

                // Step 2: Anomaly detection (already done in consolidation)
                batch.CurrentOperation = "Anomaly detection complete...";
                batch.ProcessingProgress = 60;
                await _context.SaveChangesAsync();

                // Step 3: Auto-verify clean records (already done in consolidation)
                batch.CurrentOperation = "Auto-verification complete...";
                batch.ProcessingProgress = 75;
                await _context.SaveChangesAsync();

                // Step 4: Push verified records to production
                batch.CurrentOperation = "Pushing verified records to production...";
                batch.ProcessingProgress = 85;
                await _context.SaveChangesAsync();

                // Get verification period based on number of days
                var verificationPeriod = DateTime.UtcNow.AddDays(numberOfDays);

                // Push only verified records to production (without sending notifications)
                await _stagingService.PushToProductionInBackgroundAsync(batchId, verificationPeriod, verificationType, userName, sendNotifications: false);

                // Reload batch
                await _context.Entry(batch).ReloadAsync();

                // Step 5: Count anomalies
                var anomalyCount = await _context.CallLogStagings
                    .Where(c => c.BatchId == batchId && c.HasAnomalies)
                    .CountAsync();

                // Step 6: Delete anomaly records from staging (they should be downloaded first)
                // Note: We keep them for now, admin can download and then they'll be cleaned up
                // Or we can implement a separate cleanup after download

                batch.CurrentOperation = "Processing complete";
                batch.ProcessingProgress = 100;
                batch.EndProcessingDate = DateTime.UtcNow;
                batch.Notes = $"Processed {batch.TotalRecords:N0} records. {batch.VerifiedRecords:N0} pushed to production. {anomalyCount:N0} anomalies.";
                await _context.SaveChangesAsync();

                // Step 7: Send email notification
                await SendCompletionEmailAsync(batch, userName, anomalyCount);

                _logger.LogInformation("Billing processing completed for batch {BatchId}", batchId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during billing processing for batch {BatchId}", batchId);

                batch.BatchStatus = BatchStatus.Failed;
                batch.CurrentOperation = "Processing failed";
                batch.ProcessingProgress = 0;
                batch.EndProcessingDate = DateTime.UtcNow;
                batch.FailureReason = $"{ex.Message}\n\nStack Trace:\n{ex.StackTrace}";
                batch.Notes = $"Error: {ex.Message}";
                await _context.SaveChangesAsync();

                throw;
            }
        }

        private async Task SendCompletionEmailAsync(StagingBatch batch, string userName, int anomalyCount)
        {
            try
            {
                var subject = $"Billing Processing Complete - {batch.BatchName}";
                var body = $@"
                    <h2>Billing Processing Complete</h2>
                    <p>The billing processing for <strong>{batch.BatchName}</strong> has been completed.</p>

                    <h3>Summary:</h3>
                    <ul>
                        <li>Total Records: {batch.TotalRecords:N0}</li>
                        <li>Verified & Pushed to Production: {batch.VerifiedRecords:N0}</li>
                        <li>Anomalies (excluding high usage): {anomalyCount:N0}</li>
                    </ul>

                    <p>Processing completed at: {batch.EndProcessingDate:yyyy-MM-dd HH:mm:ss} UTC</p>
                    <p>Processed by: {userName}</p>

                    {(anomalyCount > 0 ? "<p><strong>Note:</strong> Please download and review the anomaly report from the Billing Processing page.</p>" : "")}
                ";

                // Get admin email - for now just log
                _logger.LogInformation("Billing processing email would be sent: {Subject}", subject);

                // TODO: Implement actual email sending
                // await _emailService.SendEmailAsync(adminEmail, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending completion email for batch {BatchId}", batch.Id);
            }
        }

        private async Task LoadProcessingHistoryAsync()
        {
            var batches = await _context.StagingBatches
                .Where(b => b.BatchType == "BillingProcessing" || b.BatchType == "Manual")
                .OrderByDescending(b => b.CreatedDate)
                .Take(12)
                .ToListAsync();

            ProcessingHistory = batches.Select(b => new BillingProcessingHistory
            {
                BatchId = b.Id,
                BatchName = b.BatchName,
                Status = b.BatchStatus,
                TotalRecords = b.TotalRecords,
                VerifiedRecords = b.VerifiedRecords,
                AnomalyRecords = b.RecordsWithAnomalies,
                CreatedDate = b.CreatedDate,
                CompletedDate = b.EndProcessingDate,
                CreatedBy = b.CreatedBy,
                FailureReason = b.FailureReason
            }).ToList();
        }

        private async Task CheckCurrentProcessingBatchAsync()
        {
            CurrentProcessingBatch = await _context.StagingBatches
                .FirstOrDefaultAsync(b => b.BatchStatus == BatchStatus.Processing);
        }

        public async Task<IActionResult> OnPostSendNotificationsAsync(Guid batchId)
        {
            try
            {
                var batch = await _context.StagingBatches.FindAsync(batchId);
                if (batch == null)
                {
                    return new JsonResult(new { success = false, message = "Batch not found" });
                }

                if (batch.BatchStatus != BatchStatus.Published)
                {
                    return new JsonResult(new { success = false, message = "Can only send notifications for published batches" });
                }

                // Get the verification period from batch (use EndProcessingDate + 7 days as default)
                var verificationPeriod = batch.EndProcessingDate?.AddDays(7) ?? DateTime.UtcNow.AddDays(7);

                // Queue the notification job in background
                var jobId = _backgroundJobs.Enqueue<ICallLogStagingService>(
                    service => service.SendBatchNotificationsAsync(batchId, verificationPeriod));

                _logger.LogInformation("Queued notification job {JobId} for batch {BatchId}", jobId, batchId);

                return new JsonResult(new {
                    success = true,
                    message = "Notifications queued successfully. Emails will be sent in the background.",
                    jobId = jobId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending notifications for batch {BatchId}", batchId);
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }

        public async Task<IActionResult> OnPostDismissAnomaliesAsync(Guid batchId)
        {
            try
            {
                var batch = await _context.StagingBatches.FindAsync(batchId);
                if (batch == null)
                {
                    return new JsonResult(new { success = false, message = "Batch not found" });
                }

                _logger.LogInformation("Dismissing anomalies for batch {BatchId}", batchId);

                var userName = User.Identity?.Name ?? "System";

                // Mark anomaly records as Rejected (dismissed) using raw SQL for efficiency
                var dismissedCount = await _context.Database.ExecuteSqlInterpolatedAsync(
                    $@"UPDATE CallLogStagings
                       SET VerificationStatus = 2,
                           VerificationDate = {DateTime.UtcNow},
                           VerifiedBy = {userName},
                           VerificationNotes = 'Dismissed - unfixable anomaly',
                           HasAnomalies = 0,
                           ModifiedDate = {DateTime.UtcNow},
                           ModifiedBy = {userName}
                       WHERE BatchId = {batchId} AND HasAnomalies = 1");

                // Update batch statistics
                batch.RecordsWithAnomalies = 0;
                batch.VerifiedRecords = await _context.CallLogStagings
                    .Where(c => c.BatchId == batchId && c.VerificationStatus == VerificationStatus.Verified)
                    .CountAsync();

                // Update batch status to Published if all records are now handled
                var pendingCount = await _context.CallLogStagings
                    .Where(c => c.BatchId == batchId && c.VerificationStatus == VerificationStatus.Pending)
                    .CountAsync();

                if (pendingCount == 0)
                {
                    batch.BatchStatus = BatchStatus.Published;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Dismissed {Count} anomaly records for batch {BatchId}", dismissedCount, batchId);

                return new JsonResult(new {
                    success = true,
                    dismissedCount = dismissedCount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error dismissing anomalies for batch {BatchId}", batchId);
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }

        public async Task<IActionResult> OnPostDeleteAnomaliesAsync(Guid batchId)
        {
            try
            {
                var batch = await _context.StagingBatches.FindAsync(batchId);
                if (batch == null)
                {
                    return new JsonResult(new { success = false, message = "Batch not found" });
                }

                _logger.LogInformation("Deleting anomalies for batch {BatchId}", batchId);

                // Delete anomaly records using raw SQL for efficiency
                var deletedCount = await _context.Database.ExecuteSqlInterpolatedAsync(
                    $"DELETE FROM CallLogStagings WHERE BatchId = {batchId} AND HasAnomalies = 1");

                // Update batch statistics
                batch.RecordsWithAnomalies = 0;
                batch.TotalRecords = await _context.CallLogStagings
                    .Where(c => c.BatchId == batchId)
                    .CountAsync();
                batch.VerifiedRecords = await _context.CallLogStagings
                    .Where(c => c.BatchId == batchId && c.VerificationStatus == VerificationStatus.Verified)
                    .CountAsync();

                // Update batch status to Published if all records are now handled
                var pendingCount = await _context.CallLogStagings
                    .Where(c => c.BatchId == batchId && c.VerificationStatus == VerificationStatus.Pending)
                    .CountAsync();

                if (pendingCount == 0 && batch.TotalRecords > 0)
                {
                    batch.BatchStatus = BatchStatus.Published;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Deleted {Count} anomaly records for batch {BatchId}", deletedCount, batchId);

                return new JsonResult(new {
                    success = true,
                    deletedCount = deletedCount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting anomalies for batch {BatchId}", batchId);
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }

        public async Task<IActionResult> OnPostCleanupStagingDataAsync(Guid batchId)
        {
            try
            {
                var batch = await _context.StagingBatches.FindAsync(batchId);
                if (batch == null)
                {
                    return new JsonResult(new { success = false, message = "Batch not found" });
                }

                // Only allow cleanup for Published batches
                if (batch.BatchStatus != BatchStatus.Published)
                {
                    return new JsonResult(new { success = false, message = "Can only cleanup staging data for published batches" });
                }

                _logger.LogInformation("Starting staging data cleanup for batch {BatchId}", batchId);

                // Delete staging records for this batch using raw SQL for efficiency
                var deletedCount = await _context.Database.ExecuteSqlInterpolatedAsync(
                    $"DELETE FROM CallLogStagings WHERE BatchId = {batchId}");

                // Update batch to reflect cleanup
                batch.TotalRecords = 0;
                batch.VerifiedRecords = 0;
                batch.PendingRecords = 0;
                batch.RecordsWithAnomalies = 0;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Staging data cleanup complete for batch {BatchId}. Deleted {Count} records", batchId, deletedCount);

                return new JsonResult(new {
                    success = true,
                    deletedCount = deletedCount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up staging data for batch {BatchId}", batchId);
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }

        public async Task<IActionResult> OnPostReprocessAnomaliesAsync(Guid batchId)
        {
            try
            {
                var batch = await _context.StagingBatches.FindAsync(batchId);
                if (batch == null)
                {
                    return new JsonResult(new { success = false, message = "Batch not found" });
                }

                // Count original anomalies
                var originalCount = await _context.CallLogStagings
                    .Where(c => c.BatchId == batchId && c.HasAnomalies)
                    .CountAsync();

                _logger.LogInformation("Reprocessing anomalies for batch {BatchId}. Original count: {Count}", batchId, originalCount);

                // Set longer timeout for these operations
                var originalTimeout = _context.Database.GetCommandTimeout();
                _context.Database.SetCommandTimeout(300); // 5 minutes

                try
                {
                    // Step 1a: Update UserPhoneId - exact match (fast, uses index)
                    var updatePhoneExactSql = @"
                        UPDATE cls
                        SET cls.UserPhoneId = up.Id,
                            cls.ResponsibleIndexNumber = ISNULL(cls.ResponsibleIndexNumber, up.IndexNumber)
                        FROM CallLogStagings cls
                        INNER JOIN UserPhones up ON up.PhoneNumber = cls.ExtensionNumber AND up.IsActive = 1
                        WHERE cls.BatchId = @BatchId
                          AND cls.UserPhoneId IS NULL";

                    await _context.Database.ExecuteSqlRawAsync(updatePhoneExactSql, new SqlParameter("@BatchId", batchId));

                    // Step 1b: Update UserPhoneId - +254 to 0 format (staging has +254, phone has 0)
                    var updatePhone254To0Sql = @"
                        UPDATE cls
                        SET cls.UserPhoneId = up.Id,
                            cls.ResponsibleIndexNumber = ISNULL(cls.ResponsibleIndexNumber, up.IndexNumber)
                        FROM CallLogStagings cls
                        INNER JOIN UserPhones up ON up.PhoneNumber = REPLACE(cls.ExtensionNumber, '+254', '0') AND up.IsActive = 1
                        WHERE cls.BatchId = @BatchId
                          AND cls.UserPhoneId IS NULL
                          AND cls.ExtensionNumber LIKE '+254%'";

                    await _context.Database.ExecuteSqlRawAsync(updatePhone254To0Sql, new SqlParameter("@BatchId", batchId));

                    // Step 1c: Update UserPhoneId - 0 to +254 format (staging has 0, phone has +254)
                    var updatePhone0To254Sql = @"
                        UPDATE cls
                        SET cls.UserPhoneId = up.Id,
                            cls.ResponsibleIndexNumber = ISNULL(cls.ResponsibleIndexNumber, up.IndexNumber)
                        FROM CallLogStagings cls
                        INNER JOIN UserPhones up ON up.PhoneNumber = REPLACE(cls.ExtensionNumber, '0', '+254') AND up.IsActive = 1
                        WHERE cls.BatchId = @BatchId
                          AND cls.UserPhoneId IS NULL
                          AND cls.ExtensionNumber LIKE '07%'
                          AND LEN(cls.ExtensionNumber) = 10";

                    await _context.Database.ExecuteSqlRawAsync(updatePhone0To254Sql, new SqlParameter("@BatchId", batchId));

                    // Step 1d: Update ResponsibleIndexNumber for records that already have UserPhoneId
                    // but are missing ResponsibleIndexNumber (phone was matched but had no index number at the time)
                    var updateIndexSql = @"
                        UPDATE cls
                        SET cls.ResponsibleIndexNumber = up.IndexNumber
                        FROM CallLogStagings cls
                        INNER JOIN UserPhones up ON cls.UserPhoneId = up.Id
                        WHERE cls.BatchId = @BatchId
                          AND (cls.ResponsibleIndexNumber IS NULL OR cls.ResponsibleIndexNumber = '')
                          AND up.IndexNumber IS NOT NULL
                          AND up.IndexNumber != ''";

                    await _context.Database.ExecuteSqlRawAsync(updateIndexSql, new SqlParameter("@BatchId", batchId));

                    // Step 1e: Update ResponsibleIndexNumber if it changed in UserPhones (user reassigned phone)
                    var updateChangedIndexSql = @"
                        UPDATE cls
                        SET cls.ResponsibleIndexNumber = up.IndexNumber
                        FROM CallLogStagings cls
                        INNER JOIN UserPhones up ON cls.UserPhoneId = up.Id
                        WHERE cls.BatchId = @BatchId
                          AND cls.HasAnomalies = 1
                          AND up.IndexNumber IS NOT NULL
                          AND up.IndexNumber != ''
                          AND (cls.ResponsibleIndexNumber IS NULL OR cls.ResponsibleIndexNumber != up.IndexNumber)";

                    await _context.Database.ExecuteSqlRawAsync(updateChangedIndexSql, new SqlParameter("@BatchId", batchId));
                }
                finally
                {
                    // Restore original timeout
                    _context.Database.SetCommandTimeout(originalTimeout);
                }

                // Step 2: Re-run anomaly detection
                await _stagingService.DetectBatchAnomaliesFastAsync(batchId);

                // Step 3: Auto-verify clean records
                var userName = User.Identity?.Name ?? "System";
                var verifiedCount = await _stagingService.AutoVerifyCleanRecordsAsync(batchId, userName);

                // Step 4: Push newly verified records to production
                if (verifiedCount > 0)
                {
                    var verificationPeriod = DateTime.UtcNow.AddDays(7);
                    await _stagingService.PushToProductionInBackgroundAsync(batchId, verificationPeriod, "Official", userName, sendNotifications: false);
                }

                // Count remaining anomalies
                var remainingCount = await _context.CallLogStagings
                    .Where(c => c.BatchId == batchId && c.HasAnomalies)
                    .CountAsync();

                var resolvedCount = originalCount - remainingCount;

                // Update batch statistics
                batch.RecordsWithAnomalies = remainingCount;
                batch.VerifiedRecords = await _context.CallLogStagings
                    .Where(c => c.BatchId == batchId && c.VerificationStatus == VerificationStatus.Verified)
                    .CountAsync();
                await _context.SaveChangesAsync();

                _logger.LogInformation("Reprocessing complete for batch {BatchId}. Resolved: {Resolved}, Remaining: {Remaining}, Verified: {Verified}",
                    batchId, resolvedCount, remainingCount, verifiedCount);

                return new JsonResult(new {
                    success = true,
                    originalCount = originalCount,
                    resolvedCount = resolvedCount,
                    remainingCount = remainingCount,
                    verifiedCount = verifiedCount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reprocessing anomalies for batch {BatchId}", batchId);
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }

        public async Task<IActionResult> OnPostUploadCorrectionsAsync(Guid batchId, string corrections)
        {
            try
            {
                var batch = await _context.StagingBatches.FindAsync(batchId);
                if (batch == null)
                {
                    return new JsonResult(new { success = false, message = "Batch not found" });
                }

                // Parse the corrections JSON
                var correctionList = JsonSerializer.Deserialize<List<Dictionary<string, string>>>(corrections);
                if (correctionList == null || !correctionList.Any())
                {
                    return new JsonResult(new { success = false, message = "No corrections data provided" });
                }

                _logger.LogInformation("Processing {Count} correction records for batch {BatchId}", correctionList.Count, batchId);

                var updatedCount = 0;
                var userName = User.Identity?.Name ?? "System";

                foreach (var correction in correctionList)
                {
                    // Get the key fields to match the record
                    if (!correction.TryGetValue("Extension", out var extension) ||
                        !correction.TryGetValue("Call Date", out var callDateStr) ||
                        !correction.TryGetValue("Provider", out var provider))
                    {
                        continue;
                    }

                    // Get the Index Number if provided
                    correction.TryGetValue("Index Number", out var indexNumber);
                    if (string.IsNullOrWhiteSpace(indexNumber))
                    {
                        continue; // Skip if no Index Number provided
                    }

                    // Parse call date
                    if (!DateTime.TryParse(callDateStr, out var callDate))
                    {
                        continue;
                    }

                    // Find matching staging record
                    var stagingRecord = await _context.CallLogStagings
                        .FirstOrDefaultAsync(c =>
                            c.BatchId == batchId &&
                            c.ExtensionNumber == extension &&
                            c.SourceSystem == provider &&
                            c.CallDate.Date == callDate.Date &&
                            c.HasAnomalies);

                    if (stagingRecord != null)
                    {
                        // Update the ResponsibleIndexNumber
                        stagingRecord.ResponsibleIndexNumber = indexNumber.Trim();
                        stagingRecord.ModifiedDate = DateTime.UtcNow;
                        stagingRecord.ModifiedBy = userName;

                        // Try to link to UserPhone if exists
                        var userPhone = await _context.UserPhones
                            .FirstOrDefaultAsync(up =>
                                up.IndexNumber == indexNumber.Trim() &&
                                up.IsActive &&
                                (up.PhoneNumber == extension || up.PhoneNumber == extension.Replace("+254", "0")));

                        if (userPhone != null)
                        {
                            stagingRecord.UserPhoneId = userPhone.Id;
                        }

                        updatedCount++;
                    }
                }

                await _context.SaveChangesAsync();

                // Re-run anomaly detection
                await _stagingService.DetectBatchAnomaliesFastAsync(batchId);

                // Auto-verify clean records
                var verifiedCount = await _stagingService.AutoVerifyCleanRecordsAsync(batchId, userName);

                // Push to production if any verified
                if (verifiedCount > 0)
                {
                    var verificationPeriod = DateTime.UtcNow.AddDays(7);
                    await _stagingService.PushToProductionInBackgroundAsync(batchId, verificationPeriod, "Official", userName, sendNotifications: false);
                }

                // Count remaining anomalies
                var remainingCount = await _context.CallLogStagings
                    .Where(c => c.BatchId == batchId && c.HasAnomalies)
                    .CountAsync();

                // Update batch statistics
                batch.RecordsWithAnomalies = remainingCount;
                batch.VerifiedRecords = await _context.CallLogStagings
                    .Where(c => c.BatchId == batchId && c.VerificationStatus == VerificationStatus.Verified)
                    .CountAsync();
                await _context.SaveChangesAsync();

                _logger.LogInformation("Upload corrections complete for batch {BatchId}. Updated: {Updated}, Verified: {Verified}, Remaining: {Remaining}",
                    batchId, updatedCount, verifiedCount, remainingCount);

                return new JsonResult(new {
                    success = true,
                    updatedCount = updatedCount,
                    resolvedCount = verifiedCount,
                    remainingCount = remainingCount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading corrections for batch {BatchId}", batchId);
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }

        private string GetAnomalyDisplayName(string anomalyCode)
        {
            return anomalyCode switch
            {
                "DUPLICATE" => "Duplicate Record",
                "NO_PHONE" => "Extension Not Registered",
                "INVALID_DATE" => "Invalid Call Date",
                "MISSING_COST" => "Zero Cost",
                "FUTURE_DATE" => "Future Date",
                "MISSING_USER" => "No Responsible User",
                "UNASSIGNED_PHONE" => "Unassigned Phone Number",
                "INACTIVE_USER" => "Inactive User",
                _ => anomalyCode
            };
        }

        private string GetAnomalyAction(string anomalyCode)
        {
            return anomalyCode switch
            {
                "MISSING_USER" => "Create user in EBill Users or link phone number to existing user",
                "UNASSIGNED_PHONE" => "Go to User Phones and assign this phone number to a user",
                "INACTIVE_USER" => "Reactivate user in EBill Users or reassign phone to an active user",
                "NO_PHONE" => "Register extension in User Phones",
                "DUPLICATE" => "Review and remove duplicate record",
                "INVALID_DATE" => "Correct the call date in source system",
                "MISSING_COST" => "Verify cost data in source system",
                "FUTURE_DATE" => "Correct the future date in source system",
                _ => "Review and resolve manually"
            };
        }
    }

    public class BillingProcessingHistory
    {
        public Guid BatchId { get; set; }
        public string BatchName { get; set; } = string.Empty;
        public BatchStatus Status { get; set; }
        public int TotalRecords { get; set; }
        public int VerifiedRecords { get; set; }
        public int AnomalyRecords { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? CompletedDate { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string? FailureReason { get; set; }
    }
}
