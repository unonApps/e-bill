using System.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Text;
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

            try
            {
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

                // Call the stored procedure (uses TRUNCATE TABLE — instant regardless of data volume)
                var conn = _context.Database.GetDbConnection();
                if (conn.State != ConnectionState.Open) await conn.OpenAsync();

                using var cmd = conn.CreateCommand();
                cmd.CommandText = "ebill.sp_ResetTestData";
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandTimeout = 120; // 2 minutes (TRUNCATE is nearly instant, this is generous)

                int totalDeleted = 0;
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var tableName = reader.GetString(0);
                    var count = reader.GetInt32(1);
                    totalDeleted += count;
                    log.AppendLine($"  Truncated {tableName}: {count:N0} records");
                }

                log.AppendLine();
                log.AppendLine("========================================");
                log.AppendLine("OPERATION COMPLETED SUCCESSFULLY");
                log.AppendLine("========================================");
                log.AppendLine($"Total Records Deleted: {totalDeleted:N0}");
                log.AppendLine($"Completed: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                log.AppendLine();
                log.AppendLine("Master data preserved:");
                log.AppendLine("  - Organizations & Offices");
                log.AppendLine("  - Users & EBill Users");
                log.AppendLine("  - User Phones");
                log.AppendLine("  - Class of Service");
                log.AppendLine("  - Service Providers");
                log.AppendLine("  - Exchange Rates");
                log.AppendLine("  - Recovery Configurations");
                log.AppendLine("  - Email Settings & Templates");
                log.AppendLine("  - Anomaly Types");
                log.AppendLine("  - Billing Periods");

                _logger.LogInformation("Test data reset completed by {User}. {Count} records deleted.", User.Identity?.Name, totalDeleted);

                Message = log.ToString();
                Success = true;
            }
            catch (Exception ex)
            {
                log.AppendLine();
                log.AppendLine("========================================");
                log.AppendLine("ERROR - OPERATION FAILED");
                log.AppendLine("========================================");
                log.AppendLine($"Error: {ex.Message}");
                log.AppendLine($"Stack Trace: {ex.StackTrace}");

                _logger.LogError(ex, "Test data reset failed for user {User}", User.Identity?.Name);

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
