# Call Log Reporting System - Implementation Plan

**Version:** 1.0
**Date:** October 13, 2025
**Status:** Planning Phase

---

## Executive Summary

This document outlines the design and implementation plan for a comprehensive **Call Log Reporting System** that will serve as the foundation for billing and recovery operations. The system will automate the recovery of call costs based on verification deadlines and supervisor approval workflows, implementing complex business rules that determine whether calls are recovered as Personal, Official, or Class of Service charges.

### Key Objectives
1. Implement deadline-driven automatic recovery logic
2. Create comprehensive reporting infrastructure
3. Provide visibility into verification and approval workflows
4. Enable data-driven decision making for billing operations
5. Establish audit trail for all recovery decisions

---

## Current System Analysis

### Existing Workflow Overview

**Current Flow:**
```
Admin Sets Batch → Staff Verifies Calls → Supervisor Approves → Finalized
```

### Key Pages and Their Roles

#### 1. `/Admin/CallLogStaging` (Admin)
- Manages call log batches
- Sets verification periods/deadlines
- Controls batch lifecycle
- Links to billing periods

#### 2. `/Modules/EBillManagement/CallRecords/MyCallLogs` (Staff)
- Staff view their assigned calls
- Verify calls as Personal or Official
- Justify overages
- Submit for supervisor approval
- Track allowance usage

#### 3. `/Modules/EBillManagement/CallRecords/SupervisorApprovals` (Supervisor)
- Review staff submissions
- Approve/Reject/Revert verifications
- Partial approval support
- Grouped by staff member

### Current Data Models

**CallRecord** - Individual call records with:
- `verification_period` - Deadline for verification
- `verification_type` - Personal/Official
- `assignment_status` - None/Personal/Official/ClassOfService
- `supervisor_approval_status` - Pending/Approved/Rejected/Reverted

**CallLogVerification** - Verification tracking:
- Staff verification details
- Supervisor approval status
- Overage justification
- Approval amounts

**StagingBatch** - Batch management:
- Batch status
- Processing dates
- Billing period linkage

### Existing Services
- `CallLogVerificationService` - Core verification logic
- `CallLogStagingService` - Batch management

---

## Business Requirements

### Recovery Logic Rules

#### Rule 1: Staff Non-Verification Recovery
**Trigger:** Staff fails to verify calls within the verification period
**Action:** All unverified calls → **PERSONAL**
**Rationale:** Staff responsibility to verify; failure means personal liability

**Example:**
- Verification deadline: October 15, 2025
- Staff fails to verify 50 calls by deadline
- System automatically marks all 50 calls as PERSONAL on October 16

#### Rule 2: Supervisor Non-Approval Recovery
**Trigger:** Supervisor fails to approve within approval period
**Action:** All submitted but unapproved calls → **CLASS OF SERVICE**
**Rationale:** If supervisor doesn't respond, default to official charges per class

**Example:**
- Supervisor approval deadline: October 20, 2025
- Staff submits 100 calls as Official
- Supervisor doesn't respond by deadline
- System automatically marks all 100 as CLASS OF SERVICE on October 21

#### Rule 3: Supervisor Partial Approval
**Trigger:** Supervisor explicitly approves only some calls
**Action:**
- Approved calls → **OFFICIAL** (as verified by staff)
- Non-approved calls → **PERSONAL**
**Rationale:** Supervisor judgment determines legitimacy

**Example:**
- Staff submits 100 calls as Official
- Supervisor approves 75 calls, doesn't approve 25
- Result: 75 Official, 25 Personal

#### Rule 4: Supervisor Revert + Staff Re-Failure
**Trigger:** Supervisor reverts verification back to staff, staff fails to re-verify
**Action:** All reverted and unverified calls → **PERSONAL**
**Rationale:** Double failure indicates personal use

**Example:**
- Supervisor reverts 30 calls back to staff
- New deadline: October 18, 2025
- Staff fails to re-verify by deadline
- System marks all 30 as PERSONAL on October 19

#### Rule 5: Supervisor Full Approval
**Trigger:** Supervisor approves all submitted calls
**Action:** All calls → **OFFICIAL** (as verified by staff)
**Rationale:** Supervisor confirms legitimacy

#### Rule 6: Supervisor Rejection
**Trigger:** Supervisor explicitly rejects verification
**Action:** All rejected calls → **PERSONAL**
**Rationale:** Supervisor determined calls are not official

### Recovery Assignment Types

```
PERSONAL:
- Staff pays full cost
- Deducted from salary/reimbursement
- No organizational liability

OFFICIAL:
- Organization pays cost
- Verified by staff, approved by supervisor
- Legitimate business use

CLASS OF SERVICE:
- Organization pays per class allocation
- Default when supervisor doesn't respond
- Based on user's assigned class of service
```

---

## Technical Architecture

### New Components Required

#### 1. Recovery Engine Service (`CallLogRecoveryService`)
**Responsibilities:**
- Execute recovery rules based on deadlines
- Calculate recovery amounts
- Update call record assignments
- Generate recovery audit logs
- Handle complex rule chains

**Key Methods:**
```csharp
Task ProcessExpiredVerificationsAsync(Guid batchId);
Task ProcessExpiredApprovalsAsync(Guid batchId);
Task ProcessRevertedVerificationsAsync(Guid batchId);
Task<RecoveryResult> CalculateRecoveryAsync(string indexNumber, DateTime period);
```

#### 2. Deadline Management Service (`DeadlineManagementService`)
**Responsibilities:**
- Track verification deadlines
- Track approval deadlines
- Send deadline reminders
- Identify expired deadlines
- Support deadline extensions

**Key Methods:**
```csharp
Task<List<ExpiredDeadline>> GetExpiredVerificationDeadlinesAsync();
Task<List<ExpiredDeadline>> GetExpiredApprovalDeadlinesAsync();
Task SendDeadlineRemindersAsync();
Task ExtendDeadlineAsync(Guid batchId, DateTime newDeadline, string reason);
```

#### 3. Reporting Service (`CallLogReportingService`)
**Responsibilities:**
- Generate recovery reports
- Calculate statistics and metrics
- Export report data
- Generate visualizations data

**Key Methods:**
```csharp
Task<RecoverySummaryReport> GetRecoverySummaryAsync(ReportFilter filter);
Task<List<StaffRecoveryDetail>> GetStaffRecoveryDetailsAsync(ReportFilter filter);
Task<List<SupervisorActivityReport>> GetSupervisorActivityAsync(ReportFilter filter);
Task<byte[]> ExportToExcelAsync(ReportType type, ReportFilter filter);
```

#### 4. Background Job Service (`RecoveryAutomationJob`)
**Responsibilities:**
- Scheduled execution of recovery rules
- Runs daily/hourly to check deadlines
- Executes recovery actions automatically
- Logs all automated actions

