# Phone Management Email Templates Implementation

## Overview
This document outlines the implementation of professional email templates for phone number management operations in the UNON E-Billing System. These templates automatically notify users when phone numbers are assigned, reassigned, status changes, or unassigned from their accounts.

---

## Features Implemented

### 1. **Phone Number Assigned Email**
- **Template Code**: `PHONE_NUMBER_ASSIGNED`
- **Subject**: "Phone Number Assigned to Your Account - UNON E-Billing"
- **Purpose**: Sent automatically when a new phone number is assigned to a user
- **Trigger**: When admin assigns a phone number via UserPhones management page
- **Features**:
  - Professional notification with blue branding
  - Complete phone details (number, type, line type)
  - Assignment date and index number
  - Explanation of responsibilities
  - Next steps for the user
  - Direct link to view all phone assignments
  - UN SDG rainbow color footer

### 2. **Phone Type Changed Email**
- **Template Code**: `PHONE_TYPE_CHANGED`
- **Subject**: "Phone Number Status Updated - UNON E-Billing"
- **Purpose**: Sent when phone line type changes (Primary ↔ Secondary)
- **Trigger**: When admin changes a phone from secondary to primary or vice versa
- **Features**:
  - Clear before/after status comparison
  - Purple gradient branding for status changes
  - Detailed explanation of new status impact
  - Description of what Primary vs Secondary means
  - Impact on user responsibilities
  - Direct link to view current phone assignments

### 3. **Phone Number Unassigned Email**
- **Template Code**: `PHONE_NUMBER_UNASSIGNED`
- **Subject**: "Phone Number Unassigned from Your Account - UNON E-Billing"
- **Purpose**: Sent when a phone number is removed from a user's account
- **Trigger**: When admin unassigns/deactivates a phone number
- **Features**:
  - Important notice with red branding
  - Clear explanation that number is no longer assigned
  - Previous phone details for reference
  - Unassignment date
  - Optional reason for unassignment
  - Information about historical records
  - Direct link to view remaining active phones

---

## Template Placeholders

### Phone Number Assigned Template
- `{{FirstName}}` - User's first name
- `{{LastName}}` - User's last name
- `{{PhoneNumber}}` - The assigned phone number
- `{{PhoneType}}` - Type of phone (Mobile, Landline, etc.)
- `{{LineType}}` - Line type (Primary, Secondary)
- `{{LineTypeBadgeColor}}` - Background color for line type badge
- `{{LineTypeTextColor}}` - Text color for line type badge
- `{{IndexNumber}}` - User's index number
- `{{AssignedDate}}` - Date and time of assignment
- `{{UserPhonesUrl}}` - Link to view all user's phones

### Phone Type Changed Template
- `{{FirstName}}` - User's first name
- `{{LastName}}` - User's last name
- `{{PhoneNumber}}` - The phone number that changed
- `{{OldLineType}}` - Previous line type status
- `{{NewLineType}}` - New line type status
- `{{LineTypeBadgeColor}}` - Background color for new line type badge
- `{{LineTypeTextColor}}` - Text color for new line type badge
- `{{StatusDescription}}` - HTML description of what the new status means
- `{{IndexNumber}}` - User's index number
- `{{ChangeDate}}` - Date and time of change
- `{{UserPhonesUrl}}` - Link to view all user's phones

### Phone Number Unassigned Template
- `{{FirstName}}` - User's first name
- `{{LastName}}` - User's last name
- `{{PhoneNumber}}` - The unassigned phone number
- `{{PhoneType}}` - Type of phone
- `{{LineType}}` - Previous line type
- `{{IndexNumber}}` - User's index number
- `{{UnassignedDate}}` - Date and time of unassignment
- `{{Reason}}` - Optional reason for unassignment
- `{{UserPhonesUrl}}` - Link to view remaining active phones

---

## Files Created

### 1. HTML Email Templates
- **PhoneNumberAssignedTemplate.html**
  - Full HTML template for phone assignments
  - Blue gradient header theme
  - Mobile-responsive design

- **PhoneTypeChangedTemplate.html**
  - Full HTML template for status changes
  - Purple gradient header theme
  - Before/after comparison display

- **PhoneNumberUnassignedTemplate.html**
  - Full HTML template for phone unassignment
  - Red gradient header theme (warning/important)
  - Clear impact explanation

### 2. SQL Installation Script
- **INSERT_PHONE_MANAGEMENT_EMAIL_TEMPLATES.sql**
  - Inserts all three email templates
  - Automatically removes existing templates (for re-insertion)
  - Category: "Phone Management"
  - All marked as system templates and active by default

---

## Code Changes

### Updated Files

