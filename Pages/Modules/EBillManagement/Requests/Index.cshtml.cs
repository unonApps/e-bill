using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TAB.Web.Data;
using TAB.Web.Models;

namespace TAB.Web.Pages.Modules.EBillManagement.Requests
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public IndexModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [BindProperty]
        public Ebill Ebill { get; set; } = new();

        public List<SelectListItem> ServiceProviders { get; set; } = new();
        public List<SelectListItem> BillTypes { get; set; } = new();

        [TempData]
        public string? StatusMessage { get; set; }

        [TempData]
        public string? StatusMessageClass { get; set; }

        public async Task OnGetAsync()
        {
            await LoadDropdownsAsync();
            await PopulateUserInfoAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadDropdownsAsync();
                return Page();
            }

            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Challenge();
                }

                Ebill.RequestedBy = user.Id;
                Ebill.RequestDate = DateTime.UtcNow;
                Ebill.Status = EbillStatus.Draft;

                _context.Ebills.Add(Ebill);
                await _context.SaveChangesAsync();

                StatusMessage = "Your E-bill request has been submitted successfully. You will be notified once it's processed.";
                StatusMessageClass = "success";

                return RedirectToPage();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error submitting request: {ex.Message}";
                StatusMessageClass = "danger";
                await LoadDropdownsAsync();
                return Page();
            }
        }

        private async Task LoadDropdownsAsync()
        {
            // Load Service Providers
            ServiceProviders = await _context.ServiceProviders
                .Where(sp => sp.SPStatus == ServiceProviderStatus.Active)
                .Select(sp => new SelectListItem
                {
                    Value = sp.Id.ToString(),
                    Text = sp.ServiceProviderName
                })
                .ToListAsync();

            // Load Bill Types
            BillTypes = Enum.GetValues<BillType>()
                .Select(bt => new SelectListItem
                {
                    Value = ((int)bt).ToString(),
                    Text = bt.ToString().Replace("_", " ")
                })
                .ToList();
        }

        private async Task PopulateUserInfoAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                Ebill.FullName = $"{user.FirstName} {user.LastName}";
                Ebill.Email = user.Email ?? string.Empty;
                Ebill.PhoneNumber = user.PhoneNumber ?? string.Empty;
                
                // Try to get department from user's office or organization
                if (user.Office != null)
                {
                    Ebill.Department = user.Office.Name;
                }
                else if (user.Organization != null)
                {
                    Ebill.Department = user.Organization.Name;
                }
            }
        }
    }
} 