**Implementation Options:**
- **Hangfire** - Full-featured background job processor
- **Hosted Service** - Built-in ASP.NET Core background service
- **Azure Functions** - Cloud-based timer trigger (if using Azure)

---

## Data Model Enhancements

### New Tables

#### 1. RecoveryLog
**Purpose:** Audit trail for all recovery actions

```sql
CREATE TABLE RecoveryLogs (
    Id INT PRIMARY KEY IDENTITY,
    CallRecordId INT NOT NULL,
    BatchId UNIQUEIDENTIFIER NOT NULL,
    RecoveryType NVARCHAR(50) NOT NULL, -- StaffNonVerification, SupervisorNonApproval, etc.
    RecoveryAction NVARCHAR(50) NOT NULL, -- Personal, Official, ClassOfService
    RecoveryDate DATETIME2 NOT NULL,
    RecoveryReason NVARCHAR(1000) NOT NULL,
    AmountRecovered DECIMAL(18,2) NOT NULL,
    RecoveredFrom NVARCHAR(100) NULL, -- Staff/Supervisor IndexNumber
    ProcessedBy NVARCHAR(100) NULL, -- System/Admin who triggered
    DeadlineDate DATETIME2 NULL,
    IsAutomated BIT NOT NULL DEFAULT 1,

    FOREIGN KEY (CallRecordId) REFERENCES CallRecords(Id),
    FOREIGN KEY (BatchId) REFERENCES StagingBatches(Id)
);

CREATE INDEX IX_RecoveryLogs_BatchId ON RecoveryLogs(BatchId);
CREATE INDEX IX_RecoveryLogs_RecoveryDate ON RecoveryLogs(RecoveryDate);
CREATE INDEX IX_RecoveryLogs_RecoveredFrom ON RecoveryLogs(RecoveredFrom);
```

#### 2. DeadlineTracking
**Purpose:** Track all deadlines and their status

```sql
CREATE TABLE DeadlineTracking (
    Id INT PRIMARY KEY IDENTITY,
    BatchId UNIQUEIDENTIFIER NOT NULL,
    DeadlineType NVARCHAR(50) NOT NULL, -- Verification, Approval, ReVerification
    TargetEntity NVARCHAR(100) NOT NULL, -- IndexNumber or "AllSupervisors"
    DeadlineDate DATETIME2 NOT NULL,
    ExtendedDeadline DATETIME2 NULL,
    DeadlineStatus NVARCHAR(50) NOT NULL, -- Pending, Met, Missed, Extended
    MissedDate DATETIME2 NULL,
    RecoveryProcessed BIT NOT NULL DEFAULT 0,
    RecoveryProcessedDate DATETIME2 NULL,
    ExtensionReason NVARCHAR(500) NULL,
    ExtensionApprovedBy NVARCHAR(100) NULL,
    CreatedDate DATETIME2 NOT NULL,

    FOREIGN KEY (BatchId) REFERENCES StagingBatches(Id)
);

CREATE INDEX IX_DeadlineTracking_BatchId ON DeadlineTracking(BatchId);
CREATE INDEX IX_DeadlineTracking_DeadlineDate ON DeadlineTracking(DeadlineDate);
CREATE INDEX IX_DeadlineTracking_TargetEntity ON DeadlineTracking(TargetEntity);
CREATE INDEX IX_DeadlineTracking_Status ON DeadlineTracking(DeadlineStatus);
```

#### 3. RecoveryConfiguration
**Purpose:** System-wide recovery rules configuration

```sql
CREATE TABLE RecoveryConfiguration (
    Id INT PRIMARY KEY IDENTITY,
    RuleName NVARCHAR(100) NOT NULL,
    RuleType NVARCHAR(50) NOT NULL,
    IsEnabled BIT NOT NULL DEFAULT 1,
    DefaultVerificationDays INT NULL,
    DefaultApprovalDays INT NULL,
    AutomationEnabled BIT NOT NULL DEFAULT 1,
    RequireApprovalForAutomation BIT NOT NULL DEFAULT 0,
    NotificationEnabled BIT NOT NULL DEFAULT 1,
    ReminderDaysBefore INT NULL,
    ConfigValue NVARCHAR(MAX) NULL, -- JSON for complex configs
    CreatedDate DATETIME2 NOT NULL,
    ModifiedDate DATETIME2 NULL,
    ModifiedBy NVARCHAR(100) NULL
);
```

### Table Modifications

#### CallRecord Enhancements
```sql
ALTER TABLE CallRecords ADD RecoveryStatus NVARCHAR(50) NULL; -- NotProcessed, Processed, Overridden
ALTER TABLE CallRecords ADD RecoveryDate DATETIME2 NULL;
ALTER TABLE CallRecords ADD RecoveryProcessedBy NVARCHAR(100) NULL;
ALTER TABLE CallRecords ADD FinalAssignmentType NVARCHAR(50) NULL; -- Personal, Official, ClassOfService
ALTER TABLE CallRecords ADD RecoveryAmount DECIMAL(18,2) NULL;
```

#### StagingBatch Enhancements
```sql
ALTER TABLE StagingBatches ADD VerificationDeadline DATETIME2 NULL;
ALTER TABLE StagingBatches ADD ApprovalDeadline DATETIME2 NULL;
ALTER TABLE StagingBatches ADD RecoveryProcessingDate DATETIME2 NULL;
ALTER TABLE StagingBatches ADD RecoveryStatus NVARCHAR(50) NULL; -- Pending, InProgress, Completed
ALTER TABLE StagingBatches ADD TotalRecoveredAmount DECIMAL(18,2) NULL;
ALTER TABLE StagingBatches ADD TotalPersonalAmount DECIMAL(18,2) NULL;
ALTER TABLE StagingBatches ADD TotalOfficialAmount DECIMAL(18,2) NULL;
ALTER TABLE StagingBatches ADD TotalClassOfServiceAmount DECIMAL(18,2) NULL;
```

#### CallLogVerification Enhancements
```sql
ALTER TABLE CallLogVerifications ADD SubmissionDeadline DATETIME2 NULL;
ALTER TABLE CallLogVerifications ADD ApprovalDeadline DATETIME2 NULL;
ALTER TABLE CallLogVerifications ADD DeadlineMissed BIT NOT NULL DEFAULT 0;
ALTER TABLE CallLogVerifications ADD RevertDeadline DATETIME2 NULL;
ALTER TABLE CallLogVerifications ADD RevertCount INT NOT NULL DEFAULT 0;
```

### New Enums

