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
            bool isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");
            bool isICTS = await _userManager.IsInRoleAsync(currentUser, "ICTS") ||
                         await _userManager.IsInRoleAsync(currentUser, "ICTS Service Desk");
            bool isBudgetOfficer = await _userManager.IsInRoleAsync(currentUser, "Budget Officer") ||
                                   await _userManager.IsInRoleAsync(currentUser, "BudgetOfficer");
            bool isClaimsUnit = await _userManager.IsInRoleAsync(currentUser, "Claims Unit Approver") ||
                               await _userManager.IsInRoleAsync(currentUser, "Staff Claims Unit");
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
                        && (v.SupervisorApprovalStatus == null || v.SupervisorApprovalStatus == "Pending"))
                    .CountAsync();

                counts.TotalPendingCount = counts.SimRequestCount + counts.RefundRequestCount + counts.EBillRequestCount;
            }
            else if (isICTS)
            {
                // ICTS staff see only requests pending THEIR action
                counts.SimRequestCount = await _context.SimRequests
                    .Where(r => r.Status == RequestStatus.PendingAdmin || r.Status == RequestStatus.PendingIcts)
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
                        && v.SupervisorIndexNumber == userEmail
                        && (v.SupervisorApprovalStatus == null || v.SupervisorApprovalStatus == "Pending"))
                    .CountAsync();

                counts.TotalPendingCount = counts.SimRequestCount + counts.RefundRequestCount + counts.EBillRequestCount;
            }
            else if (isClaimsUnit)
            {
                // Claims Unit see only requests pending THEIR claims approval
                counts.SimRequestCount = 0;
                counts.RefundRequestCount = await _context.RefundRequests
                    .Where(r => r.Status == RefundRequestStatus.PendingStaffClaimsUnit)
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
                        && v.SupervisorIndexNumber == userEmail
                        && (v.SupervisorApprovalStatus == null || v.SupervisorApprovalStatus == "Pending"))
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
        public int TotalPendingCount { get; set; }
    }
}
