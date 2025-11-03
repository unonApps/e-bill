# Phone Type Change Email Notification - Complete Fix

## Problem Identified

User reported that when changing a phone's line type (Primary/Secondary/Reserved), **NO email was being sent** to notify the user of the change.

### Root Causes Found

1. **Issue 1: "Set as Primary" button only sends email to promoted phone**
   - Location: `Services/UserPhoneService.cs` - `SetPrimaryPhoneAsync` method
   - When a user clicks "Set as Primary" on a secondary phone:
     - ✅ Email sent to the phone being promoted to Primary
     - ❌ **NO email sent** to phones demoted from Primary to Secondary

2. **Issue 2: Editing LineType directly sends NO email**
   - Location: `Pages/Admin/UserPhones.cshtml.cs` - `OnPostEditPhoneAsync` method
   - When admin edits a phone and changes LineType:
     - ✅ History logged
     - ✅ Email sent for Status changes
     - ✅ Email sent for Phone Number changes
     - ❌ **NO email sent for LineType changes**

3. **Issue 3: Reserved line type not supported**
   - The system has THREE line types: Primary, Secondary, **Reserved**
   - Email templates and badge colors only supported Primary and Secondary
   - Reserved line type had no description or proper styling

---

## What Was Fixed

### Fix 1: Added Email for Phones Demoted to Secondary

**File**: `Services/UserPhoneService.cs`
**Method**: `SetPrimaryPhoneAsync` (lines 354-432)

**Changes**:
1. Moved user loading earlier (lines 364-367) to use for all email notifications
2. Added email notification when phones are demoted from Primary to Secondary (lines 393-398)
3. Added logging for better tracking (lines 397, 428)

**Before**:
```csharp
// Update user's primary phone
var user = await _context.EbillUsers
    .Include(u => u.ApplicationUser)
    .FirstOrDefaultAsync(u => u.IndexNumber == phone.IndexNumber);
if (user != null)
{
    user.OfficialMobileNumber = phone.PhoneNumber;
}

await _context.SaveChangesAsync();

// Send email notification for phone type change
if (user != null)
{
    await SendPhoneTypeChangedEmailAsync(user, phone.PhoneNumber, LineType.Secondary, LineType.Primary);
}
```

**After**:
```csharp
// Load user first to use for email notifications
var user = await _context.EbillUsers
    .Include(u => u.ApplicationUser)
    .FirstOrDefaultAsync(u => u.IndexNumber == phone.IndexNumber);

// Remove primary from other phones and set their LineType to Secondary
var otherPhones = await _context.UserPhones
    .Where(up => up.IndexNumber == phone.IndexNumber &&
                up.Id != phone.Id &&
                up.IsPrimary)
    .ToListAsync();

foreach (var otherPhone in otherPhones)
{
    otherPhone.IsPrimary = false;
    otherPhone.LineType = LineType.Secondary;

    // Add history...

    // Send email notification for phone being demoted from Primary to Secondary
    if (user != null)
    {
        await SendPhoneTypeChangedEmailAsync(user, otherPhone.PhoneNumber, LineType.Primary, LineType.Secondary);
        _logger.LogInformation($"Sent phone type changed email for {otherPhone.PhoneNumber} (demoted to Secondary)");
    }
}

// Set this as primary...
// Update user's primary phone...

await _context.SaveChangesAsync();

// Send email notification for phone type change to Primary
if (user != null)
{
    await SendPhoneTypeChangedEmailAsync(user, phone.PhoneNumber, LineType.Secondary, LineType.Primary);
    _logger.LogInformation($"Sent phone type changed email for {phone.PhoneNumber} (promoted to Primary)");
}
```

### Fix 2: Added Email for Direct LineType Edits

**File**: `Pages/Admin/UserPhones.cshtml.cs`

**Step 1: Added Required Services** (lines 22-23, 32-33, 41-42)
```csharp
private readonly IEnhancedEmailService _enhancedEmailService;
private readonly IHttpContextAccessor _httpContextAccessor;

public UserPhonesModel(
    // ... existing parameters
    IEnhancedEmailService enhancedEmailService,
    IHttpContextAccessor httpContextAccessor)
{
    // ... existing assignments
    _enhancedEmailService = enhancedEmailService;
    _httpContextAccessor = httpContextAccessor;
}
```

**Step 2: Added Email Notification Logic** (lines 657-673)
```csharp
// If LineType changed, send email notification
if (changedFields.Contains("LineType") && ebillUser != null)
{
    // Load ApplicationUser for email
    var userWithEmail = await _context.EbillUsers
        .Include(u => u.ApplicationUser)
        .FirstOrDefaultAsync(u => u.IndexNumber == phone.IndexNumber);

    if (userWithEmail?.ApplicationUser != null && !string.IsNullOrEmpty(userWithEmail.Email))
    {
        var oldLineType = (LineType)Enum.Parse(typeof(LineType), oldValues["LineType"].ToString()!);
        var newLineType = EditInput.LineType;

        await SendPhoneTypeChangedEmailAsync(userWithEmail, phone.PhoneNumber, oldLineType, newLineType);
        _logger.LogInformation($"Sent phone type changed email for {phone.PhoneNumber}: {oldLineType} → {newLineType}");
    }
}
```

