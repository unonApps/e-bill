# TAB Web Application - Modules Documentation

## Table of Contents
- [EBill Management Module](#ebill-management-module)
- [SIM Management Module](#sim-management-module)
- [Refund Management Module](#refund-management-module)
- [Admin Module](#admin-module)
- [Notification Module](#notification-module)
- [Dashboard Module](#dashboard-module)

---

## EBill Management Module

### Overview
The EBill Management Module is the core telecom billing system that handles call log imports, verification, approval workflows, and payment assignments.

### Module Structure
```
Pages/Modules/EBillManagement/
├── CallRecords/
│   ├── MyCallLogs.cshtml                 # User's call log view
│   ├── Verify.cshtml                     # Verification interface
│   ├── SubmitToSupervisor.cshtml        # Submission workflow
│   ├── SupervisorApprovals.cshtml       # Supervisor approval queue
│   ├── ReviewVerification.cshtml        # Review submitted verifications
│   ├── PaymentAssignments.cshtml        # Payment assignment management
│   └── ApprovalHistory.cshtml           # Historical approval records
├── Approvals/
│   ├── Index.cshtml                     # Approval dashboard
│   └── Supervisor/
│       └── Index.cshtml                 # Supervisor approval interface
└── Requests/
    └── Index.cshtml                      # Request listing
```

### Key Features

#### 1. Call Log Import System
**Purpose**: Import and process call logs from multiple telecom providers

**Supported Providers**:
- Safaricom (Mobile)
- Airtel (Mobile)
- PSTN (Landline)
- Private Wire (Dedicated lines)

**Process Flow**:
1. Admin uploads CSV/Excel file
2. System detects date format automatically
3. Data staged in `CallLogStaging` table
4. Batch validation and reconciliation
5. Records moved to provider-specific tables
6. Call records created for verification

**Related Services**:
- `CallLogStagingService` - Handles import and staging
- `DateFormatDetectorService` - Auto-detects date formats
- `FlexibleDateParserService` - Parses various date formats

**Database Tables**:
- `CallLogStaging` - Temporary staging area
- `StagingBatch` - Import batch tracking
- `Safaricom`, `Airtel`, `PSTN`, `PrivateWire` - Provider tables
- `CallLog` - Consolidated call log header
- `CallRecord` - Individual call records

#### 2. Call Log Verification System
**Purpose**: Allow users to verify their call logs and explain overages

**Workflow**:
```
User Reviews Call Logs → Marks Records → Justifies Overages → Submits to Supervisor
                                                                         ↓
                                                              Supervisor Reviews
                                                                         ↓
                                                        Approves/Rejects/Returns
```

**User Actions**:
- View call logs for verification period
- Review call details (number, duration, cost)
- Mark personal vs business calls
- Justify overage against Class of Service allowances
- Attach supporting documents
- Submit for supervisor approval

**Verification Types**:
- `SelfVerification` - User verifies own calls
- `DelegatedVerification` - Supervisor verifies on behalf
- `SystemVerification` - Automated verification

**Related Services**:
- `CallLogVerificationService` - Core verification logic
- `ClassOfServiceCalculationService` - Allowance calculations
- `DocumentManagementService` - Document handling

**Database Tables**:
- `CallLogVerification` - Verification records
- `CallLogPaymentAssignment` - Payment responsibility
- `CallLogDocument` - Supporting documents
- `CallRecord` - Call records with verification status

#### 3. Class of Service (CoS) System
**Purpose**: Define and enforce call allowances based on user roles

**Allowance Types**:
- Airtime Allowance (voice calls)
- Data Allowance (mobile data)
- Handset Allowance (device cost)

**Billing Periods**:
- Monthly
- Quarterly
- Semi-Annual
- Annual

**Features**:
- Automatic overage calculation
- Pro-rata allowance for partial periods
- USD conversion support
- Multiple service provider tracking

**Related Service**:
- `ClassOfServiceCalculationService` - Calculation engine

**Database Table**:
- `ClassOfService` - CoS definitions

#### 4. Supervisor Approval System
**Purpose**: Multi-level approval for call log verifications

**Approval Levels**:
1. **User Verification** - User verifies and submits
2. **Supervisor Review** - Direct supervisor approves/rejects
3. **Payment Assignment** - Final payment responsibility

**Supervisor Actions**:
- Review submitted verifications
- View call details and justifications
- Review supporting documents
- Approve/Reject/Return for revision
- Add approval comments

**Statuses**:
- `Pending` - Awaiting action
- `Approved` - Supervisor approved
- `Rejected` - Supervisor rejected
- `Returned` - Returned for revision
- `UnderReview` - Currently being reviewed

**Related Service**:
- `CallLogVerificationService` - Approval workflow

#### 5. Payment Assignment System
**Purpose**: Track who pays for call charges

**Assignment Types**:
- `Personal` - User pays personally
- `Official` - Organization pays
- `Shared` - Split payment
- `Waived` - Charges waived

**Features**:
- Overage justification tracking
- Payment responsibility assignment
- Cost allocation
- Historical payment records

**Database Table**:
- `CallLogPaymentAssignment` - Payment assignments

#### 6. Exchange Rate Management
**Purpose**: Convert local currency (KSH) to USD for reporting

**Features**:
- Multiple currency support
- Historical rate tracking
- Automatic USD conversion
- Rate effective dating

**Fields**:
- Source Currency (e.g., KSH)
- Target Currency (USD)
- Exchange Rate
- Effective Date
- Is Active flag

**Database Table**:
- `ExchangeRate` - Exchange rate definitions

### Services

#### CallLogStagingService
```csharp
public interface ICallLogStagingService
{
    Task<int> ImportCallLogAsync(IFormFile file, string provider, string batchReference);
    Task<bool> ValidateBatchAsync(int batchId);
    Task<bool> ProcessBatchAsync(int batchId);
    Task<List<CallLogStaging>> GetStagingRecordsAsync(int batchId);
}
```

**Responsibilities**:
- Import CSV/Excel files
- Stage records for validation
- Batch processing
- Data consolidation
- Error handling and reporting

#### CallLogVerificationService
```csharp
public interface ICallLogVerificationService
{
    Task<CallLogVerification> CreateVerificationAsync(string userEmail, DateTime period);
    Task<bool> SubmitToSupervisorAsync(int verificationId);
    Task<bool> ApproveVerificationAsync(int verificationId, string supervisorEmail);
    Task<bool> RejectVerificationAsync(int verificationId, string reason);
    Task<List<CallLogVerification>> GetPendingApprovalsAsync(string supervisorEmail);
}
```

**Responsibilities**:
- Verification lifecycle management
- Supervisor approval workflow
- Status transitions
- Email notifications
- History tracking

#### ClassOfServiceCalculationService
```csharp
public interface IClassOfServiceCalculationService
{
    Task<decimal> CalculateAllowanceAsync(int classOfServiceId, DateTime period);
    Task<decimal> CalculateOverageAsync(int userPhoneId, DateTime period);
    Task<bool> IsWithinAllowanceAsync(int userPhoneId, DateTime period);
}
```

**Responsibilities**:
- Allowance calculations
- Overage detection
- Pro-rata adjustments
- Multi-period support

### Pages

#### MyCallLogs.cshtml
**Route**: `/Modules/EBillManagement/CallRecords/MyCallLogs`
**Purpose**: Display user's call logs for current period
**Access**: All authenticated users

**Features**:
- Filter by verification period
- View call details
- See allowance vs usage
- Access verification workflow

#### Verify.cshtml
**Route**: `/Modules/EBillManagement/CallRecords/Verify`
**Purpose**: Verify call logs and justify overages
**Access**: All authenticated users

**Features**:
- Select verification period
- Review individual calls
- Mark call types
- Add justifications
- Upload documents
- Submit for approval

#### SupervisorApprovals.cshtml
**Route**: `/Modules/EBillManagement/CallRecords/SupervisorApprovals`
**Purpose**: Supervisor approval interface
**Access**: Supervisors only

**Features**:
- View pending verifications
- Review call details
- View justifications
- Download documents
- Approve/reject/return
- Add comments

---

## SIM Management Module

### Overview
SIM card request and approval workflow system for managing SIM card lifecycle.

### Module Structure
```
Pages/Modules/SimManagement/
├── Requests/
│   ├── Index.cshtml          # Request listing
│   ├── Create.cshtml         # New request form
│   ├── Edit.cshtml           # Edit request
│   └── Details.cshtml        # Request details
└── Approvals/
    ├── Index.cshtml          # Approval dashboard
    ├── Supervisor/
    │   └── Index.cshtml      # Supervisor approvals
    └── ICTS/
        └── Index.cshtml      # ICTS approvals
```

### Workflow

```
User Creates Request → Supervisor Review → ICTS Approval → SIM Issued
       ↓                     ↓                  ↓              ↓
    Pending            PendingSupervisor   PendingICTS    Approved/Issued
```

### Request Types
- New SIM
- Replacement SIM
- Additional SIM
- SIM Upgrade

### Features

#### 1. Request Creation
- Requester information
- Justification
- Request type selection
- Supporting documents

#### 2. Approval Workflow
- Multi-level approval (Supervisor → ICTS)
- Email notifications at each stage
- Comments and feedback
- Status tracking

#### 3. History Tracking
- Complete request history
- Approval timeline
- Status changes
- User actions

### Services

#### SimRequestHistoryService
```csharp
public interface ISimRequestHistoryService
{
    Task AddHistoryEntryAsync(int requestId, string action, string userId);
    Task<List<SimRequestHistory>> GetRequestHistoryAsync(int requestId);
}
```

**Responsibilities**:
- Track request changes
- Log user actions
- Maintain audit trail
- Generate history reports

### Database Tables
- `SimRequest` - Request records
- `SimRequestHistory` - Request history

---

## Refund Management Module

### Overview
Multi-tier refund request and approval workflow system.

### Module Structure
```
Pages/Modules/RefundManagement/
├── Requests/
│   ├── Index.cshtml          # Request listing
│   ├── Create.cshtml         # New request form
│   └── View.cshtml           # Request details
└── Approvals/
    ├── Index.cshtml          # Approval dashboard
    ├── Supervisor/
    │   └── Index.cshtml      # Supervisor approvals
    ├── ClaimsUnit/
    │   └── Index.cshtml      # Claims processing
    ├── BudgetOfficer/
    │   └── Index.cshtml      # Budget approvals
    └── PaymentApprover/
        └── Index.cshtml      # Payment approvals
```

### Workflow

```
User Request → Supervisor → Claims Unit → Budget Officer → Payment Approver → Paid
     ↓             ↓            ↓              ↓                  ↓            ↓
  Pending      Approved     Processed      Budgeted         Authorized     Paid
```

### Approval Levels

1. **Supervisor** - Validates request legitimacy
2. **Claims Unit** - Processes claim documentation
3. **Budget Officer** - Verifies budget availability
4. **Payment Approver** - Authorizes payment

### Features
- Multi-currency support
- Document attachments
- Email notifications
- Status tracking
- Rejection workflow with reasons

### Database Table
- `RefundRequest` - Refund records

---

## Admin Module

### Overview
Centralized administration panel for system configuration and management.

### Module Structure
```
Pages/Admin/
├── UserManagement.cshtml         # User and role management
├── EbillUsers.cshtml            # EbillUser management
├── UserPhones.cshtml            # User phone assignments
├── ClassOfService.cshtml        # CoS configuration
├── CallLogs.cshtml              # Call log management
├── CallLogStaging.cshtml        # Import staging
├── CallLogsUpload.cshtml        # File upload interface
├── ExchangeRates.cshtml         # Exchange rate management
├── ServiceProvider.cshtml       # Service provider management
├── Organizations.cshtml         # Organization management
├── Offices.cshtml               # Office management
├── EmailSettings.cshtml         # Email configuration
├── AuditLogs.cshtml            # System audit logs
└── ImportAudits.cshtml         # Import audit logs
```

### Key Features

#### 1. User Management
- Create/edit ApplicationUsers
- Assign roles
- Enable/disable accounts
- Reset passwords
- Link Azure AD accounts

#### 2. EbillUser Management
- Manage EbillUser records
- Link to ApplicationUsers
- Assign supervisors
- Set index numbers
- Configure user phones

#### 3. User Phone Management
- Assign phones to users
- Track multiple phones per user
- Set phone status (Active/Inactive)
- Link to Class of Service
- Manage billing periods

#### 4. Class of Service Configuration
- Define CoS levels
- Set allowance amounts
- Configure billing periods
- Manage provider associations

#### 5. Exchange Rate Management
- Add new exchange rates
- Set effective dates
- Activate/deactivate rates
- Historical rate tracking

#### 6. Audit Logging
- System-wide activity tracking
- User action logs
- Data change history
- Import audit trails

### Access Control
**Required Role**: Admin

---

## Notification Module

### Overview
In-app notification system for user alerts and updates.

### Module Structure
```
Pages/Notifications/
└── Index.cshtml              # Notification center
```

### Notification Types
- `Info` - Informational messages
- `Warning` - Warning messages
- `Error` - Error notifications
- `Success` - Success confirmations

### Features
- Real-time notifications
- Read/unread status
- Notification categories
- Bulk mark as read
- Notification history

### Services

#### NotificationService
```csharp
public interface INotificationService
{
    Task CreateNotificationAsync(string userId, string title, string message, string type);
    Task<List<Notification>> GetUserNotificationsAsync(string userId);
    Task MarkAsReadAsync(int notificationId);
    Task<int> GetUnreadCountAsync(string userId);
}
```

**Responsibilities**:
- Create notifications
- Track read status
- Deliver to users
- Manage notification lifecycle

### Database Table
- `Notification` - Notification records

---

## Dashboard Module

### Overview
Role-based dashboard for quick access to pending tasks.

### Module Structure
```
Pages/Dashboard/
└── Approver/
    └── Index.cshtml          # Approver dashboard
```

### Dashboard Types

#### Approver Dashboard
**Access**: Users with approval roles

**Displays**:
- Pending SIM requests
- Pending refund requests
- Pending call log verifications
- Recent approvals
- Quick statistics

#### User Dashboard (Home)
**Access**: All authenticated users

**Displays**:
- Personal call logs
- Pending verifications
- Recent requests
- Notifications
- Quick actions

---

## Module Integration

### Cross-Module Services

#### AuditLogService
**Scope**: All modules

**Purpose**: Track all critical operations

**Features**:
- User action logging
- Data change tracking
- Access logging
- Report generation

#### EmailService
**Scope**: All modules

**Purpose**: Send email notifications

**Features**:
- SMTP integration
- Template support
- Attachment handling
- Delivery tracking

### Shared Components

#### Navigation (`_Layout.cshtml`)
- Role-based menu rendering
- Module navigation
- User profile menu
- Notification bell

#### Authentication (`_LoginPartial.cshtml`)
- Azure AD login
- User profile display
- Logout functionality

---

## Adding New Modules

### Step-by-Step Guide

1. **Create Module Structure**
   ```
   Pages/Modules/YourModule/
   ├── Requests/
   └── Approvals/
   ```

2. **Create Models**
   ```csharp
   Models/YourModuleRequest.cs
   ```

3. **Create Services**
   ```csharp
   Services/IYourModuleService.cs
   Services/YourModuleService.cs
   ```

4. **Register Services** (Program.cs)
   ```csharp
   builder.Services.AddScoped<IYourModuleService, YourModuleService>();
   ```

5. **Add Navigation** (_Layout.cshtml)
   ```html
   <li class="nav-item">
       <a class="nav-link" asp-page="/Modules/YourModule/Index">Your Module</a>
   </li>
   ```

6. **Create Database Migration**
   ```bash
   dotnet ef migrations add AddYourModule
   dotnet ef database update
   ```

7. **Add Authorization**
   ```csharp
   [Authorize(Roles = "RequiredRole")]
   public class IndexModel : PageModel
   ```

8. **Update Documentation**
   - Add to this file (MODULES.md)
   - Update README.md
   - Update ARCHITECTURE.md

---

## Best Practices

### Module Design
- Keep modules independent
- Use dependency injection
- Implement service interfaces
- Follow consistent naming

### Service Layer
- One service per module
- Interface-based design
- Async operations
- Proper error handling

### Data Access
- Use DbContext through DI
- Implement repository pattern if needed
- Use async queries
- Enable query tracking appropriately

### Security
- Role-based authorization
- Validate all inputs
- Sanitize file uploads
- Audit critical operations

### Testing
- Unit test services
- Integration test workflows
- Test authorization
- Validate business logic

---

**Last Updated**: October 2025
**Version**: 1.0
