using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TAB.Web.Data;
using TAB.Web.Models;
using TAB.Web.Services;

namespace TAB.Web.Pages.Dashboard.Approver
{
    [Authorize(Roles = "Admin,ICTS,User,Supervisor,ICTS Service Desk,Claims Unit Approver,Manager,Staff Claims Unit,Budget Officer,BudgetOfficer")] // Allow all user roles
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ISimRequestHistoryService _historyService;

        public IndexModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager, ISimRequestHistoryService historyService)
        {
            _context = context;
            _userManager = userManager;
            _historyService = historyService;
        }

        // Dashboard Statistics
        public int TotalPendingRequests { get; set; }
        public int TotalSimRequests { get; set; }
        public int TotalRefundRequests { get; set; }
        public int TotalEBillRequests { get; set; }
        public int TotalIctsRequests { get; set; }

        // Recent Activity
        public List<UnifiedRequest> RecentRequests { get; set; } = new();
        public List<UnifiedRequest> UrgentRequests { get; set; } = new();

        // Status Messages
        [TempData]
        public string? StatusMessage { get; set; }
        [TempData]
        public string? StatusMessageClass { get; set; }

        // User Information
        public string CurrentUserName { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Challenge();
            }

            // Check if user has active status
            if (currentUser.Status != UserStatus.Active)
            {
                TempData["ErrorMessage"] = "Your account is not active. Please contact the administrator.";
                return RedirectToPage("/Account/AccessDenied");
            }

            // Set current user name for display
            CurrentUserName = $"{currentUser.FirstName} {currentUser.LastName}";

            await LoadDashboardStatisticsAsync(currentUser.Email ?? string.Empty);
            await LoadRecentActivityAsync(currentUser.Email ?? string.Empty);

            return Page();
        }



        private async Task LoadDashboardStatisticsAsync(string userEmail)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            bool isAdmin = await _userManager.IsInRoleAsync(currentUser!, "Admin");
            bool isICTS = await _userManager.IsInRoleAsync(currentUser!, "ICTS") || 
                         await _userManager.IsInRoleAsync(currentUser!, "ICTS Service Desk");
            bool isBudgetOfficer = await _userManager.IsInRoleAsync(currentUser!, "Budget Officer") || 
                                   await _userManager.IsInRoleAsync(currentUser!, "BudgetOfficer");
            bool isClaimsUnit = await _userManager.IsInRoleAsync(currentUser!, "Claims Unit Approver") ||
                               await _userManager.IsInRoleAsync(currentUser!, "Staff Claims Unit");
            bool isSupervisor = await _userManager.IsInRoleAsync(currentUser!, "Supervisor");
            bool isManager = await _userManager.IsInRoleAsync(currentUser!, "Manager");

            if (isAdmin)
            {
                // Admins see all pending requests across the system
                var pendingSimRequests = await _context.SimRequests
                    .Where(r => r.Status == RequestStatus.PendingSupervisor)
                    .CountAsync();

                var ictsSimRequests = await _context.SimRequests
                    .Where(r => r.Status == RequestStatus.PendingAdmin || r.Status == RequestStatus.PendingIcts)
                    .CountAsync();

                // Count call log verifications pending any supervisor approval
                var pendingEBillRequests = await _context.CallLogVerifications
                    .Where(v => v.SubmittedToSupervisor
                        && (v.SupervisorApprovalStatus == null || v.SupervisorApprovalStatus == "Pending"))
                    .CountAsync();
                    
                var pendingRefundRequests = await _context.RefundRequests
                    .Where(r => r.Status != RefundRequestStatus.Completed && 
                               r.Status != RefundRequestStatus.Cancelled &&
                               r.Status != RefundRequestStatus.Draft)
                    .CountAsync();

                TotalSimRequests = pendingSimRequests;
                TotalRefundRequests = pendingRefundRequests;
                TotalEBillRequests = pendingEBillRequests;
                TotalIctsRequests = ictsSimRequests;
                TotalPendingRequests = pendingSimRequests + pendingEBillRequests + pendingRefundRequests;
            }
            else if (isICTS)
            {
                // ICTS staff see only requests pending THEIR action (PendingAdmin/PendingIcts)
                var ictsActionRequests = await _context.SimRequests
                    .Where(r => r.Status == RequestStatus.PendingAdmin || r.Status == RequestStatus.PendingIcts)
                    .CountAsync();

                TotalSimRequests = ictsActionRequests;
                TotalRefundRequests = 0;
                TotalEBillRequests = 0;
                TotalIctsRequests = ictsActionRequests;
                TotalPendingRequests = ictsActionRequests; // Only requests pending ICTS action
            }
            else if (isBudgetOfficer)
            {
                // Budget Officers see only requests pending THEIR budget approval
                var budgetPendingSimRequests = await _context.SimRequests
                    .Where(r => r.Status == RequestStatus.PendingSupervisor && 
                               (r.SupervisorEmail == userEmail || r.Supervisor == userEmail))
                    .CountAsync();

                // Count call log verifications pending THIS budget officer's approval
                var budgetPendingEBillRequests = await _context.CallLogVerifications
                    .Where(v => v.SubmittedToSupervisor
                        && v.SupervisorIndexNumber == userEmail
                        && (v.SupervisorApprovalStatus == null || v.SupervisorApprovalStatus == "Pending"))
                    .CountAsync();
                    
                var budgetPendingRefundRequests = await _context.RefundRequests
                    .Where(r => r.Status == RefundRequestStatus.PendingBudgetOfficer)
                    .CountAsync();

                TotalSimRequests = budgetPendingSimRequests;
                TotalRefundRequests = budgetPendingRefundRequests;
                TotalEBillRequests = budgetPendingEBillRequests;
                TotalIctsRequests = 0;
                TotalPendingRequests = budgetPendingSimRequests + budgetPendingEBillRequests + budgetPendingRefundRequests;
            }
            else if (isClaimsUnit)
            {
                // Claims Unit see only requests pending THEIR claims approval
                var claimsPendingRefundRequests = await _context.RefundRequests
                    .Where(r => r.Status == RefundRequestStatus.PendingStaffClaimsUnit)
                    .CountAsync();

                TotalSimRequests = 0;
                TotalRefundRequests = claimsPendingRefundRequests;
                TotalEBillRequests = 0;
                TotalIctsRequests = 0;
                TotalPendingRequests = claimsPendingRefundRequests;
            }
            else if (isSupervisor || isManager)
            {
                // Supervisors see only requests pending THEIR supervisor approval
                var supervisorPendingSimRequests = await _context.SimRequests
                    .Where(r => r.Status == RequestStatus.PendingSupervisor && 
                               (r.SupervisorEmail == userEmail || r.Supervisor == userEmail))
                    .CountAsync();

                // Count call log verifications pending THIS supervisor's approval
                var supervisorPendingEBillRequests = await _context.CallLogVerifications
                    .Where(v => v.SubmittedToSupervisor
                        && v.SupervisorIndexNumber == userEmail
                        && (v.SupervisorApprovalStatus == null || v.SupervisorApprovalStatus == "Pending"))
                    .CountAsync();

                var supervisorPendingRefundRequests = await _context.RefundRequests
                    .Where(r => r.Status == RefundRequestStatus.PendingSupervisor && 
                               r.SupervisorEmail == userEmail)
                    .CountAsync();

                TotalSimRequests = supervisorPendingSimRequests;
                TotalRefundRequests = supervisorPendingRefundRequests;
                TotalEBillRequests = supervisorPendingEBillRequests;
                TotalIctsRequests = 0;
                TotalPendingRequests = supervisorPendingSimRequests + supervisorPendingEBillRequests + supervisorPendingRefundRequests;
            }
            else
            {
                // Regular users see their own submitted requests (informational view)
                var userSubmittedSimRequests = await _context.SimRequests
                    .Where(r => r.OfficialEmail == userEmail)
                    .CountAsync();

                var userSubmittedEBillRequests = await _context.Ebills
                    .Where(r => r.Email == userEmail)
                    .CountAsync();
                    
                var userId = currentUser?.Id;
                var userSubmittedRefundRequests = await _context.RefundRequests
                    .Where(r => r.RequestedBy == userId)
                    .CountAsync();

                TotalSimRequests = userSubmittedSimRequests;
                TotalRefundRequests = userSubmittedRefundRequests;
                TotalEBillRequests = userSubmittedEBillRequests;
                TotalIctsRequests = 0;
                TotalPendingRequests = 0; // Regular users don't have pending actions, just monitoring their requests
            }
        }

        private async Task LoadRecentActivityAsync(string userEmail)
        {
            var allRequests = new List<UnifiedRequest>();
            var currentUser = await _userManager.GetUserAsync(User);
            bool isAdmin = await _userManager.IsInRoleAsync(currentUser!, "Admin");
            bool isICTS = await _userManager.IsInRoleAsync(currentUser!, "ICTS") || 
                         await _userManager.IsInRoleAsync(currentUser!, "ICTS Service Desk");
            bool isBudgetOfficer = await _userManager.IsInRoleAsync(currentUser!, "Budget Officer") || 
                                   await _userManager.IsInRoleAsync(currentUser!, "BudgetOfficer");
            bool isClaimsUnit = await _userManager.IsInRoleAsync(currentUser!, "Claims Unit Approver") ||
                               await _userManager.IsInRoleAsync(currentUser!, "Staff Claims Unit");
            bool isSupervisor = await _userManager.IsInRoleAsync(currentUser!, "Supervisor");
            bool isManager = await _userManager.IsInRoleAsync(currentUser!, "Manager");

            if (isAdmin)
            {
                // Admins see all recent requests
                var simRequests = await _context.SimRequests
                    .Include(r => r.ServiceProvider)
                    .Where(r => r.Status == RequestStatus.PendingSupervisor || r.Status == RequestStatus.PendingAdmin)
                    .OrderByDescending(r => r.RequestDate)
                    .Take(10)
                    .ToListAsync();

                allRequests.AddRange(simRequests.Select(r => new UnifiedRequest
                {
                    Id = r.Id,
                    RequestType = RequestType.SimCard,
                    StaffName = $"{r.FirstName} {r.LastName}",
                    RequestTitle = $"SIM Card Request - {r.ServiceProvider?.ServiceProviderName ?? "Unknown Provider"}",
                    RequestDate = r.RequestDate,
                    Status = r.Status.ToString(),
                    Priority = GetPriority(r.RequestDate),
                    ServiceProvider = r.ServiceProvider,
                    OriginalRequest = r
                }));

                // Get call log verifications pending approval
                var callLogVerifications = await _context.CallLogVerifications
                    .Include(v => v.CallRecord)
                    .Where(v => v.SubmittedToSupervisor
                        && (v.SupervisorApprovalStatus == null || v.SupervisorApprovalStatus == "Pending"))
                    .OrderByDescending(v => v.SubmittedDate)
                    .Take(10)
                    .ToListAsync();

                allRequests.AddRange(callLogVerifications.Select(v => new UnifiedRequest
                {
                    Id = v.Id,
                    RequestType = RequestType.EBill,
                    StaffName = v.VerifiedBy ?? "Unknown",
                    RequestTitle = $"Call Log Verification - ${v.ActualAmount:N2}",
                    RequestDate = v.SubmittedDate ?? DateTime.Now,
                    Status = "PendingSupervisor",
                    Priority = GetPriority(v.SubmittedDate ?? DateTime.Now),
                    OriginalRequest = v
                }));
                
                // Add RefundRequests for Admin
                var refundRequests = await _context.RefundRequests
                    .Where(r => r.Status != RefundRequestStatus.Completed && 
                               r.Status != RefundRequestStatus.Cancelled &&
                               r.Status != RefundRequestStatus.Draft)
                    .OrderByDescending(r => r.RequestDate)
                    .Take(10)
                    .ToListAsync();

                allRequests.AddRange(refundRequests.Select(r => new UnifiedRequest
                {
                    Id = r.Id,
                    RequestType = RequestType.DeviceRefund,
                    StaffName = r.MobileNumberAssignedTo,
                    RequestTitle = $"Device Refund - {r.DevicePurchaseCurrency} {r.DevicePurchaseAmount:N2}",
                    RequestDate = r.RequestDate,
                    Status = r.Status.ToString(),
                    Priority = GetPriority(r.RequestDate),
                    OriginalRequest = r
                }));
            }
            else if (isICTS)
            {
                // ICTS staff see ONLY requests pending THEIR action
                var simRequests = await _context.SimRequests
                    .Include(r => r.ServiceProvider)
                    .Where(r => r.Status == RequestStatus.PendingAdmin || r.Status == RequestStatus.PendingIcts)
                    .OrderByDescending(r => r.RequestDate)
                    .Take(10)
                    .ToListAsync();

                allRequests.AddRange(simRequests.Select(r => new UnifiedRequest
                {
                    Id = r.Id,
                    RequestType = RequestType.SimCard,
                    StaffName = $"{r.FirstName} {r.LastName}",
                    RequestTitle = $"SIM Card Request - {r.ServiceProvider?.ServiceProviderName ?? "Unknown Provider"} - Pending ICTS Action",
                    RequestDate = r.RequestDate,
                    Status = r.Status.ToString(),
                    Priority = GetPriority(r.RequestDate),
                    ServiceProvider = r.ServiceProvider,
                    OriginalRequest = r
                }));
            }
            else if (isBudgetOfficer)
            {
                // Budget Officers see ONLY requests pending THEIR budget approval
                var simRequests = await _context.SimRequests
                    .Include(r => r.ServiceProvider)
                    .Where(r => r.Status == RequestStatus.PendingSupervisor &&
                               (r.SupervisorEmail == userEmail || r.Supervisor == userEmail))
                    .OrderByDescending(r => r.RequestDate)
                    .Take(10)
                    .ToListAsync();

                allRequests.AddRange(simRequests.Select(r => new UnifiedRequest
                {
                    Id = r.Id,
                    RequestType = RequestType.SimCard,
                    StaffName = $"{r.FirstName} {r.LastName}",
                    RequestTitle = $"SIM Card Request - {r.ServiceProvider?.ServiceProviderName ?? "Unknown Provider"} - Pending Budget Approval",
                    RequestDate = r.RequestDate,
                    Status = r.Status.ToString(),
                    Priority = GetPriority(r.RequestDate),
                    ServiceProvider = r.ServiceProvider,
                    OriginalRequest = r
                }));

                // Get call log verifications pending THIS budget officer's approval
                var callLogVerifications = await _context.CallLogVerifications
                    .Include(v => v.CallRecord)
                    .Where(v => v.SubmittedToSupervisor
                        && v.SupervisorIndexNumber == userEmail
                        && (v.SupervisorApprovalStatus == null || v.SupervisorApprovalStatus == "Pending"))
                    .OrderByDescending(v => v.SubmittedDate)
                    .Take(10)
                    .ToListAsync();

                allRequests.AddRange(callLogVerifications.Select(v => new UnifiedRequest
                {
                    Id = v.Id,
                    RequestType = RequestType.EBill,
                    StaffName = v.VerifiedBy ?? "Unknown",
                    RequestTitle = $"Call Log Verification - ${v.ActualAmount:N2} - Pending Budget Approval",
                    RequestDate = v.SubmittedDate ?? DateTime.Now,
                    Status = "PendingSupervisor",
                    Priority = GetPriority(v.SubmittedDate ?? DateTime.Now),
                    OriginalRequest = v
                }));

                // TODO: Add refund requests pending budget officer approval when model is available
                // var refundRequests = await _context.RefundRequests
                //     .Where(r => r.Status == RefundRequestStatus.PendingBudgetOfficer && 
                //                r.BudgetOfficerEmail == userEmail)
                //     .OrderByDescending(r => r.RequestDate)
                //     .Take(10)
                //     .ToListAsync();
            }
            else if (isClaimsUnit)
            {
                // Claims Unit see ONLY requests pending THEIR claims approval
                // TODO: Add refund requests pending claims unit approval when model is available
                // var refundRequests = await _context.RefundRequests
                //     .Where(r => r.Status == RefundRequestStatus.PendingStaffClaimsUnit && 
                //                r.ClaimsUnitEmail == userEmail)
                //     .OrderByDescending(r => r.RequestDate)
                //     .Take(10)
                //     .ToListAsync();
            }
            else if (isSupervisor || isManager)
            {
                // Supervisors see ONLY requests pending THEIR supervisor approval
                var simRequests = await _context.SimRequests
                    .Include(r => r.ServiceProvider)
                    .Where(r => r.Status == RequestStatus.PendingSupervisor &&
                               (r.SupervisorEmail == userEmail || r.Supervisor == userEmail))
                    .OrderByDescending(r => r.RequestDate)
                    .Take(10)
                    .ToListAsync();

                allRequests.AddRange(simRequests.Select(r => new UnifiedRequest
                {
                    Id = r.Id,
                    RequestType = RequestType.SimCard,
                    StaffName = $"{r.FirstName} {r.LastName}",
                    RequestTitle = $"SIM Card Request - {r.ServiceProvider?.ServiceProviderName ?? "Unknown Provider"} - Pending Supervisor Approval",
                    RequestDate = r.RequestDate,
                    Status = r.Status.ToString(),
                    Priority = GetPriority(r.RequestDate),
                    ServiceProvider = r.ServiceProvider,
                    OriginalRequest = r
                }));

                // Get call log verifications pending THIS supervisor's approval
                var callLogVerifications = await _context.CallLogVerifications
                    .Include(v => v.CallRecord)
                    .Where(v => v.SubmittedToSupervisor
                        && v.SupervisorIndexNumber == userEmail
                        && (v.SupervisorApprovalStatus == null || v.SupervisorApprovalStatus == "Pending"))
                    .OrderByDescending(v => v.SubmittedDate)
                    .Take(10)
                    .ToListAsync();

                allRequests.AddRange(callLogVerifications.Select(v => new UnifiedRequest
                {
                    Id = v.Id,
                    RequestType = RequestType.EBill,
                    StaffName = v.VerifiedBy ?? "Unknown",
                    RequestTitle = $"Call Log Verification - ${v.ActualAmount:N2} - Pending Supervisor Approval",
                    RequestDate = v.SubmittedDate ?? DateTime.Now,
                    Status = "PendingSupervisor",
                    Priority = GetPriority(v.SubmittedDate ?? DateTime.Now),
                    OriginalRequest = v
                }));

                // TODO: Add refund requests pending supervisor approval when model is available
                // var refundRequests = await _context.RefundRequests
                //     .Where(r => r.Status == RefundRequestStatus.PendingSupervisor && 
                //                r.SupervisorEmail == userEmail)
                //     .OrderByDescending(r => r.RequestDate)
                //     .Take(10)
                //     .ToListAsync();
            }
            else
            {
                // Regular users see their own submitted requests (monitoring view, not action required)
                var simRequests = await _context.SimRequests
                    .Include(r => r.ServiceProvider)
                    .Where(r => r.OfficialEmail == userEmail)
                    .OrderByDescending(r => r.RequestDate)
                    .Take(10)
                    .ToListAsync();

                allRequests.AddRange(simRequests.Select(r => new UnifiedRequest
                {
                    Id = r.Id,
                    RequestType = RequestType.SimCard,
                    StaffName = $"{r.FirstName} {r.LastName}",
                    RequestTitle = $"My SIM Card Request - {r.ServiceProvider?.ServiceProviderName ?? "Unknown Provider"}",
                    RequestDate = r.RequestDate,
                    Status = r.Status.ToString(),
                    Priority = GetPriority(r.RequestDate),
                    ServiceProvider = r.ServiceProvider,
                    OriginalRequest = r
                }));

                var eBillRequests = await _context.Ebills
                    .Where(r => r.Email == userEmail)
                    .OrderByDescending(r => r.RequestDate)
                    .Take(10)
                    .ToListAsync();

                allRequests.AddRange(eBillRequests.Select(r => new UnifiedRequest
                {
                    Id = r.Id,
                    RequestType = RequestType.EBill,
                    StaffName = r.FullName,
                    RequestTitle = $"My E-Bill Request - {r.ServiceProvider}",
                    RequestDate = r.RequestDate,
                    Status = r.Status.ToString(),
                    Priority = GetPriority(r.RequestDate),
                    OriginalRequest = r
                }));
            }

            // Sort all requests by date and take most recent
            RecentRequests = allRequests
                .OrderByDescending(r => r.RequestDate)
                .Take(5)
                .ToList();

            // Identify urgent requests (older than 3 days)
            var urgentCutoff = DateTime.Now.AddDays(-3);
            UrgentRequests = allRequests
                .Where(r => r.RequestDate < urgentCutoff)
                .OrderBy(r => r.RequestDate)
                .Take(10)
                .ToList();

            // Get urgent requests (older than 3 days)
            UrgentRequests = allRequests
                .Where(r => r.Priority == "Urgent" || r.Priority == "Attention")
                .OrderByDescending(r => r.RequestDate)
                .Take(5)
                .ToList();
        }

        private string GetPriority(DateTime requestDate)
        {
            var daysSinceRequest = (DateTime.UtcNow - requestDate).Days;
            return daysSinceRequest switch
            {
                > 7 => "Urgent",
                > 3 => "Attention",
                _ => "Normal"
            };
        }

        public string GetRequestTypeColor(RequestType requestType)
        {
            return requestType switch
            {
                RequestType.SimCard => "primary",
                RequestType.DeviceRefund => "success",
                RequestType.EBill => "warning",
                _ => "secondary"
            };
        }

        public string GetPriorityColor(string priority)
        {
            return priority switch
            {
                "Urgent" => "danger",
                "Attention" => "warning",
                _ => "primary"
            };
        }

        public string GetStatusBadgeClass(string status)
        {
            return status switch
            {
                "PendingSupervisor" => "bg-warning text-dark",
                "PendingAdmin" => "bg-info text-white",
                "Approved" => "bg-success text-white",
                "Rejected" => "bg-danger text-white",
                "Completed" => "bg-primary text-white",
                _ => "bg-secondary text-white"
            };
        }
    }
}
