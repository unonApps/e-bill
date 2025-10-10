# Call Log Verification & Approval Workflow - Implementation Plan

## Executive Summary

This document outlines the implementation approach for a comprehensive call log verification and approval system. The workflow allows EbillUsers to verify call logs (personal/official), reassign payment responsibility, justify overage based on Class of Service allowances, and submit for supervisor approval with support for partial approvals and rejections.

---

## Table of Contents

1. [Current System Analysis](#1-current-system-analysis)
2. [Workflow Requirements](#2-workflow-requirements)
3. [Database Design](#3-database-design)
4. [Implementation Phases](#4-implementation-phases)
5. [Technical Architecture](#5-technical-architecture)
6. [UI/UX Design](#6-uiux-design)
7. [API Endpoints](#7-api-endpoints)
8. [Security & Permissions](#8-security--permissions)
9. [Testing Strategy](#9-testing-strategy)
10. [Deployment Plan](#10-deployment-plan)

---

## 1. Current System Analysis

### Existing Models & Infrastructure

#### CallRecord Model
**Location**: `/Models/CallRecord.cs`

**Current Fields**:
- `ResponsibleIndexNumber` (ext_resp_index) - User responsible for the call
- `PayingIndexNumber` (call_pay_index) - User who will pay (can be different)
- `IsVerified` (call_ver_ind) - Verification flag
- `VerificationDate` (call_ver_date) - When verified
- `VerificationPeriod` - Deadline for verification
- `IsCertified` (call_cert_ind) - Certification flag
- `CertificationDate` (call_cert_date) - When certified
- `CertifiedBy` (call_cert_by) - Who certified

**Missing Fields** (to be added):
- Verification type (Personal/Official)
- Payment assignment status
- Payment assignment acceptance
- Overage justification
- Supporting documents
- Approval workflow tracking
- Supervisor actions
- Partial approval amounts

#### ClassOfService Model
**Location**: `/Models/ClassOfService.cs`

**Current Fields**:
- `AirtimeAllowance` - Text field for allowance
- `DataAllowance` - Text field for allowance
- `HandsetAllowance` - Text field for allowance
- `EligibleStaff` - Who can use this service

**Needs Enhancement**:
- Convert allowances to numeric values for calculation
- Add monthly/billing period allowance tracking
- Add overage threshold logic

#### Existing Approval Pattern
**Location**: `/Models/InterimUpdate.cs`

**Pattern Analysis**:
```csharp
- ApprovalStatus: PENDING, APPROVED, REJECTED
- RequestedBy / RequestedDate
- ApprovedBy / ApprovalDate
- RejectionReason
- SupportingDocuments (JSON array)
- Justification field
```

**Key Takeaway**: We can reuse this approval pattern for call log verification workflow.

---

## 2. Workflow Requirements

### User Verification Flow

```
┌─────────────────────────────────────────────────────────────────┐
│                    CALL LOG VERIFICATION WORKFLOW                │
└─────────────────────────────────────────────────────────────────┘

Step 1: User Views Call Logs
├─ User logs in and sees their call logs from production (CallRecords)
├─ Filter by: Verification Period, Unverified, High Cost, Overage
└─ Display: Extension, Date, Destination, Cost, Class of Service

Step 2: User Verifies Individual Call
├─ Select Verification Type:
│  ├─ Personal - User pays personally
│  └─ Official - Organization pays
├─ Check Class of Service Allowance:
│  ├─ Within Allowance → No justification needed
│  └─ Over Allowance → Justification + Document Required
└─ Option to Reassign Payment:
   ├─ Search for user by Index Number / Name
   ├─ Select new paying user
   └─ Add reassignment reason

Step 3: Payment Assignment Acceptance
├─ Assigned user receives notification
├─ Assigned user can:
│  ├─ Accept Assignment
│  ├─ Reject Assignment (with reason)
│  └─ Counter-assign to another user
└─ Original user notified of acceptance/rejection

Step 4: Submit to Supervisor
├─ Batch submit verified calls
├─ Attached documents included
├─ Justifications for overages included
└─ Status changes: Verified → Pending Supervisor Approval

Step 5: Supervisor Review
├─ Supervisor sees all pending verifications from team
├─ For each submission, supervisor can:
│  ├─ Approve All
│  ├─ Approve Partially (specify approved amount)
│  ├─ Reject (with reason)
│  └─ Revert back to user (for corrections)
└─ Actions trigger:
   ├─ Email notifications
   ├─ Status updates
   └─ Audit log entries
```

---

## 3. Database Design

### New Tables to Create

#### 3.1 CallLogVerification Table

```sql
CREATE TABLE CallLogVerifications (
    Id INT PRIMARY KEY IDENTITY(1,1),
    CallRecordId INT NOT NULL,
    VerifiedBy NVARCHAR(50) NOT NULL, -- IndexNumber
    VerifiedDate DATETIME2 NOT NULL,
    VerificationType NVARCHAR(20) NOT NULL, -- 'Personal' or 'Official'

    -- Class of Service Tracking
    ClassOfServiceId INT NULL,
    AllowanceAmount DECIMAL(18,4) NULL,
    ActualAmount DECIMAL(18,4) NOT NULL,
    IsOverage BIT NOT NULL DEFAULT 0,

    -- Overage Justification
    JustificationText NVARCHAR(MAX) NULL,
    SupportingDocuments NVARCHAR(MAX) NULL, -- JSON array of file paths

    -- Approval Workflow
    ApprovalStatus NVARCHAR(20) NOT NULL DEFAULT 'Pending',
    -- Values: Pending, Approved, PartiallyApproved, Rejected, Reverted

    SubmittedToSupervisor BIT NOT NULL DEFAULT 0,
    SubmittedDate DATETIME2 NULL,

    SupervisorIndexNumber NVARCHAR(50) NULL,
    SupervisorAction NVARCHAR(20) NULL, -- Approved, PartiallyApproved, Rejected, Reverted
    SupervisorActionDate DATETIME2 NULL,
    SupervisorComments NVARCHAR(500) NULL,

    ApprovedAmount DECIMAL(18,4) NULL, -- For partial approvals
    RejectionReason NVARCHAR(500) NULL,

    -- Audit
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedDate DATETIME2 NULL,

    CONSTRAINT FK_CallLogVerifications_CallRecords
        FOREIGN KEY (CallRecordId) REFERENCES CallRecords(Id),
    CONSTRAINT FK_CallLogVerifications_ClassOfServices
        FOREIGN KEY (ClassOfServiceId) REFERENCES ClassOfServices(Id)
);
```

#### 3.2 CallLogPaymentAssignment Table

```sql
CREATE TABLE CallLogPaymentAssignments (
    Id INT PRIMARY KEY IDENTITY(1,1),
    CallRecordId INT NOT NULL,

    -- Assignment Details
    AssignedFrom NVARCHAR(50) NOT NULL, -- Original responsible user
    AssignedTo NVARCHAR(50) NOT NULL,   -- New paying user
    AssignmentReason NVARCHAR(500) NOT NULL,
    AssignedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

    -- Acceptance
    AssignmentStatus NVARCHAR(20) NOT NULL DEFAULT 'Pending',
    -- Values: Pending, Accepted, Rejected, Reassigned

    AcceptedDate DATETIME2 NULL,
    RejectionReason NVARCHAR(500) NULL,

    -- Notification Tracking
    NotificationSent BIT NOT NULL DEFAULT 0,
    NotificationSentDate DATETIME2 NULL,
    NotificationViewedDate DATETIME2 NULL,

    -- Audit
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedDate DATETIME2 NULL,

    CONSTRAINT FK_CallLogPaymentAssignments_CallRecords
        FOREIGN KEY (CallRecordId) REFERENCES CallRecords(Id)
);
```

#### 3.3 CallLogDocument Table

```sql
CREATE TABLE CallLogDocuments (
    Id INT PRIMARY KEY IDENTITY(1,1),
    CallLogVerificationId INT NOT NULL,

    -- File Details
    FileName NVARCHAR(255) NOT NULL,
    FilePath NVARCHAR(500) NOT NULL,
    FileSize BIGINT NOT NULL,
    ContentType NVARCHAR(100) NOT NULL,

    -- Document Type
    DocumentType NVARCHAR(50) NOT NULL,
    -- Values: OverageJustification, ApprovalLetter, Receipt, Other

    Description NVARCHAR(500) NULL,

    -- Upload Details
    UploadedBy NVARCHAR(50) NOT NULL,
    UploadedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT FK_CallLogDocuments_CallLogVerifications
        FOREIGN KEY (CallLogVerificationId) REFERENCES CallLogVerifications(Id) ON DELETE CASCADE
);
```

#### 3.4 Update CallRecords Table

```sql
-- Add new columns to existing CallRecords table
ALTER TABLE CallRecords ADD VerificationType NVARCHAR(20) NULL; -- Personal/Official
ALTER TABLE CallRecords ADD PaymentAssignmentId INT NULL;
ALTER TABLE CallRecords ADD OverageJustified BIT NOT NULL DEFAULT 0;
ALTER TABLE CallRecords ADD SupervisorApprovalStatus NVARCHAR(20) NULL;
ALTER TABLE CallRecords ADD SupervisorApprovedBy NVARCHAR(50) NULL;
ALTER TABLE CallRecords ADD SupervisorApprovedDate DATETIME2 NULL;
```

#### 3.5 Update ClassOfServices Table

```sql
-- Add numeric allowance fields
ALTER TABLE ClassOfServices ADD AirtimeAllowanceAmount DECIMAL(18,4) NULL;
ALTER TABLE ClassOfServices ADD DataAllowanceAmount DECIMAL(18,4) NULL; -- In MB/GB
ALTER TABLE ClassOfServices ADD MonthlyCallCostLimit DECIMAL(18,4) NULL;
ALTER TABLE ClassOfServices ADD BillingPeriod NVARCHAR(20) NOT NULL DEFAULT 'Monthly';
```

---

## 4. Implementation Phases

### Phase 1: Database & Models (Week 1)

#### Tasks:
1. **Create Migrations**
   - Create `CallLogVerification` model
   - Create `CallLogPaymentAssignment` model
   - Create `CallLogDocument` model
   - Update `CallRecord` model with new fields
   - Update `ClassOfService` model with numeric allowances

2. **Create Enums**
   ```csharp
   public enum VerificationType { Personal, Official }
   public enum AssignmentStatus { Pending, Accepted, Rejected, Reassigned }
   public enum ApprovalStatus { Pending, Approved, PartiallyApproved, Rejected, Reverted }
   public enum SupervisorAction { Approve, PartiallyApprove, Reject, Revert }
   ```

3. **Run Migrations**
   ```bash
   dotnet ef migrations add AddCallLogVerificationSystem
   dotnet ef database update
   ```

#### Deliverables:
- ✅ All models created and tested
- ✅ Database tables created
- ✅ Navigation properties configured
- ✅ Sample data seeded for testing

---

### Phase 2: Services Layer (Week 1-2)

#### Create Services:

**1. CallLogVerificationService.cs**
```csharp
public interface ICallLogVerificationService
{
    // User Verification
    Task<CallLogVerification> VerifyCallLogAsync(int callRecordId, string indexNumber,
        VerificationType type, string? justification = null, List<IFormFile>? documents = null);

    Task<List<CallLogVerification>> GetUserVerificationsAsync(string indexNumber,
        bool pendingOnly = false);

    Task<bool> IsOverageAsync(int callRecordId, int classOfServiceId);

    Task<decimal> GetRemainingAllowanceAsync(string indexNumber, int month, int year);

    // Payment Assignment
    Task<CallLogPaymentAssignment> AssignPaymentAsync(int callRecordId,
        string assignedFrom, string assignedTo, string reason);

    Task<bool> AcceptPaymentAssignmentAsync(int assignmentId, string indexNumber);

    Task<bool> RejectPaymentAssignmentAsync(int assignmentId, string indexNumber,
        string reason);

    Task<List<CallLogPaymentAssignment>> GetPendingAssignmentsAsync(string indexNumber);

    // Supervisor Actions
    Task<int> SubmitToSupervisorAsync(List<int> verificationIds, string indexNumber);

    Task<List<CallLogVerification>> GetSupervisorPendingApprovalsAsync(
        string supervisorIndexNumber);

    Task<bool> ApproveVerificationAsync(int verificationId,
        string supervisorIndexNumber, decimal? approvedAmount = null);

    Task<bool> RejectVerificationAsync(int verificationId,
        string supervisorIndexNumber, string reason);

    Task<bool> RevertVerificationAsync(int verificationId,
        string supervisorIndexNumber, string reason);

    // Reporting
    Task<VerificationSummary> GetVerificationSummaryAsync(string indexNumber,
        int month, int year);
}
```

**2. ClassOfServiceCalculationService.cs**
```csharp
public interface IClassOfServiceCalculationService
{
    Task<bool> IsWithinAllowanceAsync(string indexNumber, decimal amount,
        int month, int year);

    Task<decimal> GetMonthlyUsageAsync(string indexNumber, int month, int year);

    Task<decimal> GetAllowanceLimitAsync(int classOfServiceId);

    Task<OverageReport> GetOverageReportAsync(string indexNumber,
        int month, int year);
}
```

**3. DocumentManagementService.cs**
```csharp
public interface IDocumentManagementService
{
    Task<CallLogDocument> UploadDocumentAsync(int verificationId,
        IFormFile file, string documentType, string uploadedBy);

    Task<Stream> DownloadDocumentAsync(int documentId);

    Task<bool> DeleteDocumentAsync(int documentId);

    Task<List<CallLogDocument>> GetDocumentsAsync(int verificationId);
}
```

#### Deliverables:
- ✅ All service interfaces defined
- ✅ All service implementations completed
- ✅ Unit tests for services
- ✅ Integration tests with database

---

### Phase 3: User Verification UI (Week 2-3)

#### Pages to Create:

**1. /Modules/EBillManagement/CallRecords/MyCallLogs.cshtml**
- Display user's call logs from CallRecords table
- Filter: Unverified, Verification Period, Date Range, High Cost
- Sortable columns: Date, Cost, Duration, Status
- Batch select for verification
- Link to verify individual calls

**2. /Modules/EBillManagement/CallRecords/Verify.cshtml**
- Single call verification form
- Display: Extension, Date, Time, Destination, Cost
- Show Class of Service allowance info
- Verification Type radio buttons (Personal/Official)
- Overage detection with threshold indicator
- Justification textarea (required if overage)
- Document upload (drag & drop + browse)
- Payment reassignment section:
  - Search user autocomplete
  - Selected user info display
  - Reason textarea
- Submit button → Creates CallLogVerification record

**3. /Modules/EBillManagement/CallRecords/PaymentAssignments.cshtml**
- List of pending payment assignments for logged-in user
- Show: Call details, Assigned from, Reason, Date
- Accept/Reject buttons per assignment
- Rejection reason modal
- Counter-assignment option

**4. /Modules/EBillManagement/CallRecords/SubmitToSupervisor.cshtml**
- Batch submission interface
- List verified calls ready for submission
- Summary: Total cost, # of calls, overages
- Attached documents list
- Submit button → Updates submission status

#### Deliverables:
- ✅ All UI pages created
- ✅ Responsive design
- ✅ Client-side validation
- ✅ File upload with preview
- ✅ User search autocomplete
- ✅ Progress indicators

---

### Phase 4: Supervisor Approval UI (Week 3)

#### Pages to Create:

**1. /Modules/EBillManagement/CallRecords/SupervisorApprovals.cshtml**
- Dashboard showing pending approvals from team
- Group by: User, Date Submitted, Amount
- Filters: User, Date Range, Overage Only
- Sortable table
- Expandable rows showing call details

**2./Modules/EBillManagement/CallRecords/ReviewVerification.cshtml**
- Detailed review page for a verification batch
- Display all calls in submission
- Show: Call details, verification type, justifications, documents
- Overage highlights with calculations
- Supervisor actions:
  - Approve All button
  - Approve Partially:
    - Input approved amount per call
    - Reason for partial approval
  - Reject:
    - Rejection reason textarea
  - Revert to User:
    - Revert reason textarea
- Comments section for supervisor notes
- Action history log

**3. /Modules/EBillManagement/CallRecords/ApprovalHistory.cshtml**
- Historical view of all approvals
- Filters: Date Range, Action Type, User
- Export to Excel/PDF
- Audit trail

#### Deliverables:
- ✅ Supervisor dashboard
- ✅ Approval workflow UI
- ✅ Partial approval logic
- ✅ Notification system integration
- ✅ Approval history & audit trail

---

### Phase 5: Notifications & Alerts (Week 4)

#### Notification Types:

1. **Email Notifications**:
   - Payment assignment received
   - Payment assignment accepted/rejected
   - Verification submitted to supervisor
   - Supervisor approved/rejected/reverted
   - Approaching verification deadline
   - Overage detected

2. **In-App Notifications**:
   - Bell icon with count
   - Notification center dropdown
   - Real-time using SignalR (optional)

#### Implementation:

**1. Create NotificationService**
```csharp
public interface INotificationService
{
    Task SendPaymentAssignmentNotificationAsync(CallLogPaymentAssignment assignment);
    Task SendSupervisorSubmissionNotificationAsync(string supervisorEmail, int count);
    Task SendApprovalNotificationAsync(CallLogVerification verification);
    Task SendDeadlineReminderAsync(string indexNumber, int daysRemaining);
}
```

**2. Email Templates**:
- Create Razor email templates
- Use SMTP settings from appsettings.json
- Queue emails using background service

#### Deliverables:
- ✅ Email notification service
- ✅ Email templates designed
- ✅ In-app notification system
- ✅ Notification preferences page
- ✅ Background job for scheduled notifications

---

### Phase 6: Reporting & Analytics (Week 4-5)

#### Reports to Create:

**1. Verification Compliance Report**
- % of calls verified within deadline
- Overdue verifications by user
- Top overagers
- Trend analysis

**2. Payment Assignment Report**
- # of assignments by user
- Acceptance/rejection rates
- Reassignment chains

**3. Supervisor Approval Report**
- Average approval time
- Rejection rates by reason
- Partial approval statistics

**4. Class of Service Utilization Report**
- Usage vs. allowance by class
- Overage trends
- Cost analysis by department

#### Implementation:

**1. Create ReportingService**
```csharp
public interface IReportingService
{
    Task<VerificationComplianceReport> GetComplianceReportAsync(int month, int year);
    Task<PaymentAssignmentReport> GetPaymentAssignmentReportAsync(DateTime start, DateTime end);
    Task<SupervisorApprovalReport> GetSupervisorReportAsync(string supervisorIndexNumber, int month, int year);
    Task<ClassOfServiceUtilizationReport> GetUtilizationReportAsync(int month, int year);
}
```

**2. Charts & Visualizations**:
- Use Chart.js or similar library
- Interactive dashboards
- Export to Excel/PDF

#### Deliverables:
- ✅ All reports implemented
- ✅ Dashboard with charts
- ✅ Export functionality
- ✅ Scheduled report generation

---

## 5. Technical Architecture

### Layered Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                        Presentation Layer                        │
│  Razor Pages (.cshtml) | API Controllers | SignalR Hubs         │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│                       Business Logic Layer                       │
│  CallLogVerificationService | ClassOfServiceCalculationService  │
│  DocumentManagementService | NotificationService                │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│                        Data Access Layer                         │
│  ApplicationDbContext | Repositories | Unit of Work             │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│                          Database (SQL Server)                   │
│  CallRecords | CallLogVerifications | PaymentAssignments        │
└─────────────────────────────────────────────────────────────────┘
```

### Key Design Patterns

1. **Repository Pattern** - For data access abstraction
2. **Unit of Work** - For transaction management
3. **Service Layer** - For business logic encapsulation
4. **DTO Pattern** - For data transfer between layers
5. **Strategy Pattern** - For approval workflow variations

---

## 6. UI/UX Design

### Design Principles

1. **Progressive Disclosure**: Show complexity only when needed
2. **Guided Workflow**: Step-by-step wizard for verification
3. **Smart Defaults**: Pre-populate fields based on context
4. **Inline Validation**: Immediate feedback on user input
5. **Responsive Design**: Mobile-first approach

### UI Components to Reuse

Based on existing codebase patterns:

1. **Cards** - For call log items
2. **Modals** - For quick actions (accept/reject)
3. **Badges** - For status indicators
4. **Alerts** - For feedback messages
5. **Data Tables** - With sorting, filtering, pagination
6. **File Upload** - Drag & drop with preview

### Color Coding

```css
/* Status Colors */
.status-pending { background: #fbbf24; }      /* Yellow */
.status-approved { background: #10b981; }     /* Green */
.status-rejected { background: #ef4444; }     /* Red */
.status-reverted { background: #f97316; }     /* Orange */
.status-personal { background: #3b82f6; }     /* Blue */
.status-official { background: #8b5cf6; }     /* Purple */

/* Overage Indicators */
.within-allowance { border-left: 4px solid #10b981; }
.overage-warning { border-left: 4px solid #f59e0b; }
.overage-critical { border-left: 4px solid #ef4444; }
```

---

## 7. API Endpoints

### User Verification Endpoints

```
GET  /api/callrecords/my-calls
POST /api/callrecords/verify
GET  /api/callrecords/verification/{id}
PUT  /api/callrecords/verification/{id}
DELETE /api/callrecords/verification/{id}

GET  /api/callrecords/allowance
GET  /api/callrecords/overage-check/{callRecordId}
```

### Payment Assignment Endpoints

```
POST /api/callrecords/assign-payment
GET  /api/callrecords/my-assignments
POST /api/callrecords/accept-assignment/{id}
POST /api/callrecords/reject-assignment/{id}
```

### Supervisor Endpoints

```
GET  /api/supervisor/pending-approvals
POST /api/supervisor/approve/{verificationId}
POST /api/supervisor/approve-partial/{verificationId}
POST /api/supervisor/reject/{verificationId}
POST /api/supervisor/revert/{verificationId}
POST /api/supervisor/batch-approve
```

### Document Endpoints

```
POST /api/documents/upload
GET  /api/documents/{id}
GET  /api/documents/verification/{verificationId}
DELETE /api/documents/{id}
```

---

## 8. Security & Permissions

### Role-Based Access Control

```csharp
[Authorize(Roles = "User")]
public class MyCallLogsModel : PageModel { }

[Authorize(Roles = "Supervisor")]
public class SupervisorApprovalsModel : PageModel { }

[Authorize(Roles = "Admin,Supervisor")]
public class ApprovalHistoryModel : PageModel { }
```

### Permission Checks

1. **User can only verify their own calls**
   - Check: `callRecord.ResponsibleIndexNumber == User.IndexNumber`

2. **User can only view assigned payments to them**
   - Check: `assignment.AssignedTo == User.IndexNumber`

3. **Supervisor can only approve their team's verifications**
   - Check: `verification.SupervisorIndexNumber == User.IndexNumber`

4. **Document access restricted to involved parties**
   - Check: Owner, Supervisor, or Admin

### Data Privacy

- Mask sensitive call details in logs
- Encrypt documents at rest
- Audit all access to call records
- GDPR compliance for data retention

---

## 9. Testing Strategy

### Unit Tests

```csharp
// Example: CallLogVerificationService Tests
[Fact]
public async Task VerifyCallLog_WithinAllowance_ShouldSucceed()
{
    // Arrange
    var service = new CallLogVerificationService(_context, _cosService);
    var callRecordId = 1;

    // Act
    var result = await service.VerifyCallLogAsync(
        callRecordId, "12345", VerificationType.Official);

    // Assert
    Assert.NotNull(result);
    Assert.False(result.IsOverage);
}

[Fact]
public async Task VerifyCallLog_OverageWithoutJustification_ShouldThrowException()
{
    // Arrange & Act & Assert
    await Assert.ThrowsAsync<ValidationException>(...);
}
```

### Integration Tests

- Test full verification workflow end-to-end
- Test payment assignment acceptance flow
- Test supervisor approval process
- Test document upload/download

### UI Tests (Selenium/Playwright)

- User can verify a call log
- User can assign payment to another user
- Supervisor can approve/reject verification
- Notifications are sent correctly

---

## 10. Deployment Plan

### Pre-Deployment Checklist

- [ ] All migrations tested on staging database
- [ ] Services registered in `Program.cs`
- [ ] Email SMTP settings configured
- [ ] File upload directory permissions set
- [ ] Performance testing completed
- [ ] Security audit passed
- [ ] User training materials prepared

### Deployment Steps

1. **Database Migration**
   ```bash
   dotnet ef database update --connection "Production_Connection_String"
   ```

2. **Deploy Application**
   - Build in Release mode
   - Publish to server
   - Update appsettings.Production.json

3. **Data Migration** (if needed)
   - Migrate existing CallRecords to new structure
   - Set default verification types
   - Link to Class of Service

4. **Post-Deployment**
   - Verify all pages load
   - Test critical workflows
   - Monitor error logs
   - Enable feature flags

### Rollback Plan

- Keep previous migration available
- Database backup before deployment
- Feature flags to disable new functionality
- Revert scripts prepared

---

## Appendix A: Sample Code Snippets

### Model: CallLogVerification

```csharp
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TAB.Web.Models
{
    public class CallLogVerification
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CallRecordId { get; set; }

        [Required]
        [MaxLength(50)]
        public string VerifiedBy { get; set; } = string.Empty;

        [Required]
        public DateTime VerifiedDate { get; set; } = DateTime.UtcNow;

        [Required]
        public VerificationType VerificationType { get; set; }

        // Class of Service Tracking
        public int? ClassOfServiceId { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal? AllowanceAmount { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,4)")]
        public decimal ActualAmount { get; set; }

        public bool IsOverage { get; set; } = false;

        // Overage Justification
        public string? JustificationText { get; set; }

        public string? SupportingDocuments { get; set; } // JSON

        // Approval Workflow
        [Required]
        [MaxLength(20)]
        public string ApprovalStatus { get; set; } = "Pending";

        public bool SubmittedToSupervisor { get; set; } = false;

        public DateTime? SubmittedDate { get; set; }

        [MaxLength(50)]
        public string? SupervisorIndexNumber { get; set; }

        [MaxLength(20)]
        public string? SupervisorAction { get; set; }

        public DateTime? SupervisorActionDate { get; set; }

        [MaxLength(500)]
        public string? SupervisorComments { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal? ApprovedAmount { get; set; }

        [MaxLength(500)]
        public string? RejectionReason { get; set; }

        // Audit
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public DateTime? ModifiedDate { get; set; }

        // Navigation Properties
        [ForeignKey("CallRecordId")]
        public virtual CallRecord CallRecord { get; set; } = null!;

        [ForeignKey("ClassOfServiceId")]
        public virtual ClassOfService? ClassOfService { get; set; }

        public virtual ICollection<CallLogDocument> Documents { get; set; } = new List<CallLogDocument>();

        // Helper Properties
        [NotMapped]
        public bool IsPending => ApprovalStatus == "Pending";

        [NotMapped]
        public bool IsApproved => ApprovalStatus == "Approved" || ApprovalStatus == "PartiallyApproved";
    }

    public enum VerificationType
    {
        Personal,
        Official
    }
}
```

---

## Appendix B: Database Migration Script

```sql
-- Phase 1: Create CallLogVerifications Table
CREATE TABLE CallLogVerifications (
    Id INT PRIMARY KEY IDENTITY(1,1),
    CallRecordId INT NOT NULL,
    VerifiedBy NVARCHAR(50) NOT NULL,
    VerifiedDate DATETIME2 NOT NULL,
    VerificationType NVARCHAR(20) NOT NULL,
    ClassOfServiceId INT NULL,
    AllowanceAmount DECIMAL(18,4) NULL,
    ActualAmount DECIMAL(18,4) NOT NULL,
    IsOverage BIT NOT NULL DEFAULT 0,
    JustificationText NVARCHAR(MAX) NULL,
    SupportingDocuments NVARCHAR(MAX) NULL,
    ApprovalStatus NVARCHAR(20) NOT NULL DEFAULT 'Pending',
    SubmittedToSupervisor BIT NOT NULL DEFAULT 0,
    SubmittedDate DATETIME2 NULL,
    SupervisorIndexNumber NVARCHAR(50) NULL,
    SupervisorAction NVARCHAR(20) NULL,
    SupervisorActionDate DATETIME2 NULL,
    SupervisorComments NVARCHAR(500) NULL,
    ApprovedAmount DECIMAL(18,4) NULL,
    RejectionReason NVARCHAR(500) NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedDate DATETIME2 NULL,
    CONSTRAINT FK_CallLogVerifications_CallRecords
        FOREIGN KEY (CallRecordId) REFERENCES CallRecords(Id),
    CONSTRAINT FK_CallLogVerifications_ClassOfServices
        FOREIGN KEY (ClassOfServiceId) REFERENCES ClassOfServices(Id)
);

-- Phase 2: Create CallLogPaymentAssignments Table
CREATE TABLE CallLogPaymentAssignments (
    Id INT PRIMARY KEY IDENTITY(1,1),
    CallRecordId INT NOT NULL,
    AssignedFrom NVARCHAR(50) NOT NULL,
    AssignedTo NVARCHAR(50) NOT NULL,
    AssignmentReason NVARCHAR(500) NOT NULL,
    AssignedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    AssignmentStatus NVARCHAR(20) NOT NULL DEFAULT 'Pending',
    AcceptedDate DATETIME2 NULL,
    RejectionReason NVARCHAR(500) NULL,
    NotificationSent BIT NOT NULL DEFAULT 0,
    NotificationSentDate DATETIME2 NULL,
    NotificationViewedDate DATETIME2 NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedDate DATETIME2 NULL,
    CONSTRAINT FK_CallLogPaymentAssignments_CallRecords
        FOREIGN KEY (CallRecordId) REFERENCES CallRecords(Id)
);

-- Phase 3: Create CallLogDocuments Table
CREATE TABLE CallLogDocuments (
    Id INT PRIMARY KEY IDENTITY(1,1),
    CallLogVerificationId INT NOT NULL,
    FileName NVARCHAR(255) NOT NULL,
    FilePath NVARCHAR(500) NOT NULL,
    FileSize BIGINT NOT NULL,
    ContentType NVARCHAR(100) NOT NULL,
    DocumentType NVARCHAR(50) NOT NULL,
    Description NVARCHAR(500) NULL,
    UploadedBy NVARCHAR(50) NOT NULL,
    UploadedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_CallLogDocuments_CallLogVerifications
        FOREIGN KEY (CallLogVerificationId) REFERENCES CallLogVerifications(Id) ON DELETE CASCADE
);

-- Phase 4: Update CallRecords Table
ALTER TABLE CallRecords ADD VerificationType NVARCHAR(20) NULL;
ALTER TABLE CallRecords ADD PaymentAssignmentId INT NULL;
ALTER TABLE CallRecords ADD OverageJustified BIT NOT NULL DEFAULT 0;
ALTER TABLE CallRecords ADD SupervisorApprovalStatus NVARCHAR(20) NULL;
ALTER TABLE CallRecords ADD SupervisorApprovedBy NVARCHAR(50) NULL;
ALTER TABLE CallRecords ADD SupervisorApprovedDate DATETIME2 NULL;

-- Phase 5: Update ClassOfServices Table
ALTER TABLE ClassOfServices ADD AirtimeAllowanceAmount DECIMAL(18,4) NULL;
ALTER TABLE ClassOfServices ADD DataAllowanceAmount DECIMAL(18,4) NULL;
ALTER TABLE ClassOfServices ADD MonthlyCallCostLimit DECIMAL(18,4) NULL;
ALTER TABLE ClassOfServices ADD BillingPeriod NVARCHAR(20) NOT NULL DEFAULT 'Monthly';

-- Phase 6: Create Indexes for Performance
CREATE INDEX IX_CallLogVerifications_VerifiedBy ON CallLogVerifications(VerifiedBy);
CREATE INDEX IX_CallLogVerifications_ApprovalStatus ON CallLogVerifications(ApprovalStatus);
CREATE INDEX IX_CallLogVerifications_SupervisorIndexNumber ON CallLogVerifications(SupervisorIndexNumber);
CREATE INDEX IX_CallLogPaymentAssignments_AssignedTo ON CallLogPaymentAssignments(AssignedTo);
CREATE INDEX IX_CallLogPaymentAssignments_AssignmentStatus ON CallLogPaymentAssignments(AssignmentStatus);
```

---

## Appendix C: Configuration Updates

### appsettings.json

```json
{
  "CallLogVerification": {
    "VerificationDeadlineDays": 30,
    "MaxDocumentSizeMB": 10,
    "AllowedDocumentTypes": [".pdf", ".jpg", ".png", ".docx"],
    "OverageThresholdPercentage": 10,
    "RequireJustificationAmount": 100.00
  },
  "Notifications": {
    "EnableEmailNotifications": true,
    "EnableInAppNotifications": true,
    "SendDeadlineReminders": true,
    "ReminderDaysBeforeDeadline": [7, 3, 1]
  },
  "FileStorage": {
    "DocumentsPath": "wwwroot/uploads/call-log-documents",
    "MaxFileSize": 10485760,
    "AllowedExtensions": [".pdf", ".jpg", ".jpeg", ".png", ".docx", ".xlsx"]
  }
}
```

---

## Summary & Next Steps

This implementation plan provides a comprehensive roadmap for building a call log verification and approval system. The phased approach allows for iterative development and testing, ensuring quality at each stage.

### Estimated Timeline
- **Phase 1-2**: 2 weeks (Database & Services)
- **Phase 3-4**: 2 weeks (UI Development)
- **Phase 5-6**: 2 weeks (Notifications & Reporting)
- **Testing & Deployment**: 1 week
- **Total**: 7 weeks

### Key Success Metrics
- 90%+ verification compliance rate
- Average approval time < 48 hours
- User satisfaction score > 4/5
- Zero security incidents
- 95%+ system uptime

### Recommendations
1. Start with Phase 1 (Database) immediately
2. Involve end-users early for UI feedback
3. Build comprehensive test suite
4. Document all business rules
5. Plan for iterative improvements post-launch

---

**Document Version**: 1.0
**Last Updated**: October 2, 2025
**Author**: Implementation Team
**Status**: Ready for Review & Approval
