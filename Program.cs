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
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.DataProtection;
using Hangfire;
using Hangfire.SqlServer;

var builder = WebApplication.CreateBuilder(args);

// Configure forwarded headers for HTTPS behind reverse proxy (IIS)
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// Configure Data Protection to persist keys to file system
// This prevents antiforgery token errors after app restarts
var keysFolder = Path.Combine(builder.Environment.ContentRootPath, "App_Data", "DataProtection-Keys");
Directory.CreateDirectory(keysFolder);
builder.Services.AddDataProtection()
    .SetApplicationName("TABWeb")
    .PersistKeysToFileSystem(new DirectoryInfo(keysFolder));

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Add connection resilience settings if not already in connection string
if (!string.IsNullOrEmpty(connectionString) && !connectionString.Contains("Command Timeout"))
{
    connectionString += ";Command Timeout=60;Max Pool Size=200;Min Pool Size=5;Connect Timeout=30";
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString, sqlOptions => {
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null);
        sqlOptions.CommandTimeout(60); // 60 second command timeout
    }));

// Configure Email Settings
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddTransient<IEmailService, EmailService>();

// Register Enhanced Email Services
builder.Services.AddScoped<IEmailTemplateService, EmailTemplateService>();
builder.Services.AddScoped<IEnhancedEmailService, EnhancedEmailService>();

// Add History Service
builder.Services.AddScoped<ISimRequestHistoryService, SimRequestHistoryService>();

// Register UserPhone Service for managing multiple phones per user
builder.Services.AddScoped<IUserPhoneService, UserPhoneService>();

// Register UserPhoneHistory Service for tracking phone line changes
builder.Services.AddScoped<IUserPhoneHistoryService, UserPhoneHistoryService>();

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
builder.Services.AddScoped<IClassOfServiceVersioningService, ClassOfServiceVersioningService>();
builder.Services.AddScoped<IDocumentManagementService, DocumentManagementService>();

// Register Notification Service for in-app notifications
builder.Services.AddScoped<INotificationService, NotificationService>();

// Register Call Log Recovery and Reporting Services
builder.Services.AddScoped<ICallLogRecoveryService, CallLogRecoveryService>();
builder.Services.AddScoped<IDeadlineManagementService, DeadlineManagementService>();
builder.Services.AddScoped<ICallLogReportingService, CallLogReportingService>();

// Register Currency Conversion Service for multi-currency dashboard
builder.Services.AddScoped<ICurrencyConversionService, CurrencyConversionService>();

// Register Recovery Automation Background Service
builder.Services.AddHostedService<RecoveryAutomationJob>();

// Register Bulk Import Service for enterprise-level upload processing
builder.Services.AddScoped<IBulkImportService, BulkImportService>();

// Register SmartUpload Import Service for Excel file imports with ImportJob tracking
builder.Services.AddScoped<ISmartUploadImportService, SmartUploadImportService>();

// Register SmartUpload User Creation Service for auto-creating users from PSTN/PW files
builder.Services.AddScoped<ISmartUploadUserCreationService, SmartUploadUserCreationService>();

// Add Hangfire with in-memory storage (avoids DB connection at startup)
// TODO: Switch back to SQL Server storage once firewall issue is resolved
builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseInMemoryStorage());

builder.Services.AddHangfireServer(options =>
{
    options.WorkerCount = 2;
    options.Queues = new[] { "imports", "default" };
});

// Configure file upload limits for large CSV files and form value limits for bulk operations
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 500 * 1024 * 1024; // 500MB
    options.ValueCountLimit = 50000; // Allow up to 50,000 form values (for submitting many call record IDs)
    options.KeyLengthLimit = 2048; // Increase key length limit
    options.ValueLengthLimit = 1024 * 1024; // 1MB per value
});

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 500 * 1024 * 1024; // 500MB
    options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(10); // Keep connection alive for long operations
    options.Limits.RequestHeadersTimeout = TimeSpan.FromMinutes(5); // Headers timeout
});

