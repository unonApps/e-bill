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
    public class ClassOfServiceModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ClassOfServiceModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public List<ClassOfService> ClassOfServices { get; set; } = new List<ClassOfService>();
        public string CurrentUserName { get; set; } = string.Empty;

        [TempData]
        public string? StatusMessage { get; set; }

        [TempData]
        public string? StatusMessageClass { get; set; }

        // Statistics
        public int TotalServices { get; set; }
        public int ActiveServices { get; set; }
        public int InactiveServices { get; set; }
        public int UniqueClasses { get; set; }

        // Line Statistics
        public int TotalLines { get; set; }
        public int ActiveLines { get; set; }
        public int InactiveLines { get; set; }

        // Usage counts dictionary - ClassOfServiceId -> (TotalPhones, ActivePhones)
        public Dictionary<int, (int Total, int Active)> PhoneUsageCounts { get; set; } = new Dictionary<int, (int, int)>();
        
        // Filter properties
        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }
        
        [BindProperty(SupportsGet = true)]
        public string? SelectedClass { get; set; }
        
        [BindProperty(SupportsGet = true)]
        public ServiceStatus? SelectedStatus { get; set; }
        
        // Available classes for filter dropdown
        public List<string> AvailableClasses { get; set; } = new List<string>();

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

        public async Task<IActionResult> OnPostCreateAsync(string classType, string service, string eligibleStaff,
            string? airtimeAllowance, string? dataAllowance, string? handsetAllowance, string? handsetAIRemarks, ServiceStatus serviceStatus,
            decimal? airtimeAllowanceAmount, decimal? dataAllowanceAmount, decimal? handsetAllowanceAmount, string billingPeriod = "Monthly")
        {
            try
            {
                // Validation
                if (string.IsNullOrWhiteSpace(classType))
                {
                    StatusMessage = "Class is required.";
                    StatusMessageClass = "danger";
                    await LoadPageDataAsync();
                    return Page();
                }

                if (string.IsNullOrWhiteSpace(service))
                {
                    StatusMessage = "Service is required.";
                    StatusMessageClass = "danger";
                    await LoadPageDataAsync();
                    return Page();
                }

                if (string.IsNullOrWhiteSpace(eligibleStaff))
                {
                    StatusMessage = "Eligible Staff is required.";
                    StatusMessageClass = "danger";
                    await LoadPageDataAsync();
                    return Page();
                }

                // Create new class of service
                var classOfService = new ClassOfService
                {
                    Class = classType.Trim(),
                    Service = service.Trim(),
                    EligibleStaff = eligibleStaff.Trim(),
                    AirtimeAllowance = string.IsNullOrWhiteSpace(airtimeAllowance) ? null : airtimeAllowance.Trim(),
                    DataAllowance = string.IsNullOrWhiteSpace(dataAllowance) ? null : dataAllowance.Trim(),
                    HandsetAllowance = string.IsNullOrWhiteSpace(handsetAllowance) ? null : handsetAllowance.Trim(),
                    HandsetAIRemarks = string.IsNullOrWhiteSpace(handsetAIRemarks) ? null : handsetAIRemarks.Trim(),
                    ServiceStatus = serviceStatus,
                    // Numeric fields for calculations
                    AirtimeAllowanceAmount = airtimeAllowanceAmount,
                    DataAllowanceAmount = dataAllowanceAmount,
                    HandsetAllowanceAmount = handsetAllowanceAmount,
                    BillingPeriod = string.IsNullOrWhiteSpace(billingPeriod) ? "Monthly" : billingPeriod,
                    CreatedDate = DateTime.UtcNow
                };

                _context.ClassOfServices.Add(classOfService);
                await _context.SaveChangesAsync();

                StatusMessage = "Class of Service created successfully.";
                StatusMessageClass = "success";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error creating Class of Service: {ex.Message}";
                StatusMessageClass = "danger";
            }

            await LoadPageDataAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostEditAsync(int id, string classType, string service, string eligibleStaff,
            string? airtimeAllowance, string? dataAllowance, string? handsetAllowance, string? handsetAIRemarks, ServiceStatus serviceStatus,
            decimal? airtimeAllowanceAmount, decimal? dataAllowanceAmount, decimal? handsetAllowanceAmount, string billingPeriod = "Monthly")
        {
            try
            {
                var classOfService = await _context.ClassOfServices.FindAsync(id);
                if (classOfService == null)
                {
                    StatusMessage = "Class of Service not found.";
                    StatusMessageClass = "danger";
                    await LoadPageDataAsync();
                    return Page();
                }

                // Check if trying to deactivate a CoS that has active lines
                if (classOfService.ServiceStatus == ServiceStatus.Active && serviceStatus == ServiceStatus.Inactive)
                {
                    var activeLines = await _context.UserPhones
                        .CountAsync(up => up.ClassOfServiceId == id && up.Status == PhoneStatus.Active);

                    if (activeLines > 0)
                    {
                        StatusMessage = $"Cannot deactivate this Class of Service. It has {activeLines} active line{(activeLines != 1 ? "s" : "")} assigned. " +
                                        "Please reassign or deactivate these lines first.";
                        StatusMessageClass = "warning";
                        await LoadPageDataAsync();
                        return Page();
                    }
                }

                // Validation
                if (string.IsNullOrWhiteSpace(classType))
                {
                    StatusMessage = "Class is required.";
                    StatusMessageClass = "danger";
                    await LoadPageDataAsync();
                    return Page();
                }

                if (string.IsNullOrWhiteSpace(service))
                {
                    StatusMessage = "Service is required.";
                    StatusMessageClass = "danger";
                    await LoadPageDataAsync();
                    return Page();
                }

                if (string.IsNullOrWhiteSpace(eligibleStaff))
                {
                    StatusMessage = "Eligible Staff is required.";
                    StatusMessageClass = "danger";
                    await LoadPageDataAsync();
                    return Page();
                }

                // Update class of service
                classOfService.Class = classType.Trim();
                classOfService.Service = service.Trim();
                classOfService.EligibleStaff = eligibleStaff.Trim();
                classOfService.AirtimeAllowance = string.IsNullOrWhiteSpace(airtimeAllowance) ? null : airtimeAllowance.Trim();
                classOfService.DataAllowance = string.IsNullOrWhiteSpace(dataAllowance) ? null : dataAllowance.Trim();
                classOfService.HandsetAllowance = string.IsNullOrWhiteSpace(handsetAllowance) ? null : handsetAllowance.Trim();
                classOfService.HandsetAIRemarks = string.IsNullOrWhiteSpace(handsetAIRemarks) ? null : handsetAIRemarks.Trim();
                classOfService.ServiceStatus = serviceStatus;
                // Update numeric fields for calculations
                classOfService.AirtimeAllowanceAmount = airtimeAllowanceAmount;
                classOfService.DataAllowanceAmount = dataAllowanceAmount;
                classOfService.HandsetAllowanceAmount = handsetAllowanceAmount;
                classOfService.BillingPeriod = string.IsNullOrWhiteSpace(billingPeriod) ? "Monthly" : billingPeriod;

                await _context.SaveChangesAsync();

                StatusMessage = "Class of Service updated successfully.";
                StatusMessageClass = "success";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error updating Class of Service: {ex.Message}";
                StatusMessageClass = "danger";
            }

            await LoadPageDataAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            try
            {
                var classOfService = await _context.ClassOfServices.FindAsync(id);
                if (classOfService == null)
                {
                    StatusMessage = "Class of Service not found.";
                    StatusMessageClass = "danger";
                    await LoadPageDataAsync();
                    return Page();
                }

                // Check if any UserPhones are using this ClassOfService
                var totalPhones = await _context.UserPhones
                    .CountAsync(up => up.ClassOfServiceId == id);

                var activePhones = await _context.UserPhones
                    .CountAsync(up => up.ClassOfServiceId == id && up.Status == PhoneStatus.Active);

                if (totalPhones > 0)
                {
                    var lineDetails = activePhones > 0
                        ? $"{activePhones} active line{(activePhones != 1 ? "s" : "")} and {totalPhones - activePhones} inactive line{(totalPhones - activePhones != 1 ? "s" : "")}"
                        : $"{totalPhones} line{(totalPhones != 1 ? "s" : "")}";

                    StatusMessage = $"Cannot delete this Class of Service. It is currently assigned to {lineDetails}. " +
                                    "Please reassign or remove these lines first.";
                    StatusMessageClass = "warning";
                    await LoadPageDataAsync();
                    return Page();
                }

                _context.ClassOfServices.Remove(classOfService);
                await _context.SaveChangesAsync();

                StatusMessage = "Class of Service deleted successfully.";
                StatusMessageClass = "success";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error deleting Class of Service: {ex.Message}";
                StatusMessageClass = "danger";
            }

            await LoadPageDataAsync();
            return Page();
        }

        private async Task LoadPageDataAsync()
        {
            // Build query with filters
            var query = _context.ClassOfServices.AsQueryable();

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var searchLower = SearchTerm.ToLower();
                query = query.Where(c =>
                    c.Class.ToLower().Contains(searchLower) ||
                    c.Service.ToLower().Contains(searchLower) ||
                    c.EligibleStaff.ToLower().Contains(searchLower) ||
                    (c.AirtimeAllowance != null && c.AirtimeAllowance.ToLower().Contains(searchLower)) ||
                    (c.DataAllowance != null && c.DataAllowance.ToLower().Contains(searchLower)) ||
                    (c.HandsetAllowance != null && c.HandsetAllowance.ToLower().Contains(searchLower)) ||
                    (c.HandsetAIRemarks != null && c.HandsetAIRemarks.ToLower().Contains(searchLower)));
            }

            // Apply class filter
            if (!string.IsNullOrWhiteSpace(SelectedClass))
            {
                query = query.Where(c => c.Class == SelectedClass);
            }

            // Apply status filter
            if (SelectedStatus.HasValue)
            {
                query = query.Where(c => c.ServiceStatus == SelectedStatus.Value);
            }

            ClassOfServices = await query
                .OrderBy(c => c.Class)
                .ThenBy(c => c.Service)
                .ToListAsync();

            // Calculate statistics
            TotalServices = ClassOfServices.Count;
            ActiveServices = ClassOfServices.Count(c => c.ServiceStatus == ServiceStatus.Active);
            InactiveServices = ClassOfServices.Count(c => c.ServiceStatus == ServiceStatus.Inactive);
            UniqueClasses = ClassOfServices.Select(c => c.Class).Distinct().Count();

            // Get available classes for filter dropdown
            AvailableClasses = await _context.ClassOfServices
                .Select(c => c.Class)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            // Calculate phone usage counts for each ClassOfService
            var phoneUsages = await _context.UserPhones
                .Where(up => up.ClassOfServiceId != null)
                .GroupBy(up => up.ClassOfServiceId!.Value)
                .Select(g => new
                {
                    ClassOfServiceId = g.Key,
                    TotalPhones = g.Count(),
                    ActivePhones = g.Count(up => up.Status == PhoneStatus.Active)
                })
                .ToListAsync();

            PhoneUsageCounts = phoneUsages.ToDictionary(
                p => p.ClassOfServiceId,
                p => (p.TotalPhones, p.ActivePhones)
            );

            // Calculate total line statistics
            TotalLines = phoneUsages.Sum(p => p.TotalPhones);
            ActiveLines = phoneUsages.Sum(p => p.ActivePhones);
            InactiveLines = TotalLines - ActiveLines;
        }
    }
} 