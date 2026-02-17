using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Hangfire;
using Hangfire.Server;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TAB.Web.Data;
using TAB.Web.Models;

namespace TAB.Web.Services
{
    public interface IBulkImportService
    {
        Task<ImportResult> ImportSafaricomAsync(Guid jobId, string filePath, int billingMonth, int billingYear, string dateFormat, IJobCancellationToken cancellationToken);
        Task<ImportResult> ImportAirtelAsync(Guid jobId, string filePath, int billingMonth, int billingYear, string dateFormat, IJobCancellationToken cancellationToken);
        Task<ImportResult> ImportPSTNAsync(Guid jobId, string filePath, int billingMonth, int billingYear, string dateFormat, IJobCancellationToken cancellationToken);
        Task<ImportResult> ImportPrivateWireAsync(Guid jobId, string filePath, int billingMonth, int billingYear, string dateFormat, IJobCancellationToken cancellationToken);
    }

    public class BulkImportService : IBulkImportService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<BulkImportService> _logger;

        public BulkImportService(ApplicationDbContext context, ILogger<BulkImportService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // PUBLIC METHODS - One for each call log type

        public async Task<ImportResult> ImportSafaricomAsync(Guid jobId, string filePath, int billingMonth, int billingYear, string dateFormat, IJobCancellationToken cancellationToken)
        {
            return await ImportSafaricomAirtelInternalAsync(jobId, "Safaricom", filePath, billingMonth, billingYear, dateFormat, cancellationToken);
        }

        public async Task<ImportResult> ImportAirtelAsync(Guid jobId, string filePath, int billingMonth, int billingYear, string dateFormat, IJobCancellationToken cancellationToken)
        {
            return await ImportSafaricomAirtelInternalAsync(jobId, "Airtel", filePath, billingMonth, billingYear, dateFormat, cancellationToken);
        }

        public async Task<ImportResult> ImportPSTNAsync(Guid jobId, string filePath, int billingMonth, int billingYear, string dateFormat, IJobCancellationToken cancellationToken)
        {
            const int BATCH_SIZE = 50000;
            var result = new ImportResult { StartTime = DateTime.UtcNow };

            try
            {
                _logger.LogInformation("Starting PSTN import for job {JobId}: {FilePath}", jobId, filePath);

                // Update job status to Processing
                await UpdateJobStatus(jobId, "Processing", null, null, null);

                // Pre-load user phone lookups
                var userPhoneLookup = await LoadUserPhoneLookup(cancellationToken.ShutdownToken);

                using var stream = File.OpenRead(filePath);
                using var reader = new StreamReader(stream);

                var headerLine = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(headerLine))
                {
                    throw new InvalidOperationException("CSV file is empty");
                }

                var headers = ParseCsvLine(headerLine);
                var columnIndices = BuildColumnIndex(headers);

                // Validate required columns for PSTN
                var requiredColumns = new[] { "extension", "dialednumber", "calldate", "amountksh" };
                var missingColumns = new List<string>();
                foreach (var required in requiredColumns)
                {
                    if (!columnIndices.ContainsKey(required.ToLower()))
                    {
                        missingColumns.Add(required);
                    }
                }

                if (missingColumns.Any())
                {
                    var foundColumns = string.Join(", ", columnIndices.Keys);
                    var missing = string.Join(", ", missingColumns);
                    throw new InvalidOperationException(
                        $"Required columns missing: {missing}. " +
                        $"Found columns in CSV: {foundColumns}. " +
                        $"Please ensure your CSV file has the correct headers (case-insensitive): Extension, DialedNumber, CallDate, AmountKSH"
                    );
                }

                // Create DataTable for PSTN (Pascal case columns)
                var dataTable = CreatePSTNDataTable();
                var lineNumber = 1;
                var batchCount = 0;

                while (!reader.EndOfStream)
                {
                    cancellationToken.ShutdownToken.ThrowIfCancellationRequested();

                    lineNumber++;
                    var line = await reader.ReadLineAsync();
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    var values = ParseCsvLine(line);
                    if (values.All(v => string.IsNullOrWhiteSpace(v))) continue;

                    try
                    {
                        var row = dataTable.NewRow();
                        var extension = NormalizePhoneNumber(GetValue(values, columnIndices, "extension"));

                        // Fast lookup from pre-loaded dictionary
                        var phoneFound = userPhoneLookup.TryGetValue(extension, out var userPhone);

                        // Get IndexNumber from CSV or UserPhone
                        var indexNumber = GetValue(values, columnIndices, "indexnumber")
                            ?? (phoneFound ? userPhone.IndexNumber : null)
                            ?? "";
                        int? ebillUserId = phoneFound ? userPhone.EbillUserId : null;

                        // Calculate billing period string
                        var billingPeriodDate = new DateTime(billingYear, billingMonth, 1);
                        var billingPeriodString = billingPeriodDate.ToString("yyyy-MM-dd");

                        // Populate row with Pascal case column names (PSTN schema)
                        row["Extension"] = extension;
                        row["DialedNumber"] = GetValue(values, columnIndices, "dialednumber") ?? "";
                        row["CallTime"] = ParseTimeSpan(GetValue(values, columnIndices, "calltime"));
                        row["Destination"] = GetValue(values, columnIndices, "destination")
                            ?? GetValue(values, columnIndices, "dialednumber") ?? "";
                        row["DestinationLine"] = GetValue(values, columnIndices, "destinationline") ?? "";
                        row["DurationExtended"] = ParseDurationToMinutes(GetValue(values, columnIndices, "durationextended"));
                        row["CallDate"] = ParseDate(GetValue(values, columnIndices, "calldate"), dateFormat);
                        row["Duration"] = ParseDurationToMinutes(GetValue(values, columnIndices, "duration"));
                        row["AmountKSH"] = ParseDecimalOrZero(GetValue(values, columnIndices, "amountksh"));
                        row["AmountUSD"] = ParseDecimalOrZero(GetValue(values, columnIndices, "amountusd"));
                        row["IndexNumber"] = indexNumber;
                        row["Carrier"] = GetValue(values, columnIndices, "carrier") ?? "";
                        row["UserPhoneId"] = phoneFound ? (object)userPhone.Id : DBNull.Value;
                        row["EbillUserId"] = ebillUserId ?? (object)DBNull.Value;
                        row["BillingPeriod"] = billingPeriodString;
                        row["CallMonth"] = billingMonth;
                        row["CallYear"] = billingYear;
                        row["CreatedDate"] = DateTime.UtcNow;
                        row["CreatedBy"] = "BulkImport";
                        row["ProcessingStatus"] = 0; // ProcessingStatus.Staged
                        row["ImportJobId"] = jobId;

                        dataTable.Rows.Add(row);
                        result.TotalRecords++;

                        // Bulk insert when batch is full
                        if (dataTable.Rows.Count >= BATCH_SIZE)
                        {
                            await BulkInsertToDatabase(dataTable, "PSTNs", cancellationToken.ShutdownToken);
                            batchCount++;
                            result.SuccessCount += dataTable.Rows.Count;

                            _logger.LogInformation(
                                "Batch {BatchNum} completed: {Count} records inserted, Total: {Total}",
                                batchCount, dataTable.Rows.Count, result.SuccessCount);

                            // Update job progress
                            var progress = (result.SuccessCount * 100) / Math.Max(result.TotalRecords, 1);
                            await UpdateJobStatus(jobId, "Processing", result.SuccessCount, null, progress);

                            dataTable.Clear();
                        }
                    }
                    catch (Exception ex)
                    {
                        result.ErrorCount++;
                        result.Errors.Add($"Line {lineNumber}: {ex.Message}");
                        _logger.LogWarning("Error on line {Line}: {Error}", lineNumber, ex.Message);
                    }
                }

                // Insert remaining records
                if (dataTable.Rows.Count > 0)
                {
                    await BulkInsertToDatabase(dataTable, "PSTNs", cancellationToken.ShutdownToken);
                    result.SuccessCount += dataTable.Rows.Count;

                    _logger.LogInformation(
                        "Final batch: {Count} records inserted, Total: {Total}",
                        dataTable.Rows.Count, result.SuccessCount);
                }

                result.EndTime = DateTime.UtcNow;

                // Update job as completed
                var duration = (int)(result.EndTime.Value - result.StartTime).TotalSeconds;
                await UpdateJobStatus(jobId, "Completed", result.SuccessCount, result.ErrorCount, 100, duration);

                _logger.LogInformation(
                    "PSTN import completed: {Success} success, {Errors} errors in {Duration}s",
                    result.SuccessCount, result.ErrorCount, duration);

                // Clean up temp file
                try { File.Delete(filePath); } catch { }

                return result;
            }
            catch (Exception ex)
            {
                result.EndTime = DateTime.UtcNow;
                result.ErrorCount++;
                result.Errors.Add(ex.Message);

                _logger.LogError(ex, "PSTN import failed for job {JobId}", jobId);

                // Update job as failed
                await UpdateJobStatus(jobId, "Failed", result.SuccessCount, result.ErrorCount, null, null, ex.Message);

                throw;
            }
        }

