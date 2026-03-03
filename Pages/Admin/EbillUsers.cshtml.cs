using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TAB.Web.Data;
using TAB.Web.Models;
using TAB.Web.Services;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Globalization;
using System.Text.Json;

namespace TAB.Web.Pages.Admin
{
    [Authorize(Roles = "Admin,Agency Focal Point")]
    public class EbillUsersModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<EbillUsersModel> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUserPhoneService _phoneService;
        private readonly IAuditLogService _auditLogService;
        private readonly IEbillUserAccountService _accountService;

        public EbillUsersModel(ApplicationDbContext context, ILogger<EbillUsersModel> logger, UserManager<ApplicationUser> userManager, IUserPhoneService phoneService, IAuditLogService auditLogService, IEbillUserAccountService accountService)
        {
            _context = context;
            _logger = logger;
            _userManager = userManager;
            _phoneService = phoneService;
            _auditLogService = auditLogService;
            _accountService = accountService;
        }

        public List<EbillUser> EbillUsers { get; set; } = new();
        public Dictionary<string, List<UserPhone>> UserPhonesMap { get; set; } = new();
        public string CurrentUserName { get; set; } = string.Empty;
        
        [TempData]
        public string StatusMessage { get; set; } = string.Empty;
        
        [TempData]
        public string StatusMessageClass { get; set; } = "success";
        
        // Statistics
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int InactiveUsers { get; set; }
        public int UsersWithSupervisors { get; set; }
        public int AutoCreatedUsers { get; set; }
        
