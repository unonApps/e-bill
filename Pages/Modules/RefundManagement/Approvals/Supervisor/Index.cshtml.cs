using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TAB.Web.Data;
using TAB.Web.Models;
using TAB.Web.Services;

namespace TAB.Web.Pages.Modules.RefundManagement.Approvals.Supervisor
{
    [Authorize] // Authorization checked in OnGetAsync based on request assignment
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly INotificationService _notificationService;
        private readonly IAuditLogService _auditLogService;
        private readonly IEnhancedEmailService _emailService;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager, INotificationService notificationService, IAuditLogService auditLogService, IEnhancedEmailService emailService, ILogger<IndexModel> logger)
        {
            _context = context;
            _userManager = userManager;
            _notificationService = notificationService;
            _auditLogService = auditLogService;
            _emailService = emailService;
            _logger = logger;
        }

        public List<RefundRequest> PendingRequests { get; set; } = new();
        public List<RefundRequest> AllSupervisorRequests { get; set; } = new();
        public List<ApplicationUser> BudgetOfficers { get; set; } = new();
        public string CurrentUserRole { get; set; } = "";
        public bool IsDetailView { get; set; } = false;
        public RefundRequest? CurrentRequest { get; set; }
        public List<RefundRequestHistory> RequestHistory { get; set; } = new();
        public bool CanActAsSupervisor { get; set; } = false;
        public bool CanActAsBudgetOfficer { get; set; } = false;
        public bool HasSupervisorAccess { get; set; } = false; // True if user has Supervisor role OR has assigned requests

        [TempData]
        public string? StatusMessage { get; set; }

        [TempData]
        public string? StatusMessageClass { get; set; }

        public async Task<IActionResult> OnGetAsync(int? requestId = null)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToPage("/Account/Login");
            }

            // Determine user role for filtering and display logic
            var userRoles = await _userManager.GetRolesAsync(currentUser);
            CurrentUserRole = userRoles.FirstOrDefault() ?? "";

            var isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");
            var isSupervisorRole = await _userManager.IsInRoleAsync(currentUser, "Supervisor");
            var isBudgetOfficerRole = await _userManager.IsInRoleAsync(currentUser, "Budget Officer") ||
                                      await _userManager.IsInRoleAsync(currentUser, "BudgetOfficer");

            // Check if this is a detail view request
            if (requestId.HasValue)
            {
                CurrentRequest = await _context.RefundRequests.FindAsync(requestId.Value);
                if (CurrentRequest != null)
                {
                    // Authorization check for specific request:
                    // - Admin: always allowed
                    // - PendingSupervisor status: allow if SupervisorEmail matches current user (regardless of role)
                    // - Other statuses: require role-based access
                    bool isAuthorized = isAdmin;

                    if (!isAuthorized && CurrentRequest.Status == RefundRequestStatus.PendingSupervisor)
                    {
                        // For PendingSupervisor, check if user is the assigned supervisor
                        isAuthorized = CurrentRequest.SupervisorEmail != null &&
                                       CurrentRequest.SupervisorEmail.Equals(currentUser.Email, StringComparison.OrdinalIgnoreCase);
                    }

                    if (!isAuthorized)
                    {
                        // For other statuses, use role-based authorization
                        if (isSupervisorRole && CurrentRequest.SupervisorEmail != null &&
                            CurrentRequest.SupervisorEmail.Equals(currentUser.Email, StringComparison.OrdinalIgnoreCase))
                        {
                            isAuthorized = true;
                        }
                        else if (isBudgetOfficerRole && CurrentRequest.BudgetOfficerEmail != null &&
                                 CurrentRequest.BudgetOfficerEmail.Equals(currentUser.Email, StringComparison.OrdinalIgnoreCase))
                        {
                            isAuthorized = true;
                        }
                    }

                    if (!isAuthorized)
                    {
                        _logger.LogWarning("User {Email} attempted to access request {RequestId} but is not authorized. Status: {Status}, SupervisorEmail: {SupervisorEmail}",
                            currentUser.Email, requestId, CurrentRequest.Status, CurrentRequest.SupervisorEmail);
                        return RedirectToPage("/Account/AccessDenied");
                    }

                    IsDetailView = true;

                    // Load request history
                    RequestHistory = await _context.RefundRequestHistories
                        .Where(h => h.RefundRequestId == requestId.Value)
                        .OrderBy(h => h.Timestamp)
                        .ToListAsync();

                    // Set action permissions for the UI
                    // User can act as supervisor if: email matches AND (status is PendingSupervisor OR has Supervisor role)
                    if (CurrentRequest.SupervisorEmail != null &&
                        CurrentRequest.SupervisorEmail.Equals(currentUser.Email, StringComparison.OrdinalIgnoreCase))
                    {
                        if (CurrentRequest.Status == RefundRequestStatus.PendingSupervisor || isSupervisorRole)
                        {
                            CanActAsSupervisor = true;
                        }
                    }

                    // User can act as budget officer if: has role AND email matches
                    if (isBudgetOfficerRole && CurrentRequest.BudgetOfficerEmail != null &&
                        CurrentRequest.BudgetOfficerEmail.Equals(currentUser.Email, StringComparison.OrdinalIgnoreCase))
                    {
                        CanActAsBudgetOfficer = true;
                    }

                    // Admin can do everything
                    if (isAdmin)
                    {
                        CanActAsSupervisor = true;
                        CanActAsBudgetOfficer = true;
                    }
                }
            }

            // Check if user has any access to the page at all
            // Allow if: Admin, has role, OR has PendingSupervisor requests assigned to them
            var hasPendingSupervisorRequests = await _context.RefundRequests
                .AnyAsync(r => r.Status == RefundRequestStatus.PendingSupervisor &&
                              r.SupervisorEmail == currentUser.Email);

            if (!isAdmin && !isSupervisorRole && !isBudgetOfficerRole && !hasPendingSupervisorRequests)
            {
                return RedirectToPage("/Account/AccessDenied");
            }

            // Filter requests based on user role
            if (isAdmin)
            {
                // Admins see all requests
                AllSupervisorRequests = await _context.RefundRequests
                    .OrderBy(r => r.RequestDate)
                    .ToListAsync();
            }
            else if (isSupervisorRole)
            {
                // Supervisors with role see all their supervised requests
                AllSupervisorRequests = await _context.RefundRequests
                    .Where(r => r.SupervisorEmail == currentUser.Email)
                    .OrderBy(r => r.RequestDate)
                    .ToListAsync();
            }
            else if (isBudgetOfficerRole)
            {
                // Budget Officers see requests assigned to them
                AllSupervisorRequests = await _context.RefundRequests
                    .Where(r => r.BudgetOfficerEmail == currentUser.Email)
                    .OrderBy(r => r.RequestDate)
                    .ToListAsync();
            }
            else
            {
                // Users without role only see PendingSupervisor requests where they are the supervisor
                AllSupervisorRequests = await _context.RefundRequests
                    .Where(r => r.Status == RefundRequestStatus.PendingSupervisor &&
                               r.SupervisorEmail == currentUser.Email)
                    .OrderBy(r => r.RequestDate)
                    .ToListAsync();
            }

            // For backward compatibility, keep PendingRequests as all requests
            PendingRequests = AllSupervisorRequests;

            // Set HasSupervisorAccess - true if user has Supervisor role OR has assigned supervisor requests
            HasSupervisorAccess = isSupervisorRole ||
                                  AllSupervisorRequests.Any(r => r.SupervisorEmail != null &&
                                                                 r.SupervisorEmail.Equals(currentUser.Email, StringComparison.OrdinalIgnoreCase));

            // Load budget officers for the dropdown (needed for supervisors approving requests)
            if (isAdmin || HasSupervisorAccess)
            {
                BudgetOfficers = await LoadBudgetOfficersAsync();
            }

            return Page();
        }

        public async Task<IActionResult> OnPostApproveAsync(int requestId, string? supervisorRemarks, string? budgetOfficerEmail)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                StatusMessage = "User not found.";
                StatusMessageClass = "danger";
                return RedirectToPage("/Dashboard/Approver/Index");
            }

            var request = await _context.RefundRequests.FindAsync(requestId);
            if (request == null)
            {
                StatusMessage = "Request not found.";
                StatusMessageClass = "danger";
                return RedirectToPage("/Dashboard/Approver/Index");
            }

            // Verify authorization based on user role and request assignment
            var isSupervisorRole = await _userManager.IsInRoleAsync(currentUser, "Supervisor");
            var isBudgetOfficer = await _userManager.IsInRoleAsync(currentUser, "Budget Officer") ||
                                 await _userManager.IsInRoleAsync(currentUser, "BudgetOfficer");
            var isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");

            // Check if user can act as supervisor for this request:
            // - PendingSupervisor status: allow if SupervisorEmail matches (regardless of role)
            // - Other statuses: require Supervisor role AND email match
            bool canActAsSupervisor = false;
            if (request.SupervisorEmail != null &&
                request.SupervisorEmail.Equals(currentUser.Email, StringComparison.OrdinalIgnoreCase))
            {
                if (request.Status == RefundRequestStatus.PendingSupervisor)
                {
                    // For PendingSupervisor, any user whose email matches can approve
                    canActAsSupervisor = true;
                }
                else if (isSupervisorRole)
                {
                    // For other statuses, require Supervisor role
                    canActAsSupervisor = true;
                }
            }

            if (canActAsSupervisor)
            {
                // Validate budget officer selection for supervisor approval
                if (string.IsNullOrEmpty(budgetOfficerEmail))
                {
                    StatusMessage = "Please select a Budget Officer before approving.";
                    StatusMessageClass = "danger";
                    return RedirectToPage("/Dashboard/Approver/Index");
                }

                // Verify the selected budget officer exists and has the correct role
                var budgetOfficer = await _userManager.FindByEmailAsync(budgetOfficerEmail);
                if (budgetOfficer == null ||
                    (!await _userManager.IsInRoleAsync(budgetOfficer, "Budget Officer") &&
                     !await _userManager.IsInRoleAsync(budgetOfficer, "BudgetOfficer")))
                {
                    StatusMessage = "Invalid Budget Officer selected.";
                    StatusMessageClass = "danger";
                    return RedirectToPage("/Dashboard/Approver/Index");
                }

                try
                {
                    request.Status = RefundRequestStatus.PendingBudgetOfficer;
                    request.SupervisorApprovalDate = DateTime.UtcNow;
                    request.SupervisorRemarks = supervisorRemarks ?? "";
                    request.SupervisorName = $"{currentUser.FirstName} {currentUser.LastName}";
                    request.BudgetOfficerEmail = budgetOfficerEmail;
                    request.BudgetOfficerName = $"{budgetOfficer.FirstName} {budgetOfficer.LastName}";

                    await _context.SaveChangesAsync();

                    // Add history entry
                    var historyEntry = new RefundRequestHistory
                    {
                        RefundRequestId = requestId,
                        Action = RefundHistoryActions.SupervisorApproved,
                        PreviousStatus = RefundRequestStatus.PendingSupervisor.ToString(),
                        NewStatus = RefundRequestStatus.PendingBudgetOfficer.ToString(),
                        Comments = supervisorRemarks,
                        PerformedBy = currentUser.Id,
                        UserName = $"{currentUser.FirstName} {currentUser.LastName}",
                        Timestamp = DateTime.UtcNow,
                        IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
                    };
                    _context.RefundRequestHistories.Add(historyEntry);
                    await _context.SaveChangesAsync();

                    // Send notification to requester
                    await _notificationService.NotifyRefundSupervisorApprovedAsync(
                        requestId,
                        request.RequestedBy ?? "",
                        supervisorRemarks
                    );

                    // Log audit trail
                    await _auditLogService.LogRefundRequestApprovedAsync(
                        requestId,
                        "Supervisor",
                        $"{currentUser.FirstName} {currentUser.LastName}",
                        request.MobileNumberAssignedTo ?? "N/A",
                        request.DevicePurchaseAmount,
                        supervisorRemarks,
                        currentUser.Id,
                        HttpContext.Connection.RemoteIpAddress?.ToString()
                    );

                    // Send notification to Budget Officer
                    await _notificationService.NotifyNewRefundRequestPendingApprovalAsync(
                        requestId,
                        budgetOfficer.Id,
                        request.MobileNumberAssignedTo,
                        "Budget Officer"
                    );

                    // Send email notifications
                    try
                    {
                        var requester = await _userManager.FindByIdAsync(request.RequestedBy ?? "");
                        if (requester != null)
                        {
                            // 1. Send approval email to requester
                            await SendSupervisorApprovedEmailAsync(request, currentUser, requester);

                            // 2. Send notification to budget officer
                            await SendBudgetOfficerNotificationEmailAsync(request, budgetOfficer, currentUser);

                            _logger.LogInformation("Approval email notifications sent successfully for refund request {RequestId}", requestId);
                        }
                    }
                    catch (Exception emailEx)
                    {
                        _logger.LogError(emailEx, "Failed to send approval email notifications for request {RequestId}", requestId);
                    }

                    StatusMessage = $"Request #{requestId} has been approved and forwarded to {budgetOfficer.FirstName} {budgetOfficer.LastName}.";
                    StatusMessageClass = "success";
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Error approving request: {ex.Message}";
                    StatusMessageClass = "danger";
                }
            }
            else if (isBudgetOfficer)
            {
                // Budget Officers can only approve requests assigned to them
                if (request.BudgetOfficerEmail != currentUser.Email)
                {
                    StatusMessage = "You are not authorized to approve this request.";
                    StatusMessageClass = "danger";
                    return RedirectToPage("/Dashboard/Approver/Index");
                }

                try
                {
                    request.Status = RefundRequestStatus.PendingStaffClaimsUnit;
                    request.BudgetOfficerApprovalDate = DateTime.UtcNow;
                    request.BudgetOfficerRemarks = supervisorRemarks ?? ""; // Reusing the remarks field

                    await _context.SaveChangesAsync();

                    // Send notification to requester
                    await _notificationService.NotifyRefundBudgetOfficerApprovedAsync(
                        requestId,
                        request.RequestedBy ?? "",
                        supervisorRemarks
                    );

                    // Log audit trail
                    await _auditLogService.LogRefundRequestApprovedAsync(
                        requestId,
                        "Budget Officer",
                        $"{currentUser.FirstName} {currentUser.LastName}",
                        request.MobileNumberAssignedTo ?? "N/A",
                        request.DevicePurchaseAmount,
                        supervisorRemarks,
                        currentUser.Id,
                        HttpContext.Connection.RemoteIpAddress?.ToString()
                    );

                    // Send email notifications
                    try
                    {
                        var requester = await _userManager.FindByIdAsync(request.RequestedBy ?? "");
                        if (requester != null)
                        {
                            // 1. Send budget approval email to requester
                            await SendBudgetOfficerApprovedEmailAsync(request, currentUser, requester);

                            // 2. Send notification to claims unit
                            await SendClaimsUnitNotificationEmailAsync(request);

                            _logger.LogInformation("Budget approval email notifications sent successfully for refund request {RequestId}", requestId);
                        }
                    }
                    catch (Exception emailEx)
                    {
                        _logger.LogError(emailEx, "Failed to send budget approval email notifications for request {RequestId}", requestId);
                    }

                    StatusMessage = $"Request #{requestId} has been approved and forwarded to Staff Claims Unit.";
                    StatusMessageClass = "success";
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Error approving request: {ex.Message}";
                    StatusMessageClass = "danger";
                }
            }
            else if (!isAdmin)
            {
                StatusMessage = "You are not authorized to perform this action.";
                StatusMessageClass = "danger";
                return RedirectToPage("/Dashboard/Approver/Index");
            }

            // Redirect to Approver Dashboard after successful action
            // This avoids AccessDenied for users who approved their only pending request
            return RedirectToPage("/Dashboard/Approver/Index");
        }

        public async Task<IActionResult> OnPostRevertAsync(int requestId, string? supervisorRemarks)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                StatusMessage = "User not found.";
                StatusMessageClass = "danger";
                return RedirectToPage("/Dashboard/Approver/Index");
            }

            var request = await _context.RefundRequests.FindAsync(requestId);
            if (request == null)
            {
                StatusMessage = "Request not found.";
                StatusMessageClass = "danger";
                return RedirectToPage("/Dashboard/Approver/Index");
            }

            // Verify authorization based on user role and request assignment
            var isSupervisorRole = await _userManager.IsInRoleAsync(currentUser, "Supervisor");
            var isBudgetOfficer = await _userManager.IsInRoleAsync(currentUser, "Budget Officer") ||
                                 await _userManager.IsInRoleAsync(currentUser, "BudgetOfficer");
            var isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");

            // Check if user can act as supervisor for this request
            bool canActAsSupervisor = false;
            if (request.SupervisorEmail != null &&
                request.SupervisorEmail.Equals(currentUser.Email, StringComparison.OrdinalIgnoreCase))
            {
                if (request.Status == RefundRequestStatus.PendingSupervisor)
                {
                    canActAsSupervisor = true;
                }
                else if (isSupervisorRole)
                {
                    canActAsSupervisor = true;
                }
            }

            if (canActAsSupervisor)
            {
                try
                {
                    var previousStatus = request.Status.ToString();
                    request.Status = RefundRequestStatus.Draft;
                    request.SupervisorApprovalDate = DateTime.UtcNow;
                    request.SupervisorRemarks = supervisorRemarks ?? "";
                    request.SupervisorName = $"{currentUser.FirstName} {currentUser.LastName}";

                    await _context.SaveChangesAsync();

                    // Add history entry
                    var historyEntry = new RefundRequestHistory
                    {
                        RefundRequestId = requestId,
                        Action = RefundHistoryActions.SupervisorReverted,
                        PreviousStatus = previousStatus,
                        NewStatus = RefundRequestStatus.Draft.ToString(),
                        Comments = supervisorRemarks,
                        PerformedBy = currentUser.Id,
                        UserName = $"{currentUser.FirstName} {currentUser.LastName}",
                        Timestamp = DateTime.UtcNow,
                        IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
                    };
                    _context.RefundRequestHistories.Add(historyEntry);
                    await _context.SaveChangesAsync();

                    StatusMessage = $"Request #{requestId} has been reverted to requestor.";
                    StatusMessageClass = "info";
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Error reverting request: {ex.Message}";
                    StatusMessageClass = "danger";
                }
            }
            else if (isBudgetOfficer)
            {
                if (request.BudgetOfficerEmail == null ||
                    !request.BudgetOfficerEmail.Equals(currentUser.Email, StringComparison.OrdinalIgnoreCase))
                {
                    StatusMessage = "You are not authorized to revert this request.";
                    StatusMessageClass = "danger";
                    return RedirectToPage("/Dashboard/Approver/Index");
                }

                try
                {
                    var previousStatus = request.Status.ToString();
                    request.Status = RefundRequestStatus.PendingSupervisor;
                    request.BudgetOfficerApprovalDate = DateTime.UtcNow;
                    request.BudgetOfficerRemarks = supervisorRemarks ?? "";

                    await _context.SaveChangesAsync();

                    // Add history entry
                    var historyEntry = new RefundRequestHistory
                    {
                        RefundRequestId = requestId,
                        Action = RefundHistoryActions.BudgetOfficerReverted,
                        PreviousStatus = previousStatus,
                        NewStatus = RefundRequestStatus.PendingSupervisor.ToString(),
                        Comments = supervisorRemarks,
                        PerformedBy = currentUser.Id,
                        UserName = $"{currentUser.FirstName} {currentUser.LastName}",
                        Timestamp = DateTime.UtcNow,
                        IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
                    };
                    _context.RefundRequestHistories.Add(historyEntry);
                    await _context.SaveChangesAsync();

                    StatusMessage = $"Request #{requestId} has been reverted to supervisor.";
                    StatusMessageClass = "info";
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Error reverting request: {ex.Message}";
                    StatusMessageClass = "danger";
                }
            }
            else if (!isAdmin)
            {
                StatusMessage = "You are not authorized to perform this action.";
                StatusMessageClass = "danger";
                return RedirectToPage("/Dashboard/Approver/Index");
            }

            // Redirect to Approver Dashboard after successful action
            return RedirectToPage("/Dashboard/Approver/Index");
        }

        public async Task<IActionResult> OnPostRejectAsync(int requestId, string? supervisorRemarks)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                StatusMessage = "User not found.";
                StatusMessageClass = "danger";
                return RedirectToPage("/Dashboard/Approver/Index");
            }

            var request = await _context.RefundRequests.FindAsync(requestId);
            if (request == null)
            {
                StatusMessage = "Request not found.";
                StatusMessageClass = "danger";
                return RedirectToPage("/Dashboard/Approver/Index");
            }

            // Verify authorization based on user role and request assignment
            var isSupervisorRole = await _userManager.IsInRoleAsync(currentUser, "Supervisor");
            var isBudgetOfficer = await _userManager.IsInRoleAsync(currentUser, "Budget Officer") ||
                                 await _userManager.IsInRoleAsync(currentUser, "BudgetOfficer");
            var isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");

            // Check if user can act as supervisor for this request
            bool canActAsSupervisor = false;
            if (request.SupervisorEmail != null &&
                request.SupervisorEmail.Equals(currentUser.Email, StringComparison.OrdinalIgnoreCase))
            {
                if (request.Status == RefundRequestStatus.PendingSupervisor)
                {
                    canActAsSupervisor = true;
                }
                else if (isSupervisorRole)
                {
                    canActAsSupervisor = true;
                }
            }

            // Check if user can act as budget officer for this request
            bool canActAsBudgetOfficer = isBudgetOfficer &&
                request.BudgetOfficerEmail != null &&
                request.BudgetOfficerEmail.Equals(currentUser.Email, StringComparison.OrdinalIgnoreCase);

            if (!canActAsSupervisor && !canActAsBudgetOfficer && !isAdmin)
            {
                StatusMessage = "You are not authorized to reject this request.";
                StatusMessageClass = "danger";
                return RedirectToPage("/Dashboard/Approver/Index");
            }

            try
            {
                var previousStatus = request.Status.ToString();
                request.Status = RefundRequestStatus.Cancelled;
                request.CancellationDate = DateTime.UtcNow;
                request.CancellationReason = supervisorRemarks ?? "";
                request.CancelledBy = $"{currentUser.FirstName} {currentUser.LastName}";

                await _context.SaveChangesAsync();

                // Add history entry
                var historyAction = canActAsSupervisor ? RefundHistoryActions.SupervisorRejected : RefundHistoryActions.BudgetOfficerRejected;
                var historyEntry = new RefundRequestHistory
                {
                    RefundRequestId = requestId,
                    Action = historyAction,
                    PreviousStatus = previousStatus,
                    NewStatus = RefundRequestStatus.Cancelled.ToString(),
                    Comments = supervisorRemarks,
                    PerformedBy = currentUser.Id,
                    UserName = $"{currentUser.FirstName} {currentUser.LastName}",
                    Timestamp = DateTime.UtcNow,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
                };
                _context.RefundRequestHistories.Add(historyEntry);
                await _context.SaveChangesAsync();

                // Send notification to requester based on who rejected
                if (canActAsSupervisor)
                {
                    await _notificationService.NotifyRefundSupervisorRejectedAsync(
                        requestId,
                        request.RequestedBy ?? "",
                        supervisorRemarks
                    );

                    // Log audit trail
                    await _auditLogService.LogRefundRequestRejectedAsync(
                        requestId,
                        "Supervisor",
                        $"{currentUser.FirstName} {currentUser.LastName}",
                        request.MobileNumberAssignedTo ?? "N/A",
                        request.DevicePurchaseAmount,
                        supervisorRemarks,
                        currentUser.Id,
                        HttpContext.Connection.RemoteIpAddress?.ToString()
                    );
                }
                else if (canActAsBudgetOfficer)
                {
                    await _notificationService.NotifyRefundBudgetOfficerRejectedAsync(
                        requestId,
                        request.RequestedBy ?? "",
                        supervisorRemarks
                    );

                    // Log audit trail
                    await _auditLogService.LogRefundRequestRejectedAsync(
                        requestId,
                        "Budget Officer",
                        $"{currentUser.FirstName} {currentUser.LastName}",
                        request.MobileNumberAssignedTo ?? "N/A",
                        request.DevicePurchaseAmount,
                        supervisorRemarks,
                        currentUser.Id,
                        HttpContext.Connection.RemoteIpAddress?.ToString()
                    );
                }

                // Send rejection email notification
                try
                {
                    var requester = await _userManager.FindByIdAsync(request.RequestedBy ?? "");
                    if (requester != null)
                    {
                        await SendRejectionEmailAsync(request, currentUser, requester);
                        _logger.LogInformation("Rejection email sent successfully for refund request {RequestId}", requestId);
                    }
                }
                catch (Exception emailEx)
                {
                    _logger.LogError(emailEx, "Failed to send rejection email for request {RequestId}", requestId);
                }

                StatusMessage = $"Request #{requestId} has been rejected.";
                StatusMessageClass = "success";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error rejecting request: {ex.Message}";
                StatusMessageClass = "danger";
            }

            // Redirect to Approver Dashboard after successful action
            return RedirectToPage("/Dashboard/Approver/Index");
        }

        public async Task<IActionResult> OnPostBudgetApproveAsync(int requestId, string? costObject, string? costCenter, string? fundCommitment, string? budgetOfficerRemarks)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                StatusMessage = "User not found.";
                StatusMessageClass = "danger";
                return RedirectToPage("/Dashboard/Approver/Index");
            }

            var request = await _context.RefundRequests.FindAsync(requestId);
            if (request == null)
            {
                StatusMessage = "Request not found.";
                StatusMessageClass = "danger";
                return RedirectToPage("/Dashboard/Approver/Index");
            }

            // Verify authorization - only budget officers assigned to this request
            var isBudgetOfficer = await _userManager.IsInRoleAsync(currentUser, "Budget Officer") ||
                                 await _userManager.IsInRoleAsync(currentUser, "BudgetOfficer");

            if (!isBudgetOfficer || request.BudgetOfficerEmail != currentUser.Email)
            {
                StatusMessage = "You are not authorized to approve this request.";
                StatusMessageClass = "danger";
                return RedirectToPage("/Dashboard/Approver/Index");
            }

            // Validate required fields
            if (string.IsNullOrWhiteSpace(costObject) || string.IsNullOrWhiteSpace(costCenter) || string.IsNullOrWhiteSpace(fundCommitment))
            {
                StatusMessage = "Cost Object, Cost Center, and Fund Commitment are required fields.";
                StatusMessageClass = "danger";
                return RedirectToPage("/Dashboard/Approver/Index");
            }

            try
            {
                request.Status = RefundRequestStatus.PendingStaffClaimsUnit;
                request.BudgetOfficerApprovalDate = DateTime.UtcNow;
                request.BudgetOfficerRemarks = budgetOfficerRemarks ?? "";
                request.CostObject = costObject.Trim();
                request.CostCenter = costCenter.Trim();
                request.FundCommitment = fundCommitment.Trim();

                await _context.SaveChangesAsync();

                // Add history entry
                var historyEntry = new RefundRequestHistory
                {
                    RefundRequestId = requestId,
                    Action = RefundHistoryActions.BudgetOfficerApproved,
                    PreviousStatus = RefundRequestStatus.PendingBudgetOfficer.ToString(),
                    NewStatus = RefundRequestStatus.PendingStaffClaimsUnit.ToString(),
                    Comments = budgetOfficerRemarks,
                    PerformedBy = currentUser.Id,
                    UserName = $"{currentUser.FirstName} {currentUser.LastName}",
                    Timestamp = DateTime.UtcNow,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
                };
                _context.RefundRequestHistories.Add(historyEntry);
                await _context.SaveChangesAsync();

                StatusMessage = $"Request #{requestId} has been approved and forwarded to Staff Claims Unit.";
                StatusMessageClass = "success";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error approving request: {ex.Message}";
                StatusMessageClass = "danger";
            }

            return RedirectToPage("/Dashboard/Approver/Index");
        }

        public async Task<IActionResult> OnPostBudgetRevertToRequestorAsync(int requestId, string? budgetOfficerRemarks)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                StatusMessage = "User not found.";
                StatusMessageClass = "danger";
                return RedirectToPage("/Dashboard/Approver/Index");
            }

            var request = await _context.RefundRequests.FindAsync(requestId);
            if (request == null)
            {
                StatusMessage = "Request not found.";
                StatusMessageClass = "danger";
                return RedirectToPage("/Dashboard/Approver/Index");
            }

            // Verify authorization
            var isBudgetOfficer = await _userManager.IsInRoleAsync(currentUser, "Budget Officer") ||
                                 await _userManager.IsInRoleAsync(currentUser, "BudgetOfficer");

            if (!isBudgetOfficer || request.BudgetOfficerEmail != currentUser.Email)
            {
                StatusMessage = "You are not authorized to revert this request.";
                StatusMessageClass = "danger";
                return RedirectToPage("/Dashboard/Approver/Index");
            }

            try
            {
                var previousStatus = request.Status.ToString();
                request.Status = RefundRequestStatus.Draft;
                request.BudgetOfficerRemarks = budgetOfficerRemarks ?? "";
                request.BudgetOfficerApprovalDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Add history entry
                var historyEntry = new RefundRequestHistory
                {
                    RefundRequestId = requestId,
                    Action = RefundHistoryActions.BudgetOfficerReverted,
                    PreviousStatus = previousStatus,
                    NewStatus = RefundRequestStatus.Draft.ToString(),
                    Comments = budgetOfficerRemarks,
                    PerformedBy = currentUser.Id,
                    UserName = $"{currentUser.FirstName} {currentUser.LastName}",
                    Timestamp = DateTime.UtcNow,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
                };
                _context.RefundRequestHistories.Add(historyEntry);
                await _context.SaveChangesAsync();

                StatusMessage = $"Request #{requestId} has been reverted to the requestor.";
                StatusMessageClass = "success";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error reverting request: {ex.Message}";
                StatusMessageClass = "danger";
            }

            return RedirectToPage("/Dashboard/Approver/Index");
        }

        public async Task<IActionResult> OnPostBudgetRevertToSupervisorAsync(int requestId, string? budgetOfficerRemarks)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                StatusMessage = "User not found.";
                StatusMessageClass = "danger";
                return RedirectToPage("/Dashboard/Approver/Index");
            }

            var request = await _context.RefundRequests.FindAsync(requestId);
            if (request == null)
            {
                StatusMessage = "Request not found.";
                StatusMessageClass = "danger";
                return RedirectToPage("/Dashboard/Approver/Index");
            }

            // Verify authorization
            var isBudgetOfficer = await _userManager.IsInRoleAsync(currentUser, "Budget Officer") ||
                                 await _userManager.IsInRoleAsync(currentUser, "BudgetOfficer");

            if (!isBudgetOfficer || request.BudgetOfficerEmail != currentUser.Email)
            {
                StatusMessage = "You are not authorized to revert this request.";
                StatusMessageClass = "danger";
                return RedirectToPage("/Dashboard/Approver/Index");
            }

            try
            {
                var previousStatus = request.Status.ToString();
                request.Status = RefundRequestStatus.PendingSupervisor;
                request.BudgetOfficerRemarks = budgetOfficerRemarks ?? "";
                request.BudgetOfficerApprovalDate = DateTime.UtcNow;
                // Clear budget officer assignment to allow supervisor to reassign
                request.BudgetOfficerEmail = null;
                request.BudgetOfficerName = null;

                await _context.SaveChangesAsync();

                // Add history entry
                var historyEntry = new RefundRequestHistory
                {
                    RefundRequestId = requestId,
                    Action = RefundHistoryActions.BudgetOfficerReverted,
                    PreviousStatus = previousStatus,
                    NewStatus = RefundRequestStatus.PendingSupervisor.ToString(),
                    Comments = budgetOfficerRemarks,
                    PerformedBy = currentUser.Id,
                    UserName = $"{currentUser.FirstName} {currentUser.LastName}",
                    Timestamp = DateTime.UtcNow,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
                };
                _context.RefundRequestHistories.Add(historyEntry);
                await _context.SaveChangesAsync();

                StatusMessage = $"Request #{requestId} has been reverted to the supervisor for review.";
                StatusMessageClass = "success";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error reverting request: {ex.Message}";
                StatusMessageClass = "danger";
            }

            return RedirectToPage("/Dashboard/Approver/Index");
        }

        public async Task<IActionResult> OnGetRequestDetailsAsync(int requestId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return BadRequest("User not found");
            }

            var request = await _context.RefundRequests.FindAsync(requestId);
            if (request == null)
            {
                return NotFound("Request not found");
            }

            // Check authorization based on user role
            var isSupervisor = await _userManager.IsInRoleAsync(currentUser, "Supervisor");
            var isBudgetOfficer = await _userManager.IsInRoleAsync(currentUser, "Budget Officer") || 
                                 await _userManager.IsInRoleAsync(currentUser, "BudgetOfficer");
            var isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");

            if (!isAdmin)
            {
                if (isSupervisor && request.SupervisorEmail != currentUser.Email)
                {
                    return Forbid("Not authorized to view this request");
                }

                if (isBudgetOfficer && request.BudgetOfficerEmail != currentUser.Email)
                {
                    return Forbid("Not authorized to view this request");
                }
            }

            var requestData = new
            {
                id = request.Id,
                primaryMobileNumber = request.PrimaryMobileNumber,
                indexNo = request.IndexNo,
                mobileNumberAssignedTo = request.MobileNumberAssignedTo,
                officeExtension = request.OfficeExtension,
                office = request.Office,
                mobileService = request.MobileService,
                classOfService = request.ClassOfService,
                deviceAllowance = request.DeviceAllowance,
                previousDeviceReimbursedDate = request.PreviousDeviceReimbursedDate,
                purchaseReceiptPath = request.PurchaseReceiptPath,
                devicePurchaseCurrency = request.DevicePurchaseCurrency,
                devicePurchaseAmount = request.DevicePurchaseAmount,
                organization = request.Organization,
                umojaBankName = request.UmojaBankName,
                supervisor = request.Supervisor,
                remarks = request.Remarks,
                requestDate = request.RequestDate,
                status = (int)request.Status,
                supervisorApprovalDate = request.SupervisorApprovalDate,
                supervisorNotes = request.SupervisorNotes,
                supervisorRemarks = request.SupervisorRemarks,
                supervisorName = request.SupervisorName,
                supervisorEmail = request.SupervisorEmail,
                budgetOfficerApprovalDate = request.BudgetOfficerApprovalDate,
                budgetOfficerNotes = request.BudgetOfficerNotes,
                budgetOfficerRemarks = request.BudgetOfficerRemarks,
                budgetOfficerName = request.BudgetOfficerName,
                budgetOfficerEmail = request.BudgetOfficerEmail,
                costObject = request.CostObject,
                costCenter = request.CostCenter,
                fundCommitment = request.FundCommitment,
                staffClaimsApprovalDate = request.StaffClaimsApprovalDate,
                staffClaimsNotes = request.StaffClaimsNotes,
                staffClaimsRemarks = request.StaffClaimsRemarks,
                staffClaimsOfficerName = request.StaffClaimsOfficerName,
                staffClaimsOfficerEmail = request.StaffClaimsOfficerEmail,
                umojaPaymentDocumentId = request.UmojaPaymentDocumentId,
                refundUsdAmount = request.RefundUsdAmount,
                claimsActionDate = request.ClaimsActionDate,
                paymentApprovalDate = request.PaymentApprovalDate,
                paymentApprovalNotes = request.PaymentApprovalNotes,
                paymentApprovalOfficerName = request.PaymentApproverName,
                paymentApprovalOfficerEmail = request.PaymentApproverEmail,
                cancellationDate = request.CancellationDate,
                cancellationReason = request.CancellationReason,
                cancelledBy = request.CancelledBy,
                completionDate = request.CompletionDate
            };

            return new JsonResult(requestData);
        }
        
        public string GetStatusText(RefundRequestStatus status)
        {
            return status switch
            {
                RefundRequestStatus.Draft => "Draft",
                RefundRequestStatus.PendingSupervisor => "Pending Supervisor",
                RefundRequestStatus.PendingBudgetOfficer => "Pending Budget Officer",
                RefundRequestStatus.PendingStaffClaimsUnit => "Pending Staff Claims",
                RefundRequestStatus.PendingPaymentApproval => "Pending Payment",
                RefundRequestStatus.Completed => "Completed",
                RefundRequestStatus.Cancelled => "Rejected",
                _ => status.ToString()
            };
        }
        
        public string GetStatusClass(RefundRequestStatus status)
        {
            return status switch
            {
                RefundRequestStatus.Draft => "status-draft",
                RefundRequestStatus.PendingSupervisor => "status-pending",
                RefundRequestStatus.PendingBudgetOfficer or 
                RefundRequestStatus.PendingStaffClaimsUnit or 
                RefundRequestStatus.PendingPaymentApproval => "status-approved",
                RefundRequestStatus.Completed => "status-approved",
                RefundRequestStatus.Cancelled => "status-rejected",
                _ => "status-pending"
            };
        }
        
        private async Task<List<ApplicationUser>> LoadBudgetOfficersAsync()
        {
            var budgetOfficers = new List<ApplicationUser>();
            
            try
            {
                // Get all users with BudgetOfficer role
                var users = await _userManager.Users.ToListAsync();
                
                foreach (var user in users)
                {
                    // Check both "Budget Officer" and "BudgetOfficer" role names
                    if (await _userManager.IsInRoleAsync(user, "Budget Officer") || 
                        await _userManager.IsInRoleAsync(user, "BudgetOfficer"))
                    {
                        budgetOfficers.Add(user);
                    }
                }
            }
            catch (Exception)
            {
                // Log error but don't throw to prevent page crash
                // Return empty list for graceful degradation
                budgetOfficers = new List<ApplicationUser>();
            }
            
            return budgetOfficers.OrderBy(u => u.FirstName).ThenBy(u => u.LastName).ToList();
        }

        private async Task SendSupervisorApprovedEmailAsync(RefundRequest request, ApplicationUser supervisor, ApplicationUser requester)
        {
            var placeholders = new Dictionary<string, string>
            {
                { "RequestId", request.Id.ToString() },
                { "RequesterName", request.MobileNumberAssignedTo ?? $"{requester.FirstName} {requester.LastName}" },
                { "SupervisorName", $"{supervisor.FirstName} {supervisor.LastName}" },
                { "ApprovalDate", request.SupervisorApprovalDate?.ToString("MMMM dd, yyyy") ?? DateTime.UtcNow.ToString("MMMM dd, yyyy") },
                { "DevicePurchaseAmount", request.DevicePurchaseAmount.ToString("N2") },
                { "DevicePurchaseCurrency", request.DevicePurchaseCurrency ?? "USD" },
                { "SupervisorRemarks", request.SupervisorRemarks ?? "Approved" },
                { "ViewRequestLink", $"{Request.Scheme}://{Request.Host}/Modules/RefundManagement/Requests/Index" },
                { "Year", DateTime.Now.Year.ToString() }
            };

            await _emailService.SendTemplatedEmailAsync(
                to: requester.Email ?? "",
                templateCode: "REFUND_SUPERVISOR_APPROVED",
                data: placeholders
            );
        }

        private async Task SendBudgetOfficerNotificationEmailAsync(RefundRequest request, ApplicationUser budgetOfficer, ApplicationUser supervisor)
        {
            var placeholders = new Dictionary<string, string>
            {
                { "RequestId", request.Id.ToString() },
                { "RequesterName", request.MobileNumberAssignedTo ?? "Staff Member" },
                { "BudgetOfficerName", $"{budgetOfficer.FirstName} {budgetOfficer.LastName}" },
                { "Organization", request.Organization ?? "N/A" },
                { "SupervisorName", $"{supervisor.FirstName} {supervisor.LastName}" },
                { "DevicePurchaseAmount", request.DevicePurchaseAmount.ToString("N2") },
                { "DevicePurchaseCurrency", request.DevicePurchaseCurrency ?? "USD" },
                { "ApprovalLink", $"{Request.Scheme}://{Request.Host}/Modules/RefundManagement/Approvals/Supervisor" },
                { "Year", DateTime.Now.Year.ToString() }
            };

            await _emailService.SendTemplatedEmailAsync(
                to: budgetOfficer.Email ?? "",
                templateCode: "REFUND_BUDGET_OFFICER_NOTIFICATION",
                data: placeholders
            );
        }

        private async Task SendBudgetOfficerApprovedEmailAsync(RefundRequest request, ApplicationUser budgetOfficer, ApplicationUser requester)
        {
            var placeholders = new Dictionary<string, string>
            {
                { "RequestId", request.Id.ToString() },
                { "RequesterName", request.MobileNumberAssignedTo ?? $"{requester.FirstName} {requester.LastName}" },
                { "DevicePurchaseAmount", request.DevicePurchaseAmount.ToString("N2") },
                { "DevicePurchaseCurrency", request.DevicePurchaseCurrency ?? "USD" },
                { "CostObject", request.CostObject ?? "N/A" },
                { "CostCenter", request.CostCenter ?? "N/A" },
                { "FundCommitment", request.FundCommitment ?? "N/A" },
                { "ViewRequestLink", $"{Request.Scheme}://{Request.Host}/Modules/RefundManagement/Requests/Index" },
                { "Year", DateTime.Now.Year.ToString() }
            };

            await _emailService.SendTemplatedEmailAsync(
                to: requester.Email ?? "",
                templateCode: "REFUND_BUDGET_OFFICER_APPROVED",
                data: placeholders
            );
        }

        private async Task SendClaimsUnitNotificationEmailAsync(RefundRequest request)
        {
            // Get Claims Unit email from configuration or use default
            var claimsUnitEmail = "claims@example.com"; // TODO: Get from configuration

            var placeholders = new Dictionary<string, string>
            {
                { "RequestId", request.Id.ToString() },
                { "RequesterName", request.MobileNumberAssignedTo ?? "Staff Member" },
                { "Organization", request.Organization ?? "N/A" },
                { "UmojaBankName", request.UmojaBankName ?? "N/A" },
                { "DevicePurchaseAmount", request.DevicePurchaseAmount.ToString("N2") },
                { "DevicePurchaseCurrency", request.DevicePurchaseCurrency ?? "USD" },
                { "ProcessLink", $"{Request.Scheme}://{Request.Host}/Modules/RefundManagement/Approvals/ClaimsUnit" },
                { "Year", DateTime.Now.Year.ToString() }
            };

            await _emailService.SendTemplatedEmailAsync(
                to: claimsUnitEmail,
                templateCode: "REFUND_CLAIMS_UNIT_NOTIFICATION",
                data: placeholders
            );
        }

        private async Task SendRejectionEmailAsync(RefundRequest request, ApplicationUser rejector, ApplicationUser requester)
        {
            var placeholders = new Dictionary<string, string>
            {
                { "RequestId", request.Id.ToString() },
                { "RequesterName", request.MobileNumberAssignedTo ?? $"{requester.FirstName} {requester.LastName}" },
                { "RejectedBy", $"{rejector.FirstName} {rejector.LastName}" },
                { "RejectionDate", request.CancellationDate?.ToString("MMMM dd, yyyy") ?? DateTime.UtcNow.ToString("MMMM dd, yyyy") },
                { "RejectionReason", request.CancellationReason ?? "No reason provided" },
                { "NewRequestLink", $"{Request.Scheme}://{Request.Host}/Modules/RefundManagement/Requests/Create" },
                { "Year", DateTime.Now.Year.ToString() }
            };

            await _emailService.SendTemplatedEmailAsync(
                to: requester.Email ?? "",
                templateCode: "REFUND_REQUEST_REJECTED",
                data: placeholders
            );
        }
    }
}