        public async Task<ImportResult> ImportPrivateWireAsync(Guid jobId, string filePath, int billingMonth, int billingYear, string dateFormat, IJobCancellationToken cancellationToken)
        {
            const int BATCH_SIZE = 50000;
            var result = new ImportResult { StartTime = DateTime.UtcNow };

            try
            {
                _logger.LogInformation("Starting PrivateWire import for job {JobId}: {FilePath}", jobId, filePath);

                // Update job status to Processing
                await UpdateJobStatus(jobId, "Processing", null, null, null);

                // Pre-load user phone lookups
                var userPhoneLookup = await LoadUserPhoneLookup(cancellationToken.ShutdownToken);

                using var stream = File.OpenRead(filePath);
                using var reader = new StreamReader(stream);

                var headerLine = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(headerLine))
                {
                    throw new InvalidOperationException("CSV file is empty");
                }

                var headers = ParseCsvLine(headerLine);
                var columnIndices = BuildColumnIndex(headers);

                // Validate required columns for PrivateWire
                var requiredColumns = new[] { "extension", "dialednumber", "calldate", "amountksh" };
                var missingColumns = new List<string>();
                foreach (var required in requiredColumns)
                {
                    if (!columnIndices.ContainsKey(required.ToLower()))
                    {
                        missingColumns.Add(required);
                    }
                }

                if (missingColumns.Any())
                {
                    var foundColumns = string.Join(", ", columnIndices.Keys);
                    var missing = string.Join(", ", missingColumns);
                    throw new InvalidOperationException(
                        $"Required columns missing: {missing}. " +
                        $"Found columns in CSV: {foundColumns}. " +
                        $"Please ensure your CSV file has the correct headers (case-insensitive): Extension, DialedNumber, CallDate, AmountKSH"
                    );
                }

                // Create DataTable for PrivateWire (Pascal case columns)
                var dataTable = CreatePrivateWireDataTable();
                var lineNumber = 1;
                var batchCount = 0;

                while (!reader.EndOfStream)
                {
                    cancellationToken.ShutdownToken.ThrowIfCancellationRequested();

                    lineNumber++;
                    var line = await reader.ReadLineAsync();
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    var values = ParseCsvLine(line);
                    if (values.All(v => string.IsNullOrWhiteSpace(v))) continue;

                    try
                    {
                        var row = dataTable.NewRow();
                        var extension = NormalizePhoneNumber(GetValue(values, columnIndices, "extension"));

                        // Fast lookup from pre-loaded dictionary
                        var phoneFound = userPhoneLookup.TryGetValue(extension, out var userPhone);

                        // Get IndexNumber from CSV or UserPhone
                        var indexNumber = GetValue(values, columnIndices, "indexnumber")
                            ?? (phoneFound ? userPhone.IndexNumber : null)
                            ?? "";
                        int? ebillUserId = phoneFound ? userPhone.EbillUserId : null;

                        // Calculate billing period string
                        var billingPeriodDate = new DateTime(billingYear, billingMonth, 1);
                        var billingPeriodString = billingPeriodDate.ToString("yyyy-MM-dd");

                        // Populate row with Pascal case column names (PrivateWire schema)
                        row["Extension"] = extension;
                        row["DialedNumber"] = GetValue(values, columnIndices, "dialednumber") ?? "";
                        row["CallTime"] = ParseTimeSpan(GetValue(values, columnIndices, "calltime"));
                        row["Destination"] = GetValue(values, columnIndices, "destination")
                            ?? GetValue(values, columnIndices, "dialednumber") ?? "";
                        row["DestinationLine"] = GetValue(values, columnIndices, "destinationline") ?? "";
                        row["DurationExtended"] = ParseDurationToMinutes(GetValue(values, columnIndices, "durationextended"));
                        row["CallDate"] = ParseDate(GetValue(values, columnIndices, "calldate"), dateFormat);
                        row["Duration"] = ParseDurationToMinutes(GetValue(values, columnIndices, "duration"));
                        row["AmountKSH"] = ParseDecimalOrZero(GetValue(values, columnIndices, "amountksh"));
                        row["AmountUSD"] = ParseDecimalOrZero(GetValue(values, columnIndices, "amountusd"));
                        row["IndexNumber"] = indexNumber;
                        row["UserPhoneId"] = phoneFound ? (object)userPhone.Id : DBNull.Value;
                        row["EbillUserId"] = ebillUserId ?? (object)DBNull.Value;
                        row["BillingPeriod"] = billingPeriodString;
                        row["CallMonth"] = billingMonth;
                        row["CallYear"] = billingYear;
                        row["CreatedDate"] = DateTime.UtcNow;
                        row["CreatedBy"] = "BulkImport";
                        row["ProcessingStatus"] = 0; // ProcessingStatus.Staged
                        row["ImportJobId"] = jobId;

                        dataTable.Rows.Add(row);
                        result.TotalRecords++;

                        // Bulk insert when batch is full
                        if (dataTable.Rows.Count >= BATCH_SIZE)
                        {
                            await BulkInsertToDatabase(dataTable, "PrivateWires", cancellationToken.ShutdownToken);
                            batchCount++;
                            result.SuccessCount += dataTable.Rows.Count;

                            _logger.LogInformation(
                                "Batch {BatchNum} completed: {Count} records inserted, Total: {Total}",
                                batchCount, dataTable.Rows.Count, result.SuccessCount);

                            // Update job progress
                            var progress = (result.SuccessCount * 100) / Math.Max(result.TotalRecords, 1);
                            await UpdateJobStatus(jobId, "Processing", result.SuccessCount, null, progress);

                            dataTable.Clear();
                        }
                    }
                    catch (Exception ex)
                    {
                        result.ErrorCount++;
                        result.Errors.Add($"Line {lineNumber}: {ex.Message}");
                        _logger.LogWarning("Error on line {Line}: {Error}", lineNumber, ex.Message);
                    }
                }

                // Insert remaining records
                if (dataTable.Rows.Count > 0)
                {
                    await BulkInsertToDatabase(dataTable, "PrivateWires", cancellationToken.ShutdownToken);
                    result.SuccessCount += dataTable.Rows.Count;

                    _logger.LogInformation(
                        "Final batch: {Count} records inserted, Total: {Total}",
                        dataTable.Rows.Count, result.SuccessCount);
                }

                result.EndTime = DateTime.UtcNow;

                // Update job as completed
                var duration = (int)(result.EndTime.Value - result.StartTime).TotalSeconds;
                await UpdateJobStatus(jobId, "Completed", result.SuccessCount, result.ErrorCount, 100, duration);

                _logger.LogInformation(
                    "PrivateWire import completed: {Success} success, {Errors} errors in {Duration}s",
                    result.SuccessCount, result.ErrorCount, duration);

                // Clean up temp file
                try { File.Delete(filePath); } catch { }

                return result;
            }
            catch (Exception ex)
            {
                result.EndTime = DateTime.UtcNow;
                result.ErrorCount++;
                result.Errors.Add(ex.Message);

                _logger.LogError(ex, "PrivateWire import failed for job {JobId}", jobId);

                // Update job as failed
                await UpdateJobStatus(jobId, "Failed", result.SuccessCount, result.ErrorCount, null, null, ex.Message);

                throw;
            }
        }

        // INTERNAL IMPLEMENTATION - Shared by Safaricom and Airtel (identical schemas)

        private async Task<ImportResult> ImportSafaricomAirtelInternalAsync(Guid jobId, string tableName, string filePath, int billingMonth, int billingYear, string dateFormat, IJobCancellationToken cancellationToken)
        {
            const int BATCH_SIZE = 50000;
            var result = new ImportResult { StartTime = DateTime.UtcNow };

            try
            {
                _logger.LogInformation("Starting {TableName} import for job {JobId}: {FilePath}", tableName, jobId, filePath);

                // Update job status to Processing
                await UpdateJobStatus(jobId, "Processing", null, null, null);

                // Pre-load user phone lookups with EbillUser relationship
                var userPhoneLookup = await LoadUserPhoneLookup(cancellationToken.ShutdownToken);

                using var stream = File.OpenRead(filePath);
                using var reader = new StreamReader(stream);

                var headerLine = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(headerLine))
                {
                    throw new InvalidOperationException("CSV file is empty");
                }

                var headers = ParseCsvLine(headerLine);
                var columnIndices = BuildColumnIndex(headers);

                // Validate required columns for Safaricom/Airtel (accepts common aliases)
                var requiredColumnsWithAliases = new Dictionary<string, string[]>
                {
                    { "ext", new[] { "ext", "callingno", "calling no", "msisdn", "phone" } },
                    { "call_date", new[] { "call_date", "date", "call date" } },
                    { "dialed", new[] { "dialed", "dialedno", "dialled", "dialled no", "number" } },
                    { "cost", new[] { "cost", "charges", "callcharges", "call charges" } }
                };
                var missingColumns = new List<string>();
                foreach (var (required, aliases) in requiredColumnsWithAliases)
                {
                    if (!aliases.Any(alias => columnIndices.ContainsKey(alias.ToLower())))
                    {
                        missingColumns.Add(required);
                    }
                }

                if (missingColumns.Any())
                {
                    var foundColumns = string.Join(", ", columnIndices.Keys);
                    var missing = string.Join(", ", missingColumns);
                    throw new InvalidOperationException(
                        $"Required columns missing: {missing}. " +
                        $"Found columns in CSV: {foundColumns}. " +
                        $"Please ensure your CSV file has the correct headers (case-insensitive): ext, call_date, dialed, cost"
                    );
                }

                // Create DataTable for Safaricom/Airtel (identical lowercase column schema)
                var dataTable = CreateSafaricomAirtelDataTable();
                var lineNumber = 1;
                var batchCount = 0;

                while (!reader.EndOfStream)
                {
                    cancellationToken.ShutdownToken.ThrowIfCancellationRequested();

                    lineNumber++;
                    var line = await reader.ReadLineAsync();
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    var values = ParseCsvLine(line);
                    if (values.All(v => string.IsNullOrWhiteSpace(v))) continue;

                    try
                    {
                        var row = dataTable.NewRow();
                        var extension = NormalizePhoneNumber(GetValueMultiple(values, columnIndices, "ext", "callingno", "calling no", "msisdn", "phone"));

                        // Fast lookup from pre-loaded dictionary
                        var phoneFound = userPhoneLookup.TryGetValue(extension, out var userPhone);

                        // Get IndexNumber from CSV or UserPhone
                        var indexNumber = GetValue(values, columnIndices, "indexnumber")
                            ?? (phoneFound ? userPhone.IndexNumber : null)
                            ?? "";
                        int? ebillUserId = phoneFound ? userPhone.EbillUserId : null;

                        // Calculate billing period string
                        var billingPeriodDate = new DateTime(billingYear, billingMonth, 1);
                        var billingPeriodString = billingPeriodDate.ToString("yyyy-MM-dd");

                        // Populate row with lowercase column names (Safaricom/Airtel schema)
                        // Uses multiple column name aliases to handle varying CSV header formats
                        row["ext"] = extension;
                        row["call_date"] = ParseDate(GetValueMultiple(values, columnIndices, "call_date", "date"), dateFormat);
                        row["call_time"] = ParseTimeSpan(GetValueMultiple(values, columnIndices, "call_time", "time"));
                        row["dialed"] = GetValueMultiple(values, columnIndices, "dialed", "dialedno", "dialled", "dialled no", "number") ?? "";
                        row["dest"] = GetValueMultiple(values, columnIndices, "dest", "destination") ?? GetValueMultiple(values, columnIndices, "dialed", "dialedno") ?? "";
                        row["durx"] = ParseDurxValue(GetValueMultiple(values, columnIndices, "durx", "dur", "duration"));
                        row["cost"] = ParseDecimalOrZero(GetValueMultiple(values, columnIndices, "cost", "charges", "callcharges", "call charges"));
                        row["dur"] = ParseDurationToMinutes(GetValueMultiple(values, columnIndices, "dur", "duration"));
                        row["call_type"] = GetValueMultiple(values, columnIndices, "call_type", "calltype", "type") ?? "Voice";
                        row["call_month"] = billingMonth;
                        row["call_year"] = billingYear;
                        row["IndexNumber"] = indexNumber;
                        row["UserPhoneId"] = phoneFound ? (object)userPhone.Id : DBNull.Value;
                        row["EbillUserId"] = ebillUserId ?? (object)DBNull.Value;
                        row["BillingPeriod"] = billingPeriodString;
                        row["CreatedDate"] = DateTime.UtcNow;
                        row["CreatedBy"] = "BulkImport";
                        row["ProcessingStatus"] = 0; // ProcessingStatus.Staged
                        row["ImportJobId"] = jobId;

                        dataTable.Rows.Add(row);
                        result.TotalRecords++;

                        // Bulk insert when batch is full
                        if (dataTable.Rows.Count >= BATCH_SIZE)
                        {
                            await BulkInsertToDatabase(dataTable, tableName, cancellationToken.ShutdownToken);
                            batchCount++;
                            result.SuccessCount += dataTable.Rows.Count;

                            _logger.LogInformation(
                                "Batch {BatchNum} completed: {Count} records inserted, Total: {Total}",
                                batchCount, dataTable.Rows.Count, result.SuccessCount);

                            // Update job progress
                            var progress = (result.SuccessCount * 100) / Math.Max(result.TotalRecords, 1);
                            await UpdateJobStatus(jobId, "Processing", result.SuccessCount, null, progress);

                            dataTable.Clear();
                        }
                    }
                    catch (Exception ex)
                    {
                        result.ErrorCount++;
                        result.Errors.Add($"Line {lineNumber}: {ex.Message}");
                        _logger.LogWarning("Error on line {Line}: {Error}", lineNumber, ex.Message);
                    }
                }

                // Insert remaining records
                if (dataTable.Rows.Count > 0)
                {
                    await BulkInsertToDatabase(dataTable, tableName, cancellationToken.ShutdownToken);
                    result.SuccessCount += dataTable.Rows.Count;

                    _logger.LogInformation(
                        "Final batch: {Count} records inserted, Total: {Total}",
                        dataTable.Rows.Count, result.SuccessCount);
                }

                result.EndTime = DateTime.UtcNow;

                // Update job as completed
                var duration = (int)(result.EndTime.Value - result.StartTime).TotalSeconds;
                await UpdateJobStatus(jobId, "Completed", result.SuccessCount, result.ErrorCount, 100, duration);

                _logger.LogInformation(
                    "{TableName} import completed: {Success} success, {Errors} errors in {Duration}s",
                    tableName, result.SuccessCount, result.ErrorCount, duration);

                // Clean up temp file
                try { File.Delete(filePath); } catch { }

                return result;
            }
            catch (Exception ex)
            {
                result.EndTime = DateTime.UtcNow;
                result.ErrorCount++;
                result.Errors.Add(ex.Message);

                _logger.LogError(ex, "{TableName} import failed for job {JobId}", tableName, jobId);

                // Update job as failed
                await UpdateJobStatus(jobId, "Failed", result.SuccessCount, result.ErrorCount, null, null, ex.Message);

                throw;
            }
        }

        // HELPER METHODS

        private async Task<Dictionary<string, (string PhoneNumber, int Id, string? IndexNumber, int? EbillUserId)>> LoadUserPhoneLookup(CancellationToken cancellationToken)
        {
            // Pre-load user phone lookups with EbillUser relationship
            var userPhones = await _context.UserPhones
                .Where(up => up.IsActive)
                .Select(up => new {
                    up.PhoneNumber,
                    up.Id,
                    up.IndexNumber,
                    EbillUserId = up.EbillUser != null ? (int?)up.EbillUser.Id : null
                })
                .ToListAsync(cancellationToken);

            // Build dictionary, taking first occurrence of duplicate phone numbers
            var userPhoneLookup = userPhones
                .GroupBy(up => NormalizePhoneNumber(up.PhoneNumber))
                .ToDictionary(
                    g => g.Key,
                    g => (g.First().PhoneNumber, g.First().Id, g.First().IndexNumber, g.First().EbillUserId));

            _logger.LogInformation(
                "Loaded {Count} user phone mappings from {Total} phone records",
                userPhoneLookup.Count,
                userPhones.Count);

            return userPhoneLookup;
        }

        private DataTable CreateSafaricomAirtelDataTable()
        {
            var dt = new DataTable();
            // Match exact database column names (case-sensitive for SqlBulkCopy)
            // Using lowercase for columns that are lowercase in the database
            dt.Columns.Add("ext", typeof(string)).AllowDBNull = true;
            dt.Columns.Add("call_date", typeof(DateTime)).AllowDBNull = true;
            dt.Columns.Add("call_time", typeof(TimeSpan)).AllowDBNull = true;
            dt.Columns.Add("dialed", typeof(string)).AllowDBNull = true;
            dt.Columns.Add("dest", typeof(string)).AllowDBNull = true;
            dt.Columns.Add("durx", typeof(decimal)).AllowDBNull = true;
            dt.Columns.Add("cost", typeof(decimal)).AllowDBNull = true;
            dt.Columns.Add("dur", typeof(decimal)).AllowDBNull = true;
            dt.Columns.Add("call_type", typeof(string)).AllowDBNull = true;
            dt.Columns.Add("call_month", typeof(int)).AllowDBNull = true;
            dt.Columns.Add("call_year", typeof(int)).AllowDBNull = true;
            dt.Columns.Add("IndexNumber", typeof(string)).AllowDBNull = true;
            dt.Columns.Add("UserPhoneId", typeof(int)).AllowDBNull = true;
            dt.Columns.Add("EbillUserId", typeof(int)).AllowDBNull = true;
            dt.Columns.Add("BillingPeriod", typeof(string)).AllowDBNull = true;
            dt.Columns.Add("CreatedDate", typeof(DateTime)).AllowDBNull = false;
            dt.Columns.Add("CreatedBy", typeof(string)).AllowDBNull = true;
            dt.Columns.Add("ProcessingStatus", typeof(int)).AllowDBNull = false;
            dt.Columns.Add("ImportJobId", typeof(Guid)).AllowDBNull = true;
            return dt;
        }

        private DataTable CreatePSTNDataTable()
        {
            var dt = new DataTable();
            // Match exact database column names (Pascal case for PSTN)
            dt.Columns.Add("Extension", typeof(string)).AllowDBNull = true;
            dt.Columns.Add("DialedNumber", typeof(string)).AllowDBNull = true;
            dt.Columns.Add("CallTime", typeof(TimeSpan)).AllowDBNull = true;
            dt.Columns.Add("Destination", typeof(string)).AllowDBNull = true;
            dt.Columns.Add("DestinationLine", typeof(string)).AllowDBNull = true;
            dt.Columns.Add("DurationExtended", typeof(decimal)).AllowDBNull = true;
            dt.Columns.Add("CallDate", typeof(DateTime)).AllowDBNull = true;
            dt.Columns.Add("Duration", typeof(decimal)).AllowDBNull = true;
            dt.Columns.Add("AmountKSH", typeof(decimal)).AllowDBNull = true;
            dt.Columns.Add("AmountUSD", typeof(decimal)).AllowDBNull = true;
            dt.Columns.Add("IndexNumber", typeof(string)).AllowDBNull = true;
            dt.Columns.Add("Carrier", typeof(string)).AllowDBNull = true;
            dt.Columns.Add("UserPhoneId", typeof(int)).AllowDBNull = true;
            dt.Columns.Add("EbillUserId", typeof(int)).AllowDBNull = true;
            dt.Columns.Add("BillingPeriod", typeof(string)).AllowDBNull = true;
            dt.Columns.Add("CallMonth", typeof(int)).AllowDBNull = false;
            dt.Columns.Add("CallYear", typeof(int)).AllowDBNull = false;
            dt.Columns.Add("CreatedDate", typeof(DateTime)).AllowDBNull = false;
            dt.Columns.Add("CreatedBy", typeof(string)).AllowDBNull = true;
            dt.Columns.Add("ProcessingStatus", typeof(int)).AllowDBNull = false;
            dt.Columns.Add("ImportJobId", typeof(Guid)).AllowDBNull = true;
            return dt;
        }

        private DataTable CreatePrivateWireDataTable()
        {
            var dt = new DataTable();
            // Match exact database column names (Pascal case for PrivateWire)
            dt.Columns.Add("Extension", typeof(string)).AllowDBNull = true;
            dt.Columns.Add("DialedNumber", typeof(string)).AllowDBNull = true;
            dt.Columns.Add("CallTime", typeof(TimeSpan)).AllowDBNull = true;
            dt.Columns.Add("Destination", typeof(string)).AllowDBNull = true;
            dt.Columns.Add("DestinationLine", typeof(string)).AllowDBNull = true;
            dt.Columns.Add("DurationExtended", typeof(decimal)).AllowDBNull = true;
            dt.Columns.Add("CallDate", typeof(DateTime)).AllowDBNull = true;
            dt.Columns.Add("Duration", typeof(decimal)).AllowDBNull = true;
            dt.Columns.Add("AmountKSH", typeof(decimal)).AllowDBNull = true;
            dt.Columns.Add("AmountUSD", typeof(decimal)).AllowDBNull = true;
            dt.Columns.Add("IndexNumber", typeof(string)).AllowDBNull = true;
            dt.Columns.Add("UserPhoneId", typeof(int)).AllowDBNull = true;
            dt.Columns.Add("EbillUserId", typeof(int)).AllowDBNull = true;
            dt.Columns.Add("BillingPeriod", typeof(string)).AllowDBNull = true;
            dt.Columns.Add("CallMonth", typeof(int)).AllowDBNull = false;
            dt.Columns.Add("CallYear", typeof(int)).AllowDBNull = false;
            dt.Columns.Add("CreatedDate", typeof(DateTime)).AllowDBNull = false;
            dt.Columns.Add("CreatedBy", typeof(string)).AllowDBNull = true;
            dt.Columns.Add("ProcessingStatus", typeof(int)).AllowDBNull = false;
            dt.Columns.Add("ImportJobId", typeof(Guid)).AllowDBNull = true;
            return dt;
        }

        private async Task BulkInsertToDatabase(DataTable dataTable, string tableName, CancellationToken cancellationToken)
        {
            var connectionString = _context.Database.GetConnectionString();

            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            using var bulkCopy = new SqlBulkCopy(connection, SqlBulkCopyOptions.Default, null)
            {
                DestinationTableName = $"[ebill].[{tableName}]",
                BatchSize = 10000,
                BulkCopyTimeout = 600,
                EnableStreaming = true
            };

            // Explicit column mappings for each table type
            // Source column names (DataTable) -> Destination column names (Database)
            if (tableName == "Safaricom" || tableName == "Airtel")
            {
                bulkCopy.ColumnMappings.Add("ext", "ext");
                bulkCopy.ColumnMappings.Add("call_date", "call_date");
                bulkCopy.ColumnMappings.Add("call_time", "call_time");
                bulkCopy.ColumnMappings.Add("dialed", "dialed");
                bulkCopy.ColumnMappings.Add("dest", "dest");
                bulkCopy.ColumnMappings.Add("durx", "durx");
                bulkCopy.ColumnMappings.Add("cost", "cost");
                bulkCopy.ColumnMappings.Add("dur", "dur");
                bulkCopy.ColumnMappings.Add("call_type", "call_type");
                bulkCopy.ColumnMappings.Add("call_month", "call_month");
                bulkCopy.ColumnMappings.Add("call_year", "call_year");
                bulkCopy.ColumnMappings.Add("IndexNumber", "IndexNumber");
                bulkCopy.ColumnMappings.Add("UserPhoneId", "UserPhoneId");
                bulkCopy.ColumnMappings.Add("EbillUserId", "EbillUserId");
                bulkCopy.ColumnMappings.Add("BillingPeriod", "BillingPeriod");
                bulkCopy.ColumnMappings.Add("CreatedDate", "CreatedDate");
                bulkCopy.ColumnMappings.Add("CreatedBy", "CreatedBy");
                bulkCopy.ColumnMappings.Add("ProcessingStatus", "ProcessingStatus");
                bulkCopy.ColumnMappings.Add("ImportJobId", "ImportJobId");
            }
            else if (tableName == "PSTNs")
            {
                bulkCopy.ColumnMappings.Add("Extension", "Extension");
                bulkCopy.ColumnMappings.Add("DialedNumber", "DialedNumber");
                bulkCopy.ColumnMappings.Add("CallTime", "CallTime");
                bulkCopy.ColumnMappings.Add("Destination", "Destination");
                bulkCopy.ColumnMappings.Add("DestinationLine", "DestinationLine");
                bulkCopy.ColumnMappings.Add("DurationExtended", "DurationExtended");
                bulkCopy.ColumnMappings.Add("CallDate", "CallDate");
                bulkCopy.ColumnMappings.Add("Duration", "Duration");
                bulkCopy.ColumnMappings.Add("AmountKSH", "AmountKSH");
                bulkCopy.ColumnMappings.Add("AmountUSD", "AmountUSD");
                bulkCopy.ColumnMappings.Add("IndexNumber", "IndexNumber");
                bulkCopy.ColumnMappings.Add("Carrier", "Carrier");
                bulkCopy.ColumnMappings.Add("UserPhoneId", "UserPhoneId");
                bulkCopy.ColumnMappings.Add("EbillUserId", "EbillUserId");
                bulkCopy.ColumnMappings.Add("BillingPeriod", "BillingPeriod");
                bulkCopy.ColumnMappings.Add("CallMonth", "CallMonth");
                bulkCopy.ColumnMappings.Add("CallYear", "CallYear");
                bulkCopy.ColumnMappings.Add("CreatedDate", "CreatedDate");
                bulkCopy.ColumnMappings.Add("CreatedBy", "CreatedBy");
                bulkCopy.ColumnMappings.Add("ProcessingStatus", "ProcessingStatus");
                bulkCopy.ColumnMappings.Add("ImportJobId", "ImportJobId");
            }
            else if (tableName == "PrivateWires")
            {
                bulkCopy.ColumnMappings.Add("Extension", "Extension");
                bulkCopy.ColumnMappings.Add("DialedNumber", "DialedNumber");
                bulkCopy.ColumnMappings.Add("CallTime", "CallTime");
                bulkCopy.ColumnMappings.Add("Destination", "Destination");
                bulkCopy.ColumnMappings.Add("DestinationLine", "DestinationLine");
                bulkCopy.ColumnMappings.Add("DurationExtended", "DurationExtended");
                bulkCopy.ColumnMappings.Add("CallDate", "CallDate");
                bulkCopy.ColumnMappings.Add("Duration", "Duration");
                bulkCopy.ColumnMappings.Add("AmountKSH", "AmountKSH");
                bulkCopy.ColumnMappings.Add("AmountUSD", "AmountUSD");
                bulkCopy.ColumnMappings.Add("IndexNumber", "IndexNumber");
                bulkCopy.ColumnMappings.Add("UserPhoneId", "UserPhoneId");
                bulkCopy.ColumnMappings.Add("EbillUserId", "EbillUserId");
                bulkCopy.ColumnMappings.Add("BillingPeriod", "BillingPeriod");
                bulkCopy.ColumnMappings.Add("CallMonth", "CallMonth");
                bulkCopy.ColumnMappings.Add("CallYear", "CallYear");
                bulkCopy.ColumnMappings.Add("CreatedDate", "CreatedDate");
                bulkCopy.ColumnMappings.Add("CreatedBy", "CreatedBy");
                bulkCopy.ColumnMappings.Add("ProcessingStatus", "ProcessingStatus");
                bulkCopy.ColumnMappings.Add("ImportJobId", "ImportJobId");
            }
            else
            {
                // Generic mapping for other tables
                foreach (DataColumn column in dataTable.Columns)
                {
                    bulkCopy.ColumnMappings.Add(column.ColumnName, column.ColumnName);
                }
            }

            _logger.LogDebug("Inserting {RowCount} rows into {TableName} with {ColumnCount} columns",
                dataTable.Rows.Count, tableName, dataTable.Columns.Count);

            try
            {
                await bulkCopy.WriteToServerAsync(dataTable, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SqlBulkCopy failed. Columns: {Columns}",
                    string.Join(", ", dataTable.Columns.Cast<DataColumn>().Select(c => c.ColumnName)));
                throw;
            }
        }

        private async Task UpdateJobStatus(Guid jobId, string status, int? recordsProcessed, int? recordsError, int? progressPercentage, int? durationSeconds = null, string? errorMessage = null)
        {
            var job = await _context.ImportJobs.FindAsync(jobId);
            if (job != null)
            {
                job.Status = status;
                if (recordsProcessed.HasValue) job.RecordsSuccess = recordsProcessed.Value;
                if (recordsError.HasValue) job.RecordsError = recordsError.Value;
                if (progressPercentage.HasValue) job.ProgressPercentage = progressPercentage.Value;
                if (durationSeconds.HasValue) job.DurationSeconds = durationSeconds.Value;
                if (!string.IsNullOrEmpty(errorMessage)) job.ErrorMessage = errorMessage;

                if (status == "Completed" || status == "Failed")
                {
                    job.CompletedDate = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
            }
        }

        // CSV PARSING HELPERS

        private string[] ParseCsvLine(string line)
        {
            var values = new List<string>();
            var currentValue = "";
            var inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                var c = line[i];

                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == ',' && !inQuotes)
                {
                    values.Add(currentValue.Trim());
                    currentValue = "";
                }
                else
                {
                    currentValue += c;
                }
            }

            values.Add(currentValue.Trim());
            return values.ToArray();
        }

        private Dictionary<string, int> BuildColumnIndex(string[] headers)
        {
            var index = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < headers.Length; i++)
            {
                var header = headers[i].Trim().ToLower();
                if (!index.ContainsKey(header))
                {
                    index[header] = i;
                }
            }
            return index;
        }

        private string? GetValue(string[] values, Dictionary<string, int> columnIndices, string columnName)
        {
            if (columnIndices.TryGetValue(columnName.ToLower(), out int index) && index < values.Length)
            {
                var value = values[index]?.Trim();
                return string.IsNullOrWhiteSpace(value) ? null : value;
            }
            return null;
        }

        /// <summary>
        /// Tries multiple column name aliases and returns the first non-null value found.
        /// Handles CSV files with varying header names (e.g., "dur" vs "duration" vs "Duration(HH:MM:SS)").
        /// </summary>
        private string? GetValueMultiple(string[] values, Dictionary<string, int> columnIndices, params string[] columnNames)
        {
            foreach (var name in columnNames)
            {
                var value = GetValue(values, columnIndices, name);
                if (value != null) return value;
            }
            return null;
        }

        private string NormalizePhoneNumber(string? phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber)) return "";

            // Remove common separators and spaces
            var normalized = phoneNumber.Replace("-", "").Replace(" ", "").Replace("(", "").Replace(")", "");

            // Remove leading zeros or +254 country code
            if (normalized.StartsWith("254"))
                normalized = normalized.Substring(3);
            else if (normalized.StartsWith("0"))
                normalized = normalized.Substring(1);

            return normalized;
        }

        private DateTime ParseDate(string? dateString, string dateFormat)
        {
            if (string.IsNullOrWhiteSpace(dateString))
                return DateTime.MinValue;

            try
            {
                if (!string.IsNullOrEmpty(dateFormat))
                {
                    return DateTime.ParseExact(dateString, dateFormat, CultureInfo.InvariantCulture);
                }

                // Try DD/MM/YYYY formats first (Safaricom/Airtel use Kenyan date format)
                var ddmmFormats = new[] { "d/M/yyyy", "dd/MM/yyyy", "d/MM/yyyy", "dd/M/yyyy",
                                          "d-M-yyyy", "dd-MM-yyyy", "d.M.yyyy", "dd.MM.yyyy" };
                if (DateTime.TryParseExact(dateString, ddmmFormats, CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var ddmmDate))
                {
                    return ddmmDate;
                }

                // Fallback to general parsing
                return DateTime.Parse(dateString);
            }
            catch
            {
                return DateTime.MinValue;
            }
        }

        private decimal ParseDecimalOrZero(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return 0;
            return decimal.TryParse(value, out var result) ? result : 0;
        }

        /// <summary>
        /// Parses duration from various formats for PSTN/PrivateWire:
        /// - Time format "H:MM:SS" or "HH:MM:SS" → converts to minutes (decimal)
        /// - Time format "MM:SS" or "M:SS" → converts to minutes (decimal)
        /// - Decimal number (already in minutes) → returns as-is
        /// </summary>
        private decimal ParseDurationToMinutes(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return 0;

            // Check if it's a time format (contains colons)
            if (value.Contains(':'))
            {
                var parts = value.Split(':');

                if (parts.Length == 3)
                {
                    // Format: H:MM:SS or HH:MM:SS
                    if (int.TryParse(parts[0], out int hours) &&
                        int.TryParse(parts[1], out int minutes) &&
                        int.TryParse(parts[2], out int seconds))
                    {
                        // Convert to total minutes (decimal)
                        // e.g., 0:01:30 = 0*60 + 1 + 30/60 = 1.5 minutes
                        return (hours * 60) + minutes + (seconds / 60.0m);
                    }
                }
                else if (parts.Length == 2)
                {
                    // Format: MM:SS or M:SS
                    if (int.TryParse(parts[0], out int minutes) &&
                        int.TryParse(parts[1], out int seconds))
                    {
                        return minutes + (seconds / 60.0m);
                    }
                }
            }

            // Try to parse as decimal number (already in minutes)
            return decimal.TryParse(value, out var result) ? result : 0;
        }

        /// <summary>
        /// Parses the durx column which can be:
        /// - Time format "H:MM:SS" or "HH:MM:SS" (voice calls) → convert to total seconds as decimal (mm.ss format)
        /// - Decimal number (internet usage in KB)
        /// - Text like "SMS" → store as 0
        /// </summary>
        private decimal ParseDurxValue(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return 0;

            // Check if it's a time format (contains colons like "0:00:33" or "0:01:57")
            if (value.Contains(':'))
            {
                var parts = value.Split(':');
                if (parts.Length == 3)
                {
                    // Format: H:MM:SS or HH:MM:SS
                    if (int.TryParse(parts[0], out int hours) &&
                        int.TryParse(parts[1], out int minutes) &&
                        int.TryParse(parts[2], out int seconds))
                    {
                        // Convert to mm.ss format (minutes with seconds as decimal)
                        // e.g., 0:01:57 = 1 minute 57 seconds = 1.57
                        // e.g., 0:00:33 = 0 minutes 33 seconds = 0.33
                        int totalMinutes = (hours * 60) + minutes;
                        return totalMinutes + (seconds / 100m);
                    }
                }
                else if (parts.Length == 2)
                {
                    // Format: MM:SS
                    if (int.TryParse(parts[0], out int minutes) &&
                        int.TryParse(parts[1], out int seconds))
                    {
                        return minutes + (seconds / 100m);
                    }
                }
            }

            // Try parsing as decimal (for internet usage KB values like "325316.64")
            if (decimal.TryParse(value, out var result))
            {
                return result;
            }

            // Text values like "SMS", "ROAMING", etc. - return 0
            return 0;
        }

        private TimeSpan ParseTimeSpan(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return TimeSpan.Zero;

            // Try parsing as TimeSpan directly (e.g., "12:34:56" or "12:34")
            if (TimeSpan.TryParse(value, out var result))
                return result;

            // Try parsing as seconds
            if (decimal.TryParse(value, out var seconds))
                return TimeSpan.FromSeconds((double)seconds);

            return TimeSpan.Zero;
        }
    }

    public class ImportResult
    {
        public bool Success { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public int TotalRecords { get; set; }
        public int SuccessCount { get; set; }
        public int ErrorCount { get; set; }
        public List<string> Errors { get; set; } = new();
        public TimeSpan Duration => EndTime.HasValue ? EndTime.Value - StartTime : TimeSpan.Zero;
    }
}
