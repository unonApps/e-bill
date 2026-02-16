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
        private readonly IUserPhoneHistoryService _historyService;
        private readonly IEnhancedEmailService _enhancedEmailService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UserPhonesModel(
            ApplicationDbContext context,
            IUserPhoneService phoneService,
            ILogger<UserPhonesModel> logger,
            IAuditLogService auditLogService,
            INotificationService notificationService,
            IUserPhoneHistoryService historyService,
            IEnhancedEmailService enhancedEmailService,
            IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _phoneService = phoneService;
            _logger = logger;
            _auditLogService = auditLogService;
            _notificationService = notificationService;
            _historyService = historyService;
            _enhancedEmailService = enhancedEmailService;
            _httpContextAccessor = httpContextAccessor;
        }

        [BindProperty(SupportsGet = true)]
        public string? IndexNumber { get; set; }

        public new EbillUser? User { get; set; }
        public List<UserPhone> UserPhones { get; set; } = new();
        public List<UserPhone> AllPhones { get; set; } = new();
        public List<ClassOfService> ClassesOfService { get; set; } = new();
        public List<EbillUser> AllUsers { get; set; } = new();

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

            [Required]
            [Display(Name = "Ownership Type")]
            public PhoneOwnershipType OwnershipType { get; set; } = PhoneOwnershipType.Personal;

            [Display(Name = "Purpose")]
            [StringLength(200)]
            public string? Purpose { get; set; }

            [Required]
            [Display(Name = "Line Type")]
            public LineType LineType { get; set; } = LineType.Secondary;

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

            [Required]
            [Display(Name = "Ownership Type")]
            public PhoneOwnershipType OwnershipType { get; set; } = PhoneOwnershipType.Personal;

            [Display(Name = "Purpose")]
            [StringLength(200)]
            public string? Purpose { get; set; }

            [Required]
            [Display(Name = "Line Type")]
            public LineType LineType { get; set; } = LineType.Secondary;

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
                OwnershipType = Enum.TryParse<PhoneOwnershipType>(Request.Form["Input.OwnershipType"], out var ownershipType)
                    ? ownershipType
                    : PhoneOwnershipType.Personal,
                Purpose = Request.Form["Input.Purpose"].FirstOrDefault(),
                LineType = Enum.TryParse<LineType>(Request.Form["Input.LineType"], out var lineType)
                    ? lineType
                    : LineType.Secondary,
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

            // Count digits in phone number for validation
            var digitCount = Input.PhoneNumber.Count(char.IsDigit);

            // Auto-correct phone type based on digit count
            // Also map old types (Desk, Extension) to Fixed
            if (Input.PhoneType == "Desk" || Input.PhoneType == "Extension")
            {
                Input.PhoneType = "Fixed";
            }
            if (digitCount < 9 && Input.PhoneType == "Mobile")
            {
                Input.PhoneType = "Fixed";
                _logger.LogInformation("Auto-corrected phone type to Fixed for {PhoneNumber} (only {DigitCount} digits)", Input.PhoneNumber, digitCount);
            }

            // For Fixed lines, clear Line Type and Class of Service
            if (Input.PhoneType == "Fixed")
            {
                Input.LineType = LineType.Secondary; // Default value for fixed lines
                Input.ClassOfServiceId = null;
            }

            // Sync LineType with IsPrimary - they must match
            if (Input.LineType == LineType.Primary)
            {
                Input.IsPrimary = true;
            }
            else
            {
                // If LineType is not Primary, IsPrimary must be false
                Input.IsPrimary = false;
            }

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

            // Get the previous user info from TempData for reassignment history
            string? fromUserName = null;
            string? fromIndexNumber = null;
            if (forceReassign)
            {
                fromUserName = TempData.Peek("ExistingUserName")?.ToString();
                fromIndexNumber = TempData.Peek("ExistingUserIndex")?.ToString();
            }

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
                    Input.Status,
                    Input.LineType,
                    Input.OwnershipType,
                    Input.Purpose,
                    reassignedFromIndex: fromIndexNumber,
                    reassignedFromName: fromUserName
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
                    if (forceReassign && !string.IsNullOrEmpty(fromIndexNumber))
                    {
                        await _auditLogService.LogPhoneReassignedAsync(
                            Input.PhoneNumber,
                            fromIndexNumber,
                            fromUserName ?? "Unknown",
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

        public async Task<IActionResult> OnPostReassignPhoneAsync(List<int> phoneIds, string newIndexNumber)
        {
            // Validate inputs
            if (phoneIds == null || !phoneIds.Any() || string.IsNullOrWhiteSpace(newIndexNumber))
            {
                StatusMessage = "Please select at least one phone number and specify the user to reassign to.";
                StatusType = "danger";
                return RedirectToPage(new { IndexNumber });
            }

            // Check if new user exists
            var newUser = await _context.EbillUsers
                .FirstOrDefaultAsync(u => u.IndexNumber == newIndexNumber);

            if (newUser == null)
            {
                StatusMessage = $"User with Index Number '{newIndexNumber}' not found.";
                StatusType = "danger";
                return RedirectToPage(new { IndexNumber });
            }

            var performedBy = base.User.Identity?.Name ?? "System";
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var successCount = 0;
            var failedPhones = new List<string>();

            foreach (var phoneId in phoneIds)
            {
                try
                {
                    // Get phone details before reassigning
                    var phone = await _context.UserPhones
                        .Include(p => p.EbillUser)
                        .FirstOrDefaultAsync(p => p.Id == phoneId);

                    if (phone == null)
                    {
                        failedPhones.Add($"Phone ID {phoneId} not found");
                        continue;
                    }

                    // Skip if trying to reassign to the same user
                    if (phone.IndexNumber == newIndexNumber)
                    {
                        failedPhones.Add($"{phone.PhoneNumber} is already assigned to {newUser.FirstName} {newUser.LastName}");
                        continue;
                    }

                    // Store old values for audit logging
                    var oldIndexNumber = phone.IndexNumber;
                    var oldUserName = phone.EbillUser != null
                        ? $"{phone.EbillUser.FirstName} {phone.EbillUser.LastName}"
                        : oldIndexNumber;
                    var phoneNumber = phone.PhoneNumber;
                    var phoneType = phone.PhoneType;
                    var wasPrimary = phone.IsPrimary;

                    // Unassign from current user
                    var unassignSuccess = await _phoneService.UnassignPhoneAsync(phoneId);

                    if (!unassignSuccess)
                    {
                        failedPhones.Add($"{phoneNumber} - failed to unassign");
                        continue;
                    }

                    // Assign to new user - pass previous user info for proper history tracking
                    var assignSuccess = await _phoneService.AssignPhoneAsync(
                        newIndexNumber,
                        phoneNumber,
                        phoneType,
                        wasPrimary,
                        phone.Location,
                        phone.Notes,
                        phone.ClassOfServiceId,
                        forceReassign: true,
                        phone.Status,
                        phone.LineType,
                        phone.OwnershipType,
                        phone.Purpose,
                        reassignedFromIndex: oldIndexNumber,
                        reassignedFromName: oldUserName,
                        oldPhoneIdForHistory: phoneId  // Pass old phone ID to copy history
                    );

                    if (assignSuccess)
                    {
                        // Log reassignment to audit trail
                        var newUserName = $"{newUser.FirstName} {newUser.LastName}";
                        await _auditLogService.LogPhoneReassignedAsync(
                            phoneNumber,
                            oldIndexNumber,
                            oldUserName,
                            newIndexNumber,
                            newUserName,
                            performedBy,
                            ipAddress
                        );

                        // Note: History entry is now added by AssignPhoneAsync with "Reassigned" action

                        // Send notification to old user
                        if (phone.EbillUser?.ApplicationUserId != null)
                        {
                            await _notificationService.NotifyPhoneUnassignedAsync(
                                phone.EbillUser.ApplicationUserId,
                                phoneNumber
                            );
                        }

                        // Send notification to new user
                        if (newUser.ApplicationUserId != null)
                        {
                            await _notificationService.NotifyPhoneAssignedAsync(
                                newUser.ApplicationUserId,
                                phoneNumber,
                                phoneType
                            );
                        }

                        successCount++;
                    }
                    else
                    {
                        failedPhones.Add($"{phoneNumber} - failed to assign to new user");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error reassigning phone {PhoneId} to {NewUser}", phoneId, newIndexNumber);
                    failedPhones.Add($"Phone ID {phoneId} - error occurred");
                }
            }

            // Build status message
            if (successCount > 0 && failedPhones.Count == 0)
            {
                StatusMessage = $"Successfully reassigned {successCount} phone number{(successCount > 1 ? "s" : "")} to {newUser.FirstName} {newUser.LastName} ({newIndexNumber}).";
                StatusType = "success";
            }
            else if (successCount > 0 && failedPhones.Count > 0)
            {
                StatusMessage = $"Reassigned {successCount} phone number{(successCount > 1 ? "s" : "")}. Failed: {string.Join(", ", failedPhones)}";
                StatusType = "warning";
            }
            else
            {
                StatusMessage = $"Failed to reassign phones. Errors: {string.Join(", ", failedPhones)}";
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
                // Count digits in phone number for validation
                var digitCount = EditInput.PhoneNumber.Count(char.IsDigit);

                // Auto-correct phone type based on digit count
                // Also map old types (Desk, Extension) to Fixed
                if (EditInput.PhoneType == "Desk" || EditInput.PhoneType == "Extension")
                {
                    EditInput.PhoneType = "Fixed";
                }
                if (digitCount < 9 && EditInput.PhoneType == "Mobile")
                {
                    EditInput.PhoneType = "Fixed";
                    _logger.LogInformation("Auto-corrected phone type to Fixed for {PhoneNumber} (only {DigitCount} digits)", EditInput.PhoneNumber, digitCount);
                }

                // For Fixed lines, clear Line Type and Class of Service
                if (EditInput.PhoneType == "Fixed")
                {
                    EditInput.LineType = LineType.Secondary; // Default value for fixed lines
                    EditInput.ClassOfServiceId = null;
                }

                // Sync LineType with IsPrimary - they must match
                if (EditInput.LineType == LineType.Primary)
                {
                    EditInput.IsPrimary = true;
                }
                else
                {
                    // If LineType is not Primary, IsPrimary must be false
                    EditInput.IsPrimary = false;
                }

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
                    { "OwnershipType", phone.OwnershipType.ToString() },
                    { "Purpose", phone.Purpose ?? "" },
                    { "LineType", phone.LineType.ToString() },
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
                phone.OwnershipType = EditInput.OwnershipType;
                phone.Purpose = EditInput.Purpose;
                phone.LineType = EditInput.LineType;
                phone.Location = EditInput.Location;
                phone.Notes = EditInput.Notes;
                phone.IsPrimary = EditInput.IsPrimary;
                phone.ClassOfServiceId = EditInput.ClassOfServiceId;
                phone.Status = EditInput.Status;

                // If setting as primary, remove primary from others and set their LineType to Secondary
                if (EditInput.IsPrimary)
                {
                    var otherPhones = await _context.UserPhones
                        .Where(p => p.IndexNumber == phone.IndexNumber && p.Id != EditInput.Id && p.IsActive)
                        .ToListAsync();

                    foreach (var otherPhone in otherPhones)
                    {
                        otherPhone.IsPrimary = false;
                        // Set LineType to Secondary when removing primary status
                        otherPhone.LineType = LineType.Secondary;
                    }
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully updated phone {PhoneId} for user {IndexNumber}", EditInput.Id, IndexNumber);

                // Get user details for audit logging
                var ebillUser = await _context.EbillUsers.FirstOrDefaultAsync(u => u.IndexNumber == phone.IndexNumber);
                var userName = ebillUser != null ? $"{ebillUser.FirstName} {ebillUser.LastName}" : phone.IndexNumber;
                var performedBy = base.User.Identity?.Name ?? "System";
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

                // Add history tracking for changed fields
                var changedFields = new List<string>();
                if (oldValues["PhoneNumber"].ToString() != EditInput.PhoneNumber) changedFields.Add("PhoneNumber");
                if (oldValues["PhoneType"].ToString() != EditInput.PhoneType) changedFields.Add("PhoneType");
                if (oldValues["LineType"].ToString() != EditInput.LineType.ToString()) changedFields.Add("LineType");
                if ((oldValues["Location"].ToString() ?? "") != (EditInput.Location ?? "")) changedFields.Add("Location");
                if ((oldValues["Notes"].ToString() ?? "") != (EditInput.Notes ?? "")) changedFields.Add("Notes");
                if ((bool)oldValues["IsPrimary"] != EditInput.IsPrimary) changedFields.Add("IsPrimary");
                if ((oldValues["ClassOfServiceId"].ToString() ?? "") != (EditInput.ClassOfServiceId?.ToString() ?? "")) changedFields.Add("ClassOfService");
                if (oldStatus != EditInput.Status) changedFields.Add("Status");

                if (changedFields.Count > 0)
                {
                    // Generate description based on what changed
                    string description = $"Phone updated: {string.Join(", ", changedFields)}";
                    if (changedFields.Contains("LineType"))
                    {
                        description = $"Line type changed from {oldValues["LineType"]} to {EditInput.LineType}";
                    }
                    else if (changedFields.Contains("IsPrimary") && EditInput.IsPrimary)
                    {
                        description = "Set as primary phone";
                    }
                    else if (changedFields.Contains("Status"))
                    {
                        description = $"Status changed from {oldStatus} to {EditInput.Status}";
                    }

                    await _historyService.AddHistoryAsync(
                        phone.Id,
                        changedFields.Contains("LineType") || changedFields.Contains("IsPrimary") ? "LineTypeChanged" : "Updated",
                        description,
                        performedBy
                    );
                }

                // Prepare new values for audit
                var newValues = new Dictionary<string, object>
                {
                    { "PhoneNumber", EditInput.PhoneNumber },
                    { "PhoneType", EditInput.PhoneType },
                    { "LineType", EditInput.LineType.ToString() },
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

                    // Send email notification for status change
                    var userWithEmail = await _context.EbillUsers
                        .Include(u => u.ApplicationUser)
                        .FirstOrDefaultAsync(u => u.IndexNumber == phone.IndexNumber);

                    if (userWithEmail?.ApplicationUser != null && !string.IsNullOrEmpty(userWithEmail.Email))
                    {
                        await SendPhoneStatusChangedEmailAsync(userWithEmail, phone.PhoneNumber, oldStatus, EditInput.Status);
                        _logger.LogInformation($"Sent phone status changed email for {phone.PhoneNumber}: {oldStatus} → {EditInput.Status}");
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

                // If LineType changed, send email notification
                if (changedFields.Contains("LineType") && ebillUser != null)
                {
                    // Load ApplicationUser for email
                    var userWithEmail = await _context.EbillUsers
                        .Include(u => u.ApplicationUser)
                        .FirstOrDefaultAsync(u => u.IndexNumber == phone.IndexNumber);

                    if (userWithEmail?.ApplicationUser != null && !string.IsNullOrEmpty(userWithEmail.Email))
                    {
                        var oldLineType = (LineType)Enum.Parse(typeof(LineType), oldValues["LineType"].ToString()!);
                        var newLineType = EditInput.LineType;

                        await SendPhoneTypeChangedEmailAsync(userWithEmail, phone.PhoneNumber, oldLineType, newLineType);
                        _logger.LogInformation($"Sent phone type changed email for {phone.PhoneNumber}: {oldLineType} → {newLineType}");
                    }
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

        public async Task<JsonResult> OnGetPhoneHistoryAsync(int phoneId)
        {
            try
            {
                var history = await _historyService.GetHistoryForPhoneAsync(phoneId);
                return new JsonResult(history);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching history for phone {PhoneId}", phoneId);
                return new JsonResult(new List<UserPhoneHistory>());
            }
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

            // Load all users for reassignment dropdown
            AllUsers = await _context.EbillUsers
                .OrderBy(u => u.FirstName)
                .ThenBy(u => u.LastName)
                .ToListAsync();
        }

        // Email notification helper methods
        private async Task SendPhoneTypeChangedEmailAsync(EbillUser user, string phoneNumber, LineType oldLineType, LineType newLineType)
        {
            try
            {
                if (user.ApplicationUser == null || string.IsNullOrEmpty(user.Email))
                {
                    _logger.LogWarning("Cannot send phone type changed email: User {IndexNumber} has no email", user.IndexNumber);
                    return;
                }

                var userPhonesUrl = GetUserPhonesUrl(user.IndexNumber);
                var (badgeColor, textColor) = GetLineTypeBadgeColors(newLineType);
                var statusDescription = GetLineTypeDescription(newLineType);

                var emailData = new Dictionary<string, string>
                {
                    { "FirstName", user.FirstName },
                    { "LastName", user.LastName },
                    { "PhoneNumber", phoneNumber },
                    { "OldLineType", oldLineType.ToString() },
                    { "NewLineType", newLineType.ToString() },
                    { "LineTypeBadgeColor", badgeColor },
                    { "LineTypeTextColor", textColor },
                    { "StatusDescription", statusDescription },
                    { "IndexNumber", user.IndexNumber },
                    { "ChangeDate", DateTime.Now.ToString("MMMM dd, yyyy 'at' hh:mm tt") },
                    { "ViewPhoneDetailsLink", userPhonesUrl },
                    { "Year", DateTime.Now.Year.ToString() }
                };

                await _enhancedEmailService.SendTemplatedEmailAsync(
                    to: user.Email,
                    templateCode: "PHONE_TYPE_CHANGED",
                    data: emailData,
                    createdBy: "System"
                );

                _logger.LogInformation("Sent phone type changed email to {Email} for phone {PhoneNumber}", user.Email, phoneNumber);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send phone type changed email to {Email}", user.Email);
            }
        }

        private string GetUserPhonesUrl(string indexNumber)
        {
            var request = _httpContextAccessor.HttpContext?.Request;
            if (request != null)
            {
                var baseUrl = $"{request.Scheme}://{request.Host}";
                return $"{baseUrl}/Admin/UserPhones?indexNumber={indexNumber}";
            }
            return $"/Admin/UserPhones?indexNumber={indexNumber}";
        }

        private (string badgeColor, string textColor) GetLineTypeBadgeColors(LineType lineType)
        {
            return lineType switch
            {
                LineType.Primary => ("#10b981", "#ffffff"), // Green background, white text
                LineType.Secondary => ("#dbeafe", "#1e40af"), // Light blue background, dark blue text
                LineType.Reserved => ("#fef3c7", "#92400e"), // Light yellow background, dark yellow text
                _ => ("#e5e7eb", "#1f2937") // Gray fallback
            };
        }

        private string GetLineTypeDescription(LineType lineType)
        {
            return lineType switch
            {
                LineType.Primary => @"
                    <li>This is now your official primary phone number</li>
                    <li>It will be used as your main contact number in the system</li>
                    <li>All official communications will reference this number</li>
                    <li>You are responsible for all calls made on this number</li>
                    <li>This number will appear on your official records and reports</li>",

                LineType.Secondary => @"
                    <li>This is a secondary phone number assigned to your account</li>
                    <li>It serves as an additional contact line</li>
                    <li>You remain responsible for calls made on this number</li>
                    <li>This number is for official UNON business use</li>
                    <li>Secondary numbers appear in your phone list but are not your primary contact</li>",

                LineType.Reserved => @"
                    <li>This phone number has been reserved for your account</li>
                    <li>Reserved numbers are held for future assignment or special purposes</li>
                    <li>You may have limited or no active usage on this line</li>
                    <li>Contact ICTS if you need this number activated</li>
                    <li>This status is typically temporary pending activation or assignment</li>",

                _ => @"<li>Line type status updated</li>"
            };
        }

        private async Task SendPhoneStatusChangedEmailAsync(EbillUser user, string phoneNumber, PhoneStatus oldStatus, PhoneStatus newStatus)
        {
            try
            {
                if (user.ApplicationUser == null || string.IsNullOrEmpty(user.Email))
                {
                    _logger.LogWarning("Cannot send phone status changed email: User {IndexNumber} has no email", user.IndexNumber);
                    return;
                }

                var userPhonesUrl = GetUserPhonesUrl(user.IndexNumber);
                var (oldBadgeColor, oldTextColor) = GetStatusBadgeColors(oldStatus);
                var (newBadgeColor, newTextColor) = GetStatusBadgeColors(newStatus);
                var statusDescription = GetStatusDescription(newStatus);

                var emailData = new Dictionary<string, string>
                {
                    { "FirstName", user.FirstName },
                    { "LastName", user.LastName },
                    { "PhoneNumber", phoneNumber },
                    { "OldStatus", oldStatus.ToString() },
                    { "NewStatus", newStatus.ToString() },
                    { "OldStatusBadgeColor", oldBadgeColor },
                    { "OldStatusTextColor", oldTextColor },
                    { "NewStatusBadgeColor", newBadgeColor },
                    { "NewStatusTextColor", newTextColor },
                    { "StatusDescription", statusDescription },
                    { "IndexNumber", user.IndexNumber },
                    { "ChangeDate", DateTime.Now.ToString("MMMM dd, yyyy 'at' hh:mm tt") },
                    { "ReasonSection", @"<div style=""background-color: #eff6ff; border-left: 4px solid #3b82f6; padding: 20px; margin-bottom: 30px; border-radius: 8px;"">
                        <p style=""margin: 0 0 10px 0; color: #1e40af; font-size: 15px; font-weight: 700;"">Reason for Change:</p>
                        <p style=""margin: 0; color: #1e40af; font-size: 14px; line-height: 1.6;"">Status updated by administrator</p>
                    </div>" },
                    { "UserPhonesUrl", userPhonesUrl },
                    { "Year", DateTime.Now.Year.ToString() }
                };

                await _enhancedEmailService.SendTemplatedEmailAsync(
                    to: user.Email,
                    templateCode: "PHONE_STATUS_CHANGED",
                    data: emailData,
                    createdBy: "System"
                );

                _logger.LogInformation("Sent phone status changed email to {Email} for phone {PhoneNumber}", user.Email, phoneNumber);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send phone status changed email to {Email}", user.Email);
            }
        }

        private (string badgeColor, string textColor) GetStatusBadgeColors(PhoneStatus status)
        {
            return status switch
            {
                PhoneStatus.Active => ("#10b981", "#ffffff"), // Green background, white text
                PhoneStatus.Suspended => ("#fbbf24", "#78350f"), // Yellow background, dark text
                PhoneStatus.Deactivated => ("#ef4444", "#ffffff"), // Red background, white text
                _ => ("#9ca3af", "#1f2937") // Gray background, dark text
            };
        }

        private string GetStatusDescription(PhoneStatus status)
        {
            return status switch
            {
                PhoneStatus.Active => @"
                    <li>Your phone line is now active and fully operational</li>
                    <li>You can make and receive calls on this number</li>
                    <li>Call charges will be billed to your account</li>
                    <li>You are responsible for all usage on this line</li>
                    <li>Please review call logs regularly to ensure accuracy</li>",

                PhoneStatus.Suspended => @"
                    <li>Your phone line has been temporarily suspended</li>
                    <li>You may have limited or no ability to make calls</li>
                    <li>This is typically due to administrative review or policy compliance</li>
                    <li>Contact ICTS Service Desk for more information</li>
                    <li>The line may be reactivated once issues are resolved</li>",

                PhoneStatus.Deactivated => @"
                    <li>Your phone line has been deactivated</li>
                    <li>You cannot make or receive calls on this number</li>
                    <li>This may be due to end of assignment, policy violation, or administrative action</li>
                    <li>Historical call logs remain available for your records</li>
                    <li>Contact ICTS if you believe this is in error</li>",

                _ => @"<li>Phone status has been updated</li>"
            };
        }
    }
}