```csharp
public enum RecoveryType
{
    StaffNonVerification,
    SupervisorNonApproval,
    SupervisorPartialApproval,
    SupervisorRejection,
    SupervisorRevertFailure,
    ManualOverride
}

public enum RecoveryAction
{
    Personal,
    Official,
    ClassOfService
}

public enum DeadlineType
{
    InitialVerification,
    SupervisorApproval,
    ReVerification,
    FinalDeadline
}

public enum DeadlineStatus
{
    Pending,
    Met,
    Missed,
    Extended,
    Cancelled
}

public enum RecoveryStatus
{
    NotProcessed,
    PendingApproval,
    Processed,
    ManuallyOverridden
}
```

---

## Service Layer Design

### CallLogRecoveryService

```csharp
public class CallLogRecoveryService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CallLogRecoveryService> _logger;
    private readonly INotificationService _notificationService;

    /// <summary>
    /// Process all expired verification deadlines for a batch
    /// </summary>
    public async Task<RecoveryResult> ProcessExpiredVerificationsAsync(Guid batchId)
    {
        // 1. Find all calls with verification_period expired
        // 2. Check if calls are still unverified
        // 3. Mark as PERSONAL
        // 4. Create RecoveryLog entries
        // 5. Update batch statistics
        // 6. Send notifications

        var batch = await _context.StagingBatches
            .FirstOrDefaultAsync(b => b.Id == batchId);

        if (batch == null || batch.VerificationDeadline == null)
            return RecoveryResult.Failed("Invalid batch or no deadline set");

        if (DateTime.UtcNow < batch.VerificationDeadline)
            return RecoveryResult.Failed("Deadline not yet reached");

        // Get all unverified calls for this batch
        var unverifiedCalls = await _context.CallRecords
            .Where(cr => cr.VerificationPeriod == batch.VerificationDeadline
                      && cr.AssignmentStatus == "None")
            .ToListAsync();

        var recoveryLogs = new List<RecoveryLog>();
        decimal totalRecovered = 0;

        foreach (var call in unverifiedCalls)
        {
            call.AssignmentStatus = "Personal";
            call.FinalAssignmentType = "Personal";
            call.RecoveryStatus = "Processed";
            call.RecoveryDate = DateTime.UtcNow;
            call.RecoveryProcessedBy = "System-AutoRecovery";
            call.RecoveryAmount = call.Amount ?? 0;

            totalRecovered += call.RecoveryAmount ?? 0;

            recoveryLogs.Add(new RecoveryLog
            {
                CallRecordId = call.Id,
                BatchId = batchId,
                RecoveryType = "StaffNonVerification",
                RecoveryAction = "Personal",
                RecoveryDate = DateTime.UtcNow,
                RecoveryReason = $"Staff failed to verify by deadline: {batch.VerificationDeadline:yyyy-MM-dd HH:mm}",
                AmountRecovered = call.RecoveryAmount ?? 0,
                RecoveredFrom = call.IndexNumber,
                ProcessedBy = "System-AutoRecovery",
                DeadlineDate = batch.VerificationDeadline,
                IsAutomated = true
            });
        }

        if (recoveryLogs.Any())
        {
            _context.RecoveryLogs.AddRange(recoveryLogs);
            batch.TotalPersonalAmount = (batch.TotalPersonalAmount ?? 0) + totalRecovered;
            await _context.SaveChangesAsync();

            // Send notifications to affected staff
            await NotifyAffectedStaffAsync(unverifiedCalls, RecoveryType.StaffNonVerification);
        }

        return RecoveryResult.Success(unverifiedCalls.Count, totalRecovered);
    }

    /// <summary>
    /// Process expired supervisor approval deadlines
    /// </summary>
    public async Task<RecoveryResult> ProcessExpiredApprovalsAsync(Guid batchId)
    {
        // Similar structure to ProcessExpiredVerificationsAsync
        // But marks as CLASS OF SERVICE instead of PERSONAL

        var batch = await _context.StagingBatches
            .FirstOrDefaultAsync(b => b.Id == batchId);

        if (batch == null || batch.ApprovalDeadline == null)
            return RecoveryResult.Failed("Invalid batch or no approval deadline set");

        if (DateTime.UtcNow < batch.ApprovalDeadline)
            return RecoveryResult.Failed("Approval deadline not yet reached");

        // Get all submitted but not approved verifications
        var pendingApprovals = await _context.CallLogVerifications
            .Include(v => v.CallRecords)
            .Where(v => v.SubmittedToSupervisor
                     && v.SupervisorApprovalStatus == "Pending"
                     && v.ApprovalDeadline == batch.ApprovalDeadline)
            .ToListAsync();

        var recoveryLogs = new List<RecoveryLog>();
        decimal totalRecovered = 0;

        foreach (var verification in pendingApprovals)
        {
            foreach (var call in verification.CallRecords)
            {
                call.AssignmentStatus = "ClassOfService";
                call.FinalAssignmentType = "ClassOfService";
                call.RecoveryStatus = "Processed";
                call.RecoveryDate = DateTime.UtcNow;
                call.RecoveryProcessedBy = "System-AutoRecovery";

                // Calculate class of service amount
                var classOfServiceAmount = await CalculateClassOfServiceAmountAsync(call);
                call.RecoveryAmount = classOfServiceAmount;
                totalRecovered += classOfServiceAmount;

                recoveryLogs.Add(new RecoveryLog
                {
                    CallRecordId = call.Id,
                    BatchId = batchId,
                    RecoveryType = "SupervisorNonApproval",
                    RecoveryAction = "ClassOfService",
                    RecoveryDate = DateTime.UtcNow,
                    RecoveryReason = $"Supervisor failed to approve by deadline: {batch.ApprovalDeadline:yyyy-MM-dd HH:mm}",
                    AmountRecovered = classOfServiceAmount,
                    RecoveredFrom = verification.SupervisorIndexNumber,
                    ProcessedBy = "System-AutoRecovery",
                    DeadlineDate = batch.ApprovalDeadline,
                    IsAutomated = true
                });
            }
        }

        if (recoveryLogs.Any())
        {
            _context.RecoveryLogs.AddRange(recoveryLogs);
            batch.TotalClassOfServiceAmount = (batch.TotalClassOfServiceAmount ?? 0) + totalRecovered;
            await _context.SaveChangesAsync();

            // Notify supervisors
            await NotifySupervisorsAsync(pendingApprovals, RecoveryType.SupervisorNonApproval);
        }

        return RecoveryResult.Success(recoveryLogs.Count, totalRecovered);
    }

    /// <summary>
    /// Process supervisor partial approvals
    /// </summary>
    public async Task ProcessPartialApprovalAsync(int verificationId, List<int> approvedCallIds)
    {
        var verification = await _context.CallLogVerifications
            .Include(v => v.CallRecords)
            .FirstOrDefaultAsync(v => v.Id == verificationId);

        if (verification == null) return;

        foreach (var call in verification.CallRecords)
        {
            if (approvedCallIds.Contains(call.Id))
            {
                // Approved calls
                call.AssignmentStatus = call.VerificationType == "Personal" ? "Personal" : "Official";
                call.FinalAssignmentType = call.VerificationType == "Personal" ? "Personal" : "Official";
                call.SupervisorApprovalStatus = "Approved";
            }
            else
            {
                // Non-approved calls become Personal
                call.AssignmentStatus = "Personal";
                call.FinalAssignmentType = "Personal";
                call.SupervisorApprovalStatus = "PartiallyApproved";
            }

            call.RecoveryStatus = "Processed";
            call.RecoveryDate = DateTime.UtcNow;
            call.RecoveryAmount = call.Amount ?? 0;

            // Create recovery log
            _context.RecoveryLogs.Add(new RecoveryLog
            {
                CallRecordId = call.Id,
                BatchId = verification.BatchId ?? Guid.Empty,
                RecoveryType = "SupervisorPartialApproval",
                RecoveryAction = call.FinalAssignmentType,
                RecoveryDate = DateTime.UtcNow,
                RecoveryReason = approvedCallIds.Contains(call.Id)
                    ? "Approved by supervisor"
                    : "Not approved by supervisor - recovered as personal",
                AmountRecovered = call.RecoveryAmount ?? 0,
                RecoveredFrom = call.IndexNumber,
                ProcessedBy = verification.SupervisorIndexNumber,
                IsAutomated = false
            });
        }

        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Process reverted verifications that missed re-verification deadline
    /// </summary>
    public async Task<RecoveryResult> ProcessRevertedVerificationsAsync(Guid batchId)
    {
        // Find verifications that were reverted and deadline passed
        var revertedExpired = await _context.CallLogVerifications
            .Include(v => v.CallRecords)
            .Where(v => v.SupervisorApprovalStatus == "Reverted"
                     && v.RevertDeadline != null
                     && v.RevertDeadline < DateTime.UtcNow
                     && !v.SubmittedToSupervisor) // Not re-submitted
            .ToListAsync();

        var recoveryLogs = new List<RecoveryLog>();
        decimal totalRecovered = 0;

        foreach (var verification in revertedExpired)
        {
            foreach (var call in verification.CallRecords)
            {
                call.AssignmentStatus = "Personal";
                call.FinalAssignmentType = "Personal";
                call.RecoveryStatus = "Processed";
                call.RecoveryDate = DateTime.UtcNow;
                call.RecoveryProcessedBy = "System-AutoRecovery";
                call.RecoveryAmount = call.Amount ?? 0;

                totalRecovered += call.RecoveryAmount ?? 0;

                recoveryLogs.Add(new RecoveryLog
                {
                    CallRecordId = call.Id,
                    BatchId = batchId,
                    RecoveryType = "SupervisorRevertFailure",
                    RecoveryAction = "Personal",
                    RecoveryDate = DateTime.UtcNow,
                    RecoveryReason = $"Staff failed to re-verify after supervisor revert. Revert deadline: {verification.RevertDeadline:yyyy-MM-dd HH:mm}",
                    AmountRecovered = call.RecoveryAmount ?? 0,
                    RecoveredFrom = call.IndexNumber,
                    ProcessedBy = "System-AutoRecovery",
                    DeadlineDate = verification.RevertDeadline,
                    IsAutomated = true
                });
            }

            verification.DeadlineMissed = true;
        }

        if (recoveryLogs.Any())
        {
            _context.RecoveryLogs.AddRange(recoveryLogs);
            await _context.SaveChangesAsync();
        }

        return RecoveryResult.Success(recoveryLogs.Count, totalRecovered);
    }

    private async Task<decimal> CalculateClassOfServiceAmountAsync(CallRecord call)
    {
        // Get user's class of service allowance
        var userPhone = await _context.UserPhones
            .Include(up => up.ClassOfService)
            .FirstOrDefaultAsync(up => up.IndexNumber == call.IndexNumber
                                    && up.PhoneNumber == call.PhoneNumber);

        if (userPhone?.ClassOfService != null)
        {
            // Return the allowance amount or a portion based on usage
            return userPhone.ClassOfService.HandsetAllowanceAmount ?? 0;
        }

        return call.Amount ?? 0;
    }
}
```

