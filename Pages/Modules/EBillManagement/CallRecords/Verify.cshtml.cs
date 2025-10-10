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
    public class VerifyModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ICallLogVerificationService _verificationService;
        private readonly IClassOfServiceCalculationService _calculationService;
        private readonly IDocumentManagementService _documentService;

        public VerifyModel(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ICallLogVerificationService verificationService,
            IClassOfServiceCalculationService calculationService,
            IDocumentManagementService documentService)
        {
            _context = context;
            _userManager = userManager;
            _verificationService = verificationService;
            _calculationService = calculationService;
            _documentService = documentService;
        }

        public CallRecord CallRecord { get; set; } = null!;
        public string UserIndexNumber { get; set; } = string.Empty;
        public ClassOfService? ClassOfService { get; set; }
        public decimal AllowanceLimit { get; set; }
        public decimal CurrentUsage { get; set; }
        public decimal PendingVerification { get; set; }
        public decimal RemainingAllowance { get; set; }
        public bool WillExceedAllowance { get; set; }
        public bool IsOverAllowance { get; set; }
        public List<EbillUser> AvailableUsers { get; set; } = new();

        // New properties for grouped call view
        public List<CallRecord> AllExtensionCalls { get; set; } = new();
        public List<GroupedCallSummary> GroupedCalls { get; set; } = new();
        public string ExtensionNumber { get; set; } = string.Empty;
        public int CallMonth { get; set; }
        public int CallYear { get; set; }

        [BindProperty]
        public string VerificationType { get; set; } = "Official";

        [BindProperty]
        public string? JustificationText { get; set; }

        [BindProperty]
        public List<IFormFile>? Documents { get; set; }

        [BindProperty]
        public bool AssignPayment { get; set; }

        [BindProperty]
        public string? AssignToIndexNumber { get; set; }

        [BindProperty]
        public string? AssignmentReason { get; set; }

        [TempData]
        public string? StatusMessage { get; set; }

        [TempData]
        public string? StatusMessageClass { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
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
                return RedirectToPage("/Modules/EBillManagement/CallRecords/MyCallLogs");
            }

            UserIndexNumber = ebillUser.IndexNumber;

            // Load the specific call record (for reference)
            CallRecord = await _context.CallRecords
                .Include(c => c.ResponsibleUser)
                .Include(c => c.UserPhone)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (CallRecord == null)
            {
                StatusMessage = "Call record not found.";
                StatusMessageClass = "danger";
                return RedirectToPage("/Modules/EBillManagement/CallRecords/MyCallLogs");
            }

            // Verify ownership - User can verify if they are either the responsible user OR the paying user (assigned and accepted)
            bool isResponsibleUser = CallRecord.ResponsibleIndexNumber == UserIndexNumber;
            bool isPayingUser = CallRecord.PayingIndexNumber == UserIndexNumber &&
                               CallRecord.AssignmentStatus == "Accepted";

            if (!isResponsibleUser && !isPayingUser)
            {
                StatusMessage = "You can only verify call records that belong to you or have been assigned to you and accepted.";
                StatusMessageClass = "danger";
                return RedirectToPage("/Modules/EBillManagement/CallRecords/MyCallLogs");
            }

            // Set extension info
            ExtensionNumber = CallRecord.ExtensionNumber;
            CallMonth = CallRecord.CallMonth;
            CallYear = CallRecord.CallYear;

            // Load ALL calls for this extension in the same month
            // Include calls where user is responsible OR calls assigned to them (accepted)
            AllExtensionCalls = await _context.CallRecords
                .Where(c => c.ExtensionNumber == ExtensionNumber &&
                           c.CallMonth == CallMonth &&
                           c.CallYear == CallYear &&
                           (c.ResponsibleIndexNumber == UserIndexNumber ||
                            (c.PayingIndexNumber == UserIndexNumber && c.AssignmentStatus == "Accepted")))
                .OrderBy(c => c.CallDate)
                .ToListAsync();

            // Group calls by dialed number
            GroupedCalls = AllExtensionCalls
                .GroupBy(c => c.CallNumber)
                .Select(g => new GroupedCallSummary
                {
                    DialedNumber = g.Key,
                    ContactName = "", // Can be populated from a contacts table if you have one
                    CallCount = g.Count(),
                    TotalDurationMinutes = g.Sum(c => c.CallDuration) / 60.0m,
                    TotalCostUSD = g.Sum(c => c.CallCostUSD),
                    TotalCostKSH = g.Sum(c => c.CallCostKSHS),
                    Calls = g.OrderBy(c => c.CallDate).ToList()
                })
                .OrderByDescending(g => g.TotalCostUSD)
                .ToList();

            // Load class of service and allowance info
            ClassOfService = await _calculationService.GetUserClassOfServiceAsync(UserIndexNumber);
            var limitNullable = await _calculationService.GetAllowanceLimitAsync(UserIndexNumber);
            AllowanceLimit = limitNullable ?? 0; // Unlimited = 0 for display
            CurrentUsage = await _calculationService.GetMonthlyUsageAsync(
                UserIndexNumber,
                CallMonth,
                CallYear);
            PendingVerification = await _calculationService.GetPendingVerificationAsync(
                UserIndexNumber,
                CallMonth,
                CallYear);
            RemainingAllowance = limitNullable.HasValue && limitNullable.Value > 0 ? Math.Max(0, limitNullable.Value - CurrentUsage - PendingVerification) : decimal.MaxValue;
            WillExceedAllowance = limitNullable.HasValue && limitNullable.Value > 0 && (CurrentUsage + PendingVerification) > limitNullable.Value;
            IsOverAllowance = limitNullable.HasValue && limitNullable.Value > 0 && (CurrentUsage + PendingVerification) > limitNullable.Value;

            // Load users for payment assignment
            AvailableUsers = await _context.EbillUsers
                .Where(u => u.IsActive && u.IndexNumber != UserIndexNumber)
                .OrderBy(u => u.FirstName)
                .ThenBy(u => u.LastName)
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
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
                return RedirectToPage("/Modules/EBillManagement/CallRecords/MyCallLogs");
            }

            try
            {
                // Parse verification type
                var verificationTypeEnum = (VerificationType)Enum.Parse(typeof(VerificationType), VerificationType);

                // Verify the call log
                var verification = await _verificationService.VerifyCallLogAsync(
                    id,
                    ebillUser.IndexNumber,
                    verificationTypeEnum,
                    JustificationText,
                    Documents);

                // Handle payment assignment if requested
                if (AssignPayment && !string.IsNullOrEmpty(AssignToIndexNumber))
                {
                    if (string.IsNullOrWhiteSpace(AssignmentReason))
                    {
                        StatusMessage = "Please provide a reason for payment assignment.";
                        StatusMessageClass = "warning";
                        return await OnGetAsync(id);
                    }

                    await _verificationService.AssignPaymentAsync(
                        id,
                        ebillUser.IndexNumber,
                        AssignToIndexNumber,
                        AssignmentReason);

                    StatusMessage = $"Call verified as {VerificationType} and payment assigned successfully!";
                }
                else
                {
                    StatusMessage = $"Call verified successfully as {VerificationType}!";
                }

                StatusMessageClass = "success";
                return RedirectToPage("/Modules/EBillManagement/CallRecords/MyCallLogs");
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error verifying call: {ex.Message}";
                StatusMessageClass = "danger";
                return await OnGetAsync(id);
            }
        }

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

        public async Task<IActionResult> OnPostBulkVerifyAsync([FromBody] BulkVerifyRequest request)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            var ebillUser = await _context.EbillUsers
                .FirstOrDefaultAsync(u => u.Email == user.Email);

            if (ebillUser == null)
                return BadRequest("Your profile is not linked to an employee record.");

            try
            {
                var verificationType = (VerificationType)Enum.Parse(typeof(VerificationType), request.VerificationType);

                foreach (var callId in request.CallIds)
                {
                    await _verificationService.VerifyCallLogAsync(
                        callId,
                        ebillUser.IndexNumber,
                        verificationType,
                        null, // No justification for bulk verify
                        null  // No documents for bulk verify
                    );
                }

                return new JsonResult(new { success = true, message = $"Successfully verified {request.CallIds.Count} calls." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        public async Task<IActionResult> OnPostReassignCallsAsync(int id)
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
                return RedirectToPage("/Modules/EBillManagement/CallRecords/MyCallLogs");
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
                    return RedirectToPage(new { id });
                }

                // Get all calls for this dialed number for the current user in the current month/year
                // We need to get the call record first to know the month/year
                var firstCall = await _context.CallRecords.FindAsync(id);

                if (firstCall == null)
                {
                    StatusMessage = "Call record not found.";
                    StatusMessageClass = "danger";
                    return RedirectToPage("/Modules/EBillManagement/CallRecords/MyCallLogs");
                }

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
                    return RedirectToPage(new { id });
                }

                // Create payment assignments for each call using the service
                // This ensures proper status tracking and audit logging
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
                return RedirectToPage(new { id });
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error reassigning calls: {ex.Message}";
                StatusMessageClass = "danger";
                return RedirectToPage(new { id });
            }
        }
    }

    // DTO for bulk verification
    public class BulkVerifyRequest
    {
        public List<int> CallIds { get; set; } = new();
        public string VerificationType { get; set; } = string.Empty;
    }

    // DTO for grouped call summary
    public class GroupedCallSummary
    {
        public string DialedNumber { get; set; } = string.Empty;
        public string ContactName { get; set; } = string.Empty;
        public int CallCount { get; set; }
        public decimal TotalDurationMinutes { get; set; }
        public decimal TotalCostUSD { get; set; }
        public decimal TotalCostKSH { get; set; }
        public List<CallRecord> Calls { get; set; } = new();
    }
}
