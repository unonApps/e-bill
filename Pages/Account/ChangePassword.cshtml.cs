using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TAB.Web.Models;

namespace TAB.Web.Pages.Account
{
    public class ChangePasswordModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<ChangePasswordModel> _logger;

        public ChangePasswordModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ILogger<ChangePasswordModel> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        [TempData]
        public string? StatusMessage { get; set; }

        public bool HasPassword { get; set; }
        public bool IsPasswordChangeRequired { get; set; }

        public class InputModel
        {
            [Display(Name = "Current password")]
            [DataType(DataType.Password)]
            public string? OldPassword { get; set; }

            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "New password")]
            public string NewPassword { get; set; } = string.Empty;

            [DataType(DataType.Password)]
            [Display(Name = "Confirm new password")]
            [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; } = string.Empty;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            HasPassword = await _userManager.HasPasswordAsync(user);
            IsPasswordChangeRequired = user.RequirePasswordChange;

            // If the user is required to change their password, they must stay on this page
            if (IsPasswordChangeRequired && !Request.Path.Value?.EndsWith("/ChangePassword") == true)
            {
                return RedirectToPage("./ChangePassword");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            HasPassword = await _userManager.HasPasswordAsync(user);
            IsPasswordChangeRequired = user.RequirePasswordChange;

            IdentityResult changePasswordResult;
            if (HasPassword)
            {
                changePasswordResult = await _userManager.ChangePasswordAsync(user, Input.OldPassword ?? string.Empty, Input.NewPassword);
            }
            else
            {
                // If the user doesn't have a password yet (e.g., external login), add a password
                var addPasswordToken = await _userManager.GeneratePasswordResetTokenAsync(user);
                changePasswordResult = await _userManager.ResetPasswordAsync(user, addPasswordToken, Input.NewPassword);
            }

            if (!changePasswordResult.Succeeded)
            {
                foreach (var error in changePasswordResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return Page();
            }

            // Clear the password change required flag
            if (user.RequirePasswordChange)
            {
                user.RequirePasswordChange = false;
                await _userManager.UpdateAsync(user);
            }

            await _signInManager.RefreshSignInAsync(user);
            _logger.LogInformation("User changed their password successfully.");
            StatusMessage = "Your password has been changed.";

            return RedirectToPage("/Index");
        }
    }
} 