### DeadlineManagementService

```csharp
public class DeadlineManagementService
{
    /// <summary>
    /// Get all expired verification deadlines that haven't been processed
    /// </summary>
    public async Task<List<ExpiredDeadline>> GetExpiredVerificationDeadlinesAsync()
    {
        return await _context.DeadlineTracking
            .Where(dt => dt.DeadlineType == "Verification"
                      && dt.DeadlineDate < DateTime.UtcNow
                      && dt.DeadlineStatus == "Pending"
                      && !dt.RecoveryProcessed)
            .ToListAsync();
    }

    /// <summary>
    /// Send deadline reminders to staff/supervisors
    /// </summary>
    public async Task SendDeadlineRemindersAsync()
    {
        var config = await _context.RecoveryConfiguration
            .FirstOrDefaultAsync(rc => rc.RuleName == "DeadlineReminders");

        if (config == null || !config.NotificationEnabled)
            return;

        var reminderDays = config.ReminderDaysBefore ?? 2;
        var reminderDate = DateTime.UtcNow.AddDays(reminderDays);

        // Get upcoming deadlines
        var upcomingDeadlines = await _context.DeadlineTracking
            .Where(dt => dt.DeadlineDate <= reminderDate
                      && dt.DeadlineDate > DateTime.UtcNow
                      && dt.DeadlineStatus == "Pending")
            .ToListAsync();

        foreach (var deadline in upcomingDeadlines)
        {
            await _notificationService.SendDeadlineReminderAsync(
                deadline.TargetEntity,
                deadline.DeadlineType,
                deadline.DeadlineDate
            );
        }
    }

    /// <summary>
    /// Extend deadline with approval
    /// </summary>
    public async Task<bool> ExtendDeadlineAsync(int deadlineId, DateTime newDeadline, string reason, string approvedBy)
    {
        var deadline = await _context.DeadlineTracking.FindAsync(deadlineId);
        if (deadline == null) return false;

        deadline.ExtendedDeadline = newDeadline;
        deadline.ExtensionReason = reason;
        deadline.ExtensionApprovedBy = approvedBy;
        deadline.DeadlineStatus = "Extended";

        await _context.SaveChangesAsync();
        return true;
    }
}
```

### CallLogReportingService