#### 1. **UserPhoneService.cs**
   - **Location**: `/Services/UserPhoneService.cs`
   - **Changes**:
     - Added `IEnhancedEmailService` dependency injection
     - Added `IHttpContextAccessor` dependency injection
     - Created `SendPhoneAssignedEmailAsync()` helper method
     - Created `SendPhoneTypeChangedEmailAsync()` helper method
     - Created `SendPhoneUnassignedEmailAsync()` helper method
     - Created `GetUserPhonesUrl()` helper method
     - Created `GetLineTypeBadgeColors()` helper method
     - Created `GetLineTypeDescription()` helper method
     - Updated `AssignPhoneAsync()` to send email after assignment
     - Updated `SetPrimaryPhoneAsync()` to send email when status changes
     - Updated `UnassignPhoneAsync()` to send email when phone is removed

   **Key Benefits**:
   - Automatic email notifications for all phone changes
   - Centralized template management
   - Easy template updates without code changes
   - Full email tracking and logging
   - Dynamic color coding based on line type

---

## Installation Instructions

### Step 1: Run the SQL Script
Execute the SQL script to insert the email templates into your database:

```bash
sqlcmd -S "MICHUKI\SQLEXPRESS" -d "TABDB" -E -i "INSERT_PHONE_MANAGEMENT_EMAIL_TEMPLATES.sql"
```

### Step 2: Verify Templates
1. Navigate to http://localhost:5041/Admin/EmailTemplates
2. Verify that you see three new templates:
   - **Phone Number Assigned** (Template Code: PHONE_NUMBER_ASSIGNED)
   - **Phone Number Status Updated** (Template Code: PHONE_TYPE_CHANGED)
   - **Phone Number Unassigned** (Template Code: PHONE_NUMBER_UNASSIGNED)
3. All should show as "Active" and "System Template"
4. Category should be "Phone Management"

### Step 3: Test the Implementation

#### Test Phone Assignment:
1. Navigate to http://localhost:5041/Admin/UserPhones?indexNumber={someIndexNumber}
2. Click "Assign Phone"
3. Fill in phone details and assign
4. Check EmailLogs to verify email was sent
5. Review the sent email content

#### Test Phone Type Change:
1. Navigate to user's phone management page
2. Click "Set as Primary" for a secondary phone
3. Check EmailLogs to confirm status change email was sent
4. Review the email showing before/after status

#### Test Phone Unassignment:
1. Navigate to user's phone management page
2. Click "Unassign" for an active phone
3. Check EmailLogs to confirm unassignment email was sent
4. Review the email explaining the change

---

## Email Template Design Features

### Professional Design Elements
1. **Responsive Layout**: Works on desktop and mobile devices
2. **Inline CSS**: Maximum compatibility with email clients
3. **UN Branding**: Official UN SDG rainbow color footer
4. **Color-Coded Headers**:
   - Blue for phone assignments (positive action)
   - Purple for status changes (informational)
   - Red for unassignments (warning/important)
5. **Clear Visual Hierarchy**:
   - Important information highlighted
   - Before/after comparisons for changes
   - Dynamic badge colors for line types

### User Experience Features
1. **Clear Explanations**: What each change means for the user
2. **Responsibility Clarification**: User's duties regarding the phone
3. **Direct Action Links**: View phone assignments button
4. **Contact Information**: ICTS Service Desk prominently displayed
5. **Historical Context**: References to historical records

---

## Line Type Badge Colors

The system automatically assigns colors to line type badges:

| Line Type | Badge Background | Text Color | Usage |
|-----------|-----------------|------------|--------|
| Primary | #10b981 (Green) | #ffffff (White) | Primary phone numbers |
| Secondary | #dbeafe (Light Blue) | #1e40af (Dark Blue) | Secondary phone numbers |

---

## Automation Logic

### When Emails Are Sent

#### 1. Phone Assignment Email
**Triggers**:
- Admin assigns new phone to user
- Admin reactivates previously unassigned phone
- Forced reassignment from one user to another

**Process**:
1. Phone is assigned in database
2. Assignment history is logged
3. `SendPhoneAssignedEmailAsync()` is called
4. Email is sent using `PHONE_NUMBER_ASSIGNED` template
5. Email log is created for audit trail

#### 2. Phone Type Change Email
**Triggers**:
- Admin clicks "Set as Primary" on a secondary phone
- System automatically promotes secondary to primary when primary is unassigned

**Process**:
1. Line type is changed from Secondary to Primary
2. Other phones are demoted to Secondary
3. Change history is logged
4. `SendPhoneTypeChangedEmailAsync()` is called
5. Email is sent using `PHONE_TYPE_CHANGED` template

#### 3. Phone Unassignment Email
**Triggers**:
- Admin clicks "Unassign" on an active phone
- System deactivates phone during forced reassignment

