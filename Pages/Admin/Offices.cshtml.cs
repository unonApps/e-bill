using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TAB.Web.Data;
using TAB.Web.Models;
using System.ComponentModel.DataAnnotations;

namespace TAB.Web.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class OfficesModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public OfficesModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public List<Office> Offices { get; set; } = new List<Office>();
        public List<SubOffice> SubOffices { get; set; } = new List<SubOffice>();
        public List<SelectListItem> Organizations { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> OfficesList { get; set; } = new List<SelectListItem>();
        public string StatusMessage { get; set; } = string.Empty;
        public string StatusMessageClass { get; set; } = "success";
        public string CurrentUserName { get; set; } = string.Empty;

        // Statistics
        public int TotalOffices { get; set; }
        public int TotalOrganizations { get; set; }
        public int TotalUsers { get; set; }
        public int ActiveOffices { get; set; }
        public int TotalSubOffices { get; set; }

        // Filter properties
        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? SelectedOrganizationId { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool? HasUsers { get; set; }

        // Pagination properties
        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public int PageSize { get; set; } = 20;

        public int TotalPages { get; set; }
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;
        public int TotalRecords { get; set; }

        [BindProperty]
        public OfficeInputModel Input { get; set; } = new OfficeInputModel();

        [BindProperty]
        public SubOfficeInputModel SubOfficeInput { get; set; } = new SubOfficeInputModel();

        public class OfficeInputModel
        {
            public int? Id { get; set; }

            [Required]
            [StringLength(100)]
            public string Name { get; set; } = string.Empty;

            [StringLength(10)]
            public string? Code { get; set; }

            [StringLength(500)]
            public string? Description { get; set; }

            [Required]
            public int OrganizationId { get; set; }
        }

        public class SubOfficeInputModel
        {
            public int? Id { get; set; }

            [Required]
            [StringLength(100)]
            public string Name { get; set; } = string.Empty;

            [StringLength(10)]
            public string? Code { get; set; }

            [StringLength(500)]
            public string? Description { get; set; }

            [StringLength(100)]
            public string? ContactPerson { get; set; }

            [StringLength(20)]
            public string? PhoneNumber { get; set; }

            [EmailAddress]
            [StringLength(100)]
            public string? Email { get; set; }

            [StringLength(200)]
            public string? Address { get; set; }

            [Required]
            public int OfficeId { get; set; }

            public bool IsActive { get; set; } = true;
        }

        public async Task OnGetAsync()
        {
            // Get current user's name
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null)
            {
                CurrentUserName = !string.IsNullOrEmpty(currentUser.FirstName) && !string.IsNullOrEmpty(currentUser.LastName)
                    ? $"{currentUser.FirstName} {currentUser.LastName}"
                    : currentUser.UserName ?? "Administrator";
            }

            await LoadDataAsync();
        }

        // Default POST handler - catches any POST that doesn't match a specific handler
        public async Task<IActionResult> OnPostAsync()
        {
            Console.WriteLine("[OnPostAsync] Default POST handler called - this shouldn't happen!");
            await LoadDataAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostCreateOfficeAsync()
        {
            Console.WriteLine("[OnPostCreateOfficeAsync] Starting...");

            // Debug: Log all form values
            Console.WriteLine($"[OnPostCreateOfficeAsync] Input.Name = '{Input?.Name ?? "null"}'");
            Console.WriteLine($"[OnPostCreateOfficeAsync] Input.OrganizationId = {Input?.OrganizationId ?? 0}");
            Console.WriteLine($"[OnPostCreateOfficeAsync] Input.Description = '{Input?.Description ?? "null"}'");

            // Get current user's name
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null)
            {
                CurrentUserName = !string.IsNullOrEmpty(currentUser.FirstName) && !string.IsNullOrEmpty(currentUser.LastName)
                    ? $"{currentUser.FirstName} {currentUser.LastName}"
                    : currentUser.UserName ?? "Administrator";
            }

            // CRITICAL: Load data FIRST before ModelState check
            await LoadDataAsync();

            Console.WriteLine($"[OnPostCreateOfficeAsync] After LoadDataAsync: Organizations.Count = {Organizations?.Count ?? 0}");

            // Log ALL ModelState keys before removal to understand what's being validated
            Console.WriteLine("[OnPostCreateOfficeAsync] All ModelState keys before removal:");
            foreach (var key in ModelState.Keys)
            {
                Console.WriteLine($"  - Key: '{key}'");
            }

            // Remove all validation errors that are not related to Input (Office creation)
            var keysToRemove = ModelState.Keys.Where(k => !k.StartsWith("Input.")).ToList();
            foreach (var key in keysToRemove)
            {
                ModelState.Remove(key);
                Console.WriteLine($"[OnPostCreateOfficeAsync] Removed ModelState key: '{key}'");
            }

            if (!ModelState.IsValid)
            {
                // Log validation errors
                foreach (var modelError in ModelState)
                {
                    if (modelError.Value.Errors.Count > 0)
                    {
                        Console.WriteLine($"[OnPostCreateOfficeAsync] Validation Error - Field: {modelError.Key}, Error: {string.Join(", ", modelError.Value.Errors.Select(e => e.ErrorMessage))}");
                    }
                }

                StatusMessage = "Error: Please check the form fields.";
                StatusMessageClass = "danger";
                Console.WriteLine($"[OnPostCreateOfficeAsync] ModelState Invalid. Organizations.Count = {Organizations?.Count ?? 0}");
                return Page();
            }

            // Check if office name already exists in the same organization
            var existingOffice = await _context.Offices
                .FirstOrDefaultAsync(o => o.Name.ToLower() == Input.Name.ToLower() &&
                                         o.OrganizationId == Input.OrganizationId);

            if (existingOffice != null)
            {
                StatusMessage = "Error: An office with this name already exists in the selected organization.";
                StatusMessageClass = "danger";
                return Page();
            }

            var office = new Office
            {
                Name = Input.Name,
                Code = Input.Code,
                Description = Input.Description,
                OrganizationId = Input.OrganizationId,
                CreatedDate = DateTime.UtcNow
            };

            _context.Offices.Add(office);
            await _context.SaveChangesAsync();

            StatusMessage = "Office created successfully.";
            Input = new OfficeInputModel(); // Reset form
            await LoadDataAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostEditOfficeAsync()
        {
            // CRITICAL: Load data FIRST before ModelState check
            await LoadDataAsync();

            // Remove all validation errors that are not related to Input (Office editing)
            var keysToRemove = ModelState.Keys.Where(k => !k.StartsWith("Input.")).ToList();
            foreach (var key in keysToRemove)
            {
                ModelState.Remove(key);
            }

            if (!ModelState.IsValid || !Input.Id.HasValue)
            {
                StatusMessage = "Error: Please check the form fields.";
                StatusMessageClass = "danger";
                return Page();
            }

            var office = await _context.Offices.FindAsync(Input.Id.Value);
            if (office == null)
            {
                StatusMessage = "Error: Office not found.";
                StatusMessageClass = "danger";
                return Page();
            }

            // Check if another office with this name exists in the same organization
            var existingOffice = await _context.Offices
                .FirstOrDefaultAsync(o => o.Name.ToLower() == Input.Name.ToLower() &&
                                         o.OrganizationId == Input.OrganizationId &&
                                         o.Id != Input.Id);

            if (existingOffice != null)
            {
                StatusMessage = "Error: Another office with this name already exists in the selected organization.";
                StatusMessageClass = "danger";
                return Page();
            }

            office.Name = Input.Name;
            office.Code = Input.Code;
            office.Description = Input.Description;
            office.OrganizationId = Input.OrganizationId;

            await _context.SaveChangesAsync();

            StatusMessage = "Office updated successfully.";
            Input = new OfficeInputModel(); // Reset form
            await LoadDataAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostDeleteOfficeAsync(int id)
        {
            var office = await _context.Offices
                .Include(o => o.Users)
                .Include(o => o.SubOffices)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (office == null)
            {
                StatusMessage = "Error: Office not found.";
                StatusMessageClass = "danger";
                await LoadDataAsync();
                return Page();
            }

            // Check if office has users
            if (office.Users.Any())
            {
                StatusMessage = "Error: Cannot delete office that has users assigned to it.";
                StatusMessageClass = "danger";
                await LoadDataAsync();
                return Page();
            }

            // Check if office has sub-offices
            if (office.SubOffices.Any())
            {
                StatusMessage = "Error: Cannot delete office that has sub-offices. Please delete or reassign sub-offices first.";
                StatusMessageClass = "danger";
                await LoadDataAsync();
                return Page();
            }

            _context.Offices.Remove(office);
            await _context.SaveChangesAsync();

            StatusMessage = "Office deleted successfully.";
            await LoadDataAsync();
            return Page();
        }

        public async Task<IActionResult> OnGetEditOfficeAsync(int id)
        {
            var office = await _context.Offices.FindAsync(id);
            if (office == null)
            {
                StatusMessage = "Error: Office not found.";
                StatusMessageClass = "danger";
                await LoadDataAsync();
                return Page();
            }

            Input = new OfficeInputModel
            {
                Id = office.Id,
                Name = office.Name,
                Code = office.Code,
                Description = office.Description,
                OrganizationId = office.OrganizationId
            };

            await LoadDataAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostCreateSubOfficeAsync()
        {
            // CRITICAL: Load data FIRST before ModelState check
            await LoadDataAsync();

            // Remove all validation errors that are not related to SubOfficeInput
            var keysToRemove = ModelState.Keys.Where(k => !k.StartsWith("SubOfficeInput.")).ToList();
            foreach (var key in keysToRemove)
            {
                ModelState.Remove(key);
            }

            if (!ModelState.IsValid)
            {
                StatusMessage = "Error: Please check the form fields.";
                StatusMessageClass = "danger";
                return Page();
            }

            // Check if sub-office name already exists in the same office
            var existingSubOffice = await _context.SubOffices
                .FirstOrDefaultAsync(s => s.Name.ToLower() == SubOfficeInput.Name.ToLower() &&
                                         s.OfficeId == SubOfficeInput.OfficeId);

            if (existingSubOffice != null)
            {
                StatusMessage = "Error: A sub-office with this name already exists in the selected office.";
                StatusMessageClass = "danger";
                return Page();
            }

            var subOffice = new SubOffice
            {
                Name = SubOfficeInput.Name,
                Code = SubOfficeInput.Code,
                Description = SubOfficeInput.Description,
                ContactPerson = SubOfficeInput.ContactPerson,
                PhoneNumber = SubOfficeInput.PhoneNumber,
                Email = SubOfficeInput.Email,
                Address = SubOfficeInput.Address,
                OfficeId = SubOfficeInput.OfficeId,
                IsActive = SubOfficeInput.IsActive,
                CreatedDate = DateTime.UtcNow
            };

            _context.SubOffices.Add(subOffice);
            await _context.SaveChangesAsync();

            StatusMessage = "Sub-office created successfully.";
            SubOfficeInput = new SubOfficeInputModel(); // Reset form
            await LoadDataAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostEditSubOfficeAsync()
        {
            // CRITICAL: Load data FIRST before ModelState check
            await LoadDataAsync();

            // Remove all validation errors that are not related to SubOfficeInput
            var keysToRemove = ModelState.Keys.Where(k => !k.StartsWith("SubOfficeInput.")).ToList();
            foreach (var key in keysToRemove)
            {
                ModelState.Remove(key);
            }

            if (!ModelState.IsValid || !SubOfficeInput.Id.HasValue)
            {
                StatusMessage = "Error: Please check the form fields.";
                StatusMessageClass = "danger";
                return Page();
            }

            var subOffice = await _context.SubOffices.FindAsync(SubOfficeInput.Id.Value);
            if (subOffice == null)
            {
                StatusMessage = "Error: Sub-office not found.";
                StatusMessageClass = "danger";
                return Page();
            }

            // Check if another sub-office with this name exists in the same office
            var existingSubOffice = await _context.SubOffices
                .FirstOrDefaultAsync(s => s.Name.ToLower() == SubOfficeInput.Name.ToLower() &&
                                         s.OfficeId == SubOfficeInput.OfficeId &&
                                         s.Id != SubOfficeInput.Id);

            if (existingSubOffice != null)
            {
                StatusMessage = "Error: Another sub-office with this name already exists in the selected office.";
                StatusMessageClass = "danger";
                return Page();
            }

            subOffice.Name = SubOfficeInput.Name;
            subOffice.Code = SubOfficeInput.Code;
            subOffice.Description = SubOfficeInput.Description;
            subOffice.ContactPerson = SubOfficeInput.ContactPerson;
            subOffice.PhoneNumber = SubOfficeInput.PhoneNumber;
            subOffice.Email = SubOfficeInput.Email;
            subOffice.Address = SubOfficeInput.Address;
            subOffice.OfficeId = SubOfficeInput.OfficeId;
            subOffice.IsActive = SubOfficeInput.IsActive;
            subOffice.ModifiedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            StatusMessage = "Sub-office updated successfully.";
            SubOfficeInput = new SubOfficeInputModel(); // Reset form
            await LoadDataAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostDeleteSubOfficeAsync(int id)
        {
            var subOffice = await _context.SubOffices
                .Include(s => s.Users)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (subOffice == null)
            {
                StatusMessage = "Error: Sub-office not found.";
                StatusMessageClass = "danger";
                await LoadDataAsync();
                return Page();
            }

            // Check if sub-office has users
            if (subOffice.Users.Any())
            {
                StatusMessage = "Error: Cannot delete sub-office that has users assigned to it.";
                StatusMessageClass = "danger";
                await LoadDataAsync();
                return Page();
            }

            _context.SubOffices.Remove(subOffice);
            await _context.SaveChangesAsync();

            StatusMessage = "Sub-office deleted successfully.";
            await LoadDataAsync();
            return Page();
        }

        public async Task<IActionResult> OnGetEditSubOfficeAsync(int id)
        {
            var subOffice = await _context.SubOffices.FindAsync(id);
            if (subOffice == null)
            {
                StatusMessage = "Error: Sub-office not found.";
                StatusMessageClass = "danger";
                await LoadDataAsync();
                return Page();
            }

            SubOfficeInput = new SubOfficeInputModel
            {
                Id = subOffice.Id,
                Name = subOffice.Name,
                Code = subOffice.Code,
                Description = subOffice.Description,
                ContactPerson = subOffice.ContactPerson,
                PhoneNumber = subOffice.PhoneNumber,
                Email = subOffice.Email,
                Address = subOffice.Address,
                OfficeId = subOffice.OfficeId,
                IsActive = subOffice.IsActive
            };

            await LoadDataAsync();
            return Page();
        }

        private async Task LoadDataAsync()
        {
            // Load Organizations for dropdown - ALWAYS load this
            var orgsQuery = _context.Organizations.OrderBy(o => o.Name);
            var orgsList = await orgsQuery.ToListAsync();

            Organizations = orgsList.Select(o => new SelectListItem
            {
                Value = o.Id.ToString(),
                Text = o.Name
            }).ToList();

            // Debug logging
            Console.WriteLine($"[LoadDataAsync] Loaded {Organizations.Count} organizations");

            // Build query with filters for offices
            var query = _context.Offices
                .Include(o => o.Organization)
                .Include(o => o.Users)
                .Include(o => o.SubOffices)
                .AsQueryable();

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var searchLower = SearchTerm.ToLower();
                query = query.Where(o =>
                    o.Name.ToLower().Contains(searchLower) ||
                    (o.Description != null && o.Description.ToLower().Contains(searchLower)) ||
                    o.Organization.Name.ToLower().Contains(searchLower));
            }

            // Apply organization filter
            if (SelectedOrganizationId.HasValue)
            {
                query = query.Where(o => o.OrganizationId == SelectedOrganizationId.Value);
            }

            // Apply has users filter
            if (HasUsers.HasValue)
            {
                if (HasUsers.Value)
                    query = query.Where(o => o.Users.Any());
                else
                    query = query.Where(o => !o.Users.Any());
            }

            // Calculate total count for pagination
            TotalRecords = await query.CountAsync();
            TotalPages = (int)Math.Ceiling(TotalRecords / (double)PageSize);

            // Ensure current page is valid
            if (PageNumber < 1) PageNumber = 1;
            if (PageNumber > TotalPages && TotalPages > 0) PageNumber = TotalPages;

            // Apply pagination
            Offices = await query
                .OrderBy(o => o.Organization.Name)
                .ThenBy(o => o.Name)
                .Skip((PageNumber - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            // Load offices for SubOffice dropdown
            OfficesList = await _context.Offices
                .OrderBy(o => o.Name)
                .Select(o => new SelectListItem
                {
                    Value = o.Id.ToString(),
                    Text = $"{o.Name} ({o.Organization.Name})"
                })
                .ToListAsync();

            // Calculate statistics (from full dataset, not just current page)
            var statsQuery = _context.Offices
                .Include(o => o.Organization)
                .Include(o => o.Users)
                .Include(o => o.SubOffices)
                .AsQueryable();

            TotalOffices = await statsQuery.CountAsync();
            TotalOrganizations = await statsQuery.Select(o => o.OrganizationId).Distinct().CountAsync();
            TotalUsers = await statsQuery.SelectMany(o => o.Users).CountAsync();
            ActiveOffices = await statsQuery.CountAsync(o => o.Users.Any());
            TotalSubOffices = await statsQuery.SelectMany(o => o.SubOffices).CountAsync();

            // Load all sub-offices
            SubOffices = await _context.SubOffices
                .Include(s => s.Office)
                    .ThenInclude(o => o.Organization)
                .Include(s => s.Users)
                .OrderBy(s => s.Office.Name)
                .ThenBy(s => s.Name)
                .ToListAsync();
        }
    }
}