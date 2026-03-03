using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TAB.Web.Data;
using TAB.Web.Models;

namespace TAB.Web.Pages.Admin
{
    [Authorize(Roles = "Admin,Agency Focal Point")]
    public class ServiceProviderModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ServiceProviderModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public List<Models.ServiceProvider> ServiceProviders { get; set; } = new();
        public string CurrentUserName { get; set; } = string.Empty;

        [TempData]
        public string? StatusMessage { get; set; }

        [TempData]
        public string? StatusMessageClass { get; set; }
        
        // Statistics
        public int TotalProviders { get; set; }
        public int ActiveProviders { get; set; }
        public int InactiveProviders { get; set; }
        public int ContactPersons { get; set; }
        
        // Filter properties
        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }
        
        [BindProperty(SupportsGet = true)]
        public ServiceProviderStatus? SelectedStatus { get; set; }

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
            
            await LoadPageDataAsync();
        }
        
        private async Task LoadPageDataAsync()
        {
            // Build query with filters
            var query = _context.ServiceProviders.AsQueryable();
            
            // Apply search filter
            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var searchLower = SearchTerm.ToLower();
                query = query.Where(sp => 
                    sp.SPID.ToLower().Contains(searchLower) ||
                    sp.ServiceProviderName.ToLower().Contains(searchLower) ||
                    sp.SPMainCP.ToLower().Contains(searchLower) ||
                    sp.SPMainCPEmail.ToLower().Contains(searchLower) ||
                    (sp.SPOtherCPsEmail != null && sp.SPOtherCPsEmail.ToLower().Contains(searchLower)));
            }
            
            // Apply status filter
            if (SelectedStatus.HasValue)
            {
                query = query.Where(sp => sp.SPStatus == SelectedStatus.Value);
            }
            
            ServiceProviders = await query
                .OrderBy(sp => sp.SPID)
                .ThenBy(sp => sp.ServiceProviderName)
                .ToListAsync();
            
            // Calculate statistics
            TotalProviders = ServiceProviders.Count;
            ActiveProviders = ServiceProviders.Count(sp => sp.SPStatus == ServiceProviderStatus.Active);
            InactiveProviders = ServiceProviders.Count(sp => sp.SPStatus == ServiceProviderStatus.Inactive);
            ContactPersons = ServiceProviders.Count(sp => !string.IsNullOrEmpty(sp.SPOtherCPsEmail)) + ServiceProviders.Count;
        }

        public async Task<IActionResult> OnPostCreateAsync(string spid, string serviceProviderName, 
            string spMainCP, string spMainCPEmail, string? spOtherCPsEmail, int spStatus)
        {
            try
            {
                // Validate required fields
                if (string.IsNullOrWhiteSpace(spid) || string.IsNullOrWhiteSpace(serviceProviderName) ||
                    string.IsNullOrWhiteSpace(spMainCP) || string.IsNullOrWhiteSpace(spMainCPEmail))
                {
                    StatusMessage = "All required fields must be filled.";
                    StatusMessageClass = "danger";
                    await LoadPageDataAsync();
                    return Page();
                }

                // Check if SPID already exists
                var existingSP = await _context.ServiceProviders
                    .FirstOrDefaultAsync(sp => sp.SPID == spid.Trim());
                
                if (existingSP != null)
                {
                    StatusMessage = $"Service Provider with SPID '{spid}' already exists.";
                    StatusMessageClass = "danger";
                    await LoadPageDataAsync();
                    return Page();
                }

                var serviceProvider = new Models.ServiceProvider
                {
                    SPID = spid.Trim(),
                    ServiceProviderName = serviceProviderName.Trim(),
                    SPMainCP = spMainCP.Trim(),
                    SPMainCPEmail = spMainCPEmail.Trim(),
                    SPOtherCPsEmail = string.IsNullOrWhiteSpace(spOtherCPsEmail) ? null : spOtherCPsEmail.Trim(),
                    SPStatus = (ServiceProviderStatus)spStatus,
                    CreatedDate = DateTime.UtcNow
                };

                _context.ServiceProviders.Add(serviceProvider);
                await _context.SaveChangesAsync();

                StatusMessage = $"Service Provider '{serviceProviderName}' has been created successfully.";
                StatusMessageClass = "success";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error creating service provider: {ex.Message}";
                StatusMessageClass = "danger";
            }

            await LoadPageDataAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostEditAsync(int id, string spid, string serviceProviderName,
            string spMainCP, string spMainCPEmail, string? spOtherCPsEmail, int spStatus)
        {
            try
            {
                // Validate required fields
                if (string.IsNullOrWhiteSpace(spid) || string.IsNullOrWhiteSpace(serviceProviderName) ||
                    string.IsNullOrWhiteSpace(spMainCP) || string.IsNullOrWhiteSpace(spMainCPEmail))
                {
                    StatusMessage = "All required fields must be filled.";
                    StatusMessageClass = "danger";
                    await LoadPageDataAsync();
                    return Page();
                }

                var serviceProvider = await _context.ServiceProviders.FindAsync(id);
                if (serviceProvider == null)
                {
                    StatusMessage = "Service Provider not found.";
                    StatusMessageClass = "danger";
                    await LoadPageDataAsync();
                    return Page();
                }

                // Check if SPID already exists (excluding current record)
                var existingSP = await _context.ServiceProviders
                    .FirstOrDefaultAsync(sp => sp.SPID == spid.Trim() && sp.Id != id);
                
                if (existingSP != null)
                {
                    StatusMessage = $"Service Provider with SPID '{spid}' already exists.";
                    StatusMessageClass = "danger";
                    await LoadPageDataAsync();
                    return Page();
                }

                serviceProvider.SPID = spid.Trim();
                serviceProvider.ServiceProviderName = serviceProviderName.Trim();
                serviceProvider.SPMainCP = spMainCP.Trim();
                serviceProvider.SPMainCPEmail = spMainCPEmail.Trim();
                serviceProvider.SPOtherCPsEmail = string.IsNullOrWhiteSpace(spOtherCPsEmail) ? null : spOtherCPsEmail.Trim();
                serviceProvider.SPStatus = (ServiceProviderStatus)spStatus;

                await _context.SaveChangesAsync();

                StatusMessage = $"Service Provider '{serviceProviderName}' has been updated successfully.";
                StatusMessageClass = "success";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error updating service provider: {ex.Message}";
                StatusMessageClass = "danger";
            }

            await LoadPageDataAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            try
            {
                var serviceProvider = await _context.ServiceProviders.FindAsync(id);
                if (serviceProvider == null)
                {
                    StatusMessage = "Service Provider not found.";
                    StatusMessageClass = "danger";
                    await LoadPageDataAsync();
                    return Page();
                }

                _context.ServiceProviders.Remove(serviceProvider);
                await _context.SaveChangesAsync();

                StatusMessage = $"Service Provider '{serviceProvider.ServiceProviderName}' has been deleted successfully.";
                StatusMessageClass = "success";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error deleting service provider: {ex.Message}";
                StatusMessageClass = "danger";
            }

            await LoadPageDataAsync();
            return Page();
        }
    }
} 