using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TAB.Web.Data;
using TAB.Web.Models;

namespace TAB.Web.Services
{
    public class CallLogStagingService : ICallLogStagingService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CallLogStagingService> _logger;

        public CallLogStagingService(ApplicationDbContext context, ILogger<CallLogStagingService> logger)
        {
            _context = context;
            _logger = logger;
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

            try
            {
                batch.BatchStatus = BatchStatus.Processing;
                batch.StartProcessingDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                int totalImported = 0;

                // Import from each source
                totalImported += await ImportFromSafaricomAsync(batch.Id, startDate, endDate);
                totalImported += await ImportFromAirtelAsync(batch.Id, startDate, endDate);
                totalImported += await ImportFromPSTNAsync(batch.Id, startDate, endDate);
                totalImported += await ImportFromPrivateWireAsync(batch.Id, startDate, endDate);

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

                var stagingRecord = new CallLogStaging
                {
                    ExtensionNumber = r.Ext ?? string.Empty,
                    CallDate = r.CallDate ?? DateTime.MinValue,
                    CallNumber = r.Dialed ?? string.Empty,
                    CallDestination = r.Dest ?? string.Empty,
                    CallEndTime = (r.CallDate ?? DateTime.MinValue).AddSeconds((int)((r.Dur ?? 0) * 60)), // Duration is in minutes
                    CallDuration = (int)((r.Dur ?? 0) * 60), // Convert minutes to seconds
                    CallCurrencyCode = "KES",
                    CallCost = r.Cost ?? 0,
                    CallCostUSD = (r.Cost ?? 0) / 150m, // Approximate conversion
                    CallCostKSHS = r.Cost ?? 0,
                    CallType = r.CallType ?? "Voice",
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

                var stagingRecord = new CallLogStaging
                {
                    ExtensionNumber = r.Ext ?? string.Empty,
                    CallDate = r.CallDate ?? DateTime.MinValue,
                    CallNumber = r.Dialed ?? string.Empty,
                    CallDestination = r.Dest ?? string.Empty,
                    CallEndTime = (r.CallDate ?? DateTime.MinValue).AddSeconds((int)((r.Dur ?? 0) * 60)),
                    CallDuration = (int)((r.Dur ?? 0) * 60),
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
            var logs = await _context.CallLogStagings
                .Where(l => l.BatchId == batchId)
                .ToListAsync();

            int anomalyCount = 0;

            foreach (var log in logs)
            {
                var anomalies = await DetectAnomaliesAsync(log.Id);
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

            await _context.SaveChangesAsync();
            return anomalyCount;
        }

        public async Task<List<CallLogAnomaly>> DetectAnomaliesAsync(int stagingId)
        {
            var log = await _context.CallLogStagings
                .Include(l => l.ResponsibleUser)
                .Include(l => l.UserPhone)
                .FirstOrDefaultAsync(l => l.Id == stagingId);

            if (log == null)
                return new List<CallLogAnomaly>();

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
                // Check if user exists and is active
                var user = await _context.EbillUsers
                    .FirstOrDefaultAsync(u => u.IndexNumber == log.ResponsibleIndexNumber);

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

            // Check for high cost
            if (log.CallCostUSD > 100)
            {
                anomalies.Add(new CallLogAnomaly
                {
                    Code = AnomalyCodes.HighCost,
                    Name = "High Cost Call",
                    Description = $"Call cost ${log.CallCostUSD:F2} exceeds threshold",
                    Severity = SeverityLevel.High,
                    Details = new Dictionary<string, object> { ["cost_usd"] = log.CallCostUSD }
                });
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

            // Check for excessive duration
            if (log.CallDuration > 14400) // 4 hours
            {
                anomalies.Add(new CallLogAnomaly
                {
                    Code = AnomalyCodes.ExcessiveDuration,
                    Name = "Excessive Duration",
                    Description = $"Call duration {log.CallDuration / 3600:F1} hours exceeds limit",
                    Severity = SeverityLevel.Medium
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

        public async Task<int> PushToProductionAsync(Guid batchId, DateTime? verificationPeriod = null)
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

            // Get only verified records to push to production
            var verifiedLogs = await _context.CallLogStagings
                .Where(l => l.BatchId == batchId && l.VerificationStatus == VerificationStatus.Verified)
                .ToListAsync();

            if (verifiedLogs.Count == 0)
            {
                _logger.LogWarning("Cannot push batch {BatchId} - no verified records found", batchId);
                return 0;
            }

            var productionRecords = verifiedLogs.Select(l => new CallRecord
            {
                ExtensionNumber = l.ExtensionNumber,
                CallDate = l.CallDate,
                CallNumber = l.CallNumber,
                CallDestination = l.CallDestination,
                CallEndTime = l.CallEndTime,
                CallDuration = l.CallDuration,
                CallCurrencyCode = l.CallCurrencyCode,
                CallCost = l.CallCost,
                CallCostUSD = l.CallCostUSD,
                CallCostKSHS = l.CallCostKSHS,
                CallType = l.CallType,
                CallDestinationType = l.CallDestinationType,
                CallYear = billingYear,  // Use billing period year
                CallMonth = billingMonth,  // Use billing period month
                ResponsibleIndexNumber = l.ResponsibleIndexNumber,
                PayingIndexNumber = l.PayingIndexNumber,
                UserPhoneId = l.UserPhoneId,  // Include UserPhone relationship
                AssignmentStatus = "None",  // Initial status - belongs to original phone owner
                IsVerified = false,  // Set to false initially for user verification workflow
                VerificationDate = null,
                VerificationPeriod = verificationPeriod,  // Set the verification period deadline
                IsCertified = false,
                IsProcessed = false,
                EntryDate = DateTime.UtcNow,
                SourceSystem = l.SourceSystem,
                SourceBatchId = l.BatchId,
                SourceStagingId = l.Id
            }).ToList();

            _context.CallRecords.AddRange(productionRecords);

            // Update staging records as processed
            foreach (var log in verifiedLogs)
            {
                log.ProcessingStatus = ProcessingStatus.Completed;
                log.ProcessedDate = DateTime.UtcNow;
            }

            // Update source telecom tables to mark records as Completed
            // Get the source record IDs from verified logs
            var sourceRecordIdsByType = verifiedLogs
                .GroupBy(l => l.SourceSystem)
                .ToDictionary(g => g.Key, g => g.Select(l => l.SourceRecordId).ToList());

            foreach (var sourceType in sourceRecordIdsByType)
            {
                var sourceSystem = sourceType.Key;
                var sourceRecordIds = sourceType.Value;

                _logger.LogInformation("Updating {Count} {SourceSystem} source records to Completed status",
                    sourceRecordIds.Count, sourceSystem);

                switch (sourceSystem.ToLower())
                {
                    case "pstn":
                        var pstnRecords = await _context.PSTNs
                            .Where(p => sourceRecordIds.Contains(p.Id.ToString()))
                            .ToListAsync();
                        foreach (var record in pstnRecords)
                        {
                            record.ProcessingStatus = ProcessingStatus.Completed;
                            record.ProcessedDate = DateTime.UtcNow;
                        }
                        break;

                    case "privatewire":
                        var privateWireRecords = await _context.PrivateWires
                            .Where(p => sourceRecordIds.Contains(p.Id.ToString()))
                            .ToListAsync();
                        foreach (var record in privateWireRecords)
                        {
                            record.ProcessingStatus = ProcessingStatus.Completed;
                            record.ProcessedDate = DateTime.UtcNow;
                        }
                        break;

                    case "safaricom":
                        var safaricomRecords = await _context.Safaricoms
                            .Where(s => sourceRecordIds.Contains(s.Id.ToString()))
                            .ToListAsync();
                        foreach (var record in safaricomRecords)
                        {
                            record.ProcessingStatus = ProcessingStatus.Completed;
                            record.ProcessedDate = DateTime.UtcNow;
                        }
                        break;

                    case "airtel":
                        var airtelRecords = await _context.Airtels
                            .Where(a => sourceRecordIds.Contains(a.Id.ToString()))
                            .ToListAsync();
                        foreach (var record in airtelRecords)
                        {
                            record.ProcessingStatus = ProcessingStatus.Completed;
                            record.ProcessedDate = DateTime.UtcNow;
                        }
                        break;

                    default:
                        _logger.LogWarning("Unknown source system: {SourceSystem}", sourceSystem);
                        break;
                }
            }

            // Check if there are any unverified/rejected records remaining in the batch
            var remainingUnprocessedCount = await _context.CallLogStagings
                .CountAsync(l => l.BatchId == batchId &&
                               l.VerificationStatus != VerificationStatus.Verified &&
                               l.ProcessingStatus != ProcessingStatus.Completed);

            // Update batch status based on remaining records
            if (remainingUnprocessedCount == 0)
            {
                // All records processed - mark batch as Published
                batch.BatchStatus = BatchStatus.Published;
                batch.EndProcessingDate = DateTime.UtcNow;
                _logger.LogInformation("Batch {BatchId} fully published - all records processed", batchId);
            }
            else
            {
                // Some records remain unverified/rejected - keep as PartiallyVerified
                batch.BatchStatus = BatchStatus.PartiallyVerified;
                _logger.LogInformation("Batch {BatchId} partially published - {Count} verified records pushed, {Remaining} records remain unverified/rejected",
                    batchId, verifiedLogs.Count, remainingUnprocessedCount);
            }

            batch.PublishedBy = "System";

            await _context.SaveChangesAsync();

            _logger.LogInformation("Pushed {Count} verified records to production from batch {BatchId}",
                productionRecords.Count, batchId);

            return productionRecords.Count;
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
                query = query.Where(l =>
                    l.ExtensionNumber.Contains(filter.SearchTerm) ||
                    l.CallNumber.Contains(filter.SearchTerm) ||
                    l.CallDestination.Contains(filter.SearchTerm) ||
                    (l.ResponsibleIndexNumber != null && l.ResponsibleIndexNumber.Contains(filter.SearchTerm)));
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
            return await _context.StagingBatches
                .Include(b => b.CallLogs)
                .FirstOrDefaultAsync(b => b.Id == batchId);
        }

        public async Task<Dictionary<string, int>> GetBatchStatisticsAsync(Guid batchId)
        {
            var logs = await _context.CallLogStagings
                .Where(l => l.BatchId == batchId)
                .ToListAsync();

            return new Dictionary<string, int>
            {
                ["Total"] = logs.Count,
                ["Pending"] = logs.Count(l => l.VerificationStatus == VerificationStatus.Pending),
                ["Verified"] = logs.Count(l => l.VerificationStatus == VerificationStatus.Verified),
                ["Rejected"] = logs.Count(l => l.VerificationStatus == VerificationStatus.Rejected),
                ["RequiresReview"] = logs.Count(l => l.VerificationStatus == VerificationStatus.RequiresReview),
                ["WithAnomalies"] = logs.Count(l => l.HasAnomalies),
                ["Processed"] = logs.Count(l => l.ProcessingStatus == ProcessingStatus.Completed)
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
            // Use the execution strategy for handling retries with transactions
            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // First check if we can delete
                    if (!await CanDeleteBatchAsync(batchId))
                    {
                        throw new InvalidOperationException("This batch cannot be deleted. It may be published or have records in production.");
                    }

                    var batch = await _context.StagingBatches.FindAsync(batchId);
                    if (batch == null)
                    {
                        throw new InvalidOperationException($"Batch {batchId} not found.");
                    }

                    _logger.LogInformation("Deleting batch {BatchName} (ID: {BatchId}) by {DeletedBy}",
                        batch.BatchName, batchId, deletedBy);

                    // Get all staging records for this batch
                    var stagingRecords = await _context.CallLogStagings
                        .Where(c => c.BatchId == batchId)
                        .ToListAsync();

                    _logger.LogInformation("Found {Count} staging records to delete", stagingRecords.Count);

                    // Create audit log entry
                    var auditLog = new AuditLog
                    {
                        EntityType = "StagingBatch",
                        EntityId = batchId.ToString(),
                        Action = "Deleted",
                        Description = $"Deleted batch '{batch.BatchName}' with {stagingRecords.Count} staging records",
                        OldValues = System.Text.Json.JsonSerializer.Serialize(new
                        {
                            batch.BatchName,
                            batch.BatchStatus,
                            batch.TotalRecords,
                            batch.VerifiedRecords,
                            batch.RejectedRecords,
                            batch.RecordsWithAnomalies,
                            batch.CreatedDate,
                            batch.CreatedBy
                        }),
                        PerformedBy = deletedBy,
                        PerformedDate = DateTime.UtcNow,
                        Module = "CallLogStaging",
                        IsSuccess = true,
                        AdditionalData = System.Text.Json.JsonSerializer.Serialize(new
                        {
                            RecordsDeleted = stagingRecords.Count,
                            SourceSystems = batch.SourceSystems
                        })
                    };

                    _context.AuditLogs.Add(auditLog);

                    // Delete all staging records
                    _context.CallLogStagings.RemoveRange(stagingRecords);

                    // Reset source records StagingBatchId back to null (Unverified state)
                    // Update all telecom tables that have this StagingBatchId
                    _logger.LogInformation("Resetting StagingBatchId to null for all records with batch {BatchId}", batchId);

                    // Update PSTN records
                    var pstnRecords = await _context.PSTNs
                        .Where(p => p.StagingBatchId == batchId)
                        .ToListAsync();
                    foreach (var record in pstnRecords)
                    {
                        record.StagingBatchId = null;
                        _context.PSTNs.Update(record);
                    }
                    if (pstnRecords.Count > 0)
                        _logger.LogInformation("Reset {RecordCount} PSTN records to unverified state", pstnRecords.Count);

                    // Update PrivateWire records
                    var privateWireRecords = await _context.PrivateWires
                        .Where(p => p.StagingBatchId == batchId)
                        .ToListAsync();
                    foreach (var record in privateWireRecords)
                    {
                        record.StagingBatchId = null;
                        _context.PrivateWires.Update(record);
                    }
                    if (privateWireRecords.Count > 0)
                        _logger.LogInformation("Reset {RecordCount} PrivateWire records to unverified state", privateWireRecords.Count);

                    // Update Safaricom records
                    var safaricomRecords = await _context.Safaricoms
                        .Where(s => s.StagingBatchId == batchId)
                        .ToListAsync();
                    foreach (var record in safaricomRecords)
                    {
                        record.StagingBatchId = null;
                        _context.Safaricoms.Update(record);
                    }
                    if (safaricomRecords.Count > 0)
                        _logger.LogInformation("Reset {RecordCount} Safaricom records to unverified state", safaricomRecords.Count);

                    // Update Airtel records
                    var airtelRecords = await _context.Airtels
                        .Where(a => a.StagingBatchId == batchId)
                        .ToListAsync();
                    foreach (var record in airtelRecords)
                    {
                        record.StagingBatchId = null;
                        _context.Airtels.Update(record);
                    }
                    if (airtelRecords.Count > 0)
                        _logger.LogInformation("Reset {RecordCount} Airtel records to unverified state", airtelRecords.Count);

                    // Delete the batch itself
                    _context.StagingBatches.Remove(batch);

                    // Save all changes
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    _logger.LogInformation("Successfully deleted batch {BatchName} and {Count} staging records",
                        batch.BatchName, stagingRecords.Count);

                    return true;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Error deleting batch {BatchId}", batchId);
                    throw;
                }
            });
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
    }
}