using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TAB.Web.Data;
using TAB.Web.Models;
using TAB.Web.Services;
using System.Text.Json;

namespace TAB.Web.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class CallLogStagingModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ICallLogStagingService _stagingService;
        private readonly ILogger<CallLogStagingModel> _logger;
        private readonly UserManager<ApplicationUser> _userManager;

        public CallLogStagingModel(
            ApplicationDbContext context,
            ICallLogStagingService stagingService,
            ILogger<CallLogStagingModel> logger,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _stagingService = stagingService;
            _logger = logger;
            _userManager = userManager;
        }

        // View Properties
        public PagedResult<CallLogStaging> StagedLogs { get; set; } = new();
        public List<StagingBatch> RecentBatches { get; set; } = new();
        public StagingBatch? CurrentBatch { get; set; }
        public Dictionary<string, int> BatchStatistics { get; set; } = new();
        public string CurrentUserName { get; set; } = string.Empty;

        // Filter Properties
        [BindProperty(SupportsGet = true)]
        public Guid? BatchId { get; set; }

        [BindProperty(SupportsGet = true)]
        public VerificationStatus? Status { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool? HasAnomalies { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? StartDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? EndDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public int PageSize { get; set; } = 25;

        [BindProperty(SupportsGet = true)]
        public int? PageSizeParam { get; set; }

        // Statistics Properties
        public int TotalBatches { get; set; }
        public int TotalPendingRecords { get; set; }
        public int TotalVerifiedToday { get; set; }
        public int TotalAnomaliesDetected { get; set; }

        // Pagination helpers
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => StagedLogs.TotalPages > PageNumber;
        public int TotalCount => StagedLogs.TotalCount;
        public int TotalPages => StagedLogs.TotalPages;

        // Form Properties
        [BindProperty]
        public ConsolidationInput ConsolidationForm { get; set; } = new();

        [BindProperty]
        public List<int> SelectedIds { get; set; } = new();

        [TempData]
        public string? StatusMessage { get; set; }

        [TempData]
        public string? StatusMessageClass { get; set; }

        // Dropdown Lists
        public List<SelectListItem> BatchList { get; set; } = new();
        public List<SelectListItem> StatusList { get; set; } = new();

        public class ConsolidationInput
        {
            public DateTime StartDate { get; set; } = DateTime.Today.AddDays(-30);
            public DateTime EndDate { get; set; } = DateTime.Today;
        }

        public async Task OnGetAsync()
        {
            // Apply page size parameter
            if (PageSizeParam.HasValue)
            {
                PageSize = PageSizeParam.Value;
            }

            await LoadPageDataAsync();

            // Calculate statistics
            await CalculateStatisticsAsync();

            // Diagnostic: Check record counts in source tables
            if (!string.IsNullOrEmpty(Request.Query["debug"]))
            {
                var safaricomCount = await _context.Safaricoms.CountAsync();
                var airtelCount = await _context.Airtels.CountAsync();
                var pstnCount = await _context.PSTNs.CountAsync();
                var privateWireCount = await _context.PrivateWires.CountAsync();

                _logger.LogInformation($"Source table counts - Safaricom: {safaricomCount}, Airtel: {airtelCount}, PSTN: {pstnCount}, PrivateWire: {privateWireCount}");

                // Check date ranges
                var safaricomDates = await _context.Safaricoms
                    .Where(s => s.CallDate != null)
                    .Select(s => s.CallDate)
                    .OrderBy(d => d)
                    .ToListAsync();

                var airtelDates = await _context.Airtels
                    .Where(a => a.CallDate != null)
                    .Select(a => a.CallDate)
                    .OrderBy(d => d)
                    .ToListAsync();

                if (safaricomDates.Any())
                    _logger.LogInformation($"Safaricom date range: {safaricomDates.First()} to {safaricomDates.Last()}");

                if (airtelDates.Any())
                    _logger.LogInformation($"Airtel date range: {airtelDates.First()} to {airtelDates.Last()}");

                TempData["DebugMessage"] = $"Records - Safaricom: {safaricomCount}, Airtel: {airtelCount}, PSTN: {pstnCount}, PrivateWire: {privateWireCount}";
            }
        }

        private async Task LoadPageDataAsync()
        {
            // Get current user
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null)
            {
                CurrentUserName = !string.IsNullOrEmpty(currentUser.FirstName) && !string.IsNullOrEmpty(currentUser.LastName)
                    ? $"{currentUser.FirstName} {currentUser.LastName}"
                    : currentUser.UserName ?? "Administrator";
            }

            // Load recent batches
            RecentBatches = await _stagingService.GetRecentBatchesAsync(5);

            // Load batch dropdown
            BatchList = RecentBatches.Select(b => new SelectListItem
            {
                Value = b.Id.ToString(),
                Text = $"{b.BatchName} ({b.CreatedDate:yyyy-MM-dd})"
            }).ToList();

            // Load status dropdown
            StatusList = Enum.GetValues<VerificationStatus>()
                .Select(s => new SelectListItem
                {
                    Value = s.ToString(),
                    Text = s.ToString()
                }).ToList();

            // If a batch is selected, load its details
            if (BatchId.HasValue)
            {
                CurrentBatch = await _stagingService.GetBatchDetailsAsync(BatchId.Value);
                BatchStatistics = await _stagingService.GetBatchStatisticsAsync(BatchId.Value);
            }

            // Load staged logs with filters
            var filter = new StagingFilter
            {
                BatchId = BatchId,
                Status = Status,
                HasAnomalies = HasAnomalies,
                SearchTerm = SearchTerm,
                StartDate = StartDate,
                EndDate = EndDate,
                PageNumber = PageNumber,
                PageSize = PageSize
            };

            StagedLogs = await _stagingService.GetStagedLogsAsync(filter);
        }

        private async Task CalculateStatisticsAsync()
        {
            var today = DateTime.UtcNow.Date;

            // Total batches
            TotalBatches = await _context.StagingBatches.CountAsync();

            // Total pending records across all batches
            TotalPendingRecords = await _context.CallLogStagings
                .Where(c => c.VerificationStatus == VerificationStatus.Pending)
                .CountAsync();

            // Total verified today
            TotalVerifiedToday = await _context.CallLogStagings
                .Where(c => c.VerificationStatus == VerificationStatus.Verified &&
                           c.VerificationDate != null &&
                           c.VerificationDate.Value.Date == today)
                .CountAsync();

            // Total anomalies detected
            TotalAnomaliesDetected = await _context.CallLogStagings
                .Where(c => c.HasAnomalies)
                .CountAsync();
        }

        public async Task<IActionResult> OnPostConsolidateAsync()
        {
            // Get the billing month from the form
            var billingMonthString = Request.Form["BillingMonth"];
            if (string.IsNullOrEmpty(billingMonthString))
            {
                StatusMessage = "Please select a billing month.";
                StatusMessageClass = "danger";
                await LoadPageDataAsync();
                return Page();
            }

            try
            {
                // Parse the month input (format: yyyy-MM)
                var billingDate = DateTime.ParseExact(billingMonthString + "-01", "yyyy-MM-dd", null);
                var startDate = new DateTime(billingDate.Year, billingDate.Month, 1);
                var endDate = startDate.AddMonths(1).AddDays(-1); // Last day of the month

                // Check for existing batch first
                var existingBatch = await _stagingService.GetExistingBatchForPeriodAsync(billingDate.Month, billingDate.Year);
                if (existingBatch != null)
                {
                    StatusMessage = $"Cannot create batch: A batch already exists for {billingDate:MMMM yyyy}. " +
                                  $"Batch '{existingBatch.BatchName}' with status '{existingBatch.BatchStatus}' " +
                                  $"was created on {existingBatch.CreatedDate:yyyy-MM-dd}. " +
                                  $"Please complete or delete the existing batch first.";
                    StatusMessageClass = "danger";
                    await LoadPageDataAsync();
                    return Page();
                }

                var userName = User.Identity?.Name ?? "System";
                var batch = await _stagingService.ConsolidateCallLogsAsync(
                    startDate,
                    endDate,
                    userName);

                StatusMessage = $"Successfully created batch '{batch.BatchName}' with {batch.TotalRecords} records. {batch.RecordsWithAnomalies} anomalies detected.";
                StatusMessageClass = "success";

                // Redirect to the new batch
                return RedirectToPage(new { BatchId = batch.Id });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Duplicate batch attempt");
                StatusMessage = ex.Message;
                StatusMessageClass = "warning";
                await LoadPageDataAsync();
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error consolidating call logs");
                StatusMessage = $"Error: {ex.Message}";
                StatusMessageClass = "danger";
                await LoadPageDataAsync();
                return Page();
            }
        }

        public async Task<IActionResult> OnPostVerifySelectedAsync()
        {
            if (!SelectedIds.Any())
            {
                StatusMessage = "Please select records to verify";
                StatusMessageClass = "warning";
                return RedirectToPage(new { BatchId });
            }

            try
            {
                var userName = User.Identity?.Name ?? "System";
                var count = await _stagingService.BulkVerifyAsync(SelectedIds, userName);

                StatusMessage = $"Successfully verified {count} records";
                StatusMessageClass = "success";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying records");
                StatusMessage = $"Error: {ex.Message}";
                StatusMessageClass = "danger";
            }

            return RedirectToPage(new { BatchId });
        }

        public async Task<IActionResult> OnPostRejectSelectedAsync(string reason)
        {
            if (!SelectedIds.Any())
            {
                StatusMessage = "Please select records to reject";
                StatusMessageClass = "warning";
                return RedirectToPage(new { BatchId });
            }

            if (string.IsNullOrEmpty(reason))
            {
                StatusMessage = "Please provide a reason for rejection";
                StatusMessageClass = "warning";
                return RedirectToPage(new { BatchId });
            }

            try
            {
                var userName = User.Identity?.Name ?? "System";
                var count = await _stagingService.BulkRejectAsync(SelectedIds, userName, reason);

                StatusMessage = $"Successfully rejected {count} records";
                StatusMessageClass = "success";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting records");
                StatusMessage = $"Error: {ex.Message}";
                StatusMessageClass = "danger";
            }

            return RedirectToPage(new { BatchId });
        }

        public async Task<IActionResult> OnPostPushToProductionAsync(Guid batchId, string verificationPeriod)
        {
            try
            {
                // Parse verification period
                DateTime? verificationDeadline = null;
                if (!string.IsNullOrEmpty(verificationPeriod))
                {
                    if (DateTime.TryParse(verificationPeriod, out DateTime parsedDate))
                    {
                        verificationDeadline = parsedDate;
                    }
                }

                var count = await _stagingService.PushToProductionAsync(batchId, verificationDeadline);

                if (count > 0)
                {
                    var periodMessage = verificationDeadline.HasValue
                        ? $" (Verification deadline: {verificationDeadline.Value:MMM dd, yyyy})"
                        : "";
                    StatusMessage = $"Successfully pushed {count} records to production{periodMessage}";
                    StatusMessageClass = "success";
                }
                else
                {
                    StatusMessage = "No verified records to push or batch not ready";
                    StatusMessageClass = "warning";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error pushing to production");
                StatusMessage = $"Error: {ex.Message}";
                StatusMessageClass = "danger";
            }

            return RedirectToPage(new { BatchId = batchId });
        }

        public async Task<IActionResult> OnGetVerifyAsync(int id)
        {
            try
            {
                var userName = User.Identity?.Name ?? "System";
                await _stagingService.VerifyCallLogAsync(id, userName);

                return new JsonResult(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying record {Id}", id);
                return new JsonResult(new { success = false, error = ex.Message });
            }
        }

        public async Task<IActionResult> OnGetRejectAsync(int id, string reason)
        {
            try
            {
                var userName = User.Identity?.Name ?? "System";
                await _stagingService.RejectCallLogAsync(id, userName, reason);

                return new JsonResult(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting record {Id}", id);
                return new JsonResult(new { success = false, error = ex.Message });
            }
        }

        public async Task<IActionResult> OnGetAnomaliesAsync(int id)
        {
            try
            {
                var anomalies = await _stagingService.DetectAnomaliesAsync(id);
                return new JsonResult(anomalies);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting anomalies for record {Id}", id);
                return new JsonResult(new { error = ex.Message });
            }
        }

        public async Task<IActionResult> OnGetPendingVerificationSummaryAsync()
        {
            try
            {
                // Get all unverified records grouped by batch
                var unverifiedByBatch = await _context.CallLogStagings
                    .Where(c => c.VerificationStatus == VerificationStatus.Pending ||
                               c.VerificationStatus == VerificationStatus.RequiresReview)
                    .GroupBy(c => c.BatchId)
                    .Select(g => new
                    {
                        BatchId = g.Key,
                        PendingCount = g.Count(x => x.VerificationStatus == VerificationStatus.Pending),
                        RequiresReviewCount = g.Count(x => x.VerificationStatus == VerificationStatus.RequiresReview),
                        TotalUnverified = g.Count(),
                        OldestRecord = g.Min(x => x.CreatedDate),
                        TotalAmount = g.Sum(x => x.CallCostUSD)
                    })
                    .ToListAsync();

                // Get batch details for unverified records
                var batchIds = unverifiedByBatch.Select(x => x.BatchId).Distinct().ToList();
                var batches = await _context.StagingBatches
                    .Where(b => batchIds.Contains(b.Id))
                    .Select(b => new
                    {
                        b.Id,
                        b.BatchName,
                        b.BatchStatus,
                        b.CreatedDate,
                        b.TotalRecords
                    })
                    .ToListAsync();

                // Combine batch information
                var batchSummaries = from u in unverifiedByBatch
                                     join b in batches on u.BatchId equals b.Id
                                     orderby b.CreatedDate descending
                                     select new
                                     {
                                         BatchId = b.Id,
                                         BatchName = b.BatchName,
                                         BatchStatus = b.BatchStatus.ToString(),
                                         BatchCreatedDate = b.CreatedDate,
                                         TotalBatchRecords = b.TotalRecords,
                                         PendingCount = u.PendingCount,
                                         RequiresReviewCount = u.RequiresReviewCount,
                                         TotalUnverified = u.TotalUnverified,
                                         PercentageUnverified = b.TotalRecords > 0 ? (u.TotalUnverified * 100.0 / b.TotalRecords) : 0,
                                         OldestUnverifiedDate = u.OldestRecord,
                                         TotalUnverifiedAmount = u.TotalAmount
                                     };

                // Get overall statistics
                var totalUnverified = unverifiedByBatch.Sum(x => x.TotalUnverified);
                var totalPending = unverifiedByBatch.Sum(x => x.PendingCount);
                var totalRequiresReview = unverifiedByBatch.Sum(x => x.RequiresReviewCount);
                var totalUnverifiedAmount = unverifiedByBatch.Sum(x => x.TotalAmount);

                // Get count of batches with unverified records
                var batchesWithUnverified = batchIds.Count;

                // Get aging information (records older than 7 days)
                var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);
                var agingRecords = await _context.CallLogStagings
                    .Where(c => (c.VerificationStatus == VerificationStatus.Pending ||
                                c.VerificationStatus == VerificationStatus.RequiresReview) &&
                               c.CreatedDate < sevenDaysAgo)
                    .CountAsync();

                return new JsonResult(new
                {
                    success = true,
                    summary = new
                    {
                        totalUnverified,
                        totalPending,
                        totalRequiresReview,
                        totalUnverifiedAmount,
                        batchesWithUnverified,
                        agingRecords,
                        oldestUnverifiedDate = unverifiedByBatch.Any() ? unverifiedByBatch.Min(x => x.OldestRecord) : (DateTime?)null
                    },
                    batches = batchSummaries.ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending verification summary");
                return new JsonResult(new { error = ex.Message });
            }
        }

        public async Task<IActionResult> OnGetConsolidationInfoAsync(string billingMonth)
        {
            try
            {
                // Parse the billing month
                if (!DateTime.TryParseExact(billingMonth + "-01", "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out var billingDate))
                {
                    return new JsonResult(new { error = "Invalid billing month format" });
                }

                var startDate = new DateTime(billingDate.Year, billingDate.Month, 1);
                var endDate = startDate.AddMonths(1).AddDays(-1);

                // Check for existing batch for this period
                var existingBatch = await _stagingService.GetExistingBatchForPeriodAsync(billingDate.Month, billingDate.Year);

                // Get unverified records count in staging
                var unverifiedCount = await _context.CallLogStagings
                    .Where(c => c.VerificationStatus == VerificationStatus.Pending ||
                               c.VerificationStatus == VerificationStatus.RequiresReview)
                    .CountAsync();

                // Get pending batches count
                var pendingBatches = await _context.StagingBatches
                    .Where(b => b.BatchStatus == BatchStatus.Processing ||
                               b.BatchStatus == BatchStatus.PartiallyVerified)
                    .CountAsync();

                // Get source records count for the selected month
                var safaricomCount = await _context.Safaricoms
                    .Where(s => s.CallDate >= startDate && s.CallDate <= endDate &&
                               (s.ProcessingStatus == ProcessingStatus.Staged || s.ProcessingStatus == ProcessingStatus.Failed))
                    .CountAsync();

                var airtelCount = await _context.Airtels
                    .Where(a => a.CallDate >= startDate && a.CallDate <= endDate &&
                               (a.ProcessingStatus == ProcessingStatus.Staged || a.ProcessingStatus == ProcessingStatus.Failed))
                    .CountAsync();

                var pstnCount = await _context.PSTNs
                    .Where(p => p.CallDate >= startDate && p.CallDate <= endDate &&
                               (p.ProcessingStatus == ProcessingStatus.Staged || p.ProcessingStatus == ProcessingStatus.Failed))
                    .CountAsync();

                var privateWireCount = await _context.PrivateWires
                    .Where(pw => pw.CallDate >= startDate && pw.CallDate <= endDate &&
                                (pw.ProcessingStatus == ProcessingStatus.Staged || pw.ProcessingStatus == ProcessingStatus.Failed))
                    .CountAsync();

                var totalSourceRecords = safaricomCount + airtelCount + pstnCount + privateWireCount;

                // Check for already processed records in this period
                // Using InvoiceDate as a proxy for call date in production CallLogs
                var alreadyProcessedCount = await _context.CallLogs
                    .Where(c => c.InvoiceDate >= startDate && c.InvoiceDate <= endDate)
                    .CountAsync();

                // Get recent batches for this month
                // Using CreatedDate to find batches created within the billing month
                var recentBatchesForMonth = await _context.StagingBatches
                    .Where(b => b.CreatedDate.Month == billingDate.Month &&
                               b.CreatedDate.Year == billingDate.Year)
                    .OrderByDescending(b => b.CreatedDate)
                    .Take(3)
                    .Select(b => new
                    {
                        b.BatchName,
                        b.BatchStatus,
                        b.CreatedDate,
                        b.TotalRecords,
                        b.RecordsWithAnomalies
                    })
                    .ToListAsync();

                return new JsonResult(new
                {
                    success = true,
                    existingBatch = existingBatch != null ? new
                    {
                        existingBatch.BatchName,
                        existingBatch.BatchStatus,
                        existingBatch.CreatedDate,
                        existingBatch.TotalRecords
                    } : null,
                    statistics = new
                    {
                        unverifiedCount,
                        pendingBatches,
                        totalSourceRecords,
                        safaricomCount,
                        airtelCount,
                        pstnCount,
                        privateWireCount,
                        alreadyProcessedCount,
                        billingMonth = billingDate.ToString("MMMM yyyy")
                    },
                    recentBatchesForMonth,
                    warnings = new List<string>()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting consolidation info");
                return new JsonResult(new { error = ex.Message });
            }
        }

        public async Task<IActionResult> OnGetValidateStaffAsync(string indexNumber)
        {
            try
            {
                var staff = await _context.EbillUsers
                    .Include(e => e.OrganizationEntity)
                    .Include(e => e.OfficeEntity)
                    .Where(e => e.IndexNumber == indexNumber)
                    .Select(e => new
                    {
                        found = true,
                        name = e.FullName,
                        organization = e.OrganizationEntity != null ? e.OrganizationEntity.Name : "Unknown",
                        office = e.OfficeEntity != null ? e.OfficeEntity.Name : "Unknown",
                        email = e.Email,
                        isActive = e.IsActive
                    })
                    .FirstOrDefaultAsync();

                if (staff == null)
                {
                    return new JsonResult(new { found = false });
                }

                return new JsonResult(staff);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating staff member");
                return new JsonResult(new { found = false, error = ex.Message });
            }
        }

        public async Task<IActionResult> OnPostConsolidateInterimAsync([FromBody] InterimConsolidationInput input)
        {
            try
            {
                // Validate input
                if (string.IsNullOrEmpty(input.StaffIndexNumber))
                {
                    return new JsonResult(new { success = false, error = "Staff index number is required" });
                }

                // Check if staff exists
                var staffMember = await _context.EbillUsers
                    .FirstOrDefaultAsync(e => e.IndexNumber == input.StaffIndexNumber);

                if (staffMember == null)
                {
                    return new JsonResult(new { success = false, error = "Staff member not found" });
                }

                // Parse dates
                if (!DateTime.TryParse(input.InterimStartDate, out var startDate) ||
                    !DateTime.TryParse(input.InterimEndDate, out var endDate))
                {
                    return new JsonResult(new { success = false, error = "Invalid date format" });
                }

                // Create interim batch
                var batch = new StagingBatch
                {
                    Id = Guid.NewGuid(),
                    BatchName = $"INTERIM - {input.StaffName} - {DateTime.Parse(input.SeparationDate):yyyy-MM-dd}",
                    BatchType = "Manual",
                    BatchCategory = "INTERIM",
                    CreatedBy = User.Identity?.Name ?? "System",
                    CreatedDate = DateTime.UtcNow,
                    BatchStatus = BatchStatus.Created,
                    Notes = $"Staff Separation: {input.SeparationReason}. Index: {input.StaffIndexNumber}"
                };

                _context.StagingBatches.Add(batch);
                await _context.SaveChangesAsync();

                // Get user's phone numbers
                var userPhones = await _context.UserPhones
                    .Where(up => up.IndexNumber == input.StaffIndexNumber && up.IsActive)
                    .Select(up => up.PhoneNumber)
                    .ToListAsync();

                if (!userPhones.Any())
                {
                    _logger.LogWarning($"No active phone numbers found for staff {input.StaffIndexNumber}");
                }

                var recordsImported = 0;

                // Import from Safaricom
                var safaricomRecords = await _context.Safaricoms
                    .Where(s => userPhones.Contains(s.IndexNumber ?? "") &&
                               s.CallDate >= startDate &&
                               s.CallDate <= endDate &&
                               (s.ProcessingStatus == ProcessingStatus.Staged || s.ProcessingStatus == ProcessingStatus.Failed))
                    .ToListAsync();

                foreach (var record in safaricomRecords)
                {
                    var stagingRecord = new CallLogStaging
                    {
                        BatchId = batch.Id,
                        ImportType = "INTERIM",
                        ExtensionNumber = record.IndexNumber ?? "",
                        CallDate = record.CallDate ?? DateTime.MinValue,
                        CallNumber = record.Dialed ?? "",
                        CallDestination = record.Dest ?? "",
                        CallEndTime = record.CallDate ?? DateTime.MinValue,
                        CallDuration = (int)(record.Dur ?? 0) * 60, // Convert minutes to seconds
                        CallCurrencyCode = "KES",
                        CallCost = record.Cost ?? 0,
                        CallCostUSD = (record.Cost ?? 0) / 125, // Convert KES to USD using approximate rate
                        ResponsibleIndexNumber = input.StaffIndexNumber,
                        ResponsibleUser = staffMember,
                        SourceSystem = "Safaricom",
                        SourceRecordId = record.Id.ToString(),
                        VerificationStatus = VerificationStatus.Pending,
                        CreatedDate = DateTime.UtcNow
                    };

                    _context.CallLogStagings.Add(stagingRecord);
                    record.ProcessingStatus = ProcessingStatus.Processing;
                    recordsImported++;
                }

                // Import from Airtel
                var airtelRecords = await _context.Airtels
                    .Where(a => userPhones.Contains(a.IndexNumber ?? "") &&
                               a.CallDate >= startDate &&
                               a.CallDate <= endDate &&
                               (a.ProcessingStatus == ProcessingStatus.Staged || a.ProcessingStatus == ProcessingStatus.Failed))
                    .ToListAsync();

                foreach (var record in airtelRecords)
                {
                    var stagingRecord = new CallLogStaging
                    {
                        BatchId = batch.Id,
                        ImportType = "INTERIM",
                        ExtensionNumber = record.IndexNumber ?? "",
                        CallDate = record.CallDate ?? DateTime.MinValue,
                        CallNumber = record.Dialed ?? "",
                        CallDestination = record.Dest ?? "",
                        CallEndTime = record.CallDate ?? DateTime.MinValue,
                        CallDuration = (int)(record.Dur ?? 0) * 60,
                        CallCurrencyCode = "KES",
                        CallCost = record.Cost ?? 0,
                        CallCostUSD = (record.Cost ?? 0) / 125, // Convert KES to USD using approximate rate
                        ResponsibleIndexNumber = input.StaffIndexNumber,
                        ResponsibleUser = staffMember,
                        SourceSystem = "Airtel",
                        SourceRecordId = record.Id.ToString(),
                        VerificationStatus = VerificationStatus.Pending,
                        CreatedDate = DateTime.UtcNow
                    };

                    _context.CallLogStagings.Add(stagingRecord);
                    record.ProcessingStatus = ProcessingStatus.Processing;
                    recordsImported++;
                }

                // Update batch statistics
                batch.TotalRecords = recordsImported;
                batch.PendingRecords = recordsImported;
                batch.BatchStatus = BatchStatus.Processing;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Created interim batch {batch.Id} with {recordsImported} records for staff {input.StaffIndexNumber}");

                return new JsonResult(new
                {
                    success = true,
                    batchId = batch.Id,
                    recordsImported = recordsImported,
                    message = $"Successfully imported {recordsImported} interim call records for {input.StaffName}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating interim consolidation");
                return new JsonResult(new { success = false, error = ex.Message });
            }
        }

        // Input model for interim consolidation
        public class InterimConsolidationInput
        {
            public string ConsolidationType { get; set; } = "";
            public string StaffIndexNumber { get; set; } = "";
            public string StaffName { get; set; } = "";
            public string SeparationDate { get; set; } = "";
            public string SeparationReason { get; set; } = "";
            public string InterimStartDate { get; set; } = "";
            public string InterimEndDate { get; set; } = "";
        }

        public async Task<IActionResult> OnPostDeleteBatchAsync(Guid batchId)
        {
            try
            {
                // Check if batch can be deleted
                if (!await _stagingService.CanDeleteBatchAsync(batchId))
                {
                    StatusMessage = "This batch cannot be deleted. It may be published or have records in production.";
                    StatusMessageClass = "danger";
                    return RedirectToPage();
                }

                var userName = User.Identity?.Name ?? "System";
                await _stagingService.DeleteBatchAsync(batchId, userName);

                StatusMessage = "Batch deleted successfully.";
                StatusMessageClass = "success";

                _logger.LogInformation("Batch {BatchId} deleted by {User}", batchId, userName);

                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting batch {BatchId}", batchId);
                StatusMessage = $"Error deleting batch: {ex.Message}";
                StatusMessageClass = "danger";
                return RedirectToPage(new { BatchId = batchId });
            }
        }

        public async Task<IActionResult> OnGetCanDeleteBatchAsync(Guid batchId)
        {
            try
            {
                var canDelete = await _stagingService.CanDeleteBatchAsync(batchId);
                return new JsonResult(new { canDelete });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if batch {BatchId} can be deleted", batchId);
                return new JsonResult(new { canDelete = false, error = ex.Message });
            }
        }
    }
}