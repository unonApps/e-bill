using System.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TAB.Web.Data;
using TAB.Web.Models;
using TAB.Web.Models.Enums;
using TAB.Web.Services;

namespace TAB.Web.Pages.Modules.EBillManagement.CallRecords
{
    [Authorize]
    public class MyCallLogsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ICallLogVerificationService _verificationService;
        private readonly IClassOfServiceCalculationService _calculationService;
        private readonly IDocumentManagementService _documentService;
        private readonly IEnhancedEmailService _emailService;
        private readonly ILogger<MyCallLogsModel> _logger;

        public MyCallLogsModel(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ICallLogVerificationService verificationService,
            IClassOfServiceCalculationService calculationService,
            IDocumentManagementService documentService,
            IEnhancedEmailService emailService,
            ILogger<MyCallLogsModel> logger)
        {
            _context = context;
            _userManager = userManager;
            _verificationService = verificationService;
            _calculationService = calculationService;
            _documentService = documentService;
            _emailService = emailService;
            _logger = logger;
        }

        // Properties - Extension Groups for Level 1 pagination
        public List<ExtensionGroup> ExtensionGroups { get; set; } = new();
        public List<CallRecord> CallRecords { get; set; } = new(); // Keep for backward compatibility
        public string? UserIndexNumber { get; set; }
        public VerificationSummary? Summary { get; set; }
        public decimal AllowanceLimit { get; set; }
        public decimal CurrentUsage { get; set; }
        public decimal RemainingAllowance { get; set; }
        public bool IsOverAllowance { get; set; }

        // Filters
        [BindProperty(SupportsGet = true)]
        public string? FilterStatus { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? FilterAssignmentType { get; set; } // "own", "assigned", "all"

        [BindProperty(SupportsGet = true)]
        public DateTime? FilterStartDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? FilterEndDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public decimal? FilterMinCost { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SortBy { get; set; } = "CallDate";

        [BindProperty(SupportsGet = true)]
        public bool SortDescending { get; set; } = true;

        [BindProperty(SupportsGet = true)]
        public int? FilterMonth { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? FilterYear { get; set; }

        // Pagination - Level 1: Extensions
        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public int PageSize { get; set; } = 10; // Extensions per page (reduced for hierarchical view)

        public int TotalPages { get; set; }
        public int TotalRecords { get; set; } // Total extensions
        public int TotalCallRecords { get; set; } // Total call records across all extensions

        // Level 2 & 3 pagination defaults
        public int DialedNumberPageSize { get; set; } = 20;
        public int CallLogPageSize { get; set; } = 10;

        public HashSet<int> SubmittedCallIds { get; set; } = new HashSet<int>();

        // Store verification approval statuses for each call
        public Dictionary<int, string> VerificationStatuses { get; set; } = new Dictionary<int, string>();

        [TempData]
        public string? StatusMessage { get; set; }

        [TempData]
        public string? StatusMessageClass { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Challenge();

            // Get user's EbillUser record to find IndexNumber
            var ebillUser = await _context.EbillUsers
                .FirstOrDefaultAsync(u => u.Email == user.Email);

            // Check if user is Admin - admins can see all records even without Staff profile
            bool isAdmin = User.IsInRole("Admin");

            if (ebillUser == null && !isAdmin)
            {
                StatusMessage = "Your profile is not linked to an Staff record. Please contact the administrator.";
                StatusMessageClass = "warning";
                return Page();
            }

            UserIndexNumber = ebillUser?.IndexNumber;

            // Set default filter to current month and year ONLY on first visit
            bool isFirstVisit = !Request.Query.ContainsKey("FilterMonth") && !Request.Query.ContainsKey("FilterYear");
            if (isFirstVisit)
            {
                FilterMonth = DateTime.UtcNow.Month;
                FilterYear = DateTime.UtcNow.Year;
            }

            // Page shell renders immediately — data loaded via AJAX (OnGetPageDataAsync)
            return Page();
        }

        /// <summary>
        /// Common auth + filter setup for AJAX endpoints.
        /// Returns null on success (properties set), or an error IActionResult.
        /// </summary>
        private async Task<IActionResult?> InitAjaxRequestAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return new JsonResult(new { error = "Unauthorized" }) { StatusCode = 401 };

            var ebillUser = await _context.EbillUsers
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email == user.Email);

            bool isAdmin = User.IsInRole("Admin");
            UserIndexNumber = ebillUser?.IndexNumber;

            if (string.IsNullOrEmpty(UserIndexNumber) && !isAdmin)
                return new JsonResult(new { error = "Profile not linked" }) { StatusCode = 403 };

            bool isFirstVisit = !Request.Query.ContainsKey("FilterMonth") && !Request.Query.ContainsKey("FilterYear");
            if (isFirstVisit)
            {
                FilterMonth = DateTime.UtcNow.Month;
                FilterYear = DateTime.UtcNow.Year;
            }

            return null; // success
        }

        /// <summary>
        /// AJAX endpoint 1: returns extension groups + pagination (table data).
        /// This is the fast path — renders the table immediately.
        /// </summary>
        public async Task<IActionResult> OnGetExtensionGroupsAsync()
        {
            var error = await InitAjaxRequestAsync();
            if (error != null) return error;

            await LoadCallRecordsAsync();

            return new JsonResult(new
            {
                extensionGroups = ExtensionGroups.Select(g => new
                {
                    groupId = g.GroupId,
                    extension = g.Extension,
                    monthName = g.MonthName,
                    month = g.Month,
                    year = g.Year,
                    callCount = g.CallCount,
                    totalCostUSD = g.TotalCostUSD,
                    totalCostKSH = g.TotalCostKSH,
                    officialUSD = g.OfficialUSD,
                    officialKSH = g.OfficialKSH,
                    personalUSD = g.PersonalUSD,
                    personalKSH = g.PersonalKSH,
                    totalRecoveredUSD = g.TotalRecoveredUSD,
                    totalRecoveredKSH = g.TotalRecoveredKSH,
                    isPrivateWirePrimary = g.IsPrivateWirePrimary,
                    dialedNumberCount = g.DialedNumberCount,
                    submittedCount = g.SubmittedCount,
                    pendingApprovalCount = g.PendingApprovalCount,
                    approvedCount = g.ApprovedCount,
                    rejectedCount = g.RejectedCount,
                    revertedCount = g.RevertedCount,
                    partiallyApprovedCount = g.PartiallyApprovedCount,
                    incomingAssignmentCount = g.IncomingAssignmentCount,
                    assignedFromUser = g.AssignedFromUser,
                    outgoingPendingCount = g.OutgoingPendingCount,
                    assignedToUser = g.AssignedToUser,
                    classOfService = g.ClassOfService,
                    cosService = g.CosService,
                    cosEligibleStaff = g.CosEligibleStaff,
                    cosAirtimeAllowance = g.CosAirtimeAllowance,
                    cosDataAllowance = g.CosDataAllowance,
                    cosHandsetAllowance = g.CosHandsetAllowance
                }),
                pagination = new
                {
                    pageNumber = PageNumber,
                    pageSize = PageSize,
                    totalRecords = TotalRecords,
                    totalPages = TotalPages
                }
            });
        }

        /// <summary>
        /// AJAX endpoint 2: returns summary stats + allowance info.
        /// This can be slower — stats cards fill in after the table is already visible.
        /// </summary>
        public async Task<IActionResult> OnGetSummaryAsync()
        {
            var error = await InitAjaxRequestAsync();
            if (error != null) return error;

            await LoadSummaryAsync();

            return new JsonResult(new
            {
                summary = Summary == null ? null : new
                {
                    totalCalls = Summary.TotalCalls,
                    verifiedCalls = Summary.VerifiedCalls,
                    unverifiedCalls = Summary.UnverifiedCalls,
                    totalAmount = Summary.TotalAmount,
                    verifiedAmount = Summary.VerifiedAmount,
                    personalCalls = Summary.PersonalCalls,
                    officialCalls = Summary.OfficialCalls,
                    compliancePercentage = Summary.CompliancePercentage,
                    overageAmount = Summary.OverageAmount
                },
                allowanceLimit = AllowanceLimit,
                currentUsage = CurrentUsage,
                remainingAllowance = RemainingAllowance,
                isOverAllowance = IsOverAllowance
            });
        }

        private async Task LoadCallRecordsAsync()
        {
            bool isAdmin = User.IsInRole("Admin");
            if (string.IsNullOrEmpty(UserIndexNumber) && !isAdmin)
                return;

            var conn = _context.Database.GetDbConnection();
            var wasOpen = conn.State == ConnectionState.Open;
            if (!wasOpen) await conn.OpenAsync();

            try
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "ebill.sp_GetMyCallLogExtensionGroups";
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandTimeout = 30;

                var p = cmd.Parameters;
                p.Add(new SqlParameter("@UserIndexNumber", (object?)UserIndexNumber ?? DBNull.Value));
                p.Add(new SqlParameter("@IsAdmin", isAdmin));
                p.Add(new SqlParameter("@FilterMonth", (object?)FilterMonth ?? DBNull.Value));
                p.Add(new SqlParameter("@FilterYear", (object?)FilterYear ?? DBNull.Value));
                p.Add(new SqlParameter("@FilterStartDate", (object?)FilterStartDate ?? DBNull.Value));
                p.Add(new SqlParameter("@FilterEndDate", (object?)FilterEndDate ?? DBNull.Value));
                p.Add(new SqlParameter("@FilterMinCost", (object?)FilterMinCost ?? DBNull.Value));
                p.Add(new SqlParameter("@FilterStatus", (object?)FilterStatus ?? DBNull.Value));
                p.Add(new SqlParameter("@FilterAssignmentType", (object?)FilterAssignmentType ?? DBNull.Value));
                p.Add(new SqlParameter("@PageNumber", PageNumber));
                p.Add(new SqlParameter("@PageSize", PageSize));

                using var reader = await cmd.ExecuteReaderAsync();

                // Result set 1: TotalRecords
                if (await reader.ReadAsync())
                {
                    TotalRecords = reader.GetInt32(0);
                    TotalPages = (int)Math.Ceiling(TotalRecords / (double)PageSize);
                }

                // Result set 2: Extension groups
                await reader.NextResultAsync();
                ExtensionGroups = new List<ExtensionGroup>();
                while (await reader.ReadAsync())
                {
                    var group = new ExtensionGroup
                    {
                        Extension = reader.GetString(reader.GetOrdinal("Extension")),
                        Month = reader.GetInt32(reader.GetOrdinal("Month")),
                        Year = reader.GetInt32(reader.GetOrdinal("Year")),
                        CallCount = reader.GetInt32(reader.GetOrdinal("CallCount")),
                        TotalCostUSD = reader.GetDecimal(reader.GetOrdinal("TotalCostUSD")),
                        TotalCostKSH = reader.GetDecimal(reader.GetOrdinal("TotalCostKSH")),
                        OfficialUSD = reader.GetDecimal(reader.GetOrdinal("OfficialUSD")),
                        OfficialKSH = reader.GetDecimal(reader.GetOrdinal("OfficialKSH")),
                        PersonalUSD = reader.GetDecimal(reader.GetOrdinal("PersonalUSD")),
                        PersonalKSH = reader.GetDecimal(reader.GetOrdinal("PersonalKSH")),
                        TotalRecoveredUSD = reader.GetDecimal(reader.GetOrdinal("TotalRecoveredUSD")),
                        TotalRecoveredKSH = reader.GetDecimal(reader.GetOrdinal("TotalRecoveredKSH")),
                        PrivateWireCount = reader.GetInt32(reader.GetOrdinal("PrivateWireCount")),
                        KshSourceCount = reader.GetInt32(reader.GetOrdinal("KshSourceCount")),
                        DialedNumberCount = reader.GetInt32(reader.GetOrdinal("DialedNumberCount")),
                        SubmittedCount = reader.GetInt32(reader.GetOrdinal("SubmittedCount")),
                        PendingApprovalCount = reader.GetInt32(reader.GetOrdinal("PendingApprovalCount")),
                        ApprovedCount = reader.GetInt32(reader.GetOrdinal("ApprovedCount")),
                        PartiallyApprovedCount = reader.GetInt32(reader.GetOrdinal("PartiallyApprovedCount")),
                        RejectedCount = reader.GetInt32(reader.GetOrdinal("RejectedCount")),
                        RevertedCount = reader.GetInt32(reader.GetOrdinal("RevertedCount")),
                        IncomingAssignmentCount = reader.GetInt32(reader.GetOrdinal("IncomingAssignmentCount")),
                        AssignedFromUser = reader.IsDBNull(reader.GetOrdinal("AssignedFromUser")) ? null : reader.GetString(reader.GetOrdinal("AssignedFromUser")),
                        OutgoingPendingCount = reader.GetInt32(reader.GetOrdinal("OutgoingPendingCount")),
                        AssignedToUser = reader.IsDBNull(reader.GetOrdinal("AssignedToUser")) ? null : reader.GetString(reader.GetOrdinal("AssignedToUser")),
                        ClassOfService = reader.IsDBNull(reader.GetOrdinal("ClassOfService")) ? null : reader.GetString(reader.GetOrdinal("ClassOfService")),
                        CosService = reader.IsDBNull(reader.GetOrdinal("CosService")) ? null : reader.GetString(reader.GetOrdinal("CosService")),
                        CosEligibleStaff = reader.IsDBNull(reader.GetOrdinal("CosEligibleStaff")) ? null : reader.GetString(reader.GetOrdinal("CosEligibleStaff")),
                        CosAirtimeAllowance = reader.IsDBNull(reader.GetOrdinal("CosAirtimeAllowance")) ? null : reader.GetString(reader.GetOrdinal("CosAirtimeAllowance")),
                        CosDataAllowance = reader.IsDBNull(reader.GetOrdinal("CosDataAllowance")) ? null : reader.GetString(reader.GetOrdinal("CosDataAllowance")),
                        CosHandsetAllowance = reader.IsDBNull(reader.GetOrdinal("CosHandsetAllowance")) ? null : reader.GetString(reader.GetOrdinal("CosHandsetAllowance"))
                    };
                    group.IsPrivateWirePrimary = group.PrivateWireCount > group.KshSourceCount;
                    group.GroupId = $"{group.Extension}_{group.Month}_{group.Year}".Replace(" ", "_").Replace("-", "_").Replace("+", "");
                    ExtensionGroups.Add(group);
                }
            }
            finally
            {
                if (!wasOpen) await conn.CloseAsync();
            }
        }

