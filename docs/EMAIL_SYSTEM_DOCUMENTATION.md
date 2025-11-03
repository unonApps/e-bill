# Comprehensive Email Service Integration - Documentation

## Overview

This document provides complete documentation for the integrated email management system in your TAB application. The system includes email configuration, template management, email sending, logging, and tracking capabilities.

## System Architecture

### Components

1. **Email Configuration** - Database-stored SMTP settings
2. **Email Templates** - Reusable email templates with placeholder support
3. **Email Service** - Core service for sending emails
4. **Email Logging** - Comprehensive email tracking and history
5. **Email Queue** - Support for scheduled and batch email sending
6. **Admin Interfaces** - Web-based management pages

### Database Models

#### EmailConfiguration
- Stores SMTP server settings
- Supports multiple configurations (only one active at a time)
- Fields: Server, Port, Credentials, SSL settings, Timeout

#### EmailTemplate
- Reusable email templates with dynamic placeholders
- Support for both HTML and plain text versions
- System templates (protected) and custom templates
- Fields: Name, Code, Subject, Body, Category, Placeholders

#### EmailLog
- Complete history of all emails sent
- Tracks status, retries, and errors
- Support for CC, BCC, and attachments
- Fields: To/From, Subject, Body, Status, Send Date, Error Messages

#### EmailAttachment
- Tracks email attachments
- Stores file metadata and paths

## Features

### 1. Email Configuration Management

**Location:** `/Admin/EmailConfiguration`

**Features:**
- Configure SMTP server settings
- Store credentials securely in database
- Test email functionality
- Support for SSL/TLS
- Configurable timeout settings
- Multiple configuration support (one active)

**Supported Email Providers:**
- Gmail (smtp.gmail.com:587)
- Microsoft 365/Outlook (smtp.office365.com:587)
- SendGrid
- Mailgun
- Any SMTP-compliant server

### 2. Email Template Management

**Location:** `/Admin/EmailTemplates`

**Features:**
- Create and edit email templates
- Support for HTML and plain text versions
- Dynamic placeholder system ({{PlaceholderName}})
- Template categories for organization
- System templates (protected from deletion)
- Active/Inactive status control
- Template preview functionality

**Built-in System Templates:**
1. **WELCOME_EMAIL** - New user welcome email
2. **PASSWORD_RESET** - Password reset requests
3. **NOTIFICATION** - General notifications

**Placeholder System:**
- Use `{{PlaceholderName}}` syntax in templates
- Placeholders are replaced with actual values when sending
- Common placeholders: FullName, Email, InitialPassword, Date, etc.

### 3. Email Sending Interface

**Location:** `/Admin/SendEmail`

**Features:**
- **Compose Mode** - Send custom HTML emails
- **Template Mode** - Send emails using templates
- Support for CC and BCC
- Queue emails for later sending
- Dynamic placeholder input for templates

### 4. Email History & Logs

**Location:** `/Admin/EmailLogs`

**Features:**
- View all sent, failed, pending, and queued emails
- Filter by status, email address, and date range
- Real-time statistics dashboard
- View email content
- Retry failed emails
- Pagination support

**Email Statistics:**
- Total Sent
- Total Failed
- Total Pending
- Total Queued
- Open tracking (if implemented)

### 5. Email Queue System

**Features:**
- Queue emails for scheduled sending
- Priority-based email sending
- Automatic retry for failed emails (configurable max retries)
- Background processing support

## How to Use

### Initial Setup

1. **Apply Database Migration:**
   ```bash
   dotnet ef database update
   ```

2. **Seed Default Templates (Optional):**
   ```csharp
   // In your Program.cs or startup code
   using (var scope = app.Services.CreateScope())
   {
       var seeder = scope.ServiceProvider.GetRequiredService<EmailTemplateSeeder>();
       await seeder.SeedDefaultTemplatesAsync();
   }
   ```

3. **Configure SMTP Settings:**
   - Navigate to `/Admin/EmailConfiguration`
   - Enter your SMTP server details
   - Test the configuration using the "Send Test Email" feature

### Sending Emails Programmatically

#### Using the Enhanced Email Service

