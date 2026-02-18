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
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ISimRequestHistoryService _historyService;

        public DetailsModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager, ISimRequestHistoryService historyService)
        {
            _context = context;
            _userManager = userManager;
            _historyService = historyService;
        }

        public Models.SimRequest? SimRequest { get; set; }
        public List<SimRequestHistory> History { get; set; } = new();
        public ApplicationUser? SupervisorDetails { get; set; }

        public async Task<IActionResult> OnGetAsync(Guid id)
        {
            SimRequest = await _context.SimRequests
                .Include(r => r.ServiceProvider)
                .FirstOrDefaultAsync(r => r.PublicId == id);

            if (SimRequest == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Forbid();
            }

            // Allow access for: requestor, assigned supervisor, ICTS role, Admin role
            var isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");
            var isIcts = await _userManager.IsInRoleAsync(currentUser, "ICTS");
            var isSupervisor = SimRequest.Supervisor == currentUser.Email || SimRequest.SupervisorEmail == currentUser.Email;
            var isRequestor = SimRequest.RequestedBy == currentUser.Id;

            if (!isRequestor && !isSupervisor && !isIcts && !isAdmin)
            {
                return Forbid();
            }

            // Load history
            History = await _historyService.GetHistoryAsync(SimRequest.Id);

            // Load supervisor details if supervisor name is available
            // Supervisor field stores email - look up by email first, then by SupervisorEmail field
            if (!string.IsNullOrEmpty(SimRequest.SupervisorEmail))
            {
                SupervisorDetails = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == SimRequest.SupervisorEmail);
            }
            if (SupervisorDetails == null && !string.IsNullOrEmpty(SimRequest.Supervisor))
            {
                SupervisorDetails = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == SimRequest.Supervisor);
            }

            return Page();
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

        public static string GetStatusAlertClass(RequestStatus status)
        {
            return status switch
            {
                RequestStatus.Draft => "alert-secondary",
                RequestStatus.PendingSupervisor => "alert-warning",
                RequestStatus.PendingIcts => "alert-primary",
                RequestStatus.PendingAdmin => "alert-info",
                RequestStatus.PendingServiceProvider => "alert-warning",
                RequestStatus.PendingSIMCollection => "alert-info",
                RequestStatus.Approved => "alert-success",
                RequestStatus.Completed => "alert-primary",
                RequestStatus.Rejected => "alert-danger",
                RequestStatus.Cancelled => "alert-secondary",
                _ => "alert-secondary"
            };
        }

        public string GetHistoryIcon(string action)
        {
            return action switch
            {
                HistoryActions.Created => "bi-plus-circle",
                HistoryActions.Updated => "bi-pencil",
                HistoryActions.StatusChanged => "bi-arrow-repeat",
                HistoryActions.SubmittedToSupervisor => "bi-send",
                HistoryActions.SupervisorApproved => "bi-check-circle",
                HistoryActions.SupervisorRejected => "bi-x-circle",
                HistoryActions.SupervisorReverted => "bi-arrow-left-circle",
                HistoryActions.AdminApproved => "bi-shield-check",
                HistoryActions.AdminRejected => "bi-shield-x",
                HistoryActions.IctsProcessed => "bi-gear",
                HistoryActions.IctsReverted => "bi-arrow-counterclockwise",
                HistoryActions.IctsNewSimApproved => "bi-sim-fill",
                HistoryActions.IctsCollectionNotified => "bi-bell",
                HistoryActions.Completed => "bi-check-all",
                HistoryActions.Cancelled => "bi-x-square",
                HistoryActions.CommentAdded => "bi-chat-text",
                _ => "bi-info-circle"
            };
        }

        public string GetHistoryBadgeClass(string action)
        {
            return action switch
            {
                HistoryActions.Created => "bg-success text-white",
                HistoryActions.Updated => "bg-info text-white",
                HistoryActions.StatusChanged => "bg-warning text-dark",
                HistoryActions.SubmittedToSupervisor => "bg-primary text-white",
                HistoryActions.SupervisorApproved => "bg-success text-white",
                HistoryActions.SupervisorRejected => "bg-danger text-white",
                HistoryActions.SupervisorReverted => "bg-warning text-dark",
                HistoryActions.AdminApproved => "bg-success text-white",
                HistoryActions.AdminRejected => "bg-danger text-white",
                HistoryActions.IctsProcessed => "bg-info text-white",
                HistoryActions.IctsReverted => "bg-warning text-dark",
                HistoryActions.IctsNewSimApproved => "bg-success text-white",
                HistoryActions.IctsCollectionNotified => "bg-primary text-white",
                HistoryActions.Completed => "bg-primary text-white",
                HistoryActions.Cancelled => "bg-secondary text-white",
                HistoryActions.CommentAdded => "bg-info text-white",
                _ => "bg-secondary text-white"
            };
        }

        public string GetStatusBadgeClass(string status)
        {
            return status switch
            {
                "Draft" => "bg-secondary text-white",
                "PendingSupervisor" => "bg-warning text-dark",
                "PendingIcts" => "bg-primary text-white",
                "PendingAdmin" => "bg-info text-white",
                "PendingServiceProvider" => "bg-warning text-dark",
                "PendingSIMCollection" => "bg-info text-white",
                "Approved" => "bg-success text-white",
                "Rejected" => "bg-danger text-white",
                "Completed" => "bg-primary text-white",
                "Cancelled" => "bg-dark text-white",
                _ => "bg-secondary text-white"
            };
        }
    }
} 