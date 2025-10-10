# Interim Billing for Staff Separation - Design Document

## Current State Analysis

The system already has foundational support for interim billing:
- **StagingBatch** model has `BatchCategory` field supporting "MONTHLY", "INTERIM", "CORRECTION"
- **CallLogStaging** model has `ImportType` field supporting "MONTHLY" or "INTERIM"
- **InterimUpdate** model exists for tracking billing updates and corrections
- **BillingPeriod** model distinguishes between monthly and interim records

## Proposed Enhancement for Staff Separation Interim Bills

### 1. Database Schema Enhancements

#### Add to StagingBatch Model:
```csharp
// Staff separation tracking
public bool IsStaffSeparation { get; set; } = false;
public string? SeparatingStaffIndexNumber { get; set; }
public string? SeparatingStaffName { get; set; }
public DateTime? SeparationDate { get; set; }
public string? SeparationReason { get; set; } // RESIGNATION, RETIREMENT, TERMINATION, END_OF_CONTRACT
```

#### Add to CallLogStaging Model:
```csharp
// Link to specific staff member for interim bills
public string? StaffIndexNumber { get; set; }
public bool IsStaffSeparationBill { get; set; } = false;
```

### 2. User Interface Enhancements

#### A. Consolidation Modal Updates

Add a toggle/tab system in the consolidation modal:

```html
<!-- Consolidation Type Selection -->
<div class="btn-group w-100 mb-3" role="group">
    <input type="radio" class="btn-check" name="consolidationType" id="monthlyConsolidation" checked>
    <label class="btn btn-outline-primary" for="monthlyConsolidation">
        <i class="bi bi-calendar-month"></i> Monthly Consolidation
    </label>

    <input type="radio" class="btn-check" name="consolidationType" id="interimConsolidation">
    <label class="btn btn-outline-primary" for="interimConsolidation">
        <i class="bi bi-person-x"></i> Staff Separation (Interim)
    </label>
</div>

<!-- Staff Separation Details (shown when interim selected) -->
<div id="staffSeparationDetails" class="d-none">
    <div class="alert alert-info mb-3">
        <i class="bi bi-info-circle"></i>
        Import interim bills for staff members separating from the organization
    </div>

    <div class="row">
        <div class="col-md-6">
            <label class="form-label">Staff Index Number</label>
            <input type="text" class="form-control" name="StaffIndexNumber">
        </div>
        <div class="col-md-6">
            <label class="form-label">Staff Name</label>
            <input type="text" class="form-control" name="StaffName">
        </div>
    </div>

    <div class="row mt-3">
        <div class="col-md-6">
            <label class="form-label">Separation Date</label>
            <input type="date" class="form-control" name="SeparationDate">
        </div>
        <div class="col-md-6">
            <label class="form-label">Separation Reason</label>
            <select class="form-control" name="SeparationReason">
                <option value="">Select reason...</option>
                <option value="RESIGNATION">Resignation</option>
                <option value="RETIREMENT">Retirement</option>
                <option value="TERMINATION">Termination</option>
                <option value="END_OF_CONTRACT">End of Contract</option>
            </select>
        </div>
    </div>

    <div class="mt-3">
        <label class="form-label">Select Billing Period</label>
        <input type="date" class="form-control" name="InterimBillingDate">
        <small class="text-muted">Select the date range for the interim bill</small>
    </div>
</div>
```

### 3. Backend Implementation

#### A. Enhanced Consolidation Handler

