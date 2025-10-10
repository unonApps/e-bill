# Call Logs Staging & Verification Workflow Design

## Overview
This document outlines the design and implementation strategy for a staging and verification system for telecom call logs data. The system will consolidate data from multiple telecom providers, allow admin verification, and push verified data to production.

## Workflow Stages

### 1. Data Ingestion (Azure Data Factory)
- **Source Tables**:
  - `Safaricom` - Safaricom call records
  - `Airtel` - Airtel call records
  - `PSTN` - PSTN/landline call records
  - `PrivateWire` - Private wire/internal call records
- **Destination**: Staging table for consolidation
- **Frequency**: Configurable (daily/weekly/monthly)

### 2. Data Consolidation & Staging
- Merge records from all 4 telecom tables
- Standardize format and fields
- Add metadata (import batch ID, source, timestamp)
- Mark records as "Pending Verification"

### 3. Admin Verification Process
- View staged records with filtering and search
- Automatic anomaly detection
- Manual review capabilities
- Bulk approval/rejection
- Comments and notes on issues

### 4. Production Push
- Move verified records to production table
- Maintain audit trail
- Archive staging records
- Notify relevant users

## Database Schema

### CallLogsStaging Table
```sql
CREATE TABLE CallLogsStaging (
    Id INT IDENTITY(1,1) PRIMARY KEY,

    -- Core Call Data (matching production structure)
    ExtensionNumber NVARCHAR(50),
    CallDate DATETIME,
    CallNumber NVARCHAR(50),
    CallDestination NVARCHAR(100),
    CallEndTime DATETIME,
    CallDuration INT, -- in seconds
    CallCurrencyCode NVARCHAR(10),
    CallCost DECIMAL(18,4),
    CallCostUSD DECIMAL(18,4),
    CallCostKSHS DECIMAL(18,4),
    CallType NVARCHAR(50), -- Voice/SMS/Data
    CallDestinationType NVARCHAR(50), -- Local/International/Mobile

    -- Date Dimensions
    CallYear INT,
    CallMonth INT,

    -- User Mapping
    ResponsibleIndexNumber NVARCHAR(50),
    PayingIndexNumber NVARCHAR(50),

    -- Source Information
    SourceSystem NVARCHAR(50), -- Safaricom/Airtel/PSTN/PrivateWire
    SourceRecordId NVARCHAR(100),

    -- Staging Metadata
    BatchId UNIQUEIDENTIFIER,
    ImportedDate DATETIME DEFAULT GETUTCDATE(),
    ImportedBy NVARCHAR(100),

    -- Verification Status
    VerificationStatus NVARCHAR(50), -- Pending/Verified/Rejected/RequiresReview
    VerificationDate DATETIME NULL,
    VerifiedBy NVARCHAR(100) NULL,
    VerificationNotes NVARCHAR(MAX) NULL,

    -- Anomaly Detection
    HasAnomalies BIT DEFAULT 0,
    AnomalyTypes NVARCHAR(MAX), -- JSON array of anomaly types
    AnomalyDetails NVARCHAR(MAX), -- JSON object with details

    -- Processing Status
    ProcessingStatus NVARCHAR(50), -- Staged/Processing/Completed/Failed
    ProcessedDate DATETIME NULL,
    ErrorDetails NVARCHAR(MAX) NULL,

    -- Audit
    CreatedDate DATETIME DEFAULT GETUTCDATE(),
    ModifiedDate DATETIME NULL,
    ModifiedBy NVARCHAR(100) NULL
);

-- Indexes for performance
CREATE INDEX IX_CallLogsStaging_BatchId ON CallLogsStaging(BatchId);
CREATE INDEX IX_CallLogsStaging_VerificationStatus ON CallLogsStaging(VerificationStatus);
CREATE INDEX IX_CallLogsStaging_ResponsibleIndexNumber ON CallLogsStaging(ResponsibleIndexNumber);
CREATE INDEX IX_CallLogsStaging_CallDate ON CallLogsStaging(CallDate);
```

