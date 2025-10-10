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
    [Authorize(Roles = "Supervisor,Budget Officer,BudgetOfficer,Admin")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly INotificationService _notificationService;
        private readonly IAuditLogService _auditLogService;

        public IndexModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager, INotificationService notificationService, IAuditLogService auditLogService)
        {
            _context = context;
            _userManager = userManager;
            _notificationService = notificationService;
            _auditLogService = auditLogService;
        }

        public List<RefundRequest> PendingRequests { get; set; } = new();
        public List<RefundRequest> AllSupervisorRequests { get; set; } = new();
        public List<ApplicationUser> BudgetOfficers { get; set; } = new();
        public string CurrentUserRole { get; set; } = "";

        [TempData]
        public string? StatusMessage { get; set; }

        [TempData]
        public string? StatusMessageClass { get; set; }

        public async Task OnGetAsync()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null)
            {
                // Determine user role for filtering and display logic
                var userRoles = await _userManager.GetRolesAsync(currentUser);
                CurrentUserRole = userRoles.FirstOrDefault() ?? "";
                
                // Filter requests based on user role
                if (await _userManager.IsInRoleAsync(currentUser, "Supervisor"))
                {
                    // Supervisors see requests where they are the supervisor
                    AllSupervisorRequests = await _context.RefundRequests
                        .Where(r => r.SupervisorEmail == currentUser.Email)
                        .OrderBy(r => r.RequestDate)
                        .ToListAsync();
                }
                else if (await _userManager.IsInRoleAsync(currentUser, "Budget Officer") || 
                         await _userManager.IsInRoleAsync(currentUser, "BudgetOfficer"))
                {
                    // Budget Officers see requests where they are assigned as the budget officer
                    AllSupervisorRequests = await _context.RefundRequests
                        .Where(r => r.BudgetOfficerEmail == currentUser.Email)
                        .OrderBy(r => r.RequestDate)
                        .ToListAsync();
                }
                else if (await _userManager.IsInRoleAsync(currentUser, "Admin"))
                {
                    // Admins see all requests
                    AllSupervisorRequests = await _context.RefundRequests
                        .OrderBy(r => r.RequestDate)
                        .ToListAsync();
                }

                // For backward compatibility, keep PendingRequests as all requests
                // The frontend will filter by status for display
                PendingRequests = AllSupervisorRequests;
                
                // Load budget officers for the dropdown (only needed for supervisors)
                if (await _userManager.IsInRoleAsync(currentUser, "Supervisor") || await _userManager.IsInRoleAsync(currentUser, "Admin"))
                {
                    BudgetOfficers = await LoadBudgetOfficersAsync();
                }
            }
        }

        public async Task<IActionResult> OnPostApproveAsync(int requestId, string? supervisorRemarks, string? budgetOfficerEmail)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                StatusMessage = "User not found.";
                StatusMessageClass = "danger";
                return RedirectToPage();
            }

            var request = await _context.RefundRequests.FindAsync(requestId);
            if (request == null)
            {
                StatusMessage = "Request not found.";
                StatusMessageClass = "danger";
                return RedirectToPage();
            }

            // Verify authorization based on user role
            var isSupervisor = await _userManager.IsInRoleAsync(currentUser, "Supervisor");
            var isBudgetOfficer = await _userManager.IsInRoleAsync(currentUser, "Budget Officer") || 
                                 await _userManager.IsInRoleAsync(currentUser, "BudgetOfficer");
            var isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");

            if (isSupervisor)
            {
                // Supervisors can only approve their own supervised requests
                if (request.SupervisorEmail != currentUser.Email)
                {
                    StatusMessage = "You are not authorized to approve this request.";
                    StatusMessageClass = "danger";
                    return RedirectToPage();
                }
                
                // Validate budget officer selection for supervisor approval
                if (string.IsNullOrEmpty(budgetOfficerEmail))
                {
                    StatusMessage = "Please select a Budget Officer before approving.";
                    StatusMessageClass = "danger";
                    return RedirectToPage();
                }

                // Verify the selected budget officer exists and has the correct role
                var budgetOfficer = await _userManager.FindByEmailAsync(budgetOfficerEmail);
                if (budgetOfficer == null || 
                    (!await _userManager.IsInRoleAsync(budgetOfficer, "Budget Officer") && 
                     !await _userManager.IsInRoleAsync(budgetOfficer, "BudgetOfficer")))
                {
                    StatusMessage = "Invalid Budget Officer selected.";
                    StatusMessageClass = "danger";
                    return RedirectToPage();
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
                    return RedirectToPage();
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
                return RedirectToPage();
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostRevertAsync(int requestId, string? supervisorRemarks)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                StatusMessage = "User not found.";
                StatusMessageClass = "danger";
                return RedirectToPage();
            }

            var request = await _context.RefundRequests.FindAsync(requestId);
            if (request == null)
            {
                StatusMessage = "Request not found.";
                StatusMessageClass = "danger";
                return RedirectToPage();
            }

            // Verify authorization based on user role
            var isSupervisor = await _userManager.IsInRoleAsync(currentUser, "Supervisor");
            var isBudgetOfficer = await _userManager.IsInRoleAsync(currentUser, "Budget Officer") || 
                                 await _userManager.IsInRoleAsync(currentUser, "BudgetOfficer");
            var isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");

            if (isSupervisor)
            {
                if (request.SupervisorEmail != currentUser.Email)
                {
                    StatusMessage = "You are not authorized to revert this request.";
                    StatusMessageClass = "danger";
                    return RedirectToPage();
                }

                try
                {
                    request.Status = RefundRequestStatus.Draft;
                    request.SupervisorApprovalDate = DateTime.UtcNow;
                    request.SupervisorRemarks = supervisorRemarks ?? "";
                    request.SupervisorName = $"{currentUser.FirstName} {currentUser.LastName}";
                    
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
                if (request.BudgetOfficerEmail != currentUser.Email)
                {
                    StatusMessage = "You are not authorized to revert this request.";
                    StatusMessageClass = "danger";
                    return RedirectToPage();
                }

                try
                {
                    request.Status = RefundRequestStatus.PendingSupervisor;
                    request.BudgetOfficerApprovalDate = DateTime.UtcNow;
                    request.BudgetOfficerRemarks = supervisorRemarks ?? "";
                    
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
                return RedirectToPage();
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostRejectAsync(int requestId, string? supervisorRemarks)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                StatusMessage = "User not found.";
                StatusMessageClass = "danger";
                return RedirectToPage();
            }

            var request = await _context.RefundRequests.FindAsync(requestId);
            if (request == null)
            {
                StatusMessage = "Request not found.";
                StatusMessageClass = "danger";
                return RedirectToPage();
            }

            // Verify authorization
            var isSupervisor = await _userManager.IsInRoleAsync(currentUser, "Supervisor");
            var isBudgetOfficer = await _userManager.IsInRoleAsync(currentUser, "Budget Officer") || 
                                 await _userManager.IsInRoleAsync(currentUser, "BudgetOfficer");
            
            if (isSupervisor && request.SupervisorEmail != currentUser.Email)
            {
                StatusMessage = "You are not authorized to reject this request.";
                StatusMessageClass = "danger";
                return RedirectToPage();
            }
            else if (isBudgetOfficer && request.BudgetOfficerEmail != currentUser.Email)
            {
                StatusMessage = "You are not authorized to reject this request.";
                StatusMessageClass = "danger";
                return RedirectToPage();
            }
            else if (!isSupervisor && !isBudgetOfficer && !await _userManager.IsInRoleAsync(currentUser, "Admin"))
            {
                StatusMessage = "You are not authorized to reject this request.";
                StatusMessageClass = "danger";
                return RedirectToPage();
            }

            try
            {
                request.Status = RefundRequestStatus.Cancelled;
                request.CancellationDate = DateTime.UtcNow;
                request.CancellationReason = supervisorRemarks ?? "";
                request.CancelledBy = $"{currentUser.FirstName} {currentUser.LastName}";

                await _context.SaveChangesAsync();

                // Send notification to requester based on who rejected
                if (isSupervisor)
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
                else if (isBudgetOfficer)
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

                StatusMessage = $"Request #{requestId} has been rejected.";
                StatusMessageClass = "success";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error rejecting request: {ex.Message}";
                StatusMessageClass = "danger";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostBudgetApproveAsync(int requestId, string? costObject, string? costCenter, string? fundCommitment, string? budgetOfficerRemarks)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                StatusMessage = "User not found.";
                StatusMessageClass = "danger";
                return RedirectToPage();
            }

            var request = await _context.RefundRequests.FindAsync(requestId);
            if (request == null)
            {
                StatusMessage = "Request not found.";
                StatusMessageClass = "danger";
                return RedirectToPage();
            }

            // Verify authorization - only budget officers assigned to this request
            var isBudgetOfficer = await _userManager.IsInRoleAsync(currentUser, "Budget Officer") || 
                                 await _userManager.IsInRoleAsync(currentUser, "BudgetOfficer");
            
            if (!isBudgetOfficer || request.BudgetOfficerEmail != currentUser.Email)
            {
                StatusMessage = "You are not authorized to approve this request.";
                StatusMessageClass = "danger";
                return RedirectToPage();
            }

            // Validate required fields
            if (string.IsNullOrWhiteSpace(costObject) || string.IsNullOrWhiteSpace(costCenter) || string.IsNullOrWhiteSpace(fundCommitment))
            {
                StatusMessage = "Cost Object, Cost Center, and Fund Commitment are required fields.";
                StatusMessageClass = "danger";
                return RedirectToPage();
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

                StatusMessage = $"Request #{requestId} has been approved and forwarded to Staff Claims Unit.";
                StatusMessageClass = "success";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error approving request: {ex.Message}";
                StatusMessageClass = "danger";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostBudgetRevertToRequestorAsync(int requestId, string? budgetOfficerRemarks)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                StatusMessage = "User not found.";
                StatusMessageClass = "danger";
                return RedirectToPage();
            }

            var request = await _context.RefundRequests.FindAsync(requestId);
            if (request == null)
            {
                StatusMessage = "Request not found.";
                StatusMessageClass = "danger";
                return RedirectToPage();
            }

            // Verify authorization
            var isBudgetOfficer = await _userManager.IsInRoleAsync(currentUser, "Budget Officer") || 
                                 await _userManager.IsInRoleAsync(currentUser, "BudgetOfficer");
            
            if (!isBudgetOfficer || request.BudgetOfficerEmail != currentUser.Email)
            {
                StatusMessage = "You are not authorized to revert this request.";
                StatusMessageClass = "danger";
                return RedirectToPage();
            }

            try
            {
                request.Status = RefundRequestStatus.Draft;
                request.BudgetOfficerRemarks = budgetOfficerRemarks ?? "";
                request.BudgetOfficerApprovalDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

                StatusMessage = $"Request #{requestId} has been reverted to the requestor.";
            StatusMessageClass = "success";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error reverting request: {ex.Message}";
                StatusMessageClass = "danger";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostBudgetRevertToSupervisorAsync(int requestId, string? budgetOfficerRemarks)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                StatusMessage = "User not found.";
                StatusMessageClass = "danger";
                return RedirectToPage();
            }

            var request = await _context.RefundRequests.FindAsync(requestId);
            if (request == null)
            {
                StatusMessage = "Request not found.";
                StatusMessageClass = "danger";
                return RedirectToPage();
            }

            // Verify authorization
            var isBudgetOfficer = await _userManager.IsInRoleAsync(currentUser, "Budget Officer") || 
                                 await _userManager.IsInRoleAsync(currentUser, "BudgetOfficer");
            
            if (!isBudgetOfficer || request.BudgetOfficerEmail != currentUser.Email)
            {
                StatusMessage = "You are not authorized to revert this request.";
                StatusMessageClass = "danger";
                return RedirectToPage();
            }

            try
            {
                request.Status = RefundRequestStatus.PendingSupervisor;
                request.BudgetOfficerRemarks = budgetOfficerRemarks ?? "";
                request.BudgetOfficerApprovalDate = DateTime.UtcNow;
                // Clear budget officer assignment to allow supervisor to reassign
                request.BudgetOfficerEmail = null;
                request.BudgetOfficerName = null;

            await _context.SaveChangesAsync();

                StatusMessage = $"Request #{requestId} has been reverted to the supervisor for review.";
                StatusMessageClass = "success";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error reverting request: {ex.Message}";
                StatusMessageClass = "danger";
            }

            return RedirectToPage();
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
    }
}