        // Filter properties
        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SelectedOrganization { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SelectedOffice { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SelectedSubOffice { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool? IsActive { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? CreationType { get; set; } // "auto" or "manual" or null (all)

        // Pagination properties
        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public int PageSize { get; set; } = 10;

        public int TotalPages { get; set; }
        public int TotalCount { get; set; }
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;

        // For dropdowns
        public List<SelectListItem> OrganizationList { get; set; } = new();
        public List<SelectListItem> OfficeList { get; set; } = new();
        public List<SelectListItem> SubOfficeList { get; set; } = new();
        public List<SelectListItem> SupervisorList { get; set; } = new();

        [BindProperty]
        public CreateEbillUserInput Input { get; set; } = new()
        {
            IsActive = true // Set default value
        };

        public EditEbillUserInput EditInput { get; set; } = new();

        public class CreateEbillUserInput
        {
            [Required(ErrorMessage = "First Name is required")]
            [Display(Name = "First Name")]
            public string FirstName { get; set; } = string.Empty;

            [Required(ErrorMessage = "Last Name is required")]
            [Display(Name = "Last Name")]
            public string LastName { get; set; } = string.Empty;

            [Required(ErrorMessage = "Index Number is required")]
            [Display(Name = "Index Number")]
            public string IndexNumber { get; set; } = string.Empty;

            [Display(Name = "Official Mobile Number")]
            public string? OfficialMobileNumber { get; set; }

            [Display(Name = "Device ID")]
            public string? IssuedDeviceID { get; set; }

            [Required(ErrorMessage = "Email is required")]
            [EmailAddress(ErrorMessage = "Invalid email format")]
            [Display(Name = "Email Address")]
            public string Email { get; set; } = string.Empty;


            [Display(Name = "Location")]
            public string? Location { get; set; }

            [Required(ErrorMessage = "Organization is required")]
            [Display(Name = "Organization")]
            public int? OrganizationId { get; set; }

            [Display(Name = "Office")]
            public int? OfficeId { get; set; }

            [Display(Name = "Sub Office")]
            public int? SubOfficeId { get; set; }

            [Display(Name = "Is Active")]
            public bool IsActive { get; set; } = true;

            [Required(ErrorMessage = "Supervisor Name is required")]
            [Display(Name = "Supervisor Name")]
            [StringLength(200)]
            public string SupervisorName { get; set; } = string.Empty;

            [Required(ErrorMessage = "Supervisor Email is required")]
            [Display(Name = "Supervisor Email")]
            [EmailAddress]
            [StringLength(256)]
            public string SupervisorEmail { get; set; } = string.Empty;

            // Login Account Fields
            [Display(Name = "Create Login Account")]
            public bool CreateLoginAccount { get; set; } = true;

            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string? Password { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Confirm Password")]
            [Compare("Password", ErrorMessage = "Passwords do not match")]
            public string? ConfirmPassword { get; set; }
        }

        public class EditEbillUserInput
        {
            public int Id { get; set; }

            [Required(ErrorMessage = "First Name is required")]
            [Display(Name = "First Name")]
            public string FirstName { get; set; } = string.Empty;

            [Required(ErrorMessage = "Last Name is required")]
            [Display(Name = "Last Name")]
            public string LastName { get; set; } = string.Empty;

            [Required(ErrorMessage = "Index Number is required")]
            [Display(Name = "Index Number")]
            public string IndexNumber { get; set; } = string.Empty;

            // OfficialMobileNumber is optional for editing (managed via User Phones)
            [Display(Name = "Official Mobile Number")]
            public string? OfficialMobileNumber { get; set; }

            [Display(Name = "Device ID")]
            public string? IssuedDeviceID { get; set; }

            [Required(ErrorMessage = "Email is required")]
            [EmailAddress(ErrorMessage = "Invalid email format")]
            [Display(Name = "Email Address")]
            public string Email { get; set; } = string.Empty;

            [Display(Name = "Location")]
            public string? Location { get; set; }

            [Display(Name = "Organization")]
            public int? OrganizationId { get; set; }

            [Display(Name = "Office")]
            public int? OfficeId { get; set; }

            [Display(Name = "Sub Office")]
            public int? SubOfficeId { get; set; }

            [Display(Name = "Is Active")]
            public bool IsActive { get; set; } = true;

            [Display(Name = "Supervisor Name")]
            [StringLength(200)]
            public string? SupervisorName { get; set; }

            [Display(Name = "Supervisor Email")]
            [EmailAddress]
            [StringLength(256)]
            public string? SupervisorEmail { get; set; }
        }

        public async Task OnGetAsync()
        {
            // Clear any previous model state
            ModelState.Clear();
            
            // Initialize Input to ensure it's not null
            Input = new() { IsActive = true };
            EditInput = new();
            
            // Get current user's name
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null)
            {
                CurrentUserName = !string.IsNullOrEmpty(currentUser.FirstName) && !string.IsNullOrEmpty(currentUser.LastName)
                    ? $"{currentUser.FirstName} {currentUser.LastName}"
                    : currentUser.UserName ?? "Administrator";
            }
            
            await LoadPageDataAsync();
        }


        public async Task<IActionResult> OnPostCreateUserAsync()
        {
            var isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";

            // Remove validation errors for the wrong model path
            var keysToRemove = ModelState.Keys.Where(k => !k.StartsWith("Input.")).ToList();
            foreach (var key in keysToRemove)
            {
                ModelState.Remove(key);
            }

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("EbillUser creation failed due to invalid ModelState");
                var errors = ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .SelectMany(x => x.Value!.Errors.Select(e => e.ErrorMessage))
                    .ToList();

                if (isAjax)
                {
                    return new JsonResult(new { success = false, errors = errors, message = string.Join(", ", errors) });
                }

                ViewData["ShowCreateModal"] = true;
                await LoadPageDataAsync();
                return Page();
            }

            try
            {
                // Focal point guard: enforce org scoping
                if (FocalPointHelper.IsFocalPoint(User))
                {
                    var fp = await _userManager.GetUserAsync(User);
                    if (Input.OrganizationId != fp?.OrganizationId)
                        return new JsonResult(new { success = false, message = "You can only create users in your own organization." });
                }

                // Check if IndexNumber already exists
                if (await _context.EbillUsers.AnyAsync(e => e.IndexNumber == Input.IndexNumber))
                {
                    var errorMsg = $"An Ebill user with Index Number '{Input.IndexNumber}' already exists.";
                    if (isAjax)
                    {
                        return new JsonResult(new { success = false, message = errorMsg });
                    }
                    StatusMessage = errorMsg;
                    StatusMessageClass = "danger";
                    ViewData["ShowCreateModal"] = true;
                    await LoadPageDataAsync();
                    return Page();
                }

                // Check if Email already exists
                if (await _context.EbillUsers.AnyAsync(e => e.Email == Input.Email))
                {
                    var errorMsg = $"An Ebill user with email '{Input.Email}' already exists.";
                    if (isAjax)
                    {
                        return new JsonResult(new { success = false, message = errorMsg });
                    }
                    StatusMessage = errorMsg;
                    StatusMessageClass = "danger";
                    ViewData["ShowCreateModal"] = true;
                    await LoadPageDataAsync();
                    return Page();
                }

                // Check if the phone number is already assigned to another user
                if (!string.IsNullOrWhiteSpace(Input.OfficialMobileNumber))
                {
                    var existingPhoneAssignment = await _context.UserPhones
                        .Include(up => up.EbillUser)
                        .FirstOrDefaultAsync(up => up.PhoneNumber == Input.OfficialMobileNumber && up.IsActive);

                    if (existingPhoneAssignment != null)
                    {
                        var assignedUserName = existingPhoneAssignment.EbillUser != null
                            ? $"{existingPhoneAssignment.EbillUser.FirstName} {existingPhoneAssignment.EbillUser.LastName}"
                            : "another user";

                        var errorMsg = $"Phone number '{Input.OfficialMobileNumber}' is already assigned to {assignedUserName} (Index: {existingPhoneAssignment.IndexNumber}).";
                        if (isAjax)
                        {
                            return new JsonResult(new { success = false, message = errorMsg });
                        }
                        StatusMessage = errorMsg;
                        StatusMessageClass = "danger";
                        ViewData["ShowCreateModal"] = true;
                        await LoadPageDataAsync();
                        return Page();
                    }
                }

                var ebillUser = new EbillUser
                {
                    FirstName = Input.FirstName,
                    LastName = Input.LastName,
                    IndexNumber = Input.IndexNumber,
                    OfficialMobileNumber = Input.OfficialMobileNumber,
                    IssuedDeviceID = Input.IssuedDeviceID,
                    Email = Input.Email,
                    Location = Input.Location,
                    OrganizationId = Input.OrganizationId,
                    OfficeId = Input.OfficeId,
                    SubOfficeId = Input.SubOfficeId,
                    IsActive = Input.IsActive,
                    SupervisorName = Input.SupervisorName,
                    SupervisorEmail = Input.SupervisorEmail,
                    CreatedDate = DateTime.UtcNow
                };

                _context.EbillUsers.Add(ebillUser);
                await _context.SaveChangesAsync();

                // Automatically add the official mobile number to UserPhones if provided
                if (!string.IsNullOrWhiteSpace(ebillUser.OfficialMobileNumber))
                {
                    var userPhone = new UserPhone
                    {
                        IndexNumber = ebillUser.IndexNumber,
                        PhoneNumber = ebillUser.OfficialMobileNumber,
                        PhoneType = "Mobile",
                        IsPrimary = true,
                        IsActive = true,
                        AssignedDate = DateTime.UtcNow,
                        CreatedDate = DateTime.UtcNow,
                        CreatedBy = User.Identity?.Name
                    };

                    _context.UserPhones.Add(userPhone);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Automatically assigned phone {PhoneNumber} to user {IndexNumber}",
                        ebillUser.OfficialMobileNumber, ebillUser.IndexNumber);
                }

                _logger.LogInformation("Created new EbillUser: {FirstName} {LastName} (ID: {Id})",
                    ebillUser.FirstName, ebillUser.LastName, ebillUser.Id);

                // Log user creation to audit trail
                var performedBy = User.Identity?.Name ?? "System";
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

                await _auditLogService.LogUserCreatedAsync(
                    ebillUser.IndexNumber,
                    $"{ebillUser.FirstName} {ebillUser.LastName}",
                    ebillUser.Email,
                    ebillUser.OfficialMobileNumber,
                    performedBy,
                    ipAddress
                );

                // Create login account if requested
                if (Input.CreateLoginAccount && !string.IsNullOrWhiteSpace(Input.Password))
                {
                    var appUser = new ApplicationUser
                    {
                        UserName = ebillUser.Email,
                        Email = ebillUser.Email,
                        FirstName = ebillUser.FirstName,
                        LastName = ebillUser.LastName,
                        EmailConfirmed = true,
                        EbillUserId = ebillUser.Id,
                        OrganizationId = ebillUser.OrganizationId,
                        OfficeId = ebillUser.OfficeId,
                        SubOfficeId = ebillUser.SubOfficeId,
                        Status = ebillUser.IsActive ? UserStatus.Active : UserStatus.Inactive
                    };

                    var result = await _userManager.CreateAsync(appUser, Input.Password);
                    if (result.Succeeded)
                    {
                        await _userManager.AddToRoleAsync(appUser, "User");

                        ebillUser.ApplicationUserId = appUser.Id;
                        ebillUser.HasLoginAccount = true;
                        ebillUser.LoginEnabled = true;
                        await _context.SaveChangesAsync();

                        _logger.LogInformation("Created login account for EbillUser {IndexNumber}", ebillUser.IndexNumber);
                        StatusMessage = $"Ebill user '{ebillUser.FirstName} {ebillUser.LastName}' and login account created successfully.";
                    }
                    else
                    {
                        var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                        _logger.LogWarning("Failed to create login account: {Errors}", errors);
                        StatusMessage = $"User created but login account failed: {errors}";
                        StatusMessageClass = "warning";
                    }
                }
                else
                {
                    StatusMessage = $"Ebill user '{ebillUser.FirstName} {ebillUser.LastName}' created successfully.";
                }

                StatusMessageClass = StatusMessageClass ?? "success";

                _logger.LogInformation("Redirecting to page after successful creation");

                if (isAjax)
                {
                    return new JsonResult(new { success = true, message = StatusMessage });
                }
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating EbillUser");
                var errorMsg = $"Error: {ex.Message}";

                // Include inner exception if available
                if (ex.InnerException != null)
                {
                    errorMsg += $" Inner: {ex.InnerException.Message}";
                }

                if (isAjax)
                {
                    return new JsonResult(new { success = false, message = errorMsg });
                }

                ModelState.AddModelError(string.Empty, errorMsg);
                await LoadPageDataAsync();
                return Page();
            }
        }

        public async Task<IActionResult> OnPostEditUserAsync()
        {
            var isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";

            _logger.LogInformation("=== EditUser Form Data ===");
            foreach (var key in Request.Form.Keys)
            {
                _logger.LogInformation("Form key: {Key}, Value: {Value}", key, Request.Form[key]);
            }

            // Manually create EditInput object from form data
            EditInput = new EditEbillUserInput
            {
                Id = int.TryParse(Request.Form["EditInput.Id"], out var id) ? id : 0,
                FirstName = Request.Form["EditInput.FirstName"].FirstOrDefault() ?? string.Empty,
                LastName = Request.Form["EditInput.LastName"].FirstOrDefault() ?? string.Empty,
                IndexNumber = Request.Form["EditInput.IndexNumber"].FirstOrDefault() ?? string.Empty,
                // OfficialMobileNumber is managed via User Phones, so we'll preserve existing value
                OfficialMobileNumber = Request.Form["EditInput.OfficialMobileNumber"].FirstOrDefault(),
                IssuedDeviceID = Request.Form["EditInput.IssuedDeviceID"].FirstOrDefault(),
                Email = Request.Form["EditInput.Email"].FirstOrDefault() ?? string.Empty,
                Location = Request.Form["EditInput.Location"].FirstOrDefault(),
                OrganizationId = string.IsNullOrEmpty(Request.Form["EditInput.OrganizationId"])
                    ? null
                    : int.TryParse(Request.Form["EditInput.OrganizationId"], out var orgId) ? orgId : null,
                OfficeId = string.IsNullOrEmpty(Request.Form["EditInput.OfficeId"])
                    ? null
                    : int.TryParse(Request.Form["EditInput.OfficeId"], out var offId) ? offId : null,
                SubOfficeId = string.IsNullOrEmpty(Request.Form["EditInput.SubOfficeId"])
                    ? null
                    : int.TryParse(Request.Form["EditInput.SubOfficeId"], out var subId) ? subId : null,
                IsActive = Request.Form["EditInput.IsActive"].Contains("true"),
                SupervisorName = Request.Form["EditInput.SupervisorName"].FirstOrDefault(),
                SupervisorEmail = Request.Form["EditInput.SupervisorEmail"].FirstOrDefault()
            };

            _logger.LogInformation("Manually created EditInput - Id: {Id}, FirstName: {FirstName}, LastName: {LastName}, Email: {Email}",
                EditInput.Id, EditInput.FirstName, EditInput.LastName, EditInput.Email);

            // Clear ModelState and validate the manually created object
            ModelState.Clear();
            TryValidateModel(EditInput);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("ModelState is invalid for EditUser after manual creation");
                var errors = ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .SelectMany(x => x.Value!.Errors.Select(e => e.ErrorMessage))
                    .ToList();

                if (isAjax)
                {
                    return new JsonResult(new { success = false, errors = errors, message = string.Join(", ", errors) });
                }

                await LoadPageDataAsync();
                return Page();
            }

            try
            {
                var ebillUser = await _context.EbillUsers.FindAsync(EditInput.Id);
                if (ebillUser == null)
                {
                    var errorMsg = "Ebill user not found.";
                    if (isAjax)
                    {
                        return new JsonResult(new { success = false, message = errorMsg });
                    }
                    StatusMessage = errorMsg;
                    StatusMessageClass = "danger";
                    return RedirectToPage();
                }

                // Check if IndexNumber already exists (excluding current user)
                if (await _context.EbillUsers.AnyAsync(e => e.IndexNumber == EditInput.IndexNumber && e.Id != EditInput.Id))
                {
                    var errorMsg = "Another Ebill user with this Index Number already exists.";
                    if (isAjax)
                    {
                        return new JsonResult(new { success = false, message = errorMsg });
                    }
                    ModelState.AddModelError(string.Empty, errorMsg);
                    await LoadPageDataAsync();
                    return Page();
                }

                // Check if Email already exists (excluding current user)
                if (await _context.EbillUsers.AnyAsync(e => e.Email == EditInput.Email && e.Id != EditInput.Id))
                {
                    var errorMsg = "Another Ebill user with this email already exists.";
                    if (isAjax)
                    {
                        return new JsonResult(new { success = false, message = errorMsg });
                    }
                    ModelState.AddModelError(string.Empty, errorMsg);
                    await LoadPageDataAsync();
                    return Page();
                }

                // Store old values for audit logging
                var oldMobileNumber = ebillUser.OfficialMobileNumber;
                var oldValues = new Dictionary<string, object>
                {
                    { "FirstName", ebillUser.FirstName },
                    { "LastName", ebillUser.LastName },
                    { "IndexNumber", ebillUser.IndexNumber },
                    { "Email", ebillUser.Email ?? "" },
                    { "OfficialMobileNumber", ebillUser.OfficialMobileNumber ?? "" },
                    { "IssuedDeviceID", ebillUser.IssuedDeviceID ?? "" },
                    { "Location", ebillUser.Location ?? "" },
                    { "OrganizationId", ebillUser.OrganizationId?.ToString() ?? "" },
                    { "OfficeId", ebillUser.OfficeId?.ToString() ?? "" },
                    { "SubOfficeId", ebillUser.SubOfficeId?.ToString() ?? "" },
                    { "IsActive", ebillUser.IsActive },
                    { "SupervisorIndexNumber", ebillUser.SupervisorIndexNumber ?? "" },
                    { "SupervisorName", ebillUser.SupervisorName ?? "" },
                    { "SupervisorEmail", ebillUser.SupervisorEmail ?? "" }
                };

                // Check if the phone number is already assigned to another user (excluding current user)
                if (!string.IsNullOrWhiteSpace(EditInput.OfficialMobileNumber))
                {
                    var existingPhoneAssignment = await _context.UserPhones
                        .Include(up => up.EbillUser)
                        .FirstOrDefaultAsync(up => up.PhoneNumber == EditInput.OfficialMobileNumber &&
                                                   up.IsActive &&
                                                   up.IndexNumber != ebillUser.IndexNumber);

                    if (existingPhoneAssignment != null)
                    {
                        var assignedUserName = existingPhoneAssignment.EbillUser != null
                            ? $"{existingPhoneAssignment.EbillUser.FirstName} {existingPhoneAssignment.EbillUser.LastName}"
                            : "another user";

                        var errorMsg = $"Phone number '{EditInput.OfficialMobileNumber}' is already assigned to {assignedUserName} (Index: {existingPhoneAssignment.IndexNumber}).";
                        if (isAjax)
                        {
                            return new JsonResult(new { success = false, message = errorMsg });
                        }
                        StatusMessage = errorMsg;
                        StatusMessageClass = "danger";
                        await LoadPageDataAsync();
                        return Page();
                    }
                }

                ebillUser.FirstName = EditInput.FirstName;
                ebillUser.LastName = EditInput.LastName;
                ebillUser.IndexNumber = EditInput.IndexNumber;
                // OfficialMobileNumber is managed via User Phones - only update if explicitly provided
                if (!string.IsNullOrEmpty(EditInput.OfficialMobileNumber))
                {
                    ebillUser.OfficialMobileNumber = EditInput.OfficialMobileNumber;
                }
                ebillUser.IssuedDeviceID = EditInput.IssuedDeviceID;
                ebillUser.Email = EditInput.Email;
                ebillUser.Location = EditInput.Location;
                ebillUser.OrganizationId = EditInput.OrganizationId;
                ebillUser.OfficeId = EditInput.OfficeId;
                ebillUser.SubOfficeId = EditInput.SubOfficeId;
                ebillUser.IsActive = EditInput.IsActive;
                ebillUser.SupervisorName = EditInput.SupervisorName;
                ebillUser.SupervisorEmail = EditInput.SupervisorEmail;
                ebillUser.LastModifiedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Handle phone number changes - update UserPhones if the official mobile number changed
                if (!string.IsNullOrEmpty(EditInput.OfficialMobileNumber) && oldMobileNumber != EditInput.OfficialMobileNumber)
                {
                    // If there was an old number, deactivate it if it was marked as primary
                    if (!string.IsNullOrWhiteSpace(oldMobileNumber))
                    {
                        var oldPhone = await _context.UserPhones
                            .FirstOrDefaultAsync(p => p.IndexNumber == ebillUser.IndexNumber &&
                                                     p.PhoneNumber == oldMobileNumber &&
                                                     p.IsPrimary &&
                                                     p.IsActive);
                        if (oldPhone != null)
                        {
                            oldPhone.IsPrimary = false;
                            _logger.LogInformation("Removed primary status from old phone {PhoneNumber} for user {IndexNumber}",
                                oldMobileNumber, ebillUser.IndexNumber);
                        }
                    }

                    // If there's a new number, add or update it in UserPhones
                    if (!string.IsNullOrWhiteSpace(EditInput.OfficialMobileNumber))
                    {
                        var existingPhone = await _context.UserPhones
                            .FirstOrDefaultAsync(p => p.IndexNumber == ebillUser.IndexNumber &&
                                                     p.PhoneNumber == EditInput.OfficialMobileNumber);

                        if (existingPhone != null)
                        {
                            // Phone already exists, just make it primary and active
                            existingPhone.IsPrimary = true;
                            existingPhone.IsActive = true;
                            existingPhone.UnassignedDate = null;
                            _logger.LogInformation("Updated existing phone {PhoneNumber} as primary for user {IndexNumber}",
                                EditInput.OfficialMobileNumber, ebillUser.IndexNumber);
                        }
                        else
                        {
                            // Add new phone entry
                            var userPhone = new UserPhone
                            {
                                IndexNumber = ebillUser.IndexNumber,
                                PhoneNumber = EditInput.OfficialMobileNumber,
                                PhoneType = "Mobile",
                                IsPrimary = true,
                                IsActive = true,
                                AssignedDate = DateTime.UtcNow,
                                CreatedDate = DateTime.UtcNow,
                                CreatedBy = User.Identity?.Name
                            };

                            _context.UserPhones.Add(userPhone);
                            _logger.LogInformation("Added new phone {PhoneNumber} as primary for user {IndexNumber}",
                                EditInput.OfficialMobileNumber, ebillUser.IndexNumber);
                        }

                        await _context.SaveChangesAsync();
                    }
                }

                _logger.LogInformation("Updated EbillUser: {FirstName} {LastName} (ID: {Id})",
                    ebillUser.FirstName, ebillUser.LastName, ebillUser.Id);

                // Log user update to audit trail
                var performedBy = User.Identity?.Name ?? "System";
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

                var newValues = new Dictionary<string, object>
                {
                    { "FirstName", EditInput.FirstName },
                    { "LastName", EditInput.LastName },
                    { "IndexNumber", EditInput.IndexNumber },
                    { "Email", EditInput.Email },
                    { "OfficialMobileNumber", EditInput.OfficialMobileNumber ?? "" },
                    { "IssuedDeviceID", EditInput.IssuedDeviceID ?? "" },
                    { "Location", EditInput.Location ?? "" },
                    { "OrganizationId", EditInput.OrganizationId?.ToString() ?? "" },
                    { "OfficeId", EditInput.OfficeId?.ToString() ?? "" },
                    { "SubOfficeId", EditInput.SubOfficeId?.ToString() ?? "" },
                    { "IsActive", EditInput.IsActive },
                    { "SupervisorName", EditInput.SupervisorName ?? "" },
                    { "SupervisorEmail", EditInput.SupervisorEmail ?? "" }
                };

                await _auditLogService.LogUserEditedAsync(
                    ebillUser.IndexNumber,
                    $"{ebillUser.FirstName} {ebillUser.LastName}",
                    oldValues,
                    newValues,
                    performedBy,
                    ipAddress
                );

                StatusMessage = "Ebill user updated successfully.";
                StatusMessageClass = "success";

                if (isAjax)
                {
                    return new JsonResult(new { success = true, message = StatusMessage });
                }
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating EbillUser");
                var errorMsg = $"Error: {ex.Message}";

                // Include inner exception if available
                if (ex.InnerException != null)
                {
                    errorMsg += $" Inner: {ex.InnerException.Message}";
                }

                if (isAjax)
                {
                    return new JsonResult(new { success = false, message = errorMsg });
                }

                ModelState.AddModelError(string.Empty, errorMsg);
                await LoadPageDataAsync();
                return Page();
            }
        }

        public async Task<IActionResult> OnGetOfficesAsync(int organizationId)
        {
            var offices = await _context.Offices
                .Where(o => o.OrganizationId == organizationId)
                .OrderBy(o => o.Name)
                .Select(o => new { value = o.Id, text = o.Name })
                .ToListAsync();

            return new JsonResult(offices);
        }

        public async Task<IActionResult> OnGetSubOfficesAsync(int officeId)
        {
            var subOffices = await _context.SubOffices
                .Where(s => s.OfficeId == officeId && s.IsActive)
                .OrderBy(s => s.Name)
                .Select(s => new { value = s.Id, text = s.Name })
                .ToListAsync();

            return new JsonResult(subOffices);
        }

        public async Task<IActionResult> OnPostDeleteUserAsync(int id)
        {
            try
            {
                var ebillUser = await _context.EbillUsers.FindAsync(id);
                if (ebillUser == null)
                {
                    StatusMessage = "Ebill user not found.";
                    StatusMessageClass = "danger";
                    return RedirectToPage();
                }

                // Check for related UserPhones
                var userPhones = await _context.UserPhones
                    .Where(up => up.IndexNumber == ebillUser.IndexNumber)
                    .ToListAsync();

                if (userPhones.Any())
                {
                    var userPhoneIds = userPhones.Select(up => up.Id).ToList();

                    // Check if any UserPhones are referenced in CallLogStaging
                    var hasCallLogStagings = await _context.CallLogStagings
                        .AnyAsync(cls => cls.UserPhoneId.HasValue && userPhoneIds.Contains(cls.UserPhoneId.Value));

                    if (hasCallLogStagings)
                    {
                        StatusMessage = $"Cannot delete user {ebillUser.FullName}. This user has associated call log staging records. Please remove or reassign the call logs before deleting the user.";
                        StatusMessageClass = "danger";
                        _logger.LogWarning("Attempted to delete EbillUser {Id} with existing CallLogStaging records", id);
                        return RedirectToPage();
                    }

                    // Check if any UserPhones are referenced in other tables (CallRecords, etc.)
                    var hasCallRecords = await _context.CallRecords
                        .AnyAsync(cr => cr.UserPhoneId.HasValue && userPhoneIds.Contains(cr.UserPhoneId.Value));

                    if (hasCallRecords)
                    {
                        StatusMessage = $"Cannot delete user {ebillUser.FullName}. This user has associated call records. Please archive or reassign the records before deleting the user.";
                        StatusMessageClass = "danger";
                        _logger.LogWarning("Attempted to delete EbillUser {Id} with existing CallRecords", id);
                        return RedirectToPage();
                    }

                    // If no related records, safe to delete UserPhones
                    _context.UserPhones.RemoveRange(userPhones);
                }

                // Check for other related records
                // Add more checks here for other tables if needed

                // Safe to delete the EbillUser
                _context.EbillUsers.Remove(ebillUser);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Deleted EbillUser: {FirstName} {LastName} (ID: {Id})",
                    ebillUser.FirstName, ebillUser.LastName, id);

                StatusMessage = "Ebill user deleted successfully.";
                StatusMessageClass = "success";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting EbillUser with ID: {Id}", id);
                StatusMessage = $"An error occurred while deleting the user. {ex.Message}";
                StatusMessageClass = "danger";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostBulkDeleteUsersAsync(List<int> userIds)
        {
            if (userIds == null || !userIds.Any())
            {
                StatusMessage = "No users selected for deletion.";
                StatusMessageClass = "warning";
                return RedirectToPage();
            }

            var deletedCount = 0;
            var errorCount = 0;
            var errors = new List<string>();

            try
            {
                foreach (var userId in userIds)
                {
                    try
                    {
                        var ebillUser = await _context.EbillUsers.FindAsync(userId);
                        if (ebillUser != null)
                        {
                            _context.EbillUsers.Remove(ebillUser);
                            deletedCount++;
                            _logger.LogInformation("Bulk delete - Deleted EbillUser: {FirstName} {LastName} (ID: {Id})",
                                ebillUser.FirstName, ebillUser.LastName, userId);
                        }
                    }
                    catch (Exception ex)
                    {
                        errorCount++;
                        errors.Add($"User ID {userId}: {ex.Message}");
                        _logger.LogError(ex, "Error deleting EbillUser with ID: {Id} during bulk delete", userId);
                    }
                }

                if (deletedCount > 0)
                {
                    await _context.SaveChangesAsync();
                }

                if (errorCount == 0)
                {
                    StatusMessage = $"Successfully deleted {deletedCount} user(s).";
                    StatusMessageClass = "success";
                }
                else
                {
                    StatusMessage = $"Deleted {deletedCount} user(s), but {errorCount} error(s) occurred. Check logs for details.";
                    StatusMessageClass = "warning";

                    if (errors.Any())
                    {
                        _logger.LogWarning("Bulk delete errors: {Errors}", string.Join("; ", errors));
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during bulk delete operation");
                StatusMessage = "An error occurred while deleting the selected users.";
                StatusMessageClass = "danger";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostBulkStatusChangeAsync(List<int> userIds, string newStatus)
        {
            if (userIds == null || !userIds.Any())
            {
                StatusMessage = "No users selected for status change.";
                StatusMessageClass = "warning";
                return RedirectToPage();
            }

            if (string.IsNullOrEmpty(newStatus))
            {
                StatusMessage = "Please select a status.";
                StatusMessageClass = "warning";
                return RedirectToPage();
            }

            var updatedCount = 0;
            var errorCount = 0;
            var errors = new List<string>();
            var isActive = newStatus.ToLower() == "active";

            try
            {
                foreach (var userId in userIds)
                {
                    try
                    {
                        var ebillUser = await _context.EbillUsers.FindAsync(userId);
                        if (ebillUser != null)
                        {
                            ebillUser.IsActive = isActive;
                            updatedCount++;
                            _logger.LogInformation("Bulk status change - Updated EbillUser: {FirstName} {LastName} (ID: {Id}) to {Status}",
                                ebillUser.FirstName, ebillUser.LastName, userId, newStatus);
                        }
                    }
                    catch (Exception ex)
                    {
                        errorCount++;
                        errors.Add($"User ID {userId}: {ex.Message}");
                        _logger.LogError(ex, "Error updating status for EbillUser with ID: {Id} during bulk status change", userId);
                    }
                }

                if (updatedCount > 0)
                {
                    await _context.SaveChangesAsync();
                }

                if (errorCount == 0)
                {
                    StatusMessage = $"Successfully updated status for {updatedCount} user(s) to {newStatus}.";
                    StatusMessageClass = "success";
                }
                else
                {
                    StatusMessage = $"Updated {updatedCount} user(s) to {newStatus}, but {errorCount} error(s) occurred. Check logs for details.";
                    StatusMessageClass = "warning";

                    if (errors.Any())
                    {
                        _logger.LogWarning("Bulk status change errors: {Errors}", string.Join("; ", errors));
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during bulk status change operation");
                StatusMessage = "An error occurred while updating user statuses.";
                StatusMessageClass = "danger";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostImportUsersAsync(IFormFile csvFile, bool skipDuplicates = true, bool updateExisting = false)
        {
            if (csvFile == null || csvFile.Length == 0)
            {
                StatusMessage = "Please select a CSV file to import.";
                StatusMessageClass = "danger";
                return RedirectToPage();
            }

            if (csvFile.Length > 5 * 1024 * 1024) // 5MB limit
            {
                StatusMessage = "File size exceeds 5MB limit.";
                StatusMessageClass = "danger";
                return RedirectToPage();
            }

            var importResults = new List<string>();
            var successCount = 0;
            var skipCount = 0;
            var errorCount = 0;
            var updateCount = 0;

            // Pre-load valid values for dropdowns
                
            var validOrganizations = await _context.Organizations
                .Select(o => o.Name)
                .ToListAsync();
                
            var validOffices = await _context.Offices
                .Select(o => o.Name)
                .ToListAsync();
                
            var validSupervisors = await _context.Users
                .Where(u => u.Status == UserStatus.Active && 
                           _context.UserRoles.Any(ur => ur.UserId == u.Id && 
                                                       _context.Roles.Any(r => r.Id == ur.RoleId && r.Name == "Supervisor")))
                .Select(u => u.UserName)
                .ToListAsync();

            try
            {
                using (var reader = new StreamReader(csvFile.OpenReadStream()))
                {
                    var lineNumber = 0;
                    string? line;
                    bool isFirstLine = true;

                    while ((line = await reader.ReadLineAsync()) != null)
                    {
                        lineNumber++;
                        
                        // Skip header row
                        if (isFirstLine)
                        {
                            isFirstLine = false;
                            continue;
                        }

                        if (string.IsNullOrWhiteSpace(line))
                            continue;

                        try
                        {
                            var values = ParseCsvLine(line);
                            
                            if (values.Length < 12)
                            {
                                importResults.Add($"Line {lineNumber}: Invalid format - expected 12 columns, found {values.Length}");
                                errorCount++;
                                continue;
                            }

                            var firstName = values[0].Trim();
                            var lastName = values[1].Trim();
                            var indexNumber = values[2].Trim();
                            var email = values[3].Trim();
                            var mobileNumber = values[4].Trim();
                            var deviceId = values[5].Trim();
                            var classOfService = values[6].Trim();
                            var location = values[7].Trim();
                            var organization = values[8].Trim();
                            var office = values[9].Trim();
                            var supervisorIndexNumber = values[10].Trim();
                            var isActiveStr = values[11].Trim().ToUpper();

                            // Validate required fields
                            if (string.IsNullOrEmpty(firstName) || string.IsNullOrEmpty(lastName) || 
                                string.IsNullOrEmpty(indexNumber) || string.IsNullOrEmpty(email) || 
                                string.IsNullOrEmpty(mobileNumber))
                            {
                                importResults.Add($"Line {lineNumber}: Missing required fields");
                                errorCount++;
                                continue;
                            }

                            // Parse IsActive
                            bool isActive = isActiveStr == "TRUE" || isActiveStr == "1" || isActiveStr == "YES";

                            // Validate dropdown values
                            
                            if (!string.IsNullOrEmpty(organization) && !validOrganizations.Contains(organization))
                            {
                                importResults.Add($"Line {lineNumber}: Invalid Organization '{organization}'");
                                errorCount++;
                                continue;
                            }
                            
                            if (!string.IsNullOrEmpty(office) && !validOffices.Contains(office))
                            {
                                importResults.Add($"Line {lineNumber}: Invalid Office '{office}'");
                                errorCount++;
                                continue;
                            }
                            
                            if (!string.IsNullOrEmpty(supervisorIndexNumber) && !validSupervisors.Contains(supervisorIndexNumber))
                            {
                                importResults.Add($"Line {lineNumber}: Invalid Supervisor Index Number '{supervisorIndexNumber}'");
                                errorCount++;
                                continue;
                            }

                            // Check for existing user
                            var existingUser = await _context.EbillUsers
                                .FirstOrDefaultAsync(e => e.IndexNumber == indexNumber || e.Email == email);

                            if (existingUser != null)
                            {
                                if (skipDuplicates && !updateExisting)
                                {
                                    importResults.Add($"Line {lineNumber}: Skipped - User with Index Number '{indexNumber}' or Email '{email}' already exists");
                                    skipCount++;
                                    continue;
                                }
                                else if (updateExisting)
                                {
                                    // Update existing user
                                    existingUser.FirstName = firstName;
                                    existingUser.LastName = lastName;
                                    existingUser.OfficialMobileNumber = mobileNumber;
                                    existingUser.IssuedDeviceID = string.IsNullOrEmpty(deviceId) ? null : deviceId;
                                    existingUser.Location = string.IsNullOrEmpty(location) ? null : location;
                                    // TODO: Map organization and office strings to IDs if needed for CSV update
                                    // existingUser.OrganizationId = ...
                                    // existingUser.OfficeId = ...
                                    existingUser.SupervisorIndexNumber = string.IsNullOrEmpty(supervisorIndexNumber) ? null : supervisorIndexNumber;
                                    existingUser.IsActive = isActive;
                                    existingUser.LastModifiedDate = DateTime.UtcNow;

                                    // Update supervisor details
                                    if (!string.IsNullOrEmpty(supervisorIndexNumber))
                                    {
                                        var supervisor = await _context.Users
                                            .FirstOrDefaultAsync(u => u.UserName == supervisorIndexNumber);
                                        
                                        if (supervisor != null)
                                        {
                                            existingUser.SupervisorName = $"{supervisor.FirstName} {supervisor.LastName}";
                                            existingUser.SupervisorEmail = supervisor.Email;
                                        }
                                    }
                                    else
                                    {
                                        existingUser.SupervisorName = null;
                                        existingUser.SupervisorEmail = null;
                                    }

                                    updateCount++;
                                    importResults.Add($"Line {lineNumber}: Updated user '{firstName} {lastName}'");
                                    continue;
                                }
                            }

                            // Create new user
                            var newUser = new EbillUser
                            {
                                FirstName = firstName,
                                LastName = lastName,
                                IndexNumber = indexNumber,
                                Email = email,
                                OfficialMobileNumber = mobileNumber,
                                IssuedDeviceID = string.IsNullOrEmpty(deviceId) ? null : deviceId,
                                Location = string.IsNullOrEmpty(location) ? null : location,
                                // Note: For CSV import, we'll need to map organization and office strings to IDs
                                // This would need to be implemented based on your business logic
                                OrganizationId = null, // TODO: Map from organization string
                                OfficeId = null, // TODO: Map from office string
                                SubOfficeId = null,
                                SupervisorIndexNumber = string.IsNullOrEmpty(supervisorIndexNumber) ? null : supervisorIndexNumber,
                                IsActive = isActive,
                                CreatedDate = DateTime.UtcNow
                            };

                            // Populate supervisor details
                            if (!string.IsNullOrEmpty(supervisorIndexNumber))
                            {
                                var supervisor = await _context.Users
                                    .FirstOrDefaultAsync(u => u.UserName == supervisorIndexNumber);
                                
                                if (supervisor != null)
                                {
                                    newUser.SupervisorName = $"{supervisor.FirstName} {supervisor.LastName}";
                                    newUser.SupervisorEmail = supervisor.Email;
                                }
                            }

                            _context.EbillUsers.Add(newUser);
                            successCount++;
                            importResults.Add($"Line {lineNumber}: Successfully imported '{firstName} {lastName}'");
                        }
                        catch (Exception lineEx)
                        {
                            importResults.Add($"Line {lineNumber}: Error - {lineEx.Message}");
                            errorCount++;
                        }
                    }
                }

                // Save all changes
                if (successCount > 0 || updateCount > 0)
                {
                    await _context.SaveChangesAsync();
                }

                // Build status message
                var summary = new StringBuilder();
                summary.Append($"Import completed: ");
                if (successCount > 0) summary.Append($"{successCount} imported, ");
                if (updateCount > 0) summary.Append($"{updateCount} updated, ");
                if (skipCount > 0) summary.Append($"{skipCount} skipped, ");
                if (errorCount > 0) summary.Append($"{errorCount} errors");
                
                StatusMessage = summary.ToString().TrimEnd(' ', ',');
                StatusMessageClass = errorCount > 0 ? "warning" : "success";
                
                // Store detailed results in TempData for display
                TempData["ImportResults"] = string.Join("\n", importResults);
                
                _logger.LogInformation("CSV Import completed: {Summary}", StatusMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing CSV file");
                StatusMessage = $"Import failed: {ex.Message}";
                StatusMessageClass = "danger";
            }

            return RedirectToPage();
        }

        private string[] ParseCsvLine(string line)
        {
            var values = new List<string>();
            var currentValue = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        // Escaped quote
                        currentValue.Append('"');
                        i++;
                    }
                    else
                    {
                        // Toggle quote mode
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    // End of field
                    values.Add(currentValue.ToString());
                    currentValue.Clear();
                }
                else
                {
                    currentValue.Append(c);
                }
            }

            // Add last field
            values.Add(currentValue.ToString());

            return values.ToArray();
        }

        private async Task LoadPageDataAsync()
        {
            var scopedOrgId = await FocalPointHelper.GetScopedOrgIdAsync(User, _userManager);

            // Build query with filters
            var query = _context.EbillUsers
                .Include(e => e.OrganizationEntity)
                .Include(e => e.OfficeEntity)
                .Include(e => e.SubOfficeEntity)
                .AsQueryable();

            // Scope to focal point's org
            if (scopedOrgId.HasValue)
                query = query.Where(e => e.OrganizationId == scopedOrgId.Value);

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var searchLower = SearchTerm.ToLower().Trim();
                var searchTerms = searchLower.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                // If multiple words, search for each word (AND logic - all words must match somewhere)
                foreach (var term in searchTerms)
                {
                    var currentTerm = term; // Capture for closure
                    query = query.Where(e =>
                        e.FirstName.ToLower().Contains(currentTerm) ||
                        e.LastName.ToLower().Contains(currentTerm) ||
                        e.IndexNumber.ToLower().Contains(currentTerm) ||
                        (e.Email != null && e.Email.ToLower().Contains(currentTerm)) ||
                        (e.OfficialMobileNumber != null && e.OfficialMobileNumber.ToLower().Contains(currentTerm)) ||
                        (e.OrganizationEntity != null && e.OrganizationEntity.Name != null && e.OrganizationEntity.Name.ToLower().Contains(currentTerm)) ||
                        (e.OfficeEntity != null && e.OfficeEntity.Name != null && e.OfficeEntity.Name.ToLower().Contains(currentTerm)));
                }
            }


            // Apply organization filter (admin only — focal points are already org-scoped)
            if (!scopedOrgId.HasValue && !string.IsNullOrWhiteSpace(SelectedOrganization) && int.TryParse(SelectedOrganization, out var orgId))
            {
                query = query.Where(e => e.OrganizationId == orgId);
            }

            // Apply office filter
            if (!string.IsNullOrWhiteSpace(SelectedOffice) && int.TryParse(SelectedOffice, out var offId))
            {
                query = query.Where(e => e.OfficeId == offId);
            }

            // Apply suboffice filter
            if (!string.IsNullOrWhiteSpace(SelectedSubOffice) && int.TryParse(SelectedSubOffice, out var subOffId))
            {
                query = query.Where(e => e.SubOfficeId == subOffId);
            }

            // Apply active status filter
            if (IsActive.HasValue)
            {
                query = query.Where(e => e.IsActive == IsActive.Value);
            }

            // Apply creation type filter
            if (!string.IsNullOrWhiteSpace(CreationType))
            {
                if (CreationType.ToLower() == "auto")
                {
                    query = query.Where(e => e.IsAutoCreated);
                }
                else if (CreationType.ToLower() == "manual")
                {
                    query = query.Where(e => !e.IsAutoCreated);
                }
            }

            // Get total count for pagination before applying paging
            TotalCount = await query.CountAsync();

            // Calculate total pages
            TotalPages = (int)Math.Ceiling(TotalCount / (double)PageSize);

            // Ensure page number is valid
            if (PageNumber < 1) PageNumber = 1;
            if (PageNumber > TotalPages && TotalPages > 0) PageNumber = TotalPages;

            // Apply pagination and load EbillUsers
            EbillUsers = await query
                .OrderBy(e => e.LastName)
                .ThenBy(e => e.FirstName)
                .Skip((PageNumber - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            // Load all ACTIVE user phones for the current page only
            var indexNumbers = EbillUsers.Select(u => u.IndexNumber).ToList();
            var allPhones = await _context.UserPhones
                .Where(p => indexNumbers.Contains(p.IndexNumber) && p.IsActive)
                .ToListAsync();

            UserPhonesMap = allPhones.GroupBy(p => p.IndexNumber)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Calculate statistics from the full query (without pagination)
            var statsQuery = _context.EbillUsers.AsQueryable();

            // Scope stats to focal point's org
            if (scopedOrgId.HasValue)
                statsQuery = statsQuery.Where(e => e.OrganizationId == scopedOrgId.Value);

            // Apply the same filters for statistics
            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var searchLower = SearchTerm.ToLower().Trim();
                var searchTerms = searchLower.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                foreach (var term in searchTerms)
                {
                    var currentTerm = term;
                    statsQuery = statsQuery.Where(e =>
                        e.FirstName.ToLower().Contains(currentTerm) ||
                        e.LastName.ToLower().Contains(currentTerm) ||
                        e.IndexNumber.ToLower().Contains(currentTerm) ||
                        (e.Email != null && e.Email.ToLower().Contains(currentTerm)) ||
                        (e.OfficialMobileNumber != null && e.OfficialMobileNumber.ToLower().Contains(currentTerm)));
                }
            }


            if (!scopedOrgId.HasValue && !string.IsNullOrWhiteSpace(SelectedOrganization) && int.TryParse(SelectedOrganization, out var statsOrgId))
            {
                statsQuery = statsQuery.Where(e => e.OrganizationId == statsOrgId);
            }

            if (!string.IsNullOrWhiteSpace(SelectedOffice) && int.TryParse(SelectedOffice, out var statsOffId))
            {
                statsQuery = statsQuery.Where(e => e.OfficeId == statsOffId);
            }

            if (!string.IsNullOrWhiteSpace(SelectedSubOffice) && int.TryParse(SelectedSubOffice, out var statsSubOffId))
            {
                statsQuery = statsQuery.Where(e => e.SubOfficeId == statsSubOffId);
            }

            if (IsActive.HasValue)
            {
                statsQuery = statsQuery.Where(e => e.IsActive == IsActive.Value);
            }

            if (!string.IsNullOrWhiteSpace(CreationType))
            {
                if (CreationType.ToLower() == "auto")
                {
                    statsQuery = statsQuery.Where(e => e.IsAutoCreated);
                }
                else if (CreationType.ToLower() == "manual")
                {
                    statsQuery = statsQuery.Where(e => !e.IsAutoCreated);
                }
            }

            TotalUsers = await statsQuery.CountAsync();
            ActiveUsers = await statsQuery.CountAsync(e => e.IsActive);
            InactiveUsers = await statsQuery.CountAsync(e => !e.IsActive);
            UsersWithSupervisors = await statsQuery.CountAsync(e => !string.IsNullOrEmpty(e.SupervisorEmail));
            AutoCreatedUsers = await statsQuery.CountAsync(e => e.IsAutoCreated);

            // Prepare dropdown lists

            var orgListQuery = _context.Organizations.OrderBy(o => o.Code).ThenBy(o => o.Name).AsQueryable();
            if (scopedOrgId.HasValue)
                orgListQuery = orgListQuery.Where(o => o.Id == scopedOrgId.Value);

            OrganizationList = await orgListQuery
                .Select(o => new SelectListItem
                {
                    Value = o.Id.ToString(),
                    Text = (o.Code != null ? o.Code + " - " : "") + o.Name
                })
                .ToListAsync();

            // Load office list (scoped for focal points)
            var officeListQuery = _context.Offices.OrderBy(o => o.Name).AsQueryable();
            if (scopedOrgId.HasValue)
                officeListQuery = officeListQuery.Where(o => o.OrganizationId == scopedOrgId.Value);

            OfficeList = await officeListQuery
                .Select(o => new SelectListItem
                {
                    Value = o.Id.ToString(),
                    Text = o.Name
                })
                .ToListAsync();

            // SubOffice list is loaded dynamically via AJAX when office is selected
            SubOfficeList = new List<SelectListItem>();

            // Get supervisors
            var supervisorRoleId = await _context.Roles
                .Where(r => r.Name == "Supervisor")
                .Select(r => r.Id)
                .FirstOrDefaultAsync();

            if (!string.IsNullOrEmpty(supervisorRoleId))
            {
                SupervisorList = await _context.Users
                    .Where(u => u.Status == UserStatus.Active && 
                               _context.UserRoles.Any(ur => ur.UserId == u.Id && ur.RoleId == supervisorRoleId))
                    .OrderBy(u => u.LastName)
                    .ThenBy(u => u.FirstName)
                    .Select(u => new SelectListItem
                    {
                        Value = u.UserName,
                        Text = $"{u.FirstName} {u.LastName} ({u.UserName})"
                    })
                    .ToListAsync();
            }
            else
            {
                SupervisorList = new();
            }
        }

        // Reset Password Handler
        public async Task<IActionResult> OnPostResetPasswordAsync(int userId)
        {
            try
            {
                var (success, message, tempPassword) = await _accountService.ResetPasswordAsync(userId, sendEmail: true);

                if (success)
                {
                    var ebillUser = await _context.EbillUsers.FindAsync(userId);
                    return new JsonResult(new
                    {
                        success = true,
                        email = ebillUser?.Email,
                        tempPassword = tempPassword,
                        message = "Password reset successfully"
                    });
                }

                return new JsonResult(new { success = false, message = message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password for user {UserId}", userId);
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }

        // Create Account Handler
        public async Task<IActionResult> OnPostCreateAccountAsync(int userId)
        {
            try
            {
                var (success, message, tempPassword) = await _accountService.CreateLoginAccountAsync(userId, sendEmail: true);

                if (success)
                {
                    var ebillUser = await _context.EbillUsers.FindAsync(userId);
                    return new JsonResult(new
                    {
                        success = true,
                        email = ebillUser?.Email,
                        tempPassword = tempPassword,
                        message = "Login account created successfully"
                    });
                }

                return new JsonResult(new { success = false, message = message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating account for user {UserId}", userId);
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }

    }
}