using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TAB.Web.Data;
using TAB.Web.Models;

namespace TAB.Web.Pages.Modules.SimManagement.Requests
{
    [Authorize]
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public EditModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [BindProperty]
        public Models.SimRequest? SimRequest { get; set; }

        public List<SelectListItem> ServiceProviders { get; set; } = new();
        public List<SelectListItem> Organizations { get; set; } = new();
        public List<SelectListItem> Offices { get; set; } = new();
        public List<SelectListItem> Supervisors { get; set; } = new();

        public string? StatusMessage { get; set; }
        public string? StatusMessageClass { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            await LoadDropdownDataAsync();

            SimRequest = await _context.SimRequests
                .Include(r => r.ServiceProvider)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (SimRequest == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (SimRequest.RequestedBy != currentUser?.Id)
            {
                return Forbid();
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string action)
        {
            await LoadDropdownDataAsync();

            if (SimRequest == null)
            {
                return NotFound();
            }

            var existingRequest = await _context.SimRequests
                .FirstOrDefaultAsync(r => r.Id == SimRequest.Id);

            if (existingRequest == null)
            {
                StatusMessage = "Request not found.";
                StatusMessageClass = "danger";
                return Page();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (existingRequest.RequestedBy != currentUser?.Id)
            {
                StatusMessage = "You can only edit your own requests.";
                StatusMessageClass = "danger";
                return Page();
            }

            if (existingRequest.Status != RequestStatus.Draft)
            {
                StatusMessage = "Only draft requests can be edited.";
                StatusMessageClass = "warning";
                return Page();
            }

            if (!ModelState.IsValid)
            {
                StatusMessage = "Please correct the errors below.";
                StatusMessageClass = "danger";
                SimRequest = existingRequest;
                return Page();
            }

            // Note: Multiple SIM requests are allowed for the same Index Number
            // as one staff member can have multiple SIM cards (e.g., personal, official)

            // Update request properties
            existingRequest.IndexNo = SimRequest.IndexNo?.Trim();
            existingRequest.FirstName = SimRequest.FirstName?.Trim();
            existingRequest.LastName = SimRequest.LastName?.Trim();
            existingRequest.Organization = SimRequest.Organization?.Trim();
            existingRequest.Office = SimRequest.Office?.Trim();
            existingRequest.Grade = SimRequest.Grade?.Trim();
            existingRequest.FunctionalTitle = SimRequest.FunctionalTitle?.Trim();
            existingRequest.OfficeExtension = SimRequest.OfficeExtension?.Trim();
            existingRequest.OfficialEmail = SimRequest.OfficialEmail?.Trim();
            existingRequest.SimType = SimRequest.SimType;
            existingRequest.ServiceProviderId = SimRequest.ServiceProviderId;
            existingRequest.Supervisor = SimRequest.Supervisor?.Trim();
            existingRequest.PreviouslyAssignedLines = SimRequest.PreviouslyAssignedLines?.Trim();
            existingRequest.Remarks = SimRequest.Remarks?.Trim();

            // Get supervisor details from the selected supervisor email
            var supervisorUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == SimRequest.Supervisor);

            // Set supervisor details if found
            if (supervisorUser != null)
            {
                existingRequest.SupervisorEmail = supervisorUser.Email ?? string.Empty;
                existingRequest.SupervisorName = $"{supervisorUser.FirstName ?? ""} {supervisorUser.LastName ?? ""}".Trim();
                // Keep the email in the Supervisor field for backward compatibility
                existingRequest.Supervisor = supervisorUser.Email ?? string.Empty;
            }

            // Update status if submitting
            if (action == "submit")
            {
                existingRequest.Status = RequestStatus.PendingSupervisor;
                existingRequest.SubmittedToSupervisor = true;
            }

            try
            {
                await _context.SaveChangesAsync();

                StatusMessage = action == "submit" 
                    ? "Your SIM request has been updated and submitted successfully."
                    : "Your SIM request has been updated successfully.";
                StatusMessageClass = "success";

                return RedirectToPage("./Index");
            }
            catch (Exception)
            {
                StatusMessage = "An error occurred while updating your request. Please try again.";
                StatusMessageClass = "danger";
                SimRequest = existingRequest;
                return Page();
            }
        }

        private async Task LoadDropdownDataAsync()
        {
            // Load Service Providers
            ServiceProviders = await _context.ServiceProviders
                .Where(sp => sp.SPStatus == ServiceProviderStatus.Active)
                .Select(sp => new SelectListItem
                {
                    Value = sp.Id.ToString(),
                    Text = sp.ServiceProviderName ?? "Unknown Provider"
                })
                .ToListAsync();

            // Load Organizations
            Organizations = await _context.Organizations
                .Select(o => new SelectListItem
                {
                    Value = o.Name,
                    Text = o.Name ?? "Unknown Organization"
                })
                .ToListAsync();

            // Load Offices
            Offices = await _context.Offices
                .Include(o => o.Organization)
                .Select(o => new SelectListItem
                {
                    Value = o.Name,
                    Text = o.Name ?? "Unknown Office"
                })
                .ToListAsync();

            // Load Active Users as Supervisors (excluding current user)
            var currentUserId = _userManager.GetUserId(User) ?? string.Empty;
            Supervisors = await _context.Users
                .Where(u => u.Status == UserStatus.Active && u.Id != currentUserId)
                .Select(u => new SelectListItem
                {
                    Value = u.Email ?? string.Empty,
                    Text = $"{u.FirstName ?? ""} {u.LastName ?? ""} ({u.Email ?? ""})".Trim()
                })
                .ToListAsync();
        }
    }
} 