**Step 3: Added Helper Methods** (lines 730-824)
```csharp
private async Task SendPhoneTypeChangedEmailAsync(EbillUser user, string phoneNumber, LineType oldLineType, LineType newLineType)
{
    // Email sending logic with PHONE_TYPE_CHANGED template
}

private string GetUserPhonesUrl(string indexNumber)
{
    // Generate URL for phone management page
}

private (string badgeColor, string textColor) GetLineTypeBadgeColors(LineType lineType)
{
    // Returns colors for Primary, Secondary, Reserved badges
}

private string GetLineTypeDescription(LineType lineType)
{
    // Returns HTML description for each line type
}
```

### Fix 3: Added Support for Reserved Line Type

**File**: `Services/UserPhoneService.cs` (lines 623-661)

**Badge Colors**:
```csharp
private (string badgeColor, string textColor) GetLineTypeBadgeColors(LineType lineType)
{
    return lineType switch
    {
        LineType.Primary => ("#10b981", "#ffffff"), // Green background, white text
        LineType.Secondary => ("#dbeafe", "#1e40af"), // Light blue background, dark blue text
        LineType.Reserved => ("#fef3c7", "#92400e"), // Light yellow background, dark yellow text
        _ => ("#e5e7eb", "#1f2937") // Gray background, dark text
    };
}
```

**Line Type Descriptions**:
```csharp
private string GetLineTypeDescription(LineType lineType)
{
    return lineType switch
    {
        LineType.Primary => @"
            <li>This is now your official primary phone number</li>
            <li>It will be used as your main contact number in the system</li>
            <li>All official communications will reference this number</li>
            <li>You are responsible for all calls made on this number</li>
            <li>This number will appear on your official records and reports</li>",

        LineType.Secondary => @"
            <li>This is a secondary phone number assigned to your account</li>
            <li>It serves as an additional contact line</li>
            <li>You remain responsible for calls made on this number</li>
            <li>This number is for official UNON business use</li>
            <li>Secondary numbers appear in your phone list but are not your primary contact</li>",

        LineType.Reserved => @"
            <li>This phone number has been reserved for your account</li>
            <li>Reserved numbers are held for future assignment or special purposes</li>
            <li>You may have limited or no active usage on this line</li>
            <li>Contact ICTS if you need this number activated</li>
            <li>This status is typically temporary pending activation or assignment</li>",

        _ => "<li>Line type status updated</li>"
    };
}
```

---

## Complete Email Scenarios

### Scenario 1: User Clicks "Set as Primary" Button

**Action**: Admin clicks "Set as Primary" on a Secondary phone

**What Happens**:
1. Phone A (currently Primary) → demoted to Secondary
   - ✅ **Email sent**: "Phone A status changed from Primary to Secondary"
   - Line type badge: Light blue background, dark blue text
2. Phone B (currently Secondary) → promoted to Primary
   - ✅ **Email sent**: "Phone B status changed from Secondary to Primary"
   - Line type badge: Green background, white text

**Both emails go to the same user** (one user owns both phones)

### Scenario 2: Admin Edits Phone and Changes LineType

**Action**: Admin opens "Edit Phone" modal and changes LineType from Primary to Secondary

**What Happens**:
1. Phone's LineType is updated in database
2. History is logged
3. ✅ **Email sent**: "Phone status changed from Primary to Secondary"
4. User receives professional notification with:
   - Before status: Primary
   - After status: Secondary
   - Description of what Secondary means
   - Link to view all phones

### Scenario 3: Admin Changes LineType to Reserved

**Action**: Admin sets a phone's LineType to Reserved

**What Happens**:
1. Phone's LineType is updated to Reserved
2. History is logged
3. ✅ **Email sent**: "Phone status changed to Reserved"
4. User receives email with:
   - Yellow badge for Reserved status
   - Explanation that number is reserved
   - Instructions to contact ICTS for activation

---

## Email Template Used

**Template Code**: `PHONE_TYPE_CHANGED`
**Subject**: "Phone Number Status Updated - UNON E-Billing"

**Placeholders**:
- `{{FirstName}}`, `{{LastName}}` - User's name
- `{{PhoneNumber}}` - The phone number
- `{{OldLineType}}` - Previous line type (Primary/Secondary/Reserved)
- `{{NewLineType}}` - New line type (Primary/Secondary/Reserved)
- `{{LineTypeBadgeColor}}` - Background color for badge
- `{{LineTypeTextColor}}` - Text color for badge
- `{{StatusDescription}}` - HTML description of new status
- `{{IndexNumber}}` - User's index number
- `{{ChangeDate}}` - When the change occurred
- `{{UserPhonesUrl}}` - Link to view all phones

---

## Testing the Fix

### Test 1: Set as Primary

