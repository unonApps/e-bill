# TAB Web Application

## Overview

TAB (Telecom and Billing) Web is a comprehensive enterprise management system built with ASP.NET Core 8.0 and Razor Pages. It provides a modular platform for managing telecommunications billing, SIM requests, refund processing, and call log verification.

## Key Features

- **Multi-Module Architecture**: Modular design with separate functional areas
- **Azure AD Integration**: Enterprise-grade authentication with auto-provisioning
- **Call Log Management**: Comprehensive telecom billing and verification system
- **SIM Card Management**: Complete lifecycle management for SIM requests
- **Refund Processing**: Multi-level approval workflow for refund requests
- **Audit Trail**: Complete activity logging across all modules
- **Notifications**: In-app notification system for user alerts
- **Exchange Rate Management**: Multi-currency support with USD conversion

## Technology Stack

- **Framework**: ASP.NET Core 8.0
- **UI**: Razor Pages with Bootstrap
- **Database**: SQL Server with Entity Framework Core 8.0
- **Authentication**: Azure AD with Microsoft Identity Web
- **ORM**: Entity Framework Core with Code-First Migrations
- **Email**: SMTP-based email service

## Project Structure

```
TAB.Web/
├── Controllers/           # API Controllers
├── Data/                 # DbContext and database configurations
├── Migrations/           # EF Core database migrations
├── Models/               # Domain models and entities
│   └── Enums/           # Enumeration types
├── Pages/                # Razor Pages
│   ├── Account/         # Authentication pages
│   ├── Admin/           # Administrative functions
│   ├── Dashboard/       # User dashboards
│   ├── Modules/         # Functional modules
│   │   ├── EBillManagement/
│   │   ├── SimManagement/
│   │   └── RefundManagement/
│   ├── Notifications/   # Notification pages
│   └── Shared/          # Shared layouts and partials
├── Services/             # Business logic services
├── Middleware/           # Custom middleware components
└── wwwroot/             # Static files (CSS, JS, images)
```

## Main Modules

### 1. EBill Management
Comprehensive telecom billing and call log management system.

**Features:**
- Call log import and staging
- Multi-provider support (Safaricom, Airtel, PSTN)
- Call record verification workflow
- Supervisor approval process
- Payment assignment tracking
- Document management
- Class of Service (CoS) allowance tracking
- Exchange rate management and USD conversion

**Pages:**
- `/Admin/CallLogs` - Call log management
- `/Admin/CallLogStaging` - Import staging area
- `/Modules/EBillManagement/CallRecords/MyCallLogs` - User's call logs
- `/Modules/EBillManagement/CallRecords/Verify` - Verification interface
- `/Modules/EBillManagement/CallRecords/SupervisorApprovals` - Supervisor review

### 2. SIM Management
SIM card request and approval workflow system.

**Features:**
- SIM request creation and tracking
- Multi-level approval workflow (Supervisor → ICTS)
- Request history and audit trail
- Status tracking and notifications

**Pages:**
- `/Modules/SimManagement/Requests` - Request management
- `/Modules/SimManagement/Approvals/Supervisor` - Supervisor approvals
- `/Modules/SimManagement/Approvals/ICTS` - ICTS approvals

### 3. Refund Management
Multi-level refund request and approval system.

**Features:**
- Refund request submission
- Multi-tier approval workflow (Supervisor → Claims → Budget → Payment)
- Document attachments
- Status tracking

**Pages:**
- `/Modules/RefundManagement/Requests` - Request management
- `/Modules/RefundManagement/Approvals/Supervisor` - Supervisor review
- `/Modules/RefundManagement/Approvals/ClaimsUnit` - Claims processing
- `/Modules/RefundManagement/Approvals/BudgetOfficer` - Budget approval
- `/Modules/RefundManagement/Approvals/PaymentApprover` - Payment authorization

### 4. Admin Panel
Centralized administration and configuration.

**Features:**
- User management
- Role assignment
- EbillUser management
- User phone assignments
- Class of Service configuration
- Service provider management
- Organization and office management
- Exchange rate configuration
- Import audit logs
- System audit logs

## Services (Modular Architecture)

The application follows a service-oriented architecture with clear separation of concerns:

### Core Services
- **EmailService**: Email notifications and communications
- **AuditLogService**: System-wide audit trail logging
- **NotificationService**: In-app notification management
- **GuidService**: Public ID generation and management

### Call Log Services
- **CallLogStagingService**: Import processing and consolidation
- **CallLogVerificationService**: Verification workflow management
- **ClassOfServiceCalculationService**: Allowance and overage calculations
- **CallLogCleanupService**: Data cleanup and maintenance

### User Management Services
- **UserPhoneService**: User phone assignment management
- **EbillUserAccountService**: EbillUser account provisioning

### Utility Services
- **DateFormatDetectorService**: Automatic date format detection
- **FlexibleDateParserService**: Multi-format date parsing
- **DocumentManagementService**: File upload and storage
- **SimRequestHistoryService**: SIM request history tracking

## Database Schema

The application uses SQL Server with Entity Framework Core migrations. Key entities include:

