using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TAB.Web.Data;
using TAB.Web.Models;
using TAB.Web.Services;

namespace TAB.Web.Pages.Modules.RefundManagement.Requests
{
    [Authorize]
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<CreateModel> _logger;
        private readonly INotificationService _notificationService;
        private readonly IAuditLogService _auditLogService;
        private readonly IEnhancedEmailService _emailService;

        public CreateModel(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment environment,
            ILogger<CreateModel> logger,
            INotificationService notificationService,
            IAuditLogService auditLogService,
            IEnhancedEmailService emailService)
        {
            _context = context;
            _userManager = userManager;
            _environment = environment;
            _logger = logger;
            _notificationService = notificationService;
            _auditLogService = auditLogService;
            _emailService = emailService;
        }

        [BindProperty]
        public RefundRequest RefundRequest { get; set; } = new RefundRequest();

        public List<SelectListItem> Supervisors { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> ClassOfServices { get; set; } = new List<SelectListItem>();

        // Add these properties for the form dropdowns
        public List<Organization> Organizations { get; set; } = new List<Organization>();
        public List<ClassOfService> ClassesOfService { get; set; } = new List<ClassOfService>();

        // Pre-populated organization ID for office loading
        public int? PreSelectedOrganizationId { get; set; }

        [TempData]
        public string? StatusMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            await LoadSupervisorsAsync();
            await LoadClassOfServicesAsync();
            // await LoadOfficesAsync(); // Removed - Office table no longer exists
            await LoadOrganizationsAsync();
            await LoadClassesOfServiceAsync();
            await PopulateUserInfoAsync();
            return Page();
        }

