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
        private readonly IEnhancedEmailService _emailService;
        private readonly ILogger<IndexModel> _logger;
        private readonly IConfiguration _configuration;

        public IndexModel(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ISimRequestHistoryService historyService,
            INotificationService notificationService,
            IAuditLogService auditLogService,
            IEnhancedEmailService emailService,
            ILogger<IndexModel> logger,
            IConfiguration configuration)
        {
            _context = context;
            _userManager = userManager;
            _historyService = historyService;
            _notificationService = notificationService;
            _auditLogService = auditLogService;
            _emailService = emailService;
            _logger = logger;
            _configuration = configuration;
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

        // Detail view properties
        public bool IsDetailView { get; set; }
        public SimRequest? CurrentRequest { get; set; }
        public List<SimRequestHistory> RequestHistory { get; set; } = new();

        [TempData]
        public string? StatusMessage { get; set; }
        [TempData]
        public string? StatusMessageClass { get; set; }

        public async Task<IActionResult> OnGetAsync(Guid? requestId)
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
                CurrentSupervisorName = $"{currentUser.FirstName} {currentUser.LastName}";
                CurrentSupervisorEmail = currentUser.Email;
            }

            // Check if we're showing detail view
            if (requestId.HasValue)
            {
                IsDetailView = true;
                CurrentRequest = await _context.SimRequests
                    .Include(s => s.ServiceProvider)
                    .Include(s => s.History)
                    .FirstOrDefaultAsync(s => s.PublicId == requestId.Value);

                if (CurrentRequest == null)
                {
                    StatusMessage = "Request not found.";
                    StatusMessageClass = "danger";
                    return RedirectToPage("/Modules/SimManagement/Approvals/Index");
                }

                // Load request history
                RequestHistory = await _context.SimRequestHistories
                    .Where(h => h.SimRequestId == CurrentRequest.Id)
                    .OrderByDescending(h => h.Timestamp)
                    .ToListAsync();

                return Page();
            }

            // List view - load all requests
            IsDetailView = false;
            await LoadSimRequestsAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostApproveAsync(
            Guid requestId,
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
                    return RedirectToPage("/Dashboard/Approver/Index");
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"An error occurred while processing the request: {ex.Message}";
                StatusMessageClass = "danger";
                return RedirectToPage("/Dashboard/Approver/Index");
            }
        }

        public async Task<IActionResult> OnPostRejectAsync(Guid requestId, string? notes)
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
                return RedirectToPage("/Dashboard/Approver/Index");
            }
        }

        public async Task<IActionResult> OnPostRevertAsync(Guid requestId, string? notes)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                StatusMessage = "User not found.";
                StatusMessageClass = "danger";
                return RedirectToPage("/Dashboard/Approver/Index");
            }

            try
            {
                return await RevertSimRequestAsync(requestId, currentUser, notes);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error processing request: {ex.Message}";
                StatusMessageClass = "danger";
                return RedirectToPage("/Dashboard/Approver/Index");
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
                PublicId = r.PublicId,
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

        private async Task<IActionResult> ApproveSimRequestAsync(Guid requestId, ApplicationUser currentUser, string? notes, string? mobileService, string? mobileServiceAllowance, string? handsetAllowance, string? supervisorRemarks, string? supervisorName, string? supervisorEmail, DateTime? supervisorActionDate)
        {
            var simRequest = await _context.SimRequests.FirstOrDefaultAsync(s => s.PublicId == requestId);
            if (simRequest == null)
            {
                StatusMessage = "SIM request not found.";
                StatusMessageClass = "danger";
                return RedirectToPage("/Dashboard/Approver/Index");
            }

            // Check if request is already approved to prevent duplicate processing
            if (simRequest.Status != RequestStatus.PendingSupervisor)
            {
                StatusMessage = $"This request has already been processed. Current status: {simRequest.Status}";
                StatusMessageClass = "warning";
                return RedirectToPage("/Dashboard/Approver/Index");
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
                simRequest.Id,
                "supervisor",
                true,
                currentUser.Id,
                $"{currentUser.FirstName} {currentUser.LastName}",
                notes ?? "Request approved by supervisor"
            );

            // Send notification to requester
            await _notificationService.NotifySimRequestSupervisorApprovedAsync(
                simRequest.Id,
                simRequest.RequestedBy,
                supervisorRemarks,
                simRequest.PublicId
            );

            // Log audit trail
            await _auditLogService.LogSimRequestApprovedAsync(
                simRequest.Id,
                "Supervisor",
                $"{currentUser.FirstName} {currentUser.LastName}",
                $"{simRequest.FirstName} {simRequest.LastName}",
                supervisorRemarks,
                currentUser.Id,
                HttpContext.Connection.RemoteIpAddress?.ToString()
            );

            // Send email notifications
            try
            {
                // 1. Send approval email to requester
                await SendApprovalEmailAsync(simRequest, currentUser, supervisorRemarks);

                // 2. Send notification to ICTS team
                await SendIctsNotificationEmailAsync(simRequest);

                _logger.LogInformation("Approval email notifications sent for SIM request {RequestId}", simRequest.Id);
            }
            catch (Exception emailEx)
            {
                // Log error but don't fail the approval
                _logger.LogError(emailEx, "Failed to send approval email notifications for request {RequestId}", simRequest.Id);
            }

            StatusMessage = "SIM request approved successfully and forwarded to UNON/ICTS.";
            StatusMessageClass = "success";
            return RedirectToPage("/Dashboard/Approver/Index");
        }

        private async Task<IActionResult> RejectSimRequestAsync(Guid requestId, ApplicationUser currentUser, string? notes)
        {
            var simRequest = await _context.SimRequests.FirstOrDefaultAsync(s => s.PublicId == requestId);
            if (simRequest == null)
            {
                StatusMessage = "SIM request not found.";
                StatusMessageClass = "danger";
                return RedirectToPage("/Dashboard/Approver/Index");
            }

            // Check if request is already processed to prevent duplicate rejection
            if (simRequest.Status != RequestStatus.PendingSupervisor)
            {
                StatusMessage = $"This request has already been processed. Current status: {simRequest.Status}";
                StatusMessageClass = "warning";
                return RedirectToPage("/Dashboard/Approver/Index");
            }

            simRequest.Status = RequestStatus.Rejected;
            simRequest.SupervisorApprovalDate = DateTime.UtcNow;
            simRequest.SupervisorNotes = notes;

            await _context.SaveChangesAsync();

            // Add history entry
            await _historyService.AddApprovalHistoryAsync(
                simRequest.Id,
                "supervisor",
                false,
                currentUser.Id,
                $"{currentUser.FirstName} {currentUser.LastName}",
                notes ?? "Request rejected by supervisor"
            );

            // Send notification to requester
            await _notificationService.NotifySimRequestSupervisorRejectedAsync(
                simRequest.Id,
                simRequest.RequestedBy,
                notes,
                simRequest.PublicId
            );

            // Log audit trail
            await _auditLogService.LogSimRequestRejectedAsync(
                simRequest.Id,
                "Supervisor",
                $"{currentUser.FirstName} {currentUser.LastName}",
                $"{simRequest.FirstName} {simRequest.LastName}",
                notes,
                currentUser.Id,
                HttpContext.Connection.RemoteIpAddress?.ToString()
            );

            // Send rejection email
            try
            {
                await SendRejectionEmailAsync(simRequest, currentUser, notes);
                _logger.LogInformation("Rejection email sent for SIM request {RequestId}", simRequest.Id);
            }
            catch (Exception emailEx)
            {
                // Log error but don't fail the rejection
                _logger.LogError(emailEx, "Failed to send rejection email for request {RequestId}", simRequest.Id);
            }

            StatusMessage = "SIM request rejected successfully.";
            StatusMessageClass = "success";
            return RedirectToPage("/Dashboard/Approver/Index");
        }

        private async Task<IActionResult> RevertSimRequestAsync(Guid requestId, ApplicationUser currentUser, string? notes)
        {
            var simRequest = await _context.SimRequests.FirstOrDefaultAsync(s => s.PublicId == requestId);
            if (simRequest == null)
            {
                StatusMessage = "SIM request not found.";
                StatusMessageClass = "danger";
                return RedirectToPage("/Dashboard/Approver/Index");
            }

            // Check if request is already processed to prevent duplicate reversion
            if (simRequest.Status != RequestStatus.PendingSupervisor)
            {
                StatusMessage = $"This request cannot be reverted. Current status: {simRequest.Status}";
                StatusMessageClass = "warning";
                return RedirectToPage("/Dashboard/Approver/Index");
            }

            simRequest.Status = RequestStatus.Draft;
            simRequest.SupervisorApprovalDate = null;
            simRequest.SupervisorNotes = null;

            await _context.SaveChangesAsync();

            // Add history entry
            await _historyService.AddReversionHistoryAsync(
                simRequest.Id,
                "supervisor",
                currentUser.Id,
                $"{currentUser.FirstName} {currentUser.LastName}",
                notes ?? "Request reverted to requestor by supervisor"
            );

            StatusMessage = "SIM request reverted to requestor successfully.";
            StatusMessageClass = "success";
            return RedirectToPage("/Dashboard/Approver/Index");
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

        // API endpoint to get Class of Service allowances
        public async Task<JsonResult> OnGetClassOfServiceAsync(string service)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(service))
                {
                    return new JsonResult(new { success = false, message = "Service name is required" });
                }

                var classOfService = await _context.ClassOfServices
                    .FirstOrDefaultAsync(c => c.Service == service && c.ServiceStatus == ServiceStatus.Active);

                if (classOfService == null)
                {
                    return new JsonResult(new { success = false, message = "Class of Service not found" });
                }

                return new JsonResult(new
                {
                    success = true,
                    mobileServiceAllowance = classOfService.AirtimeAllowanceAmount?.ToString("F2") ?? "0.00",
                    handsetAllowance = classOfService.HandsetAllowanceAmount?.ToString("F2") ?? "0.00",
                    dataAllowance = classOfService.DataAllowanceAmount?.ToString("F2") ?? "0.00"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching Class of Service for service: {Service}", service);
                return new JsonResult(new { success = false, message = "An error occurred while fetching allowances" });
            }
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

        private async Task SendApprovalEmailAsync(Models.SimRequest request, ApplicationUser approver, string? comments)
        {
            // Get request with ServiceProvider included
            var requestWithProvider = await _context.SimRequests
                .Include(r => r.ServiceProvider)
                .FirstOrDefaultAsync(r => r.Id == request.Id);

            if (requestWithProvider == null) return;

            var placeholders = new Dictionary<string, string>
            {
                { "RequestId", requestWithProvider.Id.ToString() },
                { "FirstName", requestWithProvider.FirstName ?? "" },
                { "LastName", requestWithProvider.LastName ?? "" },
                { "ApproverName", $"{approver.FirstName} {approver.LastName}" },
                { "ApproverRole", "Supervisor" },
                { "ApprovalDate", DateTime.UtcNow.ToString("MMMM dd, yyyy") },
                { "CurrentStatus", "Pending ICTS Processing" },
                { "SimType", requestWithProvider.SimType.ToString() },
                { "ServiceProvider", requestWithProvider.ServiceProvider?.ServiceProviderName ?? "N/A" },
                { "ApprovalCommentsSection", string.IsNullOrWhiteSpace(comments) ? "" :
                    $@"<div style=""background-color: #eff6ff; border-left: 4px solid #3b82f6; padding: 20px; margin-bottom: 30px; border-radius: 8px;"">
                        <p style=""margin: 0 0 8px 0; color: #1e40af; font-size: 14px; font-weight: 600;"">Approval Comments:</p>
                        <p style=""margin: 0; color: #1e40af; font-size: 14px; line-height: 1.5;"">{comments}</p>
                    </div>" },
                { "ViewRequestLink", $"{Request.Scheme}://{Request.Host}/Modules/SimManagement/Requests/Index" },
                { "Year", DateTime.Now.Year.ToString() }
            };

            await _emailService.SendTemplatedEmailAsync(
                to: requestWithProvider.OfficialEmail ?? "",
                templateCode: "SIM_REQUEST_APPROVED",
                data: placeholders
            );

            _logger.LogInformation("Sent approval email to {Email} for request {RequestId}",
                requestWithProvider.OfficialEmail, requestWithProvider.Id);
        }

        private async Task SendIctsNotificationEmailAsync(Models.SimRequest request)
        {
            // Get ICTS team email from configuration or use default
            var ictsEmail = _configuration["Email:IctsTeamEmail"] ?? "icts@example.com";

            // Get request with ServiceProvider included
            var requestWithProvider = await _context.SimRequests
                .Include(r => r.ServiceProvider)
                .FirstOrDefaultAsync(r => r.Id == request.Id);

            if (requestWithProvider == null) return;

            var placeholders = new Dictionary<string, string>
            {
                { "RequestId", requestWithProvider.Id.ToString() },
                { "RequestDate", requestWithProvider.RequestDate.ToString("MMMM dd, yyyy") },
                { "SupervisorApprovalDate", requestWithProvider.SupervisorApprovalDate?.ToString("MMMM dd, yyyy") ?? DateTime.UtcNow.ToString("MMMM dd, yyyy") },
                { "FirstName", requestWithProvider.FirstName ?? "" },
                { "LastName", requestWithProvider.LastName ?? "" },
                { "IndexNo", requestWithProvider.IndexNo ?? "" },
                { "Organization", requestWithProvider.Organization ?? "" },
                { "Office", requestWithProvider.Office ?? "" },
                { "Grade", requestWithProvider.Grade ?? "" },
                { "FunctionalTitle", requestWithProvider.FunctionalTitle ?? "" },
                { "OfficialEmail", requestWithProvider.OfficialEmail ?? "" },
                { "OfficeExtension", requestWithProvider.OfficeExtension ?? "N/A" },
                { "SimType", requestWithProvider.SimType.ToString() },
                { "ServiceProvider", requestWithProvider.ServiceProvider?.ServiceProviderName ?? "N/A" },
                { "SupervisorName", requestWithProvider.SupervisorName ?? "" },
                { "RemarksSection", string.IsNullOrWhiteSpace(requestWithProvider.Remarks) ? "" :
                    $@"<div style=""background-color: #eff6ff; border-left: 4px solid #3b82f6; padding: 20px; margin-bottom: 30px; border-radius: 8px;"">
                        <p style=""margin: 0 0 8px 0; color: #1e40af; font-size: 14px; font-weight: 600;"">Requester Remarks:</p>
                        <p style=""margin: 0; color: #1e40af; font-size: 14px; line-height: 1.5;"">{requestWithProvider.Remarks}</p>
                    </div>" },
                { "ProcessRequestLink", $"{Request.Scheme}://{Request.Host}/Modules/SimManagement/Approvals/ICTS" },
                { "Year", DateTime.Now.Year.ToString() }
            };

            await _emailService.SendTemplatedEmailAsync(
                to: ictsEmail,
                templateCode: "SIM_REQUEST_ICTS_NOTIFICATION",
                data: placeholders
            );

            _logger.LogInformation("Sent ICTS notification email for request {RequestId}", requestWithProvider.Id);
        }

        private async Task SendRejectionEmailAsync(Models.SimRequest request, ApplicationUser reviewer, string? reason)
        {
            // Get request with ServiceProvider included
            var requestWithProvider = await _context.SimRequests
                .Include(r => r.ServiceProvider)
                .FirstOrDefaultAsync(r => r.Id == request.Id);

            if (requestWithProvider == null) return;

            var placeholders = new Dictionary<string, string>
            {
                { "RequestId", requestWithProvider.Id.ToString() },
                { "FirstName", requestWithProvider.FirstName ?? "" },
                { "LastName", requestWithProvider.LastName ?? "" },
                { "SimType", requestWithProvider.SimType.ToString() },
                { "ServiceProvider", requestWithProvider.ServiceProvider?.ServiceProviderName ?? "N/A" },
                { "RequestDate", requestWithProvider.RequestDate.ToString("MMMM dd, yyyy") },
                { "RejectedBy", $"{reviewer.FirstName} {reviewer.LastName}" },
                { "RejectedByRole", "Supervisor" },
                { "RejectionDate", DateTime.UtcNow.ToString("MMMM dd, yyyy") },
                { "RejectionReason", reason ?? "No reason provided" },
                { "NewRequestLink", $"{Request.Scheme}://{Request.Host}/Modules/SimManagement/Requests/Create" },
                { "MyRequestsLink", $"{Request.Scheme}://{Request.Host}/Modules/SimManagement/Requests/Index" },
                { "Year", DateTime.Now.Year.ToString() }
            };

            await _emailService.SendTemplatedEmailAsync(
                to: requestWithProvider.OfficialEmail ?? "",
                templateCode: "SIM_REQUEST_REJECTED",
                data: placeholders
            );

            _logger.LogInformation("Sent rejection email to {Email} for request {RequestId}",
                requestWithProvider.OfficialEmail, requestWithProvider.Id);
        }
    }
} 