```csharp
public class YourController
{
    private readonly IEnhancedEmailService _emailService;

    public YourController(IEnhancedEmailService emailService)
    {
        _emailService = emailService;
    }

    // Send a simple email
    public async Task SendSimpleEmail()
    {
        await _emailService.SendEmailAsync(
            to: "user@example.com",
            subject: "Hello",
            htmlMessage: "<h1>Hello World</h1>",
            plainTextMessage: "Hello World"
        );
    }

    // Send using a template
    public async Task SendTemplatedEmail()
    {
        var data = new Dictionary<string, string>
        {
            { "FullName", "John Doe" },
            { "Email", "john@example.com" },
            { "InitialPassword", "TempPass123" }
        };

        await _emailService.SendTemplatedEmailAsync(
            to: "john@example.com",
            templateCode: "WELCOME_EMAIL",
            data: data
        );
    }

    // Queue an email for later sending
    public async Task QueueEmail()
    {
        await _emailService.QueueEmailAsync(
            to: "user@example.com",
            subject: "Scheduled Email",
            htmlMessage: "<p>This will be sent later</p>",
            scheduledSendDate: DateTime.UtcNow.AddHours(1),
            priority: 5
        );
    }
}
```

#### Using the Template Service

```csharp
public class YourController
{
    private readonly IEmailTemplateService _templateService;

    public YourController(IEmailTemplateService templateService)
    {
        _templateService = templateService;
    }

    // Render a template
    public async Task<string> RenderTemplate()
    {
        var data = new Dictionary<string, string>
        {
            { "FullName", "John Doe" },
            { "Email", "john@example.com" }
        };

        var (subject, htmlBody, plainTextBody) =
            await _templateService.RenderTemplateAsync("WELCOME_EMAIL", data);

        return htmlBody;
    }

    // Get all active templates
    public async Task<List<EmailTemplate>> GetTemplates()
    {
        return await _templateService.GetActiveTemplatesAsync();
    }
}
```

### Creating Custom Templates

1. **Navigate to `/Admin/EmailTemplates`**
2. **Click "Create Template"**
3. **Fill in template details:**
   - Name: Display name for the template
   - Template Code: Unique identifier (e.g., INVOICE_EMAIL)
   - Category: For organization
   - Subject: Email subject (can use placeholders)
   - HTML Body: HTML email content
   - Plain Text Body: Optional plain text version
   - Available Placeholders: List of placeholders (comma-separated)

4. **Use placeholders in your template:**
   ```html
   <h1>Hello {{FullName}}!</h1>
   <p>Your email is {{Email}}</p>
   <p>Invoice amount: {{Amount}}</p>
   ```

### Managing Email Logs

1. **Navigate to `/Admin/EmailLogs`**
2. **Filter emails by:**
   - Recipient email address
   - Status (Sent, Failed, Pending, Queued)
   - Date range

3. **Actions:**
   - View email details
   - Retry failed emails
   - Export logs (if implemented)

## API Reference

### IEnhancedEmailService

```csharp
public interface IEnhancedEmailService
{
    // Send an email immediately
    Task<bool> SendEmailAsync(
        string to,
        string subject,
        string htmlMessage,
        string? plainTextMessage = null,
        string? cc = null,
        string? bcc = null,
        List<string>? attachmentPaths = null,
        string? createdBy = null,
        string? relatedEntityType = null,
        string? relatedEntityId = null);

    // Send using a template
    Task<bool> SendTemplatedEmailAsync(
        string to,
        string templateCode,
        Dictionary<string, string> data,
        string? cc = null,
        string? bcc = null,
        List<string>? attachmentPaths = null,
        string? createdBy = null,
        string? relatedEntityType = null,
        string? relatedEntityId = null);

    // Queue for later sending
    Task<int> QueueEmailAsync(
        string to,
        string subject,
        string htmlMessage,
        string? plainTextMessage = null,
        string? cc = null,
        string? bcc = null,
        DateTime? scheduledSendDate = null,
        int priority = 5,
        string? createdBy = null);

    // Queue templated email
    Task<int> QueueTemplatedEmailAsync(
        string to,
        string templateCode,
        Dictionary<string, string> data,
        string? cc = null,
        string? bcc = null,
        DateTime? scheduledSendDate = null,
        int priority = 5,
        string? createdBy = null);

    // Process queued emails
    Task ProcessQueueAsync(int maxEmails = 50);

    // Get email logs
    Task<(List<EmailLog> logs, int totalCount)> GetEmailLogsAsync(
        string? toEmail = null,
        string? status = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int page = 1,
        int pageSize = 50);

    // Retry failed email
    Task<bool> RetryEmailAsync(int emailLogId);

    // Get statistics
    Task<EmailStatistics> GetEmailStatisticsAsync(
        DateTime? startDate = null,
        DateTime? endDate = null);
}
```

