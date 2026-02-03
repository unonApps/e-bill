# Class of Service Versioning System

## Overview

The Class of Service Versioning system allows you to change allowance amounts (airtime, data, handset) without affecting historical billing calculations. When you need to increase or decrease allowances, the system creates a new version with an effective date, preserving all historical data integrity.

## Problem Solved

**Before Versioning:**
- If you changed a Class of Service allowance from $50 to $75 today, ALL historical bills would be recalculated using the new $75 amount
- Historical reports would be inaccurate
- You couldn't see what allowances were in effect during past billing periods

**After Versioning:**
- Historical bills always use the allowances that were in effect during that specific billing period
- New bills use the current version
- Complete audit trail of all allowance changes
- Accurate historical reporting

## How It Works

### Core Concepts

1. **Effective Dates**: Each version has an `EffectiveFrom` and optional `EffectiveTo` date
2. **Version Number**: Auto-incremented for each new version (V1, V2, V3, etc.)
3. **Parent Linking**: All versions of the same Class of Service are linked via `ParentClassOfServiceId`
4. **Current Version**: The version with no end date or end date in the future

### Example Scenario

Let's say you have "Class A - Professional Staff" with $50 airtime allowance:

**January 2025:**
- Version 1: Effective From Jan 1, 2025 | Airtime: $50 | Status: Active

**March 2025 - You want to increase to $75 starting April 1:**
- Version 1: Effective From Jan 1, 2025 | Effective To: Mar 31, 2025 | Airtime: $50
- Version 2: Effective From Apr 1, 2025 | Effective To: NULL | Airtime: $75

**How Bills Are Calculated:**
- January bill uses Version 1 ($50)
- February bill uses Version 1 ($50)
- March bill uses Version 1 ($50)
- April bill uses Version 2 ($75)
- May bill uses Version 2 ($75)

## Database Schema

### New Fields Added to `ClassOfServices` Table

```sql
EffectiveFrom DATETIME2 NOT NULL DEFAULT GETUTCDATE()
EffectiveTo DATETIME2 NULL
Version INT NOT NULL DEFAULT 1
ParentClassOfServiceId INT NULL  -- FK to original ClassOfService
```

### Migration

The migration `AddClassOfServiceVersioning` adds these fields. Run it with:

```bash
dotnet ef database update
```

## API / Service Usage

### ClassOfServiceVersioningService

This service handles all versioning operations:

#### 1. Get Effective Version for a Specific Date

```csharp
// Get the version that was effective on a specific date (e.g., during a billing period)
var effectiveVersion = await _versioningService.GetEffectiveVersionAsync(
    classOfServiceId: 5,
    effectiveDate: new DateTime(2025, 3, 15)  // March 15, 2025
);

// Use the effective version's allowances
decimal allowance = effectiveVersion?.AirtimeAllowanceAmount ?? 0;
```

#### 2. Get Current Version

```csharp
// Get the currently active version
var currentVersion = await _versioningService.GetCurrentVersionAsync(classOfServiceId: 5);
```

#### 3. Create New Version (Increase Allowance)

```csharp
// Example: Increase airtime allowance from $50 to $75 effective April 1, 2025
var newVersion = await _versioningService.CreateNewVersionAsync(
    currentVersionId: 5,  // ID of the current version
    effectiveFrom: new DateTime(2025, 4, 1),  // When the new version becomes effective
    updatedValues: (cos) =>
    {
        // Update the fields you want to change
        cos.AirtimeAllowanceAmount = 75.00m;
        cos.AirtimeAllowance = "$75";
    }
);

// The old version is automatically end-dated to March 31, 2025
// The new version starts April 1, 2025
```

#### 4. Get Version History

```csharp
// Get all historical versions
var history = await _versioningService.GetVersionHistoryAsync(classOfServiceId: 5);

foreach (var version in history)
{
    Console.WriteLine($"Version {version.Version}");
    Console.WriteLine($"  Effective: {version.EffectiveFrom} - {version.EffectiveTo?.ToString() ?? "Current"}");
    Console.WriteLine($"  Airtime: ${version.AirtimeAllowanceAmount}");
    Console.WriteLine($"  Current: {version.IsCurrent}");
}
```

#### 5. Get All Versions

```csharp
// Get all versions of a Class of Service
var allVersions = await _versioningService.GetAllVersionsAsync(classOfServiceId: 5);
```

## Billing Integration

The `ClassOfServiceCalculationService` has been updated to use effective versions:

### Before (Old Behavior - Broken)
```csharp
// Always used the current ClassOfService, regardless of billing period
var userPhone = await _context.UserPhones
    .Include(up => up.ClassOfService)
    .FirstOrDefaultAsync(up => up.IndexNumber == indexNumber);

var allowance = userPhone?.ClassOfService?.AirtimeAllowanceAmount;
// ❌ This would use TODAY's allowance for HISTORICAL bills
```