```csharp
public class CallLogReportingService
{
    /// <summary>
    /// Generate recovery summary report
    /// </summary>
    public async Task<RecoverySummaryReport> GetRecoverySummaryAsync(ReportFilter filter)
    {
        var query = _context.RecoveryLogs
            .Include(rl => rl.CallRecord)
            .AsQueryable();

        // Apply filters
        if (filter.StartDate.HasValue)
            query = query.Where(rl => rl.RecoveryDate >= filter.StartDate.Value);
        if (filter.EndDate.HasValue)
            query = query.Where(rl => rl.RecoveryDate <= filter.EndDate.Value);
        if (!string.IsNullOrEmpty(filter.IndexNumber))
            query = query.Where(rl => rl.RecoveredFrom == filter.IndexNumber);
        if (filter.BatchId.HasValue)
            query = query.Where(rl => rl.BatchId == filter.BatchId.Value);

        var logs = await query.ToListAsync();

        return new RecoverySummaryReport
        {
            TotalRecords = logs.Count,
            TotalAmountRecovered = logs.Sum(l => l.AmountRecovered),

            PersonalRecovery = new RecoveryBreakdown
            {
                Count = logs.Count(l => l.RecoveryAction == "Personal"),
                Amount = logs.Where(l => l.RecoveryAction == "Personal").Sum(l => l.AmountRecovered)
            },

            OfficialRecovery = new RecoveryBreakdown
            {
                Count = logs.Count(l => l.RecoveryAction == "Official"),
                Amount = logs.Where(l => l.RecoveryAction == "Official").Sum(l => l.AmountRecovered)
            },

            ClassOfServiceRecovery = new RecoveryBreakdown
            {
                Count = logs.Count(l => l.RecoveryAction == "ClassOfService"),
                Amount = logs.Where(l => l.RecoveryAction == "ClassOfService").Sum(l => l.AmountRecovered)
            },

            RecoveryByType = logs.GroupBy(l => l.RecoveryType)
                .Select(g => new RecoveryTypeBreakdown
                {
                    Type = g.Key,
                    Count = g.Count(),
                    Amount = g.Sum(l => l.AmountRecovered)
                })
                .ToList()
        };
    }

    /// <summary>
    /// Get detailed staff recovery report
    /// </summary>
    public async Task<List<StaffRecoveryDetail>> GetStaffRecoveryDetailsAsync(ReportFilter filter)
    {
        var query = _context.RecoveryLogs
            .Include(rl => rl.CallRecord)
            .AsQueryable();

        // Apply filters...

        var staffRecovery = await query
            .GroupBy(rl => rl.RecoveredFrom)
            .Select(g => new StaffRecoveryDetail
            {
                IndexNumber = g.Key,
                TotalCalls = g.Count(),
                TotalAmount = g.Sum(rl => rl.AmountRecovered),
                PersonalCalls = g.Count(rl => rl.RecoveryAction == "Personal"),
                PersonalAmount = g.Where(rl => rl.RecoveryAction == "Personal").Sum(rl => rl.AmountRecovered),
                OfficialCalls = g.Count(rl => rl.RecoveryAction == "Official"),
                OfficialAmount = g.Where(rl => rl.RecoveryAction == "Official").Sum(rl => rl.AmountRecovered),
                MissedDeadlines = g.Count(rl => rl.RecoveryType == "StaffNonVerification")
            })
            .ToListAsync();

        // Enrich with EbillUser details
        foreach (var detail in staffRecovery)
        {
            var user = await _context.EbillUsers.FirstOrDefaultAsync(u => u.IndexNumber == detail.IndexNumber);
            if (user != null)
            {
                detail.FirstName = user.FirstName;
                detail.LastName = user.LastName;
                detail.Email = user.Email;
            }
        }

        return staffRecovery;
    }
}
```

---

## Background Job Service

### Implementation: Hosted Service (Built-in ASP.NET Core)

```csharp
public class RecoveryAutomationJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RecoveryAutomationJob> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(1); // Run every hour

    public RecoveryAutomationJob(
        IServiceProvider serviceProvider,
        ILogger<RecoveryAutomationJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Recovery Automation Job started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessRecoveriesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Recovery Automation Job");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }
    }

    private async Task ProcessRecoveriesAsync()
    {
        using var scope = _serviceProvider.CreateScope();

        var recoveryService = scope.ServiceProvider.GetRequiredService<CallLogRecoveryService>();
        var deadlineService = scope.ServiceProvider.GetRequiredService<DeadlineManagementService>();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        _logger.LogInformation("Running recovery automation at {Time}", DateTime.UtcNow);

        // 1. Send deadline reminders
        await deadlineService.SendDeadlineRemindersAsync();

        // 2. Get expired verification deadlines
        var expiredVerifications = await deadlineService.GetExpiredVerificationDeadlinesAsync();
        foreach (var deadline in expiredVerifications)
        {
            _logger.LogInformation("Processing expired verification deadline for batch {BatchId}", deadline.BatchId);
            var result = await recoveryService.ProcessExpiredVerificationsAsync(deadline.BatchId);

            if (result.Success)
            {
                deadline.RecoveryProcessed = true;
                deadline.RecoveryProcessedDate = DateTime.UtcNow;
                deadline.DeadlineStatus = "Missed";

                _logger.LogInformation(
                    "Processed {Count} expired verifications, recovered {Amount:C}",
                    result.RecordsProcessed,
                    result.AmountRecovered);
            }
        }

        // 3. Get expired approval deadlines
        var expiredApprovals = await deadlineService.GetExpiredApprovalDeadlinesAsync();
        foreach (var deadline in expiredApprovals)
        {
            _logger.LogInformation("Processing expired approval deadline for batch {BatchId}", deadline.BatchId);
            var result = await recoveryService.ProcessExpiredApprovalsAsync(deadline.BatchId);

            if (result.Success)
            {
                deadline.RecoveryProcessed = true;
                deadline.RecoveryProcessedDate = DateTime.UtcNow;
                deadline.DeadlineStatus = "Missed";

                _logger.LogInformation(
                    "Processed {Count} expired approvals, recovered {Amount:C}",
                    result.RecordsProcessed,
                    result.AmountRecovered);
            }
        }

        // 4. Process reverted verifications
        var activeBatches = await context.StagingBatches
            .Where(b => b.BatchStatus == BatchStatus.InProgress || b.BatchStatus == BatchStatus.PendingApproval)
            .ToListAsync();

        foreach (var batch in activeBatches)
        {
            var result = await recoveryService.ProcessRevertedVerificationsAsync(batch.Id);
            if (result.Success && result.RecordsProcessed > 0)
            {
                _logger.LogInformation(
                    "Processed {Count} reverted verifications for batch {BatchId}, recovered {Amount:C}",
                    result.RecordsProcessed,
                    batch.Id,
                    result.AmountRecovered);
            }
        }

        await context.SaveChangesAsync();

        _logger.LogInformation("Recovery automation completed at {Time}", DateTime.UtcNow);
    }
}

// Register in Program.cs
builder.Services.AddHostedService<RecoveryAutomationJob>();
```

---

## UI/UX Design

### New Menu Structure

Update `/Pages/Shared/_Layout.cshtml`:

