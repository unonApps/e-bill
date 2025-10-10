# CallLogs Page Redesign Guide

## Overview
This document outlines the comprehensive redesign applied to the CallLogs page, following the CallLogStaging design system for consistency across the application.

## Design System Components

### 1. **Color Palette**
- **Primary**: `#009EDB` (UN Blue)
- **Success**: `#10b981` → `#34d399`
- **Warning**: `#f59e0b` → `#fbbf24`
- **Danger**: `#ef4444` → `#f87171`
- **Neutral**: `#f3f4f6`, `#e5e7eb`, `#9ca3af`

### 2. **Statistics Grid**
```css
.stats-grid {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(240px, 1fr));
    gap: 1.25rem;
}
```

**Features:**
- Auto-responsive grid layout
- Gradient top border (4px)
- Hover animation (translateY)
- Icon with gradient background
- Large number display

### 3. **Filter Section**
**Structure:**
- Collapsible container
- Grid layout for filter inputs
- Custom styled inputs with focus states
- Clear filters button
- Auto-submit on change

**Key Classes:**
- `.filter-section` - Main container
- `.filter-grid` - Grid layout
- `.filter-input`, `.filter-select` - Form controls
- `.btn-clear-filters` - Reset button

### 4. **Quick Filter Tabs**
```html
<div class="quick-filter-tabs">
    <button class="quick-filter-btn active">All Records</button>
    <button class="quick-filter-btn">Linked Only</button>
    <button class="quick-filter-btn">Unlinked Only</button>
</div>
```

### 5. **Table Design**
**Features:**
- Clean header with `#f8f9fa` background
- Uppercase headers (12px, letter-spacing: 0.5px)
- Hover effects on rows
- Action buttons with subtle backgrounds

### 6. **Pagination Component**
**Structure:**
```html
<!-- First (««) | Previous («) | 1 | 2 | 3 | ... | 10 | Next (») | Last (»») -->
```

**Features:**
- Full navigation (First, Previous, Page Numbers, Next, Last)
- Ellipsis for large page ranges
- Page size selector
- Records count display

### 7. **Status Badges**
```css
.status-badge {
    padding: 0.25rem 0.75rem;
    border-radius: 9999px;
    font-size: 0.75rem;
    font-weight: 600;
    text-transform: uppercase;
}
```

## Backend Implementation

### 1. **Pagination Properties**
```csharp
[BindProperty(SupportsGet = true)]
public int PageNumber { get; set; } = 1;

[BindProperty(SupportsGet = true)]
public int PageSize { get; set; } = 25;

// Helper properties
public bool HasPreviousPage => PageNumber > 1;
public bool HasNextPage => /* logic */;
public int TotalPages => /* calculation */;
```

### 2. **Filter Properties**
```csharp
[BindProperty(SupportsGet = true)]
public string? SearchTerm { get; set; }

[BindProperty(SupportsGet = true)]
public string? FilterType { get; set; }

[BindProperty(SupportsGet = true)]
public DateTime? StartDate { get; set; }

[BindProperty(SupportsGet = true)]
public DateTime? EndDate { get; set; }
```

### 3. **Data Loading with Filters**
```csharp
var query = _context.CallLogs.AsQueryable();

// Apply filters
if (!string.IsNullOrWhiteSpace(SearchTerm))
{
    query = query.Where(/* search logic */);
}

if (FilterType == "linked")
{
    query = query.Where(c => c.EbillUserId.HasValue);
}

// Apply pagination
var totalCount = await query.CountAsync();
var items = await query
    .Skip((PageNumber - 1) * PageSize)
    .Take(PageSize)
    .ToListAsync();
```

## JavaScript Features

### 1. **Auto-Submit Filters**
```javascript
document.querySelectorAll('.filter-select, .filter-input').forEach(element => {
    element.addEventListener('change', function() {
        setTimeout(() => {
            this.closest('form').submit();
        }, 500);
    });
});
```

### 2. **Page Size Handler**
```javascript
document.getElementById('pageSizeSelect').addEventListener('change', function() {
    const url = new URL(window.location);
    url.searchParams.set('PageSize', this.value);
    url.searchParams.set('PageNumber', '1');
    window.location.href = url.toString();
});
```

### 3. **Quick Filter Function**
```javascript
function applyQuickFilter(filterType) {
    const url = new URL(window.location);
    url.searchParams.set('FilterType', filterType);
    url.searchParams.set('PageNumber', '1');
    window.location.href = url.toString();
}
```

## Applying to Other Pages

### Step 1: Copy CSS Styles
Copy the entire `<style>` section from CallLogs_New.cshtml to your page.

### Step 2: Add Statistics Grid
```html
<div class="stats-grid">
    <div class="stat-card">
        <div class="stat-content">
            <div class="stat-icon stat-icon-primary">
                <i class="bi bi-icon"></i>
            </div>
            <div class="stat-details">
                <div class="stat-number">123</div>
                <div class="stat-label">Label</div>
            </div>
        </div>
    </div>
</div>
```

### Step 3: Implement Filter Section
```html
<div class="collapse" id="filterCollapse">
    <div class="filter-section">
        <form method="get">
            <div class="filter-grid">
                <!-- Add filter inputs -->
            </div>
        </form>
    </div>
</div>
```

### Step 4: Add Pagination
```csharp
// In your PageModel
[BindProperty(SupportsGet = true)]
public int PageNumber { get; set; } = 1;

[BindProperty(SupportsGet = true)]
public int PageSize { get; set; } = 25;

// In OnGetAsync
var pagedData = await query
    .Skip((PageNumber - 1) * PageSize)
    .Take(PageSize)
    .ToListAsync();
```

### Step 5: Table Structure
```html
<div class="table-container">
    <div class="table-header">
        <h5>Table Title</h5>
        <select id="pageSizeSelect">
            <option value="25">25 per page</option>
        </select>
    </div>
    <div class="table-responsive">
        <table class="table table-hover">
            <!-- Table content -->
        </table>
    </div>
    <div class="pagination-container">
        <!-- Pagination controls -->
    </div>
</div>
```

## Key Features

1. **Responsive Design**: All components use responsive grid layouts
2. **Accessibility**: Proper ARIA labels and semantic HTML
3. **Performance**: Server-side pagination for large datasets
4. **User Experience**: Auto-submit filters, quick filters, clear visual hierarchy
5. **Consistency**: Unified color scheme and spacing system

## Files Modified

1. **CallLogs_New.cshtml** - Complete redesigned view
2. **CallLogs.cshtml.cs** - Added pagination properties
3. **CALLLOGS_REDESIGN_GUIDE.md** - This documentation

## Migration Checklist

- [ ] Backup existing page
- [ ] Copy CSS styles
- [ ] Add statistics grid
- [ ] Implement filter section
- [ ] Add quick filters
- [ ] Update table structure
- [ ] Add pagination
- [ ] Update backend properties
- [ ] Test all filters
- [ ] Test pagination
- [ ] Test responsive design
- [ ] Deploy changes

## Notes

- The design system uses Bootstrap 5 utilities where appropriate
- All icons use Bootstrap Icons library
- Colors follow UN branding guidelines
- The system is designed to handle millions of records efficiently with server-side pagination