        private async Task LoadSummaryAsync()
        {
            bool isAdmin = User.IsInRole("Admin");

            var conn = _context.Database.GetDbConnection();
            var wasOpen = conn.State == ConnectionState.Open;
            if (!wasOpen) await conn.OpenAsync();

            try
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "ebill.sp_GetMyCallLogSummary";
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandTimeout = 30;

                cmd.Parameters.Add(new SqlParameter("@UserIndexNumber", (object?)UserIndexNumber ?? DBNull.Value));
                cmd.Parameters.Add(new SqlParameter("@IsAdmin", isAdmin));
                cmd.Parameters.Add(new SqlParameter("@FilterMonth", (object?)FilterMonth ?? DBNull.Value));
                cmd.Parameters.Add(new SqlParameter("@FilterYear", (object?)FilterYear ?? DBNull.Value));

                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    var totalCalls = reader.GetInt32(reader.GetOrdinal("TotalCalls"));
                    var verifiedCalls = reader.GetInt32(reader.GetOrdinal("VerifiedCalls"));

                    Summary = new VerificationSummary
                    {
                        TotalCalls = totalCalls,
                        VerifiedCalls = verifiedCalls,
                        UnverifiedCalls = reader.GetInt32(reader.GetOrdinal("UnverifiedCalls")),
                        TotalAmount = reader.GetDecimal(reader.GetOrdinal("TotalAmount")),
                        VerifiedAmount = reader.GetDecimal(reader.GetOrdinal("VerifiedAmount")),
                        PersonalCalls = reader.GetInt32(reader.GetOrdinal("PersonalCalls")),
                        OfficialCalls = reader.GetInt32(reader.GetOrdinal("OfficialCalls")),
                        CompliancePercentage = reader.GetDecimal(reader.GetOrdinal("CompliancePercentage")),
                        OverageAmount = 0
                    };
                }
            }
            finally
            {
                if (!wasOpen) await conn.CloseAsync();
            }

            if (Summary == null) return;

            // Admin path: no allowance calculation needed
            if (string.IsNullOrEmpty(UserIndexNumber) && isAdmin)
            {
                AllowanceLimit = 0;
                CurrentUsage = Summary.TotalAmount;
                RemainingAllowance = 0;
                IsOverAllowance = false;
                return;
            }

            if (string.IsNullOrEmpty(UserIndexNumber)) return;

            // Allowance calculation stays in C# (complex business logic with EF relationships)
            var limitNullable = await _calculationService.GetAllowanceLimitAsync(UserIndexNumber);
            AllowanceLimit = limitNullable ?? 0;
            CurrentUsage = Summary.TotalAmount;

            if (limitNullable.HasValue && limitNullable.Value > 0)
            {
                if (CurrentUsage > limitNullable.Value)
                {
                    IsOverAllowance = true;
                    Summary.OverageAmount = CurrentUsage - limitNullable.Value;
                    RemainingAllowance = 0;
                }
                else
                {
                    IsOverAllowance = false;
                    RemainingAllowance = limitNullable.Value - CurrentUsage;
                    Summary.OverageAmount = 0;
                }
            }
            else
            {
                IsOverAllowance = false;
                RemainingAllowance = 0;
                Summary.OverageAmount = 0;
            }
        }

        public async Task<IActionResult> OnPostQuickVerifyAsync(List<int> selectedIds, string verificationType)
        {
            if (selectedIds == null || !selectedIds.Any())
            {
                StatusMessage = "Please select at least one call to verify.";
                StatusMessageClass = "warning";
                return RedirectToPage();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Challenge();

            var ebillUser = await _context.EbillUsers
                .FirstOrDefaultAsync(u => u.Email == user.Email);

            if (ebillUser == null)
            {
                StatusMessage = "Your profile is not linked to an Staff record.";
                StatusMessageClass = "danger";
                return RedirectToPage();
            }

            try
            {
                var verificationTypeEnum = (Models.Enums.VerificationType)Enum.Parse(
                    typeof(Models.Enums.VerificationType), verificationType);

                // Use optimized bulk verification - single transaction, minimal DB round trips
                var result = await _verificationService.BulkVerifyCallLogsAsync(
                    selectedIds,
                    ebillUser.IndexNumber,
                    verificationTypeEnum,
                    justification: $"Quick verified as {verificationType}");

                StatusMessage = $"Successfully marked {result.VerifiedCount} of {selectedIds.Count} call(s) as {verificationType}.";

                var skippedTotal = result.SkippedCount + result.LockedCount + result.ExpiredCount + result.UnauthorizedCount;
                if (skippedTotal > 0)
                {
                    var skippedDetails = new List<string>();
                    if (result.LockedCount > 0) skippedDetails.Add($"{result.LockedCount} locked");
                    if (result.ExpiredCount > 0) skippedDetails.Add($"{result.ExpiredCount} expired");
                    StatusMessage += $" ({string.Join(", ", skippedDetails)})";
                }
                StatusMessageClass = "success";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error during verification: {ex.Message}";
                StatusMessageClass = "danger";
            }

            return RedirectToPage();
        }

        /// <summary>
        /// Verify ALL call records for a specific extension, month, and year.
        /// This verifies all records in the database, not just the ones visible on the current page.
        /// </summary>
        public async Task<IActionResult> OnPostVerifyAllByExtensionMonthAsync(
            string extension, int month, int year, string verificationType)
        {
            if (string.IsNullOrEmpty(extension) || month < 1 || month > 12 || year < 2000)
            {
                StatusMessage = "Invalid parameters for verification.";
                StatusMessageClass = "danger";
                return RedirectToPage();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Challenge();

            var ebillUser = await _context.EbillUsers
                .FirstOrDefaultAsync(u => u.Email == user.Email);

            if (ebillUser == null)
            {
                StatusMessage = "Your profile is not linked to an Staff record.";
                StatusMessageClass = "danger";
                return RedirectToPage();
            }

            try
            {
                var verificationTypeEnum = (Models.Enums.VerificationType)Enum.Parse(
                    typeof(Models.Enums.VerificationType), verificationType);

                // ULTRA-FAST: Use raw SQL to verify all records in milliseconds
                var result = await _verificationService.BulkVerifyByExtensionMonthRawAsync(
                    extension,
                    month,
                    year,
                    ebillUser.IndexNumber,
                    verificationTypeEnum,
                    justification: $"Bulk verified as {verificationType}");

                var monthName = new DateTime(year, month, 1).ToString("MMMM");

                if (result.VerifiedCount > 0)
                {
                    StatusMessage = $"Successfully verified {result.VerifiedCount} call(s) for extension {extension} ({monthName} {year}) as {verificationType}.";
                    StatusMessageClass = "success";
                }
                else
                {
                    StatusMessage = "No verifiable call records found for this extension and month.";
                    StatusMessageClass = "warning";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error during verification: {ex.Message}";
                StatusMessageClass = "danger";
            }

            return RedirectToPage();
        }

        /// <summary>
        /// Verify ALL call records for a specific dialed number within extension/month/year.
        /// </summary>
        public async Task<IActionResult> OnPostVerifyAllByDialedNumberAsync(
            string extension, int month, int year, string dialedNumber, string verificationType)
        {
            if (string.IsNullOrEmpty(extension) || string.IsNullOrEmpty(dialedNumber) ||
                month < 1 || month > 12 || year < 2000)
            {
                StatusMessage = "Invalid parameters for verification.";
                StatusMessageClass = "danger";
                return RedirectToPage();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Challenge();

            var ebillUser = await _context.EbillUsers
                .FirstOrDefaultAsync(u => u.Email == user.Email);

            if (ebillUser == null)
            {
                StatusMessage = "Your profile is not linked to an Staff record.";
                StatusMessageClass = "danger";
                return RedirectToPage();
            }

            try
            {
                var verificationTypeEnum = (Models.Enums.VerificationType)Enum.Parse(
                    typeof(Models.Enums.VerificationType), verificationType);

                // ULTRA-FAST: Use raw SQL to verify all records in milliseconds
                var result = await _verificationService.BulkVerifyByDialedNumberRawAsync(
                    extension,
                    month,
                    year,
                    dialedNumber,
                    ebillUser.IndexNumber,
                    verificationTypeEnum,
                    justification: $"Bulk verified as {verificationType}");

                var monthName = new DateTime(year, month, 1).ToString("MMMM");

                if (result.VerifiedCount > 0)
                {
                    StatusMessage = $"Successfully verified {result.VerifiedCount} call(s) to {dialedNumber} ({monthName} {year}) as {verificationType}.";
                    StatusMessageClass = "success";
                }
                else
                {
                    StatusMessage = "No verifiable call records found for this dialed number.";
                    StatusMessageClass = "warning";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error during verification: {ex.Message}";
                StatusMessageClass = "danger";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostBatchVerifyAsync(List<int> selectedIds, string verificationType)
        {
            if (selectedIds == null || !selectedIds.Any())
            {
                StatusMessage = "Please select at least one call to verify.";
                StatusMessageClass = "warning";
                return RedirectToPage();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Challenge();

            var ebillUser = await _context.EbillUsers
                .FirstOrDefaultAsync(u => u.Email == user.Email);

            if (ebillUser == null)
            {
                StatusMessage = "Your profile is not linked to an Staff record.";
                StatusMessageClass = "danger";
                return RedirectToPage();
            }

            try
            {
                var verificationTypeEnum = (Models.Enums.VerificationType)Enum.Parse(
                    typeof(Models.Enums.VerificationType), verificationType);

                // Use optimized bulk verification - single transaction, minimal DB round trips
                var result = await _verificationService.BulkVerifyCallLogsAsync(
                    selectedIds,
                    ebillUser.IndexNumber,
                    verificationTypeEnum);

                StatusMessage = $"Successfully verified {result.VerifiedCount} of {selectedIds.Count} calls.";

                var skippedTotal = result.SkippedCount + result.LockedCount + result.ExpiredCount + result.UnauthorizedCount;
                if (skippedTotal > 0)
                {
                    var skippedDetails = new List<string>();
                    if (result.LockedCount > 0) skippedDetails.Add($"{result.LockedCount} locked");
                    if (result.ExpiredCount > 0) skippedDetails.Add($"{result.ExpiredCount} expired");
                    if (result.UnauthorizedCount > 0) skippedDetails.Add($"{result.UnauthorizedCount} unauthorized");
                    StatusMessage += $" ({string.Join(", ", skippedDetails)})";
                }
                StatusMessageClass = "success";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error during batch verification: {ex.Message}";
                StatusMessageClass = "danger";
            }

            return RedirectToPage();
        }

        public async Task<JsonResult> OnPostAcceptAssignmentAsync(int callRecordId)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                    return new JsonResult(new { success = false, message = "User not authenticated" });

                var ebillUser = await _context.EbillUsers
                    .FirstOrDefaultAsync(u => u.Email == user.Email);

                if (ebillUser == null)
                    return new JsonResult(new { success = false, message = "User profile not found" });

                // Look up the assignment by CallRecordId and AssignedTo
                var assignment = await _context.Set<CallLogPaymentAssignment>()
                    .FirstOrDefaultAsync(a => a.CallRecordId == callRecordId &&
                                            a.AssignedTo == ebillUser.IndexNumber &&
                                            a.AssignmentStatus == "Pending");

                if (assignment == null)
                    return new JsonResult(new { success = false, message = $"No pending assignment found for call record {callRecordId} assigned to you" });

                var success = await _verificationService.AcceptPaymentAssignmentAsync(assignment.Id, ebillUser.IndexNumber);

                if (success)
                {
                    return new JsonResult(new { success = true, message = "Assignment accepted successfully" });
                }
                else
                {
                    return new JsonResult(new { success = false, message = "Service returned false - check server logs for details" });
                }
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        public async Task<JsonResult> OnPostRejectAssignmentAsync([FromBody] RejectAssignmentRequest request)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                    return new JsonResult(new { success = false, message = "User not authenticated" });

                var ebillUser = await _context.EbillUsers
                    .FirstOrDefaultAsync(u => u.Email == user.Email);

                if (ebillUser == null)
                    return new JsonResult(new { success = false, message = "User profile not found" });

                if (string.IsNullOrWhiteSpace(request.Reason))
                    return new JsonResult(new { success = false, message = "Rejection reason is required" });

                // Look up the assignment by CallRecordId and AssignedTo
                var assignment = await _context.Set<CallLogPaymentAssignment>()
                    .FirstOrDefaultAsync(a => a.CallRecordId == request.CallRecordId &&
                                            a.AssignedTo == ebillUser.IndexNumber &&
                                            a.AssignmentStatus == "Pending");

                if (assignment == null)
                    return new JsonResult(new { success = false, message = $"No pending assignment found for call record {request.CallRecordId} assigned to you" });

                var success = await _verificationService.RejectPaymentAssignmentAsync(
                    assignment.Id,
                    ebillUser.IndexNumber,
                    request.Reason);

                if (success)
                {
                    return new JsonResult(new { success = true, message = "Assignment rejected successfully" });
                }
                else
                {
                    return new JsonResult(new { success = false, message = "Service returned false - check server logs for details" });
                }
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        /// <summary>
        /// Bulk accept all pending assignments for a specific extension/month/year or dialed number
        /// </summary>
        public async Task<JsonResult> OnPostAcceptAssignmentBulkAsync([FromBody] BulkAssignmentRequest request)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                    return new JsonResult(new { success = false, message = "User not authenticated" });

                var ebillUser = await _context.EbillUsers
                    .FirstOrDefaultAsync(u => u.Email == user.Email);

                if (ebillUser == null)
                    return new JsonResult(new { success = false, message = "User profile not found" });

                // Map "Subscription" to empty string for the service
                var dialedNumber = request.DialedNumber == "Subscription" ? "" : request.DialedNumber;

                // Use optimized bulk accept with raw SQL
                var result = await _verificationService.BulkAcceptAssignmentsAsync(
                    ebillUser.IndexNumber,
                    assignedFrom: null,
                    extension: request.Extension,
                    month: request.Month > 0 ? request.Month : null,
                    year: request.Year > 0 ? request.Year : null,
                    dialedNumber: dialedNumber);

                if (result.ProcessedCount == 0 && result.SkippedCount == 0)
                    return new JsonResult(new { success = false, message = "No pending assignments found" });

                return new JsonResult(new {
                    success = result.Success,
                    message = $"Accepted {result.ProcessedCount} assignment(s)" +
                              (result.SkippedCount > 0 ? $", {result.SkippedCount} skipped" : "") +
                              (result.Errors.Any() ? $" - {string.Join(", ", result.Errors)}" : ""),
                    acceptedCount = result.ProcessedCount,
                    skippedCount = result.SkippedCount
                });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        /// <summary>
        /// Bulk reject all pending assignments for a specific extension/month/year or dialed number
        /// </summary>
        public async Task<JsonResult> OnPostRejectAssignmentBulkAsync([FromBody] BulkRejectAssignmentRequest request)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                    return new JsonResult(new { success = false, message = "User not authenticated" });

                var ebillUser = await _context.EbillUsers
                    .FirstOrDefaultAsync(u => u.Email == user.Email);

                if (ebillUser == null)
                    return new JsonResult(new { success = false, message = "User profile not found" });

                if (string.IsNullOrWhiteSpace(request.Reason))
                    return new JsonResult(new { success = false, message = "Rejection reason is required" });

                // Map "Subscription" to empty string for the service
                var dialedNumber = request.DialedNumber == "Subscription" ? "" : request.DialedNumber;

                // Use optimized bulk reject with raw SQL
                var result = await _verificationService.BulkRejectAssignmentsAsync(
                    ebillUser.IndexNumber,
                    request.Reason,
                    assignedFrom: null,
                    extension: request.Extension,
                    month: request.Month > 0 ? request.Month : null,
                    year: request.Year > 0 ? request.Year : null,
                    dialedNumber: dialedNumber);

                if (result.ProcessedCount == 0 && result.SkippedCount == 0)
                    return new JsonResult(new { success = false, message = "No pending assignments found" });

                return new JsonResult(new {
                    success = result.Success,
                    message = $"Rejected {result.ProcessedCount} assignment(s)" +
                              (result.SkippedCount > 0 ? $", {result.SkippedCount} skipped" : "") +
                              (result.Errors.Any() ? $" - {string.Join(", ", result.Errors)}" : ""),
                    rejectedCount = result.ProcessedCount,
                    skippedCount = result.SkippedCount
                });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        /// <summary>
        /// Recall a single pending reassignment (cancel the reassignment and take the call back)
        /// </summary>
        public async Task<JsonResult> OnPostRecallAssignmentAsync(int callRecordId)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                    return new JsonResult(new { success = false, message = "User not authenticated" });

                var ebillUser = await _context.EbillUsers
                    .FirstOrDefaultAsync(u => u.Email == user.Email);

                if (ebillUser == null)
                    return new JsonResult(new { success = false, message = "User profile not found" });

                // Find the pending assignment where current user is the one who assigned it
                var assignment = await _context.Set<CallLogPaymentAssignment>()
                    .FirstOrDefaultAsync(a => a.CallRecordId == callRecordId &&
                                            a.AssignedFrom == ebillUser.IndexNumber &&
                                            a.AssignmentStatus == "Pending");

                if (assignment == null)
                    return new JsonResult(new { success = false, message = "No pending reassignment found for this call" });

                // Update call record to revert to original owner
                var callRecord = await _context.CallRecords.FindAsync(callRecordId);
                if (callRecord != null)
                {
                    callRecord.PayingIndexNumber = ebillUser.IndexNumber;
                    callRecord.PaymentAssignmentId = null;
                    callRecord.AssignmentStatus = "None";
                }

                // Mark assignment as recalled
                assignment.AssignmentStatus = "Recalled";
                assignment.ModifiedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return new JsonResult(new { success = true, message = "Reassignment recalled successfully" });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        /// <summary>
        /// Bulk recall all pending reassignments for a specific extension/month/year or dialed number
        /// </summary>
        public async Task<JsonResult> OnPostRecallAssignmentBulkAsync([FromBody] BulkAssignmentRequest request)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                    return new JsonResult(new { success = false, message = "User not authenticated" });

                var ebillUser = await _context.EbillUsers
                    .FirstOrDefaultAsync(u => u.Email == user.Email);

                if (ebillUser == null)
                    return new JsonResult(new { success = false, message = "User profile not found" });

                // Map "Subscription" to empty string
                var dialedNumber = request.DialedNumber == "Subscription" ? "" : request.DialedNumber;

                // Use raw SQL for fast bulk update
                var result = await BulkRecallAssignmentsRawAsync(
                    ebillUser.IndexNumber,
                    extension: request.Extension,
                    month: request.Month > 0 ? request.Month : null,
                    year: request.Year > 0 ? request.Year : null,
                    dialedNumber: dialedNumber);

                if (result.RecalledCount == 0)
                    return new JsonResult(new { success = false, message = "No pending reassignments found to recall" });

                return new JsonResult(new {
                    success = true,
                    message = $"Recalled {result.RecalledCount} reassignment(s)",
                    recalledCount = result.RecalledCount
                });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        /// <summary>
        /// Bulk recall using stored procedure sp_BulkRecallAssignments
        /// </summary>
        private async Task<(int RecalledCount, List<string> Errors)> BulkRecallAssignmentsRawAsync(
            string indexNumber,
            string? extension = null,
            int? month = null,
            int? year = null,
            string? dialedNumber = null)
        {
            var errors = new List<string>();
            int recalledCount = 0;

            try
            {
                var conn = _context.Database.GetDbConnection();
                var wasOpen = conn.State == ConnectionState.Open;
                if (!wasOpen) await conn.OpenAsync();

                try
                {
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = "ebill.sp_BulkRecallAssignments";
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandTimeout = 30;

                    cmd.Parameters.Add(new SqlParameter("@IndexNumber", indexNumber));
                    cmd.Parameters.Add(new SqlParameter("@Extension", (object?)extension ?? DBNull.Value));
                    cmd.Parameters.Add(new SqlParameter("@Month", (object?)month ?? DBNull.Value));
                    cmd.Parameters.Add(new SqlParameter("@Year", (object?)year ?? DBNull.Value));
                    cmd.Parameters.Add(new SqlParameter("@DialedNumber", (object?)dialedNumber ?? DBNull.Value));

                    using var reader = await cmd.ExecuteReaderAsync();
                    if (await reader.ReadAsync())
                        recalledCount = reader.GetInt32(0);
                }
                finally
                {
                    if (!wasOpen) await conn.CloseAsync();
                }

                return (recalledCount, errors);
            }
            catch (Exception ex)
            {
                errors.Add(ex.Message);
                return (0, errors);
            }
        }

        // Search users for reassignment
        public async Task<JsonResult> OnGetSearchUsersAsync(string searchTerm)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                    return new JsonResult(new { users = new List<object>(), error = "User not authenticated" });

                var ebillUser = await _context.EbillUsers
                    .FirstOrDefaultAsync(u => u.Email == user.Email);

                if (ebillUser == null)
                    return new JsonResult(new { users = new List<object>(), error = "User profile not found" });

                // First, check if the search matches the current user
                var isSearchingForSelf = false;
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    var lowerTerm = searchTerm.ToLower();
                    if (ebillUser.Email?.ToLower().Contains(lowerTerm) == true ||
                        ebillUser.FirstName.ToLower().Contains(lowerTerm) ||
                        ebillUser.LastName.ToLower().Contains(lowerTerm) ||
                        ebillUser.IndexNumber.ToLower().Contains(lowerTerm))
                    {
                        isSearchingForSelf = true;
                    }
                }

                var query = _context.EbillUsers
                    .Where(u => u.IsActive && u.IndexNumber != ebillUser.IndexNumber);

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    // Use EF.Functions.Like for better SQL Server compatibility
                    query = query.Where(u =>
                        EF.Functions.Like(u.FirstName, $"%{searchTerm}%") ||
                        EF.Functions.Like(u.LastName, $"%{searchTerm}%") ||
                        EF.Functions.Like(u.IndexNumber, $"%{searchTerm}%") ||
                        (u.Email != null && EF.Functions.Like(u.Email, $"%{searchTerm}%")));
                }

                var users = await query
                    .OrderBy(u => u.FirstName)
                    .ThenBy(u => u.LastName)
                    .Take(20)
                    .Select(u => new
                    {
                        indexNumber = u.IndexNumber,
                        firstName = u.FirstName,
                        lastName = u.LastName,
                        email = u.Email ?? ""
                    })
                    .ToListAsync();

                return new JsonResult(new {
                    users,
                    isSearchingForSelf,
                    currentUserName = $"{ebillUser.FirstName} {ebillUser.LastName}"
                });
            }
            catch (Exception ex)
            {
                return new JsonResult(new {
                    users = new List<object>(),
                    error = $"Search error: {ex.Message}"
                });
            }
        }

        // Reassign calls to another user
        public async Task<IActionResult> OnPostReassignCallsAsync(int callId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Challenge();

            var ebillUser = await _context.EbillUsers
                .FirstOrDefaultAsync(u => u.Email == user.Email);

            if (ebillUser == null)
            {
                StatusMessage = "Your profile is not linked to an Staff record.";
                StatusMessageClass = "danger";
                return RedirectToPage();
            }

            try
            {
                var dialedNumber = Request.Form["dialedNumber"].ToString();
                var assignToIndexNumber = Request.Form["assignToIndexNumber"].ToString();
                var assignmentReason = Request.Form["assignmentReason"].ToString();
                var reassignLevel = Request.Form["reassignLevel"].ToString();
                var reassignExtension = Request.Form["reassignExtension"].ToString();
                var reassignMonthStr = Request.Form["reassignMonth"].ToString();
                var reassignYearStr = Request.Form["reassignYear"].ToString();

                if (string.IsNullOrWhiteSpace(assignToIndexNumber))
                {
                    StatusMessage = "Please search for and select a user to reassign the calls to.";
                    StatusMessageClass = "warning";
                    return RedirectToPage();
                }

                if (string.IsNullOrWhiteSpace(assignmentReason))
                {
                    StatusMessage = "Please provide a reason for the reassignment.";
                    StatusMessageClass = "warning";
                    return RedirectToPage();
                }

                int successCount = 0;
                string levelDescription;

                if (reassignLevel == "extension")
                {
                    // ULTRA-FAST: Extension-level bulk reassignment using raw SQL
                    if (string.IsNullOrWhiteSpace(reassignExtension) ||
                        !int.TryParse(reassignMonthStr, out int month) ||
                        !int.TryParse(reassignYearStr, out int year))
                    {
                        StatusMessage = "Invalid extension reassignment parameters.";
                        StatusMessageClass = "danger";
                        return RedirectToPage();
                    }

                    var result = await _verificationService.BulkReassignByExtensionMonthRawAsync(
                        reassignExtension,
                        month,
                        year,
                        ebillUser.IndexNumber,
                        assignToIndexNumber,
                        assignmentReason);

                    if (result.Errors.Any())
                    {
                        StatusMessage = $"Error during reassignment: {string.Join(", ", result.Errors)}";
                        StatusMessageClass = "danger";
                        return RedirectToPage();
                    }

                    successCount = result.ReassignedCount;
                    levelDescription = $"extension {reassignExtension}";
                }
                else if (reassignLevel == "dialed")
                {
                    // ULTRA-FAST: Dialed-number-level bulk reassignment using raw SQL
                    if (string.IsNullOrWhiteSpace(dialedNumber) ||
                        string.IsNullOrWhiteSpace(reassignExtension) ||
                        !int.TryParse(reassignMonthStr, out int month) ||
                        !int.TryParse(reassignYearStr, out int year))
                    {
                        StatusMessage = "Invalid dialed number reassignment parameters.";
                        StatusMessageClass = "danger";
                        return RedirectToPage();
                    }

                    var result = await _verificationService.BulkReassignByDialedNumberRawAsync(
                        reassignExtension,
                        month,
                        year,
                        dialedNumber,
                        ebillUser.IndexNumber,
                        assignToIndexNumber,
                        assignmentReason);

                    if (result.Errors.Any())
                    {
                        StatusMessage = $"Error during reassignment: {string.Join(", ", result.Errors)}";
                        StatusMessageClass = "danger";
                        return RedirectToPage();
                    }

                    successCount = result.ReassignedCount;
                    levelDescription = $"dialed number {dialedNumber}";
                }
                else
                {
                    // Single call reassignment - still uses the individual method
                    if (string.IsNullOrWhiteSpace(dialedNumber))
                    {
                        StatusMessage = "Please provide the dialed number for reassignment.";
                        StatusMessageClass = "warning";
                        return RedirectToPage();
                    }

                    // Get the call record first to know the month/year
                    var firstCall = await _context.CallRecords.FindAsync(callId);

                    if (firstCall == null)
                    {
                        StatusMessage = "Call record not found.";
                        StatusMessageClass = "danger";
                        return RedirectToPage();
                    }

                    // Check if the call has been submitted to supervisor
                    var existingVerification = await _context.CallLogVerifications
                        .FirstOrDefaultAsync(v => v.CallRecordId == callId && v.SubmittedToSupervisor);

                    if (existingVerification != null)
                    {
                        StatusMessage = "This call has already been submitted to supervisor and cannot be reassigned. Please wait for supervisor action or contact your supervisor to revert it first.";
                        StatusMessageClass = "warning";
                        return RedirectToPage();
                    }

                    // For single call, use bulk method for the dialed number (reassigns all calls to same number)
                    var result = await _verificationService.BulkReassignByDialedNumberRawAsync(
                        firstCall.ExtensionNumber,
                        firstCall.CallMonth,
                        firstCall.CallYear,
                        dialedNumber,
                        ebillUser.IndexNumber,
                        assignToIndexNumber,
                        assignmentReason);

                    if (result.Errors.Any())
                    {
                        StatusMessage = $"Error during reassignment: {string.Join(", ", result.Errors)}";
                        StatusMessageClass = "danger";
                        return RedirectToPage();
                    }

                    successCount = result.ReassignedCount;
                    levelDescription = $"dialed number {dialedNumber}";
                }

                if (successCount == 0)
                {
                    StatusMessage = "No eligible calls found for reassignment. Calls that have been submitted to supervisor cannot be reassigned.";
                    StatusMessageClass = "warning";
                    return RedirectToPage();
                }

                StatusMessage = $"Successfully reassigned {successCount} call(s) from {levelDescription} to the selected user!";
                StatusMessageClass = "success";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error reassigning calls: {ex.Message}";
                StatusMessageClass = "danger";
                return RedirectToPage();
            }
        }

        /// <summary>
        /// AJAX endpoint to get dialed numbers for a specific extension/month/year (Level 2)
        /// Uses stored procedure sp_GetDialedNumberGroups for single-round-trip performance.
        /// </summary>
        public async Task<JsonResult> OnGetDialedNumbersAsync(
            string? extension, int month, int year, int page = 1, int pageSize = 20)
        {
            try
            {
                if (string.IsNullOrEmpty(extension))
                    return new JsonResult(new { error = "Extension is required" });

                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                    return new JsonResult(new { error = "User not authenticated" });

                var ebillUser = await _context.EbillUsers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Email == user.Email);

                bool isAdmin = User.IsInRole("Admin");
                string? userIndexNumber = ebillUser?.IndexNumber;

                if (ebillUser == null && !isAdmin)
                    return new JsonResult(new { error = "User profile not found" });

                var conn = _context.Database.GetDbConnection();
                var wasOpen = conn.State == ConnectionState.Open;
                if (!wasOpen) await conn.OpenAsync();

                var dialedNumbers = new List<DialedNumberGroupDto>();
                int totalCount = 0;

                try
                {
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = "ebill.sp_GetDialedNumberGroups";
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandTimeout = 30;

                    var p = cmd.Parameters;
                    p.Add(new SqlParameter("@UserIndexNumber", (object?)userIndexNumber ?? DBNull.Value));
                    p.Add(new SqlParameter("@IsAdmin", isAdmin));
                    p.Add(new SqlParameter("@Extension", extension));
                    p.Add(new SqlParameter("@Month", month));
                    p.Add(new SqlParameter("@Year", year));
                    p.Add(new SqlParameter("@Page", page));
                    p.Add(new SqlParameter("@PageSize", pageSize));

                    using var reader = await cmd.ExecuteReaderAsync();

                    // Result set 1: Total count
                    if (await reader.ReadAsync())
                        totalCount = reader.GetInt32(0);

                    // Result set 2: Dialed number groups
                    await reader.NextResultAsync();
                    while (await reader.ReadAsync())
                    {
                        dialedNumbers.Add(new DialedNumberGroupDto
                        {
                            DialedNumber = reader.GetString(reader.GetOrdinal("DialedNumber")),
                            Destination = reader.GetString(reader.GetOrdinal("Destination")),
                            CallCount = reader.GetInt32(reader.GetOrdinal("CallCount")),
                            TotalCostUSD = reader.GetDecimal(reader.GetOrdinal("TotalCostUSD")),
                            TotalCostKSH = reader.GetDecimal(reader.GetOrdinal("TotalCostKSH")),
                            TotalDuration = reader.GetDecimal(reader.GetOrdinal("TotalDuration")),
                            AssignmentStatus = reader.GetString(reader.GetOrdinal("AssignmentStatus")),
                            IsDataSession = reader.GetBoolean(reader.GetOrdinal("IsDataSession")),
                            SubmittedCount = reader.GetInt32(reader.GetOrdinal("SubmittedCount")),
                            PendingApprovalCount = reader.GetInt32(reader.GetOrdinal("PendingApprovalCount")),
                            ApprovedCount = reader.GetInt32(reader.GetOrdinal("ApprovedCount")),
                            RejectedCount = reader.GetInt32(reader.GetOrdinal("RejectedCount")),
                            RevertedCount = reader.GetInt32(reader.GetOrdinal("RevertedCount")),
                            PartiallyApprovedCount = reader.GetInt32(reader.GetOrdinal("PartiallyApprovedCount")),
                            IncomingAssignmentCount = reader.GetInt32(reader.GetOrdinal("IncomingAssignmentCount")),
                            AssignedFromUser = reader.IsDBNull(reader.GetOrdinal("AssignedFromUser")) ? null : reader.GetString(reader.GetOrdinal("AssignedFromUser")),
                            OutgoingPendingCount = reader.GetInt32(reader.GetOrdinal("OutgoingPendingCount")),
                            AssignedToUser = reader.IsDBNull(reader.GetOrdinal("AssignedToUser")) ? null : reader.GetString(reader.GetOrdinal("AssignedToUser"))
                        });
                    }
                }
                finally
                {
                    if (!wasOpen) await conn.CloseAsync();
                }

                var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

                // Set DialedGroupId for each
                var safeExtension = (extension ?? "Unknown").Replace(" ", "_").Replace("-", "_").Replace("+", "");
                var groupId = $"{safeExtension}_{month}_{year}";
                foreach (var dn in dialedNumbers)
                {
                    var safeDialedNumber = (dn.DialedNumber ?? "Subscription").Replace("+", "").Replace(" ", "_").Replace("-", "_");
                    dn.DialedGroupId = $"{groupId}_dialed_{safeDialedNumber}";
                }

                return new JsonResult(new DialedNumbersResponse
                {
                    DialedNumbers = dialedNumbers,
                    TotalCount = totalCount,
                    TotalPages = totalPages,
                    CurrentPage = page,
                    PageSize = pageSize,
                    Extension = extension,
                    Month = month,
                    Year = year,
                    GroupId = groupId
                });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { error = $"Error loading dialed numbers: {ex.Message}" });
            }
        }

        /// <summary>
        /// AJAX endpoint to get call logs for a specific dialed number within extension/month/year (Level 3)
        /// Uses stored procedure sp_GetCallLogs for single-round-trip performance.
        /// </summary>
        public async Task<JsonResult> OnGetCallLogsAsync(
            string? extension, int month, int year, string? dialedNumber,
            int callLogPage = 1, int callLogPageSize = 10,
            string sortBy = "CallDate", bool sortDesc = true)
        {
            try
            {
                if (string.IsNullOrEmpty(extension))
                    return new JsonResult(new { error = "Extension is required" });

                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                    return new JsonResult(new { error = "User not authenticated" });

                var ebillUser = await _context.EbillUsers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Email == user.Email);

                bool isAdmin = User.IsInRole("Admin");
                string? userIndexNumber = ebillUser?.IndexNumber;

                if (ebillUser == null && !isAdmin)
                    return new JsonResult(new { error = "User profile not found" });

                // Map sort parameter to SP expected values
                var spSortBy = sortBy?.ToLower() switch
                {
                    "duration" => "Duration",
                    "costksh" => "CostKSH",
                    "costusd" => "CostUSD",
                    "type" => "Type",
                    "status" => "Status",
                    _ => "CallDate"
                };

                var conn = _context.Database.GetDbConnection();
                var wasOpen = conn.State == ConnectionState.Open;
                if (!wasOpen) await conn.OpenAsync();

                var callLogs = new List<CallLogItemDto>();
                int totalCount = 0;

                try
                {
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = "ebill.sp_GetCallLogs";
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandTimeout = 30;

                    var p = cmd.Parameters;
                    p.Add(new SqlParameter("@UserIndexNumber", (object?)userIndexNumber ?? DBNull.Value));
                    p.Add(new SqlParameter("@IsAdmin", isAdmin));
                    p.Add(new SqlParameter("@Extension", extension));
                    p.Add(new SqlParameter("@Month", month));
                    p.Add(new SqlParameter("@Year", year));
                    p.Add(new SqlParameter("@DialedNumber", (object?)dialedNumber ?? DBNull.Value));
                    p.Add(new SqlParameter("@Page", callLogPage));
                    p.Add(new SqlParameter("@PageSize", callLogPageSize));
                    p.Add(new SqlParameter("@SortBy", spSortBy));
                    p.Add(new SqlParameter("@SortDesc", sortDesc));

                    using var reader = await cmd.ExecuteReaderAsync();

                    // Result set 1: Total count
                    if (await reader.ReadAsync())
                        totalCount = reader.GetInt32(0);

                    // Result set 2: Call log items
                    await reader.NextResultAsync();
                    while (await reader.ReadAsync())
                    {
                        callLogs.Add(new CallLogItemDto
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("Id")),
                            DialedNumber = reader.GetString(reader.GetOrdinal("DialedNumber")),
                            CallDate = reader.GetDateTime(reader.GetOrdinal("CallDate")),
                            CallEndTime = reader.GetDateTime(reader.GetOrdinal("CallEndTime")),
                            CallDuration = reader.GetInt32(reader.GetOrdinal("CallDuration")),
                            CallCostUSD = reader.GetDecimal(reader.GetOrdinal("CallCostUSD")),
                            CallCostKSH = reader.GetDecimal(reader.GetOrdinal("CallCostKSH")),
                            Destination = reader.GetString(reader.GetOrdinal("Destination")),
                            CallType = reader.GetString(reader.GetOrdinal("CallType")),
                            VerificationType = reader.GetString(reader.GetOrdinal("VerificationType")),
                            IsVerified = reader.GetBoolean(reader.GetOrdinal("IsVerified")),
                            SupervisorApprovalStatus = reader.IsDBNull(reader.GetOrdinal("SupervisorApprovalStatus")) ? null : reader.GetString(reader.GetOrdinal("SupervisorApprovalStatus")),
                            IsSubmittedToSupervisor = reader.GetBoolean(reader.GetOrdinal("IsSubmittedToSupervisor")),
                            AssignmentStatus = reader.GetString(reader.GetOrdinal("AssignmentStatus")),
                            AssignedFrom = reader.IsDBNull(reader.GetOrdinal("AssignedFrom")) ? null : reader.GetString(reader.GetOrdinal("AssignedFrom")),
                            AssignedTo = reader.IsDBNull(reader.GetOrdinal("AssignedTo")) ? null : reader.GetString(reader.GetOrdinal("AssignedTo")),
                            IsLocked = reader.GetBoolean(reader.GetOrdinal("IsLocked"))
                        });
                    }
                }
                finally
                {
                    if (!wasOpen) await conn.CloseAsync();
                }

                var totalPages = (int)Math.Ceiling(totalCount / (double)callLogPageSize);

                var safeExtension = (extension ?? "Unknown").Replace(" ", "_").Replace("-", "_").Replace("+", "");
                var groupId = $"{safeExtension}_{month}_{year}";
                var safeDialedNumber = (string.IsNullOrEmpty(dialedNumber) ? "Subscription" : dialedNumber).Replace("+", "").Replace(" ", "_").Replace("-", "_");
                var dialedGroupId = $"{groupId}_dialed_{safeDialedNumber}";

                return new JsonResult(new CallLogsResponse
                {
                    CallLogs = callLogs,
                    TotalCount = totalCount,
                    TotalPages = totalPages,
                    CurrentPage = callLogPage,
                    PageSize = callLogPageSize,
                    DialedNumber = dialedNumber,
                    DialedGroupId = dialedGroupId,
                    SortBy = sortBy,
                    SortDesc = sortDesc
                });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { error = $"Error loading call logs: {ex.Message}" });
            }
        }

        /// <summary>
        /// AJAX endpoint to get all call IDs for a specific extension/month/year
        /// Uses stored procedure sp_GetCallIdsForExtension for single-round-trip performance.
        /// </summary>
        public async Task<JsonResult> OnGetCallIdsForExtensionAsync(string? extension, int month, int year)
        {
            try
            {
                if (string.IsNullOrEmpty(extension))
                    return new JsonResult(new { error = "Extension is required" });

                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                    return new JsonResult(new { error = "Unauthorized" });

                var ebillUser = await _context.EbillUsers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Email == user.Email);

                if (ebillUser == null)
                    return new JsonResult(new { error = "User not found" });

                var conn = _context.Database.GetDbConnection();
                var wasOpen = conn.State == ConnectionState.Open;
                if (!wasOpen) await conn.OpenAsync();

                var callIds = new List<int>();
                int skippedCount = 0;

                try
                {
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = "ebill.sp_GetCallIdsForExtension";
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandTimeout = 30;

                    cmd.Parameters.Add(new SqlParameter("@UserIndexNumber", ebillUser.IndexNumber));
                    cmd.Parameters.Add(new SqlParameter("@Extension", extension));
                    cmd.Parameters.Add(new SqlParameter("@Month", month));
                    cmd.Parameters.Add(new SqlParameter("@Year", year));

                    using var reader = await cmd.ExecuteReaderAsync();

                    // Result set 1: Call IDs
                    while (await reader.ReadAsync())
                        callIds.Add(reader.GetInt32(0));

                    // Result set 2: Skipped count
                    await reader.NextResultAsync();
                    if (await reader.ReadAsync())
                        skippedCount = reader.GetInt32(0);
                }
                finally
                {
                    if (!wasOpen) await conn.CloseAsync();
                }

                return new JsonResult(new { callIds, skippedCount });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { error = $"Error fetching call IDs: {ex.Message}", callIds = new List<int>() });
            }
        }

        /// <summary>
        /// AJAX endpoint to get all call IDs for a specific dialed number within extension/month/year
        /// Uses stored procedure sp_GetCallIdsForDialedNumber for single-round-trip performance.
        /// </summary>
        public async Task<JsonResult> OnGetCallIdsForDialedNumberAsync(string? extension, int month, int year, string? dialedNumber)
        {
            try
            {
                if (string.IsNullOrEmpty(extension))
                    return new JsonResult(new { error = "Extension is required", callIds = new List<int>() });

                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                    return new JsonResult(new { error = "Unauthorized", callIds = new List<int>() });

                var ebillUser = await _context.EbillUsers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Email == user.Email);

                if (ebillUser == null)
                    return new JsonResult(new { error = "User not found", callIds = new List<int>() });

                var conn = _context.Database.GetDbConnection();
                var wasOpen = conn.State == ConnectionState.Open;
                if (!wasOpen) await conn.OpenAsync();

                var callIds = new List<int>();
                int skippedCount = 0;

                try
                {
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = "ebill.sp_GetCallIdsForDialedNumber";
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandTimeout = 30;

                    cmd.Parameters.Add(new SqlParameter("@UserIndexNumber", ebillUser.IndexNumber));
                    cmd.Parameters.Add(new SqlParameter("@Extension", extension));
                    cmd.Parameters.Add(new SqlParameter("@Month", month));
                    cmd.Parameters.Add(new SqlParameter("@Year", year));
                    cmd.Parameters.Add(new SqlParameter("@DialedNumber", (object?)dialedNumber ?? DBNull.Value));

                    using var reader = await cmd.ExecuteReaderAsync();

                    // Result set 1: Call IDs
                    while (await reader.ReadAsync())
                        callIds.Add(reader.GetInt32(0));

                    // Result set 2: Skipped count
                    await reader.NextResultAsync();
                    if (await reader.ReadAsync())
                        skippedCount = reader.GetInt32(0);
                }
                finally
                {
                    if (!wasOpen) await conn.CloseAsync();
                }

                return new JsonResult(new { callIds, skippedCount });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { error = $"Error fetching call IDs: {ex.Message}", callIds = new List<int>() });
            }
        }

        /// <summary>
        /// AJAX endpoint to get submission preview data for the Submit to Supervisor modal
        /// </summary>
        public async Task<JsonResult> OnPostSubmissionPreviewAsync([FromBody] SubmissionPreviewRequest? request)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                    return new JsonResult(new { success = false, message = "User not authenticated" });

                var currentUser = await _context.EbillUsers
                    .FirstOrDefaultAsync(u => u.Email == user.Email);

                if (currentUser == null)
                    return new JsonResult(new { success = false, message = "Your profile is not linked to an Staff record." });

                var userIndexNumber = currentUser.IndexNumber;

                // Check if supervisor is assigned
                if (string.IsNullOrEmpty(currentUser.SupervisorEmail))
                    return new JsonResult(new { success = false, message = "No supervisor assigned to your profile. Please contact ICT Service Desk to have a supervisor assigned." });

                // Parse call IDs
                var callIds = request?.CallIds;
                if (callIds == null || !callIds.Any())
                    return new JsonResult(new { success = false, message = "No calls selected for submission." });

                var idList = callIds;

                // Auto-verify unverified calls as "Official" before submission
                var unverifiedCalls = await _context.CallRecords
                    .Where(c => idList.Contains(c.Id) &&
                               (c.ResponsibleIndexNumber == userIndexNumber ||
                                (c.PayingIndexNumber == userIndexNumber && c.AssignmentStatus == "Accepted")) &&
                               !c.IsVerified &&
                               c.VerificationType != "Personal")
                    .ToListAsync();

                if (unverifiedCalls.Any())
                {
                    foreach (var call in unverifiedCalls)
                    {
                        call.IsVerified = true;
                        call.VerificationType = "Official";
                    }
                    await _context.SaveChangesAsync();
                }

                // Load ALL calls for summary display
                var allSelectedCalls = await _context.CallRecords
                    .Include(c => c.UserPhone)
                        .ThenInclude(up => up.ClassOfService)
                    .Where(c => idList.Contains(c.Id) &&
                               (c.ResponsibleIndexNumber == userIndexNumber ||
                                (c.PayingIndexNumber == userIndexNumber && c.AssignmentStatus == "Accepted")))
                    .OrderBy(c => c.CallDate)
                    .ToListAsync();

                if (!allSelectedCalls.Any())
                    return new JsonResult(new { success = false, message = "No calls found for submission." });

                // Filter to ONLY Official calls with actual cost for submission
                var callRecordsToSubmit = allSelectedCalls
                    .Where(c => c.VerificationType == "Official" && c.CallCostUSD > 0)
                    .ToList();

                // Count calls with zero cost that were filtered out
                var zeroCostCount = allSelectedCalls.Count(c => c.VerificationType == "Official" && c.CallCostUSD == 0);

                if (!callRecordsToSubmit.Any())
                {
                    if (zeroCostCount > 0)
                        return new JsonResult(new { success = false, message = $"No official calls with actual cost to submit. {zeroCostCount} call(s) have zero cost and are excluded." });
                    return new JsonResult(new { success = false, message = "No official calls selected for submission. All selected calls are marked as Personal." });
                }

                // Get month/year from first call
                var callMonth = callRecordsToSubmit.First().CallMonth;
                var callYear = callRecordsToSubmit.First().CallYear;

                // Determine if costs should display in USD or KSH based on currency code
                // PrivateWire uses USD, Safaricom/Airtel/PSTN use KSH
                var firstCall = callRecordsToSubmit.First();
                var isPrivateWire = firstCall.CallCurrencyCode?.ToUpper() == "USD";
                var serviceProvider = isPrivateWire ? "PrivateWire" : (firstCall.SourceSystem ?? "Local");

                // For display: Safaricom, Airtel, PSTN show KSH; PrivateWire shows USD
                var displayCurrency = isPrivateWire ? "USD" : "KSH";

                // Calculate summary in both currencies
                var totalCalls = callRecordsToSubmit.Count;
                var officialCostUSD = callRecordsToSubmit.Sum(c => c.CallCostUSD);
                var officialCostKSH = callRecordsToSubmit.Sum(c => c.CallCostKSHS);
                var personalCostUSD = allSelectedCalls.Where(c => c.VerificationType == "Personal" && c.CallCostUSD > 0).Sum(c => c.CallCostUSD);
                var personalCostKSH = allSelectedCalls.Where(c => c.VerificationType == "Personal" && c.CallCostUSD > 0).Sum(c => c.CallCostKSHS);

                // Get monthly allowance
                var allowanceNullable = await _calculationService.GetAllowanceLimitAsync(userIndexNumber);
                var monthlyAllowance = allowanceNullable ?? 0;

                // Calculate overage (based on USD for allowance comparison)
                var hasOverage = monthlyAllowance > 0 && officialCostUSD > monthlyAllowance;
                var overageAmountUSD = hasOverage ? officialCostUSD - monthlyAllowance : 0;

                // Calculate phone-level overages
                var phoneOverages = await CalculatePhoneLevelOveragesAsync(callRecordsToSubmit, callMonth, callYear);

                // Get supervisor info
                var supervisor = await _context.EbillUsers
                    .FirstOrDefaultAsync(u => u.Email == currentUser.SupervisorEmail);

                return new JsonResult(new
                {
                    success = true,
                    totalCalls,
                    officialCostUSD,
                    officialCostKSH,
                    personalCostUSD,
                    personalCostKSH,
                    monthlyAllowance,
                    hasOverage,
                    overageAmountUSD,
                    callMonth,
                    callYear,
                    monthName = new DateTime(callYear, callMonth, 1).ToString("MMMM yyyy"),
                    serviceProvider,
                    isPrivateWire,
                    displayCurrency,
                    phoneOverages,
                    hasAnyPhoneOverage = phoneOverages.Any(p => p.HasOverage),
                    supervisor = supervisor != null ? new
                    {
                        name = $"{supervisor.FirstName} {supervisor.LastName}",
                        email = supervisor.Email
                    } : new { name = currentUser.SupervisorName ?? currentUser.SupervisorEmail, email = currentUser.SupervisorEmail },
                    callRecordIds = callRecordsToSubmit.Select(c => c.Id).ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading submission preview");
                return new JsonResult(new { success = false, message = $"Error loading preview: {ex.Message}" });
            }
        }

        /// <summary>
        /// AJAX endpoint to submit calls to supervisor
        /// </summary>
        public async Task<JsonResult> OnPostSubmitToSupervisorAsync()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                    return new JsonResult(new { success = false, message = "User not authenticated" });

                var ebillUser = await _context.EbillUsers
                    .FirstOrDefaultAsync(u => u.Email == user.Email);

                if (ebillUser == null)
                    return new JsonResult(new { success = false, message = "Your profile is not linked to an Staff record." });

                // Parse form data
                var form = await Request.ReadFormAsync();
                var callRecordIdsRaw = form["callRecordIds"].ToList();
                var callRecordIds = callRecordIdsRaw.SelectMany(s => s.Split(',')).Select(int.Parse).Distinct().ToList();

                if (!callRecordIds.Any())
                    return new JsonResult(new { success = false, message = "No calls selected for submission." });

                // Load call records
                var callRecords = await _context.CallRecords
                    .Include(c => c.UserPhone)
                        .ThenInclude(up => up.ClassOfService)
                    .Where(c => callRecordIds.Contains(c.Id))
                    .ToListAsync();

                // Validate all calls are Official
                var personalCalls = callRecords.Where(c => c.VerificationType != "Official").ToList();
                if (personalCalls.Any())
                    return new JsonResult(new { success = false, message = "Personal calls cannot be submitted to supervisor. Only official calls can be submitted." });

                var officialCallsCost = callRecords.Sum(c => c.CallCostUSD);
                var allowanceNullable = await _calculationService.GetAllowanceLimitAsync(ebillUser.IndexNumber);
                var monthlyAllowance = allowanceNullable ?? 0;
                bool hasOverage = monthlyAllowance > 0 && officialCallsCost > monthlyAllowance;

                // Get or create verifications - batch lookup instead of per-call queries
                var verificationIds = new List<int>();
                var alreadySubmittedCalls = new List<int>();

                // Single query: get all existing verifications for these call IDs
                var existingVerifications = await _context.CallLogVerifications
                    .Where(v => callRecordIds.Contains(v.CallRecordId) && v.VerifiedBy == ebillUser.IndexNumber)
                    .ToListAsync();

                var existingByCallId = existingVerifications.ToDictionary(v => v.CallRecordId, v => v);

                var newVerifications = new List<CallLogVerification>();

                foreach (var callRecordId in callRecordIds)
                {
                    if (existingByCallId.TryGetValue(callRecordId, out var existingVerification))
                    {
                        if (existingVerification.SubmittedToSupervisor &&
                            (existingVerification.ApprovalStatus == "Pending" ||
                             existingVerification.ApprovalStatus == "Approved" ||
                             existingVerification.ApprovalStatus == "PartiallyApproved"))
                        {
                            alreadySubmittedCalls.Add(callRecordId);
                            continue;
                        }
                        verificationIds.Add(existingVerification.Id);
                    }
                    else
                    {
                        var callRecord = callRecords.First(c => c.Id == callRecordId);
                        VerificationType verificationType;
                        if (!Enum.TryParse(callRecord.VerificationType, out verificationType))
                            verificationType = VerificationType.Official;

                        var newVerification = new CallLogVerification
                        {
                            CallRecordId = callRecordId,
                            VerifiedBy = ebillUser.IndexNumber,
                            VerifiedDate = DateTime.UtcNow,
                            VerificationType = verificationType,
                            ActualAmount = callRecord.CallCostUSD,
                            JustificationText = string.Empty
                        };
                        newVerifications.Add(newVerification);
                    }
                }

                // Batch insert all new verifications in a single SaveChanges call
                if (newVerifications.Any())
                {
                    _context.CallLogVerifications.AddRange(newVerifications);
                    await _context.SaveChangesAsync();
                    verificationIds.AddRange(newVerifications.Select(v => v.Id));
                }

                if (alreadySubmittedCalls.Any() && !verificationIds.Any())
                    return new JsonResult(new { success = false, message = "All selected calls have already been submitted to supervisor and cannot be resubmitted." });

                // Process phone overage justifications from form
                var phoneJustifications = new List<PhoneOverageJustificationSubmitDto>();
                var phoneIndex = 0;
                while (form.ContainsKey($"phoneOverageJustifications[{phoneIndex}].UserPhoneId"))
                {
                    var userPhoneIdStr = form[$"phoneOverageJustifications[{phoneIndex}].UserPhoneId"].ToString();
                    var justification = form[$"phoneOverageJustifications[{phoneIndex}].Justification"].ToString();
                    var document = form.Files[$"phoneOverageJustifications[{phoneIndex}].Document"];

                    if (int.TryParse(userPhoneIdStr, out int userPhoneId) && !string.IsNullOrEmpty(justification))
                    {
                        phoneJustifications.Add(new PhoneOverageJustificationSubmitDto
                        {
                            UserPhoneId = userPhoneId,
                            Justification = justification,
                            Document = document
                        });
                    }
                    phoneIndex++;
                }

                // Save phone-level overage justifications
                if (phoneJustifications.Any())
                {
                    var callMonth = callRecords.First().CallMonth;
                    var callYear = callRecords.First().CallYear;
                    await SavePhoneOverageJustificationsAsync(phoneJustifications, ebillUser.IndexNumber, callMonth, callYear);
                    _logger.LogInformation("Saved {Count} phone overage justifications for user {IndexNumber}",
                        phoneJustifications.Count, ebillUser.IndexNumber);
                }

                // Submit to supervisor
                var submittedCount = await _verificationService.SubmitToSupervisorAsync(
                    verificationIds,
                    ebillUser.IndexNumber);

                // Send email notifications
                try
                {
                    var supervisorUser = await _context.EbillUsers
                        .FirstOrDefaultAsync(u => u.Email == ebillUser.SupervisorEmail);

                    if (supervisorUser != null)
                    {
                        await SendSubmittedConfirmationEmailAsync(ebillUser, callRecords, supervisorUser, hasOverage, monthlyAllowance, officialCallsCost, null);
                        await SendSupervisorNotificationEmailAsync(ebillUser, callRecords, supervisorUser, hasOverage, monthlyAllowance, officialCallsCost, null);
                        _logger.LogInformation("Call log submission emails sent successfully for user {IndexNumber}", ebillUser.IndexNumber);
                    }
                }
                catch (Exception emailEx)
                {
                    _logger.LogError(emailEx, "Failed to send submission emails for user {IndexNumber}", ebillUser.IndexNumber);
                }

                var message = $"Successfully submitted {submittedCount} call verifications to your supervisor for approval.";
                if (alreadySubmittedCalls.Any())
                    message += $" Note: {alreadySubmittedCalls.Count} call(s) were skipped because they were already submitted.";
                if (phoneJustifications.Any())
                    message += $" Submitted {phoneJustifications.Count} extension-level overage justification(s).";

                return new JsonResult(new { success = true, message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting to supervisor");
                return new JsonResult(new { success = false, message = $"Error submitting: {ex.Message}" });
            }
        }

        /// <summary>
        /// Calculate phone-level overages for submission preview
        /// Uses stored procedure sp_GetPhoneLevelOverages to eliminate N+1 queries.
        /// </summary>
        private async Task<List<SubmissionPhoneOverageDto>> CalculatePhoneLevelOveragesAsync(
            List<CallRecord> calls, int callMonth, int callYear)
        {
            var callIds = calls.Select(c => c.Id).ToList();
            if (!callIds.Any()) return new List<SubmissionPhoneOverageDto>();

            var phoneOverages = new List<SubmissionPhoneOverageDto>();

            var conn = _context.Database.GetDbConnection();
            var wasOpen = conn.State == ConnectionState.Open;
            if (!wasOpen) await conn.OpenAsync();

            try
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "ebill.sp_GetPhoneLevelOverages";
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandTimeout = 30;

                cmd.Parameters.Add(new SqlParameter("@CallRecordIds", string.Join(",", callIds)));
                cmd.Parameters.Add(new SqlParameter("@Month", callMonth));
                cmd.Parameters.Add(new SqlParameter("@Year", callYear));

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var justDate = reader.IsDBNull(reader.GetOrdinal("ExistingJustificationDate"))
                        ? null
                        : reader.GetDateTime(reader.GetOrdinal("ExistingJustificationDate")).ToString("MMMM dd, yyyy");

                    phoneOverages.Add(new SubmissionPhoneOverageDto
                    {
                        UserPhoneId = reader.GetInt32(reader.GetOrdinal("UserPhoneId")),
                        PhoneNumber = reader.GetString(reader.GetOrdinal("PhoneNumber")),
                        PhoneType = reader.GetString(reader.GetOrdinal("PhoneType")),
                        ClassOfService = reader.IsDBNull(reader.GetOrdinal("ClassOfService")) ? null : reader.GetString(reader.GetOrdinal("ClassOfService")),
                        AllowanceLimit = reader.GetDecimal(reader.GetOrdinal("AllowanceLimit")),
                        TotalUsage = reader.GetDecimal(reader.GetOrdinal("TotalUsage")),
                        OverageAmount = reader.GetDecimal(reader.GetOrdinal("OverageAmount")),
                        HasOverage = reader.GetBoolean(reader.GetOrdinal("HasOverage")),
                        CallCount = reader.GetInt32(reader.GetOrdinal("CallCount")),
                        HasExistingJustification = reader.GetBoolean(reader.GetOrdinal("HasExistingJustification")),
                        ExistingJustificationText = reader.IsDBNull(reader.GetOrdinal("ExistingJustificationText")) ? null : reader.GetString(reader.GetOrdinal("ExistingJustificationText")),
                        ExistingJustificationDate = justDate,
                        ExistingJustificationStatus = reader.IsDBNull(reader.GetOrdinal("ExistingJustificationStatus")) ? null : reader.GetString(reader.GetOrdinal("ExistingJustificationStatus")),
                        ExistingDocumentCount = reader.GetInt32(reader.GetOrdinal("ExistingDocumentCount"))
                    });
                }
            }
            finally
            {
                if (!wasOpen) await conn.CloseAsync();
            }

            return phoneOverages;
        }

        /// <summary>
        /// Save phone overage justifications
        /// </summary>
        private async Task SavePhoneOverageJustificationsAsync(
            List<PhoneOverageJustificationSubmitDto> phoneJustifications,
            string submittedBy,
            int month,
            int year)
        {
            foreach (var dto in phoneJustifications)
            {
                if (dto.UserPhoneId <= 0 || string.IsNullOrWhiteSpace(dto.Justification))
                    continue;

                var existingJustification = await _context.PhoneOverageJustifications
                    .FirstOrDefaultAsync(j =>
                        j.UserPhoneId == dto.UserPhoneId &&
                        j.Month == month &&
                        j.Year == year);

                if (existingJustification != null)
                {
                    _logger.LogWarning("Overage justification already exists for UserPhoneId {UserPhoneId} for {Month}/{Year}. Skipping.",
                        dto.UserPhoneId, month, year);
                    continue;
                }

                var allowanceLimit = await _calculationService.GetPhoneAllowanceLimitAsync(dto.UserPhoneId);
                if (!allowanceLimit.HasValue || allowanceLimit.Value == 0)
                    continue;

                var totalUsage = await _calculationService.GetPhoneMonthlyUsageAsync(dto.UserPhoneId, month, year);
                var overageAmount = Math.Max(0, totalUsage - allowanceLimit.Value);

                if (overageAmount <= 0)
                    continue;

                var justification = new PhoneOverageJustification
                {
                    UserPhoneId = dto.UserPhoneId,
                    Month = month,
                    Year = year,
                    AllowanceLimit = allowanceLimit.Value,
                    TotalUsage = totalUsage,
                    OverageAmount = overageAmount,
                    JustificationText = dto.Justification,
                    SubmittedBy = submittedBy,
                    SubmittedDate = DateTime.UtcNow,
                    ApprovalStatus = "Pending"
                };

                _context.PhoneOverageJustifications.Add(justification);
                await _context.SaveChangesAsync();

                if (dto.Document != null)
                {
                    await UploadPhoneOverageDocumentAsync(justification.Id, dto.Document, submittedBy, dto.Justification);
                }

                _logger.LogInformation("Saved phone overage justification for UserPhoneId {UserPhoneId} for {Month}/{Year}",
                    dto.UserPhoneId, month, year);
            }
        }

        /// <summary>
        /// Upload phone overage document
        /// </summary>
        private async Task UploadPhoneOverageDocumentAsync(
            int justificationId,
            IFormFile document,
            string uploadedBy,
            string description)
        {
            try
            {
                var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "phone-overage-documents");
                Directory.CreateDirectory(uploadPath);

                var fileExtension = Path.GetExtension(document.FileName);
                var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(uploadPath, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await document.CopyToAsync(stream);
                }

                var phoneOverageDoc = new PhoneOverageDocument
                {
                    PhoneOverageJustificationId = justificationId,
                    FileName = document.FileName,
                    FilePath = $"/uploads/phone-overage-documents/{uniqueFileName}",
                    FileSize = document.Length,
                    ContentType = document.ContentType,
                    Description = description,
                    UploadedBy = uploadedBy,
                    UploadedDate = DateTime.UtcNow
                };

                _context.PhoneOverageDocuments.Add(phoneOverageDoc);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Uploaded phone overage document for JustificationId {JustificationId}: {FileName}",
                    justificationId, document.FileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload phone overage document for JustificationId {JustificationId}",
                    justificationId);
                throw;
            }
        }

        private static string FormatDuration(decimal seconds)
        {
            var totalMinutes = seconds / 60.0m;
            if (totalMinutes < 1)
                return $"{seconds:N0}s";
            return $"{totalMinutes:N2}m";
        }

        private async Task SendSubmittedConfirmationEmailAsync(EbillUser staff, List<CallRecord> callRecords, EbillUser supervisor, bool hasOverage, decimal monthlyAllowance, decimal totalAmount, string? justification)
        {
            var callMonth = callRecords.First().CallMonth;
            var callYear = callRecords.First().CallYear;
            var monthName = new DateTime(callYear, callMonth, 1).ToString("MMMM");

            var sourceSystem = callRecords.First().SourceSystem?.ToUpperInvariant() ?? "";
            var currency = sourceSystem switch
            {
                "PW" or "PRIVATEWIRE" => "USD",
                _ => "KSH"
            };

            var overageMessage = hasOverage
                ? $"Your calls exceed the monthly allowance by {currency} {(totalAmount - monthlyAllowance):N2}. Justification has been included."
                : "Your calls are within the monthly allowance.";

            var overageBackgroundColor = hasOverage ? "#fff3cd" : "#d4edda";
            var overageBorderColor = hasOverage ? "#ffc107" : "#28a745";
            var overageTextColor = hasOverage ? "#856404" : "#155724";

            var placeholders = new Dictionary<string, string>
            {
                { "StaffName", $"{staff.FirstName} {staff.LastName}" },
                { "IndexNumber", staff.IndexNumber },
                { "Month", monthName },
                { "Year", callYear.ToString() },
                { "TotalCalls", callRecords.Count.ToString() },
                { "TotalAmount", totalAmount.ToString("N2") },
                { "Currency", currency },
                { "MonthlyAllowance", monthlyAllowance > 0 ? monthlyAllowance.ToString("N2") : "Unlimited" },
                { "SupervisorName", $"{supervisor.FirstName} {supervisor.LastName}" },
                { "OverageMessage", overageMessage },
                { "OverageBackgroundColor", overageBackgroundColor },
                { "OverageBorderColor", overageBorderColor },
                { "OverageTextColor", overageTextColor },
                { "ViewCallLogsLink", $"{Request.Scheme}://{Request.Host}/Modules/EBillManagement/CallRecords/MyCallLogs" }
            };

            await _emailService.SendTemplatedEmailAsync(
                to: staff.Email ?? "",
                templateCode: "CALL_LOG_SUBMITTED_CONFIRMATION",
                data: placeholders
            );
        }

        private async Task SendSupervisorNotificationEmailAsync(EbillUser staff, List<CallRecord> callRecords, EbillUser supervisor, bool hasOverage, decimal monthlyAllowance, decimal totalAmount, string? justification)
        {
            var callMonth = callRecords.First().CallMonth;
            var callYear = callRecords.First().CallYear;
            var monthName = new DateTime(callYear, callMonth, 1).ToString("MMMM");

            var sourceSystem = callRecords.First().SourceSystem?.ToUpperInvariant() ?? "";
            var currency = sourceSystem switch
            {
                "PW" or "PRIVATEWIRE" => "USD",
                _ => "KSH"
            };

            var overageMessage = hasOverage
                ? $"OVERAGE: Calls exceed allowance by {currency} {(totalAmount - monthlyAllowance):N2}"
                : "Calls are within allowance";

            var overageBackgroundColor = hasOverage ? "#fadbd8" : "#d4edda";
            var overageBorderColor = hasOverage ? "#dc3545" : "#28a745";
            var overageTextColor = hasOverage ? "#721c24" : "#155724";

            var placeholders = new Dictionary<string, string>
            {
                { "SupervisorName", $"{supervisor.FirstName} {supervisor.LastName}" },
                { "StaffName", $"{staff.FirstName} {staff.LastName}" },
                { "IndexNumber", staff.IndexNumber },
                { "Month", monthName },
                { "Year", callYear.ToString() },
                { "TotalCalls", callRecords.Count.ToString() },
                { "TotalAmount", totalAmount.ToString("N2") },
                { "Currency", currency },
                { "MonthlyAllowance", monthlyAllowance > 0 ? monthlyAllowance.ToString("N2") : "Unlimited" },
                { "OverageMessage", overageMessage },
                { "OverageBackgroundColor", overageBackgroundColor },
                { "OverageBorderColor", overageBorderColor },
                { "OverageTextColor", overageTextColor },
                { "JustificationText", hasOverage && !string.IsNullOrEmpty(justification) ? justification : "No justification required" },
                { "ApprovalLink", $"{Request.Scheme}://{Request.Host}/Modules/EBillManagement/CallRecords/SupervisorApprovals" }
            };

            await _emailService.SendTemplatedEmailAsync(
                to: supervisor.Email ?? "",
                templateCode: "CALL_LOG_SUPERVISOR_NOTIFICATION",
                data: placeholders
            );
        }
    }

    // DTOs for submission preview
    public class SubmissionPhoneOverageDto
    {
        public int UserPhoneId { get; set; }
        public string PhoneNumber { get; set; } = string.Empty;
        public string PhoneType { get; set; } = string.Empty;
        public string? ClassOfService { get; set; }
        public decimal AllowanceLimit { get; set; }
        public decimal TotalUsage { get; set; }
        public decimal OverageAmount { get; set; }
        public bool HasOverage { get; set; }
        public int CallCount { get; set; }
        public bool HasExistingJustification { get; set; }
        public string? ExistingJustificationText { get; set; }
        public string? ExistingJustificationDate { get; set; }
        public string? ExistingJustificationStatus { get; set; }
        public int ExistingDocumentCount { get; set; }
    }

    public class PhoneOverageJustificationSubmitDto
    {
        public int UserPhoneId { get; set; }
        public string Justification { get; set; } = string.Empty;
        public IFormFile? Document { get; set; }
    }

    // DTO for rejection request
    public class RejectAssignmentRequest
    {
        public int CallRecordId { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    // DTO for bulk assignment accept request
    public class BulkAssignmentRequest
    {
        public string? Extension { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public string? DialedNumber { get; set; } // Optional - for dialed number level
    }

    // DTO for bulk assignment reject request
    public class BulkRejectAssignmentRequest
    {
        public string? Extension { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public string? DialedNumber { get; set; } // Optional - for dialed number level
        public string Reason { get; set; } = string.Empty;
    }

    public class SubmissionPreviewRequest
    {
        public List<int> CallIds { get; set; } = new();
    }

    // Extension Group for Level 1 pagination
    public class ExtensionGroup
    {
        public string GroupId { get; set; } = string.Empty;
        public string Extension { get; set; } = string.Empty;
        public int Month { get; set; }
        public int Year { get; set; }
        public int CallCount { get; set; }
        public decimal TotalCostUSD { get; set; }
        public decimal TotalCostKSH { get; set; }
        public decimal OfficialUSD { get; set; }
        public decimal OfficialKSH { get; set; }
        public decimal PersonalUSD { get; set; }
        public decimal PersonalKSH { get; set; }
        public decimal TotalRecoveredUSD { get; set; }
        public decimal TotalRecoveredKSH { get; set; }
        public int PrivateWireCount { get; set; }
        public int KshSourceCount { get; set; }
        public bool IsPrivateWirePrimary { get; set; }
        public int DialedNumberCount { get; set; }

        // Submission status counts
        public int SubmittedCount { get; set; }
        public int PendingApprovalCount { get; set; }
        public int ApprovedCount { get; set; }
        public int RejectedCount { get; set; }
        public int RevertedCount { get; set; }
        public int PartiallyApprovedCount { get; set; }

        // Incoming assignment counts (calls assigned TO current user)
        public int IncomingAssignmentCount { get; set; }
        public string? AssignedFromUser { get; set; }

        // Outgoing pending reassignment counts (calls user reassigned to others, pending acceptance)
        public int OutgoingPendingCount { get; set; }
        public string? AssignedToUser { get; set; }

        // Class of Service details for the extension
        public string? ClassOfService { get; set; }
        public string? CosService { get; set; }
        public string? CosEligibleStaff { get; set; }
        public string? CosAirtimeAllowance { get; set; }
        public string? CosDataAllowance { get; set; }
        public string? CosHandsetAllowance { get; set; }

        public string MonthName => new DateTime(Year, Month, 1).ToString("MMMM");
    }

    // Dialed Number Group for Level 2 (AJAX response)
    public class DialedNumberGroupDto
    {
        public string DialedGroupId { get; set; } = string.Empty;
        public string DialedNumber { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public int CallCount { get; set; }
        public decimal TotalCostUSD { get; set; }
        public decimal TotalCostKSH { get; set; }
        public decimal TotalDuration { get; set; }
        public string AssignmentStatus { get; set; } = string.Empty;
        public bool IsDataSession { get; set; } // True if GPRS/data session (duration is in KB)
        public int SubmittedCount { get; set; } // Number of calls submitted to supervisor
        public int PendingApprovalCount { get; set; } // Number of calls pending supervisor approval
        public int ApprovedCount { get; set; } // Number of calls approved by supervisor
        public int RejectedCount { get; set; } // Number of calls rejected by supervisor
        public int RevertedCount { get; set; } // Number of calls reverted by supervisor
        public int PartiallyApprovedCount { get; set; } // Number of calls partially approved by supervisor
        public int IncomingAssignmentCount { get; set; } // Number of calls assigned TO current user (pending acceptance)
        public string? AssignedFromUser { get; set; } // Who assigned them (if all from same person)

        // Outgoing pending reassignment counts (calls user reassigned to others, pending acceptance)
        public int OutgoingPendingCount { get; set; }
        public string? AssignedToUser { get; set; } // Who they were assigned to (if all to same person)
    }

    // Call Log Item for Level 3 (AJAX response)
    public class CallLogItemDto
    {
        public int Id { get; set; }
        public string DialedNumber { get; set; } = string.Empty;
        public DateTime CallDate { get; set; }
        public DateTime CallEndTime { get; set; }
        public decimal CallDuration { get; set; }
        public decimal CallCostUSD { get; set; }
        public decimal CallCostKSH { get; set; }
        public string Destination { get; set; } = string.Empty;
        public string CallType { get; set; } = string.Empty;
        public string VerificationType { get; set; } = string.Empty;
        public bool IsVerified { get; set; }
        public string? SupervisorApprovalStatus { get; set; }
        public bool IsSubmittedToSupervisor { get; set; }
        public string AssignmentStatus { get; set; } = string.Empty;
        public string? AssignedFrom { get; set; }  // Who assigned this call (for incoming assignments)
        public string? AssignedTo { get; set; }    // Who this call was reassigned to (for outgoing pending assignments)
        public bool IsLocked { get; set; }
    }

    // Response DTOs for AJAX
    public class DialedNumbersResponse
    {
        public List<DialedNumberGroupDto> DialedNumbers { get; set; } = new();
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public string Extension { get; set; } = string.Empty;
        public int Month { get; set; }
        public int Year { get; set; }
        public string GroupId { get; set; } = string.Empty;
    }

    public class CallLogsResponse
    {
        public List<CallLogItemDto> CallLogs { get; set; } = new();
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public string DialedNumber { get; set; } = string.Empty;
        public string DialedGroupId { get; set; } = string.Empty;
        public string SortBy { get; set; } = "CallDate";
        public bool SortDesc { get; set; } = true;
    }
}
