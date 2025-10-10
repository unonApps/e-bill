# Email Notification Configuration Guide

Your application already has email functionality built in! Here's how to configure it for production.

---

## Current Status

✅ **Email Settings Page**: `/Admin/EmailSettings`
✅ **Email Service**: Already configured in `Services/EmailService.cs`
✅ **Test Email Feature**: Send test emails to verify configuration

---

## Configuration Options

### Option 1: Microsoft 365 / Azure (Recommended for Organizations)

**Best for**: Organizations using Microsoft 365

**Settings:**
```
SMTP Server: smtp.office365.com
SMTP Port: 587
Enable SSL: Yes (checked)
From Email: your-email@yourorganization.com
From Name: TAB System Notifications
Username: your-email@yourorganization.com
Password: Your-Password (or App Password)
```

**Steps:**
1. Log in to Azure Portal as admin
2. Go to **App Services** → `TABWeb20250926123812`
3. Click **Configuration** → **Application settings**
4. Add/Update these settings:

```
EmailSettings__SmtpServer = smtp.office365.com
EmailSettings__SmtpPort = 587
EmailSettings__FromEmail = notifications@yourdomain.com
EmailSettings__FromName = TAB System
EmailSettings__Username = notifications@yourdomain.com
EmailSettings__Password = YOUR_PASSWORD
EmailSettings__EnableSsl = true
```

5. Click **Save** and **Restart** the app

---

### Option 2: Gmail (For Testing/Small Deployments)

**Best for**: Testing or small deployments

**Important**: You need a Gmail **App Password** (not your regular password)

**Get Gmail App Password:**
1. Go to [Google Account Security](https://myaccount.google.com/security)
2. Enable **2-Step Verification** (if not already enabled)
3. Go to **App passwords**
4. Generate password for "Mail" on "Windows Computer"
5. Copy the 16-character password (remove spaces)

**Settings:**
```
SMTP Server: smtp.gmail.com
SMTP Port: 587
Enable SSL: Yes (checked)
From Email: your-email@gmail.com
From Name: TAB System Notifications
Username: your-email@gmail.com
Password: [16-character App Password]
```

---

### Option 3: SendGrid (Recommended for Production)

**Best for**: High-volume production emails

**Why SendGrid:**
- Free tier: 100 emails/day
- Reliable delivery
- Email analytics
- No spam concerns

**Setup:**
1. Sign up at [SendGrid.com](https://sendgrid.com)
2. Create an API Key
3. Use these settings:

```
SMTP Server: smtp.sendgrid.net
SMTP Port: 587
Enable SSL: Yes
From Email: noreply@yourdomain.com
From Name: TAB System
Username: apikey (literally "apikey")
Password: [Your SendGrid API Key]
```

---

## How to Configure

### Method 1: Azure Portal (Production - Recommended)

1. Go to [Azure Portal](https://portal.azure.com)
2. Navigate to **App Services** → `TABWeb20250926123812`
3. Click **Configuration** (left menu)
4. Under **Application settings**, click **+ New application setting**
5. Add each setting from your chosen option above
6. Format: `EmailSettings__SettingName` (note the double underscore)
7. Click **Save** at the top
8. Click **Restart**

**Example:**
```
Name: EmailSettings__SmtpServer
Value: smtp.office365.com
```

### Method 2: Via Application UI (Testing)

1. Deploy your application to Azure
2. Log in as Admin
3. Go to `/Admin/EmailSettings`
4. Fill in the form with your chosen provider settings
5. Click **Save Settings**
6. Enter a test email and click **Send Test Email**

---

## Testing Email Configuration

### Step 1: Access Email Settings
1. Log in as Admin
2. Navigate to: https://tabweb20250926123812.azurewebsites.net/Admin/EmailSettings

### Step 2: Configure Settings
Fill in the form based on your chosen provider (see options above)

### Step 3: Send Test Email
1. Enter your email address in "Test Email Address"
2. Click **Send Test Email**
3. Check your inbox (and spam folder)

### Step 4: Verify
You should receive an email with subject "Test Email"

---

## Where Emails Are Used

Your application sends emails for:

1. **SIM Card Requests**
   - Request submitted
   - Supervisor approval/rejection
   - ICTS approval/rejection
   - SIM ready for collection

2. **Refund Requests**
   - Request submitted
   - Supervisor approval/rejection
   - Budget Officer approval/rejection
   - Claims Unit processing
   - Payment approval

3. **E-Bill Approvals**
   - Bill submitted for approval
   - Supervisor approval/rejection

---

## Customizing Email Templates

Email templates are in `Services/EmailService.cs`

**Current Templates:**
- Request submission confirmation
- Approval notifications
- Rejection notifications
- Status updates

**To Customize:**
1. Edit `Services/EmailService.cs`
2. Look for methods like `SendEmailAsync`
3. Update HTML templates
4. Redeploy application

---

## Troubleshooting

### "Error sending test email: Authentication failed"
**Fix**:
- Gmail: Make sure you're using App Password, not regular password
- Microsoft 365: Check username/password are correct
- Verify 2FA/MFA isn't blocking

### "Error sending test email: Could not connect to SMTP server"
**Fix**:
- Check SMTP server address is correct
- Verify port number (usually 587)
- Ensure firewall isn't blocking outbound SMTP

### "Email sent but not received"
**Fix**:
- Check spam/junk folder
- Verify "From Email" is valid
- For Gmail: Check "Less secure app access" if using regular password
- For production: Use SendGrid or Microsoft 365

### "Settings not persisting after restart"
**Fix**:
- Current implementation uses TempData (temporary)
- For production, add settings to Azure App Service Configuration
- Or store in database (requires custom implementation)

---

## Production Recommendations

1. **Use Azure App Service Configuration**
   - Settings persist across restarts
   - More secure (not in code)
   - Easy to update without redeployment

2. **Use Organization Email**
   - Microsoft 365: `noreply@yourdomain.com`
   - Or dedicated service: `notifications@yourdomain.com`
   - Avoids Gmail daily limits

3. **Enable Email Logging**
   - Already configured in `EmailService.cs`
   - Check logs in Azure: `https://tabweb20250926123812.scm.azurewebsites.net/api/logs/docker`

4. **Monitor Email Delivery**
   - Use SendGrid for analytics
   - Or Microsoft 365 Message Trace

---

## Quick Start Checklist

- [ ] Choose email provider (Microsoft 365 recommended)
- [ ] Get credentials (email + password/app password)
- [ ] Configure in Azure App Service settings
- [ ] Restart application
- [ ] Test via `/Admin/EmailSettings`
- [ ] Verify test email received
- [ ] Test with actual workflow (create SIM request)

---

## Security Best Practices

✅ **Never commit passwords to Git**
✅ **Use App Passwords for Gmail**
✅ **Store credentials in Azure Configuration**
✅ **Use dedicated email account for system notifications**
✅ **Enable SSL/TLS (port 587)**
✅ **Monitor for unusual email activity**

---

## Need Help?

**Check logs:**
```
https://tabweb20250926123812.scm.azurewebsites.net/api/logs/docker
```

**Common error codes:**
- `5.7.1`: Authentication failed - check username/password
- `5.7.0`: Authentication required - enable SMTP auth
- Connection timeout: Check firewall/network settings

---

🎉 **Your email system is ready! Just configure and test!**