1. Navigate to: `http://localhost:5041/Admin/UserPhones?indexNumber=8817861`
2. User should have at least 2 phones (one Primary, one Secondary)
3. Click "Set as Primary" on the Secondary phone
4. **Verify**:
   - Go to Email Logs
   - Should see **TWO emails**:
     - One for phone being demoted (Primary → Secondary)
     - One for phone being promoted (Secondary → Primary)
   - Both sent to the same user

### Test 2: Edit LineType Directly

1. Navigate to user's phone page
2. Click "Edit" button on a phone
3. Change LineType dropdown from "Primary" to "Secondary"
4. Click "Save"
5. **Verify**:
   - Go to Email Logs
   - Should see **ONE email**: "Phone Number Status Updated"
   - Email shows old status (Primary) and new status (Secondary)

### Test 3: Set to Reserved

1. Navigate to user's phone page
2. Click "Edit" on a phone
3. Change LineType to "Reserved"
4. Click "Save"
5. **Verify**:
   - Email sent with yellow badge
   - Email contains description about Reserved status
   - User instructed to contact ICTS

### Test 4: Verify Email Content

For each email sent:
- ✅ Subject is correct
- ✅ User's name is correct
- ✅ Phone number is correct
- ✅ Old and New line types are correct
- ✅ Badge color matches line type
- ✅ Status description is accurate
- ✅ "View Your Phones" button works
- ✅ Email logs show Status = "Sent"

---

## Badge Color Reference

| Line Type | Badge Background | Text Color | Usage |
|-----------|-----------------|------------|--------|
| Primary | #10b981 (Green) | #ffffff (White) | Official primary phone |
| Secondary | #dbeafe (Light Blue) | #1e40af (Dark Blue) | Additional phone lines |
| Reserved | #fef3c7 (Light Yellow) | #92400e (Dark Yellow) | Reserved/pending phones |

---

## Files Modified

### Services/UserPhoneService.cs
- **Lines 364-367**: Moved user loading earlier
- **Lines 393-398**: Added email for phone demotion
- **Lines 397, 428**: Added logging for email tracking
- **Lines 623-661**: Added support for Reserved line type

### Pages/Admin/UserPhones.cshtml.cs
- **Lines 22-23**: Added IEnhancedEmailService field
- **Lines 32-33**: Added to constructor parameters
- **Lines 41-42**: Assigned in constructor
- **Lines 657-673**: Added email notification logic for LineType changes
- **Lines 730-824**: Added email helper methods

---

## Verification Checklist

After applying the fix:

- [x] Application builds successfully
- [ ] Test "Set as Primary" - both emails sent
- [ ] Test direct LineType edit - email sent
- [ ] Test changing to Reserved - email sent with yellow badge
- [ ] All emails appear in EmailLogs with "Sent" status
- [ ] Email content displays correctly
- [ ] Badge colors match line types
- [ ] Descriptions are accurate for all three line types
- [ ] Links in emails work correctly

---

## Common Issues and Solutions

### Issue: No email sent when clicking "Set as Primary"

**Check**:
1. User has email address: `SELECT Email FROM EbillUsers WHERE IndexNumber = '...'`
2. User has ApplicationUser account
3. Template exists: `SELECT * FROM EmailTemplates WHERE TemplateCode = 'PHONE_TYPE_CHANGED'`
4. Email configuration is active

### Issue: No email sent when editing LineType

**Check**:
1. IEnhancedEmailService properly injected in UserPhonesModel
2. User has email and ApplicationUser account
3. Check application logs for errors
4. Verify changedFields contains "LineType"

### Issue: Wrong badge color or description

**Check**:
1. Verify GetLineTypeBadgeColors returns correct colors for all three types
2. Verify GetLineTypeDescription returns correct HTML for all three types
3. Check that both UserPhoneService.cs and UserPhones.cshtml.cs have updated methods

---

## Summary of Changes

| What | Before | After |
|------|--------|-------|
| Set as Primary | Only promoted phone got email | ✅ Both phones get emails |
| Edit LineType | No email sent | ✅ Email sent with details |
| Reserved support | Not supported | ✅ Full support with yellow badge |
| Email logging | Some scenarios missed | ✅ All scenarios logged |

---

## Next Steps

1. **Restart the application** (if running)
2. **Test all scenarios** listed above
3. **Verify emails** in EmailLogs table
4. **Check user experience** - emails should be clear and helpful
5. **Monitor logs** for any errors

---

## Technical Details

### Email Sending Flow

1. **Detect LineType Change**:
   - SetPrimaryPhoneAsync: Automatic when setting primary
   - OnPostEditPhoneAsync: Detected via changedFields

2. **Load User Data**:
   - Must include ApplicationUser for email
   - Email field must not be null/empty

3. **Generate Email Data**:
   - Old and new line types
   - Badge colors based on new type
   - Description based on new type
   - Dynamic URL generation

4. **Send via EnhancedEmailService**:
   - Template: PHONE_TYPE_CHANGED
   - Async sending
   - Error handling with logging

5. **Create Email Log**:
   - Automatic by EnhancedEmailService
   - Status: Sent/Failed
   - Error message if failed

---

**Date Fixed**: October 23, 2025
**Status**: ✅ Complete and Ready for Testing
**Build Status**: ✅ Successful
**Version**: 1.0
