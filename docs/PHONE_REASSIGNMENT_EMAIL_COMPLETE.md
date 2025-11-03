# Phone Reassignment - Dual Email Notification Implementation

## Overview
This document details the complete implementation of dual email notifications for phone number reassignment in the UNON E-Billing System. When a phone number is reassigned from one user to another, **BOTH users now receive email notifications**.

---

## Implementation Complete ✅

### What Was Implemented

When an admin reassigns a phone number from one user to another (using the "Force Reassign" option):

1. **Previous User (Loses the Phone)**:
   - Receives **Phone Number Unassigned** email
   - Email includes detailed reason explaining the reassignment
   - Shows who the phone was reassigned to
   - Email Template: `PHONE_NUMBER_UNASSIGNED`
   - Subject: "Phone Number Unassigned from Your Account - UNON E-Billing"

2. **New User (Receives the Phone)**:
   - Receives **Phone Number Assigned** email
   - Email confirms their new phone assignment
   - Shows complete phone details (number, type, line type)
   - Email Template: `PHONE_NUMBER_ASSIGNED`
   - Subject: "Phone Number Assigned to Your Account - UNON E-Billing"

---

## How It Works

### Reassignment Trigger
Reassignment occurs when:
- An admin tries to assign a phone that's already assigned to another user
- The admin confirms the "Force Reassign" action
- The system automatically:
  1. Unassigns the phone from the previous user
  2. Sends unassignment email to previous user
  3. Assigns the phone to the new user
  4. Sends assignment email to new user

### Code Implementation

**Location**: `Services/UserPhoneService.cs` - `AssignPhoneAsync` method

#### Step 1: Load Previous User Data (Lines 105-110)
```csharp
// Check if phone is already assigned to another user
var existingAssignment = await _context.UserPhones
    .Include(up => up.EbillUser)
        .ThenInclude(u => u.ApplicationUser)
    .FirstOrDefaultAsync(up => up.PhoneNumber == phoneNumber &&
                              up.IsActive &&
                              up.IndexNumber != indexNumber);
```

**Why Important**: The `.Include(up => up.EbillUser).ThenInclude(u => u.ApplicationUser)` ensures we load:
- The previous user's data (`EbillUser`)
- Their login account with email address (`ApplicationUser`)

#### Step 2: Send Unassignment Email to Previous User (Lines 140-154)
```csharp
// Send unassignment email to previous user
if (previousUserForEmail != null)
{
    var newUserInfo = await _context.EbillUsers
        .FirstOrDefaultAsync(u => u.IndexNumber == indexNumber);
    var newUserName = newUserInfo != null
        ? $"{newUserInfo.FirstName} {newUserInfo.LastName}"
        : indexNumber;

    await SendPhoneUnassignedEmailAsync(
        previousUserForEmail,
        existingAssignment,
        $"This phone number has been reassigned to {newUserName} (Index: {indexNumber})"
    );
}
```

**Email Content**:
- **To**: Previous user's email
- **Subject**: "Phone Number Unassigned from Your Account - UNON E-Billing"
- **Reason**: "This phone number has been reassigned to John Doe (Index: 8817861)"
- **Phone Details**: Shows the phone number, type, and previous line type
- **Action**: Informs user they no longer have this phone

#### Step 3: Send Assignment Email to New User (Line 262)
```csharp
// Send email notification for phone assignment
await SendPhoneAssignedEmailAsync(user, assignedPhone);
```

**Email Content**:
- **To**: New user's email
- **Subject**: "Phone Number Assigned to Your Account - UNON E-Billing"
- **Phone Details**: Complete phone information (number, type, line type)
- **Responsibilities**: Explains user's duties regarding the phone
- **Link**: Direct link to view all their phones

---

## Email Templates Used

### 1. Phone Number Unassigned Email (Previous User)
**Template Code**: `PHONE_NUMBER_UNASSIGNED`

