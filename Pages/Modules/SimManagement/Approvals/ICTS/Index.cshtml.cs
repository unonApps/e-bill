using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TAB.Web.Data;
using TAB.Web.Models;
using TAB.Web.Services;

namespace TAB.Web.Pages.Modules.SimManagement.Approvals.ICTS
{
    [Authorize(Roles = "ICTS,Admin")]
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

        // Properties for ICTS processing
        public List<TAB.Web.Models.SimRequest> PendingRequests { get; set; } = new();
        public List<TAB.Web.Models.SimRequest> ProcessedRequests { get; set; } = new();

        // Summary statistics
        public int PendingCount { get; set; }
        public int ProcessedCount { get; set; }

        // Current user information
        public string? CurrentUserName { get; set; }
        public string? CurrentUserEmail { get; set; }
        public bool IsAdmin { get; set; }

        // Detail view properties
        public bool IsDetailView { get; set; }
        public TAB.Web.Models.SimRequest? CurrentRequest { get; set; }
        public List<SimRequestHistory> RequestHistory { get; set; } = new();

        [TempData]
        public string? StatusMessage { get; set; }
        [TempData]
        public string? StatusMessageClass { get; set; }

        public async Task<IActionResult> OnGetAsync(Guid? requestId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null)
            {
                IsAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");
                CurrentUserName = $"{currentUser.FirstName} {currentUser.LastName}";
                CurrentUserEmail = currentUser.Email;
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
            await LoadIctsRequestsAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostIctsProcessAsync(
            Guid requestId,
            string action,
            string? simSerialNo,
            string? serviceRequestNo,
            string? lineType,
            string? simPuk,
            string? lineUsage,
            string? previousLines,
            DateTime? spNotifiedDate,
            string? assignedNo,
            DateTime? collectionNotifiedDate,
            string? simIssuedBy,
            string? simCollectedBy,
            DateTime? simCollectedDate,
            string? ictsRemark)
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
                return action.ToLower() switch
                {
                    "revert" => await IctsRevertToRequestorAsync(requestId, currentUser, ictsRemark),
                    "newsim" => await IctsRequestNewSimAsync(requestId, currentUser, simSerialNo, serviceRequestNo, lineType, simPuk, lineUsage, previousLines, spNotifiedDate, assignedNo, ictsRemark),
                    "notify" => await IctsNotifyCollectionAsync(requestId, currentUser, simSerialNo, serviceRequestNo, lineType, simPuk, lineUsage, previousLines, spNotifiedDate, assignedNo, collectionNotifiedDate, simIssuedBy, simCollectedBy, simCollectedDate, ictsRemark),
                    "correct" => await IctsMarkAsCorrectedAsync(requestId, currentUser, simIssuedBy, simCollectedBy, simCollectedDate, ictsRemark),
                    "completeexisting" => await IctsCompleteExistingLineAsync(requestId, currentUser, ictsRemark),
                    _ => throw new ArgumentException("Invalid ICTS action")
                };
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error processing ICTS action: {ex.Message}";
                StatusMessageClass = "danger";
                return RedirectToPage("/Dashboard/Approver/Index");
            }
        }

        private async Task LoadIctsRequestsAsync()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return;

            CurrentUserName = $"{currentUser.FirstName} {currentUser.LastName}";
            CurrentUserEmail = currentUser.Email;

            // Check if user is admin
            var isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");

            // Load SIM requests pending ICTS processing
            var simRequests = await _context.SimRequests
                .Include(s => s.ServiceProvider)
                .Where(s => s.Status == RequestStatus.PendingIcts || s.Status == RequestStatus.PendingServiceProvider || s.Status == RequestStatus.PendingSIMCollection)
                .OrderByDescending(s => s.RequestDate)
                .ToListAsync();

            PendingRequests = simRequests;
            PendingCount = PendingRequests.Count;

            // Load processed requests
            if (isAdmin)
            {
                // Admin sees all processed requests
                var processedRequests = await _context.SimRequests
                    .Include(s => s.ServiceProvider)
                    .Where(s => s.Status == RequestStatus.Approved || s.Status == RequestStatus.Completed)
                    .OrderByDescending(s => s.ProcessedDate)
                    .Take(100) // Show more for admins
                    .ToListAsync();

                ProcessedRequests = processedRequests;
            }
            else
            {
                // ICTS users see only requests they processed
            var processedRequests = await _context.SimRequests
                .Include(s => s.ServiceProvider)
                    .Where(s => s.Status == RequestStatus.Approved || s.Status == RequestStatus.Completed)
                .Where(s => !string.IsNullOrEmpty(s.ProcessedBy) && s.ProcessedBy == currentUser.Id)
                .OrderByDescending(s => s.ProcessedDate)
                .Take(50) // Limit to last 50 processed requests
                .ToListAsync();

            ProcessedRequests = processedRequests;
            }
            
            ProcessedCount = ProcessedRequests.Count;
        }

        private async Task<IActionResult> IctsRevertToRequestorAsync(Guid requestId, ApplicationUser currentUser, string? ictsRemark)
        {
            var request = await _context.SimRequests.FirstOrDefaultAsync(r => r.PublicId == requestId);
            if (request == null || request.Status != RequestStatus.PendingIcts)
            {
                StatusMessage = "Request not found or not pending ICTS processing.";
                StatusMessageClass = "danger";
                return RedirectToPage("/Dashboard/Approver/Index");
            }

            // Revert to requestor
            request.Status = RequestStatus.Draft;
            request.IctsRemark = ictsRemark?.Trim();
            request.ProcessedBy = currentUser.Id;
            request.ProcessedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Log history
            await _historyService.AddReversionHistoryAsync(
                request.Id,
                "icts",
                currentUser.Id,
                $"{currentUser.FirstName} {currentUser.LastName}",
                ictsRemark,
                HttpContext.Connection.RemoteIpAddress?.ToString()
            );

            StatusMessage = $"SIM request for {request.FirstName} {request.LastName} has been reverted to requestor.";
            StatusMessageClass = "warning";

            return RedirectToPage("/Dashboard/Approver/Index");
        }

        private async Task<IActionResult> IctsRequestNewSimAsync(Guid requestId, ApplicationUser currentUser, string? simSerialNo, string? serviceRequestNo, string? lineType, string? simPuk, string? lineUsage, string? previousLines, DateTime? spNotifiedDate, string? assignedNo, string? ictsRemark)
        {
            var request = await _context.SimRequests.FirstOrDefaultAsync(r => r.PublicId == requestId);
            if (request == null || request.Status != RequestStatus.PendingIcts)
            {
                StatusMessage = "Request not found or not pending ICTS processing.";
                StatusMessageClass = "danger";
                return RedirectToPage("/Dashboard/Approver/Index");
            }

            // Update with ICTS processing details
            request.SimSerialNo = simSerialNo?.Trim();
            request.ServiceRequestNo = serviceRequestNo?.Trim();
            request.LineType = lineType?.Trim();
            request.SimPuk = simPuk?.Trim();
            request.LineUsage = lineUsage?.Trim();
            request.PreviousLines = previousLines?.Trim();
            request.SpNotifiedDate = spNotifiedDate;
            request.AssignedNo = assignedNo?.Trim();
            request.IctsRemark = ictsRemark?.Trim();
            request.ProcessedBy = currentUser.Id;
            request.ProcessedDate = DateTime.UtcNow;
            request.Status = RequestStatus.PendingServiceProvider; // Request for new SIM - pending service provider SIM issuance

            await _context.SaveChangesAsync();

            // Log history
            await _historyService.AddIctsActionHistoryAsync(
                request.Id,
                HistoryActions.IctsNewSimApproved,
                currentUser.Id,
                $"{currentUser.FirstName} {currentUser.LastName}",
                ictsRemark,
                HttpContext.Connection.RemoteIpAddress?.ToString()
            );

            // Send notification to requester
            await _notificationService.NotifySimRequestIctsProcessingAsync(
                request.Id,
                request.RequestedBy,
                ictsRemark,
                request.PublicId
            );

            // Log audit trail
            await _auditLogService.LogSimRequestIctsProcessingAsync(
                request.Id,
                $"{currentUser.FirstName} {currentUser.LastName}",
                $"{request.FirstName} {request.LastName}",
                assignedNo,
                ictsRemark,
                currentUser.Id,
                HttpContext.Connection.RemoteIpAddress?.ToString()
            );

            StatusMessage = $"New SIM request for {request.FirstName} {request.LastName} has been processed and is now pending service provider SIM issuance.";
            StatusMessageClass = "success";

            return RedirectToPage("/Dashboard/Approver/Index");
        }

        private async Task<IActionResult> IctsNotifyCollectionAsync(Guid requestId, ApplicationUser currentUser, string? simSerialNo, string? serviceRequestNo, string? lineType, string? simPuk, string? lineUsage, string? previousLines, DateTime? spNotifiedDate, string? assignedNo, DateTime? collectionNotifiedDate, string? simIssuedBy, string? simCollectedBy, DateTime? simCollectedDate, string? ictsRemark)
        {
            var request = await _context.SimRequests.FirstOrDefaultAsync(r => r.PublicId == requestId);
            if (request == null || request.Status != RequestStatus.PendingServiceProvider)
            {
                StatusMessage = "Request not found or not pending service provider processing.";
                StatusMessageClass = "danger";
                return RedirectToPage("/Dashboard/Approver/Index");
            }

            // Update with collection notification details
            request.CollectionNotifiedDate = collectionNotifiedDate;
            request.IctsRemark = ictsRemark?.Trim();
            request.ProcessedBy = currentUser.Id;
            request.ProcessedDate = DateTime.UtcNow;
            request.Status = RequestStatus.PendingSIMCollection; // Collection notified, pending SIM collection

            await _context.SaveChangesAsync();

            // Log history
            await _historyService.AddIctsActionHistoryAsync(
                request.Id,
                HistoryActions.IctsCollectionNotified,
                currentUser.Id,
                $"{currentUser.FirstName} {currentUser.LastName}",
                ictsRemark,
                HttpContext.Connection.RemoteIpAddress?.ToString()
            );

            // Send notification to requester
            await _notificationService.NotifySimRequestReadyForCollectionAsync(
                request.Id,
                request.RequestedBy,
                ictsRemark,
                request.PublicId
            );

            // Log audit trail
            await _auditLogService.LogSimRequestCollectionNotifiedAsync(
                request.Id,
                $"{currentUser.FirstName} {currentUser.LastName}",
                $"{request.FirstName} {request.LastName}",
                assignedNo,
                currentUser.Id,
                HttpContext.Connection.RemoteIpAddress?.ToString()
            );

            // Send email notification for SIM ready for collection
            try
            {
                await SendCollectionReadyEmailAsync(request, assignedNo, ictsRemark);
                _logger.LogInformation("Collection ready email sent for SIM request {RequestId}", request.Id);
            }
            catch (Exception emailEx)
            {
                // Log error but don't fail the notification
                _logger.LogError(emailEx, "Failed to send collection ready email for request {RequestId}", request.Id);
            }

            StatusMessage = $"Collection notification sent for {request.FirstName} {request.LastName}. Request is now pending SIM collection.";
            StatusMessageClass = "success";

            return RedirectToPage("/Dashboard/Approver/Index");
        }

        private async Task<IActionResult> IctsMarkAsCorrectedAsync(Guid requestId, ApplicationUser currentUser, string? simIssuedBy, string? simCollectedBy, DateTime? simCollectedDate, string? ictsRemark)
        {
            var request = await _context.SimRequests.FirstOrDefaultAsync(r => r.PublicId == requestId);
            if (request == null || request.Status != RequestStatus.PendingSIMCollection)
            {
                StatusMessage = "Request not found or not pending SIM collection.";
                StatusMessageClass = "danger";
                return RedirectToPage("/Dashboard/Approver/Index");
            }

            // Update with collection completion details
            request.SimIssuedBy = simIssuedBy?.Trim();
            request.SimCollectedBy = simCollectedBy?.Trim();
            request.SimCollectedDate = simCollectedDate;
            request.IctsRemark = ictsRemark?.Trim();
            request.ProcessedBy = currentUser.Id;
            request.ProcessedDate = DateTime.UtcNow;
            request.Status = RequestStatus.Completed; // Mark as completed

            await _context.SaveChangesAsync();

            // Log history
            await _historyService.AddIctsActionHistoryAsync(
                request.Id,
                HistoryActions.Completed,
                currentUser.Id,
                $"{currentUser.FirstName} {currentUser.LastName}",
                ictsRemark,
                HttpContext.Connection.RemoteIpAddress?.ToString()
            );

            // Send notification to requester
            await _notificationService.NotifySimRequestCompletedAsync(
                request.Id,
                request.RequestedBy,
                request.PublicId
            );

            // Log audit trail
            await _auditLogService.LogSimRequestCompletedAsync(
                request.Id,
                $"{currentUser.FirstName} {currentUser.LastName}",
                $"{request.FirstName} {request.LastName}",
                request.AssignedNo ?? "N/A",
                currentUser.Id,
                HttpContext.Connection.RemoteIpAddress?.ToString()
            );

            // Create or update EbillUser and UserPhone on completion
            await CreateOrUpdateEbillUserAsync(request, currentUser);

            // Send completion email to requester
            try
            {
                await SendCompletionEmailAsync(request, currentUser);
            }
            catch (Exception emailEx)
            {
                _logger.LogError(emailEx, "Failed to send completion email for request {RequestId}", request.Id);
            }

            StatusMessage = $"SIM collection completed for {request.FirstName} {request.LastName}. Request marked as completed.";
            StatusMessageClass = "success";

            return RedirectToPage("/Dashboard/Approver/Index");
        }

        private async Task<IActionResult> IctsCompleteExistingLineAsync(Guid requestId, ApplicationUser currentUser, string? ictsRemark)
        {
            var request = await _context.SimRequests.FirstOrDefaultAsync(r => r.PublicId == requestId);
            if (request == null || request.Status != RequestStatus.PendingIcts)
            {
                StatusMessage = "Request not found or not pending ICTS processing.";
                StatusMessageClass = "danger";
                return RedirectToPage("/Dashboard/Approver/Index");
            }

            // Verify this is an existing line request
            if (request.LineRequestType != LineRequestType.ExistingLine)
            {
                StatusMessage = "This action is only available for existing line requests.";
                StatusMessageClass = "danger";
                return RedirectToPage("/Dashboard/Approver/Index");
            }

            // Mark as completed directly for existing lines
            request.IctsRemark = ictsRemark?.Trim();
            request.ProcessedBy = currentUser.Id;
            request.ProcessedDate = DateTime.UtcNow;
            request.Status = RequestStatus.Completed;

            await _context.SaveChangesAsync();

            // Log history
            await _historyService.AddIctsActionHistoryAsync(
                request.Id,
                HistoryActions.Completed,
                currentUser.Id,
                $"{currentUser.FirstName} {currentUser.LastName}",
                $"Existing line request completed. {ictsRemark}",
                HttpContext.Connection.RemoteIpAddress?.ToString()
            );

            // Send notification to requester
            await _notificationService.NotifySimRequestCompletedAsync(
                request.Id,
                request.RequestedBy,
                request.PublicId
            );

            // Log audit trail
            await _auditLogService.LogSimRequestCompletedAsync(
                request.Id,
                $"{currentUser.FirstName} {currentUser.LastName}",
                $"{request.FirstName} {request.LastName}",
                request.ExistingPhoneNumber ?? "N/A",
                currentUser.Id,
                HttpContext.Connection.RemoteIpAddress?.ToString()
            );

            // Create or update EbillUser and UserPhone on completion
            await CreateOrUpdateEbillUserAsync(request, currentUser);

            // Send completion email to requester
            try
            {
                await SendCompletionEmailAsync(request, currentUser);
            }
            catch (Exception emailEx)
            {
                _logger.LogError(emailEx, "Failed to send completion email for request {RequestId}", request.Id);
            }

            StatusMessage = $"Existing line request for {request.FirstName} {request.LastName} (Phone: {request.ExistingPhoneNumber}) has been marked as completed.";
            StatusMessageClass = "success";

            return RedirectToPage("/Dashboard/Approver/Index");
        }

        private async Task CreateOrUpdateEbillUserAsync(Models.SimRequest request, ApplicationUser currentUser)
        {
            try
            {
                // Find existing EbillUser by IndexNumber or email
                var ebillUser = await _context.EbillUsers
                    .FirstOrDefaultAsync(u => u.IndexNumber == request.IndexNo);

                if (ebillUser == null && !string.IsNullOrEmpty(request.OfficialEmail))
                {
                    ebillUser = await _context.EbillUsers
                        .FirstOrDefaultAsync(u => u.Email == request.OfficialEmail);
                }

                // Look up OrganizationId and OfficeId by name
                int? organizationId = null;
                int? officeId = null;

                if (!string.IsNullOrEmpty(request.Organization))
                {
                    var org = await _context.Organizations
                        .FirstOrDefaultAsync(o => o.Name == request.Organization);
                    organizationId = org?.Id;
                }

                if (!string.IsNullOrEmpty(request.Office))
                {
                    var office = await _context.Offices
                        .FirstOrDefaultAsync(o => o.Name == request.Office);
                    officeId = office?.Id;
                }

                if (ebillUser == null)
                {
                    // Create new EbillUser
                    ebillUser = new EbillUser
                    {
                        FirstName = request.FirstName,
                        LastName = request.LastName,
                        IndexNumber = request.IndexNo,
                        Email = request.OfficialEmail,
                        OfficialMobileNumber = request.AssignedNo ?? request.ExistingPhoneNumber,
                        OrganizationId = organizationId,
                        OfficeId = officeId,
                        Location = request.Office,
                        SupervisorName = request.SupervisorName,
                        SupervisorEmail = request.SupervisorEmail,
                        IsActive = true,
                        IsAutoCreated = true,
                        ApplicationUserId = request.RequestedBy,
                        HasLoginAccount = true,
                        LoginEnabled = true,
                        CreatedDate = DateTime.UtcNow
                    };

                    _context.EbillUsers.Add(ebillUser);
                    await _context.SaveChangesAsync();

                    // Link EbillUser to ApplicationUser
                    var appUser = await _context.Users.FindAsync(request.RequestedBy);
                    if (appUser != null && !appUser.EbillUserId.HasValue)
                    {
                        appUser.EbillUserId = ebillUser.Id;
                        await _context.SaveChangesAsync();
                    }

                    _logger.LogInformation("Created EbillUser {EbillUserId} for SIM request {RequestId} ({FirstName} {LastName})",
                        ebillUser.Id, request.Id, request.FirstName, request.LastName);
                }
                else
                {
                    // Update existing EbillUser with latest details
                    ebillUser.SupervisorName = request.SupervisorName ?? ebillUser.SupervisorName;
                    ebillUser.SupervisorEmail = request.SupervisorEmail ?? ebillUser.SupervisorEmail;
                    if (organizationId.HasValue) ebillUser.OrganizationId = organizationId;
                    if (officeId.HasValue) ebillUser.OfficeId = officeId;
                    ebillUser.LastModifiedDate = DateTime.UtcNow;

                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Updated EbillUser {EbillUserId} for SIM request {RequestId}",
                        ebillUser.Id, request.Id);
                }

                // Create UserPhone record for the assigned number
                var phoneNumber = request.AssignedNo ?? request.ExistingPhoneNumber;
                if (!string.IsNullOrEmpty(phoneNumber))
                {
                    // Check if this phone number already exists for this user
                    var existingPhone = await _context.UserPhones
                        .FirstOrDefaultAsync(p => p.IndexNumber == request.IndexNo && p.PhoneNumber == phoneNumber);

                    if (existingPhone == null)
                    {
                        var userPhone = new UserPhone
                        {
                            IndexNumber = request.IndexNo,
                            PhoneNumber = phoneNumber,
                            PhoneType = "Mobile",
                            IsPrimary = !await _context.UserPhones.AnyAsync(p => p.IndexNumber == request.IndexNo && p.IsActive),
                            LineType = LineType.Primary,
                            IsActive = true,
                            Status = PhoneStatus.Active,
                            AssignedDate = DateTime.UtcNow,
                            Notes = $"Auto-created from SIM request #{request.Id}",
                            OwnershipType = PhoneOwnershipType.Personal,
                            CreatedBy = $"{currentUser.FirstName} {currentUser.LastName}",
                            CreatedDate = DateTime.UtcNow
                        };

                        _context.UserPhones.Add(userPhone);
                        await _context.SaveChangesAsync();

                        _logger.LogInformation("Created UserPhone {PhoneNumber} for EbillUser {IndexNumber} from SIM request {RequestId}",
                            phoneNumber, request.IndexNo, request.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error but don't fail the completion
                _logger.LogError(ex, "Failed to create/update EbillUser for SIM request {RequestId}", request.Id);
            }
        }

        public string GetStatusBadgeClass(RequestStatus status)
        {
            return status switch
            {
                RequestStatus.Draft => "badge bg-secondary",
                RequestStatus.PendingSupervisor => "badge bg-warning",
                RequestStatus.PendingIcts => "badge bg-primary",
                RequestStatus.PendingAdmin => "badge bg-info",
                RequestStatus.PendingServiceProvider => "badge bg-warning",
                RequestStatus.PendingSIMCollection => "badge bg-info",
                RequestStatus.Approved => "badge bg-success",
                RequestStatus.Rejected => "badge bg-danger",
                RequestStatus.Completed => "badge bg-dark",
                _ => "badge bg-light text-dark"
            };
        }

        public string GetStatusIcon(RequestStatus status)
        {
            return status switch
            {
                RequestStatus.Draft => "bi-pencil-square",
                RequestStatus.PendingSupervisor => "bi-clock",
                RequestStatus.PendingIcts => "bi-gear-fill",
                RequestStatus.PendingAdmin => "bi-person-check",
                RequestStatus.PendingServiceProvider => "bi-telephone",
                RequestStatus.PendingSIMCollection => "bi-collection",
                RequestStatus.Approved => "bi-check-circle",
                RequestStatus.Rejected => "bi-x-circle",
                RequestStatus.Completed => "bi-check-circle-fill",
                _ => "bi-question-circle"
            };
        }

        public string GetPriority(DateTime requestDate)
        {
            var daysSinceRequest = (DateTime.Now - requestDate).Days;
            return daysSinceRequest switch
            {
                > 14 => "Critical",
                > 7 => "High",
                > 3 => "Medium",
                _ => "Normal"
            };
        }

        public string GetPriorityClass(string priority)
        {
            return priority switch
            {
                "Critical" => "danger",
                "High" => "warning",
                "Medium" => "info",
                _ => "success"
            };
        }

        private async Task SendCompletionEmailAsync(Models.SimRequest request, ApplicationUser processedByUser)
        {
            var requestWithProvider = await _context.SimRequests
                .Include(r => r.ServiceProvider)
                .FirstOrDefaultAsync(r => r.Id == request.Id);

            if (requestWithProvider == null) return;

            var phoneNumber = requestWithProvider.AssignedNo ?? requestWithProvider.ExistingPhoneNumber ?? "N/A";

            var placeholders = new Dictionary<string, string>
            {
                { "RequestId", requestWithProvider.Id.ToString() },
                { "FirstName", requestWithProvider.FirstName ?? "" },
                { "LastName", requestWithProvider.LastName ?? "" },
                { "CompletionDate", DateTime.UtcNow.ToString("MMMM dd, yyyy") },
                { "PhoneNumber", phoneNumber },
                { "SimType", requestWithProvider.SimType.ToString() },
                { "ServiceProvider", requestWithProvider.ServiceProvider?.ServiceProviderName ?? "N/A" },
                { "IndexNo", requestWithProvider.IndexNo ?? "" },
                { "ProcessedBy", $"{processedByUser.FirstName} {processedByUser.LastName}" },
                { "ViewRequestLink", $"{Request.Scheme}://{Request.Host}/Modules/SimManagement/Requests/Details/{requestWithProvider.PublicId}" },
                { "Year", DateTime.Now.Year.ToString() }
            };

            await _emailService.SendTemplatedEmailAsync(
                to: requestWithProvider.OfficialEmail ?? "",
                templateCode: "SIM_REQUEST_COMPLETED",
                data: placeholders
            );

            _logger.LogInformation("Sent completion email to {Email} for request {RequestId}",
                requestWithProvider.OfficialEmail, requestWithProvider.Id);
        }

        private async Task SendCollectionReadyEmailAsync(Models.SimRequest request, string? assignedNo, string? ictsRemark)
        {
            // Get request with ServiceProvider included
            var requestWithProvider = await _context.SimRequests
                .Include(r => r.ServiceProvider)
                .FirstOrDefaultAsync(r => r.Id == request.Id);

            if (requestWithProvider == null) return;

            // Get collection configuration from appsettings or use defaults
            var collectionPoint = _configuration["SimCollection:Location"] ?? "ICTS Office, Main Building";
            var contactPerson = _configuration["SimCollection:ContactPerson"] ?? "ICTS Help Desk";
            var contactPhone = _configuration["SimCollection:ContactPhone"] ?? "Extension 1234";
            var collectionDeadlineDays = int.Parse(_configuration["SimCollection:DeadlineDays"] ?? "7");
            var collectionDeadline = DateTime.UtcNow.AddDays(collectionDeadlineDays);

            var placeholders = new Dictionary<string, string>
            {
                { "RequestId", requestWithProvider.Id.ToString() },
                { "FirstName", requestWithProvider.FirstName ?? "" },
                { "LastName", requestWithProvider.LastName ?? "" },
                { "ReadyDate", requestWithProvider.CollectionNotifiedDate?.ToString("MMMM dd, yyyy") ?? DateTime.UtcNow.ToString("MMMM dd, yyyy") },
                { "SimType", requestWithProvider.SimType.ToString() },
                { "ServiceProvider", requestWithProvider.ServiceProvider?.ServiceProviderName ?? "N/A" },
                { "PhoneNumber", assignedNo ?? "To be confirmed" },
                { "CollectionPoint", collectionPoint },
                { "ContactPerson", contactPerson },
                { "ContactPhone", contactPhone },
                { "CollectionDeadline", collectionDeadline.ToString("MMMM dd, yyyy") },
                { "Remarks", ictsRemark ?? "" },
                { "ViewRequestLink", $"{Request.Scheme}://{Request.Host}/Modules/SimManagement/Requests/Index" },
                { "Year", DateTime.Now.Year.ToString() }
            };

            await _emailService.SendTemplatedEmailAsync(
                to: requestWithProvider.OfficialEmail ?? "",
                templateCode: "SIM_READY_FOR_COLLECTION",
                data: placeholders
            );

            _logger.LogInformation("Sent collection ready email to {Email} for request {RequestId}",
                requestWithProvider.OfficialEmail, requestWithProvider.Id);
        }
    }
} 