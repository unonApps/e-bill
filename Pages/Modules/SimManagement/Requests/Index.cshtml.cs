using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TAB.Web.Data;
using TAB.Web.Models;

namespace TAB.Web.Pages.Modules.SimManagement.Requests
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public IndexModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public List<Models.SimRequest> UserRequests { get; set; } = new();
        public string? StatusMessage { get; set; }
        public string? StatusMessageClass { get; set; }
        public bool IsAdmin { get; set; }
        public string CurrentUserName { get; set; } = string.Empty;
        
        // Statistics
        public int TotalRequests { get; set; }
        public int PendingRequests { get; set; }
        public int ApprovedRequests { get; set; }
        public int RejectedRequests { get; set; }
        
        // Filter properties
        [BindProperty(SupportsGet = true)]
        public string? SelectedUserId { get; set; }
        
        [BindProperty(SupportsGet = true)]
        public RequestStatus? SelectedStatus { get; set; }
        
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

        public async Task OnGetAsync()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null)
            {
                CurrentUserName = !string.IsNullOrEmpty(currentUser.FirstName) && !string.IsNullOrEmpty(currentUser.LastName) 
                    ? $"{currentUser.FirstName} {currentUser.LastName}" 
                    : currentUser.UserName ?? "User";
                    
                IsAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");
                
                if (IsAdmin)
                {
                    // Load all users for filter dropdown
                    AvailableUsers = await _userManager.Users
                        .OrderBy(u => u.FirstName)
                        .ToListAsync();
                }
            }
            await LoadUserRequestsAsync();
        }

        public async Task<IActionResult> OnPostCancelRequestAsync(int requestId)
        {
            var request = await _context.SimRequests.FindAsync(requestId);
            if (request == null)
            {
                StatusMessage = "Request not found.";
                StatusMessageClass = "danger";
                await LoadUserRequestsAsync();
                return Page();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            var isAdmin = currentUser != null && await _userManager.IsInRoleAsync(currentUser, "Admin");
            
            // Check ownership (admin can cancel any request)
            if (!isAdmin && request.RequestedBy != currentUser?.Id)
            {
                StatusMessage = "You can only cancel your own requests.";
                StatusMessageClass = "danger";
                await LoadUserRequestsAsync();
                return Page();
            }

            if (request.Status != RequestStatus.Draft && request.Status != RequestStatus.PendingSupervisor)
            {
                StatusMessage = "Only draft and pending supervisor requests can be cancelled.";
                StatusMessageClass = "warning";
                await LoadUserRequestsAsync();
                return Page();
            }

            request.Status = RequestStatus.Cancelled;
            request.ProcessingNotes = isAdmin ? $"Cancelled by admin ({currentUser?.FirstName ?? "Unknown"} {currentUser?.LastName ?? "User"})" : "Cancelled by user";
            request.ProcessedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            StatusMessage = isAdmin ? 
                $"Request for {request.FirstName} {request.LastName} has been cancelled successfully." :
                "Request has been cancelled successfully.";
            StatusMessageClass = "success";
            await LoadUserRequestsAsync();
            return Page();
        }

        private async Task LoadUserRequestsAsync()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null)
            {
                // Check if user is admin
                var isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");
                
                // Build base query
                var query = _context.SimRequests
                    .Include(r => r.ServiceProvider)
                    .AsQueryable();
                
                if (isAdmin)
                {
                    // Apply user filter for admin
                    if (!string.IsNullOrEmpty(SelectedUserId))
                    {
                        query = query.Where(r => r.RequestedBy == SelectedUserId);
                    }
                }
                else
                {
                    // Regular users see only their own requests
                    query = query.Where(r => r.RequestedBy == currentUser.Id);
                }
                
                // Apply filters
                query = ApplyFilters(query);

                // Get total count before pagination (for statistics)
                var allRequests = await query.ToListAsync();

                // Calculate statistics from all filtered results
                TotalRequests = allRequests.Count;
                PendingRequests = allRequests.Count(r =>
                    r.Status == RequestStatus.Draft ||
                    r.Status == RequestStatus.PendingSupervisor ||
                    r.Status == RequestStatus.PendingIcts ||
                    r.Status == RequestStatus.PendingAdmin ||
                    r.Status == RequestStatus.PendingServiceProvider ||
                    r.Status == RequestStatus.PendingSIMCollection);
                ApprovedRequests = allRequests.Count(r => r.Status == RequestStatus.Approved || r.Status == RequestStatus.Completed);
                RejectedRequests = allRequests.Count(r => r.Status == RequestStatus.Rejected || r.Status == RequestStatus.Cancelled);

                // Calculate pagination
                TotalRecords = allRequests.Count;
                TotalPages = (int)Math.Ceiling(TotalRecords / (double)PageSize);

                // Apply pagination
                UserRequests = allRequests
                    .OrderByDescending(r => r.RequestDate)
                    .Skip((PageNumber - 1) * PageSize)
                    .Take(PageSize)
                    .ToList();
            }
        }
        
        private IQueryable<SimRequest> ApplyFilters(IQueryable<SimRequest> query)
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
                    r.FirstName.ToLower().Contains(searchLower) ||
                    r.LastName.ToLower().Contains(searchLower) ||
                    r.IndexNo.ToLower().Contains(searchLower) ||
                    r.Office.ToLower().Contains(searchLower) ||
                    r.Organization.ToLower().Contains(searchLower) ||
                    r.OfficialEmail.ToLower().Contains(searchLower));
            }
            
            return query;
        }

        public static string GetStatusBadgeClass(RequestStatus status)
        {
            return status switch
            {
                RequestStatus.Draft => "bg-secondary",
                RequestStatus.PendingSupervisor => "bg-warning text-dark",
                RequestStatus.PendingAdmin => "bg-info",
                RequestStatus.Approved => "bg-success",
                RequestStatus.Completed => "bg-primary",
                RequestStatus.Rejected => "bg-danger",
                RequestStatus.Cancelled => "bg-dark",
                _ => "bg-secondary"
            };
        }

        public static string GetStatusIcon(RequestStatus status)
        {
            return status switch
            {
                RequestStatus.Draft => "bi-file-earmark",
                RequestStatus.PendingSupervisor => "bi-clock",
                RequestStatus.PendingAdmin => "bi-hourglass-split",
                RequestStatus.Approved => "bi-check-circle",
                RequestStatus.Completed => "bi-check-all",
                RequestStatus.Rejected => "bi-x-circle",
                RequestStatus.Cancelled => "bi-slash-circle",
                _ => "bi-question-circle"
            };
        }
    }
} 