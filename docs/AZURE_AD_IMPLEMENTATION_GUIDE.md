# Azure AD Authentication Implementation Guide

## Architecture Overview

### Two User Types
1. **ApplicationUser (AspNetUsers)** - System users with roles
   - Login via Azure AD
   - Have roles: Admin, ICTS, Budget Officer, Claims Unit, Supervisor
   - Manage the application

2. **EbillUser (EbillUsers)** - Staff billing data subjects
   - Do NOT log in to the system
   - Pure data records (Index Number, Name, Phone assignments, etc.)
   - Referenced in billing records

### Optional Linking
- An ApplicationUser CAN optionally be linked to an EbillUser
- Example: If "John Doe" (Staff) also manages the system, he'll have:
  - EbillUser record (for his phone bills)
  - ApplicationUser record (to log in and manage)
  - Link between them via IndexNumber or EbillUserId

---

## Implementation Steps

### 1. Azure AD App Registration

**In Azure Portal:**
1. Go to Azure Active Directory → App registrations → New registration
2. Name: "TAB Web Application"
3. Supported account types: "Accounts in this organizational directory only"
4. Redirect URI:
   - Type: Web
   - URI: `https://tabweb20250926123812.azurewebsites.net/signin-oidc`
   - Add: `https://localhost:7000/signin-oidc` (for local dev)
5. After registration, note:
   - **Application (client) ID**
   - **Directory (tenant) ID**
6. Go to "Certificates & secrets" → New client secret
   - Note the **Client Secret Value** (only shown once!)
7. Go to "API permissions" → Add permission → Microsoft Graph:
   - User.Read
   - email
   - openid
   - profile

### 2. Install NuGet Packages

```bash
dotnet add package Microsoft.Identity.Web
dotnet add package Microsoft.Identity.Web.UI
```

### 3. Update appsettings.json

Add Azure AD configuration:

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "YOUR_TENANT_ID",
    "ClientId": "YOUR_CLIENT_ID",
    "ClientSecret": "YOUR_CLIENT_SECRET",
    "CallbackPath": "/signin-oidc",
    "SignedOutCallbackPath": "/signout-callback-oidc"
  },
  "ConnectionStrings": {
    "DefaultConnection": "..."
  }
}
```

### 4. Database Schema Changes

**Add to ApplicationUser model:**
```csharp
public class ApplicationUser : IdentityUser
{
    // Existing properties...
    public string? FirstName { get; set; }
    public string? LastName { get; set; }

    // Azure AD properties
    public string? AzureAdObjectId { get; set; }  // Azure AD unique ID
    public string? AzureAdTenantId { get; set; }
    public string? AzureAdUpn { get; set; }       // User Principal Name

    // Optional link to EbillUser (if this system user is also staff)
    public int? EbillUserId { get; set; }
    public virtual EbillUser? EbillUser { get; set; }

    // Organization relationships...
    public int? OrganizationId { get; set; }
    public int? OfficeId { get; set; }
    public int? SubOfficeId { get; set; }
}
```

**Migration:**
```bash
dotnet ef migrations add AddAzureAdToApplicationUser
dotnet ef database update
```

### 5. Update Program.cs

Replace Identity configuration with hybrid approach:

```csharp
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;

var builder = WebApplication.CreateBuilder(args);

// Add Azure AD authentication
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));

// Keep Identity for user/role management
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options => {
    options.SignIn.RequireConfirmedAccount = false;
    // Password requirements (for fallback local accounts only)
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Configure authentication
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

// Add authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("BudgetOfficer", policy => policy.RequireRole("Budget Officer", "Admin"));
    // Add more policies as needed
});

builder.Services.AddRazorPages()
    .AddMicrosoftIdentityUI(); // Adds Azure AD UI
```

### 6. Create Hybrid Login Page

**Pages/Account/Login.cshtml.cs:**

```csharp
public class LoginModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<LoginModel> _logger;

    [BindProperty]
    public InputModel Input { get; set; }

    public string ReturnUrl { get; set; }

    [TempData]
    public string ErrorMessage { get; set; }

    public class InputModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        public bool RememberMe { get; set; }
    }

    public async Task OnGetAsync(string returnUrl = null)
    {
        returnUrl ??= Url.Content("~/");
        ReturnUrl = returnUrl;
    }

    // Azure AD Login
    public IActionResult OnPostAzureAd(string returnUrl = null)
    {
        returnUrl ??= Url.Content("~/");

        var redirectUrl = Url.Page("/Account/Login",
            pageHandler: "Callback",
            values: new { returnUrl },
            protocol: Request.Scheme);

        var properties = new AuthenticationProperties
        {
            RedirectUri = redirectUrl
        };

        return Challenge(properties, OpenIdConnectDefaults.AuthenticationScheme);
    }

    // Local account login (fallback/admin)
    public async Task<IActionResult> OnPostLocalAsync(string returnUrl = null)
    {
        returnUrl ??= Url.Content("~/");

        if (ModelState.IsValid)
        {
            var result = await _signInManager.PasswordSignInAsync(
                Input.Email, Input.Password, Input.RememberMe, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                return LocalRedirect(returnUrl);
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return Page();
            }
        }

        return Page();
    }
}
```

**Pages/Account/Login.cshtml:**

```html
@page
@model LoginModel
@{
    ViewData["Title"] = "Log in";
}

