using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TAB.Web.Data;
using TAB.Web.Models;
using TAB.Web.Services;
using System.ComponentModel.DataAnnotations;

namespace TAB.Web.Pages.Admin
{
    [Authorize(Roles = "Admin,Agency Focal Point")]
    public class PhoneNumbersModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IUserPhoneService _phoneService;
        private readonly ILogger<PhoneNumbersModel> _logger;
        private readonly IUserPhoneHistoryService _historyService;
        private readonly IAuditLogService _auditLogService;
        private readonly UserManager<ApplicationUser> _userManager;

        public PhoneNumbersModel(
            ApplicationDbContext context,
            IUserPhoneService phoneService,
            ILogger<PhoneNumbersModel> logger,
            IUserPhoneHistoryService historyService,
            IAuditLogService auditLogService,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _phoneService = phoneService;
            _logger = logger;
            _historyService = historyService;
            _auditLogService = auditLogService;
            _userManager = userManager;
        }

        public List<UserPhone> Phones { get; set; } = new();
        public List<EbillUser> AllUsers { get; set; } = new();
        public List<ClassOfService> ClassesOfService { get; set; } = new();

        [TempData]
        public string StatusMessage { get; set; } = string.Empty;

        [TempData]
        public string StatusType { get; set; } = "success";

