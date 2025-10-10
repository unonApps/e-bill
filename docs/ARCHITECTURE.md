# TAB Web Application - Architecture Documentation

## Table of Contents
- [System Overview](#system-overview)
- [Architecture Patterns](#architecture-patterns)
- [Technology Stack](#technology-stack)
- [Application Layers](#application-layers)
- [Service Architecture](#service-architecture)
- [Authentication & Authorization](#authentication--authorization)
- [Data Flow](#data-flow)
- [Module Architecture](#module-architecture)
- [Integration Points](#integration-points)
- [Security Architecture](#security-architecture)
- [Performance & Scalability](#performance--scalability)
- [Deployment Architecture](#deployment-architecture)

---

## System Overview

TAB Web is an enterprise-grade telecom billing and management system built on a **modular, service-oriented architecture** using ASP.NET Core 8.0 and Razor Pages.

### High-Level Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    Presentation Layer                        │
│              (Razor Pages + Bootstrap UI)                    │
├─────────────────────────────────────────────────────────────┤
│                   Application Layer                          │
│        (Page Models + Controllers + Middleware)              │
├─────────────────────────────────────────────────────────────┤
│                    Service Layer                             │
│    (Business Logic Services + Domain Services)               │
├─────────────────────────────────────────────────────────────┤
│                      Data Layer                              │
│     (Entity Framework Core + DbContext)                      │
├─────────────────────────────────────────────────────────────┤
│                    Database Layer                            │
│              (SQL Server / Azure SQL)                        │
└─────────────────────────────────────────────────────────────┘
```

### Core Principles

1. **Separation of Concerns** - Clear boundaries between layers
2. **Dependency Injection** - Loose coupling and testability
3. **Modular Design** - Self-contained functional modules
4. **Service-Oriented** - Business logic in reusable services
5. **Convention over Configuration** - ASP.NET Core conventions
6. **Security First** - Authentication, authorization, and audit logging

---

## Architecture Patterns

### 1. Repository Pattern (Lightweight)
Entity Framework Core DbContext serves as a lightweight repository.

```csharp
public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public DbSet<EbillUser> EbillUsers { get; set; }
    public DbSet<CallRecord> CallRecords { get; set; }
    // ... other entities
}
```

**Benefits**:
- Centralized data access
- Change tracking
- Transaction management
- Query optimization

### 2. Service Layer Pattern
Business logic encapsulated in dedicated service classes.

```csharp
public interface ICallLogVerificationService
{
    Task<CallLogVerification> CreateVerificationAsync(...);
    Task<bool> SubmitToSupervisorAsync(...);
}

public class CallLogVerificationService : ICallLogVerificationService
{
    private readonly ApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly IAuditLogService _auditLogService;

    // Implementation...
}
```

**Benefits**:
- Testable business logic
- Reusable across pages/controllers
- Clear dependency management
- Single Responsibility Principle

### 3. Dependency Injection Pattern
All services registered in `Program.cs` and injected via constructor.

```csharp
// Registration
builder.Services.AddScoped<ICallLogVerificationService, CallLogVerificationService>();

// Injection
public class VerifyModel : PageModel
{
    private readonly ICallLogVerificationService _verificationService;

    public VerifyModel(ICallLogVerificationService verificationService)
    {
        _verificationService = verificationService;
    }
}
```

### 4. Unit of Work Pattern
DbContext implements Unit of Work pattern inherently.

```csharp
// Multiple operations in single transaction
using var transaction = await _context.Database.BeginTransactionAsync();
try
{
    _context.CallRecords.Add(record);
    await _context.SaveChangesAsync();

    await _auditLogService.LogAsync("Create", "CallRecord", record.Id);

    await transaction.CommitAsync();
}
catch
{
    await transaction.RollbackAsync();
    throw;
}
```

### 5. Facade Pattern
Services act as facades for complex operations.

```csharp
public class CallLogStagingService : ICallLogStagingService
{
    // Coordinates multiple operations
    public async Task<int> ImportCallLogAsync(IFormFile file, ...)
    {
        // 1. Upload file
        // 2. Parse CSV
        // 3. Detect date format
        // 4. Stage records
        // 5. Validate batch
        // 6. Create audit log
        // 7. Send notifications
    }
}
```

---

## Technology Stack

### Backend
- **Framework**: ASP.NET Core 8.0
- **UI**: Razor Pages (Server-side rendering)
- **ORM**: Entity Framework Core 8.0
- **Database**: SQL Server 2016+ / Azure SQL
- **Authentication**: Microsoft Identity Web (Azure AD)
- **Email**: SMTP via SmtpClient

### Frontend
- **Framework**: Bootstrap 5
- **JavaScript**: Vanilla JS + jQuery
- **Icons**: Font Awesome / Bootstrap Icons
- **Charts**: Chart.js (if needed)

### Development Tools
- **IDE**: Visual Studio 2022 / VS Code
- **Version Control**: Git
- **Database Tools**: SQL Server Management Studio / Azure Data Studio
- **Migration**: EF Core CLI

### Hosting
- **Production**: Azure App Service
- **Database**: Azure SQL Database
- **Storage**: Azure Blob Storage (for documents)

---

## Application Layers

### 1. Presentation Layer

**Components**: Razor Pages (`.cshtml` + `.cshtml.cs`)

**Responsibilities**:
- Render HTML views
- Handle user input
- Display data
- Client-side validation

**Structure**:
```
Pages/
├── Account/           # Authentication pages
├── Admin/             # Admin pages
├── Dashboard/         # Dashboards
├── Modules/           # Feature modules
│   ├── EBillManagement/
│   ├── SimManagement/
│   └── RefundManagement/
└── Shared/            # Layouts and partials
```

**Example PageModel**:
```csharp
[Authorize(Roles = "User")]
public class MyCallLogsModel : PageModel
{
    private readonly ICallLogVerificationService _service;

    public List<CallRecord> CallRecords { get; set; }

    public async Task<IActionResult> OnGetAsync(DateTime period)
    {
        CallRecords = await _service.GetUserCallRecordsAsync(
            User.Identity.Name,
            period
        );
        return Page();
    }
}
```

### 2. Application Layer

**Components**: Controllers, Middleware, Filters

**Middleware Pipeline**:
```csharp
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();
```

**Custom Middleware**:
- Request logging
- Error handling
- Performance monitoring

### 3. Service Layer

**Components**: Service interfaces and implementations

**Service Categories**:

1. **Domain Services** - Core business logic
   - `CallLogVerificationService`
   - `ClassOfServiceCalculationService`
   - `UserPhoneService`

2. **Infrastructure Services** - Cross-cutting concerns
   - `EmailService`
   - `AuditLogService`
   - `NotificationService`

3. **Utility Services** - Helper functionality
   - `DateFormatDetectorService`
   - `FlexibleDateParserService`
   - `GuidService`

**Service Lifetime**:
- **Scoped**: Most services (per HTTP request)
- **Singleton**: `DateFormatDetectorService` (stateless)
- **Transient**: `EmailService` (lightweight)

### 4. Data Layer

**Components**: DbContext, Entities, Migrations

**DbContext Configuration**:
```csharp
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString, sqlOptions => {
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null
        );
    })
);
```

**Entity Relationships**:
- Configured via Fluent API in `ApplicationDbContext.OnModelCreating()`
- Data annotations on entity classes

---

## Service Architecture

### Service Design Principles

1. **Interface-Based** - All services implement interfaces
2. **Single Responsibility** - One service, one concern
3. **Dependency Injection** - Constructor injection
4. **Async Operations** - All I/O operations async
5. **Transaction Management** - DbContext handles transactions

### Core Services

#### 1. CallLogVerificationService
**Purpose**: Manage call log verification workflow

```csharp
public interface ICallLogVerificationService
{
    Task<CallLogVerification> CreateVerificationAsync(
        string userEmail,
        DateTime period
    );

    Task<bool> SubmitToSupervisorAsync(
        int verificationId
    );

    Task<bool> ApproveVerificationAsync(
        int verificationId,
        string supervisorEmail,
        string comments
    );

    Task<List<CallLogVerification>> GetPendingApprovalsAsync(
        string supervisorEmail
    );
}
```

**Dependencies**:
- `ApplicationDbContext`
- `IEmailService`
- `IAuditLogService`
- `INotificationService`

#### 2. CallLogStagingService
**Purpose**: Import and process call log files

```csharp
public interface ICallLogStagingService
{
    Task<int> ImportCallLogAsync(
        IFormFile file,
        string provider,
        string batchReference
    );

    Task<bool> ProcessBatchAsync(int batchId);

    Task<List<StagingError>> ValidateBatchAsync(int batchId);
}
```

**Processing Flow**:
1. Upload file to temp location
2. Parse CSV/Excel
3. Detect date format (`DateFormatDetectorService`)
4. Stage records in `CallLogStaging`
5. Validate data
6. Process batch → Provider tables
7. Create audit log
8. Send notification

#### 3. ClassOfServiceCalculationService
**Purpose**: Calculate allowances and overages

```csharp
public interface IClassOfServiceCalculationService
{
    Task<decimal> CalculateAllowanceAsync(
        int classOfServiceId,
        DateTime period
    );

    Task<decimal> CalculateUsageAsync(
        int userPhoneId,
        DateTime period
    );

    Task<decimal> CalculateOverageAsync(
        int userPhoneId,
        DateTime period
    );
}
```

**Calculation Logic**:
- Retrieve ClassOfService settings
- Get billing period (Monthly/Quarterly/etc.)
- Sum call costs for period
- Compare against allowance
- Return overage amount

#### 4. AuditLogService
**Purpose**: System-wide audit trail

```csharp
public interface IAuditLogService
{
    Task LogAsync(
        string action,
        string entityType,
        string entityId,
        string changes = null
    );

    Task<List<AuditLog>> GetEntityHistoryAsync(
        string entityType,
        string entityId
    );
}
```

**Logged Actions**:
- Create, Update, Delete operations
- Login, Logout events
- Approve, Reject, Submit actions
- Import, Export operations

#### 5. EmailService
**Purpose**: Send email notifications

```csharp
public interface IEmailService
{
    Task SendEmailAsync(
        string to,
        string subject,
        string body,
        bool isHtml = true
    );

    Task SendTemplateEmailAsync(
        string to,
        string templateName,
        object model
    );
}
```

**Email Scenarios**:
- Verification submitted → Notify supervisor
- Verification approved → Notify user
- Request created → Notify approvers
- System alerts → Notify admins

#### 6. NotificationService
**Purpose**: In-app notifications

```csharp
public interface INotificationService
{
    Task CreateNotificationAsync(
        string userId,
        string title,
        string message,
        string type
    );

    Task<int> GetUnreadCountAsync(string userId);

    Task MarkAsReadAsync(int notificationId);
}
```

---

## Authentication & Authorization

### Authentication Flow

```
User → Azure AD Login → Token Validation → User Provisioning → Access Granted
```

### Azure AD Integration

**Configuration** (`appsettings.json`):
```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "Domain": "your-domain.com",
    "TenantId": "tenant-id",
    "ClientId": "client-id",
    "CallbackPath": "/signin-oidc"
  }
}
```

**Auto-Provisioning** (`Program.cs`):
```csharp
options.Events = new OpenIdConnectEvents
{
    OnTokenValidated = async context =>
    {
        // Extract Azure AD claims
        var objectId = context.Principal?.FindFirst("oid")?.Value;
        var email = context.Principal?.FindFirst(ClaimTypes.Email)?.Value;

        // Find or create ApplicationUser
        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                AzureAdObjectId = objectId,
                EmailConfirmed = true
            };
            await userManager.CreateAsync(user);
        }

        // Link to EbillUser
        var ebillUser = await dbContext.EbillUsers
            .FirstOrDefaultAsync(e => e.Email == email);

        // Assign default role
        if (!await userManager.IsInRoleAsync(user, "User"))
        {
            await userManager.AddToRoleAsync(user, "User");
        }
    }
};
```

### Authorization Model

**Role-Based Access Control (RBAC)**:

```csharp
public static class Roles
{
    public const string Admin = "Admin";
    public const string Supervisor = "Supervisor";
    public const string ICTS = "ICTS";
    public const string ClaimsUnit = "ClaimsUnit";
    public const string BudgetOfficer = "BudgetOfficer";
    public const string PaymentApprover = "PaymentApprover";
    public const string User = "User";
}
```

**Page Authorization**:
```csharp
[Authorize(Roles = "Supervisor")]
public class SupervisorApprovalsModel : PageModel
{
    // Only supervisors can access
}

[Authorize(Roles = "Admin,ICTS")]
public class UserManagementModel : PageModel
{
    // Admins or ICTS can access
}
```

**Conditional UI Rendering**:
```razor
@if (User.IsInRole("Admin"))
{
    <a asp-page="/Admin/Settings">Settings</a>
}
```

### Claims-Based Features

```csharp
// Get current user's email
var userEmail = User.Identity.Name;

// Get specific claim
var indexNumber = User.FindFirst("IndexNumber")?.Value;

// Check if user has permission
if (User.IsInRole("Supervisor"))
{
    // Show approval options
}
```

---

## Data Flow

### Call Log Import Flow

```
1. Admin uploads CSV file
   ↓
2. CallLogStagingService processes file
   ↓
3. DateFormatDetectorService detects format
   ↓
4. Records staged in CallLogStaging table
   ↓
5. Validation runs on batch
   ↓
6. Valid records → Provider tables (Safaricom/Airtel/PSTN)
   ↓
7. CallRecords created
   ↓
8. ImportAudit log created
   ↓
9. Admin notified of completion
```

### Call Log Verification Flow

```
1. User views unverified call logs
   ↓
2. User marks calls as Personal/Official
   ↓
3. User justifies overages
   ↓
4. User uploads supporting documents
   ↓
5. User submits to supervisor
   ↓
6. CallLogVerification created
   ↓
7. Supervisor notified via email + in-app
   ↓
8. Supervisor reviews and approves/rejects
   ↓
9. User notified of decision
   ↓
10. Payment assignment created (if needed)
```

### SIM Request Flow

```
1. User creates SIM request
   ↓
2. SimRequest created (Status: Pending)
   ↓
3. Supervisor notified
   ↓
4. Supervisor approves → Status: PendingSupervisor
   ↓
5. ICTS notified
   ↓
6. ICTS approves → Status: Approved
   ↓
7. User notified
   ↓
8. SIM issued
```

---

## Module Architecture

### Module Structure Pattern

Each module follows a consistent structure:

```
Modules/[ModuleName]/
├── Requests/
│   ├── Index.cshtml         # List view
│   ├── Create.cshtml        # Create form
│   ├── Edit.cshtml          # Edit form
│   └── Details.cshtml       # Detail view
└── Approvals/
    ├── [Role]/
    │   └── Index.cshtml     # Role-specific approvals
    └── Index.cshtml         # General approvals
```

### Module Independence

**Principles**:
1. Modules should be self-contained
2. Shared logic goes in Services layer
3. Cross-module communication via services
4. No direct dependencies between modules

**Shared Services**:
- `AuditLogService` - Used by all modules
- `EmailService` - Used by all modules
- `NotificationService` - Used by all modules

---

## Integration Points

### External Systems

#### 1. Azure Active Directory
**Purpose**: User authentication
**Integration**: Microsoft.Identity.Web
**Data Flow**: Azure AD → ApplicationUser

#### 2. Email Server (SMTP)
**Purpose**: Email notifications
**Integration**: SmtpClient
**Configuration**: EmailSettings in appsettings.json

#### 3. File Storage
**Purpose**: Document storage
**Current**: Local file system
**Future**: Azure Blob Storage

### Internal Integration

#### Service-to-Service Communication

```csharp
public class CallLogVerificationService
{
    private readonly IEmailService _emailService;
    private readonly IAuditLogService _auditLogService;
    private readonly INotificationService _notificationService;

    public async Task SubmitToSupervisorAsync(int verificationId)
    {
        // Update verification status
        // ...

        // Send email notification
        await _emailService.SendEmailAsync(...);

        // Create in-app notification
        await _notificationService.CreateNotificationAsync(...);

        // Log action
        await _auditLogService.LogAsync(...);
    }
}
```

---

## Security Architecture

### Security Layers

```
┌─────────────────────────────────────┐
│   HTTPS / SSL Certificate           │
├─────────────────────────────────────┤
│   Azure AD Authentication           │
├─────────────────────────────────────┤
│   ASP.NET Core Authorization        │
├─────────────────────────────────────┤
│   Anti-Forgery Tokens               │
├─────────────────────────────────────┤
│   Input Validation                  │
├─────────────────────────────────────┤
│   SQL Injection Prevention (EF)     │
├─────────────────────────────────────┤
│   Audit Logging                     │
└─────────────────────────────────────┘
```

### Security Features

#### 1. Authentication
- Azure AD Single Sign-On
- Token-based authentication
- Auto-provisioning of users
- Session management

#### 2. Authorization
- Role-based access control
- Page-level authorization
- Action-level authorization
- Conditional UI rendering

#### 3. Data Protection
- HTTPS enforcement
- Connection string encryption
- Sensitive data not logged
- File upload validation

#### 4. Anti-Forgery
```csharp
[ValidateAntiForgeryToken]
public async Task<IActionResult> OnPostAsync()
{
    // Protected against CSRF
}
```

#### 5. Input Validation
```csharp
[Required]
[MaxLength(100)]
public string Name { get; set; }

[EmailAddress]
public string Email { get; set; }

[Range(0, 999999)]
public decimal Amount { get; set; }
```

#### 6. Audit Trail
- All critical operations logged
- User actions tracked
- Data changes recorded
- Timestamps and IP addresses

---

## Performance & Scalability

### Performance Optimizations

#### 1. Database Level
```csharp
// Async queries
var users = await _context.EbillUsers.ToListAsync();

// Selective loading
var user = await _context.EbillUsers
    .Include(u => u.UserPhones)
    .FirstOrDefaultAsync(u => u.Id == id);

// No-tracking for read-only
var records = await _context.CallRecords
    .AsNoTracking()
    .ToListAsync();

// Pagination
var page = await _context.CallRecords
    .Skip((pageNumber - 1) * pageSize)
    .Take(pageSize)
    .ToListAsync();
```

#### 2. Caching Strategy
```csharp
// Singleton services for stateless operations
builder.Services.AddSingleton<IDateFormatDetectorService, DateFormatDetectorService>();

// In-memory caching for lookup data
private static Dictionary<string, ClassOfService> _cosCache;
```

#### 3. Connection Resiliency
```csharp
options.UseSqlServer(connectionString, sqlOptions => {
    sqlOptions.EnableRetryOnFailure(
        maxRetryCount: 5,
        maxRetryDelay: TimeSpan.FromSeconds(30)
    );
});
```

### Scalability Considerations

#### Horizontal Scaling
- Stateless application design
- Session state in database/cache
- Sticky sessions not required
- Multiple app instances supported

#### Vertical Scaling
- Efficient query design
- Index optimization
- Connection pooling
- Resource limits

#### Database Scaling
- Read replicas for reporting
- Partitioning for large tables
- Archive old data
- Index maintenance

---

## Deployment Architecture

### Development Environment
```
Developer Machine
├── Visual Studio 2022
├── SQL Server LocalDB
├── IIS Express
└── Git
```

### Staging Environment
```
Azure App Service (Staging Slot)
├── .NET 8.0 Runtime
├── Azure SQL Database
├── Azure Blob Storage
└── Application Insights
```

### Production Environment
```
Azure App Service (Production Slot)
├── .NET 8.0 Runtime
├── Azure SQL Database (Geo-replicated)
├── Azure Blob Storage
├── Application Insights
├── Azure Front Door (CDN)
└── Azure Key Vault (Secrets)
```

### Deployment Process

```
1. Developer commits code → GitHub
   ↓
2. Build pipeline runs (Azure DevOps / GitHub Actions)
   ↓
3. Unit tests execute
   ↓
4. Application builds
   ↓
5. Deploy to Staging slot
   ↓
6. Run integration tests
   ↓
7. Manual approval
   ↓
8. Swap Staging → Production
   ↓
9. Health check
   ↓
10. Rollback if needed
```

### Configuration Management

**Development** (`appsettings.Development.json`):
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;..."
  }
}
```

**Production** (`appsettings.Production.json` + Azure App Settings):
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=tcp:server.database.windows.net,..."
  }
}
```

**Azure Key Vault** (Secrets):
- Database passwords
- Azure AD client secrets
- SMTP passwords
- API keys

---

## Error Handling & Logging

### Error Handling Strategy

```csharp
// Global exception handler
app.UseExceptionHandler("/Error");

// Development vs Production
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}
```

### Logging Levels

```csharp
// Critical - System failure
_logger.LogCritical("Database connection failed");

// Error - Operation failure
_logger.LogError("Failed to process call log batch {BatchId}", batchId);

// Warning - Unexpected but handled
_logger.LogWarning("Date format detection uncertain for {FileName}", fileName);

// Information - Normal flow
_logger.LogInformation("User {Email} logged in", userEmail);

// Debug - Detailed diagnostics
_logger.LogDebug("Processing record {RecordId}", recordId);
```

### Application Insights Integration

```csharp
builder.Services.AddApplicationInsightsTelemetry();

// Custom metrics
telemetryClient.TrackMetric("CallRecordsProcessed", count);

// Custom events
telemetryClient.TrackEvent("VerificationSubmitted", properties);

// Dependencies
telemetryClient.TrackDependency("SQL", "GetCallRecords", elapsed);
```

---

## Testing Strategy

### Unit Testing
```csharp
[Fact]
public async Task CalculateOverage_ReturnsCorrectAmount()
{
    // Arrange
    var service = new ClassOfServiceCalculationService(_mockContext.Object);

    // Act
    var overage = await service.CalculateOverageAsync(1, DateTime.Now);

    // Assert
    Assert.Equal(100.00m, overage);
}
```

### Integration Testing
```csharp
[Fact]
public async Task ImportCallLog_CreatesRecords()
{
    // Arrange
    using var context = CreateDbContext();
    var service = new CallLogStagingService(context, ...);

    // Act
    var batchId = await service.ImportCallLogAsync(file, "Safaricom", "BATCH-001");

    // Assert
    var batch = await context.StagingBatches.FindAsync(batchId);
    Assert.NotNull(batch);
}
```

### End-to-End Testing
- Selenium for UI testing
- API testing with HttpClient
- Database state verification

---

## Future Enhancements

### Planned Architecture Improvements

1. **API Layer** - RESTful API for mobile apps
2. **CQRS Pattern** - Separate read/write models for complex queries
3. **Event Sourcing** - For audit trail and history
4. **Message Queue** - Azure Service Bus for async processing
5. **Microservices** - Extract heavy modules into microservices
6. **GraphQL** - Flexible querying for reports

### Technology Upgrades

1. **Blazor** - Interactive UI components
2. **SignalR** - Real-time notifications
3. **gRPC** - High-performance inter-service communication
4. **Docker** - Containerization for deployment
5. **Kubernetes** - Container orchestration

---

## Architecture Decision Records (ADRs)

### ADR-001: Razor Pages over MVC
**Decision**: Use Razor Pages for UI
**Rationale**: Simpler page-focused model, less boilerplate, better for CRUD operations
**Consequences**: Less suitable for complex SPAs

### ADR-002: Service Layer Pattern
**Decision**: Implement dedicated service layer
**Rationale**: Reusable business logic, testability, separation of concerns
**Consequences**: Additional abstraction layer

### ADR-003: EF Core over Dapper
**Decision**: Use Entity Framework Core
**Rationale**: Code-first migrations, change tracking, LINQ support
**Consequences**: Slight performance overhead vs raw SQL

### ADR-004: Azure AD Authentication
**Decision**: Use Azure AD for authentication
**Rationale**: Enterprise SSO, MFA support, user provisioning
**Consequences**: Dependency on Azure AD availability

### ADR-005: Server-Side Rendering
**Decision**: Razor Pages with server-side rendering
**Rationale**: SEO friendly, simpler state management, faster initial load
**Consequences**: More server resources, less client-side interactivity

---

## Diagrams

### System Context Diagram

```
┌─────────────┐
│    Users    │
└──────┬──────┘
       │ HTTPS
       ↓
┌─────────────┐      ┌─────────────┐
│   Azure AD  │◄────►│  TAB Web    │
└─────────────┘      │ Application │
                     └──────┬──────┘
                            │
       ┌────────────────────┼────────────────────┐
       │                    │                    │
       ↓                    ↓                    ↓
┌─────────────┐      ┌─────────────┐     ┌─────────────┐
│  Azure SQL  │      │SMTP Server  │     │ Blob Storage│
│  Database   │      │             │     │             │
└─────────────┘      └─────────────┘     └─────────────┘
```

### Service Dependency Graph

```
┌─────────────────────────────────────┐
│  CallLogVerificationService         │
├─────────────────────────────────────┤
│ Dependencies:                       │
│  • EmailService                     │
│  • AuditLogService                  │
│  • NotificationService              │
│  • ClassOfServiceCalculationService │
└─────────────────────────────────────┘
```

---

## Best Practices

### Code Organization
1. One file per class/interface
2. Organize by feature (modules)
3. Shared code in Services/
4. Consistent naming conventions

### Service Design
1. Interface for every service
2. Constructor injection only
3. Async all the way
4. Return appropriate status codes

### Data Access
1. Use async queries
2. Enable query tracking appropriately
3. Include related data explicitly
4. Paginate large results

### Security
1. Always use HTTPS
2. Validate all inputs
3. Sanitize file uploads
4. Log security events

### Performance
1. Use AsNoTracking for read-only
2. Avoid N+1 queries
3. Cache lookup data
4. Optimize indexes

---

## Glossary

- **CoS**: Class of Service
- **EbillUser**: Extended user profile for billing
- **StagingBatch**: Import batch container
- **Verification Period**: Billing period for verification
- **Payment Assignment**: Responsibility for call charges
- **Overage**: Usage exceeding allowance

---

**Last Updated**: October 2025
**Version**: 1.0
**Maintained By**: Development Team
