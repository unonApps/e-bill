using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TAB.Web.Data;
using TAB.Web.Models;
using TAB.Web.Services;

namespace TAB.Web.Pages.Modules.SimManagement.Requests
{
    [Authorize]
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ISimRequestHistoryService _historyService;
        private readonly INotificationService _notificationService;
        private readonly IAuditLogService _auditLogService;
        private readonly IEnhancedEmailService _emailService;
        private readonly ILogger<EditModel> _logger;

        public EditModel(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ISimRequestHistoryService historyService,
            INotificationService notificationService,
            IAuditLogService auditLogService,
            IEnhancedEmailService emailService,
            ILogger<EditModel> logger)
        {
            _context = context;
            _userManager = userManager;
            _historyService = historyService;
            _notificationService = notificationService;
            _auditLogService = auditLogService;
            _emailService = emailService;
            _logger = logger;
        }

        [BindProperty]
        public Models.SimRequest? SimRequest { get; set; }

        public List<SelectListItem> ServiceProviders { get; set; } = new();
        public List<SelectListItem> Organizations { get; set; } = new();
        public List<SelectListItem> Offices { get; set; } = new();
        public List<SelectListItem> Supervisors { get; set; } = new();

        public string? StatusMessage { get; set; }
        public string? StatusMessageClass { get; set; }

