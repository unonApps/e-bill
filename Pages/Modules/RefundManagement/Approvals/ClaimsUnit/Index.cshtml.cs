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
    [Authorize(Roles = "Staff Claims Unit,Admin")]
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
        public List<RefundRequest> AllClaimsRequests { get; set; } = new();
        public string CurrentUserRole { get; set; } = "";
        public bool IsDetailView { get; set; } = false;
        public RefundRequest? CurrentRequest { get; set; }
        public List<RefundRequestHistory> RequestHistory { get; set; } = new();

        [TempData]
        public string? StatusMessage { get; set; }

        [TempData]
        public string? StatusMessageClass { get; set; }

        public async Task OnGetAsync(Guid? requestId = null)
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
                    CurrentRequest = await _context.RefundRequests.FirstOrDefaultAsync(r => r.PublicId == requestId.Value);
                    if (CurrentRequest != null)
                    {
                        IsDetailView = true;
                        // Load request history
                        RequestHistory = await _context.RefundRequestHistories
                            .Where(h => h.RefundRequestId == CurrentRequest.Id)
                            .OrderBy(h => h.Timestamp)
                            .ToListAsync();
                    }
                }

                // Filter requests based on user role
                if (await _userManager.IsInRoleAsync(currentUser, "Staff Claims Unit"))
                {
                    // Staff Claims Unit users see requests in Pending Staff Claims Unit status
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

        public async Task<IActionResult> OnPostApproveAsync(Guid requestId, string? claimsRemarks)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                StatusMessage = "User not found.";
                StatusMessageClass = "danger";
                return RedirectToPage("/Dashboard/Approver/Index");
            }

            var request = await _context.RefundRequests.FirstOrDefaultAsync(r => r.PublicId == requestId);
            if (request == null)
            {
                StatusMessage = "Request not found.";
                StatusMessageClass = "danger";
                return RedirectToPage("/Dashboard/Approver/Index");
            }

            // Verify authorization
            var isStaffClaimsUnit = await _userManager.IsInRoleAsync(currentUser, "Staff Claims Unit");
            var isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");

            if (!isStaffClaimsUnit && !isAdmin)
            {
                StatusMessage = "You are not authorized to approve this request.";
                StatusMessageClass = "danger";
                return RedirectToPage("/Dashboard/Approver/Index");
            }

            // Only approve requests in correct status
            if (request.Status != RefundRequestStatus.PendingStaffClaimsUnit)
            {
                StatusMessage = "This request is not in the correct status for approval.";
                StatusMessageClass = "danger";
                return RedirectToPage("/Dashboard/Approver/Index");
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

                // Add history entry
                var historyEntry = new RefundRequestHistory
                {
                    RefundRequestId = request.Id,
                    Action = RefundHistoryActions.ClaimsUnitProcessed,
                    PreviousStatus = RefundRequestStatus.PendingStaffClaimsUnit.ToString(),
                    NewStatus = RefundRequestStatus.PendingPaymentApproval.ToString(),
                    Comments = request.StaffClaimsRemarks,
                    PerformedBy = currentUser.Id,
                    UserName = $"{currentUser.FirstName} {currentUser.LastName}",
                    Timestamp = DateTime.UtcNow,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
                };
                _context.RefundRequestHistories.Add(historyEntry);
                await _context.SaveChangesAsync();

                // Send notification to requester
                await _notificationService.NotifyRefundClaimsUnitApprovedAsync(
                    request.Id,
                    request.RequestedBy ?? "",
                    request.StaffClaimsRemarks
                );

                // Log audit trail
                await _auditLogService.LogRefundRequestApprovedAsync(
                    request.Id,
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
                        request.Id,
                        approver.Id,
                        request.MobileNumberAssignedTo,
                        "Payment Approver",
                        request.PublicId
                    );
                }

                // Send email notifications
                try
                {
                    var requester = await _userManager.FindByIdAsync(request.RequestedBy ?? "");
                    if (requester != null)
                    {
                        // 1. Send claims processed email to requester
                        await SendClaimsProcessedEmailAsync(request, requester);

                        // 2. Send notification to payment approvers
                        foreach (var approver in paymentApprovers)
                        {
                            await SendPaymentApproverNotificationEmailAsync(request, approver);
                        }

                        _logger.LogInformation("Claims processed email notifications sent successfully for refund request {RequestId}", requestId);
                    }
                }
                catch (Exception emailEx)
                {
                    _logger.LogError(emailEx, "Failed to send claims processed email notifications for request {RequestId}", requestId);
                }

                StatusMessage = $"Request #{request.Id} has been approved and forwarded to Payment Approval.";
                StatusMessageClass = "success";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error approving request: {ex.Message}";
                StatusMessageClass = "danger";
            }

            return RedirectToPage("/Dashboard/Approver/Index");
        }

        public async Task<IActionResult> OnPostRevertToRequestorAsync(Guid requestId, string? claimsRemarks)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                StatusMessage = "User not found.";
                StatusMessageClass = "danger";
                return RedirectToPage("/Dashboard/Approver/Index");
            }

            var request = await _context.RefundRequests.FirstOrDefaultAsync(r => r.PublicId == requestId);
            if (request == null)
            {
                StatusMessage = "Request not found.";
                StatusMessageClass = "danger";
                return RedirectToPage("/Dashboard/Approver/Index");
            }

            // Verify authorization
            var isStaffClaimsUnit = await _userManager.IsInRoleAsync(currentUser, "Staff Claims Unit");
            var isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");

            if (!isStaffClaimsUnit && !isAdmin)
            {
                StatusMessage = "You are not authorized to revert this request.";
                StatusMessageClass = "danger";
                return RedirectToPage("/Dashboard/Approver/Index");
            }

            try
            {
                var previousStatus = request.Status.ToString();
                request.Status = RefundRequestStatus.Draft;
                request.SubmittedToSupervisor = false;
                request.StaffClaimsRemarks = claimsRemarks ?? "";
                request.StaffClaimsApprovalDate = DateTime.UtcNow;
                request.StaffClaimsOfficerName = $"{currentUser.FirstName} {currentUser.LastName}";
                request.StaffClaimsOfficerEmail = currentUser.Email ?? string.Empty;

                await _context.SaveChangesAsync();

                // Add history entry
                var historyEntry = new RefundRequestHistory
                {
                    RefundRequestId = request.Id,
                    Action = RefundHistoryActions.ClaimsUnitRevertedToRequestor,
                    PreviousStatus = previousStatus,
                    NewStatus = RefundRequestStatus.Draft.ToString(),
                    Comments = claimsRemarks,
                    PerformedBy = currentUser.Id,
                    UserName = $"{currentUser.FirstName} {currentUser.LastName}",
                    Timestamp = DateTime.UtcNow,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
                };
                _context.RefundRequestHistories.Add(historyEntry);
                await _context.SaveChangesAsync();

                StatusMessage = $"Request #{request.Id} has been reverted to the requestor for revision.";
                StatusMessageClass = "success";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error reverting request: {ex.Message}";
                StatusMessageClass = "danger";
            }

            return RedirectToPage("/Dashboard/Approver/Index");
        }

        public async Task<IActionResult> OnPostRejectAsync(Guid requestId, string? claimsRemarks)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                StatusMessage = "User not found.";
                StatusMessageClass = "danger";
                return RedirectToPage("/Dashboard/Approver/Index");
            }

            var request = await _context.RefundRequests.FirstOrDefaultAsync(r => r.PublicId == requestId);
            if (request == null)
            {
                StatusMessage = "Request not found.";
                StatusMessageClass = "danger";
                return RedirectToPage("/Dashboard/Approver/Index");
            }

            // Verify authorization
            var isStaffClaimsUnit = await _userManager.IsInRoleAsync(currentUser, "Staff Claims Unit");
            var isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");

            if (!isStaffClaimsUnit && !isAdmin)
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
                request.CancellationReason = claimsRemarks ?? "";
                request.CancelledBy = $"{currentUser.FirstName} {currentUser.LastName}";

                await _context.SaveChangesAsync();

                // Add history entry
                var historyEntry = new RefundRequestHistory
                {
                    RefundRequestId = request.Id,
                    Action = RefundHistoryActions.ClaimsUnitRejected,
                    PreviousStatus = previousStatus,
                    NewStatus = RefundRequestStatus.Cancelled.ToString(),
                    Comments = claimsRemarks,
                    PerformedBy = currentUser.Id,
                    UserName = $"{currentUser.FirstName} {currentUser.LastName}",
                    Timestamp = DateTime.UtcNow,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
                };
                _context.RefundRequestHistories.Add(historyEntry);
                await _context.SaveChangesAsync();

                // Send notification to requester
                await _notificationService.NotifyRefundClaimsUnitRejectedAsync(
                    request.Id,
                    request.RequestedBy ?? "",
                    claimsRemarks
                );

                // Log audit trail
                await _auditLogService.LogRefundRequestRejectedAsync(
                    request.Id,
                    "Claims Unit Approver",
                    $"{currentUser.FirstName} {currentUser.LastName}",
                    request.MobileNumberAssignedTo ?? "N/A",
                    request.DevicePurchaseAmount,
                    claimsRemarks,
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

                StatusMessage = $"Request #{request.Id} has been rejected.";
                StatusMessageClass = "success";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error rejecting request: {ex.Message}";
                StatusMessageClass = "danger";
            }

            return RedirectToPage("/Dashboard/Approver/Index");
        }

        public async Task<IActionResult> OnPostRevertToBudgetOfficerAsync(Guid requestId, string? claimsRemarks)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                StatusMessage = "User not found.";
                StatusMessageClass = "danger";
                return RedirectToPage("/Dashboard/Approver/Index");
            }

            var request = await _context.RefundRequests.FirstOrDefaultAsync(r => r.PublicId == requestId);
            if (request == null)
            {
                StatusMessage = "Request not found.";
                StatusMessageClass = "danger";
                return RedirectToPage("/Dashboard/Approver/Index");
            }

            // Verify authorization
            var isStaffClaimsUnit = await _userManager.IsInRoleAsync(currentUser, "Staff Claims Unit");
            var isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");

            if (!isStaffClaimsUnit && !isAdmin)
            {
                StatusMessage = "You are not authorized to revert this request.";
                StatusMessageClass = "danger";
                return RedirectToPage("/Dashboard/Approver/Index");
            }

            try
            {
                var previousStatus = request.Status.ToString();
                request.Status = RefundRequestStatus.PendingBudgetOfficer;
                request.StaffClaimsRemarks = claimsRemarks ?? "";
                request.StaffClaimsApprovalDate = DateTime.UtcNow;
                request.StaffClaimsOfficerName = $"{currentUser.FirstName} {currentUser.LastName}";
                request.StaffClaimsOfficerEmail = currentUser.Email ?? string.Empty;

                await _context.SaveChangesAsync();

                // Add history entry
                var historyEntry = new RefundRequestHistory
                {
                    RefundRequestId = request.Id,
                    Action = RefundHistoryActions.ClaimsUnitRevertedToBudgetOfficer,
                    PreviousStatus = previousStatus,
                    NewStatus = RefundRequestStatus.PendingBudgetOfficer.ToString(),
                    Comments = claimsRemarks,
                    PerformedBy = currentUser.Id,
                    UserName = $"{currentUser.FirstName} {currentUser.LastName}",
                    Timestamp = DateTime.UtcNow,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
                };
                _context.RefundRequestHistories.Add(historyEntry);
                await _context.SaveChangesAsync();

                StatusMessage = $"Request #{request.Id} has been reverted to Budget Officer for review.";
                StatusMessageClass = "success";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error reverting request: {ex.Message}";
                StatusMessageClass = "danger";
            }

            return RedirectToPage("/Dashboard/Approver/Index");
        }

        public async Task<IActionResult> OnGetRequestDetailsAsync(Guid requestId)
        {
            var request = await _context.RefundRequests.FirstOrDefaultAsync(r => r.PublicId == requestId);
            if (request == null)
            {
                return new JsonResult(new { success = false, message = "Request not found" });
            }

            return new JsonResult(new
            {
                success = true,
                id = request.Id,
                publicId = request.PublicId,
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

        private async Task SendClaimsProcessedEmailAsync(RefundRequest request, ApplicationUser requester)
        {
            var placeholders = new Dictionary<string, string>
            {
                { "RequestId", request.Id.ToString() },
                { "RequesterName", request.MobileNumberAssignedTo ?? $"{requester.FirstName} {requester.LastName}" },
                { "RefundUsdAmount", request.RefundUsdAmount?.ToString("N2") ?? "0.00" },
                { "UmojaPaymentDocumentId", request.UmojaPaymentDocumentId ?? "Pending" },
                { "ClaimsActionDate", request.ClaimsActionDate?.ToString("MMMM dd, yyyy") ?? DateTime.UtcNow.ToString("MMMM dd, yyyy") },
                { "ViewRequestLink", $"{Request.Scheme}://{Request.Host}/Modules/RefundManagement/Requests/Index" },
                { "Year", DateTime.Now.Year.ToString() }
            };

            await _emailService.SendTemplatedEmailAsync(
                to: requester.Email ?? "",
                templateCode: "REFUND_CLAIMS_PROCESSED",
                data: placeholders
            );
        }

        private async Task SendPaymentApproverNotificationEmailAsync(RefundRequest request, ApplicationUser approver)
        {
            var placeholders = new Dictionary<string, string>
            {
                { "RequestId", request.Id.ToString() },
                { "RequesterName", request.MobileNumberAssignedTo ?? "Staff Member" },
                { "RefundUsdAmount", request.RefundUsdAmount?.ToString("N2") ?? "0.00" },
                { "UmojaPaymentDocumentId", request.UmojaPaymentDocumentId ?? "Pending" },
                { "ApprovalLink", $"{Request.Scheme}://{Request.Host}/Modules/RefundManagement/Approvals/PaymentApprover" },
                { "Year", DateTime.Now.Year.ToString() }
            };

            await _emailService.SendTemplatedEmailAsync(
                to: approver.Email ?? "",
                templateCode: "REFUND_PAYMENT_APPROVER_NOTIFICATION",
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