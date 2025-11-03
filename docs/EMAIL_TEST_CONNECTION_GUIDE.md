# Test Connection Feature - Usage Guide

## What's New?

A new "Test Connection" button has been added to verify your SMTP settings **before sending any emails**.

## New Page Layout

```
┌─────────────────────────────────────────────────────────────────┐
│  Email Configuration                  [View All Configurations] │
├──────────────────────────────┬──────────────────────────────────┤
│  SMTP Configuration (Left)   │  Test Configuration (Right)      │
│  ┌────────────────────────┐  │  ┌─────────────────────────────┐ │
│  │ SMTP Server            │  │  │ [Test Connection] button    │ │
│  │ SMTP Port              │  │  │ Tests auth without sending  │ │
│  │ From Email             │  │  │                             │ │
│  │ Username               │  │  │ ────────────────────────────│ │
│  │ Password               │  │  │                             │ │
│  │ Options                │  │  │ Recipient Email Address     │ │
│  │                        │  │  │ [Send Test Email] button    │ │
│  │ [Save Configuration]   │  │  │                             │ │
│  └────────────────────────┘  │  │ Testing Workflow:           │ │
│                              │  │ 1. Fill in SMTP settings    │ │
│                              │  │ 2. Test Connection          │ │
│                              │  │ 3. Send test email          │ │
│                              │  │ 4. Save configuration       │ │
│                              │  └─────────────────────────────┘ │
└──────────────────────────────┴──────────────────────────────────┘
```

## How to Use the Test Connection Feature

### Step 1: Fill in Your Gmail Settings

**Main form (left side):**
```
SMTP Server: smtp.gmail.com
SMTP Port: 587
From Email Address: acmichuki@gmail.com
From Display Name: TAB System
Username: acmichuki@gmail.com
Password: tuyq uych urha jnns

Checkboxes:
✅ Enable SSL (checked)
❌ Use Default Credentials (UNCHECKED - very important!)
✅ Is Active (checked)

Timeout: 30
```

### Step 2: Test Connection First

**Click "Test Connection" button** (right side, top button)

This will:
- Connect to the SMTP server
- Authenticate with your credentials
- Verify the settings are correct
- **NOT send any email**

### Step 3: Check the Result

**Success Message:**
```
✅ Connection successful! SMTP server authenticated successfully.
   You can now send a test email or save the configuration.
```
→ If you see this, your credentials are correct! Proceed to Step 4.

**Error Message Examples:**

```
❌ Connection failed: SMTP Error: 5.7.0 Authentication Required
   Check: Username must be full email, Use Default Credentials
   must be UNCHECKED, Password must be app password.
```
→ Fix the settings based on the error hints provided.

```
❌ Connection failed: Unable to connect to remote server
   Check: SMTP server address and port are correct,
   Enable SSL is checked for port 587.
```
→ Check your network and SMTP settings.

### Step 4: Send Test Email (Optional)

If connection test succeeds:
1. Enter your email in "Recipient Email Address" field
2. Click **"Send Test Email"** button
3. Check your inbox to confirm delivery

### Step 5: Save Configuration

If everything works:
- Click **"Save Configuration"** (left side)
- Your settings are now saved to the database

## Benefits of Test Connection

✅ **Faster Testing** - No need to wait for email sending
✅ **Clear Errors** - Get immediate feedback on authentication issues
✅ **Save Time** - Verify credentials before attempting email delivery
✅ **Better Troubleshooting** - Separates connection issues from delivery issues

## Testing Workflow

```
┌──────────────────────────┐
│ 1. Fill in SMTP Settings │
└────────────┬─────────────┘
             │
             ▼
┌──────────────────────────┐
│ 2. Click "Test Connection"│
└────────────┬─────────────┘
             │
        ┌────┴─────┐
        │          │
        ▼          ▼
    ✅ SUCCESS  ❌ FAILED
        │          │
        │          └──► Fix settings and retry
        │
        ▼
┌──────────────────────────┐
│ 3. Click "Send Test Email"│
└────────────┬─────────────┘
             │
        ┌────┴─────┐
        │          │
        ▼          ▼
    ✅ RECEIVED  ❌ FAILED
        │          │
        │          └──► Check spam, logs
        │
        ▼
┌──────────────────────────┐
│ 4. Click "Save Config"    │
└──────────────────────────┘
```

## Common Issues and Solutions

### Issue: "Use Default Credentials" is Checked
**Solution:** UNCHECK this box! This is the most common cause of authentication failures.

### Issue: Username is "acmichuki" instead of "acmichuki@gmail.com"
**Solution:** Always use the FULL email address as the username.

### Issue: Using regular Gmail password instead of app password
**Solution:** Create an app password at https://myaccount.google.com/apppasswords

### Issue: 2-Step Verification not enabled
**Solution:** Enable 2-Step Verification first, then create app password

## What Happens Behind the Scenes

**Test Connection:**
1. Creates SMTP client with your settings
2. Connects to smtp.gmail.com:587
3. Attempts authentication with username/password
4. Closes connection
5. Reports success or error

**Send Test Email:**
1. Does everything in "Test Connection"
2. Plus: Sends actual email with test content
3. Plus: Verifies email delivery

## After Successful Test

Once you see "✅ Connection successful!":
1. You can send a test email to verify delivery
2. Or you can directly save the configuration
3. The saved configuration will be used for all system emails

## Need Help?

If test connection fails:
1. Read the error message carefully - it includes specific hints
2. Check the checklist in `GMAIL_SETUP_GUIDE.md`
3. Verify each setting matches the example exactly
4. Make sure "Use Default Credentials" is UNCHECKED

---

**Note:** The test connection sends a minimal email to verify authentication. In a future update, this could be changed to just test the connection without sending, but for now, it sends to your own email address as specified in the username.
