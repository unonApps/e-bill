# Gmail SMTP Configuration Guide

## Current Error
**Error:** `The SMTP server requires a secure connection or the client was not authenticated. The server response was: 5.7.0 Authentication Required.`

## Correct Gmail Settings

Use these exact settings in your Email Configuration page:

```
SMTP Server: smtp.gmail.com
SMTP Port: 587
From Email Address: your-email@gmail.com
From Display Name: TAB System
Username: your-email@gmail.com (FULL email address)
Password: xxxx xxxx xxxx xxxx (16-character app password)

Checkboxes:
✅ Enable SSL (MUST be checked)
✅ Is Active (checked)
❌ Use Default Credentials (MUST be unchecked)

Timeout: 30 seconds
```

## Common Mistakes and Fixes

### 1. Username Format
**WRONG:** `username` or `username@`
**RIGHT:** `username@gmail.com` (full email address)

### 2. Password Spaces
When you copy the app password from Google, it might look like: `abcd efgh ijkl mnop`

**OPTION 1:** Copy it with spaces - Gmail accepts both formats
**OPTION 2:** Remove all spaces: `abcdefghijklmnop`

### 3. App Password Not Set Up Correctly

**Step-by-step to create Gmail App Password:**

1. Go to: https://myaccount.google.com/security
2. Scroll down to "How you sign in to Google"
3. Click on "2-Step Verification" (if not enabled, enable it first)
4. Scroll to bottom and click "App passwords"
5. Select "Mail" as the app
6. Select "Windows Computer" as the device (or Other)
7. Click "Generate"
8. Copy the 16-character password (it will be shown once)
9. Paste it into the Password field

### 4. 2-Step Verification Not Enabled
App passwords ONLY work if 2-Step Verification is enabled:
- Go to: https://myaccount.google.com/signinoptions/two-step-verification
- Follow the steps to enable it
- Then create an app password

### 5. "Use Default Credentials" Checkbox
**MUST be UNCHECKED** for Gmail. If checked, it tries to use Windows credentials instead of your Gmail username/password.

## Testing Your Configuration

After filling in the settings:

1. **DO NOT click "Save Configuration" yet**
2. Enter your email address in "Test Email Address" field
3. Click "Send Test Email" button
4. Check your inbox (and spam folder)

If test succeeds → Click "Save Configuration"
If test fails → Check the error and fix the settings before saving

## Verification Checklist

Before testing, verify:

- [ ] SMTP Server is exactly: `smtp.gmail.com`
- [ ] SMTP Port is: `587`
- [ ] Username is your FULL email: `yourname@gmail.com`
- [ ] Password is the 16-character app password (not your regular Gmail password)
- [ ] Enable SSL is CHECKED
- [ ] Use Default Credentials is UNCHECKED
- [ ] 2-Step Verification is enabled on your Google account
- [ ] App password was created for "Mail" app

## Still Not Working?

### Check Browser Console
1. Press F12 in your browser
2. Look for any JavaScript errors
3. Check if password field is being submitted correctly

### Check Application Logs
Look for authentication errors in your application logs that might give more details.

### Try Different Port (Less Common)
If port 587 doesn't work, try:
- Port 465 with SSL (less common, but some networks require this)

### Network/Firewall Issues
Some corporate networks block SMTP connections. Check with your IT department.

### Revoke and Recreate App Password
1. Go to App Passwords in Google Account
2. Delete the existing app password
3. Create a new one
4. Use the new password in your configuration

## Example of Working Configuration

**From Email:** `tab.system@gmail.com`
**Username:** `tab.system@gmail.com`
**Password:** `abcd efgh ijkl mnop` (app password with or without spaces)
**Server:** `smtp.gmail.com`
**Port:** `587`
**SSL:** `✅ Enabled`
**Default Credentials:** `❌ Disabled`

## Need More Help?

If you're still getting authentication errors after checking everything above:

1. Double-check the app password by deleting and recreating it
2. Make sure you're copying the password correctly (no extra spaces at the beginning/end)
3. Try logging into Gmail in a browser with the same account to verify the account is active
4. Check if your Google account has any security alerts or blocks
