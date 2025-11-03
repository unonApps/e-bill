# MyCallLogs.cshtml Restructure Summary

## Date: 2025-11-03

## Problem Solved
The user reported that when clicking the plus sign to expand a group, Level 1 (extension summary) was appearing below Level 2 (detail rows) instead of above. This was because the HTML was being output in the wrong order during the loop.

## Solution: Partial Views Architecture

We broke down the large monolithic MyCallLogs.cshtml file into smaller, maintainable partial views using a **two-pass rendering approach**:

### Pass 1: Data Grouping
- Loop through all call records
- Group by Extension + Month + Year
- Calculate totals for each group
- Store groups in a list

### Pass 2: HTML Rendering
- For each group, output in correct order:
  1. **Extension Summary Row** (Level 1) - Always visible
  2. **Detail Header Row** (Level 2) - Hidden by default
  3. **Detail Data Rows** (Level 2) - Hidden by default

## File Structure

```
Pages/Modules/EBillManagement/CallRecords/
├── MyCallLogs.cshtml                           (Main file - orchestrates rendering)
├── MyCallLogs.cshtml.cs                        (Code-behind - data logic)
├── MyCallLogs_Before_HTMLReorder.cshtml        (Backup before restructure)
├── MyCallLogs_Before_TwoLevel.cshtml           (Earlier backup)
└── Partials/
    ├── _ExtensionSummaryRow.cshtml            (Level 1: Summary row component)
    ├── _DetailHeader.cshtml                    (Level 2: Column headers component)
    └── _DetailRow.cshtml                       (Level 2: Individual call row component)
```

## Component Responsibilities

### 1. _ExtensionSummaryRow.cshtml
**Purpose**: Displays the aggregated extension summary (Level 1)

**Model**: Tuple with:
- `GroupId`: Unique identifier for the group
- `Extension`: Phone extension number
- `MonthName`: Month name (e.g., "October")
- `Year`: Year
- `CallCount`: Total number of calls
- `TotalCostUSD`: Total cost in USD
- `TotalCostKSH`: Total cost in KSH
- `TotalRecovered`: Total recovered amount

**Features**:
- Checkbox for bulk selection
- Plus/minus icon for expand/collapse
- High cost warning (> $50)
- Click handler to toggle details

### 2. _DetailHeader.cshtml
**Purpose**: Displays column headers for detail rows (Level 2)

**Model**: `string` (GroupId)

**Columns**:
- Checkbox
- Expand icon placeholder
- Dialled Number
- Call Type
- Date
- End Time
- Duration
- KSH cost
- USD cost
- Destination
- Status
- Action

**Behavior**: Hidden by default, shown when group is expanded

### 3. _DetailRow.cshtml
**Purpose**: Displays individual call record details (Level 2)

**Model**: Tuple with:
- `Call`: CallRecord object
- `GroupId`: Group identifier
- `UserIndexNumber`: Current user's index number
- `SubmittedCallIds`: Set of submitted call IDs
- `VerificationStatuses`: Dictionary of verification statuses

**Features**:
- Assignment status tracking
- Verification deadline logic
- Supervisor approval handling
- Row locking based on status
- Status badges with tooltips
- Action buttons (Accept/Reject/Verify/View)

**Row Classes**:
- `row-assigned-to-you`: Call assigned to current user
- `row-assigned-out`: Call assigned by current user to someone else
- `row-your-call`: Call belongs to current user

## Main File Logic (MyCallLogs.cshtml)

### First Pass: Grouping
```csharp
foreach (var call in Model.CallRecords)
{
    var groupKey = $"{extension}-{call.CallMonth}-{call.CallYear}";

    if (currentGroupKey != groupKey)
    {
        // Save previous group
        groupedCalls.Add((currentGroupKey, ...));

        // Start new group
        currentGroupKey = groupKey;
        // Reset counters
    }

    // Accumulate totals
    callCount++;
    totalCostUSD += call.CallCostUSD;
    // ... etc
}
```

### Second Pass: Rendering
```csharp
foreach (var group in groupedCalls)
{
    // 1. Extension Summary Row (Level 1)
    @await Html.PartialAsync("Partials/_ExtensionSummaryRow", ...)

    // 2. Detail Header Row (Level 2)
    @await Html.PartialAsync("Partials/_DetailHeader", groupId)

    // 3. Detail Data Rows (Level 2)
    foreach (var call in group.Calls)
    {
        @await Html.PartialAsync("Partials/_DetailRow", ...)
    }
}
```

## Benefits

### 1. **Correct HTML Order**
- Summary row appears first (Level 1)
- Header and details appear below when expanded (Level 2)
- No more "upside down" rendering issue

### 2. **Maintainability**
- Each component has a single responsibility
- Easier to find and fix bugs
- Cleaner, more readable code

### 3. **Reusability**
- Partials can be reused in other pages if needed
- Components are self-contained

### 4. **Performance**
- Two-pass approach is efficient
- Grouping happens once in memory
- HTML rendering is straightforward

### 5. **Testability**
- Each partial can be tested independently
- Logic is separated from presentation

## JavaScript Toggle Function

```javascript
function toggleCallDetails(groupId) {
    const detailRows = document.querySelectorAll(`tr.call-detail-row[data-group="${groupId}"]`);
    const toggle = document.getElementById('toggle-' + groupId);

    if (detailRows.length > 0) {
        const isHidden = detailRows[0].style.display === 'none';

        detailRows.forEach(row => {
            row.style.display = isHidden ? '' : 'none';
        });

        if (isHidden) {
            toggle.classList.remove('bi-plus-circle');
            toggle.classList.add('bi-dash-circle');
        } else {
            toggle.classList.remove('bi-dash-circle');
            toggle.classList.add('bi-plus-circle');
        }
    }
}
```

## Testing Steps

1. Run the application: `dotnet run`
2. Navigate to: `/Modules/EBillManagement/CallRecords/MyCallLogs`
3. Verify:
   - Extension summary rows are visible (Level 1)
   - Click plus icon (⊕) to expand a group
   - Summary row stays at top
   - Column headers appear immediately below summary
   - Detail rows appear below headers
   - Icon changes to minus (⊖)
   - Click again to collapse
   - All existing functionality preserved

## Build Status

✅ Build successful with 0 errors
⚠️ 129 warnings (pre-existing, unrelated to this change)

## Backups Created

- `MyCallLogs_Before_HTMLReorder.cshtml` - Before breaking into partials
- `MyCallLogs_Before_TwoLevel.cshtml` - Before two-level structure implementation

## Next Steps

If you need to make changes:
- **To modify summary row appearance**: Edit `_ExtensionSummaryRow.cshtml`
- **To change detail columns**: Edit `_DetailHeader.cshtml`
- **To adjust call row logic**: Edit `_DetailRow.cshtml`
- **To change grouping logic**: Edit main `MyCallLogs.cshtml` (first pass)
- **To modify rendering order**: Edit main `MyCallLogs.cshtml` (second pass)