**Key Placeholders**:
- `{{FirstName}}`, `{{LastName}}` - Previous user's name
- `{{PhoneNumber}}` - The phone that was reassigned
- `{{PhoneType}}` - Type (Mobile, Landline, etc.)
- `{{LineType}}` - Previous line type (Primary, Secondary)
- `{{Reason}}` - **"This phone number has been reassigned to [New User] (Index: [indexNumber])"**
- `{{UnassignedDate}}` - When the reassignment occurred
- `{{IndexNumber}}` - Previous user's index number
- `{{UserPhonesUrl}}` - Link to view remaining active phones

**Visual Theme**: Red gradient header (warning/important notice)

**What User Sees**:
```
IMPORTANT NOTICE

Dear [Previous User],

This is to inform you that the phone number [PhoneNumber] has been unassigned from your account.

Phone Details:
- Phone Number: [PhoneNumber]
- Phone Type: [PhoneType]
- Previous Line Type: [LineType]
- Unassigned Date: [UnassignedDate]

Reason for Unassignment:
This phone number has been reassigned to John Doe (Index: 8817861)

What This Means:
- You no longer have access to this phone number
- You are no longer responsible for calls made on this number
- The phone assignment history has been preserved for records
- If you had other active phones, they remain assigned to you

[View Your Active Phones Button]
```

### 2. Phone Number Assigned Email (New User)
**Template Code**: `PHONE_NUMBER_ASSIGNED`

**Key Placeholders**:
- `{{FirstName}}`, `{{LastName}}` - New user's name
- `{{PhoneNumber}}` - The assigned phone
- `{{PhoneType}}` - Type of phone
- `{{LineType}}` - Line type (Primary or Secondary)
- `{{LineTypeBadgeColor}}` - Badge background color
- `{{LineTypeTextColor}}` - Badge text color
- `{{IndexNumber}}` - New user's index number
- `{{AssignedDate}}` - When the assignment occurred
- `{{UserPhonesUrl}}` - Link to view all phones

**Visual Theme**: Blue gradient header (positive action)

**What User Sees**:
```
PHONE NUMBER ASSIGNED

Dear [New User],

A phone number has been assigned to your account in the UNON E-Billing System.

Phone Details:
- Phone Number: [PhoneNumber]
- Phone Type: [PhoneType]
- Line Type: [Primary/Secondary Badge]
- Index Number: [IndexNumber]
- Assigned Date: [AssignedDate]

Your Responsibilities:
- You are now responsible for all calls made on this number
- Please review your call logs regularly
- Report any unauthorized usage immediately
- This phone is assigned for official UNON business only

[View All Your Phones Button]
```

---

## Complete Reassignment Workflow

### Example Scenario
**Admin reassigns phone `+254712345678` from Alice to Bob**

#### What Happens:

1. **Database Updates**:
   - Alice's UserPhone record: `IsActive = false`, `UnassignedDate = Now`
   - If phone was Alice's primary: `Alice.OfficialMobileNumber = null`
   - Bob's UserPhone record: Created/Reactivated with `IsActive = true`
   - If assigned as primary to Bob: `Bob.OfficialMobileNumber = +254712345678`

2. **Email to Alice** (Previous Owner):
   ```
   From: noreply@un.org
   To: alice@example.com
   Subject: Phone Number Unassigned from Your Account - UNON E-Billing

   Dear Alice,

   The phone number +254712345678 has been unassigned from your account.

   Reason: This phone number has been reassigned to Bob Smith (Index: 8817861)

   You no longer have access to this phone number.
   ```

3. **Email to Bob** (New Owner):
   ```
   From: noreply@un.org
   To: bob@example.com
   Subject: Phone Number Assigned to Your Account - UNON E-Billing

   Dear Bob,

   A phone number has been assigned to your account:

   Phone Number: +254712345678
   Line Type: Primary

   You are now responsible for all calls made on this number.
   ```

