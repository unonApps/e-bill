using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TAB.Web.Data;
using TAB.Web.Models;

namespace TAB.Web.Pages.Profile
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public IndexModel(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // User Information
        public ApplicationUser? AppUser { get; set; }
        public EbillUser? EbillUserInfo { get; set; }
        public List<string> UserRoles { get; set; } = new();

        // Form binding for editing
        [BindProperty]
        public ProfileEditModel Input { get; set; } = new();

        // Dropdown lists
        public List<SelectListItem> Organizations { get; set; } = new();
        public List<SelectListItem> Offices { get; set; } = new();
        public List<SelectListItem> SubOffices { get; set; } = new();

        // Organization Information
        public Organization? Organization { get; set; }
        public Office? Office { get; set; }
        public SubOffice? SubOffice { get; set; }

        // Pending Actions
        public int PendingVerifications { get; set; }
        public int PendingApprovals { get; set; }
        public int UnreadNotifications { get; set; }
        public int PendingRecoveryActions { get; set; }

        // User Phones
        public List<UserPhone> UserPhones { get; set; } = new();

        // Recent Activity Summary
        public DateTime? LastVerificationDate { get; set; }
        public DateTime? LastApprovalDate { get; set; }
        public int VerificationsThisMonth { get; set; }
        public int ApprovalsThisMonth { get; set; }

        [TempData]
        public string? StatusMessage { get; set; }

        [TempData]
        public string? StatusMessageClass { get; set; }

        public class ProfileEditModel
        {
            public string? FirstName { get; set; }
            public string? LastName { get; set; }
            public string? OfficialMobileNumber { get; set; }
            public string? Location { get; set; }
            public int? OrganizationId { get; set; }
            public int? OfficeId { get; set; }
            public int? SubOfficeId { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            // Get current user
            AppUser = await _userManager.GetUserAsync(User);
            if (AppUser == null)
                return Challenge();

            // Get user roles
            UserRoles = (await _userManager.GetRolesAsync(AppUser)).ToList();

            // Get EbillUser information
            EbillUserInfo = await _context.EbillUsers
                .Include(e => e.OrganizationEntity)
                .Include(e => e.OfficeEntity)
                .Include(e => e.SubOfficeEntity)
                .FirstOrDefaultAsync(u => u.Email == AppUser.Email);

            if (EbillUserInfo != null)
            {
                Organization = EbillUserInfo.OrganizationEntity;
                Office = EbillUserInfo.OfficeEntity;
                SubOffice = EbillUserInfo.SubOfficeEntity;

                // Populate form with current values
                Input = new ProfileEditModel
                {
                    FirstName = EbillUserInfo.FirstName,
                    LastName = EbillUserInfo.LastName,
                    OfficialMobileNumber = EbillUserInfo.OfficialMobileNumber,
                    Location = EbillUserInfo.Location,
                    OrganizationId = EbillUserInfo.OrganizationId,
                    OfficeId = EbillUserInfo.OfficeId,
                    SubOfficeId = EbillUserInfo.SubOfficeId
                };
            }

            // Load dropdown data
            await LoadDropdownsAsync();

            // Load pending actions
            await LoadPendingActionsAsync();

            // Load recent activity
            await LoadRecentActivityAsync();

            // Load user phones
            if (EbillUserInfo != null)
            {
                UserPhones = await _context.UserPhones
                    .Include(p => p.ClassOfService)
                    .Where(p => p.IndexNumber == EbillUserInfo.IndexNumber && p.IsActive)
                    .OrderByDescending(p => p.IsPrimary)
                    .ThenBy(p => p.PhoneNumber)
                    .ToListAsync();
            }

            return Page();
        }

        private async Task LoadDropdownsAsync()
        {
            // Load organizations
            Organizations = await _context.Organizations
                .OrderBy(o => o.Name)
                .Select(o => new SelectListItem
                {
                    Value = o.Id.ToString(),
                    Text = o.Name
                })
                .ToListAsync();

            Organizations.Insert(0, new SelectListItem { Value = "", Text = "-- Select Organization --" });

            // Load offices
            if (Input.OrganizationId.HasValue)
            {
                Offices = await _context.Offices
                    .Where(o => o.OrganizationId == Input.OrganizationId.Value)
                    .OrderBy(o => o.Name)
                    .Select(o => new SelectListItem
                    {
                        Value = o.Id.ToString(),
                        Text = o.Name
                    })
                    .ToListAsync();
            }

            Offices.Insert(0, new SelectListItem { Value = "", Text = "-- Select Office --" });

            // Load sub-offices
            if (Input.OfficeId.HasValue)
            {
                SubOffices = await _context.SubOffices
                    .Where(s => s.OfficeId == Input.OfficeId.Value)
                    .OrderBy(s => s.Name)
                    .Select(s => new SelectListItem
                    {
                        Value = s.Id.ToString(),
                        Text = s.Name
                    })
                    .ToListAsync();
            }

            SubOffices.Insert(0, new SelectListItem { Value = "", Text = "-- Select Sub Office --" });
        }

        private async Task LoadPendingActionsAsync()
        {
            if (AppUser == null) return;

            // Get pending verifications for the user
            if (EbillUserInfo != null)
            {
                // Count call records that need verification (not yet submitted)
                var userCallRecords = await _context.CallRecords
                    .Where(c => c.ResponsibleIndexNumber == EbillUserInfo.IndexNumber)
                    .ToListAsync();

                var submittedCallIdsList = await _context.CallLogVerifications
                    .Where(v => v.VerifiedBy == EbillUserInfo.IndexNumber)
                    .Select(v => v.CallRecordId)
                    .ToListAsync();

                var submittedCallIds = new HashSet<int>(submittedCallIdsList);

                PendingVerifications = userCallRecords.Count(c => !submittedCallIds.Contains(c.Id));

                // Count pending recovery actions by checking call records with recovery status pending
                PendingRecoveryActions = await _context.CallRecords
                    .Where(r => r.ResponsibleIndexNumber == EbillUserInfo.IndexNumber
                        && r.RecoveryStatus == "Pending")
                    .CountAsync();
            }

            // Get pending approvals for supervisors
            if (UserRoles.Contains("Supervisor") || UserRoles.Contains("Admin"))
            {
                PendingApprovals = await _context.CallLogVerifications
                    .Where(v => v.SupervisorIndexNumber == AppUser.Email
                        && v.SubmittedToSupervisor
                        && (v.SupervisorApprovalStatus == null || v.SupervisorApprovalStatus == "Pending"))
                    .CountAsync();
            }

            // Get unread notifications
            UnreadNotifications = await _context.Notifications
                .Where(n => n.UserId == AppUser.Id && !n.IsRead)
                .CountAsync();
        }

        private async Task LoadRecentActivityAsync()
        {
            if (EbillUserInfo == null) return;

            var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

            // Get last verification date
            LastVerificationDate = await _context.CallLogVerifications
                .Where(v => v.VerifiedBy == EbillUserInfo.IndexNumber)
                .OrderByDescending(v => v.VerifiedDate)
                .Select(v => (DateTime?)v.VerifiedDate)
                .FirstOrDefaultAsync();

            // Get verifications count this month
            VerificationsThisMonth = await _context.CallLogVerifications
                .Where(v => v.VerifiedBy == EbillUserInfo.IndexNumber
                    && v.VerifiedDate >= startOfMonth)
                .CountAsync();

            // For supervisors - get approval activity
            if (UserRoles.Contains("Supervisor") || UserRoles.Contains("Admin"))
            {
                LastApprovalDate = await _context.CallLogVerifications
                    .Where(v => v.SupervisorApprovedBy == AppUser.Email
                        && v.SupervisorApprovedDate.HasValue)
                    .OrderByDescending(v => v.SupervisorApprovedDate)
                    .Select(v => v.SupervisorApprovedDate)
                    .FirstOrDefaultAsync();

                ApprovalsThisMonth = await _context.CallLogVerifications
                    .Where(v => v.SupervisorApprovedBy == AppUser.Email
                        && v.SupervisorApprovedDate.HasValue
                        && v.SupervisorApprovedDate >= startOfMonth)
                    .CountAsync();
            }
        }
    }
}
