using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
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
            // If user explicitly selects "All", don't override their choice
            bool isFirstVisit = !Request.Query.ContainsKey("FilterMonth") && !Request.Query.ContainsKey("FilterYear");
            if (isFirstVisit)
            {
                FilterMonth = DateTime.UtcNow.Month;
                FilterYear = DateTime.UtcNow.Year;
            }

            // Load call records with filters
            await LoadCallRecordsAsync();

            // Load summary statistics
            await LoadSummaryAsync();

            return Page();
        }

        private async Task LoadCallRecordsAsync()
        {
            // Check if user is admin
            bool isAdmin = User.IsInRole("Admin");

            // If not admin and no UserIndexNumber, return empty
            if (string.IsNullOrEmpty(UserIndexNumber) && !isAdmin)
                return;

            var query = _context.CallRecords
                .Include(c => c.UserPhone)
                    .ThenInclude(up => up.ClassOfService)
                .Include(c => c.PayingUser)
                .Include(c => c.ResponsibleUser)
                .Where(c => c.CallDate.Year > 1 || (c.CallType != null && EF.Functions.Like(c.CallType, "Corporate Value Pack Data%"))) // Filter out invalid dates but keep Corporate Value Pack Data
                .AsQueryable();

            // Filter by UserIndexNumber only if not admin
            // Use Any() for exclusions which translates to efficient NOT EXISTS in SQL
            if (!isAdmin && !string.IsNullOrEmpty(UserIndexNumber))
            {
                var userIndex = UserIndexNumber; // Capture for lambda

                if (!string.IsNullOrEmpty(FilterAssignmentType))
                {
                    switch (FilterAssignmentType.ToLower())
                    {
                        case "own":
                            // Own calls: responsible for call, not assigned out (accepted), and not incoming assigned
                            query = query.Where(c => c.ResponsibleIndexNumber == userIndex &&
                                !_context.Set<CallLogPaymentAssignment>().Any(a =>
                                    a.CallRecordId == c.Id && a.AssignedFrom == userIndex && a.AssignmentStatus == "Accepted") &&
                                (c.PaymentAssignmentId == null ||
                                 !_context.Set<CallLogPaymentAssignment>().Any(a =>
                                    a.CallRecordId == c.Id && a.AssignedTo == userIndex &&
                                    (a.AssignmentStatus == "Pending" || a.AssignmentStatus == "Accepted"))));
                            break;
                        case "assigned":
                            // Only show calls assigned TO current user
                            query = query.Where(c =>
                                _context.Set<CallLogPaymentAssignment>().Any(a =>
                                    a.CallRecordId == c.Id && a.AssignedTo == userIndex &&
                                    (a.AssignmentStatus == "Pending" || a.AssignmentStatus == "Accepted")));
                            break;
                        default:
                            // All: own calls (excluding accepted outgoing) + incoming assigned calls
                            query = query.Where(c =>
                                (c.ResponsibleIndexNumber == userIndex &&
                                 !_context.Set<CallLogPaymentAssignment>().Any(a =>
                                    a.CallRecordId == c.Id && a.AssignedFrom == userIndex && a.AssignmentStatus == "Accepted")) ||
                                _context.Set<CallLogPaymentAssignment>().Any(a =>
                                    a.CallRecordId == c.Id && a.AssignedTo == userIndex &&
                                    (a.AssignmentStatus == "Pending" || a.AssignmentStatus == "Accepted")));
                            break;
                    }
                }
                else
                {
                    // Default: own calls (excluding accepted outgoing) + incoming assigned calls
                    query = query.Where(c =>
                        (c.ResponsibleIndexNumber == userIndex &&
                         !_context.Set<CallLogPaymentAssignment>().Any(a =>
                            a.CallRecordId == c.Id && a.AssignedFrom == userIndex && a.AssignmentStatus == "Accepted")) ||
                        _context.Set<CallLogPaymentAssignment>().Any(a =>
                            a.CallRecordId == c.Id && a.AssignedTo == userIndex &&
                            (a.AssignmentStatus == "Pending" || a.AssignmentStatus == "Accepted")));
                }
            }

            // Apply filters
            if (FilterMonth.HasValue)
                query = query.Where(c => c.CallMonth == FilterMonth.Value);

            if (FilterYear.HasValue)
                query = query.Where(c => c.CallYear == FilterYear.Value);

            if (FilterStartDate.HasValue)
                query = query.Where(c => c.CallDate >= FilterStartDate.Value);

            if (FilterEndDate.HasValue)
                query = query.Where(c => c.CallDate <= FilterEndDate.Value);

            if (FilterMinCost.HasValue)
                query = query.Where(c => c.CallCostUSD >= FilterMinCost.Value);

            if (!string.IsNullOrEmpty(FilterStatus))
            {
                switch (FilterStatus.ToLower())
                {
                    case "unverified":
                        query = query.Where(c => !c.IsVerified);
                        break;
                    case "verified":
                        query = query.Where(c => c.IsVerified);
                        break;
                    case "approved":
                        query = query.Where(c => c.SupervisorApprovalStatus == "Approved");
                        break;
                    case "pending":
                        query = query.Where(c => c.IsVerified && c.SupervisorApprovalStatus == "Pending");
                        break;
                    case "overdue":
                        query = query.Where(c => !c.IsVerified && c.VerificationPeriod.HasValue && c.VerificationPeriod.Value < DateTime.UtcNow);
                        break;
                }
            }

            // Get total call records count
            TotalCallRecords = await query.CountAsync();

            // GROUP BY Extension + Month + Year to get unique extension groups
            var extensionGroupsQuery = query
                .GroupBy(c => new {
                    Extension = c.UserPhone != null ? c.UserPhone.PhoneNumber : "Unknown",
                    c.CallMonth,
                    c.CallYear
                })
                .Select(g => new ExtensionGroup
                {
                    Extension = g.Key.Extension,
                    Month = g.Key.CallMonth,
                    Year = g.Key.CallYear,
                    CallCount = g.Count(),
                    TotalCostUSD = g.Sum(c => c.CallCostUSD),
                    TotalCostKSH = g.Sum(c => c.CallCostKSHS),
                    OfficialUSD = g.Where(c => c.VerificationType == "Official").Sum(c => c.CallCostUSD),
                    OfficialKSH = g.Where(c => c.VerificationType == "Official").Sum(c => c.CallCostKSHS),
                    PersonalUSD = g.Where(c => c.VerificationType == "Personal").Sum(c => c.CallCostUSD),
                    PersonalKSH = g.Where(c => c.VerificationType == "Personal").Sum(c => c.CallCostKSHS),
                    TotalRecoveredUSD = g.Where(c => c.SourceSystem != null &&
                        (c.SourceSystem.ToLower().Contains("privatewire") || c.SourceSystem.ToLower().Contains("pw")))
                        .Sum(c => c.RecoveryAmount ?? 0),
                    TotalRecoveredKSH = g.Where(c => c.SourceSystem != null &&
                        (c.SourceSystem.ToLower().Contains("safaricom") ||
                         c.SourceSystem.ToLower().Contains("airtel") ||
                         c.SourceSystem.ToLower().Contains("pstn")))
                        .Sum(c => c.RecoveryAmount ?? 0),
                    PrivateWireCount = g.Count(c => c.SourceSystem != null &&
                        (c.SourceSystem.ToLower().Contains("privatewire") || c.SourceSystem.ToLower().Contains("pw"))),
                    KshSourceCount = g.Count(c => c.SourceSystem != null &&
                        (c.SourceSystem.ToLower().Contains("safaricom") ||
                         c.SourceSystem.ToLower().Contains("airtel") ||
                         c.SourceSystem.ToLower().Contains("pstn"))),
                    DialedNumberCount = g.Select(c => c.CallNumber).Distinct().Count()
                });

            // Order by Year DESC, Month DESC, Extension ASC
            extensionGroupsQuery = extensionGroupsQuery
                .OrderByDescending(g => g.Year)
                .ThenByDescending(g => g.Month)
                .ThenBy(g => g.Extension);

            // Count total extension groups for pagination
            TotalRecords = await extensionGroupsQuery.CountAsync();
            TotalPages = (int)Math.Ceiling(TotalRecords / (double)PageSize);

            // Apply pagination to extension groups
            ExtensionGroups = await extensionGroupsQuery
                .Skip((PageNumber - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            // Set IsPrivateWirePrimary for each group
            foreach (var group in ExtensionGroups)
            {
                group.IsPrivateWirePrimary = group.PrivateWireCount > group.KshSourceCount;
                group.GroupId = $"{group.Extension}_{group.Month}_{group.Year}".Replace(" ", "_").Replace("-", "_").Replace("+", "");
            }

            // Look up Class of Service for each extension from UserPhones
            if (ExtensionGroups.Any())
            {
                var extensionNumbers = ExtensionGroups.Select(g => g.Extension).Distinct().ToList();
                var phoneClassMap = await _context.UserPhones
                    .Where(up => extensionNumbers.Contains(up.PhoneNumber) && up.ClassOfServiceId != null)
                    .Include(up => up.ClassOfService)
                    .Select(up => new {
                        up.PhoneNumber,
                        ClassName = up.ClassOfService != null ? up.ClassOfService.Class : null,
                        Service = up.ClassOfService != null ? up.ClassOfService.Service : null,
                        EligibleStaff = up.ClassOfService != null ? up.ClassOfService.EligibleStaff : null,
                        AirtimeAllowance = up.ClassOfService != null ? up.ClassOfService.AirtimeAllowance : null,
                        DataAllowance = up.ClassOfService != null ? up.ClassOfService.DataAllowance : null,
                        HandsetAllowance = up.ClassOfService != null ? up.ClassOfService.HandsetAllowance : null
                    })
                    .ToListAsync();

                foreach (var group in ExtensionGroups)
                {
                    var cos = phoneClassMap.FirstOrDefault(p => p.PhoneNumber == group.Extension);
                    if (cos != null)
                    {
                        group.ClassOfService = cos.ClassName;
                        group.CosService = cos.Service;
                        group.CosEligibleStaff = cos.EligibleStaff;
                        group.CosAirtimeAllowance = cos.AirtimeAllowance;
                        group.CosDataAllowance = cos.DataAllowance;
                        group.CosHandsetAllowance = cos.HandsetAllowance;
                    }
                }
            }

            // Get submission status counts per extension/month/year
            if (ExtensionGroups.Any())
            {
                // Extract filter values that EF Core can translate to SQL
                var extensions = ExtensionGroups.Select(g => g.Extension).Distinct().ToList();
                var months = ExtensionGroups.Select(g => g.Month).Distinct().ToList();
                var years = ExtensionGroups.Select(g => g.Year).Distinct().ToList();

                var submissionCounts = await _context.CallLogVerifications
                    .Where(v => v.SubmittedToSupervisor)
                    .Join(_context.CallRecords,
                          v => v.CallRecordId,
                          cr => cr.Id,
                          (v, cr) => new {
                              Extension = cr.ExtensionNumber,
                              cr.CallMonth,
                              cr.CallYear,
                              v.ApprovalStatus
                          })
                    .Where(x => extensions.Contains(x.Extension) &&
                               months.Contains(x.CallMonth) &&
                               years.Contains(x.CallYear))
                    .GroupBy(x => new { x.Extension, x.CallMonth, x.CallYear })
                    .Select(g => new {
                        Extension = g.Key.Extension,
                        Month = g.Key.CallMonth,
                        Year = g.Key.CallYear,
                        SubmittedCount = g.Count(),
                        PendingCount = g.Count(x => x.ApprovalStatus == "Pending"),
                        ApprovedCount = g.Count(x => x.ApprovalStatus == "Approved"),
                        PartiallyApprovedCount = g.Count(x => x.ApprovalStatus == "PartiallyApproved"),
                        RejectedCount = g.Count(x => x.ApprovalStatus == "Rejected"),
                        RevertedCount = g.Count(x => x.ApprovalStatus == "Reverted")
                    })
                    .ToListAsync();

                // Populate extension groups with submission counts
                foreach (var group in ExtensionGroups)
                {
                    var counts = submissionCounts.FirstOrDefault(c =>
                        c.Extension == group.Extension && c.Month == group.Month && c.Year == group.Year);
                    if (counts != null)
                    {
                        group.SubmittedCount = counts.SubmittedCount;
                        group.PendingApprovalCount = counts.PendingCount;
                        group.ApprovedCount = counts.ApprovedCount;
                        group.PartiallyApprovedCount = counts.PartiallyApprovedCount;
                        group.RejectedCount = counts.RejectedCount;
                        group.RevertedCount = counts.RevertedCount;
                    }
                }

                // Get incoming assignment counts (calls assigned TO current user that are pending)
                if (!string.IsNullOrEmpty(UserIndexNumber))
                {
                    var assignmentCounts = await _context.Set<CallLogPaymentAssignment>()
                        .Where(a => a.AssignedTo == UserIndexNumber && a.AssignmentStatus == "Pending")
                        .Join(_context.CallRecords,
                              a => a.CallRecordId,
                              cr => cr.Id,
                              (a, cr) => new {
                                  Extension = cr.ExtensionNumber,
                                  cr.CallMonth,
                                  cr.CallYear,
                                  a.AssignedFrom
                              })
                        .Where(x => extensions.Contains(x.Extension) &&
                                   months.Contains(x.CallMonth) &&
                                   years.Contains(x.CallYear))
                        .GroupBy(x => new { x.Extension, x.CallMonth, x.CallYear })
                        .Select(g => new {
                            Extension = g.Key.Extension,
                            Month = g.Key.CallMonth,
                            Year = g.Key.CallYear,
                            AssignmentCount = g.Count(),
                            AssignedFromUser = g.Select(x => x.AssignedFrom).Distinct().Count() == 1
                                ? g.Select(x => x.AssignedFrom).FirstOrDefault()
                                : null
                        })
                        .ToListAsync();

                    foreach (var group in ExtensionGroups)
                    {
                        var assignmentInfo = assignmentCounts.FirstOrDefault(c =>
                            c.Extension == group.Extension && c.Month == group.Month && c.Year == group.Year);
                        if (assignmentInfo != null)
                        {
                            group.IncomingAssignmentCount = assignmentInfo.AssignmentCount;
                            group.AssignedFromUser = assignmentInfo.AssignedFromUser;
                        }
                    }

                    // Get outgoing pending reassignment counts (calls user reassigned to others, pending acceptance)
                    var outgoingCounts = await _context.Set<CallLogPaymentAssignment>()
                        .Where(a => a.AssignedFrom == UserIndexNumber && a.AssignmentStatus == "Pending")
                        .Join(_context.CallRecords,
                              a => a.CallRecordId,
                              cr => cr.Id,
                              (a, cr) => new {
                                  Extension = cr.ExtensionNumber,
                                  cr.CallMonth,
                                  cr.CallYear,
                                  a.AssignedTo
                              })
                        .Where(x => extensions.Contains(x.Extension) &&
                                   months.Contains(x.CallMonth) &&
                                   years.Contains(x.CallYear))
                        .GroupBy(x => new { x.Extension, x.CallMonth, x.CallYear })
                        .Select(g => new {
                            Extension = g.Key.Extension,
                            Month = g.Key.CallMonth,
                            Year = g.Key.CallYear,
                            OutgoingCount = g.Count(),
                            AssignedToUser = g.Select(x => x.AssignedTo).Distinct().Count() == 1
                                ? g.Select(x => x.AssignedTo).FirstOrDefault()
                                : null
                        })
                        .ToListAsync();

                    foreach (var group in ExtensionGroups)
                    {
                        var outgoingInfo = outgoingCounts.FirstOrDefault(c =>
                            c.Extension == group.Extension && c.Month == group.Month && c.Year == group.Year);
                        if (outgoingInfo != null)
                        {
                            group.OutgoingPendingCount = outgoingInfo.OutgoingCount;
                            group.AssignedToUser = outgoingInfo.AssignedToUser;
                        }
                    }
                }
            }
        }

        private async Task LoadSummaryAsync()
        {
            bool isAdmin = User.IsInRole("Admin");

            // For admin without UserIndexNumber, calculate summary for all records
            if (string.IsNullOrEmpty(UserIndexNumber) && isAdmin)
            {
                // Build query for all records
                var query = _context.CallRecords.AsQueryable();

                // Apply month/year filter only if specified
                if (FilterMonth.HasValue)
                {
                    query = query.Where(c => c.CallMonth == FilterMonth.Value);
                }

                if (FilterYear.HasValue)
                {
                    query = query.Where(c => c.CallYear == FilterYear.Value);
                }

                // Use database aggregation instead of loading all records into memory
                var totalCalls = await query.CountAsync();
                var verifiedCalls = await query.CountAsync(c => c.IsVerified);
                var totalAmount = await query.SumAsync(c => (decimal?)c.CallCostUSD) ?? 0;
                var verifiedAmount = await query.Where(c => c.IsVerified).SumAsync(c => (decimal?)c.CallCostUSD) ?? 0;
                var personalCalls = await query.CountAsync(c => c.VerificationType == "Personal");
                var officialCalls = await query.CountAsync(c => c.VerificationType == "Official");

                Summary = new VerificationSummary
                {
                    TotalCalls = totalCalls,
                    VerifiedCalls = verifiedCalls,
                    UnverifiedCalls = totalCalls - verifiedCalls,
                    TotalAmount = totalAmount,
                    VerifiedAmount = verifiedAmount,
                    PersonalCalls = personalCalls,
                    OfficialCalls = officialCalls,
                    CompliancePercentage = totalCalls > 0
                        ? (decimal)verifiedCalls / totalCalls * 100
                        : 0
                };

                AllowanceLimit = 0; // No specific limit for admin view
                CurrentUsage = Summary.TotalAmount;
                RemainingAllowance = 0;
                IsOverAllowance = false;
                return;
            }

            if (string.IsNullOrEmpty(UserIndexNumber))
                return;

            // Subquery for incoming assigned calls (calls assigned TO current user)
            var incomingAssignedCallIdsQuery = _context.Set<CallLogPaymentAssignment>()
                .Where(a => a.AssignedTo == UserIndexNumber &&
                       (a.AssignmentStatus == "Pending" || a.AssignmentStatus == "Accepted"))
                .Select(a => a.CallRecordId);

            // Subquery for outgoing accepted assignments (calls assigned BY current user that were ACCEPTED)
            // These should NOT appear in the current user's view anymore
            var acceptedOutgoingCallIdsQuery = _context.Set<CallLogPaymentAssignment>()
                .Where(a => a.AssignedFrom == UserIndexNumber && a.AssignmentStatus == "Accepted")
                .Select(a => a.CallRecordId);

            // Build query for user's calls: (own - accepted outgoing) + incoming assigned
            var userQuery = _context.CallRecords
                .Where(c => c.CallDate.Year > 1 || (c.CallType != null && EF.Functions.Like(c.CallType, "Corporate Value Pack Data%"))) // Filter out invalid dates but keep Corporate Value Pack Data
                .Where(c =>
                    (c.ResponsibleIndexNumber == UserIndexNumber && !acceptedOutgoingCallIdsQuery.Contains(c.Id)) ||
                    incomingAssignedCallIdsQuery.Contains(c.Id));

            // Apply month/year filter only if specified
            if (FilterMonth.HasValue)
            {
                userQuery = userQuery.Where(c => c.CallMonth == FilterMonth.Value);
            }

            if (FilterYear.HasValue)
            {
                userQuery = userQuery.Where(c => c.CallYear == FilterYear.Value);
            }

            // Get all calls user is responsible for (own calls + assigned calls)
            var allUserRecords = await userQuery.ToListAsync();

            // Calculate summary from actual user records (including assigned calls)
            Summary = new VerificationSummary
            {
                TotalCalls = allUserRecords.Count,
                VerifiedCalls = allUserRecords.Count(c => c.IsVerified),
                UnverifiedCalls = allUserRecords.Count(c => !c.IsVerified),
                TotalAmount = allUserRecords.Sum(c => c.CallCostUSD),
                VerifiedAmount = allUserRecords.Where(c => c.IsVerified).Sum(c => c.CallCostUSD),
                PersonalCalls = allUserRecords.Count(c => c.VerificationType == "Personal"),
                OfficialCalls = allUserRecords.Count(c => c.VerificationType == "Official"),
                CompliancePercentage = allUserRecords.Count > 0
                    ? (decimal)allUserRecords.Count(c => c.IsVerified) / allUserRecords.Count * 100
                    : 0,
                OverageAmount = 0 // Will be calculated below
            };

            // Get allowance limit and calculate usage
            var limitNullable = await _calculationService.GetAllowanceLimitAsync(UserIndexNumber);
            AllowanceLimit = limitNullable ?? 0; // Unlimited = 0 for display

            // Current usage should be total cost of all calls user is responsible for
            CurrentUsage = Summary.TotalAmount;

            // Calculate remaining allowance and overage
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
                // No limit set (unlimited) - set to 0, we'll handle display in the view
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
        /// ULTRA-FAST bulk recall using raw SQL
        /// </summary>
        private async Task<(int RecalledCount, List<string> Errors)> BulkRecallAssignmentsRawAsync(
            string indexNumber,
            string? extension = null,
            int? month = null,
            int? year = null,
            string? dialedNumber = null)
        {
            var errors = new List<string>();
            var now = DateTime.UtcNow;
            int recalledCount = 0;

            try
            {
                var strategy = _context.Database.CreateExecutionStrategy();

                await strategy.ExecuteAsync(async () =>
                {
                    using var transaction = await _context.Database.BeginTransactionAsync();

                    // Build filter clauses
                    var additionalFilters = new List<string>();
                    var parameters = new List<Microsoft.Data.SqlClient.SqlParameter>
                    {
                        new Microsoft.Data.SqlClient.SqlParameter("@indexNumber", indexNumber),
                        new Microsoft.Data.SqlClient.SqlParameter("@now", now)
                    };

                    var needsUserPhoneJoin = !string.IsNullOrEmpty(extension);
                    var userPhoneJoin = needsUserPhoneJoin ? "INNER JOIN UserPhones up ON cr.UserPhoneId = up.Id" : "";

                    if (!string.IsNullOrEmpty(extension))
                    {
                        additionalFilters.Add("up.PhoneNumber = @extension");
                        parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@extension", extension));
                    }
                    if (month.HasValue)
                    {
                        additionalFilters.Add("cr.call_month = @month");
                        parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@month", month.Value));
                    }
                    if (year.HasValue)
                    {
                        additionalFilters.Add("cr.call_year = @year");
                        parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@year", year.Value));
                    }
                    if (!string.IsNullOrEmpty(dialedNumber))
                    {
                        additionalFilters.Add("ISNULL(cr.call_number, '') = @dialedNumber");
                        parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@dialedNumber", dialedNumber));
                    }

                    var additionalFilterClause = additionalFilters.Count > 0 ? "AND " + string.Join(" AND ", additionalFilters) : "";

                    // Step 1: Update CallRecords - revert payment back to original owner
                    var updateCallRecordsSql = $@"
                        UPDATE cr
                        SET cr.call_pay_index = pa.AssignedFrom,
                            cr.payment_assignment_id = NULL,
                            cr.assignment_status = 'None'
                        FROM CallRecords cr
                        INNER JOIN CallLogPaymentAssignments pa ON cr.payment_assignment_id = pa.Id
                        {userPhoneJoin}
                        WHERE pa.AssignedFrom = @indexNumber
                          AND pa.AssignmentStatus = 'Pending'
                          {additionalFilterClause}";

                    await _context.Database.ExecuteSqlRawAsync(updateCallRecordsSql, parameters.ToArray());

                    // Step 2: Update CallLogPaymentAssignments to Recalled
                    var updateAssignmentsSql = $@"
                        UPDATE pa
                        SET pa.AssignmentStatus = 'Recalled',
                            pa.ModifiedDate = @now2
                        FROM CallLogPaymentAssignments pa
                        INNER JOIN CallRecords cr ON pa.CallRecordId = cr.Id
                        {userPhoneJoin}
                        WHERE pa.AssignedFrom = @indexNumber2
                          AND pa.AssignmentStatus = 'Pending'
                          {additionalFilterClause.Replace("@", "@2_")}";

                    // Build parameters for second query
                    var parameters2 = new List<Microsoft.Data.SqlClient.SqlParameter>
                    {
                        new Microsoft.Data.SqlClient.SqlParameter("@indexNumber2", indexNumber),
                        new Microsoft.Data.SqlClient.SqlParameter("@now2", now)
                    };
                    if (!string.IsNullOrEmpty(extension))
                        parameters2.Add(new Microsoft.Data.SqlClient.SqlParameter("@2_extension", extension));
                    if (month.HasValue)
                        parameters2.Add(new Microsoft.Data.SqlClient.SqlParameter("@2_month", month.Value));
                    if (year.HasValue)
                        parameters2.Add(new Microsoft.Data.SqlClient.SqlParameter("@2_year", year.Value));
                    if (!string.IsNullOrEmpty(dialedNumber))
                        parameters2.Add(new Microsoft.Data.SqlClient.SqlParameter("@2_dialedNumber", dialedNumber));

                    recalledCount = await _context.Database.ExecuteSqlRawAsync(
                        updateAssignmentsSql, parameters2.ToArray());

                    await transaction.CommitAsync();
                });

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
        /// </summary>
        public async Task<JsonResult> OnGetDialedNumbersAsync(
            string? extension, int month, int year, int page = 1, int pageSize = 20)
        {
            try
            {
                // Validate required parameters
                if (string.IsNullOrEmpty(extension))
                    return new JsonResult(new { error = "Extension is required" });

                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                    return new JsonResult(new { error = "User not authenticated" });

                var ebillUser = await _context.EbillUsers
                    .FirstOrDefaultAsync(u => u.Email == user.Email);

                bool isAdmin = User.IsInRole("Admin");
                string? userIndexNumber = ebillUser?.IndexNumber;

                if (ebillUser == null && !isAdmin)
                    return new JsonResult(new { error = "User profile not found" });

                // Use ExtensionNumber directly (indexed column) - no JOIN to UserPhones
                var query = _context.CallRecords
                    .Where(c => c.ExtensionNumber == extension &&
                               c.CallMonth == month && c.CallYear == year &&
                               (c.CallDate.Year > 1 || (c.CallType != null && EF.Functions.Like(c.CallType, "Corporate Value Pack Data%"))));

                // Filter by user if not admin
                if (!isAdmin && !string.IsNullOrEmpty(userIndexNumber))
                {
                    var incomingAssignedCallIdsQuery = _context.Set<CallLogPaymentAssignment>()
                        .Where(a => a.AssignedTo == userIndexNumber &&
                               (a.AssignmentStatus == "Pending" || a.AssignmentStatus == "Accepted"))
                        .Select(a => a.CallRecordId);

                    var acceptedOutgoingCallIdsQuery = _context.Set<CallLogPaymentAssignment>()
                        .Where(a => a.AssignedFrom == userIndexNumber && a.AssignmentStatus == "Accepted")
                        .Select(a => a.CallRecordId);

                    query = query.Where(c =>
                        (c.ResponsibleIndexNumber == userIndexNumber && !acceptedOutgoingCallIdsQuery.Contains(c.Id)) ||
                        incomingAssignedCallIdsQuery.Contains(c.Id));
                }

                // Single GroupBy execution - fetch all groups, paginate in memory
                // (typically < 100 distinct dialed numbers per extension/month)
                // IsDataSession moved to post-processing to avoid expensive LOWER()+LIKE in GroupBy
                var allDialedNumbers = await query
                    .GroupBy(c => c.CallNumber ?? "")
                    .Select(g => new DialedNumberGroupDto
                    {
                        DialedNumber = g.Key == "" ? "Subscription" : g.Key,
                        Destination = g.Select(c => c.CallDestination).FirstOrDefault() ?? "",
                        CallCount = g.Count(),
                        TotalCostUSD = g.Sum(c => c.CallCostUSD),
                        TotalCostKSH = g.Sum(c => c.CallCostKSHS),
                        TotalDuration = g.Sum(c => (decimal)c.CallDuration),
                        AssignmentStatus = g.Select(c => c.VerificationType).Distinct().Count() > 1 ? "Mixed" :
                                          (g.Select(c => c.VerificationType).FirstOrDefault() ?? "Unverified"),
                        IsDataSession = false
                    })
                    .OrderByDescending(d => d.CallCount)
                    .ToListAsync();

                var totalCount = allDialedNumbers.Count;
                var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

                // Paginate in memory (avoids re-executing the GroupBy for count)
                var dialedNumbers = allDialedNumbers
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                // Set DialedGroupId for each
                var safeExtension = (extension ?? "Unknown").Replace(" ", "_").Replace("-", "_").Replace("+", "");
                var groupId = $"{safeExtension}_{month}_{year}";
                foreach (var dn in dialedNumbers)
                {
                    var safeDialedNumber = (dn.DialedNumber ?? "Subscription").Replace("+", "").Replace(" ", "_").Replace("-", "_");
                    dn.DialedGroupId = $"{groupId}_dialed_{safeDialedNumber}";
                }

                if (dialedNumbers.Any())
                {
                    var dialedNumbersList = dialedNumbers
                        .Select(d => d.DialedNumber == "Subscription" ? "" : d.DialedNumber)
                        .ToList();
                    var hasSubscription = dialedNumbers.Any(d => d.DialedNumber == "Subscription");

                    // Pre-filter CallRecords for this extension/month/year (reused by multiple queries below)
                    var filteredCallRecords = _context.CallRecords
                        .Where(cr => cr.ExtensionNumber == extension && cr.CallMonth == month && cr.CallYear == year);

                    // Determine IsDataSession - lightweight query using case-insensitive LIKE (no LOWER())
                    var dataSessionNumbers = await filteredCallRecords
                        .Where(c => dialedNumbersList.Contains(c.CallNumber ?? "") ||
                                   (hasSubscription && (c.CallNumber == null || c.CallNumber == "")))
                        .Where(c => c.CallType != null &&
                            (EF.Functions.Like(c.CallType, "%GPRS%") || EF.Functions.Like(c.CallType, "%data%")))
                        .Select(c => c.CallNumber ?? "")
                        .Distinct()
                        .ToListAsync();

                    // Submission counts - uses ExtensionNumber (indexed), no UserPhone JOIN
                    var submissionCounts = await filteredCallRecords
                        .Where(c => dialedNumbersList.Contains(c.CallNumber ?? "") ||
                                   (hasSubscription && (c.CallNumber == null || c.CallNumber == "")))
                        .Join(_context.CallLogVerifications.Where(v => v.SubmittedToSupervisor),
                              cr => cr.Id,
                              v => v.CallRecordId,
                              (cr, v) => new { cr.CallNumber, v.ApprovalStatus })
                        .GroupBy(x => x.CallNumber ?? "")
                        .Select(g => new
                        {
                            DialedNumber = g.Key == "" ? "Subscription" : g.Key,
                            SubmittedCount = g.Count(),
                            PendingCount = g.Count(x => x.ApprovalStatus == "Pending"),
                            ApprovedCount = g.Count(x => x.ApprovalStatus == "Approved")
                        })
                        .ToDictionaryAsync(x => x.DialedNumber, x => x);

                    // Apply IsDataSession and submission counts
                    foreach (var dn in dialedNumbers)
                    {
                        var key = dn.DialedNumber == "Subscription" ? "" : dn.DialedNumber;
                        dn.IsDataSession = dataSessionNumbers.Contains(key);

                        if (submissionCounts.TryGetValue(dn.DialedNumber, out var counts))
                        {
                            dn.SubmittedCount = counts.SubmittedCount;
                            dn.PendingApprovalCount = counts.PendingCount;
                            dn.ApprovedCount = counts.ApprovedCount;
                        }
                    }

                    // Assignment queries - use pre-filtered CallRecords (no UserPhone JOIN)
                    if (!string.IsNullOrEmpty(userIndexNumber))
                    {
                        var assignmentCounts = await _context.Set<CallLogPaymentAssignment>()
                            .Where(a => a.AssignedTo == userIndexNumber && a.AssignmentStatus == "Pending")
                            .Join(filteredCallRecords,
                                  a => a.CallRecordId,
                                  cr => cr.Id,
                                  (a, cr) => new { cr.CallNumber, a.AssignedFrom })
                            .Where(x => dialedNumbersList.Contains(x.CallNumber ?? "") ||
                                       (hasSubscription && (x.CallNumber == null || x.CallNumber == "")))
                            .GroupBy(x => x.CallNumber ?? "")
                            .Select(g => new
                            {
                                DialedNumber = g.Key == "" ? "Subscription" : g.Key,
                                AssignmentCount = g.Count(),
                                AssignedFromUser = g.Select(x => x.AssignedFrom).Distinct().Count() == 1
                                    ? g.Select(x => x.AssignedFrom).FirstOrDefault()
                                    : null
                            })
                            .ToDictionaryAsync(x => x.DialedNumber, x => x);

                        foreach (var dn in dialedNumbers)
                        {
                            if (assignmentCounts.TryGetValue(dn.DialedNumber, out var assignmentInfo))
                            {
                                dn.IncomingAssignmentCount = assignmentInfo.AssignmentCount;
                                dn.AssignedFromUser = assignmentInfo.AssignedFromUser;
                            }
                        }

                        // Outgoing counts - use ExtensionNumber (no Include/UserPhone JOIN)
                        var outgoingCounts = await _context.Set<CallLogPaymentAssignment>()
                            .Where(a => a.AssignedFrom == userIndexNumber && a.AssignmentStatus == "Pending")
                            .Join(filteredCallRecords,
                                  a => a.CallRecordId,
                                  cr => cr.Id,
                                  (a, cr) => new { cr.CallNumber, a.AssignedTo })
                            .Where(x => dialedNumbersList.Contains(x.CallNumber ?? "") ||
                                       (hasSubscription && (x.CallNumber == null || x.CallNumber == "")))
                            .GroupBy(x => x.CallNumber ?? "")
                            .Select(g => new
                            {
                                DialedNumber = g.Key == "" ? "Subscription" : g.Key,
                                OutgoingCount = g.Count(),
                                AssignedToUser = g.Select(x => x.AssignedTo).Distinct().Count() == 1
                                    ? g.Select(x => x.AssignedTo).FirstOrDefault()
                                    : null
                            })
                            .ToDictionaryAsync(x => x.DialedNumber, x => x);

                        foreach (var dn in dialedNumbers)
                        {
                            if (outgoingCounts.TryGetValue(dn.DialedNumber, out var outgoingInfo))
                            {
                                dn.OutgoingPendingCount = outgoingInfo.OutgoingCount;
                                dn.AssignedToUser = outgoingInfo.AssignedToUser;
                            }
                        }
                    }
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
        /// </summary>
        public async Task<JsonResult> OnGetCallLogsAsync(
            string? extension, int month, int year, string? dialedNumber,
            int callLogPage = 1, int callLogPageSize = 10,
            string sortBy = "CallDate", bool sortDesc = true)
        {
            try
            {
                // Validate required parameters
                if (string.IsNullOrEmpty(extension))
                    return new JsonResult(new { error = "Extension is required" });

                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                    return new JsonResult(new { error = "User not authenticated" });

                var ebillUser = await _context.EbillUsers
                    .FirstOrDefaultAsync(u => u.Email == user.Email);

                bool isAdmin = User.IsInRole("Admin");
                string? userIndexNumber = ebillUser?.IndexNumber;

                if (ebillUser == null && !isAdmin)
                    return new JsonResult(new { error = "User profile not found" });

                // Build base query - filter by extension (indexed), month, year
                var query = _context.CallRecords
                    .Where(c => c.ExtensionNumber == extension &&
                               c.CallMonth == month && c.CallYear == year &&
                               (c.CallDate.Year > 1 || (c.CallType != null && EF.Functions.Like(c.CallType, "Corporate Value Pack Data%"))));

                // Filter by dialed number
                // If dialedNumber is empty/null or "Subscription", filter for records with blank dialed numbers (subscriptions)
                if (string.IsNullOrEmpty(dialedNumber) || dialedNumber == "Subscription" || dialedNumber == "Unknown")
                {
                    query = query.Where(c => c.CallNumber == null || c.CallNumber == "");
                }
                else
                {
                    query = query.Where(c => c.CallNumber == dialedNumber);
                }

                // Filter by user if not admin - use Any() for efficient NOT EXISTS/EXISTS in SQL
                Dictionary<int, string> assignmentData = new Dictionary<int, string>(); // CallRecordId -> AssignedFrom
                Dictionary<int, (string Status, string AssignedTo)> outgoingAssignmentData = new Dictionary<int, (string, string)>();

                if (!isAdmin && !string.IsNullOrEmpty(userIndexNumber))
                {
                    var userIndex = userIndexNumber; // Capture for lambda
                    // Filter using Any() which translates to efficient EXISTS/NOT EXISTS in SQL
                    query = query.Where(c =>
                        (c.ResponsibleIndexNumber == userIndex &&
                         !_context.Set<CallLogPaymentAssignment>().Any(a =>
                            a.CallRecordId == c.Id && a.AssignedFrom == userIndex && a.AssignmentStatus == "Accepted")) ||
                        _context.Set<CallLogPaymentAssignment>().Any(a =>
                            a.CallRecordId == c.Id && a.AssignedTo == userIndex &&
                            (a.AssignmentStatus == "Pending" || a.AssignmentStatus == "Accepted")));
                }

                var totalCount = await query.CountAsync();
                var totalPages = (int)Math.Ceiling(totalCount / (double)callLogPageSize);

                // Apply sorting
                IOrderedQueryable<CallRecord> orderedQuery = sortBy?.ToLower() switch
                {
                    "duration" => sortDesc ? query.OrderByDescending(c => c.CallDuration) : query.OrderBy(c => c.CallDuration),
                    "costksh" => sortDesc ? query.OrderByDescending(c => c.CallCostKSHS) : query.OrderBy(c => c.CallCostKSHS),
                    "costusd" => sortDesc ? query.OrderByDescending(c => c.CallCostUSD) : query.OrderBy(c => c.CallCostUSD),
                    "type" => sortDesc ? query.OrderByDescending(c => c.CallType) : query.OrderBy(c => c.CallType),
                    "status" => sortDesc ? query.OrderByDescending(c => c.IsVerified).ThenByDescending(c => c.VerificationType)
                                         : query.OrderBy(c => c.IsVerified).ThenBy(c => c.VerificationType),
                    _ => sortDesc ? query.OrderByDescending(c => c.CallDate) : query.OrderBy(c => c.CallDate) // Default: CallDate
                };

                var callRecords = await orderedQuery
                    .Skip((callLogPage - 1) * callLogPageSize)
                    .Take(callLogPageSize)
                    .ToListAsync();

                // Get data for these specific calls only (much more efficient)
                var callIds = callRecords.Select(c => c.Id).ToList();

                // Fetch verification, incoming assignment, and outgoing assignment data sequentially
                // (DbContext is not thread-safe, cannot run concurrent queries)
                var verificationData = await _context.CallLogVerifications
                    .Where(v => callIds.Contains(v.CallRecordId) && v.SubmittedToSupervisor)
                    .Select(v => new { v.CallRecordId, v.ApprovalStatus })
                    .ToDictionaryAsync(v => v.CallRecordId, v => v.ApprovalStatus);

                assignmentData = !isAdmin && !string.IsNullOrEmpty(userIndexNumber)
                    ? await _context.Set<CallLogPaymentAssignment>()
                        .Where(a => callIds.Contains(a.CallRecordId) &&
                               a.AssignedTo == userIndexNumber &&
                               (a.AssignmentStatus == "Pending" || a.AssignmentStatus == "Accepted"))
                        .ToDictionaryAsync(a => a.CallRecordId, a => a.AssignedFrom ?? "Unknown")
                    : new Dictionary<int, string>();

                outgoingAssignmentData = !isAdmin && !string.IsNullOrEmpty(userIndexNumber)
                    ? await _context.Set<CallLogPaymentAssignment>()
                        .Where(a => callIds.Contains(a.CallRecordId) &&
                               a.AssignedFrom == userIndexNumber &&
                               (a.AssignmentStatus == "Pending" || a.AssignmentStatus == "Accepted"))
                        .ToDictionaryAsync(a => a.CallRecordId, a => (a.AssignmentStatus, a.AssignedTo ?? "Unknown"))
                    : new Dictionary<int, (string, string)>();
                var assignedCallIds = assignmentData.Keys.ToHashSet();

                // Convert to CallLogItemDto
                var callLogs = callRecords.Select(c => new CallLogItemDto
                {
                    Id = c.Id,
                    DialedNumber = string.IsNullOrEmpty(c.CallNumber) ? "Subscription" : c.CallNumber,
                    CallDate = c.CallDate,
                    CallEndTime = c.CallEndTime,
                    CallDuration = c.CallDuration,
                    CallCostUSD = c.CallCostUSD,
                    CallCostKSH = c.CallCostKSHS,
                    Destination = c.CallDestination ?? "",
                    CallType = c.CallType ?? "",
                    VerificationType = c.VerificationType ?? "",
                    IsVerified = c.IsVerified,
                    SupervisorApprovalStatus = c.SupervisorApprovalStatus,
                    IsSubmittedToSupervisor = verificationData.ContainsKey(c.Id),
                    AssignmentStatus = assignedCallIds.Contains(c.Id) ? "assigned" :
                                      (outgoingAssignmentData.TryGetValue(c.Id, out var outgoing) && outgoing.Item1 == "Pending" ? "assigned_out_pending" : "own"),
                    AssignedFrom = assignmentData.TryGetValue(c.Id, out var fromUser) ? fromUser : null,
                    AssignedTo = outgoingAssignmentData.TryGetValue(c.Id, out var outgoingData) ? outgoingData.Item2 : null,
                    IsLocked = c.SupervisorApprovalStatus == "Approved"
                }).ToList();

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
        /// Used for bulk submit to supervisor when content is not expanded
        /// </summary>
        public async Task<JsonResult> OnGetCallIdsForExtensionAsync(string? extension, int month, int year)
        {
            try
            {
                // Validate required parameters
                if (string.IsNullOrEmpty(extension))
                    return new JsonResult(new { error = "Extension is required" });

                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                    return new JsonResult(new { error = "Unauthorized" });

                var ebillUser = await _context.EbillUsers
                    .FirstOrDefaultAsync(u => u.Email == user.Email);

                if (ebillUser == null)
                    return new JsonResult(new { error = "User not found" });

                var userIndexNumber = ebillUser.IndexNumber;
                var userIndex = userIndexNumber; // Capture for lambda

                // Query call records for this extension/month/year using Any() for efficient SQL
                // Include calls that are Official OR unverified (unverified defaults to Official)
                // Exclude calls that have been assigned out and accepted
                var query = _context.CallRecords
                    .Where(c => c.ExtensionNumber == extension
                           && c.CallMonth == month
                           && c.CallYear == year
                           && (c.CallDate.Year > 1 || (c.CallType != null && EF.Functions.Like(c.CallType, "Corporate Value Pack Data%"))) // Filter out invalid dates but keep Corporate Value Pack Data
                           && c.VerificationType != "Personal") // Exclude only Personal calls
                    .Where(c =>
                        (c.ResponsibleIndexNumber == userIndex &&
                         !_context.Set<CallLogPaymentAssignment>().Any(a =>
                            a.CallRecordId == c.Id && a.AssignedFrom == userIndex && a.AssignmentStatus == "Accepted")) ||
                        _context.Set<CallLogPaymentAssignment>().Any(a =>
                            a.CallRecordId == c.Id && a.AssignedTo == userIndex &&
                            (a.AssignmentStatus == "Pending" || a.AssignmentStatus == "Accepted")));

                // Get all matching call IDs
                var allCallIds = await query.Select(c => c.Id).ToListAsync();

                // Get IDs of calls that are already submitted and pending/approved (exclude these)
                var submittedPendingIds = await _context.CallLogVerifications
                    .Where(v => allCallIds.Contains(v.CallRecordId)
                           && v.SubmittedToSupervisor
                           && (v.ApprovalStatus == "Pending" || v.ApprovalStatus == "Approved"))
                    .Select(v => v.CallRecordId)
                    .ToListAsync();

                // Exclude already submitted calls
                var callIds = allCallIds.Except(submittedPendingIds).ToList();

                return new JsonResult(new { callIds, skippedCount = submittedPendingIds.Count });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { error = $"Error fetching call IDs: {ex.Message}", callIds = new List<int>() });
            }
        }

        /// <summary>
        /// AJAX endpoint to get all call IDs for a specific dialed number within extension/month/year
        /// Used for bulk submit to supervisor when content is not expanded
        /// </summary>
        public async Task<JsonResult> OnGetCallIdsForDialedNumberAsync(string? extension, int month, int year, string? dialedNumber)
        {
            try
            {
                // Validate required parameters
                if (string.IsNullOrEmpty(extension))
                    return new JsonResult(new { error = "Extension is required", callIds = new List<int>() });

                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                    return new JsonResult(new { error = "Unauthorized", callIds = new List<int>() });

                var ebillUser = await _context.EbillUsers
                    .FirstOrDefaultAsync(u => u.Email == user.Email);

                if (ebillUser == null)
                    return new JsonResult(new { error = "User not found", callIds = new List<int>() });

                var userIndexNumber = ebillUser.IndexNumber;
                var userIndex = userIndexNumber; // Capture for lambda

                // Query call records for this extension/month/year/dialedNumber using Any() for efficient SQL
                // Include calls that are Official OR unverified (unverified defaults to Official)
                // Exclude calls that have been assigned out and accepted
                var query = _context.CallRecords
                    .Where(c => c.ExtensionNumber == extension
                           && c.CallMonth == month
                           && c.CallYear == year
                           && (c.CallDate.Year > 1 || (c.CallType != null && EF.Functions.Like(c.CallType, "Corporate Value Pack Data%"))) // Filter out invalid dates but keep Corporate Value Pack Data
                           && c.VerificationType != "Personal") // Exclude only Personal calls
                    .Where(c =>
                        (c.ResponsibleIndexNumber == userIndex &&
                         !_context.Set<CallLogPaymentAssignment>().Any(a =>
                            a.CallRecordId == c.Id && a.AssignedFrom == userIndex && a.AssignmentStatus == "Accepted")) ||
                        _context.Set<CallLogPaymentAssignment>().Any(a =>
                            a.CallRecordId == c.Id && a.AssignedTo == userIndex &&
                            (a.AssignmentStatus == "Pending" || a.AssignmentStatus == "Accepted")));

                // Filter by dialed number - handle "Subscription" for empty/null dialed numbers
                if (string.IsNullOrEmpty(dialedNumber) || dialedNumber == "Subscription")
                {
                    query = query.Where(c => c.CallNumber == null || c.CallNumber == "");
                }
                else
                {
                    query = query.Where(c => c.CallNumber == dialedNumber);
                }

                // Get all matching call IDs
                var allCallIds = await query.Select(c => c.Id).ToListAsync();

                // Get IDs of calls that are already submitted and pending/approved (exclude these)
                var submittedPendingIds = await _context.CallLogVerifications
                    .Where(v => allCallIds.Contains(v.CallRecordId)
                           && v.SubmittedToSupervisor
                           && (v.ApprovalStatus == "Pending" || v.ApprovalStatus == "Approved"))
                    .Select(v => v.CallRecordId)
                    .ToListAsync();

                // Exclude already submitted calls
                var callIds = allCallIds.Except(submittedPendingIds).ToList();

                return new JsonResult(new { callIds, skippedCount = submittedPendingIds.Count });
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

                // Get or create verifications for each call record
                var verificationIds = new List<int>();
                var alreadySubmittedCalls = new List<int>();

                foreach (var callRecordId in callRecordIds)
                {
                    var existingVerification = await _context.CallLogVerifications
                        .FirstOrDefaultAsync(v => v.CallRecordId == callRecordId && v.VerifiedBy == ebillUser.IndexNumber);

                    if (existingVerification != null)
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
                        _context.CallLogVerifications.Add(newVerification);
                        await _context.SaveChangesAsync();
                        verificationIds.Add(newVerification.Id);
                    }
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
        /// </summary>
        private async Task<List<SubmissionPhoneOverageDto>> CalculatePhoneLevelOveragesAsync(
            List<CallRecord> calls, int callMonth, int callYear)
        {
            var phoneOverages = new List<SubmissionPhoneOverageDto>();

            var callsByPhone = calls
                .Where(c => c.UserPhoneId.HasValue)
                .GroupBy(c => c.UserPhoneId.Value)
                .ToList();

            foreach (var phoneGroup in callsByPhone)
            {
                var userPhoneId = phoneGroup.Key;
                var phoneCalls = phoneGroup.ToList();
                var userPhone = phoneCalls.First().UserPhone;
                if (userPhone == null) continue;

                var allowanceLimit = await _calculationService.GetPhoneAllowanceLimitAsync(userPhoneId);
                if (allowanceLimit == null || allowanceLimit == 0) continue;

                var totalUsage = await _calculationService.GetPhoneMonthlyUsageAsync(userPhoneId, callMonth, callYear);

                var existingJustification = await _context.PhoneOverageJustifications
                    .Include(j => j.Documents)
                    .FirstOrDefaultAsync(j =>
                        j.UserPhoneId == userPhoneId &&
                        j.Month == callMonth &&
                        j.Year == callYear);

                var hasOverage = allowanceLimit.Value > 0 && totalUsage > allowanceLimit.Value;
                var overageAmount = hasOverage ? totalUsage - allowanceLimit.Value : 0;

                phoneOverages.Add(new SubmissionPhoneOverageDto
                {
                    UserPhoneId = userPhoneId,
                    PhoneNumber = userPhone.PhoneNumber,
                    PhoneType = userPhone.PhoneType,
                    ClassOfService = userPhone.ClassOfService?.Class,
                    AllowanceLimit = allowanceLimit.Value,
                    TotalUsage = totalUsage,
                    OverageAmount = overageAmount,
                    HasOverage = hasOverage,
                    CallCount = phoneCalls.Count,
                    HasExistingJustification = existingJustification != null,
                    ExistingJustificationText = existingJustification?.JustificationText,
                    ExistingJustificationDate = existingJustification?.SubmittedDate.ToString("MMMM dd, yyyy"),
                    ExistingJustificationStatus = existingJustification?.ApprovalStatus,
                    ExistingDocumentCount = existingJustification?.Documents?.Count ?? 0
                });
            }

            return phoneOverages.OrderByDescending(p => p.OverageAmount).ToList();
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
