# SIM Request Supervisor Notification Email Template

## Template Details

**Template Name:** SIM Request - Supervisor Approval Notification
**Template Code:** `SIM_REQUEST_SUPERVISOR_NOTIFICATION`
**Category:** SIM Management
**Purpose:** Notify supervisors when a new SIM card request requires their approval

---

## Available Placeholders

### Request Information
- `{{RequestId}}` - Unique request ID number
- `{{RequestDate}}` - Date when request was submitted (formatted)
- `{{SimType}}` - Type of SIM (Physical SIM or eSIM)
- `{{ServiceProvider}}` - Name of the service provider
- `{{Remarks}}` - Additional remarks from requester (optional)

### Requester Details
- `{{FirstName}}` - Requester's first name
- `{{LastName}}` - Requester's last name
- `{{IndexNo}}` - Employee index number
- `{{Organization}}` - Organization name
- `{{Office}}` - Office location
- `{{Grade}}` - Employee grade
- `{{FunctionalTitle}}` - Job title
- `{{OfficialEmail}}` - Official email address
- `{{OfficeExtension}}` - Office phone extension (optional)

### Supervisor Information
- `{{SupervisorName}}` - Supervisor's full name
- `{{SupervisorEmail}}` - Supervisor's email address

### System Links
- `{{ApprovalLink}}` - Direct link to review and approve the request
- `{{Year}}` - Current year for copyright footer

---

## SQL Script to Add Template to Database

```sql
-- Insert SIM Request Supervisor Notification Template
INSERT INTO EmailTemplates
(
    Name,
    TemplateCode,
    Subject,
    HtmlBody,
    Description,
    AvailablePlaceholders,
    Category,
    IsActive,
    IsSystemTemplate,
    CreatedDate
)
VALUES
(
    'SIM Request - Supervisor Approval Notification',
    'SIM_REQUEST_SUPERVISOR_NOTIFICATION',
    'New SIM Card Request Requires Your Approval - {{FirstName}} {{LastName}}',
    -- HtmlBody: Copy the entire content from SimRequestSupervisorNotificationTemplate.html
    '<Paste HTML content here>',
    'Email notification sent to supervisors when a new SIM card request is submitted and requires their approval. Includes requester details, request information, and a direct link to review the request.',
    'RequestId, RequestDate, SimType, ServiceProvider, Remarks, FirstName, LastName, IndexNo, Organization, Office, Grade, FunctionalTitle, OfficialEmail, OfficeExtension, SupervisorName, SupervisorEmail, ApprovalLink, Year',
    'SIM Management',
    1, -- IsActive
    1, -- IsSystemTemplate (set to 0 if you want it to be editable/deletable)
    GETUTCDATE()
);
```

---

## How to Use This Template in Code

### 1. When Submitting a SIM Request to Supervisor

In your `Create.cshtml.cs` or wherever you handle the submission:

```csharp
using TAB.Web.Services;

public class CreateModel : PageModel
{
    private readonly IEnhancedEmailService _emailService;
    private readonly ApplicationDbContext _context;

    public async Task<IActionResult> OnPostSubmitToSupervisorAsync()
    {
        // ... your submission logic ...

        // Get the supervisor's email
        var supervisorEmail = simRequest.SupervisorEmail;

        if (!string.IsNullOrEmpty(supervisorEmail))
        {
            // Prepare placeholders
            var placeholders = new Dictionary<string, string>
            {
                { "RequestId", simRequest.Id.ToString() },
                { "RequestDate", simRequest.RequestDate.ToString("MMMM dd, yyyy") },
                { "SimType", simRequest.SimType.ToString() },
                { "ServiceProvider", simRequest.ServiceProvider?.Name ?? "N/A" },
                { "Remarks", simRequest.Remarks ?? "" },
                { "FirstName", simRequest.FirstName },
                { "LastName", simRequest.LastName },
                { "IndexNo", simRequest.IndexNo },
                { "Organization", simRequest.Organization },
                { "Office", simRequest.Office },
                { "Grade", simRequest.Grade },
                { "FunctionalTitle", simRequest.FunctionalTitle },
                { "OfficialEmail", simRequest.OfficialEmail },
                { "OfficeExtension", simRequest.OfficeExtension ?? "N/A" },
                { "SupervisorName", simRequest.SupervisorName ?? simRequest.Supervisor },
                { "SupervisorEmail", supervisorEmail },
                { "ApprovalLink", $"{Request.Scheme}://{Request.Host}/Modules/SimManagement/Approvals/Supervisor?requestId={simRequest.Id}" },
                { "Year", DateTime.Now.Year.ToString() }
            };

            // Send email using template
            await _emailService.SendEmailFromTemplateAsync(
                templateCode: "SIM_REQUEST_SUPERVISOR_NOTIFICATION",
                toEmail: supervisorEmail,
                toName: simRequest.SupervisorName ?? simRequest.Supervisor,
                placeholders: placeholders,
                priority: EmailPriority.High
            );

            _logger.LogInformation(
                "Supervisor notification email sent to {Email} for SIM request {RequestId}",
                supervisorEmail, simRequest.Id
            );
        }

        return RedirectToPage("./Index");
    }
}
```