- **User Management**: ApplicationUser, EbillUser, UserPhone
- **Call Logs**: CallLog, CallRecord, CallLogStaging, StagingBatch
- **Providers**: Safaricom, Airtel, PSTN, PrivateWire
- **Verification**: CallLogVerification, CallLogPaymentAssignment, CallLogDocument
- **Configuration**: ClassOfService, ServiceProvider, ExchangeRate
- **Workflow**: SimRequest, RefundRequest, SimRequestHistory
- **System**: AuditLog, Notification, ImportAudit
- **Organization**: Organization, Office, SubOffice

See [DATABASE.md](./docs/DATABASE.md) for detailed schema documentation.

## Getting Started

### Prerequisites
- .NET 8.0 SDK
- SQL Server (LocalDB, Express, or Azure SQL)
- Azure AD tenant (for authentication)

### Configuration

1. **Database Connection**
   Update `appsettings.json`:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=TABDB;Trusted_Connection=True;MultipleActiveResultSets=true"
     }
   }
   ```

2. **Azure AD**
   Configure Azure AD settings:
   ```json
   {
     "AzureAd": {
       "Instance": "https://login.microsoftonline.com/",
       "Domain": "your-domain.com",
       "TenantId": "your-tenant-id",
       "ClientId": "your-client-id",
       "CallbackPath": "/signin-oidc"
     }
   }
   ```

3. **Email Settings**
   Configure SMTP settings:
   ```json
   {
     "EmailSettings": {
       "SmtpServer": "smtp.office365.com",
       "SmtpPort": 587,
       "SmtpUsername": "your-email@domain.com",
       "SmtpPassword": "your-password",
       "SenderEmail": "noreply@domain.com",
       "SenderName": "TAB System"
     }
   }
   ```

### Installation

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd DoNetTemplate.Web
   ```

2. **Restore dependencies**
   ```bash
   dotnet restore
   ```

3. **Update database**
   ```bash
   dotnet ef database update
   ```

4. **Run the application**
   ```bash
   dotnet run
   ```

5. **Access the application**
   Navigate to `https://localhost:5001` (or the port specified in launchSettings.json)

## Database Migrations

The application uses Entity Framework Core migrations for database schema management.

### Common Migration Commands

```bash
# Create a new migration
dotnet ef migrations add MigrationName

# Apply migrations to database
dotnet ef database update

# Generate SQL script for migrations
dotnet ef migrations script --output schema.sql --idempotent

# List all migrations
dotnet ef migrations list

# Remove last migration (if not applied)
dotnet ef migrations remove
```

### Azure Database Deployment

For deploying to Azure SQL Database:

```bash
# Generate migration script
dotnet ef migrations script --output azure-migrations.sql --idempotent

# Apply to Azure (using SQLCMD)
sqlcmd -S tcp:your-server.database.windows.net,1433 -d your-database -U your-user -P your-password -i azure-migrations.sql
```

## User Roles

The application implements role-based access control:

- **Admin**: Full system access
- **Supervisor**: Approval authority for requests
- **ICTS**: SIM request approvals
- **Claims Unit**: Refund claims processing
- **Budget Officer**: Budget approvals
- **Payment Approver**: Final payment authorization
- **User**: Standard user access

## Security Features

- Azure AD integration with auto-provisioning
- Role-based authorization
- Audit logging for all critical operations
- Secure file upload and storage
- Anti-forgery token validation
- HTTPS enforcement

## Development Guidelines

### Adding a New Module

1. Create folder structure in `Pages/Modules/ModuleName/`
2. Create corresponding service interfaces and implementations in `Services/`
3. Add required models to `Models/`
4. Create database migration if needed
5. Update navigation in `_Layout.cshtml`
6. Add role-based authorization attributes
7. Update this documentation

### Code Organization

- **Models**: Domain entities and business objects
- **Services**: Business logic and data access
- **Pages**: UI presentation layer
- **Middleware**: Request pipeline components
- **Data**: Database context and configurations

### Best Practices

- Use dependency injection for all services
- Implement interface-based services for testability
- Follow async/await patterns for database operations
- Use strongly-typed ViewModels for pages
- Implement proper error handling and logging
- Add audit logging for critical operations

## Troubleshooting

### Common Issues

1. **Database Connection Errors**
   - Verify connection string in appsettings.json
   - Ensure SQL Server is running
   - Check firewall rules for Azure SQL

2. **Azure AD Authentication Fails**
   - Verify Azure AD configuration
   - Check redirect URIs in Azure portal
   - Ensure tenant ID and client ID are correct

3. **Migration Errors**
   - Check for conflicting migrations
   - Verify DbContext configuration
   - Review migration history in __EFMigrationsHistory table

## Documentation

- [ARCHITECTURE.md](./docs/ARCHITECTURE.md) - System architecture and design
- [MODULES.md](./docs/MODULES.md) - Detailed module documentation
- [DATABASE.md](./docs/DATABASE.md) - Database schema and relationships
- [API.md](./docs/API.md) - API endpoints and controllers
- [DEPLOYMENT.md](./docs/DEPLOYMENT.md) - Deployment procedures

## Contributing

1. Create a feature branch
2. Make your changes
3. Test thoroughly
4. Submit a pull request

## License

[Your License Here]

## Support

For issues and questions:
- Create an issue in the repository
- Contact the development team

---

**Version**: 1.0
**Last Updated**: October 2025
**Built with**: ASP.NET Core 8.0