<div class="row justify-content-center">
    <div class="col-md-6">
        <div class="card">
            <div class="card-body">
                <h2 class="card-title text-center">@ViewData["Title"]</h2>

                <!-- Azure AD Login (Primary) -->
                <div class="azure-login-section mb-4">
                    <form method="post" asp-page-handler="AzureAd" asp-route-returnUrl="@Model.ReturnUrl">
                        <button type="submit" class="btn btn-primary btn-lg w-100">
                            <i class="fas fa-windows"></i> Sign in with Microsoft
                        </button>
                    </form>
                    <p class="text-muted text-center mt-2">
                        Use your organization account
                    </p>
                </div>

                <hr />
                <p class="text-center text-muted">Or</p>
                <hr />

                <!-- Local Login (Fallback) -->
                <div class="local-login-section">
                    <h5 class="text-center mb-3">Administrator Login</h5>
                    <form method="post" asp-page-handler="Local" asp-route-returnUrl="@Model.ReturnUrl">
                        <div asp-validation-summary="ModelOnly" class="text-danger"></div>
                        <div class="form-floating mb-3">
                            <input asp-for="Input.Email" class="form-control" />
                            <label asp-for="Input.Email"></label>
                            <span asp-validation-for="Input.Email" class="text-danger"></span>
                        </div>
                        <div class="form-floating mb-3">
                            <input asp-for="Input.Password" class="form-control" />
                            <label asp-for="Input.Password"></label>
                            <span asp-validation-for="Input.Password" class="text-danger"></span>
                        </div>
                        <div class="checkbox mb-3">
                            <label asp-for="Input.RememberMe">
                                <input asp-for="Input.RememberMe" />
                                @Html.DisplayNameFor(m => m.Input.RememberMe)
                            </label>
                        </div>
                        <button type="submit" class="btn btn-secondary w-100">
                            Log in with Local Account
                        </button>
                    </form>
                </div>
            </div>
        </div>
    </div>
</div>
```

### 7. Create Azure AD Sign-in Handler

**Middleware/AzureAdAuthenticationHandler.cs:**

```csharp
public class AzureAdAuthenticationHandler
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ILogger<AzureAdAuthenticationHandler> _logger;

    public async Task<ApplicationUser> HandleAzureAdSignInAsync(
        ClaimsPrincipal claimsPrincipal)
    {
        // Extract Azure AD claims
        var objectId = claimsPrincipal.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value;
        var email = claimsPrincipal.FindFirst(ClaimTypes.Email)?.Value;
        var upn = claimsPrincipal.FindFirst("preferred_username")?.Value ?? email;
        var firstName = claimsPrincipal.FindFirst(ClaimTypes.GivenName)?.Value;
        var lastName = claimsPrincipal.FindFirst(ClaimTypes.Surname)?.Value;
        var tenantId = claimsPrincipal.FindFirst("http://schemas.microsoft.com/identity/claims/tenantid")?.Value;

        if (string.IsNullOrEmpty(objectId) || string.IsNullOrEmpty(email))
        {
            throw new Exception("Missing required Azure AD claims");
        }

        // Find or create user
        var user = await _userManager.Users
            .FirstOrDefaultAsync(u => u.AzureAdObjectId == objectId);

        if (user == null)
        {
            // Check if email exists (might be migrated local account)
            user = await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
                // Create new user
                user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true,
                    AzureAdObjectId = objectId,
                    AzureAdTenantId = tenantId,
                    AzureAdUpn = upn,
                    FirstName = firstName,
                    LastName = lastName,
                    Status = UserStatus.Active
                };

                var result = await _userManager.CreateAsync(user);
                if (!result.Succeeded)
                {
                    throw new Exception($"Failed to create user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }

                _logger.LogInformation("Created new user from Azure AD: {Email}", email);
            }
            else
            {
                // Link existing local account to Azure AD
                user.AzureAdObjectId = objectId;
                user.AzureAdTenantId = tenantId;
                user.AzureAdUpn = upn;
                await _userManager.UpdateAsync(user);

                _logger.LogInformation("Linked existing user to Azure AD: {Email}", email);
            }
        }
        else
        {
            // Update user info from Azure AD
            user.Email = email;
            user.UserName = email;
            user.AzureAdUpn = upn;
            user.FirstName = firstName ?? user.FirstName;
            user.LastName = lastName ?? user.LastName;
            await _userManager.UpdateAsync(user);
        }

        // Sign in the user
        await _signInManager.SignInAsync(user, isPersistent: false);

        return user;
    }
}
```

### 8. User Management Integration

**Keep existing User Management page** - it manages AspNetUsers (system users)

**Key points:**
- Admin creates ApplicationUser accounts via User Management
- Assigns roles (Admin, ICTS, Budget Officer, etc.)
- Optionally links to EbillUser by IndexNumber
- Azure AD users are auto-created on first login
- Admin can still assign roles to Azure AD users

### 9. EbillUser Management

**Remains separate** - these are billing data subjects, not system users:
- Import from CSV (existing functionality)
- Manage phone assignments
- Link to billing records
- NO login capability
- NO roles or permissions

### 10. Optional Linking Flow

When you want to link an ApplicationUser to an EbillUser:

```csharp
// In User Management page
public class UserManagementModel : PageModel
{
    public class LinkEbillUserModel
    {
        public string ApplicationUserId { get; set; }
        public int? EbillUserId { get; set; }
        public string IndexNumber { get; set; }
    }

