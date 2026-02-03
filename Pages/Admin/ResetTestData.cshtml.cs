using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TAB.Web.Data;

namespace TAB.Web.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class ResetTestDataModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ResetTestDataModel> _logger;

        public ResetTestDataModel(
            ApplicationDbContext context,
            ILogger<ResetTestDataModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public string? Message { get; set; }
        public bool Success { get; set; }

        // Statistics for display
        public int CallRecordCount { get; set; }
        public int StagingRecordCount { get; set; }
        public int RecoveryLogCount { get; set; }
        public int DeadlineTrackingCount { get; set; }
        public int RecoveryJobCount { get; set; }
        public int OverageJustificationCount { get; set; }
        public int TelecomRecordCount { get; set; }
        public int ReconciliationCount { get; set; }
        public int TotalRecordsToDelete { get; set; }

        public async Task OnGetAsync()
        {
            await LoadStatisticsAsync();
        }

        public async Task<IActionResult> OnPostAsync(string confirmText)
        {
            var log = new StringBuilder();
            int? originalTimeout = null; // Declare at method scope for access in catch block

            try
            {
                // Validate confirmation
                if (confirmText?.Trim() != "DELETE TEST DATA")
                {
                    Message = "Invalid confirmation text. Please type 'DELETE TEST DATA' exactly.";
                    Success = false;
                    await LoadStatisticsAsync();
                    return Page();
                }

                log.AppendLine("========================================");
                log.AppendLine("TEST DATA RESET OPERATION");
                log.AppendLine("========================================");
                log.AppendLine($"Started: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                log.AppendLine($"User: {User.Identity?.Name}");
                log.AppendLine();

                // Load initial counts
                await LoadStatisticsAsync();
                log.AppendLine($"Total Records to Delete: {TotalRecordsToDelete:N0}");
                log.AppendLine();

                int totalDeleted = 0;

                // Increase command timeout for large deletions (30 minutes)
                originalTimeout = _context.Database.GetCommandTimeout();
                _context.Database.SetCommandTimeout(1800); // 30 minutes

                // Use execution strategy to handle the transaction
                var strategy = _context.Database.CreateExecutionStrategy();
                await strategy.ExecuteAsync(async () =>
                {
                    // Start transaction
                    using var transaction = await _context.Database.BeginTransactionAsync();

                    try
                    {
                        // 1. Delete Phone Overage Documents (must delete before PhoneOverageJustifications)
                        log.AppendLine("[1/12] Deleting Phone Overage Documents...");
                        var overageDocs = await _context.PhoneOverageDocuments.CountAsync();
                        await _context.Database.ExecuteSqlRawAsync("DELETE FROM PhoneOverageDocuments");
                        totalDeleted += overageDocs;
                        log.AppendLine($"  ✓ Deleted {overageDocs:N0} phone overage documents");

                        // 2. Delete Phone Overage Justifications
                        log.AppendLine("[2/12] Deleting Phone Overage Justifications...");
                        var overageCount = await _context.PhoneOverageJustifications.CountAsync();
                        await _context.Database.ExecuteSqlRawAsync("DELETE FROM PhoneOverageJustifications");
                        totalDeleted += overageCount;
                        log.AppendLine($"  ✓ Deleted {overageCount:N0} phone overage justifications");

                        // 3. Delete Call Log Documents (deprecated table)
                        log.AppendLine("[3/12] Deleting Call Log Documents...");
                        var docCount = await _context.CallLogDocuments.CountAsync();
                        await _context.Database.ExecuteSqlRawAsync("DELETE FROM CallLogDocuments");
                        totalDeleted += docCount;
                        log.AppendLine($"  ✓ Deleted {docCount:N0} call log documents");

                        // 4. Delete Call Log Payment Assignments
                        log.AppendLine("[4/12] Deleting Call Log Payment Assignments...");
                        var paymentCount = await _context.CallLogPaymentAssignments.CountAsync();
                        await _context.Database.ExecuteSqlRawAsync("DELETE FROM CallLogPaymentAssignments");
                        totalDeleted += paymentCount;
                        log.AppendLine($"  ✓ Deleted {paymentCount:N0} payment assignments");

                        // 5. Delete Call Log Verifications
                        log.AppendLine("[5/12] Deleting Call Log Verifications...");
                        var verificationCount = await _context.CallLogVerifications.CountAsync();
                        await _context.Database.ExecuteSqlRawAsync("DELETE FROM CallLogVerifications");
                        totalDeleted += verificationCount;
                        log.AppendLine($"  ✓ Deleted {verificationCount:N0} call log verifications");

                        // 6. Delete Recovery Logs (must delete before CallRecords)
                        log.AppendLine("[6/12] Deleting Recovery Logs...");
                        var recoveryCount = await _context.RecoveryLogs.CountAsync();
                        await _context.Database.ExecuteSqlRawAsync("DELETE FROM RecoveryLogs");
                        totalDeleted += recoveryCount;
                        log.AppendLine($"  ✓ Deleted {recoveryCount:N0} recovery logs");

                        // 7. Delete Deadline Tracking
                        log.AppendLine("[7/12] Deleting Deadline Tracking...");
                        var deadlineCount = await _context.DeadlineTracking.CountAsync();
                        await _context.Database.ExecuteSqlRawAsync("DELETE FROM DeadlineTracking");
                        totalDeleted += deadlineCount;
                        log.AppendLine($"  ✓ Deleted {deadlineCount:N0} deadline tracking records");

                        // 8. Delete Recovery Job Executions
                        log.AppendLine("[8/12] Deleting Recovery Job Executions...");
                        var jobCount = await _context.RecoveryJobExecutions.CountAsync();
                        await _context.Database.ExecuteSqlRawAsync("DELETE FROM RecoveryJobExecutions");
                        totalDeleted += jobCount;
                        log.AppendLine($"  ✓ Deleted {jobCount:N0} recovery job executions");

                        // 9. Delete Call Records
                        log.AppendLine("[9/12] Deleting Call Records...");
                        var callRecordCount = await _context.CallRecords.CountAsync();
                        await _context.Database.ExecuteSqlRawAsync("DELETE FROM CallRecords");
                        totalDeleted += callRecordCount;
                        log.AppendLine($"  ✓ Deleted {callRecordCount:N0} call records");

                        // 10. Delete Call Log Staging (must delete before StagingBatches)
                        // Use batch deletion for large tables to prevent transaction timeout
                        log.AppendLine("[10/12] Deleting Call Log Staging...");
                        var stagingCount = await _context.CallLogStagings.CountAsync();

                        if (stagingCount > 0)
                        {
                            const int BATCH_SIZE = 10000; // Delete 10K records at a time
                            int deletedSoFar = 0;
                            int rowsAffected = 0;

                            do
                            {
                                rowsAffected = await _context.Database.ExecuteSqlRawAsync(
                                    $"DELETE TOP ({BATCH_SIZE}) FROM CallLogStagings");

                                deletedSoFar += rowsAffected;

                                if (deletedSoFar % 50000 == 0 && deletedSoFar > 0)
                                {
                                    log.AppendLine($"  ... {deletedSoFar:N0} / {stagingCount:N0} deleted");
                                }
                            }
                            while (rowsAffected > 0);

                            totalDeleted += stagingCount;
                            log.AppendLine($"  ✓ Deleted {stagingCount:N0} staging records (in batches of {BATCH_SIZE:N0})");
                        }
                        else
                        {
                            log.AppendLine($"  ✓ No staging records found");
                        }

                        // 11. Delete Staging Batches
                        log.AppendLine("[11/12] Deleting Staging Batches...");
                        var batchCount = await _context.StagingBatches.CountAsync();
                        await _context.Database.ExecuteSqlRawAsync("DELETE FROM StagingBatches");
                        totalDeleted += batchCount;
                        log.AppendLine($"  ✓ Deleted {batchCount:N0} staging batches");

                        // 12. Delete Telecom Tables (Safaricom, Airtel, PSTN, PrivateWire) - if they exist
                        log.AppendLine("[12/13] Deleting Telecom Records...");
                        var telecomTotal = 0;

                        // Check if Safaricom table exists
                        try
                        {
                            var safaricomCount = await _context.Safaricoms.CountAsync();
                            if (safaricomCount > 0)
                            {
                                // Batch deletion for large tables
                                const int BATCH_SIZE = 10000;
                                int deletedSoFar = 0;
                                int rowsAffected = 0;

                                do
                                {
                                    rowsAffected = await _context.Database.ExecuteSqlRawAsync(
                                        $"DELETE TOP ({BATCH_SIZE}) FROM Safaricom");
                                    deletedSoFar += rowsAffected;

                                    if (deletedSoFar % 50000 == 0 && deletedSoFar > 0)
                                    {
                                        log.AppendLine($"  ... Safaricom: {deletedSoFar:N0} / {safaricomCount:N0} deleted");
                                    }
                                }
                                while (rowsAffected > 0);

                                telecomTotal += safaricomCount;
                                log.AppendLine($"  ✓ Deleted {safaricomCount:N0} Safaricom records (in batches)");
                            }
                            else
                            {
                                log.AppendLine($"  ✓ No Safaricom records found");
                            }
                        }
                        catch
                        {
                            log.AppendLine($"  ⊗ Safaricom table not found (skipped)");
                        }

                        // Check if Airtel table exists
                        try
                        {
                            var airtelCount = await _context.Airtels.CountAsync();
                            if (airtelCount > 0)
                            {
                                // Batch deletion for large tables
                                const int BATCH_SIZE = 10000;
                                int deletedSoFar = 0;
                                int rowsAffected = 0;

                                do
                                {
                                    rowsAffected = await _context.Database.ExecuteSqlRawAsync(
                                        $"DELETE TOP ({BATCH_SIZE}) FROM Airtel");
                                    deletedSoFar += rowsAffected;

                                    if (deletedSoFar % 50000 == 0 && deletedSoFar > 0)
                                    {
                                        log.AppendLine($"  ... Airtel: {deletedSoFar:N0} / {airtelCount:N0} deleted");
                                    }
                                }
                                while (rowsAffected > 0);

                                telecomTotal += airtelCount;
                                log.AppendLine($"  ✓ Deleted {airtelCount:N0} Airtel records (in batches)");
                            }
                            else
                            {
                                log.AppendLine($"  ✓ No Airtel records found");
                            }
                        }
                        catch
                        {
                            log.AppendLine($"  ⊗ Airtel table not found (skipped)");
                        }

                        // Check if PSTNs table exists
                        try
                        {
                            var pstnCount = await _context.PSTNs.CountAsync();
                            if (pstnCount > 0)
                            {
                                // Batch deletion for large tables
                                const int BATCH_SIZE = 10000;
                                int deletedSoFar = 0;
                                int rowsAffected = 0;

                                do
                                {
                                    rowsAffected = await _context.Database.ExecuteSqlRawAsync(
                                        $"DELETE TOP ({BATCH_SIZE}) FROM PSTNs");
                                    deletedSoFar += rowsAffected;

                                    if (deletedSoFar % 50000 == 0 && deletedSoFar > 0)
                                    {
                                        log.AppendLine($"  ... PSTN: {deletedSoFar:N0} / {pstnCount:N0} deleted");
                                    }
                                }
                                while (rowsAffected > 0);

                                telecomTotal += pstnCount;
                                log.AppendLine($"  ✓ Deleted {pstnCount:N0} PSTN records (in batches)");
                            }
                            else
                            {
                                log.AppendLine($"  ✓ No PSTN records found");
                            }
                        }
                        catch
                        {
                            log.AppendLine($"  ⊗ PSTNs table not found (skipped)");
                        }

                        // Check if PrivateWires table exists
                        try
                        {
                            var privateWireCount = await _context.PrivateWires.CountAsync();
                            if (privateWireCount > 0)
                            {
                                // Batch deletion for large tables
                                const int BATCH_SIZE = 10000;
                                int deletedSoFar = 0;
                                int rowsAffected = 0;

                                do
                                {
                                    rowsAffected = await _context.Database.ExecuteSqlRawAsync(
                                        $"DELETE TOP ({BATCH_SIZE}) FROM PrivateWires");
                                    deletedSoFar += rowsAffected;

                                    if (deletedSoFar % 50000 == 0 && deletedSoFar > 0)
                                    {
                                        log.AppendLine($"  ... PrivateWire: {deletedSoFar:N0} / {privateWireCount:N0} deleted");
                                    }
                                }
                                while (rowsAffected > 0);

                                telecomTotal += privateWireCount;
                                log.AppendLine($"  ✓ Deleted {privateWireCount:N0} PrivateWire records (in batches)");
                            }
                            else
                            {
                                log.AppendLine($"  ✓ No PrivateWire records found");
                            }
                        }
                        catch
                        {
                            log.AppendLine($"  ⊗ PrivateWires table not found (skipped)");
                        }

                        totalDeleted += telecomTotal;

                        // Delete Legacy Call Logs (deprecated table - deleted silently if exists)
                        try
                        {
                            #pragma warning disable CS0618 // Type or member is obsolete
                            var legacyCount = await _context.CallLogs.CountAsync();
                            if (legacyCount > 0)
                            {
                                await _context.Database.ExecuteSqlRawAsync("DELETE FROM CallLogs");
                                totalDeleted += legacyCount;
                            }
                            #pragma warning restore CS0618 // Type or member is obsolete
                        }
                        catch
                        {
                            // Legacy table doesn't exist - skip silently
                        }

                        // Delete Call Log Reconciliations
                        log.AppendLine();
                        log.AppendLine("[13/13] Deleting Call Log Reconciliations...");
                        try
                        {
                            var reconCount = await _context.CallLogReconciliations.CountAsync();
                            if (reconCount > 0)
                            {
                                await _context.Database.ExecuteSqlRawAsync("DELETE FROM CallLogReconciliations");
                                totalDeleted += reconCount;
                                log.AppendLine($"  ✓ Deleted {reconCount:N0} reconciliation records");
                            }
                            else
                            {
                                log.AppendLine($"  ✓ No reconciliation records found");
                            }
                        }
                        catch
                        {
                            log.AppendLine($"  ⊗ CallLogReconciliations table not found (skipped)");
                        }

                        // Commit transaction
                        await transaction.CommitAsync();

                        log.AppendLine();
                        log.AppendLine("========================================");
                        log.AppendLine("OPERATION COMPLETED SUCCESSFULLY");
                        log.AppendLine("========================================");
                        log.AppendLine($"Total Records Deleted: {totalDeleted:N0}");
                        log.AppendLine($"Completed: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                        log.AppendLine();
                        log.AppendLine("Master data preserved:");
                        log.AppendLine("  ✓ Organizations & Offices");
                        log.AppendLine("  ✓ Users & EBill Users");
                        log.AppendLine("  ✓ User Phones");
                        log.AppendLine("  ✓ Class of Service");
                        log.AppendLine("  ✓ Service Providers");
                        log.AppendLine("  ✓ Exchange Rates");
                        log.AppendLine("  ✓ Recovery Configurations");
                        log.AppendLine("  ✓ Email Settings & Templates");
                        log.AppendLine("  ✓ Anomaly Types");
                        log.AppendLine("  ✓ Billing Periods");

                        _logger.LogInformation($"Test data reset completed by {User.Identity?.Name}. {totalDeleted} records deleted.");

                        Message = log.ToString();
                        Success = true;
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        throw new Exception($"Transaction failed: {ex.Message}", ex);
                    }
                });

                // Restore original timeout
                _context.Database.SetCommandTimeout(originalTimeout);
            }
            catch (Exception ex)
            {
                // Restore original timeout on error
                try { _context.Database.SetCommandTimeout(originalTimeout); } catch { }

                log.AppendLine();
                log.AppendLine("========================================");
                log.AppendLine("ERROR - OPERATION FAILED");
                log.AppendLine("========================================");
                log.AppendLine($"Error: {ex.Message}");
                log.AppendLine($"Stack Trace: {ex.StackTrace}");
                log.AppendLine();
                log.AppendLine("No data was deleted - transaction rolled back.");

                _logger.LogError(ex, $"Test data reset failed for user {User.Identity?.Name}");

                Message = log.ToString();
                Success = false;
            }

            await LoadStatisticsAsync();
            return Page();
        }

        private async Task LoadStatisticsAsync()
        {
            try
            {
                CallRecordCount = await _context.CallRecords.CountAsync();
                StagingRecordCount = await _context.CallLogStagings.CountAsync() +
                                    await _context.StagingBatches.CountAsync();
                RecoveryLogCount = await _context.RecoveryLogs.CountAsync();
                DeadlineTrackingCount = await _context.DeadlineTracking.CountAsync();
                RecoveryJobCount = await _context.RecoveryJobExecutions.CountAsync();
                OverageJustificationCount = await _context.PhoneOverageJustifications.CountAsync() +
                                           await _context.PhoneOverageDocuments.CountAsync();

                // Count telecom records (handle missing tables)
                TelecomRecordCount = 0;
                try { TelecomRecordCount += await _context.Safaricoms.CountAsync(); } catch { }
                try { TelecomRecordCount += await _context.Airtels.CountAsync(); } catch { }
                try { TelecomRecordCount += await _context.PSTNs.CountAsync(); } catch { }
                try { TelecomRecordCount += await _context.PrivateWires.CountAsync(); } catch { }

                // Count reconciliations (handle missing table)
                ReconciliationCount = 0;
                try { ReconciliationCount = await _context.CallLogReconciliations.CountAsync(); } catch { }

                // Calculate total (including legacy call logs in backend but not displayed)
                var legacyCallLogCount = 0;
                try
                {
                    #pragma warning disable CS0618 // Type or member is obsolete
                    legacyCallLogCount = await _context.CallLogs.CountAsync();
                    #pragma warning restore CS0618 // Type or member is obsolete
                }
                catch { }

                TotalRecordsToDelete = CallRecordCount +
                                      StagingRecordCount +
                                      RecoveryLogCount +
                                      DeadlineTrackingCount +
                                      RecoveryJobCount +
                                      OverageJustificationCount +
                                      TelecomRecordCount +
                                      legacyCallLogCount +
                                      ReconciliationCount +
                                      await _context.CallLogVerifications.CountAsync() +
                                      await _context.CallLogPaymentAssignments.CountAsync() +
                                      await _context.CallLogDocuments.CountAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading statistics");
                // Set to 0 if there's an error
                CallRecordCount = 0;
                StagingRecordCount = 0;
                RecoveryLogCount = 0;
                DeadlineTrackingCount = 0;
                RecoveryJobCount = 0;
                OverageJustificationCount = 0;
                TelecomRecordCount = 0;
                ReconciliationCount = 0;
                TotalRecordsToDelete = 0;
            }
        }
    }
}
