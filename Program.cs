using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TAB.Web.Data;
using TAB.Web.Models;
using TAB.Web.Middleware;
using TAB.Web.Services;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString, sqlOptions => {
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null);
    }));

// Configure Email Settings
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddTransient<IEmailService, EmailService>();

// Add History Service
builder.Services.AddScoped<ISimRequestHistoryService, SimRequestHistoryService>();

// Register UserPhone Service for managing multiple phones per user
builder.Services.AddScoped<IUserPhoneService, UserPhoneService>();

// Register Audit Log Service for audit trail logging
builder.Services.AddScoped<IAuditLogService, AuditLogService>();

// Register EbillUser Account Service for managing EbillUser login accounts
builder.Services.AddScoped<IEbillUserAccountService, EbillUserAccountService>();

// Register CallLog Staging Service for call logs consolidation and verification
builder.Services.AddScoped<ICallLogStagingService, CallLogStagingService>();

// Register GUID Service for handling PublicId operations
builder.Services.AddScoped<IGuidService, GuidService>();

// Register Date Parsing Services for flexible date format handling
builder.Services.AddSingleton<IDateFormatDetectorService, DateFormatDetectorService>();
builder.Services.AddScoped<IFlexibleDateParserService, FlexibleDateParserService>();

// Register Call Log Verification Services
builder.Services.AddScoped<ICallLogVerificationService, CallLogVerificationService>();
builder.Services.AddScoped<IClassOfServiceCalculationService, ClassOfServiceCalculationService>();
builder.Services.AddScoped<IDocumentManagementService, DocumentManagementService>();

// Register Notification Service for in-app notifications
builder.Services.AddScoped<INotificationService, NotificationService>();

// Add Azure AD Authentication
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(options =>
    {
        builder.Configuration.Bind("AzureAd", options);

        // Configure events for auto-provisioning users
        options.Events = new OpenIdConnectEvents
        {
            OnTokenValidated = async context =>
            {
                var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
                var signInManager = context.HttpContext.RequestServices.GetRequiredService<SignInManager<ApplicationUser>>();
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();

                // Extract Azure AD claims
                var objectId = context.Principal?.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value
                    ?? context.Principal?.FindFirst("oid")?.Value;
                var email = context.Principal?.FindFirst(ClaimTypes.Email)?.Value
                    ?? context.Principal?.FindFirst("preferred_username")?.Value;
                var upn = context.Principal?.FindFirst("preferred_username")?.Value ?? email;
                var firstName = context.Principal?.FindFirst(ClaimTypes.GivenName)?.Value;
                var lastName = context.Principal?.FindFirst(ClaimTypes.Surname)?.Value;
                var tenantId = context.Principal?.FindFirst("http://schemas.microsoft.com/identity/claims/tenantid")?.Value
                    ?? context.Principal?.FindFirst("tid")?.Value;

                if (string.IsNullOrEmpty(objectId) || string.IsNullOrEmpty(email))
                {
                    logger.LogError("Missing required Azure AD claims. ObjectId: {ObjectId}, Email: {Email}", objectId, email);
                    context.Fail("Missing required Azure AD claims");
                    return;
                }

                // Find or create user
                var user = await userManager.Users.FirstOrDefaultAsync(u => u.AzureAdObjectId == objectId);

                if (user == null)
                {
                    // Check if email exists (migrated local account)
                    user = await userManager.FindByEmailAsync(email);

                    if (user == null)
                    {
                        // Create new user from Azure AD
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

                        var result = await userManager.CreateAsync(user);
                        if (!result.Succeeded)
                        {
                            logger.LogError("Failed to create user from Azure AD: {Errors}",
                                string.Join(", ", result.Errors.Select(e => e.Description)));
                            context.Fail($"Failed to create user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                            return;
                        }

                        logger.LogInformation("Created new user from Azure AD: {Email}", email);
                    }
                    else
                    {
                        // Link existing local account to Azure AD
                        user.AzureAdObjectId = objectId;
                        user.AzureAdTenantId = tenantId;
                        user.AzureAdUpn = upn;
                        await userManager.UpdateAsync(user);
                        logger.LogInformation("Linked existing user to Azure AD: {Email}", email);
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
                    await userManager.UpdateAsync(user);
                }

                // Sign in the user with ASP.NET Identity
                await signInManager.SignInAsync(user, isPersistent: false, authenticationMethod: "AzureAD");

                // Add user's roles as claims
                var roles = await userManager.GetRolesAsync(user);
                var claimsIdentity = context.Principal?.Identity as ClaimsIdentity;
                if (claimsIdentity != null)
                {
                    // Add role claims
                    foreach (var role in roles)
                    {
                        claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, role));
                    }

                    // Add user ID claim for Identity
                    claimsIdentity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id));
                }

                logger.LogInformation("User {Email} signed in successfully via Azure AD", email);
            },
            OnAuthenticationFailed = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogError(context.Exception, "Azure AD authentication failed");
                context.HandleResponse();
                context.Response.Redirect($"/Account/Login?error={Uri.EscapeDataString(context.Exception.Message)}");
                return Task.CompletedTask;
            }
        };
    }, cookieOptions =>
    {
        cookieOptions.Cookie.Name = "TAB.AzureAD";
        cookieOptions.LoginPath = "/Account/Login";
        cookieOptions.LogoutPath = "/Account/Logout";
        cookieOptions.AccessDeniedPath = "/Account/AccessDenied";
    });

