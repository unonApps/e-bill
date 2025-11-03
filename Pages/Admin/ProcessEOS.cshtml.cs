using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TAB.Web.Data;
using TAB.Web.Models;

namespace TAB.Web.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class ProcessEOSModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ProcessEOSModel> _logger;

        public ProcessEOSModel(
            ApplicationDbContext context,
            ILogger<ProcessEOSModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        [BindProperty(SupportsGet = true)]
        public string IndexNumber { get; set; } = string.Empty;

        public EbillUser? StaffMember { get; set; }
        public List<UserPhone> AssignedPhones { get; set; } = new();

        [TempData]
        public string? SuccessMessage { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (string.IsNullOrEmpty(IndexNumber))
            {
                ErrorMessage = "Staff index number is required.";
                return RedirectToPage("/Admin/EOSRecovery");
            }

            await LoadStaffDataAsync();

            if (StaffMember == null)
            {
                ErrorMessage = $"Staff member with index number {IndexNumber} not found.";
                return RedirectToPage("/Admin/EOSRecovery");
            }

            return Page();
        }

        private async Task LoadStaffDataAsync()
        {
            StaffMember = await _context.EbillUsers
                .Include(u => u.OrganizationEntity)
                .Include(u => u.OfficeEntity)
                .FirstOrDefaultAsync(u => u.IndexNumber == IndexNumber);

            if (StaffMember != null)
            {
                AssignedPhones = await _context.UserPhones
                    .Include(p => p.ClassOfService)
                    .Where(p => p.IndexNumber == IndexNumber && p.IsActive)
                    .OrderByDescending(p => p.IsPrimary)
                    .ThenBy(p => p.PhoneType)
                    .ToListAsync();
            }
        }

        /// <summary>
        /// Search Staff - Search for staff members by name, email, or index number
        /// </summary>
        public async Task<JsonResult> OnGetSearchStaffAsync(string searchQuery)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchQuery) || searchQuery.Length < 3)
                {
                    return new JsonResult(new { success = false, message = "Search query must be at least 3 characters" });
                }

                var query = searchQuery.ToLower().Trim();

                var results = await _context.EbillUsers
                    .Include(u => u.OrganizationEntity)
                    .Where(u =>
                        u.FirstName.ToLower().Contains(query) ||
                        u.LastName.ToLower().Contains(query) ||
                        u.Email.ToLower().Contains(query) ||
                        u.IndexNumber.ToLower().Contains(query))
                    .OrderBy(u => u.FirstName)
                    .ThenBy(u => u.LastName)
                    .Take(10)
                    .Select(u => new
                    {
                        indexNumber = u.IndexNumber,
                        fullName = $"{u.FirstName} {u.LastName}",
                        email = u.Email,
                        organization = u.OrganizationEntity != null ? u.OrganizationEntity.Name : null
                    })
                    .ToListAsync();

                return new JsonResult(new
                {
                    success = true,
                    results = results
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching staff with query {Query}", searchQuery);
                return new JsonResult(new { success = false, message = "Error searching staff" });
            }
        }

        /// <summary>
        /// Toggle User Profile - Enable or disable user account during EOS processing
        /// When disabling: automatically deactivates all assigned phone numbers
        /// </summary>
        public async Task<JsonResult> OnPostToggleUserProfileAsync(string indexNumber)
        {
            try
            {
                var user = await _context.EbillUsers.FirstOrDefaultAsync(u => u.IndexNumber == indexNumber);

                if (user == null)
                {
                    return new JsonResult(new { success = false, message = "User not found" });
                }

                // Toggle the IsActive status
                var wasActive = user.IsActive;
                user.IsActive = !user.IsActive;
                var action = user.IsActive ? "enabled" : "disabled";

                // If disabling user, automatically deactivate all their phone numbers
                int phonesDeactivated = 0;
                if (!user.IsActive && wasActive)
                {
                    var userPhones = await _context.UserPhones
                        .Where(p => p.IndexNumber == indexNumber && p.IsActive)
                        .ToListAsync();

                    foreach (var phone in userPhones)
                    {
                        phone.Status = PhoneStatus.Deactivated;
                        phone.IsPrimary = false;
                        phonesDeactivated++;

                        _logger.LogInformation("Phone {PhoneNumber} automatically deactivated due to user profile disable for {IndexNumber}",
                            phone.PhoneNumber, indexNumber);
                    }
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("User profile {IndexNumber} {Action} during EOS processing by {User}. {PhoneCount} phones deactivated.",
                    indexNumber, action, User.Identity?.Name, phonesDeactivated);

                var message = phonesDeactivated > 0
                    ? $"User profile {action} successfully. {phonesDeactivated} phone number(s) automatically deactivated."
                    : $"User profile {action} successfully";

                return new JsonResult(new
                {
                    success = true,
                    message = message,
                    isActive = user.IsActive,
                    phonesDeactivated = phonesDeactivated,
                    reloadUrl = $"/Admin/ProcessEOS?indexNumber={indexNumber}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling user profile {IndexNumber}", indexNumber);
                return new JsonResult(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        /// <summary>
        /// Reassign Phone - Transfer phone from EOS staff to another staff member
        /// </summary>
        public async Task<JsonResult> OnPostReassignPhoneAsync(int phoneId, string newIndexNumber)
        {
            try
            {
                var phone = await _context.UserPhones
                    .Include(p => p.EbillUser)
                    .FirstOrDefaultAsync(p => p.Id == phoneId);

                if (phone == null)
                {
                    return new JsonResult(new { success = false, message = "Phone record not found" });
                }

                var newUser = await _context.EbillUsers
                    .FirstOrDefaultAsync(u => u.IndexNumber == newIndexNumber);

                if (newUser == null)
                {
                    return new JsonResult(new { success = false, message = "New staff member not found" });
                }

                var oldIndexNumber = phone.IndexNumber;
                var oldUserName = phone.EbillUser != null ? $"{phone.EbillUser.FirstName} {phone.EbillUser.LastName}" : oldIndexNumber;

                // Check if new user already has a primary phone if this is a primary phone
                if (phone.IsPrimary)
                {
                    var existingPrimary = await _context.UserPhones
                        .FirstOrDefaultAsync(p => p.IndexNumber == newIndexNumber && p.IsPrimary && p.IsActive);

                    if (existingPrimary != null)
                    {
                        existingPrimary.IsPrimary = false;
                        existingPrimary.LineType = LineType.Secondary;
                    }
                }

                // Reassign the phone
                phone.IndexNumber = newIndexNumber;
                phone.AssignedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Phone {PhoneNumber} reassigned from {OldIndex} to {NewIndex} during EOS processing",
                    phone.PhoneNumber, oldIndexNumber, newIndexNumber);

                return new JsonResult(new
                {
                    success = true,
                    message = $"Phone {phone.PhoneNumber} reassigned successfully to {newUser.FirstName} {newUser.LastName}",
                    reloadUrl = $"/Admin/ProcessEOS?indexNumber={oldIndexNumber}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reassigning phone {PhoneId}", phoneId);
                return new JsonResult(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        /// <summary>
        /// Deactivate Phone - Set phone status to Deactivated for EOS staff
        /// </summary>
        public async Task<JsonResult> OnPostDeactivatePhoneAsync(int phoneId)
        {
            try
            {
                var phone = await _context.UserPhones.FindAsync(phoneId);

                if (phone == null)
                {
                    return new JsonResult(new { success = false, message = "Phone record not found" });
                }

                phone.Status = PhoneStatus.Deactivated;
                phone.IsPrimary = false;

                await _context.SaveChangesAsync();

                var indexNumber = phone.IndexNumber;

                _logger.LogInformation("Phone {PhoneNumber} deactivated during EOS processing for {IndexNumber}",
                    phone.PhoneNumber, indexNumber);

                return new JsonResult(new
                {
                    success = true,
                    message = $"Phone {phone.PhoneNumber} deactivated successfully",
                    reloadUrl = $"/Admin/ProcessEOS?indexNumber={indexNumber}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating phone {PhoneId}", phoneId);
                return new JsonResult(new { success = false, message = $"Error: {ex.Message}" });
            }
        }
    }
}
