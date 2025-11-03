using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TAB.Web.Data;
using TAB.Web.Models;
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

        public MyCallLogsModel(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ICallLogVerificationService verificationService,
            IClassOfServiceCalculationService calculationService)
        {
            _context = context;
            _userManager = userManager;
            _verificationService = verificationService;
            _calculationService = calculationService;
        }

        // Properties
        public List<CallRecord> CallRecords { get; set; } = new();
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

        // Pagination
        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        public int PageSize { get; set; } = 50;
        public int TotalPages { get; set; }
        public int TotalRecords { get; set; }

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

            // Check if user is Admin - admins can see all records even without employee profile
            bool isAdmin = User.IsInRole("Admin");

            if (ebillUser == null && !isAdmin)
            {
                StatusMessage = "Your profile is not linked to an employee record. Please contact the administrator.";
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
                .Include(c => c.PayingUser) // For assigned calls
                .Include(c => c.ResponsibleUser) // For responsible user info
                .AsQueryable();

            // Will load verifications AFTER we know which call IDs we need (after pagination)
            HashSet<int> assignedCallIds = new HashSet<int>();

            // Filter by UserIndexNumber only if not admin
            if (!isAdmin && !string.IsNullOrEmpty(UserIndexNumber))
            {
                // Get payment assignments for this user - use subquery instead of loading into memory
                var assignmentList = await _context.Set<CallLogPaymentAssignment>()
                    .Where(a => a.AssignedTo == UserIndexNumber &&
                           (a.AssignmentStatus == "Pending" || a.AssignmentStatus == "Accepted"))
                    .Select(a => a.CallRecordId)
                    .ToListAsync();

                // Store for later use
                assignedCallIds = assignmentList.ToHashSet();

                // Apply assignment type filter
                if (!string.IsNullOrEmpty(FilterAssignmentType))
                {
                    switch (FilterAssignmentType.ToLower())
                    {
                        case "own":
                            // Only calls where user is responsible and no payment assignment
                            query = query.Where(c => c.ResponsibleIndexNumber == UserIndexNumber &&
                                                   (c.PaymentAssignmentId == null || !assignedCallIds.Contains(c.Id)));
                            break;
                        case "assigned":
                            // Only calls assigned to this user for payment
                            query = query.Where(c => assignedCallIds.Contains(c.Id));
                            break;
                        default: // "all" or empty
                            // Both own calls and assigned calls
                            query = query.Where(c => c.ResponsibleIndexNumber == UserIndexNumber || assignedCallIds.Contains(c.Id));
                            break;
                    }
                }
                else
                {
                    // Default: show both own calls and assigned calls
                    query = query.Where(c => c.ResponsibleIndexNumber == UserIndexNumber || assignedCallIds.Contains(c.Id));
                }
            }

            // Apply filters - allow filtering by month OR year independently
            if (FilterMonth.HasValue)
            {
                query = query.Where(c => c.CallMonth == FilterMonth.Value);
            }

            if (FilterYear.HasValue)
            {
                query = query.Where(c => c.CallYear == FilterYear.Value);
            }

            if (FilterStartDate.HasValue)
            {
                query = query.Where(c => c.CallDate >= FilterStartDate.Value);
            }

            if (FilterEndDate.HasValue)
            {
                query = query.Where(c => c.CallDate <= FilterEndDate.Value);
            }

            if (FilterMinCost.HasValue)
            {
                query = query.Where(c => c.CallCostUSD >= FilterMinCost.Value);
            }

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

            // Get total count before pagination
            TotalRecords = await query.CountAsync();
            TotalPages = (int)Math.Ceiling(TotalRecords / (double)PageSize);

            // Apply sorting - Group by Year/Month first, then PhoneNumber, then apply secondary sort
            query = SortBy?.ToLower() switch
            {
                "calldate" => SortDescending
                    ? query.OrderByDescending(c => c.CallYear).ThenByDescending(c => c.CallMonth).ThenBy(c => c.UserPhone != null ? c.UserPhone.PhoneNumber : "").ThenByDescending(c => c.CallDate)
                    : query.OrderBy(c => c.CallYear).ThenBy(c => c.CallMonth).ThenBy(c => c.UserPhone != null ? c.UserPhone.PhoneNumber : "").ThenBy(c => c.CallDate),
                "cost" => SortDescending
                    ? query.OrderByDescending(c => c.CallYear).ThenByDescending(c => c.CallMonth).ThenBy(c => c.UserPhone != null ? c.UserPhone.PhoneNumber : "").ThenByDescending(c => c.CallCostUSD)
                    : query.OrderBy(c => c.CallYear).ThenBy(c => c.CallMonth).ThenBy(c => c.UserPhone != null ? c.UserPhone.PhoneNumber : "").ThenBy(c => c.CallCostUSD),
                "duration" => SortDescending
                    ? query.OrderByDescending(c => c.CallYear).ThenByDescending(c => c.CallMonth).ThenBy(c => c.UserPhone != null ? c.UserPhone.PhoneNumber : "").ThenByDescending(c => c.CallDuration)
                    : query.OrderBy(c => c.CallYear).ThenBy(c => c.CallMonth).ThenBy(c => c.UserPhone != null ? c.UserPhone.PhoneNumber : "").ThenBy(c => c.CallDuration),
                "destination" => SortDescending
                    ? query.OrderByDescending(c => c.CallYear).ThenByDescending(c => c.CallMonth).ThenBy(c => c.UserPhone != null ? c.UserPhone.PhoneNumber : "").ThenByDescending(c => c.CallDestination)
                    : query.OrderBy(c => c.CallYear).ThenBy(c => c.CallMonth).ThenBy(c => c.UserPhone != null ? c.UserPhone.PhoneNumber : "").ThenBy(c => c.CallDestination),
                _ => query.OrderByDescending(c => c.CallYear).ThenByDescending(c => c.CallMonth).ThenBy(c => c.UserPhone != null ? c.UserPhone.PhoneNumber : "").ThenByDescending(c => c.CallDate)
            };

            // Apply pagination
            CallRecords = await query
                .Skip((PageNumber - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            // NOW load verification data ONLY for the calls we're displaying (much faster!)
            if (CallRecords.Any())
            {
                var displayedCallIds = CallRecords.Select(c => c.Id).ToList();

                var verificationsData = await _context.CallLogVerifications
                    .Where(v => displayedCallIds.Contains(v.CallRecordId) && v.SubmittedToSupervisor)
                    .Select(v => new { v.CallRecordId, v.ApprovalStatus })
                    .ToListAsync();

                // Create lookup dictionaries
                SubmittedCallIds = verificationsData.Select(v => v.CallRecordId).ToHashSet();
                VerificationStatuses = verificationsData.ToDictionary(v => v.CallRecordId, v => v.ApprovalStatus);
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

            // Get all call records for this user INCLUDING assigned calls (same logic as LoadCallRecordsAsync)
            var assignedCallIds = await _context.Set<CallLogPaymentAssignment>()
                .Where(a => a.AssignedTo == UserIndexNumber &&
                       (a.AssignmentStatus == "Pending" || a.AssignmentStatus == "Accepted"))
                .Select(a => a.CallRecordId)
                .ToListAsync();

            // Build query for user's calls (own + assigned)
            var userQuery = _context.CallRecords
                .Where(c => c.ResponsibleIndexNumber == UserIndexNumber || assignedCallIds.Contains(c.Id));

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
                StatusMessage = "Your profile is not linked to an employee record.";
                StatusMessageClass = "danger";
                return RedirectToPage();
            }

            try
            {
                int verifiedCount = 0;
                var verificationTypeEnum = (Models.Enums.VerificationType)Enum.Parse(
                    typeof(Models.Enums.VerificationType), verificationType);

                foreach (var callRecordId in selectedIds)
                {
                    try
                    {
                        // Quick verify without justification required
                        await _verificationService.VerifyCallLogAsync(
                            callRecordId,
                            ebillUser.IndexNumber,
                            verificationTypeEnum,
                            justification: $"Quick verified as {verificationType}");
                        verifiedCount++;
                    }
                    catch (Exception ex)
                    {
                        // Log but continue with other verifications
                        // Skip failed verification and continue
                    }
                }

                StatusMessage = $"Successfully marked {verifiedCount} of {selectedIds.Count} call(s) as {verificationType}.";
                StatusMessageClass = "success";
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
                StatusMessage = "Your profile is not linked to an employee record.";
                StatusMessageClass = "danger";
                return RedirectToPage();
            }

            try
            {
                int verifiedCount = 0;
                var verificationTypeEnum = (Models.Enums.VerificationType)Enum.Parse(
                    typeof(Models.Enums.VerificationType), verificationType);

                foreach (var callRecordId in selectedIds)
                {
                    try
                    {
                        await _verificationService.VerifyCallLogAsync(
                            callRecordId,
                            ebillUser.IndexNumber,
                            verificationTypeEnum);
                        verifiedCount++;
                    }
                    catch (Exception ex)
                    {
                        // Log but continue with other verifications
                        Console.WriteLine($"Error verifying call {callRecordId}: {ex.Message}");
                    }
                }

                StatusMessage = $"Successfully verified {verifiedCount} of {selectedIds.Count} calls.";
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
                StatusMessage = "Your profile is not linked to an employee record.";
                StatusMessageClass = "danger";
                return RedirectToPage();
            }

            try
            {
                var dialedNumber = Request.Form["dialedNumber"].ToString();
                var assignToIndexNumber = Request.Form["assignToIndexNumber"].ToString();
                var assignmentReason = Request.Form["assignmentReason"].ToString();

                if (string.IsNullOrWhiteSpace(dialedNumber) ||
                    string.IsNullOrWhiteSpace(assignToIndexNumber) ||
                    string.IsNullOrWhiteSpace(assignmentReason))
                {
                    StatusMessage = "Please provide all required information for reassignment.";
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

                // Get all calls for this dialed number for the current user in the current month/year
                var callsToReassign = await _context.CallRecords
                    .Where(c => c.CallNumber == dialedNumber &&
                               c.ResponsibleIndexNumber == ebillUser.IndexNumber &&
                               c.CallMonth == firstCall.CallMonth &&
                               c.CallYear == firstCall.CallYear &&
                               string.IsNullOrEmpty(c.VerificationType)) // Only unverified calls
                    .ToListAsync();

                if (!callsToReassign.Any())
                {
                    StatusMessage = "No unverified calls found for this number.";
                    StatusMessageClass = "warning";
                    return RedirectToPage();
                }

                // Create payment assignments for each call using the service
                int successCount = 0;
                foreach (var call in callsToReassign)
                {
                    try
                    {
                        await _verificationService.AssignPaymentAsync(
                            call.Id,
                            ebillUser.IndexNumber,
                            assignToIndexNumber,
                            assignmentReason);
                        successCount++;
                    }
                    catch (Exception assignEx)
                    {
                        // Log individual assignment errors but continue with others
                        Console.WriteLine($"Error assigning call {call.Id}: {assignEx.Message}");
                    }
                }

                StatusMessage = $"Successfully reassigned {successCount} call(s) to the selected user!";
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
    }

    // DTO for rejection request
    public class RejectAssignmentRequest
    {
        public int CallRecordId { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}
