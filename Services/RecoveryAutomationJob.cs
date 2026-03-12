using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TAB.Web.Data;
using TAB.Web.Models;

namespace TAB.Web.Services
{
    /// <summary>
    /// Background service that automatically processes expired deadlines and executes recovery rules
    /// </summary>
    public class RecoveryAutomationJob : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<RecoveryAutomationJob> _logger;
        private TimeSpan _checkInterval; // Not readonly - updated from database on each run
        private Timer? _timer;

        public RecoveryAutomationJob(
            IServiceProvider serviceProvider,
            ILogger<RecoveryAutomationJob> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;

            // Default: Run every hour (will be overridden by database config)
            _checkInterval = TimeSpan.FromHours(1);
        }

        private async Task<TimeSpan> GetConfiguredIntervalAsync()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var config = await context.RecoveryConfigurations
                    .FirstOrDefaultAsync(rc => rc.RuleName == "SystemConfiguration");

                if (config?.JobIntervalMinutes.HasValue == true)
                {
                    var intervalMinutes = config.JobIntervalMinutes.Value;
                    _logger.LogInformation("Loaded job interval from database: {Minutes} minutes ({Hours} hours)",
                        intervalMinutes, intervalMinutes / 60.0);
                    return TimeSpan.FromMinutes(intervalMinutes);
                }
                else
                {
                    _logger.LogWarning("RecoveryConfiguration not found or JobIntervalMinutes is null. Using default: 1 hour");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading job interval configuration, using default");
            }

            // Default to 1 hour if config not found
            _logger.LogWarning("Using fallback interval: 1 hour (60 minutes)");
            return TimeSpan.FromHours(1);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Recovery Automation Job started at {Time}", DateTime.UtcNow);

            // Wait a bit before first run to allow app to fully start
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                // Get current interval from database configuration
                var currentInterval = await GetConfiguredIntervalAsync();

                // Update the field so ProcessRecoveriesAsync can use it for NextScheduledRun
                _checkInterval = currentInterval;

                var nextRunTime = DateTime.UtcNow.Add(currentInterval);
                _logger.LogInformation("Job will run every {Interval}. Next run scheduled for: {NextRun}",
                    currentInterval, nextRunTime);

                try
                {
                    await ProcessRecoveriesAsync(isManual: false, triggeredBy: null);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Critical error in Recovery Automation Job main loop");
                }

