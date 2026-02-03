using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TAB.Web.Data;
using TAB.Web.Models;

namespace TAB.Web.Pages.Modules.EBillManagement.CallRecords
{
    [Authorize]
    public class ApprovalHistoryModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ApprovalHistoryModel(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public List<ApprovalHistoryRecord> ApprovalRecords { get; set; } = new();
        public string? SupervisorIndexNumber { get; set; }

        // Filters
        [BindProperty(SupportsGet = true)]
        public DateTime? FilterStartDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? FilterEndDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? FilterActionType { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? FilterUser { get; set; }

        // Pagination
        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        public int PageSize { get; set; } = 50;
        public int TotalPages { get; set; }
        public int TotalRecords { get; set; }

        [TempData]
        public string? StatusMessage { get; set; }

        [TempData]
        public string? StatusMessageClass { get; set; }

        public class ApprovalHistoryRecord
        {
            public int Id { get; set; }
            public string UserIndexNumber { get; set; } = string.Empty;
            public string UserName { get; set; } = string.Empty;
            public DateTime? ApprovedDate { get; set; }
            public string ApprovalStatus { get; set; } = string.Empty;
            public string CallDestination { get; set; } = string.Empty;
            public DateTime CallDate { get; set; }
            public decimal ActualAmount { get; set; }
            public decimal? ApprovedAmount { get; set; }
            public string? Comments { get; set; }
            public bool IsOverage { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Challenge();

            var ebillUser = await _context.EbillUsers
                .FirstOrDefaultAsync(u => u.Email == user.Email);

            if (ebillUser == null)
            {
                StatusMessage = "Your profile is not linked to an Staff record.";
                StatusMessageClass = "warning";
                return Page();
            }

            SupervisorIndexNumber = ebillUser.IndexNumber;

            await LoadApprovalHistoryAsync();

            return Page();
        }

        private async Task LoadApprovalHistoryAsync()
        {
            if (string.IsNullOrEmpty(SupervisorIndexNumber))
                return;

            var query = _context.CallLogVerifications
                .Include(v => v.CallRecord)
                .Where(v => v.SupervisorApprovedBy == SupervisorIndexNumber
                    && v.SupervisorApprovalStatus != "Pending");

            // Apply filters
            if (FilterStartDate.HasValue)
            {
                query = query.Where(v => v.SupervisorApprovedDate >= FilterStartDate.Value);
            }

            if (FilterEndDate.HasValue)
            {
                query = query.Where(v => v.SupervisorApprovedDate <= FilterEndDate.Value);
            }

            if (!string.IsNullOrEmpty(FilterActionType))
            {
                query = query.Where(v => v.SupervisorApprovalStatus == FilterActionType);
            }

            if (!string.IsNullOrEmpty(FilterUser))
            {
                query = query.Where(v => v.VerifiedBy == FilterUser);
            }

            // Get total count
            TotalRecords = await query.CountAsync();
            TotalPages = (int)Math.Ceiling(TotalRecords / (double)PageSize);

            // Get paginated records
            var verifications = await query
                .OrderByDescending(v => v.SupervisorApprovedDate)
                .Skip((PageNumber - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            ApprovalRecords = verifications.Select(v => new ApprovalHistoryRecord
            {
                Id = v.Id,
                UserIndexNumber = v.VerifiedBy ?? "",
                UserName = GetUserName(v.VerifiedBy ?? ""),
                ApprovedDate = v.SupervisorApprovedDate,
                ApprovalStatus = v.SupervisorApprovalStatus ?? "",
                CallDestination = v.CallRecord.CallDestination,
                CallDate = v.CallRecord.CallDate,
                ActualAmount = v.ActualAmount,
                ApprovedAmount = v.ApprovedAmount,
                Comments = v.SupervisorComments,
                IsOverage = v.IsOverage
            }).ToList();
        }

        private string GetUserName(string indexNumber)
        {
            var user = _context.EbillUsers.FirstOrDefault(u => u.IndexNumber == indexNumber);
            return user != null ? $"{user.FirstName} {user.LastName}" : indexNumber;
        }

        public async Task<IActionResult> OnGetExportAsync()
        {
            // TODO: Implement Excel/PDF export
            StatusMessage = "Export feature coming soon.";
            StatusMessageClass = "info";
            return RedirectToPage();
        }
    }
}
