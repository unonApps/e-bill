# Entity Framework Database Diagram

## Entity Relationship Diagram

```mermaid
erDiagram
    Organizations ||--o{ Offices : "has"
    Offices ||--o{ SubOffices : "contains"
    Organizations ||--o{ EbillUsers : "employs"
    Offices ||--o{ EbillUsers : "houses"
    SubOffices ||--o{ EbillUsers : "assigns"

    EbillUsers ||--o{ UserPhones : "owns"
    ClassOfServices ||--o{ UserPhones : "defines"

    EbillUsers ||--o{ Airtel : "makes calls"
    EbillUsers ||--o{ PSTNs : "makes calls"
    EbillUsers ||--o{ PrivateWires : "makes calls"
    EbillUsers ||--o{ Safaricom : "makes calls"

    ImportAudits ||--o{ Airtel : "tracks"
    ImportAudits ||--o{ PSTNs : "tracks"
    ImportAudits ||--o{ PrivateWires : "tracks"
    ImportAudits ||--o{ Safaricom : "tracks"

    StagingBatches ||--o{ CallLogStagings : "groups"
    StagingBatches ||--o{ Safaricom : "contains"

    EbillUsers ||--o{ CallLogStagings : "responsible for"
    EbillUsers ||--o{ CallLogStagings : "pays for"
    UserPhones ||--o{ CallLogStagings : "originates"

    CallLogReconciliations ||--o{ CallLogStagings : "tracks versions"

    Organizations {
        int Id PK
        guid PublicId UK
        string Name
        string Code
        string Description
        datetime CreatedDate
    }

    Offices {
        int Id PK
        guid PublicId UK
        string Name
        string Code
        string Description
        int OrganizationId FK
        datetime CreatedDate
    }

    SubOffices {
        int Id PK
        guid PublicId UK
        string Name
        string Code
        string Description
        int OfficeId FK
        string ContactPerson
        string PhoneNumber
        string Email
        string Address
        boolean IsActive
        datetime CreatedDate
    }

    EbillUsers {
        int Id PK
        guid PublicId UK
        string FirstName
        string LastName
        string IndexNumber UK
        string OfficialMobileNumber
        string Email UK
        int OrganizationId FK
        int OfficeId FK
        int SubOfficeId FK
        string Location
        string SupervisorIndexNumber
        string SupervisorName
        string SupervisorEmail
        boolean IsActive
        datetime CreatedDate
    }

    UserPhones {
        int Id PK
        guid PublicId UK
        string IndexNumber FK
        string PhoneNumber
        string PhoneType
        boolean IsPrimary
        boolean IsActive
        int ClassOfServiceId FK
        datetime AssignedDate
        datetime UnassignedDate
        string Location
        string Notes
    }

    ClassOfServices {
        int Id PK
        guid PublicId UK
        string Class
        string Service
        string EligibleStaff
        decimal AirtimeAllowance
        decimal DataAllowance
        decimal HandsetAllowance
        string ServiceStatus
    }

    CallLogStagings {
        int Id PK
        string ExtensionNumber
        datetime CallDate
        string CallNumber
        string CallDestination
        int CallDuration
        decimal CallCostUSD
        string ResponsibleIndexNumber FK
        string PayingIndexNumber FK
        int UserPhoneId FK
        guid BatchId FK
        string SourceSystem
        string ImportType
        boolean IsAdjustment
        boolean HasAnomalies
        string AnomalyTypes JSON
        enum VerificationStatus
        enum ProcessingStatus
    }

    StagingBatches {
        guid Id PK
        string BatchName
        string BatchType
        string BatchCategory
        int TotalRecords
        int VerifiedRecords
        int RejectedRecords
        int RecordsWithAnomalies
        enum BatchStatus
        datetime CreatedDate
        string CreatedBy
    }

    Safaricom {
        int Id PK
        string Ext
        datetime CallDate
        time CallTime
        string Dialed
        string Dest
        decimal Durx
        decimal Cost
        decimal Dur
        string CallType
        int CallMonth
        int CallYear
        string IndexNumber
        int EbillUserId FK
        int ImportAuditId FK
        guid StagingBatchId FK
        string BillingPeriod
        enum ProcessingStatus
        datetime ProcessedDate
    }

    Airtel {
        int Id PK
        string Ext
        datetime CallDate
        time CallTime
        string Dialed
        string Dest
        decimal Durx
        decimal Cost
        decimal Dur
        string CallType
        int CallMonth
        int CallYear
        string IndexNumber
        int EbillUserId FK
        int ImportAuditId FK
        guid StagingBatchId FK
        string BillingPeriod
        enum ProcessingStatus
    }

    PSTNs {
        int Id PK
        string Extension
        string DialedNumber
        datetime CallDate
        time CallTime
        string Destination
        string DestinationLine
        decimal DurationExtended
        decimal Duration
        decimal AmountKSH
        string IndexNumber
        string Carrier
        int CallMonth
        int CallYear
        int EbillUserId FK
        int ImportAuditId FK
        guid StagingBatchId FK
        string BillingPeriod
        enum ProcessingStatus
    }

    PrivateWires {
        int Id PK
        string Extension
        string DialedNumber
        datetime CallDate
        time CallTime
        string Destination
        string DestinationLine
        decimal DurationExtended
        decimal Duration
        decimal AmountUSD
        string IndexNumber
        int CallMonth
        int CallYear
        int EbillUserId FK
        int ImportAuditId FK
        guid StagingBatchId FK
        string BillingPeriod
        enum ProcessingStatus
    }

    ImportAudits {
        int Id PK
        string ImportType
        string FileName
        int TotalRecords
        int SuccessCount
        int ErrorCount
        datetime ImportDate
        string ImportedBy
    }

    AnomalyTypes {
        int Id PK
        string Code UK
        string Name
        string Description
        string Severity
        boolean AutoReject
        boolean IsActive
    }

    AuditLogs {
        int Id PK
        string EntityType
        string EntityId
        string Action
        string Description
        string PerformedBy
        datetime PerformedDate
        string Module
        boolean IsSuccess
    }
```

