using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TAB.Web.Data;
using TAB.Web.Models;

namespace TAB.Web.Pages.Modules.RefundManagement.Approvals.BudgetOfficer
{
    [Authorize(Roles = "Budget Officer,BudgetOfficer,Admin")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public IndexModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public List<RefundRequest> PendingRequests { get; set; } = new();
        public List<RefundRequest> AllBudgetRequests { get; set; } = new();
        public string CurrentUserRole { get; set; } = "";
        public bool IsDetailView { get; set; } = false;
        public RefundRequest? CurrentRequest { get; set; }

        [TempData]
        public string? StatusMessage { get; set; }

        [TempData]
        public string? StatusMessageClass { get; set; }

        public async Task OnGetAsync(int? requestId = null)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null)
            {
                // Determine user role for filtering and display logic
                var userRoles = await _userManager.GetRolesAsync(currentUser);
                CurrentUserRole = userRoles.FirstOrDefault() ?? "";

                // Check if this is a detail view request
                if (requestId.HasValue)
                {
                    CurrentRequest = await _context.RefundRequests.FindAsync(requestId.Value);
                    if (CurrentRequest != null)
                    {
                        IsDetailView = true;
                    }
                }

                // Filter requests based on user role
                if (await _userManager.IsInRoleAsync(currentUser, "Budget Officer") || 
                    await _userManager.IsInRoleAsync(currentUser, "BudgetOfficer"))
                {
                    // Budget Officers see requests where they are assigned as the budget officer
                    AllBudgetRequests = await _context.RefundRequests
                        .Where(r => r.BudgetOfficerEmail == currentUser.Email)
                        .OrderBy(r => r.RequestDate)
                        .ToListAsync();
                }
                else if (await _userManager.IsInRoleAsync(currentUser, "Admin"))
                {
                    // Admins see all requests in budget officer status
                    AllBudgetRequests = await _context.RefundRequests
                        .Where(r => r.Status == RefundRequestStatus.PendingBudgetOfficer)
                        .OrderBy(r => r.RequestDate)
                        .ToListAsync();
                }

                // For backward compatibility, keep PendingRequests as all requests
                PendingRequests = AllBudgetRequests;
            }
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

        public async Task<IActionResult> OnPostRejectAsync(int requestId, string? budgetOfficerRemarks)
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
                StatusMessage = "You are not authorized to reject this request.";
                StatusMessageClass = "danger";
                return RedirectToPage();
            }

            try
            {
                request.Status = RefundRequestStatus.Cancelled;
                request.CancellationDate = DateTime.UtcNow;
                request.CancellationReason = budgetOfficerRemarks ?? "";
                request.CancelledBy = $"{currentUser.FirstName} {currentUser.LastName}";

                await _context.SaveChangesAsync();

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
                organization = request.Organization,
                supervisor = request.Supervisor,
                classOfService = request.ClassOfService,
                mobileService = request.MobileService,
                office = request.Office,
                deviceAllowance = request.DeviceAllowance,
                devicePurchaseAmount = request.DevicePurchaseAmount,
                devicePurchaseCurrency = request.DevicePurchaseCurrency,
                refundUsdAmount = request.RefundUsdAmount,
                requestDate = request.RequestDate.ToString("MMM dd, yyyy"),
                status = request.Status.ToString(),
                umojaBankName = request.UmojaBankName,
                costObject = request.CostObject,
                costCenter = request.CostCenter,
                fundCommitment = request.FundCommitment,
                
                // Purchase receipt information
                purchaseReceiptPath = request.PurchaseReceiptPath,
                purchaseReceiptFileName = !string.IsNullOrEmpty(request.PurchaseReceiptPath) 
                    ? Path.GetFileName(request.PurchaseReceiptPath) 
                    : null,
                
                // Workflow history and remarks
                remarks = request.Remarks,
                supervisorRemarks = request.SupervisorRemarks,
                budgetOfficerRemarks = request.BudgetOfficerRemarks,
                
                // Workflow dates and officers
                requestDate_raw = request.RequestDate,
                supervisorApprovalDate = request.SupervisorApprovalDate?.ToString("MMM dd, yyyy HH:mm"),
                budgetOfficerApprovalDate = request.BudgetOfficerApprovalDate?.ToString("MMM dd, yyyy HH:mm"),
                
                // Officer names
                supervisorName = request.Supervisor,
                budgetOfficerName = request.BudgetOfficerName
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
                RefundRequestStatus.PendingStaffClaimsUnit => "status-processing",
                RefundRequestStatus.PendingPaymentApproval => "status-processing",
                RefundRequestStatus.Completed => "status-completed",
                RefundRequestStatus.Cancelled => "status-cancelled",
                _ => "status-draft"
            };
        }
    }
} 