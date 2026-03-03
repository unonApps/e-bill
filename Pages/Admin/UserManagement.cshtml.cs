using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TAB.Web.Data;
using TAB.Web.Models;
using TAB.Web.Services;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;

namespace TAB.Web.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class UserManagementModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IEmailService _emailService;
        private readonly IEnhancedEmailService _enhancedEmailService;
        private readonly ILogger<UserManagementModel> _logger;
        private readonly IWebHostEnvironment _environment;
        private readonly ApplicationDbContext _context;

        public UserManagementModel(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IEmailService emailService,
            IEnhancedEmailService enhancedEmailService,
            ILogger<UserManagementModel> logger,
            IWebHostEnvironment environment,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _emailService = emailService;
            _enhancedEmailService = enhancedEmailService;
            _logger = logger;
            _environment = environment;
            _context = context;
        }

        public List<UserViewModel> Users { get; set; } = new();
        public List<string> AvailableRoles { get; set; } = new();
        public List<SelectListItem> AvailableOrganizations { get; set; } = new();
        public List<SelectListItem> AvailableOffices { get; set; } = new();
        public List<SelectListItem> AvailableSubOffices { get; set; } = new();
        public string? StatusMessage { get; set; }
        public string StatusMessageClass { get; set; } = "success";
        public string CurrentUserName { get; set; } = string.Empty;

        // Filter properties
        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }
        
        [BindProperty(SupportsGet = true)]
        public string? SelectedRole { get; set; }
        
        [BindProperty(SupportsGet = true)]
        public UserStatus? SelectedStatus { get; set; }
        
        [BindProperty(SupportsGet = true)]
        public int? SelectedOrganizationId { get; set; }
        
        [BindProperty(SupportsGet = true)]
        public int? SelectedOfficeId { get; set; }

        // Role-based summary statistics
        public Dictionary<string, RoleSummary> RoleSummaries { get; set; } = new();
        public int TotalActiveUsers { get; set; }
        public int TotalInactiveUsers { get; set; }
        public int UsersWithoutRoles { get; set; }
        public Dictionary<string, int> OrganizationUserCounts { get; set; } = new();

        [BindProperty]
        public CreateUserInputModel Input { get; set; } = new();

        public class CreateUserInputModel
        {
            [System.ComponentModel.DataAnnotations.Required]
            [System.ComponentModel.DataAnnotations.EmailAddress]
            public string Email { get; set; } = string.Empty;

            [System.ComponentModel.DataAnnotations.Required]
            public string FirstName { get; set; } = string.Empty;

            [System.ComponentModel.DataAnnotations.Required]
            public string LastName { get; set; } = string.Empty;

            [System.ComponentModel.DataAnnotations.Required]
            public string Role { get; set; } = string.Empty;

            public int? OrganizationId { get; set; }

            public int? OfficeId { get; set; }

            public int? SubOfficeId { get; set; }

            [System.ComponentModel.DataAnnotations.Required]
            public UserStatus Status { get; set; } = UserStatus.Active;
        }

        public class UserViewModel
        {
            public string Id { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string FirstName { get; set; } = string.Empty;
            public string LastName { get; set; } = string.Empty;
            public List<string> Roles { get; set; } = new();
            public string? OrganizationName { get; set; }
            public string? OfficeName { get; set; }
            public string? SubOfficeName { get; set; }
            public int? OrganizationId { get; set; }
            public int? OfficeId { get; set; }
            public int? SubOfficeId { get; set; }
            public UserStatus Status { get; set; }
            public string StatusDisplay => Status == UserStatus.Active ? "Active" : "Inactive";
        }

        public class RoleSummary
        {
            public string RoleName { get; set; } = string.Empty;
            public int TotalUsers { get; set; }
            public int ActiveUsers { get; set; }
            public int InactiveUsers { get; set; }
            public string Description { get; set; } = string.Empty;
            public string IconClass { get; set; } = string.Empty;
            public string ColorClass { get; set; } = string.Empty;
            public List<string> RecentUsers { get; set; } = new();
        }

        public async Task OnGetAsync()
        {
            await LoadPageDataAsync();
        }

        public async Task<IActionResult> OnPostAddRoleAsync(string userId, string role)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                StatusMessage = "Error: User not found.";
                StatusMessageClass = "danger";
                await LoadPageDataAsync();
                return Page();
            }

            if (!await _userManager.IsInRoleAsync(user, role))
            {
                var result = await _userManager.AddToRoleAsync(user, role);
                if (result.Succeeded)
                {
                    StatusMessage = $"Successfully added user to {role} role.";
                }
                else
                {
                    StatusMessage = $"Error: Failed to add user to {role} role.";
                    StatusMessageClass = "danger";
                }
            }

            await LoadPageDataAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostRemoveRoleAsync(string userId, string role)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                StatusMessage = "Error: User not found.";
                StatusMessageClass = "danger";
                await LoadPageDataAsync();
                return Page();
            }

            if (await _userManager.IsInRoleAsync(user, role))
            {
                // Don't allow removing the last admin
                if (role == "Admin")
                {
                    var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
                    if (adminUsers.Count <= 1 && adminUsers.Any(a => a.Id == userId))
                    {
                        StatusMessage = "Error: Cannot remove the last Admin.";
                        StatusMessageClass = "danger";
                        await LoadPageDataAsync();
                        return Page();
                    }
                }

                var result = await _userManager.RemoveFromRoleAsync(user, role);
                if (result.Succeeded)
                {
                    StatusMessage = $"Successfully removed user from {role} role.";
                }
                else
                {
                    StatusMessage = $"Error: Failed to remove user from {role} role.";
                    StatusMessageClass = "danger";
                }
            }

            await LoadPageDataAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostCreateUserAsync()
        {
            if (!ModelState.IsValid)
            {
                // Log ModelState errors to help debugging
                foreach (var key in ModelState.Keys)
                {
                    var state = ModelState[key];
                    if (state?.Errors.Count > 0)
                    {
                        _logger.LogError("Validation error for field {Field}: {Error}", 
                            key, string.Join(", ", state.Errors.Select(e => e.ErrorMessage)));
                    }
                }
                
                StatusMessage = "Error: Please check the form fields.";
                StatusMessageClass = "danger";
                await LoadPageDataAsync();
                return Page();
            }
            
            // Check if user already exists
            var existingUser = await _userManager.FindByEmailAsync(Input.Email);
            if (existingUser != null)
            {
                StatusMessage = "Error: A user with this email already exists.";
                StatusMessageClass = "danger";
                await LoadPageDataAsync();
                return Page();
            }
            
            // Create the new user with email as password
            var user = new ApplicationUser
            {
                UserName = Input.Email,
                Email = Input.Email,
                FirstName = Input.FirstName,
                LastName = Input.LastName,
                EmailConfirmed = true, // Auto-confirm email for admin-created accounts
                RequirePasswordChange = true, // Require password change on first login
                OrganizationId = Input.OrganizationId,
                OfficeId = Input.OfficeId,
                SubOfficeId = Input.SubOfficeId,
                Status = Input.Status
            };
            
            // Create a password that meets complexity requirements
            // Add uppercase, digit, and special character to ensure requirements are met
            string initialPassword = $"{Input.Email}Pwd1!";

            var result = await _userManager.CreateAsync(user, initialPassword);
            
            if (result.Succeeded)
            {
                // Assign role
                if (!string.IsNullOrEmpty(Input.Role))
                {
                    await _userManager.AddToRoleAsync(user, Input.Role);
                }
                
                try
                {
                    // Send welcome email using template
                    var baseUrl = $"{Request.Scheme}://{Request.Host}";
                    var loginUrl = $"{baseUrl}/Identity/Account/Login";
                    var emailData = new Dictionary<string, string>
                    {
                        { "FirstName", user.FirstName },
                        { "LastName", user.LastName },
                        { "Email", user.Email ?? string.Empty },
                        { "InitialPassword", initialPassword },
                        { "Role", Input.Role },
                        { "BaseUrl", baseUrl },
                        { "LoginUrl", loginUrl }
                    };

                    await _enhancedEmailService.SendTemplatedEmailAsync(
                        to: user.Email ?? string.Empty,
                        templateCode: "USER_ACCOUNT_CREATED",
                        data: emailData,
                        createdBy: User.Identity?.Name,
                        redactBody: true
                    );

                    StatusMessage = $"Successfully created user {Input.Email} with role {Input.Role}. A welcome email with login instructions has been sent.";
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send welcome email to {Email}", user.Email);
                    StatusMessage = $"Successfully created user {Input.Email} with role {Input.Role}, but failed to send welcome email. Initial password is: {initialPassword}";
                }
                
                // Clear input model
                Input = new();
            }
            else
            {
                StatusMessage = $"Error creating user: {string.Join(", ", result.Errors.Select(e => e.Description))}";
                StatusMessageClass = "danger";
            }
            
            await LoadPageDataAsync();
            return Page();
        }
        
        public async Task<IActionResult> OnPostDeleteUserAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                StatusMessage = "Error: User not found.";
                StatusMessageClass = "danger";
                await LoadPageDataAsync();
                return Page();
            }
            
            // Don't allow deleting the last admin
            if (await _userManager.IsInRoleAsync(user, "Admin"))
            {
                var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
                if (adminUsers.Count <= 1)
                {
                    StatusMessage = "Error: Cannot delete the last Admin user.";
                    StatusMessageClass = "danger";
                    await LoadPageDataAsync();
                    return Page();
                }
            }
            
            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                StatusMessage = $"Successfully deleted user {user.Email}.";
            }
            else
            {
                StatusMessage = $"Error deleting user: {string.Join(", ", result.Errors.Select(e => e.Description))}";
                StatusMessageClass = "danger";
            }
            
            await LoadPageDataAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostToggleStatusAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                StatusMessage = "Error: User not found.";
                StatusMessageClass = "danger";
                await LoadPageDataAsync();
                return Page();
            }

            // Don't allow deactivating the last admin
            if (user.Status == UserStatus.Active && await _userManager.IsInRoleAsync(user, "Admin"))
            {
                var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
                var activeAdmins = adminUsers.Where(u => u.Status == UserStatus.Active).ToList();
                if (activeAdmins.Count <= 1)
                {
                    StatusMessage = "Error: Cannot deactivate the last active Admin user.";
                    StatusMessageClass = "danger";
                    await LoadPageDataAsync();
                    return Page();
                }
            }

            // Toggle status
            user.Status = user.Status == UserStatus.Active ? UserStatus.Inactive : UserStatus.Active;
            
            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                StatusMessage = $"Successfully {(user.Status == UserStatus.Active ? "activated" : "deactivated")} user {user.Email}.";
            }
            else
            {
                StatusMessage = $"Error updating user status: {string.Join(", ", result.Errors.Select(e => e.Description))}";
                StatusMessageClass = "danger";
            }

            await LoadPageDataAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostEditUserAsync(string userId, string email, string firstName, string lastName, bool resetPassword, UserStatus status, int? organizationId, int? officeId, int? subOfficeId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                StatusMessage = "Error: User not found.";
                StatusMessageClass = "danger";
                await LoadPageDataAsync();
                return Page();
            }
            
            // Update user properties
            user.Email = email;
            user.UserName = email; // Username is the same as email
            user.FirstName = firstName;
            user.LastName = lastName;
            user.Status = status;
            user.OrganizationId = organizationId;
            user.OfficeId = officeId;
            user.SubOfficeId = subOfficeId;
            
            var updateResult = await _userManager.UpdateAsync(user);
            
            if (!updateResult.Succeeded)
            {
                StatusMessage = $"Error updating user: {string.Join(", ", updateResult.Errors.Select(e => e.Description))}";
                StatusMessageClass = "danger";
                await LoadPageDataAsync();
                return Page();
            }
            
            if (resetPassword)
            {
                // Generate password reset token
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                
                // Create a compliant password
                string newPassword = $"{user.Email}Pwd1!";
                
                // Set the flag to require password change on next login
                user.RequirePasswordChange = true;
                await _userManager.UpdateAsync(user);
                
                // Reset password to a compliant password
                var resetResult = await _userManager.ResetPasswordAsync(user, token, newPassword);
                
                if (resetResult.Succeeded)
                {
                    try
                    {
                        // Send password reset email using template
                        var baseUrl = $"{Request.Scheme}://{Request.Host}";
                        var loginUrl = $"{baseUrl}/Identity/Account/Login";
                        var emailData = new Dictionary<string, string>
                        {
                            { "FirstName", user.FirstName },
                            { "LastName", user.LastName },
                            { "Email", user.Email ?? string.Empty },
                            { "NewPassword", newPassword },
                            { "ResetDate", DateTime.Now.ToString("MMMM dd, yyyy 'at' hh:mm tt") },
                            { "BaseUrl", baseUrl },
                            { "LoginUrl", loginUrl }
                        };

                        await _enhancedEmailService.SendTemplatedEmailAsync(
                            to: user.Email ?? string.Empty,
                            templateCode: "USER_PASSWORD_RESET",
                            data: emailData,
                            createdBy: User.Identity?.Name,
                            redactBody: true
                        );

                        StatusMessage = $"User {user.Email} updated successfully. Password has been reset and notification email sent.";
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send password reset email to {Email}", user.Email);
                        StatusMessage = $"User updated but failed to send password reset email: {ex.Message}";
                        StatusMessageClass = "warning";
                        await LoadPageDataAsync();
                        return Page();
                    }
                }
                else
                {
                    StatusMessage = $"User updated but password reset failed: {string.Join(", ", resetResult.Errors.Select(e => e.Description))}";
                    StatusMessageClass = "warning";
                    await LoadPageDataAsync();
                    return Page();
                }
            }
            else
            {
                StatusMessage = $"User {user.Email} updated successfully.";
            }
            
            await LoadPageDataAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostForcePasswordChangeAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                StatusMessage = "Error: User not found.";
                StatusMessageClass = "danger";
                await LoadPageDataAsync();
                return Page();
            }
            
            // Update security stamp to force user to sign out
            await _userManager.UpdateSecurityStampAsync(user);
            
            // Set a flag that will require password change
            // This requires a custom check during login that you would implement separately
            var result = await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.Now.AddYears(-1));
            
            if (result.Succeeded)
            {
                StatusMessage = $"User {user.Email} will be required to change password on next login.";
            }
            else
            {
                StatusMessage = $"Error: Failed to set password change requirement.";
                StatusMessageClass = "danger";
            }
            
            await LoadPageDataAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostResetPasswordAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return new JsonResult(new { success = false, message = "Error: User not found." });
            }

            // Generate password reset token
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            // Generate a secure random password
            string newPassword = GenerateSecurePassword();

            // Set the flag to require password change on next login
            user.RequirePasswordChange = true;
            await _userManager.UpdateAsync(user);

            // Reset password to the generated password
            var resetResult = await _userManager.ResetPasswordAsync(user, token, newPassword);

            if (resetResult.Succeeded)
            {
                bool emailSent = false;
                string emailError = string.Empty;

                try
                {
                    // Send password reset email using template
                    var baseUrl = $"{Request.Scheme}://{Request.Host}";
                    var loginUrl = $"{baseUrl}/Identity/Account/Login";
                    var emailData = new Dictionary<string, string>
                    {
                        { "FirstName", user.FirstName },
                        { "LastName", user.LastName },
                        { "Email", user.Email ?? string.Empty },
                        { "NewPassword", newPassword },
                        { "ResetDate", DateTime.Now.ToString("MMMM dd, yyyy 'at' hh:mm tt") },
                        { "BaseUrl", baseUrl },
                        { "LoginUrl", loginUrl }
                    };

                    await _enhancedEmailService.SendTemplatedEmailAsync(
                        to: user.Email ?? string.Empty,
                        templateCode: "USER_PASSWORD_RESET",
                        data: emailData,
                        createdBy: User.Identity?.Name,
                        redactBody: true
                    );

                    emailSent = true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send password reset email to {Email}", user.Email);
                    emailError = ex.Message;
                }

                // Return the password to the admin so they can copy it
                return new JsonResult(new
                {
                    success = true,
                    password = newPassword,
                    email = user.Email,
                    emailSent = emailSent,
                    emailError = emailError
                });
            }
            else
            {
                return new JsonResult(new
                {
                    success = false,
                    message = $"Password reset failed: {string.Join(", ", resetResult.Errors.Select(e => e.Description))}"
                });
            }
        }

        private string GenerateSecurePassword()
        {
            // Generate a secure random password with:
            // - At least 12 characters
            // - Mix of uppercase, lowercase, numbers, and special characters
            const string uppercase = "ABCDEFGHJKLMNPQRSTUVWXYZ"; // Removed I, O for clarity
            const string lowercase = "abcdefghjkmnpqrstuvwxyz"; // Removed i, l, o for clarity
            const string numbers = "23456789"; // Removed 0, 1 for clarity
            const string special = "@#$%&*!";

            var random = new Random();
            var password = new char[12];

            // Ensure at least one of each type
            password[0] = uppercase[random.Next(uppercase.Length)];
            password[1] = lowercase[random.Next(lowercase.Length)];
            password[2] = numbers[random.Next(numbers.Length)];
            password[3] = special[random.Next(special.Length)];

            // Fill the rest randomly
            const string allChars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghjkmnpqrstuvwxyz23456789@#$%&*!";
            for (int i = 4; i < 12; i++)
            {
                password[i] = allChars[random.Next(allChars.Length)];
            }

            // Shuffle the password
            for (int i = password.Length - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (password[i], password[j]) = (password[j], password[i]);
            }

            return new string(password);
        }

        private async Task LoadPageDataAsync()
        {
            // Get current user's name
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null)
            {
                CurrentUserName = !string.IsNullOrEmpty(currentUser.FirstName) && !string.IsNullOrEmpty(currentUser.LastName)
                    ? $"{currentUser.FirstName} {currentUser.LastName}"
                    : currentUser.UserName ?? "Administrator";
            }
            
            Users.Clear();
            
            // Build query with filters
            var query = _userManager.Users
                .Include(u => u.Organization)
                .Include(u => u.Office)
                .Include(u => u.SubOffice)
                .AsQueryable();
            
            // Apply status filter
            if (SelectedStatus.HasValue)
            {
                query = query.Where(u => u.Status == SelectedStatus.Value);
            }
            
            // Apply organization filter
            if (SelectedOrganizationId.HasValue)
            {
                query = query.Where(u => u.OrganizationId == SelectedOrganizationId.Value);
            }
            
            // Apply office filter
            if (SelectedOfficeId.HasValue)
            {
                query = query.Where(u => u.OfficeId == SelectedOfficeId.Value);
            }
            
            // Apply search filter
            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var searchLower = SearchTerm.ToLower();
                query = query.Where(u => 
                    (u.Email != null && u.Email.ToLower().Contains(searchLower)) ||
                    (u.FirstName != null && u.FirstName.ToLower().Contains(searchLower)) ||
                    (u.LastName != null && u.LastName.ToLower().Contains(searchLower)) ||
                    (u.UserName != null && u.UserName.ToLower().Contains(searchLower)));
            }
            
            var users = await query.ToListAsync();
            var userViewModels = new List<UserViewModel>();
            
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                
                // Apply role filter
                if (!string.IsNullOrEmpty(SelectedRole))
                {
                    if (!roles.Contains(SelectedRole))
                        continue;
                }
                
                userViewModels.Add(new UserViewModel
                {
                    Id = user.Id,
                    Email = user.Email ?? string.Empty,
                    FirstName = user.FirstName ?? string.Empty,
                    LastName = user.LastName ?? string.Empty,
                    Roles = roles.ToList(),
                    OrganizationName = user.Organization?.Name,
                    OfficeName = user.Office?.Name,
                    SubOfficeName = user.SubOffice?.Name,
                    OrganizationId = user.OrganizationId,
                    OfficeId = user.OfficeId,
                    SubOfficeId = user.SubOfficeId,
                    Status = user.Status
                });
            }
            
            // Sort users: Admins first, then Managers, then Users
            Users = userViewModels
                .OrderByDescending(u => u.Roles.Contains("Admin"))
                .ThenByDescending(u => u.Roles.Contains("Manager"))
                .ThenBy(u => u.Email)
                .ToList();

            AvailableRoles = await _roleManager.Roles.Select(r => r.Name ?? string.Empty).ToListAsync();
            
            // Calculate role-based statistics
            await CalculateRoleStatisticsAsync();
            
            // Load organizations and offices
            AvailableOrganizations = await _context.Organizations
                .OrderBy(o => o.Name)
                .Select(o => new SelectListItem
                {
                    Value = o.Id.ToString(),
                    Text = o.Name
                })
                .ToListAsync();
                
            AvailableOffices = await _context.Offices
                .Include(o => o.Organization)
                .OrderBy(o => o.Organization.Name)
                .ThenBy(o => o.Name)
                .Select(o => new SelectListItem
                {
                    Value = o.Id.ToString(),
                    Text = $"{o.Organization.Name} - {o.Name}"
                })
                .ToListAsync();

            AvailableSubOffices = await _context.SubOffices
                .Include(s => s.Office)
                    .ThenInclude(o => o.Organization)
                .OrderBy(s => s.Office.Organization.Name)
                .ThenBy(s => s.Office.Name)
                .ThenBy(s => s.Name)
                .Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = $"{s.Office.Organization.Name} - {s.Office.Name} - {s.Name}"
                })
                .ToListAsync();
        }

        private async Task CalculateRoleStatisticsAsync()
        {
            RoleSummaries.Clear();
            
            // Calculate basic user statistics
            TotalActiveUsers = Users.Count(u => u.Status == UserStatus.Active);
            TotalInactiveUsers = Users.Count(u => u.Status == UserStatus.Inactive);
            UsersWithoutRoles = Users.Count(u => !u.Roles.Any());
            
            // Calculate organization distribution
            OrganizationUserCounts = Users
                .Where(u => !string.IsNullOrEmpty(u.OrganizationName))
                .GroupBy(u => u.OrganizationName)
                .ToDictionary(g => g.Key ?? string.Empty, g => g.Count());
            
            // Define role descriptions and styling
            var roleDefinitions = new Dictionary<string, (string description, string icon, string color)>
            {
                { "Admin", ("System administrators with full access to all features", "bi-shield-check", "danger") },
                { "Manager", ("Department managers overseeing team operations", "bi-person-badge", "primary") },
                { "Supervisor", ("Team supervisors handling request approvals", "bi-person-gear", "warning") },
                { "ICTS", ("IT support staff managing technical operations", "bi-laptop", "info") },
                { "User", ("Standard users submitting requests and managing profiles", "bi-person", "secondary") },
                { "Budget Officer", ("Financial officers managing budget approvals", "bi-currency-dollar", "success") },
                { "BudgetOfficer", ("Financial officers managing budget approvals", "bi-currency-dollar", "success") },
                { "Claims Unit Approver", ("Staff handling claims processing and approvals", "bi-clipboard-check", "dark") },
                { "Staff Claims Unit", ("Claims unit staff processing requests", "bi-file-earmark-text", "muted") },
                { "ICTS Service Desk", ("Service desk staff providing user support", "bi-headset", "info") }
            };
            
            // Calculate statistics for each role
            foreach (var role in AvailableRoles)
            {
                var usersInRole = Users.Where(u => u.Roles.Contains(role)).ToList();
                var activeUsersInRole = usersInRole.Count(u => u.Status == UserStatus.Active);
                var inactiveUsersInRole = usersInRole.Count(u => u.Status == UserStatus.Inactive);
                var recentUsers = usersInRole
                    .Take(3)
                    .Select(u => $"{u.FirstName} {u.LastName}")
                    .ToList();
                
                var (description, icon, color) = roleDefinitions.ContainsKey(role) 
                    ? roleDefinitions[role] 
                    : ("Custom role with specific permissions", "bi-star", "secondary");
                
                RoleSummaries[role] = new RoleSummary
                {
                    RoleName = role,
                    TotalUsers = usersInRole.Count,
                    ActiveUsers = activeUsersInRole,
                    InactiveUsers = inactiveUsersInRole,
                    Description = description,
                    IconClass = icon,
                    ColorClass = color,
                    RecentUsers = recentUsers
                };
            }
        }
    }
} 