4. **History Logs Created**:
   - UserPhoneHistory entry for Alice: "Phone unassigned and reassigned to Bob Smith"
   - UserPhoneHistory entry for Bob: "Phone assigned (reassigned from Alice Johnson)"

5. **Email Logs Created**:
   - EmailLog entry for Alice's unassignment notification
   - EmailLog entry for Bob's assignment notification
   - Both logged with Status = "Sent" (if successful)

---

## Testing the Implementation

### Test Scenario: Reassign Phone Number

#### Prerequisites:
1. Two E-Bill users with email addresses:
   - User A (e.g., Index: 1234567) - Currently has phone assigned
   - User B (e.g., Index: 8817861) - Will receive the phone
2. Both users must have ApplicationUser accounts with valid emails
3. Email configuration must be active
4. Email templates must be installed

#### Test Steps:

1. **Navigate to User A's Phone Management**:
   - URL: `http://localhost:5041/Admin/UserPhones?indexNumber=1234567`
   - Verify User A has phone `+254712345678` assigned

2. **Initiate Reassignment to User B**:
   - URL: `http://localhost:5041/Admin/UserPhones?indexNumber=8817861`
   - Click "Assign Phone"
   - Enter the same phone number: `+254712345678`
   - Fill in other details (Phone Type, Line Type, etc.)
   - Click "Assign Phone"

3. **Confirm Force Reassignment**:
   - System detects phone is already assigned
   - Confirm "Force Reassign" when prompted

4. **Verify Database Changes**:
   ```sql
   -- Check User A's phone is now inactive
   SELECT * FROM UserPhones
   WHERE IndexNumber = '1234567' AND PhoneNumber = '+254712345678'
   -- Should show: IsActive = 0, UnassignedDate = [recent timestamp]

   -- Check User B now has the phone
   SELECT * FROM UserPhones
   WHERE IndexNumber = '8817861' AND PhoneNumber = '+254712345678'
   -- Should show: IsActive = 1, AssignedDate = [recent timestamp]
   ```

5. **Verify Email to User A (Previous Owner)**:
   - Navigate to: `http://localhost:5041/Admin/EmailLogs`
   - Find email to User A's email address
   - Subject: "Phone Number Unassigned from Your Account"
   - Status: "Sent"
   - Click to view email
   - Verify reason states: "This phone number has been reassigned to [User B Name] (Index: 8817861)"
   - Verify phone number is shown correctly
   - Verify unassignment date is shown

6. **Verify Email to User B (New Owner)**:
   - In EmailLogs, find email to User B's email address
   - Subject: "Phone Number Assigned to Your Account"
   - Status: "Sent"
   - Click to view email
   - Verify phone number is shown correctly
   - Verify line type badge color is correct
   - Verify assignment date is shown
   - Verify "View Your Phones" link is present

7. **Check History Logs**:
   - Navigate to User A's phones page
   - View history for the reassigned phone
   - Should show unassignment entry

   - Navigate to User B's phones page
   - View history for the newly assigned phone
   - Should show assignment entry

8. **Verify User A Can See Unassignment**:
   - Navigate to: `http://localhost:5041/Admin/UserPhones?indexNumber=1234567`
   - Phone should be listed as "Inactive" or not shown in active phones
   - History should show it was unassigned

9. **Verify User B Can See Assignment**:
   - Navigate to: `http://localhost:5041/Admin/UserPhones?indexNumber=8817861`
   - Phone should be listed as active
   - If assigned as Primary, badge should be green

---

## Email Sending Conditions

### When Unassignment Email is Sent (Previous User)

**Conditions**:
- Previous user must have an `EbillUser` record
- Previous user must have a linked `ApplicationUser` account
- Previous user's `Email` field must not be null or empty
- Email configuration must be active in the system

**If Conditions Not Met**:
- Warning is logged: "Cannot send phone unassigned email: User {IndexNumber} has no email"
- Phone is still unassigned in database
- Email is NOT sent (silent failure with log)
- Process continues normally