### StagingBatch Table
```sql
CREATE TABLE StagingBatch (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    BatchName NVARCHAR(100),
    BatchType NVARCHAR(50), -- Manual/Scheduled/ADF

    -- Statistics
    TotalRecords INT,
    VerifiedRecords INT,
    RejectedRecords INT,
    PendingRecords INT,
    RecordsWithAnomalies INT,

    -- Dates
    CreatedDate DATETIME DEFAULT GETUTCDATE(),
    StartProcessingDate DATETIME NULL,
    EndProcessingDate DATETIME NULL,

    -- Status
    BatchStatus NVARCHAR(50), -- Created/Processing/PartiallyVerified/Verified/Published/Failed

    -- User Info
    CreatedBy NVARCHAR(100),
    VerifiedBy NVARCHAR(100) NULL,
    PublishedBy NVARCHAR(100) NULL,

    -- Additional Info
    SourceSystems NVARCHAR(200), -- Comma-separated list
    Notes NVARCHAR(MAX)
);
```

### AnomalyTypes Table
```sql
CREATE TABLE AnomalyTypes (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Code NVARCHAR(50) UNIQUE,
    Name NVARCHAR(100),
    Description NVARCHAR(500),
    Severity NVARCHAR(20), -- Low/Medium/High/Critical
    AutoReject BIT DEFAULT 0,
    IsActive BIT DEFAULT 1
);

-- Sample anomaly types
INSERT INTO AnomalyTypes (Code, Name, Description, Severity, AutoReject) VALUES
('NO_USER', 'No Active User', 'Extension number not linked to any active eBill user', 'High', 0),
('INACTIVE_USER', 'Inactive User', 'Extension linked to inactive eBill user', 'Medium', 0),
('HIGH_COST', 'Unusually High Cost', 'Call cost exceeds threshold', 'High', 0),
('DUPLICATE', 'Duplicate Record', 'Potential duplicate call record', 'Low', 0),
('INVALID_NUMBER', 'Invalid Phone Number', 'Destination number format is invalid', 'Medium', 0),
('FUTURE_DATE', 'Future Date', 'Call date is in the future', 'Critical', 1),
('EXCESSIVE_DURATION', 'Excessive Duration', 'Call duration exceeds reasonable limits', 'Medium', 0);
```

## Models (C#)

### CallLogStaging.cs
```csharp
public class CallLogStaging
{
    public int Id { get; set; }

    // Core Call Data
    public string ExtensionNumber { get; set; }
    public DateTime CallDate { get; set; }
    public string CallNumber { get; set; }
    public string CallDestination { get; set; }
    public DateTime CallEndTime { get; set; }
    public int CallDuration { get; set; }
    public string CallCurrencyCode { get; set; }
    public decimal CallCost { get; set; }
    public decimal CallCostUSD { get; set; }
    public decimal CallCostKSHS { get; set; }
    public string CallType { get; set; }
    public string CallDestinationType { get; set; }
    public int CallYear { get; set; }
    public int CallMonth { get; set; }

    // User Mapping
    public string ResponsibleIndexNumber { get; set; }
    public string PayingIndexNumber { get; set; }

    // Source Information
    public string SourceSystem { get; set; }
    public string SourceRecordId { get; set; }

    // Staging Metadata
    public Guid BatchId { get; set; }
    public DateTime ImportedDate { get; set; }
    public string ImportedBy { get; set; }

    // Verification
    public VerificationStatus VerificationStatus { get; set; }
    public DateTime? VerificationDate { get; set; }
    public string VerifiedBy { get; set; }
    public string VerificationNotes { get; set; }

    // Anomalies
    public bool HasAnomalies { get; set; }
    public List<string> AnomalyTypes { get; set; }
    public Dictionary<string, object> AnomalyDetails { get; set; }

    // Processing
    public ProcessingStatus ProcessingStatus { get; set; }
    public DateTime? ProcessedDate { get; set; }
    public string ErrorDetails { get; set; }

    // Navigation Properties
    public virtual StagingBatch Batch { get; set; }
    public virtual EbillUser ResponsibleUser { get; set; }
    public virtual EbillUser PayingUser { get; set; }
}

public enum VerificationStatus
{
    Pending,
    Verified,
    Rejected,
    RequiresReview
}

public enum ProcessingStatus
{
    Staged,
    Processing,
    Completed,
    Failed
}
```

