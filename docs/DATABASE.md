# TAB Web Application - Database Schema Documentation

## Table of Contents
- [Overview](#overview)
- [Entity Relationship Diagram](#entity-relationship-diagram)
- [Core Tables](#core-tables)
- [User Management](#user-management)
- [Call Log System](#call-log-system)
- [Workflow Tables](#workflow-tables)
- [Configuration Tables](#configuration-tables)
- [System Tables](#system-tables)
- [Enumerations](#enumerations)
- [Indexes and Constraints](#indexes-and-constraints)

---

## Overview

The TAB database uses SQL Server and is managed through Entity Framework Core Code-First migrations. The schema is designed to support:

- Multi-tenant user management
- Telecom billing and verification
- Workflow approvals
- Audit logging
- Multi-currency support

**Database Name**: TABDB
**Entity Framework Version**: 8.0
**Migration History Table**: `__EFMigrationsHistory`

---

## Entity Relationship Diagram

### High-Level Relationships

```
ApplicationUser (1) ←→ (*) EbillUser
     ↓
     (*) AspNetUserRoles

EbillUser (1) ←→ (*) UserPhone ←→ (1) ClassOfService
     ↓                ↓
     (*) CallLogVerification    (*) CallRecord
           ↓                          ↓
           (*) CallLogDocument        (*) CallLogPaymentAssignment
```

---

## Core Tables

### ApplicationUser (AspNetUsers)
**Purpose**: ASP.NET Core Identity user accounts

**Schema**: Built-in Identity schema with custom extensions

**Key Fields**:
```sql
Id                      nvarchar(450)    PRIMARY KEY
Email                   nvarchar(256)    UNIQUE
UserName                nvarchar(256)
AzureAdObjectId         nvarchar(450)    -- Azure AD integration
AzureAdTenantId         nvarchar(450)
EmailConfirmed          bit
PhoneNumber             nvarchar(max)
TwoFactorEnabled        bit
LockoutEnabled          bit
AccessFailedCount       int
```

**Relationships**:
- Links to EbillUser via Email
- Links to AspNetUserRoles for role assignments
- Azure AD integration via AzureAdObjectId

**Indexes**:
- `IX_AspNetUsers_AzureAdObjectId` (Unique)
- `IX_AspNetUsers_Email` (Unique)

---

### EbillUser
**Purpose**: Extended user profile for telecom billing

**Schema**:
```sql
CREATE TABLE [dbo].[EbillUsers] (
    [Id]                      int IDENTITY(1,1) PRIMARY KEY,
    [IndexNumber]             nvarchar(50)      UNIQUE NOT NULL,
    [FirstName]               nvarchar(100)     NOT NULL,
    [LastName]                nvarchar(100)     NOT NULL,
    [Email]                   nvarchar(256)     UNIQUE NOT NULL,
    [PhoneNumber]             nvarchar(20),
    [Department]              nvarchar(100),
    [Position]                nvarchar(100),
    [OfficeId]                int,
    [OrganizationId]          int,
    [SupervisorIndexNumber]   nvarchar(50),
    [IsActive]                bit               NOT NULL DEFAULT 1,
    [DateCreated]             datetime2         NOT NULL,
    [DateModified]            datetime2
)
```

**Relationships**:
- `Email` links to `ApplicationUser.Email`
- `OfficeId` → `Office.Id`
- `OrganizationId` → `Organization.Id`
- `SupervisorIndexNumber` → Self-referential to another `EbillUser.IndexNumber`

**Business Rules**:
- IndexNumber must be unique
- Email must match ApplicationUser email for login
- SupervisorIndexNumber defines reporting structure

---

### UserPhone
**Purpose**: User phone assignments with Class of Service

**Schema**:
```sql
CREATE TABLE [dbo].[UserPhones] (
    [Id]                  int IDENTITY(1,1) PRIMARY KEY,
    [IndexNumber]         nvarchar(50)      NOT NULL,
    [PhoneNumber]         nvarchar(20)      NOT NULL,
    [ServiceProvider]     nvarchar(50)      NOT NULL,
    [ClassOfServiceId]    int               NOT NULL,
    [DateAssigned]        datetime2         NOT NULL,
    [DateDeactivated]     datetime2,
    [IsActive]            bit               NOT NULL DEFAULT 1,
    [PhoneStatus]         nvarchar(20)      DEFAULT 'Active',
    [Notes]               nvarchar(500)
)
```

**Relationships**:
- `IndexNumber` → `EbillUser.IndexNumber`
- `ClassOfServiceId` → `ClassOfService.Id`

**Phone Status Values**:
- `Active` - Currently in use
- `Inactive` - Deactivated
- `Suspended` - Temporarily suspended
- `Retired` - Permanently retired

**Indexes**:
- `IX_UserPhones_IndexNumber`
- `IX_UserPhones_PhoneNumber`
- `IX_UserPhones_ClassOfServiceId`

---

## Call Log System

### CallLog
**Purpose**: Call log header/container

**Schema**:
```sql
CREATE TABLE [dbo].[CallLogs] (
    [Id]                  int IDENTITY(1,1) PRIMARY KEY,
    [BillingPeriod]       datetime2         NOT NULL,
    [ServiceProvider]     nvarchar(50)      NOT NULL,
    [ImportDate]          datetime2         NOT NULL,
    [ImportedBy]          nvarchar(256),
    [RecordCount]         int               NOT NULL DEFAULT 0,
    [TotalCost]           decimal(18,4)     NOT NULL DEFAULT 0,
    [TotalCostUSD]        decimal(18,4)     NOT NULL DEFAULT 0,
    [Status]              nvarchar(20)      NOT NULL DEFAULT 'Imported',
    [Notes]               nvarchar(max)
)
```

**Status Values**:
- `Imported` - Newly imported
- `Staged` - In staging area
- `Processing` - Being processed
- `Verified` - Verification complete
- `Finalized` - Closed

---

### CallLogStaging
**Purpose**: Temporary staging area for imported call logs

**Schema**:
```sql
CREATE TABLE [dbo].[CallLogStaging] (
    [Id]                  int IDENTITY(1,1) PRIMARY KEY,
    [StagingBatchId]      int               NOT NULL,
    [ImportType]          nvarchar(50)      NOT NULL,
    [ExtensionNumber]     nvarchar(50),
    [CallDate]            nvarchar(50),     -- String for flexible parsing
    [CallNumber]          nvarchar(50),
    [Destination]         nvarchar(100),
    [Duration]            nvarchar(50),
    [Cost]                nvarchar(50),
    [CallType]            nvarchar(50),
    [CurrencyCode]        nvarchar(10),
    [RawData]             nvarchar(max),    -- Original CSV row
    [ProcessingStatus]    nvarchar(20)      DEFAULT 'Pending',
    [ErrorMessage]        nvarchar(max),
    [CreatedDate]         datetime2         NOT NULL
)
```

**Processing Status Values**:
- `Pending` - Awaiting processing
- `Validated` - Validation passed
- `Failed` - Validation failed
- `Processed` - Successfully processed

**Relationships**:
- `StagingBatchId` → `StagingBatch.Id`

---

### StagingBatch
**Purpose**: Track import batches

**Schema**:
```sql
CREATE TABLE [dbo].[StagingBatches] (
    [Id]                  int IDENTITY(1,1) PRIMARY KEY,
    [BatchReference]      nvarchar(100)     UNIQUE NOT NULL,
    [ImportType]          nvarchar(50)      NOT NULL,
    [FileName]            nvarchar(255)     NOT NULL,
    [ImportedBy]          nvarchar(256)     NOT NULL,
    [ImportDate]          datetime2         NOT NULL,
    [TotalRecords]        int               NOT NULL DEFAULT 0,
    [SuccessCount]        int               NOT NULL DEFAULT 0,
    [ErrorCount]          int               NOT NULL DEFAULT 0,
    [Status]              nvarchar(20)      NOT NULL DEFAULT 'Pending',
    [ProcessedDate]       datetime2,
    [DateFormat]          nvarchar(50),     -- Detected date format
    [Notes]               nvarchar(max)
)
```

---

### CallRecord
**Purpose**: Individual call records

**Schema**:
```sql
CREATE TABLE [dbo].[CallRecords] (
    [Id]                      int IDENTITY(1,1) PRIMARY KEY,
    [CallLogId]               int,
    [UserPhoneId]             int,

    -- Call Details
    [ext_no]                  nvarchar(50),
    [call_date]               datetime2         NOT NULL,
    [call_number]             nvarchar(50),
    [call_destination]        nvarchar(100),
    [call_endtime]            datetime2,
    [call_duration]           int,              -- seconds
    [call_type]               nvarchar(50),
    [call_dest_type]          nvarchar(50),

    -- Cost Information
    [call_cost]               decimal(18,4)     NOT NULL,
    [call_cost_usd]           decimal(18,4)     NOT NULL,
    [call_cost_kshs]          decimal(18,4)     NOT NULL,
    [call_curr_code]          nvarchar(10),

    -- User Information
    [ext_resp_index]          nvarchar(50),     -- Responsible user
    [call_pay_index]          nvarchar(50),     -- Paying user

    -- Verification
    [call_ver_ind]            bit               DEFAULT 0,
    [call_ver_date]           datetime2,
    [verification_period]     datetime2,
    [verification_type]       nvarchar(20),

    -- Payment Assignment
    [payment_assignment_id]   int,
    [assignment_status]       nvarchar(20),
    [overage_justified]       bit               DEFAULT 0,

    -- Supervisor Approval
    [supervisor_approval_status] nvarchar(20),
    [supervisor_approved_by]  nvarchar(256),
    [supervisor_approved_date] datetime2,

    -- Metadata
    [call_year]               int,
    [call_month]              int,
    [created_date]            datetime2         NOT NULL,
    [modified_date]           datetime2
)
```

**Relationships**:
- `CallLogId` → `CallLog.Id`
- `UserPhoneId` → `UserPhone.Id`
- `payment_assignment_id` → `CallLogPaymentAssignment.Id`

**Indexes**:
- `IX_CallRecords_CallLogId`
- `IX_CallRecords_UserPhoneId`
- `IX_CallRecords_ext_no`
- `IX_CallRecords_call_date`
- `IX_CallRecords_verification_period`

---

### Provider-Specific Tables

#### Safaricom
```sql
CREATE TABLE [dbo].[Safaricom] (
    [Id]                  int IDENTITY(1,1) PRIMARY KEY,
    [PhoneNumber]         nvarchar(20)      NOT NULL,
    [CallDate]            datetime2         NOT NULL,
    [Destination]         nvarchar(100),
    [Duration]            int,
    [Cost]                decimal(18,4),
    [CostUSD]             decimal(18,4),
    [CallType]            nvarchar(50),
    [BillingPeriod]       datetime2,
    [ImportBatchId]       int,
    [ProcessingStatus]    nvarchar(20)      DEFAULT 'Pending'
)
```

#### Airtel
```sql
CREATE TABLE [dbo].[Airtel] (
    [Id]                  int IDENTITY(1,1) PRIMARY KEY,
    [PhoneNumber]         nvarchar(20)      NOT NULL,
    [CallDate]            datetime2         NOT NULL,
    [Destination]         nvarchar(100),
    [Duration]            int,
    [Cost]                decimal(18,4),
    [CostUSD]             decimal(18,4),
    [CallType]            nvarchar(50),
    [BillingPeriod]       datetime2,
    [ImportBatchId]       int,
    [ProcessingStatus]    nvarchar(20)      DEFAULT 'Pending'
)
```

#### PSTN
```sql
CREATE TABLE [dbo].[PSTN] (
    [Id]                  int IDENTITY(1,1) PRIMARY KEY,
    [ExtensionNumber]     nvarchar(50)      NOT NULL,
    [CallDate]            datetime2         NOT NULL,
    [Destination]         nvarchar(100),
    [Duration]            int,
    [Cost]                decimal(18,4),
    [CostUSD]             decimal(18,4),
    [CallType]            nvarchar(50),
    [BillingPeriod]       datetime2,
    [ImportBatchId]       int,
    [ProcessingStatus]    nvarchar(20)      DEFAULT 'Pending'
)
```

#### PrivateWire
```sql
CREATE TABLE [dbo].[PrivateWires] (
    [Id]                  int IDENTITY(1,1) PRIMARY KEY,
    [CircuitReference]    nvarchar(50)      NOT NULL,
    [BillingPeriod]       datetime2         NOT NULL,
    [MonthlyCost]         decimal(18,4),
    [AmountKSH]           decimal(18,4),
    [CostUSD]             decimal(18,4),
    [Location]            nvarchar(100),
    [Bandwidth]           nvarchar(50),
    [ResponsibleIndex]    nvarchar(50),
    [Status]              nvarchar(20)
)
```

---

## Verification System Tables

### CallLogVerification
**Purpose**: Track call log verification submissions

**Schema**:
```sql
CREATE TABLE [dbo].[CallLogVerifications] (
    [Id]                      int IDENTITY(1,1) PRIMARY KEY,
    [VerificationPeriod]      datetime2         NOT NULL,
    [VerifiedBy]              nvarchar(256)     NOT NULL,
    [IndexNumber]             nvarchar(50)      NOT NULL,
    [UserPhoneId]             int               NOT NULL,

    -- Submission
    [SubmittedToSupervisor]   bit               DEFAULT 0,
    [SubmittedDate]           datetime2,

    -- Supervisor Review
    [SupervisorIndexNumber]   nvarchar(50),
    [SupervisorApprovalStatus] nvarchar(20)     DEFAULT 'Pending',
    [SupervisorComments]      nvarchar(max),
    [ApprovedDate]            datetime2,

    -- Summary
    [TotalCalls]              int               DEFAULT 0,
    [PersonalCalls]           int               DEFAULT 0,
    [OfficialCalls]           int               DEFAULT 0,
    [OverageAmount]           decimal(18,4)     DEFAULT 0,
    [OverageJustified]        bit               DEFAULT 0,
    [JustificationNotes]      nvarchar(max),

    -- Metadata
    [CreatedDate]             datetime2         NOT NULL,
    [ModifiedDate]            datetime2
)
```

**Relationships**:
- `UserPhoneId` → `UserPhone.Id`
- `IndexNumber` → `EbillUser.IndexNumber`
- `SupervisorIndexNumber` → `EbillUser.IndexNumber`

**Approval Status Values**:
- `Pending` - Awaiting review
- `Approved` - Fully approved
- `PartiallyApproved` - Partially approved
- `Rejected` - Rejected
- `Reverted` - Sent back for revision

---

### CallLogPaymentAssignment
**Purpose**: Track payment responsibility for calls

**Schema**:
```sql
CREATE TABLE [dbo].[CallLogPaymentAssignments] (
    [Id]                      int IDENTITY(1,1) PRIMARY KEY,
    [CallRecordId]            int               NOT NULL,
    [VerificationId]          int,
    [OriginalOwnerIndex]      nvarchar(50)      NOT NULL,
    [AssignedToIndex]         nvarchar(50),
    [AssignmentType]          nvarchar(20)      NOT NULL,
    [Amount]                  decimal(18,4)     NOT NULL,
    [Justification]           nvarchar(max),
    [AssignmentStatus]        nvarchar(20)      DEFAULT 'None',
    [AssignedDate]            datetime2,
    [AcceptedDate]            datetime2,
    [CreatedBy]               nvarchar(256)     NOT NULL,
    [CreatedDate]             datetime2         NOT NULL
)
```

**Assignment Type Values**:
- `Personal` - User pays personally
- `Official` - Organization pays
- `Shared` - Split payment
- `Waived` - Charges waived
- `Transferred` - Transferred to another user

**Assignment Status Values**: (from Enums)
- `None` - No assignment
- `Pending` - Awaiting acceptance
- `Accepted` - Accepted
- `Rejected` - Rejected
- `Reassigned` - Reassigned

**Relationships**:
- `CallRecordId` → `CallRecord.Id`
- `VerificationId` → `CallLogVerification.Id`

---

### CallLogDocument
**Purpose**: Store supporting documents for verifications

**Schema**:
```sql
CREATE TABLE [dbo].[CallLogDocuments] (
    [Id]                  int IDENTITY(1,1) PRIMARY KEY,
    [VerificationId]      int               NOT NULL,
    [DocumentType]        nvarchar(50)      NOT NULL,
    [FileName]            nvarchar(255)     NOT NULL,
    [FilePath]            nvarchar(500)     NOT NULL,
    [FileSize]            bigint,
    [ContentType]         nvarchar(100),
    [UploadedBy]          nvarchar(256)     NOT NULL,
    [UploadedDate]        datetime2         NOT NULL,
    [Description]         nvarchar(500)
)
```

**Document Type Values**: (from Enums)
- `OverageJustification`
- `ApprovalLetter`
- `Receipt`
- `Other`

**Relationships**:
- `VerificationId` → `CallLogVerification.Id`

---

## Configuration Tables

### ClassOfService
**Purpose**: Define allowances and billing rules

**Schema**:
```sql
CREATE TABLE [dbo].[ClassOfServices] (
    [Id]                      int IDENTITY(1,1) PRIMARY KEY,
    [Name]                    nvarchar(100)     UNIQUE NOT NULL,
    [Description]             nvarchar(500),
    [AirtimeAllowanceAmount]  decimal(18,4)     DEFAULT 0,
    [DataAllowanceAmount]     decimal(18,4)     DEFAULT 0,
    [HandsetAllowanceAmount]  decimal(18,4)     DEFAULT 0,
    [BillingPeriod]           nvarchar(20)      NOT NULL,
    [IsActive]                bit               DEFAULT 1,
    [CreatedDate]             datetime2         NOT NULL,
    [ModifiedDate]            datetime2
)
```

**Billing Period Values**:
- `Monthly`
- `Quarterly`
- `SemiAnnual`
- `Annual`

**Usage**:
- Linked to UserPhone for allowance enforcement
- Used by ClassOfServiceCalculationService for overage calculations

---

### ExchangeRate
**Purpose**: Currency conversion rates

**Schema**:
```sql
CREATE TABLE [dbo].[ExchangeRates] (
    [Id]                  int IDENTITY(1,1) PRIMARY KEY,
    [FromCurrency]        nvarchar(3)       NOT NULL,
    [ToCurrency]          nvarchar(3)       NOT NULL,
    [Rate]                decimal(18,6)     NOT NULL,
    [EffectiveDate]       datetime2         NOT NULL,
    [IsActive]            bit               DEFAULT 1,
    [CreatedBy]           nvarchar(256),
    [CreatedDate]         datetime2         NOT NULL
)
```

**Indexes**:
- `IX_ExchangeRates_FromCurrency_ToCurrency_EffectiveDate`

**Common Pairs**:
- KSH → USD
- USD → KSH

---

### ServiceProvider
**Purpose**: Telecom service provider information

**Schema**:
```sql
CREATE TABLE [dbo].[ServiceProviders] (
    [Id]                  int IDENTITY(1,1) PRIMARY KEY,
    [Name]                nvarchar(100)     UNIQUE NOT NULL,
    [ProviderType]        nvarchar(50)      NOT NULL,
    [ContactPerson]       nvarchar(100),
    [ContactEmail]        nvarchar(256),
    [ContactPhone]        nvarchar(20),
    [IsActive]            bit               DEFAULT 1
)
```

**Provider Types**:
- `Mobile`
- `Landline`
- `PrivateWire`
- `Internet`

---

## Workflow Tables

### SimRequest
**Purpose**: SIM card request workflow

**Schema**:
```sql
CREATE TABLE [dbo].[SimRequests] (
    [Id]                  int IDENTITY(1,1) PRIMARY KEY,
    [RequestNumber]       nvarchar(50)      UNIQUE NOT NULL,
    [RequesterIndex]      nvarchar(50)      NOT NULL,
    [RequestType]         nvarchar(50)      NOT NULL,
    [Justification]       nvarchar(max)     NOT NULL,
    [Status]              nvarchar(50)      NOT NULL DEFAULT 'Pending',
    [RequestDate]         datetime2         NOT NULL,
    [SupervisorApproval]  bit               DEFAULT 0,
    [SupervisorComments]  nvarchar(max),
    [ICTSApproval]        bit               DEFAULT 0,
    [ICTSComments]        nvarchar(max),
    [CompletedDate]       datetime2
)
```

**Status Flow**:
`Pending` → `PendingSupervisor` → `PendingICTS` → `Approved` / `Rejected`

---

### RefundRequest
**Purpose**: Refund request workflow

**Schema**:
```sql
CREATE TABLE [dbo].[RefundRequests] (
    [Id]                      int IDENTITY(1,1) PRIMARY KEY,
    [RequestNumber]           nvarchar(50)      UNIQUE NOT NULL,
    [RequesterIndex]          nvarchar(50)      NOT NULL,
    [Amount]                  decimal(18,4)     NOT NULL,
    [Currency]                nvarchar(3)       DEFAULT 'USD',
    [Description]             nvarchar(max)     NOT NULL,
    [Status]                  nvarchar(50)      NOT NULL,
    [RequestDate]             datetime2         NOT NULL,

    -- Approvals
    [SupervisorApproval]      bit               DEFAULT 0,
    [SupervisorComments]      nvarchar(max),
    [ClaimsApproval]          bit               DEFAULT 0,
    [ClaimsComments]          nvarchar(max),
    [BudgetApproval]          bit               DEFAULT 0,
    [BudgetComments]          nvarchar(max),
    [PaymentApproval]         bit               DEFAULT 0,
    [PaymentComments]         nvarchar(max),
    [CompletedDate]           datetime2
)
```

**Status Flow**:
`Pending` → `SupervisorApproved` → `ClaimsApproved` → `BudgetApproved` → `PaymentApproved` → `Paid`

---

## System Tables

### AuditLog
**Purpose**: System-wide audit trail

**Schema**:
```sql
CREATE TABLE [dbo].[AuditLogs] (
    [Id]                  int IDENTITY(1,1) PRIMARY KEY,
    [Timestamp]           datetime2         NOT NULL,
    [UserId]              nvarchar(450),
    [UserEmail]           nvarchar(256),
    [Action]              nvarchar(100)     NOT NULL,
    [EntityType]          nvarchar(100),
    [EntityId]            nvarchar(50),
    [Changes]             nvarchar(max),    -- JSON
    [IpAddress]           nvarchar(45),
    [UserAgent]           nvarchar(500)
)
```

**Indexes**:
- `IX_AuditLogs_Timestamp`
- `IX_AuditLogs_UserId`
- `IX_AuditLogs_EntityType_EntityId`

**Common Actions**:
- `Create`, `Update`, `Delete`
- `Login`, `Logout`, `PasswordChange`
- `Approve`, `Reject`, `Submit`
- `Import`, `Export`

---

### Notification
**Purpose**: In-app notifications

**Schema**:
```sql
CREATE TABLE [dbo].[Notifications] (
    [Id]                  int IDENTITY(1,1) PRIMARY KEY,
    [UserId]              nvarchar(450)     NOT NULL,
    [Title]               nvarchar(200)     NOT NULL,
    [Message]             nvarchar(max)     NOT NULL,
    [Type]                nvarchar(20)      NOT NULL,
    [IsRead]              bit               DEFAULT 0,
    [CreatedDate]         datetime2         NOT NULL,
    [ReadDate]            datetime2,
    [RelatedEntityType]   nvarchar(100),
    [RelatedEntityId]     int,
    [ActionUrl]           nvarchar(500)
)
```

**Notification Types**:
- `Info`, `Warning`, `Error`, `Success`

**Indexes**:
- `IX_Notifications_UserId_IsRead`
- `IX_Notifications_CreatedDate`

---

### ImportAudit
**Purpose**: Track data import operations

**Schema**:
```sql
CREATE TABLE [dbo].[ImportAudits] (
    [Id]                  int IDENTITY(1,1) PRIMARY KEY,
    [ImportType]          nvarchar(50)      NOT NULL,
    [FileName]            nvarchar(255)     NOT NULL,
    [ImportDate]          datetime2         NOT NULL,
    [ImportedBy]          nvarchar(256)     NOT NULL,
    [RecordCount]         int               DEFAULT 0,
    [SuccessCount]        int               DEFAULT 0,
    [ErrorCount]          int               DEFAULT 0,
    [Status]              nvarchar(20)      NOT NULL,
    [ErrorDetails]        nvarchar(max),
    [DateFormatUsed]      nvarchar(50),
    [ProcessingTime]      int,              -- milliseconds
    [BillingPeriod]       datetime2
)
```

---

## Organization Tables

### Organization
```sql
CREATE TABLE [dbo].[Organizations] (
    [Id]                  int IDENTITY(1,1) PRIMARY KEY,
    [Name]                nvarchar(200)     UNIQUE NOT NULL,
    [Code]                nvarchar(50)      UNIQUE,
    [IsActive]            bit               DEFAULT 1
)
```

### Office
```sql
CREATE TABLE [dbo].[Offices] (
    [Id]                  int IDENTITY(1,1) PRIMARY KEY,
    [Name]                nvarchar(200)     NOT NULL,
    [Code]                nvarchar(50),
    [OrganizationId]      int               NOT NULL,
    [IsActive]            bit               DEFAULT 1
)
```

### SubOffice
```sql
CREATE TABLE [dbo].[SubOffices] (
    [Id]                  int IDENTITY(1,1) PRIMARY KEY,
    [Name]                nvarchar(200)     NOT NULL,
    [OfficeId]            int               NOT NULL,
    [IsActive]            bit               DEFAULT 1
)
```

**Relationships**:
- `Office.OrganizationId` → `Organization.Id`
- `SubOffice.OfficeId` → `Office.Id`
- `EbillUser.OrganizationId` → `Organization.Id`
- `EbillUser.OfficeId` → `Office.Id`

---

## Enumerations

### VerificationType
```csharp
public enum VerificationType
{
    Personal,
    Official
}
```

### AssignmentStatus
```csharp
public enum AssignmentStatus
{
    None,           // No assignment
    Pending,        // Awaiting acceptance
    Accepted,       // Accepted
    Rejected,       // Rejected
    Reassigned      // Reassigned
}
```

### DocumentType
```csharp
public enum DocumentType
{
    OverageJustification,
    ApprovalLetter,
    Receipt,
    Other
}
```

### ApprovalStatus
```csharp
public enum ApprovalStatus
{
    Pending,
    Approved,
    PartiallyApproved,
    Rejected,
    Reverted
}
```

### SupervisorAction
```csharp
public enum SupervisorAction
{
    Approve,
    PartiallyApprove,
    Reject,
    Revert
}
```

---

## Indexes and Constraints

### Primary Keys
All tables have an `Id` column as PRIMARY KEY with IDENTITY(1,1)

### Unique Constraints
- `EbillUser.IndexNumber`
- `EbillUser.Email`
- `ApplicationUser.Email`
- `ApplicationUser.AzureAdObjectId`
- `ClassOfService.Name`
- `SimRequest.RequestNumber`
- `RefundRequest.RequestNumber`
- `StagingBatch.BatchReference`

### Foreign Key Relationships

**EbillUser**:
- `OfficeId` → `Office.Id`
- `OrganizationId` → `Organization.Id`

**UserPhone**:
- `ClassOfServiceId` → `ClassOfService.Id`

**CallRecord**:
- `CallLogId` → `CallLog.Id`
- `UserPhoneId` → `UserPhone.Id`

**CallLogVerification**:
- `UserPhoneId` → `UserPhone.Id`

**CallLogPaymentAssignment**:
- `CallRecordId` → `CallRecord.Id`
- `VerificationId` → `CallLogVerification.Id`

**CallLogDocument**:
- `VerificationId` → `CallLogVerification.Id`

### Performance Indexes

**CallRecords**:
- `IX_CallRecords_CallLogId`
- `IX_CallRecords_UserPhoneId`
- `IX_CallRecords_ext_no`
- `IX_CallRecords_call_date`
- `IX_CallRecords_verification_period`

**UserPhones**:
- `IX_UserPhones_IndexNumber`
- `IX_UserPhones_PhoneNumber`
- `IX_UserPhones_ClassOfServiceId`

**AuditLogs**:
- `IX_AuditLogs_Timestamp`
- `IX_AuditLogs_UserId`
- `IX_AuditLogs_EntityType_EntityId`

**Notifications**:
- `IX_Notifications_UserId_IsRead`
- `IX_Notifications_CreatedDate`

---

## Migration Management

### Current Migration Status

To view applied migrations:
```bash
dotnet ef migrations list
```

### Key Migrations

1. `20251002123541_AddVerificationPeriodToCallRecords` - Added verification period tracking
2. `20251002163017_AddCallLogVerificationSystemTables` - Added verification system tables
3. `20251002173558_AddUserPhoneRelationshipToCallRecords` - Linked CallRecords to UserPhones
4. `20251003180350_AddPhoneStatusToUserPhone` - Added phone status field
5. `20251003192422_AddEbillUserAuthentication` - Azure AD integration
6. `20251006074150_ReplaceMonthlyCallCostLimitWithHandsetAllowanceAmount` - CoS updates
7. `20251006175733_AddAssignmentStatusToCallRecord` - Payment assignment status
8. `20251008095859_AddNotificationsTable` - Notification system

### Generating Schema Scripts

**For local development**:
```bash
dotnet ef migrations script --output schema.sql --idempotent
```

**For Azure deployment**:
```bash
dotnet ef migrations script --output azure-schema.sql --idempotent
```

**For specific migration range**:
```bash
dotnet ef migrations script [FromMigration] [ToMigration] --output delta.sql
```

---

## Data Integrity Rules

### Business Rules

1. **User Management**
   - EbillUser.Email must match ApplicationUser.Email
   - IndexNumber must be unique across all users
   - Supervisor must be an existing EbillUser

2. **User Phones**
   - One phone can have multiple assignments over time
   - Only one Active assignment per phone at a time
   - ClassOfService is required for all assignments

3. **Call Records**
   - Must have valid UserPhone assignment
   - Verification period must be set for verification workflow
   - Payment assignment required for overages

4. **Verifications**
   - One verification per user per period
   - Must submit before supervisor can approve
   - Cannot modify after supervisor approval

5. **Exchange Rates**
   - Only one active rate per currency pair
   - Effective date must be chronological

### Cascade Behaviors

Most foreign keys use **RESTRICT** to prevent accidental deletions.

Exceptions (CASCADE DELETE):
- `CallLogDocument` → when `CallLogVerification` is deleted
- `SimRequestHistory` → when `SimRequest` is deleted

---

## Backup and Maintenance

### Recommended Maintenance

**Weekly**:
- Rebuild fragmented indexes
- Update statistics
- Archive old audit logs

**Monthly**:
- Full database backup
- Archive processed staging batches
- Clean up old notifications

**Quarterly**:
- Review and optimize slow queries
- Archive finalized call logs
- Purge old staging data

### Archive Strategy

1. **Call Logs** - Archive after 2 years
2. **Audit Logs** - Archive after 1 year
3. **Notifications** - Delete after 6 months
4. **Staging Data** - Delete after processing + 30 days

---

## Database Schema Version

**Version**: 1.0
**Last Updated**: October 2025
**EF Core Version**: 8.0
**SQL Server Compatibility**: 2016+

---

## Related Documentation

- [README.md](./README.md) - Project overview
- [ARCHITECTURE.md](./docs/ARCHITECTURE.md) - System architecture
- [MODULES.md](./MODULES.md) - Module documentation
