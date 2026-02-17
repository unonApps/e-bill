using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TAB.Web.Data;
using TAB.Web.Models;

namespace TAB.Web.ViewComponents
{
    public class PendingRequestCountsViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public PendingRequestCountsViewComponent(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var counts = new PendingRequestCounts();

            // Check if user is authenticated
            if (!UserClaimsPrincipal.Identity?.IsAuthenticated == true)
            {
                return View(counts);
            }

            var currentUser = await _userManager.GetUserAsync(UserClaimsPrincipal);
            if (currentUser == null)
            {
                return View(counts);
            }

            var userEmail = currentUser.Email ?? string.Empty;

            // Check if user has an e-bill account
            counts.HasEbillAccount = currentUser.EbillUserId.HasValue;

            // Count pending payment assignments for this user
            if (counts.HasEbillAccount && currentUser.EbillUserId.HasValue)
            {
                var ebillUser = await _context.EbillUsers
                    .FirstOrDefaultAsync(u => u.Id == currentUser.EbillUserId.Value);

                if (ebillUser != null)
                {
                    counts.PaymentAssignmentsCount = await _context.CallLogPaymentAssignments
                        .Where(a => a.AssignedTo == ebillUser.IndexNumber && a.AssignmentStatus == "Pending")
                        .CountAsync();
                }
            }

            bool isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");
            bool isICTS = await _userManager.IsInRoleAsync(currentUser, "ICTS") ||
                         await _userManager.IsInRoleAsync(currentUser, "ICTS Service Desk");
            bool isBudgetOfficer = await _userManager.IsInRoleAsync(currentUser, "Budget Officer") ||
                                   await _userManager.IsInRoleAsync(currentUser, "BudgetOfficer");
            bool isStaffClaimsUnit = await _userManager.IsInRoleAsync(currentUser, "Staff Claims Unit");
            bool isPaymentApprover = await _userManager.IsInRoleAsync(currentUser, "Claims Unit Approver");
            bool isSupervisor = await _userManager.IsInRoleAsync(currentUser, "Supervisor");
            bool isManager = await _userManager.IsInRoleAsync(currentUser, "Manager");

            if (isAdmin)
            {
                // Admins see all pending requests across the system
                counts.SimRequestCount = await _context.SimRequests
                    .Where(r => r.Status == RequestStatus.PendingSupervisor)
                    .CountAsync();

                counts.RefundRequestCount = await _context.RefundRequests
                    .Where(r => r.Status != RefundRequestStatus.Completed &&
                               r.Status != RefundRequestStatus.Cancelled &&
                               r.Status != RefundRequestStatus.Draft)
                    .CountAsync();

                counts.EBillRequestCount = await _context.CallLogVerifications
                    .Where(v => v.SubmittedToSupervisor
                        && (v.SupervisorApprovalStatus == null || v.SupervisorApprovalStatus == "" || v.SupervisorApprovalStatus == "Pending"))
                    .Select(v => v.VerifiedBy)
                    .Distinct()
                    .CountAsync();

                counts.TotalPendingCount = counts.SimRequestCount + counts.RefundRequestCount + counts.EBillRequestCount;
            }
            else if (isICTS)
            {
                // ICTS staff see requests at all ICTS workflow stages
                counts.SimRequestCount = await _context.SimRequests
                    .Where(r => r.Status == RequestStatus.PendingAdmin ||
                               r.Status == RequestStatus.PendingIcts ||
                               r.Status == RequestStatus.PendingServiceProvider ||
                               r.Status == RequestStatus.PendingSIMCollection)
                    .CountAsync();

                counts.RefundRequestCount = 0;
                counts.EBillRequestCount = 0;
                counts.TotalPendingCount = counts.SimRequestCount;
            }
            else if (isBudgetOfficer)
            {
                // Budget Officers see only requests pending THEIR budget approval
                counts.SimRequestCount = await _context.SimRequests
                    .Where(r => r.Status == RequestStatus.PendingSupervisor &&
                               (r.SupervisorEmail == userEmail || r.Supervisor == userEmail))
                    .CountAsync();

                counts.RefundRequestCount = await _context.RefundRequests
                    .Where(r => r.Status == RefundRequestStatus.PendingBudgetOfficer)
                    .CountAsync();

                counts.EBillRequestCount = await _context.CallLogVerifications
                    .Where(v => v.SubmittedToSupervisor
                        && v.SupervisorEmail == userEmail
                        && (v.SupervisorApprovalStatus == null || v.SupervisorApprovalStatus == "" || v.SupervisorApprovalStatus == "Pending"))
                    .Select(v => v.VerifiedBy)
                    .Distinct()
                    .CountAsync();

                counts.TotalPendingCount = counts.SimRequestCount + counts.RefundRequestCount + counts.EBillRequestCount;
            }
            else if (isStaffClaimsUnit)
            {
                // Staff Claims Unit see only requests pending staff claims processing
                counts.SimRequestCount = 0;
                counts.RefundRequestCount = await _context.RefundRequests
                    .Where(r => r.Status == RefundRequestStatus.PendingStaffClaimsUnit)
                    .CountAsync();
                counts.EBillRequestCount = 0;
                counts.TotalPendingCount = counts.RefundRequestCount;
            }
            else if (isPaymentApprover)
            {
                // Claims Unit Approver see only requests pending payment approval
                counts.SimRequestCount = 0;
                counts.RefundRequestCount = await _context.RefundRequests
                    .Where(r => r.Status == RefundRequestStatus.PendingPaymentApproval)
                    .CountAsync();
                counts.EBillRequestCount = 0;
                counts.TotalPendingCount = counts.RefundRequestCount;
            }
            else if (isSupervisor || isManager)
            {
                // Supervisors see only requests pending THEIR supervisor approval
                counts.SimRequestCount = await _context.SimRequests
                    .Where(r => r.Status == RequestStatus.PendingSupervisor &&
                               (r.SupervisorEmail == userEmail || r.Supervisor == userEmail))
                    .CountAsync();

                counts.RefundRequestCount = await _context.RefundRequests
                    .Where(r => r.Status == RefundRequestStatus.PendingSupervisor &&
                               r.SupervisorEmail == userEmail)
                    .CountAsync();

                counts.EBillRequestCount = await _context.CallLogVerifications
                    .Where(v => v.SubmittedToSupervisor
                        && v.SupervisorEmail == userEmail
                        && (v.SupervisorApprovalStatus == null || v.SupervisorApprovalStatus == "" || v.SupervisorApprovalStatus == "Pending"))
                    .Select(v => v.VerifiedBy)
                    .Distinct()
                    .CountAsync();

                counts.TotalPendingCount = counts.SimRequestCount + counts.RefundRequestCount + counts.EBillRequestCount;
            }
            else
            {
                // Dynamic supervisor detection - check if user's email is assigned as supervisor on any pending request
                // even if they don't have a Supervisor role

                // Check for pending SIM requests where user is the supervisor
                counts.SimRequestCount = await _context.SimRequests
                    .Where(r => r.Status == RequestStatus.PendingSupervisor &&
                               (r.SupervisorEmail == userEmail || r.Supervisor == userEmail))
                    .CountAsync();

                // Check for pending Refund requests where user is the supervisor
                counts.RefundRequestCount = await _context.RefundRequests
                    .Where(r => r.Status == RefundRequestStatus.PendingSupervisor &&
                               r.SupervisorEmail == userEmail)
                    .CountAsync();

                // Check for pending E-Bill verifications where user is the supervisor
                counts.EBillRequestCount = await _context.CallLogVerifications
                    .Where(v => v.SubmittedToSupervisor
                        && v.SupervisorEmail == userEmail
                        && (v.SupervisorApprovalStatus == null || v.SupervisorApprovalStatus == "" || v.SupervisorApprovalStatus == "Pending"))
                    .Select(v => v.VerifiedBy)
                    .Distinct()
                    .CountAsync();

                counts.TotalPendingCount = counts.SimRequestCount + counts.RefundRequestCount + counts.EBillRequestCount;
            }

            return View(counts);
        }
    }

    public class PendingRequestCounts
    {
        public int SimRequestCount { get; set; }
        public int RefundRequestCount { get; set; }
        public int EBillRequestCount { get; set; }
        public int PaymentAssignmentsCount { get; set; }
        public int TotalPendingCount { get; set; }
        public bool HasEbillAccount { get; set; }
    }
}
