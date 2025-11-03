# SIM Request Workflow - Complete Email Implementation Guide

## Table of Contents
1. [Overview](#overview)
2. [Installation](#installation)
3. [Template Catalog](#template-catalog)
4. [Implementation](#implementation)
5. [Code Examples](#code-examples)
6. [Testing](#testing)
7. [Troubleshooting](#troubleshooting)

---

## Overview

This guide provides complete implementation instructions for the SIM Request email notification system. The system includes **7 email templates** covering the entire SIM request workflow from submission to collection.

### Workflow Stages

```
1. Request Submitted → 2. Supervisor Review → 3. ICTS Processing → 4. Admin Approval → 5. Ready for Collection
                           ↓ (if rejected)         ↓ (if cancelled)
                        Rejection Notice        Cancellation Notice
```

### Email Templates

| Template | Code | Sent To | Trigger |
|----------|------|---------|---------|
| Supervisor Notification | `SIM_REQUEST_SUPERVISOR_NOTIFICATION` | Supervisor | When request is submitted |
| Request Submitted | `SIM_REQUEST_SUBMITTED` | Requester | Immediately after submission |
| Request Approved | `SIM_REQUEST_APPROVED` | Requester | When supervisor/admin approves |
| Request Rejected | `SIM_REQUEST_REJECTED` | Requester | When request is rejected |
| ICTS Notification | `SIM_REQUEST_ICTS_NOTIFICATION` | ICTS Team | After supervisor approval |
| Ready for Collection | `SIM_READY_FOR_COLLECTION` | Requester | When SIM is ready |
| Request Cancelled | `SIM_REQUEST_CANCELLED` | Requester | When request is cancelled |

---

## Installation

### Step 1: Run SQL Script

Execute the comprehensive SQL script to install all templates:

**Option A: Using SSMS**
```sql
-- Open INSERT_ALL_SIM_EMAIL_TEMPLATES.sql in SQL Server Management Studio
-- Execute the script
```

**Option B: Using Command Line**
```bash
sqlcmd -S MICHUKI\SQLEXPRESS -d TABDB -E -i INSERT_ALL_SIM_EMAIL_TEMPLATES.sql
```

### Step 2: Verify Installation

Navigate to `http://localhost:5041/Admin/EmailTemplates` and verify that all 7 templates are present with:
- ✅ **IsActive** = True
- ✅ **Category** = "SIM Management"

### Step 3: Test Email Configuration

Before using templates, ensure your email configuration is set up:
1. Go to `/Admin/EmailConfiguration`
2. Verify SMTP settings
3. Test connection using the "Test Connection" button

---

## Template Catalog

### Template 1: SIM_REQUEST_SUPERVISOR_NOTIFICATION

**Purpose:** Notify supervisor when a new SIM request requires their approval

**Placeholders:**
```csharp
RequestId, RequestDate, SimType, ServiceProvider, Remarks,
FirstName, LastName, IndexNo, Organization, Office, Grade,
FunctionalTitle, OfficialEmail, OfficeExtension, SupervisorName,
SupervisorEmail, ApprovalLink, Year
```

**Subject:** `New SIM Card Request Requires Your Approval - {{FirstName}} {{LastName}}`

---

### Template 2: SIM_REQUEST_SUBMITTED

**Purpose:** Confirm to requester that their request was received

**Placeholders:**
```csharp
RequestId, RequestDate, FirstName, LastName, SimType,
ServiceProvider, IndexNo, Organization, Office, SupervisorName,
ViewRequestLink, Year
```

**Subject:** `SIM Card Request Submitted - Request #{{RequestId}}`

---

### Template 3: SIM_REQUEST_APPROVED

**Purpose:** Notify requester that their request was approved

**Placeholders:**
```csharp
RequestId, FirstName, LastName, ApproverName, ApproverRole,
ApprovalDate, CurrentStatus, SimType, ServiceProvider,
ApprovalComments, ViewRequestLink, Year
```

**Subject:** `Great News! Your SIM Request Has Been Approved - #{{RequestId}}`

**Optional Placeholders:** `ApprovalComments` (uses `{{#if}}` conditional)

---

### Template 4: SIM_REQUEST_REJECTED

**Purpose:** Notify requester that their request was not approved

**Placeholders:**
```csharp
RequestId, FirstName, LastName, SimType, ServiceProvider,
ReviewerName, ReviewerRole, RejectionDate, RejectionReason,
ViewRequestLink, NewRequestLink, Year
```

**Subject:** `SIM Card Request Update - Action Required - #{{RequestId}}`

---

### Template 5: SIM_REQUEST_ICTS_NOTIFICATION

**Purpose:** Notify ICTS team that a request needs processing

**Placeholders:**
```csharp
RequestId, RequestDate, SupervisorApprovalDate, FirstName, LastName,
IndexNo, Organization, Office, Grade, FunctionalTitle, OfficialEmail,
OfficeExtension, SimType, ServiceProvider, SupervisorName, Remarks,
ProcessRequestLink, Year
```

**Subject:** `New SIM Request Awaiting ICTS Processing - #{{RequestId}}`

**Optional Placeholders:** `Remarks` (uses `{{#if}}` conditional)

---

### Template 6: SIM_READY_FOR_COLLECTION

**Purpose:** Notify requester that their SIM card is ready for pickup

**Placeholders:**
```csharp
RequestId, FirstName, LastName, ReadyDate, SimType, ServiceProvider,
PhoneNumber, CollectionPoint, ContactPerson, ContactPhone,
CollectionDeadline, ViewRequestLink, Year
```

**Subject:** `Your SIM Card is Ready for Collection! - #{{RequestId}}`

---

### Template 7: SIM_REQUEST_CANCELLED

**Purpose:** Confirm cancellation of a SIM request

**Placeholders:**
```csharp
RequestId, FirstName, LastName, RequestDate, CancellationDate,
SimType, ServiceProvider, PreviousStatus, CancelledBy,
CancellationReason, NewRequestLink, MyRequestsLink, Year
```

**Subject:** `SIM Card Request Cancelled - #{{RequestId}}`

**Optional Placeholders:** `CancellationReason` (uses `{{#if}}` conditional)

---

## Implementation

### Required Services

Inject `IEnhancedEmailService` in your page model:

```csharp
using TAB.Web.Services;

public class YourPageModel : PageModel
{
    private readonly IEnhancedEmailService _emailService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<YourPageModel> _logger;

    public YourPageModel(
        IEnhancedEmailService emailService,
        ApplicationDbContext context,
        ILogger<YourPageModel> logger)
    {
        _emailService = emailService;
        _context = context;
        _logger = logger;
    }
}
```

---

## Code Examples

### Example 1: Submitting a New SIM Request

```csharp
public async Task<IActionResult> OnPostSubmitRequestAsync()
{
    // Validate and save the request
    var simRequest = new SimRequest
    {
        // ... populate request fields
        Status = RequestStatus.PendingSupervisor,
        RequestDate = DateTime.UtcNow
    };

    _context.SimRequests.Add(simRequest);
    await _context.SaveChangesAsync();

    try
    {
        // 1. Send confirmation to requester
        await SendSubmittedConfirmation(simRequest);

        // 2. Send notification to supervisor
        await SendSupervisorNotification(simRequest);

        StatusMessage = "Request submitted successfully! You and your supervisor have been notified.";
        StatusMessageClass = "success";
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error sending emails for request {RequestId}", simRequest.Id);
        StatusMessage = "Request submitted, but there was an issue sending email notifications.";
        StatusMessageClass = "warning";
    }

    return RedirectToPage("./Index");
}

private async Task SendSubmittedConfirmation(SimRequest request)
{
    var placeholders = new Dictionary<string, string>
    {
        { "RequestId", request.Id.ToString() },
        { "RequestDate", request.RequestDate.ToString("MMMM dd, yyyy") },
        { "FirstName", request.FirstName },
        { "LastName", request.LastName },
        { "SimType", request.SimType.ToString() },
        { "ServiceProvider", request.ServiceProvider?.Name ?? "N/A" },
        { "IndexNo", request.IndexNo },
        { "Organization", request.Organization },
        { "Office", request.Office },
        { "SupervisorName", request.SupervisorName },
        { "ViewRequestLink", $"{Request.Scheme}://{Request.Host}/Modules/SimManagement/Requests/Index" },
        { "Year", DateTime.Now.Year.ToString() }
    };

    await _emailService.SendEmailFromTemplateAsync(
        templateCode: "SIM_REQUEST_SUBMITTED",
        toEmail: request.OfficialEmail,
        toName: $"{request.FirstName} {request.LastName}",
        placeholders: placeholders,
        priority: EmailPriority.Normal
    );

    _logger.LogInformation("Sent submission confirmation to {Email} for request {Id}",
        request.OfficialEmail, request.Id);
}

private async Task SendSupervisorNotification(SimRequest request)
{
    if (string.IsNullOrEmpty(request.SupervisorEmail))
    {
        _logger.LogWarning("No supervisor email for request {Id}", request.Id);
        return;
    }

    var placeholders = new Dictionary<string, string>
    {
        { "RequestId", request.Id.ToString() },
        { "RequestDate", request.RequestDate.ToString("MMMM dd, yyyy") },
        { "SimType", request.SimType.ToString() },
        { "ServiceProvider", request.ServiceProvider?.Name ?? "N/A" },
        { "FirstName", request.FirstName },
        { "LastName", request.LastName },
        { "IndexNo", request.IndexNo },
        { "Organization", request.Organization },
        { "Office", request.Office },
        { "Grade", request.Grade },
        { "FunctionalTitle", request.FunctionalTitle },
        { "OfficialEmail", request.OfficialEmail },
        { "OfficeExtension", request.OfficeExtension ?? "N/A" },
        { "SupervisorName", request.SupervisorName },
        { "SupervisorEmail", request.SupervisorEmail },
        { "ApprovalLink", $"{Request.Scheme}://{Request.Host}/Modules/SimManagement/Approvals/Supervisor?requestId={request.Id}" },
        { "Year", DateTime.Now.Year.ToString() }
    };

    await _emailService.SendEmailFromTemplateAsync(
        templateCode: "SIM_REQUEST_SUPERVISOR_NOTIFICATION",
        toEmail: request.SupervisorEmail,
        toName: request.SupervisorName,
        placeholders: placeholders,
        priority: EmailPriority.High
    );

    _logger.LogInformation("Sent supervisor notification to {Email} for request {Id}",
        request.SupervisorEmail, request.Id);
}
```

### Example 2: Supervisor Approval

```csharp
public async Task<IActionResult> OnPostApproveRequestAsync(int requestId, string comments)
{
    var request = await _context.SimRequests
        .Include(r => r.ServiceProvider)
        .FirstOrDefaultAsync(r => r.Id == requestId);

    if (request == null)
    {
        return NotFound();
    }

    // Update request status
    request.Status = RequestStatus.PendingIcts;
    request.SupervisorApprovalDate = DateTime.UtcNow;
    request.SupervisorComments = comments;

    await _context.SaveChangesAsync();

    try
    {
        var currentUser = await _userManager.GetUserAsync(User);

        // 1. Notify requester of approval
        await SendApprovalNotification(request, currentUser, comments);

        // 2. Notify ICTS team
        await SendIctsNotification(request);

        StatusMessage = $"Request approved and forwarded to ICTS. {request.FirstName} has been notified.";
        StatusMessageClass = "success";
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error sending approval emails for request {RequestId}", requestId);
        StatusMessage = "Request approved, but there was an issue sending email notifications.";
        StatusMessageClass = "warning";
    }

    return RedirectToPage();
}

private async Task SendApprovalNotification(SimRequest request, ApplicationUser approver, string comments)
{
    var placeholders = new Dictionary<string, string>
    {
        { "RequestId", request.Id.ToString() },
        { "FirstName", request.FirstName },
        { "LastName", request.LastName },
        { "ApproverName", $"{approver.FirstName} {approver.LastName}" },
        { "ApproverRole", "Supervisor" },
        { "ApprovalDate", DateTime.UtcNow.ToString("MMMM dd, yyyy") },
        { "CurrentStatus", "Pending ICTS Processing" },
        { "SimType", request.SimType.ToString() },
        { "ServiceProvider", request.ServiceProvider?.Name ?? "N/A" },
        { "ApprovalComments", comments ?? "" },
        { "ViewRequestLink", $"{Request.Scheme}://{Request.Host}/Modules/SimManagement/Requests/Index" },
        { "Year", DateTime.Now.Year.ToString() }
    };

    await _emailService.SendEmailFromTemplateAsync(
        templateCode: "SIM_REQUEST_APPROVED",
        toEmail: request.OfficialEmail,
        toName: $"{request.FirstName} {request.LastName}",
        placeholders: placeholders,
        priority: EmailPriority.Normal
    );

    _logger.LogInformation("Sent approval notification to {Email} for request {Id}",
        request.OfficialEmail, request.Id);
}

private async Task SendIctsNotification(SimRequest request)
{
    // Get ICTS team email (could be from configuration or settings)
    var ictsEmail = _configuration["Email:IctsTeamEmail"] ?? "icts@example.com";

    var placeholders = new Dictionary<string, string>
    {
        { "RequestId", request.Id.ToString() },
        { "RequestDate", request.RequestDate.ToString("MMMM dd, yyyy") },
        { "SupervisorApprovalDate", request.SupervisorApprovalDate?.ToString("MMMM dd, yyyy") ?? "N/A" },
        { "FirstName", request.FirstName },
        { "LastName", request.LastName },
        { "IndexNo", request.IndexNo },
        { "Organization", request.Organization },
        { "Office", request.Office },
        { "Grade", request.Grade },
        { "FunctionalTitle", request.FunctionalTitle },
        { "OfficialEmail", request.OfficialEmail },
        { "OfficeExtension", request.OfficeExtension ?? "N/A" },
        { "SimType", request.SimType.ToString() },
        { "ServiceProvider", request.ServiceProvider?.Name ?? "N/A" },
        { "SupervisorName", request.SupervisorName },
        { "ProcessRequestLink", $"{Request.Scheme}://{Request.Host}/Modules/SimManagement/Processing/Icts" },
        { "Year", DateTime.Now.Year.ToString() }
    };

    await _emailService.SendEmailFromTemplateAsync(
        templateCode: "SIM_REQUEST_ICTS_NOTIFICATION",
        toEmail: ictsEmail,
        toName: "ICTS Team",
        placeholders: placeholders,
        priority: EmailPriority.High
    );

    _logger.LogInformation("Sent ICTS notification for request {Id}", request.Id);
}
```

### Example 3: Request Rejection

```csharp
public async Task<IActionResult> OnPostRejectRequestAsync(int requestId, string reason)
{
    var request = await _context.SimRequests
        .Include(r => r.ServiceProvider)
        .FirstOrDefaultAsync(r => r.Id == requestId);

    if (request == null)
    {
        return NotFound();
    }

    if (string.IsNullOrWhiteSpace(reason))
    {
        StatusMessage = "Please provide a reason for rejection.";
        StatusMessageClass = "danger";
        return Page();
    }

    // Update request status
    request.Status = RequestStatus.Rejected;
    request.RejectionDate = DateTime.UtcNow;
    request.RejectionReason = reason;

    await _context.SaveChangesAsync();

    try
    {
        var currentUser = await _userManager.GetUserAsync(User);
        await SendRejectionNotification(request, currentUser, reason);

        StatusMessage = $"Request rejected. {request.FirstName} has been notified.";
        StatusMessageClass = "success";
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error sending rejection email for request {RequestId}", requestId);
        StatusMessage = "Request rejected, but there was an issue sending the notification.";
        StatusMessageClass = "warning";
    }

    return RedirectToPage();
}

private async Task SendRejectionNotification(SimRequest request, ApplicationUser reviewer, string reason)
{
    var placeholders = new Dictionary<string, string>
    {
        { "RequestId", request.Id.ToString() },
        { "FirstName", request.FirstName },
        { "LastName", request.LastName },
        { "SimType", request.SimType.ToString() },
        { "ServiceProvider", request.ServiceProvider?.Name ?? "N/A" },
        { "ReviewerName", $"{reviewer.FirstName} {reviewer.LastName}" },
        { "ReviewerRole", "Supervisor" }, // Or determine from role
        { "RejectionDate", DateTime.UtcNow.ToString("MMMM dd, yyyy") },
        { "RejectionReason", reason },
        { "ViewRequestLink", $"{Request.Scheme}://{Request.Host}/Modules/SimManagement/Requests/Index" },
        { "NewRequestLink", $"{Request.Scheme}://{Request.Host}/Modules/SimManagement/Requests/Create" },
        { "Year", DateTime.Now.Year.ToString() }
    };

    await _emailService.SendEmailFromTemplateAsync(
        templateCode: "SIM_REQUEST_REJECTED",
        toEmail: request.OfficialEmail,
        toName: $"{request.FirstName} {request.LastName}",
        placeholders: placeholders,
        priority: EmailPriority.Normal
    );

    _logger.LogInformation("Sent rejection notification to {Email} for request {Id}",
        request.OfficialEmail, request.Id);
}
```

### Example 4: SIM Ready for Collection

```csharp
public async Task<IActionResult> OnPostMarkReadyForCollectionAsync(int requestId, string phoneNumber)
{
    var request = await _context.SimRequests
        .Include(r => r.ServiceProvider)
        .FirstOrDefaultAsync(r => r.Id == requestId);

    if (request == null)
    {
        return NotFound();
    }

    // Update request status
    request.Status = RequestStatus.ReadyForCollection;
    request.PhoneNumber = phoneNumber;
    request.ReadyDate = DateTime.UtcNow;

    await _context.SaveChangesAsync();

    try
    {
        await SendReadyForCollectionNotification(request);

        StatusMessage = $"Request marked as ready. {request.FirstName} has been notified to collect their SIM.";
        StatusMessageClass = "success";
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error sending collection notification for request {RequestId}", requestId);
        StatusMessage = "Request updated, but there was an issue sending the notification.";
        StatusMessageClass = "warning";
    }

    return RedirectToPage();
}

private async Task SendReadyForCollectionNotification(SimRequest request)
{
    // Calculate collection deadline (e.g., 5 business days)
    var collectionDeadline = DateTime.UtcNow.AddDays(7).ToString("MMMM dd, yyyy");

    var placeholders = new Dictionary<string, string>
    {
        { "RequestId", request.Id.ToString() },
        { "FirstName", request.FirstName },
        { "LastName", request.LastName },
        { "ReadyDate", request.ReadyDate?.ToString("MMMM dd, yyyy") ?? DateTime.UtcNow.ToString("MMMM dd, yyyy") },
        { "SimType", request.SimType.ToString() },
        { "ServiceProvider", request.ServiceProvider?.Name ?? "N/A" },
        { "PhoneNumber", request.PhoneNumber ?? "To be assigned" },
        { "CollectionPoint", "ICTS Office, Building A, 3rd Floor" }, // From configuration
        { "ContactPerson", "John Doe" }, // From configuration
        { "ContactPhone", "+254-XXX-XXXXX" }, // From configuration
        { "CollectionDeadline", collectionDeadline },
        { "ViewRequestLink", $"{Request.Scheme}://{Request.Host}/Modules/SimManagement/Requests/Index" },
        { "Year", DateTime.Now.Year.ToString() }
    };

    await _emailService.SendEmailFromTemplateAsync(
        templateCode: "SIM_READY_FOR_COLLECTION",
        toEmail: request.OfficialEmail,
        toName: $"{request.FirstName} {request.LastName}",
        placeholders: placeholders,
        priority: EmailPriority.High
    );

    _logger.LogInformation("Sent collection notification to {Email} for request {Id}",
        request.OfficialEmail, request.Id);
}
```

### Example 5: Request Cancellation

```csharp
public async Task<IActionResult> OnPostCancelRequestAsync(int requestId, string reason)
{
    var request = await _context.SimRequests
        .Include(r => r.ServiceProvider)
        .FirstOrDefaultAsync(r => r.Id == requestId);

    if (request == null)
    {
        return NotFound();
    }

    var previousStatus = request.Status;

    // Update request status
    request.Status = RequestStatus.Cancelled;
    request.CancellationDate = DateTime.UtcNow;
    request.CancellationReason = reason;

    await _context.SaveChangesAsync();

    try
    {
        var currentUser = await _userManager.GetUserAsync(User);
        await SendCancellationNotification(request, previousStatus, currentUser);

        StatusMessage = $"Request cancelled. {request.FirstName} has been notified.";
        StatusMessageClass = "success";
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error sending cancellation email for request {RequestId}", requestId);
        StatusMessage = "Request cancelled, but there was an issue sending the notification.";
        StatusMessageClass = "warning";
    }

    return RedirectToPage();
}

private async Task SendCancellationNotification(SimRequest request, RequestStatus previousStatus, ApplicationUser cancelledBy)
{
    var placeholders = new Dictionary<string, string>
    {
        { "RequestId", request.Id.ToString() },
        { "FirstName", request.FirstName },
        { "LastName", request.LastName },
        { "RequestDate", request.RequestDate.ToString("MMMM dd, yyyy") },
        { "CancellationDate", DateTime.UtcNow.ToString("MMMM dd, yyyy") },
        { "SimType", request.SimType.ToString() },
        { "ServiceProvider", request.ServiceProvider?.Name ?? "N/A" },
        { "PreviousStatus", previousStatus.ToString() },
        { "CancelledBy", $"{cancelledBy.FirstName} {cancelledBy.LastName}" },
        { "CancellationReason", request.CancellationReason ?? "" },
        { "NewRequestLink", $"{Request.Scheme}://{Request.Host}/Modules/SimManagement/Requests/Create" },
        { "MyRequestsLink", $"{Request.Scheme}://{Request.Host}/Modules/SimManagement/Requests/Index" },
        { "Year", DateTime.Now.Year.ToString() }
    };

    await _emailService.SendEmailFromTemplateAsync(
        templateCode: "SIM_REQUEST_CANCELLED",
        toEmail: request.OfficialEmail,
        toName: $"{request.FirstName} {request.LastName}",
        placeholders: placeholders,
        priority: EmailPriority.Normal
    );

    _logger.LogInformation("Sent cancellation notification to {Email} for request {Id}",
        request.OfficialEmail, request.Id);
}
```

---

## Testing

### Test Each Template

1. Navigate to `/Admin/SendEmail`
2. Select the template from dropdown
3. Fill in test placeholder values
4. Send to your test email
5. Verify:
   - Subject line renders correctly
   - All placeholders are replaced
   - Layout looks correct on desktop and mobile
   - Links work correctly

### Preview Templates

Navigate to `/Admin/EmailTemplatePreview?code={TEMPLATE_CODE}` to preview each template with sample data.

### Check Email Logs

Monitor `/Admin/EmailLogs` to:
- Verify emails are being sent
- Check delivery status
- Troubleshoot failures

---

## Troubleshooting

### Common Issues

**Problem:** Template not found
```
Solution: Verify template exists in database
SELECT * FROM EmailTemplates WHERE TemplateCode = 'SIM_REQUEST_SUBMITTED'
```

**Problem:** Placeholders not replacing
```
Solution: Check placeholder names match exactly (case-sensitive)
- Correct: {{ "FirstName" }}
- Wrong: {{ "firstname" }} or {{ "first_name" }}
```

**Problem:** Email not sending
```
Solution:
1. Check SMTP configuration at /Admin/EmailConfiguration
2. Test SMTP connection
3. Check Email Logs for error messages
4. Verify recipient email address is valid
```

**Problem:** Conditional content not working
```
Solution: Ensure IEnhancedEmailService is being used (not IEmailService)
- The enhanced service supports {{#if}} syntax
- Check that optional placeholders are in the dictionary (even if empty)
```

### Logging

Add comprehensive logging to track email sending:

```csharp
try
{
    _logger.LogInformation("Preparing to send email using template {TemplateCode} to {Email}",
        "SIM_REQUEST_SUBMITTED", request.OfficialEmail);

    await _emailService.SendEmailFromTemplateAsync(...);

    _logger.LogInformation("Successfully sent email to {Email}", request.OfficialEmail);
}
catch (Exception ex)
{
    _logger.LogError(ex, "Failed to send email to {Email} using template {TemplateCode}",
        request.OfficialEmail, "SIM_REQUEST_SUBMITTED");
}
```

---

## Configuration

### Collection Point Settings

Store collection point details in `appsettings.json`:

```json
{
  "SimManagement": {
    "CollectionPoint": "ICTS Office, Building A, 3rd Floor",
    "ContactPerson": "John Doe",
    "ContactPhone": "+254-XXX-XXXXX",
    "CollectionDeadlineDays": 5,
    "IctsTeamEmail": "icts@example.com"
  }
}
```

Access in code:

```csharp
var collectionPoint = _configuration["SimManagement:CollectionPoint"];
var ictsEmail = _configuration["SimManagement:IctsTeamEmail"];
```

---

## Best Practices

1. **Always use try-catch blocks** when sending emails
2. **Log all email operations** for troubleshooting
3. **Don't fail the main operation** if email fails
4. **Provide meaningful status messages** to users
5. **Test templates thoroughly** before production use
6. **Keep placeholder values consistent** across templates
7. **Use EmailPriority appropriately**:
   - High: Supervisor notifications, urgent actions
   - Normal: Confirmations, status updates
   - Low: Informational emails

---

## Summary

You now have a complete email notification system for your SIM request workflow. All templates are professional, mobile-responsive, and include comprehensive information for recipients.

### Files Created

- **6 HTML Template Files**: Individual template designs
- **INSERT_ALL_SIM_EMAIL_TEMPLATES.sql**: Complete database setup script
- **SIM_REQUEST_EMAIL_IMPLEMENTATION_GUIDE.md**: This implementation guide

### Next Steps

1. ✅ Install templates using SQL script
2. ✅ Test each template using the admin panel
3. ✅ Implement email sending in your workflow code
4. ✅ Configure collection point settings
5. ✅ Monitor email logs for any issues

**Happy Coding! 🚀**