```csharp
public async Task<IActionResult> OnPostConsolidateInterimAsync(InterimConsolidationInput input)
{
    try
    {
        // Validate staff member exists
        var staffMember = await _context.EbillUsers
            .FirstOrDefaultAsync(e => e.IndexNumber == input.StaffIndexNumber);

        if (staffMember == null)
        {
            return new JsonResult(new { error = "Staff member not found" });
        }

        // Create interim batch
        var batch = new StagingBatch
        {
            BatchName = $"Interim - {input.StaffName} - {input.SeparationDate:yyyy-MM-dd}",
            BatchType = "Manual",
            BatchCategory = "INTERIM",
            IsStaffSeparation = true,
            SeparatingStaffIndexNumber = input.StaffIndexNumber,
            SeparatingStaffName = input.StaffName,
            SeparationDate = input.SeparationDate,
            SeparationReason = input.SeparationReason,
            CreatedBy = User.Identity?.Name ?? "System",
            CreatedDate = DateTime.UtcNow,
            BatchStatus = BatchStatus.Created
        };

        _context.StagingBatches.Add(batch);
        await _context.SaveChangesAsync();

        // Import records for the specific staff member
        var recordsImported = await ImportInterimRecordsForStaff(
            batch.Id,
            input.StaffIndexNumber,
            input.BillingStartDate,
            input.BillingEndDate
        );

        batch.TotalRecords = recordsImported;
        await _context.SaveChangesAsync();

        return new JsonResult(new {
            success = true,
            batchId = batch.Id,
            recordsImported = recordsImported
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error creating interim batch for staff separation");
        return new JsonResult(new { error = ex.Message });
    }
}

private async Task<int> ImportInterimRecordsForStaff(
    Guid batchId,
    string staffIndexNumber,
    DateTime startDate,
    DateTime endDate)
{
    var recordsImported = 0;

    // Get user's phone numbers
    var userPhones = await _context.UserPhones
        .Where(up => up.UserIndexNumber == staffIndexNumber && up.IsActive)
        .Select(up => up.PhoneNumber)
        .ToListAsync();

    // Import from Safaricom
    var safaricomRecords = await _context.Safaricoms
        .Where(s => userPhones.Contains(s.CallNumber) &&
                   s.CallDate >= startDate &&
                   s.CallDate <= endDate &&
                   s.ProcessingStatus != ProcessingStatus.Completed)
        .ToListAsync();

    foreach (var record in safaricomRecords)
    {
        var stagingRecord = new CallLogStaging
        {
            BatchId = batchId,
            ImportType = "INTERIM",
            IsStaffSeparationBill = true,
            StaffIndexNumber = staffIndexNumber,
            ExtensionNumber = record.CallNumber,
            CallDate = record.CallDate ?? DateTime.MinValue,
            CallNumber = record.CalledNumber ?? "",
            CallDestination = record.CallDestination ?? "",
            CallDuration = (int)(record.Dur ?? 0),
            CallCost = record.AmountKES ?? 0,
            CallCostUSD = record.AmountUSD ?? 0,
            SourceSystem = "Safaricom",
            SourceRecordId = record.Id,
            CreatedDate = DateTime.UtcNow
        };

        _context.CallLogStagings.Add(stagingRecord);
        record.ProcessingStatus = ProcessingStatus.Processing;
        recordsImported++;
    }

    // Repeat for Airtel, PSTN, PrivateWire...
    // Similar logic for other providers

    await _context.SaveChangesAsync();
    return recordsImported;
}
```

### 4. Verification Workflow

#### A. Enhanced Staging View
- Add filter for "Interim Bills" vs "Monthly Bills"
- Show staff separation details in the batch card
- Highlight interim bills with different color coding

#### B. Quick Actions for Interim Bills
- "Verify All for Staff" button to bulk verify all records for a separating staff member
- Export functionality specific to staff separation reports
- Automatic flagging of anomalies specific to separation (e.g., calls after separation date)

### 5. Reporting Enhancements

#### A. Staff Separation Report
```csharp
public async Task<IActionResult> OnGetStaffSeparationReportAsync(string indexNumber)
{
    var separationBatches = await _context.StagingBatches
        .Where(b => b.IsStaffSeparation &&
                   b.SeparatingStaffIndexNumber == indexNumber)
        .OrderByDescending(b => b.CreatedDate)
        .Select(b => new
        {
            b.BatchName,
            b.SeparationDate,
            b.SeparationReason,
            b.TotalRecords,
            b.BatchStatus,
            TotalAmount = _context.CallLogStagings
                .Where(c => c.BatchId == b.Id)
                .Sum(c => c.CallCostUSD)
        })
        .ToListAsync();

    return new JsonResult(separationBatches);
}
```

### 6. Integration Points

#### A. Dashboard Widget
Add a widget showing:
- Pending interim bills for separation
- Recent staff separations processed
- Total interim bills this month

#### B. Notifications
- Alert when interim bill is uploaded
- Reminder for pending verifications for separating staff
- Completion notification when separation billing is finalized

### 7. Implementation Steps

1. **Phase 1: Database Updates**
   - Add new columns to existing tables
   - Create migration scripts
   - Update entity models

2. **Phase 2: UI Enhancement**
   - Update consolidation modal
   - Add interim bill filters
   - Create staff separation form

3. **Phase 3: Backend Logic**
   - Implement interim consolidation handler
   - Add staff-specific import logic
   - Create verification workflows

4. **Phase 4: Testing & Validation**
   - Test interim bill import
   - Verify separation date validations
   - Test reporting accuracy

### 8. Benefits of This Approach

1. **Separation of Concerns**: Interim bills are clearly distinguished from monthly bills
2. **Audit Trail**: Complete tracking of staff separation billing
3. **Flexibility**: Supports different separation scenarios
4. **Integration**: Works within existing staging/verification framework
5. **Visibility**: Clear indication of what's interim vs monthly

### 9. Sample UI Flow

1. User clicks "Consolidate New Batch"
2. Selects "Staff Separation (Interim)" tab
3. Enters staff details and separation date
4. System shows preview of records to import
5. User confirms and imports
6. Records appear in staging with "INTERIM" badge
7. Verification process same as monthly but with separation context
8. Push to production marks as interim billing

This design leverages the existing infrastructure while adding specific support for staff separation scenarios.