                // Wait for next interval
                try
                {
                    _logger.LogInformation("Waiting {Minutes} minutes until next run...", currentInterval.TotalMinutes);
                    await Task.Delay(currentInterval, stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    // Service is stopping
                    _logger.LogInformation("Recovery Automation Job stopping");
                    break;
                }
            }
        }

        /// <summary>
        /// Main processing method that can be called automatically or manually
        /// </summary>
        public async Task ProcessRecoveriesAsync(bool isManual, string? triggeredBy)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var recoveryService = scope.ServiceProvider.GetRequiredService<ICallLogRecoveryService>();
            var deadlineService = scope.ServiceProvider.GetRequiredService<IDeadlineManagementService>();

            // Check if automation is enabled (skip check for manual runs)
            if (!isManual)
            {
                var config = await context.RecoveryConfigurations
                    .FirstOrDefaultAsync(rc => rc.RuleName == "SystemConfiguration");

                if (config != null && !config.AutomationEnabled)
                {
                    _logger.LogInformation("Recovery automation is disabled in configuration. Skipping automatic run.");
                    return;
                }
            }

            var stopwatch = Stopwatch.StartNew();
            var executionLog = new StringBuilder();

            var execution = new RecoveryJobExecution
            {
                StartTime = DateTime.UtcNow,
                Status = "Running",
                RunType = isManual ? "Manual" : "Automatic",
                TriggeredBy = triggeredBy
            };

            try
            {
                context.RecoveryJobExecutions.Add(execution);
                await context.SaveChangesAsync();

                _logger.LogInformation("===== Recovery Automation Job Started =====");
                _logger.LogInformation("Execution ID: {ExecutionId}, Type: {RunType}, Triggered By: {TriggeredBy}",
                    execution.Id, execution.RunType, triggeredBy ?? "System");

                executionLog.AppendLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Job started");
                executionLog.AppendLine($"Run Type: {execution.RunType}");

                // Step 1: Send deadline reminders
                _logger.LogInformation("Step 1: Sending deadline reminders");
                executionLog.AppendLine("\n--- Step 1: Deadline Reminders ---");

                try
                {
                    await deadlineService.SendDeadlineRemindersAsync();
                    execution.RemindersSent = 1; // This would need to be updated to return actual count
                    executionLog.AppendLine($"Deadline reminders sent successfully");
                    _logger.LogInformation("Deadline reminders sent successfully");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending deadline reminders");
                    executionLog.AppendLine($"ERROR sending reminders: {ex.Message}");
                }

                // Step 2: Process expired verification deadlines
                _logger.LogInformation("Step 2: Processing expired verification deadlines");
                executionLog.AppendLine("\n--- Step 2: Expired Verification Deadlines ---");

                var expiredVerifications = await deadlineService.GetExpiredVerificationDeadlinesAsync();
                _logger.LogInformation("Found {Count} expired verification deadlines", expiredVerifications.Count);
                executionLog.AppendLine($"Found {expiredVerifications.Count} expired verification deadlines");

                foreach (var deadline in expiredVerifications)
                {
                    _logger.LogInformation("Processing expired verification deadline for batch {BatchId}", deadline.BatchId);
                    executionLog.AppendLine($"\nProcessing Batch: {deadline.BatchId}");
                    executionLog.AppendLine($"  Deadline Type: {deadline.DeadlineType}");
                    executionLog.AppendLine($"  Target: {deadline.TargetEntity}");
                    executionLog.AppendLine($"  Deadline: {deadline.DeadlineDate:yyyy-MM-dd HH:mm}");

                    var result = await recoveryService.ProcessExpiredVerificationsAsync(deadline.BatchId);

                    if (result.Success)
                    {
                        deadline.RecoveryProcessed = true;
                        deadline.RecoveryProcessedDate = DateTime.UtcNow;
                        deadline.DeadlineStatus = "Missed";

                        execution.ExpiredVerificationsProcessed++;
                        execution.TotalRecordsProcessed += result.RecordsProcessed;
                        execution.TotalAmountRecovered += result.AmountRecovered;

                        _logger.LogInformation(
                            "✓ Processed {Count} expired verifications, recovered ${Amount:N2}",
                            result.RecordsProcessed,
                            result.AmountRecovered);

                        executionLog.AppendLine($"  ✓ SUCCESS: {result.RecordsProcessed} records, ${result.AmountRecovered:N2} recovered");
                        executionLog.AppendLine($"  Message: {result.Message}");
                    }
                    else
                    {
                        var errorDetails = result.Errors.Count > 0
                            ? string.Join("; ", result.Errors)
                            : result.Message;

                        _logger.LogWarning("Failed to process expired verification for batch {BatchId}: {ErrorDetails}",
                            deadline.BatchId, errorDetails);

                        executionLog.AppendLine($"  ✗ FAILED: {result.Message}");

                        if (result.Errors.Count > 0)
                        {
                            foreach (var error in result.Errors)
                            {
                                executionLog.AppendLine($"    Error: {error}");
                                _logger.LogWarning("  - Error detail: {Error}", error);
                            }
                        }
                    }
                }

                // Step 2 Fallback: When no DeadlineTracking rows exist (e.g., creation failed during push),
                // discover expired batches directly from CallRecords.verification_period
                if (expiredVerifications.Count == 0)
                {
                    _logger.LogInformation("Step 2 Fallback: Checking CallRecords for expired verification periods without DeadlineTracking");
                    executionLog.AppendLine("\n--- Step 2 Fallback: CallRecords Verification Period Check ---");

                    try
                    {
                        var now = DateTime.UtcNow;

                        // Find batch IDs with expired verification periods and unrecovered records
                        var batchesWithExpiredVerification = await context.CallRecords
                            .Where(cr => cr.VerificationPeriod.HasValue
                                      && cr.VerificationPeriod.Value < now
                                      && cr.IsVerified == false
                                      && (cr.RecoveryStatus == "NotProcessed" || cr.RecoveryStatus == null))
                            .Select(cr => cr.SourceBatchId)
                            .Where(bid => bid.HasValue)
                            .Distinct()
                            .ToListAsync();

                        // Exclude batches that already have DeadlineTracking rows
                        var batchesWithDeadlines = await context.DeadlineTracking
                            .Where(dt => dt.DeadlineType == "InitialVerification")
                            .Select(dt => dt.BatchId)
                            .Distinct()
                            .ToListAsync();

                        var orphanedBatches = batchesWithExpiredVerification
                            .Where(b => b.HasValue && !batchesWithDeadlines.Contains(b.Value))
                            .Select(b => b!.Value)
                            .ToList();

                        if (orphanedBatches.Any())
                        {
                            _logger.LogWarning("Found {Count} batches with expired verification periods but no DeadlineTracking rows", orphanedBatches.Count);
                            executionLog.AppendLine($"Found {orphanedBatches.Count} orphaned batches with expired verifications");

                            foreach (var batchId in orphanedBatches)
                            {
                                _logger.LogInformation("Processing orphaned batch {BatchId} via fallback", batchId);
                                executionLog.AppendLine($"\nProcessing orphaned batch: {batchId}");

                                var result = await recoveryService.ProcessExpiredVerificationsAsync(batchId);

                                if (result.Success)
                                {
                                    execution.ExpiredVerificationsProcessed++;
                                    execution.TotalRecordsProcessed += result.RecordsProcessed;
                                    execution.TotalAmountRecovered += result.AmountRecovered;

                                    _logger.LogInformation(
                                        "Fallback: Processed {Count} expired verifications for batch {BatchId}, recovered ${Amount:N2}",
                                        result.RecordsProcessed, batchId, result.AmountRecovered);

                                    executionLog.AppendLine($"  SUCCESS: {result.RecordsProcessed} records, ${result.AmountRecovered:N2} recovered");

                                    // Create the missing DeadlineTracking row retroactively
                                    try
                                    {
                                        var earliestExpiry = await context.CallRecords
                                            .Where(cr => cr.SourceBatchId == batchId && cr.VerificationPeriod.HasValue)
                                            .MinAsync(cr => cr.VerificationPeriod!.Value);

                                        var deadline = await deadlineService.CreateVerificationDeadlineAsync(batchId, earliestExpiry, "System-RecoveryFallback");
                                        deadline.RecoveryProcessed = true;
                                        deadline.RecoveryProcessedDate = DateTime.UtcNow;
                                        deadline.DeadlineStatus = "Missed";
                                        deadline.MissedDate = earliestExpiry;
                                        await context.SaveChangesAsync();

                                        _logger.LogInformation("Created retroactive DeadlineTracking for batch {BatchId}", batchId);
                                    }
                                    catch (Exception dtEx)
                                    {
                                        _logger.LogWarning(dtEx, "Could not create retroactive DeadlineTracking for batch {BatchId}", batchId);
                                    }
                                }
                                else
                                {
                                    executionLog.AppendLine($"  FAILED: {result.Message}");
                                    _logger.LogWarning("Fallback: Failed to process batch {BatchId}: {Message}", batchId, result.Message);
                                }
                            }
                        }
                        else
                        {
                            executionLog.AppendLine("No orphaned batches found");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error in Step 2 fallback processing");
                        executionLog.AppendLine($"ERROR in fallback: {ex.Message}");
                    }
                }

                // Step 2A: Process verified but not submitted calls
                _logger.LogInformation("Step 2A: Processing verified but not submitted calls");
                executionLog.AppendLine("\n--- Step 2A: Verified But Not Submitted Calls ---");

                var activeBatchesForVerified = await context.StagingBatches
                    .Where(b => b.BatchStatus == BatchStatus.Processing ||
                               b.BatchStatus == BatchStatus.PartiallyVerified ||
                               b.BatchStatus == BatchStatus.Verified ||
                               b.BatchStatus == BatchStatus.Published)
                    .ToListAsync();

                _logger.LogInformation("Checking {Count} active batches for verified but not submitted calls", activeBatchesForVerified.Count);
                executionLog.AppendLine($"Checking {activeBatchesForVerified.Count} active batches");

                int verifiedNotSubmittedCount = 0;
                foreach (var batch in activeBatchesForVerified)
                {
                    var result = await recoveryService.ProcessVerifiedButNotSubmittedAsync(batch.Id);

                    if (result.Success && result.RecordsProcessed > 0)
                    {
                        verifiedNotSubmittedCount += result.RecordsProcessed;
                        execution.TotalRecordsProcessed += result.RecordsProcessed;
                        execution.TotalAmountRecovered += result.AmountRecovered;

                        _logger.LogInformation(
                            "✓ Processed {Count} verified but not submitted calls for batch {BatchId}, recovered ${Amount:N2}",
                            result.RecordsProcessed,
                            batch.Id,
                            result.AmountRecovered);

                        executionLog.AppendLine($"\nBatch: {batch.BatchName} ({batch.Id})");
                        executionLog.AppendLine($"  ✓ SUCCESS: {result.RecordsProcessed} records, ${result.AmountRecovered:N2} recovered");
                        executionLog.AppendLine($"  Message: {result.Message}");
                    }
                }

                if (verifiedNotSubmittedCount > 0)
                {
                    _logger.LogInformation("Total verified but not submitted calls processed: {Count}", verifiedNotSubmittedCount);
                    executionLog.AppendLine($"\nTotal verified but not submitted: {verifiedNotSubmittedCount} records");
                }

                // Step 3: Process expired approval deadlines
                _logger.LogInformation("Step 3: Processing expired approval deadlines");
                executionLog.AppendLine("\n--- Step 3: Expired Approval Deadlines ---");

                var expiredApprovals = await deadlineService.GetExpiredApprovalDeadlinesAsync();
                _logger.LogInformation("Found {Count} expired approval deadlines", expiredApprovals.Count);
                executionLog.AppendLine($"Found {expiredApprovals.Count} expired approval deadlines");

                foreach (var deadline in expiredApprovals)
                {
                    _logger.LogInformation("Processing expired approval deadline for batch {BatchId}", deadline.BatchId);
                    executionLog.AppendLine($"\nProcessing Batch: {deadline.BatchId}");
                    executionLog.AppendLine($"  Deadline Type: {deadline.DeadlineType}");
                    executionLog.AppendLine($"  Target: {deadline.TargetEntity}");
                    executionLog.AppendLine($"  Deadline: {deadline.DeadlineDate:yyyy-MM-dd HH:mm}");

                    var result = await recoveryService.ProcessExpiredApprovalsAsync(deadline.BatchId);

                    if (result.Success)
                    {
                        deadline.RecoveryProcessed = true;
                        deadline.RecoveryProcessedDate = DateTime.UtcNow;
                        deadline.DeadlineStatus = "Missed";

                        execution.ExpiredApprovalsProcessed++;
                        execution.TotalRecordsProcessed += result.RecordsProcessed;
                        execution.TotalAmountRecovered += result.AmountRecovered;

                        _logger.LogInformation(
                            "✓ Processed {Count} expired approvals, recovered ${Amount:N2}",
                            result.RecordsProcessed,
                            result.AmountRecovered);

                        executionLog.AppendLine($"  ✓ SUCCESS: {result.RecordsProcessed} records, ${result.AmountRecovered:N2} recovered");
                        executionLog.AppendLine($"  Message: {result.Message}");
                    }
                    else
                    {
                        var errorDetails = result.Errors.Count > 0
                            ? string.Join("; ", result.Errors)
                            : result.Message;

                        _logger.LogWarning("Failed to process expired approval for batch {BatchId}: {ErrorDetails}",
                            deadline.BatchId, errorDetails);

                        executionLog.AppendLine($"  ✗ FAILED: {result.Message}");

                        if (result.Errors.Count > 0)
                        {
                            foreach (var error in result.Errors)
                            {
                                executionLog.AppendLine($"    Error: {error}");
                                _logger.LogWarning("  - Error detail: {Error}", error);
                            }
                        }
                    }
                }

                // Step 4: Process reverted verifications
                _logger.LogInformation("Step 4: Processing reverted verifications");
                executionLog.AppendLine("\n--- Step 4: Reverted Verifications ---");

                var activeBatches = await context.StagingBatches
                    .Where(b => b.BatchStatus == BatchStatus.Processing ||
                               b.BatchStatus == BatchStatus.PartiallyVerified ||
                               b.BatchStatus == BatchStatus.Verified ||
                               b.BatchStatus == BatchStatus.Published)
                    .ToListAsync();

                _logger.LogInformation("Checking {Count} active batches for reverted verifications", activeBatches.Count);
                executionLog.AppendLine($"Checking {activeBatches.Count} active batches");

                foreach (var batch in activeBatches)
                {
                    var result = await recoveryService.ProcessRevertedVerificationsAsync(batch.Id);

                    if (result.Success && result.RecordsProcessed > 0)
                    {
                        execution.RevertedVerificationsProcessed += result.RecordsProcessed;
                        execution.TotalRecordsProcessed += result.RecordsProcessed;
                        execution.TotalAmountRecovered += result.AmountRecovered;

                        _logger.LogInformation(
                            "✓ Processed {Count} reverted verifications for batch {BatchId}, recovered ${Amount:N2}",
                            result.RecordsProcessed,
                            batch.Id,
                            result.AmountRecovered);

                        executionLog.AppendLine($"\nBatch: {batch.BatchName} ({batch.Id})");
                        executionLog.AppendLine($"  ✓ SUCCESS: {result.RecordsProcessed} records, ${result.AmountRecovered:N2} recovered");
                    }
                }

                // Save deadline updates
                await context.SaveChangesAsync();

                // Calculate next run time
                execution.NextScheduledRun = DateTime.UtcNow.Add(_checkInterval);

                // Mark as completed
                stopwatch.Stop();
                execution.EndTime = DateTime.UtcNow;
                execution.DurationMs = stopwatch.ElapsedMilliseconds;
                execution.Status = "Completed";
                execution.ExecutionLog = executionLog.ToString();

                await context.SaveChangesAsync();

                _logger.LogInformation("===== Recovery Automation Job Completed =====");
                _logger.LogInformation("Duration: {Duration}ms", execution.DurationMs);
                _logger.LogInformation("Total Records Processed: {Count}", execution.TotalRecordsProcessed);
                _logger.LogInformation("Total Amount Recovered: ${Amount:N2}", execution.TotalAmountRecovered);
                _logger.LogInformation("Next Run: {NextRun}", execution.NextScheduledRun);

                executionLog.AppendLine($"\n--- Job Completed ---");
                executionLog.AppendLine($"Duration: {execution.DurationMs}ms");
                executionLog.AppendLine($"Total Records: {execution.TotalRecordsProcessed}");
                executionLog.AppendLine($"Total Amount: ${execution.TotalAmountRecovered:N2}");
                executionLog.AppendLine($"Next Scheduled Run: {execution.NextScheduledRun:yyyy-MM-dd HH:mm:ss}");
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                execution.EndTime = DateTime.UtcNow;
                execution.DurationMs = stopwatch.ElapsedMilliseconds;
                execution.Status = "Failed";
                execution.ErrorMessage = ex.Message;
                execution.ExecutionLog = executionLog.ToString() + $"\n\nFATAL ERROR: {ex.Message}\n{ex.StackTrace}";

                await context.SaveChangesAsync();

                _logger.LogError(ex, "Recovery Automation Job FAILED after {Duration}ms", execution.DurationMs);
            }
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Recovery Automation Job is stopping");

            _timer?.Dispose();

            await base.StopAsync(stoppingToken);

            _logger.LogInformation("Recovery Automation Job stopped");
        }
    }
}
