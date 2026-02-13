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
    [Authorize(Roles = "Admin")]
    public class EOSProcessingModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<EOSProcessingModel> _logger;
        private readonly ICallLogStagingService _stagingService;
        private readonly IBackgroundJobClient _backgroundJobs;
        private readonly IEnhancedEmailService _emailService;

        public EOSProcessingModel(
            ApplicationDbContext context,
            ILogger<EOSProcessingModel> logger,
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
        public string StaffIndexNumber { get; set; } = string.Empty;

        [BindProperty]
        public string StaffName { get; set; } = string.Empty;

        [BindProperty]
        public string SeparationDate { get; set; } = string.Empty;

        [BindProperty]
        public string SeparationReason { get; set; } = string.Empty;

        [BindProperty]
        public int BillingMonth { get; set; } = DateTime.Now.Month;

        [BindProperty]
        public int BillingYear { get; set; } = DateTime.Now.Year;

        [BindProperty]
        public int NumberOfDays { get; set; } = 7;

        // Display properties
        public List<BillingProcessingHistory> ProcessingHistory { get; set; } = new();
        public StagingBatch? CurrentProcessingBatch { get; set; }

        // Status messages
        [TempData]
        public string? StatusMessage { get; set; }

        [TempData]
        public string? StatusMessageClass { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            await LoadProcessingHistoryAsync();
            await CheckCurrentProcessingBatchAsync();
            return Page();
        }

        public async Task<IActionResult> OnGetLookupStaffAsync(string indexNumber)
        {
            try
            {
                var staff = await _context.EbillUsers
                    .Include(e => e.OrganizationEntity)
                    .Where(e => e.IndexNumber == indexNumber)
                    .Select(e => new
                    {
                        found = true,
                        name = e.FullName,
                        organization = e.OrganizationEntity != null ? e.OrganizationEntity.Name : "Unknown",
                        isActive = e.IsActive
                    })
                    .FirstOrDefaultAsync();

                if (staff == null)
                {
                    return new JsonResult(new { found = false });
                }

                // Count active phones
                var phoneCount = await _context.UserPhones
                    .CountAsync(up => up.IndexNumber == indexNumber && up.IsActive);

                return new JsonResult(new
                {
                    staff.found,
                    staff.name,
                    staff.organization,
                    staff.isActive,
                    phoneCount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error looking up staff member");
                return new JsonResult(new { found = false, error = ex.Message });
            }
        }

        public async Task<IActionResult> OnGetPreviewAsync(string indexNumber, int month, int year)
        {
            try
            {
                // Check exchange rate
                var exchangeRate = await _context.ExchangeRates
                    .FirstOrDefaultAsync(e => e.Month == month && e.Year == year);

                // Get staff's active phones
                var userPhones = await _context.UserPhones
                    .Where(up => up.IndexNumber == indexNumber && up.IsActive)
                    .ToListAsync();

                var phoneNumbers = userPhones.Select(up => up.PhoneNumber).ToList();

                if (!phoneNumbers.Any())
                {
                    return new JsonResult(new
                    {
                        success = true,
                        hasExchangeRate = exchangeRate != null,
                        exchangeRate = exchangeRate?.Rate,
                        phoneNumbers = new List<string>(),
                        noPhones = true,
                        safaricomCount = 0, safaricomCostKES = 0m, safaricomCostUSD = 0m,
                        airtelCount = 0, airtelCostKES = 0m, airtelCostUSD = 0m,
                        pstnCount = 0, pstnCostKES = 0m, pstnCostUSD = 0m,
                        privateWireCount = 0, privateWireCostKES = 0m, privateWireCostUSD = 0m,
                        totalCount = 0, totalCostKES = 0m, totalCostUSD = 0m
                    });
                }

                // Query each provider filtered by staff's phone numbers + month/year + Staged/Failed + no batch
                var safaricomData = await _context.Safaricoms
                    .Where(s => phoneNumbers.Contains(s.Ext ?? "") &&
                               s.CallMonth == month && s.CallYear == year &&
                               (s.ProcessingStatus == ProcessingStatus.Staged || s.ProcessingStatus == ProcessingStatus.Failed) &&
                               s.StagingBatchId == null)
                    .GroupBy(s => 1)
                    .Select(g => new { Count = g.Count(), TotalKES = g.Sum(s => s.Cost ?? 0), TotalUSD = g.Sum(s => s.AmountUSD ?? 0) })
                    .FirstOrDefaultAsync();

                var airtelData = await _context.Airtels
                    .Where(a => phoneNumbers.Contains(a.Ext ?? "") &&
                               a.CallMonth == month && a.CallYear == year &&
                               (a.ProcessingStatus == ProcessingStatus.Staged || a.ProcessingStatus == ProcessingStatus.Failed) &&
                               a.StagingBatchId == null)
                    .GroupBy(a => 1)
                    .Select(g => new { Count = g.Count(), TotalKES = g.Sum(a => a.Cost ?? 0), TotalUSD = g.Sum(a => a.AmountUSD ?? 0) })
                    .FirstOrDefaultAsync();

                var pstnData = await _context.PSTNs
                    .Where(p => phoneNumbers.Contains(p.Extension ?? "") &&
                               p.CallMonth == month && p.CallYear == year &&
                               (p.ProcessingStatus == ProcessingStatus.Staged || p.ProcessingStatus == ProcessingStatus.Failed) &&
                               p.StagingBatchId == null)
                    .GroupBy(p => 1)
                    .Select(g => new { Count = g.Count(), TotalKES = g.Sum(p => p.AmountKSH ?? 0), TotalUSD = g.Sum(p => p.AmountUSD ?? 0) })
                    .FirstOrDefaultAsync();

                var privateWireData = await _context.PrivateWires
                    .Where(p => phoneNumbers.Contains(p.Extension ?? "") &&
                               p.CallMonth == month && p.CallYear == year &&
                               (p.ProcessingStatus == ProcessingStatus.Staged || p.ProcessingStatus == ProcessingStatus.Failed) &&
                               p.StagingBatchId == null)
                    .GroupBy(p => 1)
                    .Select(g => new { Count = g.Count(), TotalKES = g.Sum(p => p.AmountKSH ?? 0), TotalUSD = g.Sum(p => p.AmountUSD ?? 0) })
                    .FirstOrDefaultAsync();

                var safaricomCount = safaricomData?.Count ?? 0;
                var airtelCount = airtelData?.Count ?? 0;
                var pstnCount = pstnData?.Count ?? 0;
                var privateWireCount = privateWireData?.Count ?? 0;
                var totalCount = safaricomCount + airtelCount + pstnCount + privateWireCount;

                var safaricomCostKES = safaricomData?.TotalKES ?? 0;
                var airtelCostKES = airtelData?.TotalKES ?? 0;
                var pstnCostKES = pstnData?.TotalKES ?? 0;
                var privateWireCostKES = privateWireData?.TotalKES ?? 0;

                var safaricomCostUSD = safaricomData?.TotalUSD ?? 0;
                var airtelCostUSD = airtelData?.TotalUSD ?? 0;
                var pstnCostUSD = pstnData?.TotalUSD ?? 0;
                var privateWireCostUSD = privateWireData?.TotalUSD ?? 0;

                // Calculate deadlines
                var baseDate = DateTime.UtcNow;
                var staffDeadline = baseDate.AddDays(NumberOfDays > 0 ? NumberOfDays : 7);
                var supervisorDeadline = staffDeadline.AddDays(3);

                // Check if any EOS batch is currently processing for this staff
                var processingBatch = await _context.StagingBatches
                    .FirstOrDefaultAsync(b => b.BatchStatus == BatchStatus.Processing &&
                                             b.BatchType == "EOSProcessing");

                return new JsonResult(new
                {
                    success = true,
                    hasExchangeRate = exchangeRate != null,
                    exchangeRate = exchangeRate?.Rate,
                    phoneNumbers = phoneNumbers,
                    noPhones = false,
                    safaricomCount,
                    safaricomCostKES,
                    safaricomCostUSD,
                    airtelCount,
                    airtelCostKES,
                    airtelCostUSD,
                    pstnCount,
                    pstnCostKES,
                    pstnCostUSD,
                    privateWireCount,
                    privateWireCostKES,
                    privateWireCostUSD,
                    totalCount,
                    staffDeadline = staffDeadline.ToString("MMM dd, yyyy HH:mm"),
                    supervisorDeadline = supervisorDeadline.ToString("MMM dd, yyyy HH:mm"),
                    isProcessing = processingBatch != null,
                    processingBatchName = processingBatch?.BatchName
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading EOS preview");
                return new JsonResult(new { success = false, message = ex.Message });
            }
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

                // Check if any EOS batch is currently processing
                var processingBatch = await _context.StagingBatches
                    .FirstOrDefaultAsync(b => b.BatchStatus == BatchStatus.Processing &&
                                             b.BatchType == "EOSProcessing");

                if (processingBatch != null)
                {
                    return new JsonResult(new {
                        success = false,
                        message = $"An EOS batch is currently being processed. Batch '{processingBatch.BatchName}' is still running. Please wait for it to complete."
                    });
                }

                // Parse separation date for batch name
                var sepDateDisplay = DateTime.TryParse(SeparationDate, out var sepDate)
                    ? sepDate.ToString("yyyy-MM-dd")
                    : SeparationDate;

                var batch = new StagingBatch
                {
                    Id = Guid.NewGuid(),
                    BatchName = $"EOS - {StaffName} ({StaffIndexNumber}) - {sepDateDisplay}",
                    BatchType = "EOSProcessing",
                    BatchCategory = "INTERIM",
                    CreatedBy = userName,
                    CreatedDate = DateTime.UtcNow,
                    BatchStatus = BatchStatus.Created,
                    SourceSystems = "Safaricom,Airtel,PSTN,PrivateWire",
                    CurrentOperation = "Initializing...",
                    ProcessingProgress = 0,
                    Notes = $"Staff Separation: {SeparationReason}. Index: {StaffIndexNumber}. Billing Period: {BillingMonth}/{BillingYear}"
                };

                _context.StagingBatches.Add(batch);
                await _context.SaveChangesAsync();

                // Start background job
                var jobId = _backgroundJobs.Enqueue<EOSProcessingModel>(
                    x => x.ProcessEOSInBackgroundAsync(
                        batch.Id,
                        StaffIndexNumber,
                        StaffName,
                        SeparationDate,
                        SeparationReason,
                        BillingMonth,
                        BillingYear,
                        NumberOfDays,
                        userName));

                // Store job ID
                batch.HangfireJobId = jobId;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created EOS batch {BatchId} for staff {StaffIndex}", batch.Id, StaffIndexNumber);

                return new JsonResult(new {
                    success = true,
                    batchId = batch.Id,
                    jobId = jobId,
                    message = "EOS processing started successfully."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting EOS processing");
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
            var fileName = $"EOS_Anomalies_{batchId:N}.csv";

            return File(bytes, "text/csv", fileName);
        }

        [AutomaticRetry(Attempts = 0)]
        public async Task ProcessEOSInBackgroundAsync(Guid batchId, string staffIndexNumber, string staffName,
            string separationDate, string separationReason, int month, int year, int numberOfDays, string userName)
        {
            var batch = await _context.StagingBatches.FindAsync(batchId);
            if (batch == null)
            {
                _logger.LogError("EOS Batch {BatchId} not found for processing", batchId);
                return;
            }

            try
            {
                // Step 1: Consolidate staff's records from provider tables
                batch.BatchStatus = BatchStatus.Processing;
                batch.CurrentOperation = "Consolidating records for staff member...";
                batch.ProcessingProgress = 10;
                batch.StartProcessingDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                // Get exchange rate
                var exchangeRate = await _context.ExchangeRates
                    .FirstOrDefaultAsync(e => e.Month == month && e.Year == year);
                var rate = exchangeRate?.Rate ?? 125m;

                // Get staff member
                var staffMember = await _context.EbillUsers
                    .FirstOrDefaultAsync(e => e.IndexNumber == staffIndexNumber);

                // Get active UserPhones for this staff
                var userPhones = await _context.UserPhones
                    .Where(up => up.IndexNumber == staffIndexNumber && up.IsActive)
                    .ToListAsync();

                var phoneNumbers = userPhones.Select(up => up.PhoneNumber).ToList();
                var recordsImported = 0;

                if (phoneNumbers.Any())
                {
                    // Import from Safaricom
                    batch.CurrentOperation = "Consolidating Safaricom records...";
                    batch.ProcessingProgress = 15;
                    await _context.SaveChangesAsync();

                    var safaricomRecords = await _context.Safaricoms
                        .Where(s => phoneNumbers.Contains(s.Ext ?? "") &&
                                   s.CallMonth == month && s.CallYear == year &&
                                   (s.ProcessingStatus == ProcessingStatus.Staged || s.ProcessingStatus == ProcessingStatus.Failed) &&
                                   s.StagingBatchId == null)
                        .ToListAsync();

                    foreach (var record in safaricomRecords)
                    {
                        var userPhone = userPhones.FirstOrDefault(up => up.PhoneNumber == record.Ext);
                        var stagingRecord = new CallLogStaging
                        {
                            BatchId = batch.Id,
                            ImportType = "INTERIM",
                            ExtensionNumber = record.Ext ?? "",
                            CallDate = record.CallDate ?? DateTime.MinValue,
                            CallNumber = record.Dialed ?? "",
                            CallDestination = record.Dest ?? "",
                            CallEndTime = record.CallDate ?? DateTime.MinValue,
                            CallDuration = (int)(record.Dur ?? 0) * 60,
                            CallCurrencyCode = "KES",
                            CallCost = record.Cost ?? 0,
                            CallCostUSD = (record.Cost ?? 0) / rate,
                            CallCostKSHS = record.Cost ?? 0,
                            CallMonth = record.CallMonth ?? month,
                            CallYear = record.CallYear ?? year,
                            ResponsibleIndexNumber = staffIndexNumber,
                            ResponsibleUser = staffMember,
                            UserPhoneId = userPhone?.Id,
                            ImportedBy = userName,
                            ImportedDate = DateTime.UtcNow,
                            SourceSystem = "Safaricom",
                            SourceRecordId = record.Id.ToString(),
                            VerificationStatus = VerificationStatus.Pending,
                            CreatedDate = DateTime.UtcNow
                        };
                        _context.CallLogStagings.Add(stagingRecord);
                        record.ProcessingStatus = ProcessingStatus.Processing;
                        record.StagingBatchId = batch.Id;
                        recordsImported++;
                    }

                    // Import from Airtel
                    batch.CurrentOperation = "Consolidating Airtel records...";
                    batch.ProcessingProgress = 25;
                    await _context.SaveChangesAsync();

                    var airtelRecords = await _context.Airtels
                        .Where(a => phoneNumbers.Contains(a.Ext ?? "") &&
                                   a.CallMonth == month && a.CallYear == year &&
                                   (a.ProcessingStatus == ProcessingStatus.Staged || a.ProcessingStatus == ProcessingStatus.Failed) &&
                                   a.StagingBatchId == null)
                        .ToListAsync();

                    foreach (var record in airtelRecords)
                    {
                        var userPhone = userPhones.FirstOrDefault(up => up.PhoneNumber == record.Ext);
                        var stagingRecord = new CallLogStaging
                        {
                            BatchId = batch.Id,
                            ImportType = "INTERIM",
                            ExtensionNumber = record.Ext ?? "",
                            CallDate = record.CallDate ?? DateTime.MinValue,
                            CallNumber = record.Dialed ?? "",
                            CallDestination = record.Dest ?? "",
                            CallEndTime = record.CallDate ?? DateTime.MinValue,
                            CallDuration = (int)(record.Dur ?? 0) * 60,
                            CallCurrencyCode = "KES",
                            CallCost = record.Cost ?? 0,
                            CallCostUSD = (record.Cost ?? 0) / rate,
                            CallCostKSHS = record.Cost ?? 0,
                            CallMonth = record.CallMonth ?? month,
                            CallYear = record.CallYear ?? year,
                            ResponsibleIndexNumber = staffIndexNumber,
                            ResponsibleUser = staffMember,
                            UserPhoneId = userPhone?.Id,
                            ImportedBy = userName,
                            ImportedDate = DateTime.UtcNow,
                            SourceSystem = "Airtel",
                            SourceRecordId = record.Id.ToString(),
                            VerificationStatus = VerificationStatus.Pending,
                            CreatedDate = DateTime.UtcNow
                        };
                        _context.CallLogStagings.Add(stagingRecord);
                        record.ProcessingStatus = ProcessingStatus.Processing;
                        record.StagingBatchId = batch.Id;
                        recordsImported++;
                    }

                    // Import from PSTN
                    batch.CurrentOperation = "Consolidating PSTN records...";
                    batch.ProcessingProgress = 35;
                    await _context.SaveChangesAsync();

                    var pstnRecords = await _context.PSTNs
                        .Where(p => phoneNumbers.Contains(p.Extension ?? "") &&
                                   p.CallMonth == month && p.CallYear == year &&
                                   (p.ProcessingStatus == ProcessingStatus.Staged || p.ProcessingStatus == ProcessingStatus.Failed) &&
                                   p.StagingBatchId == null)
                        .ToListAsync();

                    foreach (var record in pstnRecords)
                    {
                        var userPhone = userPhones.FirstOrDefault(up => up.PhoneNumber == record.Extension);
                        var stagingRecord = new CallLogStaging
                        {
                            BatchId = batch.Id,
                            ImportType = "INTERIM",
                            ExtensionNumber = record.Extension ?? "",
                            CallDate = record.CallDate ?? DateTime.MinValue,
                            CallNumber = record.DialedNumber ?? "",
                            CallDestination = record.Destination ?? "",
                            CallEndTime = record.CallDate ?? DateTime.MinValue,
                            CallDuration = (int)(record.Duration ?? 0),
                            CallCurrencyCode = "KES",
                            CallCost = record.TotalCost,
                            CallCostUSD = record.TotalCost / rate,
                            CallCostKSHS = record.AmountKSH ?? 0,
                            CallMonth = record.CallMonth > 0 ? record.CallMonth : month,
                            CallYear = record.CallYear > 0 ? record.CallYear : year,
                            ResponsibleIndexNumber = staffIndexNumber,
                            ResponsibleUser = staffMember,
                            UserPhoneId = userPhone?.Id,
                            ImportedBy = userName,
                            ImportedDate = DateTime.UtcNow,
                            SourceSystem = "PSTN",
                            SourceRecordId = record.Id.ToString(),
                            VerificationStatus = VerificationStatus.Pending,
                            CreatedDate = DateTime.UtcNow
                        };
                        _context.CallLogStagings.Add(stagingRecord);
                        record.ProcessingStatus = ProcessingStatus.Processing;
                        record.StagingBatchId = batch.Id;
                        recordsImported++;
                    }

                    // Import from PrivateWire
                    batch.CurrentOperation = "Consolidating Private Wire records...";
                    batch.ProcessingProgress = 45;
                    await _context.SaveChangesAsync();

                    var privateWireRecords = await _context.PrivateWires
                        .Where(pw => phoneNumbers.Contains(pw.Extension ?? "") &&
                                    pw.CallMonth == month && pw.CallYear == year &&
                                    (pw.ProcessingStatus == ProcessingStatus.Staged || pw.ProcessingStatus == ProcessingStatus.Failed) &&
                                    pw.StagingBatchId == null)
                        .ToListAsync();

                    foreach (var record in privateWireRecords)
                    {
                        var userPhone = userPhones.FirstOrDefault(up => up.PhoneNumber == record.Extension);
                        var stagingRecord = new CallLogStaging
                        {
                            BatchId = batch.Id,
                            ImportType = "INTERIM",
                            ExtensionNumber = record.Extension ?? "",
                            CallDate = record.CallDate ?? DateTime.MinValue,
                            CallNumber = record.DialedNumber ?? "",
                            CallDestination = record.Destination ?? "",
                            CallEndTime = record.CallDate ?? DateTime.MinValue,
                            CallDuration = (int)(record.Duration ?? 0),
                            CallCurrencyCode = "USD",
                            CallCost = record.TotalCostKSH,
                            CallCostUSD = record.TotalCostUSD,
                            CallCostKSHS = record.AmountKSH ?? 0,
                            CallMonth = record.CallMonth > 0 ? record.CallMonth : month,
                            CallYear = record.CallYear > 0 ? record.CallYear : year,
                            ResponsibleIndexNumber = staffIndexNumber,
                            ResponsibleUser = staffMember,
                            UserPhoneId = userPhone?.Id,
                            ImportedBy = userName,
                            ImportedDate = DateTime.UtcNow,
                            SourceSystem = "PrivateWire",
                            SourceRecordId = record.Id.ToString(),
                            VerificationStatus = VerificationStatus.Pending,
                            CreatedDate = DateTime.UtcNow
                        };
                        _context.CallLogStagings.Add(stagingRecord);
                        record.ProcessingStatus = ProcessingStatus.Processing;
                        record.StagingBatchId = batch.Id;
                        recordsImported++;
                    }

                    // Save all consolidated records
                    batch.TotalRecords = recordsImported;
                    batch.PendingRecords = recordsImported;
                    batch.ProcessingProgress = 50;
                    batch.CurrentOperation = $"Consolidated {recordsImported} records. Running anomaly detection...";
                    await _context.SaveChangesAsync();
                }

                // Step 2: Anomaly detection
                batch.CurrentOperation = "Detecting anomalies...";
                batch.ProcessingProgress = 60;
                await _context.SaveChangesAsync();

                await _stagingService.DetectBatchAnomaliesFastAsync(batchId);

                // Reload batch to get updated stats
                await _context.Entry(batch).ReloadAsync();

                // Step 3: Auto-verify clean records
                batch.CurrentOperation = "Auto-verifying clean records...";
                batch.ProcessingProgress = 75;
                await _context.SaveChangesAsync();

                await _stagingService.AutoVerifyCleanRecordsAsync(batchId, userName);

                // Reload batch
                await _context.Entry(batch).ReloadAsync();

                // Step 4: Push verified records to production
                batch.CurrentOperation = "Pushing verified records to production...";
                batch.ProcessingProgress = 85;
                await _context.SaveChangesAsync();

                var verificationPeriod = DateTime.UtcNow.AddDays(numberOfDays);
                await _stagingService.PushToProductionInBackgroundAsync(batchId, verificationPeriod, "Official", userName, sendNotifications: false);

                // Reload batch
                await _context.Entry(batch).ReloadAsync();

                // Step 5: Complete
                var anomalyCount = await _context.CallLogStagings
                    .Where(c => c.BatchId == batchId && c.HasAnomalies)
                    .CountAsync();

                batch.BatchStatus = BatchStatus.Published;
                batch.CurrentOperation = "Processing complete";
                batch.ProcessingProgress = 100;
                batch.EndProcessingDate = DateTime.UtcNow;
                batch.Notes = $"EOS for {staffName} ({staffIndexNumber}). {separationReason}. Processed {batch.TotalRecords:N0} records. {batch.VerifiedRecords:N0} pushed to production. {anomalyCount:N0} anomalies.";
                await _context.SaveChangesAsync();

                // Send completion email
                await SendCompletionEmailAsync(batch, userName, anomalyCount, staffName, staffIndexNumber, separationReason);

                _logger.LogInformation("EOS processing completed for batch {BatchId}, staff {StaffIndex}", batchId, staffIndexNumber);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during EOS processing for batch {BatchId}", batchId);

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

                var verificationPeriod = batch.EndProcessingDate?.AddDays(7) ?? DateTime.UtcNow.AddDays(7);

                var jobId = _backgroundJobs.Enqueue<ICallLogStagingService>(
                    service => service.SendBatchNotificationsAsync(batchId, verificationPeriod));

                _logger.LogInformation("Queued notification job {JobId} for EOS batch {BatchId}", jobId, batchId);

                return new JsonResult(new {
                    success = true,
                    message = "Notifications queued successfully. Emails will be sent in the background.",
                    jobId = jobId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending notifications for EOS batch {BatchId}", batchId);
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

                var userName = User.Identity?.Name ?? "System";

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

                batch.RecordsWithAnomalies = 0;
                batch.VerifiedRecords = await _context.CallLogStagings
                    .Where(c => c.BatchId == batchId && c.VerificationStatus == VerificationStatus.Verified)
                    .CountAsync();

                var pendingCount = await _context.CallLogStagings
                    .Where(c => c.BatchId == batchId && c.VerificationStatus == VerificationStatus.Pending)
                    .CountAsync();

                if (pendingCount == 0)
                {
                    batch.BatchStatus = BatchStatus.Published;
                }

                await _context.SaveChangesAsync();

                return new JsonResult(new { success = true, dismissedCount = dismissedCount });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error dismissing anomalies for EOS batch {BatchId}", batchId);
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

                var deletedCount = await _context.Database.ExecuteSqlInterpolatedAsync(
                    $"DELETE FROM CallLogStagings WHERE BatchId = {batchId} AND HasAnomalies = 1");

                batch.RecordsWithAnomalies = 0;
                batch.TotalRecords = await _context.CallLogStagings
                    .Where(c => c.BatchId == batchId)
                    .CountAsync();
                batch.VerifiedRecords = await _context.CallLogStagings
                    .Where(c => c.BatchId == batchId && c.VerificationStatus == VerificationStatus.Verified)
                    .CountAsync();

                var pendingCount = await _context.CallLogStagings
                    .Where(c => c.BatchId == batchId && c.VerificationStatus == VerificationStatus.Pending)
                    .CountAsync();

                if (pendingCount == 0 && batch.TotalRecords > 0)
                {
                    batch.BatchStatus = BatchStatus.Published;
                }

                await _context.SaveChangesAsync();

                return new JsonResult(new { success = true, deletedCount = deletedCount });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting anomalies for EOS batch {BatchId}", batchId);
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

                if (batch.BatchStatus != BatchStatus.Published)
                {
                    return new JsonResult(new { success = false, message = "Can only cleanup staging data for published batches" });
                }

                var deletedCount = await _context.Database.ExecuteSqlInterpolatedAsync(
                    $"DELETE FROM CallLogStagings WHERE BatchId = {batchId}");

                batch.TotalRecords = 0;
                batch.VerifiedRecords = 0;
                batch.PendingRecords = 0;
                batch.RecordsWithAnomalies = 0;
                await _context.SaveChangesAsync();

                return new JsonResult(new { success = true, deletedCount = deletedCount });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up staging data for EOS batch {BatchId}", batchId);
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

                var originalCount = await _context.CallLogStagings
                    .Where(c => c.BatchId == batchId && c.HasAnomalies)
                    .CountAsync();

                var originalTimeout = _context.Database.GetCommandTimeout();
                _context.Database.SetCommandTimeout(300);

                try
                {
                    var updatePhoneExactSql = @"
                        UPDATE cls
                        SET cls.UserPhoneId = up.Id,
                            cls.ResponsibleIndexNumber = ISNULL(cls.ResponsibleIndexNumber, up.IndexNumber)
                        FROM CallLogStagings cls
                        INNER JOIN UserPhones up ON up.PhoneNumber = cls.ExtensionNumber AND up.IsActive = 1
                        WHERE cls.BatchId = @BatchId
                          AND cls.UserPhoneId IS NULL";

                    await _context.Database.ExecuteSqlRawAsync(updatePhoneExactSql, new SqlParameter("@BatchId", batchId));

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
                    _context.Database.SetCommandTimeout(originalTimeout);
                }

                await _stagingService.DetectBatchAnomaliesFastAsync(batchId);

                var userName = User.Identity?.Name ?? "System";
                var verifiedCount = await _stagingService.AutoVerifyCleanRecordsAsync(batchId, userName);

                if (verifiedCount > 0)
                {
                    var verificationPeriod = DateTime.UtcNow.AddDays(7);
                    await _stagingService.PushToProductionInBackgroundAsync(batchId, verificationPeriod, "Official", userName, sendNotifications: false);
                }

                var remainingCount = await _context.CallLogStagings
                    .Where(c => c.BatchId == batchId && c.HasAnomalies)
                    .CountAsync();

                var resolvedCount = originalCount - remainingCount;

                batch.RecordsWithAnomalies = remainingCount;
                batch.VerifiedRecords = await _context.CallLogStagings
                    .Where(c => c.BatchId == batchId && c.VerificationStatus == VerificationStatus.Verified)
                    .CountAsync();
                await _context.SaveChangesAsync();

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
                _logger.LogError(ex, "Error reprocessing anomalies for EOS batch {BatchId}", batchId);
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

                var correctionList = JsonSerializer.Deserialize<List<Dictionary<string, string>>>(corrections);
                if (correctionList == null || !correctionList.Any())
                {
                    return new JsonResult(new { success = false, message = "No corrections data provided" });
                }

                var updatedCount = 0;
                var userName = User.Identity?.Name ?? "System";

                foreach (var correction in correctionList)
                {
                    if (!correction.TryGetValue("Extension", out var extension) ||
                        !correction.TryGetValue("Call Date", out var callDateStr) ||
                        !correction.TryGetValue("Provider", out var provider))
                    {
                        continue;
                    }

                    correction.TryGetValue("Index Number", out var indexNumber);
                    if (string.IsNullOrWhiteSpace(indexNumber))
                    {
                        continue;
                    }

                    if (!DateTime.TryParse(callDateStr, out var callDate))
                    {
                        continue;
                    }

                    var stagingRecord = await _context.CallLogStagings
                        .FirstOrDefaultAsync(c =>
                            c.BatchId == batchId &&
                            c.ExtensionNumber == extension &&
                            c.SourceSystem == provider &&
                            c.CallDate.Date == callDate.Date &&
                            c.HasAnomalies);

                    if (stagingRecord != null)
                    {
                        stagingRecord.ResponsibleIndexNumber = indexNumber.Trim();
                        stagingRecord.ModifiedDate = DateTime.UtcNow;
                        stagingRecord.ModifiedBy = userName;

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

                await _stagingService.DetectBatchAnomaliesFastAsync(batchId);

                var verifiedCount = await _stagingService.AutoVerifyCleanRecordsAsync(batchId, userName);

                if (verifiedCount > 0)
                {
                    var verificationPeriod = DateTime.UtcNow.AddDays(7);
                    await _stagingService.PushToProductionInBackgroundAsync(batchId, verificationPeriod, "Official", userName, sendNotifications: false);
                }

                var remainingCount = await _context.CallLogStagings
                    .Where(c => c.BatchId == batchId && c.HasAnomalies)
                    .CountAsync();

                batch.RecordsWithAnomalies = remainingCount;
                batch.VerifiedRecords = await _context.CallLogStagings
                    .Where(c => c.BatchId == batchId && c.VerificationStatus == VerificationStatus.Verified)
                    .CountAsync();
                await _context.SaveChangesAsync();

                return new JsonResult(new {
                    success = true,
                    updatedCount = updatedCount,
                    resolvedCount = verifiedCount,
                    remainingCount = remainingCount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading corrections for EOS batch {BatchId}", batchId);
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }

        private async Task SendCompletionEmailAsync(StagingBatch batch, string userName, int anomalyCount,
            string staffName, string staffIndexNumber, string separationReason)
        {
            try
            {
                var subject = $"EOS Processing Complete - {staffName} ({staffIndexNumber})";
                var body = $@"
                    <h2>EOS Processing Complete</h2>
                    <p>The End of Service processing for <strong>{staffName} ({staffIndexNumber})</strong> has been completed.</p>

                    <h3>Staff Details:</h3>
                    <ul>
                        <li>Name: {staffName}</li>
                        <li>Index Number: {staffIndexNumber}</li>
                        <li>Separation Reason: {separationReason}</li>
                    </ul>

                    <h3>Processing Summary:</h3>
                    <ul>
                        <li>Total Records: {batch.TotalRecords:N0}</li>
                        <li>Verified & Pushed to Production: {batch.VerifiedRecords:N0}</li>
                        <li>Anomalies: {anomalyCount:N0}</li>
                    </ul>

                    <p>Processing completed at: {batch.EndProcessingDate:yyyy-MM-dd HH:mm:ss} UTC</p>
                    <p>Processed by: {userName}</p>

                    {(anomalyCount > 0 ? "<p><strong>Note:</strong> Please download and review the anomaly report from the EOS Processing page.</p>" : "")}
                ";

                _logger.LogInformation("EOS processing email would be sent: {Subject}", subject);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending EOS completion email for batch {BatchId}", batch.Id);
            }
        }

        private async Task LoadProcessingHistoryAsync()
        {
            var batches = await _context.StagingBatches
                .Where(b => b.BatchType == "EOSProcessing")
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
                .FirstOrDefaultAsync(b => b.BatchStatus == BatchStatus.Processing &&
                                         b.BatchType == "EOSProcessing");
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
}
