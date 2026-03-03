using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TAB.Web.Data;
using TAB.Web.Models;
using TAB.Web.Services;
using System.Text.Json;
using Hangfire;

namespace TAB.Web.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class CallLogsUploadModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IBackgroundJobClient _backgroundJobs;
        private readonly ILogger<CallLogsUploadModel> _logger;

        public CallLogsUploadModel(
            ApplicationDbContext context,
            IBackgroundJobClient backgroundJobs,
            ILogger<CallLogsUploadModel> logger)
        {
            _context = context;
            _backgroundJobs = backgroundJobs;
            _logger = logger;
        }

        [TempData]
        public string? StatusMessage { get; set; }

        [TempData]
        public string? StatusMessageClass { get; set; }

        public string? LastUsedDateFormat { get; set; }
        public string? LastUsedCallLogType { get; set; }


        public async Task OnGetAsync()
        {
            // Load last used preferences for this user
            var lastImport = await _context.ImportAudits
                .Where(a => a.ImportedBy == User.Identity!.Name && a.ImportType == "CallLogs")
                .OrderByDescending(a => a.ImportDate)
                .FirstOrDefaultAsync();

            if (lastImport != null && !string.IsNullOrEmpty(lastImport.DateFormatPreferences))
            {
                try
                {
                    var preferences = JsonSerializer.Deserialize<Dictionary<string, string>>(lastImport.DateFormatPreferences);
                    if (preferences != null)
                    {
                        LastUsedDateFormat = preferences.ContainsKey("dateFormat") ? preferences["dateFormat"] : null;
                        LastUsedCallLogType = preferences.ContainsKey("callLogType") ? preferences["callLogType"] : null;
                    }
                }
                catch
                {
                    // Ignore JSON parsing errors
                }
            }
        }

        /// <summary>
        /// Enterprise-level bulk import handler - uses Hangfire background jobs and SqlBulkCopy
        /// Supports files up to 500MB and 1M+ records
        /// </summary>
        public async Task<IActionResult> OnPostEnterpriseImportAsync(
            IFormFile callLogFile,
            string callLogType,
            int? billingMonth,
            int? billingYear,
            string? dateFormat)
        {
            try
            {
                // Validate file
                if (callLogFile == null || callLogFile.Length == 0)
                {
                    StatusMessage = "Please select a file to upload";
                    StatusMessageClass = "danger";
                    return RedirectToPage();
                }

                // Validate file size (500MB max)
                if (callLogFile.Length > 500 * 1024 * 1024)
                {
                    StatusMessage = "File size exceeds 500MB limit";
                    StatusMessageClass = "danger";
                    return RedirectToPage();
                }

                // Validate CSV extension
                var extension = Path.GetExtension(callLogFile.FileName).ToLowerInvariant();
                if (extension != ".csv")
                {
                    StatusMessage = "Only CSV files are supported";
                    StatusMessageClass = "danger";
                    return RedirectToPage();
                }

                // Validate required parameters
                if (!billingMonth.HasValue || !billingYear.HasValue)
                {
                    StatusMessage = "Please select a valid billing month and year";
                    StatusMessageClass = "danger";
                    return RedirectToPage();
                }

                // Validate billing period
                if (billingMonth < 1 || billingMonth > 12 || billingYear < 2000)
                {
                    StatusMessage = "Please select a valid billing month and year";
                    StatusMessageClass = "danger";
                    return RedirectToPage();
                }

                // Use default date format if not provided
                var importDateFormat = string.IsNullOrEmpty(dateFormat) ? "dd/MM/yyyy" : dateFormat;

                // Save file to temp location
                var tempFileName = $"import_{Guid.NewGuid()}{extension}";
                var tempPath = Path.Combine(Path.GetTempPath(), tempFileName);

                _logger.LogInformation("Saving uploaded file to {TempPath}", tempPath);

                using (var stream = new FileStream(tempPath, FileMode.Create))
                {
                    await callLogFile.CopyToAsync(stream);
                }

                _logger.LogInformation("File saved successfully: {Size} bytes", callLogFile.Length);

                // Create import job record
                var jobId = Guid.NewGuid();
                var importJob = new ImportJob
                {
                    Id = jobId,
                    FileName = callLogFile.FileName,
                    FileSize = callLogFile.Length,
                    CallLogType = callLogType,
                    BillingMonth = billingMonth.Value,
                    BillingYear = billingYear.Value,
                    DateFormat = importDateFormat,
                    Status = "Queued",
                    CreatedBy = User.Identity?.Name ?? "Unknown",
                    CreatedDate = DateTime.UtcNow
                };

                _context.ImportJobs.Add(importJob);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created import job {JobId} for file {FileName}", jobId, callLogFile.FileName);

                // Queue Hangfire background job based on call log type
                string hangfireJobId;

                switch (callLogType.ToLower())
                {
                    case "safaricom":
                        hangfireJobId = _backgroundJobs.Enqueue<IBulkImportService>(
                            service => service.ImportSafaricomAsync(
                                jobId,
                                tempPath,
                                billingMonth.Value,
                                billingYear.Value,
                                importDateFormat,
                                JobCancellationToken.Null));
                        break;

                    case "airtel":
                        hangfireJobId = _backgroundJobs.Enqueue<IBulkImportService>(
                            service => service.ImportAirtelAsync(
                                jobId,
                                tempPath,
                                billingMonth.Value,
                                billingYear.Value,
                                importDateFormat,
                                JobCancellationToken.Null));
                        break;

                    case "pstn":
                        hangfireJobId = _backgroundJobs.Enqueue<IBulkImportService>(
                            service => service.ImportPSTNAsync(
                                jobId,
                                tempPath,
                                billingMonth.Value,
                                billingYear.Value,
                                importDateFormat,
                                JobCancellationToken.Null));
                        break;

                    case "privatewire":
                        hangfireJobId = _backgroundJobs.Enqueue<IBulkImportService>(
                            service => service.ImportPrivateWireAsync(
                                jobId,
                                tempPath,
                                billingMonth.Value,
                                billingYear.Value,
                                importDateFormat,
                                JobCancellationToken.Null));
                        break;

                    default:
                        StatusMessage = $"Unsupported call log type: {callLogType}";
                        StatusMessageClass = "danger";

                        // Clean up temp file and job record
                        try { System.IO.File.Delete(tempPath); } catch { }
                        _context.ImportJobs.Remove(importJob);
                        await _context.SaveChangesAsync();

                        return RedirectToPage();
                }

                // Update job with Hangfire ID
                importJob.HangfireJobId = hangfireJobId;
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Queued {CallLogType} import job {JobId} with Hangfire job ID {HangfireJobId}",
                    callLogType, jobId, hangfireJobId);

                StatusMessage = $"Import queued successfully! File: {callLogFile.FileName} ({callLogFile.Length / 1024 / 1024:N2} MB). " +
                                $"The import is running in the background. Monitor progress on the Import Jobs page or /hangfire dashboard.";
                StatusMessageClass = "success";

                return RedirectToPage("/Admin/CallLogs");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error queuing enterprise import");

                StatusMessage = $"Error queuing import: {ex.Message}";
                StatusMessageClass = "danger";

                return RedirectToPage();
            }
        }

        /// <summary>
        /// Get import job status (for AJAX polling)
        /// </summary>
        public async Task<JsonResult> OnGetImportJobStatusAsync(Guid jobId)
        {
            try
            {
                var job = await _context.ImportJobs.FindAsync(jobId);
                if (job == null)
                {
                    return new JsonResult(new { success = false, error = "Job not found" });
                }

                return new JsonResult(new
                {
                    success = true,
                    jobId = job.Id,
                    status = job.Status,
                    fileName = job.FileName,
                    fileSize = job.FileSize,
                    recordsProcessed = job.RecordsProcessed,
                    recordsSuccess = job.RecordsSuccess,
                    recordsError = job.RecordsError,
                    progressPercentage = job.ProgressPercentage,
                    createdDate = job.CreatedDate,
                    startedDate = job.StartedDate,
                    completedDate = job.CompletedDate,
                    durationSeconds = job.DurationSeconds,
                    errorMessage = job.ErrorMessage
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting import job status for {JobId}", jobId);
                return new JsonResult(new { success = false, error = ex.Message });
            }
        }
    }
}
