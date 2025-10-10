using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TAB.Web.Data;
using TAB.Web.Models;

namespace TAB.Web.Pages.Modules.EBillManagement.Approvals.Supervisor
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public IndexModel(
            ApplicationDbContext context, 
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Properties for E-bill requests
        public List<Ebill> PendingRequests { get; set; } = new();
        public List<Ebill> ProcessedRequests { get; set; } = new();
        public List<Ebill> RejectedRequests { get; set; } = new();
        
        // Summary statistics
        public int PendingCount { get; set; }
        public int ProcessedCount { get; set; }
        public int RejectedCount { get; set; }
        
        // Current supervisor information
        public string? CurrentSupervisorName { get; set; }
        public string? CurrentSupervisorEmail { get; set; }

        [TempData]
        public string? StatusMessage { get; set; }
        [TempData]
        public string? StatusMessageClass { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            // Check if user has supervisor access
            var hasAccess = await CheckSupervisorAccessAsync();
            if (!hasAccess)
            {
                return Forbid();
            }

            await LoadEbillRequestsAsync();
            return Page();
        }

        private async Task<bool> CheckSupervisorAccessAsync()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return false;

            // Check if user is Admin (admins can access all supervisor functions)
            if (await _userManager.IsInRoleAsync(currentUser, "Admin"))
            {
                return true;
            }

            // Check if user's email exists as a supervisor in any E-bill requests
            var hasEbillRequests = await _context.Ebills
                .AnyAsync(r => r.SupervisorEmail == currentUser.Email);

            return hasEbillRequests;
        }

        public async Task<IActionResult> OnPostApproveAsync(int requestId, string? notes)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Challenge();
            }

            try
            {
                return await ApproveEbillRequestAsync(requestId, currentUser, notes);
            }
            catch (Exception)
            {
                StatusMessage = "An error occurred while processing the approval.";
                StatusMessageClass = "danger";
                await LoadEbillRequestsAsync();
                return Page();
            }
        }

        public async Task<IActionResult> OnPostRejectAsync(int requestId, string? notes)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Challenge();
            }

            try
            {
                return await RejectEbillRequestAsync(requestId, currentUser, notes);
            }
            catch (Exception)
            {
                StatusMessage = "An error occurred while processing the rejection.";
                StatusMessageClass = "danger";
                await LoadEbillRequestsAsync();
                return Page();
            }
        }

        public async Task<IActionResult> OnPostRevertAsync(int requestId, string? notes)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                StatusMessage = "User not found.";
                StatusMessageClass = "danger";
                return Page();
            }

            try
            {
                return await RevertEbillRequestAsync(requestId, currentUser, notes);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error processing request: {ex.Message}";
                StatusMessageClass = "danger";
                await LoadEbillRequestsAsync();
                return Page();
            }
        }

        private async Task LoadEbillRequestsAsync()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return;

            CurrentSupervisorName = $"{currentUser.FirstName} {currentUser.LastName}";
            CurrentSupervisorEmail = currentUser.Email;

            // Load E-bill requests where current user is the supervisor (by email)
            var ebillRequests = await _context.Ebills
                .Include(e => e.ServiceProvider)
                .Where(e => e.SupervisorEmail == currentUser.Email)
                .OrderByDescending(e => e.RequestDate)
                .ToListAsync();

            // Categorize requests by status
            PendingRequests = ebillRequests.Where(e => e.Status == EbillStatus.PendingSupervisor).ToList();
            ProcessedRequests = ebillRequests.Where(e => e.Status == EbillStatus.PendingAdmin || e.Status == EbillStatus.Approved || e.Status == EbillStatus.Paid).ToList();
            RejectedRequests = ebillRequests.Where(e => e.Status == EbillStatus.Rejected).ToList();

            // Update counts
            PendingCount = PendingRequests.Count;
            ProcessedCount = ProcessedRequests.Count;
            RejectedCount = RejectedRequests.Count;
        }

        private async Task<IActionResult> ApproveEbillRequestAsync(int requestId, ApplicationUser currentUser, string? notes)
        {
            var request = await _context.Ebills.FirstOrDefaultAsync(r => r.Id == requestId);
            if (request == null || request.Status != EbillStatus.PendingSupervisor)
            {
                StatusMessage = "E-Bill request not found or not pending supervisor approval.";
                StatusMessageClass = "danger";
                return Page();
            }

            request.Status = EbillStatus.PendingAdmin;
            request.SupervisorApprovalDate = DateTime.UtcNow;
            request.SupervisorNotes = notes?.Trim();
            request.ProcessedBy = currentUser.Id;

            await _context.SaveChangesAsync();

            StatusMessage = $"E-Bill request for {request.FullName} has been approved and forwarded to Admin.";
            StatusMessageClass = "success";

            await LoadEbillRequestsAsync();
            return Page();
        }

        private async Task<IActionResult> RejectEbillRequestAsync(int requestId, ApplicationUser currentUser, string? notes)
        {
            var request = await _context.Ebills.FirstOrDefaultAsync(r => r.Id == requestId);
            if (request == null || request.Status != EbillStatus.PendingSupervisor)
            {
                StatusMessage = "E-Bill request not found or not pending supervisor approval.";
                StatusMessageClass = "danger";
                return Page();
            }

            request.Status = EbillStatus.Rejected;
            request.SupervisorApprovalDate = DateTime.UtcNow;
            request.SupervisorNotes = notes?.Trim();
            request.ProcessedBy = currentUser.Id;

            await _context.SaveChangesAsync();

            StatusMessage = $"E-Bill request for {request.FullName} has been rejected.";
            StatusMessageClass = "warning";

            await LoadEbillRequestsAsync();
            return Page();
        }

        private async Task<IActionResult> RevertEbillRequestAsync(int requestId, ApplicationUser currentUser, string? notes)
        {
            var request = await _context.Ebills.FirstOrDefaultAsync(r => r.Id == requestId);
            if (request == null || request.Status != EbillStatus.PendingSupervisor)
            {
                StatusMessage = "E-Bill request not found or not pending supervisor approval.";
                StatusMessageClass = "danger";
                return Page();
            }

            request.Status = EbillStatus.Draft;
            request.SubmittedToSupervisor = false;
            request.SupervisorNotes = notes?.Trim();

            await _context.SaveChangesAsync();

            StatusMessage = $"E-Bill request for {request.FullName} has been reverted to draft status.";
            StatusMessageClass = "info";

            await LoadEbillRequestsAsync();
            return Page();
        }

        public string GetStatusBadgeClass(EbillStatus status)
        {
            return status switch
            {
                EbillStatus.Draft => "badge bg-secondary",
                EbillStatus.PendingSupervisor => "badge bg-warning",
                EbillStatus.PendingAdmin => "badge bg-info",
                EbillStatus.Approved => "badge bg-success",
                EbillStatus.Rejected => "badge bg-danger",
                EbillStatus.Paid => "badge bg-dark",
                EbillStatus.Overdue => "badge bg-danger",
                _ => "badge bg-light text-dark"
            };
        }

        public string GetStatusIcon(EbillStatus status)
        {
            return status switch
            {
                EbillStatus.Draft => "bi-pencil-square",
                EbillStatus.PendingSupervisor => "bi-clock",
                EbillStatus.PendingAdmin => "bi-person-check",
                EbillStatus.Approved => "bi-check-circle",
                EbillStatus.Rejected => "bi-x-circle",
                EbillStatus.Paid => "bi-check-circle-fill",
                EbillStatus.Overdue => "bi-exclamation-triangle",
                _ => "bi-question-circle"
            };
        }

        public string GetPriority(DateTime requestDate, DateTime dueDate)
        {
            var daysSinceRequest = (DateTime.Now - requestDate).Days;
            var daysToDue = (dueDate - DateTime.Now).Days;
            
            if (daysToDue < 0) return "Overdue";
            if (daysToDue <= 3) return "Urgent";
            if (daysSinceRequest > 7) return "High";
            if (daysSinceRequest > 3) return "Medium";
            return "Normal";
        }

        public string GetBillTypeIcon(BillType billType)
        {
            return billType switch
            {
                BillType.Mobile => "bi-phone",
                BillType.Internet => "bi-wifi",
                BillType.Landline => "bi-telephone",
                BillType.Data => "bi-hdd-network",
                BillType.Other => "bi-receipt",
                _ => "bi-receipt"
            };
        }
    }
} 