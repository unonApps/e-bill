using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TAB.Web.Data;
using TAB.Web.Models;
using TAB.Web.Services;

namespace TAB.Web.Pages.Modules.EBillManagement.CallRecords
{
    [Authorize]
    public class PaymentAssignmentsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ICallLogVerificationService _verificationService;

        public PaymentAssignmentsModel(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ICallLogVerificationService verificationService)
        {
            _context = context;
            _userManager = userManager;
            _verificationService = verificationService;
        }

        public List<CallLogPaymentAssignment> PendingAssignments { get; set; } = new();
        public List<CallLogPaymentAssignment> CompletedAssignments { get; set; } = new();
        public string? UserIndexNumber { get; set; }

        [TempData]
        public string? StatusMessage { get; set; }

        [TempData]
        public string? StatusMessageClass { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Challenge();

            var ebillUser = await _context.EbillUsers
                .FirstOrDefaultAsync(u => u.Email == user.Email);

            // Check if user is Admin - admins can see all assignments even without employee profile
            bool isAdmin = User.IsInRole("Admin");

            if (ebillUser == null && !isAdmin)
            {
                StatusMessage = "Your profile is not linked to an employee record.";
                StatusMessageClass = "warning";
                return Page();
            }

            UserIndexNumber = ebillUser?.IndexNumber;

            // Load assignments based on role
            if (isAdmin && string.IsNullOrEmpty(UserIndexNumber))
            {
                // Admin view - show ALL payment assignments across the system
                PendingAssignments = await _context.CallLogPaymentAssignments
                    .Include(a => a.CallRecord)
                    .Where(a => a.AssignmentStatus == "Pending")
                    .OrderByDescending(a => a.AssignedDate)
                    .ToListAsync();

                CompletedAssignments = await _context.CallLogPaymentAssignments
                    .Include(a => a.CallRecord)
                    .Where(a => a.AssignmentStatus != "Pending"
                        && a.ModifiedDate >= DateTime.UtcNow.AddDays(-30))
                    .OrderByDescending(a => a.ModifiedDate)
                    .ToListAsync();
            }
            else if (!string.IsNullOrEmpty(UserIndexNumber))
            {
                // Regular user view - show only assignments for this user
                PendingAssignments = await _verificationService.GetPendingAssignmentsAsync(UserIndexNumber);

                CompletedAssignments = await _context.CallLogPaymentAssignments
                    .Include(a => a.CallRecord)
                    .Where(a => a.AssignedTo == UserIndexNumber
                        && a.AssignmentStatus != "Pending"
                        && a.ModifiedDate >= DateTime.UtcNow.AddDays(-30))
                    .OrderByDescending(a => a.ModifiedDate)
                    .ToListAsync();
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAcceptAsync(int assignmentId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Challenge();

            var ebillUser = await _context.EbillUsers
                .FirstOrDefaultAsync(u => u.Email == user.Email);

            if (ebillUser == null)
            {
                StatusMessage = "Your profile is not linked to an employee record.";
                StatusMessageClass = "danger";
                return RedirectToPage();
            }

            try
            {
                var success = await _verificationService.AcceptPaymentAssignmentAsync(assignmentId, ebillUser.IndexNumber);

                if (success)
                {
                    StatusMessage = "Payment assignment accepted successfully.";
                    StatusMessageClass = "success";
                }
                else
                {
                    StatusMessage = "Unable to accept payment assignment.";
                    StatusMessageClass = "danger";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error accepting assignment: {ex.Message}";
                StatusMessageClass = "danger";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostRejectAsync(int assignmentId, string rejectionReason)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Challenge();

            var ebillUser = await _context.EbillUsers
                .FirstOrDefaultAsync(u => u.Email == user.Email);

            if (ebillUser == null)
            {
                StatusMessage = "Your profile is not linked to an employee record.";
                StatusMessageClass = "danger";
                return RedirectToPage();
            }

            if (string.IsNullOrWhiteSpace(rejectionReason))
            {
                StatusMessage = "Please provide a reason for rejection.";
                StatusMessageClass = "warning";
                return RedirectToPage();
            }

            try
            {
                var success = await _verificationService.RejectPaymentAssignmentAsync(
                    assignmentId,
                    ebillUser.IndexNumber,
                    rejectionReason);

                if (success)
                {
                    StatusMessage = "Payment assignment rejected.";
                    StatusMessageClass = "info";
                }
                else
                {
                    StatusMessage = "Unable to reject payment assignment.";
                    StatusMessageClass = "danger";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error rejecting assignment: {ex.Message}";
                StatusMessageClass = "danger";
            }

            return RedirectToPage();
        }
    }
}
