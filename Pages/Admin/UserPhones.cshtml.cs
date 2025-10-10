using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TAB.Web.Data;
using TAB.Web.Models;
using TAB.Web.Services;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace TAB.Web.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class UserPhonesModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IUserPhoneService _phoneService;
        private readonly ILogger<UserPhonesModel> _logger;
        private readonly IAuditLogService _auditLogService;
        private readonly INotificationService _notificationService;

        public UserPhonesModel(
            ApplicationDbContext context,
            IUserPhoneService phoneService,
            ILogger<UserPhonesModel> logger,
            IAuditLogService auditLogService,
            INotificationService notificationService)
        {
            _context = context;
            _phoneService = phoneService;
            _logger = logger;
            _auditLogService = auditLogService;
            _notificationService = notificationService;
        }

        [BindProperty(SupportsGet = true)]
        public string? IndexNumber { get; set; }

        public new EbillUser? User { get; set; }
        public List<UserPhone> UserPhones { get; set; } = new();
        public List<UserPhone> AllPhones { get; set; } = new();
        public List<ClassOfService> ClassesOfService { get; set; } = new();

        [TempData]
        public string StatusMessage { get; set; } = string.Empty;

        [TempData]
        public string StatusType { get; set; } = "success";

        public PhoneInput Input { get; set; } = new();

        [BindProperty]
        public EditPhoneInput EditInput { get; set; } = new();

        public class PhoneInput
        {
            [Required]
            [Display(Name = "Phone Number")]
            [RegularExpression(@"^[\d\+\-\(\)\s]+$", ErrorMessage = "Please enter a valid phone number")]
            public string PhoneNumber { get; set; } = string.Empty;

            [Required]
            [Display(Name = "Phone Type")]
            public string PhoneType { get; set; } = "Mobile";

            [Display(Name = "Location")]
            public string? Location { get; set; }

            [Display(Name = "Notes")]
            public string? Notes { get; set; }

            [Display(Name = "Set as Primary")]
            public bool IsPrimary { get; set; }

            [Display(Name = "Class of Service")]
            public int? ClassOfServiceId { get; set; }

            [Required]
            [Display(Name = "Status")]
            public PhoneStatus Status { get; set; } = PhoneStatus.Active;
        }

        public class EditPhoneInput
        {
            [Required]
            public int Id { get; set; }

            [Required]
            [Display(Name = "Phone Number")]
            [RegularExpression(@"^[\d\+\-\(\)\s]+$", ErrorMessage = "Please enter a valid phone number")]
            public string PhoneNumber { get; set; } = string.Empty;

            [Required]
            [Display(Name = "Phone Type")]
            public string PhoneType { get; set; } = "Mobile";

            [Display(Name = "Location")]
            public string? Location { get; set; }

            [Display(Name = "Notes")]
            public string? Notes { get; set; }

            [Display(Name = "Set as Primary")]
            public bool IsPrimary { get; set; }

            [Display(Name = "Class of Service")]
            public int? ClassOfServiceId { get; set; }

            [Required]
            [Display(Name = "Status")]
            public PhoneStatus Status { get; set; } = PhoneStatus.Active;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            if (string.IsNullOrEmpty(IndexNumber))
            {
                return RedirectToPage("/Admin/EbillUsers");
            }

            await LoadDataAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAssignPhoneAsync(bool forceReassign = false)
        {
            // Log the raw form data to debug binding issues
            _logger.LogInformation("=== AssignPhone Form Data ===");
            foreach (var key in Request.Form.Keys)
            {
                _logger.LogInformation("Form key: {Key}, Value: {Value}", key, Request.Form[key]);
            }

            // Manually create Input object from form data
            Input = new PhoneInput
            {
                PhoneNumber = Request.Form["Input.PhoneNumber"].FirstOrDefault() ?? string.Empty,
                PhoneType = Request.Form["Input.PhoneType"].FirstOrDefault() ?? "Mobile",
                Location = Request.Form["Input.Location"].FirstOrDefault(),
                Notes = Request.Form["Input.Notes"].FirstOrDefault(),
                IsPrimary = Request.Form["Input.IsPrimary"].FirstOrDefault() == "true",
                ClassOfServiceId = string.IsNullOrEmpty(Request.Form["Input.ClassOfServiceId"])
                    ? null
                    : int.TryParse(Request.Form["Input.ClassOfServiceId"], out var cosId) ? cosId : null,
                Status = Enum.TryParse<PhoneStatus>(Request.Form["Input.Status"], out var status)
                    ? status
                    : PhoneStatus.Active
            };

            _logger.LogInformation("Manually created Input - PhoneNumber: {PhoneNumber}, PhoneType: {PhoneType}",
                Input.PhoneNumber, Input.PhoneType);

            // Clear ModelState and validate the manually created object
            ModelState.Clear();
            TryValidateModel(Input);

            if (!ModelState.IsValid)
            {
                // Log all validation errors with their field names
                var errors = new List<string>();
                foreach (var key in ModelState.Keys)
                {
                    var state = ModelState[key];
                    if (state.Errors.Count > 0)
                    {
                        foreach (var error in state.Errors)
                        {
                            errors.Add($"Field '{key}': {error.ErrorMessage}");
                            _logger.LogWarning("Validation error - Field: {Field}, Error: {Error}, Value: {Value}",
                                key, error.ErrorMessage, state.RawValue);
                        }
                    }
                }

                _logger.LogWarning("AssignPhone ModelState is invalid. Fields with errors: {Errors}", string.Join("; ", errors));
                StatusMessage = "Validation failed: " + string.Join(", ", errors);
                StatusType = "danger";
                await LoadDataAsync();
                return Page();
            }

            if (string.IsNullOrEmpty(IndexNumber))
            {
                _logger.LogWarning("IndexNumber is null or empty");
                StatusMessage = "User index number is missing.";
                StatusType = "danger";
                await LoadDataAsync();
                return Page();
            }

            // Check if phone is already assigned
            if (!forceReassign)
            {
                // First check if this phone is already assigned to this same user
                var alreadyAssignedToSameUser = await _context.UserPhones
                    .AnyAsync(up => up.PhoneNumber == Input.PhoneNumber &&
                                   up.IndexNumber == IndexNumber &&
                                   up.IsActive);

                if (alreadyAssignedToSameUser)
                {
                    _logger.LogInformation("Phone {PhoneNumber} is already assigned to this user {IndexNumber}",
                        Input.PhoneNumber, IndexNumber);
                    StatusMessage = $"Phone number {Input.PhoneNumber} is already assigned to this staff member.";
                    StatusType = "warning";
                    await LoadDataAsync();
                    return Page();
                }

                // Check if phone is assigned to another user
                var checkResult = await _phoneService.CheckPhoneAssignmentAsync(Input.PhoneNumber, IndexNumber);
                if (!checkResult.success)
                {
                    _logger.LogInformation("Phone {PhoneNumber} is already assigned to {ExistingUser}",
                        Input.PhoneNumber, checkResult.existingUserIndex);

                    // Phone is assigned to another user, show confirmation dialog
                    TempData["ShowReassignConfirm"] = "true";
                    TempData["ExistingUserIndex"] = checkResult.existingUserIndex;
                    TempData["ExistingUserName"] = checkResult.existingUserName;
                    TempData["PhoneToAssign"] = Input.PhoneNumber;
                    TempData["PhoneType"] = Input.PhoneType;
                    TempData["Location"] = Input.Location ?? "";
                    TempData["Notes"] = Input.Notes ?? "";
                    TempData["IsPrimary"] = Input.IsPrimary.ToString();
                    TempData["ClassOfServiceId"] = Input.ClassOfServiceId?.ToString() ?? "";
                    TempData["CurrentUserIndex"] = IndexNumber;

                    // Get current user's name for the dialog
                    var currentUser = await _context.EbillUsers
                        .FirstOrDefaultAsync(u => u.IndexNumber == IndexNumber);
                    if (currentUser != null)
                    {
                        TempData["CurrentUserName"] = $"{currentUser.FirstName} {currentUser.LastName}";
                    }

                    await LoadDataAsync();
                    return Page();
                }
            }

            // Assign the phone (with forceReassign if confirmed)
            _logger.LogInformation("Attempting to assign phone {PhoneNumber} to user {IndexNumber}", Input.PhoneNumber, IndexNumber);

            try
            {
                var success = await _phoneService.AssignPhoneAsync(
                    IndexNumber,
                    Input.PhoneNumber,
                    Input.PhoneType,
                    Input.IsPrimary,
                    Input.Location,
                    Input.Notes,
                    Input.ClassOfServiceId,
                    forceReassign,
                    Input.Status
                );

                if (success)
                {
                    _logger.LogInformation("Successfully assigned phone {PhoneNumber} to user {IndexNumber}", Input.PhoneNumber, IndexNumber);

                    // Get user details for audit logging
                    var ebillUser = await _context.EbillUsers.FirstOrDefaultAsync(u => u.IndexNumber == IndexNumber);
                    var userName = ebillUser != null ? $"{ebillUser.FirstName} {ebillUser.LastName}" : IndexNumber;
                    var performedBy = base.User.Identity?.Name ?? "System";
                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

                    // Log to audit trail
                    if (forceReassign)
                    {
                        // Get the previous user info from TempData
                        var fromUserName = TempData["ExistingUserName"]?.ToString() ?? "Unknown";
                        var fromIndexNumber = TempData["ExistingUserIndex"]?.ToString() ?? "Unknown";

                        await _auditLogService.LogPhoneReassignedAsync(
                            Input.PhoneNumber,
                            fromIndexNumber,
                            fromUserName,
                            IndexNumber,
                            userName,
                            performedBy,
                            ipAddress
                        );

                        // Send notifications to both users
                        var oldUser = await _context.EbillUsers.FirstOrDefaultAsync(u => u.IndexNumber == fromIndexNumber);
                        if (oldUser?.ApplicationUserId != null && ebillUser?.ApplicationUserId != null)
                        {
                            await _notificationService.NotifyPhoneReassignedAsync(
                                oldUser.ApplicationUserId,
                                ebillUser.ApplicationUserId,
                                Input.PhoneNumber
                            );
                        }
                    }
                    else
                    {
                        await _auditLogService.LogPhoneAssignedAsync(
                            Input.PhoneNumber,
                            IndexNumber,
                            userName,
                            Input.PhoneType,
                            Input.Status,
                            performedBy,
                            ipAddress
                        );

                        // Send notification to user
                        if (ebillUser?.ApplicationUserId != null)
                        {
                            await _notificationService.NotifyPhoneAssignedAsync(
                                ebillUser.ApplicationUserId,
                                Input.PhoneNumber,
                                Input.PhoneType
                            );
                        }
                    }

                    StatusMessage = $"Phone {Input.PhoneNumber} assigned successfully.";
                    StatusType = "success";
                }
                else
                {
                    _logger.LogWarning("Failed to assign phone {PhoneNumber} to user {IndexNumber}", Input.PhoneNumber, IndexNumber);
                    StatusMessage = "Failed to assign phone. Please try again.";
                    StatusType = "danger";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning phone {PhoneNumber} to user {IndexNumber}", Input.PhoneNumber, IndexNumber);
                StatusMessage = $"Error: {ex.Message}";
                StatusType = "danger";
            }

            return RedirectToPage(new { IndexNumber });
        }

        public async Task<IActionResult> OnPostSetPrimaryAsync(int phoneId)
        {
            var success = await _phoneService.SetPrimaryPhoneAsync(phoneId);

            if (success)
            {
                StatusMessage = "Primary phone updated successfully.";
                StatusType = "success";
            }
            else
            {
                StatusMessage = "Failed to update primary phone.";
                StatusType = "danger";
            }

            return RedirectToPage(new { IndexNumber });
        }

        public async Task<IActionResult> OnPostUnassignPhoneAsync(int phoneId)
        {
            // Get phone details before unassigning for audit logging
            var phone = await _context.UserPhones
                .Include(p => p.EbillUser)
                .FirstOrDefaultAsync(p => p.Id == phoneId);

            var success = await _phoneService.UnassignPhoneAsync(phoneId);

            if (success && phone != null)
            {
                // Get user details for audit logging
                var userName = phone.EbillUser != null
                    ? $"{phone.EbillUser.FirstName} {phone.EbillUser.LastName}"
                    : phone.IndexNumber;
                var performedBy = base.User.Identity?.Name ?? "System";
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

                // Log to audit trail
                await _auditLogService.LogPhoneUnassignedAsync(
                    phone.PhoneNumber,
                    phone.IndexNumber,
                    userName,
                    performedBy,
                    ipAddress
                );

                // Send notification to user
                if (phone.EbillUser?.ApplicationUserId != null)
                {
                    await _notificationService.NotifyPhoneUnassignedAsync(
                        phone.EbillUser.ApplicationUserId,
                        phone.PhoneNumber
                    );
                }

                StatusMessage = "Phone unassigned successfully.";
                StatusType = "success";
            }
            else
            {
                StatusMessage = "Failed to unassign phone.";
                StatusType = "danger";
            }

            return RedirectToPage(new { IndexNumber });
        }

        public async Task<IActionResult> OnPostEditPhoneAsync()
        {
            // Log the raw form data to debug binding issues
            foreach (var key in Request.Form.Keys)
            {
                _logger.LogInformation("Form key: {Key}, Value: {Value}", key, Request.Form[key]);
            }

            // Log what we received in EditInput
            _logger.LogInformation("EditPhone received - Id: {Id}, PhoneNumber: {PhoneNumber}, PhoneType: {PhoneType}",
                EditInput?.Id, EditInput?.PhoneNumber, EditInput?.PhoneType);

            // Remove validation entries for the assignment form when editing
            var inputKeys = ModelState.Keys
                .Where(key => key == nameof(Input) || key.StartsWith($"{nameof(Input)}."))
                .ToList();

            foreach (var key in inputKeys)
            {
                ModelState.Remove(key);
            }

            // Log detailed ModelState information before validation
            _logger.LogInformation("ModelState Keys before validation: {Keys}", string.Join(", ", ModelState.Keys));

            if (!ModelState.IsValid)
            {
                // Log all validation errors with their field names and values
                var errors = new List<string>();
                foreach (var key in ModelState.Keys)
                {
                    var state = ModelState[key];
                    _logger.LogInformation("ModelState Key: {Key}, Value: {Value}, Errors: {ErrorCount}",
                        key, state.RawValue, state.Errors.Count);

                    if (state.Errors.Count > 0)
                    {
                        foreach (var error in state.Errors)
                        {
                            errors.Add($"Field '{key}': {error.ErrorMessage}");
                        }
                    }
                }

                _logger.LogWarning("Edit ModelState is invalid. Fields with errors: {Errors}", string.Join("; ", errors));
                StatusMessage = "Validation failed: " + string.Join(", ", errors);
                StatusType = "danger";
                await LoadDataAsync();
                return Page();
            }

            try
            {
                // Get the existing phone record
                var phone = await _context.UserPhones.FindAsync(EditInput.Id);
                if (phone == null)
                {
                    StatusMessage = "Phone record not found.";
                    StatusType = "danger";
                    return RedirectToPage(new { IndexNumber });
                }

                // Store old values for audit logging
                var oldValues = new Dictionary<string, object>
                {
                    { "PhoneNumber", phone.PhoneNumber },
                    { "PhoneType", phone.PhoneType },
                    { "Location", phone.Location ?? "" },
                    { "Notes", phone.Notes ?? "" },
                    { "IsPrimary", phone.IsPrimary },
                    { "ClassOfServiceId", phone.ClassOfServiceId?.ToString() ?? "" },
                    { "Status", phone.Status.ToString() }
                };

                var oldStatus = phone.Status;
                var oldPhoneNumber = phone.PhoneNumber;

                // Update the phone properties
                phone.PhoneNumber = EditInput.PhoneNumber;
                phone.PhoneType = EditInput.PhoneType;
                phone.Location = EditInput.Location;
                phone.Notes = EditInput.Notes;
                phone.IsPrimary = EditInput.IsPrimary;
                phone.ClassOfServiceId = EditInput.ClassOfServiceId;
                phone.Status = EditInput.Status;

                // If setting as primary, remove primary from others
                if (EditInput.IsPrimary)
                {
                    var otherPhones = await _context.UserPhones
                        .Where(p => p.IndexNumber == phone.IndexNumber && p.Id != EditInput.Id && p.IsActive)
                        .ToListAsync();

                    foreach (var otherPhone in otherPhones)
                    {
                        otherPhone.IsPrimary = false;
                    }
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully updated phone {PhoneId} for user {IndexNumber}", EditInput.Id, IndexNumber);

                // Get user details for audit logging
                var ebillUser = await _context.EbillUsers.FirstOrDefaultAsync(u => u.IndexNumber == phone.IndexNumber);
                var userName = ebillUser != null ? $"{ebillUser.FirstName} {ebillUser.LastName}" : phone.IndexNumber;
                var performedBy = base.User.Identity?.Name ?? "System";
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

                // Prepare new values for audit
                var newValues = new Dictionary<string, object>
                {
                    { "PhoneNumber", EditInput.PhoneNumber },
                    { "PhoneType", EditInput.PhoneType },
                    { "Location", EditInput.Location ?? "" },
                    { "Notes", EditInput.Notes ?? "" },
                    { "IsPrimary", EditInput.IsPrimary },
                    { "ClassOfServiceId", EditInput.ClassOfServiceId?.ToString() ?? "" },
                    { "Status", EditInput.Status.ToString() }
                };

                // Log phone edit to audit trail
                await _auditLogService.LogPhoneEditedAsync(
                    EditInput.PhoneNumber,
                    phone.IndexNumber,
                    userName,
                    oldValues,
                    newValues,
                    performedBy,
                    ipAddress
                );

                // If status changed, log a specific status change audit
                if (oldStatus != EditInput.Status)
                {
                    await _auditLogService.LogPhoneStatusChangedAsync(
                        EditInput.PhoneNumber,
                        phone.IndexNumber,
                        userName,
                        oldStatus,
                        EditInput.Status,
                        performedBy,
                        ipAddress
                    );

                    // Send notification to user about status change
                    if (ebillUser?.ApplicationUserId != null)
                    {
                        await _notificationService.NotifyPhoneStatusChangedAsync(
                            ebillUser.ApplicationUserId,
                            EditInput.PhoneNumber,
                            oldStatus,
                            EditInput.Status
                        );
                    }
                }

                // If phone number changed, send notification
                if (oldPhoneNumber != EditInput.PhoneNumber && ebillUser?.ApplicationUserId != null)
                {
                    await _notificationService.NotifyPhoneNumberChangedAsync(
                        ebillUser.ApplicationUserId,
                        oldPhoneNumber,
                        EditInput.PhoneNumber
                    );
                }

                StatusMessage = "Phone updated successfully.";
                StatusType = "success";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating phone {PhoneId}", EditInput.Id);
                StatusMessage = $"Error updating phone: {ex.Message}";
                StatusType = "danger";
            }

            return RedirectToPage(new { IndexNumber });
        }

        private async Task LoadDataAsync()
        {
            if (!string.IsNullOrEmpty(IndexNumber))
            {
                User = await _context.EbillUsers
                    .Include(u => u.OrganizationEntity)
                    .Include(u => u.OfficeEntity)
                    .FirstOrDefaultAsync(u => u.IndexNumber == IndexNumber);

                if (User != null)
                {
                    UserPhones = await _phoneService.GetUserPhonesAsync(IndexNumber);
                }
            }

            // Load Classes of Service
            ClassesOfService = await _context.ClassOfServices
                .Where(c => c.ServiceStatus == ServiceStatus.Active)
                .OrderBy(c => c.Class)
                .ToListAsync();

            // Load all phones for checking availability
            AllPhones = await _context.UserPhones
                .Where(p => p.IsActive)
                .OrderBy(p => p.PhoneNumber)
                .ToListAsync();
        }
    }
}