// Add Azure AD Authentication
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(options =>
    {
        builder.Configuration.Bind("AzureAd", options);

        // Reduce token size by only requesting essential scopes
        options.Scope.Clear();
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.Scope.Add("email");

        // Save tokens in cookie to avoid large headers
        options.SaveTokens = false; // Don't save access tokens in cookies (reduces size)
        options.GetClaimsFromUserInfoEndpoint = true;

        // Configure events for auto-provisioning users
        options.Events = new OpenIdConnectEvents
        {
            OnRedirectToIdentityProvider = context =>
            {
                // CRITICAL: Force HTTPS redirect URI when behind IIS
                // IIS in-process hosting doesn't properly detect HTTPS, so we force it here
                if (!builder.Environment.IsDevelopment())
                {
                    // Replace any HTTP redirect URIs with HTTPS
                    if (context.ProtocolMessage.RedirectUri != null && context.ProtocolMessage.RedirectUri.StartsWith("http://"))
                    {
                        context.ProtocolMessage.RedirectUri = context.ProtocolMessage.RedirectUri.Replace("http://", "https://");
                    }
                }
                return Task.CompletedTask;
            },
            OnTokenValidated = async context =>
            {
                var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
                var signInManager = context.HttpContext.RequestServices.GetRequiredService<SignInManager<ApplicationUser>>();
                var dbContext = context.HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
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
                var user = await userManager.Users
                    .Include(u => u.EbillUser)
                    .FirstOrDefaultAsync(u => u.AzureAdObjectId == objectId);

                if (user == null)
                {
                    // Try to find by email (first time Azure AD login for existing user)
                    user = await userManager.Users
                        .Include(u => u.EbillUser)
                        .FirstOrDefaultAsync(u => u.Email == email);

                    if (user == null)
                    {
                        // Check if EbillUser exists - link it to new ApplicationUser
                        var ebillUser = await dbContext.EbillUsers
                            .FirstOrDefaultAsync(e => e.Email == email);

                        // Create new ApplicationUser from Azure AD (auto-provisioning)
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
                            Status = UserStatus.Active,
                            EbillUserId = ebillUser?.Id  // Link to EbillUser if exists
                        };

                        var result = await userManager.CreateAsync(user);
                        if (!result.Succeeded)
                        {
                            logger.LogError("Failed to create user from Azure AD: {Errors}",
                                string.Join(", ", result.Errors.Select(e => e.Description)));
                            context.Fail($"Failed to create user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                            return;
                        }

                        logger.LogInformation("Created new ApplicationUser from Azure AD: {Email} (EbillUser linked: {HasEbillUser})",
                            email, ebillUser != null);
                    }
                    else
                    {
                        // User exists by email - link Azure AD ObjectId for future logins
                        user.AzureAdObjectId = objectId;
                        user.AzureAdTenantId = tenantId;
                        user.AzureAdUpn = upn;
                        user.FirstName = firstName ?? user.FirstName;
                        user.LastName = lastName ?? user.LastName;
                        await userManager.UpdateAsync(user);
                        logger.LogInformation("Linked existing ApplicationUser to Azure AD: {Email}", email);
                    }
                }
                else
                {
                    // Update user info from Azure AD on each login
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
                        claimsIdentity.AddClaim(new System.Security.Claims.Claim(ClaimTypes.Role, role));
                    }

                    // Add user ID claim for Identity
                    claimsIdentity.AddClaim(new System.Security.Claims.Claim(ClaimTypes.NameIdentifier, user.Id));

                    // Add EbillUserId if exists
                    if (user.EbillUserId.HasValue)
                    {
                        claimsIdentity.AddClaim(new System.Security.Claims.Claim("EbillUserId", user.EbillUserId.Value.ToString()));
                    }
                }

                logger.LogInformation("User {Email} signed in successfully via Azure AD with roles: {Roles}",
                    email, roles.Any() ? string.Join(", ", roles) : "None");
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

        // Use relative paths to avoid mixed content issues
        // ASP.NET Core will automatically use the current scheme (HTTPS)
        cookieOptions.LoginPath = new PathString("/Account/Login");
        cookieOptions.LogoutPath = new PathString("/Account/Logout");
        cookieOptions.AccessDeniedPath = new PathString("/Account/AccessDenied");

        // Configure cookie chunking to handle large Azure AD tokens
        // This splits large cookies into multiple smaller cookies automatically
        cookieOptions.Cookie.HttpOnly = true;
        cookieOptions.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        cookieOptions.Cookie.SameSite = SameSiteMode.Lax;

        // Disable automatic redirects for challenge responses
        // This prevents redirect loops and mixed content issues
        cookieOptions.Events.OnRedirectToLogin = context =>
        {
            // Ensure HTTPS for redirects
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.StatusCode = 401;
            }
            else
            {
                var redirectUri = context.RedirectUri;
                if (redirectUri.StartsWith("http://"))
                {
                    redirectUri = redirectUri.Replace("http://", "https://");
                }
                context.Response.Redirect(redirectUri);
            }
            return Task.CompletedTask;
        };
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

// Use forwarded headers FIRST - before any other middleware
// This is critical for HTTPS to work properly behind IIS
app.UseForwardedHeaders();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// Only redirect to HTTPS in development mode
// In production, IIS handles HTTPS and we're already on HTTPS
if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();

app.UseRouting();

// Add authentication middleware
app.UseAuthentication();
app.UseAuthorization();

// Add Hangfire Dashboard (only accessible by Admin users)
app.UseHangfireDashboard("/hangfire", new Hangfire.DashboardOptions
{
    Authorization = new[] { new HangfireAuthorizationFilter() }
});

// Add Hangfire recurring job for email queue processing
RecurringJob.AddOrUpdate<IEnhancedEmailService>(
    "process-email-queue",
    service => service.ProcessQueueAsync(50),
    "*/5 * * * *");

// Add password change middleware
app.UsePasswordChangeMiddleware();

app.MapRazorPages();
app.MapControllers(); // Map API controllers

// Run database initialization in background so app starts immediately
_ = Task.Run(async () =>
{
    // Wait a few seconds for app to fully start
    await Task.Delay(5000);

    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        logger.LogInformation("Starting background database initialization...");

        var dbContext = services.GetRequiredService<ApplicationDbContext>();

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

        // Initialize RecoveryConfiguration if not exists
        var existingConfig = await dbContext.RecoveryConfigurations
            .FirstOrDefaultAsync(rc => rc.RuleName == "SystemConfiguration");

        if (existingConfig == null)
        {
            var defaultConfig = new RecoveryConfiguration
            {
                RuleName = "SystemConfiguration",
                RuleType = "System",
                IsEnabled = true,
                JobIntervalMinutes = 60,  // Run every hour by default
                ReminderDaysBefore = 2,
                AutomationEnabled = true,
                NotificationEnabled = true,
                DefaultApprovalDays = 5,
                DefaultRevertDays = 3,
                MaxRevertsAllowed = 2,
                EnableEmailNotifications = false,
                AdminNotificationEmail = "admin@example.com",
                CreatedDate = DateTime.UtcNow,
                CreatedBy = "System"
            };

            dbContext.RecoveryConfigurations.Add(defaultConfig);
            await dbContext.SaveChangesAsync();
            logger.LogInformation("Created default RecoveryConfiguration (SystemConfiguration)");
        }
        else
        {
            logger.LogInformation("RecoveryConfiguration (SystemConfiguration) already exists");
        }

        logger.LogInformation("Background database initialization completed successfully");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while initializing the database in background.");
    }
});

app.Run();
