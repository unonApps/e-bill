using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TAB.Web.Data;
using TAB.Web.Models;
using TAB.Web.Services;

namespace TAB.Web.Pages.Modules.RefundManagement.Approvals.PaymentApprover
{
    [Authorize(Roles = "ICTS,Admin")]
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
        public List<RefundRequest> AllPaymentRequests { get; set; } = new();
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
                if (await _userManager.IsInRoleAsync(currentUser, "ICTS"))
                {
                    // ICTS users see requests in Pending Payment Approval status
                    AllPaymentRequests = await _context.RefundRequests
                        .Where(r => r.Status == RefundRequestStatus.PendingPaymentApproval)
                        .OrderBy(r => r.RequestDate)
                        .ToListAsync();
                }
                else if (await _userManager.IsInRoleAsync(currentUser, "Admin"))
                {
                    // Admins see all requests that are ready for payment or completed
                    AllPaymentRequests = await _context.RefundRequests
                        .Where(r => r.Status == RefundRequestStatus.PendingPaymentApproval || 
                                   r.Status == RefundRequestStatus.Completed)
                        .OrderBy(r => r.RequestDate)
                        .ToListAsync();
                }

                // For backward compatibility, keep PendingRequests as all requests
                PendingRequests = AllPaymentRequests;
            }
        }

        public async Task<IActionResult> OnPostApproveAsync(int requestId, string? paymentRemarks)
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
            var isPaymentApprover = await _userManager.IsInRoleAsync(currentUser, "ICTS");
            var isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");

            if (!isPaymentApprover && !isAdmin)
            {
                StatusMessage = "You are not authorized to approve this payment.";
                StatusMessageClass = "danger";
                return RedirectToPage();
            }

            // Only approve requests in correct status
            if (request.Status != RefundRequestStatus.PendingPaymentApproval)
            {
                StatusMessage = "This request is not in the correct status for payment approval.";
                StatusMessageClass = "danger";
                return RedirectToPage();
            }

            try
            {
                // Parse the payment remarks as JSON to extract additional fields
                var paymentData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(paymentRemarks ?? "{}");
                
                request.Status = RefundRequestStatus.Completed;
                request.PaymentApprovalDate = DateTime.UtcNow;
                request.PaymentApprovalRemarks = paymentData.ContainsKey("paymentRemarks") ? paymentData["paymentRemarks"]?.ToString() ?? string.Empty : string.Empty;
                request.PaymentApproverName = $"{currentUser.FirstName} {currentUser.LastName}";
                request.PaymentApproverEmail = currentUser.Email ?? string.Empty;
                request.CompletionDate = DateTime.UtcNow;
                request.ProcessedBy = currentUser.Id;

                // Update payment reference if provided
                if (paymentData.ContainsKey("paymentReference"))
                    request.PaymentReference = paymentData["paymentReference"]?.ToString() ?? string.Empty;

                await _context.SaveChangesAsync();

                // Send notification to requester
                await _notificationService.NotifyRefundPaymentApprovedAsync(
                    requestId,
                    request.RequestedBy ?? "",
                    request.PaymentApprovalRemarks
                );

                // Log audit trail
                await _auditLogService.LogRefundRequestCompletedAsync(
                    requestId,
                    $"{currentUser.FirstName} {currentUser.LastName}",
                    request.MobileNumberAssignedTo ?? "N/A",
                    request.DevicePurchaseAmount,
                    request.PaymentReference,
                    currentUser.Id,
                    HttpContext.Connection.RemoteIpAddress?.ToString()
                );

                // Send payment approved/completion email
                try
                {
                    var requester = await _userManager.FindByIdAsync(request.RequestedBy ?? "");
                    if (requester != null)
                    {
                        await SendPaymentApprovedEmailAsync(request, requester);
                        _logger.LogInformation("Payment approved email sent successfully for refund request {RequestId}", requestId);
                    }
                }
                catch (Exception emailEx)
                {
                    _logger.LogError(emailEx, "Failed to send payment approved email for request {RequestId}", requestId);
                }

                StatusMessage = $"Payment for Request #{requestId} has been approved and completed.";
                StatusMessageClass = "success";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error approving payment: {ex.Message}";
                StatusMessageClass = "danger";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostRejectAsync(int requestId, string? paymentRemarks)
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
            var isPaymentApprover = await _userManager.IsInRoleAsync(currentUser, "ICTS");
            var isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");

            if (!isPaymentApprover && !isAdmin)
            {
                StatusMessage = "You are not authorized to reject this payment.";
                StatusMessageClass = "danger";
                return RedirectToPage();
            }

            try
            {
                request.Status = RefundRequestStatus.Cancelled;
                request.CancellationDate = DateTime.UtcNow;
                request.CancellationReason = paymentRemarks ?? "";
                request.CancelledBy = $"{currentUser.FirstName} {currentUser.LastName}";
                request.PaymentApprovalDate = DateTime.UtcNow;
                request.PaymentApprovalRemarks = paymentRemarks ?? "";
                request.PaymentApproverName = $"{currentUser.FirstName} {currentUser.LastName}";
                request.PaymentApproverEmail = currentUser.Email ?? string.Empty;

                await _context.SaveChangesAsync();

                // Send notification to requester
                await _notificationService.NotifyRefundPaymentRejectedAsync(
                    requestId,
                    request.RequestedBy ?? "",
                    paymentRemarks
                );

                // Log audit trail
                await _auditLogService.LogRefundRequestRejectedAsync(
                    requestId,
                    "Payment Approver",
                    $"{currentUser.FirstName} {currentUser.LastName}",
                    request.MobileNumberAssignedTo ?? "N/A",
                    request.DevicePurchaseAmount,
                    paymentRemarks,
                    currentUser.Id,
                    HttpContext.Connection.RemoteIpAddress?.ToString()
                );

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

                StatusMessage = $"Payment for Request #{requestId} has been rejected.";
                StatusMessageClass = "warning";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error rejecting payment: {ex.Message}";
                StatusMessageClass = "danger";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostRevertToClaimsUnitAsync(int requestId, string? paymentRemarks)
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
            var isPaymentApprover = await _userManager.IsInRoleAsync(currentUser, "ICTS");
            var isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");

            if (!isPaymentApprover && !isAdmin)
            {
                StatusMessage = "You are not authorized to revert this request.";
                StatusMessageClass = "danger";
                return RedirectToPage();
            }

            try
            {
                request.Status = RefundRequestStatus.PendingStaffClaimsUnit;
                request.PaymentApprovalRemarks = paymentRemarks ?? "";
                request.PaymentApprovalDate = DateTime.UtcNow;
                request.PaymentApproverName = $"{currentUser.FirstName} {currentUser.LastName}";
                request.PaymentApproverEmail = currentUser.Email ?? string.Empty;

                await _context.SaveChangesAsync();

                StatusMessage = $"Request #{requestId} has been reverted to Staff Claims Unit for review.";
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
                costObject = request.CostObject,
                costCenter = request.CostCenter,
                fundCommitment = request.FundCommitment,
                budgetOfficerRemarks = request.BudgetOfficerRemarks,
                staffClaimsRemarks = request.StaffClaimsRemarks,
                umojaPaymentDocumentId = request.UmojaPaymentDocumentId,
                refundUsdAmount = request.RefundUsdAmount,
                claimsActionDate = request.ClaimsActionDate?.ToString("yyyy-MM-dd"),
                requestDate = request.RequestDate.ToString("MMM dd, yyyy"),
                status = request.Status.ToString(),
                // Payment specific fields
                paymentReference = request.PaymentReference,
                paymentApprovalRemarks = request.PaymentApprovalRemarks,
                // Purchase Receipt fields
                purchaseReceiptPath = request.PurchaseReceiptPath,
                umojaBankName = request.UmojaBankName,
                // Workflow History fields
                requestedBy = request.RequestedBy,
                supervisorApprovalDate = request.SupervisorApprovalDate?.ToString("MMM dd, yyyy"),
                supervisorName = request.SupervisorName,
                supervisorRemarks = request.SupervisorRemarks,
                budgetOfficerApprovalDate = request.BudgetOfficerApprovalDate?.ToString("MMM dd, yyyy"),
                budgetOfficerName = request.BudgetOfficerName,
                staffClaimsApprovalDate = request.StaffClaimsApprovalDate?.ToString("MMM dd, yyyy"),
                staffClaimsOfficerName = request.StaffClaimsOfficerName,
                paymentApprovalDate = request.PaymentApprovalDate?.ToString("MMM dd, yyyy"),
                paymentApproverName = request.PaymentApproverName,
                completionDate = request.CompletionDate?.ToString("MMM dd, yyyy")
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

        private async Task SendPaymentApprovedEmailAsync(RefundRequest request, ApplicationUser requester)
        {
            var placeholders = new Dictionary<string, string>
            {
                { "RequestId", request.Id.ToString() },
                { "RequesterName", request.MobileNumberAssignedTo ?? $"{requester.FirstName} {requester.LastName}" },
                { "RefundUsdAmount", request.RefundUsdAmount?.ToString("N2") ?? "0.00" },
                { "PaymentReference", request.PaymentReference ?? "Processing" },
                { "CompletionDate", request.CompletionDate?.ToString("MMMM dd, yyyy") ?? DateTime.UtcNow.ToString("MMMM dd, yyyy") },
                { "ViewRequestLink", $"{Request.Scheme}://{Request.Host}/Modules/RefundManagement/Requests/Index" },
                { "Year", DateTime.Now.Year.ToString() }
            };

            await _emailService.SendTemplatedEmailAsync(
                to: requester.Email ?? "",
                templateCode: "REFUND_PAYMENT_APPROVED",
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