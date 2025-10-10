using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TAB.Web.Data;
using TAB.Web.Models;

namespace TAB.Web.Pages.Modules.RefundManagement.Requests
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

        // Properties for the page
        public List<Models.RefundRequest> RefundRequests { get; set; } = new();
        
        // Statistics
        public int TotalRequests { get; set; }
        public int PendingRequests { get; set; }
        public int ApprovedRequests { get; set; }
        public int CancelledRequests { get; set; }
        
        public bool IsAdmin { get; set; }
        public string CurrentUserName { get; set; } = string.Empty;
        
        // Filter properties
        [BindProperty(SupportsGet = true)]
        public string? SelectedUserId { get; set; }
        
        [BindProperty(SupportsGet = true)]
        public RefundRequestStatus? SelectedStatus { get; set; }
        
        [BindProperty(SupportsGet = true)]
        public DateTime? StartDate { get; set; }
        
        [BindProperty(SupportsGet = true)]
        public DateTime? EndDate { get; set; }
        
        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        // Pagination
        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        public int PageSize { get; set; } = 20;
        public int TotalPages { get; set; }
        public int TotalRecords { get; set; }

        public List<ApplicationUser> AvailableUsers { get; set; } = new();

        [TempData]
        public string? StatusMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Challenge();
            }

            CurrentUserName = !string.IsNullOrEmpty(currentUser.FirstName) && !string.IsNullOrEmpty(currentUser.LastName) 
                ? $"{currentUser.FirstName} {currentUser.LastName}" 
                : currentUser.UserName ?? "User";
            
            // Check if user is admin
            IsAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");
            
            if (IsAdmin)
            {
                // Load all users for filter dropdown
                AvailableUsers = await _userManager.Users
                    .OrderBy(u => u.FirstName)
                    .ToListAsync();
                
                if (!string.IsNullOrEmpty(SelectedUserId))
                {
                    // Filter by selected user
                    await LoadUserRefundRequestsAsync(SelectedUserId);
                }
                else
                {
                    await LoadAllRefundRequestsAsync();
                }
            }
            else
            {
                await LoadUserRefundRequestsAsync(currentUser.Id);
            }
            
            return Page();
        }

        private async Task LoadAllRefundRequestsAsync()
        {
            // Build query with filters
            var query = _context.RefundRequests.AsQueryable();

            // Apply filters
            query = ApplyFilters(query);

            // Get all filtered results for statistics
            var allRequests = await query.ToListAsync();

            // Calculate statistics from all filtered results
            TotalRequests = allRequests.Count;
            PendingRequests = allRequests.Count(r =>
                r.Status == RefundRequestStatus.Draft ||
                r.Status == RefundRequestStatus.PendingSupervisor ||
                r.Status == RefundRequestStatus.PendingBudgetOfficer ||
                r.Status == RefundRequestStatus.PendingStaffClaimsUnit ||
                r.Status == RefundRequestStatus.PendingPaymentApproval);
            ApprovedRequests = allRequests.Count(r => r.Status == RefundRequestStatus.Completed);
            CancelledRequests = allRequests.Count(r => r.Status == RefundRequestStatus.Cancelled);

            // Calculate pagination
            TotalRecords = allRequests.Count;
            TotalPages = (int)Math.Ceiling(TotalRecords / (double)PageSize);

            // Apply pagination
            RefundRequests = allRequests
                .OrderByDescending(r => r.RequestDate)
                .Skip((PageNumber - 1) * PageSize)
                .Take(PageSize)
                .ToList();
        }

        private async Task LoadUserRefundRequestsAsync(string userId)
        {
            // Build query with user filter
            var query = _context.RefundRequests
                .Where(r => r.RequestedBy == userId);

            // Apply additional filters
            query = ApplyFilters(query);

            // Get all filtered results for statistics
            var allRequests = await query.ToListAsync();

            // Calculate statistics from all filtered results
            TotalRequests = allRequests.Count;
            PendingRequests = allRequests.Count(r =>
                r.Status == RefundRequestStatus.Draft ||
                r.Status == RefundRequestStatus.PendingSupervisor ||
                r.Status == RefundRequestStatus.PendingBudgetOfficer ||
                r.Status == RefundRequestStatus.PendingStaffClaimsUnit ||
                r.Status == RefundRequestStatus.PendingPaymentApproval);
            ApprovedRequests = allRequests.Count(r => r.Status == RefundRequestStatus.Completed);
            CancelledRequests = allRequests.Count(r => r.Status == RefundRequestStatus.Cancelled);

            // Calculate pagination
            TotalRecords = allRequests.Count;
            TotalPages = (int)Math.Ceiling(TotalRecords / (double)PageSize);

            // Apply pagination
            RefundRequests = allRequests
                .OrderByDescending(r => r.RequestDate)
                .Skip((PageNumber - 1) * PageSize)
                .Take(PageSize)
                .ToList();
        }
        
        private IQueryable<RefundRequest> ApplyFilters(IQueryable<RefundRequest> query)
        {
            // Apply status filter
            if (SelectedStatus.HasValue)
            {
                query = query.Where(r => r.Status == SelectedStatus.Value);
            }
            
            // Apply date range filter
            if (StartDate.HasValue)
            {
                query = query.Where(r => r.RequestDate >= StartDate.Value);
            }
            
            if (EndDate.HasValue)
            {
                var endOfDay = EndDate.Value.AddDays(1).AddTicks(-1);
                query = query.Where(r => r.RequestDate <= endOfDay);
            }
            
            // Apply search filter
            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var searchLower = SearchTerm.ToLower();
                query = query.Where(r => 
                    r.MobileNumberAssignedTo.ToLower().Contains(searchLower) ||
                    r.PrimaryMobileNumber.Contains(searchLower) ||
                    r.IndexNo.ToLower().Contains(searchLower) ||
                    r.Office.ToLower().Contains(searchLower) ||
                    r.Organization.ToLower().Contains(searchLower));
            }
            
            return query;
        }
    }
} 