```html
<!-- Add new Reporting menu item after Administration -->
<li class="nav-item">
    <a class="nav-link" href="#" data-bs-toggle="collapse" data-bs-target="#reportingSubmenu"
       aria-expanded="false" aria-controls="reportingSubmenu">
        <i class="bi bi-bar-chart-line me-2"></i>
        <span>Reporting</span>
        <i class="bi bi-chevron-down ms-auto"></i>
    </a>
    <div class="collapse" id="reportingSubmenu">
        <ul class="nav flex-column ms-3">
            <li class="nav-item">
                <a class="nav-link submenu-link" asp-page="/Reports/RecoverySummary">
                    <i class="bi bi-pie-chart me-2"></i> Recovery Summary
                </a>
            </li>
            <li class="nav-item">
                <a class="nav-link submenu-link" asp-page="/Reports/StaffRecoveryReport">
                    <i class="bi bi-people me-2"></i> Staff Recovery
                </a>
            </li>
            <li class="nav-item">
                <a class="nav-link submenu-link" asp-page="/Reports/SupervisorActivity">
                    <i class="bi bi-person-check me-2"></i> Supervisor Activity
                </a>
            </li>
            <li class="nav-item">
                <a class="nav-link submenu-link" asp-page="/Reports/DeadlineTracking">
                    <i class="bi bi-clock-history me-2"></i> Deadline Tracking
                </a>
            </li>
            <li class="nav-item">
                <a class="nav-link submenu-link" asp-page="/Reports/RecoveryAuditLog">
                    <i class="bi bi-journal-text me-2"></i> Recovery Audit Log
                </a>
            </li>
            <li class="nav-item">
                <a class="nav-link submenu-link" asp-page="/Reports/BatchAnalysis">
                    <i class="bi bi-graph-up me-2"></i> Batch Analysis
                </a>
            </li>
        </ul>
    </div>
</li>
```

### Report Pages

#### 1. Recovery Summary Dashboard
**Path:** `/Pages/Reports/RecoverySummary.cshtml`

**Features:**
- High-level KPI cards (Total Recovered, Personal, Official, Class of Service)
- Recovery breakdown by type (pie chart)
- Trend over time (line chart)
- Recent recovery activities (table)
- Filter by date range, batch, department

**Mockup:**
```
┌─────────────────────────────────────────────────────────────┐
│  Recovery Summary Dashboard                    [Date Filter] │
├─────────────────────────────────────────────────────────────┤
│                                                               │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐   │
│  │ Total    │  │ Personal │  │ Official │  │ Class of │   │
│  │ $125,450 │  │ $45,200  │  │ $55,000  │  │ Service  │   │
│  │ 2,450    │  │ 890 calls│  │ 1,100    │  │ $25,250  │   │
│  └──────────┘  └──────────┘  └──────────┘  │ 460 calls│   │
│                                              └──────────┘   │
│                                                               │
│  ┌─────────────────┐  ┌────────────────────────────────┐   │
│  │  Recovery by    │  │  Recovery Trend (Last 6 mos)   │   │
│  │  Type           │  │                                 │   │
│  │                 │  │   ^                             │   │
│  │  [Pie Chart]    │  │   │    /-\                      │   │
│  │                 │  │   │   /   \    /\               │   │
│  │                 │  │   │  /     \  /  \              │   │
│  │                 │  │   │ /       \/    \             │   │
│  │                 │  │   └──────────────────────>      │   │
│  └─────────────────┘  └────────────────────────────────┘   │
│                                                               │
│  Recent Recovery Activities                                  │
│  ┌────────────────────────────────────────────────────────┐ │
│  │ Date       │ Type              │ Staff    │ Amount     │ │
│  ├────────────────────────────────────────────────────────┤ │
│  │ 2025-10-10 │ StaffNonVerif    │ 8817861  │ $1,250.00 │ │
│  │ 2025-10-09 │ SupervisorNonApp │ Multiple │ $5,400.00 │ │
│  └────────────────────────────────────────────────────────┘ │
│                                           [Export to Excel]  │
└─────────────────────────────────────────────────────────────┘
```

#### 2. Staff Recovery Report
**Path:** `/Pages/Reports/StaffRecoveryReport.cshtml`

**Features:**
- List all staff with recovery details
- Search/filter by name, index, department
- Sort by amount, missed deadlines, personal vs official ratio
- Drill-down to individual staff details
- Export to Excel

**Layout:**
```
┌─────────────────────────────────────────────────────────────────────┐
│  Staff Recovery Report                      [Search] [Filters]      │
├─────────────────────────────────────────────────────────────────────┤
│                                                                       │
│  ┌────┬──────────┬────────┬────────┬──────────┬──────────┬────────┐│
│  │ #  │ Staff    │ Total  │Personal│ Official │ Class of │Missed  ││
│  │    │          │ Amount │ Amount │ Amount   │ Service  │Deadlines│
│  ├────┼──────────┼────────┼────────┼──────────┼──────────┼────────┤│
│  │ 1  │John Doe  │$3,450  │$2,100  │ $1,350   │ $0       │ 2      ││
│  │    │8817861   │150 calls│ 80 calls│ 70 calls│ 0 calls │        ││
│  │    │          │[View Details]                                   ││
│  ├────┼──────────┼────────┼────────┼──────────┼──────────┼────────┤│
│  │ 2  │Jane Smith│$2,890  │$1,200  │ $1,200   │ $490     │ 1      ││
│  │    │8817862   │120 calls│ 50 calls│ 50 calls│ 20 calls│        ││
│  │    │          │[View Details]                                   ││
│  └────┴──────────┴────────┴────────┴──────────┴──────────┴────────┘│
│                                                                       │
│  Pagination: [1] [2] [3] ... [10]           [Export to Excel]       │
└─────────────────────────────────────────────────────────────────────┘
```

#### 3. Supervisor Activity Report
**Path:** `/Pages/Reports/SupervisorActivity.cshtml`

**Features:**
- Show supervisor approval statistics
- Approval rate, rejection rate, average response time
- Missed deadlines
- Staff under supervision statistics

#### 4. Deadline Tracking
**Path:** `/Pages/Reports/DeadlineTracking.cshtml`

**Features:**
- Show all active deadlines
- Highlight approaching deadlines (within reminder period)
- Show missed deadlines
- Allow deadline extensions with approval
- Send manual reminders

#### 5. Recovery Audit Log
**Path:** `/Pages/Reports/RecoveryAuditLog.cshtml`

**Features:**
- Complete audit trail of all recovery actions
- Filter by date, type, staff, amount range
- Show automated vs manual recoveries
- Export for compliance/auditing

#### 6. Batch Analysis
**Path:** `/Pages/Reports/BatchAnalysis.cshtml`

**Features:**
- Compare batches over time
- Batch completion rates
- Recovery breakdown by batch
- Identify problem areas (high personal recovery batches)

---

## Implementation Phases

