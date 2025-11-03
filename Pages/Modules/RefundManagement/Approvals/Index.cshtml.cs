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

        public List<RefundRequest> RefundRequests { get; set; } = new();

        // Statistics
        public int TotalRequests { get; set; }
        public int PendingSupervisorCount { get; set; }
        public int PendingBudgetCount { get; set; }
        public int PendingClaimsCount { get; set; }
        public int PendingPaymentCount { get; set; }
        public int CompletedCount { get; set; }
        public int CancelledCount { get; set; }

        // User role flags
        public bool IsSupervisor { get; set; }
        public bool IsBudgetOfficer { get; set; }
        public bool IsClaimsUnit { get; set; }
        public bool IsPaymentApprover { get; set; }
        public bool IsAdmin { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var userName = User.Identity?.Name;
            IsAdmin = User.IsInRole("Admin");
            IsSupervisor = User.IsInRole("Supervisor");
            IsBudgetOfficer = User.IsInRole("Budget Officer") || User.IsInRole("BudgetOfficer");
            IsClaimsUnit = User.IsInRole("Claims Unit Approver");
            IsPaymentApprover = User.IsInRole("ICTS") || User.IsInRole("Payment Approver");

            // Load refund requests based on role
            // Exclude completed and cancelled requests since this is an approval page
            if (IsAdmin)
            {
                // Admin sees all pending requests (not completed or cancelled)
                RefundRequests = await _context.RefundRequests
                    .Where(r => r.Status != RefundRequestStatus.Completed && r.Status != RefundRequestStatus.Cancelled)
                    .OrderByDescending(r => r.RequestDate)
                    .ToListAsync();
            }
            else
            {
                // Non-admin users see requests relevant to their role (excluding completed/cancelled)
                var query = _context.RefundRequests
                    .Where(r => r.Status != RefundRequestStatus.Completed && r.Status != RefundRequestStatus.Cancelled);

                if (IsSupervisor)
                {
                    // Supervisors see requests assigned to them
                    query = query.Where(r => r.SupervisorEmail == userName);
                }
                else if (IsBudgetOfficer)
                {
                    // Budget officers see requests pending budget approval
                    query = query.Where(r => r.Status == RefundRequestStatus.PendingBudgetOfficer);
                }
                else if (IsClaimsUnit)
                {
                    // Claims unit sees requests pending claims approval
                    query = query.Where(r => r.Status == RefundRequestStatus.PendingStaffClaimsUnit);
                }
                else if (IsPaymentApprover)
                {
                    // Payment approvers see requests pending payment approval
                    query = query.Where(r => r.Status == RefundRequestStatus.PendingPaymentApproval);
                }

                RefundRequests = await query
                    .OrderByDescending(r => r.RequestDate)
                    .ToListAsync();
            }

            // Calculate statistics
            TotalRequests = RefundRequests.Count;
            PendingSupervisorCount = RefundRequests.Count(r => r.Status == RefundRequestStatus.PendingSupervisor);
            PendingBudgetCount = RefundRequests.Count(r => r.Status == RefundRequestStatus.PendingBudgetOfficer);
            PendingClaimsCount = RefundRequests.Count(r => r.Status == RefundRequestStatus.PendingStaffClaimsUnit);
            PendingPaymentCount = RefundRequests.Count(r => r.Status == RefundRequestStatus.PendingPaymentApproval);
            CompletedCount = RefundRequests.Count(r => r.Status == RefundRequestStatus.Completed);
            CancelledCount = RefundRequests.Count(r => r.Status == RefundRequestStatus.Cancelled);

            return Page();
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