### 2. Example with Error Handling

```csharp
try
{
    var emailSent = await _emailService.SendEmailFromTemplateAsync(
        templateCode: "SIM_REQUEST_SUPERVISOR_NOTIFICATION",
        toEmail: supervisorEmail,
        toName: supervisorName,
        placeholders: placeholders,
        priority: EmailPriority.High
    );

    if (emailSent)
    {
        _logger.LogInformation("Supervisor notification sent successfully for request {Id}", simRequest.Id);
        StatusMessage = "Request submitted successfully. Supervisor has been notified via email.";
        StatusMessageClass = "success";
    }
    else
    {
        _logger.LogWarning("Failed to send supervisor notification for request {Id}", simRequest.Id);
        StatusMessage = "Request submitted, but email notification failed. Please inform your supervisor manually.";
        StatusMessageClass = "warning";
    }
}
catch (Exception ex)
{
    _logger.LogError(ex, "Error sending supervisor notification email for request {Id}", simRequest.Id);
    // Don't fail the request submission if email fails
    StatusMessage = "Request submitted successfully, but there was an issue sending the notification email.";
    StatusMessageClass = "warning";
}
```

---

## Template Features

✅ **Professional Design**: Modern, responsive HTML email template
✅ **Mobile Friendly**: Optimized for all devices
✅ **Clear Call-to-Action**: Prominent "Review & Approve Request" button
✅ **Complete Information**: All relevant request and requester details
✅ **Conditional Content**: Shows remarks only if provided
✅ **Visual Hierarchy**: Color-coded sections for easy reading
✅ **Urgency Indicators**: Important notices highlighted
✅ **Branded Footer**: Professional system branding

---

## Testing the Template

1. **Preview**: Use `/Admin/EmailTemplatePreview?id={templateId}` to preview
2. **Test Send**: Use `/Admin/SendEmail` to send a test email
3. **Check Logs**: Monitor `/Admin/EmailLogs` for delivery status

---

## Customization Notes

- The template uses Handlebars-style placeholders: `{{PlaceholderName}}`
- Conditional content uses: `{{#if FieldName}}...{{/if}}`
- Inline styles ensure compatibility with all email clients
- Color scheme matches the TAB system branding (#009edb)
- All fonts are web-safe for maximum compatibility

---

## Approval Link Format

The approval link should point to the supervisor approval page:

```
https://yourdomain.com/Modules/SimManagement/Approvals/Supervisor?requestId={Id}
```

Make sure this page exists and is accessible to supervisors.

---

## Related Templates You May Need

Consider creating these related templates:

1. **SIM_REQUEST_APPROVED** - Notify requester when approved
2. **SIM_REQUEST_REJECTED** - Notify requester when rejected
3. **SIM_REQUEST_SUBMITTED** - Confirmation to requester
4. **SIM_READY_FOR_COLLECTION** - Notify when SIM is ready
5. **SIM_REQUEST_ICTS_NOTIFICATION** - Notify ICTS team

---

## Support

For issues or questions about this template:
- Check email logs at `/Admin/EmailLogs`
- View template at `/Admin/EmailTemplates`
- Edit template at `/Admin/EmailTemplateEdit?id={templateId}`
