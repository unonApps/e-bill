# Manual Microsoft 365 Email Configuration

## Quick Setup Guide

### Step 1: Prepare Your Email Account

**You need:**
- A Microsoft 365 email account
- Example: `notifications@yourdomain.com` or `noreply@yourdomain.com`
- The password for this account

**Important:**
- The email account must have SMTP authentication enabled
- If your organization uses MFA, you may need to create an App Password

---

## Step 2: Add Settings to Azure App Service

### Option A: Via Azure Portal (Easiest)

1. **Go to Azure Portal**
   - Navigate to: https://portal.azure.com
   - Sign in with your admin account

2. **Find Your App Service**
   - Click **App Services** in the left menu
   - Select: `TABWeb20250926123812`

3. **Open Configuration**
   - Click **Configuration** in the left menu
   - Click the **Application settings** tab

4. **Add Email Settings**
   Click **+ New application setting** for each of these:

   **Setting 1:**
   ```
   Name: EmailSettings__SmtpServer
   Value: smtp.office365.com
   ```

   **Setting 2:**
   ```
   Name: EmailSettings__SmtpPort
   Value: 587
   ```

   **Setting 3:**
   ```
   Name: EmailSettings__FromEmail
   Value: [YOUR_EMAIL]@yourdomain.com
   ```
   Example: `notifications@unon.org`

   **Setting 4:**
   ```
   Name: EmailSettings__FromName
   Value: TAB System
   ```

   **Setting 5:**
   ```
   Name: EmailSettings__Username
   Value: [YOUR_EMAIL]@yourdomain.com
   ```
   (Usually same as FromEmail)

   **Setting 6:**
   ```
   Name: EmailSettings__Password
   Value: [YOUR_PASSWORD]
   ```
   ⚠️ **Important**: This will be hidden after saving

   **Setting 7:**
   ```
   Name: EmailSettings__EnableSsl
   Value: true
   ```

5. **Save and Restart**
   - Click **Save** at the top
   - Click **Continue** when prompted
   - Click **Overview** → **Restart** to restart the app

---

### Option B: Via PowerShell (Advanced)

Run the provided script:
```powershell
.\configure-email-azure.ps1
```

Follow the prompts to enter your email credentials.

---

## Step 3: Test Email Configuration

1. **Access Your Application**
   - Go to: https://tabweb20250926123812.azurewebsites.net
   - Log in as Admin

2. **Navigate to Email Settings**
   - Go to: **Admin** → **Email Settings**
   - OR directly: https://tabweb20250926123812.azurewebsites.net/Admin/EmailSettings

3. **Verify Settings Loaded**
   - You should see the settings you configured
   - If not, the settings are still being loaded from Azure

4. **Send Test Email**
   - Enter your email address in "Test Email Address"
   - Click **Send Test Email**
   - Check your inbox (and spam folder!)

5. **Success!**
   - You should receive an email with subject "Test Email"
   - If successful, email notifications are now working!

---

## Troubleshooting

### Error: "Authentication failed"

**Possible causes:**
- Username or password is incorrect
- Account has MFA enabled without App Password
- SMTP authentication is disabled

**Solutions:**
1. **Verify credentials** - Try logging in to Outlook Web with the same credentials
2. **Check MFA** - If MFA is enabled:
   - Go to: https://account.microsoft.com/security
   - Create an App Password
   - Use the App Password instead of your regular password
3. **Contact IT** - Your IT department may need to enable SMTP authentication

### Error: "Could not connect to SMTP server"

**Solutions:**
- Verify SMTP server: `smtp.office365.com`
- Verify port: `587`
- Check that outbound port 587 is not blocked by firewall

### Test email sent but not received

**Solutions:**
- Check spam/junk folder
- Verify "From Email" is a valid address in your organization
- Check Microsoft 365 Message Trace:
  - Go to: https://admin.microsoft.com
  - Exchange admin center → Mail flow → Message trace

### Settings not showing in /Admin/EmailSettings

**This is normal!** Settings from Azure App Service Configuration take precedence and won't show in the UI.

To verify settings are working:
- Just send a test email
- Check Azure logs if there are errors

---

## Security Best Practices

✅ **Use a dedicated service account**
   - Example: `noreply@yourdomain.com` or `notifications@yourdomain.com`
   - Don't use a personal email account

✅ **Use App Password if MFA is enabled**
   - Never disable MFA to make email work
   - Create an App Password instead

✅ **Limit mailbox permissions**
   - The service account only needs "Send As" permission
   - No need for admin rights

✅ **Monitor email activity**
   - Check sent items periodically
   - Set up alerts for unusual activity

✅ **Rotate passwords regularly**
   - Change password every 90 days
   - Update in Azure App Service Configuration

---

## When Are Emails Sent?

Your application sends emails for these workflows:

### SIM Card Requests
- ✉️ Request submitted → Requester
- ✉️ Pending supervisor approval → Supervisor
- ✉️ Approved by supervisor → Requester + ICTS
- ✉️ Rejected by supervisor → Requester
- ✉️ Approved by ICTS → Requester
- ✉️ SIM ready for collection → Requester

### Refund Requests
- ✉️ Request submitted → Requester
- ✉️ Pending supervisor approval → Supervisor
- ✉️ Approved by supervisor → Requester + Budget Officer
- ✉️ Rejected by supervisor → Requester
- ✉️ Budget approved → Requester + Claims Unit
- ✉️ Claims processed → Requester + Payment Approver
- ✉️ Payment approved → Requester

### E-Bill Approvals
- ✉️ Bill submitted → Requester
- ✉️ Pending approval → Supervisor
- ✉️ Approved → Requester
- ✉️ Rejected → Requester

---

## Email Templates

Default templates are in `Services/EmailService.cs`

To customize:
1. Edit the HTML templates in EmailService.cs
2. Update subject lines
3. Add your organization's logo/branding
4. Redeploy the application

---

## Alternative: Using Gmail (Testing Only)

If you don't have access to Microsoft 365 credentials, you can use Gmail for testing:

**Settings:**
```
SMTP Server: smtp.gmail.com
Port: 587
From Email: your-email@gmail.com
Username: your-email@gmail.com
Password: [16-character App Password]
Enable SSL: true
```

**Get Gmail App Password:**
1. Go to: https://myaccount.google.com/security
2. Enable 2-Step Verification
3. Generate App Password for "Mail"
4. Use the 16-character password

⚠️ **Note**: Gmail has daily sending limits (100-500 emails/day)

---

## Need Help?

**Check Azure Logs:**
```
https://tabweb20250926123812.scm.azurewebsites.net/api/logs/docker
```

Look for entries with:
- `Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure.PageActionInvoker`
- `TAB.Web.Services.EmailService`

**Common Log Messages:**
- "Successfully sent email" → Email was sent
- "Error sending email" → Check error details
- "Authentication failed" → Check username/password

---

## Configuration Checklist

- [ ] Microsoft 365 email account created/available
- [ ] SMTP authentication enabled on the account
- [ ] Password/App Password obtained
- [ ] Settings added to Azure App Service Configuration
- [ ] App Service restarted
- [ ] Test email sent successfully
- [ ] Test email received
- [ ] Spam folder checked (if not received)

---

🎉 **Once configured, your email notifications will work automatically!**

All workflows (SIM requests, refunds, approvals) will send emails to relevant users.