    public async Task<IActionResult> OnPostLinkEbillUser(LinkEbillUserModel model)
    {
        var appUser = await _userManager.FindByIdAsync(model.ApplicationUserId);
        if (appUser == null) return NotFound();

        var ebillUser = await _context.EbillUsers
            .FirstOrDefaultAsync(e => e.IndexNumber == model.IndexNumber);

        if (ebillUser != null)
        {
            appUser.EbillUserId = ebillUser.Id;
            await _userManager.UpdateAsync(appUser);
        }

        return RedirectToPage();
    }
}
```

---

## Benefits of This Approach

1. ✅ **Separation of Concerns**
   - ApplicationUser = System access
   - EbillUser = Billing data

2. ✅ **Azure AD Integration**
   - Single Sign-On for staff
   - Centralized user management
   - No password management

3. ✅ **Flexibility**
   - Local admin accounts still work
   - Can link system users to billing records
   - Not all staff need system access

4. ✅ **Security**
   - Azure AD authentication
   - Role-based access control
   - Audit trail via Azure AD

5. ✅ **Scalability**
   - Auto-provisioning from Azure AD
   - Easy user onboarding/offboarding
   - Group-based role assignment (future)

---

## Migration Strategy

### Phase 1: Setup (No Downtime)
1. Register app in Azure AD
2. Install packages
3. Add Azure AD columns to ApplicationUser
4. Deploy with both login methods available

### Phase 2: Testing
1. Test Azure AD login with select users
2. Verify role assignments work
3. Test EbillUser linking if needed

### Phase 3: Full Rollout
1. Communicate to users
2. Everyone uses Azure AD login
3. Keep local admin account for emergencies

### Phase 4: Cleanup
1. Optionally disable local login (except admin)
2. Remove password fields from UI
3. Link ApplicationUsers to EbillUsers where applicable

---

## Configuration Checklist

- [ ] Azure AD app registered
- [ ] Client ID and Secret saved securely
- [ ] Redirect URIs configured
- [ ] API permissions granted
- [ ] NuGet packages installed
- [ ] appsettings.json updated
- [ ] Database migration created
- [ ] Login page updated
- [ ] Authentication handler implemented
- [ ] Tested with Azure AD account
- [ ] Tested with local admin account
- [ ] User management page updated
- [ ] Documentation created for users

---

## Troubleshooting

### "AADSTS50011: The reply URL specified in the request does not match"
- Check redirect URI in Azure AD matches exactly
- Include both production and localhost URIs

### "User created but has no roles"
- Azure AD users auto-created with no roles by default
- Admin must assign roles via User Management page
- Consider auto-role assignment based on Azure AD groups (advanced)

### "Can't link ApplicationUser to EbillUser"
- Ensure IndexNumber exists in EbillUsers table
- Check that EbillUser hasn't been deleted
- Verify foreign key constraints

### "Local admin account doesn't work"
- Ensure you're using the "Local" login form
- Check password hasn't been changed
- Verify user exists in AspNetUsers table
