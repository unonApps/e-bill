using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TAB.Web.Data;
using TAB.Web.Models;
using TAB.Web.Services;

namespace TAB.Web.Pages.Modules.SimManagement.Approvals.Supervisor
{
    [Authorize] // Only require authentication, not specific roles
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ISimRequestHistoryService _historyService;
        private readonly INotificationService _notificationService;
        private readonly IAuditLogService _auditLogService;

        public IndexModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager, ISimRequestHistoryService historyService, INotificationService notificationService, IAuditLogService auditLogService)
        {
            _context = context;
            _userManager = userManager;
            _historyService = historyService;
            _notificationService = notificationService;
            _auditLogService = auditLogService;
        }

        // SIM Request Properties
        public List<UnifiedRequest> PendingSimRequests { get; set; } = new();
        public List<UnifiedRequest> ProcessedSimRequests { get; set; } = new();
        public List<UnifiedRequest> RejectedSimRequests { get; set; } = new();
        
        // Summary statistics
        public int PendingCount { get; set; }
        public int ProcessedCount { get; set; }
        public int RejectedCount { get; set; }
        
        // Current supervisor information
        public string? CurrentSupervisorName { get; set; }
        public string? CurrentSupervisorEmail { get; set; }
        public bool IsAdmin { get; set; }
        
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

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null)
            {
                IsAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");
            }

            await LoadSimRequestsAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostApproveAsync(
            int requestId,
            string? action,
            string? notes,
            string? mobileService,
            string? mobileServiceAllowance,
            string? handsetAllowance,
            string? supervisorRemarks,
            string? supervisorName,
            string? supervisorEmail,
            DateTime? supervisorActionDate)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Challenge();
            }

            try
            {
                // Handle different actions based on the button clicked
                if (action == "revert")
                {
                    return await RevertSimRequestAsync(requestId, currentUser, supervisorRemarks);
                }
                else if (action == "approve")
                {
                    return await ApproveSimRequestAsync(requestId, currentUser, notes, mobileService, mobileServiceAllowance, handsetAllowance, supervisorRemarks, supervisorName, supervisorEmail, supervisorActionDate);
                }
                else
                {
                    StatusMessage = "Invalid action specified.";
                    StatusMessageClass = "danger";
                    await LoadSimRequestsAsync();
                    return Page();
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"An error occurred while processing the request: {ex.Message}";
                StatusMessageClass = "danger";
                await LoadSimRequestsAsync();
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
                return await RejectSimRequestAsync(requestId, currentUser, notes);
            }
            catch (Exception)
            {
                StatusMessage = "An error occurred while processing the rejection.";
                StatusMessageClass = "danger";
                await LoadSimRequestsAsync();
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
                return await RevertSimRequestAsync(requestId, currentUser, notes);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error processing request: {ex.Message}";
                StatusMessageClass = "danger";
                await LoadSimRequestsAsync();
                return Page();
            }
        }

        public async Task<IActionResult> OnGetServiceAllowancesAsync(string service)
        {
            try
            {
                var classOfService = await _context.ClassOfServices
                    .Where(c => c.Service == service && c.ServiceStatus == ServiceStatus.Active)
                    .FirstOrDefaultAsync();

                if (classOfService != null)
                {
                    return new JsonResult(new
                    {
                        success = true,
                        mobileServiceAllowance = classOfService.AirtimeAllowance ?? "0",
                        handsetAllowance = classOfService.HandsetAllowance ?? "0",
                        dataAllowance = classOfService.DataAllowance ?? "0"
                    });
                }

                return new JsonResult(new
                {
                    success = false,
                    message = "Service configuration not found"
                });
            }
            catch (Exception ex)
            {
                return new JsonResult(new
                {
                    success = false,
                    message = $"Error retrieving service allowances: {ex.Message}"
                });
            }
        }

        private async Task LoadSimRequestsAsync()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return;

            CurrentSupervisorName = $"{currentUser.FirstName} {currentUser.LastName}";
            CurrentSupervisorEmail = currentUser.Email;

            // Check if user is admin
            var isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");
            
            List<TAB.Web.Models.SimRequest> simRequests;
            
            if (isAdmin)
            {
                // Admin sees all SIM requests
                simRequests = await _context.SimRequests
                    .Include(r => r.ServiceProvider)
                    .OrderByDescending(r => r.RequestDate)
                    .ToListAsync();
            }
            else
            {
            // Load SIM requests where current user is the supervisor (by email)
                simRequests = await _context.SimRequests
                .Include(r => r.ServiceProvider)
                .Where(r => r.SupervisorEmail == currentUser.Email || r.Supervisor == currentUser.Email)
                .OrderByDescending(r => r.RequestDate)
                .ToListAsync();
            }

            // Convert to unified requests for display
            var unifiedRequests = simRequests.Select(r => new UnifiedRequest
            {
                Id = r.Id,
                RequestType = RequestType.SimCard,
                StaffName = $"{r.FirstName} {r.LastName}",
                Email = r.OfficialEmail,
                Department = r.Organization,
                Organization = r.Organization,
                Office = r.Office,
                RequestTitle = $"SIM Card Request - {r.ServiceProvider?.ServiceProviderName ?? "Unknown Provider"}",
                RequestDescription = $"Index: {r.IndexNo}, Type: {r.SimType}{(isAdmin ? $" | Supervisor: {r.Supervisor}" : "")}",
                RequestDate = r.RequestDate,
                Status = r.Status.ToString(),
                Priority = GetPriority(r.RequestDate),
                ServiceProvider = r.ServiceProvider,
                OriginalRequest = r
            }).ToList();

            // Categorize requests by status
            PendingSimRequests = unifiedRequests.Where(r => r.Status == "PendingSupervisor").ToList();
            ProcessedSimRequests = unifiedRequests.Where(r => r.Status == "PendingAdmin" || r.Status == "PendingIcts" || r.Status == "Approved" || r.Status == "Completed").ToList();
            RejectedSimRequests = unifiedRequests.Where(r => r.Status == "Rejected").ToList();

            // Update counts
            PendingCount = PendingSimRequests.Count;
            ProcessedCount = ProcessedSimRequests.Count;
            RejectedCount = RejectedSimRequests.Count;
        }

        private async Task<IActionResult> ApproveSimRequestAsync(int requestId, ApplicationUser currentUser, string? notes, string? mobileService, string? mobileServiceAllowance, string? handsetAllowance, string? supervisorRemarks, string? supervisorName, string? supervisorEmail, DateTime? supervisorActionDate)
        {
            var simRequest = await _context.SimRequests.FindAsync(requestId);
            if (simRequest == null)
            {
                StatusMessage = "SIM request not found.";
                StatusMessageClass = "danger";
                await LoadSimRequestsAsync();
                return Page();
            }

            // Update request with supervisor approval - now goes to ICTS instead of Admin
            simRequest.Status = RequestStatus.PendingIcts;
            simRequest.SupervisorApprovalDate = supervisorActionDate;
            simRequest.SupervisorNotes = notes;
            simRequest.MobileService = mobileService;
            simRequest.MobileServiceAllowance = mobileServiceAllowance;
            simRequest.HandsetAllowance = handsetAllowance;
            simRequest.SupervisorRemarks = supervisorRemarks;
            simRequest.SupervisorName = supervisorName;
            simRequest.SupervisorEmail = supervisorEmail;

            await _context.SaveChangesAsync();

            // Add history entry
            await _historyService.AddApprovalHistoryAsync(
                requestId,
                "supervisor",
                true,
                currentUser.Id,
                $"{currentUser.FirstName} {currentUser.LastName}",
                notes ?? "Request approved by supervisor"
            );

            // Send notification to requester
            await _notificationService.NotifySimRequestSupervisorApprovedAsync(
                requestId,
                simRequest.RequestedBy,
                supervisorRemarks
            );

            // Log audit trail
            await _auditLogService.LogSimRequestApprovedAsync(
                requestId,
                "Supervisor",
                $"{currentUser.FirstName} {currentUser.LastName}",
                $"{simRequest.FirstName} {simRequest.LastName}",
                supervisorRemarks,
                currentUser.Id,
                HttpContext.Connection.RemoteIpAddress?.ToString()
            );

            StatusMessage = "SIM request approved successfully and forwarded to UNON/ICTS.";
            StatusMessageClass = "success";
            await LoadSimRequestsAsync();
            return Page();
        }

        private async Task<IActionResult> RejectSimRequestAsync(int requestId, ApplicationUser currentUser, string? notes)
        {
            var simRequest = await _context.SimRequests.FindAsync(requestId);
            if (simRequest == null)
            {
                StatusMessage = "SIM request not found.";
                StatusMessageClass = "danger";
                await LoadSimRequestsAsync();
                return Page();
            }

            simRequest.Status = RequestStatus.Rejected;
            simRequest.SupervisorApprovalDate = DateTime.UtcNow;
            simRequest.SupervisorNotes = notes;

            await _context.SaveChangesAsync();

            // Add history entry
            await _historyService.AddApprovalHistoryAsync(
                requestId,
                "supervisor",
                false,
                currentUser.Id,
                $"{currentUser.FirstName} {currentUser.LastName}",
                notes ?? "Request rejected by supervisor"
            );

            // Send notification to requester
            await _notificationService.NotifySimRequestSupervisorRejectedAsync(
                requestId,
                simRequest.RequestedBy,
                notes
            );

            // Log audit trail
            await _auditLogService.LogSimRequestRejectedAsync(
                requestId,
                "Supervisor",
                $"{currentUser.FirstName} {currentUser.LastName}",
                $"{simRequest.FirstName} {simRequest.LastName}",
                notes,
                currentUser.Id,
                HttpContext.Connection.RemoteIpAddress?.ToString()
            );

            StatusMessage = "SIM request rejected successfully.";
            StatusMessageClass = "success";
            await LoadSimRequestsAsync();
            return Page();
        }

        private async Task<IActionResult> RevertSimRequestAsync(int requestId, ApplicationUser currentUser, string? notes)
        {
            var simRequest = await _context.SimRequests.FindAsync(requestId);
            if (simRequest == null)
            {
                StatusMessage = "SIM request not found.";
                StatusMessageClass = "danger";
                await LoadSimRequestsAsync();
                return Page();
            }

            simRequest.Status = RequestStatus.Draft;
            simRequest.SupervisorApprovalDate = null;
            simRequest.SupervisorNotes = null;

            await _context.SaveChangesAsync();

            // Add history entry
            await _historyService.AddReversionHistoryAsync(
                requestId,
                "supervisor",
                currentUser.Id,
                $"{currentUser.FirstName} {currentUser.LastName}",
                notes ?? "Request reverted to requestor by supervisor"
            );

            StatusMessage = "SIM request reverted to requestor successfully.";
            StatusMessageClass = "success";
            await LoadSimRequestsAsync();
            return Page();
        }

        public string GetStatusBadgeClass(string status)
        {
            return status switch
            {
                "PendingSupervisor" => "bg-warning text-dark",
                "PendingIcts" => "bg-info text-white",
                "PendingAdmin" => "bg-info text-white",
                "Approved" => "bg-success text-white",
                "Rejected" => "bg-danger text-white",
                "Completed" => "bg-primary text-white",
                _ => "bg-secondary text-white"
            };
        }

        public string GetStatusIcon(string status)
        {
            return status switch
            {
                "PendingSupervisor" => "bi-clock-history",
                "PendingIcts" => "bi-gear-fill",
                "PendingAdmin" => "bi-gear",
                "Approved" => "bi-check-circle",
                "Rejected" => "bi-x-circle",
                "Completed" => "bi-check-all",
                _ => "bi-question-circle"
            };
        }

        public string GetPriority(DateTime requestDate)
        {
            var daysSinceRequest = (DateTime.UtcNow - requestDate).Days;
            return daysSinceRequest switch
            {
                > 7 => "Urgent",
                > 3 => "Attention",
                _ => "Normal"
            };
        }

        public string GetPriorityColor(string priority)
        {
            return priority switch
            {
                "Urgent" => "danger",
                "Attention" => "warning",
                _ => "primary"
            };
        }

        public string GetRequestStatus(UnifiedRequest request)
        {
            if (request.OriginalRequest is Models.SimRequest simRequest)
            {
                return simRequest.Status.ToString();
            }
            return "Unknown";
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

            // Check if user's email exists as a supervisor in any requests
            var hasSimRequests = await _context.SimRequests
                .AnyAsync(r => r.SupervisorEmail == currentUser.Email || r.Supervisor == currentUser.Email);

            return hasSimRequests;
        }
    }
} 
