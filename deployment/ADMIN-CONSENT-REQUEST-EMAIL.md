# Email to Request Admin Consent for Azure AD Application

---

**Subject:** Request for Admin Consent - UNON-EBILL2-DEV Azure AD Application

---

Dear IT Administrator,

I am writing to request admin consent for our Azure AD application **UNON-EBILL2-DEV** which is required for our E-Bill Management System deployment.

## Application Details

- **Application Name:** UNON-EBILL2-DEV
- **Application ID (Client ID):** be65f496-53a8-472b-ac6e-43c3e09dacdb
- **Tenant ID:** 0f9e35db-544f-4f60-bdcc-5ea416e6dc70
- **Application URL:** http://10.104.104.78

## Issue

Users are currently unable to sign in to the application because it requires admin consent to access the following user information:
- Email address
- First name (given_name)
- Last name (family_name)
- Preferred username
- Group membership

These permissions are standard and necessary for the application to function properly and provision user accounts automatically.

## Action Required

I kindly request that you grant admin consent for this application using one of the following methods:

### Method 1: Direct Consent URL (Quickest)
Please click on this link while signed in as an Azure AD administrator:

```
https://login.microsoftonline.com/0f9e35db-544f-4f60-bdcc-5ea416e6dc70/adminconsent?client_id=be65f496-53a8-472b-ac6e-43c3e09dacdb
```

This will prompt you to review and approve the permissions. Simply click "Accept" to grant consent for the entire organization.

### Method 2: Via Azure Portal
1. Go to [Azure Portal](https://portal.azure.com)
2. Navigate to **Azure Active Directory** → **App registrations**
3. Search for and open: **UNON-EBILL2-DEV**
4. Click **"API permissions"** in the left menu
5. Click the button: **"Grant admin consent for United Nations"**
6. Click **"Yes"** to confirm

## Permissions Requested

The application requests the following Microsoft Graph permissions:
- **User.Read** - Sign in and read user profile (basic permission)
- **Optional Claims:**
  - email
  - given_name
  - family_name
  - preferred_username
  - groups

All permissions are read-only and are used solely for user authentication and profile information.

## Security & Compliance

- This application is hosted internally on our server (10.104.104.78)
- It uses Azure AD for secure authentication
- No sensitive data is shared with third parties
- The application follows Microsoft's security best practices

## Urgency

This is blocking user access to the E-Bill Management System. Your prompt attention to this matter would be greatly appreciated.

Please let me know if you need any additional information or have any questions about this request.

## Verification After Consent

Once consent is granted, I will verify that users can successfully log in. I will notify you once the verification is complete.

Thank you for your assistance.

Best regards,
[Your Name]
[Your Title]
[Your Contact Information]

---

## Additional Information for IT Admin

### What Happens When Admin Consent is Granted?

1. All users in the organization will be able to sign in to the application
2. The application will be able to read basic user profile information
3. Users will not see additional consent prompts when signing in
4. The application will appear in the user's "My Apps" portal

### How to Verify Consent Was Granted

After granting consent, you should see green checkmarks with "Granted for United Nations" next to each permission in the API permissions blade.

### How to Revoke Consent (If Needed in Future)

1. Go to **Azure Active Directory** → **Enterprise applications**
2. Find **UNON-EBILL2-DEV**
3. Go to **Permissions**
4. Click on individual permissions to review or revoke

### Security Considerations

- The application uses industry-standard OAuth 2.0 / OpenID Connect protocols
- All communication is encrypted via HTTPS
- Client secrets are stored securely and not exposed to end users
- The application follows the principle of least privilege

---

## Quick Reference for IT Admin

**One-Click Consent URL:**
```
https://login.microsoftonline.com/0f9e35db-544f-4f60-bdcc-5ea416e6dc70/adminconsent?client_id=be65f496-53a8-472b-ac6e-43c3e09dacdb
```

**Required Role:** Global Administrator, Application Administrator, or Cloud Application Administrator

**Time Required:** Approximately 2-3 minutes

**Impact:** Enables all organization users to access the E-Bill Management System

---

