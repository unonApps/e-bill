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
    public class SupervisorApprovalsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ICallLogVerificationService _verificationService;

        public SupervisorApprovalsModel(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ICallLogVerificationService verificationService)
        {
            _context = context;
            _userManager = userManager;
            _verificationService = verificationService;
        }

        public List<SupervisorSubmissionGroup> PendingSubmissions { get; set; } = new();
        public string? SupervisorIndexNumber { get; set; }
        public string? SupervisorName { get; set; }
        public List<StaffOption> StaffWithPendingRequests { get; set; } = new();

        // Filters
        [BindProperty(SupportsGet = true)]
        public string? FilterUser { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? FilterStartDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? FilterEndDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? FilterMonth { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? FilterYear { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool FilterOverageOnly { get; set; }

        public class StaffOption
        {
            public string IndexNumber { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
        }

        [TempData]
        public string? StatusMessage { get; set; }

        [TempData]
        public string? StatusMessageClass { get; set; }

        public class SupervisorSubmissionGroup
        {
            public string UserIndexNumber { get; set; } = string.Empty;
            public string UserName { get; set; } = string.Empty;
            public DateTime SubmittedDate { get; set; }
            public int TotalCalls { get; set; }
            public decimal TotalAmount { get; set; }
            public int OverageCount { get; set; }
            public bool HasDocuments { get; set; }
            public List<CallLogVerification> Verifications { get; set; } = new();
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Challenge();

            // Use the logged-in user's email to find verifications assigned to them
            // SupervisorIndexNumber field in CallLogVerifications contains the supervisor's email
            SupervisorIndexNumber = user.Email;

            // Try to get name from EbillUser if exists, otherwise use ApplicationUser info
            var ebillUser = await _context.EbillUsers
                .FirstOrDefaultAsync(u => u.Email == user.Email);

            if (ebillUser != null)
            {
                SupervisorName = $"{ebillUser.FirstName} {ebillUser.LastName}";
            }
            else
            {
                SupervisorName = user.FirstName != null && user.LastName != null
                    ? $"{user.FirstName} {user.LastName}"
                    : user.Email;
            }

            // Load list of staff with pending requests for dropdown
            await LoadStaffWithPendingRequestsAsync();

            await LoadPendingSubmissionsAsync();

            return Page();
        }

        private async Task LoadStaffWithPendingRequestsAsync()
        {
            if (string.IsNullOrEmpty(SupervisorIndexNumber))
                return;

            // Get distinct staff members who have pending requests
            var staffIndexNumbers = await _context.CallLogVerifications
                .Where(v => v.SubmittedToSupervisor
                    && v.SupervisorIndexNumber == SupervisorIndexNumber
                    && (v.SupervisorApprovalStatus == null || v.SupervisorApprovalStatus == "Pending"))
                .Select(v => v.VerifiedBy)
                .Distinct()
                .ToListAsync();

            // Get staff details
            var staffUsers = await _context.EbillUsers
                .Where(u => staffIndexNumbers.Contains(u.IndexNumber))
                .ToListAsync();

            var staffOptions = staffUsers.Select(u => new StaffOption
            {
                IndexNumber = u.IndexNumber,
                Name = $"{u.FirstName} {u.LastName}"
            }).ToList();

            // Add any staff not found in EbillUsers (use index number as name)
            foreach (var indexNumber in staffIndexNumbers)
            {
                if (!staffOptions.Any(s => s.IndexNumber == indexNumber))
                {
                    staffOptions.Add(new StaffOption
                    {
                        IndexNumber = indexNumber ?? "",
                        Name = indexNumber ?? "Unknown"
                    });
                }
            }

            StaffWithPendingRequests = staffOptions.OrderBy(s => s.Name).ToList();
        }

        private async Task LoadPendingSubmissionsAsync()
        {
            if (string.IsNullOrEmpty(SupervisorIndexNumber))
                return;

            // Get all verifications submitted to this supervisor
            // SupervisorApprovalStatus is NULL for pending approvals (not yet acted upon)
            var query = _context.CallLogVerifications
                .Include(v => v.CallRecord)
                    .ThenInclude(c => c.UserPhone)
                        .ThenInclude(up => up.ClassOfService)
                .Include(v => v.Documents)
                .Where(v => v.SubmittedToSupervisor
                    && v.SupervisorIndexNumber == SupervisorIndexNumber
                    && (v.SupervisorApprovalStatus == null || v.SupervisorApprovalStatus == "Pending"));

            // Apply filters
            if (!string.IsNullOrEmpty(FilterUser))
            {
                // FilterUser can be either index number or name
                // First check if it matches any index number directly
                var matchingStaff = _context.EbillUsers
                    .Where(u => u.IndexNumber == FilterUser ||
                               (u.FirstName + " " + u.LastName).Contains(FilterUser))
                    .Select(u => u.IndexNumber)
                    .ToList();

                if (matchingStaff.Any())
                {
                    query = query.Where(v => matchingStaff.Contains(v.VerifiedBy));
                }
                else
                {
                    // If no match in EbillUsers, try direct index number match
                    query = query.Where(v => v.VerifiedBy == FilterUser);
                }
            }

            if (FilterStartDate.HasValue)
            {
                query = query.Where(v => v.SubmittedDate >= FilterStartDate.Value);
            }

            if (FilterEndDate.HasValue)
            {
                query = query.Where(v => v.SubmittedDate <= FilterEndDate.Value);
            }

            // Month and Year filters
            if (FilterMonth.HasValue && FilterYear.HasValue)
            {
                query = query.Where(v => v.SubmittedDate.HasValue
                    && v.SubmittedDate.Value.Month == FilterMonth.Value
                    && v.SubmittedDate.Value.Year == FilterYear.Value);
            }
            else if (FilterMonth.HasValue)
            {
                query = query.Where(v => v.SubmittedDate.HasValue
                    && v.SubmittedDate.Value.Month == FilterMonth.Value);
            }
            else if (FilterYear.HasValue)
            {
                query = query.Where(v => v.SubmittedDate.HasValue
                    && v.SubmittedDate.Value.Year == FilterYear.Value);
            }

            if (FilterOverageOnly)
            {
                query = query.Where(v => v.IsOverage);
            }

            var verifications = await query.OrderByDescending(v => v.SubmittedDate).ToListAsync();

            // Group by user and submission date (same day submissions grouped together)
            PendingSubmissions = verifications
                .GroupBy(v => new {
                    v.VerifiedBy,
                    SubmissionDate = v.SubmittedDate.HasValue ? v.SubmittedDate.Value.Date : DateTime.MinValue
                })
                .Select(g => new SupervisorSubmissionGroup
                {
                    UserIndexNumber = g.Key.VerifiedBy ?? "",
                    UserName = GetUserName(g.Key.VerifiedBy ?? ""),
                    SubmittedDate = g.Key.SubmissionDate,
                    TotalCalls = g.Count(),
                    TotalAmount = g.Sum(v => v.ActualAmount),
                    OverageCount = g.Count(v => v.IsOverage),
                    HasDocuments = g.Any(v => v.Documents.Any()),
                    Verifications = g.ToList()
                })
                .OrderByDescending(g => g.SubmittedDate)
                .ToList();
        }

        private string GetUserName(string indexNumber)
        {
            var user = _context.EbillUsers.FirstOrDefault(u => u.IndexNumber == indexNumber);
            return user != null ? $"{user.FirstName} {user.LastName}" : indexNumber;
        }

        public async Task<IActionResult> OnPostApproveSelectedAsync(List<int> verificationIds)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Challenge();

            if (verificationIds == null || !verificationIds.Any())
            {
                StatusMessage = "No verifications selected.";
                StatusMessageClass = "warning";
                return RedirectToPage();
            }

            try
            {
                int successCount = 0;
                int failCount = 0;

                foreach (var verificationId in verificationIds)
                {
                    // Use the service method which properly updates both tables
                    var result = await _verificationService.ApproveVerificationAsync(
                        verificationId,
                        user.Email, // SupervisorIndexNumber
                        null, // approvedAmount - null means approve full amount
                        null  // comments
                    );

                    if (result)
                        successCount++;
                    else
                        failCount++;
                }

                if (successCount > 0)
                {
                    StatusMessage = failCount > 0
                        ? $"Approved {successCount} verification(s). {failCount} failed."
                        : $"Successfully approved {successCount} call verification(s).";
                    StatusMessageClass = failCount > 0 ? "warning" : "success";
                }
                else
                {
                    StatusMessage = "Failed to approve verifications.";
                    StatusMessageClass = "danger";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error approving verifications: {ex.Message}";
                StatusMessageClass = "danger";
            }

            return RedirectToPage();
        }
    }
}
