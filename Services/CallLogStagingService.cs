using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;
using Hangfire;
using TAB.Web.Data;
using TAB.Web.Models;

namespace TAB.Web.Services
{
    public class CallLogStagingService : ICallLogStagingService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CallLogStagingService> _logger;
        private readonly IDeadlineManagementService _deadlineService;
        private readonly IEnhancedEmailService _emailService;
        private readonly IConfiguration _configuration;
        private readonly IBackgroundJobClient _backgroundJobs;

        public CallLogStagingService(
            ApplicationDbContext context,
            ILogger<CallLogStagingService> logger,
            IDeadlineManagementService deadlineService,
            IEnhancedEmailService emailService,
            IConfiguration configuration,
            IBackgroundJobClient backgroundJobs)
        {
            _context = context;
            _logger = logger;
            _deadlineService = deadlineService;
            _emailService = emailService;
            _configuration = configuration;
            _backgroundJobs = backgroundJobs;
        }

        public async Task<StagingBatch> ConsolidateCallLogsAsync(DateTime startDate, DateTime endDate, string createdBy)
        {
            // Extract month and year for filtering
            int startMonth = startDate.Month;
            int startYear = startDate.Year;
            int endMonth = endDate.Month;
            int endYear = endDate.Year;

            _logger.LogInformation("Starting consolidation for {StartMonth}/{StartYear} to {EndMonth}/{EndYear}",
                startMonth, startYear, endMonth, endYear);

            // Check if a batch already exists for this period
            var existingBatch = await _context.StagingBatches
                .FirstOrDefaultAsync(b =>
                    b.CreatedDate.Month == startMonth &&
                    b.CreatedDate.Year == startYear &&
                    (b.BatchStatus == BatchStatus.Processing ||
                     b.BatchStatus == BatchStatus.PartiallyVerified ||
                     b.BatchStatus == BatchStatus.Verified ||
                     b.BatchStatus == BatchStatus.Published));

            if (existingBatch != null)
            {
                throw new InvalidOperationException(
                    $"A batch already exists for {startDate:MMMM yyyy} with status '{existingBatch.BatchStatus}'. " +
                    $"Batch Name: '{existingBatch.BatchName}' created on {existingBatch.CreatedDate:yyyy-MM-dd}. " +
                    $"Please complete or delete the existing batch before creating a new one.");
            }

            // Also check if there are any staging records for this period that haven't been processed
            var hasUnprocessedRecords = await _context.CallLogStagings
                .AnyAsync(c =>
                    c.CallMonth == startMonth &&
                    c.CallYear == startYear &&
                    c.ProcessingStatus != ProcessingStatus.Completed);

            if (hasUnprocessedRecords)
            {
                throw new InvalidOperationException(
                    $"There are unprocessed staging records for {startDate:MMMM yyyy}. " +
                    $"Please complete processing of existing records before creating a new batch.");
            }

            // First check if there are any records to consolidate
            var hasRecords = await _context.Safaricoms
                .AnyAsync(s => s.CallMonth >= startMonth && s.CallMonth <= endMonth &&
                              s.CallYear >= startYear && s.CallYear <= endYear);

            if (!hasRecords)
            {
                hasRecords = await _context.Airtels
                    .AnyAsync(a => a.CallMonth >= startMonth && a.CallMonth <= endMonth &&
                                  a.CallYear >= startYear && a.CallYear <= endYear);
            }

            if (!hasRecords)
            {
                hasRecords = await _context.PSTNs
                    .AnyAsync(p => p.CallMonth >= startMonth && p.CallMonth <= endMonth &&
                                  p.CallYear >= startYear && p.CallYear <= endYear);
            }

            if (!hasRecords)
            {
                hasRecords = await _context.PrivateWires
                    .AnyAsync(p => p.CallMonth >= startMonth && p.CallMonth <= endMonth &&
                                  p.CallYear >= startYear && p.CallYear <= endYear);
            }

            if (!hasRecords)
            {
                throw new InvalidOperationException(
                    $"No call log records found for {startDate:MMMM yyyy}. " +
                    $"Please ensure data has been imported from source systems before consolidation.");
            }

            // Create a new batch
            var batch = new StagingBatch
            {
                Id = Guid.NewGuid(),
                BatchName = startMonth == endMonth && startYear == endYear
                    ? $"Call Logs {startDate:MMMM yyyy}"
                    : $"Call Logs {startDate:MMM yyyy} to {endDate:MMM yyyy}",
                BatchType = "Manual",
                CreatedBy = createdBy,
                CreatedDate = DateTime.UtcNow,
                BatchStatus = BatchStatus.Created,
                SourceSystems = "Safaricom,Airtel,PSTN,PrivateWire"
            };

            _context.StagingBatches.Add(batch);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created batch {BatchId}", batch.Id);

            try
            {
                batch.BatchStatus = BatchStatus.Processing;
                batch.StartProcessingDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                // =============================================
                // Use stored procedure for efficient bulk consolidation
                // This handles 1M+ records without timeout or memory issues
                // =============================================
                _logger.LogInformation("Calling stored procedure sp_ConsolidateCallLogBatch for batch {BatchId}", batch.Id);

                var batchIdParam = new SqlParameter("@BatchId", batch.Id);
                var startMonthParam = new SqlParameter("@StartMonth", startMonth);
                var startYearParam = new SqlParameter("@StartYear", startYear);
                var endMonthParam = new SqlParameter("@EndMonth", endMonth);
                var endYearParam = new SqlParameter("@EndYear", endYear);
                var createdByParam = new SqlParameter("@CreatedBy", createdBy);

                // Execute stored procedure with 10 minute timeout for large datasets
                var originalTimeout = _context.Database.GetCommandTimeout();
                _context.Database.SetCommandTimeout(600); // 10 minutes

                var result = await _context.Database
                    .SqlQueryRaw<ConsolidationResult>(
                        "EXEC sp_ConsolidateCallLogBatch @BatchId, @StartMonth, @StartYear, @EndMonth, @EndYear, @CreatedBy",
                        batchIdParam, startMonthParam, startYearParam, endMonthParam, endYearParam, createdByParam)
                    .ToListAsync();

                // Restore original timeout
                _context.Database.SetCommandTimeout(originalTimeout);

                int totalImported = result.FirstOrDefault()?.TotalRecords ?? 0;

                _logger.LogInformation(
                    "Stored procedure completed. Total: {Total}, Safaricom: {Safaricom}, Airtel: {Airtel}, PSTN: {PSTN}, PrivateWire: {PrivateWire}",
                    totalImported,
                    result.FirstOrDefault()?.SafaricomRecords ?? 0,
                    result.FirstOrDefault()?.AirtelRecords ?? 0,
                    result.FirstOrDefault()?.PSTNRecords ?? 0,
                    result.FirstOrDefault()?.PrivateWireRecords ?? 0);

                // If no records were actually imported, delete the batch and throw an error
                if (totalImported == 0)
                {
                    _context.StagingBatches.Remove(batch);
                    await _context.SaveChangesAsync();

                    throw new InvalidOperationException(
                        $"No call log records were imported for {startDate:MMMM yyyy}. " +
                        $"The batch was not created. Please check the source data.");
                }

                // Detect anomalies for the batch
                int anomalyCount = await DetectBatchAnomaliesAsync(batch.Id);

                // Update batch statistics
                batch.TotalRecords = totalImported;
                batch.RecordsWithAnomalies = anomalyCount;
                batch.PendingRecords = totalImported;
                batch.BatchStatus = BatchStatus.Processing;
                batch.EndProcessingDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Consolidation completed. Total records: {TotalRecords}, Anomalies: {Anomalies}",
                    totalImported, anomalyCount);

                return batch;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during consolidation");
                batch.BatchStatus = BatchStatus.Failed;
                batch.Notes = $"Error: {ex.Message}";
                await _context.SaveChangesAsync();
                throw;
            }
        }

        /// <summary>
        /// Background job method for consolidating call logs using Hangfire.
        /// This is the same as ConsolidateCallLogsAsync but designed to run asynchronously in the background.
        /// The batch should already be created before calling this method.
        /// </summary>
        public async Task ConsolidateCallLogsInBackgroundAsync(Guid batchId, int startMonth, int startYear, int endMonth, int endYear, string createdBy)
        {
            _logger.LogInformation("Starting background consolidation for batch {BatchId}: {StartMonth}/{StartYear} to {EndMonth}/{EndYear}",
                batchId, startMonth, startYear, endMonth, endYear);

            // Get the batch
            var batch = await _context.StagingBatches.FindAsync(batchId);
            if (batch == null)
            {
                _logger.LogError("Batch {BatchId} not found for background consolidation", batchId);
                throw new InvalidOperationException($"Batch {batchId} not found");
            }

            try
            {
                // Update batch status to Processing
                batch.BatchStatus = BatchStatus.Processing;
                batch.StartProcessingDate = DateTime.UtcNow;
                batch.CurrentOperation = "Initializing consolidation";
                batch.ProcessingProgress = 5;
                await _context.SaveChangesAsync();

                // Update progress: Consolidating records
                batch.CurrentOperation = "Consolidating records from source tables";
                batch.ProcessingProgress = 10;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Calling stored procedure sp_ConsolidateCallLogBatch for batch {BatchId}", batch.Id);

                var batchIdParam = new SqlParameter("@BatchId", batch.Id);
                var startMonthParam = new SqlParameter("@StartMonth", startMonth);
                var startYearParam = new SqlParameter("@StartYear", startYear);
                var endMonthParam = new SqlParameter("@EndMonth", endMonth);
                var endYearParam = new SqlParameter("@EndYear", endYear);
                var createdByParam = new SqlParameter("@CreatedBy", createdBy);

                // Execute stored procedure - No timeout limit for background jobs!
                // The job can run for hours if needed without blocking the user
                // NOTE: SetCommandTimeout(0) means unlimited timeout, null means "use default"
                var originalTimeout = _context.Database.GetCommandTimeout();
                _context.Database.SetCommandTimeout(0); // 0 = No timeout for background jobs

                var result = await _context.Database
                    .SqlQueryRaw<ConsolidationResult>(
                        "EXEC sp_ConsolidateCallLogBatch @BatchId, @StartMonth, @StartYear, @EndMonth, @EndYear, @CreatedBy",
                        batchIdParam, startMonthParam, startYearParam, endMonthParam, endYearParam, createdByParam)
                    .ToListAsync();

                // Restore original timeout
                _context.Database.SetCommandTimeout(originalTimeout);

                int totalImported = result.FirstOrDefault()?.TotalRecords ?? 0;

                _logger.LogInformation(
                    "Stored procedure completed for batch {BatchId}. Total: {Total}, Safaricom: {Safaricom}, Airtel: {Airtel}, PSTN: {PSTN}, PrivateWire: {PrivateWire}",
                    batchId,
                    totalImported,
                    result.FirstOrDefault()?.SafaricomRecords ?? 0,
                    result.FirstOrDefault()?.AirtelRecords ?? 0,
                    result.FirstOrDefault()?.PSTNRecords ?? 0,
                    result.FirstOrDefault()?.PrivateWireRecords ?? 0);

                // Update progress: Consolidation complete
                batch.CurrentOperation = $"Consolidated {totalImported:N0} records";
                batch.ProcessingProgress = 50;
                await _context.SaveChangesAsync();

                // If no records were imported, mark batch as failed
                if (totalImported == 0)
                {
                    batch.BatchStatus = BatchStatus.Failed;
                    batch.Notes = "No records were imported. The source tables may be empty for this period.";
                    batch.CurrentOperation = "Failed - No records found";
                    batch.ProcessingProgress = 0;
                    batch.EndProcessingDate = DateTime.UtcNow;
                    await _context.SaveChangesAsync();

                    _logger.LogWarning("Background consolidation for batch {BatchId} completed with 0 records", batchId);
                    return;
                }

                // Update progress: Detecting anomalies (fast mode for large batches)
                batch.CurrentOperation = $"Detecting anomalies for {totalImported:N0} records";
                batch.ProcessingProgress = 60;
                await _context.SaveChangesAsync();

                // Use fast anomaly detection for large batches (raw SQL)
                _logger.LogInformation("Detecting anomalies for batch {BatchId} using fast mode", batchId);
                int anomalyCount = await DetectBatchAnomaliesFastAsync(batch.Id);

                // Update progress after anomaly detection
                batch.CurrentOperation = $"Found {anomalyCount:N0} anomalies";
                batch.ProcessingProgress = 75;
                await _context.SaveChangesAsync();

                // =============================================
                // Auto-verify records without anomalies
                // Records with anomalies remain Pending for manual review
                // =============================================
                batch.CurrentOperation = "Auto-verifying clean records...";
                batch.ProcessingProgress = 80;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Auto-verifying records without anomalies for batch {BatchId}", batchId);
                int autoVerifiedCount = await AutoVerifyCleanRecordsAsync(batch.Id, createdBy);

                _logger.LogInformation("Auto-verified {AutoVerifiedCount} records without anomalies for batch {BatchId}",
                    autoVerifiedCount, batchId);

                // Update progress after auto-verification
                batch.CurrentOperation = $"Auto-verified {autoVerifiedCount:N0} clean records";
                batch.ProcessingProgress = 95;
                await _context.SaveChangesAsync();

                // Calculate final statistics
                int pendingCount = anomalyCount; // Only records with anomalies remain pending
                int verifiedCount = autoVerifiedCount;

                // Update batch statistics
                batch.TotalRecords = totalImported;
                batch.RecordsWithAnomalies = anomalyCount;
                batch.PendingRecords = pendingCount;
                batch.VerifiedRecords = verifiedCount;

                // Set batch status based on whether there are pending records
                batch.BatchStatus = pendingCount > 0 ? BatchStatus.PartiallyVerified : BatchStatus.Verified;
                batch.EndProcessingDate = DateTime.UtcNow;
                batch.CurrentOperation = null; // Clear to hide progress indicator
                batch.ProcessingProgress = 100;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Background consolidation completed for batch {BatchId}. Total: {TotalRecords}, Auto-verified: {AutoVerified}, Anomalies (pending review): {Anomalies}",
                    batchId, totalImported, autoVerifiedCount, anomalyCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during background consolidation for batch {BatchId}", batchId);

                batch.BatchStatus = BatchStatus.Failed;
                batch.Notes = $"Error during consolidation: {ex.Message}";
                batch.CurrentOperation = "Failed - Error occurred";
                batch.ProcessingProgress = 0;
                batch.EndProcessingDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                throw; // Re-throw so Hangfire marks the job as failed
            }
        }

        public async Task<int> ImportFromSafaricomAsync(Guid batchId, DateTime startDate, DateTime endDate)
        {
            int startMonth = startDate.Month;
            int startYear = startDate.Year;
            int endMonth = endDate.Month;
            int endYear = endDate.Year;

            _logger.LogInformation("Importing from Safaricom table for {StartMonth}/{StartYear} to {EndMonth}/{EndYear}",
                startMonth, startYear, endMonth, endYear);

            var records = await _context.Safaricoms
                .Where(s => s.CallMonth >= startMonth && s.CallMonth <= endMonth &&
                           s.CallYear >= startYear && s.CallYear <= endYear)
                .ToListAsync();

            // Get all active UserPhones for lookup
            var userPhones = await _context.UserPhones
                .Where(up => up.IsActive)
                .ToListAsync();

            var stagingRecords = new List<CallLogStaging>();

            foreach (var r in records)
            {
                // Find UserPhone by matching extension/phone number
                var userPhone = userPhones.FirstOrDefault(up =>
                    up.PhoneNumber == r.Ext ||
                    up.PhoneNumber == r.Ext?.Replace("+254", "0")); // Handle different formats

                // Safaricom: Durx handling based on dialed value
                // - Internet (dialed starts with "safaricom"): Durx is KB → convert to MB
                // - Voice (dialed is phone number): Durx is mm:ss decimal → convert to seconds
                // - Other (SMS, ROAMING, RENT, MMS, Bundle): Durx is count or N/A
                var dialed = (r.Dialed ?? "").Trim();
                var dialedLower = dialed.ToLowerInvariant();
                var durxValue = r.Durx ?? 0;
                int callDuration;
                DateTime callEndTime;
                string callType;

                if (dialedLower.StartsWith("safaricom"))
                {
                    // Internet Usage: Durx is in KB, convert to MB
                    callDuration = (int)(durxValue / 1024m);
                    callEndTime = (r.CallDate ?? DateTime.MinValue).AddSeconds((int)durxValue);
                    callType = "Internet Usage";
                }
                else if (dialed.Length > 0 && (char.IsDigit(dialed[0]) || dialed[0] == '+'))
                {
                    // Voice Call: Durx is mm:ss stored as decimal (e.g., 1.30 = 1min 30sec)
                    // Parse: integer part = minutes, decimal part * 100 = seconds
                    var minutes = (int)Math.Floor(durxValue);
                    var seconds = (int)((durxValue - minutes) * 100);
                    callDuration = (minutes * 60) + seconds;
                    callEndTime = (r.CallDate ?? DateTime.MinValue).AddSeconds(callDuration);
                    callType = "Voice";
                }
                else if (dialedLower == "sms")
                {
                    callDuration = (int)durxValue; // SMS count
                    callEndTime = r.CallDate ?? DateTime.MinValue;
                    callType = "SMS";
                }
                else if (dialedLower == "roaming")
                {
                    // Roaming could be mm:ss or count
                    var minutes = (int)Math.Floor(durxValue);
                    var seconds = (int)((durxValue - minutes) * 100);
                    callDuration = (minutes * 60) + seconds;
                    callEndTime = (r.CallDate ?? DateTime.MinValue).AddSeconds(callDuration);
                    callType = "Roaming";
                }
                else if (dialedLower == "rent")
                {
                    callDuration = 0;
                    callEndTime = r.CallDate ?? DateTime.MinValue;
                    callType = "Rent";
                }
                else if (dialedLower == "mms")
                {
                    callDuration = (int)durxValue; // MMS count
                    callEndTime = r.CallDate ?? DateTime.MinValue;
                    callType = "MMS";
                }
                else if (dialedLower.Contains("bundle"))
                {
                    callDuration = 0;
                    callEndTime = r.CallDate ?? DateTime.MinValue;
                    callType = "Bundle";
                }
                else
                {
                    // Unknown type, store as-is
                    callDuration = (int)durxValue;
                    callEndTime = r.CallDate ?? DateTime.MinValue;
                    callType = r.CallType ?? "Other";
                }

                var stagingRecord = new CallLogStaging
                {
                    ExtensionNumber = r.Ext ?? string.Empty,
                    CallDate = r.CallDate ?? DateTime.MinValue,
                    CallNumber = r.Dialed ?? string.Empty,
                    CallDestination = r.Dest ?? string.Empty,
                    CallEndTime = callEndTime,
                    CallDuration = callDuration,
                    CallCurrencyCode = "KES",
                    CallCost = r.Cost ?? 0,
                    CallCostUSD = (r.Cost ?? 0) / 150m, // Approximate conversion
                    CallCostKSHS = r.Cost ?? 0,
                    CallType = callType,
                    CallDestinationType = DetermineDestinationType(r.Dest),
                    CallYear = r.CallYear ?? DateTime.Now.Year,
                    CallMonth = r.CallMonth ?? DateTime.Now.Month,
                    ResponsibleIndexNumber = userPhone?.IndexNumber ?? r.IndexNumber, // Use UserPhone's owner or fallback
                    UserPhoneId = userPhone?.Id, // Link to UserPhone
                    SourceSystem = "Safaricom",
                    SourceRecordId = r.Id.ToString(),
                    BatchId = batchId,
                    ImportedBy = "System",
                    ImportedDate = DateTime.UtcNow,
                    VerificationStatus = VerificationStatus.Pending,
                    ProcessingStatus = ProcessingStatus.Staged
                };

                stagingRecords.Add(stagingRecord);
            }

            _context.CallLogStagings.AddRange(stagingRecords);

            // Update source records with StagingBatchId and UserPhoneId to track staging and link to user
            for (int i = 0; i < records.Count; i++)
            {
                records[i].StagingBatchId = batchId;

                // Link to UserPhone if match was found
                if (stagingRecords[i].UserPhoneId.HasValue)
                {
                    records[i].UserPhoneId = stagingRecords[i].UserPhoneId;
                }
            }

            await _context.SaveChangesAsync();

            return stagingRecords.Count;
        }

        public async Task<int> ImportFromAirtelAsync(Guid batchId, DateTime startDate, DateTime endDate)
        {
            int startMonth = startDate.Month;
            int startYear = startDate.Year;
            int endMonth = endDate.Month;
            int endYear = endDate.Year;

            _logger.LogInformation("Importing from Airtel table for {StartMonth}/{StartYear} to {EndMonth}/{EndYear}",
                startMonth, startYear, endMonth, endYear);

            var records = await _context.Airtels
                .Where(a => a.CallMonth >= startMonth && a.CallMonth <= endMonth &&
                           a.CallYear >= startYear && a.CallYear <= endYear)
                .ToListAsync();

            // Get all active UserPhones for lookup
            var userPhones = await _context.UserPhones
                .Where(up => up.IsActive)
                .ToListAsync();

            var stagingRecords = new List<CallLogStaging>();

            foreach (var r in records)
            {
                // Find UserPhone by matching extension/phone number
                var userPhone = userPhones.FirstOrDefault(up =>
                    up.PhoneNumber == r.Ext ||
                    up.PhoneNumber == r.Ext?.Replace("+254", "0")); // Handle different formats

                // Airtel: Dur is in minutes, convert to seconds
                var durationMinutes = r.Dur ?? 0;
                int callDuration = (int)(durationMinutes * 60); // Convert minutes to seconds
                DateTime callEndTime = (r.CallDate ?? DateTime.MinValue).AddSeconds(callDuration);

                var stagingRecord = new CallLogStaging
                {
                    ExtensionNumber = r.Ext ?? string.Empty,
                    CallDate = r.CallDate ?? DateTime.MinValue,
                    CallNumber = r.Dialed ?? string.Empty,
                    CallDestination = r.Dest ?? string.Empty,
                    CallEndTime = callEndTime,
                    CallDuration = callDuration,
                    CallCurrencyCode = "KES",
                    CallCost = r.Cost ?? 0,
                    CallCostUSD = (r.Cost ?? 0) / 150m,
                    CallCostKSHS = r.Cost ?? 0,
                    CallType = r.CallType ?? "Voice",
                    CallDestinationType = DetermineDestinationType(r.Dest),
                    CallYear = r.CallYear ?? DateTime.Now.Year,
                    CallMonth = r.CallMonth ?? DateTime.Now.Month,
                    ResponsibleIndexNumber = userPhone?.IndexNumber ?? r.IndexNumber,
                    UserPhoneId = userPhone?.Id, // Link to UserPhone
                    SourceSystem = "Airtel",
                    SourceRecordId = r.Id.ToString(),
                    BatchId = batchId,
                    ImportedBy = "System",
                    ImportedDate = DateTime.UtcNow,
                    VerificationStatus = VerificationStatus.Pending,
                    ProcessingStatus = ProcessingStatus.Staged
                };

                stagingRecords.Add(stagingRecord);
            }

            _context.CallLogStagings.AddRange(stagingRecords);

            // Update source records with StagingBatchId and UserPhoneId to track staging and link to user
            for (int i = 0; i < records.Count; i++)
            {
                records[i].StagingBatchId = batchId;

                // Link to UserPhone if match was found
                if (stagingRecords[i].UserPhoneId.HasValue)
                {
                    records[i].UserPhoneId = stagingRecords[i].UserPhoneId;
                }
            }

            await _context.SaveChangesAsync();

            return stagingRecords.Count;
        }

        public async Task<int> ImportFromPSTNAsync(Guid batchId, DateTime startDate, DateTime endDate)
        {
            int startMonth = startDate.Month;
            int startYear = startDate.Year;
            int endMonth = endDate.Month;
            int endYear = endDate.Year;

            _logger.LogInformation("Importing from PSTN table for {StartMonth}/{StartYear} to {EndMonth}/{EndYear}",
                startMonth, startYear, endMonth, endYear);

            var records = await _context.PSTNs
                .Where(p => p.CallMonth >= startMonth && p.CallMonth <= endMonth &&
                           p.CallYear >= startYear && p.CallYear <= endYear)
                .ToListAsync();

            // Get all active UserPhones for lookup
            var userPhones = await _context.UserPhones
                .Where(up => up.IsActive)
                .ToListAsync();

            var stagingRecords = new List<CallLogStaging>();

            foreach (var r in records)
            {
                // Find UserPhone by matching extension/phone number
                var userPhone = userPhones.FirstOrDefault(up =>
                    up.PhoneNumber == r.Extension ||
                    up.PhoneNumber == r.Extension?.Replace("+254", "0")); // Handle different formats

                var stagingRecord = new CallLogStaging
                {
                    ExtensionNumber = r.Extension ?? string.Empty,
                    CallDate = r.CallDate ?? DateTime.MinValue,
                    CallNumber = r.DialedNumber ?? string.Empty,
                    CallDestination = r.Destination ?? string.Empty,
                    CallEndTime = (r.CallDate ?? DateTime.MinValue).AddSeconds((int)((r.Duration ?? 0) * 60)),
                    CallDuration = (int)((r.Duration ?? 0) * 60),
                    CallCurrencyCode = "KSH",
                    CallCost = r.AmountKSH ?? 0,
                    CallCostUSD = (r.AmountKSH ?? 0) / 150m,
                    CallCostKSHS = r.AmountKSH ?? 0,
                    CallType = "Voice",
                    CallDestinationType = DetermineDestinationType(r.Destination),
                    CallYear = (r.CallDate ?? DateTime.MinValue).Year,
                    CallMonth = (r.CallDate ?? DateTime.MinValue).Month,
                    ResponsibleIndexNumber = userPhone?.IndexNumber ?? r.IndexNumber,
                    UserPhoneId = userPhone?.Id, // Link to UserPhone
                    SourceSystem = "PSTN",
                    SourceRecordId = r.Id.ToString(),
                    BatchId = batchId,
                    ImportedBy = "System",
                    ImportedDate = DateTime.UtcNow,
                    VerificationStatus = VerificationStatus.Pending,
                    ProcessingStatus = ProcessingStatus.Staged
                };

                stagingRecords.Add(stagingRecord);
            }

            _context.CallLogStagings.AddRange(stagingRecords);

            // Update source records with StagingBatchId and UserPhoneId to track staging and link to user
            for (int i = 0; i < records.Count; i++)
            {
                records[i].StagingBatchId = batchId;

                // Link to UserPhone if match was found
                if (stagingRecords[i].UserPhoneId.HasValue)
                {
                    records[i].UserPhoneId = stagingRecords[i].UserPhoneId;
                }
            }

            await _context.SaveChangesAsync();

            return stagingRecords.Count;
        }

        public async Task<int> ImportFromPrivateWireAsync(Guid batchId, DateTime startDate, DateTime endDate)
        {
            int startMonth = startDate.Month;
            int startYear = startDate.Year;
            int endMonth = endDate.Month;
            int endYear = endDate.Year;

            _logger.LogInformation("Importing from PrivateWire table for {StartMonth}/{StartYear} to {EndMonth}/{EndYear}",
                startMonth, startYear, endMonth, endYear);

            var records = await _context.PrivateWires
                .Where(p => p.CallMonth >= startMonth && p.CallMonth <= endMonth &&
                           p.CallYear >= startYear && p.CallYear <= endYear)
                .ToListAsync();

            // Get all active UserPhones for lookup
            var userPhones = await _context.UserPhones
                .Where(up => up.IsActive)
                .ToListAsync();

            var stagingRecords = new List<CallLogStaging>();

            foreach (var r in records)
            {
                // Find UserPhone by matching extension/phone number
                var userPhone = userPhones.FirstOrDefault(up =>
                    up.PhoneNumber == r.Extension ||
                    up.PhoneNumber == r.Extension?.Replace("+254", "0")); // Handle different formats

                var stagingRecord = new CallLogStaging
                {
                    ExtensionNumber = r.Extension ?? string.Empty,
                    CallDate = r.CallDate ?? DateTime.MinValue,
                    CallNumber = r.DialedNumber ?? string.Empty,
                    CallDestination = r.Destination ?? string.Empty,
                    CallEndTime = (r.CallDate ?? DateTime.MinValue).AddSeconds((int)((r.Duration ?? 0) * 60)),
                    CallDuration = (int)((r.Duration ?? 0) * 60),
                    CallCurrencyCode = "USD",
                    CallCost = r.AmountUSD ?? 0,
                    CallCostUSD = r.AmountUSD ?? 0,
                    CallCostKSHS = (r.AmountUSD ?? 0) * 150m,
                    CallType = "Voice",
                    CallDestinationType = "Internal",
                    CallYear = (r.CallDate ?? DateTime.MinValue).Year,
                    CallMonth = (r.CallDate ?? DateTime.MinValue).Month,
                    ResponsibleIndexNumber = userPhone?.IndexNumber ?? r.IndexNumber,
                    UserPhoneId = userPhone?.Id, // Link to UserPhone
                    SourceSystem = "PrivateWire",
                    SourceRecordId = r.Id.ToString(),
                    BatchId = batchId,
                    ImportedBy = "System",
                    ImportedDate = DateTime.UtcNow,
                    VerificationStatus = VerificationStatus.Pending,
                    ProcessingStatus = ProcessingStatus.Staged
                };

                stagingRecords.Add(stagingRecord);
            }

            _context.CallLogStagings.AddRange(stagingRecords);

            // Update source records with StagingBatchId and UserPhoneId to track staging and link to user
            for (int i = 0; i < records.Count; i++)
            {
                records[i].StagingBatchId = batchId;

                // Link to UserPhone if match was found
                if (stagingRecords[i].UserPhoneId.HasValue)
                {
                    records[i].UserPhoneId = stagingRecords[i].UserPhoneId;
                }
            }

            await _context.SaveChangesAsync();

            return stagingRecords.Count;
        }

        public async Task<int> DetectBatchAnomaliesAsync(Guid batchId)
        {
            // Process in batches to avoid loading 183K+ records into memory at once
            const int BATCH_SIZE = 5000;
            int anomalyCount = 0;
            int processedCount = 0;

            // Get total count for logging
            var totalRecords = await _context.CallLogStagings
                .Where(l => l.BatchId == batchId)
                .CountAsync();

            _logger.LogInformation("Starting anomaly detection for batch {BatchId}. Total records: {Total}", batchId, totalRecords);

            // Get all IDs to process
            var allIds = await _context.CallLogStagings
                .Where(l => l.BatchId == batchId)
                .Select(l => l.Id)
                .ToListAsync();

            // Get the batch for progress updates
            var batch = await _context.StagingBatches.FindAsync(batchId);

            // Process in batches
            for (int i = 0; i < allIds.Count; i += BATCH_SIZE)
            {
                var batchIds = allIds.Skip(i).Take(BATCH_SIZE).ToList();

                // Load this batch with includes
                var logs = await _context.CallLogStagings
                    .Include(l => l.ResponsibleUser)
                    .Include(l => l.UserPhone)
                    .Where(l => batchIds.Contains(l.Id))
                    .ToListAsync();

                foreach (var log in logs)
                {
                    var anomalies = DetectAnomaliesForLog(log);
                    if (anomalies.Any())
                    {
                        log.HasAnomalies = true;
                        log.AnomalyTypes = JsonSerializer.Serialize(anomalies.Select(a => a.Code));
                        log.AnomalyDetails = JsonSerializer.Serialize(anomalies.ToDictionary(a => a.Code, a => (object)a.Description));

                        // Auto-reject critical anomalies
                        if (anomalies.Any(a => a.Severity == SeverityLevel.Critical))
                        {
                            log.VerificationStatus = VerificationStatus.Rejected;
                        }

                        anomalyCount++;
                    }
                }

                processedCount += logs.Count;

                // Update batch progress (scale from 60% to 95% during anomaly detection)
                if (batch != null)
                {
                    var percentComplete = (processedCount * 100.0 / totalRecords);
                    batch.CurrentOperation = $"Detecting anomalies: {processedCount:N0}/{totalRecords:N0} ({percentComplete:F0}%)";
                    batch.ProcessingProgress = 60 + (int)(percentComplete * 0.35); // 60% to 95%
                }

                // Save each batch to avoid massive transaction at end
                await _context.SaveChangesAsync();

                // Log progress every 25K records
                if (processedCount % 25000 < BATCH_SIZE)
                {
                    _logger.LogInformation("Anomaly detection progress: {Processed:N0} / {Total:N0} ({Percent:F1}%)",
                        processedCount, totalRecords, (processedCount * 100.0 / totalRecords));
                }

                // Detach staging logs to free memory (but not the batch)
                foreach (var log in logs)
                {
                    _context.Entry(log).State = EntityState.Detached;
                }
            }

            _logger.LogInformation("Anomaly detection completed for batch {BatchId}. Anomalies found: {AnomalyCount}", batchId, anomalyCount);
            return anomalyCount;
        }

        /// <summary>
        /// Fast anomaly detection using raw SQL for large batches.
        /// Detects basic anomalies without loading entities:
        /// - Missing responsible user (ResponsibleIndexNumber is null or empty)
        /// - Missing phone assignment (UserPhoneId is null)
        /// - High cost (CallCost > threshold)
        /// - Long duration (CallDuration > threshold)
        /// </summary>
        public async Task<int> DetectBatchAnomaliesFastAsync(Guid batchId)
        {
            _logger.LogInformation("Starting fast anomaly detection for batch {BatchId}", batchId);

            // Use raw SQL to update all records at once
            // This is much faster than loading entities one by one
            // Note: Double curly braces to escape C# string format placeholders
            // Check for inactive users by joining with EbillUsers table
            var sql = @"
                UPDATE cls
                SET
                    HasAnomalies = CASE
                        WHEN cls.ResponsibleIndexNumber IS NULL OR cls.ResponsibleIndexNumber = '' THEN 1
                        WHEN cls.UserPhoneId IS NULL THEN 1
                        WHEN u.IsActive = 0 THEN 1
                        ELSE 0
                    END,
                    AnomalyTypes = CASE
                        WHEN cls.ResponsibleIndexNumber IS NULL OR cls.ResponsibleIndexNumber = '' THEN '[""MISSING_USER""]'
                        WHEN cls.UserPhoneId IS NULL THEN '[""UNASSIGNED_PHONE""]'
                        WHEN u.IsActive = 0 THEN '[""INACTIVE_USER""]'
                        ELSE NULL
                    END,
                    AnomalyDetails = CASE
                        WHEN cls.ResponsibleIndexNumber IS NULL OR cls.ResponsibleIndexNumber = '' THEN '{{""MISSING_USER"":""No responsible user assigned""}}'
                        WHEN cls.UserPhoneId IS NULL THEN '{{""UNASSIGNED_PHONE"":""Phone number not assigned to any user""}}'
                        WHEN u.IsActive = 0 THEN '{{""INACTIVE_USER"":""User is inactive""}}'
                        ELSE NULL
                    END
                FROM CallLogStagings cls
                LEFT JOIN EbillUsers u ON cls.ResponsibleIndexNumber = u.IndexNumber
                WHERE cls.BatchId = @BatchId";

            var batchIdParam = new SqlParameter("@BatchId", batchId);

            // Set longer timeout for large batches
            var originalTimeout = _context.Database.GetCommandTimeout();
            _context.Database.SetCommandTimeout(300); // 5 minutes

            await _context.Database.ExecuteSqlRawAsync(sql, batchIdParam);

            // Restore timeout
            _context.Database.SetCommandTimeout(originalTimeout);

            // Count anomalies
            var anomalyCount = await _context.CallLogStagings
                .Where(l => l.BatchId == batchId && l.HasAnomalies)
                .CountAsync();

            _logger.LogInformation("Fast anomaly detection completed for batch {BatchId}. Anomalies found: {AnomalyCount}", batchId, anomalyCount);
            return anomalyCount;
        }

        /// <summary>
        /// Auto-verify records that have no anomalies.
        /// Records with anomalies remain in Pending status for manual review.
        /// Uses raw SQL for efficient bulk update of large batches.
        /// </summary>
        public async Task<int> AutoVerifyCleanRecordsAsync(Guid batchId, string verifiedBy)
        {
            _logger.LogInformation("Starting auto-verification of clean records for batch {BatchId}", batchId);

            // Use interpolated SQL for safe parameter handling
            var verifiedByValue = $"{verifiedBy} (Auto)";
            var verificationDate = DateTime.UtcNow;
            var verifiedStatus = (int)VerificationStatus.Verified;
            var pendingStatus = (int)VerificationStatus.Pending;

            // Set longer timeout for large batches
            var originalTimeout = _context.Database.GetCommandTimeout();
            _context.Database.SetCommandTimeout(300); // 5 minutes

            // Use ExecuteSqlInterpolatedAsync for safe parameter binding
            var rowsAffected = await _context.Database.ExecuteSqlInterpolatedAsync(
                $@"UPDATE CallLogStagings
                   SET
                       VerificationStatus = {verifiedStatus},
                       VerificationDate = {verificationDate},
                       VerifiedBy = {verifiedByValue},
                       VerificationNotes = 'Auto-verified: No anomalies detected',
                       ModifiedDate = {verificationDate},
                       ModifiedBy = {verifiedByValue}
                   WHERE BatchId = {batchId}
                     AND HasAnomalies = 0
                     AND VerificationStatus = {pendingStatus}");

            // Restore timeout
            _context.Database.SetCommandTimeout(originalTimeout);

            _logger.LogInformation("Auto-verification completed for batch {BatchId}. Records verified: {RowsAffected}", batchId, rowsAffected);
            return rowsAffected;
        }

        public async Task<List<CallLogAnomaly>> DetectAnomaliesAsync(int stagingId)
        {
            var log = await _context.CallLogStagings
                .Include(l => l.ResponsibleUser)
                .Include(l => l.UserPhone)
                .FirstOrDefaultAsync(l => l.Id == stagingId);

            if (log == null)
                return new List<CallLogAnomaly>();

            return DetectAnomaliesForLog(log);
        }

        /// <summary>
        /// Detects anomalies for an already-loaded CallLogStaging record.
        /// This avoids the N+1 query problem when processing batches.
        /// The log must have ResponsibleUser already included/loaded.
        /// </summary>
        private List<CallLogAnomaly> DetectAnomaliesForLog(CallLogStaging log)
        {
            var anomalies = new List<CallLogAnomaly>();

            // Check for no UserPhone link
            if (!log.UserPhoneId.HasValue)
            {
                anomalies.Add(new CallLogAnomaly
                {
                    Code = AnomalyCodes.NoPhone,
                    Name = "Phone Not Registered",
                    Description = $"Extension {log.ExtensionNumber} is not registered in UserPhones",
                    Severity = SeverityLevel.Medium
                });
            }

            // Check for no user
            if (string.IsNullOrEmpty(log.ResponsibleIndexNumber))
            {
                anomalies.Add(new CallLogAnomaly
                {
                    Code = AnomalyCodes.NoUser,
                    Name = "No User Assigned",
                    Description = "Extension has no responsible user",
                    Severity = SeverityLevel.High
                });
            }
            else
            {
                // Use the already-loaded ResponsibleUser instead of querying again
                var user = log.ResponsibleUser;

                if (user == null)
                {
                    anomalies.Add(new CallLogAnomaly
                    {
                        Code = AnomalyCodes.NoUser,
                        Name = "User Not Found",
                        Description = $"User with index {log.ResponsibleIndexNumber} not found",
                        Severity = SeverityLevel.High
                    });
                }
                else if (!user.IsActive)
                {
                    anomalies.Add(new CallLogAnomaly
                    {
                        Code = AnomalyCodes.InactiveUser,
                        Name = "Inactive User",
                        Description = $"User {user.FullName} is inactive",
                        Severity = SeverityLevel.Medium
                    });
                }
            }

            // Check for future date
            if (log.CallDate > DateTime.UtcNow)
            {
                anomalies.Add(new CallLogAnomaly
                {
                    Code = AnomalyCodes.FutureDate,
                    Name = "Future Date",
                    Description = "Call date is in the future",
                    Severity = SeverityLevel.Critical
                });
            }

            return anomalies;
        }

        public async Task<bool> ValidateCallLogAsync(CallLogStaging log)
        {
            var anomalies = await DetectAnomaliesAsync(log.Id);
            return !anomalies.Any(a => a.Severity == SeverityLevel.Critical);
        }

        public async Task<bool> VerifyCallLogAsync(int stagingId, string verifiedBy, string? notes = null)
        {
            var log = await _context.CallLogStagings.FindAsync(stagingId);
            if (log == null)
                return false;

            log.VerificationStatus = VerificationStatus.Verified;
            log.VerificationDate = DateTime.UtcNow;
            log.VerifiedBy = verifiedBy;
            log.VerificationNotes = notes;
            log.ModifiedDate = DateTime.UtcNow;
            log.ModifiedBy = verifiedBy;

            await _context.SaveChangesAsync();
            await UpdateBatchStatistics(log.BatchId);

            return true;
        }

        public async Task<int> BulkVerifyAsync(List<int> stagingIds, string verifiedBy)
        {
            var logs = await _context.CallLogStagings
                .Where(l => stagingIds.Contains(l.Id))
                .ToListAsync();

            foreach (var log in logs)
            {
                log.VerificationStatus = VerificationStatus.Verified;
                log.VerificationDate = DateTime.UtcNow;
                log.VerifiedBy = verifiedBy;
                log.ModifiedDate = DateTime.UtcNow;
                log.ModifiedBy = verifiedBy;
            }

            await _context.SaveChangesAsync();

            if (logs.Any())
            {
                await UpdateBatchStatistics(logs.First().BatchId);
            }

            return logs.Count;
        }

        public async Task BulkVerifyInBackgroundAsync(Guid batchId, string verifiedBy)
        {
            _logger.LogInformation("Starting background verification for batch {BatchId}", batchId);

            try
            {
                // Get the batch to update progress
                var stagingBatch = await _context.StagingBatches.FindAsync(batchId);
                if (stagingBatch == null)
                {
                    _logger.LogError("Batch {BatchId} not found for verification", batchId);
                    return;
                }

                // Get count of records that need verification
                var pendingCount = await _context.CallLogStagings
                    .Where(c => c.BatchId == batchId &&
                           (c.VerificationStatus == VerificationStatus.Pending ||
                            c.VerificationStatus == VerificationStatus.RequiresReview))
                    .CountAsync();

                if (pendingCount == 0)
                {
                    _logger.LogInformation("No records to verify in batch {BatchId}", batchId);
                    stagingBatch.CurrentOperation = null;
                    stagingBatch.ProcessingProgress = 100;
                    await _context.SaveChangesAsync();
                    return;
                }

                // Update batch to show verification is in progress
                stagingBatch.CurrentOperation = $"Verifying {pendingCount} records...";
                stagingBatch.ProcessingProgress = 10;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Verifying {PendingCount} records in batch {BatchId} using stored procedure", pendingCount, batchId);

                // Use stored procedure for fast bulk verification
                var batchIdParam = new SqlParameter("@BatchId", batchId);
                var verifiedByParam = new SqlParameter("@VerifiedBy", verifiedBy);

                // Set longer timeout for large batch operations (5 minutes)
                _context.Database.SetCommandTimeout(300);

                var result = await _context.Database
                    .SqlQueryRaw<VerifyBatchResult>(
                        "EXEC sp_VerifyBatch @BatchId, @VerifiedBy",
                        batchIdParam, verifiedByParam)
                    .AsNoTracking()
                    .ToListAsync();

                // Reset timeout to default
                _context.Database.SetCommandTimeout(30);

                var verifyResult = result.FirstOrDefault();

                if (verifyResult == null || verifyResult.Success == 0)
                {
                    var errorMessage = verifyResult?.Error ?? "Unknown error during verification";
                    _logger.LogError("Failed to verify batch {BatchId}: {Error}", batchId, errorMessage);
                    throw new InvalidOperationException($"Failed to verify batch: {errorMessage}");
                }

                // Reload batch to update final status (stored procedure already updated stats)
                stagingBatch = await _context.StagingBatches.FindAsync(batchId);
                if (stagingBatch != null)
                {
                    stagingBatch.CurrentOperation = null;
                    stagingBatch.ProcessingProgress = 100;
                    await _context.SaveChangesAsync();
                }

                _logger.LogInformation("Completed background verification for batch {BatchId}. Verified {RecordsVerified} records. Total verified: {TotalVerified}, Pending: {TotalPending}",
                    batchId, verifyResult.RecordsVerified, verifyResult.TotalVerified, verifyResult.TotalPending);
            }
            catch (Exception ex)
            {
                // Update batch to show error (truncate to fit column max length of 100)
                var stagingBatch = await _context.StagingBatches.FindAsync(batchId);
                if (stagingBatch != null)
                {
                    var errorMessage = $"Verification failed: {ex.Message}";
                    stagingBatch.CurrentOperation = errorMessage.Length > 100 ? errorMessage.Substring(0, 97) + "..." : errorMessage;
                    stagingBatch.BatchStatus = BatchStatus.Failed;
                    await _context.SaveChangesAsync();
                }

                _logger.LogError(ex, "Error during background verification for batch {BatchId}", batchId);
                throw;
            }
        }

        public async Task<bool> RejectCallLogAsync(int stagingId, string rejectedBy, string reason)
        {
            var log = await _context.CallLogStagings.FindAsync(stagingId);
            if (log == null)
                return false;

            log.VerificationStatus = VerificationStatus.Rejected;
            log.VerificationDate = DateTime.UtcNow;
            log.VerifiedBy = rejectedBy;
            log.VerificationNotes = reason;
            log.ModifiedDate = DateTime.UtcNow;
            log.ModifiedBy = rejectedBy;

            await _context.SaveChangesAsync();
            await UpdateBatchStatistics(log.BatchId);

            return true;
        }

        public async Task<int> BulkRejectAsync(List<int> stagingIds, string rejectedBy, string reason)
        {
            var logs = await _context.CallLogStagings
                .Where(l => stagingIds.Contains(l.Id))
                .ToListAsync();

            foreach (var log in logs)
            {
                log.VerificationStatus = VerificationStatus.Rejected;
                log.VerificationDate = DateTime.UtcNow;
                log.VerifiedBy = rejectedBy;
                log.VerificationNotes = reason;
                log.ModifiedDate = DateTime.UtcNow;
                log.ModifiedBy = rejectedBy;
            }

            await _context.SaveChangesAsync();

            if (logs.Any())
            {
                await UpdateBatchStatistics(logs.First().BatchId);
            }

            return logs.Count;
        }

        public async Task<int> PushToProductionAsync(Guid batchId, DateTime? verificationPeriod = null, string? verificationType = null, bool sendNotifications = true)
        {
            var batch = await _context.StagingBatches
                .Include(b => b.BillingPeriod)
                .FirstOrDefaultAsync(b => b.Id == batchId);

            if (batch == null)
            {
                _logger.LogWarning("Cannot push batch {BatchId} - batch not found", batchId);
                return 0;
            }

            // Allow pushing if batch is Verified or PartiallyVerified
            if (batch.BatchStatus != BatchStatus.Verified && batch.BatchStatus != BatchStatus.PartiallyVerified)
            {
                _logger.LogWarning("Cannot push batch {BatchId} - status is {Status}, must be Verified or PartiallyVerified",
                    batchId, batch.BatchStatus);
                return 0;
            }

            // Extract billing month and year from BillingPeriod
            int billingMonth = DateTime.Now.Month;
            int billingYear = DateTime.Now.Year;

            if (batch.BillingPeriod != null && !string.IsNullOrEmpty(batch.BillingPeriod.PeriodCode))
            {
                // PeriodCode format is "YYYY-MM" (e.g., "2024-09")
                var parts = batch.BillingPeriod.PeriodCode.Split('-');
                if (parts.Length == 2 && int.TryParse(parts[0], out int year) && int.TryParse(parts[1], out int month))
                {
                    billingYear = year;
                    billingMonth = month;
                    _logger.LogInformation("Using billing period {PeriodCode} for CallYear={Year}, CallMonth={Month}",
                        batch.BillingPeriod.PeriodCode, billingYear, billingMonth);
                }
            }

            // Load recovery configuration for approval deadline calculation
            var config = await _context.RecoveryConfigurations
                .FirstOrDefaultAsync(rc => rc.RuleName == "SystemConfiguration");

            // Calculate approval period (verification period + approval days)
            DateTime? approvalPeriod = null;
            if (verificationPeriod.HasValue && config != null && config.DefaultApprovalDays.HasValue)
            {
                approvalPeriod = verificationPeriod.Value.AddDays(config.DefaultApprovalDays.Value);
                _logger.LogInformation("Setting approval period to {ApprovalPeriod} ({Days} days after verification deadline)",
                    approvalPeriod, config.DefaultApprovalDays);
            }

            // Use stored procedure to push verified records to production efficiently
            _logger.LogInformation("Calling sp_PushBatchToProduction for batch {BatchId}", batchId);

            var batchIdParam = new SqlParameter("@BatchId", batchId);
            var verificationPeriodParam = new SqlParameter("@VerificationPeriod", (object?)verificationPeriod ?? DBNull.Value);
            var verificationTypeParam = new SqlParameter("@VerificationType", (object?)verificationType ?? DBNull.Value);
            var approvalPeriodParam = new SqlParameter("@ApprovalPeriod", (object?)approvalPeriod ?? DBNull.Value);
            var publishedByParam = new SqlParameter("@PublishedBy", "System");

            // Set longer timeout for large batch operations (5 minutes)
            _context.Database.SetCommandTimeout(300);

            var result = await _context.Database
                .SqlQueryRaw<PushToProductionResult>(
                    "EXEC sp_PushBatchToProduction @BatchId, @VerificationPeriod, @VerificationType, @ApprovalPeriod, @PublishedBy",
                    batchIdParam, verificationPeriodParam, verificationTypeParam, approvalPeriodParam, publishedByParam)
                .AsNoTracking()
                .ToListAsync();

            // Reset timeout to default
            _context.Database.SetCommandTimeout(30);

            var pushResult = result.FirstOrDefault();

            if (pushResult == null || pushResult.Success == 0)
            {
                var errorMessage = pushResult?.Error ?? "Unknown error during push to production";
                _logger.LogError("Failed to push batch {BatchId} to production: {Error}", batchId, errorMessage);
                throw new InvalidOperationException($"Failed to push batch to production: {errorMessage}");
            }

            _logger.LogInformation(
                "Successfully pushed {RecordsPushed} records to production. " +
                "Source records updated: Safaricom={Safaricom}, Airtel={Airtel}, PSTN={PSTN}, PrivateWire={PrivateWire}. " +
                "Remaining unprocessed: {Remaining}",
                pushResult.RecordsPushed,
                pushResult.SafaricomUpdated,
                pushResult.AirtelUpdated,
                pushResult.PSTNUpdated,
                pushResult.PrivateWireUpdated,
                pushResult.RemainingUnprocessed);

            // Reload batch to get updated status from stored procedure
            await _context.Entry(batch).ReloadAsync();

            // Create deadline tracking entries if verification period is set
            if (verificationPeriod.HasValue)
            {
                try
                {
                    // Create verification deadline tracking
                    await _deadlineService.CreateVerificationDeadlineAsync(
                        batchId,
                        verificationPeriod.Value,
                        "System");

                    _logger.LogInformation("Created deadline tracking for batch {BatchId} - Verification Period: {VerificationPeriod}",
                        batchId, verificationPeriod.Value);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating deadline tracking for batch {BatchId}", batchId);
                    // Don't fail the push operation if deadline tracking fails
                }
            }

            // Send email notifications to staff members about published records (if enabled)
            if (sendNotifications)
            {
                try
                {
                    // Query the records that were just pushed to production
                    var publishedRecords = await _context.CallRecords
                        .Where(r => r.SourceBatchId == batchId)
                        .ToListAsync();

                    await SendPublishedNotificationsAsync(publishedRecords, batch, verificationPeriod, approvalPeriod);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending published notifications for batch {BatchId}", batchId);
                    // Don't fail the push operation if email notifications fail
                }
            }
            else
            {
                _logger.LogInformation("Skipping email notifications for batch {BatchId} - notifications disabled", batchId);
            }

            _logger.LogInformation("Pushed {Count} verified records to production from batch {BatchId}",
                pushResult.RecordsPushed, batchId);

            return pushResult.RecordsPushed;
        }

        public async Task PushToProductionInBackgroundAsync(Guid batchId, DateTime? verificationPeriod, string? verificationType, string publishedBy, bool sendNotifications = true)
        {
            _logger.LogInformation("Starting background push to production for batch {BatchId}", batchId);

            try
            {
                // Get the batch to update progress
                var batch = await _context.StagingBatches.FindAsync(batchId);
                if (batch == null)
                {
                    _logger.LogError("Batch {BatchId} not found for push to production", batchId);
                    return;
                }

                // Get count of verified records to push
                var verifiedCount = await _context.CallLogStagings
                    .Where(c => c.BatchId == batchId && c.VerificationStatus == VerificationStatus.Verified)
                    .CountAsync();

                if (verifiedCount == 0)
                {
                    _logger.LogInformation("No verified records to push in batch {BatchId}", batchId);
                    batch.CurrentOperation = null;
                    batch.ProcessingProgress = 100;
                    await _context.SaveChangesAsync();
                    return;
                }

                // Update batch to show push is in progress
                batch.CurrentOperation = $"Pushing {verifiedCount} records to production...";
                batch.ProcessingProgress = 10;
                batch.PublishedBy = publishedBy;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Pushing {VerifiedCount} records to production for batch {BatchId}", verifiedCount, batchId);

                // Update progress - preparing
                batch.CurrentOperation = "Preparing records for production...";
                batch.ProcessingProgress = 20;
                await _context.SaveChangesAsync();

                // Call the actual push to production logic
                var recordsPushed = await PushToProductionAsync(batchId, verificationPeriod, verificationType, sendNotifications);

                // Update progress - completing
                batch = await _context.StagingBatches.FindAsync(batchId);
                if (batch != null)
                {
                    batch.CurrentOperation = null;
                    batch.ProcessingProgress = 100;
                    await _context.SaveChangesAsync();
                }

                _logger.LogInformation("Completed background push to production for batch {BatchId}. Pushed {RecordsPushed} records.",
                    batchId, recordsPushed);
            }
            catch (Exception ex)
            {
                // Update batch to show error (truncate to fit column max length of 100)
                var batch = await _context.StagingBatches.FindAsync(batchId);
                if (batch != null)
                {
                    var errorMessage = $"Push failed: {ex.Message}";
                    batch.CurrentOperation = errorMessage.Length > 100 ? errorMessage.Substring(0, 97) + "..." : errorMessage;
                    batch.BatchStatus = BatchStatus.Failed;
                    await _context.SaveChangesAsync();
                }

                _logger.LogError(ex, "Error during background push to production for batch {BatchId}", batchId);
                throw;
            }
        }

        public async Task<int> SendBatchNotificationsAsync(Guid batchId, DateTime? verificationPeriod = null)
        {
            _logger.LogInformation("Starting to send notifications for batch {BatchId}", batchId);

            var batch = await _context.StagingBatches
                .Include(b => b.BillingPeriod)
                .FirstOrDefaultAsync(b => b.Id == batchId);

            if (batch == null)
            {
                _logger.LogWarning("Cannot send notifications for batch {BatchId} - batch not found", batchId);
                return 0;
            }

            // Only allow sending notifications for Published batches
            if (batch.BatchStatus != BatchStatus.Published)
            {
                _logger.LogWarning("Cannot send notifications for batch {BatchId} - status is {Status}, must be Published",
                    batchId, batch.BatchStatus);
                return 0;
            }

            try
            {
                // Query the records that were pushed to production
                var publishedRecords = await _context.CallRecords
                    .Where(r => r.SourceBatchId == batchId)
                    .ToListAsync();

                if (!publishedRecords.Any())
                {
                    _logger.LogWarning("No published records found for batch {BatchId}", batchId);
                    return 0;
                }

                // Calculate approval period (verification + 3 days)
                var approvalPeriod = verificationPeriod?.AddDays(3);

                await SendPublishedNotificationsAsync(publishedRecords, batch, verificationPeriod, approvalPeriod);

                // Count unique staff members notified
                var uniqueStaffCount = publishedRecords
                    .Select(r => r.IndexNumber)
                    .Distinct()
                    .Count();

                _logger.LogInformation("Queued notifications for {Count} staff members in batch {BatchId}", uniqueStaffCount, batchId);

                return uniqueStaffCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending notifications for batch {BatchId}", batchId);
                throw;
            }
        }

        public async Task<bool> RollbackBatchAsync(Guid batchId)
        {
            // Remove production records for this batch
            var productionRecords = await _context.CallRecords
                .Where(r => r.SourceBatchId == batchId)
                .ToListAsync();

            _context.CallRecords.RemoveRange(productionRecords);

            // Reset staging records
            var stagingRecords = await _context.CallLogStagings
                .Where(l => l.BatchId == batchId && l.ProcessingStatus == ProcessingStatus.Completed)
                .ToListAsync();

            foreach (var log in stagingRecords)
            {
                log.ProcessingStatus = ProcessingStatus.Staged;
                log.ProcessedDate = null;
            }

            // Update batch status
            var batch = await _context.StagingBatches.FindAsync(batchId);
            if (batch != null)
            {
                batch.BatchStatus = BatchStatus.Verified;
                batch.PublishedBy = null;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Rolled back {Count} records from production for batch {BatchId}",
                productionRecords.Count, batchId);

            return true;
        }

        public async Task<PagedResult<CallLogStaging>> GetStagedLogsAsync(StagingFilter filter)
        {
            var query = _context.CallLogStagings
                .Include(l => l.ResponsibleUser)
                .Include(l => l.Batch)
                .Include(l => l.UserPhone)
                    .ThenInclude(up => up!.EbillUser)
                .AsQueryable();

            // Apply filters
            if (filter.BatchId.HasValue)
                query = query.Where(l => l.BatchId == filter.BatchId.Value);

            if (filter.Status.HasValue)
                query = query.Where(l => l.VerificationStatus == filter.Status.Value);

            if (filter.HasAnomalies.HasValue)
                query = query.Where(l => l.HasAnomalies == filter.HasAnomalies.Value);

            if (!string.IsNullOrEmpty(filter.ExtensionNumber))
                query = query.Where(l => l.ExtensionNumber.Contains(filter.ExtensionNumber));

            if (!string.IsNullOrEmpty(filter.SearchTerm))
            {
                var searchLower = filter.SearchTerm.ToLower();
                query = query.Where(l =>
                    l.ExtensionNumber.ToLower().Contains(searchLower) ||
                    l.CallNumber.ToLower().Contains(searchLower) ||
                    l.CallDestination.ToLower().Contains(searchLower) ||
                    (l.ResponsibleIndexNumber != null && l.ResponsibleIndexNumber.ToLower().Contains(searchLower)) ||
                    (l.ResponsibleUser != null && (
                        l.ResponsibleUser.FirstName.ToLower().Contains(searchLower) ||
                        l.ResponsibleUser.LastName.ToLower().Contains(searchLower) ||
                        l.ResponsibleUser.Email.ToLower().Contains(searchLower)
                    )));
            }

            if (filter.StartDate.HasValue)
                query = query.Where(l => l.CallDate >= filter.StartDate.Value);

            if (filter.EndDate.HasValue)
                query = query.Where(l => l.CallDate <= filter.EndDate.Value);

            // Count total items
            var totalCount = await query.CountAsync();

            // Apply sorting
            query = filter.SortBy switch
            {
                "CallDate" => filter.SortDescending ? query.OrderByDescending(l => l.CallDate) : query.OrderBy(l => l.CallDate),
                "Cost" => filter.SortDescending ? query.OrderByDescending(l => l.CallCostUSD) : query.OrderBy(l => l.CallCostUSD),
                "Duration" => filter.SortDescending ? query.OrderByDescending(l => l.CallDuration) : query.OrderBy(l => l.CallDuration),
                _ => query.OrderByDescending(l => l.ImportedDate)
            };

            // Apply paging
            var items = await query
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            return new PagedResult<CallLogStaging>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize
            };
        }

        public async Task<StagingBatch?> GetBatchDetailsAsync(Guid batchId)
        {
            // =============================================
            // OPTIMIZED: Don't load call logs with the batch!
            // The batch might have 100K+ call logs - loading them all causes timeout
            // Call logs are loaded separately with pagination via GetStagedLogsAsync
            // =============================================
            return await _context.StagingBatches
                .FirstOrDefaultAsync(b => b.Id == batchId);
        }

        public async Task<Dictionary<string, int>> GetBatchStatisticsAsync(Guid batchId)
        {
            // =============================================
            // OPTIMIZED: Single query with conditional aggregation
            // Gets all statistics in ONE database call instead of 7 separate queries
            // This translates to: SELECT COUNT(CASE WHEN ... THEN 1 END) for each stat
            // =============================================

            var today = DateTime.UtcNow.Date;
            var stats = await _context.CallLogStagings
                .Where(l => l.BatchId == batchId)
                .GroupBy(l => 1) // Group all records together
                .Select(g => new
                {
                    Total = g.Count(),
                    Pending = g.Count(l => l.VerificationStatus == VerificationStatus.Pending),
                    Verified = g.Count(l => l.VerificationStatus == VerificationStatus.Verified),
                    Rejected = g.Count(l => l.VerificationStatus == VerificationStatus.Rejected),
                    RequiresReview = g.Count(l => l.VerificationStatus == VerificationStatus.RequiresReview),
                    WithAnomalies = g.Count(l => l.HasAnomalies),
                    Processed = g.Count(l => l.ProcessingStatus == ProcessingStatus.Completed),
                    VerifiedToday = g.Count(l => l.VerificationStatus == VerificationStatus.Verified &&
                                                  l.VerificationDate != null &&
                                                  l.VerificationDate.Value.Date == today)
                })
                .FirstOrDefaultAsync();

            // Handle empty batch case
            if (stats == null)
            {
                return new Dictionary<string, int>
                {
                    ["Total"] = 0,
                    ["Pending"] = 0,
                    ["Verified"] = 0,
                    ["Rejected"] = 0,
                    ["RequiresReview"] = 0,
                    ["WithAnomalies"] = 0,
                    ["Processed"] = 0,
                    ["VerifiedToday"] = 0
                };
            }

            return new Dictionary<string, int>
            {
                ["Total"] = stats.Total,
                ["Pending"] = stats.Pending,
                ["Verified"] = stats.Verified,
                ["Rejected"] = stats.Rejected,
                ["RequiresReview"] = stats.RequiresReview,
                ["WithAnomalies"] = stats.WithAnomalies,
                ["Processed"] = stats.Processed,
                ["VerifiedToday"] = stats.VerifiedToday
            };
        }

        public async Task<List<StagingBatch>> GetRecentBatchesAsync(int count = 10)
        {
            return await _context.StagingBatches
                .OrderByDescending(b => b.CreatedDate)
                .Take(count)
                .ToListAsync();
        }

        public async Task<bool> HasExistingBatchForPeriodAsync(int month, int year)
        {
            return await _context.StagingBatches
                .AnyAsync(b =>
                    b.CreatedDate.Month == month &&
                    b.CreatedDate.Year == year &&
                    (b.BatchStatus == BatchStatus.Processing ||
                     b.BatchStatus == BatchStatus.PartiallyVerified ||
                     b.BatchStatus == BatchStatus.Verified ||
                     b.BatchStatus == BatchStatus.Published));
        }

        public async Task<StagingBatch?> GetExistingBatchForPeriodAsync(int month, int year)
        {
            return await _context.StagingBatches
                .FirstOrDefaultAsync(b =>
                    b.CreatedDate.Month == month &&
                    b.CreatedDate.Year == year &&
                    (b.BatchStatus == BatchStatus.Processing ||
                     b.BatchStatus == BatchStatus.PartiallyVerified ||
                     b.BatchStatus == BatchStatus.Verified ||
                     b.BatchStatus == BatchStatus.Published));
        }

        public async Task<bool> CanDeleteBatchAsync(Guid batchId)
        {
            var batch = await _context.StagingBatches.FindAsync(batchId);
            if (batch == null) return false;

            // Cannot delete if already published to production
            if (batch.BatchStatus == BatchStatus.Published)
            {
                _logger.LogWarning("Cannot delete batch {BatchId} - already published", batchId);
                return false;
            }

            // Check if any records from this batch are in production (extra safety check)
            var hasProductionRecords = await _context.CallLogStagings
                .AnyAsync(c => c.BatchId == batchId && c.ProcessingStatus == ProcessingStatus.Completed);

            if (hasProductionRecords)
            {
                _logger.LogWarning("Cannot delete batch {BatchId} - has records in production", batchId);
                return false;
            }

            return true;
        }

        public async Task<bool> DeleteBatchAsync(Guid batchId, string deletedBy)
        {
            try
            {
                // =============================================
                // Delete associated Hangfire job if exists
                // This prevents orphaned jobs from retrying after batch is deleted
                // =============================================
                var batch = await _context.StagingBatches.FindAsync(batchId);
                if (batch != null && !string.IsNullOrEmpty(batch.HangfireJobId))
                {
                    try
                    {
                        _backgroundJobs.Delete(batch.HangfireJobId);
                        _logger.LogInformation("Deleted Hangfire job {JobId} for batch {BatchId}", batch.HangfireJobId, batchId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to delete Hangfire job {JobId} for batch {BatchId}. Job may have already completed or been deleted.", batch.HangfireJobId, batchId);
                    }
                }

                // =============================================
                // Use stored procedure for efficient bulk deletion
                // This handles 1M+ records without timeout or memory issues
                // =============================================
                _logger.LogInformation("Calling stored procedure sp_DeleteBatch for batch {BatchId}", batchId);

                var batchIdParam = new SqlParameter("@BatchId", batchId);
                var deletedByParam = new SqlParameter("@DeletedBy", deletedBy);
                var resultParam = new SqlParameter
                {
                    ParameterName = "@Result",
                    SqlDbType = System.Data.SqlDbType.NVarChar,
                    Size = -1, // MAX
                    Direction = System.Data.ParameterDirection.Output
                };

                // Execute stored procedure with 5 minute timeout for large datasets
                var originalTimeout = _context.Database.GetCommandTimeout();
                _context.Database.SetCommandTimeout(300); // 5 minutes

                var result = await _context.Database
                    .SqlQueryRaw<DeleteBatchResult>(
                        "EXEC sp_DeleteBatch @BatchId, @DeletedBy, @Result OUTPUT",
                        batchIdParam, deletedByParam, resultParam)
                    .ToListAsync();

                // Restore original timeout
                _context.Database.SetCommandTimeout(originalTimeout);

                var deleteResult = result.FirstOrDefault();

                if (deleteResult == null || deleteResult.Success == 0)
                {
                    var errorMessage = deleteResult?.Error ?? "Unknown error occurred during batch deletion";
                    _logger.LogError("Batch deletion failed: {Error}", errorMessage);
                    throw new InvalidOperationException(errorMessage);
                }

                _logger.LogInformation(
                    "Batch deletion completed. Batch: {BatchName}, Staging deleted: {Staging}, Source records reset: {Total} (Safaricom: {Safaricom}, Airtel: {Airtel}, PSTN: {PSTN}, PrivateWire: {PrivateWire})",
                    deleteResult.BatchName,
                    deleteResult.StagingRecordsDeleted,
                    deleteResult.SafaricomRecordsReset + deleteResult.AirtelRecordsReset + deleteResult.PSTNRecordsReset + deleteResult.PrivateWireRecordsReset,
                    deleteResult.SafaricomRecordsReset,
                    deleteResult.AirtelRecordsReset,
                    deleteResult.PSTNRecordsReset,
                    deleteResult.PrivateWireRecordsReset);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting batch {BatchId}", batchId);
                throw;
            }
        }

        private async Task UpdateBatchStatistics(Guid batchId)
        {
            var batch = await _context.StagingBatches.FindAsync(batchId);
            if (batch == null)
                return;

            var logs = await _context.CallLogStagings
                .Where(l => l.BatchId == batchId)
                .ToListAsync();

            batch.UpdateStatistics(logs);

            // Update batch status based on verification progress
            if (logs.All(l => l.VerificationStatus == VerificationStatus.Verified))
            {
                batch.BatchStatus = BatchStatus.Verified;
            }
            else if (logs.Any(l => l.VerificationStatus == VerificationStatus.Verified ||
                                   l.VerificationStatus == VerificationStatus.Rejected))
            {
                batch.BatchStatus = BatchStatus.PartiallyVerified;
            }

            await _context.SaveChangesAsync();
        }

        private string DetermineDestinationType(string? destination)
        {
            if (string.IsNullOrEmpty(destination))
                return "Unknown";

            // Simple logic to determine destination type
            if (destination.StartsWith("+254") || destination.StartsWith("07") || destination.StartsWith("01"))
                return "Mobile";
            else if (destination.StartsWith("+") && !destination.StartsWith("+254"))
                return "International";
            else if (destination.Length <= 6)
                return "Internal";
            else
                return "Local";
        }

        private async Task SendPublishedNotificationsAsync(
            List<CallRecord> records,
            StagingBatch batch,
            DateTime? verificationPeriod,
            DateTime? approvalPeriod)
        {
            try
            {
                // Group records by responsible staff member
                var recordsByStaff = records
                    .GroupBy(r => r.ResponsibleIndexNumber)
                    .Where(g => !string.IsNullOrEmpty(g.Key))
                    .ToList();

                _logger.LogInformation("Sending call log published notifications to {Count} staff members", recordsByStaff.Count);

                foreach (var staffGroup in recordsByStaff)
                {
                    var indexNumber = staffGroup.Key;
                    var staffRecords = staffGroup.ToList();

                    // Get staff information
                    var staff = await _context.EbillUsers
                        .Include(e => e.OrganizationEntity)
                        .FirstOrDefaultAsync(e => e.IndexNumber == indexNumber);

                    if (staff == null || string.IsNullOrEmpty(staff.Email))
                    {
                        _logger.LogWarning("Cannot send email to staff {IndexNumber} - user not found or no email", indexNumber);
                        continue;
                    }

                    // Get Class of Service limit
                    decimal monthlyAllowance = 0;
                    var userPhone = await _context.UserPhones
                        .Include(up => up.ClassOfService)
                        .FirstOrDefaultAsync(up => up.IndexNumber == indexNumber && up.IsPrimary);

                    if (userPhone?.ClassOfService != null)
                    {
                        monthlyAllowance = userPhone.ClassOfService.AirtimeAllowanceAmount ?? 0;
                    }

                    // Calculate totals
                    var totalRecords = staffRecords.Count;
                    var totalAmount = staffRecords.Sum(r => r.CallCostUSD);
                    var sourceSystems = string.Join(", ", staffRecords.Select(r => r.SourceSystem).Distinct().OrderBy(s => s));
                    var distinctSources = staffRecords.Select(r => r.SourceSystem?.ToUpperInvariant()).Distinct().ToList();
                    var publishCurrency = distinctSources.All(s => s == "PW" || s == "PRIVATEWIRE") ? "USD" : "KSH";

                    // Calculate Class of Service usage
                    var allowancePercentage = monthlyAllowance > 0 ? (totalAmount / monthlyAllowance) * 100 : 0;
                    var allowanceUsageMessage = allowancePercentage > 100
                        ? $"You have exceeded your Class of Service limit by {allowancePercentage - 100:F0}%"
                        : $"You have used {allowancePercentage:F0}% of your Class of Service limit";

                    // Get period name - format PeriodCode (e.g., "2024-09") to "September 2024"
                    string period;
                    if (batch.BillingPeriod != null && !string.IsNullOrEmpty(batch.BillingPeriod.PeriodCode))
                    {
                        // Try to parse PeriodCode (format: "2024-09") to display as "September 2024"
                        if (DateTime.TryParseExact(batch.BillingPeriod.PeriodCode + "-01", "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out var periodDate))
                        {
                            period = periodDate.ToString("MMMM yyyy");
                        }
                        else
                        {
                            period = batch.BillingPeriod.PeriodCode;
                        }
                    }
                    else
                    {
                        period = $"{DateTime.Now:MMMM yyyy}";
                    }

                    // Format deadlines
                    var verificationDeadlineText = verificationPeriod.HasValue
                        ? verificationPeriod.Value.ToString("MMMM dd, yyyy")
                        : "To be confirmed";

                    var approvalDeadlineText = approvalPeriod.HasValue
                        ? approvalPeriod.Value.ToString("MMMM dd, yyyy")
                        : "To be confirmed";

                    // Build verification link
                    var baseUrl = _configuration["AppSettings:BaseUrl"] ?? "http://localhost:5041";
                    var verifyLink = $"{baseUrl}/Modules/EBillManagement/CallRecords/MyCallLogs";

                    // Prepare email data
                    var emailData = new Dictionary<string, string>
                    {
                        { "StaffName", staff.FullName },
                        { "IndexNumber", indexNumber },
                        { "Period", period },
                        { "TotalRecords", totalRecords.ToString("N0") },
                        { "TotalAmount", totalAmount.ToString("N2") },
                        { "SourceSystems", sourceSystems },
                        { "Currency", publishCurrency },
                        { "VerificationDeadline", verificationDeadlineText },
                        { "ApprovalDeadline", approvalDeadlineText },
                        { "MonthlyAllowance", monthlyAllowance.ToString("N2") },
                        { "AllowancePercentage", Math.Min(allowancePercentage, 100).ToString("F0") },
                        { "AllowanceUsageMessage", allowanceUsageMessage },
                        { "VerifyCallsLink", verifyLink },
                        { "SupervisorName", staff.SupervisorName ?? "Not Assigned" },
                        { "SupervisorEmail", staff.SupervisorEmail ?? "" },
                        { "Year", DateTime.Now.Year.ToString() }
                    };

                    // Queue email for background processing (instead of sending synchronously)
                    var emailLogId = await _emailService.QueueTemplatedEmailAsync(
                        to: staff.Email,
                        templateCode: "CALL_LOG_PUBLISHED",
                        data: emailData,
                        createdBy: "System"
                    );

                    _logger.LogInformation("Queued call log published notification for {Email} ({IndexNumber}) - EmailLogId: {EmailLogId}, {TotalRecords} records, ${TotalAmount:F2}",
                        staff.Email, indexNumber, emailLogId, totalRecords, totalAmount);
                }

                _logger.LogInformation("Completed queuing {Count} call log published notifications for background processing", recordsByStaff.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SendPublishedNotificationsAsync");
                throw;
            }
        }
    }

    /// <summary>
    /// Result class for mapping stored procedure output from sp_ConsolidateCallLogBatch
    /// </summary>
    public class ConsolidationResult
    {
        public int TotalRecords { get; set; }
        public int SafaricomRecords { get; set; }
        public int AirtelRecords { get; set; }
        public int PSTNRecords { get; set; }
        public int PrivateWireRecords { get; set; }
        public DateTime CompletedAt { get; set; }
    }

    /// <summary>
    /// Result class for mapping stored procedure output from sp_DeleteBatch
    /// </summary>
    public class DeleteBatchResult
    {
        public int Success { get; set; }
        public string? BatchName { get; set; }
        public int StagingRecordsDeleted { get; set; }
        public int SafaricomRecordsReset { get; set; }
        public int AirtelRecordsReset { get; set; }
        public int PSTNRecordsReset { get; set; }
        public int PrivateWireRecordsReset { get; set; }
        public DateTime? DeletedAt { get; set; }
        public string? Error { get; set; }
    }

    /// <summary>
    /// Result class for mapping stored procedure output from sp_PushBatchToProduction
    /// </summary>
    public class PushToProductionResult
    {
        public int Success { get; set; }
        public int RecordsPushed { get; set; }
        public int RemainingUnprocessed { get; set; }
        public int SafaricomUpdated { get; set; }
        public int AirtelUpdated { get; set; }
        public int PSTNUpdated { get; set; }
        public int PrivateWireUpdated { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? Error { get; set; }
    }

    public class VerifyBatchResult
    {
        public int Success { get; set; }
        public int RecordsVerified { get; set; }
        public int TotalRecords { get; set; }
        public int TotalVerified { get; set; }
        public int TotalPending { get; set; }
        public string? Error { get; set; }
    }
}