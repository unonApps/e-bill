# 📧 SIM Request Supervisor Email Template - Setup Complete

## ✅ Files Created

1. **SimRequestSupervisorNotificationTemplate.html** - The HTML email template
2. **SIM_REQUEST_EMAIL_TEMPLATE_GUIDE.md** - Complete documentation
3. **INSERT_SIM_REQUEST_EMAIL_TEMPLATE.sql** - Ready-to-execute SQL script
4. **README_SIM_EMAIL_TEMPLATE.md** - This file

---

## 🚀 Quick Setup (3 Steps)

### Step 1: Add Template to Database

Execute the SQL script in SQL Server Management Studio:

```bash
File: INSERT_SIM_REQUEST_EMAIL_TEMPLATE.sql
```

**OR** run via command line:
```bash
sqlcmd -S MICHUKI\SQLEXPRESS -d TABDB -E -i INSERT_SIM_REQUEST_EMAIL_TEMPLATE.sql
```

### Step 2: Verify Template in Admin Panel

1. Navigate to: `http://localhost:5041/Admin/EmailTemplates`
2. Look for: **SIM Request - Supervisor Approval Notification**
3. Template Code: `SIM_REQUEST_SUPERVISOR_NOTIFICATION`
4. Preview the template to ensure it looks correct

### Step 3: Add Email Sending Code

Add this code when submitting SIM request to supervisor (in `Create.cshtml.cs` or similar):

```csharp
// After successfully submitting request to supervisor
if (!string.IsNullOrEmpty(simRequest.SupervisorEmail))
{
    var placeholders = new Dictionary<string, string>
    {
        { "RequestId", simRequest.Id.ToString() },
        { "RequestDate", simRequest.RequestDate.ToString("MMMM dd, yyyy") },
        { "SimType", simRequest.SimType.ToString() },
        { "ServiceProvider", simRequest.ServiceProvider?.Name ?? "N/A" },
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
        { "SupervisorEmail", simRequest.SupervisorEmail },
        { "ApprovalLink", $"{Request.Scheme}://{Request.Host}/Modules/SimManagement/Approvals/Supervisor?requestId={simRequest.Id}" },
        { "Year", DateTime.Now.Year.ToString() }
    };

    await _emailService.SendEmailFromTemplateAsync(
        templateCode: "SIM_REQUEST_SUPERVISOR_NOTIFICATION",
        toEmail: simRequest.SupervisorEmail,
        toName: simRequest.SupervisorName,
        placeholders: placeholders
    );
}
```

---

## 📋 Template Information

**Template Name:** SIM Request - Supervisor Approval Notification
**Template Code:** `SIM_REQUEST_SUPERVISOR_NOTIFICATION`
**Category:** SIM Management
**Status:** Active
**System Template:** Yes (protected from deletion)

---

## 🎨 Template Features

✅ **Professional Design** - Modern, responsive HTML email
✅ **Mobile Friendly** - Optimized for all devices
✅ **Clear CTA Button** - "Review & Approve Request"
✅ **Complete Info** - All requester and request details
✅ **Color-Coded Sections** - Easy to scan
✅ **Urgency Notice** - Highlighted important message
✅ **Branded Footer** - TAB System branding

---

## 📦 Available Placeholders

### Core Fields (Required)
- `{{RequestId}}` - Request ID number
- `{{RequestDate}}` - Submission date
- `{{FirstName}}`, `{{LastName}}` - Requester name
- `{{SupervisorName}}` - Supervisor name
- `{{ApprovalLink}}` - Link to approval page

### Requester Details
- `{{IndexNo}}`, `{{Organization}}`, `{{Office}}`
- `{{Grade}}`, `{{FunctionalTitle}}`
- `{{OfficialEmail}}`, `{{OfficeExtension}}`

### Request Details
- `{{SimType}}` - Physical SIM or eSIM
- `{{ServiceProvider}}` - Provider name
- `{{Remarks}}` - Optional remarks (conditional)

### System
- `{{Year}}` - Current year

---

## 🧪 Testing

### 1. Preview Template
Navigate to: `/Admin/EmailTemplatePreview?code=SIM_REQUEST_SUPERVISOR_NOTIFICATION`

### 2. Send Test Email
Navigate to: `/Admin/SendEmail`
- Select template: SIM_REQUEST_SUPERVISOR_NOTIFICATION
- Enter test email address
- Fill in placeholder values

### 3. Check Email Logs
Navigate to: `/Admin/EmailLogs`
- Monitor email delivery status
- Check for errors
- View sent emails

---

## 🎯 Email Subject Line

```
New SIM Card Request Requires Your Approval - {{FirstName}} {{LastName}}
```

Example:
```
New SIM Card Request Requires Your Approval - John Doe
```

---

## 📝 Next Steps (Optional)

Consider creating these related templates:

1. **SIM_REQUEST_SUBMITTED** - Confirmation to requester
2. **SIM_REQUEST_APPROVED** - Approval notification to requester
3. **SIM_REQUEST_REJECTED** - Rejection notification with reason
4. **SIM_READY_FOR_COLLECTION** - SIM ready notification
5. **SIM_REQUEST_ICTS_NOTIFICATION** - Notification to ICTS team

---

## 🔧 Troubleshooting

### Email not sending?
1. Check Email Configuration at `/Admin/EmailConfiguration`
2. Verify SMTP settings are correct
3. Check Email Logs for error messages

### Template not found?
1. Verify template exists: `SELECT * FROM EmailTemplates WHERE TemplateCode = 'SIM_REQUEST_SUPERVISOR_NOTIFICATION'`
2. Check template is Active: `IsActive = 1`
3. Verify exact template code spelling

### Placeholders not replacing?
1. Ensure placeholder names match exactly (case-sensitive)
2. Check dictionary keys match placeholder names
3. Verify `IEnhancedEmailService` is being used

---

## 📚 Documentation

For complete documentation, see:
- **SIM_REQUEST_EMAIL_TEMPLATE_GUIDE.md** - Full guide with code examples
- **SimRequestSupervisorNotificationTemplate.html** - Raw HTML template

---

## ✉️ Support

If you encounter any issues:
1. Check `/Admin/EmailLogs` for error details
2. Review Email Configuration settings
3. Verify all required placeholders are provided
4. Test with a simple email first

---

## 🎉 You're All Set!

The SIM Request Supervisor Notification email template is ready to use. Execute the SQL script and add the sending code to start sending professional email notifications to supervisors!

**Happy Coding! 🚀**
