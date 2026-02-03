using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TAB.Web.Data;
using TAB.Web.Models;
using TAB.Web.Services;

namespace TAB.Web.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class RecoveryJobStatusModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<RecoveryJobStatusModel> _logger;
        private readonly IServiceProvider _serviceProvider;

        public RecoveryJobStatusModel(
            ApplicationDbContext context,
            ILogger<RecoveryJobStatusModel> logger,
            IServiceProvider serviceProvider)
        {
            _context = context;
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        [TempData]
        public string? SuccessMessage { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        // Current Status
        public RecoveryJobExecution? CurrentExecution { get; set; }
        public RecoveryJobExecution? LastExecution { get; set; }

        // Execution History
        public List<RecoveryJobExecution> ExecutionHistory { get; set; } = new();

        // Statistics
        public JobStatistics Statistics { get; set; } = new();

        // Pagination
        [BindProperty(SupportsGet = true)]
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int TotalPages { get; set; }
        public int TotalExecutions { get; set; }

        // Filter
        [BindProperty(SupportsGet = true)]
        public string? StatusFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? RunTypeFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? StartDateFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? EndDateFilter { get; set; }

        public async Task OnGetAsync()
        {
            // Check for stuck jobs first (running more than 2 hours)
            await MarkStuckJobsAsFailedAsync();

            // Get current running job
            CurrentExecution = await _context.RecoveryJobExecutions
                .Where(e => e.Status == "Running")
                .OrderByDescending(e => e.StartTime)
                .FirstOrDefaultAsync();

            // Get last completed job
            LastExecution = await _context.RecoveryJobExecutions
                .Where(e => e.Status == "Completed")
                .OrderByDescending(e => e.EndTime)
                .FirstOrDefaultAsync();

            // Build query with filters
            var query = _context.RecoveryJobExecutions.AsQueryable();

            if (!string.IsNullOrEmpty(StatusFilter))
            {
                query = query.Where(e => e.Status == StatusFilter);
            }

            if (!string.IsNullOrEmpty(RunTypeFilter))
            {
                query = query.Where(e => e.RunType == RunTypeFilter);
            }

            if (StartDateFilter.HasValue)
            {
                query = query.Where(e => e.StartTime >= StartDateFilter.Value);
            }

            if (EndDateFilter.HasValue)
            {
                query = query.Where(e => e.StartTime <= EndDateFilter.Value.AddDays(1));
            }

            // Get total count for pagination
            TotalExecutions = await query.CountAsync();
            TotalPages = (int)Math.Ceiling(TotalExecutions / (double)PageSize);

            // Get execution history with pagination
            ExecutionHistory = await query
                .OrderByDescending(e => e.StartTime)
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            // Calculate statistics
            await CalculateStatisticsAsync();
        }

        public async Task<IActionResult> OnPostManualTriggerAsync()
        {
            try
            {
                _logger.LogInformation("Manual recovery job trigger requested by {User}", User.Identity?.Name);

                // Check for stuck jobs (running more than 2 hours) and mark them as failed
                await MarkStuckJobsAsFailedAsync();

                // Check if a job is already running
                var runningJob = await _context.RecoveryJobExecutions
                    .Where(e => e.Status == "Running")
                    .AnyAsync();

                if (runningJob)
                {
                    ErrorMessage = "A recovery job is already running. Please wait for it to complete or cancel it.";
                    return RedirectToPage();
                }

                // Trigger manual job execution in background
                var userName = User.Identity?.Name ?? "Unknown";

                try
                {
                    // Run in background but capture any startup errors
                    var backgroundTask = Task.Run(async () =>
                    {
                        try
                        {
                            await ProcessManualRecoveryAsync(userName);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Background recovery task failed");
                            // Don't rethrow - already logged
                        }
                    });

                    // Wait a moment to ensure the job starts successfully
                    await Task.Delay(500);

                    SuccessMessage = "Recovery job has been triggered manually. Check the status below.";
                    _logger.LogInformation("Manual recovery job triggered successfully");
                }
                catch (Exception startupEx)
                {
                    _logger.LogError(startupEx, "Failed to start manual recovery job");
                    ErrorMessage = $"Failed to start recovery job: {startupEx.Message}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error triggering manual recovery job");
                ErrorMessage = $"Error triggering job: {ex.Message}";
            }

            return RedirectToPage();
        }

        private async Task ProcessManualRecoveryAsync(string triggeredBy)
        {
            // Create a new scope for background processing
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var recoveryService = scope.ServiceProvider.GetRequiredService<ICallLogRecoveryService>();
            var deadlineService = scope.ServiceProvider.GetRequiredService<IDeadlineManagementService>();

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            var execution = new RecoveryJobExecution
            {
                StartTime = DateTime.UtcNow,
                Status = "Running",
                RunType = "Manual",
                TriggeredBy = triggeredBy
            };

            try
            {
                context.RecoveryJobExecutions.Add(execution);
                await context.SaveChangesAsync();

                _logger.LogInformation("===== Manual Recovery Job Started =====");
                _logger.LogInformation("Execution ID: {ExecutionId}, Triggered By: {TriggeredBy}",
                    execution.Id, triggeredBy);

                // Step 1: Send deadline reminders
                try
                {
                    await deadlineService.SendDeadlineRemindersAsync();
                    execution.RemindersSent = 1;
                    _logger.LogInformation("Deadline reminders sent successfully");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending deadline reminders");
                }

                // Step 2: Process expired verification deadlines
                var expiredVerifications = await deadlineService.GetExpiredVerificationDeadlinesAsync();
                _logger.LogInformation("Found {Count} expired verification deadlines", expiredVerifications.Count);

                foreach (var deadline in expiredVerifications)
                {
                    var result = await recoveryService.ProcessExpiredVerificationsAsync(deadline.BatchId);
                    if (result.Success)
                    {
                        deadline.RecoveryProcessed = true;
                        deadline.RecoveryProcessedDate = DateTime.UtcNow;
                        deadline.DeadlineStatus = "Missed";

                        execution.ExpiredVerificationsProcessed++;
                        execution.TotalRecordsProcessed += result.RecordsProcessed;
                        execution.TotalAmountRecovered += result.AmountRecovered;

                        _logger.LogInformation("Processed {Count} expired verifications, recovered ${Amount:N2}",
                            result.RecordsProcessed, result.AmountRecovered);
                    }
                }

                // Step 2A: Process verified but not submitted calls (NEW RULES)
                _logger.LogInformation("Step 2A: Processing verified but not submitted calls");
                var verifiedNotSubmittedCount = 0;

                var activeBatchesForVerified = await context.StagingBatches
                    .Where(b => b.BatchStatus == BatchStatus.Processing ||
                               b.BatchStatus == BatchStatus.PartiallyVerified ||
                               b.BatchStatus == BatchStatus.Verified ||
                               b.BatchStatus == BatchStatus.Published)
                    .ToListAsync();

                foreach (var batch in activeBatchesForVerified)
                {
                    var result = await recoveryService.ProcessVerifiedButNotSubmittedAsync(batch.Id);
                    if (result.Success && result.RecordsProcessed > 0)
                    {
                        verifiedNotSubmittedCount += result.RecordsProcessed;
                        execution.TotalRecordsProcessed += result.RecordsProcessed;
                        execution.TotalAmountRecovered += result.AmountRecovered;

                        _logger.LogInformation("Processed {Count} verified but not submitted calls for batch {BatchId}, recovered ${Amount:N2}",
                            result.RecordsProcessed, batch.Id, result.AmountRecovered);
                    }
                }

                if (verifiedNotSubmittedCount > 0)
                {
                    _logger.LogInformation("Total verified but not submitted calls processed: {Count}", verifiedNotSubmittedCount);
                }

                // Step 3: Process expired approval deadlines
                var expiredApprovals = await deadlineService.GetExpiredApprovalDeadlinesAsync();
                _logger.LogInformation("Found {Count} expired approval deadlines", expiredApprovals.Count);

                foreach (var deadline in expiredApprovals)
                {
                    var result = await recoveryService.ProcessExpiredApprovalsAsync(deadline.BatchId);
                    if (result.Success)
                    {
                        deadline.RecoveryProcessed = true;
                        deadline.RecoveryProcessedDate = DateTime.UtcNow;
                        deadline.DeadlineStatus = "Missed";

                        execution.ExpiredApprovalsProcessed++;
                        execution.TotalRecordsProcessed += result.RecordsProcessed;
                        execution.TotalAmountRecovered += result.AmountRecovered;

                        _logger.LogInformation("Processed {Count} expired approvals, recovered ${Amount:N2}",
                            result.RecordsProcessed, result.AmountRecovered);
                    }
                }

                // Step 4: Process reverted verifications
                var activeBatches = await context.StagingBatches
                    .Where(b => b.BatchStatus == BatchStatus.Processing ||
                               b.BatchStatus == BatchStatus.PartiallyVerified ||
                               b.BatchStatus == BatchStatus.Verified ||
                               b.BatchStatus == BatchStatus.Published)
                    .ToListAsync();

                _logger.LogInformation("Checking {Count} active batches for reverted verifications", activeBatches.Count);

                foreach (var batch in activeBatches)
                {
                    var result = await recoveryService.ProcessRevertedVerificationsAsync(batch.Id);
                    if (result.Success && result.RecordsProcessed > 0)
                    {
                        execution.RevertedVerificationsProcessed += result.RecordsProcessed;
                        execution.TotalRecordsProcessed += result.RecordsProcessed;
                        execution.TotalAmountRecovered += result.AmountRecovered;

                        _logger.LogInformation("Processed {Count} reverted verifications for batch {BatchId}, recovered ${Amount:N2}",
                            result.RecordsProcessed, batch.Id, result.AmountRecovered);
                    }
                }

                // Save deadline updates
                await context.SaveChangesAsync();

                // Mark as completed
                stopwatch.Stop();
                execution.EndTime = DateTime.UtcNow;
                execution.DurationMs = stopwatch.ElapsedMilliseconds;
                execution.Status = "Completed";
                execution.ExecutionLog = $"Manual recovery job completed successfully.\n" +
                    $"Total Records: {execution.TotalRecordsProcessed}\n" +
                    $"Total Amount: ${execution.TotalAmountRecovered:N2}";

                await context.SaveChangesAsync();

                _logger.LogInformation("===== Manual Recovery Job Completed =====");
                _logger.LogInformation("Duration: {Duration}ms", execution.DurationMs);
                _logger.LogInformation("Total Records Processed: {Count}", execution.TotalRecordsProcessed);
                _logger.LogInformation("Total Amount Recovered: ${Amount:N2}", execution.TotalAmountRecovered);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                execution.EndTime = DateTime.UtcNow;
                execution.DurationMs = stopwatch.ElapsedMilliseconds;
                execution.Status = "Failed";
                execution.ErrorMessage = ex.Message;
                execution.ExecutionLog = $"Manual recovery job failed.\nError: {ex.Message}\n{ex.StackTrace}";

                await context.SaveChangesAsync();

                _logger.LogError(ex, "Manual Recovery Job FAILED after {Duration}ms", execution.DurationMs);
            }
        }

        public async Task<IActionResult> OnPostDeleteExecutionAsync(int id)
        {
            try
            {
                var execution = await _context.RecoveryJobExecutions.FindAsync(id);
                if (execution != null && execution.Status != "Running")
                {
                    _context.RecoveryJobExecutions.Remove(execution);
                    await _context.SaveChangesAsync();
                    SuccessMessage = "Execution record deleted successfully.";
                }
                else if (execution?.Status == "Running")
                {
                    ErrorMessage = "Cannot delete a running execution.";
                }
                else
                {
                    ErrorMessage = "Execution record not found.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting execution {Id}", id);
                ErrorMessage = $"Error deleting execution: {ex.Message}";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnGetViewExecutionLogAsync(int id)
        {
            var execution = await _context.RecoveryJobExecutions.FindAsync(id);
            if (execution == null)
            {
                return NotFound();
            }

            return new JsonResult(new
            {
                id = execution.Id,
                startTime = execution.StartTime,
                endTime = execution.EndTime,
                status = execution.Status,
                runType = execution.RunType,
                triggeredBy = execution.TriggeredBy,
                durationMs = execution.DurationMs,
                expiredVerificationsProcessed = execution.ExpiredVerificationsProcessed,
                expiredApprovalsProcessed = execution.ExpiredApprovalsProcessed,
                revertedVerificationsProcessed = execution.RevertedVerificationsProcessed,
                totalRecordsProcessed = execution.TotalRecordsProcessed,
                totalAmountRecovered = execution.TotalAmountRecovered,
                totalAmountRecoveredKSH = execution.TotalAmountRecoveredKSH,
                totalAmountRecoveredUSD = execution.TotalAmountRecoveredUSD,
                remindersSent = execution.RemindersSent,
                errorMessage = execution.ErrorMessage,
                executionLog = execution.ExecutionLog,
                nextScheduledRun = execution.NextScheduledRun
            });
        }

        private async Task CalculateStatisticsAsync()
        {
            var last30Days = DateTime.UtcNow.AddDays(-30);

            // Total executions
            Statistics.TotalExecutions = await _context.RecoveryJobExecutions.CountAsync();

            // Executions in last 30 days
            Statistics.ExecutionsLast30Days = await _context.RecoveryJobExecutions
                .Where(e => e.StartTime >= last30Days)
                .CountAsync();

            // Success rate
            var completedJobs = await _context.RecoveryJobExecutions
                .Where(e => e.Status == "Completed" || e.Status == "Failed")
                .CountAsync();

            if (completedJobs > 0)
            {
                var successfulJobs = await _context.RecoveryJobExecutions
                    .Where(e => e.Status == "Completed")
                    .CountAsync();
                Statistics.SuccessRate = (decimal)successfulJobs / completedJobs * 100;
            }

            // Total amounts recovered
            Statistics.TotalAmountRecovered = await _context.RecoveryJobExecutions
                .Where(e => e.Status == "Completed")
                .SumAsync(e => (decimal?)e.TotalAmountRecovered) ?? 0;

            Statistics.TotalAmountRecoveredKSH = await _context.RecoveryJobExecutions
                .Where(e => e.Status == "Completed")
                .SumAsync(e => (decimal?)e.TotalAmountRecoveredKSH) ?? 0;

            Statistics.TotalAmountRecoveredUSD = await _context.RecoveryJobExecutions
                .Where(e => e.Status == "Completed")
                .SumAsync(e => (decimal?)e.TotalAmountRecoveredUSD) ?? 0;

            Statistics.AmountRecoveredLast30Days = await _context.RecoveryJobExecutions
                .Where(e => e.Status == "Completed" && e.StartTime >= last30Days)
                .SumAsync(e => (decimal?)e.TotalAmountRecovered) ?? 0;

            Statistics.AmountRecoveredLast30DaysKSH = await _context.RecoveryJobExecutions
                .Where(e => e.Status == "Completed" && e.StartTime >= last30Days)
                .SumAsync(e => (decimal?)e.TotalAmountRecoveredKSH) ?? 0;

            Statistics.AmountRecoveredLast30DaysUSD = await _context.RecoveryJobExecutions
                .Where(e => e.Status == "Completed" && e.StartTime >= last30Days)
                .SumAsync(e => (decimal?)e.TotalAmountRecoveredUSD) ?? 0;

            // Total records processed
            Statistics.TotalRecordsProcessed = await _context.RecoveryJobExecutions
                .Where(e => e.Status == "Completed")
                .SumAsync(e => (int?)e.TotalRecordsProcessed) ?? 0;

            Statistics.RecordsProcessedLast30Days = await _context.RecoveryJobExecutions
                .Where(e => e.Status == "Completed" && e.StartTime >= last30Days)
                .SumAsync(e => (int?)e.TotalRecordsProcessed) ?? 0;

            // Average duration
            var completedJobsWithDuration = await _context.RecoveryJobExecutions
                .Where(e => e.Status == "Completed" && e.DurationMs.HasValue)
                .Select(e => e.DurationMs!.Value)
                .ToListAsync();

            if (completedJobsWithDuration.Any())
            {
                Statistics.AverageDurationMs = (long)completedJobsWithDuration.Average();
            }

            // Failed jobs
            Statistics.FailedExecutions = await _context.RecoveryJobExecutions
                .Where(e => e.Status == "Failed")
                .CountAsync();
        }

        /// <summary>
        /// Automatically marks jobs as "Failed" if they've been running for more than 2 hours (stuck)
        /// </summary>
        private async Task MarkStuckJobsAsFailedAsync()
        {
            try
            {
                var timeoutThreshold = DateTime.UtcNow.AddHours(-2); // Jobs running more than 2 hours are stuck

                var stuckJobs = await _context.RecoveryJobExecutions
                    .Where(e => e.Status == "Running" && e.StartTime < timeoutThreshold)
                    .ToListAsync();

                if (stuckJobs.Any())
                {
                    foreach (var job in stuckJobs)
                    {
                        job.Status = "Failed";
                        job.EndTime = DateTime.UtcNow;
                        job.DurationMs = (long)(job.EndTime.Value - job.StartTime).TotalMilliseconds;
                        job.ErrorMessage = "Job timeout - exceeded 2 hour execution limit. Marked as failed automatically.";
                        job.ExecutionLog = (job.ExecutionLog ?? "") + $"\n\n[{DateTime.UtcNow}] Job marked as FAILED due to timeout (running for {(DateTime.UtcNow - job.StartTime).TotalHours:F2} hours)";

                        _logger.LogWarning("Marked stuck job {JobId} as failed. Started: {StartTime}, Duration: {Duration} hours",
                            job.Id, job.StartTime, (DateTime.UtcNow - job.StartTime).TotalHours);
                    }

                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Automatically marked {Count} stuck job(s) as failed", stuckJobs.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking stuck jobs as failed");
            }
        }

        /// <summary>
        /// Manually cancel a running recovery job
        /// </summary>
        public async Task<IActionResult> OnPostCancelJobAsync(int id)
        {
            try
            {
                var job = await _context.RecoveryJobExecutions.FindAsync(id);

                if (job == null)
                {
                    ErrorMessage = "Job not found.";
                    return RedirectToPage();
                }

                if (job.Status != "Running")
                {
                    ErrorMessage = $"Cannot cancel job - status is '{job.Status}'.";
                    return RedirectToPage();
                }

                // Mark job as cancelled
                job.Status = "Cancelled";
                job.EndTime = DateTime.UtcNow;
                job.DurationMs = (long)(job.EndTime.Value - job.StartTime).TotalMilliseconds;
                job.ErrorMessage = $"Job manually cancelled by {User.Identity?.Name ?? "Unknown"} at {DateTime.UtcNow}";
                job.ExecutionLog = (job.ExecutionLog ?? "") + $"\n\n[{DateTime.UtcNow}] Job manually CANCELLED by {User.Identity?.Name ?? "Unknown"}";

                await _context.SaveChangesAsync();

                _logger.LogWarning("Recovery job {JobId} manually cancelled by {User}", id, User.Identity?.Name);

                SuccessMessage = $"Job #{id} has been cancelled successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling job {JobId}", id);
                ErrorMessage = $"Error cancelling job: {ex.Message}";
            }

            return RedirectToPage();
        }
    }

    public class JobStatistics
    {
        public int TotalExecutions { get; set; }
        public int ExecutionsLast30Days { get; set; }
        public decimal SuccessRate { get; set; }
        public decimal TotalAmountRecovered { get; set; }
        public decimal TotalAmountRecoveredKSH { get; set; }
        public decimal TotalAmountRecoveredUSD { get; set; }
        public decimal AmountRecoveredLast30Days { get; set; }
        public decimal AmountRecoveredLast30DaysKSH { get; set; }
        public decimal AmountRecoveredLast30DaysUSD { get; set; }
        public int TotalRecordsProcessed { get; set; }
        public int RecordsProcessedLast30Days { get; set; }
        public long AverageDurationMs { get; set; }
        public int FailedExecutions { get; set; }
    }
}
