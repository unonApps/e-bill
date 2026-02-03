using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TAB.Web.Data;
using TAB.Web.Models;
using System.ComponentModel.DataAnnotations;

namespace TAB.Web.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class OrganizationsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public OrganizationsModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public List<Organization> Organizations { get; set; } = new List<Organization>();
        public string StatusMessage { get; set; } = string.Empty;
        public string StatusMessageClass { get; set; } = "success";
        public string CurrentUserName { get; set; } = string.Empty;
        
        // Statistics
        public int TotalOrganizations { get; set; }
        public int TotalOffices { get; set; }
        public int TotalUsers { get; set; }
        public int ActiveOrganizations { get; set; }
        public int TotalSubOffices { get; set; }
        
        // Filter properties
        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool? HasOffices { get; set; }

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
        public OrganizationInputModel Input { get; set; } = new OrganizationInputModel();

        public class OrganizationInputModel
        {
            public int? Id { get; set; }

            [Required]
            [StringLength(100)]
            public string Name { get; set; } = string.Empty;

            [StringLength(10)]
            public string? Code { get; set; }

            [StringLength(500)]
            public string? Description { get; set; }
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

        public async Task<IActionResult> OnPostCreateAsync()
        {
            if (!ModelState.IsValid)
            {
                StatusMessage = "Error: Please check the form fields.";
                StatusMessageClass = "danger";
                await LoadDataAsync();
                return Page();
            }

            // Check if organization name already exists
            var existingOrg = await _context.Organizations
                .FirstOrDefaultAsync(o => o.Name.ToLower() == Input.Name.ToLower());
            
            if (existingOrg != null)
            {
                StatusMessage = "Error: An organization with this name already exists.";
                StatusMessageClass = "danger";
                await LoadDataAsync();
                return Page();
            }

            var organization = new Organization
            {
                Name = Input.Name,
                Code = Input.Code,
                Description = Input.Description,
                CreatedDate = DateTime.UtcNow
            };

            _context.Organizations.Add(organization);
            await _context.SaveChangesAsync();

            StatusMessage = "Organization created successfully.";
            Input = new OrganizationInputModel(); // Reset form
            await LoadDataAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostEditAsync()
        {
            if (!ModelState.IsValid || !Input.Id.HasValue)
            {
                StatusMessage = "Error: Please check the form fields.";
                StatusMessageClass = "danger";
                await LoadDataAsync();
                return Page();
            }

            var organization = await _context.Organizations.FindAsync(Input.Id.Value);
            if (organization == null)
            {
                StatusMessage = "Error: Organization not found.";
                StatusMessageClass = "danger";
                await LoadDataAsync();
                return Page();
            }

            // Check if another organization with this name exists
            var existingOrg = await _context.Organizations
                .FirstOrDefaultAsync(o => o.Name.ToLower() == Input.Name.ToLower() && o.Id != Input.Id);
            
            if (existingOrg != null)
            {
                StatusMessage = "Error: Another organization with this name already exists.";
                StatusMessageClass = "danger";
                await LoadDataAsync();
                return Page();
            }

            organization.Name = Input.Name;
            organization.Code = Input.Code;
            organization.Description = Input.Description;

            await _context.SaveChangesAsync();

            StatusMessage = "Organization updated successfully.";
            Input = new OrganizationInputModel(); // Reset form
            await LoadDataAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var organization = await _context.Organizations
                .Include(o => o.Offices)
                .Include(o => o.Users)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (organization == null)
            {
                StatusMessage = "Error: Organization not found.";
                StatusMessageClass = "danger";
                await LoadDataAsync();
                return Page();
            }

            // Check if organization has ApplicationUsers
            if (organization.Users.Any())
            {
                StatusMessage = $"Error: Cannot delete organization. It has {organization.Users.Count} system user(s) assigned to it.";
                StatusMessageClass = "danger";
                await LoadDataAsync();
                return Page();
            }

            // Check if organization has offices
            if (organization.Offices.Any())
            {
                StatusMessage = $"Error: Cannot delete organization. It has {organization.Offices.Count} office(s) assigned to it.";
                StatusMessageClass = "danger";
                await LoadDataAsync();
                return Page();
            }

            // Check if organization has EbillUsers
            var ebillUserCount = await _context.EbillUsers.CountAsync(u => u.OrganizationId == id);
            if (ebillUserCount > 0)
            {
                StatusMessage = $"Error: Cannot delete organization. It has {ebillUserCount} E-Bill user(s) assigned to it.";
                StatusMessageClass = "danger";
                await LoadDataAsync();
                return Page();
            }

            _context.Organizations.Remove(organization);
            await _context.SaveChangesAsync();

            StatusMessage = "Organization deleted successfully.";
            await LoadDataAsync();
            return Page();
        }

        public async Task<IActionResult> OnGetEditAsync(int id)
        {
            var organization = await _context.Organizations.FindAsync(id);
            if (organization == null)
            {
                StatusMessage = "Error: Organization not found.";
                StatusMessageClass = "danger";
                await LoadDataAsync();
                return Page();
            }

            Input = new OrganizationInputModel
            {
                Id = organization.Id,
                Name = organization.Name,
                Code = organization.Code,
                Description = organization.Description
            };

            await LoadDataAsync();
            return Page();
        }

        private async Task LoadDataAsync()
        {
            // Build query with filters
            var query = _context.Organizations
                .Include(o => o.Offices)
                    .ThenInclude(office => office.SubOffices)
                .Include(o => o.Users)
                .AsQueryable();
            
            // Apply search filter
            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var searchLower = SearchTerm.ToLower();
                query = query.Where(o => 
                    o.Name.ToLower().Contains(searchLower) ||
                    (o.Description != null && o.Description.ToLower().Contains(searchLower)));
            }
            
            // Apply has offices filter
            if (HasOffices.HasValue)
            {
                if (HasOffices.Value)
                    query = query.Where(o => o.Offices.Any());
                else
                    query = query.Where(o => !o.Offices.Any());
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
            Organizations = await query
                .OrderBy(o => o.Name)
                .Skip((PageNumber - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            // Calculate statistics (from full dataset, not just current page)
            var statsQuery = _context.Organizations
                .Include(o => o.Offices)
                    .ThenInclude(office => office.SubOffices)
                .Include(o => o.Users)
                .AsQueryable();

            TotalOrganizations = await statsQuery.CountAsync();
            TotalOffices = await statsQuery.SelectMany(o => o.Offices).CountAsync();
            TotalUsers = await statsQuery.SelectMany(o => o.Users).CountAsync();
            ActiveOrganizations = await statsQuery.CountAsync(o => o.Offices.Any() || o.Users.Any());
            TotalSubOffices = await statsQuery
                .SelectMany(o => o.Offices)
                .SelectMany(office => office.SubOffices)
                .CountAsync();
        }
    }
} 