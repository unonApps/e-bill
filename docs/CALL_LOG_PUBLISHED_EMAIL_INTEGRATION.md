# Call Log Published Email Notification Integration Guide

## Overview
This guide explains how to integrate the "Call Log Published Notification" email template into the Push to Production workflow.

## Template Information

**Template Name:** Call Log Published Notification
**Template Code:** `CALL_LOG_PUBLISHED`
**Category:** CallLog
**Purpose:** Notify staff members when their call records are pushed to production and ready for verification

## Files Created

1. **CallLogPublishedNotificationTemplate.html** - HTML email template
2. **INSERT_CALL_LOG_PUBLISHED_TEMPLATE.sql** - SQL script to insert template into database
3. **CALL_LOG_PUBLISHED_EMAIL_INTEGRATION.md** - This documentation file

## Installation Steps

### Step 1: Insert the Template into Database

Run the SQL script in SQL Server Management Studio or Azure Data Studio:

```sql
-- Execute this file
INSERT_CALL_LOG_PUBLISHED_TEMPLATE.sql
```

This will create the email template in the `EmailTemplates` table.

### Step 2: Verify Template Installation

```sql
SELECT * FROM EmailTemplates WHERE TemplateCode = 'CALL_LOG_PUBLISHED';
```

You should see the template with all placeholders ready to use.

## Integration with Push to Production

### Modify CallLogStagingService.cs

Add email sending logic in the `PushToProductionAsync` method after records are successfully pushed.

**Location:** `Services/CallLogStagingService.cs`
**Method:** `PushToProductionAsync` (around line 717)

### Code Changes Required

Add this code after line 828 (after updating source tables):

```csharp
// Send email notifications to staff members
await SendPublishedNotificationsAsync(verifiedLogs, batch, verificationPeriod, approvalPeriod);
```

### Add the Email Notification Method

Add this new method to `CallLogStagingService.cs`:

```csharp
private async Task SendPublishedNotificationsAsync(
    List<CallLogStaging> records,
    StagingBatch batch,
    DateTime? verificationPeriod,
    DateTime? approvalPeriod)
{
    try
    {
        // Group records by responsible staff member
        var recordsByStaff = records
            .GroupBy(r => r.ResponsibleIndexNumber)
            .ToList();

        foreach (var staffGroup in recordsByStaff)
        {
            var indexNumber = staffGroup.Key;
            var staffRecords = staffGroup.ToList();

            // Get staff information
            var staff = await _context.EbillUsers
                .Include(e => e.OrganizationEntity)
                .FirstOrDefaultAsync(e => e.IndexNumber == indexNumber);

            if (staff == null || string.IsNullOrEmpty(staff.Email))
            {
                _logger.LogWarning($"Cannot send email to staff {indexNumber} - user not found or no email");
                continue;
            }

            // Get class of service for monthly allowance
            decimal monthlyAllowance = 0;
            var userPhone = await _context.UserPhones
                .Include(up => up.ClassOfService)
                .FirstOrDefaultAsync(up => up.IndexNumber == indexNumber && up.IsPrimary);

            if (userPhone?.ClassOfService != null)
            {
                monthlyAllowance = userPhone.ClassOfService.MonthlyAllowance ?? 0;
            }

            // Calculate totals
            var totalRecords = staffRecords.Count;
            var totalAmount = staffRecords.Sum(r => r.CallCostUSD);
            var sourceSystems = string.Join(", ", staffRecords.Select(r => r.SourceSystem).Distinct());

            // Calculate allowance usage
            var allowancePercentage = monthlyAllowance > 0 ? (totalAmount / monthlyAllowance) * 100 : 0;
            var allowanceUsageMessage = allowancePercentage > 100
                ? $"You have exceeded your allowance by {allowancePercentage - 100:F0}%"
                : $"You have used {allowancePercentage:F0}% of your monthly allowance";

            // Get period name
            var period = batch.BillingPeriod != null
                ? $"{batch.BillingPeriod.PeriodName}"
                : $"{DateTime.Now:MMMM yyyy}";

            // Format deadlines
            var verificationDeadlineText = verificationPeriod.HasValue
                ? verificationPeriod.Value.ToString("MMMM dd, yyyy")
                : "To be confirmed";

            var approvalDeadlineText = approvalPeriod.HasValue
                ? approvalPeriod.Value.ToString("MMMM dd, yyyy")
                : "To be confirmed";

            // Build verification link
            var baseUrl = _configuration["AppSettings:BaseUrl"] ?? "http://localhost:5041";
            var verifyLink = $"{baseUrl}/Modules/EBillManagement/CallRecords/MyCallLogs";

            // Prepare email data
            var emailData = new Dictionary<string, string>
            {
                { "StaffName", staff.FullName },
                { "IndexNumber", indexNumber },
                { "Period", period },
                { "TotalRecords", totalRecords.ToString("N0") },
                { "TotalAmount", totalAmount.ToString("N2") },
                { "SourceSystems", sourceSystems },
                { "VerificationDeadline", verificationDeadlineText },
                { "ApprovalDeadline", approvalDeadlineText },
                { "MonthlyAllowance", monthlyAllowance.ToString("N2") },
                { "AllowancePercentage", Math.Min(allowancePercentage, 100).ToString("F0") },
                { "AllowanceUsageMessage", allowanceUsageMessage },
                { "VerifyCallsLink", verifyLink },
                { "SupervisorName", staff.SupervisorName ?? "Not Assigned" },
                { "SupervisorEmail", staff.SupervisorEmail ?? "" },
                { "Year", DateTime.Now.Year.ToString() }
            };

            // Send email using the template service
            await _emailService.SendEmailFromTemplateAsync(
                templateCode: "CALL_LOG_PUBLISHED",
                recipientEmail: staff.Email,
                recipientName: staff.FullName,
                placeholderValues: emailData
            );

            _logger.LogInformation($"Sent call log published notification to {staff.Email} ({indexNumber}) - {totalRecords} records");
        }

        _logger.LogInformation($"Sent {recordsByStaff.Count} call log published notifications");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error sending call log published notifications");
        // Don't throw - email failures shouldn't stop the publish process
    }
}
```