## Relationship Details

### 1. **Organization Hierarchy**
- **Organizations → Offices** (1:N)
  - One organization can have multiple offices
  - Foreign Key: `Offices.OrganizationId`

- **Offices → SubOffices** (1:N)
  - One office can have multiple sub-offices
  - Foreign Key: `SubOffices.OfficeId`

### 2. **User Management**
- **Organizations → EbillUsers** (1:N)
  - One organization can have multiple users
  - Foreign Key: `EbillUsers.OrganizationId`

- **Offices → EbillUsers** (1:N)
  - One office can house multiple users
  - Foreign Key: `EbillUsers.OfficeId`

- **SubOffices → EbillUsers** (1:N)
  - One sub-office can have multiple users
  - Foreign Key: `EbillUsers.SubOfficeId`

### 3. **Phone Management**
- **EbillUsers → UserPhones** (1:N)
  - One user can have multiple phones
  - Foreign Key: `UserPhones.IndexNumber` references `EbillUsers.IndexNumber`

- **ClassOfServices → UserPhones** (1:N)
  - One class of service can apply to multiple phones
  - Foreign Key: `UserPhones.ClassOfServiceId`

### 4. **Call Records (Source Tables)**
- **EbillUsers → Safaricom/Airtel/PSTNs/PrivateWires** (1:N)
  - One user can have multiple call records
  - Foreign Key: `EbillUserId` in each table

- **ImportAudits → Safaricom/Airtel/PSTNs/PrivateWires** (1:N)
  - One import audit tracks multiple records imported
  - Foreign Key: `ImportAuditId` in each table

- **StagingBatches → Safaricom/Airtel/PSTNs/PrivateWires** (1:N)
  - One staging batch can contain records from multiple sources
  - Foreign Key: `StagingBatchId` in each table

### 5. **Staging & Processing**
- **StagingBatches → CallLogStagings** (1:N)
  - One batch contains multiple staged call logs
  - Foreign Key: `CallLogStagings.BatchId`

- **EbillUsers → CallLogStagings** (1:N) - Two relationships:
  - Responsible User: `CallLogStagings.ResponsibleIndexNumber`
  - Paying User: `CallLogStagings.PayingIndexNumber`

