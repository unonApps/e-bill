using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TAB.Web.Data;
using TAB.Web.Models;

namespace TAB.Web.Pages.Modules.RefundManagement.Approvals
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public int SupervisorPendingCount { get; set; }
        public int BudgetPendingCount { get; set; }
        public int ClaimsPendingCount { get; set; }
        public int PaymentPendingCount { get; set; }
        public List<RefundRequest> RefundRequests { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var userName = User.Identity?.Name;
            var supervisorName = $"{User.FindFirst("FirstName")?.Value} {User.FindFirst("LastName")?.Value}";
            var isAdmin = User.IsInRole("Admin");

            // Get refund requests - all for admin, filtered for others
            if (isAdmin)
            {
                RefundRequests = await _context.RefundRequests
                    .OrderByDescending(r => r.RequestDate)
                    .ToListAsync();
            }
            else
            {
                RefundRequests = await _context.RefundRequests
                    .Where(r => r.SupervisorName == supervisorName)
                    .OrderByDescending(r => r.RequestDate)
                    .ToListAsync();
            }

            SupervisorPendingCount = RefundRequests.Count(r => r.Status == RefundRequestStatus.PendingSupervisor);

            // Count pending budget officer approvals
            if (User.IsInRole("Budget Officer") || User.IsInRole("BudgetOfficer") || User.IsInRole("Admin"))
            {
                BudgetPendingCount = await _context.RefundRequests
                    .Where(r => r.Status == RefundRequestStatus.PendingBudgetOfficer)
                    .CountAsync();
            }

            // Count pending claims unit approvals
            if (User.IsInRole("Claims Unit Approver") || User.IsInRole("Admin"))
            {
                ClaimsPendingCount = await _context.RefundRequests
                    .Where(r => r.Status == RefundRequestStatus.PendingStaffClaimsUnit)
                    .CountAsync();
            }

            // Count pending payment approvals
            if (User.IsInRole("ICTS") || User.IsInRole("Payment Approver") || User.IsInRole("Admin"))
            {
                PaymentPendingCount = await _context.RefundRequests
                    .Where(r => r.Status == RefundRequestStatus.PendingPaymentApproval)
                    .CountAsync();
            }

            // Pass data to ViewData for partial views
            ViewData["RefundRequests"] = RefundRequests;
            ViewData["Context"] = new { _context = _context };

            return Page();
        }

        // Supervisor Handlers
        public async Task<IActionResult> OnPostApproveSupervisorAsync(int requestId, string supervisorNotes, string? supervisorRemarks)
        {
            var request = await _context.RefundRequests.FindAsync(requestId);
            if (request == null || request.Status != RefundRequestStatus.PendingSupervisor)
            {
                TempData["Error"] = "Request not found or already processed.";
                return RedirectToPage();
            }

            var userName = $"{User.FindFirst("FirstName")?.Value} {User.FindFirst("LastName")?.Value}";
            var userEmail = User.Identity?.Name;

            request.Status = RefundRequestStatus.PendingBudgetOfficer;
            request.SupervisorApprovalDate = DateTime.UtcNow;
            request.SupervisorNotes = supervisorNotes;
            request.SupervisorRemarks = supervisorRemarks;
            request.SupervisorName = userName;
            request.SupervisorEmail = userEmail;

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Refund request for {request.MobileNumberAssignedTo} has been approved and forwarded to Budget Officer.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostRejectSupervisorAsync(int requestId, string rejectionReason)
        {
            var request = await _context.RefundRequests.FindAsync(requestId);
            if (request == null || request.Status != RefundRequestStatus.PendingSupervisor)
            {
                TempData["Error"] = "Request not found or already processed.";
                return RedirectToPage();
            }

            var userName = $"{User.FindFirst("FirstName")?.Value} {User.FindFirst("LastName")?.Value}";

            request.Status = RefundRequestStatus.Cancelled;
            request.CancellationDate = DateTime.UtcNow;
            request.CancellationReason = $"Rejected by Supervisor: {rejectionReason}";
            request.CancelledBy = userName;
            request.SupervisorNotes = $"REJECTED: {rejectionReason}";

            await _context.SaveChangesAsync();

            TempData["Warning"] = $"Refund request for {request.MobileNumberAssignedTo} has been rejected.";
            return RedirectToPage();
        }

        // Budget Officer Handlers
        public async Task<IActionResult> OnPostApproveBudgetAsync(int requestId, string costObject, string costCenter, 
            string fundCommitment, string budgetNotes, string? budgetRemarks)
        {
            var request = await _context.RefundRequests.FindAsync(requestId);
            if (request == null || request.Status != RefundRequestStatus.PendingBudgetOfficer)
            {
                TempData["Error"] = "Request not found or already processed.";
                return RedirectToPage();
            }

            var userName = $"{User.FindFirst("FirstName")?.Value} {User.FindFirst("LastName")?.Value}";
            var userEmail = User.Identity?.Name;

            request.Status = RefundRequestStatus.PendingStaffClaimsUnit;
            request.BudgetOfficerApprovalDate = DateTime.UtcNow;
            request.CostObject = costObject;
            request.CostCenter = costCenter;
            request.FundCommitment = fundCommitment;
            request.BudgetOfficerNotes = budgetNotes;
            request.BudgetOfficerRemarks = budgetRemarks;
            request.BudgetOfficerName = userName;
            request.BudgetOfficerEmail = userEmail;

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Budget approval completed for {request.MobileNumberAssignedTo}. Forwarded to Claims Unit.";
            return RedirectToPage(new { tab = "budget" });
        }

        public async Task<IActionResult> OnPostRejectBudgetAsync(int requestId, string rejectionType, string rejectionDetails)
        {
            var request = await _context.RefundRequests.FindAsync(requestId);
            if (request == null || request.Status != RefundRequestStatus.PendingBudgetOfficer)
            {
                TempData["Error"] = "Request not found or already processed.";
                return RedirectToPage();
            }

            var userName = $"{User.FindFirst("FirstName")?.Value} {User.FindFirst("LastName")?.Value}";

            request.Status = RefundRequestStatus.Cancelled;
            request.CancellationDate = DateTime.UtcNow;
            request.CancellationReason = $"Rejected by Budget Officer - {rejectionType}: {rejectionDetails}";
            request.CancelledBy = userName;
            request.BudgetOfficerNotes = $"REJECTED - {rejectionType}: {rejectionDetails}";

            await _context.SaveChangesAsync();

            TempData["Warning"] = $"Refund request for {request.MobileNumberAssignedTo} has been rejected.";
            return RedirectToPage(new { tab = "budget" });
        }

        // Claims Unit Handlers
        public async Task<IActionResult> OnPostProcessClaimsAsync(int requestId, string umojaPaymentDocumentId, 
            decimal refundUsdAmount, DateTime claimsActionDate, string claimsNotes, string? claimsRemarks)
        {
            var request = await _context.RefundRequests.FindAsync(requestId);
            if (request == null || request.Status != RefundRequestStatus.PendingStaffClaimsUnit)
            {
                TempData["Error"] = "Request not found or already processed.";
                return RedirectToPage();
            }

            var userName = $"{User.FindFirst("FirstName")?.Value} {User.FindFirst("LastName")?.Value}";
            var userEmail = User.Identity?.Name;

            request.Status = RefundRequestStatus.PendingPaymentApproval;
            request.StaffClaimsApprovalDate = DateTime.UtcNow;
            request.UmojaPaymentDocumentId = umojaPaymentDocumentId;
            request.RefundUsdAmount = refundUsdAmount;
            request.ClaimsActionDate = claimsActionDate;
            request.StaffClaimsNotes = claimsNotes;
            request.StaffClaimsRemarks = claimsRemarks;
            request.StaffClaimsOfficerName = userName;
            request.StaffClaimsOfficerEmail = userEmail;

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Claims processing completed for {request.MobileNumberAssignedTo}. Forwarded for payment approval.";
            return RedirectToPage(new { tab = "claims" });
        }

        public async Task<IActionResult> OnPostRejectClaimsAsync(int requestId, string rejectionType, string rejectionDetails)
        {
            var request = await _context.RefundRequests.FindAsync(requestId);
            if (request == null || request.Status != RefundRequestStatus.PendingStaffClaimsUnit)
            {
                TempData["Error"] = "Request not found or already processed.";
                return RedirectToPage();
            }

            var userName = $"{User.FindFirst("FirstName")?.Value} {User.FindFirst("LastName")?.Value}";

            request.Status = RefundRequestStatus.Cancelled;
            request.CancellationDate = DateTime.UtcNow;
            request.CancellationReason = $"Rejected by Claims Unit - {rejectionType}: {rejectionDetails}";
            request.CancelledBy = userName;
            request.StaffClaimsNotes = $"REJECTED - {rejectionType}: {rejectionDetails}";

            await _context.SaveChangesAsync();

            TempData["Warning"] = $"Refund request for {request.MobileNumberAssignedTo} has been rejected.";
            return RedirectToPage(new { tab = "claims" });
        }

        // Payment Approver Handlers
        public async Task<IActionResult> OnPostApprovePaymentAsync(int requestId, string paymentReference, 
            string paymentNotes, string? completionNotes)
        {
            var request = await _context.RefundRequests.FindAsync(requestId);
            if (request == null || request.Status != RefundRequestStatus.PendingPaymentApproval)
            {
                TempData["Error"] = "Request not found or already processed.";
                return RedirectToPage();
            }

            var userName = $"{User.FindFirst("FirstName")?.Value} {User.FindFirst("LastName")?.Value}";
            var userEmail = User.Identity?.Name;

            request.Status = RefundRequestStatus.Completed;
            request.PaymentApprovalDate = DateTime.UtcNow;
            request.CompletionDate = DateTime.UtcNow;
            request.PaymentReference = paymentReference;
            request.PaymentApprovalNotes = paymentNotes;
            request.CompletionNotes = completionNotes;
            request.PaymentApproverName = userName;
            request.PaymentApproverEmail = userEmail;
            request.ProcessedBy = userName;

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Payment approved and completed for {request.MobileNumberAssignedTo}. Reference: {paymentReference}";
            return RedirectToPage(new { tab = "payment" });
        }

        public async Task<IActionResult> OnPostRejectPaymentAsync(int requestId, string rejectionType, string rejectionDetails)
        {
            var request = await _context.RefundRequests.FindAsync(requestId);
            if (request == null || request.Status != RefundRequestStatus.PendingPaymentApproval)
            {
                TempData["Error"] = "Request not found or already processed.";
                return RedirectToPage();
            }

            var userName = $"{User.FindFirst("FirstName")?.Value} {User.FindFirst("LastName")?.Value}";

            request.Status = RefundRequestStatus.Cancelled;
            request.CancellationDate = DateTime.UtcNow;
            request.CancellationReason = $"Rejected at Payment Stage - {rejectionType}: {rejectionDetails}";
            request.CancelledBy = userName;
            request.PaymentApprovalNotes = $"REJECTED - {rejectionType}: {rejectionDetails}";

            await _context.SaveChangesAsync();

            TempData["Warning"] = $"Payment rejected for {request.MobileNumberAssignedTo}. Request has been cancelled.";
            return RedirectToPage(new { tab = "payment" });
        }
    }
}