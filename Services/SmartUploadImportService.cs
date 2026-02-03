using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ExcelDataReader;
using Hangfire;
using Hangfire.Server;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TAB.Web.Data;
using TAB.Web.Models;

namespace TAB.Web.Services
{
    public interface ISmartUploadImportService
    {
        Task<ImportResult> ImportFileAsync(Guid jobId, string filePath, int billingMonth, int billingYear, string provider, bool isExcelFile, IJobCancellationToken cancellationToken);
        Task<ImportResult> ImportExcelAsync(Guid jobId, string filePath, int billingMonth, int billingYear, string provider, IJobCancellationToken cancellationToken);
    }

    public class SmartUploadImportService : ISmartUploadImportService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SmartUploadImportService> _logger;
        private readonly ISmartUploadUserCreationService _userCreationService;

        public SmartUploadImportService(
            ApplicationDbContext context,
            ILogger<SmartUploadImportService> logger,
            ISmartUploadUserCreationService userCreationService)
        {
            _context = context;
            _logger = logger;
            _userCreationService = userCreationService;
        }

        /// <summary>
        /// Main entry point for SmartUpload imports - handles both CSV and Excel files
        /// </summary>
        public async Task<ImportResult> ImportFileAsync(Guid jobId, string filePath, int billingMonth, int billingYear, string provider, bool isExcelFile, IJobCancellationToken cancellationToken)
        {
            var result = new ImportResult { StartTime = DateTime.UtcNow };

            try
            {
                _logger.LogInformation("Starting SmartUpload import for job {JobId}: {Provider} from {FilePath} (Excel: {IsExcel})",
                    jobId, provider, filePath, isExcelFile);

                // Update job status to Processing
                await UpdateJobStatus(jobId, "Processing", null, null, null);

                // Route to appropriate handler based on file type and provider
                if (isExcelFile)
                {
                    // Excel files
                    switch (provider.ToLower())
                    {
                        case "safaricom":
                        case "airtel":
                            result = await ImportMobileExcelAsync(jobId, filePath, billingMonth, billingYear, provider, cancellationToken);
                            break;
                        case "pstn":
                            result = await ImportPstnExcelAsync(jobId, filePath, billingMonth, billingYear, cancellationToken);
                            break;
                        case "privatewire":
                            result = await ImportPrivateWireExcelAsync(jobId, filePath, billingMonth, billingYear, cancellationToken);
                            break;
                        default:
                            throw new InvalidOperationException($"Unsupported provider: {provider}");
                    }
                }
                else
                {
                    // CSV files
                    switch (provider.ToLower())
                    {
                        case "safaricom":
                        case "airtel":
                            result = await ImportMobileCsvAsync(jobId, filePath, billingMonth, billingYear, provider, cancellationToken);
                            break;
                        case "pstn":
                            result = await ImportPstnCsvAsync(jobId, filePath, billingMonth, billingYear, cancellationToken);
                            break;
                        case "privatewire":
                            result = await ImportPrivateWireCsvAsync(jobId, filePath, billingMonth, billingYear, cancellationToken);
                            break;
                        default:
                            throw new InvalidOperationException($"Unsupported provider: {provider}");
                    }
                }

                // Update job status to Completed
                await UpdateJobStatus(jobId, "Completed", result.SuccessCount, result.ErrorCount, null);

                result.EndTime = DateTime.UtcNow;
                _logger.LogInformation("SmartUpload import completed for job {JobId}: {SuccessCount} success, {ErrorCount} errors",
                    jobId, result.SuccessCount, result.ErrorCount);

                // Clean up temp file
                try { File.Delete(filePath); } catch { }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SmartUpload import failed for job {JobId}", jobId);
                await UpdateJobStatus(jobId, "Failed", result.SuccessCount, result.ErrorCount, ex.Message);

                // Clean up temp file
                try { File.Delete(filePath); } catch { }

                throw;
            }
        }

        public async Task<ImportResult> ImportExcelAsync(Guid jobId, string filePath, int billingMonth, int billingYear, string provider, IJobCancellationToken cancellationToken)
        {
            var result = new ImportResult { StartTime = DateTime.UtcNow };

            try
            {
                _logger.LogInformation("Starting SmartUpload Excel import for job {JobId}: {Provider} from {FilePath}", jobId, provider, filePath);

                // Update job status to Processing
                await UpdateJobStatus(jobId, "Processing", null, null, null);

                // Route to appropriate handler based on provider
                switch (provider.ToLower())
                {
                    case "safaricom":
                    case "airtel":
                        result = await ImportMobileExcelAsync(jobId, filePath, billingMonth, billingYear, provider, cancellationToken);
                        break;

                    case "pstn":
                        result = await ImportPstnExcelAsync(jobId, filePath, billingMonth, billingYear, cancellationToken);
                        break;

                    case "privatewire":
                        result = await ImportPrivateWireExcelAsync(jobId, filePath, billingMonth, billingYear, cancellationToken);
                        break;

                    default:
                        throw new InvalidOperationException($"Unsupported provider: {provider}");
                }

                // Update job status to Completed
                await UpdateJobStatus(jobId, "Completed", result.SuccessCount, result.ErrorCount, null);

                result.EndTime = DateTime.UtcNow;
                _logger.LogInformation("SmartUpload Excel import completed for job {JobId}: {SuccessCount} success, {ErrorCount} errors",
                    jobId, result.SuccessCount, result.ErrorCount);

                // Clean up temp file
                try { File.Delete(filePath); } catch { }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SmartUpload Excel import failed for job {JobId}", jobId);
                await UpdateJobStatus(jobId, "Failed", result.SuccessCount, result.ErrorCount, ex.Message);

                // Clean up temp file
                try { File.Delete(filePath); } catch { }

                throw;
            }
        }

