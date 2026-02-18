using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TAB.Web.Models;

namespace TAB.Web.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<LoginModel> _logger;

        public LoginModel(SignInManager<ApplicationUser> signInManager, ILogger<LoginModel> logger)
        {
            _signInManager = signInManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new InputModel();

        public string? ReturnUrl { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            [StringLength(256, ErrorMessage = "Email must not exceed 256 characters.")]
            [RegularExpression(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
                ErrorMessage = "Please enter a valid email address.")]
            public string Email { get; set; } = string.Empty;

            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; } = string.Empty;

            [Display(Name = "Remember me?")]
            public bool RememberMe { get; set; }
        }

        public async Task OnGetAsync(string? returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            returnUrl ??= Url.Content("~/");

            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ReturnUrl = returnUrl;
        }

        // Azure AD Login Handler
        public IActionResult OnPostAzureAdAsync(string? returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            var redirectUrl = Url.Page("/Account/Login", pageHandler: null, values: new { returnUrl }, protocol: Request.Scheme);

            var properties = new AuthenticationProperties
            {
                RedirectUri = returnUrl
            };

            return Challenge(properties, OpenIdConnectDefaults.AuthenticationScheme);
        }

        // Local Account Login Handler
        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            if (ModelState.IsValid)
            {
                // Security: Sanitize email input to prevent any injection attempts
                // Reject emails containing shell metacharacters or suspicious patterns
                var email = Input.Email?.Trim() ?? string.Empty;
                if (ContainsSuspiciousCharacters(email))
                {
                    _logger.LogWarning("Suspicious characters detected in login email: {Email}", email);
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    return Page();
                }

                var result = await _signInManager.PasswordSignInAsync(email, Input.Password, Input.RememberMe, lockoutOnFailure: true);
                if (result.Succeeded)
                {
                    _logger.LogInformation("User logged in.");
                    return LocalRedirect(returnUrl);
                }
                if (result.RequiresTwoFactor)
                {
                    return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl, RememberMe = Input.RememberMe });
                }
                if (result.IsLockedOut)
                {
                    _logger.LogWarning("User account locked out.");
                    return RedirectToPage("./Lockout");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    return Page();
                }
            }

            // If we got this far, something failed, redisplay form
            return Page();
        }

        /// <summary>
        /// Checks for shell metacharacters and suspicious patterns that could indicate injection attempts.
        /// This is a defense-in-depth measure - ASP.NET Core Identity uses parameterized queries,
        /// but this provides an additional security layer.
        /// </summary>
        private static bool ContainsSuspiciousCharacters(string input)
        {
            if (string.IsNullOrEmpty(input))
                return false;

            // Shell metacharacters and command injection patterns
            char[] suspiciousChars = { '|', ';', '&', '$', '`', '(', ')', '{', '}', '[', ']', '<', '>', '\\', '\n', '\r', '\0' };

            foreach (var c in suspiciousChars)
            {
                if (input.Contains(c))
                    return true;
            }

            // Check for common command injection patterns
            string[] suspiciousPatterns = { "ping", "wget", "curl", "nc ", "bash", "sh ", "cmd", "powershell", "/bin/", "/etc/" };
            var lowerInput = input.ToLowerInvariant();

            foreach (var pattern in suspiciousPatterns)
            {
                if (lowerInput.Contains(pattern))
                    return true;
            }

            return false;
        }
    }
} 