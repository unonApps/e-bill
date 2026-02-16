using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using TAB.Web.Data;
using TAB.Web.Models;
using TAB.Web.Services;

namespace TAB.Web.Pages.Modules.SimManagement.Requests
{
    [Authorize]
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ISimRequestHistoryService _historyService;
        private readonly INotificationService _notificationService;
        private readonly IAuditLogService _auditLogService;
        private readonly IEnhancedEmailService _emailService;
        private readonly ILogger<CreateModel> _logger;

        public CreateModel(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ISimRequestHistoryService historyService,
            INotificationService notificationService,
            IAuditLogService auditLogService,
            IEnhancedEmailService emailService,
            ILogger<CreateModel> logger)
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
        public Models.SimRequest SimRequest { get; set; } = new();

        public List<SelectListItem> ServiceProviders { get; set; } = new();
        public List<SelectListItem> Organizations { get; set; } = new();
        public List<SelectListItem> Offices { get; set; } = new();
        public List<SelectListItem> Supervisors { get; set; } = new();

        public string? StatusMessage { get; set; }
        public string? StatusMessageClass { get; set; }
        public int? PreSelectedOrganizationId { get; set; }
        public List<UserPhone> ExistingPhones { get; set; } = new();
        public bool HasExistingLines { get; set; } = false;

        public async Task OnGetAsync()
        {
            await LoadDropdownDataAsync();
            await PopulateUserInfoAsync();
        }

        public async Task<IActionResult> OnPostAsync(string action)
        {
            await LoadDropdownDataAsync();

            // Skip validation for drafts - allow saving incomplete forms
            bool isDraft = action == "draft";

            if (!isDraft && !ModelState.IsValid)
            {
                // Build detailed error message
                var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => new {
                        Field = x.Key.Replace("SimRequest.", ""),
                        Errors = x.Value.Errors.Select(e => e.ErrorMessage)
                    })
                    .ToList();

                if (errors.Any())
                {
                    var errorMessages = string.Join("; ",
                        errors.SelectMany(e => e.Errors.Select(err => $"{e.Field}: {err}")));
                    StatusMessage = $"Please correct the following errors: {errorMessages}";
                }
                else
                {
                    StatusMessage = "Please correct the errors below.";
                }

                StatusMessageClass = "danger";
                return Page();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                StatusMessage = "User not found.";
                StatusMessageClass = "danger";
                return Page();
            }

            // Note: Multiple SIM requests are allowed for the same Index Number
            // as one staff member can have multiple SIM cards (e.g., personal, official)