### IEmailTemplateService

```csharp
public interface IEmailTemplateService
{
    // Render template with data
    Task<(string subject, string htmlBody, string plainTextBody)> RenderTemplateAsync(
        string templateCode,
        Dictionary<string, string> data);

    // Get template by code
    Task<EmailTemplate?> GetTemplateByCodeAsync(string templateCode);

    // Get all active templates
    Task<List<EmailTemplate>> GetActiveTemplatesAsync();

    // Get templates by category
    Task<List<EmailTemplate>> GetTemplatesByCategoryAsync(string category);

    // CRUD operations
    Task<EmailTemplate> CreateTemplateAsync(EmailTemplate template);
    Task<EmailTemplate> UpdateTemplateAsync(EmailTemplate template);
    Task<bool> DeleteTemplateAsync(int templateId);
}
```

## Admin Pages

### Available Pages

1. **`/Admin/EmailConfiguration`** - Configure SMTP settings
2. **`/Admin/EmailTemplates`** - Manage email templates
3. **`/Admin/EmailTemplateEdit`** - Create/edit templates
4. **`/Admin/EmailLogs`** - View email history and logs
5. **`/Admin/SendEmail`** - Send emails manually

### Navigation

All email management pages are accessible from the admin menu under "Email Management" section.

## Best Practices

### Security

1. **Store SMTP passwords securely** - Consider encrypting passwords in database
2. **Use app passwords** - For Gmail, use app-specific passwords
3. **Validate email addresses** - Always validate recipient addresses
4. **Rate limiting** - Implement rate limiting for email sending
5. **Authentication** - All admin pages require Admin role

### Performance

1. **Use email queue** - For bulk emails, use the queue system
2. **Process queue in background** - Implement a background job to process queue
3. **Limit batch size** - Process emails in batches (default: 50)
4. **Monitor logs** - Regularly review email logs for failures

### Template Design

1. **Keep it simple** - Use simple HTML for better email client compatibility
2. **Provide plain text** - Always provide a plain text alternative
3. **Test templates** - Test templates across different email clients
4. **Use placeholders** - Document all available placeholders
5. **Version control** - Keep track of template changes

## Troubleshooting

### Common Issues

1. **Emails not sending:**
   - Check SMTP configuration
   - Verify server and port settings
   - Ensure SSL/TLS is configured correctly
   - Check firewall rules

2. **Authentication failures:**
   - Verify username and password
   - For Gmail, use app passwords
   - Check if "less secure apps" is enabled (if required)

3. **Emails in spam:**
   - Configure SPF, DKIM, and DMARC records
   - Use a reputable SMTP service
   - Ensure "From" address matches domain

4. **Template placeholders not replaced:**
   - Verify placeholder names match exactly
   - Check for typos in placeholder names
   - Ensure data is passed to template renderer

### Logging

All email operations are logged. Check application logs for:
- Email send attempts
- SMTP connection errors
- Template rendering issues
- Configuration problems

## Future Enhancements

Potential improvements:
1. Email open tracking (pixel tracking)
2. Link click tracking
3. Attachment management UI
4. Batch email campaigns
5. Email scheduling UI
6. Template versioning
7. A/B testing support
8. Email analytics dashboard
9. Unsubscribe management
10. Email encryption support

## Support

For issues or questions:
1. Check application logs
2. Review this documentation
3. Contact your system administrator
4. Review ASP.NET Core email documentation

---

## Quick Start Checklist

- [ ] Apply database migration (`dotnet ef database update`)
- [ ] Configure SMTP settings at `/Admin/EmailConfiguration`
- [ ] Test email configuration using "Send Test Email"
- [ ] (Optional) Seed default templates
- [ ] Create custom templates if needed
- [ ] Start sending emails!

---

**Last Updated:** 2025-10-15
**Version:** 1.0
