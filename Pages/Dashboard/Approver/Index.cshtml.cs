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
    [Authorize] // Allow all authenticated users - role-based filtering handled in data logic
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

        // All Pending Requests (for combined table view)
        public List<UnifiedRequest> AllPendingRequests { get; set; } = new();

        // Pending Call Log Submissions by Staff
        public List<PendingCallLogStaff> PendingCallLogStaff { get; set; } = new();

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
            await LoadPendingCallLogStaffAsync(currentUser.Email ?? string.Empty);

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

            // Dynamic supervisor detection - check if user's email is assigned as supervisor on any pending request
            bool isDynamicSupervisor = false;
            if (!isAdmin && !isICTS && !isBudgetOfficer && !isClaimsUnit && !isSupervisor && !isManager)
            {
                // Check if this user's email is a supervisor on any pending SIM requests
                var hasSupervisorAssignments = await _context.SimRequests
                    .AnyAsync(r => (r.SupervisorEmail == userEmail || r.Supervisor == userEmail) &&
                                   r.Status == RequestStatus.PendingSupervisor);

                // Check if this user's email is a supervisor on any pending call log verifications
                var hasEbillSupervisorAssignments = await _context.CallLogVerifications
                    .AnyAsync(v => v.SupervisorEmail == userEmail &&
                                   v.SubmittedToSupervisor &&
                                   (v.SupervisorApprovalStatus == null || v.SupervisorApprovalStatus == "Pending"));

                // Check if this user's email is a supervisor on any pending refund requests
                var hasRefundSupervisorAssignments = await _context.RefundRequests
                    .AnyAsync(r => r.SupervisorEmail == userEmail &&
                                   r.Status == RefundRequestStatus.PendingSupervisor);

                isDynamicSupervisor = hasSupervisorAssignments || hasEbillSupervisorAssignments || hasRefundSupervisorAssignments;
            }

            if (isAdmin)
            {
                // Admins see all pending requests across the system
                var pendingSimRequests = await _context.SimRequests
                    .Where(r => r.Status == RequestStatus.PendingSupervisor)
                    .CountAsync();

                var ictsSimRequests = await _context.SimRequests
                    .Where(r => r.Status == RequestStatus.PendingAdmin || r.Status == RequestStatus.PendingIcts)
                    .CountAsync();

                // Count distinct staff with pending call log approvals (for admin view)
                var pendingEBillRequests = await _context.CallLogVerifications
                    .Where(v => v.SubmittedToSupervisor
                        && (v.SupervisorApprovalStatus == null || v.SupervisorApprovalStatus == "Pending"))
                    .Select(v => v.VerifiedBy)
                    .Distinct()
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
                // ICTS staff see requests at all ICTS workflow stages
                var ictsActionRequests = await _context.SimRequests
                    .Where(r => r.Status == RequestStatus.PendingAdmin ||
                               r.Status == RequestStatus.PendingIcts ||
                               r.Status == RequestStatus.PendingServiceProvider ||
                               r.Status == RequestStatus.PendingSIMCollection)
                    .CountAsync();

                // ICTS staff may also be supervisors - count distinct staff with pending call log approvals
                var ictsPendingEBillStaffCount = await _context.CallLogVerifications
                    .Where(v => v.SubmittedToSupervisor
                        && v.SupervisorEmail == userEmail
                        && (v.SupervisorApprovalStatus == null || v.SupervisorApprovalStatus == "Pending"))
                    .Select(v => v.VerifiedBy)
                    .Distinct()
                    .CountAsync();

                TotalSimRequests = ictsActionRequests;
                TotalRefundRequests = 0;
                TotalEBillRequests = ictsPendingEBillStaffCount;
                TotalIctsRequests = ictsActionRequests;
                TotalPendingRequests = ictsActionRequests + ictsPendingEBillStaffCount;
            }
            else if (isBudgetOfficer)
            {
                // Budget Officers see only requests pending THEIR budget approval
                var budgetPendingSimRequests = await _context.SimRequests
                    .Where(r => r.Status == RequestStatus.PendingSupervisor && 
                               (r.SupervisorEmail == userEmail || r.Supervisor == userEmail))
                    .CountAsync();

                // Count distinct staff with pending call log approvals for THIS budget officer
                var budgetPendingEBillRequests = await _context.CallLogVerifications
                    .Where(v => v.SubmittedToSupervisor
                        && v.SupervisorEmail == userEmail
                        && (v.SupervisorApprovalStatus == null || v.SupervisorApprovalStatus == "Pending"))
                    .Select(v => v.VerifiedBy)
                    .Distinct()
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
                // Claims Unit see requests pending THEIR claims approval
                var claimsPendingRefundRequests = await _context.RefundRequests
                    .Where(r => r.Status == RefundRequestStatus.PendingStaffClaimsUnit)
                    .CountAsync();

                // Claims Unit staff may also be supervisors - count distinct staff with pending call log approvals
                var claimsPendingEBillStaffCount = await _context.CallLogVerifications
                    .Where(v => v.SubmittedToSupervisor
                        && v.SupervisorEmail == userEmail
                        && (v.SupervisorApprovalStatus == null || v.SupervisorApprovalStatus == "Pending"))
                    .Select(v => v.VerifiedBy)
                    .Distinct()
                    .CountAsync();

                TotalSimRequests = 0;
                TotalRefundRequests = claimsPendingRefundRequests;
                TotalEBillRequests = claimsPendingEBillStaffCount;
                TotalIctsRequests = 0;
                TotalPendingRequests = claimsPendingRefundRequests + claimsPendingEBillStaffCount;
            }
            else if (isSupervisor || isManager || isDynamicSupervisor)
            {
                // Supervisors (with role OR dynamically detected) see only requests pending THEIR supervisor approval
                var supervisorPendingSimRequests = await _context.SimRequests
                    .Where(r => r.Status == RequestStatus.PendingSupervisor &&
                               (r.SupervisorEmail == userEmail || r.Supervisor == userEmail))
                    .CountAsync();

                // Count distinct staff with pending call log approvals for THIS supervisor
                var supervisorPendingEBillRequests = await _context.CallLogVerifications
                    .Where(v => v.SubmittedToSupervisor
                        && v.SupervisorEmail == userEmail
                        && (v.SupervisorApprovalStatus == null || v.SupervisorApprovalStatus == "Pending"))
                    .Select(v => v.VerifiedBy)
                    .Distinct()
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

            // Dynamic supervisor detection - check if user's email is assigned as supervisor on any pending request
            bool isDynamicSupervisor = false;
            if (!isAdmin && !isICTS && !isBudgetOfficer && !isClaimsUnit && !isSupervisor && !isManager)
            {
                // Check if this user's email is a supervisor on any pending SIM requests
                var hasSupervisorAssignments = await _context.SimRequests
                    .AnyAsync(r => (r.SupervisorEmail == userEmail || r.Supervisor == userEmail) &&
                                   r.Status == RequestStatus.PendingSupervisor);

                // Check if this user's email is a supervisor on any pending call log verifications
                var hasEbillSupervisorAssignments = await _context.CallLogVerifications
                    .AnyAsync(v => v.SupervisorEmail == userEmail &&
                                   v.SubmittedToSupervisor &&
                                   (v.SupervisorApprovalStatus == null || v.SupervisorApprovalStatus == "Pending"));

                // Check if this user's email is a supervisor on any pending refund requests
                var hasRefundSupervisorAssignments = await _context.RefundRequests
                    .AnyAsync(r => r.SupervisorEmail == userEmail &&
                                   r.Status == RefundRequestStatus.PendingSupervisor);

                isDynamicSupervisor = hasSupervisorAssignments || hasEbillSupervisorAssignments || hasRefundSupervisorAssignments;
            }

            if (isAdmin)
            {
                // Admins see all recent requests
                var simRequests = await _context.SimRequests
                    .Include(r => r.ServiceProvider)
                    .Where(r => r.Status == RequestStatus.PendingSupervisor || r.Status == RequestStatus.PendingAdmin)
                    .OrderByDescending(r => r.RequestDate)
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
                // ICTS staff see requests at all ICTS workflow stages
                var simRequests = await _context.SimRequests
                    .Include(r => r.ServiceProvider)
                    .Where(r => r.Status == RequestStatus.PendingAdmin ||
                               r.Status == RequestStatus.PendingIcts ||
                               r.Status == RequestStatus.PendingServiceProvider ||
                               r.Status == RequestStatus.PendingSIMCollection)
                    .OrderByDescending(r => r.RequestDate)
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

                // ICTS staff may also be supervisors - include call log verifications assigned to them
                var ictsCallLogVerifications = await _context.CallLogVerifications
                    .Include(v => v.CallRecord)
                    .Where(v => v.SubmittedToSupervisor
                        && v.SupervisorEmail == userEmail
                        && (v.SupervisorApprovalStatus == null || v.SupervisorApprovalStatus == "Pending"))
                    .OrderByDescending(v => v.SubmittedDate)
                    .ToListAsync();

                allRequests.AddRange(ictsCallLogVerifications.Select(v => new UnifiedRequest
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
            }
            else if (isBudgetOfficer)
            {
                // Budget Officers see ONLY requests pending THEIR budget approval
                var simRequests = await _context.SimRequests
                    .Include(r => r.ServiceProvider)
                    .Where(r => r.Status == RequestStatus.PendingSupervisor &&
                               (r.SupervisorEmail == userEmail || r.Supervisor == userEmail))
                    .OrderByDescending(r => r.RequestDate)
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
                        && v.SupervisorEmail == userEmail
                        && (v.SupervisorApprovalStatus == null || v.SupervisorApprovalStatus == "Pending"))
                    .OrderByDescending(v => v.SubmittedDate)
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
                // Claims Unit see requests pending THEIR claims approval
                // TODO: Add refund requests pending claims unit approval when model is available

                // Claims Unit staff may also be supervisors - include call log verifications assigned to them
                var claimsCallLogVerifications = await _context.CallLogVerifications
                    .Include(v => v.CallRecord)
                    .Where(v => v.SubmittedToSupervisor
                        && v.SupervisorEmail == userEmail
                        && (v.SupervisorApprovalStatus == null || v.SupervisorApprovalStatus == "Pending"))
                    .OrderByDescending(v => v.SubmittedDate)
                    .ToListAsync();

                allRequests.AddRange(claimsCallLogVerifications.Select(v => new UnifiedRequest
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
            }
            else if (isSupervisor || isManager || isDynamicSupervisor)
            {
                // Supervisors (with role OR dynamically detected) see ONLY requests pending THEIR supervisor approval
                var simRequests = await _context.SimRequests
                    .Include(r => r.ServiceProvider)
                    .Where(r => r.Status == RequestStatus.PendingSupervisor &&
                               (r.SupervisorEmail == userEmail || r.Supervisor == userEmail))
                    .OrderByDescending(r => r.RequestDate)
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
                        && v.SupervisorEmail == userEmail
                        && (v.SupervisorApprovalStatus == null || v.SupervisorApprovalStatus == "Pending"))
                    .OrderByDescending(v => v.SubmittedDate)
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

            // Store all pending requests for the combined table view
            AllPendingRequests = allRequests
                .OrderByDescending(r => r.RequestDate)
                .ToList();

            // Sort all requests by date and take most recent
            RecentRequests = allRequests
                .OrderByDescending(r => r.RequestDate)
                .Take(5)
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

        private async Task LoadPendingCallLogStaffAsync(string userEmail)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            bool isAdmin = currentUser != null && await _userManager.IsInRoleAsync(currentUser, "Admin");

            // Get pending call log submissions grouped by staff
            // Admins see all pending call logs; other roles see only those assigned to them
            var query = _context.CallLogVerifications
                .Include(v => v.CallRecord)
                .Where(v => v.SubmittedToSupervisor
                    && (v.SupervisorApprovalStatus == null || v.SupervisorApprovalStatus == "Pending"));

            if (!isAdmin)
            {
                query = query.Where(v => v.SupervisorEmail == userEmail);
            }

            var pendingVerifications = await query.ToListAsync();

            // Group by staff (VerifiedBy)
            var staffGroups = pendingVerifications
                .GroupBy(v => v.VerifiedBy)
                .Select(g => new PendingCallLogStaff
                {
                    IndexNumber = g.Key ?? "Unknown",
                    TotalRecords = g.Count(),
                    TotalAmount = g.Sum(v => v.ActualAmount),
                    SubmittedDate = g.Min(v => v.SubmittedDate) ?? DateTime.Now
                })
                .ToList();

            // Get staff names from EbillUsers
            var indexNumbers = staffGroups.Select(s => s.IndexNumber).ToList();
            var ebillUsers = await _context.EbillUsers
                .Where(u => indexNumbers.Contains(u.IndexNumber))
                .ToDictionaryAsync(u => u.IndexNumber, u => $"{u.FirstName} {u.LastName}");

            foreach (var staff in staffGroups)
            {
                staff.StaffName = ebillUsers.TryGetValue(staff.IndexNumber, out var name) ? name : staff.IndexNumber;
            }

            PendingCallLogStaff = staffGroups.OrderBy(s => s.SubmittedDate).ToList();
        }
    }

    public class PendingCallLogStaff
    {
        public string IndexNumber { get; set; } = string.Empty;
        public string StaffName { get; set; } = string.Empty;
        public int TotalRecords { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime SubmittedDate { get; set; }
        public int DaysPending => (DateTime.Now - SubmittedDate).Days;
    }
}