### Phase 1: Database Foundation (Week 1)
**Goal:** Establish data model for recovery tracking

**Tasks:**
1. Create migration for RecoveryLog table
2. Create migration for DeadlineTracking table
3. Create migration for RecoveryConfiguration table
4. Add columns to CallRecord (RecoveryStatus, RecoveryDate, etc.)
5. Add columns to StagingBatch (deadlines and totals)
6. Add columns to CallLogVerification (deadline tracking)
7. Create C# models for new tables
8. Create enums (RecoveryType, RecoveryAction, DeadlineType, etc.)
9. Update DbContext with new DbSets
10. Test migrations on local database
11. Apply migrations to Azure SQL

**Deliverables:**
- All database migrations complete
- Models created and tested
- Database ready for service layer

### Phase 2: Core Recovery Services (Week 2-3)
**Goal:** Build business logic for recovery automation

**Tasks:**
1. Create CallLogRecoveryService with all methods
   - ProcessExpiredVerificationsAsync
   - ProcessExpiredApprovalsAsync
   - ProcessPartialApprovalAsync
   - ProcessRevertedVerificationsAsync
2. Create DeadlineManagementService
   - Deadline tracking methods
   - Reminder methods
   - Extension methods
3. Create CallLogReportingService
   - Report generation methods
   - Statistics calculation
   - Export methods
4. Implement result/response DTOs
5. Add comprehensive logging
6. Write unit tests for all services
7. Write integration tests for workflows

**Deliverables:**
- All services implemented and tested
- Service documentation
- Test coverage >80%

### Phase 3: Background Automation (Week 4)
**Goal:** Automate recovery processing

**Tasks:**
1. Implement RecoveryAutomationJob as Hosted Service
2. Configure check interval (default: hourly)
3. Implement error handling and retries
4. Add comprehensive logging
5. Create admin page to view job status
6. Create admin page to manually trigger job
7. Test automation with various scenarios
8. Setup monitoring/alerting for job failures

**Deliverables:**
- Background job running and tested
- Admin controls implemented
- Monitoring in place

### Phase 4: Deadline Management UI (Week 5)
**Goal:** Allow admins to set and manage deadlines

**Tasks:**
1. Update CallLogStaging page to set verification deadline
2. Update CallLogStaging page to set approval deadline
3. Add deadline display to MyCallLogs page
4. Add deadline display to SupervisorApprovals page
5. Create deadline extension request workflow
6. Add deadline countdown/warnings in UI
7. Add automatic deadline calculation based on config

**Deliverables:**
- Deadline UI complete
- User notifications working
- Extension workflow tested

### Phase 5: Reporting Menu & Pages (Week 6-7)
**Goal:** Create reporting infrastructure

**Tasks:**
1. Add "Reporting" menu to _Layout.cshtml
2. Create RecoverySummary page with KPIs and charts
3. Create StaffRecoveryReport page with detailed table
4. Create SupervisorActivity page
5. Create DeadlineTracking page
6. Create RecoveryAuditLog page
7. Create BatchAnalysis page
8. Implement report filters and search
9. Add export to Excel functionality
10. Implement pagination for large reports
11. Add report access permissions (Admin, Supervisor, Staff views)

**Deliverables:**
- All report pages complete
- Export functionality working
- Role-based access implemented

### Phase 6: Charts & Visualizations (Week 8)
**Goal:** Add visual representations of data

**Tasks:**
1. Choose charting library (Chart.js recommended)
2. Implement pie chart for recovery breakdown
3. Implement line chart for trends
4. Implement bar chart for batch comparisons
5. Implement heatmap for deadline compliance
6. Make charts interactive and responsive
7. Add chart export functionality

**Deliverables:**
- All charts implemented
- Interactive features working
- Mobile responsive

### Phase 7: Notifications & Alerts (Week 9)
**Goal:** Keep users informed of deadlines and actions

**Tasks:**
1. Enhance NotificationService for recovery notifications
2. Send email notifications for approaching deadlines
3. Send email notifications when recovery is processed
4. Create in-app notification center
5. Add notification preferences for users
6. Implement escalation for missed deadlines

**Deliverables:**
- Notification system complete
- Email templates created
- User preferences working

### Phase 8: Testing & Refinement (Week 10)
**Goal:** Ensure system quality and reliability

**Tasks:**
1. Comprehensive end-to-end testing
2. Load testing for large batches
3. Edge case testing
4. User acceptance testing with real users
5. Performance optimization
6. Bug fixes
7. Documentation updates
8. Training materials creation

**Deliverables:**
- System fully tested
- Performance benchmarks met
- Documentation complete

### Phase 9: Deployment (Week 11)
**Goal:** Deploy to production

**Tasks:**
1. Deploy database migrations to Azure SQL
2. Deploy application to Azure App Service
3. Configure background job in production
4. Verify all configurations
5. Monitor for issues
6. Gradual rollout to users
7. Collect feedback

**Deliverables:**
- System live in production
- Monitoring active
- User feedback collected

### Phase 10: Iteration & Enhancement (Ongoing)
**Goal:** Continuous improvement

**Tasks:**
1. Add requested features from user feedback
2. Optimize based on usage patterns
3. Add more advanced reports
4. Enhance visualizations
5. Improve performance

---

## Testing Strategy

### Unit Tests
- Test all service methods independently
- Mock dependencies
- Test edge cases (null values, empty lists, etc.)
- Test business logic accuracy
- Target: >80% code coverage

### Integration Tests
- Test complete workflows end-to-end
- Test database interactions
- Test service interactions
- Use test database
- Verify data integrity

### Scenario Tests
**Scenario 1: Staff Non-Verification**
1. Create batch with verification deadline
2. Add unverified calls
3. Wait for deadline to pass (or mock time)
4. Run recovery job
5. Verify calls marked as PERSONAL
6. Verify RecoveryLog entries created
7. Verify notifications sent

**Scenario 2: Supervisor Non-Approval**
1. Staff verifies calls as Official
2. Submit to supervisor
3. Set approval deadline
4. Don't approve (or mock time)
5. Run recovery job
6. Verify calls marked as CLASS OF SERVICE
7. Verify RecoveryLog entries
8. Verify notifications

**Scenario 3: Partial Approval**
1. Staff submits 100 calls as Official
2. Supervisor approves 75, doesn't approve 25
3. Verify 75 marked as OFFICIAL
4. Verify 25 marked as PERSONAL
5. Verify RecoveryLog entries for both sets

**Scenario 4: Revert and Re-Failure**
1. Supervisor reverts verification
2. Set new deadline
3. Staff doesn't re-verify
4. Run recovery job
5. Verify calls marked as PERSONAL
6. Verify revert count incremented

### Performance Tests
- Test with large batches (10,000+ calls)
- Test concurrent user access to reports
- Test background job performance
- Monitor database query performance
- Optimize slow queries