### After (New Behavior - Correct)
```csharp
// Gets the version that was effective during the billing period
var billingPeriodDate = new DateTime(year, month, 1);
var effectiveVersion = await _versioningService.GetEffectiveVersionAsync(
    userPhone.ClassOfServiceId.Value,
    billingPeriodDate
);

var allowance = effectiveVersion?.AirtimeAllowanceAmount;
// ✅ Uses the correct historical allowance
```

This is automatically handled in:
- `GetOverageReportAsync()` - Overage reports use historical allowances
- Future methods can use the same pattern

## UI Integration (Future Enhancement)

To fully support versioning in the UI, you can add:

1. **Version History View**: Show all versions with effective dates
2. **Create New Version Button**: Instead of "Edit", use "Create New Version"
3. **Preview Impact**: Show which users will be affected by the new version
4. **Effective Date Picker**: Let admins choose when the new version starts

### Example UI Flow

```
Current Version: V2 | Effective: Apr 1, 2025 - Current
Airtime: $75 | Data: $50 | Handset: $500

[View History] [Create New Version]

When "Create New Version" is clicked:
┌─────────────────────────────────────┐
│ Create New Version                   │
├─────────────────────────────────────┤
│ Effective From: [May 1, 2025]       │
│                                      │
│ Airtime Allowance:                  │
│   Display: [Unlimited]               │
│   Amount:  [$100.00]                 │
│                                      │
│ [Preview Impact] [Save New Version]  │
└─────────────────────────────────────┘
```

## Best Practices

### When to Create a New Version

✅ **DO** create a new version when:
- Changing allowance amounts (airtime, data, handset)
- Policy changes that affect billing calculations
- Annual allowance updates
- Grade reclassifications

❌ **DON'T** create a new version for:
- Fixing typos in service description
- Updating remarks/notes
- Changing status (Active/Inactive) for the current version

### Effective Date Guidelines

1. **Future Effective Dates**: Always recommended
   - Set `EffectiveFrom` to the start of next month
   - Gives users advance notice
   - Cleaner month boundaries

2. **Same-Day Changes**: Possible but not recommended
   - Only if absolutely necessary (emergency policy change)
   - May cause confusion for current month calculations

3. **Retroactive Changes**: **NEVER DO THIS**
   - Don't set `EffectiveFrom` in the past
   - Defeats the purpose of versioning
   - Will corrupt historical data

### Version Naming Convention

The system auto-increments version numbers:
- V1, V2, V3, etc.

You can add notes in the database or UI about what changed:
```csharp
// Example: Add a change log field
cos.ChangeReason = "Annual allowance increase per HR policy 2025-03";
```

## Migration Guide for Existing Data

If you have existing ClassOfService records without versioning:

1. **Run the migration** - Adds versioning fields with defaults:
   - `EffectiveFrom`: Current date
   - `Version`: 1
   - `EffectiveTo`: NULL (meaning current/active)

2. **All existing records become Version 1** automatically

3. **Future changes** will create Version 2, 3, etc.

## Testing Scenarios

### Test 1: Historical Bill Accuracy
```csharp
// Setup: Class of Service had $50 in January, increased to $75 in March
// Test: Generate January bill
var report = await _calculationService.GetOverageReportAsync(
    indexNumber: "12345",
    month: 1,  // January
    year: 2025
);

Assert.Equal(50.00m, report.AllowanceLimit);  // Should use $50, not $75
```

### Test 2: Current Bill Uses Latest Version
```csharp
// Test: Generate current month bill
var report = await _calculationService.GetOverageReportAsync(
    indexNumber: "12345",
    month: DateTime.UtcNow.Month,
    year: DateTime.UtcNow.Year
);

Assert.Equal(75.00m, report.AllowanceLimit);  // Should use current $75
```

### Test 3: Create New Version
```csharp
var newVersion = await _versioningService.CreateNewVersionAsync(
    currentVersionId: 1,
    effectiveFrom: new DateTime(2025, 4, 1),
    updatedValues: (cos) => { cos.AirtimeAllowanceAmount = 100.00m; }
);

Assert.Equal(2, newVersion.Version);
Assert.Equal(100.00m, newVersion.AirtimeAllowanceAmount);
```

## Troubleshooting

### Issue: Historical bills showing wrong allowances

**Cause**: Not using the versioning service
**Solution**: Update your code to use `GetEffectiveVersionAsync()` instead of direct ClassOfService lookup

### Issue: Multiple "current" versions

**Cause**: EffectiveTo dates not set correctly
**Solution**: Use `CreateNewVersionAsync()` which automatically end-dates the previous version

### Issue: Gap in effective dates

**Cause**: Effective dates don't align properly
**Solution**: Always use `CreateNewVersionAsync()` - it handles date continuity

## Summary

✅ **Benefits:**
- Historical data integrity
- Accurate billing calculations
- Complete audit trail
- Easy to understand and maintain

✅ **Key Points:**
- Use `CreateNewVersionAsync()` to create new versions
- Always set future effective dates (start of next month)
- Historical bills automatically use the correct version
- All versions are preserved forever

✅ **Next Steps:**
1. Run the migration: `dotnet ef database update`
2. Test with a sample Class of Service
3. Create UI for version management (optional)
4. Train users on the new versioning workflow