        private async Task<ImportResult> ImportMobileExcelAsync(Guid jobId, string filePath, int billingMonth, int billingYear, string provider, IJobCancellationToken cancellationToken)
        {
            const int BATCH_SIZE = 50000;
            var result = new ImportResult { StartTime = DateTime.UtcNow };

            // Pre-load user phone lookups
            var userPhoneLookup = await LoadUserPhoneLookup(cancellationToken.ShutdownToken);

            // Required for .xls support
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            IExcelDataReader reader;
            FileStream stream;
            try
            {
                stream = File.Open(filePath, FileMode.Open, FileAccess.Read);
                reader = ExcelReaderFactory.CreateReader(stream);
            }
            catch (BadImageFormatException ex)
            {
                _logger.LogError(ex, "BadImageFormatException reading Excel file - this usually indicates a 32-bit/64-bit mismatch. Try converting .xls to .xlsx format.");
                throw new InvalidOperationException($"Cannot read .xls file due to platform compatibility. Please convert the file to .xlsx format and try again. Error: {ex.Message}", ex);
            }

            // Create DataTable
            var dataTable = CreateMobileDataTable();
            var billingPeriodString = new DateTime(billingYear, billingMonth, 1).ToString("yyyy-MM-dd");

            // Count total worksheets to determine which ones to skip
            int totalWorksheets = reader.ResultsCount;
            int currentWorksheet = 0;

            _logger.LogInformation("{Provider} Excel file has {Count} worksheets", provider, totalWorksheets);

            // Process each worksheet
            do
            {
                currentWorksheet++;

                // Skip the last worksheet for Airtel (it typically contains summary/metadata)
                if (provider.ToLower() == "airtel" && totalWorksheets > 1 && currentWorksheet == totalWorksheets)
                {
                    _logger.LogInformation("Skipping last worksheet ({Current}/{Total}) for Airtel", currentWorksheet, totalWorksheets);
                    continue;
                }

                _logger.LogInformation("Processing worksheet {Current}/{Total} for {Provider}", currentWorksheet, totalWorksheets, provider);

                // Find header row for this worksheet
                int rowNumber = 0;
                Dictionary<string, int>? columnIndices = null;

                while (reader.Read() && rowNumber < 20)
                {
                    rowNumber++;
                    var rowValues = new List<string>();
                    for (int col = 0; col < reader.FieldCount; col++)
                    {
                        rowValues.Add(reader.GetValue(col)?.ToString()?.Trim().ToLower() ?? "");
                    }

                    if (IsMobileHeaderRow(rowValues, provider))
                    {
                        columnIndices = BuildMobileColumnIndex(rowValues, provider);
                        break;
                    }
                }

                if (columnIndices == null)
                {
                    _logger.LogWarning("Could not find valid header row in worksheet {Worksheet} for {Provider}, skipping", currentWorksheet, provider);
                    continue;
                }

                // Process data rows in this worksheet
                while (reader.Read())
                {
                    cancellationToken.ShutdownToken.ThrowIfCancellationRequested();
                    rowNumber++;

                    try
                    {
                        var callingNo = NormalizePhoneNumber(GetReaderValue(reader, columnIndices, "callingno", provider));
                        if (string.IsNullOrWhiteSpace(callingNo)) continue;

                        var phoneFound = userPhoneLookup.TryGetValue(callingNo, out var userPhone);

                        var dataRow = dataTable.NewRow();
                        dataRow["ext"] = callingNo;
                        dataRow["call_date"] = ParseReaderDate(reader, columnIndices, "date", provider);
                        dataRow["call_time"] = ParseReaderTime(reader, columnIndices, "time", provider);
                        dataRow["dialed"] = GetReaderValue(reader, columnIndices, "dialedno", provider) ?? "";
                        dataRow["dur"] = ParseReaderDecimal(reader, columnIndices, "duration", provider);
                        dataRow["cost"] = ParseReaderDecimal(reader, columnIndices, "charges", provider);
                        dataRow["call_type"] = GetReaderValue(reader, columnIndices, "calltype", provider) ?? "";
                        dataRow["IndexNumber"] = phoneFound ? userPhone.IndexNumber ?? "" : "";
                        dataRow["UserPhoneId"] = phoneFound ? (object)userPhone.Id : DBNull.Value;
                        dataRow["EbillUserId"] = phoneFound ? (object?)userPhone.EbillUserId : DBNull.Value;
                        dataRow["BillingPeriod"] = billingPeriodString;
                        dataRow["call_month"] = billingMonth;
                        dataRow["call_year"] = billingYear;
                        dataRow["CreatedDate"] = DateTime.UtcNow;
                        dataRow["ProcessingStatus"] = 0; // Staged
                        dataRow["ImportJobId"] = jobId;

                        dataTable.Rows.Add(dataRow);
                        result.SuccessCount++;

                        // Bulk insert in batches
                        if (dataTable.Rows.Count >= BATCH_SIZE)
                        {
                            await BulkInsertMobileAsync(dataTable, provider);
                            await UpdateJobStatus(jobId, "Processing", result.SuccessCount, result.ErrorCount, null);
                            dataTable.Clear();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error processing row {Row} in worksheet {Worksheet}", rowNumber, currentWorksheet);
                        result.ErrorCount++;
                    }
                }

                _logger.LogInformation("Completed worksheet {Current}/{Total}, records so far: {Count}",
                    currentWorksheet, totalWorksheets, result.SuccessCount);

            } while (reader.NextResult()); // Move to next worksheet

            // Insert remaining rows
            if (dataTable.Rows.Count > 0)
            {
                await BulkInsertMobileAsync(dataTable, provider);
            }

            _logger.LogInformation("{Provider} Excel import complete: {Success} records imported, {Errors} errors",
                provider, result.SuccessCount, result.ErrorCount);

            return result;
        }

        private async Task<ImportResult> ImportPstnExcelAsync(Guid jobId, string filePath, int billingMonth, int billingYear, IJobCancellationToken cancellationToken)
        {
            const int BATCH_SIZE = 50000;
            var result = new ImportResult { StartTime = DateTime.UtcNow };

            // Pre-load user phone lookups
            var userPhoneLookup = await LoadUserPhoneLookup(cancellationToken.ShutdownToken);
            // Load EbillUser lookup by IndexNumber for direct linking from Staff ID in file
            var ebillUserLookup = await LoadEbillUserLookupByIndexNumber(cancellationToken.ShutdownToken);

            // Required for .xls support
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            IExcelDataReader reader;
            FileStream stream;
            try
            {
                stream = File.Open(filePath, FileMode.Open, FileAccess.Read);
                reader = ExcelReaderFactory.CreateReader(stream);
            }
            catch (BadImageFormatException ex)
            {
                _logger.LogError(ex, "BadImageFormatException reading Excel file - this usually indicates a 32-bit/64-bit mismatch. Try converting .xls to .xlsx format.");
                throw new InvalidOperationException($"Cannot read .xls file due to platform compatibility. Please convert the file to .xlsx format and try again. Error: {ex.Message}", ex);
            }

            // Create DataTable for PSTN
            var dataTable = CreatePSTNDataTable();
            var billingPeriodString = new DateTime(billingYear, billingMonth, 1).ToString("yyyy-MM-dd");

            string currentExtension = "";
            string currentIndexNumber = "";
            string currentIndexNumberFromFile = ""; // IndexNumber extracted from Staff ID in file
            int? currentUserPhoneId = null;
            int? currentEbillUserId = null;
            int rowNumber = 0;
            int extensionHeadersFound = 0;
            int dateRowsFound = 0;
            int skippedNoExtension = 0;
            var foundExtensions = new List<string>(); // Track first few found extensions for diagnostics
            var extractedUsersForAutoCreate = new Dictionary<string, ExtractedUserInfo>(); // Track users for auto-creation
            UserCreationResult? autoCreateResult = null;

            // First, try to detect format by reading first 20 rows
            var sampleRows = new List<string>();
            Dictionary<string, int>? tabularColumnIndices = null;
            bool isTabularFormat = false;

            while (reader.Read() && rowNumber < 20)
            {
                rowNumber++;
                var rowValues = new List<string>();
                for (int col = 0; col < Math.Min(reader.FieldCount, 10); col++)
                {
                    rowValues.Add(reader.GetValue(col)?.ToString()?.Trim() ?? "");
                }
                var rowStr = string.Join(" | ", rowValues);
                sampleRows.Add($"Row {rowNumber}: {rowStr}");

                // Check if this looks like a tabular header row
                var lowerValues = rowValues.Select(v => v.ToLower()).ToList();
                if (lowerValues.Any(v => v.Contains("extension") || v.Contains("ext")) &&
                    lowerValues.Any(v => v.Contains("date")) &&
                    lowerValues.Any(v => v.Contains("cost") || v.Contains("amount") || v.Contains("charge")))
                {
                    isTabularFormat = true;
                    tabularColumnIndices = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                    for (int i = 0; i < lowerValues.Count; i++)
                    {
                        if (!string.IsNullOrWhiteSpace(lowerValues[i]) && !tabularColumnIndices.ContainsKey(lowerValues[i]))
                            tabularColumnIndices[lowerValues[i]] = i;
                    }
                    _logger.LogInformation("PSTN Excel: Detected TABULAR format with columns: {Columns}", string.Join(", ", tabularColumnIndices.Keys));
                    break;
                }
            }

            var sampleInfo = new System.Text.StringBuilder(string.Join("\n", sampleRows.Take(10)));
            _logger.LogInformation("PSTN Excel first rows sample:\n{Sample}", sampleInfo);

            // Save sample info to job for debugging
            await UpdateJobStatus(jobId, "Processing", null, null, $"Format: {(isTabularFormat ? "TABULAR" : "GROUPED")}. Columns: {(tabularColumnIndices != null ? string.Join(", ", tabularColumnIndices.Keys) : "N/A")}. Sample:\n{sampleInfo}");

            // For GROUPED format: First pass to extract all extensions with Staff names for auto-creation
            if (!isTabularFormat)
            {
                _logger.LogInformation("PSTN Excel: Running first pass to extract extensions for auto-creation");

                // Reset reader for first pass
                reader.Dispose();
                stream.Dispose();
                using var firstPassStream = File.Open(filePath, FileMode.Open, FileAccess.Read);
                using var firstPassReader = ExcelReaderFactory.CreateReader(firstPassStream);

                while (firstPassReader.Read())
                {
                    cancellationToken.ShutdownToken.ThrowIfCancellationRequested();
                    var cellA = firstPassReader.GetValue(0)?.ToString()?.Trim() ?? "";

                    // Check for extension header row
                    // Full format: "Org 187: WFP, Division 1477: RO, ..., Staff ID 6726: 8847535, ..., Extension 2:22959: Yaa Rachael"
                    var extIndex = cellA.IndexOf("Extension", StringComparison.OrdinalIgnoreCase);
                    if (extIndex >= 0)
                    {
                        var extPart = cellA.Substring(extIndex);

                        // Pattern: "Extension 2:22959: Yaa Rachael" - extract extension and name
                        var match = System.Text.RegularExpressions.Regex.Match(extPart, @"Extension\s+\d+:(\d+):\s*(.+?)(?:\s*$|,)");
                        if (match.Success && match.Groups[1].Success)
                        {
                            var ext = NormalizePhoneNumber(match.Groups[1].Value);
                            var name = match.Groups[2].Success ? match.Groups[2].Value.Trim() : "";

                            // Also extract Staff ID (index number) from the full header
                            // Pattern: "Staff ID 6726: 8847535"
                            string? indexNumber = null;
                            var empIdMatch = System.Text.RegularExpressions.Regex.Match(cellA, @"Staff ID\s+\d+:\s*(\d+)");
                            if (empIdMatch.Success && empIdMatch.Groups[1].Success)
                            {
                                indexNumber = empIdMatch.Groups[1].Value.Trim();
                            }

                            if (!string.IsNullOrWhiteSpace(ext) && !string.IsNullOrWhiteSpace(name) && !extractedUsersForAutoCreate.ContainsKey(ext))
                            {
                                extractedUsersForAutoCreate[ext] = new ExtractedUserInfo
                                {
                                    Extension = ext,
                                    EmployeeName = name,
                                    IndexNumber = indexNumber
                                };
                                _logger.LogDebug("First pass extracted: Extension={Ext}, Name={Name}, IndexNumber={Index}",
                                    ext, name, indexNumber ?? "N/A");
                            }
                        }
                    }
                }

                _logger.LogInformation("PSTN Excel: First pass found {Count} unique extensions with names", extractedUsersForAutoCreate.Count);

                // Auto-create missing users
                if (extractedUsersForAutoCreate.Count > 0)
                {
                    _logger.LogInformation("PSTN Excel: Starting auto-creation of missing users");
                    autoCreateResult = await _userCreationService.AutoCreateMissingUsersAsync(
                        extractedUsersForAutoCreate, jobId, cancellationToken.ShutdownToken);

                    _logger.LogInformation("PSTN Excel: Auto-creation completed. Created: {UsersCreated} users, {PhonesCreated} phones",
                        autoCreateResult.UsersCreated, autoCreateResult.PhonesCreated);

                    // Reload user phone lookup if any users were created
                    if (autoCreateResult.UsersCreated > 0)
                    {
                        _logger.LogInformation("PSTN Excel: Reloading user phone lookup after auto-creation");
                        userPhoneLookup = await LoadUserPhoneLookup(cancellationToken.ShutdownToken);
                    }
                }
            }
            else
            {
                // For tabular format, close the initial readers
                reader.Dispose();
                stream.Dispose();
            }

            // Reset reader to start - close and reopen for clean state
            using var stream2 = File.Open(filePath, FileMode.Open, FileAccess.Read);
            using var reader2 = ExcelReaderFactory.CreateReader(stream2);
            rowNumber = 0;

            if (isTabularFormat && tabularColumnIndices != null)
            {
                // Process tabular format (like CSV with headers)
                _logger.LogInformation("PSTN Excel: Processing as TABULAR format");
                bool headerSkipped = false;

                while (reader2.Read())
                {
                    cancellationToken.ShutdownToken.ThrowIfCancellationRequested();
                    rowNumber++;

                    try
                    {
                        // Skip header row
                        if (!headerSkipped)
                        {
                            var firstCell = reader2.GetValue(0)?.ToString()?.ToLower() ?? "";
                            if (firstCell.Contains("ext") || firstCell.Contains("date"))
                            {
                                headerSkipped = true;
                                continue;
                            }
                        }

                        // Get extension from column
                        var extension = GetTabularValue(reader2, tabularColumnIndices, new[] { "extension", "ext", "callingno", "calling no" });
                        extension = NormalizePhoneNumber(extension);
                        if (string.IsNullOrWhiteSpace(extension)) continue;

                        // Get other values
                        var dateStr = GetTabularValue(reader2, tabularColumnIndices, new[] { "date", "call_date", "calldate" });
                        if (!DateTime.TryParse(dateStr, out var callDate)) continue;

                        var phoneFound = userPhoneLookup.TryGetValue(extension, out var userPhone);

                        var time = GetTabularValue(reader2, tabularColumnIndices, new[] { "time", "call_time", "calltime" });
                        var callType = GetTabularValue(reader2, tabularColumnIndices, new[] { "calltype", "call_type", "type", "destinationline" });
                        var place = GetTabularValue(reader2, tabularColumnIndices, new[] { "destination", "place", "dest" });
                        var distantNumber = GetTabularValue(reader2, tabularColumnIndices, new[] { "dialednumber", "dialed", "dialedno", "distant_number", "distantnumber", "number" });
                        var durationStr = GetTabularValue(reader2, tabularColumnIndices, new[] { "duration", "dur" });
                        var costStr = GetTabularValue(reader2, tabularColumnIndices, new[] { "cost", "amount", "amountksh", "charges", "charge" });

                        var duration = ParseDurationToSeconds(durationStr);
                        var cost = ParseDecimalOrZero(costStr);

                        var dataRow = dataTable.NewRow();
                        dataRow["Extension"] = extension;
                        dataRow["DialedNumber"] = distantNumber ?? "";
                        dataRow["CallTime"] = ParseTimeSpan(time);
                        dataRow["Destination"] = place ?? "";
                        dataRow["DestinationLine"] = callType ?? "";
                        dataRow["DurationExtended"] = duration;
                        dataRow["CallDate"] = callDate;
                        dataRow["Duration"] = duration;
                        dataRow["AmountKSH"] = cost;
                        dataRow["AmountUSD"] = 0m;
                        dataRow["IndexNumber"] = phoneFound ? userPhone.IndexNumber ?? "" : "";
                        dataRow["Carrier"] = "PSTN";
                        dataRow["UserPhoneId"] = phoneFound ? (object)userPhone.Id : DBNull.Value;
                        dataRow["EbillUserId"] = phoneFound ? (object?)userPhone.EbillUserId : DBNull.Value;
                        dataRow["BillingPeriod"] = billingPeriodString;
                        dataRow["CallMonth"] = billingMonth;
                        dataRow["CallYear"] = billingYear;
                        dataRow["CreatedDate"] = DateTime.UtcNow;
                        dataRow["ImportJobId"] = jobId;

                        dataTable.Rows.Add(dataRow);
                        result.SuccessCount++;

                        if (dataTable.Rows.Count >= BATCH_SIZE)
                        {
                            await BulkInsertPSTNAsync(dataTable);
                            await UpdateJobStatus(jobId, "Processing", result.SuccessCount, result.ErrorCount, null);
                            dataTable.Clear();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error processing PSTN tabular row {Row}", rowNumber);
                        result.ErrorCount++;
                    }
                }
            }
            else
            {
                // Process grouped format (Extension 1234 headers followed by data rows)
                _logger.LogInformation("PSTN Excel: Processing as GROUPED format (Extension headers)");

                // Track column indices - will be detected from header rows
                int colTime = 1, colCallType = 2, colPlace = 3, colDistant = 6, colDuration = 8, colCost = 9;
                bool columnsDetected = false;

                while (reader2.Read())
                {
                    cancellationToken.ShutdownToken.ThrowIfCancellationRequested();
                    rowNumber++;

                    try
                    {
                        var cellA = reader2.GetValue(0)?.ToString()?.Trim() ?? "";
                        var fieldCount = reader2.FieldCount;

                        // Detect column structure from header row
                        if (!columnsDetected && cellA.ToLower() == "date")
                        {
                            // Read header row to detect column positions
                            var headerRow = new List<string>();
                            for (int col = 0; col < Math.Min(fieldCount, 15); col++)
                            {
                                headerRow.Add(reader2.GetValue(col)?.ToString()?.Trim()?.ToLower() ?? "");
                            }

                            _logger.LogInformation("PSTN: Detected header row: {Headers}", string.Join(" | ", headerRow));
                            sampleInfo.AppendLine($"Header: {string.Join(" | ", headerRow)}");

                            // Find column indices based on header names
                            for (int i = 0; i < headerRow.Count; i++)
                            {
                                var h = headerRow[i];
                                if (h == "time") colTime = i;
                                else if (h == "call" || h == "calltype" || h == "call type" || h == "type") colCallType = i;
                                else if (h == "place" || h == "destination") colPlace = i;
                                else if (h == "distant" || h == "distantnumber" || h == "distant number" || h == "dialed" || h == "dialednumber") colDistant = i;
                                else if (h == "duration" || h == "dur") colDuration = i;
                                else if (h == "cost" || h == "amount" || h == "charges" || h == "charge") colCost = i;
                                else if (h == "band") { /* skip band column */ }
                                else if (h == "rate") { /* skip rate column */ }
                                else if (h == "ringtime") { /* skip ringtime column */ }
                            }

                            columnsDetected = true;
                            _logger.LogInformation("PSTN column indices: Time={Time}, CallType={CallType}, Place={Place}, Distant={Distant}, Duration={Duration}, Cost={Cost}",
                                colTime, colCallType, colPlace, colDistant, colDuration, colCost);
                            continue;
                        }

                        // Check for extension header row - extension info may be embedded in a longer string
                        // Format examples:
                        //   "Extension 1234" (simple)
                        //   "Extension 2:21588: Makarigakis" (complex - extension is after first colon)
                        //   "Org 145:..., Extension 2:21588: Name" (embedded within other data)
                        var extensionIndex = cellA.IndexOf("Extension", StringComparison.OrdinalIgnoreCase);
                        if (extensionIndex >= 0)
                        {
                            extensionHeadersFound++;
                            var extensionPart = cellA.Substring(extensionIndex);

                            // Try to extract extension number and Staff name using pattern: "Extension X:NNNNN: Name" or "Extension NNNNN"
                            string? extractedExtension = null;
                            string? extractedName = null;

                            // Pattern 1: "Extension 2:21588: Name" - number after first colon, name after second colon
                            var colonMatch = System.Text.RegularExpressions.Regex.Match(extensionPart, @"Extension\s+\d+:(\d+):\s*(.+?)(?:\s*$|,)");
                            if (colonMatch.Success && colonMatch.Groups[1].Success)
                            {
                                extractedExtension = colonMatch.Groups[1].Value;
                                if (colonMatch.Groups[2].Success)
                                {
                                    extractedName = colonMatch.Groups[2].Value.Trim();
                                }
                            }
                            else
                            {
                                // Pattern 2: "Extension 21588" or "Extension: 21588" - simple format (no name available)
                                var simpleMatch = System.Text.RegularExpressions.Regex.Match(extensionPart, @"Extension[:\s]+(\d+)");
                                if (simpleMatch.Success && simpleMatch.Groups[1].Success)
                                {
                                    extractedExtension = simpleMatch.Groups[1].Value;
                                }
                            }

                            if (!string.IsNullOrWhiteSpace(extractedExtension))
                            {
                                currentExtension = NormalizePhoneNumber(extractedExtension);
                                _logger.LogDebug("Found extension: {Extension}, Name: {Name} from row: {Row}",
                                    currentExtension, extractedName ?? "N/A", cellA.Substring(0, Math.Min(100, cellA.Length)));

                                // Track first 10 extensions found for diagnostics
                                if (foundExtensions.Count < 10 && !foundExtensions.Contains(currentExtension))
                                    foundExtensions.Add(currentExtension);

                                // Track extracted users for auto-creation
                                if (!extractedUsersForAutoCreate.ContainsKey(currentExtension) && !string.IsNullOrWhiteSpace(extractedName))
                                {
                                    extractedUsersForAutoCreate[currentExtension] = new ExtractedUserInfo
                                    {
                                        Extension = currentExtension,
                                        EmployeeName = extractedName
                                    };
                                }

                                // Extract Staff ID (IndexNumber) from full header row for direct EbillUser linking
                                // Pattern: "Staff ID 6726: 8847535" - extract 8847535
                                currentIndexNumberFromFile = "";
                                var staffIdMatch = System.Text.RegularExpressions.Regex.Match(cellA, @"Staff ID\s+\d+:\s*(\d+)");
                                if (staffIdMatch.Success && staffIdMatch.Groups[1].Success)
                                {
                                    currentIndexNumberFromFile = staffIdMatch.Groups[1].Value.Trim();
                                }

                                // Priority 1: Try to link directly using IndexNumber from file (Staff ID)
                                if (!string.IsNullOrEmpty(currentIndexNumberFromFile) && ebillUserLookup.TryGetValue(currentIndexNumberFromFile, out var ebillUserId))
                                {
                                    currentIndexNumber = currentIndexNumberFromFile;
                                    currentEbillUserId = ebillUserId;
                                    // Try to get UserPhoneId if available
                                    if (userPhoneLookup.TryGetValue(currentExtension, out var userPhone))
                                    {
                                        currentUserPhoneId = userPhone.Id;
                                    }
                                    else
                                    {
                                        currentUserPhoneId = null;
                                    }
                                    _logger.LogDebug("PSTN: Linked via IndexNumber from file: {IndexNumber} -> EbillUserId: {EbillUserId}", currentIndexNumberFromFile, ebillUserId);
                                }
                                // Priority 2: Fall back to UserPhone lookup by Extension
                                else if (userPhoneLookup.TryGetValue(currentExtension, out var userPhone))
                                {
                                    currentIndexNumber = userPhone.IndexNumber ?? "";
                                    currentUserPhoneId = userPhone.Id;
                                    currentEbillUserId = userPhone.EbillUserId;
                                    _logger.LogDebug("PSTN: Linked via UserPhone: Extension {Extension} -> EbillUserId: {EbillUserId}", currentExtension, currentEbillUserId);
                                }
                                else
                                {
                                    currentIndexNumber = currentIndexNumberFromFile; // Keep the index number from file even if not linked
                                    currentUserPhoneId = null;
                                    currentEbillUserId = null;
                                    _logger.LogDebug("PSTN: No link found for Extension {Extension}, IndexNumber from file: {IndexNumber}", currentExtension, currentIndexNumberFromFile);
                                }
                            }
                            continue;
                        }

                        // Skip header rows and empty rows
                        if (string.IsNullOrWhiteSpace(cellA) || cellA.ToLower() == "date") continue;

                        // Try to parse as date - if successful, it's a data row
                        if (!DateTime.TryParse(cellA, out var callDate))
                        {
                            continue;
                        }

                        dateRowsFound++;

                        if (string.IsNullOrWhiteSpace(currentExtension))
                        {
                            skippedNoExtension++;
                            continue;
                        }

                        // Parse data row using detected column indices (with bounds checking)
                        string SafeGetValue(int col) => col < fieldCount ? (reader2.GetValue(col)?.ToString()?.Trim() ?? "") : "";

                        var time = SafeGetValue(colTime);
                        var callType = SafeGetValue(colCallType);
                        var place = SafeGetValue(colPlace);
                        var distantNumber = SafeGetValue(colDistant);
                        var durationStr = SafeGetValue(colDuration);
                        var costStr = SafeGetValue(colCost);

                        var duration = ParseDurationToSeconds(durationStr);
                        var cost = ParseDecimalOrZero(costStr);

                        var dataRow = dataTable.NewRow();
                        dataRow["Extension"] = currentExtension;
                        dataRow["DialedNumber"] = distantNumber;
                        dataRow["CallTime"] = ParseTimeSpan(time);
                        dataRow["Destination"] = place;
                        dataRow["DestinationLine"] = callType;
                        dataRow["DurationExtended"] = duration;
                        dataRow["CallDate"] = callDate;
                        dataRow["Duration"] = duration;
                        dataRow["AmountKSH"] = cost; // PSTN costs are in KES
                        dataRow["AmountUSD"] = 0m;
                        dataRow["IndexNumber"] = currentIndexNumber;
                        dataRow["Carrier"] = "PSTN";
                        dataRow["UserPhoneId"] = currentUserPhoneId ?? (object)DBNull.Value;
                        dataRow["EbillUserId"] = currentEbillUserId ?? (object)DBNull.Value;
                        dataRow["BillingPeriod"] = billingPeriodString;
                        dataRow["CallMonth"] = billingMonth;
                        dataRow["CallYear"] = billingYear;
                        dataRow["CreatedDate"] = DateTime.UtcNow;
                        dataRow["ImportJobId"] = jobId;

                        dataTable.Rows.Add(dataRow);
                        result.SuccessCount++;

                        // Bulk insert in batches
                        if (dataTable.Rows.Count >= BATCH_SIZE)
                        {
                            await BulkInsertPSTNAsync(dataTable);
                            await UpdateJobStatus(jobId, "Processing", result.SuccessCount, result.ErrorCount, null);
                            dataTable.Clear();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error processing PSTN row {Row}", rowNumber);
                        result.ErrorCount++;
                    }
                }

                var extensionsSample = foundExtensions.Count > 0 ? string.Join(", ", foundExtensions) : "NONE";
                _logger.LogInformation("PSTN Excel grouped format stats: ExtensionHeaders={ExtHeaders}, DateRows={DateRows}, SkippedNoExtension={Skipped}, FirstExtensions={Extensions}",
                    extensionHeadersFound, dateRowsFound, skippedNoExtension, extensionsSample);

                // Save diagnostic info including auto-creation stats
                var autoCreateInfo = autoCreateResult != null
                    ? $"Auto-created: {autoCreateResult.UsersCreated} users, {autoCreateResult.PhonesCreated} phones. "
                    : "";
                var diagMsg = $"GROUPED format. {autoCreateInfo}ExtHeaders: {extensionHeadersFound}, DateRows: {dateRowsFound}, SkippedNoExt: {skippedNoExtension}, Extensions: [{extensionsSample}]. ColIndices: Time={colTime}, Distant={colDistant}, Duration={colDuration}, Cost={colCost}. Sample:\n{sampleInfo}";
                await UpdateJobStatus(jobId, "Processing", result.SuccessCount, result.ErrorCount, diagMsg);
            }

            // Insert remaining rows
            if (dataTable.Rows.Count > 0)
            {
                await BulkInsertPSTNAsync(dataTable);
            }

            // Log auto-creation summary
            if (autoCreateResult != null && autoCreateResult.UsersCreated > 0)
            {
                _logger.LogInformation("PSTN Excel import auto-created {UsersCreated} users and {PhonesCreated} phones",
                    autoCreateResult.UsersCreated, autoCreateResult.PhonesCreated);
            }

            _logger.LogInformation("PSTN Excel import completed: {Success} success, {Error} errors", result.SuccessCount, result.ErrorCount);
            return result;
        }

        private async Task<ImportResult> ImportPrivateWireExcelAsync(Guid jobId, string filePath, int billingMonth, int billingYear, IJobCancellationToken cancellationToken)
        {
            const int BATCH_SIZE = 50000;
            var result = new ImportResult { StartTime = DateTime.UtcNow };

            // Pre-load user phone lookups
            var userPhoneLookup = await LoadUserPhoneLookup(cancellationToken.ShutdownToken);
            // Load EbillUser lookup by IndexNumber for direct linking from Staff ID in file
            var ebillUserLookup = await LoadEbillUserLookupByIndexNumber(cancellationToken.ShutdownToken);

            // Required for .xls support
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            IExcelDataReader reader;
            FileStream stream;
            try
            {
                stream = File.Open(filePath, FileMode.Open, FileAccess.Read);
                reader = ExcelReaderFactory.CreateReader(stream);
            }
            catch (BadImageFormatException ex)
            {
                _logger.LogError(ex, "BadImageFormatException reading Excel file - this usually indicates a 32-bit/64-bit mismatch. Try converting .xls to .xlsx format.");
                throw new InvalidOperationException($"Cannot read .xls file due to platform compatibility. Please convert the file to .xlsx format and try again. Error: {ex.Message}", ex);
            }

            // Create DataTable for PrivateWire
            var dataTable = CreatePrivateWireDataTable();
            var billingPeriodString = new DateTime(billingYear, billingMonth, 1).ToString("yyyy-MM-dd");

            string currentExtension = "";
            string currentIndexNumber = "";
            string currentIndexNumberFromFile = ""; // IndexNumber extracted from Staff ID in file
            int? currentUserPhoneId = null;
            int? currentEbillUserId = null;
            int rowNumber = 0;
            int extensionHeadersFound = 0;
            int dateRowsFound = 0;
            int skippedNoExtension = 0;
            var foundExtensions = new List<string>(); // Track first few found extensions for diagnostics
            var extractedUsersForAutoCreate = new Dictionary<string, ExtractedUserInfo>(); // Track users for auto-creation
            UserCreationResult? autoCreateResult = null;

            // First, try to detect format by reading first 20 rows
            var sampleRows = new List<string>();
            var sampleInfo = new System.Text.StringBuilder();
            Dictionary<string, int>? tabularColumnIndices = null;
            bool isTabularFormat = false;

            while (reader.Read() && rowNumber < 20)
            {
                rowNumber++;
                var rowValues = new List<string>();
                for (int col = 0; col < Math.Min(reader.FieldCount, 10); col++)
                {
                    rowValues.Add(reader.GetValue(col)?.ToString()?.Trim() ?? "");
                }
                var rowStr = string.Join(" | ", rowValues);
                sampleRows.Add($"Row {rowNumber}: {rowStr}");

                // Check if this looks like a tabular header row
                var lowerValues = rowValues.Select(v => v.ToLower()).ToList();
                if (lowerValues.Any(v => v.Contains("extension") || v.Contains("ext")) &&
                    lowerValues.Any(v => v.Contains("date")) &&
                    lowerValues.Any(v => v.Contains("cost") || v.Contains("amount") || v.Contains("charge")))
                {
                    isTabularFormat = true;
                    tabularColumnIndices = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                    for (int i = 0; i < lowerValues.Count; i++)
                    {
                        if (!string.IsNullOrWhiteSpace(lowerValues[i]) && !tabularColumnIndices.ContainsKey(lowerValues[i]))
                            tabularColumnIndices[lowerValues[i]] = i;
                    }
                    _logger.LogInformation("PrivateWire Excel: Detected TABULAR format with columns: {Columns}", string.Join(", ", tabularColumnIndices.Keys));
                    break;
                }
            }

            sampleInfo = new System.Text.StringBuilder(string.Join("\n", sampleRows.Take(10)));
            _logger.LogInformation("PrivateWire Excel first rows sample:\n{Sample}", sampleInfo);

            // For GROUPED format: First pass to extract all extensions with Staff names for auto-creation
            if (!isTabularFormat)
            {
                _logger.LogInformation("PrivateWire Excel: Running first pass to extract extensions for auto-creation");

                // Reset reader for first pass
                reader.Dispose();
                stream.Dispose();
                using var firstPassStream = File.Open(filePath, FileMode.Open, FileAccess.Read);
                using var firstPassReader = ExcelReaderFactory.CreateReader(firstPassStream);

                while (firstPassReader.Read())
                {
                    cancellationToken.ShutdownToken.ThrowIfCancellationRequested();
                    var cellA = firstPassReader.GetValue(0)?.ToString()?.Trim() ?? "";

                    // Check for extension header row
                    // Full format: "Org 187: WFP, Division 1477: RO, ..., Staff ID 6726: 8847535, ..., Extension 2:22959: Yaa Rachael"
                    var extIndex = cellA.IndexOf("Extension", StringComparison.OrdinalIgnoreCase);
                    if (extIndex >= 0)
                    {
                        var extPart = cellA.Substring(extIndex);

                        // Pattern: "Extension 2:22959: Yaa Rachael" - extract extension and name
                        var match = System.Text.RegularExpressions.Regex.Match(extPart, @"Extension\s+\d+:(\d+):\s*(.+?)(?:\s*$|,)");
                        if (match.Success && match.Groups[1].Success)
                        {
                            var ext = NormalizePhoneNumber(match.Groups[1].Value);
                            var name = match.Groups[2].Success ? match.Groups[2].Value.Trim() : "";

                            // Also extract Staff ID (index number) from the full header
                            // Pattern: "Staff ID 6726: 8847535"
                            string? indexNumber = null;
                            var empIdMatch = System.Text.RegularExpressions.Regex.Match(cellA, @"Staff ID\s+\d+:\s*(\d+)");
                            if (empIdMatch.Success && empIdMatch.Groups[1].Success)
                            {
                                indexNumber = empIdMatch.Groups[1].Value.Trim();
                            }

                            if (!string.IsNullOrWhiteSpace(ext) && !string.IsNullOrWhiteSpace(name) && !extractedUsersForAutoCreate.ContainsKey(ext))
                            {
                                extractedUsersForAutoCreate[ext] = new ExtractedUserInfo
                                {
                                    Extension = ext,
                                    EmployeeName = name,
                                    IndexNumber = indexNumber
                                };
                                _logger.LogDebug("First pass extracted: Extension={Ext}, Name={Name}, IndexNumber={Index}",
                                    ext, name, indexNumber ?? "N/A");
                            }
                        }
                    }
                }

                _logger.LogInformation("PrivateWire Excel: First pass found {Count} unique extensions with names", extractedUsersForAutoCreate.Count);

                // Auto-create missing users
                if (extractedUsersForAutoCreate.Count > 0)
                {
                    _logger.LogInformation("PrivateWire Excel: Starting auto-creation of missing users");
                    autoCreateResult = await _userCreationService.AutoCreateMissingUsersAsync(
                        extractedUsersForAutoCreate, jobId, cancellationToken.ShutdownToken);

                    _logger.LogInformation("PrivateWire Excel: Auto-creation completed. Created: {UsersCreated} users, {PhonesCreated} phones",
                        autoCreateResult.UsersCreated, autoCreateResult.PhonesCreated);

                    // Reload user phone lookup if any users were created
                    if (autoCreateResult.UsersCreated > 0)
                    {
                        _logger.LogInformation("PrivateWire Excel: Reloading user phone lookup after auto-creation");
                        userPhoneLookup = await LoadUserPhoneLookup(cancellationToken.ShutdownToken);
                    }
                }
            }
            else
            {
                // For tabular format, close the initial readers
                reader.Dispose();
                stream.Dispose();
            }

            // Reset reader to start - close and reopen for clean state
            using var stream2 = File.Open(filePath, FileMode.Open, FileAccess.Read);
            using var reader2 = ExcelReaderFactory.CreateReader(stream2);
            rowNumber = 0;

            if (isTabularFormat && tabularColumnIndices != null)
            {
                // Process tabular format (like CSV with headers)
                _logger.LogInformation("PrivateWire Excel: Processing as TABULAR format");
                bool headerSkipped = false;

                while (reader2.Read())
                {
                    cancellationToken.ShutdownToken.ThrowIfCancellationRequested();
                    rowNumber++;

                    try
                    {
                        // Skip header row
                        if (!headerSkipped)
                        {
                            var firstCell = reader2.GetValue(0)?.ToString()?.ToLower() ?? "";
                            if (firstCell.Contains("ext") || firstCell.Contains("date"))
                            {
                                headerSkipped = true;
                                continue;
                            }
                        }

                        // Get extension from column
                        var extension = GetTabularValue(reader2, tabularColumnIndices, new[] { "extension", "ext", "callingno", "calling no" });
                        extension = NormalizePhoneNumber(extension);
                        if (string.IsNullOrWhiteSpace(extension)) continue;

                        // Get other values
                        var dateStr = GetTabularValue(reader2, tabularColumnIndices, new[] { "date", "call_date", "calldate" });
                        if (!DateTime.TryParse(dateStr, out var callDate)) continue;

                        var phoneFound = userPhoneLookup.TryGetValue(extension, out var userPhone);

                        var time = GetTabularValue(reader2, tabularColumnIndices, new[] { "time", "call_time", "calltime" });
                        var callType = GetTabularValue(reader2, tabularColumnIndices, new[] { "calltype", "call_type", "type", "destinationline" });
                        var place = GetTabularValue(reader2, tabularColumnIndices, new[] { "destination", "place", "dest" });
                        var distantNumber = GetTabularValue(reader2, tabularColumnIndices, new[] { "dialednumber", "dialed", "dialedno", "distant_number", "distantnumber", "number" });
                        var durationStr = GetTabularValue(reader2, tabularColumnIndices, new[] { "duration", "dur" });
                        var costStr = GetTabularValue(reader2, tabularColumnIndices, new[] { "cost", "amount", "amountusd", "charges", "charge" });

                        var duration = ParseDurationToSeconds(durationStr);
                        var cost = ParseDecimalOrZero(costStr);

                        var dataRow = dataTable.NewRow();
                        dataRow["Extension"] = extension;
                        dataRow["DialedNumber"] = distantNumber ?? "";
                        dataRow["CallTime"] = ParseTimeSpan(time);
                        dataRow["Destination"] = place ?? "";
                        dataRow["DestinationLine"] = callType ?? "";
                        dataRow["DurationExtended"] = duration;
                        dataRow["CallDate"] = callDate;
                        dataRow["Duration"] = duration;
                        dataRow["AmountKSH"] = 0m;
                        dataRow["AmountUSD"] = cost; // PrivateWire costs are in USD
                        dataRow["IndexNumber"] = phoneFound ? userPhone.IndexNumber ?? "" : "";
                        dataRow["UserPhoneId"] = phoneFound ? (object)userPhone.Id : DBNull.Value;
                        dataRow["EbillUserId"] = phoneFound ? (object?)userPhone.EbillUserId : DBNull.Value;
                        dataRow["BillingPeriod"] = billingPeriodString;
                        dataRow["CallMonth"] = billingMonth;
                        dataRow["CallYear"] = billingYear;
                        dataRow["CreatedDate"] = DateTime.UtcNow;
                        dataRow["ImportJobId"] = jobId;

                        dataTable.Rows.Add(dataRow);
                        result.SuccessCount++;

                        if (dataTable.Rows.Count >= BATCH_SIZE)
                        {
                            await BulkInsertPrivateWireAsync(dataTable);
                            await UpdateJobStatus(jobId, "Processing", result.SuccessCount, result.ErrorCount, null);
                            dataTable.Clear();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error processing PrivateWire tabular row {Row}", rowNumber);
                        result.ErrorCount++;
                    }
                }
            }
            else
            {
                // Process grouped format (Extension 1234 headers followed by data rows)
                _logger.LogInformation("PrivateWire Excel: Processing as GROUPED format (Extension headers)");

                // Track column indices - will be detected from header rows
                int colTime = 1, colCallType = 2, colPlace = 3, colDistant = 4, colDuration = 5, colCost = 6;
                bool columnsDetected = false;

                while (reader2.Read())
                {
                    cancellationToken.ShutdownToken.ThrowIfCancellationRequested();
                    rowNumber++;

                    try
                    {
                        var cellA = reader2.GetValue(0)?.ToString()?.Trim() ?? "";
                        var fieldCount = reader2.FieldCount;

                        // Detect column structure from header row
                        if (!columnsDetected && cellA.ToLower() == "date")
                        {
                            // Read header row to detect column positions
                            var headerRow = new List<string>();
                            for (int col = 0; col < Math.Min(fieldCount, 15); col++)
                            {
                                headerRow.Add(reader2.GetValue(col)?.ToString()?.Trim()?.ToLower() ?? "");
                            }

                            _logger.LogInformation("PrivateWire: Detected header row: {Headers}", string.Join(" | ", headerRow));
                            sampleInfo.AppendLine($"Header: {string.Join(" | ", headerRow)}");

                            // Find column indices based on header names
                            for (int i = 0; i < headerRow.Count; i++)
                            {
                                var h = headerRow[i];
                                if (h == "time") colTime = i;
                                else if (h == "call" || h == "calltype" || h == "call type" || h == "type") colCallType = i;
                                else if (h == "place" || h == "destination") colPlace = i;
                                else if (h == "distant" || h == "distantnumber" || h == "distant number" || h == "dialed" || h == "dialednumber") colDistant = i;
                                else if (h == "duration" || h == "dur") colDuration = i;
                                else if (h == "cost" || h == "amount" || h == "charges" || h == "charge") colCost = i;
                                else if (h == "band") { /* skip band column */ }
                                else if (h == "rate") { /* skip rate column */ }
                                else if (h == "ringtime") { /* skip ringtime column */ }
                            }

                            columnsDetected = true;
                            _logger.LogInformation("PrivateWire column indices: Time={Time}, CallType={CallType}, Place={Place}, Distant={Distant}, Duration={Duration}, Cost={Cost}",
                                colTime, colCallType, colPlace, colDistant, colDuration, colCost);
                            continue;
                        }

                        // Check for extension header row - extension info may be embedded in a longer string
                        // Format examples:
                        //   "Extension 1234" (simple)
                        //   "Extension 2:21588: Makarigakis" (complex - extension is after first colon)
                        //   "Org 145:..., Extension 2:21588: Name" (embedded within other data)
                        var extensionIndex = cellA.IndexOf("Extension", StringComparison.OrdinalIgnoreCase);
                        if (extensionIndex >= 0)
                        {
                            extensionHeadersFound++;
                            var extensionPart = cellA.Substring(extensionIndex);

                            // Try to extract extension number and Staff name using pattern: "Extension X:NNNNN: Name" or "Extension NNNNN"
                            string? extractedExtension = null;
                            string? extractedName = null;

                            // Pattern 1: "Extension 2:21588: Name" - number after first colon, name after second colon
                            var colonMatch = System.Text.RegularExpressions.Regex.Match(extensionPart, @"Extension\s+\d+:(\d+):\s*(.+?)(?:\s*$|,)");
                            if (colonMatch.Success && colonMatch.Groups[1].Success)
                            {
                                extractedExtension = colonMatch.Groups[1].Value;
                                if (colonMatch.Groups[2].Success)
                                {
                                    extractedName = colonMatch.Groups[2].Value.Trim();
                                }
                            }
                            else
                            {
                                // Pattern 2: "Extension 21588" or "Extension: 21588" - simple format (no name available)
                                var simpleMatch = System.Text.RegularExpressions.Regex.Match(extensionPart, @"Extension[:\s]+(\d+)");
                                if (simpleMatch.Success && simpleMatch.Groups[1].Success)
                                {
                                    extractedExtension = simpleMatch.Groups[1].Value;
                                }
                            }

                            if (!string.IsNullOrWhiteSpace(extractedExtension))
                            {
                                currentExtension = NormalizePhoneNumber(extractedExtension);
                                _logger.LogDebug("Found PrivateWire extension: {Extension}, Name: {Name} from row: {Row}",
                                    currentExtension, extractedName ?? "N/A", cellA.Substring(0, Math.Min(100, cellA.Length)));

                                // Track first few extensions found for diagnostics
                                if (foundExtensions.Count < 10 && !foundExtensions.Contains(currentExtension))
                                    foundExtensions.Add(currentExtension);

                                // Extract Staff ID (IndexNumber) from full header row for direct EbillUser linking
                                // Pattern: "Staff ID 6726: 8847535" - extract 8847535
                                currentIndexNumberFromFile = "";
                                var staffIdMatch = System.Text.RegularExpressions.Regex.Match(cellA, @"Staff ID\s+\d+:\s*(\d+)");
                                if (staffIdMatch.Success && staffIdMatch.Groups[1].Success)
                                {
                                    currentIndexNumberFromFile = staffIdMatch.Groups[1].Value.Trim();
                                }

                                // Priority 1: Try to link directly using IndexNumber from file (Staff ID)
                                if (!string.IsNullOrEmpty(currentIndexNumberFromFile) && ebillUserLookup.TryGetValue(currentIndexNumberFromFile, out var ebillUserId))
                                {
                                    currentIndexNumber = currentIndexNumberFromFile;
                                    currentEbillUserId = ebillUserId;
                                    // Try to get UserPhoneId if available
                                    if (userPhoneLookup.TryGetValue(currentExtension, out var userPhone))
                                    {
                                        currentUserPhoneId = userPhone.Id;
                                    }
                                    else
                                    {
                                        currentUserPhoneId = null;
                                    }
                                    _logger.LogDebug("PrivateWire: Linked via IndexNumber from file: {IndexNumber} -> EbillUserId: {EbillUserId}", currentIndexNumberFromFile, ebillUserId);
                                }
                                // Priority 2: Fall back to UserPhone lookup by Extension
                                else if (userPhoneLookup.TryGetValue(currentExtension, out var userPhone))
                                {
                                    currentIndexNumber = userPhone.IndexNumber ?? "";
                                    currentUserPhoneId = userPhone.Id;
                                    currentEbillUserId = userPhone.EbillUserId;
                                    _logger.LogDebug("PrivateWire: Linked via UserPhone: Extension {Extension} -> EbillUserId: {EbillUserId}", currentExtension, currentEbillUserId);
                                }
                                else
                                {
                                    currentIndexNumber = currentIndexNumberFromFile; // Keep the index number from file even if not linked
                                    currentUserPhoneId = null;
                                    currentEbillUserId = null;
                                    _logger.LogDebug("PrivateWire: No link found for Extension {Extension}, IndexNumber from file: {IndexNumber}", currentExtension, currentIndexNumberFromFile);
                                }
                            }
                            continue;
                        }

                        // Skip header rows and empty rows
                        if (string.IsNullOrWhiteSpace(cellA) || cellA.ToLower() == "date") continue;

                        // Try to parse as date
                        if (!DateTime.TryParse(cellA, out var callDate))
                        {
                            continue;
                        }

                        dateRowsFound++;

                        if (string.IsNullOrWhiteSpace(currentExtension))
                        {
                            skippedNoExtension++;
                            continue;
                        }

                        // Parse data row using detected column indices (with bounds checking)
                        string SafeGetValue(int col) => col < fieldCount ? (reader2.GetValue(col)?.ToString()?.Trim() ?? "") : "";

                        var time = SafeGetValue(colTime);
                        var callType = SafeGetValue(colCallType);
                        var place = SafeGetValue(colPlace);
                        var distantNumber = SafeGetValue(colDistant);
                        var durationStr = SafeGetValue(colDuration);
                        var costStr = SafeGetValue(colCost);

                        var duration = ParseDurationToSeconds(durationStr);
                        var cost = ParseDecimalOrZero(costStr);

                        var dataRow = dataTable.NewRow();
                        dataRow["Extension"] = currentExtension;
                        dataRow["DialedNumber"] = distantNumber;
                        dataRow["CallTime"] = ParseTimeSpan(time);
                        dataRow["Destination"] = place;
                        dataRow["DestinationLine"] = callType;
                        dataRow["DurationExtended"] = duration;
                        dataRow["CallDate"] = callDate;
                        dataRow["Duration"] = duration;
                        dataRow["AmountKSH"] = 0m;
                        dataRow["AmountUSD"] = cost; // PrivateWire costs are in USD
                        dataRow["IndexNumber"] = currentIndexNumber;
                        dataRow["UserPhoneId"] = currentUserPhoneId ?? (object)DBNull.Value;
                        dataRow["EbillUserId"] = currentEbillUserId ?? (object)DBNull.Value;
                        dataRow["BillingPeriod"] = billingPeriodString;
                        dataRow["CallMonth"] = billingMonth;
                        dataRow["CallYear"] = billingYear;
                        dataRow["CreatedDate"] = DateTime.UtcNow;
                        dataRow["ImportJobId"] = jobId;

                        dataTable.Rows.Add(dataRow);
                        result.SuccessCount++;

                        // Bulk insert in batches
                        if (dataTable.Rows.Count >= BATCH_SIZE)
                        {
                            await BulkInsertPrivateWireAsync(dataTable);
                            await UpdateJobStatus(jobId, "Processing", result.SuccessCount, result.ErrorCount, null);
                            dataTable.Clear();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error processing PrivateWire row {Row}", rowNumber);
                        result.ErrorCount++;
                    }
                }

                var extensionsSample = foundExtensions.Count > 0 ? string.Join(", ", foundExtensions) : "NONE";
                _logger.LogInformation("PrivateWire Excel grouped format stats: ExtensionHeaders={ExtHeaders}, DateRows={DateRows}, SkippedNoExtension={Skipped}, Extensions={Extensions}",
                    extensionHeadersFound, dateRowsFound, skippedNoExtension, extensionsSample);

                // Save diagnostic info including auto-creation stats
                var autoCreateInfo = autoCreateResult != null
                    ? $"Auto-created: {autoCreateResult.UsersCreated} users, {autoCreateResult.PhonesCreated} phones. "
                    : "";
                var diagMsg = $"GROUPED format. {autoCreateInfo}ExtHeaders: {extensionHeadersFound}, DateRows: {dateRowsFound}, SkippedNoExt: {skippedNoExtension}, Extensions: [{extensionsSample}]. ColIndices: Time={colTime}, Distant={colDistant}, Duration={colDuration}, Cost={colCost}. Sample:\n{sampleInfo}";
                await UpdateJobStatus(jobId, "Processing", result.SuccessCount, result.ErrorCount, diagMsg);
            }

            // Insert remaining rows
            if (dataTable.Rows.Count > 0)
            {
                await BulkInsertPrivateWireAsync(dataTable);
            }

            // Log auto-creation summary
            if (autoCreateResult != null && autoCreateResult.UsersCreated > 0)
            {
                _logger.LogInformation("PrivateWire Excel import auto-created {UsersCreated} users and {PhonesCreated} phones",
                    autoCreateResult.UsersCreated, autoCreateResult.PhonesCreated);
            }

            _logger.LogInformation("PrivateWire Excel import completed: {Success} success, {Error} errors", result.SuccessCount, result.ErrorCount);
            return result;
        }

        #region CSV Import Methods

        /// <summary>
        /// Import Mobile (Safaricom/Airtel) CSV files
        /// CSV format: CallingNo, Date, Time, DialledNo, Duration, CallCharges, CallType
        /// </summary>
        private async Task<ImportResult> ImportMobileCsvAsync(Guid jobId, string filePath, int billingMonth, int billingYear, string provider, IJobCancellationToken cancellationToken)
        {
            const int BATCH_SIZE = 50000;
            var result = new ImportResult { StartTime = DateTime.UtcNow };

            // Pre-load user phone lookups
            var userPhoneLookup = await LoadUserPhoneLookup(cancellationToken.ShutdownToken);

            var dataTable = CreateMobileDataTable();
            var billingPeriodString = new DateTime(billingYear, billingMonth, 1).ToString("yyyy-MM-dd");

            using var reader = new StreamReader(filePath);
            string? line;
            int lineNumber = 0;
            Dictionary<string, int>? columnIndices = null;
            bool headerLogged = false;

            _logger.LogInformation("Starting CSV import from {FilePath} for {Provider}", filePath, provider);

            while ((line = await reader.ReadLineAsync()) != null)
            {
                cancellationToken.ShutdownToken.ThrowIfCancellationRequested();
                lineNumber++;

                try
                {
                    var values = ParseCsvLine(line);
                    if (values.Count == 0) continue;

                    // Find header row
                    if (columnIndices == null)
                    {
                        var lowerValues = values.Select(v => v.ToLower().Trim()).ToList();

                        // Log first few lines to help debug header detection
                        if (lineNumber <= 5)
                        {
                            _logger.LogInformation("CSV line {LineNumber}: {Values}", lineNumber, string.Join(", ", values.Take(10)));
                        }

                        if (IsMobileCsvHeaderRow(lowerValues, provider))
                        {
                            columnIndices = BuildCsvColumnIndex(lowerValues);
                            _logger.LogInformation("Header found at line {LineNumber} for {Provider}. Columns: {Columns}",
                                lineNumber, provider, string.Join(", ", columnIndices.Keys));
                            headerLogged = true;
                            continue;
                        }

                        // Warn if we've checked many lines without finding header
                        if (lineNumber == 50 && !headerLogged)
                        {
                            _logger.LogWarning("No header row found in first 50 lines for {Provider}. Last checked values: {Values}",
                                provider, string.Join(", ", values.Take(10)));
                        }
                        continue;
                    }

                    // Parse data row
                    var callingNo = NormalizePhoneNumber(GetCsvValue(values, columnIndices, "callingno", provider));
                    if (string.IsNullOrWhiteSpace(callingNo)) continue;

                    var phoneFound = userPhoneLookup.TryGetValue(callingNo, out var userPhone);

                    var dataRow = dataTable.NewRow();
                    dataRow["ext"] = callingNo;
                    dataRow["call_date"] = ParseCsvDate(values, columnIndices, "date", provider);
                    dataRow["call_time"] = ParseCsvTime(values, columnIndices, "time", provider);
                    dataRow["dialed"] = GetCsvValue(values, columnIndices, "dialedno", provider) ?? "";
                    dataRow["dur"] = ParseCsvDecimal(values, columnIndices, "duration", provider);
                    dataRow["cost"] = ParseCsvDecimal(values, columnIndices, "charges", provider);
                    dataRow["call_type"] = GetCsvValue(values, columnIndices, "calltype", provider) ?? "";
                    dataRow["IndexNumber"] = phoneFound ? userPhone.IndexNumber ?? "" : "";
                    dataRow["UserPhoneId"] = phoneFound ? (object)userPhone.Id : DBNull.Value;
                    dataRow["EbillUserId"] = phoneFound ? (object?)userPhone.EbillUserId : DBNull.Value;
                    dataRow["BillingPeriod"] = billingPeriodString;
                    dataRow["call_month"] = billingMonth;
                    dataRow["call_year"] = billingYear;
                    dataRow["CreatedDate"] = DateTime.UtcNow;
                    dataRow["ProcessingStatus"] = 0; // Staged
                    dataRow["ImportJobId"] = jobId;

                    dataTable.Rows.Add(dataRow);
                    result.SuccessCount++;

                    // Bulk insert in batches
                    if (dataTable.Rows.Count >= BATCH_SIZE)
                    {
                        await BulkInsertMobileAsync(dataTable, provider);
                        await UpdateJobStatus(jobId, "Processing", result.SuccessCount, result.ErrorCount, null);
                        dataTable.Clear();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error processing CSV line {Line}", lineNumber);
                    result.ErrorCount++;
                }
            }

            // Insert remaining rows
            if (dataTable.Rows.Count > 0)
            {
                await BulkInsertMobileAsync(dataTable, provider);
            }

            // Log completion status
            if (columnIndices == null)
            {
                _logger.LogError("No header row was found in the CSV file for {Provider}. File: {FilePath}, Lines checked: {LineNumber}",
                    provider, filePath, lineNumber);
            }
            else
            {
                _logger.LogInformation("CSV import completed for {Provider}. Total lines: {LineNumber}, Success: {Success}, Errors: {Errors}",
                    provider, lineNumber, result.SuccessCount, result.ErrorCount);
            }

            return result;
        }

        /// <summary>
        /// Import PSTN CSV files
        /// CSV format typically: Extension, Date, Time, DialedNumber, Duration, Cost, CallType
        /// </summary>
        private async Task<ImportResult> ImportPstnCsvAsync(Guid jobId, string filePath, int billingMonth, int billingYear, IJobCancellationToken cancellationToken)
        {
            const int BATCH_SIZE = 50000;
            var result = new ImportResult { StartTime = DateTime.UtcNow };

            // Pre-load user phone lookups
            var userPhoneLookup = await LoadUserPhoneLookup(cancellationToken.ShutdownToken);

            var dataTable = CreatePSTNDataTable();
            var billingPeriodString = new DateTime(billingYear, billingMonth, 1).ToString("yyyy-MM-dd");

            using var reader = new StreamReader(filePath);
            string? line;
            int lineNumber = 0;
            Dictionary<string, int>? columnIndices = null;

            while ((line = await reader.ReadLineAsync()) != null)
            {
                cancellationToken.ShutdownToken.ThrowIfCancellationRequested();
                lineNumber++;

                try
                {
                    var values = ParseCsvLine(line);
                    if (values.Count == 0) continue;

                    // Find header row
                    if (columnIndices == null)
                    {
                        var lowerValues = values.Select(v => v.ToLower().Trim()).ToList();
                        if (IsPstnCsvHeaderRow(lowerValues))
                        {
                            columnIndices = BuildCsvColumnIndex(lowerValues);
                            continue;
                        }
                        continue;
                    }

                    // Parse data row
                    var extension = NormalizePhoneNumber(GetCsvValueByAnyKey(values, columnIndices, new[] { "extension", "ext", "callingno" }));
                    if (string.IsNullOrWhiteSpace(extension)) continue;

                    var phoneFound = userPhoneLookup.TryGetValue(extension, out var userPhone);

                    var callDate = ParseCsvDateByAnyKey(values, columnIndices, new[] { "date", "call_date", "calldate" });
                    var callTime = ParseCsvTimeByAnyKey(values, columnIndices, new[] { "time", "call_time", "calltime" });
                    var dialedNumber = GetCsvValueByAnyKey(values, columnIndices, new[] { "dialedno", "dialed", "dialednumber", "dialednum", "distant_number", "distantnumber" }) ?? "";
                    var duration = ParseCsvDecimalByAnyKey(values, columnIndices, new[] { "duration", "dur" });
                    var cost = ParseCsvDecimalByAnyKey(values, columnIndices, new[] { "cost", "amount", "charges", "callcharges", "amountksh" });
                    var callType = GetCsvValueByAnyKey(values, columnIndices, new[] { "calltype", "type", "destination", "destinationline" }) ?? "";
                    var place = GetCsvValueByAnyKey(values, columnIndices, new[] { "place", "destination", "dest" }) ?? "";

                    var dataRow = dataTable.NewRow();
                    dataRow["Extension"] = extension;
                    dataRow["DialedNumber"] = dialedNumber;
                    dataRow["CallTime"] = callTime;
                    dataRow["Destination"] = place;
                    dataRow["DestinationLine"] = callType;
                    dataRow["DurationExtended"] = duration;
                    dataRow["CallDate"] = callDate;
                    dataRow["Duration"] = duration;
                    dataRow["AmountKSH"] = cost; // PSTN costs are in KES
                    dataRow["AmountUSD"] = 0m;
                    dataRow["IndexNumber"] = phoneFound ? userPhone.IndexNumber ?? "" : "";
                    dataRow["Carrier"] = "PSTN";
                    dataRow["UserPhoneId"] = phoneFound ? (object)userPhone.Id : DBNull.Value;
                    dataRow["EbillUserId"] = phoneFound ? (object?)userPhone.EbillUserId : DBNull.Value;
                    dataRow["BillingPeriod"] = billingPeriodString;
                    dataRow["CallMonth"] = billingMonth;
                    dataRow["CallYear"] = billingYear;
                    dataRow["CreatedDate"] = DateTime.UtcNow;
                    dataRow["ImportJobId"] = jobId;

                    dataTable.Rows.Add(dataRow);
                    result.SuccessCount++;

                    // Bulk insert in batches
                    if (dataTable.Rows.Count >= BATCH_SIZE)
                    {
                        await BulkInsertPSTNAsync(dataTable);
                        await UpdateJobStatus(jobId, "Processing", result.SuccessCount, result.ErrorCount, null);
                        dataTable.Clear();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error processing PSTN CSV line {Line}", lineNumber);
                    result.ErrorCount++;
                }
            }

            // Insert remaining rows
            if (dataTable.Rows.Count > 0)
            {
                await BulkInsertPSTNAsync(dataTable);
            }

            return result;
        }

        /// <summary>
        /// Import PrivateWire CSV files
        /// </summary>
        private async Task<ImportResult> ImportPrivateWireCsvAsync(Guid jobId, string filePath, int billingMonth, int billingYear, IJobCancellationToken cancellationToken)
        {
            const int BATCH_SIZE = 50000;
            var result = new ImportResult { StartTime = DateTime.UtcNow };

            // Pre-load user phone lookups
            var userPhoneLookup = await LoadUserPhoneLookup(cancellationToken.ShutdownToken);

            var dataTable = CreatePrivateWireDataTable();
            var billingPeriodString = new DateTime(billingYear, billingMonth, 1).ToString("yyyy-MM-dd");

            using var reader = new StreamReader(filePath);
            string? line;
            int lineNumber = 0;
            Dictionary<string, int>? columnIndices = null;

            while ((line = await reader.ReadLineAsync()) != null)
            {
                cancellationToken.ShutdownToken.ThrowIfCancellationRequested();
                lineNumber++;

                try
                {
                    var values = ParseCsvLine(line);
                    if (values.Count == 0) continue;

                    // Find header row
                    if (columnIndices == null)
                    {
                        var lowerValues = values.Select(v => v.ToLower().Trim()).ToList();
                        if (IsPrivateWireCsvHeaderRow(lowerValues))
                        {
                            columnIndices = BuildCsvColumnIndex(lowerValues);
                            continue;
                        }
                        continue;
                    }

                    // Parse data row
                    var extension = NormalizePhoneNumber(GetCsvValueByAnyKey(values, columnIndices, new[] { "extension", "ext", "callingno" }));
                    if (string.IsNullOrWhiteSpace(extension)) continue;

                    var phoneFound = userPhoneLookup.TryGetValue(extension, out var userPhone);

                    var callDate = ParseCsvDateByAnyKey(values, columnIndices, new[] { "date", "call_date", "calldate" });
                    var callTime = ParseCsvTimeByAnyKey(values, columnIndices, new[] { "time", "call_time", "calltime" });
                    var dialedNumber = GetCsvValueByAnyKey(values, columnIndices, new[] { "dialedno", "dialed", "dialednumber", "dialednum" }) ?? "";
                    var duration = ParseCsvDecimalByAnyKey(values, columnIndices, new[] { "duration", "dur" });
                    var cost = ParseCsvDecimalByAnyKey(values, columnIndices, new[] { "cost", "amount", "charges", "callcharges", "amountusd" });
                    var callType = GetCsvValueByAnyKey(values, columnIndices, new[] { "calltype", "type", "destinationline" }) ?? "";
                    var place = GetCsvValueByAnyKey(values, columnIndices, new[] { "place", "destination", "dest" }) ?? "";

                    var dataRow = dataTable.NewRow();
                    dataRow["Extension"] = extension;
                    dataRow["DialedNumber"] = dialedNumber;
                    dataRow["CallTime"] = callTime;
                    dataRow["Destination"] = place;
                    dataRow["DestinationLine"] = callType;
                    dataRow["DurationExtended"] = duration;
                    dataRow["CallDate"] = callDate;
                    dataRow["Duration"] = duration;
                    dataRow["AmountKSH"] = 0m;
                    dataRow["AmountUSD"] = cost; // PrivateWire costs are in USD
                    dataRow["IndexNumber"] = phoneFound ? userPhone.IndexNumber ?? "" : "";
                    dataRow["UserPhoneId"] = phoneFound ? (object)userPhone.Id : DBNull.Value;
                    dataRow["EbillUserId"] = phoneFound ? (object?)userPhone.EbillUserId : DBNull.Value;
                    dataRow["BillingPeriod"] = billingPeriodString;
                    dataRow["CallMonth"] = billingMonth;
                    dataRow["CallYear"] = billingYear;
                    dataRow["CreatedDate"] = DateTime.UtcNow;
                    dataRow["ImportJobId"] = jobId;

                    dataTable.Rows.Add(dataRow);
                    result.SuccessCount++;

                    // Bulk insert in batches
                    if (dataTable.Rows.Count >= BATCH_SIZE)
                    {
                        await BulkInsertPrivateWireAsync(dataTable);
                        await UpdateJobStatus(jobId, "Processing", result.SuccessCount, result.ErrorCount, null);
                        dataTable.Clear();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error processing PrivateWire CSV line {Line}", lineNumber);
                    result.ErrorCount++;
                }
            }

            // Insert remaining rows
            if (dataTable.Rows.Count > 0)
            {
                await BulkInsertPrivateWireAsync(dataTable);
            }

            return result;
        }

        #endregion

        #region CSV Helper Methods

        private List<string> ParseCsvLine(string line)
        {
            var result = new List<string>();
            if (string.IsNullOrWhiteSpace(line)) return result;

            bool inQuotes = false;
            var current = new System.Text.StringBuilder();

            foreach (var c in line)
            {
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(current.ToString().Trim());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }
            result.Add(current.ToString().Trim());

            return result;
        }

        private Dictionary<string, int> BuildCsvColumnIndex(List<string> headers)
        {
            var indices = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < headers.Count; i++)
            {
                var header = headers[i].ToLower().Trim();
                if (!string.IsNullOrWhiteSpace(header) && !indices.ContainsKey(header))
                    indices[header] = i;
            }
            return indices;
        }

        private bool IsMobileCsvHeaderRow(List<string> values, string provider)
        {
            // Normalize values by removing spaces for comparison
            var normalizedValues = values.Select(v => v.Replace(" ", "").ToLower()).ToList();

            if (provider.ToLower() == "airtel")
            {
                return normalizedValues.Any(v => v.Contains("msisdn")) && normalizedValues.Any(v => v.Contains("charge"));
            }
            else // Safaricom - check for callingno/calling no and callcharges/call charges
            {
                var hasCallingNo = normalizedValues.Any(v => v.Contains("callingno") || v.Contains("msisdn"));
                var hasCharges = normalizedValues.Any(v => v.Contains("callcharges") || v.Contains("charges") || v.Contains("cost"));
                return hasCallingNo && hasCharges;
            }
        }

        private bool IsPstnCsvHeaderRow(List<string> values)
        {
            // PSTN header should have extension/ext and some cost/amount field
            var hasExtension = values.Any(v => v.Contains("ext") || v.Contains("callingno"));
            var hasCost = values.Any(v => v.Contains("cost") || v.Contains("amount") || v.Contains("charge"));
            return hasExtension && hasCost;
        }

        private bool IsPrivateWireCsvHeaderRow(List<string> values)
        {
            // Similar to PSTN
            var hasExtension = values.Any(v => v.Contains("ext") || v.Contains("callingno"));
            var hasCost = values.Any(v => v.Contains("cost") || v.Contains("amount") || v.Contains("charge"));
            return hasExtension && hasCost;
        }

        private string? GetCsvValue(List<string> values, Dictionary<string, int> indices, string field, string provider)
        {
            // Define multiple possible column names for each field (handles variations with/without spaces)
            var possibleColumnNames = (provider.ToLower(), field.ToLower()) switch
            {
                ("airtel", "callingno") => new[] { "msisdn" },
                ("airtel", "date") => new[] { "charge date", "chargedate", "date" },
                ("airtel", "time") => new[] { "charge time", "chargetime", "time" },
                ("airtel", "dialedno") => new[] { "number", "dialedno", "dialled" },
                ("airtel", "duration") => new[] { "quantity", "duration" },
                ("airtel", "charges") => new[] { "charges", "charge", "cost" },
                ("airtel", "calltype") => new[] { "charge type", "chargetype", "calltype" },
                ("safaricom", "callingno") => new[] { "callingno", "calling no", "calling", "msisdn", "phone" },
                ("safaricom", "date") => new[] { "date" },
                ("safaricom", "time") => new[] { "time" },
                ("safaricom", "dialedno") => new[] { "dialledNo", "dialled no", "dialled", "dialedno", "dialed" },
                ("safaricom", "duration") => new[] { "duration" },
                ("safaricom", "charges") => new[] { "callcharges", "call charges", "charges", "cost" },
                ("safaricom", "calltype") => new[] { "calltype", "call type", "type" },
                _ => new[] { field }
            };

            // Try each possible column name
            foreach (var columnName in possibleColumnNames)
            {
                // Find the column (case-insensitive, also try without spaces)
                var normalizedColumnName = columnName.Replace(" ", "");
                var key = indices.Keys.FirstOrDefault(k =>
                    k.Contains(columnName, StringComparison.OrdinalIgnoreCase) ||
                    k.Replace(" ", "").Contains(normalizedColumnName, StringComparison.OrdinalIgnoreCase));

                if (key != null && indices.TryGetValue(key, out var idx) && idx < values.Count)
                    return values[idx];
            }

            return null;
        }

        private string? GetCsvValueByAnyKey(List<string> values, Dictionary<string, int> indices, string[] possibleKeys)
        {
            foreach (var key in possibleKeys)
            {
                var matchingKey = indices.Keys.FirstOrDefault(k => k.Contains(key, StringComparison.OrdinalIgnoreCase));
                if (matchingKey != null && indices.TryGetValue(matchingKey, out var idx) && idx < values.Count)
                {
                    var val = values[idx];
                    if (!string.IsNullOrWhiteSpace(val))
                        return val;
                }
            }
            return null;
        }

        private DateTime ParseCsvDate(List<string> values, Dictionary<string, int> indices, string field, string provider)
        {
            var value = GetCsvValue(values, indices, field, provider);
            if (string.IsNullOrWhiteSpace(value)) return DateTime.MinValue;
            if (DateTime.TryParse(value, out var date)) return date;
            return DateTime.MinValue;
        }

        private DateTime ParseCsvDateByAnyKey(List<string> values, Dictionary<string, int> indices, string[] possibleKeys)
        {
            var value = GetCsvValueByAnyKey(values, indices, possibleKeys);
            if (string.IsNullOrWhiteSpace(value)) return DateTime.MinValue;
            if (DateTime.TryParse(value, out var date)) return date;
            return DateTime.MinValue;
        }

        private TimeSpan ParseCsvTime(List<string> values, Dictionary<string, int> indices, string field, string provider)
        {
            var value = GetCsvValue(values, indices, field, provider);
            return ParseTimeSpan(value);
        }

        private TimeSpan ParseCsvTimeByAnyKey(List<string> values, Dictionary<string, int> indices, string[] possibleKeys)
        {
            var value = GetCsvValueByAnyKey(values, indices, possibleKeys);
            return ParseTimeSpan(value);
        }

        private decimal ParseCsvDecimal(List<string> values, Dictionary<string, int> indices, string field, string provider)
        {
            var value = GetCsvValue(values, indices, field, provider);
            return ParseDecimalOrZero(value);
        }

        private decimal ParseCsvDecimalByAnyKey(List<string> values, Dictionary<string, int> indices, string[] possibleKeys)
        {
            var value = GetCsvValueByAnyKey(values, indices, possibleKeys);
            return ParseDecimalOrZero(value);
        }

        #endregion

        #region Helper Methods

        private async Task UpdateJobStatus(Guid jobId, string status, int? recordsSuccess, int? recordsError, string? errorMessage)
        {
            var job = await _context.ImportJobs.FindAsync(jobId);
            if (job != null)
            {
                job.Status = status;
                if (status == "Processing" && job.StartedDate == null)
                    job.StartedDate = DateTime.UtcNow;
                if (status == "Completed" || status == "Failed")
                {
                    job.CompletedDate = DateTime.UtcNow;
                    if (job.StartedDate.HasValue)
                        job.DurationSeconds = (int)(job.CompletedDate.Value - job.StartedDate.Value).TotalSeconds;
                }
                if (recordsSuccess.HasValue)
                    job.RecordsSuccess = recordsSuccess.Value;
                if (recordsError.HasValue)
                    job.RecordsError = recordsError.Value;
                job.RecordsProcessed = (recordsSuccess ?? 0) + (recordsError ?? 0);
                if (!string.IsNullOrEmpty(errorMessage))
                    job.ErrorMessage = errorMessage.Length > 4000 ? errorMessage.Substring(0, 4000) : errorMessage;

                await _context.SaveChangesAsync();
            }
        }

        private async Task<Dictionary<string, (int Id, int? EbillUserId, string? IndexNumber)>> LoadUserPhoneLookup(System.Threading.CancellationToken ct)
        {
            var phones = await _context.UserPhones
                .AsNoTracking()
                .Where(p => p.IsActive)
                .Select(p => new {
                    p.Id,
                    p.PhoneNumber,
                    EbillUserId = p.EbillUser != null ? (int?)p.EbillUser.Id : null,
                    p.IndexNumber
                })
                .ToListAsync(ct);

            // Group by normalized phone number and take first to handle duplicates
            return phones
                .GroupBy(p => NormalizePhoneNumber(p.PhoneNumber), StringComparer.OrdinalIgnoreCase)
                .Where(g => !string.IsNullOrEmpty(g.Key)) // Exclude empty phone numbers
                .ToDictionary(
                    g => g.Key,
                    g => (g.First().Id, g.First().EbillUserId, g.First().IndexNumber),
                    StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Load EbillUser lookup by IndexNumber for direct linking (used by PSTN/PrivateWire imports)
        /// </summary>
        private async Task<Dictionary<string, int>> LoadEbillUserLookupByIndexNumber(System.Threading.CancellationToken ct)
        {
            var users = await _context.EbillUsers
                .AsNoTracking()
                .Where(u => u.IsActive && !string.IsNullOrEmpty(u.IndexNumber))
                .Select(u => new { u.Id, u.IndexNumber })
                .ToListAsync(ct);

            return users
                .GroupBy(u => u.IndexNumber, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key,
                    g => g.First().Id,
                    StringComparer.OrdinalIgnoreCase);
        }

        private static string NormalizePhoneNumber(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) return "";
            return new string(phone.Where(c => char.IsDigit(c)).ToArray());
        }

        private bool IsMobileHeaderRow(List<string> values, string provider)
        {
            if (provider.ToLower() == "airtel")
            {
                return values.Any(v => v.Contains("msisdn")) && values.Any(v => v.Contains("charge"));
            }
            else // Safaricom
            {
                return values.Any(v => v.Contains("callingno")) && values.Any(v => v.Contains("callcharges"));
            }
        }

        private Dictionary<string, int> BuildMobileColumnIndex(List<string> headers, string provider)
        {
            var indices = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < headers.Count; i++)
            {
                var header = headers[i].ToLower().Trim();
                if (!indices.ContainsKey(header))
                    indices[header] = i; // ExcelDataReader uses 0-based indexing
            }
            return indices;
        }

        #region ExcelDataReader Helper Methods

        private string? GetReaderValue(IExcelDataReader reader, Dictionary<string, int> indices, string field, string provider)
        {
            // Map generic field names to provider-specific column names
            var possibleColumnNames = (provider.ToLower(), field.ToLower()) switch
            {
                ("airtel", "callingno") => new[] { "msisdn" },
                ("airtel", "date") => new[] { "charge date", "chargedate", "date" },
                ("airtel", "time") => new[] { "charge time", "chargetime", "time" },
                ("airtel", "dialedno") => new[] { "number", "dialedno", "dialled" },
                ("airtel", "duration") => new[] { "quantity", "duration" },
                ("airtel", "charges") => new[] { "charges", "charge", "cost" },
                ("airtel", "calltype") => new[] { "charge type", "chargetype", "calltype" },
                ("safaricom", "callingno") => new[] { "callingno", "calling no", "calling", "msisdn", "phone" },
                ("safaricom", "date") => new[] { "date" },
                ("safaricom", "time") => new[] { "time" },
                ("safaricom", "dialedno") => new[] { "dialledNo", "dialled no", "dialled", "dialedno", "dialed" },
                ("safaricom", "duration") => new[] { "duration" },
                ("safaricom", "charges") => new[] { "callcharges", "call charges", "charges", "cost" },
                ("safaricom", "calltype") => new[] { "calltype", "call type", "type" },
                _ => new[] { field }
            };

            // Try each possible column name
            foreach (var columnName in possibleColumnNames)
            {
                var normalizedColumnName = columnName.Replace(" ", "");
                var key = indices.Keys.FirstOrDefault(k =>
                    k.Contains(columnName, StringComparison.OrdinalIgnoreCase) ||
                    k.Replace(" ", "").Contains(normalizedColumnName, StringComparison.OrdinalIgnoreCase));

                if (key != null && indices.TryGetValue(key, out var idx) && idx < reader.FieldCount)
                {
                    return reader.GetValue(idx)?.ToString()?.Trim();
                }
            }

            return null;
        }

        private DateTime ParseReaderDate(IExcelDataReader reader, Dictionary<string, int> indices, string field, string provider)
        {
            var value = GetReaderValue(reader, indices, field, provider);
            if (string.IsNullOrWhiteSpace(value)) return DateTime.MinValue;

            if (DateTime.TryParse(value, out var date)) return date;
            return DateTime.MinValue;
        }

        private TimeSpan ParseReaderTime(IExcelDataReader reader, Dictionary<string, int> indices, string field, string provider)
        {
            var value = GetReaderValue(reader, indices, field, provider);
            return ParseTimeSpan(value);
        }

        private decimal ParseReaderDecimal(IExcelDataReader reader, Dictionary<string, int> indices, string field, string provider)
        {
            var value = GetReaderValue(reader, indices, field, provider);
            return ParseDecimalOrZero(value);
        }

        /// <summary>
        /// Gets a value from a tabular Excel format by trying multiple possible column names
        /// </summary>
        private string? GetTabularValue(IExcelDataReader reader, Dictionary<string, int> columnIndices, string[] possibleColumnNames)
        {
            foreach (var colName in possibleColumnNames)
            {
                // Try exact match first
                if (columnIndices.TryGetValue(colName, out var idx) && idx < reader.FieldCount)
                {
                    var val = reader.GetValue(idx)?.ToString()?.Trim();
                    if (!string.IsNullOrWhiteSpace(val))
                        return val;
                }

                // Try partial match
                var matchingKey = columnIndices.Keys.FirstOrDefault(k =>
                    k.Contains(colName, StringComparison.OrdinalIgnoreCase) ||
                    colName.Contains(k, StringComparison.OrdinalIgnoreCase));
                if (matchingKey != null && columnIndices.TryGetValue(matchingKey, out var idx2) && idx2 < reader.FieldCount)
                {
                    var val = reader.GetValue(idx2)?.ToString()?.Trim();
                    if (!string.IsNullOrWhiteSpace(val))
                        return val;
                }
            }
            return null;
        }

        #endregion

        private static TimeSpan ParseTimeSpan(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return TimeSpan.Zero;

            // Try direct TimeSpan parse first (handles "hh:mm" or "hh:mm:ss")
            if (TimeSpan.TryParse(value, out var ts)) return ts;

            // Handle Excel datetime format like "12/30/1899 14:30:00" - extract just the time
            if (DateTime.TryParse(value, out var dt))
            {
                return dt.TimeOfDay;
            }

            // Handle Excel numeric time format (fractional day, e.g., 0.604166667 = 14:30:00)
            if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var numericTime))
            {
                // Excel stores time as fraction of a day (0.5 = 12:00:00)
                if (numericTime >= 0 && numericTime < 1)
                {
                    return TimeSpan.FromDays(numericTime);
                }
                // If it's a full OLE Automation date, extract time portion
                try
                {
                    var oleDate = DateTime.FromOADate(numericTime);
                    return oleDate.TimeOfDay;
                }
                catch { }
            }

            return TimeSpan.Zero;
        }

        private static decimal ParseDecimalOrZero(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return 0m;
            value = value.Replace(",", "").Trim();
            if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var d)) return d;
            return 0m;
        }

        private static decimal ParseDurationToSeconds(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return 0;

            // Try to parse as TimeSpan (HH:MM:SS or MM:SS)
            if (TimeSpan.TryParse(value, out var ts))
            {
                return (decimal)ts.TotalSeconds;
            }

            // Try to parse as decimal seconds
            if (decimal.TryParse(value, out var d))
            {
                return d;
            }

            return 0;
        }

        #endregion

        #region DataTable Creation

        private DataTable CreateMobileDataTable()
        {
            // Match Safaricom/Airtel table structure (using actual DB column names - lowercase)
            var dt = new DataTable();
            dt.Columns.Add("ext", typeof(string));           // Extension/phone number
            dt.Columns.Add("call_date", typeof(DateTime));   // Call date
            dt.Columns.Add("call_time", typeof(TimeSpan));   // Call time
            dt.Columns.Add("dialed", typeof(string));        // Dialed number
            dt.Columns.Add("dur", typeof(decimal));          // Duration
            dt.Columns.Add("cost", typeof(decimal));         // Cost in KES
            dt.Columns.Add("call_type", typeof(string));     // Call type
            dt.Columns.Add("IndexNumber", typeof(string));
            dt.Columns.Add("UserPhoneId", typeof(int));
            dt.Columns.Add("EbillUserId", typeof(int));
            dt.Columns.Add("BillingPeriod", typeof(string));
            dt.Columns.Add("call_month", typeof(int));
            dt.Columns.Add("call_year", typeof(int));
            dt.Columns.Add("CreatedDate", typeof(DateTime));
            dt.Columns.Add("ProcessingStatus", typeof(int)); // Default to Staged (0)
            dt.Columns.Add("ImportJobId", typeof(Guid));
            return dt;
        }

        private DataTable CreatePSTNDataTable()
        {
            var dt = new DataTable();
            dt.Columns.Add("Extension", typeof(string));
            dt.Columns.Add("DialedNumber", typeof(string));
            dt.Columns.Add("CallTime", typeof(TimeSpan));
            dt.Columns.Add("Destination", typeof(string));
            dt.Columns.Add("DestinationLine", typeof(string));
            dt.Columns.Add("DurationExtended", typeof(decimal));
            dt.Columns.Add("CallDate", typeof(DateTime));
            dt.Columns.Add("Duration", typeof(decimal));
            dt.Columns.Add("AmountKSH", typeof(decimal));
            dt.Columns.Add("AmountUSD", typeof(decimal));
            dt.Columns.Add("IndexNumber", typeof(string));
            dt.Columns.Add("Carrier", typeof(string));
            dt.Columns.Add("UserPhoneId", typeof(int));
            dt.Columns.Add("EbillUserId", typeof(int));
            dt.Columns.Add("BillingPeriod", typeof(string));
            dt.Columns.Add("CallMonth", typeof(int));
            dt.Columns.Add("CallYear", typeof(int));
            dt.Columns.Add("CreatedDate", typeof(DateTime));
            dt.Columns.Add("ImportJobId", typeof(Guid));
            return dt;
        }

        private DataTable CreatePrivateWireDataTable()
        {
            var dt = new DataTable();
            dt.Columns.Add("Extension", typeof(string));
            dt.Columns.Add("DialedNumber", typeof(string));
            dt.Columns.Add("CallTime", typeof(TimeSpan));
            dt.Columns.Add("Destination", typeof(string));
            dt.Columns.Add("DestinationLine", typeof(string));
            dt.Columns.Add("DurationExtended", typeof(decimal));
            dt.Columns.Add("CallDate", typeof(DateTime));
            dt.Columns.Add("Duration", typeof(decimal));
            dt.Columns.Add("AmountKSH", typeof(decimal));
            dt.Columns.Add("AmountUSD", typeof(decimal));
            dt.Columns.Add("IndexNumber", typeof(string));
            // Note: PrivateWire model doesn't have Carrier column
            dt.Columns.Add("UserPhoneId", typeof(int));
            dt.Columns.Add("EbillUserId", typeof(int));
            dt.Columns.Add("BillingPeriod", typeof(string));
            dt.Columns.Add("CallMonth", typeof(int));
            dt.Columns.Add("CallYear", typeof(int));
            dt.Columns.Add("CreatedDate", typeof(DateTime));
            dt.Columns.Add("ImportJobId", typeof(Guid));
            return dt;
        }

        #endregion

        #region Bulk Insert Methods

        private async Task BulkInsertMobileAsync(DataTable dataTable, string provider)
        {
            var connectionString = _context.Database.GetConnectionString();
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            using var bulkCopy = new SqlBulkCopy(connection);
            // Use correct table name based on provider
            bulkCopy.DestinationTableName = provider.ToLower() == "safaricom" ? "Safaricom" : "Airtel";
            bulkCopy.BatchSize = 10000;
            bulkCopy.BulkCopyTimeout = 300;

            // Map columns to match Safaricom/Airtel table structure (lowercase column names)
            bulkCopy.ColumnMappings.Add("ext", "ext");
            bulkCopy.ColumnMappings.Add("call_date", "call_date");
            bulkCopy.ColumnMappings.Add("call_time", "call_time");
            bulkCopy.ColumnMappings.Add("dialed", "dialed");
            bulkCopy.ColumnMappings.Add("dur", "dur");
            bulkCopy.ColumnMappings.Add("cost", "cost");
            bulkCopy.ColumnMappings.Add("call_type", "call_type");
            bulkCopy.ColumnMappings.Add("IndexNumber", "IndexNumber");
            bulkCopy.ColumnMappings.Add("UserPhoneId", "UserPhoneId");
            bulkCopy.ColumnMappings.Add("EbillUserId", "EbillUserId");
            bulkCopy.ColumnMappings.Add("BillingPeriod", "BillingPeriod");
            bulkCopy.ColumnMappings.Add("call_month", "call_month");
            bulkCopy.ColumnMappings.Add("call_year", "call_year");
            bulkCopy.ColumnMappings.Add("CreatedDate", "CreatedDate");
            bulkCopy.ColumnMappings.Add("ProcessingStatus", "ProcessingStatus");
            bulkCopy.ColumnMappings.Add("ImportJobId", "ImportJobId");

            await bulkCopy.WriteToServerAsync(dataTable);
        }

        private async Task BulkInsertPSTNAsync(DataTable dataTable)
        {
            var connectionString = _context.Database.GetConnectionString();
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            using var bulkCopy = new SqlBulkCopy(connection);
            bulkCopy.DestinationTableName = "PSTNs";
            bulkCopy.BatchSize = 10000;
            bulkCopy.BulkCopyTimeout = 300;

            // Map columns
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
            bulkCopy.ColumnMappings.Add("ImportJobId", "ImportJobId");

            await bulkCopy.WriteToServerAsync(dataTable);
        }

        private async Task BulkInsertPrivateWireAsync(DataTable dataTable)
        {
            var connectionString = _context.Database.GetConnectionString();
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            using var bulkCopy = new SqlBulkCopy(connection);
            bulkCopy.DestinationTableName = "PrivateWires";
            bulkCopy.BatchSize = 10000;
            bulkCopy.BulkCopyTimeout = 300;

            // Map columns (PrivateWire model doesn't have Carrier column)
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
            bulkCopy.ColumnMappings.Add("ImportJobId", "ImportJobId");

            await bulkCopy.WriteToServerAsync(dataTable);
        }

        #endregion
    }
}