        private async Task PopulateUserInfoAsync()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return;
            }

            // Try to get user information from ApplicationUser
            var user = await _context.Users
                .Include(u => u.Organization)
                .Include(u => u.Office)
                .FirstOrDefaultAsync(u => u.Id == currentUser.Id);

            if (user != null)
            {
                // Auto-populate name
                RefundRequest.MobileNumberAssignedTo = $"{user.FirstName} {user.LastName}".Trim();

                // Auto-populate organization
                if (user.Organization != null)
                {
                    RefundRequest.Organization = user.Organization.Name ?? string.Empty;
                    PreSelectedOrganizationId = user.OrganizationId; // Store ID for JavaScript
                }

                // Auto-populate office
                if (user.Office != null)
                {
                    RefundRequest.Office = user.Office.Name ?? string.Empty;
                }
            }

            // Try to get additional information from EbillUser
            var ebillUser = await _context.EbillUsers
                .FirstOrDefaultAsync(u => u.Email == currentUser.Email);

            if (ebillUser != null)
            {
                // Auto-populate index number
                if (!string.IsNullOrEmpty(ebillUser.IndexNumber))
                {
                    RefundRequest.IndexNo = ebillUser.IndexNumber;
                }

                // Auto-populate primary mobile number from EbillUser's OfficialMobileNumber
                if (!string.IsNullOrEmpty(ebillUser.OfficialMobileNumber))
                {
                    RefundRequest.PrimaryMobileNumber = ebillUser.OfficialMobileNumber;
                }

                // Auto-populate supervisor details
                if (!string.IsNullOrEmpty(ebillUser.SupervisorEmail))
                {
                    RefundRequest.Supervisor = ebillUser.SupervisorEmail;
                }

                if (!string.IsNullOrEmpty(ebillUser.SupervisorName))
                {
                    RefundRequest.SupervisorName = ebillUser.SupervisorName;
                }
            }
        }

        public async Task<IActionResult> OnPostAsync(string action, IFormFile? receiptFile)
        {
            _logger.LogInformation("=== FORM SUBMISSION DEBUG ===");
            _logger.LogInformation("Action: {Action}", action);
            _logger.LogInformation("Receipt file: {FileName}", receiptFile?.FileName ?? "No file");
            _logger.LogInformation("RefundRequest bound: {IsBound}", RefundRequest != null);
            
            if (RefundRequest != null)
            {
                _logger.LogInformation("Key fields received:");
                _logger.LogInformation("  MobileNumberAssignedTo: '{Value}'", RefundRequest.MobileNumberAssignedTo ?? "null");
                _logger.LogInformation("  PrimaryMobileNumber: '{Value}'", RefundRequest.PrimaryMobileNumber ?? "null");
                _logger.LogInformation("  DevicePurchaseAmount: {Value}", RefundRequest.DevicePurchaseAmount);
                _logger.LogInformation("  Supervisor: '{Value}'", RefundRequest.Supervisor ?? "null");
            }
            // Handle file upload - Store in database
            if (receiptFile != null && receiptFile.Length > 0)
            {
                if (receiptFile.ContentType != "application/pdf")
                {
                    ModelState.AddModelError("ReceiptFile", "Only PDF files are allowed.");
                }
                else if (receiptFile.Length > 10 * 1024 * 1024) // 10MB limit
                {
                    ModelState.AddModelError("ReceiptFile", "File size cannot exceed 10MB.");
                }
                else
                {
                    // Save the file to database as binary data
                    using (var memoryStream = new MemoryStream())
                    {
                        await receiptFile.CopyToAsync(memoryStream);
                        RefundRequest.PurchaseReceiptData = memoryStream.ToArray();
                    }

                    RefundRequest.PurchaseReceiptFileName = receiptFile.FileName;
                    RefundRequest.PurchaseReceiptContentType = receiptFile.ContentType;
                    RefundRequest.PurchaseReceiptUploadDate = DateTime.UtcNow;

                    // Set a marker in PurchaseReceiptPath to indicate file is stored in database
                    RefundRequest.PurchaseReceiptPath = $"database:{receiptFile.FileName}";

                    _logger.LogInformation("Receipt file '{FileName}' ({Size} bytes) saved to database for refund request",
                        receiptFile.FileName, RefundRequest.PurchaseReceiptData.Length);
                }
            }
            else if (action == "submit")
            {
                // For submit, receipt is required but we'll handle it as a warning, not a hard error
                if (string.IsNullOrEmpty(RefundRequest.PurchaseReceiptPath))
                {
                    RefundRequest.PurchaseReceiptPath = ""; // Set to empty string to pass validation
                    ModelState.AddModelError("ReceiptFile", "Purchase receipt is recommended but not required.");
                }
            }
            else if (action == "save")
            {
                // For drafts, remove the PurchaseReceiptPath validation
                ModelState.Remove("RefundRequest.PurchaseReceiptPath");
                RefundRequest.PurchaseReceiptPath = ""; // Set empty for drafts
            }

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("ModelState is invalid for RefundRequest submission");
                foreach (var modelState in ModelState)
                {
                    if (modelState.Value?.Errors.Count > 0)
                    {
                        foreach (var error in modelState.Value.Errors)
                        {
                            _logger.LogWarning("Validation error for field {Field}: {Error}", 
                                modelState.Key, error.ErrorMessage);
                        }
                    }
                }
                await LoadSupervisorsAsync();
                await LoadClassOfServicesAsync();
                // await LoadOfficesAsync(); // Removed - Office table no longer exists
                await LoadOrganizationsAsync();
                await LoadClassesOfServiceAsync();
                return Page();
            }

            // Set system fields
            var currentUser = await _userManager.GetUserAsync(User);
            RefundRequest.RequestedBy = currentUser?.Id;
            RefundRequest.RequestDate = DateTime.UtcNow;

            // Set supervisor email (now directly from email input field)
            if (!string.IsNullOrEmpty(RefundRequest.Supervisor))
            {
                RefundRequest.SupervisorEmail = RefundRequest.Supervisor;

                // If supervisor name is not provided, try to get it from the database
                if (string.IsNullOrEmpty(RefundRequest.SupervisorName))
                {
                    var supervisorUser = await _context.Users
                        .FirstOrDefaultAsync(u => u.Email == RefundRequest.SupervisorEmail);
                    if (supervisorUser != null)
                    {
                        RefundRequest.SupervisorName = $"{supervisorUser.FirstName} {supervisorUser.LastName}";
                    }
                }
            }

            if (action == "submit")
            {
                RefundRequest.Status = RefundRequestStatus.PendingSupervisor;
                RefundRequest.SubmittedToSupervisor = true;
                StatusMessage = "Your mobile device reimbursement request has been submitted to your supervisor for approval.";
            }
            else
            {
                RefundRequest.Status = RefundRequestStatus.Draft;
                RefundRequest.SubmittedToSupervisor = false;
                StatusMessage = "Your mobile device reimbursement request has been saved as a draft.";
            }

            try
            {
                _context.RefundRequests.Add(RefundRequest);
                await _context.SaveChangesAsync();

                // Send notification if submitted to supervisor
                if (action == "submit")
                {
                    var supervisorUser = await _context.Users
                        .FirstOrDefaultAsync(u => u.Email == RefundRequest.SupervisorEmail);

                    if (supervisorUser != null)
                    {
                        await _notificationService.NotifyRefundRequestSubmittedAsync(
                            RefundRequest.Id,
                            RefundRequest.RequestedBy ?? currentUser.Id,
                            supervisorUser.Id
                        );

                        // Log audit trail - get index number from EbillUser
                        var ebillUser = await _context.EbillUsers
                            .FirstOrDefaultAsync(u => u.Email == currentUser.Email);

                        await _auditLogService.LogRefundRequestSubmittedAsync(
                            RefundRequest.Id,
                            RefundRequest.MobileNumberAssignedTo ?? "N/A",
                            ebillUser?.IndexNumber ?? RefundRequest.IndexNo ?? "N/A",
                            RefundRequest.DevicePurchaseAmount,
                            RefundRequest.PrimaryMobileNumber ?? "N/A",
                            currentUser.Id,
                            HttpContext.Connection.RemoteIpAddress?.ToString()
                        );

                        // Send email notifications
                        try
                        {
                            // 1. Send confirmation email to requester
                            await SendSubmittedConfirmationEmailAsync(RefundRequest, currentUser);

                            // 2. Send notification to supervisor
                            await SendSupervisorNotificationEmailAsync(RefundRequest, supervisorUser);

                            _logger.LogInformation("Email notifications sent successfully for refund request {RequestId}", RefundRequest.Id);
                        }
                        catch (Exception emailEx)
                        {
                            // Log error but don't fail the request
                            _logger.LogError(emailEx, "Failed to send email notifications for refund request {RequestId}", RefundRequest.Id);
                        }
                    }
                }

                return RedirectToPage("./Index", new { message = StatusMessage });
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "An error occurred while saving your request. Please try again.");
                await LoadSupervisorsAsync();
                await LoadClassOfServicesAsync();
                // await LoadOfficesAsync(); // Removed - Office table no longer exists
                await LoadOrganizationsAsync();
                await LoadClassesOfServiceAsync();
                return Page();
            }
        }

        public async Task<IActionResult> OnGetClassOfServiceAllowanceAsync(int classOfServiceId)
        {
            try
            {
                var classOfService = await _context.ClassOfServices
                    .FirstOrDefaultAsync(c => c.Id == classOfServiceId && c.ServiceStatus == ServiceStatus.Active);

                if (classOfService == null)
                {
                    return new JsonResult(new { success = false, message = "Class of Service not found" });
                }

                return new JsonResult(new
                {
                    success = true,
                    handsetAllowance = classOfService.HandsetAllowance ?? "0",
                    airtimeAllowance = classOfService.AirtimeAllowance ?? "",
                    dataAllowance = classOfService.DataAllowance ?? "",
                    service = classOfService.Service
                });
            }
            catch (Exception)
            {
                return new JsonResult(new { success = false, message = "Error retrieving allowance information" });
            }
        }

        public async Task<IActionResult> OnGetOfficesByOrganizationAsync(int organizationId)
        {
            try
            {
                var offices = await _context.Offices
                    .Where(o => o.OrganizationId == organizationId)
                    .OrderBy(o => o.Name)
                    .Select(o => new { id = o.Id, name = o.Name })
                    .ToListAsync();

                return new JsonResult(new { success = true, offices = offices });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving offices for organization {OrganizationId}", organizationId);
                return new JsonResult(new { success = false, message = "Error retrieving offices" });
            }
        }

        private async Task LoadSupervisorsAsync()
        {
            // Note: Supervisors are now entered via text inputs (email and name)
            // This list is kept for backward compatibility but can be empty
            Supervisors = new List<SelectListItem>();
            await Task.CompletedTask; // Keep async signature
        }

        private async Task LoadClassOfServicesAsync()
        {
            var classOfServices = await _context.ClassOfServices
                .Where(c => c.ServiceStatus == ServiceStatus.Active)
                .OrderBy(c => c.Class)
                .ThenBy(c => c.Service)
                .ToListAsync();

            ClassOfServices = classOfServices.Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = $"{c.Class} - {c.Service}",
                // Store the handset allowance in a data attribute for JavaScript access
            }).ToList();

            // Add a default "Select Class of Service" option
            ClassOfServices.Insert(0, new SelectListItem
            {
                Value = "",
                Text = "-- Select Class of Service --",
                Selected = true
            });
        }
        
        // Removed - Office table no longer exists
        // private async Task LoadOfficesAsync()
        // {
        //     Offices = await _context.Offices
        //         .OrderBy(o => o.Name)
        //         .ToListAsync();
        // }
        
        private async Task LoadOrganizationsAsync()
        {
            Organizations = await _context.Organizations
                .OrderBy(o => o.Name)
                .ToListAsync();
        }
        
        private async Task LoadClassesOfServiceAsync()
        {
            ClassesOfService = await _context.ClassOfServices
                .Where(c => c.ServiceStatus == ServiceStatus.Active)
                .OrderBy(c => c.Class)
                .ThenBy(c => c.Service)
                .ToListAsync();
        }

        private async Task SendSubmittedConfirmationEmailAsync(RefundRequest request, ApplicationUser requester)
        {
            var placeholders = new Dictionary<string, string>
            {
                { "RequestId", request.Id.ToString() },
                { "RequestDate", request.RequestDate.ToString("MMMM dd, yyyy") },
                { "RequesterName", request.MobileNumberAssignedTo ?? $"{requester.FirstName} {requester.LastName}" },
                { "PrimaryMobileNumber", request.PrimaryMobileNumber ?? "N/A" },
                { "DevicePurchaseAmount", request.DevicePurchaseAmount.ToString("N2") },
                { "DevicePurchaseCurrency", request.DevicePurchaseCurrency ?? "USD" },
                { "DeviceAllowance", request.DeviceAllowance.ToString("N2") },
                { "SupervisorName", request.SupervisorName ?? "Supervisor" },
                { "ViewRequestLink", $"{Request.Scheme}://{Request.Host}/Modules/RefundManagement/Requests/Index" },
                { "Year", DateTime.Now.Year.ToString() }
            };

            await _emailService.SendTemplatedEmailAsync(
                to: requester.Email ?? "",
                templateCode: "REFUND_REQUEST_SUBMITTED",
                data: placeholders
            );

            _logger.LogInformation("Sent submission confirmation email to {Email} for refund request {RequestId}",
                requester.Email, request.Id);
        }

        private async Task SendSupervisorNotificationEmailAsync(RefundRequest request, ApplicationUser supervisor)
        {
            var placeholders = new Dictionary<string, string>
            {
                { "RequestId", request.Id.ToString() },
                { "RequestDate", request.RequestDate.ToString("MMMM dd, yyyy") },
                { "RequesterName", request.MobileNumberAssignedTo ?? "Staff Member" },
                { "IndexNo", request.IndexNo ?? "N/A" },
                { "Organization", request.Organization ?? "N/A" },
                { "Office", request.Office ?? "N/A" },
                { "PrimaryMobileNumber", request.PrimaryMobileNumber ?? "N/A" },
                { "ClassOfService", request.ClassOfService ?? "N/A" },
                { "DeviceAllowance", request.DeviceAllowance.ToString("N2") },
                { "DevicePurchaseAmount", request.DevicePurchaseAmount.ToString("N2") },
                { "DevicePurchaseCurrency", request.DevicePurchaseCurrency ?? "USD" },
                { "SupervisorName", request.SupervisorName ?? $"{supervisor.FirstName} {supervisor.LastName}" },
                { "ApprovalLink", $"{Request.Scheme}://{Request.Host}/Modules/RefundManagement/Approvals/Supervisor" },
                { "Year", DateTime.Now.Year.ToString() }
            };

            await _emailService.SendTemplatedEmailAsync(
                to: supervisor.Email ?? request.SupervisorEmail ?? "",
                templateCode: "REFUND_SUPERVISOR_NOTIFICATION",
                data: placeholders
            );

            _logger.LogInformation("Sent supervisor notification email to {Email} for refund request {RequestId}",
                supervisor.Email, request.Id);
        }
    }
} 