## Service Layer

### ICallLogStagingService.cs
```csharp
public interface ICallLogStagingService
{
    // Consolidation
    Task<StagingBatch> ConsolidateCallLogsAsync(DateTime startDate, DateTime endDate);
    Task<int> ImportFromSourceTableAsync(string sourceTable, Guid batchId);

    // Anomaly Detection
    Task<List<CallLogAnomaly>> DetectAnomaliesAsync(int stagingId);
    Task<bool> ValidateCallLogAsync(CallLogStaging log);

    // Verification
    Task<bool> VerifyCallLogAsync(int stagingId, string verifiedBy, string notes);
    Task<int> BulkVerifyAsync(List<int> stagingIds, string verifiedBy);
    Task<bool> RejectCallLogAsync(int stagingId, string rejectedBy, string reason);

    // Production Push
    Task<int> PushToProductionAsync(Guid batchId);
    Task<bool> RollbackBatchAsync(Guid batchId);

    // Queries
    Task<PagedResult<CallLogStaging>> GetStagedLogsAsync(StagingFilter filter);
    Task<StagingBatch> GetBatchDetailsAsync(Guid batchId);
    Task<Dictionary<string, int>> GetBatchStatisticsAsync(Guid batchId);
}
```

## UI Components

### Admin Verification Page Features

1. **Dashboard View**
   - Pending verification count
   - Anomaly summary
   - Recent batches
   - Processing status

2. **Staging Table View**
   - Filterable/sortable grid
   - Color-coded anomaly indicators
   - Bulk selection
   - Quick actions (Verify/Reject/Review)

3. **Detail View Modal**
   - Full call record details
   - Anomaly explanations
   - User information lookup
   - Historical data comparison
   - Action buttons with notes

4. **Batch Management**
   - Create new batch
   - View batch statistics
   - Bulk approve batch
   - Export batch report

## Anomaly Detection Rules

### Automatic Checks
1. **User Validation**
   - Check if extension has active eBill user
   - Verify user is active
   - Check authorization limits

2. **Data Quality**
   - Duplicate detection (same call, time, duration)
   - Invalid phone number formats
   - Future dates
   - Negative costs
   - Excessive durations (>24 hours)

3. **Cost Thresholds**
   - Calls exceeding department limits
   - Unusual international destinations
   - High-cost premium numbers

4. **Pattern Analysis**
   - Sudden spike in usage
   - After-hours calling patterns
   - Unusual destinations

## Implementation Phases

### Phase 1: Core Infrastructure
- Create staging tables
- Implement basic models
- Set up consolidation service

### Phase 2: Verification UI
- Admin dashboard
- Staging grid with filters
- Basic verify/reject actions

### Phase 3: Anomaly Detection
- Implement validation rules
- Automatic anomaly flagging
- Anomaly reporting

### Phase 4: Production Push
- Push to production logic
- Rollback capabilities
- Audit trail

### Phase 5: Advanced Features
- Bulk operations
- Export/reporting
- Email notifications
- API for Azure Data Factory

## Security Considerations
- Role-based access (Admin only)
- Audit all verification actions
- Secure batch operations
- Data validation before production push

## Performance Optimization
- Indexed queries
- Batch processing
- Async operations
- Pagination for large datasets
- Caching for reference data

## Success Metrics
- Reduction in billing disputes
- Faster verification cycles
- Anomaly detection rate
- Admin productivity improvement