        public async Task<IActionResult> OnGetAsync(Guid id)
        {
            await LoadDropdownDataAsync();

            SimRequest = await _context.SimRequests
                .Include(r => r.ServiceProvider)
                .FirstOrDefaultAsync(r => r.PublicId == id);

            if (SimRequest == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (SimRequest.RequestedBy != currentUser?.Id)
            {
                return Forbid();
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string action)
        {
            await LoadDropdownDataAsync();

            if (SimRequest == null)
            {
                return NotFound();
            }

            var existingRequest = await _context.SimRequests
                .FirstOrDefaultAsync(r => r.Id == SimRequest.Id);

            if (existingRequest == null)
            {
                StatusMessage = "Request not found.";
                StatusMessageClass = "danger";
                return Page();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (existingRequest.RequestedBy != currentUser?.Id)
            {
                StatusMessage = "You can only edit your own requests.";
                StatusMessageClass = "danger";
                return Page();
            }

            if (existingRequest.Status != RequestStatus.Draft)
            {
                StatusMessage = "Only draft requests can be edited.";
                StatusMessageClass = "warning";
                return Page();
            }

            // Skip validation for drafts - allow saving incomplete forms
            bool isDraft = action == "draft";

            if (!isDraft && !ModelState.IsValid)
            {
                StatusMessage = "Please correct the errors below.";
                StatusMessageClass = "danger";
                SimRequest = existingRequest;
                return Page();
            }

            // Validate required fields only when submitting (not for drafts)
            if (!isDraft)
            {
                bool isExistingLine = SimRequest.LineRequestType == LineRequestType.ExistingLine;

                if (string.IsNullOrWhiteSpace(SimRequest.IndexNo) ||
                    string.IsNullOrWhiteSpace(SimRequest.FirstName) ||
                    string.IsNullOrWhiteSpace(SimRequest.LastName) ||
                    string.IsNullOrWhiteSpace(SimRequest.Organization) ||
                    string.IsNullOrWhiteSpace(SimRequest.Office) ||
                    string.IsNullOrWhiteSpace(SimRequest.Grade) ||
                    string.IsNullOrWhiteSpace(SimRequest.FunctionalTitle) ||
                    string.IsNullOrWhiteSpace(SimRequest.OfficialEmail) ||
                    string.IsNullOrWhiteSpace(SimRequest.Supervisor))
                {
                    StatusMessage = "Please fill in all required fields marked with *.";
                    StatusMessageClass = "danger";
                    SimRequest = existingRequest;
                    return Page();
                }

                // Validate based on Line Request Type
                if (isExistingLine)
                {
                    if (string.IsNullOrWhiteSpace(SimRequest.ExistingPhoneNumber))
                    {
                        StatusMessage = "Please enter the existing phone number.";
                        StatusMessageClass = "danger";
                        SimRequest = existingRequest;
                        return Page();
                    }
                }
                else
                {
                    if (!SimRequest.ServiceProviderId.HasValue || SimRequest.ServiceProviderId.Value == 0)
                    {
                        StatusMessage = "Please select a service provider.";
                        StatusMessageClass = "danger";
                        SimRequest = existingRequest;
                        return Page();
                    }
                }
            }

            // For drafts, ensure ServiceProviderId is null if not selected (to avoid FK constraint)
            if (isDraft && (!SimRequest.ServiceProviderId.HasValue || SimRequest.ServiceProviderId.Value == 0))
            {
                SimRequest.ServiceProviderId = null;
            }

            // Note: Multiple SIM requests are allowed for the same Index Number
            // as one staff member can have multiple SIM cards (e.g., personal, official)

            // Update request properties - provide default empty values for drafts to avoid NULL constraints
            existingRequest.IndexNo = string.IsNullOrWhiteSpace(SimRequest.IndexNo) ? (isDraft ? "" : SimRequest.IndexNo) : SimRequest.IndexNo.Trim();
            existingRequest.FirstName = string.IsNullOrWhiteSpace(SimRequest.FirstName) ? (isDraft ? "" : SimRequest.FirstName) : SimRequest.FirstName.Trim();
            existingRequest.LastName = string.IsNullOrWhiteSpace(SimRequest.LastName) ? (isDraft ? "" : SimRequest.LastName) : SimRequest.LastName.Trim();
            existingRequest.Organization = string.IsNullOrWhiteSpace(SimRequest.Organization) ? (isDraft ? "" : SimRequest.Organization) : SimRequest.Organization.Trim();
            existingRequest.Office = string.IsNullOrWhiteSpace(SimRequest.Office) ? (isDraft ? "" : SimRequest.Office) : SimRequest.Office.Trim();
            existingRequest.Grade = string.IsNullOrWhiteSpace(SimRequest.Grade) ? (isDraft ? "" : SimRequest.Grade) : SimRequest.Grade.Trim();
            existingRequest.FunctionalTitle = string.IsNullOrWhiteSpace(SimRequest.FunctionalTitle) ? (isDraft ? "" : SimRequest.FunctionalTitle) : SimRequest.FunctionalTitle.Trim();
            existingRequest.OfficeExtension = SimRequest.OfficeExtension?.Trim();
            existingRequest.OfficialEmail = string.IsNullOrWhiteSpace(SimRequest.OfficialEmail) ? (isDraft ? "" : SimRequest.OfficialEmail) : SimRequest.OfficialEmail.Trim();
            existingRequest.SimType = SimRequest.SimType;
            existingRequest.LineRequestType = SimRequest.LineRequestType;
            existingRequest.ExistingPhoneNumber = SimRequest.ExistingPhoneNumber?.Trim();
            existingRequest.ServiceProviderId = SimRequest.ServiceProviderId;
            existingRequest.Supervisor = string.IsNullOrWhiteSpace(SimRequest.Supervisor) ? (isDraft ? "" : SimRequest.Supervisor) : SimRequest.Supervisor.Trim();
            existingRequest.PreviouslyAssignedLines = SimRequest.PreviouslyAssignedLines?.Trim();
            existingRequest.Remarks = SimRequest.Remarks?.Trim();

            // Get supervisor details from the selected supervisor email
            var supervisorUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == SimRequest.Supervisor);

            // Set supervisor details if found
            if (supervisorUser != null)
            {
                existingRequest.SupervisorEmail = supervisorUser.Email ?? string.Empty;
                existingRequest.SupervisorName = $"{supervisorUser.FirstName ?? ""} {supervisorUser.LastName ?? ""}".Trim();
                // Keep the email in the Supervisor field for backward compatibility
                existingRequest.Supervisor = supervisorUser.Email ?? string.Empty;
            }

            // Update status if submitting
            if (action == "submit")
            {
                existingRequest.Status = RequestStatus.PendingSupervisor;
                existingRequest.SubmittedToSupervisor = true;
            }

            try
            {
                await _context.SaveChangesAsync();

                // Add re-submission workflow when submitting (same as Create page)
                if (action == "submit")
                {
                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    var userName = $"{currentUser.FirstName ?? ""} {currentUser.LastName ?? ""}".Trim();

                    // Track re-submission to supervisor as a workflow change
                    await _historyService.AddSubmissionHistoryAsync(
                        existingRequest.Id,
                        currentUser.Id,
                        userName,
                        ipAddress ?? string.Empty
                    );

                    // Send notifications and emails if supervisor found
                    if (supervisorUser != null)
                    {
                        await _notificationService.NotifySimRequestSubmittedAsync(
                            existingRequest.Id,
                            currentUser.Id,
                            supervisorUser.Id,
                            existingRequest.PublicId
                        );

                        await _auditLogService.LogSimRequestSubmittedAsync(
                            existingRequest.Id,
                            $"{existingRequest.FirstName} {existingRequest.LastName}",
                            existingRequest.IndexNo ?? "",
                            existingRequest.ServiceProvider?.ServiceProviderName ?? "N/A",
                            currentUser.Id,
                            ipAddress
                        );

                        try
                        {
                            await SendResubmissionEmailsAsync(existingRequest, currentUser, supervisorUser);
                        }
                        catch (Exception emailEx)
                        {
                            _logger.LogError(emailEx, "Failed to send email notifications for re-submitted SIM request {RequestId}", existingRequest.Id);
                        }
                    }
                }

                StatusMessage = action == "submit"
                    ? "Your SIM request has been updated and submitted successfully."
                    : "Your SIM request has been updated successfully.";
                StatusMessageClass = "success";

                return RedirectToPage("./Index");
            }
            catch (Exception)
            {
                StatusMessage = "An error occurred while updating your request. Please try again.";
                StatusMessageClass = "danger";
                SimRequest = existingRequest;
                return Page();
            }
        }

        private async Task LoadDropdownDataAsync()
        {
            // Load Service Providers
            ServiceProviders = await _context.ServiceProviders
                .Where(sp => sp.SPStatus == ServiceProviderStatus.Active)
                .Select(sp => new SelectListItem
                {
                    Value = sp.Id.ToString(),
                    Text = sp.ServiceProviderName ?? "Unknown Provider"
                })
                .ToListAsync();

            // Load Organizations with abbreviations
            Organizations = await _context.Organizations
                .Select(o => new SelectListItem
                {
                    Value = o.Name,
                    Text = !string.IsNullOrEmpty(o.Code) ? $"{o.Code} - {o.Name}" : o.Name ?? "Unknown Organization"
                })
                .ToListAsync();

            // Load Offices with Organization relationship
            Offices = await _context.Offices
                .Include(o => o.Organization)
                .Select(o => new SelectListItem
                {
                    Value = o.Name,
                    Text = o.Name ?? "Unknown Office",
                    Group = new SelectListGroup { Name = o.Organization.Name ?? "Unknown" }
                })
                .ToListAsync();

            // Note: Supervisors are loaded via Azure AD live search (same as Create page)
            Supervisors = new List<SelectListItem>();
        }

        private async Task SendResubmissionEmailsAsync(Models.SimRequest request, ApplicationUser requester, ApplicationUser supervisor)
        {
            // Send confirmation to requester
            var requesterPlaceholders = new Dictionary<string, string>
            {
                { "RequestId", request.Id.ToString() },
                { "RequestDate", request.RequestDate.ToString("MMMM dd, yyyy") },
                { "FirstName", request.FirstName ?? "" },
                { "LastName", request.LastName ?? "" },
                { "SimType", request.SimType.ToString() },
                { "ServiceProvider", request.ServiceProvider?.ServiceProviderName ?? "N/A" },
                { "IndexNo", request.IndexNo ?? "" },
                { "Organization", request.Organization ?? "" },
                { "Office", request.Office ?? "" },
                { "SupervisorName", request.SupervisorName ?? "" },
                { "ViewRequestLink", $"{Request.Scheme}://{Request.Host}/Modules/SimManagement/Requests/Index" },
                { "Year", DateTime.Now.Year.ToString() }
            };

            await _emailService.SendTemplatedEmailAsync(
                to: request.OfficialEmail ?? requester.Email ?? "",
                templateCode: "SIM_REQUEST_SUBMITTED",
                data: requesterPlaceholders
            );

            // Send notification to supervisor
            var supervisorPlaceholders = new Dictionary<string, string>
            {
                { "RequestId", request.Id.ToString() },
                { "RequestDate", request.RequestDate.ToString("MMMM dd, yyyy") },
                { "SimType", request.SimType.ToString() },
                { "ServiceProvider", request.ServiceProvider?.ServiceProviderName ?? "N/A" },
                { "FirstName", request.FirstName ?? "" },
                { "LastName", request.LastName ?? "" },
                { "IndexNo", request.IndexNo ?? "" },
                { "Organization", request.Organization ?? "" },
                { "Office", request.Office ?? "" },
                { "Grade", request.Grade ?? "" },
                { "FunctionalTitle", request.FunctionalTitle ?? "" },
                { "OfficialEmail", request.OfficialEmail ?? "" },
                { "OfficeExtension", request.OfficeExtension ?? "N/A" },
                { "SupervisorName", request.SupervisorName ?? "" },
                { "JustificationSection", string.IsNullOrWhiteSpace(request.Remarks) ? "" :
                    $@"<div style=""background-color: #eff6ff; border-left: 4px solid #3b82f6; padding: 20px; margin-bottom: 30px; border-radius: 8px;"">
                        <p style=""margin: 0 0 8px 0; color: #1e40af; font-size: 14px; font-weight: 600;"">Requester Justification:</p>
                        <p style=""margin: 0; color: #1e40af; font-size: 14px; line-height: 1.5;"">{request.Remarks}</p>
                    </div>" },
                { "ReviewRequestLink", $"{Request.Scheme}://{Request.Host}/Modules/SimManagement/Approvals/Supervisor" },
                { "Year", DateTime.Now.Year.ToString() }
            };

            await _emailService.SendTemplatedEmailAsync(
                to: supervisor.Email ?? request.SupervisorEmail ?? "",
                templateCode: "SIM_REQUEST_SUPERVISOR_NOTIFICATION",
                data: supervisorPlaceholders
            );

            _logger.LogInformation("Sent re-submission email notifications for SIM request {RequestId}", request.Id);
        }
    }
} 