### Update Constructor

Make sure the `CallLogStagingService` constructor has the email service and configuration:

```csharp
private readonly IEnhancedEmailService _emailService;
private readonly IConfiguration _configuration;

public CallLogStagingService(
    ApplicationDbContext context,
    ILogger<CallLogStagingService> logger,
    IEnhancedEmailService emailService,
    IConfiguration configuration)
{
    _context = context;
    _logger = logger;
    _emailService = emailService;
    _configuration = configuration;
}
```

## Email Template Placeholders

The template uses the following placeholders:

| Placeholder | Description | Example |
|------------|-------------|---------|
| `{{StaffName}}` | Staff member's full name | "John Doe" |
| `{{IndexNumber}}` | Staff member's index number | "10283784" |
| `{{Period}}` | Billing period | "September 2024" |
| `{{TotalRecords}}` | Total number of call records | "156" |
| `{{TotalAmount}}` | Total cost of all calls | "234.56" |
| `{{SourceSystems}}` | Source systems | "Safaricom, Airtel, PSTN" |
| `{{VerificationDeadline}}` | Staff verification deadline | "October 15, 2024" |
| `{{ApprovalDeadline}}` | Supervisor approval deadline | "October 20, 2024" |
| `{{MonthlyAllowance}}` | Monthly call allowance | "200.00" |
| `{{AllowancePercentage}}` | Percentage used (0-100) | "75" |
| `{{AllowanceUsageMessage}}` | Usage message | "You have used 75% of your monthly allowance" |
| `{{VerifyCallsLink}}` | Link to verification page | "http://localhost:5041/Modules/..." |
| `{{SupervisorName}}` | Supervisor's full name | "Jane Smith" |
| `{{SupervisorEmail}}` | Supervisor's email | "jane.smith@un.org" |
| `{{Year}}` | Current year | "2024" |

## Email Content Features

The email template includes:

1. **Professional Header** - UN branding with logo
2. **Action Required Notice** - Highlighted deadline warning
3. **Call Records Summary** - Detailed breakdown of records and costs
4. **Important Deadlines** - Verification and approval deadlines
5. **Step-by-Step Instructions** - What staff members need to do
6. **Monthly Allowance Tracker** - Visual progress bar showing usage
7. **Call-to-Action Button** - Direct link to verify calls
8. **Important Reminders** - Policy information
9. **Supervisor Information** - Contact details
10. **Help Section** - Support contact information
11. **Professional Footer** - UN SDG colors and branding

## Testing

### Test the Email Template

1. **Preview in Email Template Manager**:
   - Go to: http://localhost:5041/Admin/EmailTemplates
   - Find "Call Log Published Notification"
   - Click "Preview" to see how it looks with sample data

2. **Send Test Email**:
   ```sql
   -- Get a sample batch and user for testing
   SELECT TOP 1 * FROM StagingBatches WHERE BatchStatus = 2; -- Verified
   SELECT TOP 1 * FROM EbillUsers WHERE Email IS NOT NULL;
   ```

3. **Test Push to Production**:
   - Go to: http://localhost:5041/Admin/CallLogStaging
   - Select a verified batch
   - Click "Push to Production"
   - Set deadlines
   - Confirm - emails should be sent to all affected staff

## Troubleshooting

### Emails Not Sending

1. **Check SMTP Configuration**:
   ```sql
   SELECT * FROM EmailConfiguration WHERE IsActive = 1;
   ```

2. **Check Email Logs**:
   ```sql
   SELECT TOP 10 * FROM EmailLogs
   WHERE TemplateCode = 'CALL_LOG_PUBLISHED'
   ORDER BY SentDate DESC;
   ```

3. **Check Application Logs**:
   Look for entries like:
   - "Sent call log published notification to..."
   - "Error sending call log published notifications"

### Common Issues

| Issue | Solution |
|-------|----------|
| Template not found | Run the INSERT SQL script |
| Missing placeholders | Check EmailLog.ErrorMessage for details |
| No staff email address | Update EbillUsers with email addresses |
| Wrong base URL | Update AppSettings:BaseUrl in appsettings.json |

## Configuration

Add this to `appsettings.json` if not already present:

```json
{
  "AppSettings": {
    "BaseUrl": "http://localhost:5041"
  }
}
```

For production, update to your actual domain:

```json
{
  "AppSettings": {
    "BaseUrl": "https://ebilling.un.org"
  }
}
```

## Future Enhancements

Consider adding:

1. **Batch Summary Email** - Send to admins with overall statistics
2. **Reminder Emails** - Auto-send reminders before deadline
3. **Deadline Extension Notification** - If admin extends deadlines
4. **Completion Confirmation** - When all staff have verified

## Support

For questions or issues:
- Check EmailLogs table for delivery status
- Review application logs for errors
- Contact ICTS Service Desk: ICTS.Servicedesk@un.org
