# GUID Migration Guide for TAB Application

## Overview
This guide outlines the migration from sequential integer IDs to GUIDs for improved security and preventing enumeration attacks.

## Why Use GUIDs?

### Security Benefits:
1. **Non-Sequential**: Cannot predict next/previous IDs
2. **Unguessable**: 2^128 possible values
3. **No Information Disclosure**: Doesn't reveal business metrics
4. **Prevents Enumeration**: Cannot iterate through valid IDs

### Current Risks with Integer IDs:
- URLs like `/Admin/EbillUsers/Edit/123` expose sequential IDs
- Attackers can enumerate: `/Edit/124`, `/Edit/125`, etc.
- Business information leakage (user count, growth rate)

## Migration Strategy

### Phase 1: Add GUID Columns (Non-Breaking)
1. Add `PublicId` column to all tables
2. Keep existing `Id` columns for internal relations
3. Generate GUIDs for existing records

### Phase 2: Update Application Code
1. Use `PublicId` in all URLs and APIs
2. Keep `Id` for database relationships
3. Update queries to lookup by `PublicId`

### Phase 3: Update UI and Controllers
1. Change routes to use GUIDs
2. Update forms to submit GUIDs
3. Validate GUID format in controllers

## Implementation Steps

### Step 1: Run Database Migration
```bash
# Run the SQL script to add GUID columns
sqlcmd -S localhost -d TABDB -i add-guid-columns-migration.sql
```

### Step 2: Create EF Core Migration
```bash
# Generate migration
dotnet ef migrations add AddPublicIdToAllTables

# Apply migration
dotnet ef database update
```

### Step 3: Register GuidService
```csharp
// In Program.cs
builder.Services.AddScoped<IGuidService, GuidService>();
```

### Step 4: Update Controllers
```csharp
// Before (using int ID)
public async Task<IActionResult> OnGetEditAsync(int id)
{
    var user = await _context.EbillUsers.FindAsync(id);
}

// After (using GUID)
public async Task<IActionResult> OnGetEditAsync(Guid publicId)
{
    var user = await _guidService.GetEbillUserByPublicIdAsync(publicId);
}
```

### Step 5: Update Views
```html
<!-- Before -->
<a href="/Admin/EbillUsers/Edit/@user.Id">Edit</a>

<!-- After -->
<a href="/Admin/EbillUsers/Edit/@user.PublicId">Edit</a>
```

### Step 6: Update URLs
```
# Before
/Admin/EbillUsers/Edit/123
/Admin/EbillUsers/Delete/456

# After
/Admin/EbillUsers/Edit/f47ac10b-58cc-4372-a567-0e02b2c3d479
/Admin/EbillUsers/Delete/6ba7b810-9dad-11d1-80b4-00c04fd430c8
```

## Database Schema Changes

### Example: EbillUsers Table
```sql
-- Before
Id (int, PK)
FirstName (varchar)
LastName (varchar)

-- After
Id (int, PK) -- Keep for relationships
PublicId (uniqueidentifier, UNIQUE) -- Use in URLs
FirstName (varchar)
LastName (varchar)
```

## Code Examples

### Model with GUID
```csharp
public class EbillUser
{
    public int Id { get; set; } // Internal use only
    public Guid PublicId { get; set; } = Guid.NewGuid(); // Public facing
    public string FirstName { get; set; }
    // ... other properties
}
```

### Controller Action
```csharp
[HttpGet("edit/{publicId:guid}")]
public async Task<IActionResult> Edit(Guid publicId)
{
    var user = await _context.EbillUsers
        .FirstOrDefaultAsync(u => u.PublicId == publicId);

    if (user == null)
        return NotFound();

    return View(user);
}
```

### Validation
```csharp
// Validate GUID format
if (!Guid.TryParse(publicIdString, out Guid publicId))
{
    return BadRequest("Invalid ID format");
}
```

## Testing Checklist

- [ ] All tables have PublicId column
- [ ] Existing records have unique GUIDs
- [ ] Controllers accept GUID parameters
- [ ] Views display and link using GUIDs
- [ ] Forms submit GUIDs
- [ ] API endpoints use GUIDs
- [ ] No integer IDs exposed in URLs
- [ ] Proper error handling for invalid GUIDs

## Rollback Plan

If issues arise:
1. Controllers can temporarily accept both int and GUID
2. Add route constraints to differentiate
3. Keep both columns until fully migrated

```csharp
// Support both during transition
[HttpGet("edit/{id:int}")]
[HttpGet("edit/{publicId:guid}")]
public async Task<IActionResult> Edit(int? id, Guid? publicId)
{
    // Handle both cases
}
```

## Performance Considerations

- GUIDs are 16 bytes vs 4 bytes for int
- Index on PublicId column is crucial
- Use sequential GUIDs for clustering if needed
- Consider caching frequently accessed entities

## Security Best Practices

1. Never expose internal `Id` in responses
2. Always validate GUID format
3. Use authorization checks regardless of ID type
4. Log suspicious patterns of GUID access
5. Consider rate limiting on entity access

## Timeline

- **Week 1**: Add GUID columns, populate existing records
- **Week 2**: Update models and services
- **Week 3**: Update controllers and views
- **Week 4**: Testing and validation
- **Week 5**: Production deployment

## Conclusion

This migration significantly improves security by preventing:
- ID enumeration attacks
- Information disclosure
- Predictable resource access
- Business metric exposure

The dual-column approach (Id + PublicId) allows gradual migration without breaking existing functionality.