### When Assignment Email is Sent (New User)

**Conditions**:
- New user must have an `EbillUser` record
- New user must have a linked `ApplicationUser` account
- New user's `Email` field must not be null or empty
- Email configuration must be active in the system

**If Conditions Not Met**:
- Warning is logged: "Cannot send phone assigned email: User {IndexNumber} has no email"
- Phone is still assigned in database
- Email is NOT sent (silent failure with log)
- Process continues normally

---

## Code Locations

### Main Implementation File
**File**: `Services/UserPhoneService.cs`

#### Key Methods:

1. **AssignPhoneAsync** (Lines 75-270)
   - Main method handling phone assignment and reassignment
   - Line 105-110: Load previous user data with Include/ThenInclude
   - Line 149-153: Send unassignment email to previous user
   - Line 262: Send assignment email to new user

2. **SendPhoneUnassignedEmailAsync** (Lines 560-600)
   - Helper method to send unassignment emails
   - Parameters: `user`, `phone`, `reason` (optional)
   - Uses template: `PHONE_NUMBER_UNASSIGNED`
   - Line 581: Reason placeholder filled with reassignment details

3. **SendPhoneAssignedEmailAsync** (Lines 472-514)
   - Helper method to send assignment emails
   - Parameters: `user`, `phone`
   - Uses template: `PHONE_NUMBER_ASSIGNED`
   - Line 483: Gets badge colors based on line type

---

## Logging

### Log Entries During Reassignment

#### Information Logs:
```
Reassigning phone +254712345678 from user 1234567 to user 8817861
Removed primary phone from user 1234567
Sent phone unassigned email to alice@example.com for phone +254712345678
Phone +254712345678 assigned to user 8817861
Sent phone assigned email to bob@example.com for phone +254712345678
```

#### Warning Logs (if applicable):
```
Cannot send phone unassigned email: User 1234567 has no email
Cannot send phone assigned email: User 8817861 has no email
```

#### Error Logs (if email fails):
```
Failed to send phone unassigned email to alice@example.com
Failed to send phone assigned email to bob@example.com
```

---

## Troubleshooting

### Issue: Previous User Doesn't Receive Unassignment Email

**Check**:
1. Does previous user have email address in EbillUser table?
   ```sql
   SELECT IndexNumber, Email FROM EbillUsers WHERE IndexNumber = '1234567'
   ```
2. Does previous user have ApplicationUser account?
   ```sql
   SELECT eu.IndexNumber, eu.Email, au.Email
   FROM EbillUsers eu
   LEFT JOIN AspNetUsers au ON eu.Email = au.Email
   WHERE eu.IndexNumber = '1234567'
   ```
3. Check application logs for warnings
4. Check EmailLogs table for failed attempts

### Issue: New User Doesn't Receive Assignment Email

**Check**:
1. Does new user have email address?
2. Does new user have ApplicationUser account?
3. Are email templates installed?
   ```sql
   SELECT * FROM EmailTemplates WHERE TemplateCode = 'PHONE_NUMBER_ASSIGNED'
   ```
4. Is email configuration active?
   ```sql
   SELECT * FROM EmailConfigurations WHERE IsActive = 1
   ```

### Issue: Both Emails Fail

**Check**:
1. Email configuration settings (SMTP server, port, credentials)
2. Run verification script: `VERIFY_EMAIL_TEMPLATES.sql`
3. Test email connection from Admin > Email Configuration
4. Check EmailLogs for error messages

### Issue: Wrong User Gets Email

**Check**:
1. Verify Include/ThenInclude is loading correct user data
2. Check application logs for user loading
3. Verify IndexNumber matching is correct

---

## Database Schema References

### Tables Involved:

1. **UserPhones**:
   - `Id` - Primary key
   - `IndexNumber` - Links to user
   - `PhoneNumber` - The actual phone
   - `IsActive` - Active status
   - `UnassignedDate` - When unassigned
   - `IsPrimary` - Primary status
   - `LineType` - Primary/Secondary enum