            // Get supervisor details from the selected supervisor email
            var supervisorUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == SimRequest.Supervisor);

            // Set request properties
            SimRequest.RequestedBy = currentUser.Id;
            SimRequest.RequestDate = DateTime.UtcNow;
            SimRequest.Status = action == "submit" ? RequestStatus.PendingSupervisor : RequestStatus.Draft;
            SimRequest.SubmittedToSupervisor = action == "submit";

            // Set supervisor details if found
            if (supervisorUser != null)
            {
                SimRequest.SupervisorEmail = supervisorUser.Email ?? string.Empty;
                SimRequest.SupervisorName = $"{supervisorUser.FirstName ?? ""} {supervisorUser.LastName ?? ""}".Trim();
                // Keep the email in the Supervisor field for backward compatibility
                SimRequest.Supervisor = supervisorUser.Email ?? string.Empty;
            }

            // Validate required fields only when submitting (not for drafts)
            if (!isDraft)
            {
                // Check if LineRequestType is for Existing Line
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
                    await LoadDropdownDataAsync();
                    return Page();
                }

                // Validate based on Line Request Type
                if (isExistingLine)
                {
                    // For existing lines, ExistingPhoneNumber is required
                    if (string.IsNullOrWhiteSpace(SimRequest.ExistingPhoneNumber))
                    {
                        StatusMessage = "Please enter the existing phone number.";
                        StatusMessageClass = "danger";
                        await LoadDropdownDataAsync();
                        return Page();
                    }
                }
                else
                {
                    // For new lines, ServiceProviderId is required
                    if (!SimRequest.ServiceProviderId.HasValue || SimRequest.ServiceProviderId.Value == 0)
                    {
                        StatusMessage = "Please select a service provider.";
                        StatusMessageClass = "danger";
                        await LoadDropdownDataAsync();
                        return Page();
                    }
                }
            }

            // For drafts, ensure ServiceProviderId is null if not selected (to avoid FK constraint)
            if (isDraft && (!SimRequest.ServiceProviderId.HasValue || SimRequest.ServiceProviderId.Value == 0))
            {
                SimRequest.ServiceProviderId = null;
            }

            // Trim string properties and ensure required fields have default values for database NOT NULL constraints
            SimRequest.IndexNo = string.IsNullOrWhiteSpace(SimRequest.IndexNo) ? (isDraft ? "" : SimRequest.IndexNo) : SimRequest.IndexNo.Trim();
            SimRequest.FirstName = string.IsNullOrWhiteSpace(SimRequest.FirstName) ? (isDraft ? "" : SimRequest.FirstName) : SimRequest.FirstName.Trim();
            SimRequest.LastName = string.IsNullOrWhiteSpace(SimRequest.LastName) ? (isDraft ? "" : SimRequest.LastName) : SimRequest.LastName.Trim();
            SimRequest.Organization = string.IsNullOrWhiteSpace(SimRequest.Organization) ? (isDraft ? "" : SimRequest.Organization) : SimRequest.Organization.Trim();
            SimRequest.Office = string.IsNullOrWhiteSpace(SimRequest.Office) ? (isDraft ? "" : SimRequest.Office) : SimRequest.Office.Trim();
            SimRequest.Grade = string.IsNullOrWhiteSpace(SimRequest.Grade) ? (isDraft ? "" : SimRequest.Grade) : SimRequest.Grade.Trim();
            SimRequest.FunctionalTitle = string.IsNullOrWhiteSpace(SimRequest.FunctionalTitle) ? (isDraft ? "" : SimRequest.FunctionalTitle) : SimRequest.FunctionalTitle.Trim();
            SimRequest.OfficeExtension = SimRequest.OfficeExtension?.Trim();
            SimRequest.OfficialEmail = string.IsNullOrWhiteSpace(SimRequest.OfficialEmail) ? (isDraft ? "" : SimRequest.OfficialEmail) : SimRequest.OfficialEmail.Trim();
            SimRequest.Supervisor = string.IsNullOrWhiteSpace(SimRequest.Supervisor) ? (isDraft ? "" : SimRequest.Supervisor) : SimRequest.Supervisor.Trim();
            SimRequest.PreviouslyAssignedLines = SimRequest.PreviouslyAssignedLines?.Trim();
            SimRequest.Remarks = SimRequest.Remarks?.Trim();

            try
            {
                _context.SimRequests.Add(SimRequest);
                await _context.SaveChangesAsync();

                // Add history entry only for workflow changes
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                var userName = $"{currentUser.FirstName ?? ""} {currentUser.LastName ?? ""}".Trim();
                
                if (action == "submit")
                {
                    // Track submission to supervisor as a workflow change
                    await _historyService.AddSubmissionHistoryAsync(
                        SimRequest.Id,
                        currentUser.Id,
                        userName,
                        ipAddress ?? string.Empty
                    );

                    // Send notifications to requester and supervisor
                    if (supervisorUser != null)
                    {
                        await _notificationService.NotifySimRequestSubmittedAsync(
                            SimRequest.Id,
                            currentUser.Id,
                            supervisorUser.Id
                        );

                        // Log audit trail
                        await _auditLogService.LogSimRequestSubmittedAsync(
                            SimRequest.Id,
                            $"{SimRequest.FirstName} {SimRequest.LastName}",
                            SimRequest.IndexNo ?? "",
                            SimRequest.ServiceProvider?.ServiceProviderName ?? "N/A",
                            currentUser.Id,
                            ipAddress
                        );

                        // Send email notifications using templates
                        try
                        {
                            // 1. Send confirmation email to requester
                            await SendSubmittedConfirmationEmailAsync(SimRequest, currentUser);

                            // 2. Send notification to supervisor
                            await SendSupervisorNotificationEmailAsync(SimRequest, supervisorUser);

                            _logger.LogInformation("Email notifications sent successfully for SIM request {RequestId}", SimRequest.Id);
                        }
                        catch (Exception emailEx)
                        {
                            // Log error but don't fail the request
                            _logger.LogError(emailEx, "Failed to send email notifications for SIM request {RequestId}", SimRequest.Id);
                        }
                    }
                }
                // Note: Draft saves are not tracked as they're not workflow changes

                StatusMessage = action == "submit" 
                    ? "Your SIM request has been submitted successfully and sent to your supervisor for approval."
                    : "Your SIM request has been saved as draft successfully.";
                StatusMessageClass = "success";

                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                // Log the actual error for debugging
                var errorDetails = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                
                // Check for specific database errors
                if (ex.InnerException != null && ex.InnerException.Message.Contains("FOREIGN KEY"))
                {
                    StatusMessage = "Invalid selection: Please ensure all dropdown selections are valid.";
                }
                else if (ex.InnerException != null && ex.InnerException.Message.Contains("NULL"))
                {
                    StatusMessage = "Required field missing: Please fill in all required fields marked with *.";
                }
                else
                {
                    // For development/debugging - show the actual error
                    StatusMessage = $"An error occurred while saving your request: {errorDetails}";
                }
                
                StatusMessageClass = "danger";
                
                // Reload dropdown data before returning the page
                await LoadDropdownDataAsync();
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

            // Note: Supervisors are now loaded via Azure AD live search
            // This list is kept for backward compatibility but can be empty
            Supervisors = new List<SelectListItem>();
        }

        private async Task PopulateUserInfoAsync()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return;

            // Get user from ApplicationUser
            var user = await _context.Users
                .Include(u => u.Organization)
                .Include(u => u.Office)
                .FirstOrDefaultAsync(u => u.Id == currentUser.Id);

            if (user != null)
            {
                // Auto-populate name
                SimRequest.FirstName = user.FirstName;
                SimRequest.LastName = user.LastName;

                // Auto-populate organization
                if (user.Organization != null)
                {
                    SimRequest.Organization = user.Organization.Name ?? string.Empty;
                    PreSelectedOrganizationId = user.OrganizationId;
                }

                // Auto-populate office
                if (user.Office != null)
                {
                    SimRequest.Office = user.Office.Name ?? string.Empty;
                }

                // Auto-populate official email
                SimRequest.OfficialEmail = user.Email ?? string.Empty;
            }

            // Get additional info from EbillUser
            var ebillUser = await _context.EbillUsers
                .FirstOrDefaultAsync(u => u.Email == currentUser.Email);

            if (ebillUser != null)
            {
                // Auto-populate index number
                if (!string.IsNullOrEmpty(ebillUser.IndexNumber))
                {
                    SimRequest.IndexNo = ebillUser.IndexNumber;

                    // Query existing phone numbers for this user
                    ExistingPhones = await _context.UserPhones
                        .Include(p => p.ClassOfService)
                        .Where(p => p.IndexNumber == ebillUser.IndexNumber && p.IsActive)
                        .OrderByDescending(p => p.IsPrimary)
                        .ThenBy(p => p.AssignedDate)
                        .ToListAsync();

                    // Auto-compute Previously Assigned Lines
                    HasExistingLines = ExistingPhones.Any();
                    SimRequest.PreviouslyAssignedLines = HasExistingLines ? "Yes" : "No";
                }

                // Auto-populate supervisor details
                if (!string.IsNullOrEmpty(ebillUser.SupervisorEmail))
                {
                    SimRequest.Supervisor = ebillUser.SupervisorEmail;
                }

                if (!string.IsNullOrEmpty(ebillUser.SupervisorName))
                {
                    SimRequest.SupervisorName = ebillUser.SupervisorName;
                }
            }
        }

        private async Task SendSubmittedConfirmationEmailAsync(Models.SimRequest request, ApplicationUser requester)
        {
            var placeholders = new Dictionary<string, string>
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
                data: placeholders
            );

            _logger.LogInformation("Sent submission confirmation email to {Email} for request {RequestId}",
                request.OfficialEmail, request.Id);
        }

        private async Task SendSupervisorNotificationEmailAsync(Models.SimRequest request, ApplicationUser supervisor)
        {
            var placeholders = new Dictionary<string, string>
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
                data: placeholders
            );

            _logger.LogInformation("Sent supervisor notification email to {Email} for request {RequestId}",
                supervisor.Email, request.Id);
        }
    }
} 