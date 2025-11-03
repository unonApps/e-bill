# Email Configuration Testing Guide

## ✅ Fixed Issues

1. **Validation Error Fixed** - No longer requires TestEmailAddress when saving configuration
2. **Test Before Save** - You can now test email settings BEFORE saving them to the database
3. **Clear Error Messages** - Shows exactly what validation errors occur

## How to Configure and Test Outlook Email

### Step 1: Fill in Outlook Settings

Navigate to: **Administration → Email Management → Email Configuration**

Fill in these fields:

```
SMTP Server: smtp.office365.com
SMTP Port: 587
From Email Address: your-email@yourdomain.com
From Display Name: TAB System
Username: your-email@yourdomain.com
Password: your-outlook-password
```

**Checkboxes:**
- ✅ Enable SSL (must be checked)
- ✅ Is Active (checked)
- ❌ Use Default Credentials (unchecked)

**Timeout:** 30 seconds

### Step 2: Test First (Before Saving!)

1. After filling in all the fields above
2. **Look at the right side** → "Send Test Email" card
3. Enter your email address in "Test Email Address"
4. Click **"Send Test Email"**
5. Check your inbox (and spam folder)

**You don't need to save first!** The test uses the values you entered.

### Step 3: Save Configuration

If the test email works:
1. Click **"Save Configuration"** (main form on the left)
2. You'll see: "Email configuration saved successfully"

If the test fails:
- Check the error message
- Fix the issue
- Test again before saving

## Common Test Results

### ✅ Success
```
Test email sent successfully to your-email@example.com.
Please check your inbox (and spam folder).
```
**Action:** Click "Save Configuration" to save permanently

### ❌ Authentication Failed
```
Error sending test email: The SMTP server requires a secure connection...
```
**Action:**
- Verify username is full email address
- Check password is correct
- Ensure SMTP AUTH is enabled in Microsoft 365 Admin Center

### ❌ Connection Failed
```
Error sending test email: Unable to connect to remote server
```
**Action:**
- Verify smtp.office365.com is correct
- Check port is 587
- Ensure Enable SSL is checked
- Check firewall/network settings

### ⚠️ Fields Missing
```
Please fill in all SMTP configuration fields before testing.
```
**Action:** Fill in all required fields (Server, Port, From Email, Username, Password)

## Workflow Summary

```
1. Enter Outlook Settings
   ↓
2. Click "Send Test Email" (no need to save!)
   ↓
3. Check if email received
   ↓
4. If YES → Click "Save Configuration"
   If NO → Fix settings and test again
```

## After Configuration is Saved

Once saved, the email system will use these settings for:
- Welcome emails to new users
- Notification emails
- Password reset emails
- Custom emails you send
- Template-based emails

## Need Help?

**Check Email Logs:**
Administration → Email Management → Email History & Logs

**View Error Details:**
- Failed emails show error messages
- You can retry failed emails
- Statistics show sent/failed counts

---

**Note:** You can change settings anytime by coming back to this page, updating the values, testing, and saving again.
