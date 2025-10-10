using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TAB.Web.Data;
using TAB.Web.Models;
using TAB.Web.Services;

namespace TAB.Web.Pages.Modules.RefundManagement.Approvals.ClaimsUnit
{
    [Authorize(Roles = "Claims Unit Approver,Admin")]
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
        public List<RefundRequest> AllClaimsRequests { get; set; } = new();
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
                if (await _userManager.IsInRoleAsync(currentUser, "Claims Unit Approver"))
                {
                    // Claims Unit Approvers see requests in Pending Staff Claims Unit status
                    AllClaimsRequests = await _context.RefundRequests
                        .Where(r => r.Status == RefundRequestStatus.PendingStaffClaimsUnit)
                        .OrderBy(r => r.RequestDate)
                        .ToListAsync();
                }
                else if (await _userManager.IsInRoleAsync(currentUser, "Admin"))
                {
                    // Admins see all requests
                    AllClaimsRequests = await _context.RefundRequests
                        .OrderBy(r => r.RequestDate)
                        .ToListAsync();
                }

                // For backward compatibility, keep PendingRequests as all requests
                PendingRequests = AllClaimsRequests;
            }
        }

        public async Task<IActionResult> OnPostApproveAsync(int requestId, string? claimsRemarks)
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
            var isClaimsApprover = await _userManager.IsInRoleAsync(currentUser, "Claims Unit Approver");
            var isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");

            if (!isClaimsApprover && !isAdmin)
            {
                StatusMessage = "You are not authorized to approve this request.";
                StatusMessageClass = "danger";
                return RedirectToPage();
            }

            // Only approve requests in correct status
            if (request.Status != RefundRequestStatus.PendingStaffClaimsUnit)
            {
                StatusMessage = "This request is not in the correct status for approval.";
                StatusMessageClass = "danger";
                return RedirectToPage();
            }

            try
            {
                // Parse the claims remarks as JSON to extract additional fields
                var claimsData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(claimsRemarks ?? "{}");
                
                request.Status = RefundRequestStatus.PendingPaymentApproval;
                request.StaffClaimsApprovalDate = DateTime.UtcNow;
                request.StaffClaimsRemarks = claimsData.ContainsKey("claimsRemarks") ? claimsData["claimsRemarks"]?.ToString() ?? string.Empty : string.Empty;
                request.StaffClaimsOfficerName = $"{currentUser.FirstName} {currentUser.LastName}";
                request.StaffClaimsOfficerEmail = currentUser.Email ?? string.Empty;

                // Update additional fields if provided
                if (claimsData.ContainsKey("umojaPaymentDocId"))
                    request.UmojaPaymentDocumentId = claimsData["umojaPaymentDocId"]?.ToString() ?? string.Empty;
                
                if (claimsData.ContainsKey("refundUsdAmount") && decimal.TryParse(claimsData["refundUsdAmount"]?.ToString(), out decimal refundAmount))
                    request.RefundUsdAmount = refundAmount;
                
                if (claimsData.ContainsKey("claimsActionDate") && DateTime.TryParse(claimsData["claimsActionDate"]?.ToString(), out DateTime actionDate))
                    request.ClaimsActionDate = actionDate;

                await _context.SaveChangesAsync();

                // Send notification to requester
                await _notificationService.NotifyRefundClaimsUnitApprovedAsync(
                    requestId,
                    request.RequestedBy ?? "",
                    request.StaffClaimsRemarks
                );

                // Log audit trail
                await _auditLogService.LogRefundRequestApprovedAsync(
                    requestId,
                    "Claims Unit Approver",
                    $"{currentUser.FirstName} {currentUser.LastName}",
                    request.MobileNumberAssignedTo ?? "N/A",
                    request.DevicePurchaseAmount,
                    request.StaffClaimsRemarks,
                    currentUser.Id,
                    HttpContext.Connection.RemoteIpAddress?.ToString()
                );

                // Send notification to Payment Approvers
                var paymentApprovers = await _userManager.GetUsersInRoleAsync("Payment Approver");
                foreach (var approver in paymentApprovers)
                {
                    await _notificationService.NotifyNewRefundRequestPendingApprovalAsync(
                        requestId,
                        approver.Id,
                        request.MobileNumberAssignedTo,
                        "Payment Approver"
                    );
                }

                StatusMessage = $"Request #{requestId} has been approved and forwarded to Payment Approval.";
                StatusMessageClass = "success";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error approving request: {ex.Message}";
                StatusMessageClass = "danger";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostRevertToRequestorAsync(int requestId, string? claimsRemarks)
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
            var isClaimsApprover = await _userManager.IsInRoleAsync(currentUser, "Claims Unit Approver");
            var isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");

            if (!isClaimsApprover && !isAdmin)
            {
                StatusMessage = "You are not authorized to revert this request.";
                StatusMessageClass = "danger";
                return RedirectToPage();
            }

            try
            {
                request.Status = RefundRequestStatus.Draft;
                request.SubmittedToSupervisor = false;
                request.StaffClaimsRemarks = claimsRemarks ?? "";
                request.StaffClaimsApprovalDate = DateTime.UtcNow;
                request.StaffClaimsOfficerName = $"{currentUser.FirstName} {currentUser.LastName}";
                request.StaffClaimsOfficerEmail = currentUser.Email ?? string.Empty;

                await _context.SaveChangesAsync();

                StatusMessage = $"Request #{requestId} has been reverted to the requestor for revision.";
                StatusMessageClass = "success";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error reverting request: {ex.Message}";
                StatusMessageClass = "danger";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostRejectAsync(int requestId, string? claimsRemarks)
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
            var isClaimsApprover = await _userManager.IsInRoleAsync(currentUser, "Claims Unit Approver");
            var isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");

            if (!isClaimsApprover && !isAdmin)
            {
                StatusMessage = "You are not authorized to reject this request.";
                StatusMessageClass = "danger";
                return RedirectToPage();
            }

            try
            {
                request.Status = RefundRequestStatus.Cancelled;
                request.CancellationDate = DateTime.UtcNow;
                request.CancellationReason = claimsRemarks ?? "";
                request.CancelledBy = $"{currentUser.FirstName} {currentUser.LastName}";

                await _context.SaveChangesAsync();

                // Send notification to requester
                await _notificationService.NotifyRefundClaimsUnitRejectedAsync(
                    requestId,
                    request.RequestedBy ?? "",
                    claimsRemarks
                );

                // Log audit trail
                await _auditLogService.LogRefundRequestRejectedAsync(
                    requestId,
                    "Claims Unit Approver",
                    $"{currentUser.FirstName} {currentUser.LastName}",
                    request.MobileNumberAssignedTo ?? "N/A",
                    request.DevicePurchaseAmount,
                    claimsRemarks,
                    currentUser.Id,
                    HttpContext.Connection.RemoteIpAddress?.ToString()
                );

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

        public async Task<IActionResult> OnPostRevertToBudgetOfficerAsync(int requestId, string? claimsRemarks)
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
            var isClaimsApprover = await _userManager.IsInRoleAsync(currentUser, "Claims Unit Approver");
            var isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");

            if (!isClaimsApprover && !isAdmin)
            {
                StatusMessage = "You are not authorized to revert this request.";
                StatusMessageClass = "danger";
                return RedirectToPage();
            }

            try
            {
                request.Status = RefundRequestStatus.PendingBudgetOfficer;
                request.StaffClaimsRemarks = claimsRemarks ?? "";
                request.StaffClaimsApprovalDate = DateTime.UtcNow;
                request.StaffClaimsOfficerName = $"{currentUser.FirstName} {currentUser.LastName}";
                request.StaffClaimsOfficerEmail = currentUser.Email ?? string.Empty;

                await _context.SaveChangesAsync();

                StatusMessage = $"Request #{requestId} has been reverted to Budget Officer for review.";
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
            var request = await _context.RefundRequests.FindAsync(requestId);
            if (request == null)
            {
                return new JsonResult(new { success = false, message = "Request not found" });
            }

            return new JsonResult(new
            {
                success = true,
                id = request.Id,
                mobileNumberAssignedTo = request.MobileNumberAssignedTo,
                staffName = request.MobileNumberAssignedTo,
                primaryMobileNumber = request.PrimaryMobileNumber,
                indexNo = request.IndexNo,
                office = request.Office,
                organization = request.Organization,
                supervisor = request.Supervisor,
                classOfService = request.ClassOfService,
                mobileService = request.MobileService,
                deviceAllowance = request.DeviceAllowance,
                devicePurchaseAmount = request.DevicePurchaseAmount,
                devicePurchaseCurrency = request.DevicePurchaseCurrency,
                refundUsdAmount = request.RefundUsdAmount,
                costObject = request.CostObject,
                costCenter = request.CostCenter,
                fundCommitment = request.FundCommitment,
                umojaBankName = request.UmojaBankName,
                
                // Purchase receipt information
                purchaseReceiptPath = request.PurchaseReceiptPath,
                purchaseReceiptFileName = !string.IsNullOrEmpty(request.PurchaseReceiptPath) 
                    ? Path.GetFileName(request.PurchaseReceiptPath) 
                    : null,
                
                // Workflow history and remarks
                remarks = request.Remarks,
                supervisorRemarks = request.SupervisorRemarks,
                budgetOfficerRemarks = request.BudgetOfficerRemarks,
                staffClaimsRemarks = request.StaffClaimsRemarks,
                
                // Workflow dates and officers
                requestDate_raw = request.RequestDate,
                supervisorApprovalDate = request.SupervisorApprovalDate?.ToString("MMM dd, yyyy HH:mm"),
                budgetOfficerApprovalDate = request.BudgetOfficerApprovalDate?.ToString("MMM dd, yyyy HH:mm"),
                staffClaimsApprovalDate = request.StaffClaimsApprovalDate?.ToString("MMM dd, yyyy HH:mm"),
                paymentApprovalDate = request.PaymentApprovalDate?.ToString("MMM dd, yyyy HH:mm"),
                completionDate = request.CompletionDate?.ToString("MMM dd, yyyy HH:mm"),
                
                // Officer names
                supervisorName = request.SupervisorName,
                budgetOfficerName = request.BudgetOfficerName,
                staffClaimsOfficerName = request.StaffClaimsOfficerName,
                paymentApproverName = request.PaymentApproverName,
                
                // Payment details
                umojaPaymentDocumentId = request.UmojaPaymentDocumentId,
                paymentReference = request.PaymentReference,
                
                requestDate = request.RequestDate.ToString("MMM dd, yyyy"),
                requestedBy = request.RequestedBy,
                status = request.Status.ToString()
            });
        }

        public string GetStatusText(RefundRequestStatus status)
        {
            return status switch
            {
                RefundRequestStatus.Draft => "Draft",
                RefundRequestStatus.PendingSupervisor => "Pending Supervisor",
                RefundRequestStatus.PendingBudgetOfficer => "Pending Budget Officer",
                RefundRequestStatus.PendingStaffClaimsUnit => "Pending Staff Claims Unit",
                RefundRequestStatus.PendingPaymentApproval => "Pending Payment Approval",
                RefundRequestStatus.Completed => "Completed",
                RefundRequestStatus.Cancelled => "Cancelled",
                _ => status.ToString()
            };
        }

        public string GetStatusClass(RefundRequestStatus status)
        {
            return status switch
            {
                RefundRequestStatus.Draft => "status-draft",
                RefundRequestStatus.PendingSupervisor => "status-pending",
                RefundRequestStatus.PendingBudgetOfficer => "status-pending",
                RefundRequestStatus.PendingStaffClaimsUnit => "status-pending",
                RefundRequestStatus.PendingPaymentApproval => "status-processing",
                RefundRequestStatus.Completed => "status-completed",
                RefundRequestStatus.Cancelled => "status-cancelled",
                _ => "status-draft"
            };
        }
    }
} 