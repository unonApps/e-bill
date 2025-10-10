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

        public async Task<IActionResult> OnGetAsync(int id)
        {
            SimRequest = await _context.SimRequests
                .Include(r => r.ServiceProvider)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (SimRequest == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (SimRequest.RequestedBy != currentUser?.Id)
            {
                return Forbid();
            }

            // Load history
            History = await _historyService.GetHistoryAsync(id);

            // Load supervisor details if supervisor name is available
            if (!string.IsNullOrEmpty(SimRequest.Supervisor))
            {
                try
                {
                    // Try to find supervisor by full name (exact match first)
                    SupervisorDetails = await _context.Users
                        .FirstOrDefaultAsync(u => (u.FirstName + " " + u.LastName) == SimRequest.Supervisor);

                    // If not found, try splitting the name and matching parts
                    if (SupervisorDetails == null)
                    {
                        var nameParts = SimRequest.Supervisor.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        if (nameParts.Length >= 2)
                        {
                            var firstName = nameParts[0];
                            var lastName = string.Join(" ", nameParts.Skip(1));
                            
                            SupervisorDetails = await _context.Users
                                .FirstOrDefaultAsync(u => u.FirstName == firstName && u.LastName == lastName);
                        }
                    }
                }
                catch (Exception)
                {
                    // If there's any error looking up supervisor details, we'll just show the name without contact info
                    SupervisorDetails = null;
                }
            }

            return Page();
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
                _ => "bi-question-circle"
            };
        }

        public static string GetStatusAlertClass(RequestStatus status)
        {
            return status switch
            {
                RequestStatus.Draft => "alert-secondary",
                RequestStatus.PendingSupervisor => "alert-warning",
                RequestStatus.PendingAdmin => "alert-info",
                RequestStatus.Approved => "alert-success",
                RequestStatus.Completed => "alert-primary",
                RequestStatus.Rejected => "alert-danger",
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
                "PendingAdmin" => "bg-info text-white",
                "Approved" => "bg-success text-white",
                "Rejected" => "bg-danger text-white",
                "Completed" => "bg-primary text-white",
                _ => "bg-secondary text-white"
            };
        }
    }
} 