2. **EbillUsers**:
   - `IndexNumber` - User identifier
   - `FirstName`, `LastName` - User name
   - `Email` - User email address
   - `OfficialMobileNumber` - Primary phone

3. **AspNetUsers** (ApplicationUser):
   - `Email` - Login email
   - Links to EbillUser via Email field

4. **EmailLogs**:
   - `ToEmail` - Recipient
   - `Subject` - Email subject
   - `Body` - Email content
   - `Status` - Sent/Failed
   - `TemplateCode` - Template used
   - `RelatedEntityType` - "UserPhone"
   - `RelatedEntityId` - UserPhone.Id

5. **EmailTemplates**:
   - `TemplateCode` - Unique identifier
   - `Name` - Display name
   - `Subject` - Email subject
   - `HtmlBody` - Email HTML content
   - `IsActive` - Active status

6. **UserPhoneHistory**:
   - Tracks all phone changes
   - Stores old and new values
   - Action type (Assigned, Unassigned, etc.)

---

## Benefits of This Implementation

### For Users:
1. **Transparency**: Both parties are informed of the change
2. **Clarity**: Previous user knows why they lost the phone
3. **Accountability**: New user knows they're now responsible
4. **Record Keeping**: All communications are logged

### For Administrators:
1. **Audit Trail**: Complete email log of all reassignments
2. **Reduced Confusion**: Users can't claim they weren't notified
3. **Professional**: Official UN-branded notifications
4. **Traceable**: Can verify exactly what was communicated and when

### For System:
1. **Automated**: No manual email sending required
2. **Consistent**: Same professional template every time
3. **Reliable**: Error handling with logging
4. **Scalable**: Works for any number of reassignments

---

## Next Steps After Testing

1. **Monitor Email Logs**:
   - Check that both emails are sent for each reassignment
   - Verify delivery status
   - Review any failed emails

2. **User Feedback**:
   - Ask users if emails are clear and informative
   - Check if any information is missing
   - Verify links work correctly

3. **Template Refinement**:
   - Adjust wording based on user feedback
   - Update colors or styling if needed
   - Add additional information if requested

4. **Performance Monitoring**:
   - Ensure reassignments don't slow down significantly
   - Check email sending doesn't cause delays
   - Monitor database query performance

---

## Related Documentation

- **EMAIL_NOT_SENDING_FIX.md** - Troubleshooting email sending issues
- **PHONE_MANAGEMENT_EMAIL_TEMPLATES.md** - Complete email template documentation
- **EMAIL_SYSTEM_DOCUMENTATION.md** - General email system overview
- **VERIFY_EMAIL_TEMPLATES.sql** - Template verification script

---

## Summary

✅ **Implementation Complete**: Both users (previous and new) receive emails during phone reassignment

✅ **Previous User Notification**: Receives unassignment email with reason explaining reassignment

✅ **New User Notification**: Receives assignment email with complete phone details

✅ **Logging**: All email attempts are logged for audit trail

✅ **Error Handling**: Graceful failure with logging if emails can't be sent

✅ **Professional**: Uses official UN-branded email templates

---

## Technical Specifications

**Technology Stack**:
- .NET 8.0
- ASP.NET Core Razor Pages
- Entity Framework Core
- SQL Server

**Dependencies**:
- IEnhancedEmailService
- IHttpContextAccessor
- IUserPhoneHistoryService

**Email Templates Required**:
- PHONE_NUMBER_ASSIGNED
- PHONE_NUMBER_UNASSIGNED

**Database Tables**:
- UserPhones
- EbillUsers
- AspNetUsers
- EmailLogs
- EmailTemplates
- UserPhoneHistory

---

**Implementation Date**: October 23, 2025
**Version**: 1.0
**Status**: ✅ Complete and Ready for Testing