        // Filter properties
        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? FilterStatus { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? FilterLineType { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? FilterOwnershipType { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool? FilterAssigned { get; set; }

        // Pagination
        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public int PageSize { get; set; } = 20;

        public int TotalPages { get; set; }
        public int TotalCount { get; set; }
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;

        // Statistics
        public int TotalPhones { get; set; }
        public int AssignedPhones { get; set; }
        public int UnassignedPhones { get; set; }
        public int ActivePhones { get; set; }

        [BindProperty]
        public AddPhoneInput Input { get; set; } = new();

        public class AddPhoneInput
        {
            [Required]
            [Display(Name = "Phone Number")]
            [RegularExpression(@"^[\d\+\-\(\)\s]+$", ErrorMessage = "Please enter a valid phone number")]
            public string PhoneNumber { get; set; } = string.Empty;

            [Required]
            [Display(Name = "Phone Type")]
            public string PhoneType { get; set; } = "Mobile";

            [Required]
            [Display(Name = "Line Type")]
            public LineType LineType { get; set; } = LineType.Secondary;

            [Required]
            [Display(Name = "Ownership Type")]
            public PhoneOwnershipType OwnershipType { get; set; } = PhoneOwnershipType.Personal;

            [Display(Name = "Purpose")]
            [StringLength(200)]
            public string? Purpose { get; set; }

            [Display(Name = "Assign to User (Optional)")]
            public string? IndexNumber { get; set; }

            [Display(Name = "Location")]
            [StringLength(200)]
            public string? Location { get; set; }

            [Display(Name = "Notes")]
            [StringLength(500)]
            public string? Notes { get; set; }

            [Display(Name = "Class of Service")]
            public int? ClassOfServiceId { get; set; }

            [Display(Name = "Status")]
            public PhoneStatus Status { get; set; } = PhoneStatus.Active;
        }

        public async Task OnGetAsync()
        {
            await LoadDataAsync();
        }

        public async Task<IActionResult> OnPostAddPhoneAsync()
        {
            // Remove validation entries for the Edit Phone form when adding
            var editInputKeys = ModelState.Keys
                .Where(key => key == nameof(EditInput) || key.StartsWith($"{nameof(EditInput)}."))
                .ToList();

            foreach (var key in editInputKeys)
            {
                ModelState.Remove(key);
            }

            // Also remove any bare property keys that might conflict with Input properties
            var bareKeys = new[] { "PhoneNumber", "PhoneType", "LineType", "OwnershipType", "Purpose", "IndexNumber", "Location", "Notes", "ClassOfServiceId", "Status", "Id" };
            foreach (var key in bareKeys)
            {
                if (ModelState.ContainsKey(key))
                {
                    ModelState.Remove(key);
                }
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                StatusMessage = $"Validation errors: {string.Join(", ", errors)}";
                StatusType = "danger";
                await LoadDataAsync();
                return Page();
            }

            try
            {
                // Count digits in phone number for validation
                var digitCount = Input.PhoneNumber.Count(char.IsDigit);

                // Auto-correct phone type based on digit count
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

                // Check if phone number already exists
                var existingPhone = await _context.UserPhones
                    .FirstOrDefaultAsync(p => p.PhoneNumber == Input.PhoneNumber);

                if (existingPhone != null)
                {
                    StatusMessage = $"Phone number {Input.PhoneNumber} already exists in the system.";
                    StatusType = "danger";
                    await LoadDataAsync();
                    return Page();
                }

                // If assigning to a user
                if (!string.IsNullOrWhiteSpace(Input.IndexNumber))
                {
                    var user = await _context.EbillUsers
                        .FirstOrDefaultAsync(u => u.IndexNumber == Input.IndexNumber);

                    if (user == null)
                    {
                        StatusMessage = $"User with Index Number '{Input.IndexNumber}' not found.";
                        StatusType = "danger";
                        await LoadDataAsync();
                        return Page();
                    }

                    var success = await _phoneService.AssignPhoneAsync(
                        Input.IndexNumber,
                        Input.PhoneNumber,
                        Input.PhoneType,
                        Input.LineType == LineType.Primary,
                        Input.Location,
                        Input.Notes,
                        Input.ClassOfServiceId,
                        false,
                        Input.Status,
                        Input.LineType,
                        Input.OwnershipType,
                        Input.Purpose
                    );

                    if (success)
                    {
                        StatusMessage = $"Phone {Input.PhoneNumber} added and assigned to {user.FirstName} {user.LastName}.";
                        StatusType = "success";
                    }
                    else
                    {
                        StatusMessage = "Failed to add and assign phone.";
                        StatusType = "danger";
                    }
                }
                else
                {
                    // Create unassigned phone
                    var phone = new UserPhone
                    {
                        PhoneNumber = Input.PhoneNumber,
                        PhoneType = Input.PhoneType,
                        LineType = Input.LineType,
                        OwnershipType = Input.OwnershipType,
                        Purpose = Input.Purpose,
                        Location = Input.Location,
                        Notes = Input.Notes,
                        ClassOfServiceId = Input.ClassOfServiceId,
                        Status = Input.Status,
                        IsActive = false, // Unassigned phones are inactive
                        IndexNumber = string.Empty, // No user assigned
                        AssignedDate = DateTime.UtcNow
                    };

                    _context.UserPhones.Add(phone);
                    await _context.SaveChangesAsync();

                    StatusMessage = $"Phone {Input.PhoneNumber} added successfully (unassigned).";
                    StatusType = "success";
                }

                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding phone");
                StatusMessage = "An error occurred while adding the phone.";
                StatusType = "danger";
                await LoadDataAsync();
                return Page();
            }
        }

        [BindProperty]
        public EditPhoneInput EditInput { get; set; } = new();

        public class EditPhoneInput
        {
            [Required]
            public int Id { get; set; }

            [Required]
            [Display(Name = "Phone Number")]
            public string PhoneNumber { get; set; } = string.Empty;

            [Required]
            [Display(Name = "Phone Type")]
            public string PhoneType { get; set; } = "Mobile";

            [Required]
            [Display(Name = "Line Type")]
            public LineType LineType { get; set; } = LineType.Secondary;

            [Required]
            [Display(Name = "Ownership Type")]
            public PhoneOwnershipType OwnershipType { get; set; } = PhoneOwnershipType.Personal;

            [Display(Name = "Purpose")]
            [StringLength(200)]
            public string? Purpose { get; set; }

            [Display(Name = "Assigned User")]
            [StringLength(50)]
            public string? IndexNumber { get; set; }

            [Display(Name = "Location")]
            [StringLength(200)]
            public string? Location { get; set; }

            [Display(Name = "Notes")]
            [StringLength(500)]
            public string? Notes { get; set; }

            [Display(Name = "Class of Service")]
            public int? ClassOfServiceId { get; set; }

            [Display(Name = "Status")]
            public PhoneStatus Status { get; set; } = PhoneStatus.Active;
        }

        public async Task<IActionResult> OnPostEditPhoneAsync()
        {
            // Log all form data
            _logger.LogInformation("=== Edit Phone Form Submission ===");
            foreach (var key in Request.Form.Keys)
            {
                _logger.LogInformation("Form key: {Key}, Value: {Value}", key, Request.Form[key]);
            }

            // Log ModelState before cleanup
            _logger.LogInformation("ModelState keys BEFORE cleanup: {Keys}", string.Join(", ", ModelState.Keys));
            var errorsBeforeCleanup = ModelState
                .Where(ms => ms.Value?.Errors.Any() == true)
                .Select(ms => $"{ms.Key}: {string.Join("; ", ms.Value.Errors.Select(e => e.ErrorMessage))}")
                .ToList();
            _logger.LogInformation("ModelState errors BEFORE cleanup: {Errors}", string.Join(" | ", errorsBeforeCleanup));

            // Remove validation entries for the Add Phone form when editing
            var inputKeys = ModelState.Keys
                .Where(key => key == nameof(Input) || key.StartsWith($"{nameof(Input)}."))
                .ToList();

            _logger.LogInformation("Removing {Count} Input keys from ModelState: {Keys}", inputKeys.Count, string.Join(", ", inputKeys));

            foreach (var key in inputKeys)
            {
                ModelState.Remove(key);
            }

            // Also remove any bare property keys that might conflict with EditInput properties
            // These can be created when ASP.NET tries to bind to the page model's properties directly
            var bareKeys = new[] { "PhoneNumber", "PhoneType", "LineType", "OwnershipType", "Purpose", "IndexNumber", "Location", "Notes", "ClassOfServiceId", "Status" };
            foreach (var key in bareKeys)
            {
                if (ModelState.ContainsKey(key))
                {
                    _logger.LogInformation("Removing bare key from ModelState: {Key}", key);
                    ModelState.Remove(key);
                }
            }

            // Log ModelState after cleanup
            _logger.LogInformation("ModelState keys AFTER cleanup: {Keys}", string.Join(", ", ModelState.Keys));
            var errorsAfterCleanup = ModelState
                .Where(ms => ms.Value?.Errors.Any() == true)
                .Select(ms => $"{ms.Key}: {string.Join("; ", ms.Value.Errors.Select(e => e.ErrorMessage))}")
                .ToList();
            _logger.LogInformation("ModelState errors AFTER cleanup: {Errors}", string.Join(" | ", errorsAfterCleanup));

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                StatusMessage = $"Validation errors: {string.Join(", ", errors)}";
                StatusType = "danger";
                _logger.LogWarning("Edit phone validation failed. Errors: {Errors}", string.Join(", ", errors));
                await LoadDataAsync();
                return Page();
            }

            try
            {
                var phone = await _context.UserPhones.FindAsync(EditInput.Id);

                if (phone == null)
                {
                    StatusMessage = "Phone number not found.";
                    StatusType = "danger";
                    return RedirectToPage();
                }

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

                // Update phone details
                phone.PhoneNumber = EditInput.PhoneNumber;
                phone.PhoneType = EditInput.PhoneType;
                phone.LineType = EditInput.LineType;
                phone.OwnershipType = EditInput.OwnershipType;
                phone.Purpose = EditInput.Purpose;
                phone.Location = EditInput.Location;
                phone.Notes = EditInput.Notes;
                phone.ClassOfServiceId = EditInput.ClassOfServiceId;
                phone.Status = EditInput.Status;

                // Update user assignment
                if (!string.IsNullOrWhiteSpace(EditInput.IndexNumber))
                {
                    // Assigning to a user
                    phone.IndexNumber = EditInput.IndexNumber;
                    phone.IsActive = true;
                    phone.AssignedDate = DateTime.UtcNow;
                    phone.UnassignedDate = null;
                }
                else
                {
                    // Unassigning the phone
                    phone.IndexNumber = string.Empty;
                    phone.IsActive = false;
                    phone.UnassignedDate = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                StatusMessage = $"Phone {phone.PhoneNumber} updated successfully.";
                StatusType = "success";

                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error editing phone");
                StatusMessage = "An error occurred while updating the phone.";
                StatusType = "danger";
                await LoadDataAsync();
                return Page();
            }
        }

        public async Task<IActionResult> OnPostReassignPhonesAsync(string phoneIds, string indexNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneIds) || string.IsNullOrWhiteSpace(indexNumber))
            {
                StatusMessage = "Please select phone numbers and a user to assign them to.";
                StatusType = "danger";
                return RedirectToPage();
            }

            try
            {
                // Parse phone IDs
                var ids = phoneIds.Split(',').Select(id => int.Parse(id.Trim())).ToList();

                // Check if user exists
                var user = await _context.EbillUsers
                    .FirstOrDefaultAsync(u => u.IndexNumber == indexNumber);

                if (user == null)
                {
                    StatusMessage = $"User with Index Number '{indexNumber}' not found.";
                    StatusType = "danger";
                    return RedirectToPage();
                }

                var performedBy = User.Identity?.Name ?? "System";
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                var newUserName = $"{user.FirstName} {user.LastName}";

                int successCount = 0;
                int failedCount = 0;

                foreach (var phoneId in ids)
                {
                    var phone = await _context.UserPhones
                        .Include(p => p.EbillUser)
                        .FirstOrDefaultAsync(p => p.Id == phoneId);

                    if (phone == null)
                    {
                        failedCount++;
                        continue;
                    }

                    // If phone is already assigned to this user, skip
                    if (phone.IndexNumber == indexNumber && phone.IsActive)
                    {
                        continue;
                    }

                    // Store previous assignment info for history
                    var hadPreviousUser = !string.IsNullOrEmpty(phone.IndexNumber) && phone.IsActive;
                    var previousIndexNumber = phone.IndexNumber;
                    var previousUserName = phone.EbillUser != null
                        ? $"{phone.EbillUser.FirstName} {phone.EbillUser.LastName}"
                        : previousIndexNumber;
                    var oldPhoneId = phone.Id; // Store ID before any changes for history copy

                    // Update phone assignment (same record, just changing owner)
                    phone.IndexNumber = indexNumber;
                    phone.IsActive = true;
                    phone.AssignedDate = DateTime.UtcNow;
                    phone.UnassignedDate = null;

                    await _context.SaveChangesAsync();

                    // Add history entry - differentiate between reassignment and new assignment
                    if (hadPreviousUser && previousIndexNumber != indexNumber)
                    {
                        // This is a reassignment from another user
                        // Note: Since we're updating the same record (not creating new), history stays with this phone
                        await _historyService.AddHistoryAsync(
                            phone.Id,
                            "Reassigned",
                            $"Phone reassigned from {previousUserName} ({previousIndexNumber}) to {newUserName} ({indexNumber})",
                            performedBy
                        );

                        // Log to audit trail
                        await _auditLogService.LogPhoneReassignedAsync(
                            phone.PhoneNumber,
                            previousIndexNumber ?? "",
                            previousUserName ?? "",
                            indexNumber,
                            newUserName,
                            performedBy,
                            ipAddress
                        );
                    }
                    else
                    {
                        // This is a new assignment (phone was unassigned)
                        await _historyService.AddHistoryAsync(
                            phone.Id,
                            "Assigned",
                            $"Phone assigned to {newUserName} ({indexNumber})",
                            performedBy
                        );

                        // Log to audit trail
                        await _auditLogService.LogPhoneAssignedAsync(
                            phone.PhoneNumber,
                            indexNumber,
                            newUserName,
                            phone.PhoneType ?? "Mobile",
                            phone.Status,
                            performedBy,
                            ipAddress
                        );
                    }

                    successCount++;
                }

                if (successCount > 0 && failedCount == 0)
                {
                    StatusMessage = $"Successfully reassigned {successCount} phone number{(successCount > 1 ? "s" : "")} to {user.FirstName} {user.LastName}.";
                    StatusType = "success";
                }
                else if (successCount > 0 && failedCount > 0)
                {
                    StatusMessage = $"Reassigned {successCount} phone number{(successCount > 1 ? "s" : "")}. {failedCount} failed.";
                    StatusType = "warning";
                }
                else
                {
                    StatusMessage = "Failed to reassign phone numbers.";
                    StatusType = "danger";
                }

                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reassigning phones");
                StatusMessage = "An error occurred while reassigning phones.";
                StatusType = "danger";
                return RedirectToPage();
            }
        }

        private async Task LoadDataAsync()
        {
            var scopedOrgId = await FocalPointHelper.GetScopedOrgIdAsync(User, _userManager);

            // Build query
            var query = _context.UserPhones
                .Include(p => p.EbillUser)
                .Include(p => p.ClassOfService)
                .AsQueryable();

            // Scope to focal point's org via EbillUser.OrganizationId
            if (scopedOrgId.HasValue)
                query = query.Where(p => p.EbillUser != null && p.EbillUser.OrganizationId == scopedOrgId.Value);

            // Apply filters
            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var searchLower = SearchTerm.ToLower();
                query = query.Where(p =>
                    p.PhoneNumber.ToLower().Contains(searchLower) ||
                    p.IndexNumber.ToLower().Contains(searchLower) ||
                    (p.EbillUser != null && (
                        p.EbillUser.FirstName.ToLower().Contains(searchLower) ||
                        p.EbillUser.LastName.ToLower().Contains(searchLower) ||
                        p.EbillUser.Email.ToLower().Contains(searchLower)
                    ))
                );
            }

            if (!string.IsNullOrWhiteSpace(FilterStatus) && Enum.TryParse<PhoneStatus>(FilterStatus, out var status))
            {
                query = query.Where(p => p.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(FilterLineType) && Enum.TryParse<LineType>(FilterLineType, out var lineType))
            {
                query = query.Where(p => p.LineType == lineType);
            }

            if (!string.IsNullOrWhiteSpace(FilterOwnershipType) && Enum.TryParse<PhoneOwnershipType>(FilterOwnershipType, out var ownershipType))
            {
                query = query.Where(p => p.OwnershipType == ownershipType);
            }

            if (FilterAssigned.HasValue)
            {
                if (FilterAssigned.Value)
                {
                    query = query.Where(p => p.IsActive && !string.IsNullOrEmpty(p.IndexNumber));
                }
                else
                {
                    query = query.Where(p => !p.IsActive || string.IsNullOrEmpty(p.IndexNumber));
                }
            }

            // Get total count
            TotalCount = await query.CountAsync();
            TotalPages = (int)Math.Ceiling(TotalCount / (double)PageSize);

            // Ensure valid page number
            if (PageNumber < 1) PageNumber = 1;
            if (PageNumber > TotalPages && TotalPages > 0) PageNumber = TotalPages;

            // Load phones with pagination
            Phones = await query
                .OrderByDescending(p => p.IsActive)
                .ThenBy(p => p.PhoneNumber)
                .Skip((PageNumber - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            // Load users for dropdown (scoped for focal points)
            var usersQuery = _context.EbillUsers.OrderBy(u => u.FirstName).ThenBy(u => u.LastName).AsQueryable();
            if (scopedOrgId.HasValue)
                usersQuery = usersQuery.Where(u => u.OrganizationId == scopedOrgId.Value);
            AllUsers = await usersQuery.ToListAsync();

            // Load classes of service
            ClassesOfService = await _context.ClassOfServices
                .Where(c => c.ServiceStatus == ServiceStatus.Active)
                .OrderBy(c => c.Class)
                .ToListAsync();

            // Calculate statistics
            TotalPhones = await _context.UserPhones.CountAsync();
            AssignedPhones = await _context.UserPhones.CountAsync(p => p.IsActive && !string.IsNullOrEmpty(p.IndexNumber));
            UnassignedPhones = TotalPhones - AssignedPhones;
            ActivePhones = await _context.UserPhones.CountAsync(p => p.IsActive);
        }
    }
}