- **UserPhones → CallLogStagings** (1:N)
  - One phone can originate multiple calls
  - Foreign Key: `CallLogStagings.UserPhoneId`

### 6. **Reconciliation**
- **CallLogReconciliations** tracks version history
  - Maintains audit trail of changes to call records
  - Links to staging records for traceability

### 7. **Data Quality & Auditing**

#### AnomalyTypes Table
- **Standalone reference table** - No direct foreign key relationships
- Used as a **lookup table** for anomaly detection
- `CallLogStagings.AnomalyTypes` stores JSON array of anomaly codes
- System matches codes against this table for descriptions and severity
- Example: `["NO_USER", "HIGH_COST"]` in staging references AnomalyTypes records

#### AuditLogs Table
- **Generic audit trail** - No direct foreign key relationships
- Records all system actions across all entities
- Uses `EntityType` and `EntityId` for polymorphic relationships
- Can track changes to ANY table:
  - `EntityType = "EbillUser", EntityId = "123"` → tracks EbillUser changes
  - `EntityType = "CallLogStaging", EntityId = "456"` → tracks staging changes
  - `EntityType = "RefundRequest", EntityId = "789"` → tracks refund changes
- Stores old/new values as JSON for complete audit trail

## Key Features

### Processing Flow
1. **Import Phase**: Raw call data imported from Safaricom, Airtel, PSTN, PrivateWires
2. **Batch Creation**: StagingBatch created to group imports
3. **Staging Phase**: Data consolidated into CallLogStaging for verification
4. **Anomaly Detection**: System checks against AnomalyTypes reference data
5. **Verification Phase**: Admin reviews and approves/rejects
6. **Audit Logging**: All actions recorded in AuditLogs table
7. **Publishing Phase**: Verified data published to production

### Data Integrity
- **Soft References**: Uses IndexNumber for user relationships
- **GUID Protection**: PublicId fields for secure external references
- **Audit Trail**: Comprehensive audit logging for all entities
- **Processing Status**: Tracks data through various stages

### Business Rules
- Users can have multiple phone numbers
- Each phone has a class of service defining allowances
- Call records maintain source system information (Safaricom/Airtel/PSTN/PrivateWire)
- Staging batches group imports for processing
- Anomaly detection uses reference table for consistency
- Audit logs provide complete traceability without foreign keys

## Database Indexes (Recommended)

```sql
-- Performance indexes for frequent queries
CREATE INDEX IX_EbillUsers_IndexNumber ON EbillUsers(IndexNumber);
CREATE INDEX IX_EbillUsers_OrganizationId ON EbillUsers(OrganizationId);
CREATE INDEX IX_UserPhones_IndexNumber ON UserPhones(IndexNumber);
CREATE INDEX IX_CallLogStagings_ResponsibleIndexNumber ON CallLogStagings(ResponsibleIndexNumber);
CREATE INDEX IX_CallLogStagings_UserPhoneId ON CallLogStagings(UserPhoneId);
CREATE INDEX IX_CallLogStagings_BatchId ON CallLogStagings(BatchId);
CREATE INDEX IX_Safaricom_EbillUserId ON Safaricom(EbillUserId);
CREATE INDEX IX_Safaricom_StagingBatchId ON Safaricom(StagingBatchId);
CREATE INDEX IX_AuditLogs_EntityType_EntityId ON AuditLogs(EntityType, EntityId);
```

## Entity Framework Configuration Notes

### Navigation Properties
- Virtual properties enable lazy loading
- Use Include() for eager loading related data
- Configure cascade delete rules carefully

### Composite Keys & Unique Constraints
- IndexNumber serves as natural key for users
- PublicId provides secure external reference
- AnomalyTypes.Code is unique for lookup consistency

### JSON Columns
- AnomalyTypes stored as JSON in CallLogStaging
- DetailedResults stored as JSON in ImportAudits
- Consider using EF Core JSON column support

### Enums
- VerificationStatus: Pending, Verified, Rejected, RequiresReview
- ProcessingStatus: Staged, Processing, Completed, Failed
- BatchStatus: Created, Processing, PartiallyVerified, Verified, Published, Failed