### User Acceptance Testing
- Test with real users in staging environment
- Gather feedback on UI/UX
- Validate business logic with stakeholders
- Ensure reports meet requirements

---

## Security Considerations

### Authorization
- Admin: Full access to all reports and recovery configurations
- Supervisor: Access to their staff's reports and approval functions
- Staff: Access to their own recovery details only
- Implement role-based authorization on all report pages

### Data Privacy
- Protect sensitive call details
- Audit log access to reports
- Mask sensitive phone numbers in logs
- Comply with data retention policies

### Audit Trail
- Log all recovery actions
- Log all manual overrides
- Log configuration changes
- Log report access (who viewed what and when)

### Validation
- Validate all deadline dates (must be future dates)
- Validate recovery amounts
- Prevent double-processing of recoveries
- Validate user permissions before processing

---

## Configuration Management

### RecoveryConfiguration Table

**Default Configuration:**
```json
[
  {
    "RuleName": "DefaultVerificationDeadline",
    "RuleType": "Deadline",
    "IsEnabled": true,
    "DefaultVerificationDays": 7,
    "AutomationEnabled": true,
    "NotificationEnabled": true,
    "ReminderDaysBefore": 2
  },
  {
    "RuleName": "DefaultApprovalDeadline",
    "RuleType": "Deadline",
    "IsEnabled": true,
    "DefaultApprovalDays": 5,
    "AutomationEnabled": true,
    "NotificationEnabled": true,
    "ReminderDaysBefore": 1
  },
  {
    "RuleName": "RevertDeadline",
    "RuleType": "Deadline",
    "IsEnabled": true,
    "DefaultVerificationDays": 3,
    "AutomationEnabled": true,
    "NotificationEnabled": true,
    "ReminderDaysBefore": 1
  },
  {
    "RuleName": "AutoRecoveryProcessing",
    "RuleType": "Automation",
    "IsEnabled": true,
    "AutomationEnabled": true,
    "RequireApprovalForAutomation": false
  }
]
```

### Admin Configuration Page
Create `/Admin/RecoveryConfiguration` page to allow admins to:
- View all recovery rules
- Enable/disable automation
- Change default deadline durations
- Configure notification settings
- Test recovery processing manually

---

## Performance Optimization

### Database Indexing
```sql
-- Optimize recovery log queries
CREATE INDEX IX_RecoveryLogs_RecoveryDate_Include ON RecoveryLogs(RecoveryDate)
INCLUDE (RecoveryType, RecoveryAction, AmountRecovered);

CREATE INDEX IX_RecoveryLogs_RecoveredFrom_Include ON RecoveryLogs(RecoveredFrom)
INCLUDE (RecoveryDate, AmountRecovered);

-- Optimize deadline tracking
CREATE INDEX IX_DeadlineTracking_DeadlineDate_Status ON DeadlineTracking(DeadlineDate, DeadlineStatus);

-- Optimize call record queries
CREATE INDEX IX_CallRecords_VerificationPeriod_Status ON CallRecords(verification_period, assignment_status);
```

### Caching Strategy
- Cache report data for 5 minutes
- Cache configuration values
- Invalidate cache on data updates
- Use memory cache for frequently accessed lookups

### Query Optimization
- Use pagination for large result sets
- Limit initial page loads to summary data
- Load details on-demand
- Use database views for complex reports
- Consider materialized views for heavy reports

---

## Risks & Mitigation

### Risk 1: Double Processing
**Risk:** Recovery job runs twice and processes same records
**Mitigation:**
- Check `RecoveryStatus` before processing
- Use database transactions
- Add unique constraints where applicable
- Implement idempotent operations

### Risk 2: Incorrect Recovery Amounts
**Risk:** Wrong amounts calculated or assigned
**Mitigation:**
- Comprehensive unit tests for calculations
- Manual verification for first few batches
- Admin override capability
- Detailed audit logs

### Risk 3: Deadline Confusion
**Risk:** Users confused about which deadline applies
**Mitigation:**
- Clear UI messaging
- Countdown displays
- Email reminders
- User training
- Help documentation

### Risk 4: Performance Degradation
**Risk:** Reports slow with large data volumes
**Mitigation:**
- Database indexing
- Query optimization
- Pagination
- Async loading
- Caching

### Risk 5: Notification Overload
**Risk:** Too many notifications annoy users
**Mitigation:**
- User notification preferences
- Digest emails (daily summary)
- Smart notification timing
- Configurable reminder frequency

---

## Success Metrics

### Business Metrics
- **Recovery Accuracy:** >95% of recoveries correctly classified
- **Deadline Compliance:** >80% of staff meet verification deadlines
- **Supervisor Response Rate:** >90% of supervisors respond within deadline
- **User Satisfaction:** >4/5 rating from users
- **Processing Time:** Automated recovery completes within 30 minutes for 10K records

### Technical Metrics
- **System Uptime:** >99.5%
- **Background Job Success Rate:** >99%
- **Report Load Time:** <3 seconds for summary reports
- **Database Query Performance:** <500ms for 90% of queries
- **API Response Time:** <200ms for 95% of requests

### Operational Metrics
- **Manual Interventions:** <5% of recoveries require manual override
- **Support Tickets:** <10 per month related to reporting
- **Bug Reports:** <3 critical bugs per quarter
- **User Adoption:** >80% of supervisors use reports regularly

---

## Future Enhancements

### Phase 2 Features (Post-Launch)
1. **Machine Learning Integration**
   - Predict likely personal vs official calls
   - Suggest optimal deadline durations
   - Identify unusual patterns

2. **Advanced Analytics**
   - Predictive billing forecasts
   - Anomaly detection
   - Cost trend analysis

3. **Mobile App**
   - Mobile verification interface
   - Push notifications
   - Quick approval actions

4. **API Integration**
   - REST API for external systems
   - Webhook notifications
   - Third-party reporting tools integration

5. **Advanced Reporting**
   - Custom report builder
   - Scheduled report delivery
   - More visualization types

---

## Conclusion

This reporting system will provide a robust foundation for automated call log recovery based on verification deadlines and supervisor approvals. The phased implementation approach ensures incremental delivery of value while maintaining system stability. The comprehensive audit trail and reporting capabilities will enable data-driven decision making and ensure accountability in the billing process.

**Next Steps:**
1. Review and approve this plan
2. Begin Phase 1: Database Foundation
3. Set up project tracking (Jira, Azure DevOps, etc.)
4. Schedule regular progress reviews
5. Prepare test environments

**Estimated Timeline:** 11 weeks for full implementation
**Team Required:** 2-3 developers, 1 QA engineer, 1 product owner
**Budget Considerations:** Development time, Azure resources, potential third-party tools

---

**Document Version History:**
- v1.0 - October 13, 2025 - Initial plan created

**Questions? Contact:** Development Team
