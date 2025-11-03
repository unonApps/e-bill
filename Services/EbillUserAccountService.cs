using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TAB.Web.Data;
using TAB.Web.Models;

namespace TAB.Web.Services
{
    public interface IEbillUserAccountService
    {
        Task<(bool Success, string Message, string? TempPassword)> CreateLoginAccountAsync(int ebillUserId, bool sendEmail = false);
        Task<(bool Success, string Message, string? TempPassword)> ResetPasswordAsync(int ebillUserId, bool sendEmail = false);
        Task<(bool Success, string Message)> EnableLoginAsync(int ebillUserId);
        Task<(bool Success, string Message)> DisableLoginAsync(int ebillUserId);
        Task<bool> HasLoginAccountAsync(int ebillUserId);
    }

    public class EbillUserAccountService : IEbillUserAccountService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<EbillUserAccountService> _logger;
        private readonly IEmailService _emailService;
        private readonly IEnhancedEmailService _enhancedEmailService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public EbillUserAccountService(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<EbillUserAccountService> logger,
            IEmailService emailService,
            IEnhancedEmailService enhancedEmailService,
            IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
            _emailService = emailService;
            _enhancedEmailService = enhancedEmailService;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<(bool Success, string Message, string? TempPassword)> CreateLoginAccountAsync(int ebillUserId, bool sendEmail = false)
        {
            try
            {
                var ebillUser = await _context.EbillUsers.FindAsync(ebillUserId);
                if (ebillUser == null)
                {
                    return (false, "EbillUser not found.", null);
                }

                // Check if account already exists
                if (ebillUser.HasLoginAccount && !string.IsNullOrEmpty(ebillUser.ApplicationUserId))
                {
                    return (false, "Login account already exists for this user.", null);
                }

                // Generate temporary password
                string tempPassword = GenerateTemporaryPassword();

                // Check if an ApplicationUser with this email already exists
                var existingAppUser = await _userManager.FindByEmailAsync(ebillUser.Email);

                if (existingAppUser != null)
                {
                    // ApplicationUser exists but is not linked or was previously unlinked
                    _logger.LogInformation("Found existing ApplicationUser for email {Email}, reactivating and linking", ebillUser.Email);

                    // Remove old password
                    var hasPassword = await _userManager.HasPasswordAsync(existingAppUser);
                    if (hasPassword)
                    {
                        var removeResult = await _userManager.RemovePasswordAsync(existingAppUser);
                        if (!removeResult.Succeeded)
                        {
                            var errors = string.Join(", ", removeResult.Errors.Select(e => e.Description));
                            return (false, $"Failed to update existing account: {errors}", null);
                        }
                    }

                    // Set new password
                    var addPasswordResult = await _userManager.AddPasswordAsync(existingAppUser, tempPassword);
                    if (!addPasswordResult.Succeeded)
                    {
                        var errors = string.Join(", ", addPasswordResult.Errors.Select(e => e.Description));
                        return (false, $"Failed to set password: {errors}", null);
                    }

                    // Update user details
                    existingAppUser.FirstName = ebillUser.FirstName;
                    existingAppUser.LastName = ebillUser.LastName;
                    existingAppUser.EbillUserId = ebillUser.Id;
                    existingAppUser.OrganizationId = ebillUser.OrganizationId;
                    existingAppUser.OfficeId = ebillUser.OfficeId;
                    existingAppUser.SubOfficeId = ebillUser.SubOfficeId;
                    existingAppUser.Status = ebillUser.IsActive ? UserStatus.Active : UserStatus.Inactive;
                    existingAppUser.RequirePasswordChange = true;
                    existingAppUser.EmailConfirmed = true;

                    var updateResult = await _userManager.UpdateAsync(existingAppUser);
                    if (!updateResult.Succeeded)
                    {
                        var errors = string.Join(", ", updateResult.Errors.Select(e => e.Description));
                        return (false, $"Failed to update user: {errors}", null);
                    }

                    // Ensure user has the "User" role
                    var roles = await _userManager.GetRolesAsync(existingAppUser);
                    if (!roles.Contains("User"))
                    {
                        await _userManager.AddToRoleAsync(existingAppUser, "User");
                    }

                    // Update EbillUser
                    ebillUser.ApplicationUserId = existingAppUser.Id;
                    ebillUser.HasLoginAccount = true;
                    ebillUser.LoginEnabled = true;
                    ebillUser.LastModifiedDate = DateTime.UtcNow;

                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Reactivated and linked login account for EbillUser {IndexNumber}", ebillUser.IndexNumber);

                    // Send email with credentials if requested
                    if (sendEmail)
                    {
                        await SendCredentialsEmail(ebillUser, tempPassword);
                    }

                    return (true, "Login account activated successfully.", tempPassword);
                }

                // Create new ApplicationUser
                var applicationUser = new ApplicationUser
                {
                    UserName = ebillUser.Email,
                    Email = ebillUser.Email,
                    FirstName = ebillUser.FirstName,
                    LastName = ebillUser.LastName,
                    EmailConfirmed = true, // Auto-confirm since admin is creating
                    EbillUserId = ebillUser.Id,
                    OrganizationId = ebillUser.OrganizationId,
                    OfficeId = ebillUser.OfficeId,
                    SubOfficeId = ebillUser.SubOfficeId,
                    Status = ebillUser.IsActive ? UserStatus.Active : UserStatus.Inactive,
                    RequirePasswordChange = true // Force password change on first login
                };

                var result = await _userManager.CreateAsync(applicationUser, tempPassword);

                if (result.Succeeded)
                {
                    // Assign default "User" role (you can make this configurable)
                    await _userManager.AddToRoleAsync(applicationUser, "User");

                    // Update EbillUser
                    ebillUser.ApplicationUserId = applicationUser.Id;
                    ebillUser.HasLoginAccount = true;
                    ebillUser.LoginEnabled = true;
                    ebillUser.LastModifiedDate = DateTime.UtcNow;

                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Created login account for EbillUser {IndexNumber}", ebillUser.IndexNumber);

                    // Send email with credentials if requested
                    if (sendEmail)
                    {
                        await SendCredentialsEmail(ebillUser, tempPassword);
                    }

                    return (true, "Login account created successfully.", tempPassword);
                }
                else
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    _logger.LogError("Failed to create login account: {Errors}", errors);
                    return (false, $"Failed to create account: {errors}", null);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating login account for EbillUser {EbillUserId}", ebillUserId);
                return (false, $"Error: {ex.Message}", null);
            }
        }

        public async Task<(bool Success, string Message, string? TempPassword)> ResetPasswordAsync(int ebillUserId, bool sendEmail = false)
        {
            try
            {
                var ebillUser = await _context.EbillUsers
                    .Include(e => e.ApplicationUser)
                    .FirstOrDefaultAsync(e => e.Id == ebillUserId);

                if (ebillUser == null)
                {
                    return (false, "EbillUser not found.", null);
                }

                if (!ebillUser.HasLoginAccount || ebillUser.ApplicationUser == null)
                {
                    return (false, "No login account exists for this user.", null);
                }

                // Generate new temporary password
                string tempPassword = GenerateTemporaryPassword();

                // Remove existing password
                var removeResult = await _userManager.RemovePasswordAsync(ebillUser.ApplicationUser);
                if (!removeResult.Succeeded)
                {
                    var errors = string.Join(", ", removeResult.Errors.Select(e => e.Description));
                    return (false, $"Failed to reset password: {errors}", null);
                }

                // Add new password
                var addResult = await _userManager.AddPasswordAsync(ebillUser.ApplicationUser, tempPassword);
                if (addResult.Succeeded)
                {
                    // Force password change on next login
                    ebillUser.ApplicationUser.RequirePasswordChange = true;
                    ebillUser.LastModifiedDate = DateTime.UtcNow;
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Reset password for EbillUser {IndexNumber}", ebillUser.IndexNumber);

                    // Send email with new password if requested
                    if (sendEmail)
                    {
                        await SendPasswordResetEmail(ebillUser, tempPassword);
                    }

                    return (true, "Password reset successfully.", tempPassword);
                }
                else
                {
                    var errors = string.Join(", ", addResult.Errors.Select(e => e.Description));
                    return (false, $"Failed to set new password: {errors}", null);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password for EbillUser {EbillUserId}", ebillUserId);
                return (false, $"Error: {ex.Message}", null);
            }
        }

        public async Task<(bool Success, string Message)> EnableLoginAsync(int ebillUserId)
        {
            try
            {
                var ebillUser = await _context.EbillUsers
                    .Include(e => e.ApplicationUser)
                    .FirstOrDefaultAsync(e => e.Id == ebillUserId);

                if (ebillUser == null)
                {
                    return (false, "EbillUser not found.");
                }

                if (!ebillUser.HasLoginAccount || ebillUser.ApplicationUser == null)
                {
                    return (false, "No login account exists. Please create one first.");
                }

                ebillUser.LoginEnabled = true;
                ebillUser.ApplicationUser.Status = UserStatus.Active;
                ebillUser.LastModifiedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Enabled login for EbillUser {IndexNumber}", ebillUser.IndexNumber);
                return (true, "Login enabled successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enabling login for EbillUser {EbillUserId}", ebillUserId);
                return (false, $"Error: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> DisableLoginAsync(int ebillUserId)
        {
            try
            {
                var ebillUser = await _context.EbillUsers
                    .Include(e => e.ApplicationUser)
                    .FirstOrDefaultAsync(e => e.Id == ebillUserId);

                if (ebillUser == null)
                {
                    return (false, "EbillUser not found.");
                }

                if (!ebillUser.HasLoginAccount)
                {
                    return (false, "No login account exists.");
                }

                ebillUser.LoginEnabled = false;
                if (ebillUser.ApplicationUser != null)
                {
                    ebillUser.ApplicationUser.Status = UserStatus.Inactive;
                }
                ebillUser.LastModifiedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Disabled login for EbillUser {IndexNumber}", ebillUser.IndexNumber);
                return (true, "Login disabled successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disabling login for EbillUser {EbillUserId}", ebillUserId);
                return (false, $"Error: {ex.Message}");
            }
        }

        public async Task<bool> HasLoginAccountAsync(int ebillUserId)
        {
            var ebillUser = await _context.EbillUsers.FindAsync(ebillUserId);
            return ebillUser?.HasLoginAccount ?? false;
        }

        private string GenerateTemporaryPassword()
        {
            // Generate a secure random password that meets Identity requirements
            const string upperCase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string lowerCase = "abcdefghijklmnopqrstuvwxyz";
            const string digits = "0123456789";
            const string specialChars = "!@#$%^&*";

            var random = new Random();
            var password = new char[12];

            // Ensure at least one of each required character type
            password[0] = upperCase[random.Next(upperCase.Length)];
            password[1] = lowerCase[random.Next(lowerCase.Length)];
            password[2] = digits[random.Next(digits.Length)];
            password[3] = specialChars[random.Next(specialChars.Length)];

            // Fill the rest randomly
            const string allChars = upperCase + lowerCase + digits + specialChars;
            for (int i = 4; i < password.Length; i++)
            {
                password[i] = allChars[random.Next(allChars.Length)];
            }

            // Shuffle the array
            for (int i = password.Length - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (password[i], password[j]) = (password[j], password[i]);
            }

            return new string(password);
        }

        private async Task SendCredentialsEmail(EbillUser ebillUser, string tempPassword)
        {
            try
            {
                // Send account creation email using template
                var baseUrl = GetBaseUrl();
                var loginUrl = GetLoginUrl();
                var emailData = new Dictionary<string, string>
                {
                    { "FirstName", ebillUser.FirstName },
                    { "LastName", ebillUser.LastName },
                    { "Email", ebillUser.Email },
                    { "TempPassword", tempPassword },
                    { "IndexNumber", ebillUser.IndexNumber },
                    { "BaseUrl", baseUrl },
                    { "LoginUrl", loginUrl }
                };

                await _enhancedEmailService.SendTemplatedEmailAsync(
                    to: ebillUser.Email,
                    templateCode: "EBILL_USER_ACCOUNT_CREATED",
                    data: emailData,
                    createdBy: "System"
                );

                _logger.LogInformation("Sent credentials email to {Email} using template", ebillUser.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send credentials email to {Email}", ebillUser.Email);
            }
        }

        private async Task SendPasswordResetEmail(EbillUser ebillUser, string tempPassword)
        {
            try
            {
                // Send password reset email using template
                var baseUrl = GetBaseUrl();
                var loginUrl = GetLoginUrl();
                var emailData = new Dictionary<string, string>
                {
                    { "FirstName", ebillUser.FirstName },
                    { "LastName", ebillUser.LastName },
                    { "Email", ebillUser.Email },
                    { "TempPassword", tempPassword },
                    { "IndexNumber", ebillUser.IndexNumber },
                    { "ResetDate", DateTime.Now.ToString("MMMM dd, yyyy 'at' hh:mm tt") },
                    { "BaseUrl", baseUrl },
                    { "LoginUrl", loginUrl }
                };

                await _enhancedEmailService.SendTemplatedEmailAsync(
                    to: ebillUser.Email,
                    templateCode: "EBILL_USER_PASSWORD_RESET",
                    data: emailData,
                    createdBy: "System"
                );

                _logger.LogInformation("Sent password reset email to {Email} using template", ebillUser.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send password reset email to {Email}", ebillUser.Email);
            }
        }

        private string GetBaseUrl()
        {
            // Get base URL from HttpContext if available, otherwise use default
            var request = _httpContextAccessor.HttpContext?.Request;
            if (request != null)
            {
                return $"{request.Scheme}://{request.Host}";
            }

            // Fallback to localhost for background jobs or when HttpContext is not available
            return "http://localhost:5041";
        }

        private string GetLoginUrl()
        {
            // Get login URL from HttpContext if available, otherwise use default
            var request = _httpContextAccessor.HttpContext?.Request;
            if (request != null)
            {
                return $"{request.Scheme}://{request.Host}/Identity/Account/Login";
            }

            // Fallback to localhost for background jobs or when HttpContext is not available
            return "http://localhost:5041/Identity/Account/Login";
        }
    }
}
