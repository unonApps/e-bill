using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TAB.Web.Data;
using TAB.Web.Models;

namespace TAB.Web.Pages.Modules.SimManagement.Approvals
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // All SIM requests for the unified table view
        public List<SimRequest> SimRequests { get; set; } = new();

        // Statistics
        public int TotalRequests { get; set; }
        public int PendingSupervisorCount { get; set; }
        public int PendingIctsCount { get; set; }
        public int PendingAdminCount { get; set; }
        public int PendingServiceProviderCount { get; set; }
        public int PendingCollectionCount { get; set; }
        public int ApprovedCount { get; set; }
        public int CompletedCount { get; set; }
        public int RejectedCount { get; set; }
        public int CancelledCount { get; set; }

        // Pagination
        public int CurrentPage { get; set; }
        public int PageSize { get; set; } = 20;
        public int TotalPages { get; set; }
        public int TotalRecords { get; set; }

        // User role flags
        public bool IsSupervisor { get; set; }
        public bool IsIcts { get; set; }
        public bool IsAdmin { get; set; }

        public async Task<IActionResult> OnGetAsync(int page = 1)
        {
            var userName = User.Identity?.Name;

            // Set role flags
            IsAdmin = User.IsInRole("Admin");
            IsSupervisor = User.IsInRole("Supervisor");
            IsIcts = User.IsInRole("ICTS");

            // Set current page
            CurrentPage = page < 1 ? 1 : page;

            // Build query based on user role
            IQueryable<SimRequest> query = _context.SimRequests.Include(s => s.ServiceProvider);

            if (IsAdmin)
            {
                // Admin sees all requests
                query = query.OrderByDescending(r => r.RequestDate);
            }
            else if (IsSupervisor && IsIcts)
            {
                // User has both roles - show requests assigned to them as supervisor + ICTS pending
                query = query.Where(r => r.Supervisor == userName || r.SupervisorEmail == userName || r.Status == RequestStatus.PendingIcts)
                            .OrderByDescending(r => r.RequestDate);
            }
            else if (IsSupervisor)
            {
                // Supervisor sees only their assigned requests
                query = query.Where(r => r.Supervisor == userName || r.SupervisorEmail == userName)
                            .OrderByDescending(r => r.RequestDate);
            }
            else if (IsIcts)
            {
                // ICTS sees only pending ICTS requests
                query = query.Where(r => r.Status == RequestStatus.PendingIcts)
                            .OrderByDescending(r => r.RequestDate);
            }
            else
            {
                // Other users see nothing (or could show their own requests if needed)
                query = query.Where(r => false).OrderByDescending(r => r.RequestDate);
            }

            // Get total count before pagination
            TotalRecords = await query.CountAsync();
            TotalPages = (int)Math.Ceiling(TotalRecords / (double)PageSize);

            // Apply pagination
            SimRequests = await query
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            // Calculate statistics using database-level counts (not materializing all records)
            var statsBaseQuery = _context.SimRequests
                .Where(r => IsAdmin ||
                           (IsSupervisor && (r.Supervisor == userName || r.SupervisorEmail == userName)) ||
                           (IsIcts && r.Status == RequestStatus.PendingIcts));

            // Group by status and count in a single query
            var statusCounts = await statsBaseQuery
                .GroupBy(r => r.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            var countLookup = statusCounts.ToDictionary(x => x.Status, x => x.Count);
            TotalRequests = statusCounts.Sum(x => x.Count);
            PendingSupervisorCount = countLookup.GetValueOrDefault(RequestStatus.PendingSupervisor);
            PendingIctsCount = countLookup.GetValueOrDefault(RequestStatus.PendingIcts);
            PendingAdminCount = countLookup.GetValueOrDefault(RequestStatus.PendingAdmin);
            PendingServiceProviderCount = countLookup.GetValueOrDefault(RequestStatus.PendingServiceProvider);
            PendingCollectionCount = countLookup.GetValueOrDefault(RequestStatus.PendingSIMCollection);
            ApprovedCount = countLookup.GetValueOrDefault(RequestStatus.Approved);
            CompletedCount = countLookup.GetValueOrDefault(RequestStatus.Completed);
            RejectedCount = countLookup.GetValueOrDefault(RequestStatus.Rejected);
            CancelledCount = countLookup.GetValueOrDefault(RequestStatus.Cancelled);

            return Page();
        }
    }
}