**Process**:
1. Phone is marked as inactive
2. Unassignment date is recorded
3. If primary, another phone may be promoted
4. Unassignment history is logged
5. `SendPhoneUnassignedEmailAsync()` is called
6. Email is sent using `PHONE_NUMBER_UNASSIGNED` template

---

## Security Considerations

### Email Notifications
- Only sent to users with active email addresses
- Only sent to users with ApplicationUser accounts
- Emails logged for complete audit trail
- Template placeholders prevent injection attacks
- URLs dynamically generated from actual request context

### User Privacy
- Phone assignments are internal UNON operations
- Emails only sent to the affected user
- No CC or BCC to other parties
- Historical records maintained for compliance

---

## Troubleshooting

### Email Not Sending
1. **Check user has email**: Verify EbillUser has email address set
2. **Check user has login account**: User must have ApplicationUser record
3. **Check email configuration**: Admin > Email Configuration must be properly set up
4. **Check templates exist**: Verify all three templates are active in database
5. **Check EmailLogs**: Review for error messages
6. **Check application logs**: Look for exceptions in UserPhoneService

### Template Not Found
1. Run SQL script to insert templates
2. Verify template codes match exactly:
   - PHONE_NUMBER_ASSIGNED
   - PHONE_TYPE_CHANGED
   - PHONE_NUMBER_UNASSIGNED
3. Check IsActive = 1 for all templates

### Placeholder Not Replaced
1. Verify placeholder names are exact (case-sensitive)
2. Check GetLineTypeBadgeColors() returns proper colors
3. Check GetLineTypeDescription() returns HTML
4. Review EnhancedEmailService logs

### Wrong User Received Email
1. Check EbillUser association with phone
2. Verify .Include(u => u.ApplicationUser) is present
3. Check phone.IndexNumber matches user

---

## Maintenance and Customization

### Updating Templates
Templates can be updated two ways:

#### Method 1: Through Admin Interface
1. Navigate to http://localhost:5041/Admin/EmailTemplates
2. Find the template to edit
3. Click "Edit"
4. Modify the HtmlBody content
5. Test with Preview function
6. Click "Save"

#### Method 2: Re-run SQL Script
1. Edit HTML template files
2. Regenerate SQL script using provided bash commands
3. Execute updated SQL script
4. Templates will be replaced

### Customization Points
- **Colors**: Update gradient colors in headers
- **Content**: Modify explanatory text
- **Branding**: Add organizational logos
- **Line Type Descriptions**: Update GetLineTypeDescription() method
- **Badge Colors**: Modify GetLineTypeBadgeColors() method
- **Support Contact**: Update ICTS contact information

---

## Future Enhancements

### Potential Additions
1. **Bulk Assignment Notifications**: Summary email for multiple phone changes
2. **Class of Service Notifications**: Alert when COS changes
3. **Phone Expiry Warnings**: Notify before phone assignments expire
4. **Usage Alerts**: Notify users of unusual call patterns
5. **Monthly Phone Summary**: List all active phones monthly
6. **Reassignment Notifications**: Alert previous user when phone is reassigned

### Template Improvements
1. **Multilingual Support**: Templates in multiple languages
2. **Dark Mode Version**: Alternative styling for dark mode
3. **Rich Media**: Embedded phone management tutorials
4. **Interactive Elements**: Quick action buttons
5. **Mobile App Deep Links**: Direct links to mobile app pages

---

## Support

For issues or questions regarding phone management email templates:

**UNON ICTS Service Desk**
- **Email**: ICTS.Servicedesk@un.org
- **Phone**: +254 20 76 21111
- **Hours**: Monday - Friday, 8:00 AM - 6:00 PM

---

## Technical Specifications

### Dependencies
- .NET 8.0
- ASP.NET Core
- Entity Framework Core
- EnhancedEmailService
- IHttpContextAccessor
- UserPhoneHistoryService

### Database Tables
- **EmailTemplates**: Stores template definitions
- **EmailLogs**: Tracks sent emails
- **UserPhones**: Phone assignments
- **EbillUsers**: User records
- **UserPhoneHistory**: Phone change history

### Email Client Compatibility
Tested and optimized for:
- Outlook 2016+
- Gmail (Web & Mobile)
- Apple Mail
- Thunderbird
- Mobile email clients (iOS Mail, Android Gmail)

---

## Implementation Complete

All phone management email templates have been successfully implemented and integrated into the UNON E-Billing System. Users will now receive professional, branded email notifications for:
- ✅ New phone number assignments
- ✅ Phone type/status changes (Primary ↔ Secondary)
- ✅ Phone number unassignments/removals

**Automatic Email Sending**: ✅ Enabled
**Email Logging**: ✅ Enabled
**Audit Trail**: ✅ Complete

**Date Implemented**: October 23, 2025
**Version**: 1.0
**Status**: Production Ready
