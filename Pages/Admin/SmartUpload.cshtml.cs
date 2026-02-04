using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TAB.Web.Data;
using TAB.Web.Models;
using TAB.Web.Services;
using System.Globalization;
using System.Data;
using Microsoft.Data.SqlClient;
using ClosedXML.Excel;
using ExcelDataReader;
using Hangfire;

namespace TAB.Web.Pages.Admin
{
    [Authorize]
    public class SmartUploadModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<SmartUploadModel> _logger;
        private readonly IBackgroundJobClient _backgroundJobs;
        private readonly IBlobStorageService _blobStorageService;

        public SmartUploadModel(
            ApplicationDbContext context,
            IConfiguration configuration,
            ILogger<SmartUploadModel> logger,
            IBackgroundJobClient backgroundJobs,
            IBlobStorageService blobStorageService)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
            _backgroundJobs = backgroundJobs;
            _blobStorageService = blobStorageService;
        }

        [TempData]
        public string? StatusMessage { get; set; }

        [TempData]
        public string? StatusMessageClass { get; set; }

        // Preview data
        public bool ShowPreview { get; set; }
        public int HeaderRowNumber { get; set; }
        public int TotalRows { get; set; }
        public int ValidRows { get; set; }
        public int SkippedRows { get; set; }
        public List<SafaricomPreviewRow> PreviewRows { get; set; } = new();
        public string? TempFilePath { get; set; }
        public int? PreviewBillingMonth { get; set; }
        public int? PreviewBillingYear { get; set; }
        public string? PreviewProvider { get; set; }

        // Totals for preview summary
        public decimal TotalAmountKES { get; set; }
        public decimal TotalAmountUSD { get; set; }

        // Linked vs Unlinked counts
        public int LinkedRecords { get; set; }
        public int UnlinkedRecords { get; set; }

        public class SafaricomPreviewRow
        {
            public string CallingNo { get; set; } = "";
            public string Date { get; set; } = "";
            public string Time { get; set; } = "";
            public string DialledNo { get; set; } = "";
            public string Duration { get; set; } = "";
            public string CallCharges { get; set; } = "";
            public string CallType { get; set; } = "";
        }

        // Expected header columns for Safaricom format
        private readonly string[] ExpectedHeaders = { "callingno", "date", "callcharges" };

        public void OnGet()
        {
        }

        /// <summary>
        /// Preview the file - detect header and show sample data
        /// </summary>
        public async Task<IActionResult> OnPostPreviewAsync(IFormFile callLogFile, int? billingMonth, int? billingYear, string provider = "Safaricom")
        {
            try
            {
                if (callLogFile == null || callLogFile.Length == 0)
                {
                    StatusMessage = "Please select a file to upload";
                    StatusMessageClass = "danger";
                    return Page();
                }

                if (!billingMonth.HasValue || !billingYear.HasValue)
                {
                    StatusMessage = "Please select billing month and year";
                    StatusMessageClass = "danger";
                    return Page();
                }

                // Determine file type from extension
                var fileExtension = Path.GetExtension(callLogFile.FileName).ToLower();
                var isExcelFile = fileExtension == ".xlsx" || fileExtension == ".xls";

                // Save to temp file for later processing
                var tempFileName = $"smart_upload_{Guid.NewGuid()}{fileExtension}";
                var tempPath = Path.Combine(Path.GetTempPath(), tempFileName);

                using (var stream = new FileStream(tempPath, FileMode.Create))
                {
                    await callLogFile.CopyToAsync(stream);
                }

                // Load user phone lookup for linked/unlinked analysis
                var userPhoneLookup = await LoadUserPhoneLookupForAnalysis();
                // Load EbillUser lookup by IndexNumber for PSTN/PrivateWire (Staff ID in file)
                var ebillUserLookup = await LoadEbillUserLookupByIndexNumber();

                // Analyze the file based on type and provider
                FileAnalysisResult analysisResult;
                if ((provider == "PSTN" || provider == "PrivateWire") && isExcelFile)
                {
                    analysisResult = await AnalyzePstnExcelFileAsync(tempPath, userPhoneLookup, ebillUserLookup);
                }
                else if (isExcelFile)
                {
                    analysisResult = await AnalyzeExcelFileAsync(tempPath, provider, userPhoneLookup);
                }
                else
                {
                    analysisResult = await AnalyzeFileAsync(tempPath, userPhoneLookup);
                }

                if (!analysisResult.HeaderFound)
                {
                    var expectedColumns = provider switch
                    {
                        "PSTN" or "PrivateWire" => "Date, Time, Call Type, Place, Distant Number, Duration, Cost (with Extension header rows)",
                        "Airtel" => "MSISDN, Charge Date, Charge Time, Number, QUANTITY, CHARGES, Charge Type",
                        _ => "CallingNo, Date, Time, DialledNo, Duration, CallCharges, CallType"
                    };
                    StatusMessage = $"Could not find valid data. Expected columns: {expectedColumns}";
                    StatusMessageClass = "danger";
                    try { System.IO.File.Delete(tempPath); } catch { }
                    return Page();
                }

                // Set preview data
                ShowPreview = true;
                HeaderRowNumber = analysisResult.HeaderRowNumber;
                TotalRows = analysisResult.TotalRows;
                ValidRows = analysisResult.ValidRows;
                SkippedRows = analysisResult.SkippedRows;
                PreviewRows = analysisResult.PreviewRows;
                TempFilePath = tempPath;
                PreviewBillingMonth = billingMonth;
                PreviewBillingYear = billingYear;
                PreviewProvider = provider;

                // Assign totals based on provider currency
                // PrivateWire uses USD, all others (Safaricom, Airtel, PSTN) use KES
                if (provider == "PrivateWire")
                {
                    TotalAmountKES = 0;
                    TotalAmountUSD = analysisResult.TotalAmountKES; // PSTN analysis stores cost here temporarily
                }
                else
                {
                    TotalAmountKES = analysisResult.TotalAmountKES;
                    TotalAmountUSD = analysisResult.TotalAmountUSD;
                }

                // Assign linked/unlinked counts
                LinkedRecords = analysisResult.LinkedRecords;
                UnlinkedRecords = analysisResult.UnlinkedRecords;

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error previewing {Provider} file", provider);
                StatusMessage = $"Error previewing file: {ex.Message}";
                StatusMessageClass = "danger";
                return Page();
            }
        }

        /// <summary>
        /// Confirm and import the previewed file using ImportJob tracking and Hangfire background processing
        /// </summary>
        public async Task<IActionResult> OnPostImportAsync(string tempFilePath, int billingMonth, int billingYear, string provider = "Safaricom")
        {
            try
            {
                if (string.IsNullOrEmpty(tempFilePath) || !System.IO.File.Exists(tempFilePath))
                {
                    StatusMessage = "Preview file not found. Please upload again.";
                    StatusMessageClass = "danger";
                    return RedirectToPage();
                }

                // Get file info
                var fileInfo = new FileInfo(tempFilePath);
                var fileExtension = Path.GetExtension(tempFilePath).ToLower();
                var isExcelFile = fileExtension == ".xlsx" || fileExtension == ".xls";
                var originalFileName = $"SmartUpload_{provider}_{billingYear}_{billingMonth:D2}{fileExtension}";

                // Create import job record for tracking
                var jobId = Guid.NewGuid();

                // Upload file to Azure Blob Storage for reliable access by background worker
                var blobPath = $"imports/{jobId:N}/{originalFileName}";
                string? blobUrl = null;

                using (var fileStream = System.IO.File.OpenRead(tempFilePath))
                {
                    var contentType = isExcelFile
                        ? (fileExtension == ".xlsx" ? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" : "application/vnd.ms-excel")
                        : "text/csv";

                    var uploadResult = await _blobStorageService.UploadStreamAsync(fileStream, blobPath, contentType);

                    if (!uploadResult.Success)
                    {
                        _logger.LogError("Failed to upload file to Blob Storage: {Error}", uploadResult.ErrorMessage);
                        StatusMessage = $"Failed to upload file: {uploadResult.ErrorMessage}";
                        StatusMessageClass = "danger";
                        return RedirectToPage();
                    }

                    blobUrl = uploadResult.Url;
                    _logger.LogInformation("Uploaded file to Blob Storage: {BlobPath}", blobPath);
                }

                // Clean up temp file now that it's in Blob Storage
                try { System.IO.File.Delete(tempFilePath); } catch { }

                var importJob = new ImportJob
                {
                    Id = jobId,
                    FileName = originalFileName,
                    FileSize = fileInfo.Length,
                    CallLogType = provider,
                    BillingMonth = billingMonth,
                    BillingYear = billingYear,
                    DateFormat = "auto", // SmartUpload auto-detects date format
                    Status = "Queued",
                    CreatedBy = User.Identity?.Name ?? "Unknown",
                    CreatedDate = DateTime.UtcNow
                };

                _context.ImportJobs.Add(importJob);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created import job {JobId} for SmartUpload {Provider} file (Excel: {IsExcel})", jobId, provider, isExcelFile);

                // Queue Hangfire background job - pass blob path instead of temp file path
                // SmartUpload handles specific column formats (CallingNo, Date, CallCharges, etc.)
                // which are different from BulkImportService's expected format (ext, call_date, cost, etc.)
                string hangfireJobId = _backgroundJobs.Enqueue<ISmartUploadImportService>(
                    service => service.ImportFileAsync(
                        jobId,
                        blobPath,  // Pass blob path instead of temp file path
                        billingMonth,
                        billingYear,
                        provider,
                        isExcelFile,
                        JobCancellationToken.Null));

                // Update job with Hangfire ID
                importJob.HangfireJobId = hangfireJobId;
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Queued {Provider} SmartUpload import job {JobId} with Hangfire job ID {HangfireJobId}, Blob: {BlobPath}",
                    provider, jobId, hangfireJobId, blobPath);

                StatusMessage = $"Import queued successfully! {provider} file ({fileInfo.Length / 1024:N0} KB) is being processed in the background. " +
                                $"Monitor progress on the Import Jobs page.";
                StatusMessageClass = "success";

                return RedirectToPage("/Admin/ImportJobs");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error queuing SmartUpload import for {Provider}", provider);
                StatusMessage = $"Error queuing import: {ex.Message}";
                StatusMessageClass = "danger";
                return RedirectToPage();
            }
        }

        private async Task<FileAnalysisResult> AnalyzeFileAsync(string filePath, HashSet<string> userPhoneLookup)
        {
            var result = new FileAnalysisResult();

            using var stream = System.IO.File.OpenRead(filePath);
            using var reader = new StreamReader(stream);

            int lineNumber = 0;
            Dictionary<string, int>? columnIndices = null;

            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                lineNumber++;

                if (string.IsNullOrWhiteSpace(line)) continue;

                var values = ParseCsvLine(line);

                // Look for header row
                if (columnIndices == null)
                {
                    if (IsHeaderRow(values))
                    {
                        result.HeaderFound = true;
                        result.HeaderRowNumber = lineNumber;
                        columnIndices = BuildColumnIndex(values);
                        continue;
                    }
                    result.SkippedRows++;
                    continue;
                }

                // Skip repeated header rows
                if (IsHeaderRow(values))
                {
                    result.SkippedRows++;
                    continue;
                }

                // Process data row
                result.TotalRows++;

                var dateValue = GetValue(values, columnIndices, "date");

                // Skip if date is empty or not a valid date
                if (string.IsNullOrWhiteSpace(dateValue) || !IsValidDate(dateValue))
                {
                    result.SkippedRows++;
                    continue;
                }

                result.ValidRows++;

                // Check if phone number is linked to a user
                var callingNo = GetValue(values, columnIndices, "callingno") ?? "";
                var normalizedPhone = NormalizePhoneNumber(callingNo);
                if (userPhoneLookup.Contains(normalizedPhone))
                {
                    result.LinkedRecords++;
                }
                else
                {
                    result.UnlinkedRecords++;
                }

                // Sum up charges (KES for Safaricom/Airtel)
                var chargesStr = GetValue(values, columnIndices, "callcharges") ?? "0";
                if (decimal.TryParse(chargesStr, out var charges))
                {
                    result.TotalAmountKES += charges;
                }

                // Collect preview rows (first 10 valid rows)
                if (result.PreviewRows.Count < 10)
                {
                    result.PreviewRows.Add(new SafaricomPreviewRow
                    {
                        CallingNo = callingNo,
                        Date = dateValue,
                        Time = GetValue(values, columnIndices, "time") ?? "",
                        DialledNo = GetValue(values, columnIndices, "diallednumber") ?? GetValue(values, columnIndices, "dialledNo") ?? "",
                        Duration = GetValue(values, columnIndices, "duration") ?? "",
                        CallCharges = chargesStr,
                        CallType = GetValue(values, columnIndices, "calltype") ?? ""
                    });
                }
            }

            return result;
        }

        private async Task<ImportResult> ImportFileAsync(string filePath, int billingMonth, int billingYear, string provider = "Safaricom")
        {
            const int BATCH_SIZE = 10000;
            var result = new ImportResult();

            // Pre-load user phone lookups
            var userPhoneLookup = await LoadUserPhoneLookup();

            using var stream = System.IO.File.OpenRead(filePath);
            using var reader = new StreamReader(stream);

            Dictionary<string, int>? columnIndices = null;
            var dataTable = CreateSafaricomDataTable();
            var billingPeriodDate = new DateTime(billingYear, billingMonth, 1);
            var billingPeriodString = billingPeriodDate.ToString("yyyy-MM-dd");
            var tableName = provider; // Table name matches provider name (Safaricom, Airtel)

            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(line)) continue;

                var values = ParseCsvLine(line);

                // Look for header row
                if (columnIndices == null)
                {
                    if (IsHeaderRow(values))
                    {
                        columnIndices = BuildColumnIndex(values);
                    }
                    continue;
                }

                // Skip repeated header rows
                if (IsHeaderRow(values))
                {
                    result.SkippedCount++;
                    continue;
                }

                // Process data row
                var dateValue = GetValue(values, columnIndices, "date");

                // Skip if date is empty or not a valid date
                if (string.IsNullOrWhiteSpace(dateValue) || !IsValidDate(dateValue))
                {
                    result.SkippedCount++;
                    continue;
                }

                try
                {
                    var row = dataTable.NewRow();

                    var callingNo = GetValue(values, columnIndices, "callingno") ?? "";
                    var extension = NormalizePhoneNumber(callingNo);
                    var timeValue = GetValue(values, columnIndices, "time") ?? "";
                    var durationValue = GetValue(values, columnIndices, "duration") ?? "";

                    // Parse date and time
                    var callDateTime = ParseDateTime(dateValue, timeValue);

                    // Parse duration (HH:MM:SS → mm.ss format)
                    var parsedDuration = ParseDuration(durationValue);

                    // Lookup user phone
                    var phoneFound = userPhoneLookup.TryGetValue(extension, out var userPhone);
                    var indexNumber = phoneFound ? userPhone.IndexNumber : "";
                    int? ebillUserId = phoneFound ? userPhone.EbillUserId : null;

                    // Populate row
                    row["ext"] = extension;
                    row["call_date"] = callDateTime;
                    row["dialed"] = GetValue(values, columnIndices, "diallednumber") ?? GetValue(values, columnIndices, "dialledNo") ?? "";
                    row["dest"] = GetValue(values, columnIndices, "calltype") ?? "";
                    row["durx"] = parsedDuration;
                    row["cost"] = ParseDecimal(GetValue(values, columnIndices, "callcharges"));
                    row["dur"] = parsedDuration; // Same value in both fields
                    row["call_type"] = GetValue(values, columnIndices, "calltype") ?? "Voice";
                    row["call_month"] = billingMonth;
                    row["call_year"] = billingYear;
                    row["IndexNumber"] = indexNumber ?? "";
                    row["UserPhoneId"] = phoneFound ? (object)userPhone.Id : DBNull.Value;
                    row["EbillUserId"] = ebillUserId.HasValue ? (object)ebillUserId.Value : DBNull.Value;
                    row["BillingPeriod"] = billingPeriodString;
                    row["CreatedDate"] = DateTime.UtcNow;
                    row["CreatedBy"] = User.Identity?.Name ?? "SafaricomUpload";
                    row["ProcessingStatus"] = 0; // Staged

                    dataTable.Rows.Add(row);

                    // Bulk insert when batch is full
                    if (dataTable.Rows.Count >= BATCH_SIZE)
                    {
                        await BulkInsertAsync(dataTable, tableName);
                        result.SuccessCount += dataTable.Rows.Count;
                        dataTable.Clear();

                        _logger.LogInformation("Inserted batch: {Count} records to {Table}, Total: {Total}", BATCH_SIZE, tableName, result.SuccessCount);
                    }
                }
                catch (Exception ex)
                {
                    result.ErrorCount++;
                    _logger.LogWarning("Error processing row: {Error}", ex.Message);
                }
            }

            // Insert remaining records
            if (dataTable.Rows.Count > 0)
            {
                await BulkInsertAsync(dataTable, tableName);
                result.SuccessCount += dataTable.Rows.Count;
            }

            _logger.LogInformation("{Provider} import completed: {Success} success, {Skipped} skipped, {Errors} errors",
                provider, result.SuccessCount, result.SkippedCount, result.ErrorCount);

            return result;
        }

        private bool IsHeaderRow(string[] values)
        {
            var lowerValues = values.Select(v => v.ToLower().Trim()).ToArray();
            return ExpectedHeaders.All(h => lowerValues.Contains(h));
        }

        private Dictionary<string, int> BuildColumnIndex(string[] headers)
        {
            var index = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < headers.Length; i++)
            {
                var header = headers[i].Trim().ToLower().Replace(" ", "");
                if (!index.ContainsKey(header))
                {
                    index[header] = i;
                }
            }
            return index;
        }

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

        private string? GetValue(string[] values, Dictionary<string, int> columnIndices, string columnName)
        {
            var key = columnName.ToLower().Replace(" ", "");
            if (columnIndices.TryGetValue(key, out int index) && index < values.Length)
            {
                var value = values[index]?.Trim();
                return string.IsNullOrWhiteSpace(value) ? null : value;
            }
            return null;
        }

        private bool IsValidDate(string? dateValue)
        {
            if (string.IsNullOrWhiteSpace(dateValue)) return false;

            // Try common date formats
            string[] formats = { "dd/MM/yyyy", "MM/dd/yyyy", "yyyy-MM-dd", "d/M/yyyy" };

            return DateTime.TryParseExact(dateValue, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out _)
                   || DateTime.TryParse(dateValue, out _);
        }

        private string NormalizePhoneNumber(string? phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber)) return "";

            var normalized = phoneNumber.Replace("-", "").Replace(" ", "").Replace("(", "").Replace(")", "");

            if (normalized.StartsWith("254"))
                normalized = normalized.Substring(3);
            else if (normalized.StartsWith("0"))
                normalized = normalized.Substring(1);

            return normalized;
        }

        private DateTime ParseDateTime(string dateValue, string timeValue)
        {
            // Try parsing date (dd/MM/yyyy format)
            if (DateTime.TryParseExact(dateValue, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            {
                // Try adding time if available
                if (!string.IsNullOrWhiteSpace(timeValue) && TimeSpan.TryParse(timeValue, out var time))
                {
                    return date.Add(time);
                }
                return date;
            }

            // Fallback to general parsing
            if (DateTime.TryParse(dateValue, out var fallbackDate))
            {
                return fallbackDate;
            }

            return DateTime.MinValue;
        }

        private decimal ParseDuration(string? durationValue)
        {
            if (string.IsNullOrWhiteSpace(durationValue)) return 0;

            // Parse HH:MM:SS format → mm.ss format
            if (durationValue.Contains(':'))
            {
                var parts = durationValue.Split(':');
                if (parts.Length == 3)
                {
                    if (int.TryParse(parts[0], out int hours) &&
                        int.TryParse(parts[1], out int minutes) &&
                        int.TryParse(parts[2], out int seconds))
                    {
                        int totalMinutes = (hours * 60) + minutes;
                        return totalMinutes + (seconds / 100m);
                    }
                }
                else if (parts.Length == 2)
                {
                    if (int.TryParse(parts[0], out int minutes) &&
                        int.TryParse(parts[1], out int seconds))
                    {
                        return minutes + (seconds / 100m);
                    }
                }
            }

            if (decimal.TryParse(durationValue, out var result))
            {
                return result;
            }

            return 0;
        }

        private decimal ParseDecimal(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return 0;
            return decimal.TryParse(value, out var result) ? result : 0;
        }

        /// <summary>
        /// Load user phone lookup for analysis - returns a HashSet of normalized phone numbers for quick lookup
        /// </summary>
        private async Task<HashSet<string>> LoadUserPhoneLookupForAnalysis()
        {
            var userPhones = await _context.UserPhones
                .Where(up => up.IsActive)
                .Select(up => up.PhoneNumber)
                .ToListAsync();

            return userPhones
                .Select(NormalizePhoneNumber)
                .Where(p => !string.IsNullOrEmpty(p))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Load EbillUser lookup by IndexNumber for direct linking (used by PSTN/PrivateWire)
        /// </summary>
        private async Task<HashSet<string>> LoadEbillUserLookupByIndexNumber()
        {
            var users = await _context.EbillUsers
                .Where(u => u.IsActive && !string.IsNullOrEmpty(u.IndexNumber))
                .Select(u => u.IndexNumber)
                .ToListAsync();

            return users
                .Where(i => !string.IsNullOrEmpty(i))
                .ToHashSet(StringComparer.OrdinalIgnoreCase)!;
        }

        private async Task<Dictionary<string, (string PhoneNumber, int Id, string? IndexNumber, int? EbillUserId)>> LoadUserPhoneLookup()
        {
            var userPhones = await _context.UserPhones
                .Where(up => up.IsActive)
                .Select(up => new
                {
                    up.PhoneNumber,
                    up.Id,
                    up.IndexNumber,
                    EbillUserId = up.EbillUser != null ? (int?)up.EbillUser.Id : null
                })
                .ToListAsync();

            return userPhones
                .GroupBy(up => NormalizePhoneNumber(up.PhoneNumber))
                .ToDictionary(
                    g => g.Key,
                    g => (g.First().PhoneNumber, g.First().Id, g.First().IndexNumber, g.First().EbillUserId));
        }

        private DataTable CreateSafaricomDataTable()
        {
            var dt = new DataTable();
            dt.Columns.Add("ext", typeof(string));
            dt.Columns.Add("call_date", typeof(DateTime));
            dt.Columns.Add("dialed", typeof(string));
            dt.Columns.Add("dest", typeof(string));
            dt.Columns.Add("durx", typeof(decimal));
            dt.Columns.Add("cost", typeof(decimal));
            dt.Columns.Add("dur", typeof(decimal));
            dt.Columns.Add("call_type", typeof(string));
            dt.Columns.Add("call_month", typeof(int));
            dt.Columns.Add("call_year", typeof(int));
            dt.Columns.Add("IndexNumber", typeof(string));
            dt.Columns.Add("UserPhoneId", typeof(int));
            dt.Columns.Add("EbillUserId", typeof(int));
            dt.Columns.Add("BillingPeriod", typeof(string));
            dt.Columns.Add("CreatedDate", typeof(DateTime));
            dt.Columns.Add("CreatedBy", typeof(string));
            dt.Columns.Add("ProcessingStatus", typeof(int));
            return dt;
        }

        private DataTable CreatePstnDataTable()
        {
            var dt = new DataTable();
            dt.Columns.Add("Extension", typeof(string));
            dt.Columns.Add("DialedNumber", typeof(string));
            dt.Columns.Add("CallDate", typeof(DateTime));
            dt.Columns.Add("CallTime", typeof(TimeSpan));
            dt.Columns.Add("Destination", typeof(string));
            dt.Columns.Add("DestinationLine", typeof(string));
            dt.Columns.Add("DurationExtended", typeof(decimal));
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
            dt.Columns.Add("CreatedBy", typeof(string));
            dt.Columns.Add("ProcessingStatus", typeof(int));
            return dt;
        }

        private DataTable CreatePrivateWireDataTable()
        {
            var dt = new DataTable();
            dt.Columns.Add("Extension", typeof(string));
            dt.Columns.Add("DialedNumber", typeof(string));
            dt.Columns.Add("CallDate", typeof(DateTime));
            dt.Columns.Add("CallTime", typeof(TimeSpan));
            dt.Columns.Add("Destination", typeof(string));
            dt.Columns.Add("DestinationLine", typeof(string));
            dt.Columns.Add("DurationExtended", typeof(decimal));
            dt.Columns.Add("Duration", typeof(decimal));
            dt.Columns.Add("AmountKSH", typeof(decimal));
            dt.Columns.Add("AmountUSD", typeof(decimal));
            dt.Columns.Add("IndexNumber", typeof(string));
            dt.Columns.Add("UserPhoneId", typeof(int));
            dt.Columns.Add("EbillUserId", typeof(int));
            dt.Columns.Add("BillingPeriod", typeof(string));
            dt.Columns.Add("CallMonth", typeof(int));
            dt.Columns.Add("CallYear", typeof(int));
            dt.Columns.Add("CreatedDate", typeof(DateTime));
            dt.Columns.Add("CreatedBy", typeof(string));
            dt.Columns.Add("ProcessingStatus", typeof(int));
            return dt;
        }

        private async Task BulkInsertAsync(DataTable dataTable, string tableName = "Safaricom")
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            using var bulkCopy = new SqlBulkCopy(connection)
            {
                DestinationTableName = tableName,
                BatchSize = dataTable.Rows.Count,
                BulkCopyTimeout = 300
            };

            // Column mappings
            bulkCopy.ColumnMappings.Add("ext", "ext");
            bulkCopy.ColumnMappings.Add("call_date", "call_date");
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

            await bulkCopy.WriteToServerAsync(dataTable);
        }

        private async Task BulkInsertPstnAsync(DataTable dataTable)
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            using var bulkCopy = new SqlBulkCopy(connection)
            {
                DestinationTableName = "PSTNs",
                BatchSize = dataTable.Rows.Count,
                BulkCopyTimeout = 300
            };

            // Column mappings for PSTN table
            bulkCopy.ColumnMappings.Add("Extension", "Extension");
            bulkCopy.ColumnMappings.Add("DialedNumber", "DialedNumber");
            bulkCopy.ColumnMappings.Add("CallDate", "CallDate");
            bulkCopy.ColumnMappings.Add("CallTime", "CallTime");
            bulkCopy.ColumnMappings.Add("Destination", "Destination");
            bulkCopy.ColumnMappings.Add("DestinationLine", "DestinationLine");
            bulkCopy.ColumnMappings.Add("DurationExtended", "DurationExtended");
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

            await bulkCopy.WriteToServerAsync(dataTable);
        }

        private async Task BulkInsertPrivateWireAsync(DataTable dataTable)
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            using var bulkCopy = new SqlBulkCopy(connection)
            {
                DestinationTableName = "PrivateWires",
                BatchSize = dataTable.Rows.Count,
                BulkCopyTimeout = 300
            };

            // Column mappings for PrivateWires table (no Carrier column)
            bulkCopy.ColumnMappings.Add("Extension", "Extension");
            bulkCopy.ColumnMappings.Add("DialedNumber", "DialedNumber");
            bulkCopy.ColumnMappings.Add("CallDate", "CallDate");
            bulkCopy.ColumnMappings.Add("CallTime", "CallTime");
            bulkCopy.ColumnMappings.Add("Destination", "Destination");
            bulkCopy.ColumnMappings.Add("DestinationLine", "DestinationLine");
            bulkCopy.ColumnMappings.Add("DurationExtended", "DurationExtended");
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

            await bulkCopy.WriteToServerAsync(dataTable);
        }

        // Expected header columns for Airtel Excel format
        private readonly string[] AirtelExpectedHeaders = { "msisdn", "chargedate", "charges" };

        private Task<FileAnalysisResult> AnalyzeExcelFileAsync(string filePath, string provider, HashSet<string> userPhoneLookup)
        {
            return Task.Run(() =>
            {
                var result = new FileAnalysisResult();

                using var workbook = new XLWorkbook(filePath);
                var worksheets = workbook.Worksheets.ToList();

                // Skip the last worksheet for Airtel (per user requirement)
                var worksheetsToProcess = worksheets.Count > 1
                    ? worksheets.Take(worksheets.Count - 1).ToList()
                    : worksheets;

                int worksheetIndex = 0;
                foreach (var worksheet in worksheetsToProcess)
                {
                    worksheetIndex++;
                    var rows = worksheet.RangeUsed()?.RowsUsed().ToList();
                    if (rows == null || rows.Count == 0) continue;

                    Dictionary<string, int>? columnIndices = null;
                    int headerRowInSheet = 0;

                    foreach (var row in rows)
                    {
                        var rowNumber = row.RowNumber();
                        var values = row.Cells().Select(c => c.GetString().Trim()).ToArray();

                        // Look for header row
                        if (columnIndices == null)
                        {
                            if (IsAirtelHeaderRow(values))
                            {
                                if (!result.HeaderFound)
                                {
                                    result.HeaderFound = true;
                                    result.HeaderRowNumber = rowNumber;
                                }
                                columnIndices = BuildColumnIndex(values);
                                headerRowInSheet = rowNumber;
                                continue;
                            }
                            result.SkippedRows++;
                            continue;
                        }

                        // Skip if this looks like another header row
                        if (IsAirtelHeaderRow(values))
                        {
                            result.SkippedRows++;
                            continue;
                        }

                        // Process data row
                        result.TotalRows++;

                        var dateValue = GetValue(values, columnIndices, "chargedate");

                        // Skip if date is empty or not a valid date
                        if (string.IsNullOrWhiteSpace(dateValue) || !IsValidAirtelDate(dateValue))
                        {
                            result.SkippedRows++;
                            continue;
                        }

                        result.ValidRows++;

                        // Check if phone number is linked to a user
                        var msisdn = GetValue(values, columnIndices, "msisdn") ?? "";
                        var normalizedPhone = NormalizePhoneNumber(msisdn);
                        if (userPhoneLookup.Contains(normalizedPhone))
                        {
                            result.LinkedRecords++;
                        }
                        else
                        {
                            result.UnlinkedRecords++;
                        }

                        // Sum up charges (KES for Airtel)
                        var chargesStr = GetValue(values, columnIndices, "charges") ?? "0";
                        if (decimal.TryParse(chargesStr, out var charges))
                        {
                            result.TotalAmountKES += charges;
                        }

                        // Collect preview rows (first 10 valid rows)
                        if (result.PreviewRows.Count < 10)
                        {
                            var quantity = GetValue(values, columnIndices, "quantity") ?? "0";
                            var chargeType = GetValue(values, columnIndices, "chargetype") ?? "";
                            var durationDisplay = ConvertAirtelQuantityToDisplay(quantity, chargeType);

                            result.PreviewRows.Add(new SafaricomPreviewRow
                            {
                                CallingNo = msisdn,
                                Date = dateValue,
                                Time = GetValue(values, columnIndices, "chargetime") ?? "",
                                DialledNo = GetValue(values, columnIndices, "number") ?? "",
                                Duration = durationDisplay,
                                CallCharges = chargesStr,
                                CallType = chargeType
                            });
                        }
                    }
                }

                return result;
            });
        }

        private async Task<ImportResult> ImportExcelFileAsync(string filePath, int billingMonth, int billingYear, string provider)
        {
            const int BATCH_SIZE = 10000;
            var result = new ImportResult();

            // Pre-load user phone lookups
            var userPhoneLookup = await LoadUserPhoneLookup();

            var dataTable = CreateSafaricomDataTable();
            var billingPeriodDate = new DateTime(billingYear, billingMonth, 1);
            var billingPeriodString = billingPeriodDate.ToString("yyyy-MM-dd");
            var tableName = provider; // Table name matches provider name

            using var workbook = new XLWorkbook(filePath);
            var worksheets = workbook.Worksheets.ToList();

            // Skip the last worksheet for Airtel
            var worksheetsToProcess = worksheets.Count > 1
                ? worksheets.Take(worksheets.Count - 1).ToList()
                : worksheets;

            foreach (var worksheet in worksheetsToProcess)
            {
                var rows = worksheet.RangeUsed()?.RowsUsed().ToList();
                if (rows == null || rows.Count == 0) continue;

                Dictionary<string, int>? columnIndices = null;

                foreach (var row in rows)
                {
                    var values = row.Cells().Select(c => c.GetString().Trim()).ToArray();

                    // Look for header row
                    if (columnIndices == null)
                    {
                        if (IsAirtelHeaderRow(values))
                        {
                            columnIndices = BuildColumnIndex(values);
                        }
                        continue;
                    }

                    // Skip repeated header rows
                    if (IsAirtelHeaderRow(values))
                    {
                        result.SkippedCount++;
                        continue;
                    }

                    // Process data row
                    var dateValue = GetValue(values, columnIndices, "chargedate");

                    // Skip if date is empty or not a valid date
                    if (string.IsNullOrWhiteSpace(dateValue) || !IsValidAirtelDate(dateValue))
                    {
                        result.SkippedCount++;
                        continue;
                    }

                    try
                    {
                        var dataRow = dataTable.NewRow();

                        var msisdn = GetValue(values, columnIndices, "msisdn") ?? "";
                        var extension = NormalizePhoneNumber(msisdn);
                        var timeValue = GetValue(values, columnIndices, "chargetime") ?? "";
                        var quantityValue = GetValue(values, columnIndices, "quantity") ?? "0";
                        var chargeType = GetValue(values, columnIndices, "chargetype") ?? "";

                        // Parse date and time
                        var callDateTime = ParseAirtelDateTime(dateValue, timeValue);

                        // Convert quantity (seconds) to mm.ss format
                        var parsedDuration = ConvertAirtelQuantity(quantityValue, chargeType);

                        // Lookup user phone
                        var phoneFound = userPhoneLookup.TryGetValue(extension, out var userPhone);
                        var indexNumber = phoneFound ? userPhone.IndexNumber : "";
                        int? ebillUserId = phoneFound ? userPhone.EbillUserId : null;

                        // Populate row
                        dataRow["ext"] = extension;
                        dataRow["call_date"] = callDateTime;
                        dataRow["dialed"] = GetValue(values, columnIndices, "number") ?? "";
                        dataRow["dest"] = chargeType;
                        dataRow["durx"] = parsedDuration;
                        dataRow["cost"] = ParseDecimal(GetValue(values, columnIndices, "charges"));
                        dataRow["dur"] = parsedDuration;
                        dataRow["call_type"] = chargeType;
                        dataRow["call_month"] = billingMonth;
                        dataRow["call_year"] = billingYear;
                        dataRow["IndexNumber"] = indexNumber ?? "";
                        dataRow["UserPhoneId"] = phoneFound ? (object)userPhone.Id : DBNull.Value;
                        dataRow["EbillUserId"] = ebillUserId.HasValue ? (object)ebillUserId.Value : DBNull.Value;
                        dataRow["BillingPeriod"] = billingPeriodString;
                        dataRow["CreatedDate"] = DateTime.UtcNow;
                        dataRow["CreatedBy"] = User.Identity?.Name ?? "AirtelUpload";
                        dataRow["ProcessingStatus"] = 0; // Staged

                        dataTable.Rows.Add(dataRow);

                        // Bulk insert when batch is full
                        if (dataTable.Rows.Count >= BATCH_SIZE)
                        {
                            await BulkInsertAsync(dataTable, tableName);
                            result.SuccessCount += dataTable.Rows.Count;
                            dataTable.Clear();

                            _logger.LogInformation("Inserted batch: {Count} records to {Table}, Total: {Total}",
                                BATCH_SIZE, tableName, result.SuccessCount);
                        }
                    }
                    catch (Exception ex)
                    {
                        result.ErrorCount++;
                        _logger.LogWarning("Error processing row: {Error}", ex.Message);
                    }
                }
            }

            // Insert remaining records
            if (dataTable.Rows.Count > 0)
            {
                await BulkInsertAsync(dataTable, tableName);
                result.SuccessCount += dataTable.Rows.Count;
            }

            _logger.LogInformation("{Provider} import completed: {Success} success, {Skipped} skipped, {Errors} errors",
                provider, result.SuccessCount, result.SkippedCount, result.ErrorCount);

            return result;
        }

        private bool IsAirtelHeaderRow(string[] values)
        {
            var lowerValues = values.Select(v => v.ToLower().Trim().Replace(" ", "")).ToArray();
            return AirtelExpectedHeaders.All(h => lowerValues.Contains(h));
        }

        private bool IsValidAirtelDate(string? dateValue)
        {
            if (string.IsNullOrWhiteSpace(dateValue)) return false;

            // Airtel uses dd-MM-yyyy format
            string[] formats = { "dd-MM-yyyy", "dd/MM/yyyy", "yyyy-MM-dd", "d-M-yyyy" };

            return DateTime.TryParseExact(dateValue, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out _)
                   || DateTime.TryParse(dateValue, out _);
        }

        private DateTime ParseAirtelDateTime(string dateValue, string timeValue)
        {
            // Try parsing date (dd-MM-yyyy format for Airtel)
            string[] formats = { "dd-MM-yyyy", "dd/MM/yyyy", "yyyy-MM-dd" };

            foreach (var format in formats)
            {
                if (DateTime.TryParseExact(dateValue, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                {
                    // Try adding time if available
                    if (!string.IsNullOrWhiteSpace(timeValue) && TimeSpan.TryParse(timeValue, out var time))
                    {
                        return date.Add(time);
                    }
                    return date;
                }
            }

            // Fallback to general parsing
            if (DateTime.TryParse(dateValue, out var fallbackDate))
            {
                return fallbackDate;
            }

            return DateTime.MinValue;
        }

        private decimal ConvertAirtelQuantity(string quantityStr, string chargeType)
        {
            if (!decimal.TryParse(quantityStr, out var quantity)) return 0;

            // For voice calls, quantity is in seconds - convert to mm.ss format
            // For data (GPRS), quantity is in bytes - store as-is (or convert to KB/MB)
            var lowerChargeType = chargeType.ToLower();

            if (lowerChargeType.Contains("gprs") || lowerChargeType.Contains("data"))
            {
                // For data sessions, store the quantity as-is (bytes)
                // Could convert to MB: quantity / 1024 / 1024
                return quantity;
            }
            else
            {
                // Voice: convert seconds to mm.ss format
                var totalSeconds = (int)quantity;
                var minutes = totalSeconds / 60;
                var seconds = totalSeconds % 60;
                return minutes + (seconds / 100m);
            }
        }

        private string ConvertAirtelQuantityToDisplay(string quantityStr, string chargeType)
        {
            if (!decimal.TryParse(quantityStr, out var quantity)) return "0";

            var lowerChargeType = chargeType.ToLower();

            if (lowerChargeType.Contains("gprs") || lowerChargeType.Contains("data"))
            {
                // Display data in MB
                var mb = quantity / 1024 / 1024;
                return $"{mb:F2} MB";
            }
            else
            {
                // Voice: show as mm:ss
                var totalSeconds = (int)quantity;
                var minutes = totalSeconds / 60;
                var seconds = totalSeconds % 60;
                return $"{minutes}:{seconds:D2}";
            }
        }

        // ==================== PSTN Excel Support ====================

        // PSTN files have per-Staff sections with extension in header row
        // Pattern: "Extension 2:XXXXX: Staff Name"
        private readonly string[] PstnExpectedHeaders = { "date", "time", "call", "distant", "duration", "cost" };

        private Task<FileAnalysisResult> AnalyzePstnExcelFileAsync(string filePath, HashSet<string> userPhoneLookup, HashSet<string> ebillUserLookup)
        {
            return Task.Run(() =>
            {
                var result = new FileAnalysisResult();
                string? currentExtension = null;
                bool currentExtensionIsLinked = false;

                // Required for .xls support
                System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

                using var stream = System.IO.File.Open(filePath, FileMode.Open, FileAccess.Read);
                using var reader = ExcelReaderFactory.CreateReader(stream);

                Dictionary<string, int>? columnIndices = null;
                bool inDataSection = false;
                int rowNumber = 0;

                do
                {
                    while (reader.Read())
                    {
                        rowNumber++;
                        var values = new string[reader.FieldCount];
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            values[i] = reader.GetValue(i)?.ToString()?.Trim() ?? "";
                        }

                        var firstCell = values.Length > 0 ? values[0] : "";

                        // Check for extension header row (contains "Extension")
                        if (firstCell.Contains("Extension", StringComparison.OrdinalIgnoreCase))
                        {
                            currentExtension = ExtractPstnExtension(firstCell);

                            // Extract Staff ID (IndexNumber) from header for direct EbillUser linking
                            // Pattern: "Staff ID 6726: 8847535" - extract 8847535
                            string? indexNumberFromFile = null;
                            var staffIdMatch = System.Text.RegularExpressions.Regex.Match(firstCell, @"Staff ID\s+\d+:\s*(\d+)");
                            if (staffIdMatch.Success && staffIdMatch.Groups[1].Success)
                            {
                                indexNumberFromFile = staffIdMatch.Groups[1].Value.Trim();
                            }

                            // Priority 1: Check if IndexNumber from file (Staff ID) links to EbillUser
                            if (!string.IsNullOrEmpty(indexNumberFromFile) && ebillUserLookup.Contains(indexNumberFromFile))
                            {
                                currentExtensionIsLinked = true;
                            }
                            // Priority 2: Fall back to checking UserPhone by Extension
                            else if (!string.IsNullOrEmpty(currentExtension) && userPhoneLookup.Contains(currentExtension))
                            {
                                currentExtensionIsLinked = true;
                            }
                            else
                            {
                                currentExtensionIsLinked = false;
                            }

                            columnIndices = null; // Reset for new section
                            inDataSection = false;
                            continue;
                        }

                        // Look for column header row
                        if (columnIndices == null && IsPstnHeaderRow(values))
                        {
                            if (!result.HeaderFound)
                            {
                                result.HeaderFound = true;
                                result.HeaderRowNumber = rowNumber;
                            }
                            columnIndices = BuildColumnIndex(values);
                            inDataSection = true;
                            continue;
                        }

                        // Skip format hint row (mm/dd/yyyy | hh:mm | Type...)
                        if (firstCell.ToLower().Contains("mm/dd/yyyy") || firstCell.ToLower().Contains("mm/dd"))
                        {
                            continue;
                        }

                        // Skip summary rows (Type | Number | Cost | Duration header)
                        if (firstCell.ToLower() == "type" && values.Length > 1 && values[1].ToLower() == "number")
                        {
                            inDataSection = false;
                            continue;
                        }

                        // Skip summary data rows and empty rows
                        if (!inDataSection || columnIndices == null || string.IsNullOrWhiteSpace(currentExtension))
                        {
                            result.SkippedRows++;
                            continue;
                        }

                        // Process data row
                        result.TotalRows++;

                        var dateValue = GetValue(values, columnIndices, "date");

                        // Skip if date is empty or not a valid date
                        if (string.IsNullOrWhiteSpace(dateValue) || !IsValidPstnDate(dateValue))
                        {
                            result.SkippedRows++;
                            continue;
                        }

                        result.ValidRows++;

                        // Count linked/unlinked based on current extension
                        if (currentExtensionIsLinked)
                        {
                            result.LinkedRecords++;
                        }
                        else
                        {
                            result.UnlinkedRecords++;
                        }

                        // Sum up charges (stored in TotalAmountKES, will be assigned to correct field based on provider)
                        var costStr = GetValue(values, columnIndices, "cost") ?? "0";
                        if (decimal.TryParse(costStr, out var cost))
                        {
                            result.TotalAmountKES += cost; // Temporarily store here, will be reassigned based on provider
                        }

                        // Collect preview rows (first 10 valid rows)
                        if (result.PreviewRows.Count < 10)
                        {
                            var distantNumber = GetValue(values, columnIndices, "distant") ?? "";
                            var cleanDialed = CleanPstnDialedNumber(distantNumber);

                            result.PreviewRows.Add(new SafaricomPreviewRow
                            {
                                CallingNo = currentExtension,
                                Date = dateValue,
                                Time = FormatTimeOnly(GetValue(values, columnIndices, "time")),
                                DialledNo = cleanDialed,
                                Duration = GetValue(values, columnIndices, "duration") ?? "",
                                CallCharges = costStr,
                                CallType = GetValue(values, columnIndices, "place") ?? GetValue(values, columnIndices, "call") ?? ""
                            });
                        }
                    }
                } while (reader.NextResult()); // Move to next sheet if any

                return result;
            });
        }

        private async Task<ImportResult> ImportPstnExcelFileAsync(string filePath, int billingMonth, int billingYear)
        {
            const int BATCH_SIZE = 10000;
            var result = new ImportResult();

            // Pre-load user phone lookups
            var userPhoneLookup = await LoadUserPhoneLookup();

            var dataTable = CreatePstnDataTable();
            var billingPeriodDate = new DateTime(billingYear, billingMonth, 1);
            var billingPeriodString = billingPeriodDate.ToString("yyyy-MM-dd");

            string? currentExtension = null;

            // Required for .xls support
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            using var stream = System.IO.File.Open(filePath, FileMode.Open, FileAccess.Read);
            using var reader = ExcelReaderFactory.CreateReader(stream);

            Dictionary<string, int>? columnIndices = null;
            bool inDataSection = false;

            do
            {
                while (reader.Read())
                {
                    var values = new string[reader.FieldCount];
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        values[i] = reader.GetValue(i)?.ToString()?.Trim() ?? "";
                    }

                    var firstCell = values.Length > 0 ? values[0] : "";

                    // Check for extension header row
                    if (firstCell.Contains("Extension 2:"))
                    {
                        currentExtension = ExtractPstnExtension(firstCell);
                        columnIndices = null;
                        inDataSection = false;
                        continue;
                    }

                    // Look for column header row
                    if (columnIndices == null && IsPstnHeaderRow(values))
                    {
                        columnIndices = BuildColumnIndex(values);
                        inDataSection = true;
                        continue;
                    }

                    // Skip format hint row
                    if (firstCell.ToLower().Contains("mm/dd/yyyy") || firstCell.ToLower().Contains("mm/dd"))
                    {
                        continue;
                    }

                    // Skip summary rows
                    if (firstCell.ToLower() == "type" && values.Length > 1 && values[1].ToLower() == "number")
                    {
                        inDataSection = false;
                        continue;
                    }

                    // Skip if not in data section
                    if (!inDataSection || columnIndices == null || string.IsNullOrWhiteSpace(currentExtension))
                    {
                        result.SkippedCount++;
                        continue;
                    }

                    // Process data row
                    var dateValue = GetValue(values, columnIndices, "date");

                    if (string.IsNullOrWhiteSpace(dateValue) || !IsValidPstnDate(dateValue))
                    {
                        result.SkippedCount++;
                        continue;
                    }

                    try
                    {
                        var dataRow = dataTable.NewRow();

                        var extension = currentExtension;
                        var timeValue = GetValue(values, columnIndices, "time") ?? "";
                        var distantNumber = GetValue(values, columnIndices, "distant") ?? "";
                        var durationValue = GetValue(values, columnIndices, "duration") ?? "";
                        var costValue = GetValue(values, columnIndices, "cost") ?? "0";
                        var callType = GetValue(values, columnIndices, "place") ?? GetValue(values, columnIndices, "call") ?? "";

                        // Parse date and time (mm/dd/yyyy format)
                        var callDateTime = ParsePstnDateTime(dateValue, timeValue);

                        // Parse time value to TimeSpan (may come as datetime string from Excel)
                        var callTime = ParsePstnTime(timeValue);

                        // Parse duration (h:mm:ss format) to mm.ss
                        var parsedDuration = ParsePstnDuration(durationValue);

                        // Clean dialed number (remove ER1+90+ prefix)
                        var cleanDialed = CleanPstnDialedNumber(distantNumber);

                        // Parse cost (already in decimal format)
                        var cost = ParseDecimal(costValue);

                        // Lookup user phone
                        var phoneFound = userPhoneLookup.TryGetValue(extension, out var userPhone);
                        var indexNumber = phoneFound ? userPhone.IndexNumber : "";
                        int? ebillUserId = phoneFound ? userPhone.EbillUserId : null;

                        // Populate row with PSTN column names
                        dataRow["Extension"] = extension;
                        dataRow["DialedNumber"] = cleanDialed;
                        dataRow["CallDate"] = callDateTime;
                        dataRow["CallTime"] = callTime;
                        dataRow["Destination"] = callType;
                        dataRow["DestinationLine"] = "";
                        dataRow["DurationExtended"] = parsedDuration;
                        dataRow["Duration"] = parsedDuration;
                        dataRow["AmountKSH"] = cost; // PSTN costs are in KES
                        dataRow["AmountUSD"] = 0m;
                        dataRow["IndexNumber"] = indexNumber ?? "";
                        dataRow["Carrier"] = "PSTN";
                        dataRow["UserPhoneId"] = phoneFound ? (object)userPhone.Id : DBNull.Value;
                        dataRow["EbillUserId"] = ebillUserId.HasValue ? (object)ebillUserId.Value : DBNull.Value;
                        dataRow["BillingPeriod"] = billingPeriodString;
                        dataRow["CallMonth"] = billingMonth;
                        dataRow["CallYear"] = billingYear;
                        dataRow["CreatedDate"] = DateTime.UtcNow;
                        dataRow["CreatedBy"] = User.Identity?.Name ?? "PSTNUpload";
                        dataRow["ProcessingStatus"] = 0; // Staged

                        dataTable.Rows.Add(dataRow);

                        // Bulk insert when batch is full
                        if (dataTable.Rows.Count >= BATCH_SIZE)
                        {
                            await BulkInsertPstnAsync(dataTable);
                            result.SuccessCount += dataTable.Rows.Count;
                            dataTable.Clear();

                            _logger.LogInformation("Inserted batch: {Count} records to PSTNs, Total: {Total}",
                                BATCH_SIZE, result.SuccessCount);
                        }
                    }
                    catch (Exception ex)
                    {
                        result.ErrorCount++;
                        _logger.LogWarning("Error processing PSTN row: {Error}", ex.Message);
                    }
                }
            } while (reader.NextResult()); // Move to next sheet if any

            // Insert remaining records
            if (dataTable.Rows.Count > 0)
            {
                await BulkInsertPstnAsync(dataTable);
                result.SuccessCount += dataTable.Rows.Count;
            }

            _logger.LogInformation("PSTN import completed: {Success} success, {Skipped} skipped, {Errors} errors",
                result.SuccessCount, result.SkippedCount, result.ErrorCount);

            return result;
        }

        private async Task<ImportResult> ImportPrivateWireExcelFileAsync(string filePath, int billingMonth, int billingYear)
        {
            const int BATCH_SIZE = 10000;
            var result = new ImportResult();

            // Pre-load user phone lookups
            var userPhoneLookup = await LoadUserPhoneLookup();

            var dataTable = CreatePrivateWireDataTable();
            var billingPeriodDate = new DateTime(billingYear, billingMonth, 1);
            var billingPeriodString = billingPeriodDate.ToString("yyyy-MM-dd");

            string? currentExtension = null;

            // Required for .xls support
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            using var stream = System.IO.File.Open(filePath, FileMode.Open, FileAccess.Read);
            using var reader = ExcelReaderFactory.CreateReader(stream);

            Dictionary<string, int>? columnIndices = null;
            bool inDataSection = false;

            do
            {
                while (reader.Read())
                {
                    var values = new string[reader.FieldCount];
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        values[i] = reader.GetValue(i)?.ToString()?.Trim() ?? "";
                    }

                    var firstCell = values.Length > 0 ? values[0] : "";

                    // Check for extension header row
                    if (firstCell.Contains("Extension 2:"))
                    {
                        currentExtension = ExtractPstnExtension(firstCell);
                        columnIndices = null;
                        inDataSection = false;
                        continue;
                    }

                    // Look for column header row
                    if (columnIndices == null && IsPstnHeaderRow(values))
                    {
                        columnIndices = BuildColumnIndex(values);
                        inDataSection = true;
                        continue;
                    }

                    // Skip format hint row
                    if (firstCell.ToLower().Contains("mm/dd/yyyy") || firstCell.ToLower().Contains("mm/dd"))
                    {
                        continue;
                    }

                    // Skip summary rows
                    if (firstCell.ToLower() == "type" && values.Length > 1 && values[1].ToLower() == "number")
                    {
                        inDataSection = false;
                        continue;
                    }

                    // Skip if not in data section
                    if (!inDataSection || columnIndices == null || string.IsNullOrWhiteSpace(currentExtension))
                    {
                        result.SkippedCount++;
                        continue;
                    }

                    // Process data row
                    var dateValue = GetValue(values, columnIndices, "date");

                    if (string.IsNullOrWhiteSpace(dateValue) || !IsValidPstnDate(dateValue))
                    {
                        result.SkippedCount++;
                        continue;
                    }

                    try
                    {
                        var dataRow = dataTable.NewRow();

                        var extension = currentExtension;
                        var timeValue = GetValue(values, columnIndices, "time") ?? "";
                        var distantNumber = GetValue(values, columnIndices, "distant") ?? "";
                        var durationValue = GetValue(values, columnIndices, "duration") ?? "";
                        var costValue = GetValue(values, columnIndices, "cost") ?? "0";
                        var callType = GetValue(values, columnIndices, "place") ?? GetValue(values, columnIndices, "call") ?? "";

                        // Parse date and time (mm/dd/yyyy format)
                        var callDateTime = ParsePstnDateTime(dateValue, timeValue);

                        // Parse time value to TimeSpan (may come as datetime string from Excel)
                        var callTime = ParsePstnTime(timeValue);

                        // Parse duration (h:mm:ss format) to mm.ss
                        var parsedDuration = ParsePstnDuration(durationValue);

                        // Clean dialed number (remove ER1+90+ prefix)
                        var cleanDialed = CleanPstnDialedNumber(distantNumber);

                        // Parse cost (already in decimal format)
                        var cost = ParseDecimal(costValue);

                        // Lookup user phone
                        var phoneFound = userPhoneLookup.TryGetValue(extension, out var userPhone);
                        var indexNumber = phoneFound ? userPhone.IndexNumber : "";
                        int? ebillUserId = phoneFound ? userPhone.EbillUserId : null;

                        // Populate row with PrivateWire column names (no Carrier column)
                        dataRow["Extension"] = extension;
                        dataRow["DialedNumber"] = cleanDialed;
                        dataRow["CallDate"] = callDateTime;
                        dataRow["CallTime"] = callTime;
                        dataRow["Destination"] = callType;
                        dataRow["DestinationLine"] = "";
                        dataRow["DurationExtended"] = parsedDuration;
                        dataRow["Duration"] = parsedDuration;
                        dataRow["AmountKSH"] = 0m; // Private Wire costs are in USD
                        dataRow["AmountUSD"] = cost;
                        dataRow["IndexNumber"] = indexNumber ?? "";
                        dataRow["UserPhoneId"] = phoneFound ? (object)userPhone.Id : DBNull.Value;
                        dataRow["EbillUserId"] = ebillUserId.HasValue ? (object)ebillUserId.Value : DBNull.Value;
                        dataRow["BillingPeriod"] = billingPeriodString;
                        dataRow["CallMonth"] = billingMonth;
                        dataRow["CallYear"] = billingYear;
                        dataRow["CreatedDate"] = DateTime.UtcNow;
                        dataRow["CreatedBy"] = User.Identity?.Name ?? "PrivateWireUpload";
                        dataRow["ProcessingStatus"] = 0; // Staged

                        dataTable.Rows.Add(dataRow);

                        // Bulk insert when batch is full
                        if (dataTable.Rows.Count >= BATCH_SIZE)
                        {
                            await BulkInsertPrivateWireAsync(dataTable);
                            result.SuccessCount += dataTable.Rows.Count;
                            dataTable.Clear();

                            _logger.LogInformation("Inserted batch: {Count} records to PrivateWires, Total: {Total}",
                                BATCH_SIZE, result.SuccessCount);
                        }
                    }
                    catch (Exception ex)
                    {
                        result.ErrorCount++;
                        _logger.LogWarning("Error processing Private Wire row: {Error}", ex.Message);
                    }
                }
            } while (reader.NextResult()); // Move to next sheet if any

            // Insert remaining records
            if (dataTable.Rows.Count > 0)
            {
                await BulkInsertPrivateWireAsync(dataTable);
                result.SuccessCount += dataTable.Rows.Count;
            }

            _logger.LogInformation("Private Wire import completed: {Success} success, {Skipped} skipped, {Errors} errors",
                result.SuccessCount, result.SkippedCount, result.ErrorCount);

            return result;
        }

        private bool IsPstnHeaderRow(string[] values)
        {
            var lowerValues = values.Select(v => v.ToLower().Trim()).ToArray();
            // Check for Date, Time, and Distant columns
            return lowerValues.Contains("date") && lowerValues.Contains("time") &&
                   lowerValues.Any(v => v.Contains("distant"));
        }

        private string? ExtractPstnExtension(string headerRow)
        {
            // Extract extension from "Extension 2:XXXXX: Staff Name"
            var match = System.Text.RegularExpressions.Regex.Match(headerRow, @"Extension\s*2:(\d+):");
            return match.Success ? match.Groups[1].Value : null;
        }

        private string CleanPstnDialedNumber(string distantNumber)
        {
            // Remove ER1+90+ prefix and clean the number
            var cleaned = distantNumber;
            if (cleaned.StartsWith("ER1+90+"))
                cleaned = cleaned.Substring(7);
            else if (cleaned.StartsWith("ER1+"))
                cleaned = cleaned.Substring(4);

            // Remove any remaining + signs
            cleaned = cleaned.Replace("+", "");

            return cleaned;
        }

        private bool IsValidPstnDate(string? dateValue)
        {
            if (string.IsNullOrWhiteSpace(dateValue)) return false;

            // PSTN uses mm/dd/yyyy format
            string[] formats = { "MM/dd/yyyy", "M/d/yyyy", "MM/d/yyyy", "M/dd/yyyy" };

            return DateTime.TryParseExact(dateValue, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out _)
                   || DateTime.TryParse(dateValue, out _);
        }

        private DateTime ParsePstnDateTime(string dateValue, string timeValue)
        {
            // PSTN uses mm/dd/yyyy format
            string[] formats = { "MM/dd/yyyy", "M/d/yyyy", "MM/d/yyyy", "M/dd/yyyy" };

            foreach (var format in formats)
            {
                if (DateTime.TryParseExact(dateValue, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                {
                    // Try adding time if available (hh:mm format)
                    if (!string.IsNullOrWhiteSpace(timeValue) && TimeSpan.TryParse(timeValue, out var time))
                    {
                        return date.Add(time);
                    }
                    return date;
                }
            }

            // Fallback to general parsing
            if (DateTime.TryParse(dateValue, out var fallbackDate))
            {
                return fallbackDate;
            }

            return DateTime.MinValue;
        }

        private decimal ParsePstnDuration(string? durationValue)
        {
            if (string.IsNullOrWhiteSpace(durationValue)) return 0;

            // Parse h:mm:ss or hh:mm:ss format → mm.ss
            if (durationValue.Contains(':'))
            {
                var parts = durationValue.Split(':');
                if (parts.Length == 3)
                {
                    if (int.TryParse(parts[0], out int hours) &&
                        int.TryParse(parts[1], out int minutes) &&
                        int.TryParse(parts[2], out int seconds))
                    {
                        int totalMinutes = (hours * 60) + minutes;
                        return totalMinutes + (seconds / 100m);
                    }
                }
                else if (parts.Length == 2)
                {
                    if (int.TryParse(parts[0], out int minutes) &&
                        int.TryParse(parts[1], out int seconds))
                    {
                        return minutes + (seconds / 100m);
                    }
                }
            }

            if (decimal.TryParse(durationValue, out var result))
            {
                return result;
            }

            return 0;
        }

        private TimeSpan ParsePstnTime(string? timeValue)
        {
            if (string.IsNullOrWhiteSpace(timeValue)) return TimeSpan.Zero;

            // Handle Excel datetime format like "12/31/1899 4:12:13 PM"
            if (DateTime.TryParse(timeValue, out var dateTime))
            {
                return dateTime.TimeOfDay;
            }

            // Handle direct TimeSpan format
            if (TimeSpan.TryParse(timeValue, out var timeSpan))
            {
                return timeSpan;
            }

            return TimeSpan.Zero;
        }

        /// <summary>
        /// Extracts and formats time only from Excel datetime strings like "12/30/1899 14:30:00" → "14:30:00"
        /// </summary>
        private string FormatTimeOnly(string? timeValue)
        {
            if (string.IsNullOrWhiteSpace(timeValue)) return "";

            // If it's already just a time (hh:mm or hh:mm:ss), return formatted
            if (TimeSpan.TryParse(timeValue, out var ts))
            {
                return ts.ToString(@"hh\:mm\:ss");
            }

            // Handle Excel datetime format like "12/31/1899 4:12:13 PM" - extract just the time
            if (DateTime.TryParse(timeValue, out var dt))
            {
                return dt.ToString("HH:mm:ss");
            }

            // Handle Excel numeric time format (fractional day, e.g., 0.604166667 = 14:30:00)
            if (double.TryParse(timeValue, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var numericTime))
            {
                // Excel stores time as fraction of a day (0.5 = 12:00:00)
                if (numericTime >= 0 && numericTime < 1)
                {
                    var timeSpan = TimeSpan.FromDays(numericTime);
                    return timeSpan.ToString(@"hh\:mm\:ss");
                }
                // If it's a full OLE Automation date, extract time portion
                try
                {
                    var oleDate = DateTime.FromOADate(numericTime);
                    return oleDate.ToString("HH:mm:ss");
                }
                catch { }
            }

            return timeValue;
        }

        private class FileAnalysisResult
        {
            public bool HeaderFound { get; set; }
            public int HeaderRowNumber { get; set; }
            public int TotalRows { get; set; }
            public int ValidRows { get; set; }
            public int SkippedRows { get; set; }
            public List<SafaricomPreviewRow> PreviewRows { get; set; } = new();
            public decimal TotalAmountKES { get; set; }
            public decimal TotalAmountUSD { get; set; }
            public int LinkedRecords { get; set; }
            public int UnlinkedRecords { get; set; }
        }

        private class ImportResult
        {
            public int SuccessCount { get; set; }
            public int SkippedCount { get; set; }
            public int ErrorCount { get; set; }
        }
    }
}