// Add Identity with roles (for local accounts and role management)
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options => {
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Configure Identity cookie (for local accounts)
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "TAB.Identity";
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

// Configure Razor Pages to require authentication for all pages
builder.Services.AddRazorPages(options =>
{
    // Require authentication for all pages by default
    options.Conventions.AuthorizeFolder("/");
    
    // Allow anonymous access only to login, logout, and access denied pages
    options.Conventions.AllowAnonymousToPage("/Account/Login");
    options.Conventions.AllowAnonymousToPage("/Account/Logout");
    options.Conventions.AllowAnonymousToPage("/Account/AccessDenied");
    
    // Admin-only pages
    options.Conventions.AuthorizePage("/Account/Register", "AdminOnly");
    options.Conventions.AuthorizeFolder("/Admin", "AdminOnly");
});

// Add policy for admin-only pages
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
});

builder.Services.AddRazorPages()
    .AddMicrosoftIdentityUI(); // Add Azure AD UI components

// Add API Controllers support
builder.Services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Add authentication middleware
app.UseAuthentication();
app.UseAuthorization();

// Add password change middleware
app.UsePasswordChangeMiddleware();

app.MapRazorPages();
app.MapControllers(); // Map API controllers

// Ensure database created and apply migrations
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var dbContext = services.GetRequiredService<ApplicationDbContext>();
        var logger = services.GetRequiredService<ILogger<Program>>();

        // Apply pending migrations (this is better than EnsureCreated for production)
        if (dbContext.Database.GetPendingMigrations().Any())
        {
            dbContext.Database.Migrate();
            logger.LogInformation("Database migrations applied successfully");
        }
        else
        {
            logger.LogInformation("No pending migrations");
        }
        
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        
        // Create roles if they don't exist
        string[] roleNames = { "Admin", "User", "ICTS", "ICTS Service Desk", "Budget Officer", "Staff Claims Unit", "Claims Unit Approver", "Supervisor" };
        foreach (var roleName in roleNames)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
                logger.LogInformation("Created role {Role}", roleName);
            }
        }
        
        // Create admin user if it doesn't exist
        var adminUser = await userManager.FindByEmailAsync("admin@example.com");
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = "admin@example.com",
                Email = "admin@example.com",
                EmailConfirmed = true,
                FirstName = "Admin",
                LastName = "User"
            };
            
            var result = await userManager.CreateAsync(adminUser, "Admin123!");
            if (result.Succeeded)
            {
                logger.LogInformation("Admin user created successfully");
                await userManager.AddToRoleAsync(adminUser, "Admin");
                logger.LogInformation("Admin role assigned to admin user");
            }
            else
            {
                logger.LogError("Error creating admin user: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
        
        // Create sample budget officers if they don't exist
        var budgetOfficers = new[]
        {
            new { Email = "budget.officer1@example.com", FirstName = "John", LastName = "Budget" },
            new { Email = "budget.officer2@example.com", FirstName = "Jane", LastName = "Finance" },
            new { Email = "budget.officer3@example.com", FirstName = "Mike", LastName = "Accounting" }
        };
        
        foreach (var officer in budgetOfficers)
        {
            var existingUser = await userManager.FindByEmailAsync(officer.Email);
            if (existingUser == null)
            {
                var budgetUser = new ApplicationUser
                {
                    UserName = officer.Email,
                    Email = officer.Email,
                    EmailConfirmed = true,
                    FirstName = officer.FirstName,
                    LastName = officer.LastName
                };
                
                var result = await userManager.CreateAsync(budgetUser, "Budget123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(budgetUser, "Budget Officer");
                    logger.LogInformation("Budget Officer {Email} created successfully", officer.Email);
                }
                else
                {
                    logger.LogError("Error creating budget officer {Email}: {Errors}", 
                        officer.Email, string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
        }

        // Create sample claims unit approvers if they don't exist
        var claimsApprovers = new[]
        {
            new { Email = "claims.approver1@example.com", FirstName = "Sarah", LastName = "Claims" },
            new { Email = "claims.approver2@example.com", FirstName = "David", LastName = "Review" },
            new { Email = "amichuki@gmail.com", FirstName = "Boniface", LastName = "Michuki" }
        };
        
        foreach (var approver in claimsApprovers)
        {
            var existingUser = await userManager.FindByEmailAsync(approver.Email);
            if (existingUser == null)
            {
                var claimsUser = new ApplicationUser
                {
                    UserName = approver.Email,
                    Email = approver.Email,
                    EmailConfirmed = true,
                    FirstName = approver.FirstName,
                    LastName = approver.LastName
                };
                
                var result = await userManager.CreateAsync(claimsUser, "Claims123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(claimsUser, "Claims Unit Approver");
                    logger.LogInformation("Claims Unit Approver {Email} created successfully", approver.Email);
                }
                else
                {
                    logger.LogError("Error creating claims unit approver {Email}: {Errors}", 
                        approver.Email, string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while initializing the database.");
    }
}

app.Run();
