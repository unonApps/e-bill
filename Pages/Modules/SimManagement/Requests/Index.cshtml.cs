using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TAB.Web.Data;
using TAB.Web.Models;
using TAB.Web.Services;

namespace TAB.Web.Pages.Modules.SimManagement.Requests
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ISimRequestHistoryService _historyService;
        private readonly INotificationService _notificationService;
        private readonly IAuditLogService _auditLogService;

        public IndexModel(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ISimRequestHistoryService historyService,
            INotificationService notificationService,
            IAuditLogService auditLogService)
        {
            _context = context;
            _userManager = userManager;
            _historyService = historyService;
            _notificationService = notificationService;
            _auditLogService = auditLogService;
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

        public int PageSize { get; set; } = 10;
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
            var userName = $"{currentUser?.FirstName ?? "Unknown"} {currentUser?.LastName ?? "User"}".Trim();
            request.ProcessingNotes = isAdmin ? $"Cancelled by admin ({userName})" : "Cancelled by user";
            request.ProcessedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Log history, notification, and audit trail
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            await _historyService.AddHistoryAsync(
                requestId,
                HistoryActions.Cancelled,
                request.Status.ToString(),
                RequestStatus.Cancelled.ToString(),
                request.ProcessingNotes,
                currentUser!.Id,
                userName,
                ipAddress
            );

            await _notificationService.NotifySimRequestCancelledAsync(
                requestId,
                request.RequestedBy
            );

            await _auditLogService.LogSimRequestRejectedAsync(
                requestId,
                "Cancellation",
                userName,
                $"{request.FirstName} {request.LastName}",
                request.ProcessingNotes,
                currentUser.Id,
                ipAddress
            );

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
                var baseQuery = _context.SimRequests
                    .Include(r => r.ServiceProvider)
                    .AsQueryable();

                if (isAdmin)
                {
                    // Apply user filter for admin
                    if (!string.IsNullOrEmpty(SelectedUserId))
                    {
                        baseQuery = baseQuery.Where(r => r.RequestedBy == SelectedUserId);
                    }
                }
                else
                {
                    // Regular users see only their own requests
                    baseQuery = baseQuery.Where(r => r.RequestedBy == currentUser.Id);
                }

                // Calculate statistics using database-level counts (not materializing all records)
                var statusCounts = await baseQuery
                    .GroupBy(r => r.Status)
                    .Select(g => new { Status = g.Key, Count = g.Count() })
                    .ToListAsync();

                var countLookup = statusCounts.ToDictionary(x => x.Status, x => x.Count);
                TotalRequests = statusCounts.Sum(x => x.Count);
                PendingRequests = countLookup.GetValueOrDefault(RequestStatus.Draft) +
                    countLookup.GetValueOrDefault(RequestStatus.PendingSupervisor) +
                    countLookup.GetValueOrDefault(RequestStatus.PendingIcts) +
                    countLookup.GetValueOrDefault(RequestStatus.PendingAdmin) +
                    countLookup.GetValueOrDefault(RequestStatus.PendingServiceProvider) +
                    countLookup.GetValueOrDefault(RequestStatus.PendingSIMCollection);
                ApprovedRequests = countLookup.GetValueOrDefault(RequestStatus.Approved) +
                    countLookup.GetValueOrDefault(RequestStatus.Completed);
                RejectedRequests = countLookup.GetValueOrDefault(RequestStatus.Rejected) +
                    countLookup.GetValueOrDefault(RequestStatus.Cancelled);

                // Apply filters for the table display
                var filteredQuery = ApplyFilters(baseQuery);

                // Calculate pagination using database count
                TotalRecords = await filteredQuery.CountAsync();
                TotalPages = (int)Math.Ceiling(TotalRecords / (double)PageSize);

                // Apply pagination at database level
                UserRequests = await filteredQuery
                    .OrderByDescending(r => r.RequestDate)
                    .Skip((PageNumber - 1) * PageSize)
                    .Take(PageSize)
                    .ToListAsync();
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
                RequestStatus.PendingIcts => "bg-primary",
                RequestStatus.PendingAdmin => "bg-info",
                RequestStatus.PendingServiceProvider => "bg-warning text-dark",
                RequestStatus.PendingSIMCollection => "bg-info",
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
                RequestStatus.PendingIcts => "bi-gear-fill",
                RequestStatus.PendingAdmin => "bi-hourglass-split",
                RequestStatus.PendingServiceProvider => "bi-telephone",
                RequestStatus.PendingSIMCollection => "bi-collection",
                RequestStatus.Approved => "bi-check-circle",
                RequestStatus.Completed => "bi-check-all",
                RequestStatus.Rejected => "bi-x-circle",
                RequestStatus.Cancelled => "bi-slash-circle",
                _ => "bi-question-circle"
            };
        }
    }
} 